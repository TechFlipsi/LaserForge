using System.Text.Json;
using System.Text.Json.Nodes;
using LaserForge.Core.Formate;
using LaserForge.Core.Pipeline;

namespace LaserForge.Cli;

/// <summary>
/// MCP-Server-Modus (stdio JSON-RPC 2.0) — alles-ist-ein-Befehl-Prinzip.
/// Werkzeuge: app_info · maschinen_status(port, baud) [nur Auslesung] · projekt_konvertieren(input).
/// Notifications bleiben stumm; alle Beschreibungen auf Deutsch.
/// Aufbau-Regel: JsonNode-Objekte werden Schritt für Schritt gesetzt
/// (Objekt-Initialisierer direkt nach Methodenaufrufen sind in C# unzulässig).
/// </summary>
public static class McpServer
{
    public static void Laufen()
    {
        Console.Out.WriteLine("[MCP] LaserForge MCP bereit (stdio).");
        while (true)
        {
            var zeile = Console.In.ReadLine();
            if (zeile is null) return;
            zeile = zeile.Trim();
            if (zeile.Length == 0) continue;

            JsonObject? anfrage;
            try { anfrage = JsonNode.Parse(zeile) as JsonObject; }
            catch (JsonException)
            {
                Ausgabe(null, FehlerObjekt(-32700, "Ungueltiges JSON."));
                continue;
            }
            if (anfrage is null) continue;

            var methode = (string?)anfrage["method"] ?? "";
            if (methode.Contains("notifications", StringComparison.OrdinalIgnoreCase) ||
                methode.CompareTo("notification") == 0)
                continue;  // Notifications sind stumm.

            JsonNode? kennung = anfrage["id"]?.DeepClone();
            try
            {
                switch (methode)
                {
                    case "initialize":
                        Ausgabe(kennung, InitializeAntwort());
                        break;

                    case "tools/list":
                        Ausgabe(kennung, WerkzeuglisteAntwort());
                        break;

                    case "tools/call":
                    {
                        var parameter = anfrage["params"] as JsonObject;
                        var name = (string?)parameter?["name"] ?? "";
                        var aufruf = parameter?["arguments"] as JsonObject;
                        var text = WerkzeugAufruf(name, aufruf);
                        Ausgabe(kennung, TextInhaltAntwort(text));
                        break;
                    }

                    default:
                        Ausgabe(kennung, FehlerObjekt(-32601, "Unbekannte Methode: " + methode));
                        break;
                }
            }
            catch (Exception ex)
            {
                Ausgabe(kennung, FehlerObjekt(-32603, ex.Message));
            }
        }
    }

    private static JsonObject InitializeAntwort()
    {
        var werkzeugeFahigkeit = Neu();
        werkzeugeFahigkeit["listChanged"] = false;
        var fahigkeiten = Neu();
        fahigkeiten["tools"] = werkzeugeFahigkeit;
        var serverInfo = Neu();
        serverInfo["name"] = "laserforge";
        serverInfo["version"] = "0.1.0";
        var ergebnis = Neu();
        ergebnis["protocolVersion"] = "2024-11-05";
        ergebnis["capabilities"] = fahigkeiten;
        ergebnis["serverInfo"] = serverInfo;
        return ergebnis;
    }

    private static JsonObject WerkzeuglisteAntwort()
    {
        var werkzeuge = new JsonArray();
        werkzeuge.Add(Werkzeug("app_info", "Infos zu LaserForge (Version, Formate, Projektstand)."));
        werkzeuge.Add(Werkzeug("maschinen_status",
            "GRBL-Maschinenstatus pruefen (Port + Baud); nur Auslesung, keine Veraenderungen.",
            ("port", "string", "COM-Port"), ("baud", "number", "Baudrate, Standard 115200")));
        werkzeuge.Add(Werkzeug("projekt_konvertieren",
            "Erzeugt GCode (GRBL-Flavor) aus einer .lbrn/.lbrn2/.svg-Datei.",
            ("input", "string", "Dateipfad zum Projekt (.lbrn/.lbrn2/.svg)")));
        var ergebnis = Neu();
        ergebnis["tools"] = werkzeuge;
        return ergebnis;
    }

    private static JsonObject TextInhaltAntwort(string text)
    {
        var stueck = Neu();
        stueck["type"] = "text";
        stueck["text"] = text;
        var inhalt = new JsonArray();
        inhalt.Add(stueck);
        var ergebnis = Neu();
        ergebnis["content"] = inhalt;
        return ergebnis;
    }

    private static JsonObject Neu() => new();

    private static JsonObject FehlerObjekt(int code, string meldung)
    {
        var inwendig = Neu();
        inwendig["code"] = code;
        inwendig["message"] = meldung;
        var o = Neu();
        o["error"] = inwendig;
        return o;
    }

    private static JsonObject Werkzeug(string name, string beschreibung, params (string Feld, string Typ, string Titel)[] felder)
    {
        var schema = Neu();
        schema["type"] = "object";
        if (felder.Length > 0)
        {
            var eigenschaften = Neu();
            var pflichtf = new JsonArray();
            foreach (var (feld, typ, titel) in felder)
            {
                var einzeln = Neu();
                einzeln["type"] = typ;
                einzeln["description"] = titel;
                eigenschaften[feld] = einzeln;
                pflichtf.Add(feld);
            }
            schema["properties"] = eigenschaften;
            schema["required"] = pflichtf;
        }
        var o = Neu();
        o["name"] = name;
        o["description"] = beschreibung;
        o["inputSchema"] = schema;
        return o;
    }

    private static string WerkzeugAufruf(string name, JsonObject? args)
    {
        switch (name)
        {
            case "app_info":
                return "LaserForge 0.1.0 — saubere Open-Source-Laserschnitt-/Gravursteuerung "
                     + "(.lbrn/.lbrn2-Lesen, GRBL-Flavor, Avalonia-GUI). Sprache: Deutsch.";

            case "maschinen_status":
            {
                var port = (string?)args?["port"];
                if (string.IsNullOrWhiteSpace(port)) return "Fehler: 'port' fehlt.";
                var baud = 115200;
                if (args?["baud"] is JsonValue v && v.TryGetValue(out double d)) baud = (int)d;
                try
                {
                    var transport = new LaserForge.Devices.Transport.SeriellTransport(port, baud);
                    var verbindung = new LaserForge.Devices.Grbl.GrblVerbindung(transport);
                    verbindung.Oeffnen();
                    var status = verbindung.StatusHolen();
                    transport.Schliessen();
                    return status is null ? "Keine Status-Antwort im Zeitfenster." : status.ToString();
                }
                catch (Exception ex) { return "Verbindungsfehler: " + ex.Message; }
            }

            case "projekt_konvertieren":
            {
                var eingang = (string?)args?["input"];
                if (string.IsNullOrWhiteSpace(eingang) || !File.Exists(eingang)) return "Fehler: 'input'-Datei fehlt.";
                var quelle = File.ReadAllText(eingang);
                Projekt projekt;
                if (eingang.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    projekt = new Projekt { Formen = { LeserSvg.Lesen(quelle) } };
                else projekt = LeserLbrn.Lesen(quelle);
                var zeilen = GCodeErzeuger.Erzeuge(projekt);
                return string.Join("\n", zeilen);
            }

            default:
                return "Fehler: unbekanntes Werkzeug '" + name + "'.";
        }
    }

    private static void Ausgabe(JsonNode? kennung, JsonObject inhaltOderFehler)
    {
        var antwort = new JsonObject();
        if (kennung is not null) antwort["id"] = kennung;
        antwort["jsonrpc"] = "2.0";
        if (inhaltOderFehler["error"] is not null)
            antwort["error"] = inhaltOderFehler["error"];
        else
            antwort["result"] = inhaltOderFehler;
        Console.Out.WriteLine(antwort.ToJsonString());
        Console.Out.Flush();
    }
}