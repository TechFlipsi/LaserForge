# Open-Source-Laser-Apps — funktionale Referenz für LaserForge (Track B)

**Stand:** 25.10.2026 · **Zweck:** Bestehende freie Laser-Apps überschauen und funktional referenzieren — als Architektur-/UX- und Protokoll-Lernquelle. **Kein Code, kein Text davon wird übernommen** (Clean-Room-Ziel; Lizenz-Hinweise unten).

## Quellen (abgerufen am 25.10.2026)

- MeerK40t: `github.com/meerk40t/meerk40t` (README, Repository-Baum), Lizenzdatei (MIT)
- LaserGRBL: `github.com/arkypita/LaserGRBL` (README, LICENSE.md), Website `lasergrbl.com` (User-Interface-Seite)
- Rayforge: `rayforge.org`, Repository `github.com/barebaric/rayforge` (MIT)
- LaserWeb4: `github.com/LaserWeb/LaserWeb4` (README, AGPL-3.0), Doku-Seite `laserweb.yurl.ch`
- bCNC: `github.com/vlachoudis/bCNC` (README; Lizenz-Badge „GPL-2.0“ laut Repo-Seite)

---

## 1. MeerK40t

- **Was es ist:** vollständig frei (MIT), in Python gebaut, plattformübergreifend (Windows/macOS/Linux, auch Raspberry Pi); Selbstbezeichnung: von Grund auf eigene Laser-Cut-Software, primär für K40-artige Maschinen gedacht, inzwischen breit.
- **Status:** Version 0.9.9000 (Januar 2026) gilt als aktuelle Empfehlung; Projekt ist laut README in einem **Wartungs-Modus** (weniger aktive Weiterentwicklung, wöchentliche experimentelle Builds).
- **Geräte-/Treiberumfang (aus Repository-Baum belegt):** eigenes GRBL-Modul (driver/controller/emulator, inkl. ESP3D/WLAN-Notiz), Lihuiyu/M2nano (K40), Moshi, **Ruida** (driver/controller/**emulator**), Balor (Galvo/JCZ) u. a. Je Gerät: eigener Treiber- + GUI-Unterordner.
- **LightBurn-Kompatibilität:** Ja — es gibt ein eigenständiges **`.lbrn`-Ladermodul** (`meerk40t/extra/lbrn.py`, mit eigenem Parser + geometrischer Innen-Darstellung; `.lbrn2` taucht im Code auf). Die Format-Fähigkeit ist also **eigenständig nachgebaut**, nicht aus LightBurn kopiert. Export-Anteil nicht verifiziert (nur Ladepfad in dieser Recherche gesichert).
- **Besonderheit als Referenz:** der **Ruida-Emulator** — die App kann sich als Ruida-Controller ausgeben („ruidacontrol“-Muster), damit fremde Software (z. B. RDWorks) mit der App läuft. Für LaserForge ein interessantes Architektur-Muster (Virtuelles-Gerät-Testen), aber nicht nötig für unser Elegoo/GRBL-Start.
- **Lernbausteine für uns:** Treiber-Ordnerstruktur je Geräteart; `.lbrn`-Ladepfad (parallel zu unserer eigenen Format-Spezifikation); Emulator-Konzept für Treibertests ohne Hardware.

## 2. LaserGRBL

- **Was es ist:** Windows-Only, .NET/C#-Desktop-App (aus dem Repository), **GPL** (LICENSE.md mit GPL-Hinweis und Copyright 2016 Diego Settimi); die populärste freie Bedienoberfläche für **GRBL-Diodenlaser**.
- **Geräte-/Firmware-Umfang:** GRBL-Firmware (v0.9 **und** v1.1) über serielle Verbindung; WLAN-Zubehör via ESP8266-/WebSocket-Anbindung. Setzt für volle Leistung **GCode-`S`-Leistungsmusterung** voraus.
- **Funktionsumfang (eigene Zusammenfassung der README-Liste):** Laden von GCode mit Lauf-/Schneidvoransicht (inkl. Graustufen-/Alphablend-Darstellung); Bildimporte mit zeilenbasiertem Raster (horizontal/vertikal/diagonal), mit Vektorisierung oder 1-Bit-Dithering; SVG-Import als experimentell; Farbschemata passend zu Schutzbrillen; eigene Knöpfe (Nutzer-Makroknöpfe); GRBL-Konfiguration Import/Export; **Alarm-/Error-/Einstell-Decoder mit Klartext-Tooltips**; Homing/Halt/Resume/Reset-Knöpfe; Job-Zeitorakel; Jogging; **Feed-Overrides** für GRBL ≥1.1.
- **UI-Bausteine (aus der Bedienoberfläche-Seite der Website):** klassischer Kopf-/Wertlayout: Verbindungsknopf + Geräteauswahl, Jog-/Override-Schalter, GCode-/Log-Datenstrom, eigene Knöpfe. Genau diese Bausteine (Jog-Panels mit Distance/Speed, Override-Regler, Console/Log, Alarmkatalog) sind die **nächstliegende Referenz** für unser GRBL-Panel.
- **Lernbausteine für uns:** UI-Satz für GRBL-Diodenlaser; Alarm-/Error-Mapping; Bild-Raster-Pfade (Zeilen-/Sprungstruktur); Override-Handhabung; WLAN-Variante (ESP8266) als zukünftiger Port-Ausweg.

## 3. Rayforge

- **Was es ist:** moderne, frei (MIT, `github.com/barebaric/rayforge`) gestaltete Laser-Cut-Suite mit eigener Website-Positionierung als **freie Alternative zu LightBurn**; Zielmaschinen: **GRBL-artige** Geräte.
- **Funktionsspektrum (Website, eigene Zusammenfassung):** eingebauter **2D-CAD-Zeichner** (geometrisch gezeichnete Formen), mehrschichtige Jobs, Kamera-Ausrichtung, Rotarisch-Unterstützung, Material-Rezepte (wiederverwendbare Einstellpakete), Pfad-Optimierung, 3D-Vorschau/Simulation, „KI“-Werkstück-Generator (Text → Formenvorschlag).
- **Status:** aktiv in Entwicklung (junges, wachsendes Projekt; mehrere aktive Add-on-Repositories für Maschinenzusätze, z. B. Epilog-/Lasersaur-Ausgaben — MIT).
- **Lernbausteine für uns:** Material-„Rezepte“ als UX-Konzept; Pfad-Optimierung/Kamera-Ausrichtung; moderne Oberflächen-Muster ohne LightBurn-Kopie. Für das Zielbild „LightBurn-ähnlich“: eher **UX-Ideenquelle** als UI-Kopie.

## 4. LaserWeb4

- **Was es ist:** Browser-/Webbasiertes CAM-Programm für Laser/CNC/Plasma/Verschieber (Node + Browser; Doku-Seite laserweb.yurl.ch); Lizenz **AGPL-3.0**; laut Repository „gemeinschaftlich gebratene“ Entwicklung, Wartung weitgehend eingeschlaffen (README verweist auf alten Entwicklungsstand — keine frische Aktivität).
- **Funktionsspektrum:** CAM von SVG/DXF zu GCode, Maschinenprofile (u. a. GRBL/Smoothie/Marlin), Arbeitsflächen-Voransicht, mehrschichtige Operations-Verwaltung.
- **Lernbausteine für uns:** das *Operations/Job*-Datenmodell (Operations → Maschinenprofile) als Konzept; sonst wegen Ruhephase und AGPL-Charakter **kein** Übernahme-Pfad.

## 5. bCNC

- **Was es ist:** Python-GUI (plattformübergreifend) als **GRBL(HAL)-GCode-Absender**: Autoleveler (Fräsen), GCode-Editor, Digitizer, CAM-Teilfunktionen; laut Repo-Badge **GPL-2.0**.
- **Relevanz für uns:** nicht laser-spezifisch, aber bewährtes Muster für **GRBL-Streaming/-Status-Handling** (Robustheit auf schwacher Hardware wie Raspberry Pi dokumentiert). Nur Konzeptreferenz (GPL-2.0/AGPL-Fach — keine Codeübernahme für unser GPL-3.0-only-Projekt).

## 6. Einordnung für LaserForge

| App | Lizenz | Plattform | Geräte | Status | LightBurn-Dateien |
|---|---|---|---|---|---|
| MeerK40t | MIT | Win/Mac/Linux/Pi | Lihuiyu, GRBL, Ruida, Moshi, Balor u. a. | Wartungs-Modus (0.9.9000, Jan 2026) | `.lbrn`-Ladepfad belegt (eigenständig) |
| LaserGRBL | GPL | Windows | GRBL v0.9/v1.1 | aktiv (Diodenlaser-Fokus) | nein |
| Rayforge | MIT | Win/Mac/Linux | GRBL-artig | aktiv/jung | nein (eigene Formate) |
| LaserWeb4 | AGPL-3.0 | Browser/Electron | GRBL/Smoothie/Marlin | weitgehend eingeschlaffen | nein |
| bCNC | GPL-2.0 | Win/Mac/Linux | GRBL/GRBLHAL | aktiv (CNC-Fokus) | nein |

**Wichtigste Erkenntnis:** Keine der freien Apps deckt LichtLaser 2.2's Funktionsspektrum/UX vollständig ab und **keine** kann `.lbrn`/`.lbrn2` nachweislich in beide Richtungen gleichwertig verarbeiten — unsere LightBurn-Format-Spezifikation (Track A) ist damit ein echtes Unterscheidungsmerkmal. MeerK40t ist der einzige belegbare Nachweis, dass das `.lbrn`-Format **ohne LightBurn-Code** nachgebaut werden kann (MIT, eigenständiger Parser) — genau unsere Vorgehensweise.

## 7. Lizenz-Hinweis (für unser GPL-3.0-only-Projekt)

- **MIT (MeerK40t, Rayforge):** kombinierbar mit GPL-3.0; Übernahme einzelner Bausteine technisch möglich — laut Projektregel trotzdem erstmal nur **funktionale Referenz**, keine Codeübernahme (Clean-Room).
- **GPL-3.0 (LaserGRBL):** gleichlautende Lizenz, formal kombinierbar — trotzdem Clean-Room-Regel: nur Architektur-/UX-Referenz, kein Quellcode.
- **AGPL-3.0 (LaserWeb4) / GPL-2.0 (bCNC):** für GPL-3.0-only problematisch/nicht verträglicher — ausschließlich als Konzeptstudie.

---
*Nur funktionale Recherch-Referenz — keine Übernahme von Code, Formulierungen oder Assets der genannten Projekte.*