using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
using Xunit;
namespace LaserForge.Tests;
public class FormenTests
{
    [Fact]
    public void Rechteck_Geschlossener_Zug_Mit_4_Ecken()
    {
        var zug = Assert.Single(new RechteckForm { W = 50, H = 80 }.ZuZuege());
        Assert.True(zug.Geschlossen);
        Assert.Equal(5, zug.Punkte.Count);
        Assert.Equal(new Punkt(-25, -40), zug.Punkte[0]);
    }
    [Fact]
    public void Rechteck_Mit_XForm()
    {
        var f = new RechteckForm { W = 10, H = 10, XForm = Transformationsmatrix.Verschieben(100, 200) };
        Assert.Equal(new Punkt(95, 195), f.ZuZuege()[0].Punkte[0]);
    }
    [Fact]
    public void Ellipse_64_Segmente()
    {
        var zug = Assert.Single(new EllipseForm { Rx = 10, Ry = 10 }.ZuZuege());
        Assert.True(zug.Geschlossen);
        Assert.Equal(65, zug.Punkte.Count);
        Assert.Equal(new Punkt(10, 0), zug.Punkte[0]);
    }
    [Fact] public void Polygon_6_Seiten() =>
        Assert.Equal(7, Assert.Single(new PolygonForm { Rx = 20, Ry = 20, Seiten = 6 }.ZuZuege()).Punkte.Count);
    [Fact]
    public void Gruppe_Kombiniert_Kinder_und_XForm()
    {
        var kind = new RechteckForm { W = 4, H = 4, XForm = Transformationsmatrix.Verschieben(2, 2) };
        var gruppe = new GruppeForm { XForm = Transformationsmatrix.Verschieben(10, 0) };
        gruppe.Kinder.Add(kind);
        Assert.Equal(new Punkt(10, 0), gruppe.ZuZuege()[0].Punkte[0]);
    }
    [Fact] public void Text_M1_leer() => Assert.Empty(new TextForm().ZuZuege());
}
