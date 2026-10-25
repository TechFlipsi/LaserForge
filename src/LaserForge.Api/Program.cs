using System.Net;
using System.Net.Sockets;
using System.Text;
// LaserForge-API: leichtes eigenes REST auf TcpListener (Port 19530 — bewusst EIGENER Port;
// 19520 bleibt fremdes Programm). Zweck: QA-Automatisierung + Integration. Deutsche Antworten.
const int Port = 19530;
var listener = new TcpListener(IPAddress.Loopback, Port);
listener.Start();
Console.WriteLine($"LaserForge-API lauscht auf http://127.0.0.1:{Port}/ — bereit.");
while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();
    try { Behandeln(client); }
    catch (Exception ex) { Console.WriteLine($"Verbindungsfehler: {ex.Message}"); }
}
static void Behandeln(TcpClient client)
{
    using (client)
    {
        var strom = client.GetStream();
        strom.ReadTimeout = 2000;
        var sb = new StringBuilder();
        var puff = new byte[4096];
        while (!sb.ToString().Contains("\r\n\r\n"))
        {
            int n = strom.Read(puff, 0, puff.Length);
            if (n <= 0) break;
            sb.Append(Encoding.UTF8.GetString(puff, 0, n));
        }
        var kopf = sb.ToString();
        var erste = kopf.Split("\r\n")[0];
        var teile = erste.Split(' ');
        var pfad = teile.Length > 1 ? teile[1] : "/";
        (int Code, string Grund, string Koerper) antwort = pfad switch
        {
            "/forge/status" => (200, "OK", """{"app":"LaserForge","status":"laufend","version":"0.1.0","hinweis":"M1-Skelett; Maschinen-Funktionen folgen in M4"}"""),
            "/forge/info"   => (200, "OK", """{"app":"LaserForge","beschreibung":"Clean-Room Laser-Cutter-/Engraver-Steuerung","sprache":"de-DE"}"""),
            "/"             => (200, "OK", """{"app":"LaserForge","endpunkte":["/forge/status","/forge/info"]}"""),
            _               => (404, "Nicht gefunden", """{"fehler":"Unbekannter Pfad"}"""),
        };
        var body = Encoding.UTF8.GetBytes(antwort.Koerper);
        var kopfbytes = Encoding.UTF8.GetBytes(
            $"HTTP/1.1 {antwort.Code} {antwort.Grund}\r\n" +
            "Content-Type: application/json; charset=utf-8\r\n" +
            $"Content-Length: {body.Length}\r\n" +
            "Connection: close\r\n\r\n");
        strom.Write(kopfbytes, 0, kopfbytes.Length);
        strom.Write(body, 0, body.Length);
        strom.Flush();
    }
}
