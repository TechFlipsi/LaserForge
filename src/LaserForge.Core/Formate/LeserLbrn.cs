using System.Globalization;
using System.Xml.Linq;
using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Formate;
/// <summary>
/// Leser fuer .lbrn (FormatVersion 0: vollstaendige XML-Tags) und .lbrn2 (FormatVersion 1: kompakte Strings).
/// Kompatibilitaets-Hinweis: die XML-Tagnamen des Dateiformats (LightBurnProject, CutSetting, Shape, V, P ...)
/// gehoeren zum bestehenden Dateiformat und werden ausschliesslich zum Lesen bestehender Dateien verwendet.
/// </summary>
public static class LeserLbrn
{
    private static readonly CultureInfo E = CultureInfo.InvariantCulture;

    /// <summary>„True"/„1" -> true: .lbrn schreibt Bool als grosses True/False.</summary>
    public static bool IstDoppel(string? v) =>
        string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || v == "1";

    public static Projekt Lesen(string text)
    {
        var doc = XDocument.Parse(text);
        var wurzel = doc.Root ?? throw new InvalidDataException("Keine Wurzel in der Projekt-Datei.");
        if (wurzel.Name.LocalName != "LightBurnProject")
            throw new InvalidDataException($"Unbekannte Projektwurzel '{wurzel.Name.LocalName}' — nur .lbrn/.lbrn2 wird gelesen.");
        var p = new Projekt
        {
            AppVersion = wurzel.GetString("AppVersion") ?? "1.0.0",
            FormatVersion = wurzel.GetString("FormatVersion") ?? "0",
            MaterialHeight = wurzel.GetDoppel("MaterialHeight") ?? 0,
            MirrorX = IstDoppel(wurzel.GetString("MirrorX")),
            MirrorY = IstDoppel(wurzel.GetString("MirrorY")),
        };
        foreach (var cs in wurzel.Elements("CutSetting"))
        {
            var s = new SchnittEinstellung { Typ = cs.GetString("type") ?? "Cut" };
            foreach (var kind in cs.Elements())
            {
                string name = kind.Name.LocalName;
                string wert = kind.GetString("Value") ?? "";
                switch (name)
                {
                    case "index": s.Index = ZahlInt(wert); break;
                    case "name": s.NameSektion = wert; break;
                    case "minPower": s.MinPower = ZahlD(wert); break;
                    case "maxPower": s.MaxPower = ZahlD(wert); break;
                    case "minPower2": s.MinPower2 = ZahlD(wert); break;
                    case "maxPower2": s.MaxPower2 = ZahlD(wert); break;
                    case "speed": s.Speed = ZahlD(wert); break;
                    case "priority": s.Prioritaet = ZahlInt(wert); break;
                    case "numPasses": s.NumPasses = ZahlInt(wert); break;
                    case "zPerPass": s.ZProPass = ZahlD(wert); break;
                    case "zOffset": s.ZOffset = ZahlD(wert); break;
                    case "lineSpacing": s.LineSpacing = ZahlD(wert); break;
                    case "scanAngle": s.ScanAngle = ZahlD(wert); break;
                    case "runBlower": s.RunBlower = IstDoppel(wert); break;
                    case "kerfWidth": s.KerfWidth = ZahlD(wert); break;
                    case "overcut": s.Overcut = ZahlD(wert); break;
                    case "frequency": s.Frequency = ZahlInt(wert); break;
                    case "doOutput": s.Aktiviert = wert.Trim().ToLowerInvariant() != "false"; break;
                    default: s.RestParameter.Add((name, wert)); break;
                }
            }
            p.Schnitte.Add(s);
        }
        foreach (var sh in wurzel.Elements("Shape"))
            p.Formen.Add(LesenForm(sh));
        // VariableText / Notes / UIPrefs (Format-Realnamen) + Rest-Knoten verlustfrei
        var thumb = wurzel.Element("Thumbnail");
        if (thumb is not null)
            p.ThumbnailBasis64 = thumb.GetString("Source");
        var vt = wurzel.Element("VariableText");
        if (vt is not null)
        {
            var v = new VariableTextEinstellungen();
            foreach (var k in vt.Elements())
            {
                string w = k.GetString("Value") ?? "";
                switch (k.Name.LocalName)
                {
                    case "Start": v.Start = ZahlInt(w); break;
                    case "End": v.Ende = ZahlInt(w); break;
                    case "Current": v.Aktueller = ZahlInt(w); break;
                    case "Increment": v.Schritt = ZahlInt(w); break;
                    case "AutoAdvance": v.AutoWeiterschalten = IstDoppel(w); break;
                }
            }
            p.VariableText = v;
        }
        var notizen = wurzel.Element("Notes");
        if (notizen is not null)
            p.Notizen = new Notizen
            {
                BeiLadenZeigen = IstDoppel(notizen.GetString("ShowOnLoad")),
                Text = notizen.GetString("Notes") ?? "",
            };
        var up = wurzel.Element("UIPrefs");
        if (up is not null)
            foreach (var k in up.Elements())
                p.UIPrefs[k.Name.LocalName] = k.GetString("Value") ?? "";
        foreach (var k in wurzel.Elements())
            if (k.Name.LocalName is not ("CutSetting" or "Shape" or "VariableText" or "Notes" or "UIPrefs" or "Thumbnail"))
                p.RestKnoten.Add(new XElement(k));
        return p;
    }

    public static Form LesenForm(XElement sh)
    {
        Form f;
        switch (sh.GetString("Type"))
        {
            case "Rect":
                f = new RechteckForm
                {
                    W = ZahlD(sh.GetString("W") ?? "0"),
                    H = ZahlD(sh.GetString("H") ?? "0"),
                    Eckradius = ZahlD(sh.GetString("Cr") ?? "0"),
                };
                break;
            case "Ellipse":
                f = new EllipseForm { Rx = ZahlD(sh.GetString("Rx") ?? "0"), Ry = ZahlD(sh.GetString("Ry") ?? "0") };
                break;
            case "Polygon":
                f = new PolygonForm
                {
                    Rx = ZahlD(sh.GetString("Rx") ?? "0"),
                    Ry = ZahlD(sh.GetString("Ry") ?? "0"),
                    Seiten = Math.Max(3, ZahlInt(sh.GetString("N") ?? "6")),
                };
                break;
            case "Text":
                f = new TextForm
                {
                    Zeichensatz = FontName(sh.GetString("Font")),
                    Text = sh.GetString("Str") ?? "",
                    Hoehe = ZahlD(sh.GetString("H") ?? "10"),
                };
                break;
            case "Image":
                f = new BildForm(); // Bild-Daten des .lbrn werden in M3 eingebettet gelesen
                break;
            case "Group":
            {
                var g = new GruppeForm();
                foreach (var kind in sh.Elements("Children").Elements("Shape"))
                    g.Kinder.Add(LesenForm(kind));
                f = g;
                break;
            }
            case "Path":
            {
                var pfad = new PfadForm();
                var vert = new List<(Punkt p, Punkt? c0, Punkt? c1, bool smooth)>();
                foreach (var v in sh.Elements("V"))
                {
                    Punkt pv = new(ZahlD(v.GetString("vx") ?? "0"), ZahlD(v.GetString("vy") ?? "0"));
                    var x0 = v.GetString("c0x"); var y0 = v.GetString("c0y");
                    var x1 = v.GetString("c1x"); var y1 = v.GetString("c1y");
                    Punkt? c0 = (x0 is not null && y0 is not null) ? new Punkt(ZahlD(x0), ZahlD(y0)) : null;
                    Punkt? c1 = (x1 is not null && y1 is not null) ? new Punkt(ZahlD(x1), ZahlD(y1)) : null;
                    vert.Add((pv, c0, c1, v.GetString("S") == "1"));
                }
                var prims = sh.Elements("P").Select(pp =>
                    (pp.GetString("T") ?? "L", ZahlInt(pp.GetString("p0") ?? "0"), ZahlInt(pp.GetString("p1") ?? "1"))).ToList();
                var vl = sh.GetString("VertList");
                if (vl is not null && sh.GetString("PrimList") is { } pl)
                {
                    vert.Clear();
                    foreach (var t in ZerlegeKompakt(vl)) vert.Add((new Punkt(t.x, t.y), null, null, false));
                    prims = ZerlegePrims(pl);
                }
                pfad.Rohtuege = BaueZuege(vert, prims);
                pfad.RoheVerte = vert.Select(vv => new PfadVertex(vv.p, vv.c0, vv.c1, vv.smooth)).ToList();
                pfad.RohePrimse = prims.Select(pp => new PfadPrimitive(pp.Item1, pp.Item2, pp.Item3)).ToList();
                f = pfad;
                break;
            }
            default:
                throw new InvalidDataException($"Unbekannter Shape-Typ '{sh.GetString("Type")}' im Projekt.");
        }
        f.SchnittIndex = ZahlInt(sh.GetString("CutIndex") ?? "0");
        f.Name = sh.GetString("Name");
        var xf = sh.Element("XForm")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(xf)) f.XForm = LesereMatrix(xf);
        return f;
    }

    public static string FontName(string? fontStrophen) => (fontStrophen ?? "").Split(',')[0];

    /// <summary>XForm-Zeile "a b c d e f" parsen.</summary>
    public static Transformationsmatrix LesereMatrix(string text)
    {
        var teile = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (teile.Length != 6) throw new InvalidDataException($"XForm braucht 6 Werte, erhalten: '{text}'");
        return new Transformationsmatrix(ZahlD(teile[0]), ZahlD(teile[1]), ZahlD(teile[2]),
                                         ZahlD(teile[3]), ZahlD(teile[4]), ZahlD(teile[5]));
    }

    /// <summary>lbrn2-kompakte Vertliste "Vx y"-Tokens.</summary>
    public static List<(double x, double y)> ZerlegeKompakt(string text)
    {
        var liste = new List<(double, double)>();
        int i = 0;
        while (i < text.Length)
        {
            while (i < text.Length && text[i] != 'V') i++;
            if (i >= text.Length) break;
            i++;
            var zahlen = new List<double>();
            for (int z = 0; z < 2; z++)
            {
                while (i < text.Length && text[i] == ' ') i++;
                int s = i;
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '-' || text[i] == '.')) i++;
                if (i > s) zahlen.Add(ZahlD(text[s..i]));
            }
            if (zahlen.Count >= 2) liste.Add((zahlen[0], zahlen[1]));
        }
        return liste;
    }

    /// <summary>lbrn2-kompakte Primitives "L0 1"-Tokens.</summary>
    public static List<(string T, int P0, int P1)> ZerlegePrims(string text)
    {
        var liste = new List<(string, int, int)>();
        int i = 0;
        while (i < text.Length)
        {
            while (i < text.Length && !char.IsLetter(text[i])) i++;
            if (i >= text.Length) break;
            string t = text[i].ToString();
            i++;
            var zahlen = new List<int>();
            for (int z = 0; z < 2; z++)
            {
                while (i < text.Length && text[i] == ' ') i++;
                int s = i;
                while (i < text.Length && char.IsDigit(text[i])) i++;
                if (i > s) zahlen.Add(ZahlInt(text[s..i]));
            }
            if (zahlen.Count >= 1) liste.Add((t, zahlen[0], zahlen.Count > 1 ? zahlen[1] : zahlen[0]));
        }
        return liste;
    }

    /// <summary>Vertexliste + Primitives -> Zuege; Bezier mit 24 Segmenten tasselieren (23 Zwischen + Endpunkt).</summary>
    public static List<Zug> BaueZuege(List<(Punkt p, Punkt? c0, Punkt? c1, bool smooth)> vert,
                                      List<(string T, int P0, int P1)> prims)
    {
        var zuege = new List<Zug>();
        var laufend = new List<Punkt>();
        void Start() { if (laufend.Count > 1) zuege.Add(new Zug(laufend, false)); laufend = new(); }
        foreach (var (t, p0, p1) in prims)
        {
            if (p0 < 0 || p1 < 0 || p0 >= vert.Count || p1 >= vert.Count) continue;
            var a = vert[p0].p;
            var b = vert[p1].p;
            if (laufend.Count == 0) laufend.Add(a);
            else if (laufend[^1] != a) { Start(); laufend.Add(a); }
            if (t == "B")
            {
                var c0 = vert[p0].c0 ?? new Punkt((a.X + b.X) / 2, (a.Y + b.Y) / 2);
                var c1 = vert[p1].c1 ?? new Punkt((a.X + c0.X) / 2, (a.Y + c0.Y) / 2);
                for (int k = 1; k <= 23; k++)
                {
                    double z = k / 24.0, u = 1 - z;
                    double x = u*u*u*a.X + 3*u*u*z*c0.X + 3*u*z*z*c1.X + z*z*z*b.X;
                    double y = u*u*u*a.Y + 3*u*u*z*c0.Y + 3*u*z*z*c1.Y + z*z*z*b.Y;
                    laufend.Add(new Punkt(x, y));
                }
            }
            laufend.Add(b);
        }
        Start();
        return zuege;
    }

    internal static double ZahlD(string? s) => double.Parse((s ?? "0").Trim(), E);
    internal static int ZahlInt(string? s) => int.Parse((s ?? "0").Trim(), E);
}
