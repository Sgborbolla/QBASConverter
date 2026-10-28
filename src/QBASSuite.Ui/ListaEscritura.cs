using QBASSuite.Engine;
using QBASSuite.Util;

namespace QBASSuite.Ui;

/// <summary>
/// La lista de ficheros que le quedan por escribir a UN disco. Se abre con doble
/// clic en un disco que esta trabajando (o desde su menú). Se puede quitar un
/// fichero concreto con Ctrl+clic o con Supr: lo que se quita YA NO se copia.
///
/// Los que ya se han copiado se quedan abajo, en verde, solo para saber por
/// donde va; esos no se pueden quitar.
/// </summary>
public sealed class ListaEscritura : Form
{
    private readonly Tema _tema;
    private readonly TrabajoDestino _trabajo;
    private readonly CopyJobsManager _trabajos;
    private readonly ListaOscura _lista;
    private readonly Label _lblCabecera;
    private readonly Label _lblPie;

    private string _firma = "";

    public ListaEscritura(Tema tema, TrabajoDestino trabajo, CopyJobsManager trabajos)
    {
        _tema = tema;
        _trabajo = trabajo;
        _trabajos = trabajos;

        Text = "Ficheros por escribir · " + trabajo.Nombre;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(840, 460);
        MinimumSize = new Size(640, 360);
        BackColor = _tema.Panel;
        ForeColor = _tema.Tinta;
        Font = new Font("Segoe UI", 9f);
        ShowInTaskbar = false;

        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            BackColor = _tema.Panel, Padding = new Padding(10)
        };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        _lblCabecera = new Label
        {
            Dock = DockStyle.Fill, ForeColor = _tema.Acento,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };
        raiz.Controls.Add(_lblCabecera, 0, 0);

        _lista = new ListaOscura(_tema)
        {
            Dock = DockStyle.Fill, FondoFilas = _tema.Panel2, Font = new Font("Segoe UI", 9f)
        };
        _lista.Columns.Add("Archivo", 330);
        _lista.Columns.Add("Carpeta", 290);
        _lista.Columns.Add("Tamaño", 100, HorizontalAlignment.Right);
        _lista.Columns.Add("Estado", 90, HorizontalAlignment.Right);
        _lista.MultiSelect = true;
        _lista.MouseDown += (_, e) =>
        {
            // Ctrl+clic quita ESE fichero de la lista, igual que en el navegador.
            if ((Control.ModifierKeys & Keys.Control) == 0) return;
            var fila = _lista.GetItemAt(e.X, e.Y);
            if (fila != null) Quitar(new[] { fila });
        };
        _lista.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Delete) return;
            e.Handled = true;
            Quitar(_lista.SelectedItems.Cast<ListViewItem>());
        };
        raiz.Controls.Add(_lista, 0, 1);

        _lblPie = new Label
        {
            Dock = DockStyle.Fill, ForeColor = _tema.Tinta2,
            Font = new Font("Segoe UI", 8.25f), TextAlign = ContentAlignment.MiddleLeft,
            Text = "  Ctrl+clic o Supr quita un fichero de la lista: ese fichero no se copiará."
        };
        raiz.Controls.Add(_lblPie, 0, 2);

        var botones = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0)
        };

        var bCerrar = new BotonRedondo(_tema)
        {
            Text = "Cerrar", Width = 96, Height = 30, Radio = 4,
            DialogResult = DialogResult.Cancel, Margin = new Padding(6, 4, 0, 0)
        };
        var bQuitar = new BotonRedondo(_tema)
        {
            Text = "Quitar seleccionados", Width = 168, Height = 30, Radio = 4,
            Margin = new Padding(6, 4, 0, 0)
        };
        bQuitar.Click += (_, _) => Quitar(_lista.SelectedItems.Cast<ListViewItem>());

        botones.Controls.Add(bCerrar);
        botones.Controls.Add(bQuitar);
        raiz.Controls.Add(botones, 0, 3);

        Controls.Add(raiz);
        CancelButton = bCerrar;

        var reloj = new System.Windows.Forms.Timer { Interval = 900 };
        reloj.Tick += (_, _) => Refrescar();
        reloj.Start();
        FormClosed += (_, _) => { try { reloj.Stop(); reloj.Dispose(); } catch { } };

        Refrescar();
    }

    private void Refrescar()
    {
        var pendientes = _trabajos.PendientesDe(_trabajo);
        var hechos = _trabajos.TerminadosDe(_trabajo);

        _lblCabecera.Text = $"{_trabajo.Nombre}   ·   {_trabajo.EstadoTexto}   ·   " +
                            $"{_trabajo.Percent:0}%   ·   {_trabajo.FicherosHechos}/{_trabajo.FicherosTotal} ficheros" +
                            (string.IsNullOrWhiteSpace(_trabajo.FicheroActual) ? "" : $"   ·   ahora: {_trabajo.FicheroActual}");

        // Solo se reconstruye la lista si de verdad cambio algo.
        var firma = $"{pendientes.Count}|{hechos.Count}|{_trabajo.Estado}";
        if (firma == _firma) return;
        _firma = firma;

        var seleccion = _lista.SelectedItems.Cast<ListViewItem>()
            .Select(i => i.Tag as string ?? "")
            .Where(s => s.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _lista.BeginUpdate();
        try
        {
            _lista.Items.Clear();

            foreach (var f in pendientes)
            {
                var fila = new ListViewItem(Path.GetFileName(f.FullPath)) { Tag = f.FullPath };
                fila.SubItems.Add(Path.GetDirectoryName(f.RelativePath) ?? "");
                fila.SubItems.Add(Human.Size(f.Length));
                fila.SubItems.Add("Pendiente");
                fila.ForeColor = _tema.Tinta;
                _lista.Items.Add(fila);
            }

            foreach (var ruta in hechos)
            {
                var fila = new ListViewItem(Path.GetFileName(ruta)) { Tag = null };
                fila.SubItems.Add(Path.GetDirectoryName(ruta) ?? "");
                fila.SubItems.Add("");
                fila.SubItems.Add("Hecho");
                fila.ForeColor = _tema.Hecho;
                _lista.Items.Add(fila);
            }

            foreach (ListViewItem fila in _lista.Items)
                if (fila.Tag is string ruta && seleccion.Contains(ruta))
                    fila.Selected = true;
        }
        finally { _lista.EndUpdate(); }
    }

    private void Quitar(IEnumerable<ListViewItem> filas)
    {
        var rutas = filas.Select(f => f.Tag as string)
                         .Where(s => !string.IsNullOrEmpty(s))
                         .Cast<string>()
                         .ToList();

        if (rutas.Count == 0)
        {
            _lblPie.Text = "  Solo se pueden quitar los que aún están pendientes (los verdes ya se copiaron).";
            return;
        }

        var quitados = _trabajos.QuitarPendientes(_trabajo, rutas);
        _firma = "";
        Refrescar();
        _lblPie.Text = $"  {quitados} fichero(s) quitados de la lista: no se copiarán.";
    }
}
