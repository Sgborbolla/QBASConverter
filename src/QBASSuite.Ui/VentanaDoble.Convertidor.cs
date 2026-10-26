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
    private CheckBox _chkGpu = null!;
    private CheckBox _chkEstructura = null!;
    private CheckBox _chkBorrarOriginal = null!;
    private CheckBox _chkAbrirAlTerminar = null!;
    private CheckBox _chkNoReconvertir = null!;
    private Label _lblSalidaResumen = null!;
    private Label _lblConvEstado = null!, _lblCronometro = null!;
    private ListView _lstConv = null!;
    private ProgressBar _barraConv = null!;
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
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));   // perfil + formato
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));    // cola
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));   // opciones + boton
        raiz.Controls.Add(PerfilYFormato(), 0, 0);
        raiz.Controls.Add(PanelColaConv(), 0, 1);
        raiz.Controls.Add(PieConvertidor(), 0, 2);
        return raiz;
    }

    // ---------- cabecera del convertidor: perfil, formato y modo ----------
    private Control PerfilYFormato()
    {
        var t = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = _tema.Panel, Padding = new Padding(0, 2, 0, 2)
        };
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

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
            Text = "PERFIL", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = _tema.Tinta2,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0), Margin = new Padding(0)
        }, 1, 0);

        _cmbPerfil = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 0, 2) };
        CargarPerfiles();
        cab.Controls.Add(_cmbPerfil, 2, 0);
        t.Controls.Add(cab, 0, 0);

        // 2) formato, calidad y conversiones a la vez
        var fila = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = _tema.Panel, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        fila.Controls.Add(Etiqueta("Formato:"));
        _cmbFormato = new ComboBox { Width = 88, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbFormato.Items.AddRange(ConversionEngine.Formatos.Cast<object>().ToArray());
        _cmbFormato.SelectedItem = "mp4";
        fila.Controls.Add(_cmbFormato);

        fila.Controls.Add(Etiqueta("Calidad:"));
        _cmbModoConv = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbModoConv.Items.AddRange(new object[] { "Rápido (copia directa)", "Equilibrado", "Alta calidad" });
        _cmbModoConv.SelectedIndex = 1;
        fila.Controls.Add(_cmbModoConv);

        fila.Controls.Add(Etiqueta("A la vez:"));
        _cmbLimiteConv = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbLimiteConv.Items.Add("Automático");
        for (int i = 1; i <= 16; i++) _cmbLimiteConv.Items.Add(i.ToString());
        _cmbLimiteConv.SelectedIndex = 0;
        fila.Controls.Add(_cmbLimiteConv);
        t.Controls.Add(fila, 0, 1);

        // 3) donde se guarda
        var fila2 = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = _tema.Panel, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        fila2.Controls.Add(Etiqueta("Guardar:"));
        _cmbSalida = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbSalida.Items.AddRange(new object[] { "Subcarpeta junto al original", "Misma carpeta del original", "Carpeta fija que yo elija" });
        _cmbSalida.SelectedIndex = 0;
        _cmbSalida.SelectedIndexChanged += (_, _) => PintarResumenSalida();
        fila2.Controls.Add(_cmbSalida);

        fila2.Controls.Add(Etiqueta("Nombre:"));
        _txtSubcarpeta = new TextBox { Width = 82, Text = "Convertidos" };
        _txtSubcarpeta.TextChanged += (_, _) => PintarResumenSalida();
        fila2.Controls.Add(_txtSubcarpeta);

        var btnCarpeta = new BotonRedondo(_tema) { Text = "Carpeta…", Width = 92, Radio = 8 };
        btnCarpeta.Click += (_, _) => ElegirCarpetaFija();
        fila2.Controls.Add(btnCarpeta);
        _txtCarpetaFija = new TextBox { Width = 120, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
        fila2.Controls.Add(_txtCarpetaFija);
        t.Controls.Add(fila2, 0, 2);

        // 4) casillas, como en la captura (dos columnas)
        var casillas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, BackColor = _tema.Panel };
        casillas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        casillas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _chkEstructura = new CheckBox { Text = "Mantener carpetas", AutoSize = true, ForeColor = _tema.Tinta };
        _chkBorrarOriginal = new CheckBox { Text = "Borrar original si salió bien", AutoSize = true, ForeColor = _tema.Tinta };
        _chkNoReconvertir = new CheckBox { Text = "No reconvertir lo ya hecho", AutoSize = true, Checked = true, ForeColor = _tema.Tinta };
        _chkAbrirAlTerminar = new CheckBox { Text = "Abrir carpeta al terminar", AutoSize = true, Checked = true, ForeColor = _tema.Tinta };
        _chkGpu = new CheckBox
        {
            Text = ConversionEngine.Disponible && !string.IsNullOrWhiteSpace(ConversionEngine.DetectarGpu())
                ? $"Usar GPU ({ConversionEngine.NombreGpu()})"
                : "Usar GPU (si hay)",
            AutoSize = true, Checked = true, ForeColor = _tema.Tinta
        };
        casillas.Controls.Add(_chkEstructura, 0, 0);
        casillas.Controls.Add(_chkNoReconvertir, 1, 0);
        casillas.Controls.Add(_chkBorrarOriginal, 0, 1);
        casillas.Controls.Add(_chkAbrirAlTerminar, 1, 1);
        casillas.Controls.Add(_chkGpu, 0, 2);
        casillas.SetColumnSpan(_chkGpu, 2);
        t.Controls.Add(casillas, 0, 3);

        return t;
    }

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

        _lstConv = new ListView
        {
            View = View.Details, FullRowSelect = true, MultiSelect = true, Dock = DockStyle.Fill,
            BackColor = _tema.Panel2, ForeColor = _tema.Tinta, BorderStyle = BorderStyle.None,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        _lstConv.Columns.Add("Archivo", 170);
        _lstConv.Columns.Add("Tamaño", 68);
        _lstConv.Columns.Add("Estado", 100);
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
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        _lblSalidaResumen = new Label { Dock = DockStyle.Fill, ForeColor = _tema.Tinta2, TextAlign = ContentAlignment.MiddleLeft };
        t.Controls.Add(_lblSalidaResumen, 0, 0);
        PintarResumenSalida();

        // cronometro grande + CONVERTIR + iconos de accion
        var medio = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel };
        _lblCronometro = new Label
        {
            Text = "00:00:00", Dock = DockStyle.Left, Width = 150,
            Font = new Font("Consolas", 22f, FontStyle.Bold), ForeColor = _tema.Tinta,
            TextAlign = ContentAlignment.MiddleLeft
        };
        medio.Controls.Add(_lblCronometro);

        var acciones = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, Width = 248, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, BackColor = _tema.Panel, Padding = new Padding(0, 16, 0, 0)
        };
        _btnConvertir = new BotonRedondo(_tema)
        {
            Text = "CONVERTIR", Width = 148, Height = 40, Primario = true, Radio = 10,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        _btnConvertir.Click += async (_, _) => await LanzarConversion();
        acciones.Controls.Add(_btnConvertir);

        var bDetener = new BotonRedondo(_tema) { Text = "■", Width = 32, Height = 40, Radio = 8, Font = new Font("Segoe UI", 10f) };
        bDetener.Click += (_, _) => { _ctsConv?.Cancel(); };
        var bQuitar = new BotonRedondo(_tema) { Text = "✕", Width = 32, Height = 40, Radio = 8, Font = new Font("Segoe UI", 10f) };
        bQuitar.Click += (_, _) => QuitarConvSeleccion();
        var bLimpiar = new BotonRedondo(_tema) { Text = "🗑", Width = 32, Height = 40, Radio = 8, Font = new Font("Segoe UI", 10f) };
        bLimpiar.Click += (_, _) => { _lstConv.Items.Clear(); ActualizarEstadoConv(); };
        acciones.Controls.Add(bDetener);
        acciones.Controls.Add(bQuitar);
        acciones.Controls.Add(bLimpiar);
        medio.Controls.Add(acciones);
        t.Controls.Add(medio, 0, 1);

        _barraConv = new ProgressBar { Dock = DockStyle.Fill, Height = 16, Style = ProgressBarStyle.Continuous };
        t.Controls.Add(_barraConv, 0, 2);

        _crono = new System.Windows.Forms.Timer { Interval = 500 };
        _crono.Tick += (_, _) => { if (_relojConv.IsRunning) _lblCronometro.Text = _relojConv.Elapsed.ToString(@"hh\:mm\:ss"); };
        _crono.Start();

        // Preguntar si hay cola de conversión guardada
        ReanudarSiHayConversion();

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
        var encontrados = new List<string>();
        try
        {
            foreach (var f in Directory.EnumerateFiles(dlg.SelectedPath, "*.*", SearchOption.AllDirectories))
                if (ConversionEngine.EsSoportado(f)) encontrados.Add(f);
        }
        catch { }
        if (encontrados.Count == 0) { Mensaje("En esa carpeta no hay archivos soportados."); return; }
        AnadirRutasConv(encontrados);
    }

    private void AnadirRutasConv(IEnumerable<string> rutas)
    {
        foreach (var r in rutas)
        {
            if (_lstConv.Items.Cast<ListViewItem>().Any(x => string.Equals(x.Tag as string, r, StringComparison.OrdinalIgnoreCase)))
                continue;
            long tam = 0;
            try { tam = new FileInfo(r).Length; } catch { }
            var it = new ListViewItem(Path.GetFileName(r)) { Tag = r };
            it.SubItems.Add(Human.Size(tam));
            it.SubItems.Add("En cola");
            _lstConv.Items.Add(it);
        }
        ActualizarEstadoConv();
        GuardarSesionConversion(true);
    }

    private void QuitarConvSeleccion()
    {
        foreach (ListViewItem it in _lstConv.SelectedItems.Cast<ListViewItem>().ToList())
            _lstConv.Items.Remove(it);
        ActualizarEstadoConv();
        GuardarSesionConversion(true);
    }

    private void ActualizarEstadoConv()
    {
        if (_lblConvEstado != null)
            _lblConvEstado.Text = _lstConv.Items.Count == 0
                ? "Sin trabajos. Añade archivos y pulsa CONVERTIR."
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
                ["gpu"] = _chkGpu?.Checked ?? true
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
            if (_chkGpu != null) _chkGpu.Checked = S("gpu").Equals("True", StringComparison.OrdinalIgnoreCase);
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
            UsarGpu = _chkGpu?.Checked ?? true
        };

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
        var txt = _cmbLimiteConv.SelectedItem?.ToString() ?? "Automático";
        if (int.TryParse(txt, out var n)) return Math.Clamp(n, 1, 16);
        return ConversionEngine.ProcesosSugeridos();
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

            if (_cmbLimiteConv != null) _cmbLimiteConv.SelectedItem = S("limiteConv", "Automático");
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
                gpu = _chkGpu?.Checked ?? true
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

            var r = MessageBox.Show(
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
                if (raiz.TryGetProperty("gpu", out var gp) && _chkGpu != null) _chkGpu.Checked = gp.ValueKind == JsonValueKind.True;

                _lstConv.Items.Clear();
                AnadirRutasConv(lista);
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
