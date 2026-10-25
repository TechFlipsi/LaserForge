using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
using Xunit;
namespace LaserForge.Tests;
public class ThumbnailTests
{
    [Fact]
    public void Erzeugt_Gueltiges_Basis64_Png()
    {
        var p = new Projekt();
        p.Schnitte.Add(new LaserForge.Core.Einstellungen.SchnittEinstellung { Index = 0 });
        p.Formen.Add(new RechteckForm { W = 100, H = 100 });
        var b = ThumbnailErzeuger.ErzeugeBasis64(p);
        Assert.True(b.Length > 100, "PNG-Basis64 sollte Daten enthalten");
        var bytes = Convert.FromBase64String(b);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]); // P
        Assert.Equal(0x4E, bytes[2]); // N
        Assert.Equal(0x47, bytes[3]); // G
        // IHDR lauft nach Signatur mit Laenge 13 + IHDR
        Assert.Equal(0, bytes[8]); Assert.Equal(0, bytes[9]); Assert.Equal(0, bytes[10]); Assert.Equal(13, bytes[11]);
        Assert.Equal((byte)'I', bytes[12]); Assert.Equal((byte)'H', bytes[13]); Assert.Equal((byte)'D', bytes[14]); Assert.Equal((byte)'R', bytes[15]);
        Assert.Equal(250, bytes[16] << 24 | bytes[17] << 16 | bytes[18] << 8 | bytes[19]);
    }
    [Fact]
    public void Thumbnail_Roundtrip_Erhalten()
    {
        var p = new Projekt();
        p.Formen.Add(new RechteckForm { W = 100, H = 100 });
        p.ThumbnailBasis64 = ThumbnailErzeuger.ErzeugeBasis64(p);
        var text = SchreiberLbrn.Schreiben(p);
        var zu = LeserLbrn.Lesen(text);
        Assert.Equal(p.ThumbnailBasis64, zu.ThumbnailBasis64);
    }
}
