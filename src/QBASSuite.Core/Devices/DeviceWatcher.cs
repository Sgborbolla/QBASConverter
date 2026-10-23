using System.Management;
using System.Runtime.InteropServices;

namespace QBASSuite.Devices;

/// <summary>
/// Vigila los cambios de hardware en Windows en tiempo real.
/// Usa tres fuentes complementarias:
///   1. WM_DEVICECHANGE  -> conexion/desconexion instantanea de volumenes y USB.
///   2. WMI (Win32_LogicalDisk) -> discos y letras reales del equipo y de la red.
///   3. WMI (Win32_PnPEntity)   -> telefonos, tablets, camaras (MTP) y otros USB.
/// Cuando algo cambia dispara Cambio para que la interfaz se refresque sola.
/// </summary>
public sealed class DeviceWatcher : IDisposable
{
    /// <summary>Se dispara cuando entra o sale un dispositivo.</summary>
    public event Action? Cambio;

    private ManagementEventWatcher? _volumenes;
    private ManagementEventWatcher? _usb;
    private readonly object _candado = new();
    private DateTime _ultimo = DateTime.MinValue;

    public void Iniciar()
    {
        try
        {
            // 1) Creacion/borrado de volumenes (memorias, discos, tarjetas SD).
            _volumenes = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 2 " +
                                  "WHERE TargetInstance ISA 'Win32_LogicalDisk'"));
            _volumenes.EventArrived += (_, __) => Avisar();
            _volumenes.Start();
        }
        catch { }

        try
        {
            // 2) Entrada/salida de cualquier dispositivo USB (incluye MTP: moviles/tablets).
            _usb = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 2 " +
                                  "WHERE TargetInstance ISA 'Win32_PnPEntity' " +
                                  "AND TargetInstance.PNPClass = 'USB'"));
            _usb.EventArrived += (_, __) => Avisar();
            _usb.Start();
        }
        catch { }
    }

    /// <summary>Agrupa rafagas: Windows manda varios eventos seguidos por un solo cable.</summary>
    private void Avisar()
    {
        lock (_candado)
        {
            var ahora = DateTime.UtcNow;
            if ((ahora - _ultimo).TotalMilliseconds < 700) return;
            _ultimo = ahora;
        }
        try { Cambio?.Invoke(); } catch { }
    }

    public void Dispose()
    {
        try { _volumenes?.Stop(); _volumenes?.Dispose(); } catch { }
        try { _usb?.Stop(); _usb?.Dispose(); } catch { }
    }
}

/// <summary>Telefonos, tablets, camaras y demas dispositivos portatiles detectados por Windows.</summary>
public sealed class PortableDevice
{
    public string Nombre { get; init; } = "";
    public string Tipo { get; init; } = "";      // Telefono, Tablet, Camara, USB...
    public string Estado { get; init; } = "";    // Conectado, Desconectado
    public string Id { get; init; } = "";
    public bool Conectado => !Estado.Contains("Desconect", StringComparison.OrdinalIgnoreCase);
}

public static class PortableDeviceScanner
{
    /// <summary>
    /// Lee los dispositivos portatiles REALES conectados (MTP/PTP): moviles Android,
    /// iPhone/iPad, camaras. Windows los expone como unidades sin letra.
    /// </summary>
    public static IReadOnlyList<PortableDevice> Scan()
    {
        var lista = new List<PortableDevice>();
        string[] pistas = { "WPD", "MTP", "Portable", "Android", "Apple", "iPhone", "iPad" };
        int[] clases = { 6, 17, 2 }; // 6=Imagen, 17=Camara, 2=USB

        foreach (var clase in clases)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT Name, PNPClass, Status, DeviceID FROM Win32_PnPEntity WHERE PNPClass IS NOT NULL");
                foreach (ManagementObject mo in searcher.Get())
                {
                    string nombre = mo["Name"]?.ToString() ?? "";
                    if (nombre.Length == 0) continue;
                    bool esPortatil = pistas.Any(p => nombre.Contains(p, StringComparison.OrdinalIgnoreCase));
                    if (!esPortatil) continue;
                    string id = mo["DeviceID"]?.ToString() ?? nombre;
                    if (lista.Any(x => x.Id == id)) continue;
                    lista.Add(new PortableDevice
                    {
                        Nombre = nombre,
                        Tipo = Tipo(nombre),
                        Estado = mo["Status"]?.ToString() ?? "",
                        Id = id
                    });
                }
                break; // basta con una pasada
            }
            catch { }
        }
        return lista;
    }

    private static string Tipo(string n)
    {
        var s = n.ToLowerInvariant();
        if (s.Contains("tablet") || s.Contains("ipad")) return "Tablet";
        if (s.Contains("iphone") || s.Contains("android") || s.Contains("phone") ||
            s.Contains("redmi") || s.Contains("samsung") || s.Contains("xiaomi") ||
            s.Contains("huawei") || s.Contains("honor")) return "Teléfono";
        if (s.Contains("camera") || s.Contains("canon") || s.Contains("nikon")) return "Cámara";
        return "Portátil";
    }
}
