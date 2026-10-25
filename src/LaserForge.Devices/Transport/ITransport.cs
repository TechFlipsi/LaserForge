namespace LaserForge.Devices.Transport;
/// <summary>Abstraktion einer Zeilen-Verbindung zum Geraet (Serial, spaeter TCP/UDP).</summary>
public interface ITransport : IDisposable
{
    bool Verbunden { get; }
    void Oeffnen();
    void Schliessen();
    void SchreibenZeile(string zeile);
    string? LesenZeile(int timeoutMs = 100);
}
