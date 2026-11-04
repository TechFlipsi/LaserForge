# LightBurn 2.2 — Theme & Toolbar-Redraw-Spec (04.11.2026, Screencap-Beweis)

Quelle: Vollbild-Capture Session-1 (Stand-PC, Hauptbildschirm Monitor 2), Zonen-Crops in
NAS `Fabian/LaserForge/QA-Beweise/lightburn-ui-20261104/` (zone_toolbar/palette_tabs/bedienfeld/layer_leiste.png).
Rechtslage: Redraw = eigene Glyphen nach Form (keine Pixeldatei-Kopie); Theme-Werte als abgelesene Werte (kein QSS-Datei-Import).

## 1. Theme-Farben (gemessen, dunkles Theme)
| Zone | Farbe |
|---|---|
| Fenstertitelleiste / Toolbar / Panels | #1e1e1e |
| Arbeitsfläche (Canvas mit Grid) | #0c0c0c |
| Panel-Innenfläche links | #1f1f1f |
| Bedienfeld-Buttons (Jog/Panel) | #484848 / #555555 |
| Layer-/Statusleiste (unten) | #f8f8f8 (HELL) |
| QSS-Fund (Dock-Header-Verlauf, Bedingung offen) | rgb(0,0,0) → rgb(190,50,50) → rgb(220,120,50) |
| Toggle-Semantik (QSS + Schalter-Icons) | ON = grün, OFF = rot |

Avalonia-Umsetzung: eigene Theme-Ressourcen (Brush-Keys ThemeBg=#1E1E1E, Canvas=#0C0C0C,
Panel=#1F1F1F, Button=#484848, Statusbar=#F8F8F8, OnGreen/OffRed) — Werte im Binary abgelesen, Dateien eigen.

## 2. Toolbar-Reihenfolge (vom Screencap, links→rechts)
1. Neu · Öffnen · Sichern · Import · | · Undo · Redo · | · Auswahl(Cursor) · Knoten-Edit · Zeichnen(Pen) · Zwischenablage · Löschen · | · Verschieben/Rotieren · Skala · Zoom+/−/Fit · | · Maschine · Einstellungen(gear) · Werkzeuge · | · Gruppieren · Auflösen · Ausrichten (5×) · Origin · Job-Origin · Koordinaten-Kreuz · | · Schnitt-Modus-Icons · Rotation · Druck · Laserkopf · | · Verbindung/USB · Grid/Snap · Checkbox „Als Gruppe verschieben"/„Innere Objekte sperren" · Abstand/Randpuffer-Felder
2. Property-Zeile: XPos/Breite/% · mm-Umschalter · Schriftart · Höhe/HSpace · X ausrichten · Modus

## 3. Icon-Zuordnung zum Binary-Katalog (Redraw-Quellenliste)
Screencap-Slot ↔ `:/res/icons/`-Name (246er-Katalog im kenntnisse-Dok §3):
Neu/Öffnen/Sichern → add-file/open-folder/save(-as)-128 · Undo/Redo → undo-5/redo-5-128 ·
Auswahl → cursor-128 · Knoten → add-node-128 (+del/break-node) · Pen → edit-2-128 ·
Verschieben → cursor-move_323814_256 · Skala → picture-size-width/height_128 ·
Zoom → zoom-in-2/zoom-out-2/zoom-page-128 · Maschine → desktop-2-128 · Einstellungen → settings-5-256 ·
Gruppieren → grid-three-up-128 · Ausrichten → align-{left,right,top,bottom,h-center,v-center}-128 ·
Origin → center-both-icon-128 · Schnitt-Modi → type_cut/fill/image/offset_fill.png ·
Rotation → rotate-128 · Laserkopf → laserpos-128 · Verbindung → usb-off/wireless-128 ·
Grid → tabs-128 · Druck → export-icon.png.
(Zuordnung an den Crop-Vorlagen verifizieren — Reihenfolge im 246er-Katalog ist die Namensquelle.)

## 4. Redraw-Umfang (P1 = im Screencap sichtbar)
~45 Glyphen Toolbar+Bedienfeld (28px-Gitter, 2px-Strichstärke, hell #E8E8E8 auf transparent,
Hover/Hell-Färbung über Brush-Keys). P2 = restlicher Katalog aus §3 des Kenntnis-Dokuments
(auf Anforderung je Dialog/Fenster).

## 5. Verfahren je Glyphe
1. Crop aus zone_*.png (Vorlage-Ansicht 2–4×) → 2. SVG-Entwurf (eigenes Path-Design) →
3. Export PNG 128/64/32/16 (PIL-Rendering, eigenes Skript) → 4. Side-by-Side-Gegencheck
(Vorlage vs. Nachbau im 2×-Grid) → 5. In `assets/icons/` + Avalonia-Resource-Verzeichnis einhängen.
Name-Konvention: `lf_<funktionsname>.svg` (eigenes Prefix, KEINE LightBurn-Grafiknamen in Dateinamen).
