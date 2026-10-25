using LaserForge.Api.Rest;
using Xunit;
namespace LaserForge.Tests;
public class RestTests
{
    private static ApiZustand Zustand()
    {
        var z = new ApiZustand();
        z.Projekt.Schnitte.Add(new LaserForge.Core.Einstellungen.SchnittEinstellung { Index = 0, Typ = "Cut", NameSektion = "SchnittA", MaxPower = 80, Speed = 30 });
        z.Projekt.Formen.Add(new LaserForge.Core.Formen.RechteckForm { W = 50, H = 40, SchnittIndex = 0 });
        return z;
    }
    [Fact]
    public void Ohne_Bearer_401()
    {
        var (code, _, _) = Router.Behandeln("GET", "/api/status", null, true, Zustand());
        Assert.Equal(401, code);
    }
    [Fact]
    public void Paarung_Nur_Loopback()
    {
        var z = Zustand() as ApiZustand;
        var (code, _, _) = Router.Behandeln("POST", "/api/connect", null, false, z!);
        Assert.Equal(403, code);
        var (code2, _, koerper2) = Router.Behandeln("POST", "/api/connect", null, true, z!);
        Assert.Equal(200, code2);
        Assert.Contains("secret", koerper2);
    }
    [Fact]
    public void Status_mit_Bearer_200()
    {
        var z = Zustand();
        var (code1, _, koerper1) = Router.Behandeln("POST", "/api/connect", null, true, z);
        Assert.Equal(200, code1);
        // Secret aus der Paarung entnehmen (zustand.Paarungen hat genau 1)
        var secret = Assert.Single(z.Paarungen.Values).Secret;
        var paarung = new Paarung();
        var token = paarung.BearerToken(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), secret);
        var (code, _, koerper) = Router.Behandeln("GET", "/api/status", token, true, z);
        Assert.Equal(200, code);
        Assert.Contains("device_name", koerper);
    }
    [Fact]
    public void Layer_Farben_Nach_Protokoll()
    {
        var z = Zustand();
        var (code, _, koerper) = Router.Behandeln("GET", "/api/layers", PaarungBear(z), true, z);
        Assert.Equal(200, code);
        Assert.Contains("#000000", koerper); // Layer 0 schwarz
    }
    [Fact]
    public void Cuts_Nach_REA_Struktur()
    {
        var z = Zustand();
        var (code, _, koerper) = Router.Behandeln("GET", "/api/cuts", PaarungBear(z), true, z);
        Assert.Equal(200, code);
        Assert.Contains("\"name\":\"C00\"", koerper);
        Assert.Contains("\"mode\":\"cut\"", koerper);
        Assert.Contains("frequency", koerper);
    }
    private static string PaarungBear(ApiZustand z)
    {
        Router.Behandeln("POST", "/api/connect", null, true, z);
        var secret = Assert.Single(z.Paarungen.Values).Secret;
        var paarung = new Paarung();
        return paarung.BearerToken(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), secret);
    }
}
