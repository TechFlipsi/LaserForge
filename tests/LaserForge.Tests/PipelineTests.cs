using LaserForge.Core.Einstellungen;
using LaserForge.Core.Geometrie;
using LaserForge.Core.Pipeline;
using Xunit;
namespace LaserForge.Tests;
public class PipelineTests
{
    private static Zug Viereck(double x0, double y0, double x1, double y1) => new(
        new[] { new Punkt(x0, y0), new Punkt(x0, y1), new Punkt(x1, y1), new Punkt(x1, y0), new Punkt(x0, y0) }, true);
    [Fact]
    public void Innen_Erst()
    {
        var zuege = new List<Zug> { Viereck(0, 0, 100, 100), Viereck(25, 25, 75, 75) };
        var geordnet = Optimierer.Ordne(zuege, new OptimiererOptionen { NaechsteNachbarSortierung = false });
        Assert.True(Optimierer.EnthaeltPunkt(geordnet[1], geordnet[0].Startpunkt()),
            "Innere Kontur zuerst (innen-erst).");
    }
    [Fact]
    public void Enthaelt_Punkt()
    {
        var q = Viereck(0, 0, 10, 10);
        Assert.True(Optimierer.EnthaeltPunkt(q, new Punkt(5, 5)));
        Assert.False(Optimierer.EnthaeltPunkt(q, new Punkt(15, 5)));
    }
    [Fact]
    public void Naechste_Nachbar_Kette()
    {
        var zuege = new List<Zug>
        {
            Viereck(1000, 1000, 1020, 1020),
            Viereck(2, 2, 12, 12),
            Viereck(500, 500, 510, 510),
        };
        var geordnet = Optimierer.Ordne(zuege, new OptimiererOptionen { InnenErst = false });
        Assert.Equal(new Punkt(2, 2), geordnet[0].Startpunkt());
        Assert.Equal(new Punkt(500, 500), geordnet[1].Startpunkt());
        Assert.Equal(new Punkt(1000, 1000), geordnet[2].Startpunkt());
    }
    [Fact]
    public void Kontur_GRBL()
    {
        var e = new SchnittEinstellung { Typ = "Cut", MinPower = 80, MaxPower = 80, Speed = 30, NumPasses = 1 };
        var zeilen = GCodeErzeuger.Kontur(Viereck(0, 0, 10, 10), e, new GCodeOptionen());
        Assert.Equal(7, zeilen.Count);
        Assert.Equal("G0 X0 Y0", zeilen[0]);
        Assert.Equal("M4 S800", zeilen[1]);
        Assert.StartsWith("G1 X0 Y10 F1800", zeilen[2]);
        Assert.Equal("M5", zeilen[^1]);
        Assert.All(zeilen, z => Assert.DoesNotContain(',', z));
    }
    [Fact]
    public void Kontur_Zwei_Passen()
    {
        var e = new SchnittEinstellung { Typ = "Cut", MinPower = 80, MaxPower = 80, Speed = 30, NumPasses = 2 };
        var zeilen = GCodeErzeuger.Kontur(Viereck(0, 0, 10, 10), e, new GCodeOptionen());
        Assert.Equal(2, zeilen.Count(zeile => zeile.StartsWith("M4")));
        Assert.Equal(14, zeilen.Count); // G0 + (M4+4G1+M5)*2 + Zwischen-G0
    }
    [Fact]
    public void Raster_Bidi()
    {
        var e = new SchnittEinstellung { Typ = "Scan", MinPower = 40, MaxPower = 40, Speed = 200, LineSpacing = 1 };
        var zeilen = GCodeErzeuger.Raster(new[] { Viereck(0, 0, 10, 10) }, e, new GCodeOptionen());
        Assert.Equal(44, zeilen.Count);
        Assert.Contains("G0 X0 Y1", zeilen);
        Assert.Contains("G0 X10 Y1", zeilen);
        Assert.Contains("M4 S400", zeilen);
    }
    [Fact]
    public void Rahmen()
    {
        var zeilen = GCodeErzeuger.Rahmen(0, 0, 400, 400, new GCodeOptionen { RahmenPower = 2 });
        Assert.Equal("G0 X0 Y0", zeilen[0]);
        Assert.Equal("M4 S20", zeilen[1]);
        Assert.Equal(7, zeilen.Count);
    }
}
