using LaserForge.Api.Rest;
using LaserForge.Core.Geraete;
using LaserForge.Devices.Grbl;
using LaserForge.Devices.Transport;
using Xunit;

namespace LaserForge.Tests;

/// <summary>M9: Job-Engine — echte GCode-Fahrt aus dem Projekt mit Safety-Gate + Realtime-Tasten.</summary>
public class JobEngineTests
{
    private sealed class FakeTransport : ITransport
    {
        private readonly Queue<string> _antworten = new();
        public List<string> Gesendet { get; } = new();
        public int LeseVerzoegerungMs { get; set; }
        public void Reihen(string[] zeilen) { foreach (var z in zeilen) _antworten.Enqueue(z); }
        public bool Verbunden => true;
        public void Oeffnen() { }
        public void Schliessen() { }
        public void SchreibenZeile(string zeile) { Gesendet.Add(zeile); }
        public void SchreibenRoh(string text) { Gesendet.Add("roh:" + (text.Length > 0 ? ((int)text[0]).ToString("x2") : "")); }
        public string? LesenZeile(int timeoutMs = 100)
        {
            if (LeseVerzoegerungMs > 0) Thread.Sleep(LeseVerzoegerungMs);
            return _antworten.Count > 0 ? _antworten.Dequeue() : null;
        }
        public void Dispose() { }
    }

    private static ApiZustand ProjektZustand()
    {
        var z = new ApiZustand();
        z.Projekt.Schnitte.Add(new LaserForge.Core.Einstellungen.SchnittEinstellung { Index = 0, Typ = "Cut", NameSektion = "A", MaxPower = 80, Speed = 30 });
        z.Projekt.Formen.Add(new LaserForge.Core.Formen.RechteckForm { W = 50, H = 40, SchnittIndex = 0 });
        return z;
    }

    private static MaschinenSitzung SitzungMit(params string[] antworten)
    {
        return new MaschinenSitzung((port, baud) =>
        {
            var f = new FakeTransport();
            f.Reihen(antworten);
            return f;
        });
    }

    [Fact]
    public void Start_Verweigert_ohne_Lasermodus32()
    {
        GeraeteSpeicher.Speichern(new List<Geraet> { new Geraet { Name = "S", Anschluss = "C", Baud = 115200, IstStandard = true } });
        var sitzung = SitzungMit("$$", "$32=0", "ok");
        sitzung.Oeffnen(new Geraet { Name = "S", Anschluss = "C", Baud = 115200 });
        var e = new JobEngine();
        Assert.False(e.Start(sitzung, ProjektZustand(), out var fehlertext));
        Assert.Contains("Brandgefahr", fehlertext);
        GeraeteSpeicher.Speichern(new List<Geraet>());
    }

    [Fact]
    public void Start_Faehrt_GCode_und_Endet_Idle()
    {
        GeraeteSpeicher.Speichern(new List<Geraet> { new Geraet { Name = "S", Anschluss = "C", Baud = 115200, IstStandard = true } });
        var antworten = new List<string> { "$$", "$32=1", "ok" };
        for (int i = 0; i < 60; i++) antworten.Add("ok");
        var sitzung = SitzungMit(antworten.ToArray());
        sitzung.Oeffnen(new Geraet { Name = "S", Anschluss = "C", Baud = 115200 });
        var zustand = ProjektZustand();
        var e = new JobEngine();
        Assert.True(e.Start(sitzung, zustand, out var fehlertext), fehlertext);
        Assert.Equal("running", zustand.JobZustand);
        // Lauf (kleines Projekt) läuft in Millisekunden durch — kurz warten, dann idle + Fortschritt
        Thread.Sleep(400);
        Assert.Equal("idle", zustand.JobZustand);
        Assert.Equal(100, zustand.JobFortschritt);
        GeraeteSpeicher.Speichern(new List<Geraet>());
    }

    [Fact]
    public void Pause_hold_Realtime()
    {
        GeraeteSpeicher.Speichern(new List<Geraet> { new Geraet { Name = "S", Anschluss = "C", Baud = 115200, IstStandard = true } });
        var antworten = new List<string> { "$$", "$32=1", "ok" };
        for (int i = 0; i < 20; i++) antworten.Add("ok");
        FakeTransport? f = null;
        var sitzung = new MaschinenSitzung((port, baud) =>
        {
            f = new FakeTransport { LeseVerzoegerungMs = 250 };
            f.Reihen(antworten.ToArray());
            return f;
        });
        sitzung.Oeffnen(new Geraet { Name = "S", Anschluss = "C", Baud = 115200 });
        var e = new JobEngine();
        Assert.True(e.Start(sitzung, ProjektZustand(), out _));
        Thread.Sleep(900);   // Lauf ist mitten in der Kontur (gedrosselter Fake)
        var zustand = e.HalteOderFortsetzen(sitzung);
        Assert.Equal("paused", zustand);
        Assert.Contains("roh:21", f!.Gesendet.ToArray());   // Realtime Feed-Hold 0x21
        e.Stoppen(sitzung);   // Abbruch-Taste (0x18)
        Assert.Contains("roh:18", f.Gesendet.ToArray());
        GeraeteSpeicher.Speichern(new List<Geraet>());
    }

    [Fact]
    public void Router_Job_Pfad_Mit_Engine()
    {
        GeraeteSpeicher.Speichern(new List<Geraet> { new Geraet { Name = "S", Anschluss = "C", Baud = 115200, IstStandard = true } });
        var antworten = new List<string> { "$$", "$32=1", "ok" };
        for (int i = 0; i < 60; i++) antworten.Add("ok");
        FakeTransport? f = null;
        var sitzung = new MaschinenSitzung((port, baud) => { f = new FakeTransport(); f.Reihen(antworten.ToArray()); return f; });
        sitzung.Oeffnen(new Geraet { Name = "S", Anschluss = "C", Baud = 115200 });
        var z = new ApiZustand();
        Router.Behandeln("POST", "/api/connect", null, true, z);
        var bear = new Paarung().BearerToken(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), z.Paarungen.Values.Last().Secret);
        var (code, _, koerper) = Router.Behandeln(
            "POST", "/api/job/start", bear, true, z, "", "", sitzung, new JobEngine());
        Assert.True(code == 200, $"job/start fehlgeschlagen: {koerper}");
        Assert.Contains("started", koerper);
        Thread.Sleep(500);   // kleiner Job-Lauf (Kopf/Kontur/Fuß) durch
        Assert.Contains("$$", f!.Gesendet.ToArray());   // Safety-Abfrage ($$) wirklich gesendet
        Assert.Contains("G21", f.Gesendet.ToArray());    // GCode-Kopf gesendet
        GeraeteSpeicher.Speichern(new List<Geraet>());
    }
}
