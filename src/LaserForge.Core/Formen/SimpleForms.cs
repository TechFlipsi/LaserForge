using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Formen;
public sealed class RechteckForm : Form
{
    public double W { get; set; }
    public double H { get; set; }
    public double Eckradius { get; set; }
    public override string Rohtyp() => "Rect";
    protected override List<Zug> Rohezuege()
    {
        double hw = W / 2, hh = H / 2;
        if (Eckradius <= 0)
        {
            return new() { new Zug(new[] { new Punkt(-hw,-hh), new Punkt(-hw,hh), new Punkt(hw,hh), new Punkt(hw,-hh), new Punkt(-hw,-hh) }, true) };
        }
        // Abgerundete Ecken: 8-Segmente-Bogen je Quart
        var punkte = new List<Punkt>();
        double r = Math.Min(Eckradius, Math.Min(W, H) / 2);
        punkte.AddRange(Eckbogen(hw - r, hh - r, r, 0, 90, 8));
        punkte.AddRange(Eckbogen(hw - r, hh + r, r, 90, 180, 8));
        punkte.AddRange(Eckbogen(-hw + r, hh + r, r, 180, 270, 8));
        punkte.AddRange(Eckbogen(-hw + r, hh - r, r, 270, 360, 8));
        punkte.Add(punkte[0]);
        return new List<Zug> { new Zug(punkte, true) };
    }
    private static IEnumerable<Punkt> Eckbogen(double cx, double cy, double r, double a0, double a1, int n)
    {
        var liste = new List<Punkt>();
        for (int i = 0; i <= n; i++)
        {
            double w = (a0 + (a1 - a0) * i / n) * Math.PI / 180.0;
            liste.Add(new Punkt(cx + r * Math.Cos(w), cy + r * Math.Sin(w)));
        }
        return liste;
    }
}
public sealed class EllipseForm : Form
{
    public double Rx { get; set; }
    public double Ry { get; set; }
    public override string Rohtyp() => "Ellipse";
    protected override List<Zug> Rohezuege()
    {
        const int n = 64;
        var punkte = new List<Punkt>();
        for (int i = 0; i < n; i++)
        {
            double w = 2 * Math.PI * i / n;
            punkte.Add(new Punkt(Math.Cos(w) * Rx, Math.Sin(w) * Ry));
        }
        punkte.Add(punkte[0]);
        return new List<Zug>{ new Zug(punkte, true) };
    }
}
public sealed class PolygonForm : Form
{
    public double Rx { get; set; }
    public double Ry { get; set; }
    public int Seiten { get; set; } = 6;
    public override string Rohtyp() => "Polygon";
    protected override List<Zug> Rohezuege()
    {
        var punkte = new List<Punkt>();
        for (int i = 0; i < Seiten; i++)
        {
            double w = 2 * Math.PI * i / Seiten + Math.PI / 2;
            punkte.Add(new Punkt(Math.Cos(w) * Rx, Math.Sin(w) * Ry));
        }
        punkte.Add(punkte[0]);
        return new List<Zug>{ new Zug(punkte, true) };
    }
}
public sealed class PfadForm : Form
{
    public List<Zug> Rohtuege { get; set; } = new();
    public override string Rohtyp() => "Path";
    protected override List<Zug> Rohezuege() => Rohtuege.Select(z => new Zug(z.Punkte.Select(XForm.Anwenden), z.Geschlossen)).ToList();
}
/// <summary>Text-Form: Umriss wird spaeter ueber Systemfont-Raster erzeugt (M3) — M1 liefert leere Zuege.</summary>
public sealed class TextForm : Form
{
    public string Zeichensatz { get; set; } = "Arial";
    public string Text { get; set; } = "";
    public double Hoehe { get; set; } = 10;
    public override string Rohtyp() => "Text";
    protected override List<Zug> Rohezuege() => new(); // M3: Font-Zerlegung
}
public sealed class GruppeForm : Form
{
    public List<Form> Kinder { get; set; } = new();
    public override string Rohtyp() => "Group";
    // Kinder tragen ihre eigene XForm (k.ZuZuege()); die Gruppen-XForm wendet die Basis einmal an.
    protected override List<Zug> Rohezuege() => Kinder.SelectMany(k => k.ZuZuege()).ToList();
}
