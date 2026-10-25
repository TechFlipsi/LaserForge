using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
using Xunit;
namespace LaserForge.Tests;
public class FormateVollTests
{
    [Fact]
    public void Voll_Roundtrip_Erhaelt_Alles()
    {
        var p = new Projekt
        {
            AppVersion = "1.7.05",
            MaterialHeight = 2.4,
            MirrorX = true,
            Notizen = new Notizen { BeiLadenZeigen = true, Text = "Erstellt von LaserForge. Pruefungsprojekt." },
            VariableText = new VariableTextEinstellungen { Start = 5, Ende = 45, Aktueller = 7, Schritt = 2, AutoWeiterschalten = true },
        };
        p.UIPrefs["Optimize_ByLayer"] = "1";
        p.UIPrefs["Optimize_ByPriority"] = "0";
        p.Schnitte.Add(new SchnittEinstellung { Index = 0, Typ = "Cut", NameSektion = "CutA", MinPower = 70, MaxPower = 90, Speed = 25 });
        p.Schnitte[0].RestParameter.Add(("gantryPower", "99"));
        p.Schnitte[0].RestParameter.Add(("fiber_pulse_width", "100"));
        p.Formen.Add(new RechteckForm { W = 60, H = 40, SchnittIndex = 0, XForm = Transformationsmatrix.Verschieben(50, 60) });
        var text = SchreiberLbrn.Schreiben(p);
        var zu = LeserLbrn.Lesen(text);
        Assert.Equal("1.7.05", zu.AppVersion);
        Assert.Equal(2.4, zu.MaterialHeight);
        Assert.True(zu.Notizen is { BeiLadenZeigen: true, Text: "Erstellt von LaserForge. Pruefungsprojekt." });
        var vt = Assert.IsType<VariableTextEinstellungen>(zu.VariableText);
        Assert.Equal((5, 45, 7, 2, true), (vt.Start, vt.Ende, vt.Aktueller, vt.Schritt, vt.AutoWeiterschalten));
        Assert.Equal("1", zu.UIPrefs["Optimize_ByLayer"]);
        Assert.Equal(2, zu.Schnitte[0].RestParameter.Count);
        Assert.Contains(("gantryPower", "99"), zu.Schnitte[0].RestParameter);
        Assert.Contains(("fiber_pulse_width", "100"), zu.Schnitte[0].RestParameter);
        Assert.Equal(60, ((RechteckForm)zu.Formen[0]).W);
        // Doppel-Roundtrip bleibt identisch (Determinismus)
        var text2 = SchreiberLbrn.Schreiben(zu);
        Assert.Equal(text, text2);
    }
    [Fact]
    public void Notes_am_Ende_und_Vor_Cuts_Ordnung()
    {
        var p = new Projekt();
        p.Notizen = new Notizen { Text = "x" };
        p.Schnitte.Add(new SchnittEinstellung { Index = 0 });
        p.Formen.Add(new RechteckForm { W = 10, H = 10, SchnittIndex = 0 });
        var text = SchreiberLbrn.Schreiben(p);
        int vtIndex = text.IndexOf("<Notes", StringComparison.Ordinal);
        int cutIndex = text.IndexOf("<CutSetting", StringComparison.Ordinal);
        int shapeIndex = text.IndexOf("<Shape", StringComparison.Ordinal);
        Assert.True(cutIndex < shapeIndex, "CutSettings vor Shapes");
        Assert.True(shapeIndex < vtIndex, "Notes nach den Shapes");
    }
}
