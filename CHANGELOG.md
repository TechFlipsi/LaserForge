# Änderungen

## Unreleased (04.11.2026 — M7.5 API-Parität + Geräte-Dialog + Geräte-Konfig am Laser)
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
- Desktop-Referenz auf LaserForge.Devices (Ports-Scan); +8 Tests (82/82 grün am PC).
- REST: +14 Routen nach der LightBurn-2.2-API (offizielle Spec + Befunde aus der Clean-Room-Analyse 25.10./04.11.):
  `jog/settings`(POST), `position`/`events/poll` mit Nullpunkt-Arbeitsbereich, `goto/saved`(Liste), `goto/saved/add`,
  `zero/edit`, `spindle/toggle`, `spindle/speed`, `coolant/toggle`, `vacuum/toggle`, `aux/stop`,
  `override/speed`, `override/power` (GRBL-Bereich 10–200 %), `file/open` (202 + Projekt-Ersatz),
  `bind` (Zweit-Pairing, nur Loopback), `_internal/standby`, `overlay/metadata` (404 ohne Kamera),
  `material-library` (leere Bibliothek, product=lightburn).
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