using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QBASSuite.Devices;
using QBASSuite.Media;

namespace QBASSuite.Ui;

/// <summary>
/// Arrastrar y soltar archivos o carpetas desde el Explorador (o desde
/// cualquier programa) directamente sobre la ventana:
///  · mitad izquierda  -> se anaden a la cola de CONVERSION
///  · mitad derecha    -> se anaden como ORIGEN de la copia
///  · sobre una fila de disco -> ademas ese disco queda marcado como DESTINO
/// </summary>
public sealed partial class VentanaDoble
{
    /// <summary>Se llama al final de Construir(), cuando ya existen los paneles.</summary>
    private void ActivarArrastrar()
    {
        // La ventana entera acepta archivos: se decide por la mitad donde se suelta.
        EngancharArbol(this);
    }

    /// <summary>Recorre todos los controles y deja cada uno listo para recibir archivos.
    /// Un control que ya lo estaba (las filas de disco, que marcan el destino) no se
    /// vuelve a enganchar.</summary>
    private void EngancharArbol(Control raiz)
    {
        foreach (Control c in raiz.Controls)
        {
            // En las cajas de texto el arrastre se usa para mover texto: no se toca.
            if (c is not TextBoxBase)
                EngancharSoltar(c, SoltarSegunSitio(c));

            EngancharArbol(c);
        }
    }

    /// <summary>Accion de soltar segun donde caiga: izquierda convierte y derecha copia.
    /// Solo si se suelta sobre una fila de disco, ese disco queda ademas como destino.</summary>
    private Action<string[]> SoltarSegunSitio(Control c) => rutas =>
    {
        if (EstaEnLaIzquierda(c)) { SoltarEnConvertidor(rutas); return; }
        SoltarEnDiscos(rutas, UnidadDeControl(c));
    };

    private bool EstaEnLaIzquierda(Control c)
    {
        for (var p = c; p != null; p = p.Parent)
            if (p == _partidor.Panel1) return true;
        return false;
    }

    /// <summary>Disco al que pertenece un control (la fila o cualquiera de sus hijos).</summary>
    private static DeviceInfo? UnidadDeControl(Control c)
    {
        for (var p = c; p != null; p = p.Parent)
            if (p.Tag is DeviceInfo u) return u;
        return null;
    }

    /// <summary>Deja un control listo para recibir archivos arrastrados (una sola vez).</summary>
    private void EngancharSoltar(Control? destino, Action<string[]> alSoltar)
    {
        if (destino == null) return;
        try
        {
            if (destino.AllowDrop) return;   // ya estaba enganchado (filas de disco)
            destino.AllowDrop = true;
            destino.DragEnter += (_, e) => AceptarArrastre(e, "Suelta para añadirlo");
            destino.DragOver += (_, e) => AceptarArrastre(e, "Suelta para añadirlo");
            destino.DragLeave += (_, _) => Pista("");
            destino.DragDrop += (_, e) =>
            {
                Pista("");
                var rutas = RutasDe(e);
                if (rutas is not { Length: > 0 }) return;

                // Soltar puede repintar la lista y destruir este mismo control. Si se
                // hiciera ahora, se quedaria a medias dentro del arrastre: se aplaza
                // al siguiente mensaje de la ventana, cuando el drop ya termino.
                try { destino.BeginInvoke((Action)(() => alSoltar(rutas))); }
                catch { alSoltar(rutas); }
            };
        }
        catch { }
    }

    private void AceptarArrastre(DragEventArgs e, string pista)
    {
        var rutas = RutasDe(e);
        e.Effect = rutas is { Length: > 0 } ? DragDropEffects.Copy : DragDropEffects.None;
        if (rutas is { Length: > 0 })
        {
            _arrastrando = rutas.Length;
            Pista(pista);
        }
    }

    /// <summary>Rutas que trae el arrastre (archivos y/o carpetas).</summary>
    private static string[]? RutasDe(DragEventArgs e)
    {
        try
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                return e.Data.GetData(DataFormats.FileDrop) as string[];
        }
        catch { }
        return null;
    }

    /// <summary>Empieza a arrastrar rutas DESDE el programa (navegador interno, lista
    /// de origenes o cola de conversion). Asi se pueden llevar al convertidor o al
    /// Explorador sin salir de la ventana.</summary>
    private static void ArrastrarRutas(Control desde, IEnumerable<string?> rutas)
    {
        var lista = rutas.Where(r => !string.IsNullOrWhiteSpace(r))
                         .Select(r => r!)
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .ToArray();
        if (lista.Length == 0) return;
        var datos = new DataObject();
        datos.SetData(DataFormats.FileDrop, lista);
        desde.DoDragDrop(datos, DragDropEffects.Copy);
    }

    /// <summary>Permite arrastrar un control que representa una ruta (por ejemplo el
    /// chip de un origen). Solo empieza el arrastre cuando el raton se mueve de
    /// verdad, para no estorbar al clic normal ni al boton de quitar.</summary>
    private void PonerArrastre(Control control, string ruta)
    {
        var inicio = Point.Empty;
        var pulsado = false;

        control.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            pulsado = true;
            inicio = e.Location;
        };
        control.MouseUp += (_, _) => pulsado = false;
        control.MouseMove += (_, e) =>
        {
            if (!pulsado || e.Button != MouseButtons.Left) return;
            if (Math.Abs(e.X - inicio.X) < SystemInformation.DragSize.Width &&
                Math.Abs(e.Y - inicio.Y) < SystemInformation.DragSize.Height) return;
            pulsado = false;
            ArrastrarRutas(control, new[] { ruta });
        };
    }

    /// <summary>Mensaje corto en la barra de abajo mientras se arrastra.</summary>
    private void Pista(string texto)
    {
        if (_lblMensaje == null) return;
        _lblMensaje.Text = string.IsNullOrEmpty(texto)
            ? ""
            : $"  {texto}   ·   {_arrastrando} archivo(s)/carpeta(s)";
    }

    private int _arrastrando;

    // =====================================================================
    //  Soltar en la mitad IZQUIERDA (convertir)
    // =====================================================================
    private void SoltarEnConvertidor(string[] rutas)
    {
        var archivos = new List<(string Ruta, string Prefijo)>();
        var huboCarpeta = false;

        foreach (var r in rutas)
        {
            try
            {
                if (Directory.Exists(r))
                {
                    huboCarpeta = true;
                    foreach (var f in Directory.EnumerateFiles(r, "*.*", SearchOption.AllDirectories))
                        if (ConversionEngine.EsSoportado(f))
                            archivos.Add((f, PrefijoDeCarpeta(r, f)));
                }
                else if (File.Exists(r))
                {
                    archivos.Add((r, PrefijoDeArchivo(r)));
                }
            }
            catch { }
        }

        if (archivos.Count == 0)
        {
            Mensaje("Lo que has soltado no se puede convertir (no hay audio, vídeo ni imagen dentro).");
            return;
        }

        // Al arrastrar se conserva la carpeta tal cual venia (SerieX\T1\...).
        if (huboCarpeta) _chkEstructura.Checked = true;

        var antes = _lstConv.Items.Count;
        AnadirRutasConv(archivos);
        var nuevos = _lstConv.Items.Count - antes;
        _lblConvEstado.Text = $"{_lstConv.Items.Count} archivos en cola · {nuevos} añadidos al soltar.";
        Mensaje($"Arrastrados {nuevos} archivo(s) a la cola de conversión"
                + (huboCarpeta ? ", conservando sus carpetas." : "."));
    }

    // =====================================================================
    //  Soltar en la mitad DERECHA (copiar)
    // =====================================================================
    private void SoltarEnDiscos(string[] rutas, DeviceInfo? destino)
    {
        var anadidos = 0;
        foreach (var r in rutas)
        {
            try
            {
                if (!Directory.Exists(r) && !File.Exists(r)) continue;
                if (_origenes.Any(o => string.Equals(o, r, StringComparison.OrdinalIgnoreCase))) continue;
                _origenes.Add(r);

                // La carpeta raiz que se conserva en el destino.
                _prefijosOrigen[r] = Directory.Exists(r)
                    ? NombreDeCarpeta(r)
                    : NombreDeCarpeta(Path.GetDirectoryName(r));
                anadidos++;
            }
            catch { }
        }

        if (anadidos == 0 && destino == null)
        {
            Mensaje("Esos archivos ya estaban en la lista de origen.");
            return;
        }

        if (destino != null)
        {
            _destinos.Add(destino.Root);
            PintarDiscos();
        }

        PintarOrigenes();
        GuardarSesion(true);

        var aviso = anadidos > 0
            ? $"{anadidos} origen/es añadidos a la copia."
            : "Ese disco queda marcado como destino.";
        if (destino != null)
            aviso += $"  Destino: {(string.IsNullOrWhiteSpace(destino.Label) ? destino.Name : destino.Label)}.";
        Mensaje(aviso);
    }
}
