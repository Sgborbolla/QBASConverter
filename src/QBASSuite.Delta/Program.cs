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

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Avisar(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Avisar(e.ExceptionObject as Exception);

        try
        {
            // Atajo para revisar el diseno de Opciones sin abrir la ventana principal.
            if (args.Any(a => string.Equals(a, "--opciones", StringComparison.OrdinalIgnoreCase)))
            {
                var seccion = args.SkipWhile(a => !string.Equals(a, "--opciones", StringComparison.OrdinalIgnoreCase))
                                  .Skip(1).FirstOrDefault() ?? "general";
                Application.Run(new VentanaOpciones(Tema.PorDefecto, () => { }, seccion));
                return 0;
            }

            Application.Run(new VentanaDoble(icono));
        }
        catch (Exception ex)
        {
            Avisar(ex);
        }
        return 0;
    }

    /// <summary>
    /// Si algo falla, se ve un mensaje claro en vez de una ventana vacia o negra.
    /// </summary>
    private static void Avisar(Exception? ex)
    {
        try
        {
            MessageBox.Show(
                "QBASFreemanCopy-Converter no pudo arrancar:\n\n" + (ex?.ToString() ?? "error desconocido"),
                "QBASFreemanCopy-Converter",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch { }
    }
}
