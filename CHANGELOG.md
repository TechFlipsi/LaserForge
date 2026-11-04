# Änderungen

## v0.2.0 — 04.11.2026 (API-Parität + Geräte-Dialog + Geräte-Konfig am Laser + Job-Engine)
- **Geraet-Modell + Persistenz** (Core): geraete.json je Benutzer; **LightBurn-Import**:
  prefs.ini (JSON) DeviceList → Anschluss/Baud/Workspace/Jog (mm/s→mm/min ×60) /Spiegel/StartHome;
  Secret-/RestAPI-Felder bewusst NICHT übernommen.
- **Geräte-Dialog (GUI)**: Liste (Name + „Protokoll | Sprache" + Standard-Markierung), 8 Buttons:
  Laser suchen (Ports-Liste), Manuell erstellen, LightBurn Bridge (bewusst deaktiviert —
  Transport-TCP folgt), Importieren (LightBurn-prefs.ini-DeviceList), Als Standard setzen,
  Bearbeiten, Entfernen, Exportieren (JSON); OK/Abbrechen; QA-Hilfslinie qa-geraete-dialog.txt.
- **Geräte-Konfiguration direkt am Laser (GRBL)**: neuer Dialog (Parität zu LightBurns
  „Machine Settings → GRBL Configuration") — `$$` lesen, Werte bearbeiten, geänderte Werte
  per `$x=value` zum Gerät schreiben (EEPROM, Injektions-Schranke, error-Zeile durchgereicht);
  Menü „Laserwerkzeuge → Geräte-Konfiguration (am Laser) …", QA-Hook `--konfig-dialog
  --konfig-port=COMx` + Status-Datei; Fehler-Route bewiesen (belegter Port → saubere Meldung).
- MainWindow: Geräte-Zeile = Laser-Auswahl (Auswahl wechselt das aktive Gerät inkl. Persistenz);
  Menü „Geräte …" füllt die Zeile nach OK.
- **Maschinenrouten M8 — echte Geräte-Ansteuerung**: jog/jog/stop/home/zero/goto/goto-saved-go
  senden jetzt an den Laser (MaschinenSitzung → aktives Gerät, serieller Transport; Position
  aus echtem GRBL-Status in position/events-poll). Fehler-Ebene ehrlich: port_busy (LB hält
  den Port) / port_error / not_connected / no_device. End-to-End-Tests bis zur Sendezeile ($J=/G0/$H/roh-0x85).
- **Job-Engine M9 — echte GCode-Fahrt über die API**: job/start erzeugt GRBL-GCode aus dem
  Projekt (Kern-Pipeline, Golden-Spec) und fährt ihn zeilenweise mit ok-Sync; Pause = Realtime
  Feed-Hold / Resume (Toggle), Stop = Abort 0x18; Sicherheits-Gate: Laser-Modus $32=1 Pflicht
  (sonst job_rejected + Brandgefahr-Meldung), bei error: SENDEN STOPPEN; Fortschritt live im poll.
- Test-Läufe xunit-serial (Geraete-/Material-Dateien parallel-fest); 93/93 grün am PC.
- **Material-Bibliothek mit Inhalt**: /api/material-library liefert echte Einträge aus
  material-library.json je Benutzer (Protokoll-Shape incl. cut index=-1/layer_index=-1).
- **Theme-Ebene (hell/dunkel)**: ThemeFarben (dunkel = gemessen; hell = Vorlaul-Set, Messung
  folgt), --theme=hell|dunkel Startflag, Geräte-/Konfig-Fenster nutzen die Paletten (MainWindow-Farben folgen in Runde 3).
- **Icon-Set P1 (12 Glyphen)**: eigene Clean-Room-PNGs (assets/icons/lf_*.png 128px +
  Kontaktbogen-Pass), tools/bauer_icons.py erweiterbar auf den vollen 40er-P1-Katalog;
  GUI-Anbindung nächste Runde mit Sirs Look-Abnahme.
- Desktop-Referenz auf LaserForge.Devices (Ports-Scan); +8 Tests (82/82 grün am PC).
- REST: +14 Routen nach der LightBurn-2.2-API (offizielle Spec + Befunde aus der Clean-Room-Analyse 25.10./04.11.):
  `jog/settings`(POST), `position`/`events/poll` mit Nullpunkt-Arbeitsbereich, `goto/saved`(Liste), `goto/saved/add`,
  `zero/edit`, `spindle/toggle`, `spindle/speed`, `coolant/toggle`, `vacuum/toggle`, `aux/stop`,
  `override/speed`, `override/power` (GRBL-Bereich 10–200 %), `file/open` (202 + Projekt-Ersatz),
  `bind` (Zweit-Pairing, nur Loopback), `_internal/standby`, `overlay/metadata` (404 ohne Kamera),
  `material-library` (Bibliothek-Inhalt, product=lightburn).
- Maschinennahe Routen (jog/home/zero/goto/job) mit ehrlichem Gate-Körper
  (`not_connected` / `device_integration_pending`) bis zur Devices-Integration.
- `events/poll` = kompletter PollSnapshot (position/job/overrides/aux/connection/settings) als SSE-Fallback.
- 14 neue Parität-Tests (74/74 grün, Stand-PC-Bau).

## v0.1 — 25.10.2026 (Erstveröffentlichung)
- Lösungsskelett: 6 Projekte (Core/Devices/Api/Cli/Desktop + Tests), .NET 10, GPL-3.0-only
- Kernbibliothek: Geometrie (Punkt/Zug/Transformationsmatrix a b c d e f), Formen (Rechteck/Ellipse/Polygon/Pfad mit Bezier-Knotendaten/Bild/Gruppe)
- Formate: .lbrn/.lbrn2-Lesen (legacy + kompakte Tokens) + Schreiben (legacy), verlustfreie Erhaltung, Thumbnail mit eigenem PNG-Enkoder (Deflate/CRC32/Adler32), SVG-Leser, DXF-Leser
- Pipeline: Innen-erst-Optimierung (Schachteltiefe), Nächste-Nachbar-Kette, By-Layer/By-Priorität, kollineare Vereinfachung; GRBL-Flavor mit Goldener Spec; Bild-Pixelraster; Preview-Simulator mit Fahrtzeit-Abschätzung
- Geräte: serieller Transport (CH340/CDC + Echtzeit-Rohermittlung), GRBL-Verbindung (Begrüssung/Status/Jog/Fahrt/Home/Rahmen), \$\$-Einstellungen + 32-Lasermodus-Test + ALARM-Register + Baud-Auto + COM-Scan
- Automatisierung: QA-REST (Port 19540, Pairing nur Loopback, HMAC-Minuten-Bearer, 10 Endpunkte), CLI (info/status/konvert/mcp), MCP-stdio (2024-11-05)
- CI: GitHub-Actions-Workflow (Windows-Bau + Tests + Release-Anhang)
- App-Icon (eigenes Design); deutschsprachige Betriebsanleitung + Funktionsabbild-Tabelle