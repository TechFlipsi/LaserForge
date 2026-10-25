using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Pipeline;
/// <summary>Optimierer-Optionen (eigene Abbild der Optimierungsschalter).</summary>
public sealed class OptimiererOptionen
{
    public bool InnenErst { get; set; } = true;
    public bool NaechsteNachbarSortierung { get; set; } = true;
    public Punkt Startpunkt { get; set; } = new(0, 0);
}
/// <summary>
/// Pfadoptimierung: Schachtelungstiefe (innen-erst: tiefere Konturen zuerst), innerhalb
/// gleicher Tiefe die naechste-Nachbar-Kette vom Startpunkt (kurze Luftfahrten).
/// </summary>
public static class Optimierer
{
    public static List<Zug> Ordne(IEnumerable<Zug> zuege, OptimiererOptionen? o = null)
    {
        var opt = o ?? new OptimiererOptionen();
        var liste = zuege.Where(z => z.Punkte.Count > 1).Select(z => new Eintrag(z)).ToList();
        if (liste.Count == 0) return new List<Zug>();
        foreach (var e in liste)
            e.Tiefe = opt.InnenErst
                ? liste.Count(x => x != e && x.Zug.Geschlossen && EnthaeltPunkt(x.Zug, e.Zug.Startpunkt()))
                : 0;
        // Ebenen (Tiefe) absteigend, in jeder Ebene die naechste-Nachbar-Kette
        var klassen = liste.GroupBy(e => e.Tiefe).OrderByDescending(g => g.Key).ToList();
        var ergebnis = new List<Zug>();
        Punkt pos = opt.Startpunkt;
        foreach (var klasse in klassen)
        {
            var rest = klasse.ToList();
            while (rest.Count > 0)
            {
                Eintrag? best = null; double beste = double.MaxValue;
                foreach (var e in rest)
                {
                    if (!opt.NaechsteNachbarSortierung) { best = e; break; }
                    double d = e.Zug.Startpunkt().EntfernungZu(pos);
                    if (d < beste) { beste = d; best = e; }
                }
                rest.Remove(best!);
                ergebnis.Add(best!.Zug);
                pos = best.Zug.Startpunkt();
            }
        }
        return ergebnis;
    }
    /// <summary>Strahlwurf: Punkt-in-Polygon (fuer geschlossene Zuege, Schlusspunkt doppelt erlaubt).</summary>
    public static bool EnthaeltPunkt(Zug polygon, Punkt p)
    {
        var pk = polygon.Punkte;
        int n = pk.Count;
        if (n < 3) return false;
        var liste = (n > 1 && pk[0] == pk[^1]) ? pk.Take(n - 1).ToList() : pk.ToList();
        bool innen = false;
        int m = liste.Count;
        for (int i = 0, j = m - 1; i < m; j = i++)
        {
            var a = liste[i]; var b = liste[j];
            if ((a.Y > p.Y) == (b.Y > p.Y)) continue;
            double xSchnitt = (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X;
            if (p.X < xSchnitt) innen = !innen;
        }
        return innen;
    }
    private sealed class Eintrag
    {
        public Zug Zug { get; }
        public int Tiefe { get; set; }
        public Eintrag(Zug zug) { Zug = zug; }
    }
}
