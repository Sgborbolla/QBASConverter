using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QBASSuite.Ui;

/// <summary>
/// Estilo comun de QBASFreemanCopy-Converter: esquinas redondeadas de verdad y
/// controles suaves. Todo se pinta aqui, sin imagenes ni recursos de nadie.
/// </summary>
internal static class Estilo
{
    /// <summary>Camino redondeado reutilizable. Seguro con tamanos diminutos.</summary>
    public static GraphicsPath Redondo(Rectangle r, int radio)
    {
        var p = new GraphicsPath();
        if (r.Width < 3 || r.Height < 3) { p.AddRectangle(r); return p; }
        if (radio <= 0) { p.AddRectangle(r); return p; }

        var d = radio * 2;
        if (d > r.Width) d = r.Width;
        if (d > r.Height) d = r.Height;
        if (d < 2) { p.AddRectangle(r); return p; }

        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    /// <summary>Activa el doble buffer en un control y en todos sus hijos.</summary>
    public static void DobleBuffer(Control c)
    {
        try
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            prop?.SetValue(c, true, null);
        }
        catch { }

        foreach (Control hijo in c.Controls) DobleBuffer(hijo);
    }

    /// <summary>Aclara u oscurece un color (t = -1 negro, 0 igual, 1 blanco).</summary>
    public static Color Mezcla(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t),
        (int)(a.G + (b.G - a.G) * t),
        (int)(a.B + (b.B - a.B) * t));
}

/// <summary>
/// Boton propio: fondo redondeado, borde fino, texto centrado y estados de
/// raton. Nada de aspecto gris cuadrado de Windows.
/// </summary>
internal sealed class BotonRedondo : Button
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Tema Tema { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Primario { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radio { get; set; } = 3;
    /// <summary>Va colocado encima de la zona de discos (fondo verde translucido).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SobreDiscos { get; set; }

    private bool _encima;
    private bool _pulsado;

    public BotonRedondo(Tema tema)
    {
        Tema = tema;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 8.75f, FontStyle.Regular);
    }

    protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _encima = false; _pulsado = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pulsado = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pulsado = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        if (Width < 4 || Height < 4) return;

        try
        {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        // El fondo del boton tiene que ser el mismo que el de la fila donde vive:
        // si la fila es translucida, se usa su color ya mezclado (si no, quedaria
        // un rectangulo negro detras de las esquinas).
        var fondoPadre = SobreDiscos
            ? Estilo.Mezcla(Tema.PanelDiscos, Color.White, 0.10f)
            : (Parent?.BackColor ?? Tema.Fondo);
        if (fondoPadre.A < 255) fondoPadre = Color.FromArgb(255, fondoPadre);
        g.Clear(fondoPadre);

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using var camino = Estilo.Redondo(r, Radio);

        var baseColor = Primario ? Tema.Acento : Tema.Panel2;
        if (_encima) baseColor = Estilo.Mezcla(baseColor, Primario ? Color.White : Tema.Acento, 0.12f);
        if (_pulsado) baseColor = Estilo.Mezcla(baseColor, Color.Black, 0.10f);
        if (!Enabled) baseColor = Estilo.Mezcla(baseColor, Tema.Fondo, 0.55f);

        using (var b = new SolidBrush(baseColor)) g.FillPath(b, camino);

        if (!Primario)
        {
            using var pl = new Pen(_encima ? Tema.Acento : Tema.Linea, 1f);
            g.DrawPath(pl, camino);
        }

        var tinta = !Enabled ? Tema.Tenue
            : Primario ? Tema.SobreAcento
            : Tema.Tinta;

        var texto = Text;
        var tieneImagen = Image != null;
        var anchoTexto = 0f;
        var altoTexto = 0f;

        var fuente = Font;
        var tam = g.MeasureString(texto, fuente);
        anchoTexto = tam.Width;
        altoTexto = tam.Height;

        if (tieneImagen)
        {
            var icono = Image!;
            var total = icono.Width + (texto.Length > 0 ? 6 + anchoTexto : 0);
            var inicioX = (Width - total) / 2f;
            var iy = (Height - icono.Height) / 2f;
            g.DrawImage(icono, inicioX, iy, icono.Width, icono.Height);

            if (texto.Length > 0)
            {
                using var brocha = new SolidBrush(tinta);
                using var formato = new StringFormat { LineAlignment = StringAlignment.Center };
                g.DrawString(texto, Font, brocha,
                    new RectangleF(inicioX + icono.Width + 6, 0, anchoTexto + 2, Height), formato);
            }
        }
        else if (texto.Length > 0)
        {
            using var brocha = new SolidBrush(tinta);
            using var formato = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(texto, Font, brocha, new RectangleF(0, 0, Width, Height), formato);
        }
        }
        catch { }
    }
}

/// <summary>Panel con esquinas redondeadas (fondo suave, borde fino).</summary>
internal sealed class PanelRedondo : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Tema Tema { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radio { get; set; } = 11;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ConBorde { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ConSombra { get; set; }

    public PanelRedondo(Tema tema)
    {
        Tema = tema;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.UserPaint | ControlStyles.ResizeRedraw
                 | ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        if (Width < 6 || Height < 6) { base.OnPaintBackground(e); return; }

        // Fondo transparente: se deja ver lo que hay detras (el fondo con el
        // icono de marca de agua). No se pinta ninguna caja.
        if (BackColor.A == 0) { base.OnPaintBackground(e); return; }

        try
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Si el fondo lleva transparencia, primero se pinta lo que hay
            // detras (el fondo con el icono) y luego la caja encima. Asi la
            // caja se ve translucida y bonita, no un bloque pegado.
            if (BackColor.A < 255) base.OnPaintBackground(e);
            else g.Clear(Parent?.BackColor ?? Tema.PanelDiscos);

            var r = new Rectangle(0, 0, Width - 1, Height - 1);

            if (ConSombra)
            {
                for (var i = 3; i >= 1; i--)
                {
                    using var sombra = Estilo.Redondo(new Rectangle(i, i + 1, Width - 1 - i, Height - 1 - i), Radio);
                    using var b = new SolidBrush(Color.FromArgb(10, Color.Black));
                    g.FillPath(b, sombra);
                }
            }

            using var camino = Estilo.Redondo(r, Radio);
            using (var b = new SolidBrush(BackColor)) g.FillPath(b, camino);
            if (ConBorde)
            {
                using var pl = new Pen(Color.FromArgb(BackColor.A < 255 ? 70 : 255, Color.White), 1f);
                g.DrawPath(pl, camino);
            }
        }
        catch
        {
            base.OnPaintBackground(e);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }
}

/// <summary>Lista (ListView) con la cabecera y las filas pintadas por nosotros:
/// la de Windows sale blanca y desentona en el tema oscuro. Se usa en el
/// navegador interno y en la cola del convertidor.</summary>
internal sealed class ListaOscura : ListView
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Tema Tema { get; set; } = Tema.PorDefecto;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FondoFilas { get; set; } = Color.FromArgb(35, 35, 35);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorSeleccion { get; set; } = Color.FromArgb(60, 76, 194, 255);

    public ListaOscura(Tema tema)
    {
        Tema = tema;
        OwnerDraw = true;
        View = View.Details;
        FullRowSelect = true;
        HideSelection = false;
        BorderStyle = BorderStyle.None;
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        // La barra de la lista tambien en oscuro.
        HandleCreated += (_, _) => VentanaDoble.TemaOscuroBarra(Handle);
    }

    protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
    {
        using (var fondo = new SolidBrush(Tema.Panel2)) e.Graphics.FillRectangle(fondo, e.Bounds);
        using (var pluma = new Pen(Tema.Linea))
        {
            e.Graphics.DrawLine(pluma, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            e.Graphics.DrawLine(pluma, e.Bounds.Right - 1, e.Bounds.Top + 4, e.Bounds.Right - 1, e.Bounds.Bottom - 6);
        }
        var texto = e.Header?.Text ?? "";
        TextRenderer.DrawText(e.Graphics, texto, Font, e.Bounds, Tema.Tinta2,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void OnDrawItem(DrawListViewItemEventArgs e)
    {
        var seleccionado = e.Item.Selected;
        var color = seleccionado ? ColorSeleccion : FondoFilas;
        using (var b = new SolidBrush(color)) e.Graphics.FillRectangle(b, e.Bounds);
    }

    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
    {
        var it = e.Item;
        if (it == null) return;
        var caja = e.Bounds;

        var tinta = it.ForeColor == Color.Empty ? Tema.Tinta : it.ForeColor;
        if (it.ForeColor.A == 0 || (it.ForeColor.R < 45 && it.ForeColor.G < 45 && it.ForeColor.B < 45))
            tinta = Tema.Tinta;

        // La imagen solo se dibuja en la primera columna.
        if (e.ColumnIndex == 0 && SmallImageList != null && it.ImageIndex >= 0
            && it.ImageIndex < SmallImageList.Images.Count)
        {
            var img = SmallImageList.Images[it.ImageIndex];
            e.Graphics.DrawImage(img, caja.Left + 2, caja.Top + (caja.Height - img.Height) / 2, img.Width, img.Height);
            caja = new Rectangle(caja.Left + img.Width + 6, caja.Top, caja.Width - img.Width - 6, caja.Height);
        }

        var banderas = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
        banderas |= e.ColumnIndex == 0 ? TextFormatFlags.Left : TextFormatFlags.Right;
        TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? "", it.Font ?? Font, caja, tinta,
            banderas | TextFormatFlags.NoPrefix);
    }
}

/// <summary>Barra de progreso fina dibujada por nosotros (la de Windows sale
/// blanca y rompe el tema oscuro). Tiene Value/Maximum como la de siempre.</summary>
internal sealed class BarraFina : Control
{
    private readonly Tema _tema;
    private int _maximo = 100;
    private int _valor;

    public BarraFina(Tema tema)
    {
        _tema = tema;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Height = 10;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum
    {
        get => _maximo;
        set { _maximo = Math.Max(1, value); if (_valor > _maximo) _valor = _maximo; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _valor;
        set { _valor = Math.Clamp(value, 0, _maximo); Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var alto = Math.Max(4, Height - 4);
        var y = (Height - alto) / 2;
        var r = new Rectangle(0, y, Math.Max(4, Width - 1), alto);

        using (var camino = Estilo.Redondo(r, alto / 2))
        using (var fondo = new SolidBrush(Color.FromArgb(110, _tema.Linea)))
            g.FillPath(fondo, camino);

        var ancho = (int)(r.Width * (double)_valor / _maximo);
        if (ancho < 2) return;
        var r2 = new Rectangle(r.X, r.Y, Math.Min(ancho, r.Width), r.Height);
        using var camino2 = Estilo.Redondo(r2, alto / 2);
        using var relleno = new LinearGradientBrush(
            new Rectangle(r2.X, r2.Y, Math.Max(2, r2.Width), r2.Height),
            _tema.Acento, _tema.Hecho, 0f);
        g.FillPath(relleno, camino2);
    }
}

/// <summary>
/// Panel con desplazamiento y barra FINA pintada por nosotros: la barra nativa
/// de Windows sale blanca y rompe el tema oscuro. Se oculta la del sistema y se
/// dibuja una barra de 3 px que ademas se puede arrastrar.
/// </summary>
internal sealed class PanelDesliz : FlowLayoutPanel
{
    private readonly Tema _tema;
    private bool _arrastrando;

    [DllImport("user32.dll")]
    private static extern bool ShowScrollBar(IntPtr ventana, int barra, bool mostrar);

    private const int SbVert = 1;

    public PanelDesliz(Tema tema)
    {
        _tema = tema;
        AutoScroll = true;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        OcultarBarraNativa();
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        OcultarBarraNativa();
        Invalidate();
    }

    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        OcultarBarraNativa();
        Invalidate();
    }

    private void OcultarBarraNativa()
    {
        try { if (IsHandleCreated) ShowScrollBar(Handle, SbVert, false); } catch { }
    }

    /// <summary>Zona (en pixeles) donde se puede agarrar la barra.</summary>
    private const int Zona = 18;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var v = VerticalScroll;
        var recorrido = v.Maximum - v.LargeChange;
        if (recorrido <= 0 || v.LargeChange <= 0) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var x = Width - 7;
        var altoPista = Height - 4;

        // Pista muy tenue.
        using (var pista = Estilo.Redondo(new Rectangle(x, 2, 3, altoPista), 2))
        using (var b = new SolidBrush(Color.FromArgb(28, _tema.Tinta)))
            g.FillPath(b, pista);

        // El boton: cuanto mas contenido, mas pequeno.
        var altoMin = 26;
        var alto = Math.Max(altoMin, (int)(altoPista * (double)v.LargeChange / Math.Max(1, v.Maximum)));
        alto = Math.Min(alto, altoPista);
        var y = 2 + (int)((altoPista - alto) * (v.Value / (double)recorrido));

        using var boton = Estilo.Redondo(new Rectangle(x, y, 3, alto), 2);
        using var relleno = new SolidBrush(Color.FromArgb(190, _tema.Acento));
        g.FillPath(relleno, boton);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || e.X < Width - Zona) return;
        _arrastrando = true;
        Capture = true;
        IrAPosicion(e.Y);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_arrastrando) IrAPosicion(e.Y);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_arrastrando) return;
        _arrastrando = false;
        Capture = false;
    }

    /// <summary>Coloca el contenido como si se hubiera arrastrado el boton a esa altura.</summary>
    private void IrAPosicion(int y)
    {
        var v = VerticalScroll;
        var recorrido = v.Maximum - v.LargeChange;
        if (recorrido <= 0) return;

        var altoPista = Math.Max(1, Height - 4);
        var altoMin = 26;
        var alto = Math.Max(altoMin, (int)(altoPista * (double)v.LargeChange / Math.Max(1, v.Maximum)));
        alto = Math.Min(alto, altoPista);
        var util = Math.Max(1, altoPista - alto);

        var rel = Math.Clamp((y - 2 - alto / 2) / (double)util, 0d, 1d);
        v.Value = (int)Math.Round(rel * recorrido);
        Invalidate();
    }
}
