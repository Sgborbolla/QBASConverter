using QBASSuite.Devices;

namespace QBASSuite.Engine;

/// <summary>
/// El multicopiador de verdad. Cada disco tiene SU trabajo, con su propia cola
/// y su propio hilo de copia. Eso significa:
///
///  - Puedes lanzar una tanda a unos discos y, mientras siguen copiando, lanzar
///    otra a otros discos: los nuevos arrancan a la vez, sin esperar a nadie.
///  - Si le tiras algo a un disco que YA esta copiando, no se pierde: se le
///    anade a su cola y lo copia despues de lo que esta haciendo.
///  - Cada disco lleva su velocidad real (MB/s) y su tiempo restante.
///  - Puedes pausar, continuar, cancelar y dar prioridad por disco.
///
/// No hay limite de discos ni de trabajos: todos los que Windows ve.
/// </summary>
public sealed class CopyJobsManager : IDisposable
{
    private readonly List<TrabajoDestino> _trabajos = new();
    private readonly object _candado = new();
    private readonly CopyOptions _opciones;

    /// <summary>Modo de velocidad: Si es true, cada disco usa bloques y carriles
    /// adaptados a su tipo REAL (SSD / disco duro / USB / red).</summary>
    public bool VelocidadAdaptativa { get; set; } = true;

    /// <summary>Verificar por SHA-256 (apagado = maxima velocidad).</summary>
    public bool Verificar { get; set; }

    /// <summary>Se dispara cuando algo cambia, para repintar la interfaz.</summary>
    public event Action? Cambio;

    /// <summary>Se dispara cuando un disco termina (etiqueta, ficheros, bytes, errores).</summary>
    public event Action<string, long, long, long>? Terminado;

    public CopyJobsManager(CopyOptions? opciones = null)
    {
        _opciones = opciones ?? new CopyOptions();
    }

    /// <summary>Todos los trabajos (uno por disco).</summary>
    public IReadOnlyList<TrabajoDestino> Trabajos
    {
        get { lock (_candado) return _trabajos.ToList(); }
    }

    public bool AlgunoCopiando
    {
        get
        {
            lock (_candado)
                return _trabajos.Any(t => t.Estado is TrabajoEstado.Copiando or TrabajoEstado.EnCola or TrabajoEstado.Pausado);
        }
    }

    /// <summary>
    /// Lanza una tanda. Devuelve la lista de discos que arrancaron o se les
    /// anadio a su cola. Si un disco ya existe, se le ANADE a su cola; si no
    /// existe, se crea su trabajo y arranca ya.
    /// </summary>
    public void Enviar(IReadOnlyList<FileEntry> ficheros, IReadOnlyList<Destino> destinos)
    {
        if (ficheros.Count == 0 || destinos.Count == 0) return;

        var nuevo = new List<TrabajoDestino>();
        lock (_candado)
        {
            foreach (var d in destinos)
            {
                var t = _trabajos.FirstOrDefault(x =>
                    string.Equals(x.Destino.Raiz, d.Raiz, StringComparison.OrdinalIgnoreCase));

                if (t == null)
                {
                    t = new TrabajoDestino { Destino = d };
                    _trabajos.Add(t);
                    nuevo.Add(t);
                }

                foreach (var f in ficheros)
                    t.Pendientes.Add(f);

                t.BytesTotal += ficheros.Sum(f => f.Length);
                t.FicherosTotal += ficheros.Count;
            }

            // Los que tienen prioridad van primero, y la regla automatica decide el resto.
            Ordenar();
        }

        foreach (var t in nuevo)
            Arrancar(t);

        Cambio?.Invoke();
    }

    private void Arrancar(TrabajoDestino t)
    {
        if (t.Estado == TrabajoEstado.Copiando) return;
        t.Cts = new CancellationTokenSource();
        t.Estado = TrabajoEstado.Copiando;
        t.Inicio = DateTime.UtcNow;
        var ct = t.Cts.Token;
        _ = Task.Run(() => BucleAsync(t, ct), ct);
    }

    /// <summary>El hilo de copia de UN disco. Coge lo que haya en su cola y lo
    /// copia; cuando se queda sin cola, termina. Todo con SU bloque y SUS carriles.</summary>
    private async Task BucleAsync(TrabajoDestino t, CancellationToken ct)
    {
        try
        {
            var motor = new CopyEngine(OpcionesPara(t.Destino.Info));
            var reloj = System.Diagnostics.Stopwatch.StartNew();
            var ultimoTiempo = 0.0;
            var ultimosBytes = 0L;

            while (true)
            {
                List<FileEntry> tanda;
                lock (_candado)
                {
                    if (t.Pendientes.Count == 0) break;
                    tanda = new List<FileEntry>(t.Pendientes);
                    t.Pendientes.Clear();
                }

                var progreso = new Progress<CopyProgress>(p =>
                {
                    lock (_candado)
                    {
                        t.BytesHechos = t.BytesSimples + p.BytesDone;
                        t.FicherosHechos = t.FicherosSimples + p.FilesDone;
                        t.FicheroActual = p.FicheroActual;
                        var seg = reloj.Elapsed.TotalSeconds;
                        if (seg > 0.4)
                        {
                            var inst = (t.BytesHechos - ultimosBytes) / Math.Max(0.001, seg - ultimoTiempo);
                            // media suave, para que el numero no baile
                            t.Velocidad = t.Velocidad <= 0 ? inst : (t.Velocidad * 0.7 + inst * 0.3);
                            t.Restante = t.Velocidad > 0
                                ? Math.Max(0, (t.BytesTotal - t.BytesHechos) / t.Velocidad)
                                : double.NaN;
                            ultimoTiempo = seg;
                            ultimosBytes = t.BytesHechos;
                        }
                    }
                    Cambio?.Invoke();
                });

                var res = await motor.CopiarAsync(tanda, new[] { t.Destino }, progreso, ct: ct)
                                   .ConfigureAwait(false);

                lock (_candado)
                {
                    t.BytesSimples += res.BytesCopied;
                    t.FicherosSimples += res.FilesCopied + res.FilesSkipped;
                    t.BytesHechos = t.BytesSimples;
                    t.FicherosHechos = t.FicherosSimples;
                    t.Errores += res.FilesFailed;
                }

                // Si no habia nada que copiar y no hay errores, no repetir.
                if (res.FilesCopied == 0 && res.FilesSkipped == 0 && res.FilesFailed == 0) break;
            }

            t.BytesHechos = t.BytesTotal; // completa la barra
            t.Estado = t.Errores > 0 ? TrabajoEstado.Hecho : TrabajoEstado.Hecho;
            t.Restante = 0;

            var segundos = reloj.Elapsed.TotalSeconds;
            if (segundos > 0)
                DeviceManager.MedirVelocidad(t.Destino.Info?.Id ?? t.Destino.Raiz, t.BytesTotal / segundos);
        }
        catch (OperationCanceledException)
        {
            t.Estado = TrabajoEstado.Cancelado;
        }
        catch (Exception ex)
        {
            t.Errores++;
            t.Estado = TrabajoEstado.Error;
            t.FicheroActual = CopyEngine.Motivo(ex);
        }
        finally
        {
            Terminado?.Invoke(t.Nombre, t.FicherosHechos, t.BytesHechos, t.Errores);
            Cambio?.Invoke();
        }
    }

    /// <summary>Bloque y carriles adaptados al disco REAL (maxima velocidad).</summary>
    private CopyOptions OpcionesPara(DeviceInfo? info)
    {
        var o = new CopyOptions
        {
            Verify = Verificar,
            CopyTimestamps = true,
            CopyAttributes = true,
            MinFreeSpace = 50L * 1024 * 1024,
            Structure = _opciones.Structure,
            Conflict = _opciones.Conflict,
            Move = _opciones.Move,
            Mirror = _opciones.Mirror,
            Retries = _opciones.Retries,
            DateMode = _opciones.DateMode,
            DateFrom = _opciones.DateFrom,
            DateTo = _opciones.DateTo,
            IncludePatterns = _opciones.IncludePatterns,
            ExcludePatterns = _opciones.ExcludePatterns
        };

        if (!VelocidadAdaptativa) return o;

        // Cada disco a su tope: SSD/NVMe bloques grandes y mas carriles;
        // USB lenta bloques pequenos y menos carriles para no ahogarla.
        var esSsd = info?.IsSsd == true;
        var esLento = info?.EsLento == true ||
                      info?.Kind is DeviceKind.UsbFlashSlow or DeviceKind.UsbExternalSlow;
        var esRed = info?.Kind == DeviceKind.NetworkShare;

        if (esLento) { o.BufferSize = 1 << 20; o.ParallelFiles = 1; }
        else if (esRed) { o.BufferSize = 4 << 20; o.ParallelFiles = 2; }
        else if (esSsd) { o.BufferSize = 4 << 20; o.ParallelFiles = 4; }
        else { o.BufferSize = 2 << 20; o.ParallelFiles = 2; }

        return o;
    }

    // ---- control por disco ----

    public void Pausar(TrabajoDestino t)
    {
        lock (_candado)
        {
            if (t.Estado != TrabajoEstado.Copiando) return;
            t.Cts?.Cancel();
            t.Estado = TrabajoEstado.Pausado;
        }
        Cambio?.Invoke();
    }

    public void Continuar(TrabajoDestino t)
    {
        if (t.Estado != TrabajoEstado.Pausado) return;
        Arrancar(t);
        Cambio?.Invoke();
    }

    public void Cancelar(TrabajoDestino t)
    {
        lock (_candado)
        {
            t.Cts?.Cancel();
            t.Pendientes.Clear();
            t.Estado = TrabajoEstado.Cancelado;
        }
        Cambio?.Invoke();
    }

    /// <summary>Pausa o continua segun en que punto este.</summary>
    public void AlternarPausa(TrabajoDestino t)
    {
        if (t.Estado == TrabajoEstado.Pausado) Continuar(t);
        else Pausar(t);
    }

    /// <summary>
    /// Regla de prioridad automatica: cuando varios discos copian a la vez, quien
    /// se considera "primero". No para a nadie: solo decide el orden de la cola
    /// y a quien se le da preferencia al repartir.
    /// </summary>
    public PrioridadAuto PrioridadAutomatica { get; set; } = PrioridadAuto.ElQueMenosLeQueda;

    public void Prioridad(TrabajoDestino t, bool on)
    {
        lock (_candado)
        {
            t.Prioridad = on;
            Ordenar();
            // Si la acabas de marcar y estaba esperando, arranca YA, sin parar a nadie.
            if (on && t.Estado == TrabajoEstado.EnCola) Arrancar(t);
        }
        Cambio?.Invoke();
    }

    /// <summary>Reordena la lista segun la prioridad manual y la automatica.</summary>
    public void Ordenar()
    {
        _trabajos.Sort((a, b) =>
        {
            // 1) La marca manual manda siempre.
            if (a.Prioridad != b.Prioridad) return a.Prioridad ? -1 : 1;

            // 2) La regla automatica decide entre los demas.
            int cmp = PrioridadAutomatica switch
            {
                PrioridadAuto.ElQueMenosLeQueda => (a.BytesTotal - a.BytesHechos).CompareTo(b.BytesTotal - b.BytesHechos),
                PrioridadAuto.ElMasRapido => (b.Velocidad > 0 ? b.Velocidad : double.MaxValue)
                                             .CompareTo(a.Velocidad > 0 ? a.Velocidad : double.MaxValue),
                PrioridadAuto.ElMasLento => a.Velocidad.CompareTo(b.Velocidad),
                _ => 0
            };
            if (cmp != 0) return cmp;

            // 3) Por nombre, para que no baile.
            return string.Compare(a.Nombre, b.Nombre, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>Olvida los trabajos ya terminados/cancelados.</summary>
    public void LimpiarTerminados()
    {
        lock (_candado)
        {
            _trabajos.RemoveAll(t => t.Estado is TrabajoEstado.Hecho or TrabajoEstado.Cancelado or TrabajoEstado.Error);
        }
        Cambio?.Invoke();
    }

    public void Dispose()
    {
        lock (_candado)
            foreach (var t in _trabajos)
                try { t.Cts?.Cancel(); } catch { }
    }
}
