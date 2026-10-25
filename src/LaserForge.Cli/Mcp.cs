using System.Text.Json.Nodes;
using LaserForge.Core.Formate;
using LaserForge.Core.Pipeline;

namespace LaserForge.Cli;

/// <summary>
/// MCP-Server-Modus (stdio JSON-RPC 2.0) — alles-ist-ein-Befehl-Prinzip.
/// Werkzeuge: app_info · maschinen_status(port, baud) [nur Auslesung] · projekt_konvertieren(input).
/// Notifications bleiben stumm; alle Beschreibungen auf Deutsch.
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

            var methode = anfrage["method"] as string ?? "";
            if (methode.Contains("notifications", StringComparison.OrdinalIgnoreCase) ||
                methode.CompareTo("notification") == 0)
                continue;  // Notifications sind stumm.

            JsonNode? kennung = anfrage["id"]?.DeepClone();
            try
            {
                switch (methode)
                {
                    case "initialize":
                        Ausgabe(kennung, Neu()
                        {
                            ["protocolVersion"] = "2024-11-05",
                            ["capabilities"] = Neu() { ["tools"] = Neu() { ["listChanged"] = false } },
                            ["serverInfo"] = Neu() { ["name"] = "laserforge", ["version"] = "0.1.0" },
                        });
                        break;

                    case "tools/list":
                        var werkzeuge = new JsonArray
                        {
                            Werkzeug("app_info", "Infos zu LaserForge (Version, Formate, Projektstand)."),
                            Werkzeug("maschinen_status",
                                "GRBL-Maschinenstatus pruefen (Port + Baud); nur Auslesung, keine Veraenderungen.",
                                ("port", "string", "COM-Port"), ("baud", "number", "Baudrate, Standard 115200")),
                            Werkzeug("projekt_konvertieren",
                                "Erzeugt GCode (GRBL-Flavor) aus einer .lbrn/.lbrn2/.svg-Datei.",
                                ("input", "string", "Dateipfad zum Projekt (.lbrn/.lbrn2/.svg)")),
                        };
                        Ausgabe(kennung, Neu() { ["tools"] = werkzeuge });
                        break;

                    case "tools/call":
                    {
                        var parameter = anfrage["params"] as JsonObject;
                        var name = parameter?["name"] as string ?? "";
                        var aufruf = parameter?["arguments"] as JsonObject;
                        var text = WerkzeugAufruf(name, aufruf);
                        Ausgabe(kennung, Neu()
                        {
                            ["content"] = new JsonArray { Neu() { ["type"] = "text", ["text"] = text } },
                        });
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

    private static JsonObject Neu() => new();

    private static JsonObject FehlerObjekt(int code, string meldung) => Neu()
    {
        ["error"] = Neu() { ["code"] = code, ["message"] = meldung },
    };

    private static JsonObject Werkzeug(string name, string beschreibung, params (string Feld, string Typ, string Titel)[] felder)
    {
        var schema = Neu() { ["type"] = "object" };
        if (felder.Length > 0)
        {
            var eigenschaften = Neu();
            var pflichtf = new JsonArray();
            foreach (var (feld, typ, titel) in felder)
            {
                eigenschaften[feld] = Neu() { ["type"] = typ, ["description"] = titel };
                pflichtf.Add(feld);
            }
            schema["properties"] = eigenschaften;
            schema["required"] = pflichtf;
        }
        return Neu() { ["name"] = name, ["description"] = beschreibung, ["inputSchema"] = schema };
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
                var port = args?["port"] as string;
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
                var eingang = args?["input"] as string;
                if (string.IsNullOrWhiteSpace(eingang) || !File.Exists(eingang)) return "Fehler: 'input'-Datei fehlt.";
                var text = File.ReadAllText(eingang);
                Projekt projekt;
                if (eingang.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    projekt = new Projekt { Formen = { LeserSvg.Lesen(text) } };
                else projekt = LeserLbrn.Lesen(text);
                var zeilen = GCodeErzeuger.Erzeuge(projekt);
                return string.Join("\n", zeilen);
            }

            default:
                return "Fehler: unbekanntes Werkzeug '" + name + "'.";
        }
    }

    private static void Ausgabe(JsonNode? kennung, JsonObject ergebnisOderFehler)
    {
        var antwort = new JsonObject();
        if (kennung is not null) antwort["id"] = kennung;
        antwort["jsonrpc"] = "2.0";
        if (ergebnisOderFehler["error"] is not null)
            antwort["error"] = ergebnisOderFehler["error"];
        else
            antwort["result"] = ergebnisOderFehler;
        Console.Out.WriteLine(antwort.ToJsonString());
        Console.Out.Flush();
    }
}