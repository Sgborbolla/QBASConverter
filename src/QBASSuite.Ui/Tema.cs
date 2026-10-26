using System.Drawing;

namespace QBASSuite.Ui;

/// <summary>
/// Los temas de QBASFreemanCopy-Converter. No son imagenes ni ficheros binarios de nadie:
/// son colores de verdad, y se cambian desde la ventana.
/// Son siete estilos inspirados en paletas conocidas, con nombres neutros.
/// </summary>
public sealed class Tema
{
    public required string Nombre { get; init; }

    // --- superficie ---
    public required Color Fondo { get; init; }
    public required Color Panel { get; init; }
    public required Color Panel2 { get; init; }
    public required Color Linea { get; init; }

    // --- texto ---
    public required Color Tinta { get; init; }
    public required Color Tinta2 { get; init; }
    public required Color Tenue { get; init; }

    // --- acento ---
    public required Color Acento { get; init; }
    public required Color AcentoSuave { get; init; }
    public required Color SobreAcento { get; init; }

    // --- estados del disco / fichero ---
    public required Color Copiando { get; init; }
    public required Color Verificado { get; init; }
    public required Color Pausado { get; init; }
    public required Color Error { get; init; }
    public required Color Hecho { get; init; }
    public required Color EnCola { get; init; }
    public required Color Prioridad { get; init; }

    /// <summary>Panel de la lista de discos.</summary>
    public required Color PanelDiscos { get; init; }

    /// <summary>Es un tema oscuro (para saber si hay que aclarar los iconos).</summary>
    public required bool Oscuro { get; init; }

    /// <summary>
    /// Forma decorativa del tema. NO es la marca real de ninguna serie: es un
    /// dibujo vectorial original inspirado en el estilo (una hoja, una llama,
    /// una luna, un rayo...). Se pinta en la cabecera y como franja.
    /// </summary>
    public required FormaTema Forma { get; init; }

    /// <summary>Un simbolo de texto corto para el chip/boton del tema.</summary>
    public required string Glifo { get; init; }

    public static Color De(string hex) => ColorTranslator.FromHtml(hex);

    /// <summary>Los temas. El primero es el de por defecto.</summary>
    public static IReadOnlyList<Tema> Todos { get; } = new List<Tema>
    {
        // 0) Estilo pantalla: gris carbon plano a la izquierda y verde petroleo
        //    a la derecha, con acento azul. Es el de por defecto.
        new()
        {
            Nombre = "Estilo pantalla",
            Fondo = De("#1b1b1b"), Panel = De("#232323"), Panel2 = De("#2d2d2d"), Linea = De("#3a3a3a"),
            Tinta = De("#f5f5f5"), Tinta2 = De("#c7c7c7"), Tenue = De("#8e8e8e"),
            Acento = De("#2196F3"), AcentoSuave = De("#14324a"), SobreAcento = Color.White,
            Copiando = De("#2196F3"), Verificado = De("#43d17a"), Pausado = De("#F5B041"),
            Error = De("#E74C3C"), Hecho = De("#2ECC71"), EnCola = De("#6b7c8a"), Prioridad = De("#E74C3C"),
            PanelDiscos = De("#223a41"), Oscuro = true,
            Forma = FormaTema.Engranaje, Glifo = "Q"
        },

        // 1) Ninja naranja: naranja sobre arena. Claro.
        new()
        {
            Nombre = "Ninja naranja",
            Fondo = De("#faf6ef"), Panel = Color.White, Panel2 = De("#f2ebdd"), Linea = De("#e0d3ba"),
            Tinta = De("#2a2620"), Tinta2 = De("#5c5446"), Tenue = De("#8c8271"),
            Acento = De("#F26522"), AcentoSuave = De("#fbe1d2"), SobreAcento = Color.White,
            Copiando = De("#F26522"), Verificado = De("#2e7d32"), Pausado = De("#b07d00"),
            Error = De("#c0392b"), Hecho = De("#2e7d32"), EnCola = De("#8c8271"),
            Prioridad = De("#c0392b"), PanelDiscos = De("#f7efe2"), Oscuro = false,
            Forma = FormaTema.HojaNinja, Glifo = "N"
        },

        // 2) Esferas del dragón: azul profundo con energia naranja.
        new()
        {
            Nombre = "Esferas del dragón",
            Fondo = De("#0e2350"), Panel = De("#12306b"), Panel2 = De("#173a80"), Linea = De("#2b4f9e"),
            Tinta = De("#eaf1ff"), Tinta2 = De("#b5c7ea"), Tenue = De("#8298c4"),
            Acento = De("#FF8A00"), AcentoSuave = De("#1d3f86"), SobreAcento = De("#241200"),
            Copiando = De("#FF8A00"), Verificado = De("#57d07a"), Pausado = De("#FFCB05"),
            Error = De("#ff6b5e"), Hecho = De("#57d07a"), EnCola = De("#8298c4"),
            Prioridad = De("#ff6b5e"), PanelDiscos = De("#0f2857"), Oscuro = true,
            Forma = FormaTema.Esfera, Glifo = "O"
        },

        // 3) Pirata marina: azul mar con rojo.
        new()
        {
            Nombre = "Pirata marina",
            Fondo = De("#eef4f8"), Panel = Color.White, Panel2 = De("#e0ecf3"), Linea = De("#c2d6e2"),
            Tinta = De("#12283a"), Tinta2 = De("#3d5568"), Tenue = De("#6f8698"),
            Acento = De("#D32011"), AcentoSuave = De("#f7d9d5"), SobreAcento = Color.White,
            Copiando = De("#123B63"), Verificado = De("#2e7d32"), Pausado = De("#b07d00"),
            Error = De("#D32011"), Hecho = De("#2e7d32"), EnCola = De("#6f8698"),
            Prioridad = De("#D32011"), PanelDiscos = De("#e6eff5"), Oscuro = false,
            Forma = FormaTema.Ancla, Glifo = "A"
        },

        // 4) Filoverde: verde con negro ajedrezado.
        new()
        {
            Nombre = "Filoverde",
            Fondo = De("#f2f7f3"), Panel = Color.White, Panel2 = De("#e4efe6"), Linea = De("#c6dcc9"),
            Tinta = De("#16261a"), Tinta2 = De("#3f5444"), Tenue = De("#6f8474"),
            Acento = De("#2E8B57"), AcentoSuave = De("#d3ecdc"), SobreAcento = Color.White,
            Copiando = De("#2E8B57"), Verificado = De("#1b5e20"), Pausado = De("#b07d00"),
            Error = De("#b00020"), Hecho = De("#1b5e20"), EnCola = De("#6f8474"),
            Prioridad = De("#1b1b1b"), PanelDiscos = De("#e9f2ea"), Oscuro = false,
            Forma = FormaTema.Cuadricula, Glifo = "#"
        },

        // 5) Manto verde: verde militar y marron.
        new()
        {
            Nombre = "Manto verde",
            Fondo = De("#f3f2ea"), Panel = Color.White, Panel2 = De("#e7e6d8"), Linea = De("#cfcdb6"),
            Tinta = De("#2b2a20"), Tinta2 = De("#56543f"), Tenue = De("#837f63"),
            Acento = De("#4B5320"), AcentoSuave = De("#dfe0c8"), SobreAcento = Color.White,
            Copiando = De("#4B5320"), Verificado = De("#3d6b2f"), Pausado = De("#8a5b00"),
            Error = De("#a63a2a"), Hecho = De("#3d6b2f"), EnCola = De("#837f63"),
            Prioridad = De("#a63a2a"), PanelDiscos = De("#eceada"), Oscuro = false,
            Forma = FormaTema.Manto, Glifo = "M"
        },

        // 6) Héroe rojo: rojo, amarillo y azul.
        new()
        {
            Nombre = "Héroe rojo",
            Fondo = De("#f6f7fa"), Panel = Color.White, Panel2 = De("#e9ecf3"), Linea = De("#ccd3e0"),
            Tinta = De("#1b2230"), Tinta2 = De("#465064"), Tenue = De("#778196"),
            Acento = De("#D32F2F"), AcentoSuave = De("#f8dcdc"), SobreAcento = Color.White,
            Copiando = De("#2f6fd0"), Verificado = De("#2e7d32"), Pausado = De("#FFCB05"),
            Error = De("#D32F2F"), Hecho = De("#2e7d32"), EnCola = De("#778196"),
            Prioridad = De("#D32F2F"), PanelDiscos = De("#eef1f7"), Oscuro = false,
            Forma = FormaTema.EstrellaHeroe, Glifo = "H"
        },

        // 7) Rosa lunar: rosa, blanco y dorado.
        new()
        {
            Nombre = "Rosa lunar",
            Fondo = De("#fdf5f8"), Panel = Color.White, Panel2 = De("#f9e8f0"), Linea = De("#eccfdd"),
            Tinta = De("#31202a"), Tinta2 = De("#5e4653"), Tenue = De("#8d7481"),
            Acento = De("#E75480"), AcentoSuave = De("#fbdce8"), SobreAcento = Color.White,
            Copiando = De("#E75480"), Verificado = De("#2e7d32"), Pausado = De("#C9A227"),
            Error = De("#c0392b"), Hecho = De("#2e7d32"), EnCola = De("#8d7481"),
            Prioridad = De("#c0392b"), PanelDiscos = De("#fbeef4"), Oscuro = false,
            Forma = FormaTema.Luna, Glifo = "L"
        },

        // 8) Segador blanco: negro, blanco y naranja. Claro.
        new()
        {
            Nombre = "Segador blanco",
            Fondo = De("#f5f5f6"), Panel = Color.White, Panel2 = De("#e9e9ec"), Linea = De("#c9c9d0"),
            Tinta = De("#17171a"), Tinta2 = De("#43434b"), Tenue = De("#74747e"),
            Acento = De("#FF7A00"), AcentoSuave = De("#ffe4cc"), SobreAcento = De("#1a1a1a"),
            Copiando = De("#FF7A00"), Verificado = De("#2e7d32"), Pausado = De("#b07d00"),
            Error = De("#c0392b"), Hecho = De("#2e7d32"), EnCola = De("#74747e"),
            Prioridad = De("#c0392b"), PanelDiscos = De("#eeeeef"), Oscuro = false,
            Forma = FormaTema.Filo, Glifo = "F"
        },

        // 9) Espada de ceniza: acero sobre negro.
        new()
        {
            Nombre = "Espada de ceniza",
            Fondo = De("#1b1d20"), Panel = De("#24272b"), Panel2 = De("#2d3136"), Linea = De("#3f444a"),
            Tinta = De("#eef0f2"), Tinta2 = De("#b9bfc6"), Tenue = De("#858c94"),
            Acento = De("#c9ced6"), AcentoSuave = De("#32373d"), SobreAcento = De("#15171a"),
            Copiando = De("#c9ced6"), Verificado = De("#7fbf7f"), Pausado = De("#d8b24a"),
            Error = De("#d9736b"), Hecho = De("#7fbf7f"), EnCola = De("#858c94"),
            Prioridad = De("#d9736b"), PanelDiscos = De("#1f2226"), Oscuro = true,
            Forma = FormaTema.Acero, Glifo = "E"
        },

        // 10) Alquimia dorada: rojo y dorado.
        new()
        {
            Nombre = "Alquimia dorada",
            Fondo = De("#faf6ef"), Panel = Color.White, Panel2 = De("#f1e9db"), Linea = De("#ddcfb4"),
            Tinta = De("#241f18"), Tinta2 = De("#574d3d"), Tenue = De("#8b7f68"),
            Acento = De("#B22222"), AcentoSuave = De("#f2ded0"), SobreAcento = Color.White,
            Copiando = De("#B22222"), Verificado = De("#2e7d32"), Pausado = De("#C9A227"),
            Error = De("#8f1d1d"), Hecho = De("#2e7d32"), EnCola = De("#8b7f68"),
            Prioridad = De("#8f1d1d"), PanelDiscos = De("#f6efe2"), Oscuro = false,
            Forma = FormaTema.Transmutacion, Glifo = "T"
        },

        // 11) Titanes: verde militar y cuero.
        new()
        {
            Nombre = "Titanes",
            Fondo = De("#f2f1e9"), Panel = Color.White, Panel2 = De("#e6e4d8"), Linea = De("#cdc9b6"),
            Tinta = De("#2a281f"), Tinta2 = De("#544f3d"), Tenue = De("#827b62"),
            Acento = De("#556B2F"), AcentoSuave = De("#dfe1c8"), SobreAcento = Color.White,
            Copiando = De("#556B2F"), Verificado = De("#3d6b2f"), Pausado = De("#8a5b00"),
            Error = De("#8b4a2f"), Hecho = De("#3d6b2f"), EnCola = De("#827b62"),
            Prioridad = De("#8b4a2f"), PanelDiscos = De("#ebe9db"), Oscuro = false,
            Forma = FormaTema.Muro, Glifo = "U"
        },

        // 12) Katana negra: negro, rojo y blanco.
        new()
        {
            Nombre = "Katana negra",
            Fondo = De("#141414"), Panel = De("#1d1d1f"), Panel2 = De("#26262a"), Linea = De("#3a3a40"),
            Tinta = De("#f4f4f6"), Tinta2 = De("#c2c2c8"), Tenue = De("#8a8a92"),
            Acento = De("#E63946"), AcentoSuave = De("#3a1c20"), SobreAcento = Color.White,
            Copiando = De("#E63946"), Verificado = De("#6fd08a"), Pausado = De("#d8b24a"),
            Error = De("#ff6b6b"), Hecho = De("#6fd08a"), EnCola = De("#8a8a92"),
            Prioridad = De("#ff6b6b"), PanelDiscos = De("#191919"), Oscuro = true,
            Forma = FormaTema.Katana, Glifo = "K"
        },

        // 13) Rojo imperial: mecha rojo con dorado.
        new()
        {
            Nombre = "Rojo imperial",
            Fondo = De("#171418"), Panel = De("#201c21"), Panel2 = De("#2a242c"), Linea = De("#41333f"),
            Tinta = De("#f6eef4"), Tinta2 = De("#c6b6c2"), Tenue = De("#907e8c"),
            Acento = De("#C1121F"), AcentoSuave = De("#3a1a1f"), SobreAcento = Color.White,
            Copiando = De("#C1121F"), Verificado = De("#7fbf7f"), Pausado = De("#C9A227"),
            Error = De("#ff5c5c"), Hecho = De("#7fbf7f"), EnCola = De("#907e8c"),
            Prioridad = De("#ff5c5c"), PanelDiscos = De("#1b171c"), Oscuro = true,
            Forma = FormaTema.EmblemaMecha, Glifo = "R"
        },

        // 14) Sintonía azul: azul con llama azul.
        new()
        {
            Nombre = "Sintonía azul",
            Fondo = De("#0d1a2b"), Panel = De("#12233a"), Panel2 = De("#183049"), Linea = De("#2a4a72"),
            Tinta = De("#e9f2ff"), Tinta2 = De("#b3c8e4"), Tenue = De("#8098b8"),
            Acento = De("#3FA9F5"), AcentoSuave = De("#15304d"), SobreAcento = De("#06121f"),
            Copiando = De("#3FA9F5"), Verificado = De("#57d07a"), Pausado = De("#FFCB05"),
            Error = De("#ff6b5e"), Hecho = De("#57d07a"), EnCola = De("#8098b8"),
            Prioridad = De("#ff6b5e"), PanelDiscos = De("#0f1e31"), Oscuro = true,
            Forma = FormaTema.LlamaAzul, Glifo = "S"
        },

        // 15) Naranja ceniza: verde y negro ajedrezado.
        new()
        {
            Nombre = "Naranja ceniza",
            Fondo = De("#eff5f0"), Panel = Color.White, Panel2 = De("#e1ece3"), Linea = De("#c4d8c8"),
            Tinta = De("#161d1a"), Tinta2 = De("#3d4a42"), Tenue = De("#6d7c72"),
            Acento = De("#2E8B57"), AcentoSuave = De("#d0e8d9"), SobreAcento = Color.White,
            Copiando = De("#2E8B57"), Verificado = De("#1b5e20"), Pausado = De("#b07d00"),
            Error = De("#b00020"), Hecho = De("#1b5e20"), EnCola = De("#6d7c72"),
            Prioridad = De("#141414"), PanelDiscos = De("#e7f0e8"), Oscuro = false,
            Forma = FormaTema.Colmillo, Glifo = "C"
        },

        // 16) Sombra monarca: azul violeta con morado (estilo cazador de sombras).
        new()
        {
            Nombre = "Sombra monarca",
            Fondo = De("#0e1020"), Panel = De("#161a33"), Panel2 = De("#1e2444"), Linea = De("#333a63"),
            Tinta = De("#e9ecff"), Tinta2 = De("#b6bde6"), Tenue = De("#828bb8"),
            Acento = De("#7C4DFF"), AcentoSuave = De("#241f4d"), SobreAcento = Color.White,
            Copiando = De("#7C4DFF"), Verificado = De("#5ce1c8"), Pausado = De("#ffd166"),
            Error = De("#ff5c7a"), Hecho = De("#5ce1c8"), EnCola = De("#828bb8"),
            Prioridad = De("#ff5c7a"), PanelDiscos = De("#111430"), Oscuro = true,
            Forma = FormaTema.CoronaSombra, Glifo = "X"
        },

        // 17) Lobo solitario: gris luna con azul hielo.
        new()
        {
            Nombre = "Lobo solitario",
            Fondo = De("#eef1f4"), Panel = Color.White, Panel2 = De("#e1e6ec"), Linea = De("#c3ccd6"),
            Tinta = De("#1b2229"), Tinta2 = De("#46525e"), Tenue = De("#748292"),
            Acento = De("#2F6FD0"), AcentoSuave = De("#dbe6f6"), SobreAcento = Color.White,
            Copiando = De("#2F6FD0"), Verificado = De("#2e7d32"), Pausado = De("#b07d00"),
            Error = De("#c0392b"), Hecho = De("#2e7d32"), EnCola = De("#748292"),
            Prioridad = De("#c0392b"), PanelDiscos = De("#e7ecf2"), Oscuro = false,
            Forma = FormaTema.Aullido, Glifo = "W"
        },

        // 18) Bruja escarlata: rojo oscuro con carmesi.
        new()
        {
            Nombre = "Bruja escarlata",
            Fondo = De("#17070c"), Panel = De("#230d14"), Panel2 = De("#2e121b"), Linea = De("#4a1f2b"),
            Tinta = De("#ffe9ee"), Tinta2 = De("#e0b3bd"), Tenue = De("#a87f89"),
            Acento = De("#E5383B"), AcentoSuave = De("#3d131a"), SobreAcento = Color.White,
            Copiando = De("#E5383B"), Verificado = De("#6fd08a"), Pausado = De("#ffd166"),
            Error = De("#ff7a7a"), Hecho = De("#6fd08a"), EnCola = De("#a87f89"),
            Prioridad = De("#ff7a7a"), PanelDiscos = De("#1c0a10"), Oscuro = true,
            Forma = FormaTema.Escarlata, Glifo = "B"
        },

        // 19) Aliento de fuego: naranja y rojo (estilo cazador de demonios).
        new()
        {
            Nombre = "Aliento de fuego",
            Fondo = De("#1a1008"), Panel = De("#26170c"), Panel2 = De("#33200f"), Linea = De("#543317"),
            Tinta = De("#ffefdd"), Tinta2 = De("#e3c3a0"), Tenue = De("#a98d6d"),
            Acento = De("#FF6B00"), AcentoSuave = De("#41260f"), SobreAcento = De("#1a0f04"),
            Copiando = De("#FF6B00"), Verificado = De("#7fbf7f"), Pausado = De("#FFCB05"),
            Error = De("#ff5252"), Hecho = De("#7fbf7f"), EnCola = De("#a98d6d"),
            Prioridad = De("#ff5252"), PanelDiscos = De("#20140a"), Oscuro = true,
            Forma = FormaTema.Llamarada, Glifo = "G"
        },

        // 20) Cazador de demonios: morado y rosa neón.
        new()
        {
            Nombre = "Cazador de demonios",
            Fondo = De("#140f1c"), Panel = De("#1d1527"), Panel2 = De("#271c34"), Linea = De("#3d2c50"),
            Tinta = De("#f2e9ff"), Tinta2 = De("#cbb6e6"), Tenue = De("#9a86b8"),
            Acento = De("#FF3D9A"), AcentoSuave = De("#331a3d"), SobreAcento = Color.White,
            Copiando = De("#FF3D9A"), Verificado = De("#5ce1c8"), Pausado = De("#ffd166"),
            Error = De("#ff5c5c"), Hecho = De("#5ce1c8"), EnCola = De("#9a86b8"),
            Prioridad = De("#ff5c5c"), PanelDiscos = De("#181120"), Oscuro = true,
            Forma = FormaTema.SonrisaFilo, Glifo = "D"
        },

        // 21) Genio azul: azul con dorado (estilo lámpara mágica).
        new()
        {
            Nombre = "Genio azul",
            Fondo = De("#0b1a2e"), Panel = De("#10243d"), Panel2 = De("#16304f"), Linea = De("#284a72"),
            Tinta = De("#eaf3ff"), Tinta2 = De("#b6cbe6"), Tenue = De("#8299b8"),
            Acento = De("#F4B400"), AcentoSuave = De("#16304f"), SobreAcento = De("#101010"),
            Copiando = De("#F4B400"), Verificado = De("#57d07a"), Pausado = De("#FFCB05"),
            Error = De("#ff6b5e"), Hecho = De("#57d07a"), EnCola = De("#8299b8"),
            Prioridad = De("#ff6b5e"), PanelDiscos = De("#0d1f36"), Oscuro = true,
            Forma = FormaTema.Lampara, Glifo = "J"
        },

        // 22) Alquimista de acero: engranaje y llama.
        new()
        {
            Nombre = "Alquimista de acero",
            Fondo = De("#f3f2f5"), Panel = Color.White, Panel2 = De("#e7e6ec"), Linea = De("#cdcbd6"),
            Tinta = De("#22222a"), Tinta2 = De("#4c4c58"), Tenue = De("#7c7c8a"),
            Acento = De("#B22222"), AcentoSuave = De("#efdcdc"), SobreAcento = Color.White,
            Copiando = De("#B22222"), Verificado = De("#2e7d32"), Pausado = De("#C9A227"),
            Error = De("#8f1d1d"), Hecho = De("#2e7d32"), EnCola = De("#7c7c8a"),
            Prioridad = De("#8f1d1d"), PanelDiscos = De("#eceaf0"), Oscuro = false,
            Forma = FormaTema.Engranaje, Glifo = "E"
        },

        // 23) Caballero dorado: constelacion sobre azul noche.
        new()
        {
            Nombre = "Caballero dorado",
            Fondo = De("#0b1226"), Panel = De("#121b36"), Panel2 = De("#1a2547"), Linea = De("#2f3d68"),
            Tinta = De("#eef2ff"), Tinta2 = De("#bcc6e6"), Tenue = De("#8791b4"),
            Acento = De("#F4B400"), AcentoSuave = De("#1a2547"), SobreAcento = De("#101010"),
            Copiando = De("#F4B400"), Verificado = De("#5ce1c8"), Pausado = De("#FFCB05"),
            Error = De("#ff6b5e"), Hecho = De("#5ce1c8"), EnCola = De("#8791b4"),
            Prioridad = De("#ff6b5e"), PanelDiscos = De("#0e162c"), Oscuro = true,
            Forma = FormaTema.Constelacion, Glifo = "C"
        },

        // 24) Nube voladora: nube sobre cielo claro.
        new()
        {
            Nombre = "Nube voladora",
            Fondo = De("#eef6fb"), Panel = Color.White, Panel2 = De("#e0eef7"), Linea = De("#c2d9ea"),
            Tinta = De("#122736"), Tinta2 = De("#3c5566"), Tenue = De("#6e8598"),
            Acento = De("#2196F3"), AcentoSuave = De("#d6e9f8"), SobreAcento = Color.White,
            Copiando = De("#2196F3"), Verificado = De("#2e7d32"), Pausado = De("#b07d00"),
            Error = De("#c0392b"), Hecho = De("#2e7d32"), EnCola = De("#6e8598"),
            Prioridad = De("#c0392b"), PanelDiscos = De("#e6f2fa"), Oscuro = false,
            Forma = FormaTema.Nube, Glifo = "N"
        },

        // 25) Shinigami de cuaderno: negro con rojo.
        new()
        {
            Nombre = "Shinigami de cuaderno",
            Fondo = De("#151515"), Panel = De("#1e1e1e"), Panel2 = De("#272727"), Linea = De("#3b3b3b"),
            Tinta = De("#f2f2f2"), Tinta2 = De("#c2c2c2"), Tenue = De("#8c8c8c"),
            Acento = De("#C1121F"), AcentoSuave = De("#34171a"), SobreAcento = Color.White,
            Copiando = De("#C1121F"), Verificado = De("#6fd08a"), Pausado = De("#d8b24a"),
            Error = De("#ff6b6b"), Hecho = De("#6fd08a"), EnCola = De("#8c8c8c"),
            Prioridad = De("#ff6b6b"), PanelDiscos = De("#1a1a1a"), Oscuro = true,
            Forma = FormaTema.Cuaderno, Glifo = "Q"
        },

        // 26) Caballero negro: dos espadas cruzadas.
        new()
        {
            Nombre = "Caballero negro",
            Fondo = De("#101319"), Panel = De("#181d26"), Panel2 = De("#212833"), Linea = De("#36404f"),
            Tinta = De("#e9eef5"), Tinta2 = De("#b7c1cd"), Tenue = De("#828d9b"),
            Acento = De("#4FC3F7"), AcentoSuave = De("#1b2a3a"), SobreAcento = De("#06121c"),
            Copiando = De("#4FC3F7"), Verificado = De("#6fd08a"), Pausado = De("#ffd166"),
            Error = De("#ff6b6b"), Hecho = De("#6fd08a"), EnCola = De("#828d9b"),
            Prioridad = De("#ff6b6b"), PanelDiscos = De("#131822"), Oscuro = true,
            Forma = FormaTema.Espadas, Glifo = "V"
        },

        // 27) Cazador de duendes: escudo con daga.
        new()
        {
            Nombre = "Cazador de duendes",
            Fondo = De("#f0f1ee"), Panel = Color.White, Panel2 = De("#e3e5df"), Linea = De("#c8ccc0"),
            Tinta = De("#242821"), Tinta2 = De("#4e5449"), Tenue = De("#7e8577"),
            Acento = De("#556B2F"), AcentoSuave = De("#dfe3d2"), SobreAcento = Color.White,
            Copiando = De("#556B2F"), Verificado = De("#3d6b2f"), Pausado = De("#8a5b00"),
            Error = De("#8b4a2f"), Hecho = De("#3d6b2f"), EnCola = De("#7e8577"),
            Prioridad = De("#8b4a2f"), PanelDiscos = De("#e8eae2"), Oscuro = false,
            Forma = FormaTema.EscudoDaga, Glifo = "G"
        },

        // 28) Zorro de nueve colas: naranja con llamas.
        new()
        {
            Nombre = "Zorro de nueve colas",
            Fondo = De("#fdf4ec"), Panel = Color.White, Panel2 = De("#f6e7d8"), Linea = De("#e6cfb6"),
            Tinta = De("#2b2018"), Tinta2 = De("#5c4a3a"), Tenue = De("#8c7660"),
            Acento = De("#E8590C"), AcentoSuave = De("#fbdfcb"), SobreAcento = Color.White,
            Copiando = De("#E8590C"), Verificado = De("#2e7d32"), Pausado = De("#b07d00"),
            Error = De("#c0392b"), Hecho = De("#2e7d32"), EnCola = De("#8c7660"),
            Prioridad = De("#c0392b"), PanelDiscos = De("#f8ecdf"), Oscuro = false,
            Forma = FormaTema.NueveColas, Glifo = "Z"
        },

        // 29) Maga de luna: rosa con estrella y varita.
        new()
        {
            Nombre = "Maga de luna",
            Fondo = De("#fdf6fb"), Panel = Color.White, Panel2 = De("#f7e9f4"), Linea = De("#e8cfe1"),
            Tinta = De("#2e2030"), Tinta2 = De("#5a4659"), Tenue = De("#8c7488"),
            Acento = De("#B14AAF"), AcentoSuave = De("#f1ddf0"), SobreAcento = Color.White,
            Copiando = De("#B14AAF"), Verificado = De("#2e7d32"), Pausado = De("#C9A227"),
            Error = De("#c0392b"), Hecho = De("#2e7d32"), EnCola = De("#8c7488"),
            Prioridad = De("#c0392b"), PanelDiscos = De("#f9edf6"), Oscuro = false,
            Forma = FormaTema.Magica, Glifo = "Y"
        },

        // 30) Pirata de fuego: ancla con llama sobre azul.
        new()
        {
            Nombre = "Pirata de fuego",
            Fondo = De("#0d1b2a"), Panel = De("#122438"), Panel2 = De("#183148"), Linea = De("#2a4b6a"),
            Tinta = De("#eaf3ff"), Tinta2 = De("#b6cae2"), Tenue = De("#8297b0"),
            Acento = De("#FF6B00"), AcentoSuave = De("#183148"), SobreAcento = De("#160d02"),
            Copiando = De("#FF6B00"), Verificado = De("#57d07a"), Pausado = De("#FFCB05"),
            Error = De("#ff6b5e"), Hecho = De("#57d07a"), EnCola = De("#8297b0"),
            Prioridad = De("#ff6b5e"), PanelDiscos = De("#0f2033"), Oscuro = true,
            Forma = FormaTema.AnclaFuego, Glifo = "P"
        },

        // 31) Cazador de sombras: silueta con corona, violeta.
        new()
        {
            Nombre = "Cazador de sombras",
            Fondo = De("#0c0f1c"), Panel = De("#141828"), Panel2 = De("#1c2237"), Linea = De("#303a58"),
            Tinta = De("#e9edff"), Tinta2 = De("#b7bfe4"), Tenue = De("#848cb4"),
            Acento = De("#00E5FF"), AcentoSuave = De("#152a3d"), SobreAcento = De("#04121a"),
            Copiando = De("#00E5FF"), Verificado = De("#5ce1c8"), Pausado = De("#ffd166"),
            Error = De("#ff5c7a"), Hecho = De("#5ce1c8"), EnCola = De("#848cb4"),
            Prioridad = De("#ff5c7a"), PanelDiscos = De("#10142a"), Oscuro = true,
            Forma = FormaTema.CoronaSombra, Glifo = "S"
        },

        // 32) Espada de humo: acero claro con azul.
        new()
        {
            Nombre = "Espada de humo",
            Fondo = De("#eef1f5"), Panel = Color.White, Panel2 = De("#e0e5ec"), Linea = De("#c4ccd8"),
            Tinta = De("#1c2229"), Tinta2 = De("#47505c"), Tenue = De("#75808e"),
            Acento = De("#57647a"), AcentoSuave = De("#dde2ea"), SobreAcento = Color.White,
            Copiando = De("#57647a"), Verificado = De("#2e7d32"), Pausado = De("#b07d00"),
            Error = De("#c0392b"), Hecho = De("#2e7d32"), EnCola = De("#75808e"),
            Prioridad = De("#c0392b"), PanelDiscos = De("#e7ebf1"), Oscuro = false,
            Forma = FormaTema.Filo, Glifo = "F"
        },

        // 33) Titan de hierro: escudo y engranaje, gris azulado.
        new()
        {
            Nombre = "Titán de hierro",
            Fondo = De("#eceef1"), Panel = Color.White, Panel2 = De("#dde1e7"), Linea = De("#c0c6cf"),
            Tinta = De("#1b1f26"), Tinta2 = De("#454b56"), Tenue = De("#737a86"),
            Acento = De("#455A64"), AcentoSuave = De("#d8dee3"), SobreAcento = Color.White,
            Copiando = De("#455A64"), Verificado = De("#2e7d32"), Pausado = De("#8a5b00"),
            Error = De("#a63a2a"), Hecho = De("#2e7d32"), EnCola = De("#737a86"),
            Prioridad = De("#a63a2a"), PanelDiscos = De("#e4e8ec"), Oscuro = false,
            Forma = FormaTema.EmblemaMecha, Glifo = "R"
        }
    };

    public static Tema PorDefecto => Todos[0];

    public static Tema PorNombre(string? nombre) =>
        Todos.FirstOrDefault(t => string.Equals(t.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
        ?? PorDefecto;
}
