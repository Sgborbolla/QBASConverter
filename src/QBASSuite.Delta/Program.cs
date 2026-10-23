using QBASSuite.Ui;

namespace QBASSuite.Delta;

/// <summary>
/// QBASFreemanCopy-Converter: un solo programa con las dos mitades en una ventana
/// nativa de Windows. Sin navegador, sin servidor y sin puerto: no se abre nada de red.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        }
        catch { }

        Icon? icono = null;
        try
        {
            // Icono del lanzador: combinacion de copiador + convertidor.
            var iconPath = Path.Combine(AppContext.BaseDirectory, "QBASFreemanCopy-Converter (lanzador).ico");
            if (!File.Exists(iconPath))
                iconPath = Path.Combine(AppContext.BaseDirectory, "QBASFreemanCopy-Converter.ico");
            if (File.Exists(iconPath))
                icono = new Icon(iconPath);
        }
        catch { }

        Application.Run(new VentanaDoble(icono));
        return 0;
    }
}
