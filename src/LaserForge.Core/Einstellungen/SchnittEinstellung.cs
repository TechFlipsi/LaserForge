namespace LaserForge.Core.Einstellungen;
/// <summary>
/// Einstellung einer (Layer-)Schnittzeile, abgebildet auf das .lbrn CutSetting
/// (Kompatibilidadeits-Kontext: Format-Tag-Namen). Mengen-Einheiten intern: mm/s, %, mm.
/// </summary>
public sealed class SchnittEinstellung
{
    public int Index { get; set; }
    public string NameSektion { get; set; } = "";
    public string Typ { get; set; } = "Cut";         // Cut / Scan / Fill / Score / KerfTest
    public bool Aktiviert { get; set; } = true;
    public double MinPower { get; set; } = 80;
    public double MaxPower { get; set; } = 80;
    /// <summary>Feld-Partner minPower2/maxPower2 (Format-Spec; Default 80).</summary>
    public double MinPower2 { get; set; } = 80;
    public double MaxPower2 { get; set; } = 80;
    public double Speed { get; set; } = 30;          // mm/s
    public int Prioritaet { get; set; }
    public int NumPasses { get; set; } = 1;
    public double ZProPass { get; set; }
    public double ZOffset { get; set; }
    public double LineSpacing { get; set; } = 0.1;   // mm (Scan/Fill)
    public double ScanAngle { get; set; } = 0;
    public bool RunBlower { get; set; }
    public double KerfWidth { get; set; }
    public double Overcut { get; set; }
    public int Frequency { get; set; }
    public bool Versteckt { get; set; }
    /// <summary>Restliche Parameter verlustfrei (Name -> Rohwert-String), fuer Roundtrip-Vollstaendigkeit.</summary>
    public List<(string Name, string Value)> RestParameter { get; set; } = new();
    public override string ToString() => $"[{Index}] {Typ} '{NameSektion}' {MinPower}% @ {Speed}mm/s {NumPasses}x";
}
