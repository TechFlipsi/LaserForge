using System.Globalization;
using LaserForge.Core.Formate;
using LaserForge.Core.Pipeline;
using LaserForge.Devices.Transport;
using LaserForge.Devices.Grbl;
if (args.Length == 0)
{
    Console.WriteLine("laserforge-cli — Nutzung:");
    Console.WriteLine("  info                          — Versionsausgabe");
    Console.WriteLine("  status <COMx> [baud]          — Verbindungstest + Status");
    Console.WriteLine("  konvert <in.lbrn|.svg> <out>  — Projekt in GCode umfuehren");
    return 1;
}
switch (args[0].ToLowerInvariant())
{
    case "mcp":
        McpServer.Laufen();
        return 0;
    case "info":
        Console.WriteLine($"LaserForge CLI 0.1.0 — .NET {Environment.Version}");
        Console.WriteLine($"Zeit: {DateTime.Now:yyyy-MM-dd HH:mm:ss} — Formate: .lbrn/.lbrn2, GRBL-Flavor");
        return 0;
    case "status":
    {
        if (args.Length < 2 || args.Length > 3)
        { Console.WriteLine("Nutzung: status <COMx> [baud]"); return 1; }
        var baud = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 115200;
        using var transport = new SeriellTransport(args[1], baud);
        var verbindung = new GrblVerbindung(transport);
        verbindung.Oeffnen();
        Console.WriteLine("Verbindung offen — Begruessung:");
        foreach (var z in verbindung.BegruessungZeilen) Console.WriteLine("  > " + z);
        var status = verbindung.StatusHolen();
        Console.WriteLine(status is null ? "Keine Status-Antwort erhalten." : "Status: " + status);
        return 0;
    }
    case "konvert":
    {
        if (args.Length < 3)
        { Console.WriteLine("Nutzung: konvert <in.lbrn> <out.gcode>"); return 1; }
        var text = File.ReadAllText(args[1]);
        Projekt projekt;
        if (args[1].EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            var form = LeserSvg.Lesen(text, Path.GetFileName(args[1]));
            projekt = new Projekt { Formen = { form } };
        }
        else projekt = LeserLbrn.Lesen(text);
        var zeilen = GCodeErzeuger.Erzeuge(projekt);
        File.WriteAllLines(args[2], zeilen);
        Console.WriteLine($"Konvertiert: {projekt.FormenZahl()} Formen -> {zeilen.Count} GCode-Zeilen");
        Console.WriteLine($"Ziel: {Path.GetFullPath(args[2])}");
        return 0;
    }
    default:
        Console.WriteLine($"Unbekannter Befehl '{args[0]}'.");
        return 1;
}
