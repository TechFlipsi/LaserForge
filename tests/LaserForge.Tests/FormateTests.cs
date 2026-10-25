using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
using Xunit;
namespace LaserForge.Tests;
public class FormateTests
{
    [Fact]
    public void Schreiben_und_Ruecklesen()
    {
        var p = new Projekt();
        p.Schnitte.Add(new SchnittEinstellung { Index = 0, Typ = "Cut", NameSektion = "SchnittA", MinPower = 80, MaxPower = 88, Speed = 30, NumPasses = 2 });
        p.Schnitte.Add(new SchnittEinstellung { Index = 1, Typ = "Scan", NameSektion = "GravurB", MinPower = 40, MaxPower = 40, Speed = 200, LineSpacing = 0.08, ScanAngle = 90 });
        p.Formen.Add(new RechteckForm { W = 50, H = 100, SchnittIndex = 0, XForm = Transformationsmatrix.Verschieben(100, 50) });
        p.Formen.Add(new EllipseForm { Rx = 25, Ry = 25, SchnittIndex = 1 });
        var zu = LeserLbrn.Lesen(SchreiberLbrn.Schreiben(p));
        Assert.Equal(2, zu.Schnitte.Count);
        Assert.Equal("Cut", zu.Schnitte[0].Typ);
        Assert.Equal(88, zu.Schnitte[0].MaxPower);
        Assert.Equal(2, zu.Schnitte[0].NumPasses);
        Assert.Equal(0.08, zu.Schnitte[1].LineSpacing, 12);
        Assert.Equal(2, zu.Formen.Count);
        var r = Assert.IsType<RechteckForm>(zu.Formen[0]);
        Assert.Equal(50, r.W); Assert.Equal(100, r.H); Assert.Equal(100, r.XForm.E);
    }
    [Fact]
    public void Nameplate_Fixture_Lesebar()
    {
        var pfad = Path.Combine(AppContext.BaseDirectory, "Fixtures", "nameplate.lbrn");
        Assert.True(File.Exists(pfad), $"Fixture fehlt: {pfad}");
        var p = LeserLbrn.Lesen(File.ReadAllText(pfad));
        Assert.True(p.Schnitte.Count > 0);
        Assert.True(p.Formen.Count > 0);
        foreach (var s in p.Schnitte) Assert.True(s.Speed > 0, $"Schnitt {s.Index} braucht Speed>0");
    }
    [Fact]
    public void Kompakte_Tokens_lbrn2()
    {
        Assert.Equal(new[] { (0.0, 0.0), (10.0, 20.0) }, LeserLbrn.ZerlegeKompakt("V0 0V10 20").ToArray());
        Assert.Equal(new[] { ("L", 0, 1) }, LeserLbrn.ZerlegePrims("L0 1").ToArray());
    }
    [Fact]
    public void Svg_Zuege()
    {
        var zug = Assert.Single(LeserSvg.ZuegeAusD("M0 0 L10 0 L10 10 Z"));
        Assert.True(zug.Geschlossen);
        Assert.Equal(4, zug.Punkte.Count);
        Assert.Equal(new Punkt(10, 10), zug.Punkte[2]);
    }
    [Fact]
    public void Svg_Relativ()
    {
        var zug = Assert.Single(LeserSvg.ZuegeAusD("M 5 5 l 10 0 l 0 10 z"));
        Assert.Equal(4, zug.Punkte.Count);
        Assert.Equal(new Punkt(15, 5), zug.Punkte[1]);
        Assert.Equal(new Punkt(15, 15), zug.Punkte[2]);
    }
    [Fact]
    public void Svg_Bezier()
    {
        var zug = Assert.Single(LeserSvg.ZuegeAusD("M0 0 C10 0 20 10 30 10"));
        Assert.Equal(24, zug.Punkte.Count);
        Assert.Equal(new Punkt(30, 10), zug.Punkte[^1]);
    }
    [Fact]
    public void Fontname()
    {
        Assert.Equal("Arial", LeserLbrn.FontName("Arial,-1,100,5,50,0,0,0,0,0"));
        Assert.Equal("", LeserLbrn.FontName(null));
    }
}
