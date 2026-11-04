using LaserForge.Core.Geraete;
using LaserForge.Devices.Grbl;
using LaserForge.Devices.Transport;

namespace LaserForge.Api.Rest;

/// <summary>
/// Maschinen-Sitzung des API-Dienstes: verbindet — auf Verlangen einer Maschinen-Route —
/// zum aktiven Gerät (GeraeteSpeicher-Standard) und hält die Transportsitzung offen.
/// Der serielle Port ist exklusiv: while LightBurn am Port hängt, schlägt Oeffnen fehlt —
/// der Fehler wird als PortFehler durchgereicht (ehrlche 409-Antwort).
/// Testbar: transportFabrik injiziert Mock-Transporte (End-to-End-Router-Tests ohne COM).
/// </summary>
public sealed class MaschinenSitzung : IDisposable
{
    private readonly Func<string, int, ITransport> _fabrik;
    private readonly object _lock = new();

    public MaschinenSitzung(Func<string, int, ITransport>? transportFabrik = null)
    {
        _fabrik = transportFabrik ?? ((port, baud) => new SeriellTransport(port, baud));
    }

    public GrblVerbindung? Verbindung { get; private set; }
    public string? VerbundenPort { get; private set; }
    public string? PortFehler { get; private set; }
    public GrblStatus? LetzteStatus { get; private set; }
    public ITransport? Transport { get; private set; }

    /// <summary>Verbindet mit dem Gerät-Port (lazy + idempotent). false = PortFehler gesetzt.</summary>
    public bool Oeffnen(Geraet geraet)
    {
        lock (_lock)
        {
            if (Verbindung is { Transport.Verbunden: true } && VerbundenPort == geraet.Anschluss)
                return true;
            Schliessen();
            try
            {
                var t = _fabrik(geraet.Anschluss, geraet.Baud);
                t.Oeffnen();
                Transport = t;
                Verbindung = new GrblVerbindung(t);
                VerbundenPort = geraet.Anschluss;
                PortFehler = null;
                return true;
            }
            catch (Exception ex)
            {
                PortFehler = ex.GetType().Name + ": " + ex.Message;
                Transport = null; Verbindung = null; VerbundenPort = null;
                return false;
            }
        }
    }

    /// <summary>Aktive Position frisch per Realtime-Frage '?' holen (null bei stiller Verbindung).</summary>
    public GrblStatus? FrischePosition()
    {
        try { LetzteStatus = Verbindung?.StatusHolen(); }
        catch { LetzteStatus = null; }
        return LetzteStatus;
    }

    /// <summary>Jog-Abbruch (GRBL-Realtime 0x85).</summary>
    public void JogAbbruch()
    {
        try { Transport?.SchreibenRoh("\x85"); } catch { /* Port weg = egal */ }
    }

    /// <summary>Z-Gehhilfe nicht Teil des Kontrakts — Schließe sauber.</summary>
    public void Schliessen()
    {
        try { Transport?.Schliessen(); } catch { /* war zu */ }
        Transport = null; Verbindung = null; VerbundenPort = null;
    }
    public void Dispose() => Schliessen();
}
