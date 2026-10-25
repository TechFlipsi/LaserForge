using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Formen;
/// <summary>
/// Basis aller Formen (Rechteck, Ellipse, Polygon, Pfad, Text, Gruppe).
/// Alle Formen stehen in mm; ihre Ausdehnung liegt um den eigenen Ursprung, XForm platziert sie.
/// </summary>
public abstract class Form
{
    public Transformationsmatrix XForm { get; set; } = Transformationsmatrix.Identitaet;
    public int SchnittIndex { get; set; }
    public bool Versteckt { get; set; }
    public string? Name { get; set; }
    /// <summary>Eigentliche Formzuege in Weltkoordinaten (XForm inklusive).</summary>
    public abstract List<Zug> ZuZuege();
    /// <summary>Rohtypname im .lbrn-Format (Kompatibilidadeits-Kontext: Format-Tagnamen).</summary>
    public abstract string Rohtyp();
}
