using System.Globalization;
using System.Text.Json;
using QBASSuite.Devices;
using QBASSuite.Engine;
using QBASSuite.Util;

namespace QBASSuite.Ui;

/// <summary>Los modos de arrastre que tenia XpressCopy. Deciden que se hace con
/// lo que sueltas en la mitad de los discos. Valen para UN arrastre y luego la
/// ventana vuelve sola a Normal, igual que en el original.</summary>
public enum ModoArrastre
{
    /// <summary>Copia normal a todos los discos marcados.</summary>
    Normal = 0,
    /// <summary>Anade sin comparar: lo que ya existe se queda como esta.</summary>
    Anadir = 1,
    /// <summary>Toma el primer disco marcado como referencia y borra diferencias.</summary>
    Comparar = 2,
    /// <summary>Quita de los discos marcados lo que sueltes. Pide confirmacion.</summary>
    Eliminar = 3,
    /// <summary>Copia solo la carpeta que contiene los ficheros, sin la ruta entera.</summary>
    SoloCarpeta = 4,
    /// <summary>Copia solo los ficheros, sueltos, sin ninguna carpeta.</summary>
    SoloFicheros = 5
}

/// <summary>Matrices de discos, listas de copia guardadas, ocultar discos, lector,
/// clonar/extraer/reparar/formatear y la lista de ficheros por escribir.</summary>
public sealed partial class VentanaDoble
{
    // ---- estado de estas funciones ----
    private ComboBox _cmbModoArrastre = null!;
    private ModoArrastre _modo = ModoArrastre.Normal;
    /// <summary>Modo con el que se anadio cada origen (para aplanar al copiar).</summary>
    private readonly Dictionary<string, ModoArrastre> _modoPorOrigen = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Discos ocultos por el usuario (se conservan al refrescar).</summary>
    private readonly HashSet<string> _ocultos = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Disco que hace de lector para los demas.</summary>
    private string? _lector;
    /// <summary>Matrices guardadas: nombre -> discos.</summary>
    private readonly Dictionary<string, string[]> _matrices = new(StringComparer.OrdinalIgnoreCase);

    private static string FicheroMatrices => Path.Combine(CarpetaPrograma, "QBASFreemanCopy-Converter.matrices.json");

    // =====================================================================
    //  Barra de modos + acciones de disco
    // =====================================================================

    /// <summary>Fila con el modo de arrastre y los botones de accion sobre discos.</summary>
    private Control BarraModos()
    {
        var t = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Color.Transparent,
            ColumnCount = 3, RowCount = 1, Margin = new Padding(0), Padding = new Padding(0)
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 146));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        t.Controls.Add(new Label
        {
            Text = "Arrastre:", Dock = DockStyle.Fill, ForeColor = _tema.Tinta2,
            TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0),
            Font = new Font("Segoe UI", 8.25f)
        }, 0, 0);

        _cmbModoArrastre = new ComboBox
        {
            Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 3, 6, 3), Font = new Font("Segoe UI", 8.25f)
        };
        _cmbModoArrastre.Items.AddRange(new object[]
        {
            "Normal",
            "Añadir sin comparar",
            "Comparar (borra diferencias)",
            "Eliminar de los discos",
            "Solo la carpeta",
            "Solo los ficheros"
        });
        _cmbModoArrastre.SelectedIndex = (int)_modo;
        _cmbModoArrastre.SelectedIndexChanged += (_, _) =>
        {
            _modo = (ModoArrastre)Math.Clamp(_cmbModoArrastre.SelectedIndex, 0, 5);
            PistaModo();
        };
        t.Controls.Add(_cmbModoArrastre, 1, 0);

        var acciones = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
            BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
        };

        acciones.Controls.Add(IconoBarra(Icono.Guardar, "Matrices y listas de copia (salvar / cargar)", MenuMatrices));
        acciones.Controls.Add(IconoBarra(Icono.Copiando, "Clonar la lista de escritura del primer disco a los demás", ClonarLista));
        acciones.Controls.Add(IconoBarra(Icono.Externo, "Extraer (expulsar) los discos marcados", ExtraerDiscos));
        acciones.Controls.Add(IconoBarra(Icono.Hecho, "Reparar los discos marcados (chkdsk)", RepararDiscos));
        acciones.Controls.Add(IconoBarra(Icono.Papelera, "Formatear los discos marcados", FormatearDiscos));
        acciones.Controls.Add(IconoBarra(Icono.Prioridad, "Usar el primer disco marcado como lector", UsarComoLector));
        acciones.Controls.Add(IconoBarra(Icono.Lupa, "Buscar archivos en las carpetas raíz", AbrirBuscador));
        acciones.Controls.Add(IconoBarra(Icono.Quitar, "Ocultar los discos marcados (Refrescar no los devuelve)", OcultarMarcados));

        t.Controls.Add(acciones, 2, 0);
        return t;
    }

    /// <summary>Aviso corto de lo que hara el modo elegido.</summary>
    private void PistaModo()
    {
        var texto = _modo switch
        {
            ModoArrastre.Anadir => "Añadir: copia solo lo que falta, no toca lo que ya está.",
            ModoArrastre.Comparar => "Comparar: el primer disco marcado manda y se borran las diferencias.",
            ModoArrastre.Eliminar => "Eliminar: lo que sueltes se BORRA de los discos marcados (pide confirmación).",
            ModoArrastre.SoloCarpeta => "Solo carpeta: copia dentro de la carpeta que contiene los archivos.",
            ModoArrastre.SoloFicheros => "Solo ficheros: los archivos van sueltos, sin carpetas.",
            _ => ""
        };
        if (texto.Length > 0) _lblMensaje.Text = "  " + texto;
    }

    private void VolverAModoNormal()
    {
        _modo = ModoArrastre.Normal;
        if (_cmbModoArrastre != null && _cmbModoArrastre.SelectedIndex != 0)
            _cmbModoArrastre.SelectedIndex = 0;
    }

    /// <summary>
    /// Aplica el modo de arrastre ANTES de anadir origenes. Devuelve false cuando
    /// el modo ya hizo todo (eliminar) y no hay que anadir nada.
    /// </summary>
    private bool PrepararSegunModo(string[] rutas, DeviceInfo? destino)
    {
        if (_modo == ModoArrastre.Eliminar)
        {
            EliminarDeDiscos(rutas, destino);
            return false;
        }
        return true;
    }

    // =====================================================================
    //  Modo ELIMINAR: borra de los discos marcados lo que sueltas
    // =====================================================================

    private void EliminarDeDiscos(string[] rutas, DeviceInfo? destino)
    {
        var discos = destino != null
            ? new List<string> { destino.Root }
            : _destinos.ToList();

        if (discos.Count == 0)
        {
            Mensaje("Para eliminar, marca primero los discos de destino o suelta sobre la fila de un disco.");
            VolverAModoNormal();
            return;
        }

        var lista = string.Join("\n", rutas.Take(12).Select(r => "  · " + r));
        if (rutas.Length > 12) lista += $"\n  … y {rutas.Length - 12} más";

        var r = MessageBox.Show(this,
            $"Vas a BORRAR de {discos.Count} disco(s):\n\n{lista}\n\n" +
            "Esto no se puede deshacer. ¿Seguir?",
            "QBASFreemanCopy-Converter · eliminar de los discos",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

        if (r != DialogResult.Yes) { VolverAModoNormal(); return; }

        var borrados = 0;
        var fallos = 0;
        foreach (var disco in discos)
        {
            foreach (var ruta in rutas)
            {
                try
                {
                    var nombre = Path.GetFileName(ruta.TrimEnd('\\'));
                    var dentro = Path.Combine(disco, nombre);
                    if (Directory.Exists(ruta))
                    {
                        if (Directory.Exists(dentro)) { Directory.Delete(dentro, true); borrados++; }
                    }
                    else if (File.Exists(ruta))
                    {
                        if (File.Exists(dentro)) { File.Delete(dentro); borrados++; }
                    }
                }
                catch { fallos++; }
            }
        }

        Mensaje(fallos == 0
            ? $"Eliminados {borrados} elemento(s) de {discos.Count} disco(s)."
            + (borrados == 0 ? " No había nada con ese nombre en el destino." : "")
            : $"Eliminados {borrados} elemento(s); {fallos} no se pudieron borrar.");
        VolverAModoNormal();
        CargarUnidades(true);
    }

    // =====================================================================
    //  Aplanar segun el modo (solo carpeta / solo ficheros)
    // =====================================================================

    /// <summary>Deja la entrada con la carpeta que la contiene o sin carpeta.</summary>
    private static FileEntry Aplanar(FileEntry f, ModoArrastre modo)
    {
        if (modo == ModoArrastre.Normal) return f;

        // «Solo carpeta»: se queda con la primera carpeta de la ruta relativa
        // (la carpeta que se arrastró), no con toda la ruta del disco.
        var carpeta = modo == ModoArrastre.SoloCarpeta
            ? f.RelativePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? ""
            : "";

        return new FileEntry
        {
            FullPath = f.FullPath,
            RelativePath = string.IsNullOrEmpty(carpeta)
                ? Path.GetFileName(f.FullPath)
                : Path.Combine(carpeta, Path.GetFileName(f.FullPath)),
            Length = f.Length,
            LastWriteUtc = f.LastWriteUtc,
            CreationUtc = f.CreationUtc,
            Attributes = f.Attributes
        };
    }

    // =====================================================================
    //  MATRICES y listas de copia (Salvar / Cargar LD)
    // =====================================================================

    private void MenuMatrices()
    {
        var m = new ContextMenuStrip { Font = new Font("Segoe UI", 9f) };

        m.Items.Add("Guardar esta selección como matriz…", null, (_, _) => GuardarMatrizDialogo());
        m.Items.Add(new ToolStripSeparator());

        if (_matrices.Count == 0)
        {
            var vacio = m.Items.Add("(no hay matrices guardadas)");
            vacio.Enabled = false;
        }
        else
        {
            foreach (var kv in _matrices.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var nombre = kv.Key;
                m.Items.Add($"{nombre}  ({kv.Value.Length} discos)", null, (_, _) => CargarMatriz(nombre));
            }
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Borrar todas las matrices…", null, (_, _) => BorrarMatrices());
        }

        m.Items.Add(new ToolStripSeparator());
        m.Items.Add("Salvar la lista de copia (LD)…", null, (_, _) => SalvarLD());
        m.Items.Add("Cargar una lista de copia (LD)…", null, (_, _) => CargarLD());
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add("Mostrar los discos ocultos", null, (_, _) => MostrarOcultos());

        m.Show(Cursor.Position);
    }

    private void GuardarMatrizDialogo()
    {
        if (_destinos.Count == 0) { Mensaje("No hay discos marcados que guardar."); return; }
        var nombre = PedirTexto("Guardar matriz", "Nombre de la matriz:", $"Matriz {_matrices.Count:00}");
        if (string.IsNullOrWhiteSpace(nombre)) return;

        if (_matrices.Count >= 100 && !_matrices.ContainsKey(nombre))
        {
            Mensaje("Ya hay 100 matrices guardadas: borra alguna antes de crear otra.");
            return;
        }

        _matrices[nombre.Trim()] = _destinos.ToArray();
        GuardarMatrices();
        Mensaje($"Matriz «{nombre.Trim()}» guardada con {_destinos.Count} disco(s).");
    }

    private void CargarMatriz(string nombre)
    {
        if (!_matrices.TryGetValue(nombre, out var discos)) return;
        _destinos.Clear();
        foreach (var d in discos) _destinos.Add(d);
        PintarDestinos();
        GuardarSesion(true);
        Mensaje($"Matriz «{nombre}» cargada: {discos.Length} disco(s).");
    }

    private void BorrarMatrices()
    {
        if (_matrices.Count == 0) return;
        if (MessageBox.Show(this, $"¿Borrar las {_matrices.Count} matrices guardadas?",
                "QBASFreemanCopy-Converter", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        _matrices.Clear();
        GuardarMatrices();
        Mensaje("Matrices borradas.");
    }

    private void CargarMatrices()
    {
        try
        {
            _matrices.Clear();
            if (!File.Exists(FicheroMatrices)) return;
            var doc = JsonDocument.Parse(File.ReadAllText(FicheroMatrices));
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (p.Value.ValueKind != JsonValueKind.Array) continue;
                var lista = p.Value.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString()!)
                    .ToArray();
                _matrices[p.Name] = lista;
            }
        }
        catch { }
    }

    private void GuardarMatrices()
    {
        try
        {
            var json = JsonSerializer.Serialize(_matrices, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FicheroMatrices, json);
        }
        catch { }
    }

    /// <summary>Salva la lista de copia (origenes y como estaban) en un fichero .qbl.</summary>
    private void SalvarLD()
    {
        if (_origenes.Count == 0) { Mensaje("No hay orígenes en la lista de copia."); return; }

        using var dlg = new SaveFileDialog
        {
            Title = "Salvar la lista de copia",
            Filter = "Lista de copia (*.qbl)|*.qbl|Todos los archivos (*.*)|*.*",
            FileName = $"lista-copia-{DateTime.Now:yyyy-MM-dd-HHmm}.qbl"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var json = JsonSerializer.Serialize(new
            {
                programa = "QBASFreemanCopy-Converter",
                tipo = "lista",
                fecha = DateTime.Now.ToString("s"),
                origenes = _origenes.ToArray(),
                prefijos = _prefijosOrigen,
                modos = _modoPorOrigen.ToDictionary(k => k.Key, v => (int)v.Value),
                estructura = _cmbEstructura?.SelectedIndex ?? 4
            }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dlg.FileName, json);
            Mensaje($"Lista salvada en {Path.GetFileName(dlg.FileName)} ({_origenes.Count} origen/es).");
        }
        catch (Exception ex) { Mensaje("No se pudo salvar: " + ex.Message); }
    }

    private void CargarLD()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Cargar una lista de copia",
            Filter = "Lista de copia (*.qbl)|*.qbl|Todos los archivos (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(dlg.FileName));
            var raiz = doc.RootElement;

            var origenes = LeerArray(raiz, "origenes");
            if (origenes.Count == 0) { Mensaje("Ese fichero no tiene orígenes."); return; }

            _origenes.Clear();
            _prefijosOrigen.Clear();
            _modoPorOrigen.Clear();

            foreach (var o in origenes) _origenes.Add(o);

            if (raiz.TryGetProperty("prefijos", out var prefs) && prefs.ValueKind == JsonValueKind.Object)
                foreach (var p in prefs.EnumerateObject())
                    if (p.Value.ValueKind == JsonValueKind.String)
                        _prefijosOrigen[p.Name] = p.Value.GetString() ?? "";

            foreach (var o in origenes)
                if (!_prefijosOrigen.ContainsKey(o))
                    _prefijosOrigen[o] = Directory.Exists(o)
                        ? NombreDeCarpeta(o)
                        : NombreDeCarpeta(Path.GetDirectoryName(o));

            if (raiz.TryGetProperty("modos", out var modos) && modos.ValueKind == JsonValueKind.Object)
                foreach (var p in modos.EnumerateObject())
                    if (p.Value.TryGetInt32(out var im))
                        _modoPorOrigen[p.Name] = (ModoArrastre)Math.Clamp(im, 0, 5);

            if (raiz.TryGetProperty("estructura", out var e) && e.TryGetInt32(out var ie) && _cmbEstructura != null)
                _cmbEstructura.SelectedIndex = Math.Clamp(ie, 0, _cmbEstructura.Items.Count - 1);

            PintarOrigenes();
            GuardarSesion(true);
            Mensaje($"Lista cargada: {origenes.Count} origen/es.");
        }
        catch (Exception ex) { Mensaje("No se pudo cargar: " + ex.Message); }
    }

    // =====================================================================
    //  OCULTAR discos (Refrescar no los devuelve)
    // =====================================================================

    private void CargarOcultos()
    {
        _ocultos.Clear();
        var texto = Opciones.LeerTexto("discosOcultos", "");
        foreach (var x in texto.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            _ocultos.Add(x);
    }

    private void GuardarOcultos()
    {
        Opciones.Guardar("discosOcultos", string.Join("|", _ocultos));
        GuardarAjustes();
    }

    private void OcultarMarcados()
    {
        var lista = _destinos.Count > 0
            ? _destinos.ToList()
            : _unidades.Where(u => _destinos.Contains(u.Root)).Select(u => u.Root).ToList();

        if (lista.Count == 0) { Mensaje("Marca los discos que quieres ocultar."); return; }

        foreach (var r in lista) _ocultos.Add(r);
        GuardarOcultos();
        PintarDiscos();
        Mensaje($"{lista.Count} disco(s) ocultos. «Mostrar ocultos» está en el menú de matrices.");
    }

    private void MostrarOcultos()
    {
        if (_ocultos.Count == 0) { Mensaje("No hay discos ocultos."); return; }
        _ocultos.Clear();
        GuardarOcultos();
        _firmaDiscos = "";
        CargarUnidades(true);
        Mensaje("Discos ocultos restaurados.");
    }

    /// <summary>Oculta UN disco desde el menú de su fila.</summary>
    private void OcultarDisco(DeviceInfo u)
    {
        _ocultos.Add(u.Root);
        _destinos.Remove(u.Root);
        GuardarOcultos();
        PintarDiscos();
        Mensaje($"{u.Display} oculto. Para recuperarlo: menú de matrices → «Mostrar los discos ocultos».");
    }

    // =====================================================================
    //  LECTOR: un disco lee y los demas escriben
    // =====================================================================

    private void UsarComoLector()
    {
        var raiz = _destinos.FirstOrDefault();
        if (raiz == null) { Mensaje("Marca primero el disco que quieres usar como lector."); return; }
        UsarLector(raiz);
    }

    /// <summary>Usa un disco concreto como lector (desde el menú de su fila).</summary>
    private void UsarLectorDisco(DeviceInfo u) => UsarLector(u.Root);

    private void UsarLector(string raiz)
    {
        var u = UnidadDe(raiz);
        _lector = raiz;

        // El lector pasa a ser el origen; los demas marcados, los destinos.
        if (!_origenes.Contains(raiz))
        {
            _origenes.Add(raiz);
            _prefijosOrigen[raiz] = NombreDeCarpeta(raiz);
        }
        _destinos.Remove(raiz);

        PintarOrigenes();
        PintarDiscos();
        GuardarSesion(true);
        Mensaje($"Lector: {(u?.Display ?? raiz)}. Los demás discos marcados escriben desde él.");
    }

    // =====================================================================
    //  CLONAR / EXTRAER / REPARAR / FORMATEAR
    // =====================================================================

    /// <summary>Marca los discos sobre los que actuar: los marcados o, si no hay, el primero.</summary>
    private List<DeviceInfo> DiscosParaAccion(string queHace)
    {
        var lista = _unidades.Where(u => _destinos.Contains(u.Root)).ToList();
        if (lista.Count == 0)
            Mensaje($"Marca primero los discos que quieres {queHace}.");
        return lista;
    }

    private void ClonarLista()
    {
        var marcados = _destinos.ToList();
        if (marcados.Count < 2)
        {
            Mensaje("Clonar necesita al menos dos discos marcados: el primero se copia a los demás.");
            return;
        }
        Clonar(marcados[0], marcados);
    }

    /// <summary>Clona la lista de un disco concreto a los demás discos marcados.</summary>
    private void ClonarDesde(DeviceInfo u)
    {
        var otros = _destinos.Where(d => !string.Equals(d, u.Root, StringComparison.OrdinalIgnoreCase)).ToList();
        if (otros.Count == 0)
        {
            Mensaje("Para clonar este disco, marca además otro(s) disco(s) de destino.");
            return;
        }
        Clonar(u.Root, new[] { u.Root }.Concat(otros).ToList());
    }

    private void Clonar(string primero, List<string> marcados)
    {
        var trabajo = _trabajos.TrabajoDe(primero);
        var ficheros = trabajo?.Pendientes.ToList() ?? new List<FileEntry>();

        // Si ese disco ya no tiene cola, se clona lo que hay preparado como origen.
        if (ficheros.Count == 0)
        {
            if (_origenes.Count == 0) { Mensaje("El primer disco no tiene lista pendiente y no hay orígenes preparados."); return; }
            Mensaje("El primer disco no tiene cola pendiente: se clona la lista de orígenes preparada.");
        }

        var otros = marcados.Where(d => !string.Equals(d, primero, StringComparison.OrdinalIgnoreCase)).ToList();
        var destinos = otros.Select(d => new Destino
        {
            Raiz = d,
            Etiqueta = UnidadDe(d)?.Display ?? d,
            Info = UnidadDe(d)
        }).ToList();

        if (ficheros.Count > 0)
        {
            _trabajos.Enviar(ficheros, destinos);
            Mensaje($"Clonada la lista de {UnidadDe(primero)?.Display ?? primero} ({ficheros.Count} archivos) a {destinos.Count} disco(s).");
        }
        else
        {
            _ = LanzarCopiaPara(destinos);
            Mensaje($"Clonando los orígenes preparados a {destinos.Count} disco(s).");
        }
    }

    private async void ExtraerDiscos()
    {
        var lista = DiscosParaAccion("extraer");
        if (lista.Count == 0) return;

        if (MessageBox.Show(this, $"¿Expulsar {lista.Count} disco(s)?\n\n" +
                string.Join("\n", lista.Select(u => "  · " + u.Display)),
                "Extraer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        var bien = 0;
        var fallos = new List<string>();
        foreach (var u in lista)
        {
            var r = await DeviceActions.ExpulsarAsync(u);
            if (r.Ok) bien++;
            else fallos.Add($"{u.Display}: {r.Mensaje}");
        }

        Mensaje(fallos.Count == 0
            ? $"{bien} disco(s) expulsados."
            : $"Expulsados {bien}; {fallos.Count} con problemas:\n" + string.Join("\n", fallos.Take(6)));
        CargarUnidades(true);
    }

    private async void RepararDiscos()
    {
        var lista = DiscosParaAccion("reparar");
        if (lista.Count == 0) return;

        if (MessageBox.Show(this, $"¿Reparar {lista.Count} disco(s) con chkdsk?\n\n" +
                string.Join("\n", lista.Select(u => "  · " + u.Display)) +
                "\n\nPuede tardar bastante y el disco quedará ocupado mientras tanto.",
                "Reparar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        _lblMensaje.Text = "  Reparando discos… (no cierres el programa)";
        var bien = 0;
        var fallos = new List<string>();
        foreach (var u in lista)
        {
            var r = await DeviceActions.RepararAsync(u);
            if (r.Ok) bien++;
            else fallos.Add($"{u.Display}: {r.Mensaje}");
        }

        Mensaje(fallos.Count == 0
            ? $"{bien} disco(s) revisados y reparados."
            : $"Reparados {bien}; {fallos.Count} con problemas:\n" + string.Join("\n", fallos.Take(6)));
        CargarUnidades(true);
    }

    private async void FormatearDiscos()
    {
        var lista = DiscosParaAccion("formatear");
        if (lista.Count == 0) return;

        var u = lista[0];
        var letra = (u.DriveLetter ?? u.Root).TrimEnd('\\', ':');

        using var dlg = new Form
        {
            Text = "Formatear disco", StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false,
            ClientSize = new Size(400, 230), Font = new Font("Segoe UI", 9f),
            BackColor = _tema.Panel, ForeColor = _tema.Tinta
        };

        dlg.Controls.Add(new Label
        {
            Text = $"Se va a BORRAR TODO el contenido de:\n\n{u.Display}\n" +
                   (lista.Count > 1 ? $"\n(Ojo: además hay {lista.Count - 1} disco(s) marcados, pero solo se formatea el primero.)" : ""),
            Location = new Point(12, 10), Size = new Size(376, 60),
            ForeColor = _tema.Error
        });

        var lblFs = new Label { Text = "Sistema:", Location = new Point(12, 78), AutoSize = true };
        var cmbFs = new ComboBox
        {
            Location = new Point(90, 74), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = _tema.Panel2, ForeColor = _tema.Tinta
        };
        cmbFs.Items.AddRange(new object[] { "exFAT", "NTFS", "FAT32" });
        cmbFs.SelectedIndex = 0;

        var lblEt = new Label { Text = "Etiqueta:", Location = new Point(12, 110), AutoSize = true };
        var txtEt = new TextBox
        {
            Location = new Point(90, 106), Width = 180, Text = u.Label ?? "",
            BackColor = _tema.Panel2, ForeColor = _tema.Tinta, BorderStyle = BorderStyle.FixedSingle
        };

        var chkRapido = new CheckBox
        {
            Text = "Formato rápido", Location = new Point(90, 136), Width = 160,
            Checked = true, ForeColor = _tema.Tinta
        };

        var lblConf = new Label
        {
            Text = $"Para confirmar, escribe la letra ({letra}):", Location = new Point(12, 162), AutoSize = true
        };
        var txtConf = new TextBox
        {
            Location = new Point(230, 158), Width = 60, BackColor = _tema.Panel2,
            ForeColor = _tema.Tinta, BorderStyle = BorderStyle.FixedSingle
        };

        var ok = new Button
        {
            Text = "FORMATEAR", DialogResult = DialogResult.OK, Location = new Point(196, 190), Width = 100,
            FlatStyle = FlatStyle.Flat, BackColor = _tema.Error, ForeColor = Color.White
        };
        var cancel = new Button
        {
            Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(304, 190), Width = 84,
            FlatStyle = FlatStyle.Flat, BackColor = _tema.Panel2, ForeColor = _tema.Tinta
        };

        dlg.Controls.AddRange(new Control[] { lblFs, cmbFs, lblEt, txtEt, chkRapido, lblConf, txtConf, ok, cancel });
        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;

        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (!string.Equals(txtConf.Text.Trim(), letra, StringComparison.OrdinalIgnoreCase))
        {
            Mensaje("La letra no coincide: no se formateó nada.");
            return;
        }

        _lblMensaje.Text = "  Formateando…";
        var res = await DeviceActions.FormatearAsync(u, cmbFs.SelectedItem?.ToString() ?? "exFAT",
            txtEt.Text.Trim(), chkRapido.Checked);
        Mensaje(res.Ok ? $"{u.Display} formateado." : "No se pudo formatear: " + res.Mensaje);
        CargarUnidades(true);
    }

    // =====================================================================
    //  LISTA DE FICHEROS POR ESCRIBIR (doble clic en un disco trabajando)
    // =====================================================================

    private void AbrirListaEscritura(DeviceInfo u)
    {
        var t = _trabajos.TrabajoDe(u.Root);
        if (t == null)
        {
            Mensaje("Ese disco no tiene ninguna lista de escritura. Lanza primero una copia.");
            return;
        }

        using var dlg = new ListaEscritura(_tema, t, _trabajos);
        dlg.ShowDialog(this);
        PintarDiscos();
    }

    // =====================================================================
    //  BUSCADOR: raíces + índice, y añadir lo encontrado a la copia
    // =====================================================================

    private void AbrirBuscador()
    {
        using var dlg = new Buscador(_tema);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var anadidos = 0;
        foreach (var ruta in dlg.Seleccionados)
        {
            if (!File.Exists(ruta)) continue;
            if (_origenes.Contains(ruta)) continue;
            _origenes.Add(ruta);
            _prefijosOrigen[ruta] = NombreDeCarpeta(Path.GetDirectoryName(ruta));
            anadidos++;
        }

        if (anadidos == 0) { Mensaje("No se añadió nada: los archivos ya estaban en la lista o no existen."); return; }

        PintarOrigenes();
        GuardarSesion(true);
        Mensaje($"{anadidos} archivo(s) añadidos a la copia desde el buscador.");
    }
}
