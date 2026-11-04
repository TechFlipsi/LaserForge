using System.Text.Json;
using LaserForge.Core.Geraete;
using Xunit;

namespace LaserForge.Tests;

public class GeraeteTests
{
    [Fact]
    public void TypZeile_Nach_Protokoll()
    {
        var g = new Geraet { Protokoll = "GRBL", Sprache = "GCode" };
        Assert.Equal("GRBL | GCode", g.TypZeile);
    }

    [Fact]
    public void LightBurn_Import_Jog_Tempx60()
    {
        // Realer Aufbau (REA 25.10./04.11.: Elegoo COM3@115200, Jog 10mm @ 100mm/s = 6000 mm/min)
        var prefs = """
        {
          "DefaultDevice": -1,
          "DeviceList": [
            {
              "DisplayName": "Elegoo",
              "Name": "GRBL",
              "Type": "Serial",
              "Width": 400,
              "Height": 400,
              "HomeOnStartup": true,
              "MoveJogDistance": 10,
              "MoveJogSpeed": 100,
              "MoveJogZDistance": 10,
              "MoveJogZSpeed": 10,
              "MirrorX": false,
              "MirrorY": true,
              "RestPendantSecret": "GEHEIM_NICHT_UEBERNEHMEN",
              "Settings": {
                "CommPort": "COM3",
                "BaudRate": 115200,
                "EnableZ": false
              }
            }
          ]
        }
        """;
        var (geraete, gefunden) = LightBurnGeraeteImport.Lesen(prefs);
        Assert.Equal(1, gefunden);
        var g = geraete[0];
        Assert.Equal("Elegoo", g.Name);
        Assert.Equal("GRBL", g.Protokoll);
        Assert.Equal("COM3", g.Anschluss);
        Assert.Equal(115200, g.Baud);
        Assert.Equal(400, g.BreiteMM);
        Assert.Equal(6000, g.JogXyTempo);   // 100 mm/s -> 6000 mm/min
        Assert.Equal(600, g.JogZTempo);     // 10 mm/s -> 600 mm/min
        Assert.Equal(10, g.JogXyAbstand);
        Assert.True(g.HomeBeimStart);
        Assert.True(g.SpiegelnY);
        // Secret-Ebene: kein Feld im Modell — Import hält bewusst nur Protokoll-Daten
        var json = JsonSerializer.Serialize(g);
        Assert.DoesNotContain("GEHEIM", json);
    }

    [Fact]
    public void Import_ohne_DeviceList_leer()
    {
        var (geraete, gefunden) = LightBurnGeraeteImport.Lesen("""{"AdjustImgPosX": -1}""");
        Assert.Equal(0, gefunden);
        Assert.Empty(geraete);
    }

    [Fact]
    public void Standard_Flag_Exklusiv()
    {
        var liste = new List<Geraet> { new() { Name = "A" }, new() { Name = "B" } };
        GeraeteSpeicher.SetzeStandard(liste, liste[1]);
        Assert.False(liste[0].IstStandard);
        Assert.True(liste[1].IstStandard);
    }
}
