using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
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
    public int Radio { get; set; } = 9;

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
        Font = new Font("Segoe UI", 9f, FontStyle.Regular);
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
        g.Clear(Parent?.BackColor ?? Tema.Fondo);

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

        using (var fuente = Font)
        {
            var tam = g.MeasureString(texto, fuente);
            anchoTexto = tam.Width;
            altoTexto = tam.Height;
        }

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
                 | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        if (Width < 6 || Height < 6) { base.OnPaintBackground(e); return; }

        try
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Tema.PanelDiscos);

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
                using var pl = new Pen(Tema.Linea, 1f);
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
