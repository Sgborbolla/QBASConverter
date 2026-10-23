using System.Drawing;
using System.Drawing.Drawing2D;

namespace QBASSuite.Ui;

/// <summary>
/// Formas decorativas de los temas. Todas son DIBUJOS ORIGINALES hechos con
/// trazados (no son logotipos ni personajes de ninguna serie). Cada una evoca
/// un estilo: una hoja, una llama, una luna, un rayo, un engranaje...
/// </summary>
public enum FormaTema
{
    HojaNinja,      // hoja con nervio (ninja)
    Esfera,         // circulo con estrellas
    Ancla,          // ancla marina
    Cuadricula,     // ajedrez
    Manto,          // capa ondeando
    EstrellaHeroe,  // estrella de trazo grueso
    Luna,           // medialuna con estrella
    Filo,           // hoja de espada estilizada
    Acero,          // espada ancha de ceniza
    Transmutacion,  // circulo alquimico
    Muro,           // muralla con almenas
    Katana,         // filo curvo
    EmblemaMecha,   // escudo con cuernos (generico)
    LlamaAzul,      // llama de dos tonos
    Colmillo,       // colmillo de bestia
    Corona,         // corona de monarca
    Aullido,        // cabeza de lobo estilizada (geometrica)
    Escarlata,      // espiral de humo
    Llamarada,      // llama grande
    SonrisaFilo,    // media luna dentada
    Lampara,        // lampara de genio
    Engranaje,      // engranaje con llama
    Constelacion,   // puntos unidos
    Nube,           // nube estilizada
    Cuaderno,       // cuaderno con pluma
    Espadas,        // dos espadas cruzadas
    EscudoDaga,     // escudo con daga
    NueveColas,     // abanico de colas
    Magica,         // varita con estrella
    AnclaFuego,     // ancla con llama
    CoronaSombra    // silueta con corona (monarca)
}

/// <summary>Dibuja una forma del tema dentro de un rectangulo dado.</summary>
public static class Formas
{
    /// <summary>Traza y rellena la forma. `tinta` es el color de la linea/relleno
    /// y `sombra` un tono mas oscuro para dar volumen.</summary>
    public static void Dibujar(Graphics g, FormaTema forma, Rectangle r, Color tinta, Color sombra)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var lado = Math.Min(r.Width, r.Height);
        var cx = r.Left + r.Width / 2f;
        var cy = r.Top + r.Height / 2f;
        var s = lado / 100f;            // escala: las formas se piensan en 100x100
        using var pluma = new Pen(tinta, Math.Max(2f, 6f * s)) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var relleno = new SolidBrush(tinta);
        using var fondo = new SolidBrush(sombra);

        switch (forma)
        {
            case FormaTema.HojaNinja:
            {
                using var raiz = new GraphicsPath();
                raiz.AddBezier(P(cx, cy - 40 * s), P(cx + 26 * s, cy - 10 * s), P(cx + 18 * s, cy + 28 * s), P(cx, cy + 40 * s));
                raiz.AddBezier(P(cx, cy + 40 * s), P(cx - 18 * s, cy + 28 * s), P(cx - 26 * s, cy - 10 * s), P(cx, cy - 40 * s));
                Edges(raiz, g, tinta, sombra, pluma);
                g.DrawLine(pluma, cx, cy - 34 * s, cx, cy + 30 * s);
                g.DrawLine(pluma, cx, cy, cx + 16 * s, cy - 10 * s);
                g.DrawLine(pluma, cx, cy + 12 * s, cx - 14 * s, cy + 4 * s);
                break;
            }
            case FormaTema.Esfera:
            {
                g.FillEllipse(fondo, cx - 36 * s, cy - 36 * s, 72 * s, 72 * s);
                g.DrawEllipse(pluma, cx - 36 * s, cy - 36 * s, 72 * s, 72 * s);
                Estrella(g, cx, cy, 5, 22 * s, 9 * s, tinta);
                break;
            }
            case FormaTema.Ancla:
            {
                g.DrawEllipse(pluma, cx - 8 * s, cy - 36 * s, 16 * s, 16 * s);
                g.DrawLine(pluma, cx, cy - 20 * s, cx, cy + 30 * s);
                g.DrawLine(pluma, cx - 18 * s, cy - 8 * s, cx + 18 * s, cy - 8 * s);
                g.DrawArc(pluma, cx - 26 * s, cy + 2 * s, 52 * s, 34 * s, 20, 140);
                break;
            }
            case FormaTema.Cuadricula:
            {
                var m = 9f; var paso = 74 * s / m;
                using var claro = new SolidBrush(sombra);
                for (int i = 0; i < 8; i++)
                    for (int j = 0; j < 8; j++)
                        if ((i + j) % 2 == 0)
                            g.FillRectangle(claro, cx - 37 * s + i * paso, cy - 37 * s + j * paso, paso, paso);
                g.DrawRectangle(pluma, cx - 37 * s, cy - 37 * s, 74 * s, 74 * s);
                break;
            }
            case FormaTema.Manto:
            {
                using var capa = new GraphicsPath();
                Tramo(capa, P(cx - 34 * s, cy - 30 * s));
                Tramo(capa, P(cx + 34 * s, cy - 30 * s));
                capa.AddBezier(P(cx + 34 * s, cy - 30 * s), P(cx + 22 * s, cy + 20 * s), P(cx + 30 * s, cy + 34 * s), P(cx + 10 * s, cy + 40 * s));
                Tramo(capa, P(cx - 10 * s, cy + 40 * s));
                capa.AddBezier(P(cx - 10 * s, cy + 40 * s), P(cx - 30 * s, cy + 34 * s), P(cx - 22 * s, cy + 20 * s), P(cx - 34 * s, cy - 30 * s));
                Edges(capa, g, tinta, sombra, pluma);
                break;
            }
            case FormaTema.EstrellaHeroe:
                Estrella(g, cx, cy, 5, 42 * s, 18 * s, tinta);
                break;
            case FormaTema.Luna:
            {
                g.FillPie(fondo, cx - 34 * s, cy - 34 * s, 68 * s, 68 * s, 300, 210);
                g.DrawArc(pluma, cx - 34 * s, cy - 34 * s, 68 * s, 68 * s, 300, 210);
                Estrella(g, cx + 22 * s, cy - 22 * s, 5, 13 * s, 5 * s, tinta);
                break;
            }
            case FormaTema.Filo:
            {
                using var filo = new GraphicsPath();
                Tramo(filo, P(cx, cy - 40 * s));
                Tramo(filo, P(cx + 12 * s, cy - 8 * s));
                Tramo(filo, P(cx, cy + 34 * s));
                Tramo(filo, P(cx - 12 * s, cy - 8 * s));
                Edges(filo, g, tinta, sombra, pluma);
                break;
            }
            case FormaTema.Acero:
            {
                using var esp = new GraphicsPath();
                Tramo(esp, P(cx - 7 * s, cy - 40 * s));
                Tramo(esp, P(cx + 7 * s, cy - 40 * s));
                Tramo(esp, P(cx + 7 * s, cy + 12 * s));
                Tramo(esp, P(cx, cy + 26 * s));
                Tramo(esp, P(cx - 7 * s, cy + 12 * s));
                Edges(esp, g, tinta, sombra, pluma);
                g.DrawLine(pluma, cx - 20 * s, cy + 16 * s, cx + 20 * s, cy + 16 * s);
                g.DrawLine(pluma, cx, cy + 16 * s, cx, cy + 36 * s);
                break;
            }
            case FormaTema.Transmutacion:
            {
                g.DrawEllipse(pluma, cx - 36 * s, cy - 36 * s, 72 * s, 72 * s);
                g.DrawEllipse(pluma, cx - 24 * s, cy - 24 * s, 48 * s, 48 * s);
                using var tri = new GraphicsPath();
                tri.AddPolygon(new[] { P(cx, cy - 16 * s), P(cx + 14 * s, cy + 10 * s), P(cx - 14 * s, cy + 10 * s) });
                g.FillPath(relleno, tri);
                break;
            }
            case FormaTema.Muro:
            {
                for (int i = 0; i < 3; i++)
                    g.FillRectangle(i % 2 == 0 ? relleno : fondo, cx - 38 * s + i * 26 * s, cy - 30 * s, 24 * s, 22 * s);
                g.FillRectangle(fondo, cx - 38 * s, cy - 8 * s, 76 * s, 42 * s);
                g.DrawRectangle(pluma, cx - 38 * s, cy - 30 * s, 76 * s, 64 * s);
                break;
            }
            case FormaTema.Katana:
            {
                using var curva = new GraphicsPath();
                curva.AddBezier(P(cx - 30 * s, cy + 28 * s), P(cx - 4 * s, cy + 6 * s), P(cx + 10 * s, cy - 20 * s), P(cx + 30 * s, cy - 38 * s));
                g.DrawPath(pluma, curva);
                g.DrawLine(pluma, cx - 34 * s, cy + 32 * s, cx - 22 * s, cy + 18 * s);
                break;
            }
            case FormaTema.EmblemaMecha:
            {
                using var esc = new GraphicsPath();
                Tramo(esc, P(cx - 30 * s, cy - 30 * s));
                Tramo(esc, P(cx + 30 * s, cy - 30 * s));
                Tramo(esc, P(cx + 30 * s, cy + 8 * s));
                esc.AddBezier(P(cx + 30 * s, cy + 8 * s), P(cx + 18 * s, cy + 30 * s), P(cx, cy + 38 * s), P(cx, cy + 38 * s));
                esc.AddBezier(P(cx, cy + 38 * s), P(cx, cy + 38 * s), P(cx - 18 * s, cy + 30 * s), P(cx - 30 * s, cy + 8 * s));
                Edges(esc, g, tinta, sombra, pluma);
                g.DrawLine(pluma, cx - 30 * s, cy - 30 * s, cx - 46 * s, cy - 44 * s);
                g.DrawLine(pluma, cx + 30 * s, cy - 30 * s, cx + 46 * s, cy - 44 * s);
                break;
            }
            case FormaTema.LlamaAzul:
            {
                using var ll = new GraphicsPath();
                ll.AddBezier(P(cx, cy - 38 * s), P(cx + 26 * s, cy - 8 * s), P(cx + 20 * s, cy + 22 * s), P(cx, cy + 36 * s));
                ll.AddBezier(P(cx, cy + 36 * s), P(cx - 20 * s, cy + 22 * s), P(cx - 26 * s, cy - 8 * s), P(cx, cy - 38 * s));
                Edges(ll, g, tinta, sombra, pluma);
                g.FillEllipse(relleno, cx - 8 * s, cy + 6 * s, 16 * s, 22 * s);
                break;
            }
            case FormaTema.Colmillo:
            {
                using var co = new GraphicsPath();
                co.AddBezier(P(cx - 22 * s, cy - 34 * s), P(cx + 6 * s, cy - 26 * s), P(cx + 14 * s, cy + 4 * s), P(cx - 2 * s, cy + 36 * s));
                co.AddBezier(P(cx - 2 * s, cy + 36 * s), P(cx - 6 * s, cy + 6 * s), P(cx - 20 * s, cy - 10 * s), P(cx - 22 * s, cy - 34 * s));
                Edges(co, g, tinta, sombra, pluma);
                break;
            }
            case FormaTema.Corona:
            {
                using var cr = new GraphicsPath();
                Tramo(cr, P(cx - 36 * s, cy + 26 * s));
                Tramo(cr, P(cx - 36 * s, cy - 14 * s));
                Tramo(cr, P(cx - 18 * s, cy + 4 * s));
                Tramo(cr, P(cx, cy - 30 * s));
                Tramo(cr, P(cx + 18 * s, cy + 4 * s));
                Tramo(cr, P(cx + 36 * s, cy - 14 * s));
                Tramo(cr, P(cx + 36 * s, cy + 26 * s));
                cr.CloseFigure();
                Edges(cr, g, tinta, sombra, pluma);
                break;
            }
            case FormaTema.Aullido:
            {
                using var lobo = new GraphicsPath();
                Tramo(lobo, P(cx - 30 * s, cy - 30 * s));
                Tramo(lobo, P(cx - 14 * s, cy - 8 * s));
                Tramo(lobo, P(cx - 22 * s, cy + 30 * s));
                Tramo(lobo, P(cx, cy + 12 * s));
                Tramo(lobo, P(cx + 22 * s, cy + 30 * s));
                Tramo(lobo, P(cx + 14 * s, cy - 8 * s));
                Tramo(lobo, P(cx + 30 * s, cy - 30 * s));
                lobo.CloseFigure();
                Edges(lobo, g, tinta, sombra, pluma);
                g.FillEllipse(relleno, cx - 12 * s, cy + 2 * s, 6 * s, 6 * s);
                g.FillEllipse(relleno, cx + 6 * s, cy + 2 * s, 6 * s, 6 * s);
                break;
            }
            case FormaTema.Escarlata:
            {
                using var espiral = new GraphicsPath();
                var pts = new List<PointF>();
                for (int i = 0; i <= 120; i++)
                {
                    var t = i / 120f;
                    var ang = t * (float)(Math.PI * 4);
                    var rad = 4 * s + t * 34 * s;
                    pts.Add(P(cx + (float)Math.Cos(ang) * rad, cy + (float)Math.Sin(ang) * rad));
                }
                espiral.AddCurve(pts.ToArray());
                g.DrawPath(pluma, espiral);
                break;
            }
            case FormaTema.Llamarada:
            {
                using var ll = new GraphicsPath();
                ll.AddBezier(P(cx, cy - 40 * s), P(cx + 22 * s, cy - 16 * s), P(cx + 32 * s, cy + 6 * s), P(cx + 18 * s, cy + 34 * s));
                ll.AddBezier(P(cx + 18 * s, cy + 34 * s), P(cx, cy + 22 * s), P(cx - 18 * s, cy + 34 * s), P(cx - 18 * s, cy + 34 * s));
                ll.AddBezier(P(cx - 18 * s, cy + 34 * s), P(cx - 32 * s, cy + 6 * s), P(cx - 22 * s, cy - 16 * s), P(cx, cy - 40 * s));
                Edges(ll, g, tinta, sombra, pluma);
                break;
            }
            case FormaTema.SonrisaFilo:
            {
                g.DrawArc(pluma, cx - 36 * s, cy - 24 * s, 72 * s, 60 * s, 180, 180);
                for (int i = -2; i <= 2; i++)
                {
                    var x = cx + i * 14 * s;
                    g.DrawLine(pluma, x, cy + 10 * s, x, cy + 24 * s - Math.Abs(i) * 4 * s);
                }
                break;
            }
            case FormaTema.Lampara:
            {
                g.FillEllipse(fondo, cx - 22 * s, cy - 6 * s, 44 * s, 40 * s);
                g.DrawEllipse(pluma, cx - 22 * s, cy - 6 * s, 44 * s, 40 * s);
                g.DrawArc(pluma, cx - 12 * s, cy + 30 * s, 24 * s, 12 * s, 180, 180);
                g.DrawLine(pluma, cx, cy - 6 * s, cx, cy - 24 * s);
                Estrella(g, cx, cy - 32 * s, 5, 10 * s, 4 * s, tinta);
                break;
            }
            case FormaTema.Engranaje:
            {
                var dientes = 10;
                using var eng = new GraphicsPath();
                for (int i = 0; i < dientes * 2; i++)
                {
                    var ang = i * Math.PI / dientes;
                    var rad = (i % 2 == 0) ? 38 * s : 28 * s;
                    var pt = P(cx + (float)Math.Cos(ang) * rad, cy + (float)Math.Sin(ang) * rad);
                    if (i == 0) Tramo(eng, pt); else Tramo(eng, pt);
                }
                eng.CloseFigure();
                Edges(eng, g, tinta, sombra, pluma);
                g.FillEllipse(fondo, cx - 12 * s, cy - 12 * s, 24 * s, 24 * s);
                break;
            }
            case FormaTema.Constelacion:
            {
                var puntos = new[] { P(cx - 30 * s, cy + 18 * s), P(cx - 12 * s, cy - 20 * s), P(cx + 6 * s, cy + 6 * s), P(cx + 28 * s, cy - 26 * s), P(cx + 22 * s, cy + 24 * s) };
                g.DrawLines(pluma, puntos);
                foreach (var p in puntos) g.FillEllipse(relleno, p.X - 5 * s, p.Y - 5 * s, 10 * s, 10 * s);
                break;
            }
            case FormaTema.Nube:
            {
                using var nube = new GraphicsPath();
                nube.AddArc(cx - 40 * s, cy - 6 * s, 34 * s, 34 * s, 90, 180);
                nube.AddArc(cx - 18 * s, cy - 26 * s, 40 * s, 40 * s, 180, 180);
                nube.AddArc(cx + 6 * s, cy - 8 * s, 36 * s, 36 * s, 270, 180);
                Tramo(nube, P(cx + 24 * s, cy + 26 * s));
                Tramo(nube, P(cx - 24 * s, cy + 26 * s));
                Edges(nube, g, tinta, sombra, pluma);
                break;
            }
            case FormaTema.Cuaderno:
            {
                using var cu = new GraphicsPath();
                Tramo(cu, P(cx - 30 * s, cy - 34 * s));
                Tramo(cu, P(cx + 24 * s, cy - 34 * s));
                Tramo(cu, P(cx + 24 * s, cy + 34 * s));
                Tramo(cu, P(cx - 30 * s, cy + 34 * s));
                cu.CloseFigure();
                Edges(cu, g, tinta, sombra, pluma);
                g.DrawLine(pluma, cx - 20 * s, cy - 18 * s, cx + 14 * s, cy - 18 * s);
                g.DrawLine(pluma, cx - 20 * s, cy - 2 * s, cx + 14 * s, cy - 2 * s);
                g.DrawLine(pluma, cx - 20 * s, cy + 14 * s, cx + 4 * s, cy + 14 * s);
                break;
            }
            case FormaTema.Espadas:
            {
                g.DrawLine(pluma, cx - 30 * s, cy - 32 * s, cx + 26 * s, cy + 30 * s);
                g.DrawLine(pluma, cx + 30 * s, cy - 32 * s, cx - 26 * s, cy + 30 * s);
                g.DrawLine(pluma, cx - 34 * s, cy + 30 * s, cx - 18 * s, cy + 22 * s);
                g.DrawLine(pluma, cx + 34 * s, cy + 30 * s, cx + 18 * s, cy + 22 * s);
                break;
            }
            case FormaTema.EscudoDaga:
            {
                using var esc = new GraphicsPath();
                Tramo(esc, P(cx - 26 * s, cy - 32 * s));
                Tramo(esc, P(cx + 26 * s, cy - 32 * s));
                Tramo(esc, P(cx + 26 * s, cy + 6 * s));
                Tramo(esc, P(cx, cy + 36 * s));
                Tramo(esc, P(cx - 26 * s, cy + 6 * s));
                esc.CloseFigure();
                Edges(esc, g, tinta, sombra, pluma);
                g.DrawLine(pluma, cx, cy - 20 * s, cx, cy + 22 * s);
                break;
            }
            case FormaTema.NueveColas:
            {
                for (int i = 0; i < 5; i++)
                {
                    var ang = -50 + i * 25;
                    var rad = ang * Math.PI / 180;
                    var ex = cx + (float)Math.Sin(rad) * 40 * s;
                    var ey = cy - (float)Math.Cos(rad) * 40 * s;
                    g.DrawBezier(pluma, P(cx, cy + 20 * s), P(cx + (float)Math.Sin(rad) * 10 * s, cy), P(ex * 0.7f + cx * 0.3f, ey * 0.7f + cy * 0.3f), P(ex, ey));
                }
                break;
            }
            case FormaTema.Magica:
            {
                g.DrawLine(pluma, cx - 26 * s, cy + 32 * s, cx + 10 * s, cy - 10 * s);
                Estrella(g, cx + 20 * s, cy - 22 * s, 5, 18 * s, 7 * s, tinta);
                break;
            }
            case FormaTema.AnclaFuego:
            {
                g.DrawLine(pluma, cx, cy - 24 * s, cx, cy + 30 * s);
                g.DrawLine(pluma, cx - 18 * s, cy - 6 * s, cx + 18 * s, cy - 6 * s);
                g.DrawArc(pluma, cx - 26 * s, cy + 2 * s, 52 * s, 32 * s, 20, 140);
                using var ll = new GraphicsPath();
                ll.AddBezier(P(cx - 6 * s, cy - 26 * s), P(cx + 8 * s, cy - 36 * s), P(cx + 12 * s, cy - 24 * s), P(cx + 4 * s, cy - 18 * s));
                g.DrawPath(pluma, ll);
                break;
            }
            case FormaTema.CoronaSombra:
            {
                g.FillEllipse(fondo, cx - 20 * s, cy - 6 * s, 40 * s, 40 * s);
                g.DrawEllipse(pluma, cx - 20 * s, cy - 6 * s, 40 * s, 40 * s);
                using var cr = new GraphicsPath();
                Tramo(cr, P(cx - 22 * s, cy - 8 * s));
                Tramo(cr, P(cx - 22 * s, cy - 30 * s));
                Tramo(cr, P(cx - 8 * s, cy - 16 * s));
                Tramo(cr, P(cx, cy - 36 * s));
                Tramo(cr, P(cx + 8 * s, cy - 16 * s));
                Tramo(cr, P(cx + 22 * s, cy - 30 * s));
                Tramo(cr, P(cx + 22 * s, cy - 8 * s));
                cr.CloseFigure();
                g.FillPath(relleno, cr);
                break;
            }
        }
    }

    private static PointF P(float x, float y) => new(x, y);

    /// <summary>Añade un tramo desde el ultimo punto de la ruta hasta `pt`.
    /// Si la ruta esta vacia, solo fija el punto de arranque.</summary>
    private static void Tramo(GraphicsPath ruta, PointF pt)
    {
        if (ruta.PointCount == 0)
        {
            ruta.AddLine(pt, pt);
            return;
        }
        var ultimo = ruta.PathPoints[^1];
        ruta.AddLine(ultimo, pt);
    }

    private static void Edges(GraphicsPath ruta, Graphics g, Color tinta, Color sombra, Pen pluma)
    {
        using var relleno = new SolidBrush(sombra);
        g.FillPath(relleno, ruta);
        g.DrawPath(pluma, ruta);
    }

    private static void Estrella(Graphics g, float cx, float cy, int puntas, float rExterior, float rInterior, Color color)
    {
        using var relleno = new SolidBrush(color);
        using var ruta = new GraphicsPath();
        for (int i = 0; i < puntas * 2; i++)
        {
            var ang = -Math.PI / 2 + i * Math.PI / puntas;
            var rad = (i % 2 == 0) ? rExterior : rInterior;
            var pt = P(cx + (float)Math.Cos(ang) * rad, cy + (float)Math.Sin(ang) * rad);
            if (i == 0) Tramo(ruta, pt); else Tramo(ruta, pt);
        }
        ruta.CloseFigure();
        g.FillPath(relleno, ruta);
    }
}
