# Side-by-side — Live-Fassung LightBurn 2.2.00 (Elegoo) ↔ LaserForge M5-a

> Beweis-Fassungen (nur intern): NAS `/mnt/jarvis-files/Fabian/LaserForge/QA-Beweise/`
> LightBurn = `benchmark/lightburn_0_2556550.png` (1920×1080, echt, 25.10.2026)
> LaserForge = `mein-gui/LaserForge.Desktop_1_1836060.png` (1602×932 via PrintWindow)
> Vergleichs-Verfahren: Vision-Analyse mit konkreten Fragenlisten je Region; kein Asset/Text kopiert.

| # | Region (2.2-Doku A–U) | LightBurn 2.2.00 (Live-Fassung) | LaserForge M5-a | Stand |
|---|---|---|---|---|
| 1 | Titel | „&lt;Projekt&gt; - &lt;Geraet&gt; - LightBurn Core 2.2.00“ | „Unbenannt - GRBL - LaserForge“ | ✓ |
| 2 | Menüs (B) | Datei·Bearbeiten·Anordnen·Laserwerkzeuge·Web-Werkzeuge·Fenster·Sprache·Hilfe | identisch (8 Einträge, eigne deutsche Labels) | ✓ |
| 3 | Haupt-Leistenzone | 2 Reihen: Projekt-Aktionen/Werkzeuge, Einheiten-Dropdowns (mm) | 2 Reihen + mm-Dropdowns | ✓ |
| 4 | Eigenschaften-Leiste (E) | X/Y-Pos, Breite/Höhe (+%), Drehen, Ausrichten-Dropdowns | XPos/YPos/Breite/Höhe/Drehen/Feldzeile (live-Bindung folgt M5-b) | ◐ |
| 5 | Creation-Spalte (I) | Auswahl→Linien→Formen→Knoten→Trimmen→Laschen→Text→Laser-Positionieren→Messen | 13 eigne Icons, Auswahl aktiv-markiert | ✓ |
| 6 | Modifiers-Spalte (J) | Versatz/Boolesch/Grid/Kreis-Array/Startpunkt/Verrundung | unterer Säulenabschnitt (M5-b: echte Werkzeuge) | ◐ |
| 7 | Arbeitsfläche (A) | HELL; Lineal 0..400 (+40er-Schritte), 5/10-mm-Raster, Rahmen 400×400, Ursprung unten links | hell; Lineal/Raster/Frame/Grün-Pfeil-Ursprung | ✓ |
| 8 | Cuts/Layers-Tab (K) | Layerzeilen mit Farbchip/Modus/Spd-Pwr/Output/Show/Air | Tab-Chrome ✓; Zeilenliste folgt M5-b | ◐ |
| 9 | Move-Tab (L) | Jog-Kreis + Schritte (10 mm @6000) | Jog-Grid im Rechts-Block ✓ → Tab-Wanderung M5-b | ◐ |
| 10 | Konsole-Tab (M) | Log + Freitext-Send (rot solange ohne Verbindung) | rot „Auf Verbindung warten …“ ✓ + Log | ✓ |
| 11 | Macros-Tab (N) | Makroknöpfe | Tab-Chrome + Deaktiv-Hinweis | ◐ |
| 12 | Kamera/Overlays (W) | Camera Control | Tab-Chrome + Deaktiv-Hinweis (Kamera-Support „später“) | ◐ |
| 13 | Seiten/Pages (U) | Multiseiten (2.2-Feature) | Tab-Chrome (Backlog) | ◐ |
| 14 | Laser-Fenster (P) | COM-Dropdown (COM3)/Hersteller (GRBL), Start/Stopp/Pause, 2× Rahmen, G-code speichern/ausführen, „Starten von: …“, 9-Punkte-Job-O-Ursprung-Grid, 2 Checkboxen | vollständige Zone inkl. orange 9-Punkte-Grid | ✓ |
| 15 | Farbpalette (R) | 29 Layer-Farben + T1/T2 = 31 Chips | 00–30 + T1/T2 eigne Palette | ✓ |
| 16 | Statusleiste (T) | Mausmm/Werkzeug/Auswahl/Meldung/Modus | X/Y-mm + Bereit + Auswahlanzeige | ◐ |

## Functional depth (nach Checkliste Phasen)
- Belegt grün: IO-Roundtrips, Goldene GCode-Programme, GRBL-Statuslive (COM4), REST/MCP/CLI-Fluss — je Stand-PC-Tests (60/60).
- M5-b (läuft): Canvas-Zeichnen/Auswahl/Verschieben, Datei-Öffnen/Speichern in der GUI, Preview-Tab mit dem echten Core-Simulator, Konsole-Deaktiv-Logik.
- Danach: M6 Cut-Settings-Dialog/Material, M7-Recht/Events-Vertiefung, M8 Regression-Gate+Release.

## Rechteinordnung (Clean-Room-Nachweis)
- Kein fremder Code, keine fremden Assets, keine fremden Texte: eigener Code, eigene Icons (PIL), eigene deutsche Wortlaute.
- Genutzte offene Normen/Schnittstellen: XML-Format-Tagnamen der .lbrn-Familie (Datei-Interoperabilität), GCode/GRBL-Standard, das offizielle REST-Protokoll der fremden Suite (Doku + geprüfter Client).
- Beweis für Protokoll-Interoperabilität: der am ECHTEN System getestete Client spricht die eigene API unverändert.