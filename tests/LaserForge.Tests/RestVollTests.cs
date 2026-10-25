using LaserForge.Api.Rest;
using LaserForge.Core.Formate;
using Xunit;

namespace LaserForge.Tests;

public class RestVollTests
{
    private static Paarung GetestPaarung(ApiZustand z)
    {
        Router.Behandeln("POST", "/api/connect", null, true, z);
        var secret = Assert.Single(z.Paarungen.Values).Secret;
        var paarung = new Paarung();
        return new Paarung { Secret = secret };
    }

    private static string Bearer(ApiZustand z)
    {
        Router.Behandeln("POST", "/api/connect", null, true, z);
        var secret = Assert.Single(z.Paarungen.Values).Secret;
        return new Paarung().BearerToken(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), secret);
    }

    [Fact]
    public void Upload_lbrn_laed_Projekt()
    {
        var z = new ApiZustand();
        var quelle = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "nameplate.lbrn");
        var inhalt = File.ReadAllText(quelle);
        var koerper = System.Text.Json.JsonSerializer.Serialize(new { filename = "probe.lbrn", content = inhalt });
        var (code, _, koerperAntwort) = Router.Behandeln("POST", "/api/file/upload", Bearer(z), true, z, koerper);
        Assert.Equal(200, code);
        Assert.Contains("\"status\":\"ok\"", koerperAntwort);
        Assert.True(z.Projekt.FormenZahl() > 0, "Projekt sollte Formen haben");
    }

    [Fact]
    public void Upload_ohne_Inhalt_422()
    {
        var z = new ApiZustand();
        var (code, _, _) = Router.Behandeln("POST", "/api/file/upload", Bearer(z), true, z, """{"filename":"x.lbrn","content":""}""");
        Assert.Equal(422, code);
    }

    [Fact]
    public void Upload_svg_wird_Form()
    {
        var z = new ApiZustand();
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M0 0 L10 0 Z\"/></svg>";
        var koerper = System.Text.Json.JsonSerializer.Serialize(new { filename = "v.svg", content = svg });
        var (code, _, _) = Router.Behandeln("POST", "/api/file/upload", Bearer(z), true, z, koerper);
        Assert.Equal(200, code);
        Assert.Single(z.Projekt.Formen);
    }

    [Fact]
    public void Overlay_Enthaelt_shapes_und_bbox()
    {
        var z = new ApiZustand();
        z.Projekt.Formen.Add(new LaserForge.Core.Formen.RechteckForm { W = 20, H = 10, SchnittIndex = 0 });
        var (code, _, koerper) = Router.Behandeln("GET", "/api/overlay", Bearer(z), true, z);
        Assert.Equal(200, code);
        Assert.Contains("shapes", koerper);
        Assert.Contains("bbox", koerper);
        Assert.Contains("rohtyp", koerper);
    }

    [Fact]
    public void Paarungs_Persistenz_Roundtrip()
    {
        var z = new ApiZustand();
        Router.Behandeln("POST", "/api/connect", null, true, z);
        Assert.Single(z.Paarungen);
        var pfad = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "forge-test-paarungen.json");
        z.Speichern(pfad);
        var z2 = new ApiZustand();
        z2.Laden(pfad);
        Assert.Single(z2.Paarungen);
        // Token mit dem gespeicherten Secret bleibt gueltig
        var secret = Assert.Single(z2.Paarungen.Values).Secret;
        Assert.Equal(z.Paarungen.Values.First().Secret, secret);
        System.IO.File.Delete(pfad);
    }
}