using System.Security.Cryptography;
using System.Text;
namespace LaserForge.Api.Rest;
/// <summary>
/// Paarung + Minuten-Token nach dem LightBurn-2.2-Schema (Protokoll-Paritaet, eigene Implementierung):
/// Bearer = hex(HMAC-SHA256(secret, zeitFenster(UNIX//60))); Server akzeptiert aktuelles+Vorminute.
/// </summary>
public sealed class Paarung
{
    public string Anwendungsname { get; set; } = "";
    public List<string> Kapazitaeten { get; set; } = new();
    public string Secret { get; set; } = "";
    public long GekoppeltAm { get; set; } // UNIX-Zeit
    public string BearerToken(long zeitSekunden, string secret)
    {
        using var hm = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var wert = hm.ComputeHash(Encoding.UTF8.GetBytes((zeitSekunden / 60).ToString()));
        return Convert.ToHexString(wert).ToLowerInvariant();
    }
    public bool TokenGueltig(string? beearer, string secret)
    {
        if (string.IsNullOrWhiteSpace(beearer)) return false;
        long jetzt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (beearer == BearerToken(jetzt, secret)) return true;
        if (beearer == BearerToken(jetzt - 60, secret)) return true;  // Clock-Drift-Toleranz
        return false;
    }
}
public static class ZufallsSecret
{
    public static string Erzeugen(int bytes = 32)
    {
        var puffer = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToHexString(puffer).ToLowerInvariant();
    }
}
