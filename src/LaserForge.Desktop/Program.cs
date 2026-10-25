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
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}