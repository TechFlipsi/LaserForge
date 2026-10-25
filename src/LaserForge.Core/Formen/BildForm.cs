using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Formen;
/// <summary>
/// Rasterbilddaten als Form: Grauwerte (Zeilen-Hauptordnung, 0..255), Pixelgroesse in mm.
/// Entschluesseln (PNG/JPG) passiert am Einfuegeort (Desktop/Cli) — der Kern ist dekoderneutral.
/// </summary>
public sealed class BildForm : Form
{
    public byte[] Grauwerte { get; set; } = Array.Empty<byte>();
    public int BreitePix { get; set; }
    public int HoehePix { get; set; }
    public double PixelMM { get; set; } = 0.1;
    /// <summary>Schwelle: Grauwert darunter = Laser an (Standard 128).</summary>
    public int Schwelle { get; set; } = 128;
    public override string Rohtyp() => "Image";
    protected override List<Zug> Rohezuege()
    {
        double w = BreitePix * PixelMM, h = HoehePix * PixelMM;
        return new()
        {
            new Zug(new[] { new Punkt(0, 0), new Punkt(0, h), new Punkt(w, h), new Punkt(w, 0), new Punkt(0, 0) }, true),
        };
    }
    /// <summary>Ist Pixel (x, y) dunkel (Lasern)?</summary>
    public bool PixelDunkel(int x, int y)
    {
        if (x < 0 || y < 0 || x >= BreitePix || y >= HoehePix) return false;
        return Grauwerte[y * BreitePix + x] < Schwelle;
    }
}
