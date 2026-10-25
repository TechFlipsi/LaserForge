# Design-Ziel-Bild — LightBurn-2.2-Nahe UI (M5-Messlatte)

> Quelle: LIVE-Aufnahme vom echten LightBurn Core 2.2.00 (Thema „Elegoo") am Stand-PC, 25.10.2026,
> 1920×1080 (`docs/qa/benchmark/lightburn_0_2556550.png`). Eigene Beschreibung — kein Asset/Text kopiert.
> Alle Masse in Prozent des Hauptfensters (1920×1080).

## Farbwelt (eigene Token)
| Token | Wert (ca. Hex) | Einsatz |
|---|---|---|
| FG_FENSTER | #222222 | Fensterrahmen/Chrom |
| FG_DUNKEL | #111111 | Titelleiste, Panels |
| FG_PANEL | #2B2B2B | Panel-Hintergründe |
| FG_FELD | #333333 | Buttons/Felder |
| AKZENT_BAND | Rot-Gradient #7A1B1B → #C0392B | Konsole-Band, Laser-Band |
| BAND_TEXT | #FF6B6B | „Auf Verbindung warten ..."-Text (eigener Wortlaut!) |
| ARBEITSFLAECHE | #FFFFFF | helle Zeichnfläche |
| RASTER_FEIN | #E8E8E8 | feines 5-mm-Raster |
| RASTER_GROSS | #CFCFCF | gröberes Raster |
| LINEAL | #FAFAFA mit schwarzen Ziffern | Lineal oben/links |
| TXT_HELL | #D8D8D8 | UI-Text |
| AUSWAHL | #FF9900 | aktive Auswahl-Werkzeugmarkierung |

## Regionen (Doku-Regions A–U, eigener Stand)
- **Titelleiste** (100%×~2.5%): „&lt;Projekt&gt; - &lt;Geraet&gt; - LaserForge".
- **Menüleiste** (~2.5%): Datei · Bearbeiten · Ansicht | Laserwerkzeuge · Web-Werkzeuge | Fenster · Sprache · Hilfe  (eigene deutsche Bezeichnungen).
- **Leisten-Zone** (3 Reihen ~ y 3–11%):
  - R1 Haupt-Symbolleiste: Neu/Öffnen/Import/Speichern/Umdrehen-Aktionen/Importe/Kamera/Ausrichten/Gruppieren/Heben/Berühren-Operationen + Einheiten-Dropdowns (mm) + 2 3×3-Punkte-Grids (Job-Ausgangsposition) ganz rechts.
  - R2 Eigenschaften-Leiste: XPos/YPos (0.000, mm), Breite/Höhe + %-Umschalt, „Drehen 0.00°", Helfer-Spalten (H Space, V Space), Ausrichten (Mitte/Normal-Dropdowns), rechts 3 Icon-Slots.
  - R3 (optional einblendbar): Text-Optionen (Schriftart, Höhe, Abstände).
- **Linke Werkzeugspalte** (x 0.5–2.5%, y 12–62%): Auswahl (aktiv, hell markiert) · Knotenbearbeitung · Hand · Rechteck · Ellipse · Polygon · Linie · Text · Bild · Messen · Trimming · Laschen · Vertexten (eigene Icons, dunkel-on-flach-aktiv).
- **Arbeitsfläche** (x ~7–66%, y ~12–94%): HELLER Canvas; Lineal oben (0..400 in 40er-Schritten), Lineal links (400..0 abwärts), feines Raster (5 mm) + grobes (10 mm), Arbeitsbereich-Rahmen 400×400 mm hellgrau, Ursprung unten-links mit grünem Startpfeil.
- **Rechts-Panel** (x ~66–100%):
  - Kopfzeile „Konsole" + rotes-Band-Abschluss der Zeile.
  - Konsole (dunkel, ca. 55% Höhe des Panels): erste Zeile rot „Auf Verbindung warten ..." (eigener Text; nach Verbindungsaufbau grün „Verbunden" — eigenes Wortlaut).
  - Eingabefeld „Befehl eingeben…" + rechts „Alle anzeigen ⌄"-Zeile.
  - **6 Tabs**: `Schnitte/Ebenen` `Bewegen` `Konsole` `Makros` `Kamera/Overlays` `Seiten` — aktiver Tab hell auf dunklem Feld.
  - „Laser"-Band (rot) + Zustand „Nicht verbunden" (hell), nach Verbindung: „Verbunden mit COM3 @ 115200".
  - 3 Große Buttons: `Pause` `Stopp` `Start` (hellgrau, Symbol+Text).
  - Button-Reihe: `Rahmen` `Rahmen (Auswahl)` `G-code speichern` `G-code ausführen` + rechts „Starten von: Aktuelle Position ⌄".
  - Zeile: `Referenzbild` `Zur Ausgangsposition gehen` + **9-Punkte-Grid** + Label „Job-Ausgangsposition".
  - 2 Checkboxen: `Nur ausgewählte Grafiken senden` / `Ausgangsposition der Auswahl verwenden`.
  - 2 Knöpfe: `Letzte Position anzeigen` / `Optimierungseinstellungen`.
  - Zeile: `Gerät:` `COM3 ⌄` + `Elegoo ⌄` (Hersteller-/Profil-Dropdown).
- **Layer-Leiste** (unten, unter der Arbeitsfläche, ~2.2%): Farbchips 00..30 (00 schwarz, 01 rot, 02 grün, 03 blau? — eigene Palette-Tabelle unten) + `T1` `T2` rechts.
- **Statusleiste** (~1.5%): `Bewegen` `Größe` `Position` `Abbrechanzeige?` — 5 Segmente: Maus-X/Y, Werkzeug-Status, Auswahl-Info (n Objekte), Meldung, Modus.

## Layer-Palette (eigene Festlegung — 31 Chips)
00 #000000 · 01 #FF0000 · 02 #00C000 · 03 #0060FF · 04 #FF80FF · 05 #FF8000 · 06 #00C0FF ·
07 #FFFF00 · 08 #800060 · 09 #802000 · 10 #404040 · 11 #FFC000 · 12 #6080FF · 13 #C0C0C0 ·
14 #FF0080 · 15 #008060 · 16 #806000 · 17 #006080 · 18 #FF6020 · 19 #808080 · 20 #C00000 ·
21 #0080FF · 22 #8000FF · 23 #00A0A0 · 24 #A06000 · 25 #006000 · 26 #800000 · 27 #0040A0 ·
28 #A000A0 · 29 #404080 · 30 #206040 · T1 #202020 · T2 #606060

## Verifikations-Regeln (automatisierte GUI-Prüfungen)
1. Nach jedem GUI-Bauzyklus: Screenshot (tools/qa/capture_windows.ps1) → Pixel-/Regionen-Vergleich gegen dieses Dokument (Vision-Analyse mit konkreten Kernfragen).
2. MUST-Prüfliste: 6 Tabs vorhanden + Reihenfolge; rotes Konsole-Band sichtbar bis Verbindung; Laser-Panel COM-/Profil-Dropdown; 9-Punkte-Grid; Layer-Leiste 00–30 + T1/T2; helle Fläche im dunklen Fenster; Statusleiste vorhanden.
3. Berichte in docs/qa/ (deutsch, je Zyklus eine Datei).
