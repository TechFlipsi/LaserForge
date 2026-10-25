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
        for (int i = 0; i < 20; i++)
        {
            var z = Transport.LesenZeile(300);
            if (z is null) break;
            BegruessungZeilen.Add(z);
            if (z == "ok") break;
        }
    }
    public GrblStatus? StatusHolen()
    {
        Transport.SchreibenZeile("?");
        return GrblStatus.AusZeile(Transport.LesenZeile(600));
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
