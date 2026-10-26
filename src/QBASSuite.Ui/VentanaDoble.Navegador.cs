using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QBASSuite.Devices;
using QBASSuite.Util;

namespace QBASSuite.Ui;

/// <summary>
/// Navegador de archivos INTERNO del multicopiador. Se abre con doble clic en un
/// disco, torre, memoria o telefono y deja moverse por sus carpetas sin salir del
/// programa. Con Ctrl se marcan varios archivos y se quitan de la copia si no
/// caben en el disco de destino.
/// </summary>
public sealed partial class VentanaDoble
{
    private Panel _vistaNav = null!;
    private ListView _navLista = null!;
    private Label _navTitulo = null!, _navCapacidad = null!, _navRuta = null!, _navPie = null!;
    private DeviceInfo? _navDisco;
    private string _navRutaActual = "";
    private ImageList _navIconos = null!;
    private Panel _navIconoDisco = null!;

    /// <summary>Archivos (o carpetas) que el usuario ha quitado de la copia con
    /// Ctrl+clic desde el navegador interno. No se copian aunque esten dentro de
    /// una carpeta de origen.</summary>
    private readonly HashSet<string> _excluidos = new(StringComparer.OrdinalIgnoreCase);

    private Panel ConstruirNavegador()
    {
        var t = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            BackColor = Color.Transparent, Padding = new Padding(0), Margin = new Padding(0)
        };
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));    // cabecera
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));    // ruta
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));    // archivos
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));    // pie

        // --- cabecera: volver | icono | nombre | capacidad ---
        var cab = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
            BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
        };
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26));
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        cab.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var atras = new BotonRedondo(_tema)
        {
            Text = "←", Dock = DockStyle.Fill, Radio = 3, SobreDiscos = true,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold), Margin = new Padding(0, 8, 6, 8)
        };
        new ToolTip().SetToolTip(atras, "Volver a la lista de discos");
        atras.Click += (_, _) => VolverALosDiscos();
        cab.Controls.Add(atras, 0, 0);

        var iconoDisco = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0, 10, 4, 10) };
        _navIconoDisco = iconoDisco;
        iconoDisco.Paint += (_, e) =>
        {
            var lado = Math.Min(iconoDisco.Width, iconoDisco.Height);
            var caja = new Rectangle((iconoDisco.Width - lado) / 2, (iconoDisco.Height - lado) / 2, lado, lado);
            Iconos.Dibujar(e.Graphics, _navDisco != null ? Iconos.ParaDispositivo(_navDisco) : Icono.DiscoDuro,
                caja, _tema.Hecho, _tema.AcentoSuave);
        };
        cab.Controls.Add(iconoDisco, 1, 0);

        _navTitulo = new Label
        {
            Text = "", Dock = DockStyle.Fill, AutoSize = false, AutoEllipsis = true,
            ForeColor = _tema.Tinta, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(4, 0, 0, 0)
        };
        cab.Controls.Add(_navTitulo, 2, 0);

        _navCapacidad = new Label
        {
            Text = "", Dock = DockStyle.Fill, AutoSize = false, AutoEllipsis = true,
            ForeColor = _tema.Tinta2, Font = new Font("Segoe UI", 8.25f),
            TextAlign = ContentAlignment.MiddleRight, Margin = new Padding(0)
        };
        cab.Controls.Add(_navCapacidad, 3, 0);
        t.Controls.Add(cab, 0, 0);

        // --- ruta + subir ---
        var fila = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
        };
        fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62));
        fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fila.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var subir = new BotonRedondo(_tema) { Text = "Subir", Dock = DockStyle.Fill, Radio = 3, SobreDiscos = true, Font = new Font("Segoe UI", 8.25f), Margin = new Padding(0, 2, 6, 2) };
        new ToolTip().SetToolTip(subir, "Carpeta de arriba");
        subir.Click += (_, _) => SubirCarpeta();
        fila.Controls.Add(subir, 0, 0);

        _navRuta = new Label
        {
            Text = "", Dock = DockStyle.Fill, AutoSize = false, AutoEllipsis = true,
            ForeColor = _tema.Tinta2, Font = new Font("Segoe UI", 8.25f),
            TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0)
        };
        fila.Controls.Add(_navRuta, 1, 0);
        t.Controls.Add(fila, 0, 1);

        // --- lista de archivos ---
        _navIconos = new ImageList { ImageSize = new Size(20, 20), ColorDepth = ColorDepth.Depth32Bit };
        _navIconos.Images.Add("carpeta", Iconos.Bitmap(Icono.Carpeta, 20, Color.FromArgb(245, 190, 80), Color.Empty));
        _navIconos.Images.Add("video", Iconos.Bitmap(Icono.Video, 20, Color.FromArgb(90, 190, 255), Color.Empty));
        _navIconos.Images.Add("audio", Iconos.Bitmap(Icono.Audio, 20, Color.FromArgb(120, 230, 170), Color.Empty));
        _navIconos.Images.Add("imagen", Iconos.Bitmap(Icono.Imagen, 20, Color.FromArgb(200, 160, 255), Color.Empty));
        _navIconos.Images.Add("documento", Iconos.Bitmap(Icono.Documento, 20, Color.FromArgb(210, 210, 210), Color.Empty));
        _navIconos.Images.Add("otro", Iconos.Bitmap(Icono.Desconocido, 20, Color.FromArgb(170, 190, 200), Color.Empty));

        _navLista = new ListaOscura(_tema)
        {
            Dock = DockStyle.Fill, MultiSelect = true,
            BackColor = Mezclar(_tema.PanelDiscos, Color.White, 0.04f),
            ForeColor = _tema.Tinta, SmallImageList = _navIconos,
            Font = new Font("Segoe UI", 9f), Margin = new Padding(0)
        };
        _navLista.Columns.Add("Nombre", 300);
        _navLista.Columns.Add("Tamaño", 90, HorizontalAlignment.Right);
        _navLista.Resize += (_, _) =>
        {
            var ancho = Math.Max(120, _navLista.ClientSize.Width - 104);
            if (_navLista.Columns.Count == 2) _navLista.Columns[0].Width = ancho;
        };
        _navLista.DoubleClick += (_, _) => AbrirElementoNav();
        _navLista.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Back) { SubirCarpeta(); e.Handled = true; }
            else if (e.KeyCode == Keys.Delete) { QuitarSeleccionDeLaCopia(); e.Handled = true; }
            else if (e.KeyCode == Keys.Enter) { AbrirElementoNav(); e.Handled = true; }
        };
        t.Controls.Add(_navLista, 0, 2);

        // --- pie: ayuda + restaurar + quitar de la copia ---
        var pie = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
        };
        pie.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pie.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        pie.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152));
        pie.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _navPie = new Label
        {
            Text = "", Dock = DockStyle.Fill, AutoSize = false, AutoEllipsis = true,
            ForeColor = _tema.Tinta2, Font = new Font("Segoe UI", 8f),
            TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0)
        };
        pie.Controls.Add(_navPie, 0, 0);

        var restaurar = new BotonRedondo(_tema)
        {
            Text = "Restaurar", Dock = DockStyle.Fill, Radio = 3, SobreDiscos = true,
            Font = new Font("Segoe UI", 8.25f), Margin = new Padding(4, 3, 4, 3)
        };
        new ToolTip().SetToolTip(restaurar, "Vuelve a meter en la copia todo lo que habías quitado");
        restaurar.Click += (_, _) => RestaurarQuitados();
        pie.Controls.Add(restaurar, 1, 0);

        var quitar = new BotonRedondo(_tema)
        {
            Text = "Quitar de la copia", Dock = DockStyle.Fill, Radio = 3, SobreDiscos = true,
            Font = new Font("Segoe UI", 8.25f), Margin = new Padding(0, 3, 0, 3)
        };
        new ToolTip().SetToolTip(quitar, "Quita los archivos marcados (Ctrl para varios) de la cola de copia");
        quitar.Click += (_, _) => QuitarSeleccionDeLaCopia();
        pie.Controls.Add(quitar, 2, 0);
        t.Controls.Add(pie, 0, 3);

        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        p.Controls.Add(t);
        return p;
    }

    /// <summary>Abre el navegador interno en la raiz del disco elegido.</summary>
    private async void AbrirNavegador(DeviceInfo u)
    {
        try
        {
            if (!Directory.Exists(u.Root)) { AbrirDisco(u); return; }   // MTP: se abre en Windows

            _navDisco = u;
            _navIconoDisco.Invalidate();
            _navTitulo.Text = (string.IsNullOrWhiteSpace(u.Label) ? "Disco local" : u.Label)
                + $"   {(string.IsNullOrWhiteSpace(u.FileSystem) ? "" : u.FileSystem + "   ")}"
                + $"({(u.DriveLetter ?? u.KindLabel).TrimEnd('\\')})";
            _navCapacidad.Text = $"{Human.Size(u.UsedSpace)} de {Human.Size(u.Capacity)}";
            _panelDiscos.Visible = false;
            _vistaNav.Visible = true;
            _vistaNav.BringToFront();
            await NavegarA(u.Root);
        }
        catch (Exception ex)
        {
            Mensaje("No se pudo abrir el disco: " + ex.Message);
        }
    }

    private void VolverALosDiscos()
    {
        _navDisco = null;
        _navRutaActual = "";
        _navLista.Items.Clear();
        _navIconoDisco.Invalidate();
        _vistaNav.Visible = false;
        _panelDiscos.Visible = true;
        _panelDiscos.BringToFront();
        PintarDiscos();
    }

    private void SubirCarpeta()
    {
        if (_navDisco == null) return;
        var actual = _navRutaActual;
        var padre = Directory.GetParent(actual);
        if (padre == null || string.IsNullOrEmpty(padre.FullName)
            || padre.FullName.Length < _navDisco.Root.Length)
        {
            VolverALosDiscos();
            return;
        }
        _ = NavegarA(padre.FullName);
    }

    /// <summary>Rellena la lista con las carpetas y archivos de una ruta, sin bloquear.</summary>
    private async Task NavegarA(string ruta)
    {
        if (_navDisco == null) return;
        _navRutaActual = ruta;
        _navRuta.Text = ruta;
        _navPie.Text = "Leyendo…";
        _navLista.Items.Clear();

        var elementos = await Task.Run(() =>
        {
            var lista = new List<(string Ruta, bool Carpeta, long Tamano)>();
            try
            {
                foreach (var d in Directory.EnumerateDirectories(ruta))
                    lista.Add((d, true, -1));
                foreach (var f in Directory.EnumerateFiles(ruta))
                {
                    long tam = -1;
                    try { tam = new FileInfo(f).Length; } catch { }
                    lista.Add((f, false, tam));
                }
            }
            catch { }
            return lista
                .OrderByDescending(x => x.Carpeta)
                .ThenBy(x => Path.GetFileName(x.Ruta), StringComparer.OrdinalIgnoreCase)
                .ToList();
        });

        if (IsDisposed || _navDisco == null) return;
        _navLista.BeginUpdate();
        foreach (var e in elementos)
        {
            var nombre = Path.GetFileName(e.Ruta);
            if (string.IsNullOrEmpty(nombre)) nombre = e.Ruta;
            var it = new ListViewItem(nombre) { Tag = e.Ruta };
            it.ImageKey = e.Carpeta ? "carpeta" : ClaveIcono(nombre);
            it.SubItems.Add(e.Carpeta ? "" : (e.Tamano >= 0 ? Human.Size(e.Tamano) : ""));
            _navLista.Items.Add(it);
        }
        _navLista.EndUpdate();

        var carpetas = elementos.Count(x => x.Carpeta);
        var archivos = elementos.Count - carpetas;
        var total = elementos.Where(x => !x.Carpeta && x.Tamano > 0).Sum(x => x.Tamano);
        _navPie.Text = $"{carpetas} carpetas · {archivos} archivos · {Human.Size(total)}";

        MarcarExcluidos();
    }

    private static string ClaveIcono(string nombre)
    {
        var ext = Path.GetExtension(nombre).ToLowerInvariant();
        return ext switch
        {
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".webm" or ".mpg" or ".mpeg"
                or ".ts" or ".m2ts" or ".flv" or ".m4v" or ".vob" or ".3gp" => "video",
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".m4a" or ".wma" or ".opus" => "audio",
            ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp" or ".tif" or ".tiff" => "imagen",
            ".txt" or ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx"
                or ".srt" or ".ass" or ".sub" => "documento",
            _ => "otro"
        };
    }

    private void AbrirElementoNav()
    {
        if (_navLista.SelectedItems.Count == 0) return;
        var ruta = _navLista.SelectedItems[0].Tag as string;
        if (string.IsNullOrEmpty(ruta)) return;

        if (Directory.Exists(ruta)) { _ = NavegarA(ruta); return; }
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ruta) { UseShellExecute = true });
        }
        catch (Exception ex) { Mensaje("No se pudo abrir: " + ex.Message); }
    }

    /// <summary>Quita de la cola de copia los archivos marcados con Ctrl+clic.</summary>
    private void QuitarSeleccionDeLaCopia()
    {
        if (_navLista.SelectedItems.Count == 0)
        {
            _navPie.Text = "Marca uno o varios archivos (Ctrl+clic) y vuelve a pulsar.";
            return;
        }

        var quitados = 0;   // origenes que se van de la lista
        var fuera = 0;      // archivos dentro de una carpeta que no se copiaran

        foreach (ListViewItem it in _navLista.SelectedItems)
        {
            if (it.Tag is not string ruta) continue;

            // 1) si lo marcado es justo un origen, sale de la lista
            var antes = _origenes.Count;
            foreach (var o in _origenes.Where(o => string.Equals(o, ruta, StringComparison.OrdinalIgnoreCase)).ToList())
                _prefijosOrigen.Remove(o);
            _origenes.RemoveAll(o => string.Equals(o, ruta, StringComparison.OrdinalIgnoreCase));
            if (_origenes.Count != antes) { quitados += antes - _origenes.Count; continue; }

            // 2) si esta dentro de una carpeta de origen, se excluye del copiado
            if (EstaDentroDeUnOrigen(ruta))
            {
                if (_excluidos.Add(ruta)) fuera++;
                continue;
            }

            // 3) una carpeta que contiene origenes: se van todos los de dentro
            antes = _origenes.Count;
            _origenes.RemoveAll(o => o.StartsWith(ruta.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase));
            quitados += antes - _origenes.Count;
        }

        PintarOrigenes();
        MarcarExcluidos();
        GuardarSesion(true);

        var partes = new List<string>();
        if (quitados > 0) partes.Add($"{quitados} origen/es fuera de la copia");
        if (fuera > 0) partes.Add($"{fuera} archivo/s quitado/s");
        _navPie.Text = partes.Count > 0
            ? "Hecho: " + string.Join(" · ", partes) + $". Quedan {_origenes.Count} origen/es."
            : "Eso que marcaste no estaba en la copia (nada que quitar).";
    }

    /// <summary>Devuelve a la copia todo lo que se habia quitado con Ctrl+clic.</summary>
    private void RestaurarQuitados()
    {
        var cuantos = _excluidos.Count;
        _excluidos.Clear();
        MarcarExcluidos();
        _navPie.Text = cuantos > 0
            ? $"Vuelven {cuantos} archivo/s a la copia."
            : "No había nada quitado.";
    }

    /// <summary>¿La ruta esta dentro de alguna carpeta que se va a copiar?</summary>
    private bool EstaDentroDeUnOrigen(string ruta)
    {
        foreach (var o in _origenes)
        {
            if (string.Equals(o, ruta, StringComparison.OrdinalIgnoreCase)) return true;
            if (Directory.Exists(o) && ruta.StartsWith(o.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>Pinta en gris y tachados los archivos que ya no entran en la copia.</summary>
    private void MarcarExcluidos()
    {
        if (_navLista == null) return;
        _fuenteNav ??= new Font("Segoe UI", 9f, FontStyle.Regular);
        _fuenteNavTachada ??= new Font("Segoe UI", 9f, FontStyle.Strikeout);
        foreach (ListViewItem it in _navLista.Items)
        {
            var ruta = it.Tag as string;
            var fuera = ruta != null && EstaExcluido(ruta);
            it.ForeColor = fuera ? _tema.Tenue : _tema.Tinta;
            it.Font = fuera ? _fuenteNavTachada : _fuenteNav;
        }
        _navLista.Invalidate();
    }

    private Font? _fuenteNav, _fuenteNavTachada;

    /// <summary>¿Ese archivo se ha quitado de la copia (el o su carpeta)?</summary>
    private bool EstaExcluido(string ruta)
    {
        if (_excluidos.Count == 0) return false;
        if (_excluidos.Contains(ruta)) return true;
        foreach (var x in _excluidos)
            if (ruta.StartsWith(x.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>¿Cuanto ocuparia la copia, sin contar los archivos quitados?</summary>
    private long BytesDeLaCopia()
    {
        long total = 0;
        foreach (var o in _origenes.ToList())
        {
            try
            {
                if (File.Exists(o))
                {
                    if (!EstaExcluido(o)) total += new FileInfo(o).Length;
                }
                else if (Directory.Exists(o))
                {
                    foreach (var f in Directory.EnumerateFiles(o, "*", SearchOption.AllDirectories))
                    {
                        if (EstaExcluido(f)) continue;
                        try { total += new FileInfo(f).Length; } catch { }
                    }
                }
            }
            catch { }
        }
        return total;
    }
}
