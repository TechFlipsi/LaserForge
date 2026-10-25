using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
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
    public void Innen_Erst_mit_Nachbarkette()
    {
        var zuege = new List<Zug> { Viereck(0, 0, 100, 100), Viereck(25, 25, 75, 75) };
        var geordnet = Optimierer.Ordne(zuege, new OptimiererOptionen());
        // Tiefe entscheidet vor Nahe: Innen erst, dann Aussen
        Assert.True(Optimierer.EnthaeltPunkt(geordnet[1], geordnet[0].Startpunkt()));
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
    public void Kontur_Golden()
    {
        var e = new SchnittEinstellung { Typ = "Cut", MinPower = 80, MaxPower = 80, Speed = 30, NumPasses = 1 };
        var zeilen = GCodeErzeuger.Kontur(Viereck(0, 0, 10, 10), e, new GCodeOptionen());
        Assert.Equal(new List<string>
        {
            "G0 X0.000 Y0.000",
            "M4 S800.0",
            "G1 F1800.0",
            "G1 X0.000 Y10.000",
            "G1 X10.000 Y10.000",
            "G1 X10.000 Y0.000",
            "G1 X0.000 Y0.000",
            "M5",
        }, zeilen);
    }
    [Fact]
    public void Kontur_Zwei_Passen()
    {
        var e = new SchnittEinstellung { Typ = "Cut", MinPower = 80, MaxPower = 80, Speed = 30, NumPasses = 2 };
        var zeilen = GCodeErzeuger.Kontur(Viereck(0, 0, 10, 10), e, new GCodeOptionen());
        Assert.Equal(2, zeilen.Count(zeile => zeile.StartsWith("M4 S")));
        // G0 + (M4+G1F+4G1+M5) + Zwischen-G0 + (M4+G1F+4G1+M5)
        Assert.Equal(16, zeilen.Count);
        Assert.Equal("G1 F1800.0", zeilen[2]);
        Assert.StartsWith("G1 X", zeilen[3]);
    }
    [Fact]
    public void Raster_Golden_Bidi()
    {
        var e = new SchnittEinstellung { Typ = "Scan", MinPower = 40, MaxPower = 40, Speed = 30, LineSpacing = 1 };
        var zeilen = GCodeErzeuger.Raster(new[] { Viereck(0, 0, 10, 10) }, e, new GCodeOptionen());
        // Zeile 0: G0 + M4 + G1; Zeilen 1..10: G0 + G1
        Assert.Equal(3 + 10 * 2, zeilen.Count);
        Assert.Equal("G0 X0.000 Y0.000", zeilen[0]);
        Assert.Equal("M4 S400.0", zeilen[1]);
        Assert.Equal("G1 F1800.0 X10.000", zeilen[2]);
        Assert.Equal("G0 Y1.000", zeilen[3]);
        Assert.Equal("G1 F1800.0 X0.000", zeilen[4]); // bidi zurueck
        Assert.Equal("G0 Y2.000", zeilen[5]);
        Assert.Equal("G1 F1800.0 X10.000", zeilen[6]);
    }
    [Fact]
    public void Rahmen_Golden()
    {
        var zeilen = GCodeErzeuger.Rahmen(0, 0, 400, 400, new GCodeOptionen());
        Assert.Equal(new List<string>
        {
            "G0 X0.000 Y0.000",
            "M4 S50.0",
            "G1 F600.0 X400.000",
            "G1 Y400.000",
            "G1 X0.000",
            "G1 Y0.000",
            "M5",
        }, zeilen);
    }
    [Fact]
    public void Erzeuge_Kopf_und_Fuss()
    {
        var p = new Projekt();
        p.Schnitte.Add(new SchnittEinstellung { Index = 0, Typ = "Cut", MinPower = 80, MaxPower = 80, Speed = 30 });
        p.Formen.Add(new RechteckForm { W = 40, H = 40, SchnittIndex = 0 });
        p.Formen.Add(new RechteckForm { W = 20, H = 20, SchnittIndex = 0, Versteckt = true });
        var zeilen = GCodeErzeuger.Erzeuge(p);
        Assert.Equal("G21", zeilen[0]);
        Assert.Equal("G90", zeilen[1]);
        Assert.Equal("M4", zeilen[2]);
        Assert.Equal("S0", zeilen[3]);
        Assert.Equal("M5", zeilen[^2]);
        Assert.Equal("G0 X0.000 Y0.000", zeilen[^1]);
        // Versteckte Form erzeugt nur EINE Kontur (M4 S800.0 genau einmal)
        Assert.Equal(1, zeilen.Count(z => z == "M4 S800.0"));
    }
}
