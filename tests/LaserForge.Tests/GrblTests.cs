using LaserForge.Devices.Grbl;
using LaserForge.Devices.Transport;
using Xunit;
namespace LaserForge.Tests;
public class GrblTests
{
    private sealed class FakeTransport : ITransport
    {
        public List<string> Gesendet { get; } = new();
        private readonly Queue<string> _antworten = new();
        public void AntwortGeben(string zeile) => _antworten.Enqueue(zeile);
        public bool Verbunden => true;
        public void Oeffnen() { }
        public void Schliessen() { }
        public void SchreibenZeile(string zeile) => Gesendet.Add(zeile);
        public void SchreibenRoh(string text) => Gesendet.Add(text);
        public string? LesenZeile(int timeoutMs = 100) => _antworten.Count > 0 ? _antworten.Dequeue() : null;
        public void Dispose() { }
    }
    [Theory]
    [InlineData("<Idle|MPos:0.000,0.000,0.000|WPos:0.000,0.000,0.000|FS:0,0|Pn:P|DISCON>", "Idle", 0.0, 0.0, 0.0)]
    [InlineData("<Run|MPos:10.000,20.000,0.500|FS:1800,0>", "Run", 10.0, 20.0, 0.5)]
    public void Status_Parsen(string zeile, string zustand, double mx, double my, double mz)
    {
        var s = GrblStatus.AusZeile(zeile);
        Assert.NotNull(s);
        Assert.Equal(zustand, s!.Zustand);
        Assert.Equal(mx, s.MaschineX, 3);
        Assert.Equal(my, s.MaschineY, 3);
        Assert.Equal(mz, s.MaschineZ, 3);
    }
    [Fact] public void Status_Null_bei_unbekannter_Zeile() => Assert.Null(GrblStatus.AusZeile("irgendwas"));
    [Fact]
    public void Jog_und_ok()
    {
        var fake = new FakeTransport();
        fake.AntwortGeben("ok");
        Assert.True(new GrblVerbindung(fake).Joggen("X", 10, 2000));
        Assert.Contains("$J=G91 X10 F2000", fake.Gesendet);
    }
    [Fact]
    public void Rahmen_bestatigt()
    {
        var fake = new FakeTransport();
        for (int i = 0; i < 10; i++) fake.AntwortGeben("ok");
        Assert.True(new GrblVerbindung(fake).Rahmen(0, 0, 100, 100));
        Assert.Equal(7, fake.Gesendet.Count);
    }
}
