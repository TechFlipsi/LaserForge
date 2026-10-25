# Änderungen

## v0.1 — 25.10.2026 (Erstveröffentlichung)
- Lösungsskelett: 6 Projekte (Core/Devices/Api/Cli/Desktop + Tests), .NET 10, GPL-3.0-only
- Kernbibliothek: Geometrie (Punkt/Zug/Transformationsmatrix a b c d e f), Formen (Rechteck/Ellipse/Polygon/Pfad mit Bezier-Knotendaten/Bild/Gruppe)
- Formate: .lbrn/.lbrn2-Lesen (legacy + kompakte Tokens) + Schreiben (legacy), verlustfreie Erhaltung, Thumbnail mit eigenem PNG-Enkoder (Deflate/CRC32/Adler32), SVG-Leser, DXF-Leser
- Pipeline: Innen-erst-Optimierung (Schachteltiefe), Nächste-Nachbar-Kette, By-Layer/By-Priorität, kollineare Vereinfachung; GRBL-Flavor mit Goldener Spec; Bild-Pixelraster; Preview-Simulator mit Fahrtzeit-Abschätzung
- Geräte: serieller Transport (CH340/CDC + Echtzeit-Rohermittlung), GRBL-Verbindung (Begrüssung/Status/Jog/Fahrt/Home/Rahmen), \$\$-Einstellungen + 32-Lasermodus-Test + ALARM-Register + Baud-Auto + COM-Scan
- Automatisierung: QA-REST (Port 19540, Pairing nur Loopback, HMAC-Minuten-Bearer, 10 Endpunkte), CLI (info/status/konvert/mcp), MCP-stdio (2024-11-05)
- CI: GitHub-Actions-Workflow (Windows-Bau + Tests + Release-Anhang)
- App-Icon (eigenes Design); deutschsprachige Betriebsanleitung + Funktionsabbild-Tabelle