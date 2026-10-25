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
    /// <summary>Rohzuege der Form in LOKALEN Koordinaten (ohne XForm).</summary>
    protected abstract List<Zug> Rohezuege();
    /// <summary>Formzuege in Weltkoordinaten = Rohezuege mit XForm angewendet.</summary>
    public List<Zug> ZuZuege() =>
        Rohezuege().Select(z => new Zug(z.Punkte.Select(XForm.Anwenden).ToList(), z.Geschlossen)).ToList();
    /// <summary>Rohtypname im .lbrn-Format (Kompatibilidadeits-Kontext: Format-Tagnamen).</summary>
    public abstract string Rohtyp();
}
