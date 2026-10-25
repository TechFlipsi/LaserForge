using Avalonia.Media;
using LaserForge.Core.Geometrie;

namespace LaserForge.Desktop.Controls;

/// <summary>
/// Geteilte Layer-Palette aus docs/design-ziel-bild.md (eigene Festlegung):
/// einmal definieren, Layer-Leiste / Ebenen-Tab / Canvas-Zeichnung nutzen dieselben Farben.
/// </summary>
public static class LayerFarben
{
    /// <summary>31 Chips 00..30 + T1/T2 (Bibel-Palette).</summary>
    public static readonly (string Nummer, string Hex)[] Palette =
    {
        ("00", "#000000"), ("01", "#FF0000"), ("02", "#00C000"), ("03", "#0060FF"),
        ("04", "#FF80FF"), ("05", "#FF8000"), ("06", "#00C0FF"), ("07", "#FFFF00"),
        ("08", "#800060"), ("09", "#802000"), ("10", "#404040"), ("11", "#FFC000"),
        ("12", "#6080FF"), ("13", "#C0C0C0"), ("14", "#FF0080"), ("15", "#008060"),
        ("16", "#806000"), ("17", "#006080"), ("18", "#FF6020"), ("19", "#808080"),
        ("20", "#C00000"), ("21", "#0080FF"), ("22", "#8000FF"), ("23", "#00A0A0"),
        ("24", "#A06000"), ("25", "#006000"), ("26", "#800000"), ("27", "#0040A0"),
        ("28", "#A000A0"), ("29", "#404080"), ("30", "#206040"), ("T1", "#202020"), ("T2", "#606060"),
    };

    /// <summary>Farbe eines Schnitt-Layers (Index &gt; 30 = Werkzeug-Layer, neutral grau).</summary>
    public static Color VonIndex(int index)
    {
        if (index < 0) return Color.Parse("#808080");
        if (index <= 30) return Color.Parse(Palette[index].Hex);
        return Color.Parse("#808080");
    }

    /// <summary>Chipschrift: schwarz auf heller, weiß auf dunkler Grundfarbe.</summary>
    public static Color Textfarbe(Color c)
    {
        double helligkeit = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
        return helligkeit > 0.55 ? Colors.Black : Colors.White;
    }

    /// <summary>Welt-BBoundingBox einer Form in mm (über die Zuege, plus Toleranz für Striche).</summary>
    public static double[] BoxMM(LaserForge.Core.Formen.Form f, double toleranzMM = 0.4)
    {
        double mx = double.MaxValue, my = double.MaxValue, Mx = double.MinValue, My = double.MinValue;
        foreach (var z in f.ZuZuege())
        {
            foreach (var p in z.Punkte)
            {
                if (p.X < mx) mx = p.X;
                if (p.Y < my) my = p.Y;
                if (p.X > Mx) Mx = p.X;
                if (p.Y > My) My = p.Y;
            }
        }
        if (mx > Mx) return new[] { 0.0, 0.0, 0.0, 0.0 };   // leere Form
        return new[] { mx - toleranzMM, my - toleranzMM, Mx + toleranzMM, My + toleranzMM };
    }
}