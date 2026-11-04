using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaserForge.Core.Geraete;

/// <summary>
/// Laser-Geraet im Dienst/GUI (eigene Clean-Room-Struktur). Persistiert als JSON unter
/// %LocalAppData%\LaserForge\geraete.json. Import von LightBurnprefs.ini (JSON; DeviceList)
/// ist Interoperabilitaet nach Doku — Secrets werden bewusst NICHT uebernommen.
/// </summary>
public sealed class Geraet
{
    public string Name { get; set; } = "Neues Geraet";
    public string Protokoll { get; set; } = "GRBL";
    public string Sprache { get; set; } = "GCode";
    public string Anschluss { get; set; } = "";
    public int Baud { get; set; } = 115200;
    public double BreiteMM { get; set; } = 400;
    public double HoeheMM { get; set; } = 400;
    public bool HomeBeimStart { get; set; }
    public double JogXyAbstand { get; set; } = 10;
    public double JogXyTempo { get; set; } = 6000;
    public double JogZAbstand { get; set; } = 10;
    public double JogZTempo { get; set; } = 600;
    public bool SpiegelnX { get; set; }
    public bool SpiegelnY { get; set; }
    public bool ZAchse { get; set; }
    public bool IstStandard { get; set; }

    /// <summary>Anzeige im Geraete-Dialog (Paritaet „Protokoll | Sprache").</summary>
    public string TypZeile => $"{Protokoll} | {Sprache}";
}

/// <summary>Geräteliste je Benutzer (JSON-Datei, verlustfrei).</summary>
public static class GeraeteSpeicher
{
    public static string Pfad =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LaserForge", "geraete.json");

    private static readonly JsonSerializerOptions Optionen = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static List<Geraet> Laden()
    {
        try
        {
            if (!System.IO.File.Exists(Pfad)) return new List<Geraet>();
            // Share.ReadWrite: parallel laufende Schreiber (GUI/API/Tests) blockieren nicht.
            using var st = new System.IO.FileStream(Pfad, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            using var r = new System.IO.StreamReader(st);
            var liste = JsonSerializer.Deserialize<List<Geraet>>(r.ReadToEnd(), Optionen);
            return liste ?? new List<Geraet>();
        }
        catch { return new List<Geraet>(); } // beschädigter Speicher = leere Liste, Dialog bleibt bedienbar
    }

    public static void Speichern(IEnumerable<Geraet> geraete)
    {
        var ordner = System.IO.Path.GetDirectoryName(Pfad)!;
        System.IO.Directory.CreateDirectory(ordner);
        var inhalt = JsonSerializer.Serialize(geraete.ToList(), Optionen);
        // Share.ReadWrite: parallel laufende Leser (GUI/API/Tests) blockieren nicht.
        using var st = new System.IO.FileStream(Pfad, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read);
        using var w = new System.IO.StreamWriter(st);
        w.Write(inhalt);
    }

    /// <summary>Aktives Geraet setzen (Standard-Flag exklusiv) und sofort persistieren.</summary>
    public static void SetzeStandard(List<Geraet> geraete, Geraet geraet)
    {
        foreach (var g in geraete) g.IstStandard = ReferenceEquals(g, geraet);
        Speichern(geraete);
    }
}

/// <summary>
/// Geräte-Import aus einer LightBurn-Prefs-Datei (prefs.ini ist JSON): DeviceList-Sektion.
/// Interop-Parser auf die beobachteten Felder; RestPendantSecret/RestAppApiAuths/Lizenz-Ebene
/// werden NICHT gelesen (bewusste Grenze: keine Secrets/Lizenzen in unsere Daten).
/// </summary>
public static class LightBurnGeraeteImport
{
    public static (List<Geraet> Geraete, int Gefunden) Lesen(string jsonText)
    {
        var liste = new List<Geraet>();
        using var doc = JsonDocument.Parse(jsonText);
        if (!doc.RootElement.TryGetProperty("DeviceList", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return (liste, 0);
        foreach (var e in arr.EnumerateArray())
        {
            var g = new Geraet
            {
                Name = Feld(e, "DisplayName") ?? Feld(e, "Name") ?? "Importiertes Geraet",
                Protokoll = Feld(e, "Name") is string p && p.Length > 0 ? p : "GRBL",
                Sprache = "GCode",
                HomeBeimStart = Bool(e, "HomeOnStartup"),
            };
            if (e.TryGetProperty("Settings", out var s) && s.ValueKind == JsonValueKind.Object)
            {
                g.Anschluss = Feld(s, "CommPort") ?? "";
                g.Baud = (int?)Double(s, "BaudRate") ?? 115200;
                g.JogXyAbstand = Double(s, "JogXYDistance") ?? g.JogXyAbstand;
                g.JogZAbstand = Double(s, "JogZDistance") ?? g.JogZAbstand;
                g.ZAchse = Bool(s, "EnableZ");
            }
            if (Double(e, "Width") is double w) g.BreiteMM = w;
            if (Double(e, "Height") is double h) g.HoeheMM = h;
            // LightBurn speichert Jog-Tempo in mm/s (REA: 10 mm @ 6000 mm/min = 100 mm/s) → wir x60.
            g.JogXyTempo = Double(e, "MoveJogSpeed") is double v1 && v1 > 0 ? v1 * 60 : g.JogXyTempo;
            g.JogZTempo = Double(e, "MoveJogZSpeed") is double v2 && v2 > 0 ? v2 * 60 : g.JogZTempo;
            g.SpiegelnX = Bool(e, "MirrorX");
            g.SpiegelnY = Bool(e, "MirrorY");
            liste.Add(g);
        }
        return (liste, liste.Count);
    }

    private static string? Feld(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) &&
        v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool Bool(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    private static double? Double(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) ? d : null;
}
