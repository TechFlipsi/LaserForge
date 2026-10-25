using System.Globalization;
using LaserForge.Core.Geometrie;
namespace LaserForge.Devices.Grbl;
/// <summary>
/// GRBL-Verbindung (Begruessung + Status-Poll + Jog + Home + Rahmen). Sicher:
/// keine Bewegungs-/Feuerroutine ohne ausdrueckliche Benutzerausloesung.
/// </summary>
public sealed class GrblVerbindung
{
    private static readonly CultureInfo E = CultureInfo.InvariantCulture;
    public LaserForge.Devices.Transport.ITransport Transport { get; }
    public List<string> BegruessungZeilen { get; } = new();
    public GrblVerbindung(LaserForge.Devices.Transport.ITransport transport) { Transport = transport; }
    public void Oeffnen()
    {
        Transport.Oeffnen();
        BegruessungZeilen.Clear();
        // Begruessung geduldig abholen: Boot nach DTR-Reset dauert 2-3s+, FW-Antworten
        // kommen mit 600-1500ms Latenz (MKS-DLC32-Lernreihe); bis 10s Gesamtfenster.
        var ende = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < ende)
        {
            var z = Transport.LesenZeile(250);
            if (z is null) continue;
            BegruessungZeilen.Add(z);
            if (z.StartsWith("[VER", StringComparison.OrdinalIgnoreCase) ||
                z.StartsWith("Grbl", StringComparison.OrdinalIgnoreCase) ||
                z == "ok")
                break;
        }
    }
    public GrblStatus? StatusHolen()
    {
        Transport.SchreibenRoh("?");
        // Status-Report abwarten (FW-Latenz 600-1500ms): bis 3s auf "<...>"-Zeile warten
        var ende = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < ende)
        {
            var z = Transport.LesenZeile(200);
            if (z is not null && z.StartsWith('<') && z.EndsWith('>'))
                return GrblStatus.AusZeile(z);
        }
        return null;
    }
    public bool Joggen(string achse, double mm, double feedMmMin)
    {
        Transport.SchreibenZeile($"$J=G91 {achse}{mm.ToString("0.###", E)} F{feedMmMin.ToString("0", E)}");
        return WarteAufOk();
    }
    public bool HomeXY()
    {
        Transport.SchreibenZeile("G28 X0 Y0");
        return WarteAufOk();
    }
    public bool GeheZ(double z)
    {
        Transport.SchreibenZeile($"G0 Z{z.ToString("0.###", E)}");
        return WarteAufOk();
    }
    /// <summary>Arbeitsflaeche-Rahmen (optische Justage) mit geringer Power.</summary>
    public bool Rahmen(double x, double y, double w, double h, double powerPercent = 2, double speedMmS = 25)
    {
        var ecken = new[] { new Punkt(x, y), new Punkt(x + w, y), new Punkt(x + w, y + h), new Punkt(x, y + h), new Punkt(x, y) };
        var liste = new List<string>
        {
            $"G0 X{x.ToString("0.###", E)} Y{y.ToString("0.###", E)}",
            $"M4 S{powerPercent * 10:0}",
        };
        for (int i = 1; i < ecken.Length; i++)
            liste.Add($"G1 X{ecken[i].X.ToString("0.###", E)} Y{ecken[i].Y.ToString("0.###", E)} F{speedMmS * 60:0}");
        liste.Add("M5");
        foreach (var zeile in liste)
        {
            Transport.SchreibenZeile(zeile);
            if (!WarteAufOk(4000)) return false;
        }
        return true;
    }
    private bool WarteAufOk(int timeoutMs = 2500)
    {
        var ende = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < ende)
        {
            var z = Transport.LesenZeile(150);
            if (z == "ok") return true;
            if (z is not null && z.StartsWith("error", StringComparison.OrdinalIgnoreCase)) return false;
        }
        return false;
    }
}
