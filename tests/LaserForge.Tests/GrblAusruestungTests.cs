using LaserForge.Devices.Grbl;
using LaserForge.Devices.Transport;
using Xunit;

namespace LaserForge.Tests;

public class GrblAusruestungTests
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
    public void Version_aus_Willkommenstring()
    {
        var t = new FakeTransport();
        t.AntwortGeben("Grbl 1.1h ['$' for help]");
        var v = new GrblVerbindung(t);
        v.Oeffnen();
        Assert.Equal("1.1h", new GrblAusruestung(v).Version);
    }

    [Fact]
    public void Einstellungen_lesen_bis_ok()
    {
        var t = new FakeTransport();
        t.AntwortGeben("ok");          // Begruessung
        var v = new GrblVerbindung(t);
        v.Oeffnen();
        // Jetzt die $$-Antworten
        t.AntwortGeben("$30=1000");
        t.AntwortGeben("$31=0");
        t.AntwortGeben("$32=1");
        t.AntwortGeben("$110=6000.000");
        t.AntwortGeben("ok");
        var e = new GrblAusruestung(v).EinstellungenLesen();
        Assert.Equal(4, e.Count);
        Assert.Equal("1000", e["$30"]);
        Assert.Equal("1", e["$32"]);
    }

    [Fact]
    public void Lasermodus_Test()
    {
        var t = new FakeTransport();
        t.AntwortGeben("ok");
        var v = new GrblVerbindung(t);
        v.Oeffnen();
        t.AntwortGeben("$32=0");
        t.AntwortGeben("ok");
        Assert.False(new GrblAusruestung(v).LasermodusAn());

        var t2 = new FakeTransport();
        t2.AntwortGeben("ok");
        var v2 = new GrblVerbindung(t2);
        v2.Oeffnen();
        t2.AntwortGeben("$32=1");
        t2.AntwortGeben("ok");
        Assert.True(new GrblAusruestung(v2).LasermodusAn());
    }

    [Fact]
    public void Alarm_Wird_erhoben()
    {
        var t = new FakeTransport();
        t.AntwortGeben("ok");          // Begruessung
        var v = new GrblVerbindung(t);
        v.Oeffnen();
        t.AntwortGeben("ALARM:2");     // kommt waehrend des Jog-Urteils
        t.AntwortGeben("ok");
        v.Joggen("X", 5, 2000);
        Assert.Equal("ALARM:2", v.LetzterAlarm);
    }

    [Fact]
    public void Homing_Warten()
    {
        var t = new FakeTransport();
        t.AntwortGeben("ok");          // Begruessung
        var v = new GrblVerbindung(t);
        v.Oeffnen();
        t.AntwortGeben("ok");          // $H
        Assert.True(new GrblAusruestung(v).HomeMitWarten(2000));
        Assert.Contains("$H", t.Gesendet);
    }
}