namespace LaserForge.Core.Thema;

/// <summary>
/// Theme-Ebene (Paritaet zu LightBurns Application Theme: hell/dunkel; „System"-Modus folgt
/// dem OS-Rahmen). Dunkle Werte = GEMESSEN 04.11.2026 (Screencap-Analyse, siehe
/// docs/recherche/icon-redraw-spec-20261104.md). Helle Werte = VORLAULFASSUNG eigene Wahl —
/// exakte Messung folgt mit dem hellen Screencap (Screencap-Runde 2).
/// </summary>
public static class ThemeFarben
{
    /// <summary>Aktiver Modus (false = dunkel/Standard).</summary>
    public static bool IstHell { get; set; }

    /// <summary>Arbeitsflaeche extra abdunkeln (Paritaet „Use Dark Background").</summary>
    public static bool ArbeitsflaecheAbgedunkelt { get; set; }

    public static string Fensterrahmen => IstHell ? "#f5f5f5" : "#1e1e1e";
    public static string Arbeitsflaeche => IstHell
        ? "#ffffff"
        : (ArbeitsflaecheAbgedunkelt ? "#0c0c0c" : "#161616");
    public static string Panel => IstHell ? "#ececec" : "#1f1f1f";
    public static string Liste => IstHell ? "#ffffff" : "#161616";
    public static string Knopf => IstHell ? "#dcdcdc" : "#484848";
    public static string Statusleiste => IstHell ? "#1e1e1e" : "#f8f8f8";
    public static string Text => IstHell ? "#1a1a1a" : "#ffffff";
    public static string TextSekundaer => IstHell ? "#555555" : "#aaaaaa";
    public static string Linie => IstHell ? "#c0c0c0" : "#3a3a3a";
    public static string OkAkzent => "#26618c"; // wie im Geraete-Dialog
    /// <summary>QSS-Fund: Dock-Header-Verlauf (schwarz→rot→orange), Akzent beider Modus.</summary>
    public static (string A, string B, string C) Verlauf => ("#000000", "#be3232", "#dc7832");
}
