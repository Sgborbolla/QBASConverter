using System.Text.Json;

namespace QBASSuite.Ui;

/// <summary>
/// El catalogo de SECCIONES de la ventana de Opciones. Cada seccion tiene su
/// pagina, y CADA SECCION se puede APAGAR y ENCENDER entera cuando quieras.
///
/// La lista va desde General hasta Pagos. NO hay Licencia: aqui no se cobra,
/// no caduca y no se bloquea nada.
/// </summary>
public sealed class SeccionOpcion
{
    public required string Id { get; init; }
    public required string Nombre { get; init; }
    public required Icono Icono { get; init; }
    /// <summary>Construye la pagina. El tercer parametro guarda los ajustes.</summary>
    public required Func<Tema, Action, Control> Construir { get; init; }

    public static IReadOnlyList<SeccionOpcion> Todas { get; } = new List<SeccionOpcion>
    {
        new()
        {
            Id = "general", Nombre = "General", Icono = Icono.Ajustes,
            Construir = (t, guardar) =>
            {
                var p = Opciones.Pila(t);
                p.Controls.Add(VentanaOpciones.Titulo(t, "GENERAL"));
                p.Controls.Add(VentanaOpciones.Nota(t, "Lo basico del programa. Enciende y apaga cuando quieras: cada interruptor se guarda al momento."));
                p.Controls.Add(SeccionActiva(t, "general", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "mantenerEstructura", "Mantener la estructura de carpetas al copiar", "Copia las subcarpetas tal como estan en el origen.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "crearRegistro", "Crear un registro (.txt) de la copia", "Deja un fichero de texto con lo que se ha copiado.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "autoindexar", "Indexar los discos al arrancar", "Al abrir, cuenta los ficheros de los discos para tenerlos listos.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "mostrarPuertos", "Mostrar los puertos USB", "Ensena por que puerto va cada dispositivo.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "detectarVelocidadUsb", "Detectar la velocidad de los puertos USB", "Mide si el puerto es rapido o lento.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "facturaCopia", "Agregar la factura de copia", "Suma los GB copiados y calcula el precio, si has puesto precio por GB.", guardar));
                return p;
            }
        },
        new()
        {
            Id = "dispositivos", Nombre = "Dispositivos", Icono = Icono.DiscoDuro,
            Construir = (t, guardar) =>
            {
                var p = Opciones.Pila(t);
                p.Controls.Add(VentanaOpciones.Titulo(t, "DISPOSITIVOS"));
                p.Controls.Add(VentanaOpciones.Nota(t, "Que discos y memorias se miran, y que columnas se ensenan de cada uno."));
                p.Controls.Add(SeccionActiva(t, "dispositivos", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "verDiscosInternos", "Mirar los discos internos", "Los discos fijos del equipo.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "verDiscosExternos", "Mirar los discos externos y USB", "Los que se conectan por USB.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "verRed", "Mirar las unidades de red", "Carpetas compartidas y servidores.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "verOpticos", "Mirar las unidades opticas", "Lector de CD y DVD.", guardar));
                p.Controls.Add(VentanaOpciones.Titulo(t, "COLUMNAS QUE SE ENSENAN"));
                p.Controls.Add(Opciones.Interruptor(t, "mostrarLetra", "Mostrar la letra de la unidad", "El (C:), (D:)...", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "mostrarSistemaArchivos", "Mostrar el sistema de archivos", "NTFS, FAT32...", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "mostrarLibre", "Mostrar el espacio usado y total", "El \"X de Y\".", guardar));
                return p;
            }
        },
        new()
        {
            Id = "procesos", Nombre = "Información de Procesos", Icono = Icono.Copiando,
            Construir = (t, guardar) =>
            {
                var p = Opciones.Pila(t);
                p.Controls.Add(VentanaOpciones.Titulo(t, "INFORMACION DE PROCESOS"));
                p.Controls.Add(VentanaOpciones.Nota(t, "Que datos del trabajo en curso quieres ver en cada disco."));
                p.Controls.Add(SeccionActiva(t, "procesos", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "mostrarTotalFicheros", "Mostrar el total de ficheros", "Cuantos ficheros hay que copiar.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "mostrarCompletados", "Mostrar los ficheros completados", "Cuantos van hechos.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "mostrarTotalProcesar", "Mostrar el total a procesar", "El tamano total previsto.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "mostrarProcesado", "Mostrar lo procesado", "El tamano ya copiado.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "mostrarRam", "Mostrar la RAM usada", "Cuanta memoria esta gastando.", guardar));
                return p;
            }
        },
        new()
        {
            Id = "discosred", Nombre = "Discos y Red", Icono = Icono.Red,
            Construir = (t, guardar) =>
            {
                var p = Opciones.Pila(t);
                p.Controls.Add(VentanaOpciones.Titulo(t, "DISCOS Y RED"));
                p.Controls.Add(VentanaOpciones.Nota(t, "Como se reparte el trabajo entre los discos. Todo reversible."));
                p.Controls.Add(SeccionActiva(t, "discosred", guardar));
                p.Controls.Add(VentanaOpciones.Titulo(t, "PRIORIDAD AUTOMATICA"));
                p.Controls.Add(Opciones.Desplegable(t, "prioridadAuto",
                    "Cuando varios discos copian a la vez, quien va primero:",
                    new[] { "El que menos le queda por copiar", "El mas rapido", "El mas lento", "Por orden de la lista", "Solo el que yo marque con ★" }, guardar));
                p.Controls.Add(Opciones.Interruptor(t, "forzarCopia", "Forzar la copia", "Copia aunque el fichero parezca ya copiado.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "velocidadAdaptativa", "Adaptar la velocidad a cada disco", "Cada disco a su tope real (SSD, USB, red...).", guardar));
                return p;
            }
        },
        new()
        {
            Id = "memorias", Nombre = "Memorias", Icono = Icono.Usb,
            Construir = (t, guardar) =>
            {
                var p = Opciones.Pila(t);
                p.Controls.Add(VentanaOpciones.Titulo(t, "MEMORIAS"));
                p.Controls.Add(VentanaOpciones.Nota(t, "Opciones para memorias USB y tarjetas."));
                p.Controls.Add(SeccionActiva(t, "memorias", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "memForzarCopia", "Forzar la copia", "Copia aunque parezca repetido.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "memFicheros0Kb", "Crear ficheros de 0 KB", "Deja el hueco del fichero vacio para rellenarlo despues.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "memRegistroInicial", "Crear un registro inicial", "Anota en un texto por donde empezo.", guardar));
                return p;
            }
        },
        new()
        {
            Id = "celulares", Nombre = "Celulares", Icono = Icono.Telefono,
            Construir = (t, guardar) =>
            {
                var p = Opciones.Pila(t);
                p.Controls.Add(VentanaOpciones.Titulo(t, "CELULARES"));
                p.Controls.Add(VentanaOpciones.Nota(t, "Para telefonos y dispositivos conectados por cable."));
                p.Controls.Add(SeccionActiva(t, "celulares", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "celForzarCopia", "Forzar la copia", "Copia aunque parezca repetido.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "celIniciarPausado", "Empezar en pausa", "Se conecta pero no copia hasta que tu digas.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "celDetectar", "Detectar celulares", "Mira si hay telefonos conectados.", guardar));
                return p;
            }
        },
        new()
        {
            Id = "ram", Nombre = "Ram (Discos, Red)", Icono = Icono.Ssd,
            Construir = (t, guardar) =>
            {
                var p = Opciones.Pila(t);
                p.Controls.Add(VentanaOpciones.Titulo(t, "RAM (DISCOS, RED)"));
                p.Controls.Add(VentanaOpciones.Nota(t, "Cuanta memoria puede gastar cada copia. Si no estas seguro, dejalo como esta."));
                p.Controls.Add(SeccionActiva(t, "ram", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "limitarRam", "Limitar la RAM por proceso", "Pone un tope de memoria a cada copia.", guardar));
                p.Controls.Add(Opciones.Rueda(t, "ramLibreMb", "Memoria libre que se deja al sistema", "En MB.", 128, 1, 4096, 128, guardar));
                p.Controls.Add(Opciones.Rueda(t, "ramProcesoMb", "Memoria por copia", "En MB.", 256, 1, 4096, 128, guardar));
                p.Controls.Add(Opciones.Rueda(t, "ramExtraMb", "Memoria extra por copia", "En MB.", 128, 1, 2048, 64, guardar));
                return p;
            }
        },
        new()
        {
            Id = "visual", Nombre = "Visual", Icono = Icono.Imagen,
            Construir = (t, guardar) =>
            {
                var p = Opciones.Pila(t);
                p.Controls.Add(VentanaOpciones.Titulo(t, "VISUAL"));
                p.Controls.Add(VentanaOpciones.Nota(t, "El aspecto. Aqui no hay blur ni imagenes de fondo: colores limpios y nuestro logo."));
                p.Controls.Add(SeccionActiva(t, "visual", guardar));
                p.Controls.Add(Opciones.Desplegable(t, "temaVisual", "Tema de color", Tema.Todos.Select(x => x.Nombre).ToArray(), guardar));
                p.Controls.Add(Opciones.Rueda(t, "fuentePrincipal", "Tamano de la letra principal", "Puntos.", 9, 7, 20, 1, guardar));
                p.Controls.Add(Opciones.Rueda(t, "tamIconos", "Tamano de los iconos", "Pixeles.", 20, 16, 40, 2, guardar));
                p.Controls.Add(Opciones.Interruptor(t, "mostrarLineas", "Marcar con una linea el disco elegido", "", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "mostrarTiempoFinal", "Mostrar el tiempo final", "Cuando acaba, cuanto tardo.", guardar));
                return p;
            }
        },
        new()
        {
            Id = "buscador", Nombre = "Buscador", Icono = Icono.Lupa,
            Construir = (t, guardar) =>
            {
                var p = Opciones.Pila(t);
                p.Controls.Add(VentanaOpciones.Titulo(t, "BUSCADOR"));
                p.Controls.Add(VentanaOpciones.Nota(t, "Como busca el cuadro de busqueda."));
                p.Controls.Add(SeccionActiva(t, "buscador", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "ocultarRutas", "Ocultar las rutas largas en los resultados", "Ensenia solo el nombre del fichero.", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "buscarContenido", "Buscar tambien dentro de las carpetas", "Mira en las subcarpetas.", guardar));
                return p;
            }
        },
        new()
        {
            Id = "indexado", Nombre = "Indexado", Icono = Icono.Carpeta,
            Construir = (t, guardar) =>
            {
                var p = Opciones.Pila(t);
                p.Controls.Add(VentanaOpciones.Titulo(t, "INDEXADO"));
                p.Controls.Add(VentanaOpciones.Nota(t, "Contar los ficheros de tus carpetas para copiar mas rapido despues."));
                p.Controls.Add(SeccionActiva(t, "indexado", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "indexarAlContinuar", "Continuar el indexado que quedo a medias", "", guardar));
                p.Controls.Add(Opciones.Interruptor(t, "respetarExtensiones", "Respetar las extensiones de la carpeta", "", guardar));
                p.Controls.Add(Opciones.Rueda(t, "autoIndexarMin", "Auto indexar cada... (minutos)", "0 = nunca.", 0, 0, 240, 5, guardar));
                p.Controls.Add(VentanaOpciones.Titulo(t, "CARPETAS A INDEXAR"));
                p.Controls.Add(Opciones.Nota(t, "  (Puedes anadirlas desde el explorador; aqui se ven las que hay.)"));
                p.Controls.Add(Opciones.ListaRutas(t, "rutasIndexar"));
                return p;
            }
        },
        new()
        {
            Id = "pagos", Nombre = "Pagos", Icono = Icono.Hecho,
            Construir = (t, guardar) =>
            {
                var p = Opciones.Pila(t);
                p.Controls.Add(VentanaOpciones.Titulo(t, "PAGOS"));
                p.Controls.Add(VentanaOpciones.Nota(t,
                    "Esto NO cobra nada ni bloquea nada: es una CALCULADORA local para que tu sepas lo que vale " +
                    "el trabajo que has copiado. Se calcula sobre los GB copiados de verdad, y cada disco redondea " +
                    "su importe a 2 decimales antes de sumarse."));
                p.Controls.Add(SeccionActiva(t, "pagos", guardar));
                p.Controls.Add(VentanaOpciones.Nota(t, "El interruptor de abajo es el MISMO que el del multicopiador: lo que enciendas aqui se ve alli, y al reves."));
                p.Controls.Add(Opciones.Interruptor(t, "precioActivo", "Activar el precio por GB", "Ensenar un importe por disco y el total. Apagalo y desaparece.", guardar));
                p.Controls.Add(Opciones.Desplegable(t, "pagoCalcular",
                    "Sobre que se calcula:",
                    new[] { "Por GB copiado de verdad", "Por el tamano total previsto" }, guardar));
                p.Controls.Add(Opciones.Caja(t, "precioPorGb", "Precio por GB ($)", "0.50", guardar));
                p.Controls.Add(Opciones.Desplegable(t, "pagoRedondeo",
                    "Redondeo:",
                    new[] { "Redondear cada disco a 2 decimales", "Redondear solo el total al final", "Redondear a multiplos (0,5,10...)" }, guardar));
                p.Controls.Add(Opciones.Desplegable(t, "pagoMoneda", "Moneda:", new[] { "$ dolar", "€ euro" }, guardar));
                p.Controls.Add(VentanaOpciones.Nota(t, "Recuerda: sin licencia, sin caducidad y sin pagos obligatorios. Aqui nadie te cobra."));
                return p;
            }
        }
    };

    public static SeccionOpcion? PorId(string id) =>
        Todas.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Casilla que apaga/enciende la SECCION entera.</summary>
    internal static Control SeccionActiva(Tema t, string id, Action guardar) =>
        Opciones.Interruptor(t, "seccion_" + id, "Activar esta sección", "Apagala y esta parte del programa queda desactivada. Vuelve a encenderla cuando quieras.", guardar);
}
