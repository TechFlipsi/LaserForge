using System.Globalization;
using System.Xml.Linq;
using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Formate;
/// <summary>
/// Schreiber fuer .lbrn (FormatVersion 0: vollstaendige XML-Tags — von LightBurn direkt lesbar).
/// Kompatibilitaets-Hinweis: Tagnamen des Dateiformats (LightBurnProject, CutSetting, Shape ...) nur
/// fuer die Interoperabilitaet; kein LightBurn-Code involved.
/// </summary>
public static class SchreiberLbrn
{
    private static readonly CultureInfo E = CultureInfo.InvariantCulture;
    public static string Schreiben(Projekt p)
    {
        var wurzel = new XElement("LightBurnProject",
            new XAttribute("AppVersion", p.AppVersion),
            new XAttribute("FormatVersion", p.FormatVersion),
            new XAttribute("MaterialHeight", Doppel(p.MaterialHeight)),
            new XAttribute("MirrorX", Boolsch(p.MirrorX)),
            new XAttribute("MirrorY", Boolsch(p.MirrorY)));
        foreach (var s in p.GeordneteSchnitte()) wurzel.Add(SchreibeSchnitt(s));
        foreach (var f in p.Formen) wurzel.Add(SchreibeForm(f));
        var dekl = new XDeclaration("1.0", "UTF-8", null);
        return new XDocument(dekl, wurzel).ToString().Replace("\r\n", "\n") + "\n";
    }
    private static XElement SchreibeSchnitt(SchnittEinstellung s)
    {
        var cs = new XElement("CutSetting", new XAttribute("type", s.Typ));
        void Feld(string name, string v) => cs.Add(new XElement(name, new XAttribute("Value", v)));
        Feld("index", s.Index.ToString(E));
        if (s.NameSektion.Length > 0) Feld("name", s.NameSektion);
        Feld("minPower", Doppel(s.MinPower));
        Feld("maxPower", Doppel(s.MaxPower));
        Feld("minPower2", Doppel(s.MinPower));
        Feld("maxPower2", Doppel(s.MaxPower));
        Feld("speed", Doppel(s.Speed));
        Feld("priority", s.Prioritaet.ToString(E));
        Feld("runBlower", Boolsch(s.RunBlower));
        Feld("zOffset", Doppel(s.ZOffset));
        Feld("numPasses", s.NumPasses.ToString(E));
        Feld("zPerPass", Doppel(s.ZProPass));
        if (!s.Aktiviert) Feld("doOutput", "False");
        if (Math.Abs(s.LineSpacing - 0.1) > 1e-12) Feld("lineSpacing", Doppel(s.LineSpacing));
        if (Math.Abs(s.ScanAngle) > 1e-12) Feld("scanAngle", Doppel(s.ScanAngle));
        if (s.KerfWidth > 0) Feld("kerfWidth", Doppel(s.KerfWidth));
        if (s.Overcut > 0) Feld("overcut", Doppel(s.Overcut));
        if (s.Frequency > 0) Feld("frequency", s.Frequency.ToString(E));
        foreach (var (name, wert) in s.RestParameter) Feld(name, wert);
        return cs;
    }
    private static XElement SchreibeForm(Form f)
    {
        var sh = new XElement("Shape", new XAttribute("Type", f.Rohtyp()));
        switch (f)
        {
            case GruppeForm g:
                if (!string.IsNullOrEmpty(f.Name)) sh.Add(new XAttribute("Name", f.Name));
                sh.Add(new XElement("XForm", f.XForm.ZuXFormText()));
                var kinder = new XElement("Children");
                foreach (var k in g.Kinder) kinder.Add(SchreibeForm(k));
                sh.Add(kinder);
                return sh;
            case RechteckForm rf:
                sh.Add(new XAttribute("W", Doppel(rf.W)));
                sh.Add(new XAttribute("H", Doppel(rf.H)));
                if (rf.Eckradius > 0) sh.Add(new XAttribute("Cr", Doppel(rf.Eckradius)));
                break;
            case EllipseForm ef:
                sh.Add(new XAttribute("Rx", Doppel(ef.Rx)));
                sh.Add(new XAttribute("Ry", Doppel(ef.Ry)));
                break;
            case PolygonForm pf:
                sh.Add(new XAttribute("Rx", Doppel(pf.Rx)));
                sh.Add(new XAttribute("Ry", Doppel(pf.Ry)));
                sh.Add(new XAttribute("N", pf.Seiten.ToString(E)));
                break;
            case TextForm tf:
                sh.Add(new XAttribute("Font", $"{tf.Zeichensatz},-1,100,5,{tf.Hoehe.ToString("0", E)},0,0,0,0,0"));
                sh.Add(new XAttribute("Str", tf.Text));
                sh.Add(new XAttribute("H", Doppel(tf.Hoehe)));
                break;
            case PfadForm ff:
                var punkte = ff.Rohtuege.SelectMany(z => z.Punkte).ToList();
                foreach (var pk in punkte)
                    sh.Add(new XElement("V", new XAttribute("vx", Doppel(pk.X)), new XAttribute("vy", Doppel(pk.Y))));
                for (int i = 1; i < punkte.Count; i++)
                    sh.Add(new XElement("P", new XAttribute("T", "L"),
                        new XAttribute("p0", i - 1), new XAttribute("p1", i)));
                break;
        }
        sh.Add(new XAttribute("CutIndex", f.SchnittIndex.ToString(E)));
        if (!string.IsNullOrEmpty(f.Name)) sh.Add(new XAttribute("Name", f.Name));
        sh.Add(new XElement("XForm", f.XForm.ZuXFormText()));
        return sh;
    }
    internal static string Doppel(double v) => v.ToString("0.####", E);
    internal static string Boolsch(bool v) => v ? "True" : "False";
}
