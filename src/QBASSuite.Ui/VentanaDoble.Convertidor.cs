using System.Diagnostics;
using System.Text.Json;
using QBASSuite.Media;
using QBASSuite.Util;

namespace QBASSuite.Ui;

/// <summary>Mitad IZQUIERDA: el CONVERTIDOR, calcado a la captura del programa
/// original: perfil, opciones, cola, cronometro, boton CONVERTIR e iconos de
/// accion. Todo lo que convierte es ffmpeg real.</summary>
public sealed partial class VentanaDoble
{
    private ComboBox _cmbFormato = null!;
    private ComboBox _cmbModoConv = null!;
    private ComboBox _cmbLimiteConv = null!;
    private ComboBox _cmbSalida = null!;
    private ComboBox _cmbConflicto = null!;
    private ComboBox _cmbPerfil = null!;
    private TextBox _txtSubcarpeta = null!;
    private TextBox _txtCarpetaFija = null!;
    private ComboBox _cmbMotor = null!;
    private CheckBox _chkEstructura = null!;
    private CheckBox _chkBorrarOriginal = null!;
    private CheckBox _chkAbrirAlTerminar = null!;
    private CheckBox _chkNoReconvertir = null!;
    private Label _lblSalidaResumen = null!;
    private Label _lblConvEstado = null!, _lblCronometro = null!;
    private ListView _lstConv = null!;
    private readonly Dictionary<string, string> _prefijosConv = new(StringComparer.OrdinalIgnoreCase);
    private BarraFina _barraConv = null!;
    private Button _btnConvertir = null!;
    private System.Windows.Forms.Timer _crono = null!;
    private readonly Stopwatch _relojConv = new();
    private CancellationTokenSource? _ctsConv;

    private Control MitadConvertidora()
    {
        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            BackColor = _tema.Panel, Padding = new Padding(10)
        };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 188));   // perfil + campos + casillas
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));    // cola
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));   // resumen + boton
        raiz.Controls.Add(PerfilYFormato(), 0, 0);
        raiz.Controls.Add(PanelColaConv(), 0, 1);
        raiz.Controls.Add(PieConvertidor(), 0, 2);
        return raiz;
    }

    /// <summary>Etiqueta + control, en una tabla que reparte el ancho disponible.
    /// Asi el control nunca se sale de su mitad.</summary>
    private TableLayoutPanel Campo(string texto, Control control, int anchoEtiqueta)
    {
        var c = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            BackColor = Color.Transparent, Margin = new Padding(0, 1, 6, 1), Padding = new Padding(0)
        };
        c.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, anchoEtiqueta));
        c.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        c.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        c.Controls.Add(new Label
        {
            Text = texto, Dock = DockStyle.Fill, AutoSize = false, AutoEllipsis = true,
            ForeColor = _tema.Tinta2, TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0), Padding = new Padding(0, 0, 4, 0)
        }, 0, 0);

        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 3, 0, 3);
        c.Controls.Add(control, 1, 0);
        return c;
    }

    /// <summary>Dos campos lado a lado, a partes iguales.</summary>
    private TableLayoutPanel DosColumnas(Control izquierda, Control derecha)
    {
        var f = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
        };
        f.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        f.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        f.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        izquierda.Dock = DockStyle.Fill;
        derecha.Dock = DockStyle.Fill;
        izquierda.Margin = new Padding(0);
        derecha.Margin = new Padding(0);
        f.Controls.Add(izquierda, 0, 0);
        f.Controls.Add(derecha, 1, 0);
        return f;
    }

    // ---------- cabecera del convertidor: perfil, formato y modo ----------
    private Control PerfilYFormato()
    {
        var t = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = _tema.Panel, Padding = new Padding(0, 1, 0, 1)
        };
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));   // PERFIL
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // formato + calidad
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // a la vez + guardar
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // nombre + carpeta
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));   // casillas

        // 1) cabecera "PERFIL ..." con el selector de perfiles a la derecha
        // Tabla de 3 columnas: imagen | titulo | selector. Asi la imagen siempre
        // queda pegada al borde izquierdo, sin depender del orden de acoplado.
        var cab = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = _tema.Panel2,
            ColumnCount = 3, RowCount = 1, Margin = new Padding(0), Padding = new Padding(0)
        };
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26));
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cab.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        cab.Controls.Add(new PanelFoto(_tema, Marca.Convertidor)
        {
            Dock = DockStyle.Fill, Margin = new Padding(0, 1, 0, 1)
        }, 0, 0);

        cab.Controls.Add(new Label
        {
            Text = "PERFIL", Dock = DockStyle.Fill, AutoSize = false,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = _tema.Tinta2,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0), Margin = new Padding(0)
        }, 1, 0);

        _cmbPerfil = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 0, 2) };
        CargarPerfiles();
        cab.Controls.Add(_cmbPerfil, 2, 0);
        t.Controls.Add(cab, 0, 0);

        // 2) formato + calidad
        _cmbFormato = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbFormato.Items.AddRange(ConversionEngine.Formatos.Cast<object>().ToArray());
        _cmbFormato.SelectedItem = "mp4";

        _cmbModoConv = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbModoConv.Items.AddRange(new object[] { "Rápido (copia directa)", "Equilibrado", "Alta calidad" });
        _cmbModoConv.SelectedIndex = 1;

        t.Controls.Add(DosColumnas(
            Campo("Formato:", _cmbFormato, 66),
            Campo("Calidad:", _cmbModoConv, 60)), 0, 1);

        // 3) a la vez + guardar (tantos a la vez como nucleos tiene el equipo)
        _cmbLimiteConv = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbLimiteConv.Items.Add("Automático");
        var nucleos = Math.Max(1, Environment.ProcessorCount);
        for (int i = 1; i <= nucleos; i++) _cmbLimiteConv.Items.Add(i.ToString());
        _cmbLimiteConv.SelectedIndex = 0;
        new ToolTip().SetToolTip(_cmbLimiteConv,
            $"Cuántos archivos se convierten al mismo tiempo. Este equipo tiene {nucleos} núcleos.");

        _cmbSalida = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbSalida.Items.AddRange(new object[] { "Subcarpeta al lado", "Misma carpeta", "Carpeta fija" });
        _cmbSalida.SelectedIndex = 0;
        _cmbSalida.SelectedIndexChanged += (_, _) => PintarResumenSalida();
        new ToolTip().SetToolTip(_cmbSalida,
            "Subcarpeta al lado: crea una carpeta junto a los originales.\n" +
            "Misma carpeta: deja el resultado junto al original.\n" +
            "Carpeta fija: todo va a la carpeta que elijas con «Carpeta…».");

        t.Controls.Add(DosColumnas(
            Campo("A la vez:", _cmbLimiteConv, 62),
            Campo("Guardar:", _cmbSalida, 60)), 0, 2);

        // 4) nombre de la subcarpeta + boton de carpeta fija
        _txtSubcarpeta = new TextBox { Text = "Convertidos" };
        _txtSubcarpeta.TextChanged += (_, _) => PintarResumenSalida();

        var btnCarpeta = new BotonRedondo(_tema) { Text = "Carpeta…", Radio = 3 };
        btnCarpeta.Click += (_, _) => ElegirCarpetaFija();

        _txtCarpetaFija = new TextBox { ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Visible = false };

        t.Controls.Add(DosColumnas(
            Campo("Nombre:", _txtSubcarpeta, 62),
            Campo("", btnCarpeta, 0)), 0, 3);

        // 5) casillas, como en la captura (dos columnas)
        var casillas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, BackColor = Color.Transparent };
        casillas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        casillas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int i = 0; i < 3; i++) casillas.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

        CheckBox Casilla(string texto, bool marcada = false) => new()
        {
            Text = texto, AutoSize = false, Dock = DockStyle.Fill, Checked = marcada,
            ForeColor = _tema.Tinta, Margin = new Padding(0),
            Font = new Font("Segoe UI", 8.25f)
        };

        _chkEstructura = Casilla("Mantener carpetas");
        _chkNoReconvertir = Casilla("No reconvertir lo ya hecho", true);
        _chkBorrarOriginal = Casilla("Borrar original si salió bien");
        _chkAbrirAlTerminar = Casilla("Abrir carpeta al terminar", true);

        // --- MOTOR: Automatico / GPU / CPU (lo que pidio el usuario) ---
        var filaMotor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
        };
        filaMotor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        filaMotor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        filaMotor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filaMotor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        filaMotor.Controls.Add(new Label
        {
            Text = "Motor:", Dock = DockStyle.Fill, AutoSize = false,
            ForeColor = _tema.Tinta2, TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.25f), Margin = new Padding(0)
        }, 0, 0);

        _cmbMotor = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 6, 2) };
        _cmbMotor.Items.Add("Automático");
        _cmbMotor.Items.Add(ConversionEngine.Disponible && !string.IsNullOrWhiteSpace(ConversionEngine.DetectarGpu())
            ? "GPU · " + ConversionEngine.NombreGpu()
            : "GPU (si hay)");
        _cmbMotor.Items.Add($"CPU · {nucleos} núcleos");
        _cmbMotor.SelectedIndex = 0;
        new ToolTip().SetToolTip(_cmbMotor,
            "Automático usa la GPU si existe y, si no, la CPU. La GPU convierte mucho más rápido; la CPU siempre funciona.");
        filaMotor.Controls.Add(_cmbMotor, 1, 0);

        var lblMotorInfo = new Label
        {
            Dock = DockStyle.Fill, AutoSize = false, AutoEllipsis = true,
            ForeColor = _tema.Tenue, TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 7.8f), Margin = new Padding(0)
        };
        filaMotor.Controls.Add(lblMotorInfo, 2, 0);
        _cmbMotor.SelectedIndexChanged += (_, _) =>
        {
            lblMotorInfo.Text = _cmbMotor.SelectedIndex switch
            {
                1 => ConversionEngine.Disponible ? "acelerado por hardware" : "no hay GPU: se usará la CPU",
                2 => $"{nucleos} a la vez como máximo",
                _ => ConversionEngine.Disponible ? "GPU si puede, si no CPU" : "CPU (no hay GPU)"
            };
            GuardarSesionConversion(silencioso: true);
        };
        lblMotorInfo.Text = ConversionEngine.Disponible ? "GPU si puede, si no CPU" : "CPU (no hay GPU)";

        casillas.Controls.Add(_chkEstructura, 0, 0);
        casillas.Controls.Add(_chkNoReconvertir, 1, 0);
        casillas.Controls.Add(_chkBorrarOriginal, 0, 1);
        casillas.Controls.Add(_chkAbrirAlTerminar, 1, 1);
        casillas.Controls.Add(filaMotor, 0, 2);
        casillas.SetColumnSpan(filaMotor, 2);
        t.Controls.Add(casillas, 0, 4);

        return t;
    }

    /// <summary>¿Convertir con la GPU? Segun el motor elegido (Auto/GPU/CPU).</summary>
    private bool MotorGpu =>
        _cmbMotor?.SelectedIndex switch
        {
            1 => ConversionEngine.Disponible,
            2 => false,
            _ => ConversionEngine.Disponible
        };

    // ---------- cola de conversion ----------
    private Control PanelColaConv()
    {
        var tra = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel2, Padding = new Padding(6) };

        var cab = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = _tema.Panel2 };
        cab.Controls.Add(new Label
        {
            Text = "Archivos a convertir", Dock = DockStyle.Left,
            ForeColor = _tema.Acento, Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, Padding = new Padding(2, 7, 0, 0)
        });
        var bAdd = new BotonRedondo(_tema) { Text = "Añadir archivos…", Dock = DockStyle.Right, Width = 126, Radio = 8 };
        bAdd.Click += (_, _) => AnadirArchivosConv();
        var bCarpeta = new BotonRedondo(_tema) { Text = "Añadir carpeta…", Dock = DockStyle.Right, Width = 120, Radio = 8 };
        bCarpeta.Click += (_, _) => AnadirCarpetaConv();
        cab.Controls.Add(bAdd);
        cab.Controls.Add(bCarpeta);
        tra.Controls.Add(cab);

        _lstConv = new ListaOscura(_tema)
        {
            MultiSelect = true, Dock = DockStyle.Fill,
            BackColor = _tema.Panel2, ForeColor = _tema.Tinta,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        _lstConv.Columns.Add("Archivo", 170);
        _lstConv.Columns.Add("Tamaño", 68);
        _lstConv.Columns.Add("Estado", 100);
        _lstConv.Resize += (_, _) =>
        {
            if (_lstConv.Columns.Count < 3) return;
            var ancho = Math.Max(90, _lstConv.ClientSize.Width - 176);
            _lstConv.Columns[0].Width = ancho;
        };
        _lstConv.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete) QuitarConvSeleccion(); };
        tra.Controls.Add(_lstConv);

        _lblConvEstado = new Label { Dock = DockStyle.Bottom, Height = 20, ForeColor = _tema.Tinta2, Text = "Sin trabajos." };
        tra.Controls.Add(_lblConvEstado);
        return tra;
    }

    // ---------- pie: resumen de salida, cronometro, CONVERTIR y acciones ----------
    private Control PieConvertidor()
    {
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = _tema.Panel };
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        _lblSalidaResumen = new Label { Dock = DockStyle.Fill, ForeColor = _tema.Tinta2, TextAlign = ContentAlignment.MiddleLeft };
        t.Controls.Add(_lblSalidaResumen, 0, 0);
        PintarResumenSalida();

        // cronometro grande + CONVERTIR + iconos de accion
        var medio = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel };
        _lblCronometro = new Label
        {
            Text = "00:00:00", Dock = DockStyle.Left, Width = 118,
            Font = new Font("Consolas", 19f, FontStyle.Bold), ForeColor = _tema.Tinta,
            TextAlign = ContentAlignment.MiddleLeft
        };
        medio.Controls.Add(_lblCronometro);

        var acciones = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, Width = 250, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, BackColor = _tema.Panel, Padding = new Padding(0, 12, 0, 0)
        };
        _btnConvertir = new BotonRedondo(_tema)
        {
            Text = "CONVERTIR", Width = 132, Height = 38, Primario = true, Radio = 3,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        _btnConvertir.Click += async (_, _) => await LanzarConversion();
        acciones.Controls.Add(_btnConvertir);

        var bDetener = new BotonRedondo(_tema) { Width = 28, Height = 38, Radio = 3, Image = Iconos.Bitmap(Icono.Detener, 14, _tema.Tinta, _tema.AcentoSuave) };
        bDetener.Click += (_, _) => { _ctsConv?.Cancel(); };
        var bQuitar = new BotonRedondo(_tema) { Width = 28, Height = 38, Radio = 3, Image = Iconos.Bitmap(Icono.Quitar, 14, _tema.Tinta, _tema.AcentoSuave) };
        bQuitar.Click += (_, _) => QuitarConvSeleccion();
        var bLimpiar = new BotonRedondo(_tema) { Width = 28, Height = 38, Radio = 3, Image = Iconos.Bitmap(Icono.Papelera, 14, _tema.Tinta, _tema.AcentoSuave) };
        bLimpiar.Click += (_, _) => { _lstConv.Items.Clear(); _prefijosConv.Clear(); ActualizarEstadoConv(); };
        acciones.Controls.Add(bDetener);
        acciones.Controls.Add(bQuitar);
        acciones.Controls.Add(bLimpiar);
        medio.Controls.Add(acciones);
        t.Controls.Add(medio, 0, 1);

        _barraConv = new BarraFina(_tema) { Dock = DockStyle.Fill, Height = 8, Margin = new Padding(0, 10, 0, 10) };
        t.Controls.Add(_barraConv, 0, 2);

        _crono = new System.Windows.Forms.Timer { Interval = 500 };
        _crono.Tick += (_, _) => { if (_relojConv.IsRunning) _lblCronometro.Text = _relojConv.Elapsed.ToString(@"hh\:mm\:ss"); };
        _crono.Start();

        // La pregunta de "continuar la cola guardada" se hace al mostrarse la
        // ventana (ver VentanaDoble.Construir), no aqui: si se pregunta mientras se
        // construye, el aviso sale antes que la ventana y parece que se congelo.

        return t;
    }

    // ---------- cola: anadir y quitar ----------
    private void AnadirArchivosConv()
    {
        using var dlg = new OpenFileDialog
        {
            Multiselect = true, Title = "Elige archivos para convertir",
            Filter = "Multimedia|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.m4v;*.mpg;*.mpeg;*.ts;*.m2ts;*.3gp;*.ogv;*.rmvb;*.asf;" +
                     "*.mp3;*.wav;*.flac;*.aac;*.ogg;*.m4a;*.wma;*.opus;" +
                     "*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.tif;*.tiff;*.gif|Todos (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) AnadirRutasConv(dlg.FileNames);
    }

    private void AnadirCarpetaConv()
    {
        using var dlg = new FolderBrowserDialog { Description = "Elige carpeta con archivos para convertir" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var encontrados = new List<(string Ruta, string Prefijo)>();
        try
        {
            foreach (var f in Directory.EnumerateFiles(dlg.SelectedPath, "*.*", SearchOption.AllDirectories))
                if (ConversionEngine.EsSoportado(f))
                    encontrados.Add((f, PrefijoDeCarpeta(dlg.SelectedPath, f)));
        }
        catch { }
        if (encontrados.Count == 0) { Mensaje("En esa carpeta no hay archivos soportados."); return; }

        // Si se elige una carpeta, lo natural es conservar sus subcarpetas.
        _chkEstructura.Checked = true;
        AnadirRutasConv(encontrados);
    }

    private void AnadirRutasConv(IEnumerable<string> rutas)
        => AnadirRutasConv(rutas.Select(r => (Ruta: r, Prefijo: PrefijoDeArchivo(r))));

    /// <summary>Anade archivos a la cola guardando la carpeta raiz que hay que
    /// conservar en la salida (nombre de la carpeta arrastrada o elegida).</summary>
    private void AnadirRutasConv(IEnumerable<(string Ruta, string Prefijo)> rutas)
    {
        foreach (var (r, prefijo) in rutas)
        {
            if (_lstConv.Items.Cast<ListViewItem>().Any(x => string.Equals(x.Tag as string, r, StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrWhiteSpace(prefijo)) _prefijosConv[r] = prefijo;
                continue;
            }
            long tam = 0;
            try { tam = new FileInfo(r).Length; } catch { }
            var it = new ListViewItem(Path.GetFileName(r)) { Tag = r };
            it.SubItems.Add(Human.Size(tam));
            it.SubItems.Add("En cola");
            _lstConv.Items.Add(it);
            _prefijosConv[r] = prefijo;
        }
        ActualizarEstadoConv();
        GuardarSesionConversion(true);
    }

    /// <summary>Carpeta que se conserva al convertir un archivo suelto: la que lo
    /// contiene. Asi no se pierde el nombre al arrastrarlo.</summary>
    private static string PrefijoDeArchivo(string archivo)
    {
        try
        {
            var carpeta = Path.GetDirectoryName(archivo);
            return string.IsNullOrWhiteSpace(carpeta) ? "" : new DirectoryInfo(carpeta).Name;
        }
        catch { return ""; }
    }

    /// <summary>Prefijo de un archivo que esta dentro de una carpeta arrastrada o
    /// elegida: el nombre de esa carpeta MAS sus subcarpetas (SerieX\T1).</summary>
    private static string PrefijoDeCarpeta(string carpeta, string archivo)
    {
        try
        {
            var nombre = new DirectoryInfo(carpeta).Name;
            var rel = Path.GetRelativePath(carpeta, archivo);
            var sub = Path.GetDirectoryName(rel);
            return string.IsNullOrEmpty(sub) ? nombre : Path.Combine(nombre, sub);
        }
        catch { return ""; }
    }

    private void QuitarConvSeleccion()
    {
        foreach (ListViewItem it in _lstConv.SelectedItems.Cast<ListViewItem>().ToList())
        {
            if (it.Tag is string ruta) _prefijosConv.Remove(ruta);
            _lstConv.Items.Remove(it);
        }
        ActualizarEstadoConv();
        GuardarSesionConversion(true);
    }

    private void ActualizarEstadoConv()
    {
        if (_lblConvEstado != null)
            _lblConvEstado.Text = _lstConv.Items.Count == 0
                ? "Sin trabajos. Arrastra archivos aquí (o añádelos) y pulsa CONVERTIR."
                : $"{_lstConv.Items.Count} archivos en cola.";
    }

    private string FormatoActual => _cmbFormato?.SelectedItem?.ToString() ?? "mp4";

    private void PintarResumenSalida()
    {
        if (_lblSalidaResumen == null) return;
        var sub = string.IsNullOrWhiteSpace(_txtSubcarpeta?.Text) ? "Convertidos" : _txtSubcarpeta!.Text.Trim();
        _lblSalidaResumen.Text = _cmbSalida.SelectedIndex switch
        {
            1 => $"Igual que la carpeta de origen   ·   D:\\Series\\Cap 01.mkv  ⇒  D:\\Series\\Cap 01.{FormatoActual}",
            2 => "Carpeta fija elegida por ti   ·   " + (string.IsNullOrWhiteSpace(_txtCarpetaFija?.Text) ? "(sin elegir todavía)" : _txtCarpetaFija!.Text),
            _ => $"Subcarpeta junto al original   ·   D:\\Series\\Cap 01.mkv  ⇒  D:\\Series\\{sub}\\Cap 01.{FormatoActual}"
        };
    }

    private void ElegirCarpetaFija()
    {
        using var dlg = new FolderBrowserDialog { Description = "Carpeta fija para las conversiones" };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _txtCarpetaFija.Text = dlg.SelectedPath;
            if (_cmbSalida.SelectedIndex != 2) _cmbSalida.SelectedIndex = 2;
            PintarResumenSalida();
        }
    }

    // ---------- perfiles ----------
    private static string FicheroPerfiles => Path.Combine(CarpetaPrograma, "QBASFreemanCopy-Converter.perfiles.json");

    private void CargarPerfiles()
    {
        if (_cmbPerfil == null) return;
        _cmbPerfil.Items.Clear();
        _cmbPerfil.Items.Add("(sin perfil)");
        _cmbPerfil.Items.Add("POR DEFECTO");
        try
        {
            foreach (var n in LeerPerfiles().Keys) _cmbPerfil.Items.Add(n);
        }
        catch { }
        _cmbPerfil.SelectedIndex = 1;   // POR DEFECTO
    }

    private void GuardarPerfil()
    {
        var nombre = PedirTexto("Guardar perfil", "Nombre del perfil:", "Mi perfil");
        if (string.IsNullOrWhiteSpace(nombre)) return;
        try
        {
            var perfiles = LeerPerfiles();
            perfiles[nombre.Trim()] = new Dictionary<string, object?>
            {
                ["formato"] = FormatoActual,
                ["modo"] = _cmbModoConv.SelectedIndex,
                ["limite"] = _cmbLimiteConv.SelectedItem?.ToString() ?? "Automático",
                ["salida"] = _cmbSalida.SelectedIndex,
                ["subcarpeta"] = _txtSubcarpeta.Text,
                ["carpetaFija"] = _txtCarpetaFija.Text,
                ["conflicto"] = _cmbConflicto?.SelectedIndex ?? 0,
                ["estructura"] = _chkEstructura.Checked,
                ["borrarOriginal"] = _chkBorrarOriginal.Checked,
                ["abrirAlTerminar"] = _chkAbrirAlTerminar.Checked,
                ["motor"] = _cmbMotor?.SelectedIndex ?? 0
            };
            GuardarPerfiles(perfiles);
            CargarPerfiles();
            _cmbPerfil.SelectedItem = nombre.Trim();
            Mensaje($"Perfil «{nombre}» guardado.");
        }
        catch (Exception ex) { Mensaje("No se pudo guardar el perfil: " + ex.Message); }
    }

    private void AplicarPerfil()
    {
        var nombre = _cmbPerfil?.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(nombre) || nombre == "(sin perfil)" || nombre == "POR DEFECTO") return;
        try
        {
            var perfiles = LeerPerfiles();
            if (!perfiles.TryGetValue(nombre, out var p)) return;
            string S(string k) => p.TryGetValue(k, out var v) && v != null ? v.ToString()! : "";
            if (S("formato") != "") _cmbFormato.SelectedItem = S("formato");
            if (int.TryParse(S("modo"), out var im)) _cmbModoConv.SelectedIndex = Math.Clamp(im, 0, 2);
            if (S("limite") != "") _cmbLimiteConv.SelectedItem = S("limite");
            if (int.TryParse(S("salida"), out var isa)) _cmbSalida.SelectedIndex = Math.Clamp(isa, 0, 2);
            if (S("subcarpeta") != "") _txtSubcarpeta.Text = S("subcarpeta");
            if (S("carpetaFija") != "") _txtCarpetaFija.Text = S("carpetaFija");
            if (int.TryParse(S("conflicto"), out var ic) && _cmbConflicto != null) _cmbConflicto.SelectedIndex = Math.Clamp(ic, 0, 2);
            _chkEstructura.Checked = S("estructura").Equals("True", StringComparison.OrdinalIgnoreCase);
            _chkBorrarOriginal.Checked = S("borrarOriginal").Equals("True", StringComparison.OrdinalIgnoreCase);
            _chkAbrirAlTerminar.Checked = S("abrirAlTerminar").Equals("True", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(S("motor"), out var imo) && _cmbMotor != null)
                _cmbMotor.SelectedIndex = Math.Clamp(imo, 0, _cmbMotor.Items.Count - 1);
            PintarResumenSalida();
            Mensaje($"Perfil «{nombre}» aplicado.");
        }
        catch (Exception ex) { Mensaje("No se pudo aplicar el perfil: " + ex.Message); }
    }

    private static Dictionary<string, Dictionary<string, object?>> LeerPerfiles()
    {
        try
        {
            if (File.Exists(FicheroPerfiles))
                return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object?>>>(File.ReadAllText(FicheroPerfiles)) ?? new();
        }
        catch { }
        return new();
    }

    private static void GuardarPerfiles(Dictionary<string, Dictionary<string, object?>> perfiles)
    {
        File.WriteAllText(FicheroPerfiles, JsonSerializer.Serialize(perfiles, new JsonSerializerOptions { WriteIndented = true }));
    }

    // ---------- lanzar la conversion real ----------
    private async Task LanzarConversion()
    {
        if (!ConversionEngine.Disponible) { Mensaje("No se encontró ffmpeg.exe en tools\\ffmpeg\\ffmpeg.exe"); return; }
        var archivos = _lstConv.Items.Cast<ListViewItem>().Select(x => x.Tag as string).Where(x => x != null).Cast<string>().ToList();
        if (archivos.Count == 0) { Mensaje("Añade archivos o una carpeta a la cola del convertidor."); return; }

        var req = new ConversionRequest
        {
            Archivos = archivos,
            Formato = FormatoActual,
            Modo = _cmbModoConv.SelectedIndex switch { 0 => "rapido", 2 => "alta", _ => "equilibrado" },
            Salida = _cmbSalida.SelectedIndex switch
            {
                1 => SalidaModo.MismaCarpetaDelOriginal,
                2 => SalidaModo.CarpetaFija,
                _ => SalidaModo.SubcarpetaJuntoAlOriginal
            },
            NombreSubcarpeta = string.IsNullOrWhiteSpace(_txtSubcarpeta.Text) ? "Convertidos" : _txtSubcarpeta.Text.Trim(),
            CarpetaSalida = string.IsNullOrWhiteSpace(_txtCarpetaFija.Text) ? null : _txtCarpetaFija.Text,
            MantenerEstructura = _chkEstructura.Checked,
            Conflicto = _chkNoReconvertir.Checked ? SalidaConflicto.Saltar : SalidaConflicto.Sobrescribir,
            BorrarOriginalSiOk = _chkBorrarOriginal.Checked,
            UsarGpu = MotorGpu
        };

        // La carpeta raiz de cada archivo arrastrado/elegido, para reproducirla.
        req.Subcarpetas.Clear();
        foreach (var a in archivos)
            if (_prefijosConv.TryGetValue(a, out var prefijo) && !string.IsNullOrWhiteSpace(prefijo))
                req.Subcarpetas[a] = prefijo;

        var aLaVez = LimiteSeleccionado();
        _ctsConv = new CancellationTokenSource();
        _btnConvertir.Enabled = false;
        _barraConv.Value = 0;
        _relojConv.Restart();
        _lblConvEstado.Text = $"Convirtiendo {archivos.Count} archivos ({aLaVez} a la vez)…";

        var motor = new ConversionEngine();
        try
        {
            var res = await motor.ConvertirAsync(req, _ctsConv.Token, (ruta, h, t) =>
            {
                if (IsHandleCreated) BeginInvoke(() =>
                {
                    _barraConv.Maximum = Math.Max(1, t);
                    _barraConv.Value = Math.Min(h, t);
                    _lblConvEstado.Text = $"Convirtiendo… {h}/{t} · {Path.GetFileName(ruta)}";
                });
            }, aLaVez);

            var salida = res.Error == "Cancelado"
                ? $"Detenido. {res.Hechos} hechos, {res.Saltados} saltados, {res.Fallidos} fallidos."
                : $"Trabajo finalizado ({res.Motor}). Total: {res.Hechos} de {archivos.Count}, {res.Fallidos} fallidos, {res.Saltados} saltados.";
            _lblConvEstado.Text = salida;
            Mensaje(salida);

            if (_chkAbrirAlTerminar.Checked && res.Salidas.Count > 0)
            {
                try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{res.Salidas[0]}\"") { UseShellExecute = true }); }
                catch { }
            }
        }
        catch (Exception ex)
        {
            _lblConvEstado.Text = "Error: " + ex.Message;
        }
        finally
        {
            _relojConv.Stop();
            _btnConvertir.Enabled = true;
            _ctsConv?.Dispose();
            _ctsConv = null;
        }

        // Guardar estado tras conversión
        GuardarSesionConversion(true);
    }

    private int LimiteSeleccionado()
    {
        var nucleos = Math.Max(1, Environment.ProcessorCount);
        var txt = _cmbLimiteConv.SelectedItem?.ToString() ?? "Automático";
        if (int.TryParse(txt, out var n)) return Math.Clamp(n, 1, nucleos);
        return Math.Min(ConversionEngine.ProcesosSugeridos(), nucleos);
    }

    // ---------- ajustes guardados ----------
    private void AplicarAjustesGuardados()
    {
        try
        {
            if (!File.Exists(FicheroAjustes)) return;
            var doc = JsonDocument.Parse(File.ReadAllText(FicheroAjustes));
            var r = doc.RootElement;
            string S(string k, string def) => r.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : def;
            bool B(string k, bool def) => r.TryGetProperty(k, out var v) && (v.ValueKind == JsonValueKind.True || (v.ValueKind == JsonValueKind.String && v.GetString() == "True")) ? true : def;
            int I(string k, int def) => r.TryGetProperty(k, out var v) && v.TryGetInt32(out var i) ? i : def;

            if (_cmbLimiteConv != null)
            {
                _cmbLimiteConv.SelectedItem = S("limiteConv", "Automático");
                if (_cmbLimiteConv.SelectedIndex < 0) _cmbLimiteConv.SelectedIndex = 0;
            }
            if (_cmbMotor != null && r.TryGetProperty("motor", out var mo) && mo.TryGetInt32(out var imo))
                _cmbMotor.SelectedIndex = Math.Clamp(imo, 0, _cmbMotor.Items.Count - 1);
            if (_cmbFormato != null) _cmbFormato.SelectedItem = S("formato", "mp4");
            if (_cmbModoConv != null) _cmbModoConv.SelectedIndex = Math.Clamp(I("modoIdx", 1), 0, 2);
            if (_cmbSalida != null) _cmbSalida.SelectedIndex = Math.Clamp(I("salida", 0), 0, 2);
            if (_txtSubcarpeta != null) _txtSubcarpeta.Text = S("nombreSub", "Convertidos");
            if (_chkEstructura != null) _chkEstructura.Checked = B("mantenerEstructura", false);
            if (_chkBorrarOriginal != null) _chkBorrarOriginal.Checked = B("borrarOriginal", false);
            if (_txtPrecioPorGb != null) _txtPrecioPorGb.Text = S("precioPorGb", "0.50");
            if (_chkPrecio != null)
            {
                _chkPrecio.Checked = B("precioActivo", false);
                if (_txtPrecioPorGb != null) _txtPrecioPorGb.Enabled = _chkPrecio.Checked;
            }
            PintarResumenSalida();
            RefrescarPrecio();
        }
        catch { }
    }

    // ---------- guardar/cargar cola de conversión ----------
    private void GuardarSesionConversion(bool silencioso)
    {
        try
        {
            if (_lstConv == null) return;
            if (_lstConv.Items.Count == 0)
            {
                if (File.Exists(FicheroTrabajoConv)) File.Delete(FicheroTrabajoConv);
                return;
            }

            var archivos = new List<string>();
            foreach (ListViewItem it in _lstConv.Items)
                if (it.Tag is string s) archivos.Add(s);

            var json = JsonSerializer.Serialize(new
            {
                programa = "QBASFreemanCopy-Converter",
                tipo = "conversion",
                fecha = DateTime.Now.ToString("s"),
                archivos,
                formato = FormatoActual,
                modoIdx = _cmbModoConv?.SelectedIndex ?? 1,
                limite = _cmbLimiteConv?.SelectedItem?.ToString() ?? "Automático",
                salida = _cmbSalida?.SelectedIndex ?? 0,
                subcarpeta = _txtSubcarpeta?.Text ?? "Convertidos",
                carpetaFija = _txtCarpetaFija?.Text ?? "",
                mantenerEstructura = _chkEstructura?.Checked ?? false,
                borrarOriginal = _chkBorrarOriginal?.Checked ?? false,
                abrirAlTerminar = _chkAbrirAlTerminar?.Checked ?? false,
                noReconvertir = _chkNoReconvertir?.Checked ?? true,
                motor = _cmbMotor?.SelectedIndex ?? 0,
                subcarpetas = _prefijosConv
            }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FicheroTrabajoConv, json);
        }
        catch { }
    }

    private void ReanudarSiHayConversion()
    {
        try
        {
            if (_lstConv == null) return;
            if (!File.Exists(FicheroTrabajoConv)) return;

            var doc = JsonDocument.Parse(File.ReadAllText(FicheroTrabajoConv));
            var raiz = doc.RootElement;
            if (!raiz.TryGetProperty("tipo", out var tip) || tip.GetString() != "conversion") return;
            if (!raiz.TryGetProperty("archivos", out var arr) || arr.ValueKind != JsonValueKind.Array) return;

            var lista = new List<string>();
            foreach (var x in arr.EnumerateArray())
                if (x.ValueKind == JsonValueKind.String)
                {
                    var p = x.GetString()!;
                    if (File.Exists(p) || Directory.Exists(p)) lista.Add(p);
                }
            if (lista.Count == 0) { try { File.Delete(FicheroTrabajoConv); } catch { } return; }

            var r = MessageBox.Show(this,
                $"Hay una cola de conversión guardada ({lista.Count} archivo(s)).\n\n¿Quieres continuarla?\n\n· Sí: se vuelve a cargar.\n· No: se descarta.",
                "QBASFreemanCopy-Converter · continuar conversión",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (r == DialogResult.Yes)
            {
                if (raiz.TryGetProperty("formato", out var f) && f.ValueKind == JsonValueKind.String && _cmbFormato != null)
                    _cmbFormato.SelectedItem = f.GetString();
                if (raiz.TryGetProperty("modoIdx", out var m) && m.TryGetInt32(out var im) && _cmbModoConv != null)
                    _cmbModoConv.SelectedIndex = Math.Clamp(im, 0, 2);
                if (raiz.TryGetProperty("limite", out var l) && l.ValueKind == JsonValueKind.String && _cmbLimiteConv != null)
                    _cmbLimiteConv.SelectedItem = l.GetString();
                if (raiz.TryGetProperty("salida", out var s) && s.TryGetInt32(out var isal) && _cmbSalida != null)
                    _cmbSalida.SelectedIndex = Math.Clamp(isal, 0, 2);
                if (raiz.TryGetProperty("subcarpeta", out var sub) && sub.ValueKind == JsonValueKind.String && _txtSubcarpeta != null)
                    _txtSubcarpeta.Text = sub.GetString() ?? "Convertidos";
                if (raiz.TryGetProperty("carpetaFija", out var cf) && cf.ValueKind == JsonValueKind.String && _txtCarpetaFija != null)
                    _txtCarpetaFija.Text = cf.GetString() ?? "";
                if (raiz.TryGetProperty("mantenerEstructura", out var me) && _chkEstructura != null) _chkEstructura.Checked = me.ValueKind == JsonValueKind.True;
                if (raiz.TryGetProperty("borrarOriginal", out var bo) && _chkBorrarOriginal != null) _chkBorrarOriginal.Checked = bo.ValueKind == JsonValueKind.True;
                if (raiz.TryGetProperty("abrirAlTerminar", out var aa) && _chkAbrirAlTerminar != null) _chkAbrirAlTerminar.Checked = aa.ValueKind == JsonValueKind.True;
                if (raiz.TryGetProperty("noReconvertir", out var nr) && _chkNoReconvertir != null) _chkNoReconvertir.Checked = nr.ValueKind == JsonValueKind.True;
                if (raiz.TryGetProperty("motor", out var mo) && _cmbMotor != null && mo.TryGetInt32(out var imo))
                    _cmbMotor.SelectedIndex = Math.Clamp(imo, 0, _cmbMotor.Items.Count - 1);

                _lstConv.Items.Clear();
                _prefijosConv.Clear();
                if (raiz.TryGetProperty("subcarpetas", out var subs) && subs.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in subs.EnumerateObject())
                        if (p.Value.ValueKind == JsonValueKind.String)
                            _prefijosConv[p.Name] = p.Value.GetString() ?? "";
                }
                AnadirRutasConv(lista.Select(r => (r, _prefijosConv.TryGetValue(r, out var p)
                    ? p
                    : PrefijoDeArchivo(r))).ToList());
                PintarResumenSalida();
            }
            else
            {
                try { File.Delete(FicheroTrabajoConv); } catch { }
            }
        }
        catch { }
    }
}
