using System.Globalization;
using System.Text.Json;
using QBASSuite.Engine;
using QBASSuite.Util;

namespace QBASSuite.Ui;

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
            Poner("limiteConv", _cmbLimiteConv?.SelectedItem?.ToString() ?? "Automático");
            Poner("formato", _cmbFormato?.SelectedItem?.ToString() ?? "mp4");
            Poner("modoIdx", _cmbModoConv?.SelectedIndex ?? 1);
            Poner("salida", _cmbSalida?.SelectedIndex ?? 0);
            Poner("nombreSub", _txtSubcarpeta?.Text ?? "Convertidos");
            Poner("carpetaFija", _txtCarpetaFija?.Text ?? "");
            Poner("mantenerEstructura", _chkEstructura?.Checked ?? false);
            Poner("borrarOriginal", _chkBorrarOriginal?.Checked ?? false);
            Poner("gpu", _chkGpu?.Checked ?? true);
            Poner("precioActivo", _chkPrecio?.Checked ?? false);
            Poner("precioPorGb", _txtPrecioPorGb?.Text ?? "0.50");

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
                veloz = _chkVeloz?.Checked ?? true
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

            var r = MessageBox.Show(
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
                if (raiz.TryGetProperty("estructura", out var e) && e.TryGetInt32(out var ie) && _cmbEstructura != null)
                    _cmbEstructura.SelectedIndex = Math.Clamp(ie, 0, _cmbEstructura.Items.Count - 1);
                if (raiz.TryGetProperty("modo", out var m) && m.TryGetInt32(out var im) && _cmbAccion != null)
                    _cmbAccion.SelectedIndex = Math.Clamp(im, 0, _cmbAccion.Items.Count - 1);
                if (raiz.TryGetProperty("verificar", out var v) && v.ValueKind == JsonValueKind.True && _chkVerificar != null)
                    _chkVerificar.Checked = true;
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

    /// <summary>Precio por GB (0 = calculadora apagada o sin valor valido).</summary>
    private double PrecioPorGb
    {
        get
        {
            if (_chkPrecio is not { Checked: true }) return 0;
            var txt = (_txtPrecioPorGb?.Text ?? "").Trim().Replace(',', '.');
            return double.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0 ? v : 0;
        }
    }

    private static long BytesDe1Gb => 1024L * 1024 * 1024;

    /// <summary>Importe de UN disco: redondeado a 2 decimales (lo que se muestra).</summary>
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
            total += ImporteDisco(t.BytesHechos);
        }
        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
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
                texto = _chkPrecio is { Checked: true } ? "Precio por GB no válido." : "";
            else
            {
                var total = ImporteTotal(out var bytes);
                texto = $"TOTAL: {Dinero(total)}   ·   {Gb(bytes)} GB a {Dinero(precio)}/GB";
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
            _logo.EtiquetaForma = () => _tema.Forma;
            _logo.EtiquetaTinta = () => _tema.Acento;
            _logo.EtiquetaSombra = () => _tema.AcentoSuave;
            _logo.Invalidate();
        }
        if (_fondoDiscos != null)
        {
            _fondoDiscos.BackColor = _tema.PanelDiscos;
            _fondoDiscos.InvalidarFondo();
        }
        AplicarTemaRecursivo(this);
        PintarOrigenes();
        PintarDiscos();
        RefrescarPrecio();
        Invalidate(true);
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
                    pr.BackColor = _tema.Panel;
                    pr.Invalidate();
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
                    break;
                case TextBox tb:
                    tb.BackColor = _tema.Panel2;
                    tb.ForeColor = _tema.Tinta;
                    break;
                case ComboBox cb:
                    cb.BackColor = _tema.Panel2;
                    cb.ForeColor = _tema.Tinta;
                    break;
                case CheckBox chk:
                    chk.ForeColor = _tema.Tinta;
                    chk.BackColor = Color.Transparent;
                    break;
                case Label l when l.ForeColor == Color.Empty:
                    l.ForeColor = _tema.Tinta;
                    break;
            }
            AplicarTemaRecursivo(hijo);
        }
    }

    // =====================================================================
    //  Utilidad
    // =====================================================================

    private void Mensaje(string texto)
    {
        if (_lblMensaje != null) _lblMensaje.Text = "  " + texto;
    }
}
