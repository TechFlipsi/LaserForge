using LaserForge.Devices.Grbl;
using LaserForge.Devices.Transport;
using Xunit;
namespace LaserForge.Tests;

/// <summary>M7.7: Maschineninstellungen direkt am Gerät ändern ($x=value, EEPROM-Parität).</summary>
public class GrblAusruestungTests2
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

    [Fact]
    public void Einstellung_Setzen_ok()
    {
        var fake = new FakeTransport();
        fake.AntwortGeben("ok");
        Assert.True(new GrblAusruestung(new GrblVerbindung(fake)).EinstellungSetzen("$110", "6000"));
        Assert.Equal("$110=6000", fake.Gesendet[0]);
    }

    [Fact]
    public void Einstellung_Setzen_error_wird_durchgereicht()
    {
        var fake = new FakeTransport();
        fake.AntwortGeben("error:33");
        var a = new GrblAusruestung(new GrblVerbindung(fake));
        Assert.False(a.EinstellungSetzen("$110", "0"));
        Assert.Equal("error:33", a.LetzteFehlerzeile);
    }

    [Fact]
    public void Injektion_Schranke()
    {
        var fake = new FakeTransport();
        var a = new GrblAusruestung(new GrblVerbindung(fake));
        Assert.False(a.EinstellungSetzen("$110\r\nM30", "10"));   // Zeilen-Ende-Schmuggel
        Assert.False(a.EinstellungSetzen("$110=", "10"));         // '=' im Schlüssel
        Assert.Empty(fake.Gesendet);                              // NICHTS gesendet
    }

    [Fact]
    public void Einstellungen_Lesen_parst_werte()
    {
        var fake = new FakeTransport();
        fake.AntwortGeben("$100=80.00");
        fake.AntwortGeben("$110=6000.000");
        fake.AntwortGeben("$32=1");
        fake.AntwortGeben("ok");
        var e = new GrblAusruestung(new GrblVerbindung(fake)).EinstellungenLesen();
        Assert.Equal(3, e.Count);
        Assert.Equal("80.00", e["$100"]);
        Assert.Equal("1", e["$32"]);
        Assert.True(e.TryGetValue("$32", out var m) && m == "1");
    }
}
