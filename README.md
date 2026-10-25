# LaserForge v0.1

[![Build](https://img.shields.io/github/actions/workflow/status/TechFlipsi/LaserForge/build.yml?branch=main&label=Build)](https://github.com/TechFlipsi/LaserForge/actions)
[![Tests](https://img.shields.io/badge/Tests-60%2F60%20gr%C3%BCn-brightgreen)](https://github.com/TechFlipsi/LaserForge/actions)
[![License](https://img.shields.io/github/license/TechFlipsi/LaserForge?label=Lizenz)](LICENSE)

**Freie, saubere Steuerung für Laser-Cutter und Graveur** – nativ in C#/.NET 10 (Avalonia) für Windows.

> ⚡ **LaserForge** liest und schreibt .lbrn/.lbrn2-Projekte (saubere, unabhängige Umsetzung, interoperabel), steuert GRBL-Geräte über die serielle Schnittstelle (COM-Scan, Status, Jogging, \$\$-Konfiguration) und erzeugt deterministische GCode-Programme.
> 🔍 **Looking for an open-source laser cutter controller?** LaserForge opens/creates .lbrn/.lbrn2 project files (independent, interoperable implementation), drives GRBL machines over serial and generates deterministic GCode.

## ⬇️ Download

**[Download für Windows](https://github.com/TechFlipsi/LaserForge/releases/latest)** — Release-zip mit Desktop-App + CLI

Alle Versionen: [Releases](https://github.com/TechFlipsi/LaserForge/releases)

📖 **[Betriebsanleitung / Manual](MANUAL.md)** – Installation, Erststart, Gerät verbinden, Sicherheit (Deutsch)

📝 **[Änderungen](CHANGELOG.md)** · 👏 **[Danksagungen](CREDITS.md)** · 📊 **[Funktionsabbild](docs/parity-checklist.md)**

Geschrieben in **C# / .NET 10 (Avalonia 12)** – Kernbibliothek ganz ohne Fremdpakete, Lizenz GPL-3.0-only.

## Features
- 🗂️ **.lbrn/.lbrn2 kompatibel** – lesen und schreiben (legacy + kompakte Token-Form), verlustfreie Erhaltung (VariableText, Notizen, UI-Einstellungen, unbekannte Felder)
- 📐 **Geometrie-Kern** – Rechteck/Ellipse/Polygon/Pfad (mit Bezier-Knotendaten)/Bild/Gruppe; XForm-Matrix (a b c d e f) korrekt angewendet
- 📥 **Importe** – SVG (M/L/H/V/Z/C/Q/S/T), DXF (LINE/CIRCLE/ARC/LWPOLYLINE/POLYLINE/ELLIPSE), Pixelbilder (Scan-Gravur mit Segmenten)
- 🎛️ **Schnittzeilen (Layer)** – Power, Tempo (intern mm/s ↔ GCode mm/min), Passe-Wiederholung, Modus (Schnitt/Scan/Füll/Linie/Ritz), Linienabstand, Scan-Winkel, Kerf, Overcut… verlustfrei
- 🧠 **Pfadoptimierung** – Innen-erst (Schachteltiefe), Nächste-Nachbar-Kette, By-Layer/By-Priorität, kollineare Pfadvereinfachung
- 🖨️ **GCode (GRBL 1.1-Flavor)** – Modalblock (G21/G90/M4/S0), Kontursätze, bidi-Scan-Raster, Rahmen-Fahrt (5 % @ 600 mm/min), M5 + Parken; Koordinaten 3 Nachkommastellen, S = % × \$30
- 🔌 **Geräteverbindung** – COM-Scan, Baud-Auto-Erkennung (115200/9600/57600/250000/250), Willkommensstring-Version, Statusauslesung „?“ (Idle/Run/Alarm…), Jogging \$J=, Rahmenprogramm, \$\$-Einstellungen, \$H-Homingoption, ALARM-Erkennung
- 🤖 **Automatisierung** – QA-REST (eigenes Protokoll auf 19540: Pairing nur Loopback, HMAC-Minuten-Bearer; status/position/units/layers/cuts/project/upload/overlay/events), CLI (info/status/konvert/mcp), MCP-stdio-Server
- 🖥️ **Desktop-GUI (Avalonia 12)** – 2.2-nahes Hauptfenster in eigener Umsetzung: dunkles Chrom, helle Arbeitsfläche (Lineale/Raster/400×400-Umrahmung/Grün-Pfeil-Ursprung), 6 Rechts-Tabs (Schnitte/Ebenen·Bewegen·Konsole·Makros·Kamera/Overlays·Seiten), rotes Konsole-Band „Auf Verbindung warten …", Laser-Panel mit COM-/Profil-Dropdown, 9-Punkte-Job-Ausgangsposition, Layer-Leiste 00–30 + T1/T2, Statusleiste

## Projektphase (M1–M8)

| Phase | Inhalt | Stand |
|---|---|---|
| M1 | Geometrie/XForm, Formen-Modell | ✅ (Tests grün) |
| M2 | .lbrn/.lbrn2-IO, SVG/DXF/Bild-Import, Thumbnail, Verlustfreiheit | ✅ |
| M3 | Optimierung + Goldene GCode | ✅ |
| M4 | GRBL-Live-Grundfunktionen | ✅ (Verbindungs-Beweis am Gerät) |
| M5 | Hauptfenster-GUI (2.2-nah) | ◐ (Layout ✅ abgenommen, Funktionswelle läuft) |
| M6 | Preview + Dialoge | ◐ (Kern-Simulator ✅) |
| M7 | QA-REST + CLI + MCP | ✅ (Basis live bewiesen) |
| M8 | Release-Pipeline | ◐ (Workflow + Skripte liegen bereit) |

## Bauen (Entwickler)

    dotnet restore LaserForge.sln
    dotnet build LaserForge.sln
    dotnet test tests/LaserForge.Tests

Schichtenplan: [docs/ARCHITEKTUR.md](docs/ARCHITEKTUR.md) · Messwerte-Vergleich: [docs/side-by-side.md](docs/side-by-side.md)

## ⚠️ Sicherheit
Lasern ist gefährlich. Die Software sendet **nur** vom Benutzer ausgelöste Befehle; Erst-Inbetriebnahme ausschließlich mit geschlossener Gehäusehaube oder Aufsicht, Feuerlöscher bereit, Material-Kennwerte vor dem Vollbetrieb per Rahmen + Testprobe prüfen.

## ⚖️ Saubere Quelle (Clean-Room)
Eigenes Programm, eigene Grafik, eigene Texte; bestehende Software wird nicht kopiert. Genutzt werden offene Interoperabilitäts-Zusammenhänge: Format-Tagnamen der .lbrn-Familie, der GRBL-Standard, das offizielle REST-Protokoll der fremden Suite (Doku + eigener stdlib-Client). Lizenz: [GPL-3.0-only](LICENSE).

<!-- SEO: lightburn alternative open source, lbrn2 lesen, laser cutter controller windows, grbl laser software deutsch, laser engraver open source, gcode generator diode laser -->