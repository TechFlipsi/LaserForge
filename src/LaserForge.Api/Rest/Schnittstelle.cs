using System.Text.Json.Nodes;
using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
namespace LaserForge.Api.Rest;

/// <summary>
/// Gesamtzustand der API (M7: Projekt + Paarungen + Maschinenbruecken-Status).
/// </summary>
public sealed class ApiZustand
{
    public Projekt Projekt { get; set; } = new();
    public Dictionary<string, Paarung> Paarungen { get; set; } = new();
    public string GeraeteName { get; set; } = "GRBL";
    public bool Verbunden { get; set; }

    /// <summary>Layer-Palette (Protokollfarben wie im REA-Befund belegt).</summary>
    public static string PaletteFarbe(int index) =>
        index switch
        {
            0 => "#000000", 1 => "#0000ff", 2 => "#ff0000", 3 => "#00c000", 4 => "#ffff00",
            5 => "#ff8000", 6 => "#00c0ff", 7 => "#ff00ff", 8 => "#800060", 9 => "#802000",
            10 => "#404040", 11 => "#ffc000", 12 => "#6080ff", 13 => "#c0c0c0", 14 => "#ff0080",
            15 => "#008060", 16 => "#806000", 17 => "#006080", 18 => "#ff6020", 19 => "#808080",
            20 => "#c00000", 21 => "#0080ff", 22 => "#8000ff", 23 => "#00a0a0", 24 => "#a06000",
            25 => "#006000", 26 => "#800000", 27 => "#0040a0", 28 => "#a000a0", 29 => "#404080",
            30 => "#206040",
            _ => "#202020",
        };
}

/// <summary>
/// Protokoll-Router (Paritaet auf die LightBurn-2.2-API-Semantik, eigene Umsetzung).
/// Testbar ohne Netz: Behandeln(methode, pfad, bearer, loopback, zustand) = (Code, Grund, Koerper).
/// JSON-Feldnamen bleiben auf Protokoll-Niveau (Interoperabilitaet); Doku/Logs deutsch.
/// </summary>
public static class Router
{
    public static (int Code, string Grund, string Koerper) Behandeln(
        string methode, string pfad, string? bearer, bool loopback, ApiZustand zustand)
    {
        if (!pfad.StartsWith("/api/", StringComparison.Ordinal))
        {
            return pfad switch
            {
                "/forge/status" => Gruen(JsonObj(("app", "LaserForge"), ("status", "laufend"))),
                "/forge/info" => Gruen(JsonObj(("app", "LaserForge"), ("beschreibung", "Protokollparitaet M7"), ("sprache", "de-DE"))),
                "/" => (200, "OK", JsonObj(
                            ("app", "LaserForge"),
                            ("endpunkte", new JsonArray("/forge/status", "/forge/info", "/api/*"))).ToJsonString()),
                _ => (404, "Nicht gefunden", JsonObj(("error", "Unbekannter Pfad")).ToJsonString()),
            };
        }
        var pfadQuergelesen = pfad.TrimEnd('/');

        if (methode == "POST" && pfadQuergelesen == "/api/connect")
        {
            if (!loopback)
                return (403, "Verboten", JsonObj(("error", "api/connect ist nur ueber Loopback erlaubt (Paarung nur lokal).")).ToJsonString());
            var secret = ZufallsSecret.Erzeugen();
            zustand.Paarungen[secret] = new Paarung
            {
                Anwendungsname = "Paarung" + zustand.Paarungen.Count,
                Kapazitaeten = new List<string> { "state", "project" },
                Secret = secret,
                GekoppeltAm = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            return (200, "OK", JsonObj(("secret", secret), ("hinweis", "Paarung erstellt; Token = HMAC ueber Minutenfenster.")).ToJsonString());
        }

        // Alle API-Endpunkte brauchen einen gueltigen Minuten-Bearer (aktuelles Fenster + Vorminute).
        Paarung? paarung = null;
        if (bearer is not null)
            foreach (var pr in zustand.Paarungen.Values)
                if (pr.TokenGueltig(bearer, pr.Secret)) { paarung = pr; break; }
        if (paarung is null)
            return (401, "Unauthorized", JsonObj(("error", "Unauthorized")).ToJsonString());

        return pfadQuergelesen switch
        {
            "/api/status" => Gruen(MaschineObj(zustand)),
            "/api/position" => Gruen(JsonObj(
                ("machine", PunktObj(0, 0, 0)),
                ("workpiece", PunktObj(0, 0, 0)))),
            "/api/units" => Gruen(JsonObj(("distance_units", "mm"), ("units", "mm/min"))),
            "/api/layers" => Gruen(SchichtenObj(zustand)),
            "/api/cuts" => Gruen(SchnitteeinstellungenObj(zustand)),
            "/api/project" => Gruen(ProjektObj(zustand)),
            _ => (404, "Nicht gefunden", JsonObj(("error", "Endpunkt in dieser M7-Basis noch nicht umgesetzt.")).ToJsonString()),
        };
    }

    private static (int, string, string) Gruen(JsonObject o) => (200, "OK", o.ToJsonString());

    /// <summary>Einfacher JsonObject-Helfer: Key/Wert-Paare (string/bool/double/JsonNode).</summary>
    private static JsonObject JsonObj(params (string Schluessel, object Wert)[] eintraege)
    {
        var obj = new JsonObject();
        foreach (var (schluessel, wert) in eintraege)
        {
            obj[schluessel] = wert switch
            {
                null => null,
                string s => JsonValue.Create(s),
                bool b => JsonValue.Create(b),
                int i => JsonValue.Create(i),
                double d => JsonValue.Create(d),
                JsonNode n => n,
                _ => JsonValue.Create(wert.ToString() ?? ""),
            };
        }
        return obj;
    }

    private static JsonArray NeueListe() => new();

    private static JsonObject PunktObj(double x, double y, double z) =>
        JsonObj(("x", x), ("y", y), ("z", z));

    private static JsonObject MaschineObj(ApiZustand z) => JsonObj(
        ("connected", z.Verbunden),
        ("device_name", z.GeraeteName),
        ("supports_overrides", true),
        ("supports_z", true));

    private static JsonObject SchichtenObj(ApiZustand z)
    {
        var arr = NeueListe();
        var gruppen = z.Projekt.Formen.GroupBy(f => f.SchnittIndex).OrderBy(g => g.Key).ToList();
        if (gruppen.Count == 0)
            arr.Add(JsonObj(("color", ApiZustand.PaletteFarbe(0)), ("hidden", false), ("in_use", false),
                            ("index", 0), ("name", ""), ("shape_count", 0), ("type", "normal")));
        foreach (var g in gruppen)
            arr.Add(JsonObj(
                ("color", ApiZustand.PaletteFarbe(g.Key)),
                ("hidden", false),
                ("in_use", true),
                ("index", g.Key),
                ("name", ""),
                ("shape_count", g.Count()),
                ("type", "normal")));
        return JsonObj(("active_index", 0), ("layers", arr));
    }

    private static JsonObject SchnittObj(SchnittEinstellung s) => JsonObj(
        ("enable_cleanup", false),
        ("enabled", s.Aktiviert),
        ("hidden", s.Versteckt),
        ("in_use", false),
        ("index", s.Index),
        ("kind", "normal"),
        ("layer_index", s.Index),
        ("name", "C" + s.Index.ToString("00")),
        ("negative", false),
        ("params", JsonObj(
            ("mode", s.Typ.ToLowerInvariant()),
            ("frequency", (double)s.Frequency),
            ("dot_width", 0.08),
            ("fiber_pulse_width", 100))));

    private static JsonObject SchnitteeinstellungenObj(ApiZustand z)
    {
        var arr = NeueListe();
        foreach (var s in z.Projekt.GeordneteSchnitte())
            arr.Add(SchnittObj(s));
        if (arr.Count == 0)
            arr.Add(SchnittObj(new SchnittEinstellung { Index = 0, Typ = "Cut" }));
        return JsonObj(("cuts", arr));
    }

    private static JsonObject ProjektObj(ApiZustand z) => JsonObj(
        ("device", JsonObj(
            ("laser", JsonObj(
                ("controller", z.GeraeteName),
                ("flying_galvo", false),
                ("galvo", false),
                ("laser1_fiber", false),
                ("laser2_fiber", false),
                ("mopa_source", 0),
                ("sources", 1),
                ("sources_enabled", 1),
                ("uv", false))),
            ("name", z.GeraeteName),
            ("type", "serial"))),
        ("name", z.GeraeteName),
        ("sources", 1),
        ("workspace", JsonObj(("x", z.Projekt.BreiteMM), ("y", z.Projekt.HoeheMM))),
        ("units", "mm"));
}
