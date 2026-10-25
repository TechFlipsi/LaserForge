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
/// <summary>Paarungen verlustfrei persistieren (Neustart des Dienstes behält Zustände).</summary>
    public void Speichern(string pfad)
    {
        try
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(pfad)!);
            var liste = new System.Text.Json.Nodes.JsonArray();
            foreach (var p in Paarungen.Values)
                liste.Add(new System.Text.Json.Nodes.JsonObject
                {
                    ["application_name"] = p.Anwendungsname,
                    ["capabilities"] = string.Join(",", p.Kapazitaeten),
                    ["secret"] = p.Secret,
                    ["gekoppelte_zeit"] = p.GekoppeltAm,
                });
            var o = new System.Text.Json.Nodes.JsonObject { ["paarungen"] = liste };
            System.IO.File.WriteAllText(pfad, o.ToJsonString());
        }
        catch (Exception) { /* Zustandsspeicher ist optional — kein Abbruch des Dienstes */ }
    }
    public void Laden(string pfad)
    {
        try
        {
            if (!System.IO.File.Exists(pfad)) return;
            var o = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(pfad)) as System.Text.Json.Nodes.JsonObject;
            if (o?["paarungen"] is not System.Text.Json.Nodes.JsonArray arr) return;
            foreach (var e in arr)
                if (e is System.Text.Json.Nodes.JsonObject jo)
                {
                    var secret = (string?)jo["secret"] ?? "";
                    if (secret.Length == 0) continue;
                    Paarungen[secret] = new Paarung
                    {
                        Anwendungsname = (string?)jo["application_name"] ?? "",
                        Kapazitaeten = (((string?)jo["capabilities"]) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        Secret = secret,
                        GekoppeltAm = jo["gekoppelte_zeit"] is System.Text.Json.Nodes.JsonValue v && v.TryGetValue(out double d) ? (long)d : 0,
                    };
                }
        }
        catch (Exception) { /* beschädigter Speicher = ignorieren */ }
    }
}


/// <summary>
/// Protokoll-Router (Paritaet auf die LightBurn-2.2-API-Semantik, eigene Umsetzung).
/// Testbar ohne Netz: Behandeln(methode, pfad, bearer, loopback, zustand) = (Code, Grund, Koerper).
/// JSON-Feldnamen bleiben auf Protokoll-Niveau (Interoperabilitaet); Doku/Logs deutsch.
/// </summary>
public static class Router
{
    public static (int Code, string Grund, string Koerper) Behandeln(
        string methode, string pfad, string? bearer, bool loopback, ApiZustand zustand, string koerperText = "")
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

        // Datei-Upload (Protokoll-Werkzeug: Projekt in den Dienst laden)
        if (methode == "POST" && pfadQuergelesen == "/api/file/upload")
            return DateiUpload(zustand, koerperText);

        // Dinamischer Arm: /api/cuts/{index}
        var cutsMuster = "/api/cuts/";
        if (pfadQuergelesen.StartsWith(cutsMuster, StringComparison.Ordinal) &&
            int.TryParse(pfadQuergelesen[cutsMuster.Length..], out var cutIx))
        {
            var s = zustand.Projekt.Schnitte.FirstOrDefault(x => x.Index == cutIx);
            if (s is null)
                return (404, "Nicht gefunden", JsonObj(("error", "Schnittzeile " + cutIx + " existiert nicht.")).ToJsonString());
            return (200, "OK", SchnittObj(s).ToJsonString());
        }

        // SSE-Events (Protokoll-Paritaet; begrenzt dokumentierter Modus: Anredegung + 3 Positions-
        // Ereignisse in 2s-Abstand, dann Verbindung schließen — Live-Monitore kommen mit dem GUI-Integrations-Zyklus)
        if (pfadQuergelesen == "/api/events")
        {
            var stuecke = new List<string> { ": verbunden\n\n" };
            return (200, "OK", "SSE-STREAM");  // Sonderfall: Program schreibt rohen SSE-Strom
        }

        return pfadQuergelesen switch
        {
            "/api/status" => Gruen(MaschineObj(zustand)),
            "/api/overlay" => Gruen(OverlayObj(zustand)),
            "/api/position" => Gruen(JsonObj(
                ("machine", PunktObj(0, 0, 0)),
                ("workpiece", PunktObj(0, 0, 0)))),
            "/api/units" => Gruen(JsonObj(("distance_units", "mm"), ("units", "mm/min"))),
            "/api/layers" => Gruen(SchichtenObj(zustand)),
            "/api/jog/settings" => Gruen(JsonObj(
                ("xy", JsonObj(("distance", 10.0), ("speed", 6000.0))),
                ("z", JsonObj(("distance", 10.0), ("speed", 600.0))))),
            "/api/goto/saved" => Gruen(JsonObj(("positions", NeueListe()))),
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
    /// <summary>Projekt-Upload: JSON {filename, content} — Inhalt bei .lbrn/.lbrn2/.svg als Text parsen.</summary>
    private static (int, string, string) DateiUpload(ApiZustand zustand, string koerperText)
    {
        try
        {
            var json = System.Text.Json.Nodes.JsonNode.Parse(koerperText) as JsonObject;
            var dateiname = (string?)json?["filename"] ?? "upload.lbrn";
            var inhalt = (string?)json?["content"] ?? "";
            if (inhalt.Length == 0)
                return (422, "Ungueltiger Inhalt", JsonObj(("error", "content fehlt.")).ToJsonString());
            // Nur Projektformate in den Dienst-Zustand laden.
            var k = dateiname.ToLowerInvariant();
            Projekt geladen;
            if (k.EndsWith(".svg"))
                geladen = new Projekt { Formen = { LeserSvg.Lesen(inhalt, System.IO.Path.GetFileName(dateiname)) } };
            else if (k.EndsWith(".lbrn") || k.EndsWith(".lbrn2"))
                geladen = LeserLbrn.Lesen(inhalt);
            else
                return (415, "Nicht unterstuetzt", JsonObj(("error", "Nur .lbrn/.lbrn2/.svg werden geladen.")).ToJsonString());
            zustand.Projekt = geladen;
            var ordner = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LaserForge", "uploads");
            System.IO.Directory.CreateDirectory(ordner);
            var sicher = System.IO.Path.GetFileName(dateiname);
            var pfad = System.IO.Path.Combine(ordner, sicher);
            System.IO.File.WriteAllText(pfad, inhalt);
            return (200, "OK", JsonObj(("status", "ok"), ("saved", pfad), ("formen", geladen.FormenZahl())).ToJsonString());
        }
        catch (InvalidDataException idex)
        {
            return (422, "Ungueltiger Inhalt", JsonObj(("error", idex.Message)).ToJsonString());
        }
        catch (Exception ex)
        {
            return (500, "Fehler", JsonObj(("error", ex.Message)).ToJsonString());
        }
    }

    /// <summary>Overlay: Canvas-Zusammenfassung je Form (Protokollniveau, deutschkommentiert).</summary>
    private static JsonObject OverlayObj(ApiZustand z)
    {
        var arr = NeueListe();
        foreach (var f in z.Projekt.Formen)
        {
            var zuege = f.ZuZuege();
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var zg in zuege)
                foreach (var p in zg.Punkte)
                {
                    if (p.X < minX) minX = p.X; if (p.X > maxX) maxX = p.X;
                    if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y;
                }
            arr.Add(JsonObj(
                ("rohtyp", f.Rohtyp()),
                ("cut_index", f.SchnittIndex),
                ("zug_count", zuege.Count),
                ("bbox", JsonObj(
                    ("x", minX > maxX ? 0 : minX), ("y", minY > maxY ? 0 : minY),
                    ("w", minX > maxX ? 0 : maxX - minX), ("h", minY > maxY ? 0 : maxY - minY)))));
        }
        return JsonObj(("shapes", arr));
    }
}
