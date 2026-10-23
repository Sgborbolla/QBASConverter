using System.Globalization;
using System.Text.Json;

namespace QBASSuite.Ui;

/// <summary>
/// Piezas para la ventana de Opciones: interruptores, desplegables, ruedas,
/// cajas de texto y listas. TODAS guardan SOLO, al instante, cuando las cambias.
///
/// Los ajustes se leen y se escriben en el mismo fichero de ajustes que usa el
/// resto del programa (QBASFreemanCopy-Converter.ajustes.json, junto al .exe).
/// </summary>
internal static class Opciones
{
    /// <summary>Ruta del fichero de ajustes (el mismo de la ventana principal).</summary>
    public static string Fichero
    {
        get
        {
            var exe = Environment.ProcessPath;
            var dir = exe != null ? Path.GetDirectoryName(exe) : null;
            if (string.IsNullOrEmpty(dir)) dir = AppContext.BaseDirectory;
            return Path.Combine(dir!, "QBASFreemanCopy-Converter.ajustes.json");
        }
    }

    // =====================================================================
    //  Lectura y escritura del fichero de ajustes
    // =====================================================================

    public static Dictionary<string, JsonElement> Leer()
    {
        var dic = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (!File.Exists(Fichero)) return dic;
            using var doc = JsonDocument.Parse(File.ReadAllText(Fichero));
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return dic;
            foreach (var prop in doc.RootElement.EnumerateObject())
                dic[prop.Name] = prop.Value.Clone();
        }
        catch { }
        return dic;
    }

    public static void Escribir(Dictionary<string, JsonElement> datos)
    {
        try
        {
            var obj = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in datos) obj[kv.Key] = AObjeto(kv.Value);
            File.WriteAllText(Fichero, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private static object? AObjeto(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.TryGetInt64(out var l) ? l : e.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => e.EnumerateArray().Select(AObjeto).ToList(),
        JsonValueKind.Object => e.EnumerateObject().ToDictionary(p => p.Name, p => AObjeto(p.Value)),
        _ => null
    };

    /// <summary>Lee un valor: true/false, numero o cadena. Devuelve el defecto si no esta.</summary>
    public static string LeerTexto(string clave, string porDefecto)
    {
        var d = Leer();
        if (!d.TryGetValue(clave, out var v)) return porDefecto;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString() ?? porDefecto,
            JsonValueKind.Number => v.ToString(),
            JsonValueKind.True => "True",
            JsonValueKind.False => "False",
            _ => porDefecto
        };
    }

    public static bool LeerBooleano(string clave, bool porDefecto)
    {
        var t = LeerTexto(clave, porDefecto ? "True" : "False");
        return t.Equals("True", StringComparison.OrdinalIgnoreCase) || t == "1" || t.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public static int LeerEntero(string clave, int porDefecto)
    {
        var t = LeerTexto(clave, porDefecto.ToString(CultureInfo.InvariantCulture));
        return int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : porDefecto;
    }

    public static double LeerNumero(string clave, double porDefecto)
    {
        var t = LeerTexto(clave, porDefecto.ToString(CultureInfo.InvariantCulture)).Replace(',', '.');
        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : porDefecto;
    }

    /// <summary>Guarda un valor suelto sin tocar los demas (guardado automatico).</summary>
    public static void Guardar(string clave, string valor)
    {
        var d = Leer();
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(valor));
        d[clave] = doc.RootElement.Clone();
        Escribir(d);
    }

    public static void Guardar(string clave, bool valor) => Guardar(clave, valor ? "True" : "False");
    public static void Guardar(string clave, int valor) => GuardarClaveNumerica(clave, valor);
    public static void Guardar(string clave, double valor) => GuardarClaveNumerica(clave, valor);

    private static void GuardarClaveNumerica(string clave, object valor)
    {
        var d = Leer();
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(valor));
        d[clave] = doc.RootElement.Clone();
        Escribir(d);
    }

    /// <summary>Guardar como texto pero que se lea como numero (los ajustes guardan todo como texto).</summary>
    public static void GuardarTexto(string clave, string valor) => Guardar(clave, valor);

    // =====================================================================
    //  Controles
    // =====================================================================

    /// <summary>Columna vertical donde se apilan las opciones de una seccion.</summary>
    public static FlowLayoutPanel Pila(Tema t) => new()
    {
        FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = t.Panel, Padding = new Padding(0)
    };

    /// <summary>Interruptor: se enciende y apaga cuando quieras y se guarda solo.</summary>
    public static Control Interruptor(Tema t, string clave, string etiqueta, string ayuda, Action guardar)
        => Controles.Interruptor(t, etiqueta, ayuda, LeerBooleano(clave, false), valor =>
        {
            Guardar(clave, valor);
            guardar();
        });

    /// <summary>Desplegable que se guarda solo.</summary>
    public static Control Desplegable(Tema t, string clave, string etiqueta, string[] opciones, Action guardar)
    {
        var cont = new Panel { AutoSize = true, MinimumSize = new Size(600, 30), BackColor = t.Panel, Margin = new Padding(0, 4, 0, 4) };
        cont.Controls.Add(new Label { Text = etiqueta, AutoSize = true, ForeColor = t.Tinta, Location = new Point(0, 6) });

        var actual = LeerTexto(clave, opciones.Length > 0 ? opciones[0] : "");
        var cmb = new ComboBox
        {
            Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = t.Panel2, ForeColor = t.Tinta,
            Location = new Point(0, 28)
        };
        cmb.Items.AddRange(opciones.Cast<object>().ToArray());
        var idx = Array.FindIndex(opciones, o => string.Equals(o, actual, StringComparison.OrdinalIgnoreCase));
        cmb.SelectedIndex = idx < 0 ? 0 : idx;
        cmb.SelectedIndexChanged += (_, _) => { Guardar(clave, cmb.SelectedItem?.ToString() ?? ""); guardar(); };

        var etiquetaAncho = TextRenderer.MeasureText(etiqueta, cont.Font).Width;
        cmb.Location = new Point(Math.Min(etiquetaAncho + 12, 320), 4);
        cont.Height = 30;
        cont.Controls.Add(cmb);
        return cont;
    }

    /// <summary>Caja de texto que se guarda sola (al perder el foco).</summary>
    public static Control Caja(Tema t, string clave, string etiqueta, string porDefecto, Action guardar)
    {
        var cont = new Panel { AutoSize = true, MinimumSize = new Size(600, 30), BackColor = t.Panel, Margin = new Padding(0, 4, 0, 4) };
        cont.Controls.Add(new Label { Text = etiqueta, AutoSize = true, ForeColor = t.Tinta, Location = new Point(0, 6) });
        var txt = new TextBox
        {
            Width = 140, Text = LeerTexto(clave, porDefecto), BackColor = t.Panel2, ForeColor = t.Tinta,
            BorderStyle = BorderStyle.FixedSingle
        };
        txt.Leave += (_, _) => { Guardar(clave, txt.Text); guardar(); };
        var etiquetaAncho = TextRenderer.MeasureText(etiqueta, cont.Font).Width;
        txt.Location = new Point(Math.Min(etiquetaAncho + 12, 320), 3);
        cont.Controls.Add(txt);
        return cont;
    }

    /// <summary>Barra deslizadora con su numero: se guarda sola.</summary>
    public static Control Rueda(Tema t, string clave, string etiqueta, string unidad, int porDefecto, int min, int max, int paso, Action guardar)
        => Controles.Deslizador(t, etiqueta, unidad, "", LeerEntero(clave, porDefecto), min, max, paso, valor =>
        {
            Guardar(clave, valor);
            guardar();
        });

    /// <summary>Lista simple de rutas guardadas (solo lectura).</summary>
    public static Control ListaRutas(Tema t, string clave)
    {
        var lista = new ListBox
        {
            Width = 620, Height = 140, BackColor = t.Panel2, ForeColor = t.Tinta,
            BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 4, 0, 8)
        };
        try
        {
            var d = Leer();
            if (d.TryGetValue(clave, out var v) && v.ValueKind == JsonValueKind.Array)
                foreach (var x in v.EnumerateArray())
                    if (x.ValueKind == JsonValueKind.String) lista.Items.Add(x.GetString());
        }
        catch { }
        if (lista.Items.Count == 0) lista.Items.Add("(ninguna carpeta todavia)");
        return lista;
    }

    /// <summary>Etiqueta de ayuda suelta.</summary>
    public static Label Nota(Tema t, string texto) => new()
    {
        Text = texto, AutoSize = true, ForeColor = t.Tinta2,
        Font = new Font("Segoe UI", 8.5f), Margin = new Padding(0, 0, 0, 6)
    };
}

