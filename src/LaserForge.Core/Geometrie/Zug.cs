namespace LaserForge.Core.Geometrie;
/// <summary>
/// Durchgehender Formzug (Pfad): geschlossener oder offener Linienzug aus Punkten in mm.
/// Bezier-Fragmente werden zum Zeitpunkt der Erzeugung getesselt (GLM-Muster: 24 Segmente je Kurve).
/// </summary>
public sealed class Zug
{
    public List<Punkt> Punkte { get; set; } = new();
    /** Zeilenschliessung: geschlossene Zuege enden wieder am Startpunkt (physikalisch fuer Laser wichtig). */
    public bool Geschlossen { get; set; }
    public Zug() { }
    public Zug(IEnumerable<Punkt> punkte, bool geschlossen)
    {
        Punkte = punkte.ToList();
        Geschlossen = geschlossen;
    }
    public double Laenge()
    {
        double summe = 0;
        for (int i = 1; i < Punkte.Count; i++) summe += Punkte[i].EntfernungZu(Punkte[i - 1]);
        if (Geschlossen && Punkte.Count > 1) summe += Punkte[^1].EntfernungZu(Punkte[0]);
        return summe;
    }
    public Punkt Startpunkt() => Punkte.Count > 0 ? Punkte[0] : Punkt.Null;
}
