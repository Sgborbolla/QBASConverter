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
    private readonly List<(SeccionOpcion sec, Button boton)> _botones = new();
    private string _actual = "general";

    public VentanaOpciones(Tema tema, Action alCambiar)
    {
        _tema = tema;
        _alCambiar = alCambiar;

        Text = "QBASFreemanCopy-Converter · Opciones";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 640);
        MinimumSize = new Size(820, 540);
        Font = new Font("Segoe UI", 9f);
        BackColor = _tema.Fondo;
        ForeColor = _tema.Tinta;

        Construir();
        Mostrar("general");
    }

    private void Construir()
    {
        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
            BackColor = _tema.Fondo, Margin = new Padding(0), Padding = new Padding(0)
        };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // --- cabecera: titulo + cierre ---
        var cab = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel, Padding = new Padding(14, 8, 10, 8) };
        var logo = new PanelIcono { Dock = DockStyle.Left, Width = 44, Icono = Icono.Ajustes, Tinta = () => _tema.Acento, Sombra = () => _tema.AcentoSuave };
        cab.Controls.Add(logo);
        _titulo = new Label
        {
            Text = "OPCIONES", Dock = DockStyle.Fill, ForeColor = _tema.Acento,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };
        cab.Controls.Add(_titulo);
        var cerrar = new Button { Text = "Cerrar", Dock = DockStyle.Right, Width = 90, FlatStyle = FlatStyle.Flat, BackColor = _tema.Panel2, ForeColor = _tema.Tinta };
        cerrar.Click += (_, _) => Close();
        cab.Controls.Add(cerrar);
        raiz.Controls.Add(cab, 0, 0);
        raiz.SetColumnSpan(cab, 2);

        // --- barra lateral: una seccion debajo de otra ---
        _laterales = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel2, AutoScroll = true, Padding = new Padding(0, 6, 0, 6) };
        var flujo = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = _tema.Panel2, Padding = new Padding(0)
        };
        foreach (var s in SeccionOpcion.Todas)
        {
            var sec = s;
            var b = new Button
            {
                Text = "   " + sec.Nombre,
                Width = 236, Height = 40, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
                ImageAlign = ContentAlignment.MiddleLeft, TextImageRelation = TextImageRelation.ImageBeforeText,
                Image = Iconos.Bitmap(sec.Icono, 20, _tema.Tinta, _tema.AcentoSuave),
                BackColor = _tema.Panel2, ForeColor = _tema.Tinta, Padding = new Padding(10, 0, 0, 0)
            };
            b.Click += (_, _) => Mostrar(sec.Id);
            flujo.Controls.Add(b);
            _botones.Add((sec, b));
        }
        _laterales.Controls.Add(flujo);
        raiz.Controls.Add(_laterales, 0, 1);

        // --- pagina de la derecha ---
        _pagina = new Panel { Dock = DockStyle.Fill, BackColor = _tema.Panel, Padding = new Padding(18, 14, 18, 14), AutoScroll = true };
        raiz.Controls.Add(_pagina, 1, 1);

        Controls.Add(raiz);
    }

    private void Mostrar(string id)
    {
        _actual = id;
        var sec = SeccionOpcion.PorId(id) ?? SeccionOpcion.Todas[0];
        foreach (var (s, b) in _botones)
        {
            var activo = s.Id == id;
            b.BackColor = activo ? _tema.Acento : _tema.Panel2;
            b.ForeColor = activo ? _tema.SobreAcento : _tema.Tinta;
            b.Image = Iconos.Bitmap(s.Icono, 20, activo ? _tema.SobreAcento : _tema.Tinta, _tema.AcentoSuave);
        }
        if (_titulo != null) _titulo.Text = sec.Nombre.ToUpperInvariant();

        _pagina.Controls.Clear();
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

    internal static Label Titulo(Tema t, string texto) => new()
    {
        Text = texto, AutoSize = true, ForeColor = t.Acento,
        Font = new Font("Segoe UI", 10f, FontStyle.Bold), Margin = new Padding(0, 4, 0, 10)
    };

    internal static Label Nota(Tema t, string texto) => new()
    {
        Text = texto, AutoSize = true, ForeColor = t.Tinta2, MaximumSize = new Size(640, 0),
        Margin = new Padding(0, 0, 0, 12)
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
