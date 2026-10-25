using System.Xml.Linq;
using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formen;
namespace LaserForge.Core.Formate;
/// <summary>Text-Nummerierung mit Variablen (Formatname VariableText — Kompatibilitäts-Kontext).</summary>
public sealed class VariableTextEinstellungen
{
    public int Start { get; set; }
    public int Ende { get; set; } = 999;
    public int Aktueller { get; set; }
    public int Schritt { get; set; } = 1;
    public bool AutoWeiterschalten { get; set; }
}
/// <summary>Projekt-Notizen (Formatname Notes). Zeigen beim Laden optional.</summary>
public sealed class Notizen
{
    public bool BeiLadenZeigen { get; set; }
    public string Text { get; set; } = "";
}
/// <summary>
/// LaserForge-Projektmodell (unabhaengig vom Dateiformat) mit Abbild auf .lbrn/.lbrn2.
/// Unbekannte Knoten werden verlustfrei als Rohknoten erhalten (Round-trip-Sicherheit).
/// </summary>
public sealed class Projekt
{
    public string AppVersion { get; set; } = "1.0.0";
    /// <summary>Vorschaubild als Base64-PNG (250x250) — beim Ablauf-Speichern automatisch erzeugt.</summary>
    public string? ThumbnailBasis64 { get; set; }
    /// <summary>Falls gesetzt: Geraetename im Projekt (kompatibler Zusatzkontext) — z.B. „Elegoo“.</summary>
    public string FormatVersion { get; set; } = "0";
    public double MaterialHeight { get; set; }
    public bool MirrorX { get; set; }
    public bool MirrorY { get; set; }
    public double BreiteMM { get; set; } = 400;
    public double HoeheMM { get; set; } = 400;
    public VariableTextEinstellungen? VariableText { get; set; }
    public Notizen? Notizen { get; set; }
    public Dictionary<string, string> UIPrefs { get; set; } = new();
    /// <summary>Unbekannte/Rest-Knoten aus dem Quell-Dokument (verlustfrei erhalten).</summary>
    public List<XElement> RestKnoten { get; set; } = new();
    public List<SchnittEinstellung> Schnitte { get; set; } = new();
    public List<Form> Formen { get; set; } = new();
    public List<SchnittEinstellung> GeordneteSchnitte() => Schnitte.OrderBy(s => s.Index).ToList();
    public int FormenZahl()
    {
        int n = 0;
        foreach (var f in Formen) n += Zaehle(f);
        return n;
    }
    private static int Zaehle(Form f) => f is GruppeForm g ? g.Kinder.Sum(Zaehle) : 1;
}
