# LaserForge

**LaserForge** ist eine freie, in C#/.NET (Avalonia) gebaute Steuer-Software fuer CO2-
und Dioden-Laser (Cutter/Engraver). Ziel: maximale funktionale Naehe zu etablierten
Laser-Steuersuiten — als **Clean-Room-Projekt** (eigener Code, eigene Doku, eigene
Grafiken; bestehende Software wird nicht kopiert).

## Projektstand (M-Phasen)
| Phase | Inhalt | Stand |
|-------|--------|-------|
| M1 | Skelett: Geometrie/XForm, Formen, .lbrn/.lbrn2-IO, SVG, Optimierer, GRBL-Flavor, Serial, Tests | in Arbeit |
| M2 | Voll-IO + echte Projektdateien + Live-Abgleich | geplant |
| M3 | Optimierung vertieft + Scan-Clip + Bezier-Feinbau | geplant |
| M4 | GRBL-Live (COM-Erkennung, Status, Jog, Rahmen) | geplant |
| M5 | Desktop-GUI (2.2-nahes Hauptfenster-Layout) | geplant |
| M6 | Preview + Dialoge | geplant |
| M7 | QA-API + CLI-Vollsatz + MCP | geplant |
| M8 | Assets + Doku + GitHub-Publish | geplant |

## Bauen
    dotnet restore LaserForge.sln
    dotnet build LaserForge.sln
    dotnet test tests/LaserForge.Tests

## Struktur
    src/LaserForge.Core        Geometrie, Formen, Formate, Optimierer, GCode
    src/LaserForge.Devices     Serial-Transport, GRBL-Verbindung
    src/LaserForge.Api         leichtes QA-REST (TcpListener :19530)
    src/LaserForge.Desktop     Avalonia-GUI
    src/LaserForge.Cli         Headless-Befehle
    tests/LaserForge.Tests     xUnit + Fixtures
    tools/qa                   PowerShell-QA-Harnisch

## Richtlinien
- Deutsch-Pflicht fuer Logs/Kommentare/Doku.
- Clean-Room: keine Code-/Asset-/Textuebernahmen; .lbrn-Tagnamen + GRBL/GCode =
  offene Interoperabilitaet.
- Lizenz: GPL-3.0-only.
