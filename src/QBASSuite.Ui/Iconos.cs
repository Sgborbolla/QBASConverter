using System.Drawing.Drawing2D;
using QBASSuite.Devices;

namespace QBASSuite.Ui;

/// <summary>
/// Iconos PROPIOS de QBASFreemanCopy-Converter. No se copia ni un solo icono de ningun otro
/// programa: todos se dibujan aqui con trazados, en codigo.
///
/// Ventajas de hacerlo asi:
///   - No hay dependencias ni ficheros sueltos: todo va dentro del .exe.
///   - No hay licencias de nadie.
///   - Escalan a cualquier tamano sin pixelarse (son vectoriales).
///   - Cogen el COLOR DEL TEMA: cambias de tema y cambian contigo.
///
/// El estilo es PLANO MODERNO: silueta limpia, esquinas suaves y un tono de
/// sombra para dar volumen. A tamanos pequenos (16/20/24 px) se lee de un golpe.
/// </summary>
public enum Icono
{
    Ninguno,

    // --- acciones generales ---
    Convertir, Pausa, Seguir, Detener, Refrescar, AnadirCarpeta, AnadirArchivo,
    Quitar, Papelera, AbrirCarpeta, Guardar, Ajustes, Lupa, Prioridad, Editar, Dinero,

    // --- dispositivos ---
    DiscoDuro, Ssd, Usb, Pendrive, DiscoLento, Optico, Telefono, Camara, TarjetaSd, Reproductor,

    // --- red ---
    Red, CarpetaCompartida, Servidor, Nube, Wifi,

    // --- sistema ---
    Sistema, Interno, Externo,

    // --- estados ---
    EnCola, Copiando, Pausado, Hecho, Error, Cancelado,

    // --- ficheros y medios ---
    Carpeta, Video, Audio, Imagen, Subtitulo, Documento, Desconocido
}

/// <summary>Dibuja los iconos propios. Todo vectorial, todo con el color del tema.</summary>
public static class Iconos
{
    /// <summary>Pinta un icono ya escalado dentro de una caja cuadrada.</summary>
    public static void Dibujar(Graphics g, Icono icono, Rectangle caja, Color tinta, Color sombra)
    {
        if (icono == Icono.Ninguno || caja.Width <= 0 || caja.Height <= 0) return;

        var estado = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        // Trazamos siempre en un lienzo de 32x32 y luego lo encajamos en la caja.
        var lado = Math.Min(caja.Width, caja.Height);
        var ox = caja.X + (caja.Width - lado) / 2f;
        var oy = caja.Y + (caja.Height - lado) / 2f;
        g.TranslateTransform(ox, oy);
        g.ScaleTransform(lado / 32f, lado / 32f);

        using var pluma = new Pen(tinta, 2.4f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var plumaFina = new Pen(tinta, 1.6f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var relleno = new SolidBrush(tinta);
        using var tenue = new SolidBrush(sombra);

        switch (icono)
        {
            case Icono.Convertir:   Triangulo(g, relleno); break;
            case Icono.Pausa:       DosBarras(g, relleno); break;
            case Icono.Seguir:      Triangulo(g, relleno); break;
            case Icono.Detener:     Cruz(g, pluma); break;
            case Icono.Refrescar:   FlechaCircular(g, pluma, tinta); break;
            case Icono.AnadirCarpeta: Carpeta(g, pluma, tenue); Mas(g, tinta, 20, 20); break;
            case Icono.AnadirArchivo: Hoja(g, pluma, tenue); Mas(g, tinta, 20, 20); break;
            case Icono.Quitar:      Cruz(g, pluma); break;
            case Icono.Papelera:    Papelera(g, pluma, tinta); break;
            case Icono.AbrirCarpeta: Carpeta(g, pluma, tenue); break;
            case Icono.Guardar:     Disquete(g, pluma, tenue); break;
            case Icono.Ajustes:     Engranaje(g, pluma, tinta); break;
            case Icono.Lupa:        Lupa(g, pluma); break;
            case Icono.Prioridad:   Estrella(g, relleno); break;
            case Icono.Editar:      Lapiz(g, pluma, tinta); break;
            case Icono.Dinero:      Moneda(g, pluma, tinta); break;

            case Icono.DiscoDuro:   Disco(g, pluma, tenue, tinta, sistema: false); break;
            case Icono.Sistema:     Disco(g, pluma, tenue, tinta, sistema: true); break;
            case Icono.Interno:     Disco(g, pluma, tenue, tinta, sistema: false); break;
            case Icono.Externo:     Disco(g, pluma, tenue, tinta, sistema: false); break;
            case Icono.Ssd:         Ssd(g, pluma, tenue, tinta); break;
            case Icono.Usb:         Usb(g, pluma, tenue, tinta); break;
            case Icono.Pendrive:    Usb(g, pluma, tenue, tinta); break;
            case Icono.DiscoLento:  Disco(g, pluma, tenue, tinta, sistema: false); Rayo(g, tinta); break;
            case Icono.Optico:      Optico(g, pluma, tenue, tinta); break;
            case Icono.Telefono:    Telefono(g, pluma, tenue, tinta); break;
            case Icono.Camara:      Camara(g, pluma, tenue, tinta); break;
            case Icono.TarjetaSd:   TarjetaSd(g, pluma, tenue, tinta); break;
            case Icono.Reproductor: Reproductor(g, pluma, tenue, tinta); break;

            case Icono.Red:             Red(g, pluma, tinta); break;
            case Icono.CarpetaCompartida: Carpeta(g, pluma, tenue); Red(g, plumaFina, tinta); break;
            case Icono.Servidor:        Servidor(g, pluma, tenue, tinta); break;
            case Icono.Nube:            Nube(g, pluma, tinta); break;
            case Icono.Wifi:            Wifi(g, pluma, tinta); break;

            case Icono.EnCola:     Reloj(g, pluma, tenue); break;
            case Icono.Copiando:   FlechaDoble(g, pluma, tinta); break;
            case Icono.Pausado:    DosBarras(g, relleno); break;
            case Icono.Hecho:      Check(g, pluma); break;
            case Icono.Error:      Cruz(g, pluma); break;
            case Icono.Cancelado:  CirculoProhibido(g, pluma); break;

            case Icono.Carpeta:    Carpeta(g, pluma, tenue); break;
            case Icono.Video:      Hoja(g, pluma, tenue); Triangulo(g, relleno, 22); break;
            case Icono.Audio:      Nota(g, pluma, tinta); break;
            case Icono.Imagen:     Hoja(g, pluma, tenue); Montania(g, pluma); break;
            case Icono.Subtitulo:  Subtitulo(g, pluma, tenue, tinta); break;
            case Icono.Documento:  Hoja(g, pluma, tenue); break;
            case Icono.Desconocido: Hoja(g, pluma, tenue); break;
        }

        g.Restore(estado);
    }

    /// <summary>Devuelve el icono que corresponde a un dispositivo REAL detectado.</summary>
    public static Icono ParaDispositivo(DeviceInfo d)
    {
        if (d.IsSystem) return Icono.Sistema;

        return d.Kind switch
        {
            DeviceKind.SystemDisk => Icono.Sistema,
            DeviceKind.Internal => d.IsSsd == true ? Icono.Ssd : Icono.Interno,
            DeviceKind.UsbExternal or DeviceKind.UsbExternalSlow => d.IsSsd == true ? Icono.Externo : Icono.Usb,
            DeviceKind.UsbFlash => Icono.Pendrive,
            DeviceKind.UsbFlashSlow => Icono.Pendrive,
            DeviceKind.NetworkShare => Icono.Red,
            DeviceKind.Optical => Icono.Optico,
            DeviceKind.Removable => Icono.Usb,
            _ => Icono.DiscoDuro
        };
    }

    /// <summary>Icono segun el tipo de dispositivo portatil (MTP).</summary>
    public static Icono ParaPortatil(PortableDevice p)
    {
        var t = (p.Tipo ?? "").ToLowerInvariant();
        if (t.Contains("camar") || t.Contains("cámara")) return Icono.Camara;
        if (t.Contains("reproduct") || t.Contains("player") || t.Contains("mp3")) return Icono.Reproductor;
        if (t.Contains("memoria") || t.Contains("tarjeta") || t.Contains("sd")) return Icono.TarjetaSd;
        return Icono.Telefono;
    }

    /// <summary>Icono segun la extension de un fichero.</summary>
    public static Icono ParaFichero(string ruta)
    {
        var ext = Path.GetExtension(ruta).ToLowerInvariant();
        return ext switch
        {
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm" or ".m4v"
                or ".mpg" or ".mpeg" or ".ts" or ".m2ts" or ".3gp" or ".ogv" or ".rmvb" or ".asf"
                or ".vob" or ".divx" or ".f4v" => Icono.Video,
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".m4a" or ".wma" or ".opus"
                or ".aiff" or ".ape" or ".alac" or ".mid" => Icono.Audio,
            ".jpg" or ".jpeg" or ".png" or ".bmp" or ".webp" or ".tif" or ".tiff" or ".gif"
                or ".heic" or ".avif" or ".raw" => Icono.Imagen,
            ".srt" or ".ass" or ".ssa" or ".sub" or ".vtt" or ".idx" => Icono.Subtitulo,
            ".txt" or ".doc" or ".docx" or ".pdf" or ".rtf" or ".odt" => Icono.Documento,
            _ => Icono.Desconocido
        };
    }

    /// <summary>Dibuja el icono en un Bitmap (para menus, botones y listas).</summary>
    public static Bitmap Bitmap(Icono icono, int lado, Color tinta, Color sombra)
    {
        var bmp = new Bitmap(lado, lado);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            Dibujar(g, icono, new Rectangle(0, 0, lado, lado), tinta, sombra);
        }
        return bmp;
    }

    /// <summary>Pinta un boton cuadrado solo con el icono (sin texto).</summary>
    public static Bitmap BitmapBoton(Icono icono, int lado, Color tinta, Color sombra)
        => Bitmap(icono, lado, tinta, sombra);

    // =====================================================================
    //  Trazados (todo dibujado a mano, en un lienzo de 32x32)
    // =====================================================================

    private static void Triangulo(Graphics g, Brush b, float inset = 0)
    {
        var p = new[]
        {
            new PointF(11 + inset / 2, 7 + inset),
            new PointF(25 - inset, 16),
            new PointF(11 + inset / 2, 25 - inset)
        };
        g.FillPolygon(b, p);
    }

    private static void DosBarras(Graphics g, Brush b)
    {
        g.FillRectangle(b, 10, 7, 4.5f, 18);
        g.FillRectangle(b, 17.5f, 7, 4.5f, 18);
    }

    private static void Cruz(Graphics g, Pen p)
    {
        g.DrawLine(p, 9, 9, 23, 23);
        g.DrawLine(p, 23, 9, 9, 23);
    }

    private static void Check(Graphics g, Pen p)
    {
        g.DrawLines(p, new[] { new PointF(7, 17), new PointF(13, 23), new PointF(25, 9) });
    }

    private static void CirculoProhibido(Graphics g, Pen p)
    {
        g.DrawEllipse(p, 6, 6, 20, 20);
        g.DrawLine(p, 11, 11, 21, 21);
    }

    private static void Rayo(Graphics g, Color tinta)
    {
        using var b = new SolidBrush(tinta);
        var p = new[]
        {
            new PointF(18, 3), new PointF(26, 3), new PointF(20, 13),
            new PointF(27, 13), new PointF(14, 29), new PointF(18, 16), new PointF(12, 16)
        };
        g.FillPolygon(b, p);
    }

    private static void Mas(Graphics g, Color tinta, float x, float y)
    {
        using var p = new Pen(tinta, 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(p, x - 4, y, x + 4, y);
        g.DrawLine(p, x, y - 4, x, y + 4);
    }

    private static void Carpeta(Graphics g, Pen p, Brush tenue)
    {
        var pts = new[]
        {
            new PointF(4, 10), new PointF(13, 10), new PointF(15, 13),
            new PointF(28, 13), new PointF(28, 26), new PointF(4, 26)
        };
        using var path = new GraphicsPath();
        path.AddPolygon(pts);
        g.FillPath(tenue, path);
        g.DrawPath(p, path);
    }

    private static void Hoja(Graphics g, Pen p, Brush tenue)
    {
        var pts = new[]
        {
            new PointF(7, 4), new PointF(20, 4), new PointF(26, 10),
            new PointF(26, 28), new PointF(7, 28)
        };
        using var path = new GraphicsPath();
        path.AddPolygon(pts);
        g.FillPath(tenue, path);
        g.DrawPath(p, path);
        g.DrawLine(p, 20, 4, 20, 10);
        g.DrawLine(p, 20, 10, 26, 10);
    }

    private static void Disquete(Graphics g, Pen p, Brush tenue)
    {
        var r = new Rectangle(5, 5, 22, 22);
        using var path = Redondo(r, 3);
        g.FillPath(tenue, path);
        g.DrawPath(p, path);
        g.FillRectangle(Brushes.Transparent, 10, 5, 12, 8);
        g.DrawRectangle(p, 10, 5, 12, 8);
        g.DrawRectangle(p, 10, 18, 12, 9);
    }

    private static void Papelera(Graphics g, Pen p, Color tinta)
    {
        g.DrawLine(p, 6, 9, 26, 9);
        g.DrawLine(p, 13, 9, 13, 6);
        g.DrawLine(p, 13, 6, 19, 6);
        g.DrawLine(p, 19, 6, 19, 9);
        g.DrawLines(p, new[] { new PointF(8, 9), new PointF(10, 27), new PointF(22, 27), new PointF(24, 9) });
        using var f = new Pen(tinta, 1.4f);
        g.DrawLine(f, 13, 13, 13, 23);
        g.DrawLine(f, 16, 13, 16, 23);
        g.DrawLine(f, 19, 13, 19, 23);
    }

    private static void Engranaje(Graphics g, Pen p, Color tinta)
    {
        g.DrawEllipse(p, 11, 11, 10, 10);
        using var b = new SolidBrush(tinta);
        for (int i = 0; i < 8; i++)
        {
            var a = i * MathF.PI / 4f;
            var cx = 16 + MathF.Cos(a) * 11f;
            var cy = 16 + MathF.Sin(a) * 11f;
            g.FillRectangle(b, cx - 1.6f, cy - 1.6f, 3.2f, 3.2f);
        }
    }

    private static void Lupa(Graphics g, Pen p)
    {
        g.DrawEllipse(p, 6, 6, 14, 14);
        g.DrawLine(p, 19, 19, 26, 26);
    }

    private static void Moneda(Graphics g, Pen p, Color tinta)
    {
        g.DrawEllipse(p, 4.5f, 4.5f, 23f, 23f);
        using var fuente = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brocha = new SolidBrush(tinta);
        var medida = g.MeasureString("$", fuente);
        g.DrawString("$", fuente, brocha, 16f - medida.Width / 2f, 16f - medida.Height / 2f + 0.5f);
    }

    private static void Estrella(Graphics g, Brush b)
    {
        var pts = new List<PointF>();
        for (int i = 0; i < 10; i++)
        {
            var a = -MathF.PI / 2 + i * MathF.PI / 5;
            var r = i % 2 == 0 ? 12f : 5.2f;
            pts.Add(new PointF(16 + MathF.Cos(a) * r, 16 + MathF.Sin(a) * r));
        }
        g.FillPolygon(b, pts.ToArray());
    }

    private static void Lapiz(Graphics g, Pen p, Color tinta)
    {
        using var b = new SolidBrush(tinta);
        var cuerpo = new[]
        {
            new PointF(9, 23), new PointF(22, 10), new PointF(25, 13), new PointF(12, 26)
        };
        g.FillPolygon(b, cuerpo);
        g.DrawPolygon(p, new[] { new PointF(7, 28), new PointF(9, 23), new PointF(12, 26) });
        g.DrawLine(p, 22, 10, 25, 13);
    }

    private static void FlechaCircular(Graphics g, Pen p, Color tinta)
    {
        g.DrawArc(p, 7, 7, 18, 18, 40, 280);
        using var b = new SolidBrush(tinta);
        g.FillPolygon(b, new[]
        {
            new PointF(23, 5), new PointF(28, 11), new PointF(21, 12)
        });
    }

    private static void FlechaDoble(Graphics g, Pen p, Color tinta)
    {
        g.DrawLine(p, 8, 13, 24, 13);
        g.DrawLine(p, 8, 19, 24, 19);
        using var b = new SolidBrush(tinta);
        g.FillPolygon(b, new[] { new PointF(5, 13), new PointF(10, 10), new PointF(10, 16) });
        g.FillPolygon(b, new[] { new PointF(27, 19), new PointF(22, 16), new PointF(22, 22) });
    }

    private static void Reloj(Graphics g, Pen p, Brush tenue)
    {
        g.FillEllipse(tenue, 6, 6, 20, 20);
        g.DrawEllipse(p, 6, 6, 20, 20);
        g.DrawLine(p, 16, 16, 16, 10);
        g.DrawLine(p, 16, 16, 21, 18);
    }

    private static void Disco(Graphics g, Pen p, Brush tenue, Color tinta, bool sistema)
    {
        var r = new Rectangle(5, 8, 22, 16);
        using var path = Redondo(r, 4);
        g.FillPath(tenue, path);
        g.DrawPath(p, path);
        g.DrawEllipse(p, 10, 13, 6, 6);
        using var b = new SolidBrush(tinta);
        g.FillEllipse(b, 22, 12, 2.4f, 2.4f);
        if (sistema)
        {
            // cuatro cuadraditos (escudo del sistema), dibujo propio
            g.DrawRectangle(p, 12, 20, 8, 8);
            g.DrawLine(p, 16, 20, 16, 28);
            g.DrawLine(p, 12, 24, 20, 24);
        }
    }

    private static void Ssd(Graphics g, Pen p, Brush tenue, Color tinta)
    {
        var r = new Rectangle(5, 9, 22, 14);
        using var path = Redondo(r, 3);
        g.FillPath(tenue, path);
        g.DrawPath(p, path);
        using var b = new SolidBrush(tinta);
        for (int i = 0; i < 4; i++) g.FillRectangle(b, 8 + i * 4.5f, 13, 2.6f, 6);
        g.DrawLine(p, 8, 22, 24, 22);
    }

    private static void Usb(Graphics g, Pen p, Brush tenue, Color tinta)
    {
        var r = new Rectangle(11, 9, 10, 18);
        using var path = Redondo(r, 2);
        g.FillPath(tenue, path);
        g.DrawPath(p, path);
        using var b = new SolidBrush(tinta);
        g.FillRectangle(b, 13, 3, 6, 7);
        g.FillRectangle(b, 13, 6, 1.6f, 3);
    }

    private static void Optico(Graphics g, Pen p, Brush tenue, Color tinta)
    {
        g.FillEllipse(tenue, 4, 4, 24, 24);
        g.DrawEllipse(p, 4, 4, 24, 24);
        g.DrawEllipse(p, 12, 12, 8, 8);
        using var b = new SolidBrush(tinta);
        g.FillEllipse(b, 14.4f, 14.4f, 3.2f, 3.2f);
    }

    private static void Telefono(Graphics g, Pen p, Brush tenue, Color tinta)
    {
        var r = new Rectangle(10, 3, 12, 26);
        using var path = Redondo(r, 3);
        g.FillPath(tenue, path);
        g.DrawPath(p, path);
        using var b = new SolidBrush(tinta);
        g.FillRectangle(b, 13, 7, 6, 1.8f);
        g.FillEllipse(b, 14.6f, 24, 2.8f, 2.8f);
    }

    private static void Camara(Graphics g, Pen p, Brush tenue, Color tinta)
    {
        var r = new Rectangle(4, 9, 24, 16);
        using var path = Redondo(r, 3);
        g.FillPath(tenue, path);
        g.DrawPath(p, path);
        g.DrawRectangle(p, 9, 6, 7, 4);
        g.DrawEllipse(p, 11, 12, 10, 10);
        using var b = new SolidBrush(tinta);
        g.FillEllipse(b, 14, 15, 4, 4);
    }

    private static void TarjetaSd(Graphics g, Pen p, Brush tenue, Color tinta)
    {
        var pts = new[]
        {
            new PointF(8, 4), new PointF(20, 4), new PointF(25, 10),
            new PointF(25, 28), new PointF(8, 28)
        };
        using var path = new GraphicsPath();
        path.AddPolygon(pts);
        g.FillPath(tenue, path);
        g.DrawPath(p, path);
        using var b = new SolidBrush(tinta);
        for (int i = 0; i < 3; i++) g.FillRectangle(b, 11 + i * 4, 8, 2.2f, 5);
    }

    private static void Reproductor(Graphics g, Pen p, Brush tenue, Color tinta)
    {
        var r = new Rectangle(8, 3, 16, 26);
        using var path = Redondo(r, 4);
        g.FillPath(tenue, path);
        g.DrawPath(p, path);
        using var b = new SolidBrush(tinta);
        g.FillEllipse(b, 12, 8, 8, 8);
        g.FillEllipse(b, 15, 24, 3, 3);
    }

    private static void Red(Graphics g, Pen p, Color tinta)
    {
        g.DrawLine(p, 16, 9, 16, 24);
        g.DrawLine(p, 7, 24, 25, 24);
        using var b = new SolidBrush(tinta);
        g.FillRectangle(b, 11, 3, 10, 7);
        g.FillRectangle(b, 3, 21, 8, 6);
        g.FillRectangle(b, 21, 21, 8, 6);
    }

    private static void Servidor(Graphics g, Pen p, Brush tenue, Color tinta)
    {
        using var b = new SolidBrush(tinta);
        for (int i = 0; i < 3; i++)
        {
            var r = new Rectangle(6, 5 + i * 8, 20, 6);
            using var path = Redondo(r, 2);
            g.FillPath(tenue, path);
            g.DrawPath(p, path);
            g.FillEllipse(b, 9, 7 + i * 8, 2, 2);
        }
    }

    private static void Nube(Graphics g, Pen p, Color tinta)
    {
        using var path = new GraphicsPath();
        path.AddArc(4, 14, 10, 10, 150, 200);
        path.AddArc(9, 8, 12, 12, 180, 200);
        path.AddArc(18, 11, 11, 11, 250, 200);
        path.CloseFigure();
        using var b = new SolidBrush(Color.FromArgb(70, tinta));
        g.FillPath(b, path);
        g.DrawPath(p, path);
    }

    private static void Wifi(Graphics g, Pen p, Color tinta)
    {
        for (int i = 0; i < 3; i++)
        {
            var r = new Rectangle(5 + i * 4, 8 + i * 6, 22 - i * 8, 20 - i * 8);
            g.DrawArc(p, r, 200, 140);
        }
        using var b = new SolidBrush(tinta);
        g.FillEllipse(b, 14, 24, 4, 4);
    }

    private static void Nota(Graphics g, Pen p, Color tinta)
    {
        using var b = new SolidBrush(tinta);
        g.FillRectangle(b, 20, 5, 2.4f, 16);
        g.FillEllipse(b, 13, 19, 9, 7);
        using var path = new GraphicsPath();
        path.AddBezier(new PointF(22.4f, 5), new PointF(28, 7), new PointF(28, 12), new PointF(23, 13));
        g.DrawPath(p, path);
    }

    private static void Montania(Graphics g, Pen p)
    {
        g.DrawLines(p, new[] { new PointF(10, 24), new PointF(15, 17), new PointF(19, 22), new PointF(23, 18) });
    }

    private static void Subtitulo(Graphics g, Pen p, Brush tenue, Color tinta)
    {
        var r = new Rectangle(4, 9, 24, 14);
        using var path = Redondo(r, 3);
        g.FillPath(tenue, path);
        g.DrawPath(p, path);
        using var b = new SolidBrush(tinta);
        for (int i = 0; i < 3; i++) g.FillRectangle(b, 8, 13 + i * 3.4f, 16, 1.6f);
    }

    private static GraphicsPath Redondo(Rectangle r, int radio)
    {
        var path = new GraphicsPath();
        var d = radio * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
