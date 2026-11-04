using Avalonia;

namespace LaserForge.Desktop;

/// <summary>QA-/Komfort-Startargumente der Bau-Sandbox (optional, alle mit Default).</summary>
public static class StartEinstellung
{
    /// <summary>Start-Tab (0..6; 2 = Konsole wie die Bibel).</summary>
    public static int Tab = 2;

    /// <summary>.lbrn-Datei, die sofort geöffnet wird (echter Öffnen-Pfad).</summary>
    public static string Datei = "";

    /// <summary>Fenster maximiert starten (deterministische QA-Geometrie).</summary>
    public static bool Maximieren;

    /// <summary>Gesamte Formen beim Start auswählen (QA-Auswahlrahmen/Nachweis).</summary>
    public static bool AuswahlAlle;

    /// <summary>Statuszeile zeigt die Auswahl-Box in mm (App-interne Beweis-Zahl für QA).</summary>
    public static bool Info;

    /// <summary>QA-Zug der gesamten Auswahl beim Start: "dx;dy" in mm (App-interne Zug-Automation).</summary>
    public static string ZugMM = "";

    /// <summary>QA: 0,9 s nach dem Zug einmal Rückgängig (Klone-Stapel-Beweis).</summary>
    public static bool UndoQA;

    /// <summary>QA: Geräte-Dialog als Startfenster (Dialog-Nachweis ohne Menü-Klick).</summary>
    public static bool GeraeteDialog;

    /// <summary>QA: Konfig-Dialog als Startfenster (Fern-Drill am echten Port).</summary>
    public static bool KonfigDialog;

    /// <summary>QA: Port für den Konfig-Drill (--konfig-port=COM4) — überschreibt aktives Gerät.</summary>
    public static string KonfigPort = "";

    /// <summary>Theme: hell/dunkel (--theme=hell|dunkel; dunkel = gemessene Palette).</summary>
    public static bool ThemeHell;

    /// <summary>Verzögerung der QA-Automatik (ms) — zum Rhythmus-Machen der Capture.</summary>
    public static int WarteMs = 700;
}

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        foreach (var a in args)
        {
            if (a.StartsWith("--tab=", StringComparison.Ordinal))
            {
                if (int.TryParse(a[6..], out int t)) StartEinstellung.Tab = t;
            }
            else if (a.StartsWith("--datei=", StringComparison.Ordinal))
            {
                StartEinstellung.Datei = a[8..].Trim('"');
            }
            else if (string.Equals(a, "--qa-max", StringComparison.OrdinalIgnoreCase))
            {
                StartEinstellung.Maximieren = true;
            }
            else if (string.Equals(a, "--qa-auswahl", StringComparison.OrdinalIgnoreCase))
            {
                StartEinstellung.AuswahlAlle = true;
            }
            else if (string.Equals(a, "--qa-info", StringComparison.OrdinalIgnoreCase))
            {
                StartEinstellung.Info = true;
            }
            else if (a.StartsWith("--qa-zug=", StringComparison.Ordinal))
            {
                StartEinstellung.ZugMM = a[9..].Trim('"');
            }
            else if (string.Equals(a, "--qa-undo", StringComparison.OrdinalIgnoreCase))
            {
                StartEinstellung.UndoQA = true;
            }
            else if (string.Equals(a, "--geraete-dialog", StringComparison.OrdinalIgnoreCase))
            {
                StartEinstellung.GeraeteDialog = true;
            }
            else if (string.Equals(a, "--konfig-dialog", StringComparison.OrdinalIgnoreCase))
            {
                StartEinstellung.KonfigDialog = true;
            }
            else if (a.StartsWith("--konfig-port=", StringComparison.Ordinal))
            {
                StartEinstellung.KonfigPort = a[14..].Trim('"');
            }
            else if (string.Equals(a, "--theme=hell", StringComparison.OrdinalIgnoreCase))
            {
                StartEinstellung.ThemeHell = true;
            }
            else if (string.Equals(a, "--theme=dunkel", StringComparison.OrdinalIgnoreCase))
            {
                StartEinstellung.ThemeHell = false;
            }
        }
        LaserForge.Core.Thema.ThemeFarben.IstHell = StartEinstellung.ThemeHell;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}