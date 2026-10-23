using System.Text.Json;
using System.Text.Json.Serialization;

namespace QBASSuite.Util;

/// <summary>
/// Sistema de skins/pieles para QBAS Suite.
/// Permite cambiar el tema visual con personajes famosos, anime, películas, etc.
/// Los skins se guardan en %AppData%\QBAS Suite\skins.json
/// </summary>
public static class SkinManager
{
    private static readonly string SkinsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "QBAS Suite", 
        "skins");
    
    private static readonly string SkinsConfig = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QBAS Suite",
        "skins.json");
    
    public static List<Skin> Skins { get; private set; } = new();
    public static string ActiveSkinId { get; set; } = "default";
    
    static SkinManager()
    {
        Directory.CreateDirectory(SkinsDir);
        CargarSkins();
    }
    
    public static void CargarSkins()
    {
        Skins.Clear();
        
        // Skin por defecto
        Skins.Add(new Skin
        {
            Id = "default",
            Nombre = "QBAS Clásico",
            Descripcion = "Tema clásico azul/celeste",
            Categoria = "Clásico",
            EsPorDefecto = true,
            Colores = new SkinColors
            {
                Fondo = "#0B1626",
                Tarjeta = "#15233B",
                Borde = "#243452",
                Texto = "#FFFFFF",
                TextoSecundario = "#7A8AA6",
                Acento = "#41A5F5",
                AcentoFuerte = "#0E4FA6",
                Exito = "#2ECC71",
                Aviso = "#F5B041",
                Error = "#C8102E",
                Progreso = "#41A5F5"
            },
            Efectos = new SkinEffects
            {
                Brillo = 0,
                Particulas = false,
                Animado = false
            }
        });
        
        // Skins con personajes famosos
        Skins.Add(new Skin
        {
            Id = "goku",
            Nombre = "Son Goku",
            Descripcion = "Dragon Ball - Kamehameha",
            Categoria = "Anime",
            Colores = new SkinColors
            {
                Fondo = "#1a0e0e",
                Tarjeta = "#2d1810",
                Borde = "#ff6b35",
                Texto = "#fff4e6",
                TextoSecundario = "#ffa94d",
                Acento = "#ff6b35",
                AcentoFuerte = "#d9480f",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#ff6b6b",
                Progreso = "#ff6b35"
            },
            ImagenFondo = "goku_bg.jpg",
            Icono = "goku_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 15,
                Particulas = true,
                ParticulasTipo = "fuego",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "vegeta",
            Nombre = "Vegeta",
            Descripcion = "Dragon Ball - Príncipe Saiyano",
            Categoria = "Anime",
            Colores = new SkinColors
            {
                Fondo = "#0a0a1a",
                Tarjeta = "#15152d",
                Borde = "#4dabf7",
                Texto = "#e7f5ff",
                TextoSecundario = "#74c476",
                Acento = "#4dabf7",
                AcentoFuerte = "#228be6",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#ff6b6b",
                Progreso = "#4dabf7"
            },
            ImagenFondo = "vegeta_bg.jpg",
            Icono = "vegeta_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 20,
                Particulas = true,
                ParticulasTipo = "electricidad",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "naruto",
            Nombre = "Naruto Uzumaki",
            Descripcion = "Naruto Shippuden",
            Categoria = "Anime",
            Colores = new SkinColors
            {
                Fondo = "#1a1408",
                Tarjeta = "#2d2510",
                Borde = "#ffa94d",
                Texto = "#fff9db",
                TextoSecundario = "#fab005",
                Acento = "#ffa94d",
                AcentoFuerte = "#fd7e14",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#ff6b6b",
                Progreso = "#ffa94d"
            },
            ImagenFondo = "naruto_bg.jpg",
            Icono = "naruto_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 12,
                Particulas = true,
                ParticulasTipo = "chakra",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "luffy",
            Nombre = "Monkey D. Luffy",
            Descripcion = "One Piece - Rey de los Piratas",
            Categoria = "Anime",
            Colores = new SkinColors
            {
                Fondo = "#0f1a0f",
                Tarjeta = "#1a2d1a",
                Borde = "#51cf66",
                Texto = "#f4fff4",
                TextoSecundario = "#69db7c",
                Acento = "#51cf66",
                AcentoFuerte = "#40c057",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#ff6b6b",
                Progreso = "#51cf66"
            },
            ImagenFondo = "luffy_bg.jpg",
            Icono = "luffy_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 10,
                Particulas = true,
                ParticulasTipo = "sol",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "ichigo",
            Nombre = "Ichigo Kurosaki",
            Descripcion = "Bleach - Shinigami Sustituto",
            Categoria = "Anime",
            Colores = new SkinColors
            {
                Fondo = "#0a0a0a",
                Tarjeta = "#1a1a1a",
                Borde = "#868e96",
                Texto = "#f8f9fa",
                TextoSecundario = "#adb5bd",
                Acento = "#fab005",
                AcentoFuerte = "#f59f00",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#fa5252",
                Progreso = "#fab005"
            },
            ImagenFondo = "ichigo_bg.jpg",
            Icono = "ichigo_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 18,
                Particulas = true,
                ParticulasTipo = "reiryoku",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "itachi",
            Nombre = "Itachi Uchiha",
            Descripcion = "Naruto - Mangekyō Sharingan",
            Categoria = "Anime",
            Colores = new SkinColors
            {
                Fondo = "#0a0000",
                Tarjeta = "#1a0000",
                Borde = "#fa5252",
                Texto = "#fff5f5",
                TextoSecundario = "#ffc9c9",
                Acento = "#fa5252",
                AcentoFuerte = "#e03131",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#ff6b6b",
                Progreso = "#fa5252"
            },
            ImagenFondo = "itachi_bg.jpg",
            Icono = "itachi_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 25,
                Particulas = true,
                ParticulasTipo = "amaterasu",
                Animado = true
            }
        });
        
        // Películas
        Skins.Add(new Skin
        {
            Id = "terminator",
            Nombre = "Terminator",
            Descripcion = "T-800 - Hasta la vista, baby",
            Categoria = "Películas",
            Colores = new SkinColors
            {
                Fondo = "#050505",
                Tarjeta = "#101010",
                Borde = "#868e96",
                Texto = "#f1f3f5",
                TextoSecundario = "#ced4da",
                Acento = "#fab005",
                AcentoFuerte = "#f59f00",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#fa5252",
                Progreso = "#fab005"
            },
            ImagenFondo = "terminator_bg.jpg",
            Icono = "terminator_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 30,
                Particulas = true,
                ParticulasTipo = "chispas",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "robocop",
            Nombre = "RoboCop",
            Descripcion = "Detroit - Ley y Orden",
            Categoria = "Películas",
            Colores = new SkinColors
            {
                Fondo = "#0a0f1a",
                Tarjeta = "#101820",
                Borde = "#339af0",
                Texto = "#e7f5ff",
                TextoSecundario = "#74c0fc",
                Acento = "#339af0",
                AcentoFuerte = "#228be6",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#fa5252",
                Progreso = "#339af0"
            },
            ImagenFondo = "robocop_bg.jpg",
            Icono = "robocop_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 22,
                Particulas = true,
                ParticulasTipo = "circuitos",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "johnwick",
            Nombre = "John Wick",
            Descripcion = "Baba Yaga",
            Categoria = "Películas",
            Colores = new SkinColors
            {
                Fondo = "#0a0a0a",
                Tarjeta = "#141414",
                Borde = "#495057",
                Texto = "#f8f9fa",
                TextoSecundario = "#adb5bd",
                Acento = "#fab005",
                AcentoFuerte = "#f59f00",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#fa5252",
                Progreso = "#fab005"
            },
            ImagenFondo = "johnwick_bg.jpg",
            Icono = "johnwick_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 15,
                Particulas = false,
                Animado = false
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "spiderman",
            Nombre = "Spider-Man",
            Descripcion = "Un gran poder conlleva una gran responsabilidad",
            Categoria = "Superhéroes",
            Colores = new SkinColors
            {
                Fondo = "#0a0000",
                Tarjeta = "#1a0000",
                Borde = "#ff6b6b",
                Texto = "#fff5f5",
                TextoSecundario = "#ffc9c9",
                Acento = "#ff6b6b",
                AcentoFuerte = "#fa5252",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#ff6b6b",
                Progreso = "#ff6b6b"
            },
            ImagenFondo = "spiderman_bg.jpg",
            Icono = "spiderman_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 14,
                Particulas = true,
                ParticulasTipo = "telaraña",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "batman",
            Nombre = "Batman",
            Descripcion = "El Caballero Oscuro",
            Categoria = "Superhéroes",
            Colores = new SkinColors
            {
                Fondo = "#000000",
                Tarjeta = "#0f0f0f",
                Borde = "#495057",
                Texto = "#f8f9fa",
                TextoSecundario = "#adb5bd",
                Acento = "#fab005",
                AcentoFuerte = "#f59f00",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#fa5252",
                Progreso = "#fab005"
            },
            ImagenFondo = "batman_bg.jpg",
            Icono = "batman_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 35,
                Particulas = true,
                ParticulasTipo = "murciélagos",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "deadpool",
            Nombre = "Deadpool",
            Descripcion = "Máximo esfuerzo",
            Categoria = "Superhéroes",
            Colores = new SkinColors
            {
                Fondo = "#1a0000",
                Tarjeta = "#2d0000",
                Borde = "#ff6b6b",
                Texto = "#fff5f5",
                TextoSecundario = "#ffc9c9",
                Acento = "#ff6b6b",
                AcentoFuerte = "#fa5252",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#ff6b6b",
                Progreso = "#ff6b6b"
            },
            ImagenFondo = "deadpool_bg.jpg",
            Icono = "deadpool_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 16,
                Particulas = true,
                ParticulasTipo = "balas",
                Animado = true
            }
        });
        
        // Videojuegos
        Skins.Add(new Skin
        {
            Id = "sonic",
            Nombre = "Sonic",
            Descripcion = "Gotta go fast!",
            Categoria = "Videojuegos",
            Colores = new SkinColors
            {
                Fondo = "#000814",
                Tarjeta = "#001d3d",
                Borde = "#00d9ff",
                Texto = "#e0fbfc",
                TextoSecundario = "#90e0ef",
                Acento = "#00d9ff",
                AcentoFuerte = "#0096c7",
                Exito = "#80ffdb",
                Aviso = "#ffd60a",
                Error = "#ff6b6b",
                Progreso = "#00d9ff"
            },
            ImagenFondo = "sonic_bg.jpg",
            Icono = "sonic_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 20,
                Particulas = true,
                ParticulasTipo = "estrellas",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "mario",
            Nombre = "Super Mario",
            Descripcion = "It's-a me, Mario!",
            Categoria = "Videojuegos",
            Colores = new SkinColors
            {
                Fondo = "#1a0e00",
                Tarjeta = "#2d1b00",
                Borde = "#ff6b6b",
                Texto = "#fff4e6",
                TextoSecundario = "#ffa94d",
                Acento = "#ff6b6b",
                AcentoFuerte = "#fa5252",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#ff6b6b",
                Progreso = "#ff6b6b"
            },
            ImagenFondo = "mario_bg.jpg",
            Icono = "mario_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 12,
                Particulas = true,
                ParticulasTipo = "monedas",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "kratos",
            Nombre = "Kratos",
            Descripcion = "God of War",
            Categoria = "Videojuegos",
            Colores = new SkinColors
            {
                Fondo = "#0a0000",
                Tarjeta = "#150000",
                Borde = "#fa5252",
                Texto = "#fff5f5",
                TextoSecundario = "#ffc9c9",
                Acento = "#fa5252",
                AcentoFuerte = "#e03131",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#ff6b6b",
                Progreso = "#fa5252"
            },
            ImagenFondo = "kratos_bg.jpg",
            Icono = "kratos_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 28,
                Particulas = true,
                ParticulasTipo = "sangre",
                Animado = true
            }
        });
        
        // Más anime/películas
        Skins.Add(new Skin
        {
            Id = "onepiece_law",
            Nombre = "Trafalgar Law",
            Descripcion = "One Piece - Surgeon of Death",
            Categoria = "Anime",
            Colores = new SkinColors
            {
                Fondo = "#0a0a1a",
                Tarjeta = "#15152d",
                Borde = "#fab005",
                Texto = "#fff9db",
                TextoSecundario = "#ffe066",
                Acento = "#fab005",
                AcentoFuerte = "#f59f00",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#fa5252",
                Progreso = "#fab005"
            },
            ImagenFondo = "law_bg.jpg",
            Icono = "law_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 18,
                Particulas = true,
                ParticulasTipo = "room",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "demon_slayer",
            Nombre = "Tanjiro Kamado",
            Descripcion = "Kimetsu no Yaiba",
            Categoria = "Anime",
            Colores = new SkinColors
            {
                Fondo = "#0a1a0a",
                Tarjeta = "#152d15",
                Borde = "#51cf66",
                Texto = "#f4fff4",
                TextoSecundario = "#69db7c",
                Acento = "#51cf66",
                AcentoFuerte = "#40c057",
                Exito = "#51cf66",
                Aviso = "#ffd43b",
                Error = "#ff6b6b",
                Progreso = "#51cf66"
            },
            ImagenFondo = "tanjiro_bg.jpg",
            Icono = "tanjiro_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 14,
                Particulas = true,
                ParticulasTipo = "fuego_agua",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "cyberpunk",
            Nombre = "Cyberpunk 2077",
            Descripcion = "Night City",
            Categoria = "Cyberpunk",
            Colores = new SkinColors
            {
                Fondo = "#0a000f",
                Tarjeta = "#150020",
                Borde = "#ff00ff",
                Texto = "#f0f0ff",
                TextoSecundario = "#b0b0ff",
                Acento = "#ff00ff",
                AcentoFuerte = "#cc00cc",
                Exito = "#00ff88",
                Aviso = "#ffff00",
                Error = "#ff0066",
                Progreso = "#ff00ff"
            },
            ImagenFondo = "cyberpunk_bg.jpg",
            Icono = "cyberpunk_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 40,
                Particulas = true,
                ParticulasTipo = "neón",
                Animado = true
            }
        });
        
        Skins.Add(new Skin
        {
            Id = "matrix",
            Nombre = "Matrix",
            Descripcion = "Welcome to the Real World",
            Categoria = "Sci-Fi",
            Colores = new SkinColors
            {
                Fondo = "#000000",
                Tarjeta = "#001100",
                Borde = "#00ff00",
                Texto = "#00ff00",
                TextoSecundario = "#008800",
                Acento = "#00ff00",
                AcentoFuerte = "#00cc00",
                Exito = "#00ff00",
                Aviso = "#ffff00",
                Error = "#ff0000",
                Progreso = "#00ff00"
            },
            ImagenFondo = "matrix_bg.jpg",
            Icono = "matrix_icon.png",
            Efectos = new SkinEffects
            {
                Brillo = 30,
                Particulas = true,
                ParticulasTipo = "matrix_code",
                Animado = true
            }
        });
        
        CargarSkinActivo();
    }
    
    public static void CargarSkinActivo()
    {
        try
        {
            if (File.Exists(SkinsConfig))
            {
                var json = File.ReadAllText(SkinsConfig);
                var config = JsonSerializer.Deserialize<SkinConfig>(json);
                if (config != null)
                {
                    ActiveSkinId = config.ActiveSkinId;
                }
            }
        }
        catch { }
    }
    
    public static void GuardarSkinActivo(string skinId)
    {
        ActiveSkinId = skinId;
        try
        {
            var config = new SkinConfig { ActiveSkinId = skinId };
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SkinsConfig, json);
        }
        catch { }
    }
    
    public static Skin? GetSkin(string id)
    {
        return Skins.FirstOrDefault(s => s.Id == id);
    }
    
    public static Skin GetSkinActivo()
    {
        return GetSkin(ActiveSkinId) ?? Skins.First(s => s.EsPorDefecto);
    }
    
    public static List<Skin> GetSkinsPorCategoria(string categoria)
    {
        return Skins.Where(s => s.Categoria == categoria).ToList();
    }
    
    public static List<string> GetCategorias()
    {
        return Skins.Select(s => s.Categoria).Distinct().OrderBy(c => c).ToList();
    }
}

public class Skin
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Categoria { get; set; } = "";
    public bool EsPorDefecto { get; set; }
    public SkinColors Colores { get; set; } = new();
    public SkinEffects Efectos { get; set; } = new();
    public string? ImagenFondo { get; set; }
    public string? Icono { get; set; }
    public DateTime Creado { get; set; } = DateTime.Now;
}

public class SkinColors
{
    public string Fondo { get; set; } = "#0B1626";
    public string Tarjeta { get; set; } = "#15233B";
    public string Borde { get; set; } = "#243452";
    public string Texto { get; set; } = "#FFFFFF";
    public string TextoSecundario { get; set; } = "#7A8AA6";
    public string Acento { get; set; } = "#41A5F5";
    public string AcentoFuerte { get; set; } = "#0E4FA6";
    public string Exito { get; set; } = "#2ECC71";
    public string Aviso { get; set; } = "#F5B041";
    public string Error { get; set; } = "#C8102E";
    public string Progreso { get; set; } = "#41A5F5";
}

public class SkinEffects
{
    public int Brillo { get; set; } = 0;
    public bool Particulas { get; set; }
    public string? ParticulasTipo { get; set; }
    public bool Animado { get; set; }
}

public class SkinConfig
{
    public string ActiveSkinId { get; set; } = "default";
}
