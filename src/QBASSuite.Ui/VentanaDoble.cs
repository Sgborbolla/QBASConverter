using System.Diagnostics;
using System.Drawing.Drawing2D;
using QBASSuite.Devices;
using QBASSuite.Engine;
using QBASSuite.Media;
using QBASSuite.Util;

namespace QBASSuite.Ui;

/// <summary>
/// La ventana unica de QBASFreemanCopy-Converter, 100% nativa de Windows (WinForms).
/// Sin navegador, sin servidor y sin puerto: no se abre nada de red.
///
/// ESTA CALCADA A LA CAPTURA DEL PROGRAMA ORIGINAL, PERO SIN LO QUE COBRABA:
///   - IZQUIERDA: el CONVERTIDOR (perfil, opciones y el boton CONVERTIR).
///   - DERECHA:   la lista de DISCOS con su espacio, su velocidad (MB/s) y su
///                tiempo restante, mas una barra de progreso por disco.
/// En medio hay una barra que puedes arrastrar. No hay pestanas.
///
/// Del programa original se ha quitado TODO lo que cobraba: ni "Unregistered",
/// ni precios, ni licencias. Y la marca pasa a ser QBASFreemanCopy-Converter.
/// </summary>
public sealed partial class VentanaDoble : Form
{
    /// <summary>
    /// WS_EX_COMPOSITED: Windows compone la ventana entera fuera de pantalla y la
    /// pega de una vez. Es lo que quita el parpadeo de verdad, incluso con
    /// controles transparentes y redimensionando.
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return cp;
        }
    }

    // ---- estado ----
    private readonly DeviceManager _dispositivos = new();
    private DeviceWatcher? _vigia;
    private readonly List<DeviceInfo> _unidades = new();
    private readonly List<PortableDevice> _portatiles = new();
    private readonly Dictionary<string, double> _msPorDisco = new(StringComparer.OrdinalIgnoreCase);
    private Tema _tema = Tema.PorDefecto;

    // ---- controles ----
    private SplitContainer _partidor = null!;
    private ComboBox _cmbTema = null!;
    private Label _lblReloj = null!, _lblFecha = null!;
    private Label _lblCuentaDiscos = null!, _lblMensaje = null!;
    private System.Windows.Forms.Timer _reloj = null!;
    private PanelLogo? _logo;

    public VentanaDoble(Icon? icono = null)
    {        Text = "QBASFreemanCopy-Converter  ·  Convertidor + Multicopiador";
        StartPosition = FormStartPosition.CenterScreen;
        // Ventana pequena y recogida, como la captura: no se abre a pantalla completa.
        WindowState = FormWindowState.Normal;
        ClientSize = new Size(960, 620);
        MinimumSize = new Size(800, 500);
        Font = new Font("Segoe UI", _fuentePt);
        if (icono != null) Icon = icono;

        // Opciones de discos (que se ve y que columnas) antes de construir.
        LeerOpcionesDiscos();

        // Discos ocultos y matrices guardadas, antes de pintar nada.
        CargarOcultos();
        CargarMatrices();

        Construir();

        // Doble buffer en toda la ventana: se acabo el parpadeo al repintar.
        DoubleBuffered = true;
        Estilo.DobleBuffer(this);

        AplicarTema();

        Load += (_, _) =>
        {
            // Los discos se leen en segundo plano: la ventana ya esta pintada y
            // no se queda en negro esperando a unidades de red que no responden.
            CargarUnidades();
            IniciarVigia();
            IniciarAutosave();
            EngancharTrabajos();
            CargarPrecioGuardado();
            RefrescarPrecio();
        };
        FormClosing += (_, _) =>
        {
            GuardarSesion(silencioso: true);
            GuardarAjustes();
            try { _vigia?.Dispose(); } catch { }
            try { _trabajos.Dispose(); } catch { }
        };
    }

    // =====================================================================
    //  Montaje: cabecera + (convertidor | discos) + pie
    // =====================================================================

    /// <summary>
    /// Reparte la ventana entre las dos mitades: el CONVERTIDOR mas estrecho (izq.) y
    /// los DISCOS mas anchos (der.), como la captura. Se llama solo al mostrarse.
    /// </summary>
    internal void FijarReparto()
    {
        if (_partidor == null || _partidor.IsDisposed) return;
        var ancho = _partidor.ClientSize.Width;
        if (ancho <= 0) return;
        try
        {
            // Primero liberar los minimos (por si venian altos), luego repartir y
            // por ultimo volver a apretarlos un poco. Asi nunca chocan entre si.
            _partidor.Panel1MinSize = 0;
            _partidor.Panel2MinSize = 0;

            // 44% para el convertidor (necesita el ancho para las casillas), el resto discos.
            var min = Math.Min(200, Math.Max(70, ancho / 5));
            var objetivo = (int)(ancho * 0.44);
            if (objetivo < min) objetivo = min;
            if (objetivo > ancho - min) objetivo = ancho - min;
            if (objetivo > 0 && objetivo < ancho) _partidor.SplitterDistance = objetivo;

            _partidor.Panel1MinSize = min;
            _partidor.Panel2MinSize = min;
        }
        catch { }
    }

    private void Construir()
    {
        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            BackColor = _tema.Fondo, Margin = new Padding(0), Padding = new Padding(0)
        };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        // SuspendLayout en toda la tabla: evita reacomodos y repintados en cadena.
        raiz.SuspendLayout();
        raiz.Controls.Add(BarraSuperior(), 0, 0);

        _partidor = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 7,
            BackColor = _tema.Linea
        };
        _partidor.Panel1.BackColor = _tema.Fondo;
        _partidor.Panel2.BackColor = _tema.PanelDiscos;
        _partidor.Panel1.Controls.Add(MitadConvertidora());
        _partidor.Panel2.Controls.Add(MitadDiscos());
        raiz.Controls.Add(_partidor, 0, 1);

        // La distancia se fija cuando la ventana ya tiene ancho de verdad. Si se pone
        // aqui, el control aun mide 0 y SplitContainer lanza una excepcion y se cierra.
        // NO se hace en cada Resize: eso hacia parpadear toda la ventana.
        Shown += (_, _) =>
        {
            FijarReparto();
            // Los avisos de "continuar trabajo" se preguntan con la ventana YA
            // visible y como mucho uno detras de otro. Antes se preguntaban durante
            // la carga y el segundo salia detras de la ventana: parecia congelado.
            BeginInvoke(() =>
            {
                ReanudarSiHayConversion();
                ReanudarSiHayTrabajo();
            });
        };

        raiz.Controls.Add(BarraInferior(), 0, 2);
        Controls.Add(raiz);

        _reloj = new System.Windows.Forms.Timer { Interval = 1000 };
        _reloj.Tick += (_, _) => PintarReloj();
        _reloj.Start();
        PintarReloj();

        AplicarAjustesGuardados();

        // Cerrar el SuspendLayout de arriba: sin esto los controles no se colocan
        // y la ventana se queda en negro.
        raiz.ResumeLayout(true);
        raiz.PerformLayout();

        // Arrastrar y soltar archivos/carpetas sobre cualquiera de las dos mitades.
        ActivarArrastrar();
    }

    private Panel BarraSuperior()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel, Padding = new Padding(12, 4, 12, 4) };

        var textos = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel, Padding = new Padding(6, 2, 8, 2) };
        var titulo = new Label
        {
            Text = "QBASFreemanCopy-Converter", Dock = DockStyle.Top, Height = 24,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = _tema.Acento, TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        var subtitulo = new Label
        {
            Text = "Nativo de Windows  ·  sin puertos  ·  sin licencias",
            Dock = DockStyle.Top, Height = 16, Font = new Font("Segoe UI", 8f),
            ForeColor = _tema.Tinta2, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };
        textos.Controls.Add(subtitulo);
        textos.Controls.Add(titulo);
        p.Controls.Add(textos);

        var der = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, Width = 386, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, Padding = new Padding(0, 6, 0, 0), BackColor = _tema.Panel
        };
        var bOpciones = new Button
        {
            Width = 40, Height = 32, FlatStyle = FlatStyle.Flat,
            Image = Iconos.Bitmap(Icono.Ajustes, 20, _tema.Tinta, _tema.AcentoSuave),
            BackColor = _tema.Panel2, ImageAlign = ContentAlignment.MiddleCenter
        };
        bOpciones.Click += (_, _) => AbrirOpciones();
        var tip = new ToolTip();
        tip.SetToolTip(bOpciones, "Opciones");
        der.Controls.Add(bOpciones);
        _cmbTema = new ComboBox
        {
            Width = 168, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9f)
        };
        foreach (var t in Tema.Todos) _cmbTema.Items.Add(t.Nombre);
        var idx = Tema.Todos.ToList().FindIndex(t => string.Equals(t.Nombre, CargarTemaGuardado(), StringComparison.OrdinalIgnoreCase));
        _cmbTema.SelectedIndex = idx < 0 ? 0 : idx;
        _tema = Tema.PorNombre(_cmbTema.SelectedItem?.ToString());
        _cmbTema.SelectedIndexChanged += (_, _) =>
        {
            _tema = Tema.PorNombre(_cmbTema.SelectedItem?.ToString());
            AplicarTema();
            GuardarAjustes();
        };
        der.Controls.Add(_cmbTema);
        der.Controls.Add(new Label { Text = "Tema:", AutoSize = true, ForeColor = _tema.Tinta2, Padding = new Padding(0, 5, 6, 0) });
        p.Controls.Add(der);

        _logo = new PanelLogo(_tema) { Dock = DockStyle.Left, Width = 52 };
        p.Controls.Add(_logo);

        return p;
    }

    private Panel BarraInferior()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel2, Padding = new Padding(12, 0, 12, 0) };
        _lblCuentaDiscos = new Label { Dock = DockStyle.Left, Width = 300, ForeColor = _tema.Tinta2, TextAlign = ContentAlignment.MiddleLeft };
        _lblMensaje = new Label { Dock = DockStyle.Fill, ForeColor = _tema.Tinta2, TextAlign = ContentAlignment.MiddleLeft };
        _lblFecha = new Label { Dock = DockStyle.Right, Width = 180, ForeColor = _tema.Tinta2, TextAlign = ContentAlignment.MiddleRight };
        _lblReloj = new Label { Dock = DockStyle.Right, Width = 110, ForeColor = _tema.Acento, Font = new Font("Consolas", 12f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
        p.Controls.Add(_lblCuentaDiscos);
        p.Controls.Add(_lblMensaje);
        p.Controls.Add(_lblReloj);
        p.Controls.Add(_lblFecha);
        return p;
    }

    private void PintarReloj()
    {
        if (_lblReloj != null) _lblReloj.Text = DateTime.Now.ToString("HH:mm:ss");
        if (_lblFecha != null) _lblFecha.Text = DateTime.Now.ToString("ddd d MMM yyyy");
    }

    /// <summary>Icono de la ROSCA: abre la ventana de Opciones. Al cerrarla, la
    /// ventana principal recoge lo que se haya cambiado (tema, precio, etc.).</summary>
    private void AbrirOpciones()
    {
        using var dlg = new VentanaOpciones(_tema, () =>
        {
            // Guardado automatico: lo unico que hay que hacer aqui es que el
            // multicopiador se entere si cambio el precio o el tema.
            RefrescarPrecio();
        });
        dlg.ShowDialog(this);

        // Reaplica lo guardado: tema por si cambio, y el precio.
        var nombreTema = CargarTemaGuardado();
        var idx = Tema.Todos.ToList().FindIndex(t => string.Equals(t.Nombre, nombreTema, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0 && _cmbTema != null && _cmbTema.SelectedIndex != idx) _cmbTema.SelectedIndex = idx;

        CargarPrecioGuardado();
        LeerOpcionesDiscos();
        AplicarTema();
        _firmaDiscos = "";
        CargarUnidades(true);
    }

    /// <summary>Recoge de los ajustes el interruptor, la tarifa y el modo de cobro.</summary>
    private void CargarPrecioGuardado()
    {
        if (_chkPrecio == null || _txtPrecioPorGb == null) return;
        _chkPrecio.Checked = Opciones.LeerBooleano("precioActivo", false);
        _txtPrecioPorGb.Text = Opciones.LeerTexto("precioPorGb", "0.50");
        _txtPrecioPorGb.Enabled = _chkPrecio.Checked;
        if (_cmbCobro != null)
        {
            _cmbCobro.SelectedIndex = Math.Clamp(Opciones.LeerEntero("cobroModo", 0), 0, _cmbCobro.Items.Count - 1);
            _cmbCobro.Enabled = _chkPrecio.Checked;
            _lblUnidad.Text = _cmbCobro.SelectedIndex switch { 2 => "/min", 3 => "/carp.", _ => "/GB" };
        }
    }

    // =====================================================================
    //  Dispositivos reales
    // =====================================================================

    private void IniciarVigia()
    {
        try
        {
            _vigia = new DeviceWatcher();
            _vigia.Cambio += () => { if (IsHandleCreated) BeginInvoke(() => CargarUnidades()); };
            _vigia.Iniciar();
        }
        catch { }

        // ANTES habia aqui un temporizador de 4 s que volvia a escanear discos y
        // portatiles en el hilo de la ventana: eso dejaba el programa lento y
        // parpadeando. Ahora solo se refresca cuando Windows avisa de un cambio
        // (el vigia ya lleva su propio antirrebote de 700 ms).
    }

    /// <summary>
    /// Reescanea las unidades reales SIN bloquear la ventana: el sondeo de discos y
    /// de unidades de red puede tardar, asi que se hace en segundo plano y solo se
    /// toca la interfaz cuando ya hay resultados.
    /// </summary>
    private void CargarUnidades(bool forzar = false)
    {
        if (Interlocked.Exchange(ref _cargandoDiscos, 1) == 1) return;

        System.Threading.Tasks.Task.Run(() =>
        {
            var unidades = new List<DeviceInfo>();
            var portatiles = new List<PortableDevice>();
            try
            {
                lock (_candadoDiscos)
                {
                    unidades.AddRange(_dispositivos.Scan(includeSystem: true));
                    portatiles.AddRange(PortableDeviceScanner.Scan());
                }
            }
            catch { }

            try
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(() =>
                    {
                        Interlocked.Exchange(ref _cargandoDiscos, 0);
                        AplicarUnidades(unidades, portatiles, forzar);
                    });
                    return;
                }
            }
            catch { }

            Interlocked.Exchange(ref _cargandoDiscos, 0);
        });
    }

    private readonly object _candadoDiscos = new();
    private int _cargandoDiscos;

    /// <summary>Pinta en el hilo de la ventana lo que devolvio el sondeo en segundo plano.</summary>
    private void AplicarUnidades(List<DeviceInfo> unidades, List<PortableDevice> portatiles, bool forzar)
    {
        if (IsDisposed || _lblCuentaDiscos == null) return;

        // Solo se muestran los tipos de disco que el usuario tenga encendidos, y
        // nunca los que haya ocultado a mano (Refrescar no los devuelve).
        var visibles = unidades.Where(u => DiscoVisible(u) && !_ocultos.Contains(u.Root)).ToList();
        if (_ocultos.Count > 0)
            foreach (var oculto in _ocultos.ToList()) _destinos.Remove(oculto);
        unidades = visibles;

        _unidades.Clear();
        _unidades.AddRange(unidades);
        _portatiles.Clear();
        _portatiles.AddRange(portatiles);
        _lblCuentaDiscos.Text = $"{_unidades.Count} unidades · {_portatiles.Count} portátiles";

        // Si la lista es la misma, NO se destruyen y se recrean las filas: eso era
        // lo que hacia parpadear cada pocos segundos sin motivo.
        var firma = string.Join("|", _unidades.Select(u => $"{u.Root}^{u.Capacity}^{u.FreeSpace}^{u.Label}"))
                    + "#" + string.Join("|", _portatiles.Select(p => $"{p.Nombre}^{p.Estado}"));
        if (!forzar && firma == _firmaDiscos && (_panelDiscos?.Controls.Count ?? 0) > 0) return;
        _firmaDiscos = firma;

        PintarDiscos();
    }

    private string _firmaDiscos = "";

    // ---- opciones que afectan a la lista de discos (se leen una vez) ----
    private bool _verInternos = true, _verExternos = true, _verRed = true, _verOpticos = true;
    private bool _colLetra = true, _colSistema = true, _colLibre = true;
    private float _fuentePt = 9f;
    private int _tamIcono = 26;
    private bool _lineas = true;

    /// <summary>Recoge de los ajustes que discos se miran y que columnas se ven.</summary>
    private void LeerOpcionesDiscos()
    {
        _verInternos = Opciones.LeerBooleano("verDiscosInternos", true);
        _verExternos = Opciones.LeerBooleano("verDiscosExternos", true);
        _verRed = Opciones.LeerBooleano("verRed", true);
        _verOpticos = Opciones.LeerBooleano("verOpticos", true);
        _colLetra = Opciones.LeerBooleano("mostrarLetra", true);
        _colSistema = Opciones.LeerBooleano("mostrarSistemaArchivos", true);
        _colLibre = Opciones.LeerBooleano("mostrarLibre", true);
        _fuentePt = Math.Clamp(Opciones.LeerEntero("fuentePrincipal", 9), 7, 20);
        _tamIcono = Math.Clamp(Opciones.LeerEntero("tamIconos", 26), 16, 40);
        _lineas = Opciones.LeerBooleano("mostrarLineas", true);
    }

    /// <summary>Dice si un dispositivo entra en la lista segun las opciones.</summary>
    private bool DiscoVisible(DeviceInfo u) => u.Kind switch
    {
        DeviceKind.SystemDisk or DeviceKind.Internal => _verInternos,
        DeviceKind.NetworkShare => _verRed,
        DeviceKind.Optical => _verOpticos,
        _ => _verExternos
    };

    private DeviceInfo? UnidadDe(string raiz) =>
        _unidades.FirstOrDefault(x => string.Equals(x.Root, raiz, StringComparison.OrdinalIgnoreCase));

    // =====================================================================
    //  Utilidades de interfaz
    // =====================================================================

    private static Label Etiqueta(string t) => new()
    {
        Text = t, AutoSize = true, ForeColor = Color.Empty, Padding = new Padding(0, 6, 4, 0)
    };

    private static string Tiempo(double segundos)
    {
        if (double.IsNaN(segundos) || double.IsInfinity(segundos)) return "—";
        if (segundos < 60) return $"{segundos:0} s";
        if (segundos < 3600) return $"{segundos / 60:0} min";
        return $"{(int)(segundos / 3600)}:{segundos % 3600 / 60:00} h";
    }

    private static Color Mezclar(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));

    /// <summary>Cabecera con la imagen real del programa (la que dio el autor).</summary>
    private sealed class PanelLogo : Panel
    {
        private readonly Tema _tema;

        public PanelLogo(Tema tema)
        {
            _tema = tema;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            var lado = Math.Min(42, Height - 8);
            var caja = new Rectangle((Width - lado) / 2, (Height - lado) / 2, lado, lado);
            Marca.Dibujar(g, Marca.Imagen(Marca.Copiador), caja, Math.Max(6, lado / 4), true, _tema);
        }
    }

    /// <summary>
    /// Fondo con el LOGOTIPO grande y transparente: la forma del tema dibujada
    /// con mucha transparencia, centrada, mas el glifo. No es de nadie: sale del
    /// tema elegido en ese momento.
    /// </summary>
    private sealed class PanelMarcaAgua : Panel
    {
        public Func<FormaTema>? EtiquetaForma;
        public Func<string>? EtiquetaGlifo;
        public Func<Color>? EtiquetaColor;
        /// <summary>Imagen del icono para usar como marca de agua de fondo (mejor aspecto).</summary>
        public Image? IconoFondo;

        /// <summary>Colores del degradado del panel, si el tema los trae.</summary>
        public Color? GradienteArriba;
        public Color? GradienteAbajo;

        // El fondo se dibuja UNA vez en un bitmap y luego solo se pega.
        private Bitmap? _cache;
        private Size _cacheTam;
        private int _cacheIconVer;
        private string _cacheClave = "";

        public PanelMarcaAgua()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        }

        public void InvalidarFondo()
        {
            _cache?.Dispose();
            _cache = null;
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            _cache?.Dispose();
            _cache = null;
        }

        private int VerIcono() => IconoFondo == null ? 0 : IconoFondo.GetHashCode();

        private Bitmap ConstruirFondo()
        {
            var bmp = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            var r = new Rectangle(0, 0, bmp.Width, bmp.Height);

            // Degradado suave del panel: si el tema trae sus dos colores, se usan tal
            // cual (es lo que da el verde-azul de la captura); si no, se deducen.
            var arriba = GradienteArriba ?? Estilo.Mezcla(BackColor, Color.White, 0.055f);
            var abajo = GradienteAbajo ?? Estilo.Mezcla(BackColor, Color.Black, 0.38f);
            using (var degradado = new LinearGradientBrush(
                new Rectangle(0, 0, r.Width, Math.Max(2, r.Height)), arriba, abajo, 90f))
                g.FillRectangle(degradado, r);

            if (Width < 80 || Height < 80) return bmp;

            var color = EtiquetaColor?.Invoke() ?? Color.White;

            if (IconoFondo != null)
            {
                // Usar la imagen del icono como marca de agua, grande, centrada y muy tenue.
                var lado = (int)(Math.Min(Width, Height) * 0.64);
                var caja = new Rectangle((Width - lado) / 2, (Height - lado) / 2, lado, lado);

                // Crear una imagen con alpha reducido para que no compita con el texto
                using var img = new Bitmap(lado, lado);
                using (var gg = Graphics.FromImage(img))
                {
                    gg.SmoothingMode = SmoothingMode.AntiAlias;
                    gg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gg.DrawImage(IconoFondo, 0, 0, lado, lado);
                }

                // Aplicar transparencia muy baja
                var cm = new System.Drawing.Imaging.ColorMatrix
                {
                    Matrix33 = 0.16f // opacidad
                };
                var ia = new System.Drawing.Imaging.ImageAttributes();
                ia.SetColorMatrix(cm, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);

                g.DrawImage(img, caja, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, ia);
            }
            else
            {
                // Fallback: forma vectorial del tema
                var lado = (int)(Math.Min(Width, Height) * 0.72);
                var caja = new Rectangle((Width - lado) / 2, (Height - lado) / 2, lado, lado);
                Formas.Dibujar(g, EtiquetaForma?.Invoke() ?? FormaTema.HojaNinja, caja,
                    Color.FromArgb(20, color), Color.FromArgb(12, color));

                var glifo = EtiquetaGlifo?.Invoke() ?? "";
                if (glifo.Length > 0)
                {
                    using var fuente = new Font("Segoe UI", Math.Max(60f, lado * 0.42f), FontStyle.Bold);
                    using var brocha = new SolidBrush(Color.FromArgb(14, color));
                    var tam = g.MeasureString(glifo, fuente);
                    g.DrawString(glifo, fuente, brocha, (Width - tam.Width) / 2f, (Height - tam.Height) / 2f);
                }
            }

            // Sello discreto con el nombre
            using (var fuente2 = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var brocha2 = new SolidBrush(Color.FromArgb(26, color)))
            {
                const string texto = "QBASFreemanCopy-Converter";
                var tam2 = g.MeasureString(texto, fuente2);
                g.DrawString(texto, fuente2, brocha2, (Width - tam2.Width) / 2f, Height - tam2.Height - 14f);
            }

            return bmp;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0) return;
            var clave = $"{Width}x{Height}|{BackColor.ToArgb()}|{EtiquetaForma?.Invoke()}|{EtiquetaGlifo?.Invoke()}|{EtiquetaColor?.Invoke().ToArgb()}";
            var verIcono = IconoFondo == null ? 0 : IconoFondo.GetHashCode();
            if (_cache == null || _cacheTam != Size || _cacheClave != clave || _cacheIconVer != verIcono)
            {
                _cache?.Dispose();
                _cache = ConstruirFondo();
                _cacheTam = Size;
                _cacheClave = clave;
                _cacheIconVer = verIcono;
            }
            e.Graphics.DrawImageUnscaled(_cache, 0, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _cache?.Dispose();
            base.Dispose(disposing);
        }
    }
}
