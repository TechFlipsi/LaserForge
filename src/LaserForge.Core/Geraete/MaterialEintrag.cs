using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaserForge.Core.Geraete;

/// <summary>
/// Material-Bibliothek (Paritaet zu LightBurns CutLibrary): Datei je Benutzer
/// (%LocalAppData%\LaserForge\material-library.json). /api/material-library liefert die
/// Eintraege im Protokoll-Format (cut mit index=-1/layer_index=-1); leere Datei = leere
/// Bibliothek (wie LightBurn: keine Bibliothek gewaehlt).
/// </summary>
public sealed class MaterialEintrag
{
    public int Guid { get; set; }
    public string Material { get; set; } = "";
    public string Beschreibung { get; set; } = "";
    public string TitelOhneDicke { get; set; } = "";
    public double Dicke { get; set; } = -1;
    public bool Gesperrt { get; set; }
    public string SchnittdatenJson { get; set; } = "";   // leer = Standard-Cut-Block beim Ausliefern
}

public static class MaterialBibliothek
{
    public static string Pfad =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LaserForge", "material-library.json");

    private static readonly JsonSerializerOptions Optionen = new() { WriteIndented = true };

    public static List<MaterialEintrag> Laden()
    {
        try
        {
            if (!System.IO.File.Exists(Pfad)) return new List<MaterialEintrag>();
            using var st = new System.IO.FileStream(Pfad, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            using var r = new System.IO.StreamReader(st);
            var liste = JsonSerializer.Deserialize<List<MaterialEintrag>>(r.ReadToEnd(), Optionen);
            return liste ?? new List<MaterialEintrag>();
        }
        catch { return new List<MaterialEintrag>(); }
    }

    public static void Speichern(IEnumerable<MaterialEintrag> eintraege)
    {
        var ordner = System.IO.Path.GetDirectoryName(Pfad)!;
        System.IO.Directory.CreateDirectory(ordner);
        using var st = new System.IO.FileStream(Pfad, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read);
        using var w = new System.IO.StreamWriter(st);
        w.Write(JsonSerializer.Serialize(eintraege.ToList(), Optionen));
    }
}
