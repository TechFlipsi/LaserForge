using System.Globalization;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Formate;
/// <summary>
/// Minimaler DXF-Leser (ASCII): LINE, CIRCLE, ARC, LWPOLYLINE, POLYLINE/VERTEX, ELLIPSE.
/// </summary>
public static class LeserDxf
{
    private static readonly CultureInfo E = CultureInfo.InvariantCulture;
    public static PfadForm Lesen(string dxfText, string? name = null)
    {
        var zeilen = dxfText.Replace("\r", "").Split('\n');
        var paare = new List<(int Code, string Wert)>();
        for (int i = 0; i + 1 < zeilen.Length; i++)
            if (int.TryParse(zeilen[i].Trim(), out var code))
                paare.Add((code, zeilen[i + 1].Trim()));
        var pfad = new PfadForm { Name = name ?? "DXF-Import" };
        int i2 = 0;
        while (i2 < paare.Count)
        {
            var (code, wert) = paare[i2];
            if (code == 0 && IstEntitaet(wert))
            {
                int ende = i2 + 1;
                while (ende < paare.Count && paare[ende].Code != 0) ende++;
                SchreibeZuege(pfad, wert.ToUpperInvariant(), paare[i2..ende]);
                i2 = ende;
            }
            else i2++;
        }
        return pfad;
    }
    private static bool IstEntitaet(string w) =>
        w is "LINE" or "CIRCLE" or "ARC" or "LWPOLYLINE" or "POLYLINE" or "VERTEX" or "ELLIPSE" or "SEQEND";
    private static double D(string v) => double.TryParse(v, NumberStyles.Any, E, out var d) ? d : 0;
    private static double? Wert(List<(int Code, string Wert)> e, int code)
    {
        foreach (var (c, v) in e) if (c == code) return D(v);
        return null;
    }
    private static int IntW(List<(int Code, string Wert)> e, int code)
    {
        foreach (var (c, v) in e) if (c == code) { int.TryParse(v, NumberStyles.Any, E, out var i); return i; }
        return 0;
    }
    private static void SchreibeZuege(PfadForm pfad, string typ, List<(int Code, string Wert)> e)
    {
        switch (typ)
        {
            case "LINE":
            {
                double? x1 = Wert(e, 10), y1 = Wert(e, 20), x2 = Wert(e, 11), y2 = Wert(e, 21);
                if (x1.HasValue && y1.HasValue && x2.HasValue && y2.HasValue)
                    pfad.Rohtuege.Add(new Zug(new[] { new Punkt(x1.Value, y1.Value), new Punkt(x2.Value, y2.Value) }, false));
                break;
            }
            case "CIRCLE":
            {
                var c = new Punkt(Wert(e, 10) ?? 0, Wert(e, 20) ?? 0);
                double r = Wert(e, 40) ?? 0;
                if (r > 0) pfad.Rohtuege.Add(Kreis(c, r));
                break;
            }
            case "ARC":
            {
                var c = new Punkt(Wert(e, 10) ?? 0, Wert(e, 20) ?? 0);
                double r = Wert(e, 40) ?? 0;
                double a0 = Wert(e, 50) ?? 0;
                double a1 = Wert(e, 51) ?? 360;
                if (a1 <= a0) a1 += 360;
                var punkte = new List<Punkt>();
                const int n = 32;
                for (int k = 0; k <= n; k++)
                {
                    double w = (a0 + (a1 - a0) * k / n) * Math.PI / 180.0;
                    punkte.Add(new Punkt(c.X + r * Math.Cos(w), c.Y + r * Math.Sin(w)));
                }
                pfad.Rohtuege.Add(new Zug(punkte, punkte[0] == punkte[^1]));
                break;
            }
            case "LWPOLYLINE":
            {
                var punkte = new List<Punkt>();
                for (int k = 0; k < e.Count; k++)
                    if (e[k].Code == 10 && k + 1 < e.Count && e[k + 1].Code == 20)
                    { punkte.Add(new Punkt(D(e[k].Wert), D(e[k + 1].Wert))); k++; }
                bool zu = (IntW(e, 70) & 1) == 1;
                if (punkte.Count > 1)
                {
                    if (zu) punkte.Add(punkte[0]);
                    pfad.Rohtuege.Add(new Zug(punkte, zu));
                }
                break;
            }
            case "POLYLINE":
            {
                var punkte = new List<Punkt>();
                bool zu = false;
                for (int k = 0; k < e.Count; k++)
                {
                    if (e[k].Code == 70) zu = (IntW(e, 70) & 1) == 1;
                    if (e[k].Code != 0 || e[k].Wert.ToUpperInvariant() != "VERTEX") continue;
                    double? x = null, y = null;
                    int m = k + 1;
                    while (m < e.Count && e[m].Code != 0)
                    {
                        if (e[m].Code == 10) x = D(e[m].Wert);
                        if (e[m].Code == 20) y = D(e[m].Wert);
                        m++;
                    }
                    if (x.HasValue && y.HasValue) punkte.Add(new Punkt(x.Value, y.Value));
                }
                if (punkte.Count > 1)
                {
                    if (zu) punkte.Add(punkte[0]);
                    pfad.Rohtuege.Add(new Zug(punkte, zu));
                }
                break;
            }
            case "ELLIPSE":
            {
                var c = new Punkt(Wert(e, 10) ?? 0, Wert(e, 20) ?? 0);
                double dx = Wert(e, 11) ?? 1, dy = Wert(e, 21) ?? 0;
                double verhaeltnis = Wert(e, 40) ?? 1;
                double t0 = Wert(e, 41) ?? 0;
                double t1 = Wert(e, 42) ?? Math.PI * 2;
                double rmaj = Math.Sqrt(dx * dx + dy * dy);
                if (rmaj <= 0) break;
                double ix = dx / rmaj, iy = dy / rmaj;
                var punkte = new List<Punkt>();
                const int n = 48;
                for (int k = 0; k <= n; k++)
                {
                    double t = t0 + (t1 - t0) * k / n;
                    punkte.Add(new Punkt(c.X + rmaj * Math.Cos(t) * ix + rmaj * verhaeltnis * Math.Sin(t) * (-iy),
                                         c.Y + rmaj * Math.Cos(t) * iy + rmaj * verhaeltnis * Math.Sin(t) * ix));
                }
                pfad.Rohtuege.Add(new Zug(punkte, punkte[0] == punkte[^1]));
                break;
            }
        }
    }
    private static Zug Kreis(Punkt c, double r)
    {
        var punkte = new List<Punkt>();
        const int n = 64;
        for (int i = 0; i < n; i++)
        {
            double w = 2 * Math.PI * i / n;
            punkte.Add(new Punkt(c.X + r * Math.Cos(w), c.Y + r * Math.Sin(w)));
        }
        punkte.Add(punkte[0]);
        return new Zug(punkte, true);
    }
}
