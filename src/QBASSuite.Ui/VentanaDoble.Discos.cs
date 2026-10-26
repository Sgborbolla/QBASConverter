using System.Drawing.Drawing2D;
using QBASSuite.Devices;
using QBASSuite.Engine;
using QBASSuite.Util;

namespace QBASSuite.Ui;

/// <summary>Mitad DERECHA: los DISCOS. Calcado a la captura del programa
/// original: cada disco es una fila con su nombre, sistema de archivos, letra,
/// "usado de total", la respuesta en Ms, el tiempo restante y un check verde
/// cuando termina. Ademas lleva una BARRA DE PROGRESO por disco (lo que la
/// captura no tenia).
///
/// Aqui tambien vive el MULTICOPIADOR: se eligen los origenes a copiar y los
/// discos de destino, se lanza la copia a TODOS a la vez (sin limite) y cada
/// disco va a su velocidad real maxima.</summary>
public sealed partial class VentanaDoble
{
    // ---- controles de la mitad derecha ----
    private FlowLayoutPanel _panelDiscos = null!;
    private PanelMarcaAgua _fondoDiscos = null!;
    private FlowLayoutPanel _flOrigen = null!;
    private ComboBox _cmbEstructura = null!;
    private ComboBox _cmbAccion = null!;
    private ComboBox _cmbPrioridadAuto = null!;
    private CheckBox _chkVerificar = null!, _chkVeloz = null!;
    private Button _btnCopiar = null!;
    private CheckBox _chkPrecio = null!;
    private TextBox _txtPrecioPorGb = null!;
    private Label _lblPrecioTotal = null!;
    private readonly List<string> _origenes = new();
    /// <summary>Carpeta raiz que se conserva en el destino para cada origen
    /// arrastrado o elegido (nombre de la carpeta, no la ruta del disco).</summary>
    private readonly Dictionary<string, string> _prefijosOrigen = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _destinos = new(StringComparer.OrdinalIgnoreCase);
    private readonly CopyJobsManager _trabajos = new();
    private CancellationTokenSource? _ctsEnumerar;

    private Control MitadDiscos()
    {
        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7,
            BackColor = _tema.PanelDiscos, Padding = new Padding(8)
        };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));   // titulo + iconos
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // lista de discos
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // origenes + añadir
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // velocidad / verificar / accion
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // estructura / prioridad
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // COPIAR + pausa + limpiar
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // precio por GB

        var cab = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = _tema.PanelDiscos,
            ColumnCount = 3, RowCount = 1, Margin = new Padding(0), Padding = new Padding(0)
        };
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26));
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        cab.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // La imagen del multicopiador, pequena, delante del titulo.
        cab.Controls.Add(new PanelFoto(_tema, Marca.Copiador)
        {
            Dock = DockStyle.Fill, Margin = new Padding(0, 1, 0, 1)
        }, 0, 0);

        cab.Controls.Add(new Label
        {
            Text = "DISCOS  ·  copia a todos a la vez",
            Dock = DockStyle.Fill, ForeColor = _tema.Acento, AutoEllipsis = true, AutoSize = false,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 0, 0), Margin = new Padding(0)
        }, 1, 0);

        // --- barra de iconos de arriba: todos los dibujos son nuestros ---
        var accionesCab = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, Width = 168, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, BackColor = _tema.PanelDiscos, Padding = new Padding(0, 1, 0, 0)
        };
        accionesCab.Controls.Add(IconoBarra(Icono.Refrescar, "Refrescar los discos", () => CargarUnidades()));
        accionesCab.Controls.Add(IconoBarra(Icono.Papelera, "Limpiar los trabajos terminados", () => _trabajos.LimpiarTerminados()));
        accionesCab.Controls.Add(IconoBarra(Icono.Detener, "Cancelar todos los discos", () =>
        {
            foreach (var t in _trabajos.Trabajos.ToList()) _trabajos.Cancelar(t);
        }));
        accionesCab.Controls.Add(IconoBarra(Icono.Seguir, "Continuar todos", () =>
        {
            foreach (var t in _trabajos.Trabajos.ToList())
                if (t.Estado == TrabajoEstado.Pausado) _trabajos.AlternarPausa(t);
        }));
        accionesCab.Controls.Add(IconoBarra(Icono.Pausa, "Pausar todos", () =>
        {
            foreach (var t in _trabajos.Trabajos.ToList())
                if (t.Estado == TrabajoEstado.Copiando) _trabajos.AlternarPausa(t);
        }));
        accionesCab.Controls.Add(IconoBarra(Icono.AnadirCarpeta, "Añadir origen", () => AnadirOrigenDialogo()));
        cab.Controls.Add(accionesCab, 2, 0);
        raiz.Controls.Add(cab, 0, 0);

        _panelDiscos = new PanelDesliz(_tema)
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 4, 4, 4)
        };

        // Fondo con el logotipo grande y transparente (marca de agua del tema).
        var fondo = new PanelMarcaAgua
        {
            Dock = DockStyle.Fill,
            BackColor = _tema.PanelDiscos,
            EtiquetaForma = () => _tema.Forma,
            EtiquetaGlifo = () => _tema.Glifo,
            EtiquetaColor = () => _tema.Oscuro ? Color.White : _tema.Acento,
            // La imagen del multicopiador como marca de agua, muy tenue.
            IconoFondo = Marca.Imagen(Marca.Copiador)
        };
        _fondoDiscos = fondo;
        fondo.Controls.Add(_panelDiscos);

        // El navegador interno vive aqui mismo, encima de la lista de discos:
        // doble clic en un disco y se entra en el sin abrir el Explorador.
        _vistaNav = ConstruirNavegador();
        _vistaNav.Visible = false;
        fondo.Controls.Add(_vistaNav);

        raiz.Controls.Add(fondo, 0, 1);

        raiz.Controls.Add(BarraOrigenDestino(), 0, 2);
        raiz.Controls.Add(BarraOpcionesUno(), 0, 3);
        raiz.Controls.Add(BarraOpcionesDos(), 0, 4);
        raiz.Controls.Add(BarraCopiar(), 0, 5);
        raiz.Controls.Add(BarraPrecio(), 0, 6);
        return raiz;
    }

    /// <summary>Lee la prioridad guardada y la convierte al indice del desplegable.</summary>
    private int IndicePrioridadGuardada()
    {
        var texto = Opciones.LeerTexto("prioridadAuto", "El que menos le queda por copiar");
        if (texto.StartsWith("El más rápido", StringComparison.OrdinalIgnoreCase)) return 2;
        if (texto.StartsWith("El más lento", StringComparison.OrdinalIgnoreCase)) return 3;
        if (texto.StartsWith("Por orden", StringComparison.OrdinalIgnoreCase)) return 0;
        if (texto.StartsWith("Solo", StringComparison.OrdinalIgnoreCase)) return 4;
        return 1;
    }

    /// <summary>Guarda la prioridad elegida en el mismo ajuste de Opciones.</summary>
    private void GuardarPrioridad()
    {
        var texto = _cmbPrioridadAuto.SelectedIndex switch
        {
            0 => "Por orden de la lista",
            2 => "El más rápido",
            3 => "El más lento",
            4 => "Solo el que yo marque con ★",
            _ => "El que menos le queda por copiar"
        };
        Opciones.Guardar("prioridadAuto", texto);
    }

    private Control BarraOrigenDestino()
    {
        var p = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Color.Transparent,
            ColumnCount = 2, RowCount = 1, Margin = new Padding(0), Padding = new Padding(0)
        };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        p.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _flOrigen = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 6, 0), Margin = new Padding(0), WrapContents = false
        };
        p.Controls.Add(_flOrigen, 0, 0);

        var btn = new BotonRedondo(_tema) { Text = "Añadir origen…", Dock = DockStyle.Fill, Radio = 3, Margin = new Padding(0, 2, 0, 2) };
        btn.Click += (_, _) => AnadirOrigenDialogo();
        p.Controls.Add(btn, 1, 0);
        return p;
    }

    // ---------- opciones de copia, en dos lineas que siempre caben ----------
    private Control BarraOpcionesUno()
    {
        var t = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Color.Transparent,
            ColumnCount = 4, RowCount = 1, Margin = new Padding(0), Padding = new Padding(0)
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _chkVeloz = new CheckBox
        {
            Text = "Velocidad máxima real", AutoSize = false, Dock = DockStyle.Fill,
            Checked = true, ForeColor = _tema.Tinta, Margin = new Padding(0),
            Font = new Font("Segoe UI", 8.25f)
        };
        _chkVerificar = new CheckBox
        {
            Text = "Verificar SHA-256", AutoSize = false, Dock = DockStyle.Fill,
            ForeColor = _tema.Tinta, Margin = new Padding(0), Font = new Font("Segoe UI", 8.25f)
        };
        _cmbAccion = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 0, 2) };
        _cmbAccion.Items.AddRange(new object[] { "Copiar", "Mover", "Espejo" });
        _cmbAccion.SelectedIndex = 0;

        t.Controls.Add(_chkVeloz, 0, 0);
        t.Controls.Add(_chkVerificar, 1, 0);
        t.Controls.Add(new Label
        {
            Text = "Acción:", Dock = DockStyle.Fill, ForeColor = _tema.Tinta2,
            TextAlign = ContentAlignment.MiddleRight, Margin = new Padding(0, 0, 4, 0),
            Font = new Font("Segoe UI", 8.25f)
        }, 2, 0);
        t.Controls.Add(_cmbAccion, 3, 0);
        return t;
    }

    private Control BarraOpcionesDos()
    {
        var t = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Color.Transparent,
            ColumnCount = 4, RowCount = 1, Margin = new Padding(0), Padding = new Padding(0)
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _cmbEstructura = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 0, 2) };
        _cmbEstructura.Items.AddRange(new object[] { "Plana", "Por extensión", "Por tipo", "Por fecha", "Como en el origen", "Por tamaño", "Solo nombres" });
        _cmbEstructura.SelectedIndex = 4;

        _cmbPrioridadAuto = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 0, 2) };
        _cmbPrioridadAuto.Items.AddRange(new object[] {
            "Por orden de la lista",
            "El que menos le queda",
            "El más rápido",
            "El más lento",
            "Solo el marcado con ★"
        });
        _cmbPrioridadAuto.SelectedIndex = IndicePrioridadGuardada();
        _cmbPrioridadAuto.SelectedIndexChanged += (_, _) =>
        {
            GuardarPrioridad();
            AplicarPrioridadAutomatica();
        };

        t.Controls.Add(new Label
        {
            Text = "Estructura:", Dock = DockStyle.Fill, ForeColor = _tema.Tinta2,
            TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0),
            Font = new Font("Segoe UI", 8.25f)
        }, 0, 0);
        t.Controls.Add(_cmbEstructura, 1, 0);
        t.Controls.Add(new Label
        {
            Text = "Prioridad:", Dock = DockStyle.Fill, ForeColor = _tema.Tinta2,
            TextAlign = ContentAlignment.MiddleRight, Margin = new Padding(0, 0, 4, 0),
            Font = new Font("Segoe UI", 8.25f)
        }, 2, 0);
        t.Controls.Add(_cmbPrioridadAuto, 3, 0);
        return t;
    }

    private Control BarraCopiar()
    {
        var t = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Color.Transparent,
            ColumnCount = 3, RowCount = 1, Margin = new Padding(0), Padding = new Padding(0)
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _btnCopiar = new BotonRedondo(_tema)
        {
            Text = "COPIAR A LOS DISCOS MARCADOS", Dock = DockStyle.Fill, Height = 34,
            Primario = true, Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            Margin = new Padding(0, 2, 6, 2), Radio = 3
        };
        _btnCopiar.Click += async (_, _) => await LanzarCopia();
        t.Controls.Add(_btnCopiar, 0, 0);

        var bPausaTodo = new BotonRedondo(_tema) { Text = "Pausar / seguir", Dock = DockStyle.Fill, Margin = new Padding(0, 2, 6, 2), Radio = 3, Font = new Font("Segoe UI", 8.25f) };
        bPausaTodo.Click += (_, _) => AlternarPausaTodos();
        t.Controls.Add(bPausaTodo, 1, 0);

        var bLimpia = new BotonRedondo(_tema) { Text = "Limpiar hechos", Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2), Radio = 3, Font = new Font("Segoe UI", 8.25f) };
        bLimpia.Click += (_, _) => _trabajos.LimpiarTerminados();
        t.Controls.Add(bLimpia, 2, 0);
        return t;
    }

    /// <summary>Barra del precio por GB: etiqueta, caja, casilla y el total en verde.</summary>
    private Control BarraPrecio()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = _tema.PanelDiscos };

        // El orden de los Dock=Left se pinta al reves de como se anaden: el ultimo
        // queda mas a la izquierda. Se anaden en el orden inverso al que se ve.
        _lblPrecioTotal = new Label
        {
            Dock = DockStyle.Fill, ForeColor = _tema.Hecho,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
        };
        p.Controls.Add(_lblPrecioTotal);

        _chkPrecio = new CheckBox
        {
            Text = "Calcular precio por GB", Dock = DockStyle.Left, Width = 170, AutoSize = false,
            ForeColor = _tema.Tinta, TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat
        };
        _chkPrecio.CheckedChanged += (_, _) =>
        {
            _txtPrecioPorGb.Enabled = _chkPrecio.Checked;
            RefrescarPrecio();
            GuardarAjustes();
        };
        p.Controls.Add(_chkPrecio);

        _txtPrecioPorGb = new TextBox
        {
            Dock = DockStyle.Left, Width = 70, Text = "0.50", TextAlign = HorizontalAlignment.Right,
            Enabled = false, BorderStyle = BorderStyle.FixedSingle
        };
        _txtPrecioPorGb.TextChanged += (_, _) => { RefrescarPrecio(); };
        _txtPrecioPorGb.Leave += (_, _) => GuardarAjustes();
        p.Controls.Add(_txtPrecioPorGb);

        p.Controls.Add(new Label
        {
            Text = "$ /GB:", Dock = DockStyle.Left, Width = 52,
            ForeColor = _tema.Tinta2, TextAlign = ContentAlignment.MiddleLeft
        });
        return p;
    }

    // =====================================================================
    //  Pintar los discos: una tarjeta por disco con barra, datos y acciones
    // =====================================================================

    private void PintarDiscos()
    {
        if (_panelDiscos == null) return;
        _panelDiscos.SuspendLayout();
        _panelDiscos.Controls.Clear();

        foreach (var u in _unidades)
            _panelDiscos.Controls.Add(TarjetaDisco(u));

        foreach (var p in _portatiles)
            _panelDiscos.Controls.Add(TarjetaPortatil(p));

        _panelDiscos.ResumeLayout(true);

        // Un solo manejador para todas las filas (antes habia uno por fila y parpadeaba).
        if (!_escuchaAncho)
        {
            _escuchaAncho = true;
            _panelDiscos.SizeChanged += (_, _) =>
            {
                if (_panelDiscos == null || _panelDiscos.IsDisposed) return;
                var ancho = AnchoFila();
                _panelDiscos.SuspendLayout();
                foreach (Control c in _panelDiscos.Controls) c.Width = ancho;
                _panelDiscos.ResumeLayout(true);
            };
        }
    }

    private bool _escuchaAncho;

    /// <summary>Una fila-tarjeta de disco: datos + barra + botones (prioridad, pausa/sigue, cancelar).</summary>
    private Control TarjetaDisco(DeviceInfo u)
    {
        var fila = new PanelRedondo(_tema)
        {
            Width = AnchoFila(), Height = 56,
            // Sin caja: la fila se apoya directamente en el fondo con el degradado
            // y el icono de marca de agua, como en la captura.
            BackColor = Color.Transparent,
            Margin = new Padding(0), Tag = u,
            Padding = new Padding(0, 3, 8, 3), Radio = 0, ConSombra = false, ConBorde = false
        };
        fila.Paint += (_, e) => PintarFilaDisco(e.Graphics, fila, u);
        fila.Cursor = Cursors.Hand;
        ActualizarSombra(fila, u);

        // --- botones de fila, con nuestros iconos (no se copia ninguno de nadie) ---
        var acciones = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, Width = 118, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 14, 0, 0)
        };

        var bPrio = BotonIcono(Icono.Prioridad, "Prioridad: este disco va primero");
        bPrio.Click += (_, _) => { PrioridadDisco(u); RefrescarBotones(fila, u); };
        var bPausa = BotonIcono(Icono.Pausa, "Pausar o continuar este disco");
        bPausa.Click += (_, _) => { PausaDisco(u); RefrescarBotones(fila, u); };
        var bCancel = BotonIcono(Icono.Detener, "Cancelar la copia de este disco");
        bCancel.Click += (_, _) => { CancelarDisco(u); RefrescarBotones(fila, u); };
        acciones.Controls.Add(bPrio);
        acciones.Controls.Add(bPausa);
        acciones.Controls.Add(bCancel);
        fila.Controls.Add(acciones);
        EngancharSoltar(acciones, r => SoltarEnDiscos(r, u));

        // doble clic entra en el disco (navegador interno); un clic marca/desmarca como destino.
        // Un doble clic dispara un solo Click, asi que se deshace el cambio antes de abrir.
        fila.Click += (_, _) => AlternarDestino(u);
        fila.DoubleClick += (_, _) => { AlternarDestino(u); AbrirNavegador(u); };

        // Arrastrar y soltar justo encima de ESTA fila: se anade el origen y el disco
        // queda marcado como destino. Los hijos no heredan el drop, asi que se engancha
        // tambien el panel de botones de la derecha.
        EngancharSoltar(fila, r => SoltarEnDiscos(r, u));

        var menu = new ContextMenuStrip();
        menu.Items.Add("Usar como destino", null, (_, _) => AnadirDestino(u));
        menu.Items.Add("Quitar como destino", null, (_, _) => { _destinos.Remove(u.Root); PintarDestinos(); });
        menu.Items.Add("Examinar este disco (aquí dentro)", null, (_, _) => AbrirNavegador(u));
        menu.Items.Add("Abrir en el explorador", null, (_, _) => AbrirDisco(u));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Marcar prioridad (va primero)", null, (_, _) => { PrioridadDisco(u); RefrescarBotones(fila, u); });
        menu.Items.Add("Pausar / continuar", null, (_, _) => { PausaDisco(u); RefrescarBotones(fila, u); });
        menu.Items.Add("Cancelar este disco", null, (_, _) => { CancelarDisco(u); RefrescarBotones(fila, u); });
        fila.ContextMenuStrip = menu;
        return fila;
    }

    private Button BotonIcono(Icono icono, string ayuda)
    {
        var b = new BotonRedondo(_tema)
        {
            Width = 28, Height = 28, Radio = 4, SobreDiscos = true,
            Image = Iconos.Bitmap(icono, 16, _tema.Tinta, _tema.AcentoSuave),
            Margin = new Padding(2, 0, 0, 0)
        };
        new ToolTip().SetToolTip(b, ayuda);
        return b;
    }

    /// <summary>Boton pequeno de la barra de arriba, solo con nuestro icono.</summary>
    private Button IconoBarra(Icono icono, string ayuda, Action accion)
    {
        var b = new BotonRedondo(_tema)
        {
            Width = 30, Height = 26, Radio = 4, SobreDiscos = true,
            Image = Iconos.Bitmap(icono, 15, _tema.Tinta, _tema.AcentoSuave)
        };
        b.Click += (_, _) => { try { accion(); } catch { } };
        new ToolTip().SetToolTip(b, ayuda);
        return b;
    }

    private void RefrescarBotones(Panel fila, DeviceInfo u)
    {
        var t = _trabajos.Trabajos.FirstOrDefault(x =>
            string.Equals(x.Destino.Raiz, u.Root, StringComparison.OrdinalIgnoreCase));
        var pausado = t?.Estado == TrabajoEstado.Pausado;
        if (fila.Controls.Count > 0 && fila.Controls[0] is FlowLayoutPanel fl && fl.Controls.Count == 3)
        {
            ((Button)fl.Controls[1]).Image = Iconos.Bitmap(pausado ? Icono.Seguir : Icono.Pausa, 18, _tema.Tinta, _tema.AcentoSuave);
        }
        Invalidate();
    }

    private void AbrirDisco(DeviceInfo u)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{u.Root}\"") { UseShellExecute = true }); }
        catch { }
    }

    private Control TarjetaPortatil(PortableDevice p)
    {
        var fila = new PanelRedondo(_tema)
        {
            Width = AnchoFila(), Height = 42,
            BackColor = Color.FromArgb(212, Mezclar(_tema.PanelDiscos, Color.White, 0.10f)),
            Margin = new Padding(0, 0, 0, 6), Padding = new Padding(10, 4, 8, 4),
            Radio = 8, ConBorde = true
        };
        fila.Controls.Add(new Label
        {
            Text = $"{p.Nombre}   ·   MTP · {p.Tipo}   ·   {p.Estado}",
            Dock = DockStyle.Fill, ForeColor = _tema.Tinta2, TextAlign = ContentAlignment.MiddleLeft
        });
        return fila;
    }

    /// <summary>Dibuja la fila del disco con la anatomia de la captura.</summary>
    private void PintarFilaDisco(Graphics g, Panel fila, DeviceInfo u)
    {
        if (fila.Width < 80 || fila.Height < 26) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var trabajo = _trabajos.Trabajos.FirstOrDefault(t =>
            string.Equals(t.Destino.Raiz, u.Root, StringComparison.OrdinalIgnoreCase));
        var elegido = _destinos.Contains(u.Root);

        // --- SIN caja: solo un velo suave cuando esta marcado o trabajando ---
        if (elegido || trabajo != null)
        {
            var velo = trabajo is { Estado: TrabajoEstado.Copiando } ? _tema.Copiando
                : trabajo is { Estado: TrabajoEstado.Pausado } ? _tema.Pausado
                : trabajo is { Estado: TrabajoEstado.Error } ? _tema.Error
                : _tema.Acento;
            using var b = new SolidBrush(Color.FromArgb(elegido ? 26 : 18, velo));
            g.FillRectangle(b, new Rectangle(0, 1, fila.Width - 8, fila.Height - 3));
        }

        // barra de seleccion, vertical, a la izquierda (verde como la captura)
        var colorBarra = trabajo?.Estado switch
        {
            TrabajoEstado.Copiando => _tema.Copiando,
            TrabajoEstado.Pausado => _tema.Pausado,
            TrabajoEstado.Error => _tema.Error,
            TrabajoEstado.Hecho => _tema.Hecho,
            _ => _tema.Hecho
        };
        if (elegido || trabajo != null)
            using (var barra = new SolidBrush(colorBarra))
                g.FillRectangle(barra, 2, 8, 3, fila.Height - 18);

        var colorEstado = trabajo == null ? _tema.Tenue : ColorEstado(trabajo.Estado);

        // --- icono del disco, como en la captura ---
        var lateral = Math.Clamp(_tamIcono, 16, 40);
        var cajaIcono = new Rectangle(14, (fila.Height - lateral) / 2 - 2, lateral, lateral);
        Iconos.Dibujar(g, Iconos.ParaDispositivo(u), cajaIcono,
            elegido ? _tema.Hecho : _tema.Tinta, _tema.AcentoSuave);

        var x = 48;
        var limiteDerecho = fila.Width - 124; // deja sitio a los botones de la fila
        if (limiteDerecho < x + 100) limiteDerecho = fila.Width - 10;

        // --- linea 1: nombre + sistema + letra (segun las opciones) ---
        var titulo = !string.IsNullOrWhiteSpace(u.Label) ? u.Label : "Disco local";
        var linea1 = titulo;
        if (_colSistema && !string.IsNullOrWhiteSpace(u.FileSystem)) linea1 += "  " + u.FileSystem;
        if (_colLetra) linea1 += $"  ({(u.DriveLetter ?? u.KindLabel).TrimEnd('\\')})";
        using (var fuente = new Font("Segoe UI", 9.5f, FontStyle.Bold))
        using (var tinta = new SolidBrush(elegido ? _tema.Acento : _tema.Tinta))
            g.DrawString(linea1, fuente, tinta, x, 3);

        // --- linea 2: capacidad · velocidad · tiempo restante ---
        var uso = $"{Human.Size(u.UsedSpace)} de {Human.Size(u.Capacity)}";
        string extra;
        if (trabajo is { Estado: TrabajoEstado.Copiando })
            extra = $"{trabajo.Velocidad / (1024 * 1024):0.0} MB/s   ·   queda {Tiempo(trabajo.Restante)}";
        else if (trabajo is { Estado: TrabajoEstado.Pausado })
            extra = "pausado   ·   " + trabajo.EstadoTexto;
        else if (trabajo is { Estado: TrabajoEstado.Hecho })
            extra = $"{trabajo.EstadoTexto}   ·   {Human.Size(trabajo.BytesHechos)}";
        else if (_msPorDisco.TryGetValue(u.Root, out var ms) && ms > 0)
            extra = $"{ms:0} Ms";
        else
            extra = u.Bus;

        using (var fuente = new Font("Segoe UI", 8.3f))
        using (var tinta = new SolidBrush(trabajo != null ? colorEstado : _tema.Tinta2))
            g.DrawString(_colLibre ? $"{uso}   ·   {extra}" : extra, fuente, tinta, x, 21);

        // --- precio de ESTE disco, en verde, arriba a la derecha ---
        if (PrecioPorGb > 0 && trabajo != null && trabajo.BytesHechos > 0)
        {
            var importe = ImporteDisco(trabajo.BytesHechos);
            using var fuentePrecio = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            using var tintaPrecio = new SolidBrush(_tema.Hecho);
            var anchoPrecio = g.MeasureString(Dinero(importe), fuentePrecio).Width;
            var px = limiteDerecho - anchoPrecio;
            if (px > x) g.DrawString(Dinero(importe), fuentePrecio, tintaPrecio, px, 3);
        }

        // --- barra fina de progreso abajo ---
        var barraX = x;
        var barraY = fila.Height - 14;
        var barraW = limiteDerecho - barraX - 44;
        if (barraW > 30)
        {
            var pct = trabajo?.Percent ?? (u.Capacity > 0 ? u.UsedPercent : 0);
            using (var fondoBarra = new SolidBrush(Color.FromArgb(120, Color.Black)))
                g.FillRectangle(fondoBarra, barraX, barraY, barraW, 4);

            var anchoRelleno = (int)(barraW * Math.Clamp(pct, 0, 100) / 100.0);
            if (anchoRelleno > 0)
                using (var relleno = new SolidBrush(trabajo != null ? colorEstado : _tema.Acento))
                    g.FillRectangle(relleno, barraX, barraY, anchoRelleno, 4);

            using (var fuente = new Font("Segoe UI", 7.8f, FontStyle.Bold))
            using (var tinta = new SolidBrush(trabajo != null ? colorEstado : _tema.Tinta2))
                g.DrawString($"{pct:0}%", fuente, tinta, barraX + barraW + 6, barraY - 5);
        }

        // --- separador fino, de borde a borde, en vez de caja ---
        using (var pluma = new Pen(Color.FromArgb(56, _tema.Tinta), 1f))
            g.DrawLine(pluma, 2, fila.Height - 2, fila.Width - 10, fila.Height - 2);

        // check si termino bien
        if (trabajo is { Estado: TrabajoEstado.Hecho, Errores: 0 })
            using (var chk = new SolidBrush(_tema.Hecho))
                g.FillEllipse(chk, fila.Width - 130, 6, 10, 10);
    }

    private void ActualizarSombra(Panel fila, DeviceInfo u)
    {
        // Nada de un manejador por fila: eso multiplicaba el trabajo y hacia parpadear.
        // El ancho lo ajusta PintarDiscos de una sola vez.
        fila.Width = AnchoFila();
    }

    /// <summary>Ancho util de una fila, siempre dentro del panel (sin scroll horizontal).</summary>
    private int AnchoFila()
    {
        var ancho = _panelDiscos?.ClientSize.Width ?? 0;
        if (ancho < 80) ancho = 480;
        // Se reserva siempre el hueco de la barra de desplazamiento: si se descuenta
        // solo cuando ya es visible, las ultimas filas quedan debajo de ella y se
        // cortan el porcentaje y los botones.
        var w = ancho - 34;
        return Math.Max(200, w);
    }

    private Color ColorEstado(TrabajoEstado e) => e switch
    {
        TrabajoEstado.Copiando => _tema.Copiando,
        TrabajoEstado.Pausado => _tema.Pausado,
        TrabajoEstado.Hecho => _tema.Hecho,
        TrabajoEstado.Cancelado => _tema.Tenue,
        TrabajoEstado.Error => _tema.Error,
        _ => _tema.EnCola
    };

    // =====================================================================
    //  Destinos y origenes
    // =====================================================================

    private void AlternarDestino(DeviceInfo u)
    {
        if (_destinos.Contains(u.Root)) _destinos.Remove(u.Root);
        else AnadirDestino(u);
        PintarDestinos();
    }

    private void AnadirDestino(DeviceInfo u)
    {
        _destinos.Add(u.Root);
        PintarDestinos();
    }

    private void PintarDestinos()
    {
        // los destinos se ven marcados en las propias filas de disco;
        // el origen se ve como chips en la barra inferior
        PintarOrigenes();
        if (_panelDiscos != null) foreach (Control c in _panelDiscos.Controls) c.Invalidate();
    }

    private void PintarOrigenes()
    {
        if (_flOrigen == null) return;
        _flOrigen.Controls.Clear();
        foreach (var o in _origenes.ToList())
        {
            var chip = new Panel
            {
                AutoSize = true, BackColor = _tema.AcentoSuave, Margin = new Padding(0, 2, 4, 2),
                Padding = new Padding(8, 2, 22, 2), MinimumSize = new Size(60, 26)
            };
            chip.Controls.Add(new Label { Text = Path.GetFileName(o.TrimEnd('\\')), AutoSize = true, ForeColor = _tema.Tinta, Location = new Point(8, 5) });
            var quitar = new Button { Text = "×", Width = 18, Height = 18, FlatStyle = FlatStyle.Flat, ForeColor = _tema.Error, Location = new Point(chip.Width - 20, 4), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var capturado = o;
            quitar.Click += (_, _) => { _origenes.Remove(capturado); _prefijosOrigen.Remove(capturado); PintarOrigenes(); GuardarSesion(true); };
            chip.Controls.Add(quitar);
            _flOrigen.Controls.Add(chip);
        }
        if (_origenes.Count == 0)
            _flOrigen.Controls.Add(new Label { Text = "  Arrastra archivos o carpetas aquí", ForeColor = _tema.Tenue, AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
    }

    private void AnadirOrigenDialogo()
    {
        using var dlg = new FolderBrowserDialog { Description = "Elige la carpeta o disco que quieres copiar", ShowNewFolderButton = false };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (!_origenes.Contains(dlg.SelectedPath))
        {
            _origenes.Add(dlg.SelectedPath);
            _prefijosOrigen[dlg.SelectedPath] = NombreDeCarpeta(dlg.SelectedPath);
        }
        PintarOrigenes();
        GuardarSesion(true);
    }

    private void AnadirArchivoOrigen()
    {
        using var dlg = new OpenFileDialog { Multiselect = true, Title = "Elige archivos para copiar" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        foreach (var f in dlg.FileNames)
        {
            if (_origenes.Contains(f)) continue;
            _origenes.Add(f);
            _prefijosOrigen[f] = NombreDeCarpeta(Path.GetDirectoryName(f));
        }
        PintarOrigenes();
        GuardarSesion(true);
    }

    /// <summary>Nombre de la carpeta raiz que se conserva al copiar.</summary>
    private static string NombreDeCarpeta(string? carpeta)
    {
        if (string.IsNullOrWhiteSpace(carpeta)) return "";
        try
        {
            var nombre = new DirectoryInfo(carpeta).Name;
            return string.IsNullOrWhiteSpace(nombre) ? "" : nombre;
        }
        catch { return ""; }
    }

    private void PrioridadDisco(DeviceInfo u)
    {
        var t = _trabajos.Trabajos.FirstOrDefault(x => string.Equals(x.Destino.Raiz, u.Root, StringComparison.OrdinalIgnoreCase));
        if (t != null) _trabajos.Prioridad(t, !t.Prioridad);
    }

    private void PausaDisco(DeviceInfo u)
    {
        var t = _trabajos.Trabajos.FirstOrDefault(x => string.Equals(x.Destino.Raiz, u.Root, StringComparison.OrdinalIgnoreCase));
        if (t != null) _trabajos.AlternarPausa(t);
    }

    private void CancelarDisco(DeviceInfo u)
    {
        var t = _trabajos.Trabajos.FirstOrDefault(x => string.Equals(x.Destino.Raiz, u.Root, StringComparison.OrdinalIgnoreCase));
        if (t != null) _trabajos.Cancelar(t);
    }

    private void AlternarPausaTodos()
    {
        foreach (var t in _trabajos.Trabajos)
            _trabajos.AlternarPausa(t);
    }

    /// <summary>Aplica la prioridad automática seleccionada al gestor de trabajos.</summary>
    private void AplicarPrioridadAutomatica()
    {
        try
        {
            if (_cmbPrioridadAuto == null) return;
            _trabajos.PrioridadAutomatica = _cmbPrioridadAuto.SelectedIndex switch
            {
                0 => QBASSuite.Engine.PrioridadAuto.PorOrden,
                1 => QBASSuite.Engine.PrioridadAuto.ElQueMenosLeQueda,
                2 => QBASSuite.Engine.PrioridadAuto.ElMasRapido,
                3 => QBASSuite.Engine.PrioridadAuto.ElMasLento,
                4 => QBASSuite.Engine.PrioridadAuto.SoloElMarcado,
                _ => QBASSuite.Engine.PrioridadAuto.PorOrden
            };
            _trabajos.Ordenar();
        }
        catch { }
    }

    // =====================================================================
    //  Lanzar la copia a todos los discos marcados a la vez
    // =====================================================================

    private async Task LanzarCopia()
    {
        if (_origenes.Count == 0) { Mensaje("No has elegido qué copiar (origen)."); return; }
        if (_destinos.Count == 0) { Mensaje("No has marcado ningún disco de destino (clic en la fila del disco)."); return; }

        var opciones = new CopyOptions
        {
            Structure = _cmbEstructura.SelectedIndex switch
            {
                0 => StructureMode.Plana,
                1 => StructureMode.PorExtension,
                2 => StructureMode.PorTipo,
                3 => StructureMode.PorFecha,
                4 => StructureMode.PorCarpetaOrigen,
                5 => StructureMode.PorTamano,
                _ => StructureMode.SoloNombres
            },
            Move = _cmbAccion.SelectedIndex == 1,
            Mirror = _cmbAccion.SelectedIndex == 2,
            Verify = _chkVerificar.Checked,
            Conflict = _cmbAccion.SelectedIndex == 2 || Opciones.LeerBooleano("forzarCopia", false)
                ? Conflict.Sobrescribir : Conflict.SaltarSiIgual
        };
        var motor = new CopyEngine(opciones);

        var ficheros = new List<FileEntry>();
        foreach (var o in _origenes)
        {
            try
            {
                var l = await Task.Run(() => motor.Enumerar(o));

                // Se conserva la carpeta raiz tal como se arrastro/elegio:
                // D:\Series\SerieX\T1\cap1.mkv -> destino\SerieX\T1\cap1.mkv
                if (_prefijosOrigen.TryGetValue(o, out var prefijo) && !string.IsNullOrWhiteSpace(prefijo))
                    l = l.Select(f => f.Bajo(prefijo)).ToList();

                ficheros.AddRange(l);
            }
            catch (Exception ex) { Mensaje($"No se pudo leer {o}: {ex.Message}"); }
        }
        if (ficheros.Count == 0) { Mensaje("No hay archivos que copiar en el origen."); return; }

        // Los archivos quitados con Ctrl+clic en el navegador interno no entran.
        var quitadosFuera = 0;
        if (_excluidos.Count > 0)
        {
            var antes = ficheros.Count;
            ficheros = ficheros.Where(f => !EstaExcluido(f.FullPath)).ToList();
            quitadosFuera = antes - ficheros.Count;
            if (ficheros.Count == 0)
            {
                Mensaje("Has quitado todos los archivos de la copia. Pulsa «Restaurar» en el navegador para devolverlos.");
                return;
            }
        }

        // --- comprobaciones antes de lanzar: nada de fallos silenciosos ---
        var validos = new List<string>();
        var avisos = new List<string>();
        foreach (var d in _destinos)
        {
            var info = UnidadDe(d);
            if (!Directory.Exists(d))
            {
                avisos.Add($"{info?.Display ?? d}: no está disponible");
                continue;
            }
            var libre = info?.FreeSpace ?? 0;
            var necesario = ficheros.Sum(f => f.Length);
            if (libre > 0 && necesario > libre)
            {
                avisos.Add($"{info?.Display ?? d}: faltan {Human.Size(necesario - libre)}");
                continue;
            }
            validos.Add(d);
        }

        if (validos.Count == 0)
        {
            Mensaje("Ningún disco de destino sirve:\n\n" + string.Join("\n", avisos)
                    + "\n\nAbre el disco con doble clic y quita archivos con Ctrl+clic para que quepa.");
            return;
        }
        if (avisos.Count > 0)
            Mensaje("Se omite(n) " + avisos.Count + " disco(s):\n\n" + string.Join("\n", avisos)
                    + "\n\nPuedes abrir el disco con doble clic y quitar archivos con Ctrl+clic.");

        var destinos = validos.Select(d => new Destino
        {
            Raiz = d,
            Etiqueta = UnidadDe(d)?.Display ?? d,
            Info = UnidadDe(d)
        }).ToList();

        _trabajos.VelocidadAdaptativa = _chkVeloz.Checked;
        _trabajos.Verificar = _chkVerificar.Checked;
        AplicarPrioridadAutomatica();
        _trabajos.Enviar(ficheros, destinos);
        Mensaje($"Copia lanzada a {destinos.Count} disco(s), a la vez. Cada uno a su velocidad."
                + (quitadosFuera > 0 ? $"\n({quitadosFuera} archivo/s quedaron fuera por tu selección.)" : ""));
    }

    /// <summary>¿Queda algo copiando, en cola o pausado?</summary>
    private bool HayTrabajoActivo()
    {
        try
        {
            return _trabajos.Trabajos.Any(t => t.Estado is TrabajoEstado.Copiando
                or TrabajoEstado.EnCola or TrabajoEstado.Pausado);
        }
        catch { return false; }
    }

    /// <summary>La ventana se refresca como mucho 5 veces por segundo, y SOLO mientras hay trabajo.</summary>
    private void EngancharTrabajos()
    {
        var reloj = new System.Windows.Forms.Timer { Interval = 200 };
        var sucio = false;

        reloj.Tick += (_, _) =>
        {
            if (!sucio) { reloj.Stop(); return; }
            sucio = false;
            RefrescarPrecio();

            // Sin nada copiando no hay nada que animar: se apaga el reloj y la
            // ventana se queda quieta (antes seguia repintando para nada).
            if (!HayTrabajoActivo()) reloj.Stop();
        };

        _trabajos.Cambio += () =>
        {
            sucio = true;
            if (!reloj.Enabled) reloj.Start();
        };

        _trabajos.Terminado += (nombre, ficheros, bytes, errores) =>
        {
            if (!IsHandleCreated) return;
            BeginInvoke(() =>
            {
                RefrescarPrecio();
                if (Opciones.LeerBooleano("crearRegistro", false))
                    RegistrarCopia(nombre, ficheros, bytes, errores);
                Mensaje(errores == 0
                    ? $"✔ {nombre}: {ficheros} archivos, {Human.Size(bytes)}"
                    : $"⚠ {nombre}: {ficheros} archivos, {errores} error(es)");
            });
        };
    }

    /// <summary>Deja una linea en el registro de copias, si el usuario lo tiene encendido.</summary>
    private static void RegistrarCopia(string destino, long ficheros, long bytes, long errores)
    {
        try
        {
            var ruta = Path.Combine(AppContext.BaseDirectory, "QBASFreemanCopy-Converter.registro.txt");
            var linea = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{destino}\t{ficheros} archivos\t{Human.Size(bytes)}\t{errores} errores";
            File.AppendAllText(ruta, linea + Environment.NewLine);
        }
        catch { }
    }
}

