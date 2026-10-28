using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using QBASSuite.Engine;
using QBASSuite.Util;

namespace QBASSuite.Ui;

/// <summary>Las cuatro formas de cobrar el trabajo del multicopiador.</summary>
public enum ModoCobro
{
    /// <summary>Por GB real, con decimales.</summary>
    PorGb = 0,
    /// <summary>Por GB empezado: cada tramo se cobra entero.</summary>
    PorGbComenzado = 1,
    /// <summary>Por minuto empezado de copia real.</summary>
    PorDuracion = 2,
    /// <summary>Por carpeta de primer nivel copiada.</summary>
    PorCarpeta = 3
}

/// <summary>Piezas comunes de la ventana: ajustes y trabajo en curso (formato
/// propio .qbt, guardado junto al programa), autosave y la calculadora de
/// precio por GB del multicopiador.</summary>
public sealed partial class VentanaDoble
{
    // =====================================================================
    //  Ajustes y trabajo en curso (formato propio, junto al programa)
    // =====================================================================

    private static string CarpetaPrograma
    {
        get
        {
            var exe = Environment.ProcessPath;
            var dir = exe != null ? Path.GetDirectoryName(exe) : null;
            if (string.IsNullOrEmpty(dir)) dir = AppContext.BaseDirectory;
            return dir!;
        }
    }

    private static string FicheroAjustes => Path.Combine(CarpetaPrograma, "QBASFreemanCopy-Converter.ajustes.json");
    private static string FicheroTrabajo => Path.Combine(CarpetaPrograma, "QBASFreemanCopy-Converter.trabajo.qbt");
    private static string FicheroTrabajoConv => Path.Combine(CarpetaPrograma, "QBASFreemanCopy-Converter.conversion.qbt");

    private string CargarTemaGuardado()
    {
        try
        {
            if (File.Exists(FicheroAjustes))
            {
                var txt = File.ReadAllText(FicheroAjustes);
                var doc = JsonDocument.Parse(txt);
                // La ventana de Opciones guarda el tema como "temaVisual"; el
                // desplegable de la cabecera lo guarda como "tema". Vale cualquiera.
                if (doc.RootElement.TryGetProperty("temaVisual", out var tv) && tv.ValueKind == JsonValueKind.String)
                    return tv.GetString() ?? Tema.PorDefecto.Nombre;
                if (doc.RootElement.TryGetProperty("tema", out var t) && t.ValueKind == JsonValueKind.String)
                    return t.GetString() ?? Tema.PorDefecto.Nombre;
            }
        }
        catch { }
        return Tema.PorDefecto.Nombre;
    }

    private void GuardarAjustes()
    {
        try
        {
            // Se lee lo que ya hay y se actualizan SOLO nuestras claves, para no
            // borrar lo que guarde la ventana de Opciones (que escribe suelto).
            var datos = Opciones.Leer();

            void Poner(string clave, object? valor)
            {
                var json = JsonSerializer.Serialize(valor);
                datos[clave] = JsonDocument.Parse(json).RootElement.Clone();
            }

            Poner("programa", "QBASFreemanCopy-Converter");
            Poner("version", 2);
            Poner("tema", _tema.Nombre);
            Poner("temaVisual", _tema.Nombre);
            Poner("limiteConv", _cmbLimiteConv?.SelectedItem?.ToString() ?? "Automático");
            Poner("formato", _cmbFormato?.SelectedItem?.ToString() ?? "mp4");
            Poner("modoIdx", _cmbModoConv?.SelectedIndex ?? 1);
            Poner("salida", _cmbSalida?.SelectedIndex ?? 0);
            Poner("nombreSub", _txtSubcarpeta?.Text ?? "Convertidos");
            Poner("carpetaFija", _txtCarpetaFija?.Text ?? "");
            Poner("mantenerEstructura", _chkEstructura?.Checked ?? false);
            Poner("borrarOriginal", _chkBorrarOriginal?.Checked ?? false);
            Poner("motor", _cmbMotor?.SelectedIndex ?? 0);
            Poner("precioActivo", _chkPrecio?.Checked ?? false);
            Poner("precioPorGb", _txtPrecioPorGb?.Text ?? "0.50");
            Poner("cobroModo", _cmbCobro?.SelectedIndex ?? 0);

            Opciones.Escribir(datos);
        }
        catch { }
    }

    /// <summary>Guarda el trabajo de copia en curso (origenes, destinos y opciones).</summary>
    private void GuardarSesion(bool silencioso)
    {
        try
        {
            if (_origenes.Count == 0 && _destinos.Count == 0)
            {
                if (File.Exists(FicheroTrabajo)) File.Delete(FicheroTrabajo);
                return;
            }
            var json = JsonSerializer.Serialize(new
            {
                programa = "QBASFreemanCopy-Converter",
                tipo = "copia",
                fecha = DateTime.Now.ToString("s"),
                origenes = _origenes.ToArray(),
                destinos = _destinos.ToArray(),
                estructura = _cmbEstructura?.SelectedIndex ?? 4,
                modo = _cmbAccion?.SelectedIndex ?? 0,
                verificar = _chkVerificar?.Checked ?? false,
                veloz = _chkVeloz?.Checked ?? true,
                // Carpeta raiz que hay que conservar en el destino de cada origen.
                prefijos = _prefijosOrigen,
                // Archivos quitados con Ctrl+clic en el navegador interno.
                excluidos = _excluidos.ToArray()
            }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FicheroTrabajo, json);
        }
        catch { }
    }

    /// <summary>Pregunta al abrir si continuar con el trabajo guardado.</summary>
    private void ReanudarSiHayTrabajo()
    {
        try
        {
            if (!File.Exists(FicheroTrabajo)) return;
            var doc = JsonDocument.Parse(File.ReadAllText(FicheroTrabajo));
            var raiz = doc.RootElement;
            if (!raiz.TryGetProperty("tipo", out var tip) || tip.GetString() != "copia") return;

            var origenes = LeerArray(raiz, "origenes");
            var destinos = LeerArray(raiz, "destinos");
            if (origenes.Count == 0 && destinos.Count == 0) return;

            var r = MessageBox.Show(this,
                $"Hay un trabajo de copia guardado ({origenes.Count} origen/es → {destinos.Count} disco/s).\n\n" +
                "¿Quieres continuarlo?\n\n" +
                "· Sí: se vuelve a cargar tal cual estaba.\n" +
                "· No: se empieza de cero (se descarta el guardado).",
                "QBASFreemanCopy-Converter · continuar trabajo",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (r == DialogResult.Yes)
            {
                foreach (var o in origenes) if (!_origenes.Contains(o)) _origenes.Add(o);
                foreach (var d in destinos) _destinos.Add(d);

                // Recuperar la carpeta raiz de cada origen (o deducirla).
                _prefijosOrigen.Clear();
                if (raiz.TryGetProperty("prefijos", out var prefs) && prefs.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in prefs.EnumerateObject())
                        if (p.Value.ValueKind == JsonValueKind.String)
                            _prefijosOrigen[p.Name] = p.Value.GetString() ?? "";
                }
                foreach (var o in origenes)
                    if (!_prefijosOrigen.ContainsKey(o))
                        _prefijosOrigen[o] = Directory.Exists(o)
                            ? NombreDeCarpeta(o)
                            : NombreDeCarpeta(Path.GetDirectoryName(o));

                // Lo que se habia quitado con Ctrl+clic sigue fuera de la copia.
                _excluidos.Clear();
                foreach (var x in LeerArray(raiz, "excluidos")) _excluidos.Add(x);
                if (raiz.TryGetProperty("estructura", out var e) && e.TryGetInt32(out var ie) && _cmbEstructura != null)
                    _cmbEstructura.SelectedIndex = Math.Clamp(ie, 0, _cmbEstructura.Items.Count - 1);
                if (raiz.TryGetProperty("modo", out var m) && m.TryGetInt32(out var im) && _cmbAccion != null)
                    _cmbAccion.SelectedIndex = Math.Clamp(im, 0, _cmbAccion.Items.Count - 1);
                if (raiz.TryGetProperty("verificar", out var v) && v.ValueKind == JsonValueKind.True && _chkVerificar != null)
                    _chkVerificar.Checked = true;
                if (raiz.TryGetProperty("veloz", out var vz) && _chkVeloz != null)
                    _chkVeloz.Checked = vz.ValueKind == JsonValueKind.True;
                PintarOrigenes();
                PintarDiscos();
            }
            else
            {
                try { File.Delete(FicheroTrabajo); } catch { }
            }
        }
        catch { }
    }

    private static List<string> LeerArray(JsonElement raiz, string nombre)
    {
        var lista = new List<string>();
        if (raiz.TryGetProperty(nombre, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var x in arr.EnumerateArray())
                if (x.ValueKind == JsonValueKind.String) lista.Add(x.GetString()!);
        return lista;
    }

    private System.Windows.Forms.Timer? _autosave;

    private void IniciarAutosave()
    {
        _autosave = new System.Windows.Forms.Timer { Interval = 60_000 };  // cada 1 minuto
        _autosave.Tick += (_, _) =>
        {
            GuardarSesion(silencioso: true);
            try { ((VentanaDoble)FindForm() ?? this).GuardarSesionConversion(true); } catch { }
            GuardarAjustes();
        };
        _autosave.Start();
    }

    // =====================================================================
    //  Calculadora de precio por GB (SOLO el multicopiador)
    // =====================================================================
    //  No cobra nada: es una cuenta local para que tu sepas lo que vale el
    //  trabajo. Se calcula sobre los GB copiados DE VERDAD (no los previstos),
    //  cada disco redondea su importe a 2 decimales y luego se suman.

    /// <summary>Precio o tarifa por unidad (0 = calculadora apagada o sin valor valido).</summary>
    private double PrecioPorGb
    {
        get
        {
            if (_chkPrecio is not { Checked: true }) return 0;
            var txt = (_txtPrecioPorGb?.Text ?? "").Trim().Replace(',', '.');
            return double.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0 ? v : 0;
        }
    }

    /// <summary>Como se cobra el trabajo: por GB, por GB comenzado, por duracion o por carpeta.</summary>
    private ModoCobro CobroActual =>
        (ModoCobro)Math.Clamp(_cmbCobro?.SelectedIndex ?? 0, 0, (int)ModoCobro.PorCarpeta);

    private static long BytesDe1Gb => 1024L * 1024 * 1024;

    /// <summary>Carpetas de primer nivel que se han copiado en un disco (para cobrar por carpeta).</summary>
    private static int CarpetasDe(TrabajoDestino t)
    {
        var raiz = t.Destino.Raiz.TrimEnd('\\', '/');
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ruta in t.Terminados)
        {
            var rel = ruta.StartsWith(raiz, StringComparison.OrdinalIgnoreCase)
                ? ruta[raiz.Length..].TrimStart('\\', '/')
                : ruta;
            var partes = rel.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            // Si el fichero va suelto en la raiz, cuenta como una carpeta mas.
            set.Add(partes.Length >= 2 ? partes[0] : "");
        }
        return Math.Max(1, set.Count);
    }

    /// <summary>Importe de UN disco segun el modo de cobro, redondeado a 2 decimales.</summary>
    private double ImporteDisco(TrabajoDestino t)
    {
        var precio = PrecioPorGb;
        if (precio <= 0) return 0;
        if (t.BytesHechos <= 0 && t.Duracion <= TimeSpan.Zero) return 0;

        double importe = CobroActual switch
        {
            // Cada GB empezado se cobra entero.
            ModoCobro.PorGbComenzado => Math.Ceiling(t.BytesHechos / (double)BytesDe1Gb) * precio,
            // Cada minuto empezado se cobra entero.
            ModoCobro.PorDuracion => Math.Ceiling(t.Duracion.TotalMinutes) * precio,
            // Cada carpeta de primer nivel copiada se cobra aparte.
            ModoCobro.PorCarpeta => CarpetasDe(t) * precio,
            // Por GB real, con decimales.
            _ => t.BytesHechos / (double)BytesDe1Gb * precio
        };
        return Math.Round(importe, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Importe de UN disco por bytes (se conserva para las filas antiguas).</summary>
    private double ImporteDisco(long bytesCopiados)
    {
        var precio = PrecioPorGb;
        if (precio <= 0) return 0;
        return Math.Round(bytesCopiados / (double)BytesDe1Gb * precio, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Total: la SUMA de los importes ya redondeados de cada disco.</summary>
    private double ImporteTotal(out long bytesContados)
    {
        bytesContados = 0;
        var precio = PrecioPorGb;
        if (precio <= 0) return 0;
        double total = 0;
        foreach (var t in _trabajos.Trabajos)
        {
            if (t.Estado is not (TrabajoEstado.Hecho or TrabajoEstado.Cancelado or TrabajoEstado.Error)) continue;
            if (t.BytesHechos <= 0) continue;
            bytesContados += t.BytesHechos;
            total += ImporteDisco(t);
        }
        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Resumen de lo que se esta cobrando, para la barra de abajo.</summary>
    private string DetalleCobro(long bytes)
    {
        var precio = PrecioPorGb;
        if (CobroActual == ModoCobro.PorDuracion)
        {
            var segundos = _trabajos.Trabajos.Sum(t => t.Duracion.TotalSeconds);
            var minutos = (long)Math.Ceiling(segundos / 60.0);
            return $"{minutos} min a {Dinero(precio)}/min";
        }
        if (CobroActual == ModoCobro.PorCarpeta)
        {
            var carpetas = _trabajos.Trabajos
                .Where(t => t.Estado is TrabajoEstado.Hecho or TrabajoEstado.Cancelado or TrabajoEstado.Error)
                .Sum(CarpetasDe);
            return $"{carpetas} carpeta(s) a {Dinero(precio)}/carpeta";
        }
        if (CobroActual == ModoCobro.PorGbComenzado)
        {
            var tramos = _trabajos.Trabajos
                .Where(t => t.Estado is TrabajoEstado.Hecho or TrabajoEstado.Cancelado or TrabajoEstado.Error)
                .Sum(t => (long)Math.Ceiling(t.BytesHechos / (double)BytesDe1Gb));
            return $"{tramos} GB comenzado(s) a {Dinero(precio)}/GB";
        }
        return $"{Gb(bytes)} GB a {Dinero(precio)}/GB";
    }

    private static string Dinero(double importe) =>
        "$" + importe.ToString("N2", CultureInfo.GetCultureInfo("en-US"));

    private static string Gb(long bytes) =>
        (bytes / (double)BytesDe1Gb).ToString("N2", CultureInfo.GetCultureInfo("en-US"));

    /// <summary>Repinta SOLO el total y las filas. No guarda en disco ni repinta botones.</summary>
    private void RefrescarPrecio()
    {
        var precio = PrecioPorGb;
        if (_lblPrecioTotal != null)
        {
            string texto;
            if (precio <= 0)
                texto = _chkPrecio is { Checked: true } ? "La tarifa no es válida." : "";
            else
            {
                var total = ImporteTotal(out var bytes);
                texto = $"TOTAL: {Dinero(total)}   ·   {DetalleCobro(bytes)}";
            }
            if (_lblPrecioTotal.Text != texto) _lblPrecioTotal.Text = texto;
            if (_lblPrecioTotal.ForeColor != _tema.Hecho) _lblPrecioTotal.ForeColor = _tema.Hecho;
        }
        RepintarFilas();
    }

    /// <summary>Repinta las tarjetas de disco, sin tocar sus botones hijos.</summary>
    private void RepintarFilas()
    {
        if (_panelDiscos == null || _panelDiscos.IsDisposed) return;
        foreach (Control c in _panelDiscos.Controls)
            if (c is PanelRedondo) c.Invalidate();
        // El fondo con la marca de agua NO se repinta aqui: ya esta cacheado y si
        // se invalida en cada refresco es lo que hacia que todo fuera a tirones.
    }

    /// <summary>Pide un texto corto (para nombres de perfil), sin dependencias raras.</summary>
    private string? PedirTexto(string titulo, string etiqueta, string porDefecto)
    {
        using var dlg = new Form
        {
            Text = titulo, StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false,
            ClientSize = new Size(360, 110), Font = new Font("Segoe UI", 9f),
            BackColor = _tema.Panel, ForeColor = _tema.Tinta
        };
        var l = new Label { Text = etiqueta, AutoSize = true, Location = new Point(12, 14), ForeColor = _tema.Tinta };
        var t = new TextBox { Location = new Point(12, 38), Width = 336, Text = porDefecto, BackColor = _tema.Panel2, ForeColor = _tema.Tinta };
        var ok = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, Location = new Point(196, 72), Width = 74, FlatStyle = FlatStyle.Flat, BackColor = _tema.Acento, ForeColor = _tema.SobreAcento };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(274, 72), Width = 74, FlatStyle = FlatStyle.Flat, BackColor = _tema.Panel2, ForeColor = _tema.Tinta };
        dlg.Controls.Add(l); dlg.Controls.Add(t); dlg.Controls.Add(ok); dlg.Controls.Add(cancel);
        dlg.AcceptButton = ok; dlg.CancelButton = cancel;
        return dlg.ShowDialog(this) == DialogResult.OK ? t.Text : null;
    }

    // =====================================================================
    //  Tema
    // =====================================================================

    /// <summary>Repinta TODA la ventana con el tema elegido.</summary>
    private void AplicarTema()
    {
        BackColor = _tema.Fondo;
        if (_logo != null)
        {
            _logo.Invalidate();
        }
        if (_fondoDiscos != null)
        {
            _fondoDiscos.BackColor = _tema.PanelDiscos;
            _fondoDiscos.GradienteArriba = _tema.PanelDiscosArriba;
            _fondoDiscos.GradienteAbajo = _tema.PanelDiscosAbajo;
            _fondoDiscos.InvalidarFondo();
        }
        AplicarTemaRecursivo(this);
        PintarOrigenes();
        PintarDiscos();
        RefrescarPrecio();
        TemaBarraTitulo();
        Invalidate(true);
    }

    // =====================================================================
    //  Barra de titulo oscura (la de Windows), como en la captura
    // =====================================================================

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr ventana, int atributo, ref int valor, int tamano);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr ventana, string? subApp, string? subIdList);

    /// <summary>Pone la barra de desplazamiento nativa en oscuro. Sin esto, el panel
    /// de discos saca una barra blanca que rompe el tema.</summary>
    internal static void TemaOscuroBarra(IntPtr ventana)
    {
        try { SetWindowTheme(ventana, "DarkMode_Explorer", null); } catch { }
    }

    private const int DwmModoOscuro = 20;       // Windows 10 2004 y posteriores
    private const int DwmModoOscuroViejo = 19;  // Windows 10 anterior
    private const int DwmColorBarra = 35;       // Windows 11
    private const int DwmColorTexto = 36;       // Windows 11

    /// <summary>Pinta la barra de titulo de Windows en oscuro y del color del panel.</summary>
    private void TemaBarraTitulo()
    {
        if (!OscuroParaWindows()) return;
        try
        {
            var h = Handle;
            int oscuro = _tema.Oscuro ? 1 : 0;
            if (DwmSetWindowAttribute(h, DwmModoOscuro, ref oscuro, sizeof(int)) != 0)
                DwmSetWindowAttribute(h, DwmModoOscuroViejo, ref oscuro, sizeof(int));

            int color = ColorTranslator.ToWin32(_tema.Panel);
            DwmSetWindowAttribute(h, DwmColorBarra, ref color, sizeof(int));

            int texto = ColorTranslator.ToWin32(_tema.Tinta);
            DwmSetWindowAttribute(h, DwmColorTexto, ref texto, sizeof(int));
        }
        catch { }
    }

    [DllImport("ntdll.dll", PreserveSig = true)]
    private static extern int RtlGetVersion(ref VersionInfo version);

    [StructLayout(LayoutKind.Sequential)]
    private struct VersionInfo
    {
        public int Tamano;
        public int Mayor;
        public int Menor;
        public int Compilacion;
        public int Plataforma;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Texto;
    }

    private static bool _esWindows10O17;
    private static bool _versionComprobada;

    /// <summary>DWM existe desde Windows 10 1809; en versiones anteriores se ignora.</summary>
    private static bool OscuroParaWindows()
    {
        if (_versionComprobada) return _esWindows10O17;
        _versionComprobada = true;
        try
        {
            var v = new VersionInfo { Tamano = Marshal.SizeOf<VersionInfo>(), Texto = "" };
            if (RtlGetVersion(ref v) == 0)
                _esWindows10O17 = v.Mayor > 10 || (v.Mayor == 10 && v.Compilacion >= 17763);
        }
        catch { }
        return _esWindows10O17;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        TemaBarraTitulo();
    }

    private void AplicarTemaRecursivo(Control c)
    {
        foreach (Control hijo in c.Controls)
        {
            switch (hijo)
            {
                case BotonRedondo br:
                    br.Tema = _tema;
                    br.Invalidate();
                    break;
                case PanelRedondo pr:
                    pr.Tema = _tema;
                    if (pr.Tag is QBASSuite.Devices.DeviceInfo)
                    {
                        // Filas de disco: SIN caja, transparentes sobre el fondo con
                        // la marca de agua (como en la captura). El estado se pinta
                        // en PintarFilaDisco, no aqui.
                        pr.BackColor = Color.Transparent;
                        pr.ConBorde = false;
                    }
                    else pr.BackColor = _tema.Panel;
                    pr.Invalidate();
                    break;
                case PanelFoto pf:
                    pf.Invalidate();
                    break;
                case Button b:
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = _tema.Linea;
                    b.BackColor = _tema.Panel2;
                    b.ForeColor = _tema.Tinta;
                    break;
                case ListView lv:
                    lv.BackColor = lv.Tag as string == "discos" ? _tema.PanelDiscos : _tema.Panel2;
                    lv.ForeColor = _tema.Tinta;
                    if (lv is ListaOscura lo)
                    {
                        lo.Tema = _tema;
                        lo.FondoFilas = lv.BackColor;
                        lv.Invalidate();
                    }
                    break;
                case TextBox tb:
                    tb.BackColor = _tema.Panel2;
                    tb.ForeColor = _tema.Tinta;
                    break;
                case ComboBox cb:
                    cb.FlatStyle = FlatStyle.Flat;
                    cb.BackColor = _tema.Panel2;
                    cb.ForeColor = _tema.Tinta;
                    break;
                case CheckBox chk:
                    chk.ForeColor = _tema.Tinta;
                    chk.BackColor = Color.Transparent;
                    break;
                case RadioButton rb:
                    rb.ForeColor = _tema.Tinta;
                    rb.BackColor = Color.Transparent;
                    break;
                case LinkLabel ll:
                    if (EsTintaOscura(ll.ForeColor)) ll.ForeColor = _tema.Acento;
                    ll.LinkColor = _tema.Acento;
                    break;
                case Label l:
                    // ANTES se miraba "ForeColor == Color.Empty", pero WinForms
                    // NUNCA devuelve Empty: devuelve el negro por defecto de
                    // Windows. Por eso media ventana se quedaba con texto negro
                    // sobre fondo oscuro, es decir, sin texto visible.
                    if (EsTintaOscura(l.ForeColor)) l.ForeColor = _tema.Tinta;
                    break;
                case ListBox lb:
                    lb.BackColor = _tema.Panel2;
                    lb.ForeColor = _tema.Tinta;
                    break;
                case NumericUpDown nu:
                    nu.BackColor = _tema.Panel2;
                    nu.ForeColor = _tema.Tinta;
                    break;
                case GroupBox gb:
                    gb.ForeColor = _tema.Tinta;
                    break;
            }

            // Cualquier panel que se haya quedado con el gris de fabrica de
            // Windows tambien se pinta con el color del tema.
            if (hijo is Panel or TableLayoutPanel or FlowLayoutPanel
                && hijo is not PanelRedondo and not PanelFoto
                && hijo is not PanelMarcaAgua and not PanelLogo
                && EsFondoDeFabrica(hijo.BackColor))
            {
                hijo.BackColor = _tema.Panel;
            }

            AplicarTemaRecursivo(hijo);
        }
    }

    /// <summary>Negro de texto de Windows, que en un tema oscuro no se veria.</summary>
    private static bool EsTintaOscura(Color c)
    {
        if (c.A == 0) return true;
        if (c.ToArgb() == SystemColors.ControlText.ToArgb()) return true;
        return c.R < 45 && c.G < 45 && c.B < 45;
    }

    /// <summary>Gris o blanco de fabrica de Windows (control, ventana).</summary>
    private static bool EsFondoDeFabrica(Color c)
        => c.A != 0 && (c.ToArgb() == SystemColors.Control.ToArgb()
                     || c.ToArgb() == SystemColors.Window.ToArgb());

    // =====================================================================
    //  Utilidad
    // =====================================================================

    private void Mensaje(string texto)
    {
        // En la barra de abajo cabe una linea: si el aviso es largo o trae varias,
        // se enseña tambien en una ventana para que no se pierda nada.
        if (_lblMensaje != null)
            _lblMensaje.Text = "  " + texto.Replace("\r", " ").Replace("\n", "   ");
        if (texto.Contains('\n'))
            MessageBox.Show(this, texto, "QBASFreemanCopy-Converter",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
