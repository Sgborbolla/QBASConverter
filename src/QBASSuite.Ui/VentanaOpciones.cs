using System.Text.Json;
using QBASSuite.Util;

namespace QBASSuite.Ui;

/// <summary>
/// La ventana de OPCIONES de QBASFreemanCopy-Converter. Es la pantalla de configuracion que se
/// abre al pulsar el icono de la ROSCA (el engranaje) de la barra superior.
///
/// Diseno: BARRA LATERAL a la izquierda con las secciones, UNA DEBAJO DE OTRA,
/// y a la derecha la pagina de la seccion que pulses. Se navega entre ellas con
/// un clic, sin pestanas arriba.
///
/// IMPORTANTE:
///   - Cada SECCION se puede APAGAR y ENCENDER entera cuando quieras. Apagada,
///     esa parte del programa queda desactivada. Se vuelve a encender y ya esta.
///   - Dentro de cada seccion, cada opcion es un INTERRUPTOR que puedes cambiar
///     las veces que quieras.
///   - NO hay boton "Aceptar": todo se guarda SOLO, al instante, en el fichero
///     de ajustes que va junto al programa. Cierras y sigue puesto.
///   - NO existe la seccion de Licencia: aqui no se cobra, no caduca y no se
///     bloquea nada. Pagos es solo una calculadora informativa local.
/// </summary>
public sealed class VentanaOpciones : Form
{
    private readonly Tema _tema;
    private readonly Action _alCambiar;

    private Panel _laterales = null!;
    private Panel _pagina = null!;
    private Label _titulo = null!;
    private readonly List<(SeccionOpcion sec, BotonSeccion boton)> _botones = new();
    private string _actual = "general";

    public VentanaOpciones(Tema tema, Action alCambiar, string inicial = "general")
    {
        _tema = tema;
        _alCambiar = alCambiar;

        Text = "QBASFreemanCopy-Converter · Opciones";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1000, 680);
        MinimumSize = new Size(860, 560);
        Font = new Font("Segoe UI", 9f);
        BackColor = _tema.Fondo;
        ForeColor = _tema.Tinta;
        DoubleBuffered = true;

        Construir();
        Mostrar(inicial);
    }

    private void Construir()
    {
        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
            BackColor = _tema.Fondo, Margin = new Padding(0), Padding = new Padding(0)
        };
        raiz.SuspendLayout();
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 268));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // --- cabecera: logo, titulo y subtitulo ---
        var cab = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel, Padding = new Padding(18, 0, 14, 0) };
        cab.Paint += (_, e) =>
        {
            using var p = new Pen(_tema.Linea, 1f);
            e.Graphics.DrawLine(p, 0, cab.Height - 1, cab.Width, cab.Height - 1);
        };

        var logo = new PanelIcono { Dock = DockStyle.Left, Width = 46, Icono = Icono.Ajustes, Tinta = () => _tema.Acento, Sombra = () => _tema.AcentoSuave };

        var textos = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel, Padding = new Padding(6, 12, 0, 10) };
        _titulo = new Label
        {
            Text = "OPCIONES", Dock = DockStyle.Top, Height = 28, ForeColor = _tema.Tinta,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
        };
        var sub = new Label
        {
            Text = "Todo se guarda solo, al instante. Sin botón de aceptar.",
            Dock = DockStyle.Top, Height = 20, ForeColor = _tema.Tinta2,
            Font = new Font("Segoe UI", 8.75f), TextAlign = ContentAlignment.MiddleLeft
        };
        textos.Controls.Add(sub);
        textos.Controls.Add(_titulo);
        cab.Controls.Add(textos);

        var cerrar = new BotonRedondo(_tema) { Text = "Cerrar", Dock = DockStyle.Right, Width = 92, Height = 34, Primario = false, Radio = 9 };
        cerrar.Click += (_, _) => Close();
        var cajaCerrar = new Panel { Dock = DockStyle.Right, Width = 100, BackColor = _tema.Panel, Padding = new Padding(0, 17, 0, 0) };
        cajaCerrar.Controls.Add(cerrar);
        cerrar.Dock = DockStyle.Fill;
        cab.Controls.Add(cajaCerrar);
        cab.Controls.Add(logo);

        raiz.Controls.Add(cab, 0, 0);
        raiz.SetColumnSpan(cab, 2);

        // --- barra lateral ---
        _laterales = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Fondo, AutoScroll = true, Padding = new Padding(0, 10, 0, 10) };
        var flujo = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = _tema.Fondo, Padding = new Padding(0)
        };
        foreach (var s in SeccionOpcion.Todas)
        {
            var sec = s;
            var b = new BotonSeccion(_tema, sec.Nombre, sec.Icono) { Width = 252, Margin = new Padding(0, 1, 0, 1) };
            b.Click += (_, _) => Mostrar(sec.Id);
            flujo.Controls.Add(b);
            _botones.Add((sec, b));
        }
        _laterales.Controls.Add(flujo);
        raiz.Controls.Add(_laterales, 0, 1);

        // --- pagina de la derecha ---
        _pagina = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel, Padding = new Padding(28, 20, 24, 10), AutoScroll = true };
        raiz.Controls.Add(_pagina, 1, 1);

        Controls.Add(raiz);
        raiz.ResumeLayout(true);
    }

    private void Mostrar(string id)
    {
        _actual = id;
        var sec = SeccionOpcion.PorId(id) ?? SeccionOpcion.Todas[0];
        foreach (var (s, b) in _botones)
            b.Activo = s.Id == id;

        if (_titulo != null) _titulo.Text = "OPCIONES · " + sec.Nombre.ToUpperInvariant();

        foreach (Control viejo in _pagina.Controls.Cast<Control>().ToArray())
        {
            _pagina.Controls.Remove(viejo);
            viejo.Dispose();
        }

        var contenido = sec.Construir(_tema, Guardar);
        contenido.Dock = DockStyle.Top;
        _pagina.Controls.Add(contenido);
    }

    /// <summary>Guarda los ajustes al instante y avisa a la ventana principal.</summary>
    private void Guardar()
    {
        _alCambiar?.Invoke();
    }

    // =====================================================================
    //  Piezas de interfaz reutilizables
    // =====================================================================

    internal static Label Titulo(Tema t, string texto) => new TituloSeccion(t, texto);

    internal static Label Nota(Tema t, string texto) => new()
    {
        Text = texto, AutoSize = true, ForeColor = t.Tinta2, Font = new Font("Segoe UI", 8.75f),
        MaximumSize = new Size(660, 0), Margin = new Padding(0, 0, 0, 10)
    };

    /// <summary>Panel con un icono propio pintado (para cabeceras).</summary>
    internal sealed class PanelIcono : Panel
    {
        public Icono Icono = QBASSuite.Ui.Icono.Ajustes;
        public Func<Color>? Tinta;
        public Func<Color>? Sombra;

        public PanelIcono()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var lado = Math.Min(28, Math.Min(Width, Height) - 8);
            var caja = new Rectangle((Width - lado) / 2, (Height - lado) / 2, lado, lado);
            Iconos.Dibujar(e.Graphics, Icono, caja, Tinta?.Invoke() ?? Color.Orange, Sombra?.Invoke() ?? Color.DarkOrange);
        }
    }
}
