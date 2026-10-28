using QBASSuite.Devices;

namespace QBASSuite.Engine;

/// <summary>En que punto va el trabajo de UN destino.</summary>
public enum TrabajoEstado
{
    EnCola,
    Copiando,
    Pausado,
    Hecho,
    Cancelado,
    Error
}

/// <summary>
/// Regla de prioridad AUTOMATICA entre discos. No para a nadie: solo decide a
/// quien se le da preferencia cuando hay varios copiando a la vez.
/// </summary>
public enum PrioridadAuto
{
    /// <summary>El que menos le queda por copiar.</summary>
    ElQueMenosLeQueda,
    /// <summary>El que va mas rapido.</summary>
    ElMasRapido,
    /// <summary>El que va mas lento, para que no se quede atras.</summary>
    ElMasLento,
    /// <summary>Por orden de la lista, sin reglas.</summary>
    PorOrden,
    /// <summary>Solo cuenta la marca manual con la estrella.</summary>
    SoloElMarcado
}

/// <summary>
/// El trabajo de copia de UN disco. Cada destino tiene el suyo, con su propia
/// cola de ficheros, su estado, su velocidad real medida y su tiempo restante.
/// Esto es lo que hace que el multicopiador sea de verdad: puedes tirar una
/// tanda a unos discos y, mientras siguen copiando, tirar otra a otros discos
/// y esos arrancan a la vez sin frenar a los anteriores.
/// </summary>
public sealed class TrabajoDestino
{
    /// <summary>El destino (raiz, etiqueta e info real del disco).</summary>
    public required Destino Destino { get; init; }

    /// <summary>Ficheros que le quedan por copiar.</summary>
    public List<FileEntry> Pendientes { get; } = new();

    /// <summary>Los que ya se intentaron (copiados o saltados).</summary>
    public List<string> Terminados { get; } = new();

    public TrabajoEstado Estado { get; internal set; } = TrabajoEstado.EnCola;

    /// <summary>True si va antes que los demas (se marca en rojo).</summary>
    public bool Prioridad { get; set; }

    // --- progreso solo de este disco ---
    public long BytesTotal { get; internal set; }
    public long BytesHechos { get; internal set; }
    public long FicherosTotal { get; internal set; }
    public long FicherosHechos { get; internal set; }
    public long Errores { get; internal set; }

    /// <summary>Lo acumulado en tandas anteriores de este mismo disco.</summary>
    internal long BytesSimples { get; set; }
    internal long FicherosSimples { get; set; }

    /// <summary>Bytes/s reales de este disco mientras copia.</summary>
    public double Velocidad { get; internal set; }

    /// <summary>Segundos que le quedan segun su velocidad real (NaN si no se sabe).</summary>
    public double Restante { get; internal set; } = double.NaN;

    /// <summary>Ultimo fichero que toco.</summary>
    public string FicheroActual { get; internal set; } = "";

    /// <summary>Cuando empezo a copiar de verdad (UTC). Se usa para cobrar por duracion.</summary>
    public DateTime Inicio { get; internal set; }

    /// <summary>Cuando termino (UTC), o default si sigue trabajando.</summary>
    public DateTime Fin { get; internal set; }

    /// <summary>Tiempo que lleva copiando (o lo que tardo si ya termino).</summary>
    public TimeSpan Duracion
    {
        get
        {
            if (Inicio == default) return TimeSpan.Zero;
            var fin = Fin == default ? DateTime.UtcNow : Fin;
            var d = fin - Inicio;
            return d < TimeSpan.Zero ? TimeSpan.Zero : d;
        }
    }

    internal CancellationTokenSource? Cts { get; set; }

    /// <summary>0..100 del disco.</summary>
    public double Percent => BytesTotal > 0
        ? Math.Clamp(BytesHechos * 100.0 / BytesTotal, 0, 100)
        : 0;

    /// <summary>Etiqueta visible: nombre del disco o su letra.</summary>
    public string Nombre =>
        string.IsNullOrWhiteSpace(Destino.Etiqueta) ? Destino.Raiz : Destino.Etiqueta;

    public string EstadoTexto => Estado switch
    {
        TrabajoEstado.EnCola => "En cola",
        TrabajoEstado.Copiando => "Copiando",
        TrabajoEstado.Pausado => "Pausado",
        TrabajoEstado.Hecho => Errores > 0 ? $"Hecho con {Errores} error(es)" : "Hecho",
        TrabajoEstado.Cancelado => "Cancelado",
        TrabajoEstado.Error => "Error",
        _ => ""
    };
}
