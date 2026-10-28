using System.Text.Json;
using QBASSuite.Util;

namespace QBASSuite.Ui;

/// <summary>Una ficha del índice del buscador: ruta, tamaño y fecha.</summary>
public sealed class EntradaIndice
{
    public string Ruta { get; set; } = "";
    public long Tamano { get; set; }
    public DateTime Fecha { get; set; }
}

/// <summary>
/// El BUSCADOR del multicopiador. Busca archivos por nombre o extensión en las
/// raíces que elijas (discos enteros o carpetas sueltas), con un índice propio
/// que se guarda junto al programa: la primera búsqueda recorre el disco y las
/// siguientes son instantáneas.
///
/// Lo encontrado se puede marcar y añadir a la copia con un botón.
/// </summary>
public sealed class Buscador : Form
{
    private static string CarpetaPrograma
    {
        get
        {
            var exe = Environment.ProcessPath;
            var dir = exe != null ? Path.GetDirectoryName(exe) : null;
            if (string.IsNullOrEmpty(dir)) dir = AppContext.BaseDirectory;
            return dir!;
        }
    }

    private static string FicheroIndice => Path.Combine(CarpetaPrograma, "QBASFreemanCopy-Converter.indice.json");

    /// <summary>Índice en memoria: raíz → ficheros encontrados.</summary>
    private static Dictionary<string, List<EntradaIndice>>? _indice;

    private readonly Tema _tema;
    private readonly CheckedListBox _raices;
    private readonly TextBox _txtPatron;
    private readonly ListaOscura _lista;
    private readonly Label _lblEstado;
    private readonly BotonRedondo _btnBuscar;
    private readonly BotonRedondo _btnDetener;
    private readonly BotonRedondo _btnIndexar;
    private readonly BotonRedondo _btnAnadir;
    private CancellationTokenSource? _cts;

    /// <summary>Rutas marcadas para añadir a la copia (lo lee la ventana principal).</summary>
    public List<string> Seleccionados { get; } = new();

    public Buscador(Tema tema)
    {
        _tema = tema;

        Text = "Buscar archivos · añadir a la copia";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(880, 560);
        MinimumSize = new Size(700, 440);
        BackColor = _tema.Panel;
        ForeColor = _tema.Tinta;
        Font = new Font("Segoe UI", 9f);
        ShowInTaskbar = false;

        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
            BackColor = _tema.Panel, Padding = new Padding(10)
        };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        // --- fila 1: qué se busca ---
        var filaBuscar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
        };
        filaBuscar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        filaBuscar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filaBuscar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        filaBuscar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        filaBuscar.Controls.Add(new Label
        {
            Text = "Buscar:", Dock = DockStyle.Fill, ForeColor = _tema.Tinta2,
            TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0)
        }, 0, 0);

        _txtPatron = new TextBox
        {
            Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle,
            BackColor = _tema.Panel2, ForeColor = _tema.Tinta,
            PlaceholderText = "nombre, *.mkv, *.pdf…", Margin = new Padding(0, 6, 8, 6)
        };
        _txtPatron.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Buscar(); }
        };
        filaBuscar.Controls.Add(_txtPatron, 1, 0);

        _btnBuscar = new BotonRedondo(_tema)
        {
            Text = "Buscar", Dock = DockStyle.Fill, Radio = 4, Primario = true,
            Margin = new Padding(0, 5, 0, 5)
        };
        _btnBuscar.Click += (_, _) => Buscar();
        filaBuscar.Controls.Add(_btnBuscar, 2, 0);
        raiz.Controls.Add(filaBuscar, 0, 0);

        // --- fila 2: raíces donde buscar ---
        var cajaRaices = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        _raices = new CheckedListBox
        {
            Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle,
            BackColor = _tema.Panel2, ForeColor = _tema.Tinta
        };
        cajaRaices.Controls.Add(_raices);

        var botonesRaices = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, Width = 190, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(6, 0, 0, 0)
        };

        var bCarpeta = new BotonRedondo(_tema)
        {
            Text = "Añadir carpeta…", Width = 178, Height = 28, Radio = 4, Margin = new Padding(0, 0, 0, 4)
        };
        bCarpeta.Click += (_, _) => AnadirCarpetaRaiz();

        var bQuitar = new BotonRedondo(_tema)
        {
            Text = "Quitar de la lista", Width = 178, Height = 28, Radio = 4, Margin = new Padding(0, 0, 0, 4)
        };
        bQuitar.Click += (_, _) => QuitarRaiz();

        _btnIndexar = new BotonRedondo(_tema)
        {
            Text = "Indexar las marcadas", Width = 178, Height = 28, Radio = 4, Margin = new Padding(0, 0, 0, 4)
        };
        _btnIndexar.Click += (_, _) => IndexarMarcadas();

        _btnDetener = new BotonRedondo(_tema)
        {
            Text = "Detener", Width = 178, Height = 28, Radio = 4, Enabled = false, Margin = new Padding(0)
        };
        _btnDetener.Click += (_, _) => _cts?.Cancel();

        botonesRaices.Controls.Add(bCarpeta);
        botonesRaices.Controls.Add(bQuitar);
        botonesRaices.Controls.Add(_btnIndexar);
        botonesRaices.Controls.Add(_btnDetener);
        cajaRaices.Controls.Add(botonesRaices);

        raiz.Controls.Add(cajaRaices, 0, 1);

        // --- fila 3: resultados ---
        _lista = new ListaOscura(_tema)
        {
            Dock = DockStyle.Fill, FondoFilas = _tema.Panel2, Font = new Font("Segoe UI", 9f),
            MultiSelect = true
        };
        _lista.Columns.Add("Archivo", 300);
        _lista.Columns.Add("Carpeta", 330);
        _lista.Columns.Add("Tamaño", 100, HorizontalAlignment.Right);
        _lista.Columns.Add("Modificado", 120, HorizontalAlignment.Right);
        _lista.DoubleClick += (_, _) => AnadirSeleccionados();
        raiz.Controls.Add(_lista, 0, 2);

        _lblEstado = new Label
        {
            Dock = DockStyle.Fill, ForeColor = _tema.Tinta2,
            Font = new Font("Segoe UI", 8.25f), TextAlign = ContentAlignment.MiddleLeft,
            Text = "  Elige las raíces y pulsa Buscar. La primera vez recorre el disco; luego es instantáneo."
        };
        raiz.Controls.Add(_lblEstado, 0, 3);

        var acciones = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0)
        };

        var bCerrar = new BotonRedondo(_tema)
        {
            Text = "Cerrar", Width = 96, Height = 30, Radio = 4,
            DialogResult = DialogResult.Cancel, Margin = new Padding(6, 4, 0, 0)
        };
        _btnAnadir = new BotonRedondo(_tema)
        {
            Text = "Añadir a la copia", Width = 160, Height = 30, Radio = 4, Primario = true,
            Margin = new Padding(6, 4, 0, 0)
        };
        _btnAnadir.Click += (_, _) => AnadirSeleccionados();

        acciones.Controls.Add(bCerrar);
        acciones.Controls.Add(_btnAnadir);
        raiz.Controls.Add(acciones, 0, 4);

        Controls.Add(raiz);
        CancelButton = bCerrar;

        CargarRaices();

        FormClosed += (_, _) => { try { _cts?.Cancel(); } catch { } };
    }

    // =====================================================================
    //  Raíces
    // =====================================================================

    private void CargarRaices()
    {
        _raices.Items.Clear();

        var guardadas = (Opciones.LeerTexto("busquedaRaices", "") ?? "")
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var puestas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Discos listos.
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                try
                {
                    if (!d.IsReady) continue;
                    var raiz = d.RootDirectory.FullName;
                    var texto = string.IsNullOrWhiteSpace(d.VolumeLabel)
                        ? raiz
                        : $"{raiz}  ({d.VolumeLabel})";
                    _raices.Items.Add(texto, guardadas.Contains(raiz, StringComparer.OrdinalIgnoreCase));
                    puestas.Add(raiz);
                }
                catch { }
            }
        }
        catch { }

        // Carpetas guardadas que no son una unidad.
        foreach (var r in guardadas)
        {
            if (puestas.Contains(r) || !Directory.Exists(r)) continue;
            _raices.Items.Add(r, true);
            puestas.Add(r);
        }
    }

    private List<string> RaicesMarcadas()
    {
        var lista = new List<string>();
        for (var i = 0; i < _raices.Items.Count; i++)
        {
            if (!_raices.GetItemChecked(i)) continue;
            var texto = _raices.Items[i]?.ToString() ?? "";
            // Quita el "  (etiqueta)" que se añade a los discos.
            var parentesis = texto.IndexOf("  (", StringComparison.Ordinal);
            if (parentesis > 0) texto = texto[..parentesis];
            texto = texto.Trim();
            if (texto.Length > 0) lista.Add(texto);
        }
        return lista;
    }

    private void GuardarRaices()
    {
        var marcadas = RaicesMarcadas();
        Opciones.Guardar("busquedaRaices", string.Join("|", marcadas));
    }

    private void AnadirCarpetaRaiz()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Elige una carpeta para buscar dentro",
            ShowNewFolderButton = false
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var ruta = dlg.SelectedPath;
        if (!Directory.Exists(ruta)) return;
        _raices.Items.Add(ruta, true);
        GuardarRaices();
    }

    private void QuitarRaiz()
    {
        if (_raices.SelectedIndex < 0) return;
        _raices.Items.RemoveAt(_raices.SelectedIndex);
        GuardarRaices();
    }

    // =====================================================================
    //  Índice
    // =====================================================================

    private static Dictionary<string, List<EntradaIndice>> Indice
    {
        get
        {
            if (_indice != null) return _indice;
            _indice = new Dictionary<string, List<EntradaIndice>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (File.Exists(FicheroIndice))
                {
                    var cargado = JsonSerializer.Deserialize<Dictionary<string, List<EntradaIndice>>>(
                        File.ReadAllText(FicheroIndice));
                    if (cargado != null)
                        foreach (var kv in cargado)
                            _indice[kv.Key] = kv.Value;
                }
            }
            catch { }
            return _indice;
        }
    }

    private static void GuardarIndice()
    {
        try
        {
            var json = JsonSerializer.Serialize(Indice);
            File.WriteAllText(FicheroIndice, json);
        }
        catch { }
    }

    /// <summary>Recorre una raíz y devuelve sus ficheros (sin hash: rápido).</summary>
    private static List<EntradaIndice> ConstruirIndice(string raiz, CancellationToken ct, Action<long> progreso)
    {
        var lista = new List<EntradaIndice>();
        const int tope = 300_000;

        var pila = new Stack<string>();
        pila.Push(raiz);
        while (pila.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var dir = pila.Pop();

            try
            {
                foreach (var sub in Directory.EnumerateDirectories(dir))
                {
                    ct.ThrowIfCancellationRequested();
                    var nombre = Path.GetFileName(sub);
                    // Nada de carpetas de sistema que no aportan y frenan todo.
                    if (nombre.Equals("$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase)
                        || nombre.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase)) continue;
                    pila.Push(sub);
                }
            }
            catch { }

            try
            {
                foreach (var f in Directory.EnumerateFiles(dir))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var info = new FileInfo(f);
                        lista.Add(new EntradaIndice
                        {
                            Ruta = f, Tamano = info.Length, Fecha = info.LastWriteTimeUtc
                        });
                    }
                    catch { }
                    if (lista.Count >= tope) return lista;
                }
            }
            catch { }

            if (lista.Count % 400 == 0) progreso(lista.Count);
        }
        progreso(lista.Count);
        return lista;
    }

    private async void IndexarMarcadas()
    {
        if (_cts != null) return;
        var raices = RaicesMarcadas();
        if (raices.Count == 0) { _lblEstado.Text = "  Marca al menos una raíz donde buscar."; return; }

        _cts = new CancellationTokenSource();
        Bloquear(true);

        var progreso = new Progress<string>(texto => _lblEstado.Text = "  " + texto);
        try
        {
            await Task.Run(() =>
            {
                foreach (var r in raices)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var lista = ConstruirIndice(r, _cts.Token, n =>
                        ((IProgress<string>)progreso).Report($"Indexando {r}… {n:N0} archivos"));
                    Indice[r] = lista;
                }
                GuardarIndice();
            }, _cts.Token);

            _lblEstado.Text = $"  Índice listo: {Indice.Count} raíz(es), {Indice.Values.Sum(v => v.Count):N0} archivos.";
        }
        catch (OperationCanceledException) { _lblEstado.Text = "  Indexado detenido."; }
        catch (Exception ex) { _lblEstado.Text = "  No se pudo indexar: " + ex.Message; }
        finally
        {
            _cts.Dispose();
            _cts = null;
            Bloquear(false);
            GuardarRaices();
        }
    }

    // =====================================================================
    //  Buscar
    // =====================================================================

    private async void Buscar()
    {
        if (_cts != null) return;
        var raices = RaicesMarcadas();
        if (raices.Count == 0) { _lblEstado.Text = "  Marca al menos una raíz donde buscar."; return; }

        var patron = _txtPatron.Text.Trim();
        var patrones = Matcher.Patrones(patron);
        var soloExtension = patrones.Length == 0
                            && patron.Length > 0
                            && !patron.Contains('.')
                            && !patron.Contains('*')
                            && !patron.Contains('?');

        _cts = new CancellationTokenSource();
        Bloquear(true);
        _lblEstado.Text = "  Buscando…";

        var progreso = new Progress<string>(texto => _lblEstado.Text = "  " + texto);
        try
        {
            var encontrados = await Task.Run(() =>
            {
                var salida = new List<EntradaIndice>();

                foreach (var r in raices)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    // Si esa raíz ya está indexada, se usa al instante.
                    if (!Indice.TryGetValue(r, out var entradas))
                    {
                        entradas = ConstruirIndice(r, _cts.Token, n =>
                            ((IProgress<string>)progreso).Report($"Preparando el índice de {r}… {n:N0} archivos"));
                        Indice[r] = entradas;
                        GuardarIndice();
                    }

                    foreach (var e in entradas)
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var nombre = Path.GetFileName(e.Ruta);
                        var casa = patrones.Length > 0
                            ? Matcher.LikeAny(nombre, patrones)
                            : patron.Length == 0
                                ? true
                                : soloExtension
                                    ? (Extensiones.De(nombre).Equals(patron.TrimStart('.'), StringComparison.OrdinalIgnoreCase)
                                       || nombre.Contains(patron, StringComparison.OrdinalIgnoreCase))
                                    : nombre.Contains(patron, StringComparison.OrdinalIgnoreCase)
                                      || e.Ruta.Contains(patron, StringComparison.OrdinalIgnoreCase);

                        if (!casa) continue;
                        salida.Add(e);
                        if (salida.Count >= 5000) break;
                    }
                    if (salida.Count >= 5000) break;
                }

                return salida;
            }, _cts.Token);

            Rellenar(encontrados);
            _lblEstado.Text = encontrados.Count == 0
                ? "  No se encontró nada con ese nombre."
                : $"  {encontrados.Count:N0} archivo(s). Marca los que quieras y pulsa «Añadir a la copia».";
        }
        catch (OperationCanceledException) { _lblEstado.Text = "  Búsqueda detenida."; }
        catch (Exception ex) { _lblEstado.Text = "  No se pudo buscar: " + ex.Message; }
        finally
        {
            _cts.Dispose();
            _cts = null;
            Bloquear(false);
            GuardarRaices();
        }
    }

    private void Rellenar(List<EntradaIndice> entradas)
    {
        _lista.BeginUpdate();
        try
        {
            _lista.Items.Clear();
            foreach (var e in entradas)
            {
                var fila = new ListViewItem(Path.GetFileName(e.Ruta)) { Tag = e.Ruta };
                fila.SubItems.Add(Path.GetDirectoryName(e.Ruta) ?? "");
                fila.SubItems.Add(Human.Size(e.Tamano));
                fila.SubItems.Add(e.Fecha.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
                fila.ForeColor = _tema.Tinta;
                _lista.Items.Add(fila);
            }
        }
        finally { _lista.EndUpdate(); }
    }

    private void Bloquear(bool trabajando)
    {
        _btnBuscar.Enabled = !trabajando;
        IndexarHabilitado(!trabajando);
        _btnDetener.Enabled = trabajando;
        _btnAnadir.Enabled = !trabajando;
        _raices.Enabled = !trabajando;
        _txtPatron.Enabled = !trabajando;
    }

    private void IndexarHabilitado(bool activo) => _btnIndexar.Enabled = activo;

    // =====================================================================
    //  Llevar lo marcado a la copia
    // =====================================================================

    private void AnadirSeleccionados()
    {
        var rutas = _lista.SelectedItems.Cast<ListViewItem>()
            .Select(f => f.Tag as string)
            .Where(s => !string.IsNullOrEmpty(s))
            .Cast<string>()
            .ToList();

        if (rutas.Count == 0)
        {
            _lblEstado.Text = "  Marca primero (con clic o Ctrl+clic) los archivos que quieres añadir.";
            return;
        }

        Seleccionados.Clear();
        Seleccionados.AddRange(rutas);
        DialogResult = DialogResult.OK;
        Close();
    }
}
