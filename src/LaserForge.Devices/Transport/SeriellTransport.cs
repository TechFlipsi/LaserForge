using System.IO.Ports;
using System.Text;
namespace LaserForge.Devices.Transport;
/// <summary>
/// Serielle Zeilenverbindung (CH340/CDC). Praxis-Fakten (MKS-DLC32-Testreihe): CR-LF noetig,
/// DTR/RTS an, 2-3s Boot-Latenz, 50ms-Polllesen statt Block.
/// </summary>
public sealed class SeriellTransport : ITransport
{
    private readonly string _port;
    private readonly int _baud;
    private SerialPort? _ser;
    public SeriellTransport(string port, int baud = 115200) { _port = port; _baud = baud; }
    public bool Verbunden => _ser is { IsOpen: true };
    public void Oeffnen()
    {
        if (Verbunden) return;
        _ser = new SerialPort(_port, _baud, Parity.None, 8, StopBits.One)
        {
            NewLine = "\r\n",
            ReadTimeout = 100,
            DtrEnable = true,
            RtsEnable = true,
        };
        _ser.Open();
        Thread.Sleep(2500);
        _ser.DiscardInBuffer();
    }
    public void Schliessen()
    {
        try { _ser?.Close(); } catch (InvalidOperationException) { /* war zu */ }
        _ser = null;
    }
    public void SchreibenZeile(string zeile)
    {
        if (_ser is not { IsOpen: true }) throw new InvalidOperationException("Serielle Verbindung nicht offen.");
        _ser.Write(zeile.TrimEnd('\r', '\n') + "\r\n");
    }
    public string? LesenZeile(int timeoutMs = 100)
    {
        if (_ser is not { IsOpen: true }) return null;
        var ende = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var sb = new StringBuilder();
        while (DateTime.UtcNow < ende)
        {
            var stueck = _ser.ReadExisting();
            if (stueck.Length > 0)
            {
                sb.Append(stueck);
                if (sb.ToString().Contains('\n')) break;
            }
            Thread.Sleep(50);
        }
        var text = sb.ToString();
        var zeilen = text.Split('\n').Select(z => z.Trim('\r', ' ', '\0')).Where(z => z.Length > 0).ToList();
        return zeilen.Count > 0 ? zeilen[^1] : null;
    }
    public void Dispose() => Schliessen();
}
