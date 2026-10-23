using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QBASSuite.Ui;

/// <summary>
/// Piezas de interfaz PROPIAS de QBASFreemanCopy-Converter: interruptores y
/// deslizadores dibujados a mano, sin controles grises de Windows y sin copiar
/// el aspecto de ningun otro programa. Todo se pinta con colores del tema.
/// </summary>
internal static class Controles
{
    /// <summary>Fila con etiqueta, ayuda y un interruptor a la derecha.</summary>
    public static Control Interruptor(Tema t, string etiqueta, string ayuda, bool valor, Action<bool> cambio)
        => new FilaInterruptor(t, etiqueta, ayuda, valor, cambio);

    /// <summary>Fila con etiqueta, valor y una barra deslizadora.</summary>
    public static Control Deslizador(Tema t, string etiqueta, string unidad, string ayuda,
        int valor, int min, int max, int paso, Action<int> cambio)
        => new FilaDeslizador(t, etiqueta, unidad, ayuda, valor, min, max, paso, cambio);
}

/// <summary>Fila de opcion con interruptor dibujado (pastilla + bola).</summary>
internal sealed class FilaInterruptor : Control
{
    private readonly Tema _t;
    private readonly string _etiqueta;
    private readonly string _ayuda;
    private readonly Action<bool> _cambio;
    private bool _valor;
    private bool _encima;

    private const int AnchoPastilla = 44;
    private const int AltoPastilla = 24;

    public FilaInterruptor(Tema t, string etiqueta, string ayuda, bool valor, Action<bool> cambio)
    {
        _t = t;
        _etiqueta = etiqueta;
        _ayuda = ayuda ?? "";
        _valor = valor;
        _cambio = cambio;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = t.Panel;
        ForeColor = t.Tinta;
        Width = 620;
        Height = string.IsNullOrWhiteSpace(_ayuda) ? 40 : 56;
        Margin = new Padding(0, 1, 0, 1);
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleName = etiqueta;
        AccessibleRole = AccessibleRole.CheckButton;
    }

    private Rectangle Pastilla => new(Width - AnchoPastilla - 6, (Height - AltoPastilla) / 2, AnchoPastilla, AltoPastilla);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var x = 2;
        var y = string.IsNullOrWhiteSpace(_ayuda) ? (Height - 20) / 2 : 7;

        TextRenderer.DrawText(g, _etiqueta, new Font("Segoe UI", 9.5f),
            new Rectangle(x, y, Width - AnchoPastilla - 24, 22), _t.Tinta,
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);

        if (!string.IsNullOrWhiteSpace(_ayuda))
        {
            TextRenderer.DrawText(g, _ayuda, new Font("Segoe UI", 8.25f),
                new Rectangle(x + 1, y + 20, Width - AnchoPastilla - 24, 18), _t.Tinta2,
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        var p = Pastilla;
        var carril = _valor ? _t.Acento : (_encima ? Estilo.Mezcla(_t.Linea, _t.Tinta2, 0.25f) : _t.Linea);

        using (var camino = Estilo.Redondo(p, AltoPastilla / 2))
        using (var b = new SolidBrush(carril))
            g.FillPath(b, camino);

        var d = AltoPastilla - 8;
        var bolaX = _valor ? p.Right - d - 4 : p.X + 4;
        var bola = new Rectangle(bolaX, p.Y + 4, d, d);

        using (var sombra = new SolidBrush(Color.FromArgb(45, Color.Black)))
            g.FillEllipse(sombra, new Rectangle(bola.X, bola.Y + 1, d, d));

        using (var b = new SolidBrush(_valor ? Color.White : _t.Panel))
            g.FillEllipse(b, bola);
    }

    protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _encima = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        _valor = !_valor;
        Invalidate();
        _cambio(_valor);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            _valor = !_valor;
            Invalidate();
            _cambio(_valor);
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
}

/// <summary>Fila de opcion con barra deslizadora dibujada a mano.</summary>
internal sealed class FilaDeslizador : Control
{
    private readonly Tema _t;
    private readonly string _etiqueta;
    private readonly string _unidad;
    private readonly string _ayuda;
    private readonly Action<int> _cambio;
    private readonly int _min, _max, _paso;
    private int _valor;

    public FilaDeslizador(Tema t, string etiqueta, string unidad, string ayuda,
        int valor, int min, int max, int paso, Action<int> cambio)
    {
        _t = t;
        _etiqueta = etiqueta;
        _unidad = unidad ?? "";
        _ayuda = ayuda ?? "";
        _cambio = cambio;
        _min = min;
        _max = Math.Max(max, min + 1);
        _paso = Math.Max(1, paso);
        _valor = Math.Clamp(valor, _min, _max);

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = t.Panel;
        ForeColor = t.Tinta;
        Width = 620;
        Height = string.IsNullOrWhiteSpace(_ayuda) ? 48 : 64;
        Margin = new Padding(0, 2, 0, 2);
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleName = etiqueta;
        AccessibleRole = AccessibleRole.Slider;
    }

    private Rectangle Carril => new(6, Height - 20, Width - 30, 8);
    private Rectangle ZonaRaton => new(0, Height - 34, Width, 34);

    private float Fraccion => (float)(_valor - _min) / (_max - _min);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var texto = $"{_valor:N0}".Replace(",", "") + (_unidad.Length > 0 ? " " + _unidad : "");

        TextRenderer.DrawText(g, _etiqueta, new Font("Segoe UI", 9.5f),
            new Rectangle(2, 5, Width - 150, 22), _t.Tinta,
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);

        var fuenteValor = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        var anchoValor = TextRenderer.MeasureText(g, texto, fuenteValor).Width;
        TextRenderer.DrawText(g, texto, fuenteValor, new Rectangle(Width - anchoValor - 8, 5, anchoValor + 2, 22), _t.Acento,
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

        if (!string.IsNullOrWhiteSpace(_ayuda))
            TextRenderer.DrawText(g, _ayuda, new Font("Segoe UI", 8.25f), new Rectangle(3, 25, Width - 12, 18), _t.Tinta2,
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

        var c = Carril;
        using (var fondo = Estilo.Redondo(c, 4))
        using (var b = new SolidBrush(_t.Linea))
            g.FillPath(b, fondo);

        var cubierto = Math.Max(6, (int)(c.Width * Fraccion));
        using (var relleno = Estilo.Redondo(new Rectangle(c.X, c.Y, cubierto, c.Height), 4))
        using (var b = new SolidBrush(_t.Acento))
            g.FillPath(b, relleno);

        var d = 16;
        var x = c.X + (int)((c.Width - d) * Fraccion);
        var bola = new Rectangle(x, c.Y + (c.Height - d) / 2, d, d);

        using (var sombra = new SolidBrush(Color.FromArgb(60, Color.Black)))
            g.FillEllipse(sombra, new Rectangle(bola.X, bola.Y + 1, d, d));
        using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, bola);
        using (var p = new Pen(_t.Acento, 2f)) g.DrawEllipse(p, bola);
    }

    private void Poner(int x)
    {
        var c = Carril;
        var f = (float)(x - c.X) / Math.Max(1, c.Width);
        f = Math.Clamp(f, 0f, 1f);
        var v = _min + (int)Math.Round(f * (_max - _min) / _paso) * _paso;
        v = Math.Clamp(v, _min, _max);
        if (v == _valor) return;
        _valor = v;
        Invalidate();
        _cambio(_valor);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left) { Focus(); Poner(e.X); }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.Button == MouseButtons.Left) Poner(e.X);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var nuevo = _valor;
        if (e.KeyCode == Keys.Left) nuevo -= _paso;
        else if (e.KeyCode == Keys.Right) nuevo += _paso;
        else if (e.KeyCode == Keys.Home) nuevo = _min;
        else if (e.KeyCode == Keys.End) nuevo = _max;
        else { base.OnKeyDown(e); return; }

        nuevo = Math.Clamp(nuevo, _min, _max);
        if (nuevo != _valor) { _valor = nuevo; Invalidate(); _cambio(_valor); }
        e.Handled = true;
    }
}

/// <summary>Boton de la barra lateral: pastilla, barra de acento e icono propio.</summary>
internal sealed class BotonSeccion : Control
{
    private readonly Tema _t;
    private readonly Icono _icono;
    private bool _activo;
    private bool _encima;

    public BotonSeccion(Tema t, string texto, Icono icono)
    {
        _t = t;
        _icono = icono;
        Text = texto;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = t.Fondo;
        ForeColor = t.Tinta;
        Height = 46;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleName = texto;
        AccessibleRole = AccessibleRole.PushButton;
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Activo
    {
        get => _activo;
        set { if (_activo == value) return; _activo = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? _t.Fondo);

        var r = new Rectangle(8, 3, Width - 16, Height - 6);
        if (_activo)
        {
            using var camino = Estilo.Redondo(r, 9);
            using var b = new SolidBrush(Estilo.Mezcla(_t.Fondo, _t.Acento, 0.16f));
            g.FillPath(b, camino);

            using var barra = Estilo.Redondo(new Rectangle(2, r.Y + 8, 3, r.Height - 16), 2);
            using var bb = new SolidBrush(_t.Acento);
            g.FillPath(bb, barra);
        }
        else if (_encima)
        {
            using var camino = Estilo.Redondo(r, 9);
            using var b = new SolidBrush(Estilo.Mezcla(_t.Fondo, _t.Tinta, 0.05f));
            g.FillPath(b, camino);
        }

        var tinta = _activo ? _t.Acento : _t.Tinta2;
        var cajaIcono = new Rectangle(r.X + 12, r.Y + (r.Height - 18) / 2, 18, 18);
        Iconos.Dibujar(g, _icono, cajaIcono, tinta, _t.AcentoSuave);

        TextRenderer.DrawText(g, Text, new Font("Segoe UI", 9.5f, _activo ? FontStyle.Bold : FontStyle.Regular),
            new Rectangle(cajaIcono.Right + 10, r.Y, r.Right - cajaIcono.Right - 16, r.Height),
            _activo ? _t.Tinta : _t.Tinta2,
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _encima = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
}

/// <summary>Titulo de seccion con una barra de acento a la izquierda.</summary>
internal sealed class TituloSeccion : Label
{
    private readonly Color _acento;

    public TituloSeccion(Tema t, string texto)
    {
        _acento = t.Acento;
        Text = texto;
        AutoSize = true;
        ForeColor = t.Tinta;
        Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        Padding = new Padding(14, 2, 0, 6);
        Margin = new Padding(0, 12, 0, 6);
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Color.Transparent);
        using (var b = new SolidBrush(_acento))
            g.FillRectangle(b, 0, Height / 2 - 8, 3, 16);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(Padding.Left, 0, Width - Padding.Left, Height), ForeColor,
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
    }
}
