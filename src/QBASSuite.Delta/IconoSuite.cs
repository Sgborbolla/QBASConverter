using System.Drawing;
using System.Drawing.Drawing2D;

namespace QBASSuite.Desktop;

/// <summary>
/// El icono de QBAS Suite.
///
/// Se dibuja en memoria con GDI+ en vez de leerse de un .ico para no
// arrastrar un binario en el repositorio y para que se vea nitido en
// cualquier tamano (16, 32, 48, 256). El motivo es el mismo del logo
// vectorial de la interfaz: fondo azul bandera, chevron carmesi con la
// estrella blanca y las flechas de copia multiple en abanico.
/// </summary>
internal static class IconoSuite
{
    public static Icon Crear(int tamano)
    {
        using var bmp = new Bitmap(tamano, tamano);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.Transparent);

            float s = tamano / 48f;

            // --- fondo redondeado con degradado azul ---
            using (var camino = new GraphicsPath())
            {
                camino.AddArc(2 * s, 2 * s, 10 * s, 10 * s, 180, 90);
                camino.AddArc(36 * s, 2 * s, 10 * s, 10 * s, 270, 90);
                camino.AddArc(36 * s, 36 * s, 10 * s, 10 * s, 0, 90);
                camino.AddArc(2 * s, 36 * s, 10 * s, 10 * s, 90, 90);
                camino.CloseFigure();

                using (var brocha = new LinearGradientBrush(
                    new RectangleF(0, 0, tamano, tamano),
                    ColorTranslator.FromHtml("#002A8F"),
                    ColorTranslator.FromHtml("#0B1A30"),
                    45f))
                {
                    g.FillPath(brocha, camino);
                }
                using (var borde = new Pen(Color.FromArgb(51, 255, 255, 255), 1.5f * s))
                    g.DrawPath(borde, camino);
            }

            // --- chevron carmesi ---
            using (var rojo = new SolidBrush(ColorTranslator.FromHtml("#C4161A")))
            {
                var triangulo = new[]
                {
                    new PointF(4 * s, 4 * s),
                    new PointF(22 * s, 24 * s),
                    new PointF(4 * s, 44 * s)
                };
                g.FillPolygon(rojo, triangulo);
            }

            // --- estrella blanca ---
            using (var blanco = new SolidBrush(Color.White))
            {
                float[] px = { 10, 11.8f, 15.8f, 12.8f, 13.7f, 10, 6.3f, 7.2f, 4.2f, 8.2f };
                float[] py = { 18.5f, 22.2f, 22.6f, 25.2f, 29.1f, 27, 29.1f, 25.2f, 22.6f, 22.2f };
                var estrella = new PointF[px.Length];
                for (int i = 0; i < px.Length; i++)
                    estrella[i] = new PointF(px[i] * s, py[i] * s);
                g.FillPolygon(blanco, estrella);
            }

            // --- flechas en abanico (la copia multiple) ---
            using (var flecha = new Pen(Color.White, Math.Max(1f, 2.5f * s)))
            {
                flecha.StartCap = LineCap.Round;
                flecha.EndCap = LineCap.Round;
                flecha.LineJoin = LineJoin.Round;

                Flecha(g, flecha, s, 25, 15, 38, 15, 33, 11, 38, 15, 33, 19);
                Flecha(g, flecha, s, 25, 24, 42, 24, 37, 20, 42, 24, 37, 28);
                Flecha(g, flecha, s, 25, 33, 38, 33, 33, 29, 38, 33, 33, 37);
            }
        }

        // El bitmap tiene que copiarse a uno con formato de icono de 32 bits
        // para que Windows lo muestre con transparencia.
        var icono = Icon.FromHandle(bmp.GetHicon());
        return (Icon)icono.Clone();
    }

    /// <summary>Una flecha: el cuerpo va de (x1,y1) a (x2,y2) y la punta es un
    /// triangulo abierto con el vertice en (tx,ty) y las dos patas en (ax,ay) y (bx,by).</summary>
    private static void Flecha(Graphics g, Pen p, float s,
        float x1, float y1, float x2, float y2,
        float ax, float ay, float tx, float ty,
        float bx, float by)
    {
        g.DrawLine(p, x1 * s, y1 * s, x2 * s, y2 * s);
        g.DrawLine(p, ax * s, ay * s, tx * s, ty * s);
        g.DrawLine(p, tx * s, ty * s, bx * s, by * s);
    }
}
