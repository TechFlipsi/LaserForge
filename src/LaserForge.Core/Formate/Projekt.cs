using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Formate;
/// <summary>
/// LaserForge-Projektmodell (unabhaengig vom Dateiformat!) mit Abbild-Abdeckung auf .lbrn/.lbrn2.
/// </summary>
public sealed class Projekt
{
    public string AppVersion { get; set; } = "1.0.0";
    public string FormatVersion { get; set; } = "0";
    public double MaterialHeight { get; set; }
    public bool MirrorX { get; set; }
    public bool MirrorY { get; set; }
    public double BreiteMM { get; set; } = 400;   // Arbeitsflaeche-Geraet (Default wie Sirs Elegoo)
    public double HoeheMM { get; set; } = 400;
    public List<SchnittEinstellung> Schnitte { get; set; } = new();
    public List<Form> Formen { get; set; } = new();
    public List<SchnittEinstellung> GeordneteSchnitte() => Schnitte.OrderBy(s => s.Index).ToList();
    public int FormenZahl() { int n = 0; foreach (var f in Formen) n += Zaehle(f); return n; }
    private static int Zaehle(Form f) => f is GruppeForm g ? g.Kinder.Sum(Zaehle) : 1;
}
