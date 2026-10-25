# LaserForge — Betriebsanleitung (Deutsch)

## 1. Installation
1. **Voraussetzung:** Windows 10/11; .NET-10-Laufzeit (falls als Framework-Abhängigkeit verteilt) oder die mitgelieferte Fassung.
2. Release-zip von [Releases](https://github.com/TechFlipsi/LaserForge/releases/latest) entpacken (Beliebiges Verzeichnis, z. B. `C:\Tools\LaserForge`).
3. Bestandteile:
   - `LaserForgeGui.exe` — Desktop-App (Avalonia)
   - `laserforge-cli.exe` — Headless-Werkzeug

## 2. Erststart der App
1. Doppelklick `LaserForgeGui.exe` — das Hauptfenster öffnet sich (dunkles Chrom, helle Arbeitsfläche).
2. Rechts unten im „Laser"-Panel: Gerät auswählen (COM-Dropdown) + Profil (z. B. GRBL), dann **Verbinden**.
   - Das rote Konsole-Band zeigt solange „Auf Verbindung warten …"; nach Erfolg: Verbunden-Meldung + Live-Position.
   - Kommt „Port belegt (Zugriff verweigert)": eine andere Software hält den Port — diese schließen und erneut verbinden.

## 3. Gerät verbinden (Grundfunktionen)
- **COM-Scan:** Die App listet die vorhandenen seriellen Ports automatisch.
- **Baud-Auto-Erkennung:** Es werden 115200 / 9600 / 57600 / 250000 / 250 über den Willkommenstring erkannt.
- **Homing bei Start:** Ist im Profil eingestellt, fährt das Gerät beim Verbinden mit `\$H` die Referenzpunkte an (bis 60 s warten — bitte nicht unterbrechen).
- **Alarmzustand:** Meldet das Gerät `ALARM:n`, erscheint ein Banner; Auflösung über Homing (oder Notaus mit Vorsicht).

## 4. Dateien
- **Öffnen:** `.lbrn` / `.lbrn2` (sowohl legacy als auch kompakte Form) — unbekannte Felder werden verlustfrei erhalten (Roundtrip-sicher).
- **Import:** SVG-Dateien (Pfad-Befehle) und DXF-Dateien (Linien/Kreise/Bögen/Polylinien/Ellipsen) kommen als Formzüge auf die aktive Schnittzeile; Pixelbilder (PNG/JPG) werden als Rastergravur (Scan) eingebunden.
- **Speichern:** legacy-Format (`FormatVersion="0"`) — bestehende Suiten lesen die Datei direkt.

## 5. Sicherheit (PFLICHTLEKTÜRE)
1. Laser nie ohne Gehäusehaube/Aufsicht betreiben; reflektierende Objekte/Fernhalten.
2. Erst-Rahmenfahrt benutzen (5 % Rahmenleistung), **Augen/Hand weg** vom Strahlweg.
3. Materialproben vor Vollbetrieb (Tempo/Leistung-Matrix), Feuerlöscher (Klasse A/CO2) bereit.
4. Die Software ist keine Sicherheitsvorrichtung — Abzug bleibt beim Betreiber. Lüfter/Saugung prüfen (Diodenlaser: Brandgefahr bei S≠0 mit G0).
5. Bewegungs-/Feuer-Befehle gehen NUR vom Benutzer aus (keine Automatik ohne Aufforderung).

## 6. CLI (Headless)
    laserforge-cli info
    laserforge-cli status COM4 115200        →Verbindungstest + Status
    laserforge-cli konvert probe.lbrn out.gcode → Projekt → GCode (GRBL-Flavor)

## 7. MCP (KI-Integration)
    laserforge-cli mcp
stdio JSON-RPC 2.0: `initialize` (Protokoll 2024-11-05), `tools/list` (app_info, maschinen_status, projekt_konvertieren), `tools/call` — Beschreibungen auf Deutsch, Auslesung ohne Veraenderungen.

## 8. QA-REST (Automatisierung)
- Eigener Port (Standard 19540) — bewusst NICHT 19520 (dort läuft die fremde Suite).
- `POST /api/connect` = Pairing (nur Loopback; Consent-Dialog am Gerät).
- Authentifizierte Endpunkte mit Bearer `hex(HMAC-SHA256(secret, str(unix_time // 60)))` (Vorminute-Toleranz):
  status · position · units · layers · cuts · cuts/{index} · project · file/upload · overlay · events (SSE).

## 9. Häufige Fragen
- **„Port konnte nicht geöffnet werden“** → andere Software hält den Port (auch die fremde Suite! Nur EINE Anwendung pro Port).
- **„Keine Status-Antwort“** → Gerätekabel/Adapter prüfen; CH340-Treiber installiert? Boot-Latenz von 2-3 s einplanen.
- **Achse fährt schief** → \$\$-Einstellungen (Schritte/mm, Endschalter-Maske) gegenleuchten.

## 10. Bauen aus Quelle
    dotnet restore LaserForge.sln
    dotnet build LaserForge.sln
    dotnet test tests/LaserForge.Tests

Lizenz: GPL-3.0-only (siehe LICENSE).