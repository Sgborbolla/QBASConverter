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
        // ---- izquierda: cola de conversion ----
        EngancharSoltar(_lstConv, SoltarEnConvertidor);
        if (_lstConv?.Parent != null) EngancharSoltar(_lstConv.Parent, SoltarEnConvertidor);
        foreach (var c in Contenedores(_partidor.Panel1)) EngancharSoltar(c, SoltarEnConvertidor);

        // ---- derecha: origenes de la copia ----
        EngancharSoltar(_flOrigen, r => SoltarEnDiscos(r, null));
        EngancharSoltar(_panelDiscos, r => SoltarEnDiscos(r, null));
        EngancharSoltar(_fondoDiscos, r => SoltarEnDiscos(r, null));
        EngancharSoltar(_vistaNav, r => SoltarEnDiscos(r, null));
        EngancharSoltar(_navLista, r => SoltarEnDiscos(r, null));
    }

    /// <summary>Todos los paneles y etiquetas de un contenedor (para que se pueda
    /// soltar tambien encima de un titulo o de un hueco vacio).</summary>
    private static IEnumerable<Control> Contenedores(Control raiz)
    {
        foreach (Control hijo in raiz.Controls)
        {
            if (hijo is Panel or TableLayoutPanel or FlowLayoutPanel or Label or GroupBox
                or PictureBox or PanelRedondo or PanelMarcaAgua)
            {
                yield return hijo;
                foreach (var nieto in Contenedores(hijo)) yield return nieto;
            }
        }
    }

    /// <summary>Deja un control listo para recibir archivos arrastrados.</summary>
    private void EngancharSoltar(Control? destino, Action<string[]> alSoltar)
    {
        if (destino == null) return;
        try
        {
            destino.AllowDrop = true;
            destino.DragEnter += (_, e) => AceptarArrastre(e, "Suelta para añadirlo");
            destino.DragOver += (_, e) => AceptarArrastre(e, "Suelta para añadirlo");
            destino.DragLeave += (_, _) => Pista("");
            destino.DragDrop += (_, e) =>
            {
                Pista("");
                var rutas = RutasDe(e);
                if (rutas is { Length: > 0 }) alSoltar(rutas);
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
