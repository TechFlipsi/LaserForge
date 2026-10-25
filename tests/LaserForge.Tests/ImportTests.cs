using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
using LaserForge.Core.Pipeline;
using Xunit;
namespace LaserForge.Tests;
public class ImportTests
{
    private const string DxfProbe = """
        0
        SECTION
        2
        ENTITIES
        0
        LINE
        8
        Ebene1
        10
        0.0
        20
        0.0
        11
        10.0
        21
        0.0
        0
        CIRCLE
        10
        50.0
        20
        50.0
        40
        10.0
        0
        LWPOLYLINE
        90
        2
        70
        1
        10
        1.0
        20
        200.0
        10
        21.0
        20
        200.0
        0
        ENDSEC
        0
        EOF
        """;
    [Fact]
    public void Dxf_Linie_Kreis_Lwp()
    {
        var pfad = LeserDxf.Lesen(DxfProbe);
        Assert.Equal(3, pfad.Rohtuege.Count);
        var linie = pfad.Rohtuege[0];
        Assert.Equal(new Punkt(0, 0), linie.Punkte[0]);
        Assert.Equal(new Punkt(10, 0), linie.Punkte[1]);
        Assert.False(linie.Geschlossen);
        var kreis = pfad.Rohtuege[1];
        Assert.True(kreis.Geschlossen);
        Assert.Equal(65, kreis.Punkte.Count);
        // Kreis Startpunkt = (cx+r, cy)
        Assert.Equal(new Punkt(60, 50), kreis.Punkte[0]);
        var poly = pfad.Rohtuege[2];
        Assert.True(poly.Geschlossen);
        Assert.Equal(new Punkt(1, 200), poly.Punkte[0]);
        Assert.Equal(new Punkt(21, 200), poly.Punkte[1]);
    }
    [Fact]
    public void Bild_Raster_Erzeugt_Segmente()
    {
        var bild = new BildForm { BreitePix = 3, HoehePix = 2, PixelMM = 1.0, Schwelle = 128 };
        bild.Grauwerte = new byte[] { 0, 255, 0,   // Zeile 0: dunkel, hell, dunkel
                                       255, 0, 255 }; // Zeile 1: hell, dunkel, hell
        var e = new SchnittEinstellung { Typ = "Scan", MinPower = 40, MaxPower = 40, Speed = 30, LineSpacing = 1 };
        var zeilen = GCodeErzeuger.BildRaster(bild, e, new GCodeOptionen());
        // Erste: M4; dann Segmente: Zeile0: (0..1) + (2..3); Zeile1: (1..2)
        Assert.Equal("M4 S400.0", zeilen[0]);
        Assert.Equal("G0 X0.000 Y0.000", zeilen[1]);
        Assert.Equal("G1 F1800.0 X1.000 Y0.000", zeilen[2]);
        Assert.Equal("G0 X2.000 Y0.000", zeilen[3]);
        Assert.Equal("G1 F1800.0 X3.000 Y0.000", zeilen[4]);
        Assert.Equal("G0 X1.000 Y1.000", zeilen[5]);
        Assert.Equal("G1 F1800.0 X2.000 Y1.000", zeilen[6]);
        Assert.Equal(7, zeilen.Count);
    }
    [Fact]
    public void Image_Leser_Als_BildForm()
    {
        string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <LightBurnProject AppVersion="1.7.05" FormatVersion="0" MaterialHeight="0" MirrorX="False" MirrorY="False">
              <CutSetting type="Scan">
                <index Value="1" /><speed Value="200" /><maxPower Value="40" /><lineSpacing Value="0.1" />
              </CutSetting>
              <Shape Type="Image" CutIndex="1" />
            </LightBurnProject>
            """;
        var zu = LeserLbrn.Lesen(xml);
        Assert.IsType<BildForm>(zu.Formen[0]);
    }
}
