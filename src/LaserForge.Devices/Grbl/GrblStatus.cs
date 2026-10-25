using System.Globalization;
namespace LaserForge.Devices.Grbl;
/// <summary>Aufbereiteter GRBL-Status (&lt;Idle|MPos:..|WPos:..|FS:..&gt;) mit X/Y/Z-Zugriff.</summary>
public sealed class GrblStatus
{
    public string Zustand { get; init; } = "Unbekannt";
    public double[] Maschine { get; init; } = new double[3];
    public double[] Werk { get; init; } = new double[3];
    public double Feed { get; init; }
    public double Feedueberschreibung { get; init; }
    public double MaschineX => Maschine[0];
    public double MaschineY => Maschine[1];
    public double MaschineZ => Maschine[2];
    public double WerkX => Werk[0];
    public double WerkY => Werk[1];
    public double WerkZ => Werk[2];
    public override string ToString() =>
        $"Zustand={Zustand} Maschine=({MaschineX:0.###}; {MaschineY:0.###}; {MaschineZ:0.###}) Werk=({WerkX:0.###}; {WerkY:0.###}; {WerkZ:0.###})";
    public static GrblStatus? AusZeile(string? zeile)
    {
        if (zeile is null) return null;
        var t = zeile.Trim();
        int von = t.IndexOf('<'); int bis = t.IndexOf('>');
        if (von < 0 || bis <= von) return null;
        var teile = t[(von + 1)..bis].Split('|');
        var status = new GrblStatus { Zustand = teile[0].Trim() };
        for (int i = 1; i < teile.Length; i++)
        {
            var token = teile[i];
            int ix = token.IndexOf(':');
            if (ix <= 0) continue;
            var schluessel = token[..ix].Trim();
            var wertText = token[(ix + 1)..].Trim();
            var zahl = wertText.Split(',')
                .Select(s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : double.NaN)
                .Where(v => !double.IsNaN(v)).ToArray();
            switch (schluessel)
            {
                case "MPos":
                    for (int k = 0; k < 3; k++) status.Maschine[k] = zahl.Length > k ? zahl[k] : 0;
                    break;
                case "WPos":
                    for (int k = 0; k < 3; k++) status.Werk[k] = zahl.Length > k ? zahl[k] : 0;
                    break;
                case "FS":
                    status.Feed = zahl.Length > 0 ? zahl[0] : 0;
                    status.Feedueberschreibung = zahl.Length > 1 ? zahl[1] : 0;
                    break;
                case "F":
                    status.Feed = zahl.Length > 0 ? zahl[0] : 0;
                    break;
            }
        }
        return status;
    }
}
