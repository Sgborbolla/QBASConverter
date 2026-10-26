using System.Diagnostics;
using System.Text;

namespace QBASSuite.Media;

/// <summary>Donde se guarda el resultado de la conversion.</summary>
public enum SalidaModo
{
    /// <summary>En una subcarpeta elegible dentro de la carpeta del original: D:\Series\Cap 01.mkv -> D:\Series\Convertidos\Cap 01.mp4</summary>
    SubcarpetaJuntoAlOriginal = 0,
    /// <summary>En la misma carpeta del original: D:\Series\Cap 01.mkv -> D:\Series\Cap 01.mp4</summary>
    MismaCarpetaDelOriginal = 1,
    /// <summary>En una carpeta fija elegida por el usuario.</summary>
    CarpetaFija = 2
}

/// <summary>Que hacer si el fichero de salida ya existe.</summary>
public enum SalidaConflicto
{
    Saltar = 0,
    Sobrescribir = 1,
    Renombrar = 2
}

/// <summary>Opciones de una conversion pedida desde la interfaz.</summary>
public sealed class ConversionRequest
{
    public List<string> Archivos { get; set; } = new();

    /// <summary>Destino: mp4, mkv, avi, mov, webm, mp3, m4a, wav, flac, ogg, gif, jpg, png.</summary>
    public string Formato { get; set; } = "mp4";

    /// <summary>rapido (copia directa), equilibrado o alta.</summary>
    public string Modo { get; set; } = "equilibrado";

    /// <summary>Donde se guarda (subcarpeta junto al original, misma carpeta o carpeta fija).</summary>
    public SalidaModo Salida { get; set; } = SalidaModo.SubcarpetaJuntoAlOriginal;

    /// <summary>Nombre de la subcarpeta junto al original. Por defecto "Convertidos".</summary>
    public string NombreSubcarpeta { get; set; } = "Convertidos";

    /// <summary>Carpeta fija elegida por el usuario (solo si Salida = CarpetaFija).</summary>
    public string? CarpetaSalida { get; set; }

    /// <summary>Replicar la forma de las subcarpetas dentro de la carpeta de salida.</summary>
    public bool MantenerEstructura { get; set; }

    /// <summary>Subcarpeta que se pone delante de cada archivo, conservando la
    /// carpeta raiz tal como se arrastro (clave = ruta completa del archivo).
    /// Ej.: D:\Series\SerieX\T1\cap1.mkv -> "SerieX\T1".</summary>
    public Dictionary<string, string> Subcarpetas { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Si el resultado ya existe: saltar, sobrescribir o renombrar.</summary>
    public SalidaConflicto Conflicto { get; set; } = SalidaConflicto.Saltar;

    /// <summary>Borrar el original al terminar, pero SOLO si la conversion salio bien.</summary>
    public bool BorrarOriginalSiOk { get; set; }

    /// <summary>Prefijo del nombre del resultado. Vacio = mismo nombre que el original.</summary>
    public string Sufijo { get; set; } = "";

    /// <summary>true = usar GPU si existe (NVENC/QSV/AMF).</summary>
    public bool UsarGpu { get; set; } = true;
}

public sealed class ConversionResultado
{
    public bool Ok { get; set; }
    public int Hechos { get; set; }
    public int Fallidos { get; set; }
    public int Saltados { get; set; }
    public string? Error { get; set; }
    public string? Motor { get; set; }
    public List<string> Salidas { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

/// <summary>
/// Convierte video, audio e imagenes de verdad con ffmpeg.
/// Nada simulado: se lanza ffmpeg.exe (va junto al programa) y se espera.
/// Detecta aceleracion por GPU una sola vez y la reutiliza.
/// </summary>
public sealed class ConversionEngine
{
    private static string? _ffmpeg;
    private static string? _gpu;      // null = sin comprobar, "" = no hay, otro = encoder
    private static readonly object _candado = new();

    /// <summary>Extensiones que se aceptan como entrada (todo lo habitual de ffmpeg).</summary>
    public static readonly string[] Extensiones =
    {
        // video
        ".mp4",".mkv",".avi",".mov",".wmv",".flv",".webm",".m4v",".mpg",".mpeg",".ts",".m2ts",
        ".3gp",".ogv",".vob",".rm",".rmvb",".divx",".asf",".f4v",".mts",
        // audio
        ".mp3",".wav",".flac",".aac",".ogg",".m4a",".wma",".opus",".aiff",".alac",".ape",
        // imagen
        ".jpg",".jpeg",".png",".bmp",".webp",".tif",".tiff",".gif"
    };

    /// <summary>Formatos de salida ofrecidos en la ventana.</summary>
    public static readonly string[] Formatos =
    {
        "mp4","mkv","avi","mov","webm","wmv","flv","ts","mpg","3gp","ogv","gif",
        "mp3","m4a","aac","wav","flac","ogg","opus","wma",
        "jpg","png","webp","bmp"
    };

    public static bool EsSoportado(string ruta)
    {
        var e = Path.GetExtension(ruta).ToLowerInvariant();
        return Extensiones.Contains(e);
    }

    /// <summary>Ruta real de ffmpeg.exe: junto al ejecutable o en el repo.</summary>
    public static string? Ffmpeg
    {
        get
        {
            if (_ffmpeg != null && File.Exists(_ffmpeg)) return _ffmpeg;
            var candidatos = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe"),
                Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"),
                Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg", "ffmpeg.exe"),
            };
            foreach (var c in candidatos)
                if (File.Exists(c)) { _ffmpeg = c; return c; }

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var c = Path.Combine(dir.FullName, "tools", "ffmpeg", "ffmpeg.exe");
                if (File.Exists(c)) { _ffmpeg = c; return c; }
                dir = dir.Parent;
            }
            return null;
        }
    }

    public static bool Disponible => Ffmpeg != null;

    /// <summary>
    /// Detecta una sola vez si hay encoder por GPU. Devuelve "" si no hay.
    /// nvenc = NVIDIA, qsv = Intel, amf = AMD.
    /// </summary>
    public static string DetectarGpu()
    {
        lock (_candado)
        {
            if (_gpu != null) return _gpu;
            _gpu = "";
            var exe = Ffmpeg;
            if (exe == null) return _gpu;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe, Arguments = "-hide_banner -encoders",
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                using var p = Process.Start(psi)!;
                var salida = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit(8000);
                if (salida.Contains("h264_nvenc")) _gpu = "h264_nvenc";
                else if (salida.Contains("h264_qsv")) _gpu = "h264_qsv";
                else if (salida.Contains("h264_amf")) _gpu = "h264_amf";
            }
            catch { }
            return _gpu;
        }
    }

    public static string NombreGpu() => DetectarGpu() switch
    {
        "h264_nvenc" => "NVIDIA NVENC",
        "h264_qsv" => "Intel Quick Sync",
        "h264_amf" => "AMD AMF",
        _ => "CPU"
    };

    /// <summary>Sugiere cuantas conversiones a la vez puede aguantar esta maquina.</summary>
    public static int ProcesosSugeridos()
    {
        var nucleos = Math.Max(1, Environment.ProcessorCount);
        var gpu = DetectarGpu() != "";
        // Con GPU, el propio codificador limita cuantos procesos conviene lanzar.
        // Con CPU, dejamos un nucleo libre y metemos la mitad de los nucleos.
        var n = gpu ? Math.Max(2, Math.Min(4, nucleos / 2)) : Math.Max(1, nucleos / 2);
        return Math.Clamp(n, 1, 16);
    }

    /// <summary>Convierte una lista real. `aLaVez` = cuantas conversiones en paralelo.</summary>
    public async Task<ConversionResultado> ConvertirAsync(ConversionRequest req, CancellationToken ct,
        Action<string, int, int>? aviso = null, int aLaVez = 1)
    {
        var r = new ConversionResultado();
        var exe = Ffmpeg;
        if (exe == null) { r.Error = "No se encontró ffmpeg.exe"; return r; }

        r.Motor = req.UsarGpu && DetectarGpu() != "" ? NombreGpu() : "CPU";

        var total = req.Archivos.Count;
        var hecho = 0;
        var candado = new object();

        using var puerta = new SemaphoreSlim(Math.Max(1, Math.Min(aLaVez, 32)));
        var tareas = new List<Task>();

        foreach (var entrada in req.Archivos)
        {
            ct.ThrowIfCancellationRequested();
            await puerta.WaitAsync(ct).ConfigureAwait(false);

            tareas.Add(Task.Run(async () =>
            {
                try
                {
                    await ConvertirUnoAsync(entrada, req, exe, r, candado, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    lock (candado) { r.Fallidos++; r.Errores.Add($"{Path.GetFileName(entrada)}: {ex.Message}"); }
                }
                finally
                {
                    puerta.Release();
                    lock (candado) { hecho++; aviso?.Invoke(entrada, hecho, total); }
                }
            }, ct));
        }

        try { await Task.WhenAll(tareas).ConfigureAwait(false); }
        catch (OperationCanceledException) { r.Error = "Cancelado"; throw; }

        r.Ok = r.Fallidos == 0 && r.Hechos > 0;
        return r;
    }

    private static async Task ConvertirUnoAsync(string entrada, ConversionRequest req, string exe,
        ConversionResultado r, object candado, CancellationToken ct)
    {
        if (!File.Exists(entrada))
        {
            lock (candado) { r.Fallidos++; r.Errores.Add($"No existe: {entrada}"); }
            return;
        }
        if (!EsSoportado(entrada))
        {
            lock (candado) { r.Fallidos++; r.Errores.Add($"Extensión no soportada: {entrada}"); }
            return;
        }

        var (salida, saltar) = RutaSalida(entrada, req);
        if (saltar)
        {
            lock (candado) { r.Saltados++; }
            return;
        }

        var args = Argumentos(entrada, salida, req);
        var (ok, log) = await EjecutarAsync(exe, args, ct).ConfigureAwait(false);
        if (ok)
        {
            lock (candado) { r.Hechos++; r.Salidas.Add(salida); }
            // Borrar el original SOLO si salio bien y el usuario lo pidio.
            if (req.BorrarOriginalSiOk && File.Exists(salida))
            {
                try { File.Delete(entrada); }
                catch { /* si no se deja, no se fuerza */ }
            }
        }
        else
        {
            lock (candado) { r.Fallidos++; r.Errores.Add($"{Path.GetFileName(entrada)}: {UltimaLinea(log)}"); }
        }
    }

    /// <summary>
    /// Calcula donde va el resultado y si hay que saltarlo. Devuelve
    /// (rutaSalida, saltar). Aplica las opciones de guardado elegidas.
    /// </summary>
    public static (string Salida, bool Saltar) RutaSalida(string entrada, ConversionRequest req)
    {
        var carpetaOriginal = Path.GetDirectoryName(entrada) ?? ".";
        string dir;

        switch (req.Salida)
        {
            case SalidaModo.MismaCarpetaDelOriginal:
                dir = carpetaOriginal;
                break;

            case SalidaModo.CarpetaFija:
                dir = !string.IsNullOrWhiteSpace(req.CarpetaSalida)
                    ? req.CarpetaSalida!
                    : carpetaOriginal;
                break;

            default: // SubcarpetaJuntoAlOriginal
                var sub = string.IsNullOrWhiteSpace(req.NombreSubcarpeta) ? "Convertidos" : req.NombreSubcarpeta.Trim();
                dir = Path.Combine(carpetaOriginal, sub);
                break;
        }

        // Replicar la forma de las subcarpetas. Solo hace falta cuando todo va a
        // UNA carpeta fija: al guardar junto al original (o en una subcarpeta suya)
        // la estructura ya se conserva sola y repetirla anidaria SerieX\T1 dos veces.
        if (req.MantenerEstructura && req.Salida == SalidaModo.CarpetaFija)
        {
            // 1) si el archivo vino de algo arrastrado, se conserva EXACTAMENTE la
            //    carpeta raiz que se solto (SerieX\T1\...), no la ruta del disco.
            if (req.Subcarpetas.TryGetValue(entrada, out var relativa)
                && !string.IsNullOrWhiteSpace(relativa))
            {
                dir = Path.Combine(dir, relativa);
            }
            else
            {
                var raiz = Path.GetPathRoot(entrada) ?? "";
                var rel = Path.GetRelativePath(raiz, carpetaOriginal);
                if (!rel.StartsWith("..") && rel != ".")
                {
                    // quita la primera carpeta (la del disco) para no anidar de mas
                    var partes = rel.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                    if (partes.Length > 1)
                        dir = Path.Combine(dir, Path.Combine(partes.Skip(1).ToArray()));
                }
            }
        }

        try { Directory.CreateDirectory(dir); } catch { }

        var baseNombre = Path.GetFileNameWithoutExtension(entrada) + req.Sufijo;
        var destino = Path.Combine(dir, $"{baseNombre}.{req.Formato}");

        // Nunca pisar el original si coincide nombre y extension.
        if (string.Equals(destino, entrada, StringComparison.OrdinalIgnoreCase))
            destino = Path.Combine(dir, $"{baseNombre}_1.{req.Formato}");

        if (File.Exists(destino))
        {
            switch (req.Conflicto)
            {
                case SalidaConflicto.Saltar:
                    return (destino, true);

                case SalidaConflicto.Renombrar:
                    destino = Path.Combine(dir, $"{baseNombre}_{SiguienteNumero(dir, baseNombre, req.Formato)}.{req.Formato}");
                    break;

                case SalidaConflicto.Sobrescribir:
                default:
                    break;
            }
        }

        return (destino, false);
    }

    private static int SiguienteNumero(string dir, string baseNombre, string ext)
    {
        for (int i = 1; i < 10000; i++)
        {
            var c = Path.Combine(dir, $"{baseNombre}_{i}.{ext}");
            if (!File.Exists(c)) return i;
        }
        return 1;
    }

    private static string Argumentos(string entrada, string salida, ConversionRequest req)
    {
        var fmt = req.Formato.ToLowerInvariant();
        bool rapido = req.Modo == "rapido";
        bool alta = req.Modo == "alta";

        // --- destino de imagen ---
        if (fmt is "jpg" or "jpeg" or "png" or "webp" or "bmp")
            return $"-y -i \"{entrada}\" -frames:v 1 \"{salida}\"";

        // --- destino de solo audio ---
        if (fmt is "mp3" or "m4a" or "wav" or "flac" or "ogg" or "opus" or "aac" or "wma")
        {
            var codec = fmt switch
            {
                "mp3" => "-c:a libmp3lame -b:a " + (alta ? "320k" : rapido ? "128k" : "192k"),
                "m4a" => "-c:a aac -b:a " + (alta ? "256k" : "192k"),
                "aac" => "-c:a aac -b:a " + (alta ? "256k" : "192k"),
                "wav" => "-c:a pcm_s16le",
                "flac" => "-c:a flac",
                "ogg" => "-c:a libvorbis -q:a " + (alta ? "8" : "5"),
                "opus" => "-c:a libopus -b:a 160k",
                "wma" => "-c:a wmav2 -b:a " + (alta ? "192k" : "128k"),
                _ => "-c:a aac -b:a 192k",
            };
            return $"-y -i \"{entrada}\" -vn {codec} \"{salida}\"";
        }

        // --- destino de vídeo ---
        // Modo rápido: copia directa (instantáneo). Solo válido si contenedor y códecs encajan.
        if (rapido && fmt is "mp4" or "mov" or "mkv" or "ts")
            return $"-y -i \"{entrada}\" -c copy -movflags +faststart \"{salida}\"";

        var crf = alta ? "18" : "23";
        var preset = alta ? "slow" : "medium";
        var audio = alta ? "192k" : "128k";

        if (fmt == "webm")
            return $"-y -i \"{entrada}\" -c:v libvpx-vp9 -crf {(alta ? "20" : "30")} -b:v 0 -c:a libopus \"{salida}\"";

        if (fmt == "avi")
            return $"-y -i \"{entrada}\" -c:v mpeg4 -q:v {(alta ? "2" : "4")} -c:a mp3 \"{salida}\"";

        if (fmt == "gif")
            return $"-y -i \"{entrada}\" -vf \"fps=12,scale=480:-1:flags=lanczos\" \"{salida}\"";

        if (fmt == "wmv")
            return $"-y -i \"{entrada}\" -c:v wmv2 -b:v {(alta ? "6M" : "3M")} -c:a wmav2 -b:a {audio} \"{salida}\"";

        if (fmt == "flv")
            return $"-y -i \"{entrada}\" -c:v flv -b:v {(alta ? "6M" : "3M")} -c:a libmp3lame -b:a {audio} \"{salida}\"";

        if (fmt == "mpg")
            return $"-y -i \"{entrada}\" -c:v mpeg2video -q:v {(alta ? "3" : "6")} -c:a mp2 -b:a {audio} \"{salida}\"";

        if (fmt == "3gp")
            return $"-y -i \"{entrada}\" -c:v libx264 -crf {crf} -preset {preset} -c:a aac -b:a 96k -ar 44100 \"{salida}\"";

        if (fmt == "ogv")
            return $"-y -i \"{entrada}\" -c:v libtheora -q:v {(alta ? "8" : "5")} -c:a libvorbis -q:a 5 \"{salida}\"";

        // mp4 / mov / mkv / ts con H.264: GPU si existe.
        var vcodec = "-c:v libx264";
        if (req.UsarGpu)
        {
            var gpu = DetectarGpu();
            if (gpu != "")
            {
                vcodec = gpu switch
                {
                    "h264_nvenc" => $"-c:v h264_nvenc -preset p5 -cq {crf} -b:v 0",
                    "h264_qsv" => $"-c:v h264_qsv -global_quality {crf}",
                    "h264_amf" => $"-c:v h264_amf -quality {(alta ? "quality" : "balanced")} -qp_i {crf} -qp_p {crf}",
                    _ => "-c:v libx264"
                };
            }
        }
        var tune = vcodec.Contains("libx264") ? $"-preset {preset} -crf {crf}" : "";
        var fast = fmt is "mp4" or "mov" ? "-movflags +faststart" : "";
        return $"-y -i \"{entrada}\" {vcodec} {tune} -c:a aac -b:a {audio} {fast} \"{salida}\"";
    }

    private static async Task<(bool ok, string log)> EjecutarAsync(string exe, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe, Arguments = args,
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardError = true, RedirectStandardOutput = true
        };
        using var p = new Process { StartInfo = psi };
        var sb = new StringBuilder();
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) { lock (sb) sb.AppendLine(e.Data); } };
        if (!p.Start()) return (false, "");
        p.BeginErrorReadLine();
        await p.WaitForExitAsync(ct);
        return (p.ExitCode == 0, sb.ToString());
    }

    private static string UltimaLinea(string log)
    {
        var lineas = log.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lineas.Length == 0 ? "error desconocido" : lineas[^1].Trim();
    }
}
