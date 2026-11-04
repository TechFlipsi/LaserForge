using System.Text.Json.Nodes;
using LaserForge.Api.Rest;
using Xunit;

namespace LaserForge.Tests;

/// <summary>M7.5 API-Parität: neue Routen nach den LightBurn-2.2-Schemata (Spec 04.11.2026).</summary>
public class RestParitaetTests
{
    private static ApiZustand Zustand()
    {
        var z = new ApiZustand();
        z.Projekt.Schnitte.Add(new LaserForge.Core.Einstellungen.SchnittEinstellung { Index = 0, Typ = "Cut", NameSektion = "SchnittA", MaxPower = 80, Speed = 30 });
        z.Projekt.Formen.Add(new LaserForge.Core.Formen.RechteckForm { W = 50, H = 40, SchnittIndex = 0 });
        return z;
    }

    private static string Bear(ApiZustand z)
    {
        Router.Behandeln("POST", "/api/connect", null, true, z);
        var secret = z.Paarungen.Values.Last().Secret;
        return new Paarung().BearerToken(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), secret);
    }

    [Fact]
    public void Bind_Nur_Loopback()
    {
        var z = Zustand();
        var (codeAus, _, _) = Router.Behandeln("POST", "/api/bind", null, false, z);
        Assert.Equal(403, codeAus);
        var (codeEin, _, koerper) = Router.Behandeln("POST", "/api/bind", null, true, z);
        Assert.Equal(200, codeEin);
        Assert.Contains("secret", koerper);
    }

    [Fact]
    public void Material_Library_Leer_Nach_Protokoll()
    {
        var z = Zustand();
        var (code, _, koerper) = Router.Behandeln("GET", "/api/material-library", Bear(z), true, z);
        Assert.Equal(200, code);
        var o = JsonNode.Parse(koerper)!.AsObject();
        Assert.Equal("lightburn", (string?)o["product"]);
        Assert.NotNull(o["entries"]);
    }

    [Fact]
    public void Overlay_Metadata_404_ohne_Kamera()
    {
        var z = Zustand();
        var (code, _, koerper) = Router.Behandeln("GET", "/api/overlay/metadata", Bear(z), true, z);
        Assert.Equal(404, code);
        Assert.Contains("error", koerper);
    }

    [Fact]
    public void Events_Poll_Snapshot_Felder()
    {
        var z = Zustand();
        var (code, _, koerper) = Router.Behandeln("GET", "/api/events/poll", Bear(z), true, z);
        Assert.Equal(200, code);
        var o = JsonNode.Parse(koerper)!.AsObject();
        Assert.NotNull(o["position"]); Assert.NotNull(o["job"]); Assert.NotNull(o["overrides"]);
        Assert.NotNull(o["aux"]); Assert.NotNull(o["connection"]); Assert.NotNull(o["settings"]);
        Assert.Equal("idle", (string?)o["job"]!["state"]);
        Assert.Equal(100, (int?)o["overrides"]!["speed_percent"]);
    }

    [Fact]
    public void Jog_Settings_Flat_und_Setzen()
    {
        var z = Zustand();
        var (code, _, koerper) = Router.Behandeln("GET", "/api/jog/settings", Bear(z), true, z);
        Assert.Equal(200, code);
        var o = JsonNode.Parse(koerper)!.AsObject();
        Assert.Equal("mm/min", (string?)o["units"]);
        Assert.NotNull(o["xy_distance"]);
        var (codeSet, _, koerperSet) = Router.Behandeln(
            "POST", "/api/jog/settings", Bear(z), true, z, "{\"xy_distance\":5.5,\"xy_speed\":9000}");
        Assert.Equal(200, codeSet);
        Assert.Contains("5.5", koerperSet);
        var (codeLeer, _, _) = Router.Behandeln("POST", "/api/jog/settings", Bear(z), true, z, "{}");
        Assert.Equal(422, codeLeer);
    }

    [Fact]
    public void Maschinenrouten_Ehrlicher_Gate()
    {
        var z = Zustand();
        var (codeAus, _, koerperAus) = Router.Behandeln("POST", "/api/jog", Bear(z), true, z);
        Assert.Equal(409, codeAus);
        Assert.Contains("not_connected", koerperAus);
        z.Verbunden = true;
        var (codeEin, _, koerperEin) = Router.Behandeln("POST", "/api/job/start", Bear(z), true, z);
        Assert.Equal(409, codeEin);
        Assert.Contains("device_integration_pending", koerperEin);
    }

    [Fact]
    public void Jog_Stop_Immer_OK()
    {
        var z = Zustand();
        var (code, _, koerper) = Router.Behandeln("POST", "/api/jog/stop", Bear(z), true, z);
        Assert.Equal(200, code);
        Assert.Contains("ok", koerper);
    }

    [Fact]
    public void Spindel_Toggle_und_Speed()
    {
        var z = Zustand();
        var (code1, _, k1) = Router.Behandeln("POST", "/api/spindle/toggle", Bear(z), true, z);
        Assert.Equal(200, code1);
        Assert.Contains("\"spindle_on\":true", k1);
        Assert.Contains("8000", k1);
        var (code2, _, k2) = Router.Behandeln("POST", "/api/spindle/speed", Bear(z), true, z, "{\"rpm\":12000}");
        Assert.Equal(200, code2);
        Assert.Contains("12000", k2);
        var (codeLeer, _, _) = Router.Behandeln("POST", "/api/spindle/speed", Bear(z), true, z, "{}");
        Assert.Equal(422, codeLeer);
    }

    [Fact]
    public void Kuehlung_Vakuum_Toggle_und_AuxStop()
    {
        var z = Zustand();
        Assert.Contains("\"coolant_on\":true", Router.Behandeln("POST", "/api/coolant/toggle", Bear(z), true, z).Koerper);
        Assert.Contains("\"vacuum_on\":true", Router.Behandeln("POST", "/api/vacuum/toggle", Bear(z), true, z).Koerper);
        var (_, _, stop) = Router.Behandeln("POST", "/api/aux/stop", Bear(z), true, z);
        Assert.Contains("\"spindle_on\":false", stop);
        Assert.Contains("\"coolant_on\":false", stop);
        Assert.Contains("\"vacuum_on\":false", stop);
    }

    [Fact]
    public void Override_Speed_Power_GRBL_Bereich()
    {
        var z = Zustand();
        var (code, _, koerper) = Router.Behandeln("POST", "/api/override/speed", Bear(z), true, z, "{\"percent\":150}");
        Assert.Equal(200, code);
        Assert.Contains("\"speed_percent\":150", koerper);
        var (codeP, _, koerperP) = Router.Behandeln("POST", "/api/override/power", Bear(z), true, z, "{\"percent\":40}");
        Assert.Equal(200, codeP);
        Assert.Contains("\"power_percent\":40", koerperP);
        var (codeHoch, _, _) = Router.Behandeln("POST", "/api/override/speed", Bear(z), true, z, "{\"percent\":300}");
        Assert.Equal(422, codeHoch); // GRBL-Override 10..200
    }

    [Fact]
    public void NullPunkt_Edit_Mit_Verlauf()
    {
        var z = Zustand();
        var (codeLeer, _, _) = Router.Behandeln("POST", "/api/zero/edit", Bear(z), true, z, "{}");
        Assert.Equal(422, codeLeer);
        var (code, _, koerper) = Router.Behandeln("POST", "/api/zero/edit", Bear(z), true, z, "{\"x\":10.5,\"y\":3}");
        Assert.Equal(200, code);
        Assert.Contains("previous", koerper); Assert.Contains("current", koerper);
        // workpiece position spiegelt den Nullpunkt (machine 0 - nullpunkt)
        var (_, _, poll) = Router.Behandeln("GET", "/api/events/poll", Bear(z), true, z);
        Assert.Contains("-10.5", poll);
    }

    [Fact]
    public void Goto_Saved_Add_und_Liste()
    {
        var z = Zustand();
        var (codeAdd, _, koerperAdd) = Router.Behandeln(
            "POST", "/api/goto/saved/add", Bear(z), true, z, "{\"name\":\"Ecke\",\"x\":5,\"y\":7}");
        Assert.Equal(200, codeAdd);
        Assert.Contains("\"saved\":1", koerperAdd);
        var (code, _, koerper) = Router.Behandeln("GET", "/api/goto/saved", Bear(z), true, z);
        Assert.Equal(200, code);
        Assert.Contains("Ecke", koerper);
    }

    [Fact]
    public void File_Open_202_Ersetzt_Projekt()
    {
        var z = Zustand();
        var (code, _, koerper) = Router.Behandeln("POST", "/api/file/open", Bear(z), true, z, "", "test.lbrn2");
        Assert.Equal(202, code);
        Assert.Contains("accepted", koerper);
        Assert.Contains("test.lbrn2", koerper);
        Assert.Empty(((ApiZustand)z).Projekt.Formen); // File→Open: Projekt ganz ersetzt
    }

    [Fact]
    public void Standby_Intern_OK()
    {
        var z = Zustand();
        var (code, _, koerper) = Router.Behandeln("GET", "/api/_internal/standby", Bear(z), true, z);
        Assert.Equal(200, code);
        Assert.Contains("\"ok\":true", koerper);
    }
}
