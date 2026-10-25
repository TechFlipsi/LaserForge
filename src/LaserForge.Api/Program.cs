using System.Net;
using System.Net.Sockets;
using System.Text;
using LaserForge.Api.Rest;
// ===========================================================================
// LaserForge-API — Schale: TcpListener + HTTP-1.1-Auslesung; die gesamte
// Protokolllogik liegt testbar in Rest.Router (Rest/Schnittstelle.cs).
// Port: eigener Standard 19540 (CLI-Argument 1); 19520 bleibt dem Fremdprogramm.
// Paarung nur Loopback; alle Protokoll-JSON-Endpunkte brauchen den Bearer-Minuten-Token.
// ===========================================================================
int port = args.Length > 0 && int.TryParse(args[0], out var p0) ? p0 : 19540;
var zustand = new ApiZustand();
var listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();
Console.WriteLine($"LaserForge-API lauscht auf http://127.0.0.1:{port}/ — Protokollparitaet bereit (Paarung nur Loopback).");
while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();
    try { Behandeln(client); }
    catch (Exception ex) { Console.WriteLine($"Verbindungsfehler: {ex.Message}"); }
}
void Behandeln(TcpClient client)
{
    using (client)
    {
        var strom = client.GetStream();
        strom.ReadTimeout = 3000;
        var sb = new StringBuilder();
        var puff = new byte[8192];
        while (!sb.ToString().Contains("\r\n\r\n"))
        {
            int n = strom.Read(puff, 0, puff.Length);
            if (n <= 0) break;
            sb.Append(Encoding.UTF8.GetString(puff, 0, n));
        }
        var erste = sb.ToString().Split("\r\n")[0];
        var teile = erste.Split(' ');
        if (teile.Length < 2) return;
        var methode = teile[0];
        var pfad = teile[1].Split('?')[0];
        string? bearer = null;
        foreach (var z in sb.ToString().Split("\r\n"))
            if (z.StartsWith("Authorization: Bearer ", StringComparison.OrdinalIgnoreCase))
                bearer = z[21..].Trim();
        var loopback = client.Client.RemoteEndPoint is IPEndPoint ip && IPAddress.IsLoopback(ip.Address);
        var (code, grund, koerper) = Router.Behandeln(methode, pfad, bearer, loopback, zustand);
        var body = Encoding.UTF8.GetBytes(koerper);
        var kopf = Encoding.UTF8.GetBytes(
            $"HTTP/1.1 {code} {grund}\r\n" +
            "Content-Type: application/json; charset=utf-8\r\n" +
            $"Content-Length: {body.Length}\r\n" +
            "Connection: close\r\n\r\n");
        strom.Write(kopf, 0, kopf.Length);
        strom.Write(body, 0, body.Length);
        strom.Flush();
    }
}
