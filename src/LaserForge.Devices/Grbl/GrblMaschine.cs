using System.Globalization;
namespace LaserForge.Devices.Grbl;
/// <summary>
/// GRBL-Erweiterungen ueber die Basisverbindung: Willkommensversion, $-Maschineinstellungen
/// ($$), Homing bei Start (HomeOnStartup), ALARM-Erkennung, Baud-Auto-Erkennung, COM-Scan.
/// Sicherheitsregeln: alle Schreibzugriffe ($x=) nur im Send-Response-Modus; bei error
/// waehrend Jobs: ABSENDEN STOPPEN (kein Weiterstreamen bei verweigerten Befehlen).
/// </summary>
public sealed class GrblAusruestung
{
    private readonly GrblVerbindung _v;
    public GrblAusruestung(GrblVerbindung verbindung) { _v = verbindung; }
    /// <summary>Version aus dem Willkommenstring ("Grbl 1.1h ['$' for help]").</summary>
    public string? Version {
        get
        {
            foreach (var z in _v.BegruessungZeilen)
            {
                var t = z.Trim();
                if (t.StartsWith("Grbl", StringComparison.OrdinalIgnoreCase))
                {
                    var teile = t.Split(' ');
                    if (teile.Length > 1) return teile[1];
                }
                if (t.StartsWith("[VER:", StringComparison.OrdinalIgnoreCase))
                {
                    var inhalt = t.Trim('[', ']');
                    var teile = inhalt.Split(':');
                    if (teile.Length > 1) return teile[^1].Trim();
                }
            }
            return null;
        }
    }
    /// <summary>$-Settings lesen ("$$") -> Schluessel=StringWert; bis "ok".</summary>
    public Dictionary<string, string> EinstellungenLesen(int timeoutMs = 4000)
    {
        var liste = new Dictionary<string, string>();
        _v.Transport.SchreibenZeile("$$");
        var ende = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < ende)
        {
            var z = _v.Transport.LesenZeile(200);
            if (z is null || z.Trim() == "ok") break;
            var ix = z.IndexOf('=');
            if (ix > 0)
            {
                var schluessel = z[..ix].Trim();
                var wert = z[(ix + 1)..].Trim();
                if (schluessel.StartsWith('$')) liste[schluessel] = wert;
            }
        }
        return liste;
    }
    /// <summary>Lasermodus-Test ($32): true = Lasermodus an; false = WARNUNG nötig (Brandgefahr bei S!=0 mit G0).</summary>
    public bool LasermodusAn(Dictionary<string, string>? einstellungen = null) =>
        (einstellungen ?? EinstellungenLesen()).TryGetValue("$32", out var wert) && wert.Trim() == "1";
    /// <summary>Homing bei Start ("$H" + bis 60s auf ok).</summary>
    public bool HomeMitWarten(int timeoutMs = 60000)
    {
        _v.Transport.SchreibenZeile("$H");
        return _v.WarteAufOkLange(timeoutMs);
    }
    /// <summary>Baud-Auto: probiert Baudraten, bis ein Willkommenstring kam.</summary>
    public static int? BaudAutomatik(string port, int[]? kandidaten = null, int versuchTimeoutS = 6)
    {
        var list = kandidaten ?? new[] { 115200, 9600, 57600, 250000, 250 };
        foreach (var baud in list)
        {
            try
            {
                var t = new LaserForge.Devices.Transport.SeriellTransport(port, baud);
                var v = new GrblVerbindung(t);
                v.Oeffnen();
                var hatBegruessung = false;
                foreach (var z in v.BegruessungZeilen)
                {
                    var tt = z.Trim();
                    if (tt.StartsWith("Grbl", StringComparison.OrdinalIgnoreCase) || tt.StartsWith("[VER:", StringComparison.OrdinalIgnoreCase))
                        hatBegruessung = true;
                }
                t.Schliessen();
                if (hatBegruessung) return baud;
            }
            catch (Exception) { /* naechste Baudrate */ }
        }
        return null;
    }
}
public static class SeriellSuche
{
    /// <summary>Verfuegbare COM-Ports (fuer Geraeft-Dropdown + Port-belegt-Hinweis).</summary>
    public static List<string> Portliste()
    {
        try { return System.IO.Ports.SerialPort.GetPortNames().OrderBy(p => p).ToList(); }
        catch (Exception) { return new(); }
    }
}
