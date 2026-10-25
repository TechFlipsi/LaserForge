using System.Globalization;
using System.Text.RegularExpressions;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Formate;
/// <summary>
/// Minimaler SVG-Leser fuer &lt;path d="..."&gt;: M/L/H/V/Z/C/S/Q/T (absolut und relativ).
/// A-Arcs werden in M1 als Linie zum Endpunkt genaehert (Ausbau in M3: echte Ellipsenboegen).
/// </summary>
public static class LeserSvg
{
    private static readonly Regex TokenE =
        new(@"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled);

    public static PfadForm Lesen(string svgText, string? name = null)
    {
        var pfad = new PfadForm { Name = name ?? "SVG-Import" };
        foreach (var z in AlleZuege(svgText)) pfad.Rohtuege.Add(z);
        return pfad;
    }

    /// <summary>Alle pfad-Elemente der SVG-Datei als Zugliste (naive d-Extraktion).</summary>
    public static List<Zug> AlleZuege(string svgText)
    {
        var zuege = new List<Zug>();
        int idx = 0;
        while (true)
        {
            idx = svgText.IndexOf("d=\"", idx, StringComparison.Ordinal);
            if (idx < 0) break;
            int von = idx + 3;
            int bis = svgText.IndexOf('"', von);
            if (bis < 0) break;
            zuege.AddRange(ZuegeAusD(svgText[von..bis]));
            idx = bis + 1;
        }
        return zuege;
    }

    public static List<Zug> ZuegeAusD(string d)
    {
        var einheiten = new List<(char Befehl, List<double> Argumente)>();
        char? aktBefehl = null;
        var zahlen = new List<double>();
        foreach (Match m in TokenE.Matches(d))
        {
            var t = m.Value;
            if (char.IsLetter(t, 0) && t.Length == 1)
            {
                if (aktBefehl is not null) einheiten.Add((aktBefehl.Value, zahlen));
                aktBefehl = t[0];
                zahlen = new List<double>();
            }
            else if (double.TryParse(t, CultureInfo.InvariantCulture, out var v))
                zahlen.Add(v);
        }
        if (aktBefehl is not null) einheiten.Add((aktBefehl.Value, zahlen));

        var zuege = new List<Zug>();
        var zug = new List<Punkt>();
        bool zugOffen = false;
        Punkt cp = new(0, 0);
        Punkt? letzterKontrollWuerfel = null;
        Punkt? letzterKontrollQuadrat = null;

        void Beginne(Punkt p)
        {
            if (zugOffen && zug.Count > 1) zuege.Add(new Zug(zug, false));
            zug = new List<Punkt> { p };
            zugOffen = true;
            cp = p;
        }
        void Zu(Punkt p) { zug.Add(p); cp = p; }
        void Schliesse()
        {
            if (zugOffen && zug.Count > 1) { zug = zug.Append(zug[0]).ToList(); zuege.Add(new Zug(zug, true)); }
            zug = new List<Punkt>();
            zugOffen = false;
        }
        double N(List<double> a, int i, bool rel, double basis)
        {
            double v = i >= 0 && i < a.Count ? a[i] : 0;
            return rel ? basis + v : v;
        }

        foreach (var (befehl, args) in einheiten)
        {
            bool rel = char.IsLower(befehl);
            char B = char.ToUpperInvariant(befehl);
            int i = 0;
            switch (B)
            {
                case 'M':
                {
                    bool erster = true;
                    while (i + 1 < args.Count)
                    {
                        var p = new Punkt(N(args, i, rel && !erster, cp.X), N(args, i + 1, rel && !erster, cp.Y));
                        if (erster) { Beginne(p); erster = false; }
                        else Zu(p);
                        i += 2;
                    }
                    letzterKontrollWuerfel = null; letzterKontrollQuadrat = null;
                    break;
                }
                case 'L':
                    while (i + 1 < args.Count) { Zu(new Punkt(N(args, i, rel, cp.X), N(args, i + 1, rel, cp.Y))); i += 2; }
                    letzterKontrollWuerfel = null; letzterKontrollQuadrat = null;
                    break;
                case 'H':
                    while (i < args.Count) { Zu(new Punkt(N(args, i, rel, cp.X), cp.Y)); i++; }
                    break;
                case 'V':
                    while (i < args.Count) { Zu(new Punkt(cp.X, N(args, i, rel, cp.Y))); i++; }
                    break;
                case 'Z':
                    Schliesse();
                    break;
                case 'C':
                {
                    while (i + 5 < args.Count || (i + 5 == args.Count - 0 && args.Count - i >= 6))
                    {
                        var c0 = new Punkt(N(args, i, rel, cp.X), N(args, i + 1, rel, cp.Y));
                        var c1 = new Punkt(N(args, i + 2, rel, cp.X), N(args, i + 3, rel, cp.Y));
                        var e = new Punkt(N(args, i + 4, rel, cp.X), N(args, i + 5, rel, cp.Y));
                        BezierFalten(zug, cp, c0, c1, e);
                        cp = e;
                        letzterKontrollWuerfel = c1;
                        i += 6;
                    }
                    break;
                }
                case 'S':
                {
                    while (args.Count - i >= 4)
                    {
                        var c1 = new Punkt(N(args, i, rel, cp.X), N(args, i + 1, rel, cp.Y));
                        var e = new Punkt(N(args, i + 2, rel, cp.X), N(args, i + 3, rel, cp.Y));
                        var c0 = ersterKontroll(cp, letzterKontrollWuerfel);
                        BezierFalten(zug, cp, c0, c1, e);
                        cp = e;
                        letzterKontrollWuerfel = c1;
                        i += 4;
                    }
                    break;
                }
                case 'Q':
                {
                    while (args.Count - i >= 4)
                    {
                        var k = new Punkt(N(args, i, rel, cp.X), N(args, i + 1, rel, cp.Y));
                        var e = new Punkt(N(args, i + 2, rel, cp.X), N(args, i + 3, rel, cp.Y));
                        QuadFalten(zug, cp, k, e);
                        cp = e;
                        letzterKontrollQuadrat = k;
                        i += 4;
                    }
                    break;
                }
                case 'T':
                {
                    while (args.Count - i >= 2)
                    {
                        var e = new Punkt(N(args, i, rel, cp.X), N(args, i + 1, rel, cp.Y));
                        var k = ersterKontroll(cp, letzterKontrollQuadrat);
                        QuadFalten(zug, cp, k, e);
                        cp = e;
                        letzterKontrollQuadrat = k;
                        i += 2;
                    }
                    break;
                }
                case 'A':
                {
                    while (args.Count - i >= 7)
                    {
                        var e = new Punkt(N(args, i + 5, rel, cp.X), N(args, i + 6, rel, cp.Y));
                        Zu(e);
                        i += 7;
                    }
                    break;
                }
            }
        }
        if (zugOffen && zug.Count > 1) zuege.Add(new Zug(zug, false));
        return zuege;
    }
    private static Punkt ersterKontroll(Punkt cp, Punkt? spiegel) =>
        spiegel is null ? cp : new Punkt(2 * cp.X - spiegel.Value.X, 2 * cp.Y - spiegel.Value.Y);
    private static void BezierFalten(List<Punkt> zug, Punkt a, Punkt c0, Punkt c1, Punkt b)
    {
        for (int k = 1; k <= 23; k++)
        {
            double z = k / 24.0, u = 1 - z;
            double x = Math.Pow(u, 3) * a.X + 3 * u * u * z * c0.X + 3 * u * z * z * c1.X + Math.Pow(z, 3) * b.X;
            double y = Math.Pow(u, 3) * a.Y + 3 * u * u * z * c0.Y + 3 * u * z * z * c1.Y + Math.Pow(z, 3) * b.Y;
            zug.Add(new Punkt(x, y));
        }
        zug.Add(b);
    }
    private static void QuadFalten(List<Punkt> zug, Punkt a, Punkt k, Punkt b)
    {
        for (int n = 1; n <= 11; n++)
        {
            double z = n / 12.0, u = 1 - z;
            double x = u * u * a.X + 2 * u * z * k.X + z * z * b.X;
            double y = u * u * a.Y + 2 * u * z * k.Y + z * z * b.Y;
            zug.Add(new Punkt(x, y));
        }
        zug.Add(b);
    }
}
