using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
using LaserForge.Core.Pipeline;
using Xunit;
namespace LaserForge.Tests;
public class ByLayerTests
{
    [Fact]
    public void Erzeuge_Ordnung_Nach_Layer()
    {
        var p = new Projekt();
        p.Schnitte.Add(new SchnittEinstellung { Index = 0, Typ = "Cut", MaxPower = 80, Speed = 30 });
        p.Schnitte.Add(new SchnittEinstellung { Index = 1, Typ = "Cut", MaxPower = 60, Speed = 20 });
        p.Formen.Add(new RechteckForm { W = 10, H = 10, SchnittIndex = 1 });  // Layer 1 FIRST im Dokument
        p.Formen.Add(new RechteckForm { W = 10, H = 10, SchnittIndex = 0, XForm = Transformationsmatrix.Verschieben(20, 0) });
        var zeilen = GCodeErzeuger.Erzeuge(p);
        int s60 = zeilen.FindIndex(z => z == "M4 S600.0");
        int s80 = zeilen.FindIndex(z => z == "M4 S800.0");
        Assert.True(s60 >= 0 && s80 >= 0, $"S600/S800 fehlen ({s60}/{s80})");
        Assert.True(s80 < s60, "Layer 0 (S800) muss vor Layer 1 (S600) kommen — By-Layer-Reihenfolge");
    }
    [Fact]
    public void Vereinfachen_Beseitigt_Kollineare()
    {
        var z = new Zug(new[] { new Punkt(0, 0), new Punkt(5, 0), new Punkt(10, 0), new Punkt(10, 10) }, false);
        var erg = GCodeErzeuger.Vereinfachen(new[] { z });
        var erster = Assert.Single(erg);
        Assert.Equal(new[] { new Punkt(0, 0), new Punkt(10, 0), new Punkt(10, 10) }, erster.Punkte);
    }
}
