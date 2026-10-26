using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QBASSuite.Ui;

/// <summary>
/// Las DOS imagenes de QBASFreemanCopy-Converter: una para el convertidor y otra
/// para el multicopiador. Se cargan una sola vez y se reutilizan, para no estar
/// leyendo el disco cada vez que se repinta la ventana.
/// </summary>
internal static class Marca
{
    public const string Copiador = "icono-copiador.png";
    public const string Convertidor = "icono-convertidor.png";

    private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static Bitmap? Imagen(string archivo)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(archivo, out var ya)) return ya;

            Bitmap? bmp = null;
            try
            {
                var ruta = Path.Combine(AppContext.BaseDirectory, archivo);
                if (File.Exists(ruta))
                {
                    using var original = new Bitmap(ruta);
                    bmp = new Bitmap(original);
                }
            }
            catch { }

            Cache[archivo] = bmp;
            return bmp;
        }
    }

    /// <summary>Pinta la imagen recortada en esquinas redondeadas dentro de una caja.</summary>
    public static void Dibujar(Graphics g, Image? imagen, Rectangle caja, int radio, bool conBorde = true, Tema? tema = null)
    {
        if (imagen == null || caja.Width < 4 || caja.Height < 4) return;

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using var camino = Estilo.Redondo(caja, radio);
        var estado = g.Save();
        g.SetClip(camino);

        // Recorte tipo "cover": llena la caja sin deformar la imagen.
        var escala = Math.Max((float)caja.Width / imagen.Width, (float)caja.Height / imagen.Height);
        var ancho = (int)Math.Ceiling(imagen.Width * escala);
        var alto = (int)Math.Ceiling(imagen.Height * escala);
        var x = caja.X + (caja.Width - ancho) / 2;
        var y = caja.Y + (caja.Height - alto) / 2;
        g.DrawImage(imagen, new Rectangle(x, y, ancho, alto));

        g.Restore(estado);

        if (conBorde && tema != null)
        {
            using var pluma = new Pen(Color.FromArgb(46, tema.Oscuro ? Color.White : Color.Black), 1f);
            g.DrawPath(pluma, camino);
        }
    }
}

/// <summary>Control pequeno que muestra una de las dos imagenes de la marca.</summary>
internal sealed class PanelFoto : Control
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Tema Tema { get; set; }

    private readonly string _archivo;

    public PanelFoto(Tema tema, string archivo)
    {
        Tema = tema;
        _archivo = archivo;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.UserPaint | ControlStyles.ResizeRedraw
                 | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? Tema.Panel);
        var lado = Math.Min(Width, Height) - 2;
        if (lado < 4) return;
        var caja = new Rectangle((Width - lado) / 2, (Height - lado) / 2, lado, lado);
        Marca.Dibujar(e.Graphics, Marca.Imagen(_archivo), caja, Math.Max(4, lado / 4), true, Tema);
    }
}
