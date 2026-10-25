using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Formen;
public sealed class RechteckForm : Form
{
    public double W { get; set; }
    public double H { get; set; }
    public double Eckradius { get; set; }
    public override string Rohtyp() => "Rect";
    public override List<Zug> ZuZuege()
    {
        if (Eckradius <= 0)
        {
            double hw = W/2, hh = H/2;
            return new() { new Zug(new[] { new Punkt(-hw,-hh), new Punkt(-hw,hh), new Punkt(hw,hh), new Punkt(hw,-hh) }, true) };
        }
        // Abgerundete Ecken: Kreisquart je 8 Segmente
        var punkte = new List<Punkt>();
        double hw = W/2, hh = H/2, r = Math.Min(Eckradius, Math.Min(W, H)/2);
        // Ecke unten links -> gegen den Uhrzeigersinn
        punkte.AddRange(Eckbogen(hw - r, hh - r, r, 0, 90, 8));
        punkte.AddRange(Eckbogen(hw - r, hh + r, r, 90, 180, 8)?? new List<Punkt>());
        punkte.AddRange(Eckbogen(-hw + r, hh + r, r, 180, 270, 8));
        punkte.AddRange(Eckbogen(-hw + r, hh - r, r, 270, 360, 8));
        punkte.Add(punkte[0]);
        return new List<Zug>{ new Zug(punkte, true) };
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
    public override List<Zug> ZuZuege()
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
    public override List<Zug> ZuZuege()
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
    public override List<Zug> ZuZuege() => Rohtuege.Select(z => new Zug(z.Punkte.Select(XForm.Anwenden), z.Geschlossen)).ToList();
}
/// <summary>Text-Form: Umriss wird spaeter ueber Systemfont-Raster erzeugt (M3) — M1 liefert leere Zuege.</summary>
public sealed class TextForm : Form
{
    public string Zeichensatz { get; set; } = "Arial";
    public string Text { get; set; } = "";
    public double Hoehe { get; set; } = 10;
    public override string Rohtyp() => "Text";
    public override List<Zug> ZuZuege() => new(); // M3: Font-Zerlegung
}
public sealed class GruppeForm : Form
{
    public List<Form> Kinder { get; set; } = new();
    public override string Rohtyp() => "Group";
    public override List<Zug> ZuZuege() =>
        Kinder.SelectMany(k => k.ZuZuege().Select(z =>
            new Zug(z.Punkte.Select(p => XForm.Anwenden(p)), z.Geschlossen))).ToList();
}
