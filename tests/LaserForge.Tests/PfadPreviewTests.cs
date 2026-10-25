using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
using LaserForge.Core.Pipeline;
using Xunit;

namespace LaserForge.Tests;

public class PfadErhaltTests
{
    [Fact]
    public void Bezier_Roundtrip_Erhaelt_Kontrollpunkte()
    {
        // Pfad mit Bezier-Primitive (c0 an Startvertex, c1 am Endvertex)
        var p = new Projekt();
        p.Schnitte.Add(new SchnittEinstellung { Index = 0, Typ = "Cut" });
        var pfad = new PfadForm
        {
            SchnittIndex = 0,
            RoheVerte =
            {
                new PfadVertex(new Punkt(0, 0), null, null, false),
                new PfadVertex(new Punkt(50, 100), new Punkt(10, 100), new Punkt(10, 0), true),
            },
            RohePrimse = { new PfadPrimitive("B", 0, 1) },
            Rohtuege = LeserSvg.ZuegeAusD("M0 0 C10 100 10 0 50 100"),
        };
        p.Formen.Add(pfad);
        var text = SchreiberLbrn.Schreiben(p);
        Assert.Contains("c0x", text);
        Assert.Contains("c1y", text);
        Assert.Contains("T=\"B\"", text);
        var zu = LeserLbrn.Lesen(text);
        var zurueck = Assert.IsType<PfadForm>(zu.Formen[0]);
        Assert.True(zurueck.HatRoheVerte);
        Assert.Equal(2, zurueck.RoheVerte.Count);
        Assert.Equal(new Punkt(10, 100), zurueck.RoheVerte[1].C0);
        Assert.Equal("B", zurueck.RohePrimse[0].Typ);
        Assert.Equal(25, zurueck.Rohtuege[0].Punkte.Count);
    }
}

public class PreviewTests
{
    private static SchnittEinstellung Schnitt(int index, string typ, double power = 80, double speed = 30) =>
        new() { Index = index, Typ = typ, MinPower = power, MaxPower = power, Speed = speed };

    [Fact]
    public void Preview_Schneidet_Konturen_und_Zeit_Plus()
    {
        var p = new Projekt();
        p.Schnitte.Add(Schnitt(0, "Cut", 80, 40));
        p.Formen.Add(new RechteckForm { W = 100, H = 60, XForm = Transformationsmatrix.Verschieben(200, 300) });
        var e = PreviewSimulator.Simuliere(p);
        Assert.Equal(400, e.BreitePix);
        Assert.Equal(400, e.HoehePix);
        int dunkel = e.Grau.Count(g => g < 128);
        Assert.True(dunkel > 150, $"Kontur-Linien sollten dunkle Pixel geben, gefunden: {dunkel}");
        // Konturumfang 2*(100+60)=320 mm @ 40mm/s = 8s
        Assert.True(e.Sekunden > 7 && e.Sekunden < 10, $"Erwartet ~8s, gefunden: {e.Sekunden:F2}s");
        Assert.True(e.FahrtenSekunden >= 0);
    }

    [Fact]
    public void Preview_Scan_Zeilen_Nachs_Interval()
    {
        var p = new Projekt();
        p.Schnitte.Add(Schnitt(0, "Scan", 40, 50));
        // 20x10 Rechteck mit 1mm-Zeilenabstand -> 10 Zeilen à 20mm @50mm/s = 4s
        p.Formen.Add(new RechteckForm { W = 20, H = 10, XForm = Transformationsmatrix.Verschieben(200, 395) });
        p.Schnitte[0].LineSpacing = 1;
        var e = PreviewSimulator.Simuliere(p);
        Assert.True(e.Sekunden > 3.5 && e.Sekunden < 5.5, $"Erwartet ~4s (+Zustzeilen-Fahrten), gefunden: {e.Sekunden:F2}s");
    }

    [Fact]
    public void Preview_Plus_Versteckt_Skip()
    {
        var p = new Projekt();
        p.Schnitte.Add(Schnitt(0, "Cut"));
        p.Formen.Add(new RechteckForm { W = 100, H = 100, Versteckt = true });
        var e = PreviewSimulator.Simuliere(p);
        Assert.Equal(0, e.Grau.Count(g => g < 128));
        Assert.Equal(0, e.Sekunden);
    }
}