using LaserForge.Core.Geraete;
using LaserForge.Core.Pipeline;
using LaserForge.Devices.Grbl;

namespace LaserForge.Api.Rest;

/// <summary>
/// Job-Runner des API-Dienstes (M9): erzeugt GRBL-GCode aus dem Projekt (Kern-Pipeline,
/// Golden-Spec-Flavor) und fährt ihn zeilenweise mit ok-Sync zum Gerät.
/// Sicherheitsregeln (aus der GRBL-Verbindungs-Dokumentation): Laser-Modus $32=1 Pflicht
/// (sonst Brandgefahr bei S!=0 mit G0); bei error: SENDEN STOPPEN (kein Weiterstreamen);
/// Realtime: Hold 0x21, Resume 0x7E, Abort 0x18.
/// </summary>
public sealed class JobEngine
{
    private readonly object _lock = new();
    private Task? _lauf;
    private volatile bool _hold;
    private volatile bool _abbruch;

    /// <summary>Startet den Job (hintereinander-Thread). false = verweigert (fehltext gesetzt).</summary>
    public bool Start(MaschinenSitzung sitzung, ApiZustand zustand, out string fehlertext)
    {
        lock (_lock)
        {
            if (_lauf is not null && !_lauf.IsCompleted)
            {
                fehlertext = "Ein Job läuft bereits.";
                return false;
            }
            var verbindung = sitzung.Verbindung;
            if (verbindung is null || sitzung.Transport is not { Verbunden: true })
            {
                fehlertext = "Keine Maschinenverbindung.";
                return false;
            }
            _abbruch = false; _hold = false;
            // Sicherheits-Gate: Laser-Modus prüfen ($32) — kein Brand-Risiko aus dem API-Weg.
            var ausrüstung = new GrblAusruestung(verbindung);
            if (!ausrüstung.LasermodusAn())
            {
                fehlertext = "Laser-Modus $32=1 fehlt am Gerät (Brandgefahr bei S!=0 mit G0) — Job verweigert.";
                return false;
            }
            var zeilen = GCodeErzeuger.Erzeuge(zustand.Projekt);
            if (zeilen.Count == 0)
            {
                fehlertext = "Projekt enthält keine zu fahrenden Konturen.";
                return false;
            }
            var transport = verbindung.Transport;
            zustand.JobZustand = "running";
            var gesamt = zeilen.Count;
            _lauf = Task.Run(() =>
            {
                for (int i = 0; i < gesamt; i++)
                {
                    if (_abbruch) { try { transport.SchreibenRoh("\x18"); } catch { } break; } // 0x18 Abort
                    while (_hold && !_abbruch) Thread.Sleep(150);
                    if (_abbruch) { try { transport.SchreibenRoh("\x18"); } catch { } break; }
                    try
                    {
                        transport.SchreibenZeile(zeilen[i]);
                        if (!verbindung.WarteAufOkLange(5000))
                        {
                            // error/no-ok: SENDEN STOPPEN (Sicherheitsregel), Abort ins Gerät.
                            try { transport.SchreibenRoh("\x18"); } catch { }
                            break;
                        }
                    }
                    catch { break; }
                    zustand.JobFortschritt = Math.Round((i + 1) * 100.0 / gesamt, 2);
                }
                zustand.JobZustand = "idle";
            });
            fehlertext = "";
            return true;
        }
    }

    /// <summary>Pause-Toggle: Feed-Hold (0x21) / Resume (0x7E). Liefert Zustand nach dem Umschalten.</summary>
    public string HalteOderFortsetzen(MaschinenSitzung sitzung)
    {
        lock (_lock)
        {
            var transport = sitzung.VerbundenPort is null ? null : sitzung.Transport;
            if (_lauf is null || _lauf.IsCompleted) return "idle";
            if (!_hold)
            {
                _hold = true;
                try { transport?.SchreibenRoh("\x21"); } catch { /* Port weg */ }
                return "paused";
            }
            _hold = false;
            try { transport?.SchreibenRoh("\x7E"); } catch { /* Port weg */ }
            return "running";
        }
    }

    /// <summary>Job hart abbrechen (Abort 0x18 + Lauf stoppt beim nächsten Takt).</summary>
    public void Stoppen(MaschinenSitzung sitzung)
    {
        _abbruch = true;
        _hold = false;
        try { sitzung.Transport?.SchreibenRoh("\x18"); } catch { /* Port weg */ }
    }
}
