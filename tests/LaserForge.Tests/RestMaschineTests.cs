using LaserForge.Api.Rest;
using LaserForge.Core.Geraete;
using LaserForge.Devices.Grbl;
using LaserForge.Devices.Transport;
using Xunit;

namespace LaserForge.Tests;

/// <summary>M8: Maschinenrouten über die MaschinenSitzung — echte Sendekette bis zur Transport-Zeile.</summary>
public class RestMaschineTests : IDisposable
{
    private sealed class FakeTransport : ITransport
    {
        private readonly Queue<string> _antworten = new();
        public List<string> Gesendet { get; } = new();
        public void Reihen(string[] zeilen) { foreach (var z in zeilen) _antworten.Enqueue(z); }
        public bool Verbunden => true;
        public void Oeffnen() { }
        public void Schliessen() { }
        public void SchreibenZeile(string zeile) { Gesendet.Add(zeile); }
        public void SchreibenRoh(string text) { Gesendet.Add("roh:" + (text.Length > 0 ? ((int)text[0]).ToString("x2") : "")); }
        public string? LesenZeile(int timeoutMs = 100) => _antworten.Count > 0 ? _antworten.Dequeue() : null;
        public void Dispose() { }
    }

    private readonly List<Geraet> _vorher;

    public RestMaschineTests() => _vorher = GeraeteSpeicher.Laden();
    public void Dispose() => GeraeteSpeicher.Speichern(_vorher);

    private static (MaschinenSitzung Sitzung, Func<FakeTransport> Zuletzt) Sitzung(params string[] antworten)
    {
        var fakes = new List<FakeTransport>();
        var sitzung = new MaschinenSitzung((port, baud) =>
        {
            var f = new FakeTransport();
            f.Reihen(antworten);
            fakes.Add(f);
            return f;
        });
        return (sitzung, () => fakes[fakes.Count - 1]);
    }

    private static void GeraetPort(string name, string port)
    {
        GeraeteSpeicher.Speichern(new List<Geraet> { new Geraet { Name = name, Anschluss = port, Baud = 115200, IstStandard = true } });
    }

    private static ApiZustand Gestuetzt(out string bear)
    {
        var z = new ApiZustand();
        Router.Behandeln("POST", "/api/connect", null, true, z);
        var secret = z.Paarungen.Values.Last().Secret;
        bear = new Paarung().BearerToken(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), secret);
        return z;
    }

    [Fact]
    public void Jog_Faehrt_Echte_Sendekette()
    {
        GeraetPort("Drill", "COMTEST");
        var (sitzung, zuletzt) = Sitzung("ok");
        var z = Gestuetzt(out var bear);
        var (code, _, _) = Router.Behandeln(
            "POST", "/api/jog", bear, true, z, "{\"axis\":\"Y\",\"distance\":20}", "", sitzung);
        Assert.Equal(200, code);
        Assert.Contains("$J=G91 Y20 F", zuletzt().Gesendet[0]);
    }

    [Fact]
    public void Port_Belegts_Fehler_Sauber_Durchgereicht()
    {
        GeraetPort("Drill", "COMBUSY");
        var sitzung = new MaschinenSitzung((port, baud) =>
            throw new UnauthorizedAccessException("Access to the path 'COMBUSY' is denied."));
        var z = Gestuetzt(out var bear);
        var (code, _, koerper) = Router.Behandeln(
            "POST", "/api/jog", bear, true, z, "{\"axis\":\"X\",\"distance\":5}", "", sitzung);
        Assert.Equal(409, code);
        Assert.Contains("port_busy", koerper);
    }

    [Fact]
    public void Home_Sendet_Grbl_Homing()
    {
        GeraetPort("Drill", "COMTEST");
        var (sitzung, zuletzt) = Sitzung("ok");
        var z = Gestuetzt(out var bear);
        var (code, _, koerper) = Router.Behandeln("POST", "/api/home", bear, true, z, "", "", sitzung);
        Assert.Equal(200, code);
        Assert.Contains("$H", zuletzt().Gesendet[0]);
        Assert.Contains("Homing", koerper);
    }

    [Fact]
    public void Position_Aus_Echtem_Status()
    {
        GeraetPort("Drill", "COMTEST");
        var (sitzung, _) = Sitzung("ok", "<Idle|MPos:10.000,20.000,0.000|WPos:10.000,20.000,0.000|FS:0,0>");
        var z = Gestuetzt(out var bear);
        Router.Behandeln("POST", "/api/jog", bear, true, z, "{\"axis\":\"X\",\"distance\":5}", "", sitzung);
        var (c2, _, k2) = Router.Behandeln("GET", "/api/position", bear, true, z, "", "", sitzung);
        Assert.Equal(200, c2);
        Assert.Contains("\"machine\"", k2);
        Assert.Contains("\"x\":10", k2);
    }

    [Fact]
    public void Jog_Stop_Sendet_Realtime_85()
    {
        GeraetPort("Drill", "COMTEST");
        var (sitzung, zuletzt) = Sitzung("ok");
        var z = Gestuetzt(out var bear);
        var (code, _, _) = Router.Behandeln("POST", "/api/jog/stop", bear, true, z, "", "", sitzung);
        Assert.Equal(200, code);
        Assert.Contains("roh:85", zuletzt().Gesendet.Last());
    }

    [Fact]
    public void Job_Routen_Gate_Nur_Mit_Verbindung()
    {
        GeraetPort("Drill", "COMTEST");
        var (sitzung, _) = Sitzung("ok");
        var z = Gestuetzt(out var bear);
        var (c0, _, k0) = Router.Behandeln("POST", "/api/job/start", bear, true, z, "", "", sitzung);
        Assert.Equal(409, c0);                      // noch nie verbunden → not_connected
        Assert.Contains("not_connected", k0);
        Router.Behandeln("POST", "/api/jog", bear, true, z, "{\"axis\":\"X\",\"distance\":5}", "", sitzung);
        var (code, _, koerper) = Router.Behandeln("POST", "/api/job/start", bear, true, z, "", "", sitzung);
        Assert.Equal(409, code);                    // verbunden → Pipeline-Runner-Gate
        Assert.Contains("job_engine_pending", koerper);
    }
}
