# LightBurn 2.2 — Bildschirmaufbau des Hauptfensters (Track B: Web-Recherche)

**Stand:** 25.10.2026 · **Zweck:** funktionale Referenz für LaserForge (Clean-Room)
**Regel:** Alles hier sind eigene Umschreibungen der recherchierten Fakten. Es wurde kein LightBurn-Text, -Code oder -Asset übernommen. LightBurn-/RDWorks-Bezeichner erscheinen nur als Eigennamen zur Identifikation.

## Quellenbasis (abgerufen am 25.10.2026)

- Offizielle 2.2er-Doku, beim Abruf als „Release Candidate“ gekennzeichnet (Details der finalen Version können leicht abweichen):
  - Hauptfenster-Übersicht aller Regionen: `docs.lightburnsoftware.com/2.2/Reference/UI/`
  - Unterpungen: `.../Reference/UI/MenuToolbar/`, `...MainToolbar/`, `...CreationToolbar/`, `...ModesToolbar/`, `...ModifiersToolbar/`, `...ColorPalette/`, `...StatusBar/`, `...WindowMenu/`, `...Workspace/`, `...ArrangeToolbar/`
  - Fensterseiten: `.../2.2/Reference/CutsLayersWindow/`, `.../Reference/LaserWindow/`, `.../Reference/MoveWindow/`, `.../Reference/ConsoleWindow/`, `.../Reference/MacrosWindow/`, `.../Reference/CutSettingsEditor/`
  - Einsteigermaterial: `.../2.2/GetStarted/ColorsAndLayers/`, `.../GetStarted/JobControl/`, `.../GetStarted/CutSettingsBeginner/`
- Anpassungs-Guide (2.0er-Doku): `docs.lightburnsoftware.com/2.0/Guides/CustomizingTheLightBurnWindow/`
- Release-Blog 2.2.00 (Early Access, Blogtitel: True Nesting / Center-Line-Trace / Multi-Page): `lightburnsoftware.com/blogs/news/lightburn-2-2-00-early-access-true-nesting-center-line-trace-multi-page-projects`
- Abgleich mit der lokalen Track-A-Aufzeichnung (REA des echten Core 2.2.00 am Stand-PC).

---

## 1. Grundstruktur des Hauptfensters

- Standard-Layout gilt für **GCode-Geräte** (GRBL, Smoothie, Marlin); bei DSP/Galvo-Profilen ändert sich die Zusammensetzung leicht (andere Bedienelemente). Für LaserForge (GRBL zuerst) ist das GCode-Layout maßgeblich.
- Track-A-Beobachtung am echten 2.2.00-Client: dunkles Fenstergestell, helle Arbeitsfläche in der Mitte. Die Doku selbst sagt nichts über Farben — dieses Bild kommt aus unserer Live-Aufnahme.
- Die Doku identifiziert **21 Regionen (A–U)** des Standard-Layouts:

| Region | Name in der 2.2-Doku | Funktion (eigene Umschreibung) |
|---|---|---|
| A | Workspace / Edit Window | Zeichen-, Anpass- und Anordnungsbereich der Grafiken |
| B | Menüs | sämtliche Top-Menüs (siehe Abschnitt 2) |
| C | Main Toolbar | Projekt öffnen/importieren/speichern, Clipboard, Ansicht |
| D | Arrange Toolbar | Objekte untereinander und relativ zueinander positionieren |
| E | Numeric Edits Toolbar | Größe/Position/Orientierung der Auswahl über Zahlenfelder (≈ unsere „Eigenschaften-Leiste“) |
| F | Text Options Toolbar | Text-Attribute (Schriftart, Höhe, Zeichen-/Zeilenabstand) |
| G | Modes Toolbar | Rotary, Print-and-Cut, Split-Marking (abhängig von der Geräteart) |
| H | Docking Toolbar | Objekte an Kanten bzw. an andere Objekte „anstoßen“ (= Anlegen) |
| I | Creation Toolbar | Auswahl, Zeichnen, Knotenbearbeitung (linke obere Hälfte) |
| J | Modifiers Toolbar | Kombinieren, Versetzen, Reihen (linke untere Hälfte) |
| K | Cuts / Layers Window | Layer-/Operations-Liste (rechts oben, vorderster Tab) |
| L | Move Window | Jogging, Positionierung, Homing (Tab hinter Cuts/Layers) |
| M | Console Window | Controller-Meldungen + Direkteingabe (nur GCode-Geräte) |
| N | Macros Window | Makro-Knopfleiste (nur GCode-Geräte) |
| O | Shape Properties Window | Eigenschaften der gewählten Objekte (nicht default) |
| P | Laser Window | Verbindung, Framing, Start — **unten rechts** |
| Q | Material Library Window | Material-Voreinstellungen (nicht default) |
| R | Color Palette| Farbpaletten-Leiste der Layer — **unten** |
| S | Transform Control Toggles | Transform-Umschalter neben Statusleiste |
| T | Status Bar | Cursor-Koordinaten, Tool-Status, Meldungen — **unten** |
| U | Pages | Seiten-Registrierung (Multiseiten-Projekte, 2.2-Feature) |

- Zusammenfassung des Standard-Layouts (eigene Synthese):
  - oben: Menüleiste; darunter eine Leistenzone: **Main** ganz links, **Arrange** in der Mitte, **Numeric Edits**, **Text Options**, **Modes**, **Docking** rechts.
  - **Creation** (oben) und **Modifiers** (unten) bilden links eine vertikale Werkzeuggruppe; horizontale Auslegung ist ebenfalls möglich.
  - rechts: Fenster-Tab-Stapel (siehe Abschnitt 5); das **Laser-Fenster** ist separat unten rechts.
  - unten: Farbpaletten (Farbchips) + Statusleiste.

## 2. Menüpunkte (Top-Menüs in 2.2)

In 2.2 lautet die Top-Menüliste (Doku-Reihenfolge): **File, Edit, Arrange, Laser Tools, Web Tools, Window, Language, Help**.

- **File** — Öffnen/Importieren/Speichern/Exportieren sowie Sichern/Exportieren der Einstellungen des Programms.
- **Edit** — Werkzeuge zur Auswahl/Manipulation von Objekten; daneben allgemeine Einstellungen.
- **Arrange** — Positionierungswerkzeuge (einzeln und in Relation zueinander) + physische Laserfahrt zur Auswahl.
- **Laser Tools** — Tests, Schritt-für-Schritt-Assistenten für Laser-Aufgaben (Feinjustierung, Kalibrierung, Zubehör).
- **Web Tools** — ab 2.1: eingebauter Browser-Aufruf externer „Projekt-Werkzeuge“ (z. B. ein Design-Dienst); Befunde direkt in die Arbeitsfläche übernehmen.
- **Window** — Fenster & Leisten ein-/ausschalten, Preview, Zoom, Ansichts-Stil, Seiten-Panels (F12), Layout-Reset.
- **Language** — umschaltbare 24 Sprachvarianten.
- **Help** — Hilfe-/Ressourcen-Zugänge, Lizenzverwaltung, Diagnose-Werkzeuge.
- Auf macOS stehen die Menüs oben am Bildschirmrand, nicht im Fenster (Doku-Notiz).

## 3. Obere Leisten (unterhalb des Menüs) — inkl. Eigenschaften-Leiste

- **Main Toolbar** (ganz links): Neu (Strg+N), Öffnen (Strg+O; Filter standardmäßig auf `.lbrn`/`.lbrn2`), **Import** (Strg+I; Br bringt Artwork ins laufende Projekt, ohne das Projekt zu ersetzen — Warnung: Öffnen ersetzt das Projekt!), Speichern (Strg+S; `.lbrn`/`.lbrn2`), Rückgängig/Wiederholen, Kopieren/Ausschneiden/Einfügen/Löschen; Zugang zu Einstellungen/Geräteeinstellungen.
- **Arrange Toolbar** (Mitte): Gruppieren/Entgruppieren, Spiegel-Werkzeuge (vertikal/horizontal/freie Linie), Ausrichten an Kanten/Mitten des zuletzt gewählten Objekts; Gleich breit/hoch machen; Verteilen; aktuelle Auswahl an Ecken/Mittelpunkten/Zentrum/letzter gemeldeter Kopf-Position versetzen (mit Dropdown „Laser zur Auswahl“).
- **Numeric Edits Toolbar — unsere „Eigenschaften-Leiste“**: Zahlenfelder für **Breite/Höhe (Skalierung), Position (X/Y) und Rotation** der gewählten Objekte. Track-A-Beobachtung zeigt hier W/H/X/Y-Eingaben mit Einheitsumschaltung; exakte Feldliste beim UI-Bau am echten Client abnehmen.
- **Text Options Toolbar**: Schriftart, Größe (Höhe), Abstände (horizontal/vertikal) und mehr.
- **Modes Toolbar** (rechts oben, zwischen Text-Options und Docking): Gantry: Rotary-Modus, Print-and-Cut, Split-Marking; Galvo: Rotary, Zylinder-Ausgleich, Split-Marking.
- **Docking Toolbar**: Objekte links/rechts/oben/unten bis an Arbeitsflächen-Kanten oder an andere Objekte verschieben.

## 4. Linke Werkzeugleiste — Werkzeuge in Reihenfolge

Die linke Seite besteht aus **zwei vertikalen Leisten**.

### 4.1 Creation Toolbar (obere Hälfte, links)

Doku-Reihenfolge der Werkzeuge:

| # | Werkzeug | Taste | Kurze Funktion |
|---|---|---|---|
| 1 | Select | Esc | Objekte anklick-/auswählen (auch Rechteckauswahl) |
| 2 | Draw Lines | Strg/Cmd + L | Vektorpfade aus geraden/gemischten Liniensegmenten ziehen |
| 3 | Draw Shapes | — | Primärform-Untermenü (u. a. Rechteck, Ellipse, Polygon) mit Halten/Ziehen |
| 4 | Edit Nodes | Strg/Cmd + \` | Knoten (Punkte) von Vektorpfaden bearbeiten; nur an nicht gruppierten Pfaden |
| 5 | Trim Shapes | Strg/Cmd + K | Linienschnitte zwischen Überschneidungen kappen |
| 6 | Add Tabs | Strg/Cmd + Tab | Laschen (bewusst ungeschnittene Brücken) ergänzen |
| 7 | Create / Edit Text | Strg/Cmd + T | Texte erfassen/ändern |
| 8 | Position Laser | Alt/Option + L | Laser fährt zu Klickpunkt auf der Arbeitsfläche (Sicherheitsnotiz!) |
| 9 | Measure | Alt/Option + M | Distanzen messen; Objekt-/Knoteninfos beim Überfliegen |

### 4.2 Modifiers Toolbar (untere Hälfte, links)

Doku-Reihenfolge:

| # | Werkzeug | Kurze Funktion |
|---|---|---|
| 1 | Offset Shapes (Alt/Option + O) | Konturversatz erzeugen (innen/außen/beidseitig) |
| 2 | Boolean Tools | Union / Subtraktion / Überschnitt / Schmelzen (mehrfach) |
| 3 | Grid Array | Raster-Anordnung von Kopien (Abstand, Spiegel, Zufallsdreh) |
| 4 | Circular Array | radiale Kopieranordnung (Abstand, Drehwinkel) |
| 5 | Set Start Point | Startpunkt und Laufrichtung pro Form festlegen |
| 6 | Radius/Fillet | Verrundung setzen, mit negativem Wert aushöhlen, oder Verrundung entfernen |

## 5. Rechte Fenster-/Tab-Leiste (Reihenfolge + exakte Namen)

- Standard (rechts oben): **Cuts / Layers Window** vorne; dahinter als Tab der **Move Window**; für GCode-Geräte sind **Console Window** und **Macros Window** im selben Tab-Stapel (Doku: „docked behind the Cuts / Layers Window“).
- Exakte Fensterbezeichner laut 2.2-Doku: „Cuts / Layers Window“, „Move Window“, „Console Window“, „Macros Window“, „Shape Properties Window“, „Laser Window“, „Material Library Window“, „Art Library“, „File List Window“, „Variable Text“, „Camera Control“.
- Fenster, die standardmäßig **nicht** angezeigt sind und per Window-Menü aktiviert werden: Shape Properties, Material Library, Art Library, File List (nur DSP), Variable Text, Camera Control.
- **Abgleich Track A:** Am echten 2.2.00-Client sind an der rechten Kante **6 Tabs** sichtbar. Die Text-Doku nennt als Standard-Tab-Stapel eindeutig nur die vier oben (Cuts/Layers, Move, Console, Macros); die Übrigen zwei Tabs (Kandidaten: Material Library / Shape Properties o. ä.) beim UI-Bau am echten Client abzählen und exakt benennen.

### 5.1 Cuts / Layers Window (Layer-Liste rechts)

- Liste aller Project-Schichten; Reihenfolge = Ausführreihenfolge (per Drag oder Pfeil-Knopf änderbar).
- Spalten: **#** (Schichten-Nr/Name), **Layer** (Farbchip + Titel; Farben gehören zur Paletten unten), **Mode** (Vektor: Line / Fill / Offset Fill; Bilder: automatisch „Image“; Sub-Layers → „Multi“), **Spd/Pwr** (Geschwindigkeit/Leistung, direkt unterhalb der Liste editierbar), **Output** (Ausgabe-Ein-, Ausschalter; grau-markierte Layer in der Arbeitsfläche), **Show** (sichtbar/unsichtbar in der Arbeitsfläche), **Air** (Air Assist je Layer, nur Gantry).
- Doppelt anklicken → öffnet den **Cut Settings Editor**: oben stets Geschwindigkeit/Leistung/Modus, darunter zwei Reiter „Common“ und „Advanced“; links eine Spalten-/Layerliste zum Wechseln.
- Spaltenköpfe-Rechtsklick: alle Ein-/Ausblenden/-Sortierungen (u. a. „Schneidelayer ganz nach unten“); Layer-Rechtsklick: einzelner/all außer ausblenden, layer hervorleuchten lassen, Layer-Objekte auswählen.

### 5.2 Move Window (Tab hinter Cuts/Layers)

- Get Position (Position des Controllers abfragen; zeigt X/Y/Z/U), „Go“ zu eingegebener X/Y-Position, gespeicherte Positionen (eigene Liste, Verwaltung-Dialog).
- Jogging: Richtungs-Pfeile um den Home-Knopf herum, Schritte via **Distance**-Feld, Tempo via **Speed**-Feld (Speed-Werte steuern auch Framing und Positionierungswerkzeug); gestrichelte Pfeile für Z-, Rundpfeile für A-Achse (Rotary).
- **Continuous-Jog**-Umschalter halten-bewegen: bei DSP-Geräten Standard; bei GCode-Geräten ab GRBL 1.1f teils erst durch „Enable $J Jogging“ in den Geräteeinstellungen freigeschaltet.
- Tastatur-/Ziffernpad-Jogging (mit Numlock, Arbeitsfläche im Fokus), Alternative-Jog-Tasten mit Strg/Shift.
- „Move From Machine Zero“ (Fahrt relativ zum Maschinennullpunkt), „Focus Z“ (Fokussieren-Hilfe).

## 6. Untere Layer-Nummern-Leiste (Farbpalette)

- Standard-Position: **unten im Hauptfenster** (Track-A-Bild: Reihenfolge unter der Arbeitsfläche); alternativ links andockbar.
- Umfang: **Farbchips für 29 verschiedenen Layer-Farben plus zwei Werkzeug-Layer (T1, T2) = 31 Chips** (Doku Window-Menü, eigener Wortlaut).
- Track-A-Aufnahme: fortlaufend nummerierte Chips **00–30** plus **T1/T2** rechts — passt exakt zu 29+2.
- Verhalten: Klick weist eine Farbe/Layer zu (bei Auswahl: die gewählten Objekte; ohne Auswahl: Standardfarbe für künftige Objekte). Farbelayer erscheinen automatisch im Cuts/Layers-Fenster.
- **T1/T2 = Tool-Layer**: keinerlei Ausgabe-Snippets, nie zum Laser ausgesendet — z. B. Materialumriss, Linien zum Ausrichten, oder Werkzeugpfad-Hilfen ohne Ausgabe.
- Konvention in Doku: helles Rot für Schneiden, Schwarz für Gravur — üblich, aber freigestellt.
- Layer-Stil/Farben steuern auch die Anzeigeform (Vektor-Grafiken erscheinen üblich als Umrandung, unabhängig vom Layer-Modus).

## 7. Laser-/Maschinen-Panel (Laser Window, standardmäßig unten rechts)

- Kernaufgabe: Laser auswählen/konfigurieren, Verbinden, Framing und Start der Jobs; Aussehen hängt von Geräteprofil und Einstellungen (u. a. „Anfängermodus“) ab.
- **Verbindungs-/Status-Zone (GCode-Geräte):** Geräteprofil-Auswahl, **COM-Port-Dropdown**, Verbinden/Trennen-Kontrolle. Track-A-Aufnahme am echten Core: COM3 bei 115 200 Baud, Geräteprofil „GRBL“, Hersteller-/Controller-Dropdowns; solange die Verbindung fehlt, erscheint ein rotes Statusband — LaserForge formuliert den Bandtext auf Deutsch selbst (keine Textübernahme).
- **Job-Zone:** Start (Versand + Start unmittelbar), Pause/Resume, Stop (Notabbruch); Tastenkombinationen Pause/Strg+Break auch während der Abarbeitung; „Send“ (nur DSP): Projekt als benannte Datei verladen, Verschieben mit Shift → Start danach automatisch.
- **Framing:** zwei Frame-Buttons — „Bounding-Box-Rahmen“ (kleinstes Umschließungsrechteck) vs. „Gummiband-Rahmen“ (engste Umschließungsform); Framing-Tempo übernimmt das Move-Window-Speed-Feld; „Fortlaufend rahmen“ und „Laser während Framing“ per Geräteeinstellungen (Vorsicht: nur niedrige Leistung, Diodenlaser); Shift+Frame = einmaliger Test-Blitz beim Framen.
- **Save/Run Machine Files:** Je Controller-Typ speichert die App ein maschinenfertige Datei: GCode (GRBL/Smoothie/Marlin), RD-Datei (Ruida), OUT (TopWisdom), UD5 (Trocen); „Run …“ lädt+läuft eine solche Datei über die bestehende Verbindung.
- **Home** (Fahrt zum Referenzpunkt; nur bei vorhandenen Homing-Sensoren nutzbar!), **Go to Origin** (Fahrt zum Nutzer-Ursprung).
- **Start From + Job Origin**: zwei Steuerelemente, die bestimmen, wo das Projekt auf dem Tisch ausgeführt wird; Doku-Verweis auf die „Koordinaten und Job-Ursprung“-Seite. Track-A-Beobachtung: ein **9-Felder-Raster (3×3)** für die Job-Ursprung-Position (aktuelle Position, absolute Ecken/Mitten, Nutzer-Ursprung).
- **Cut Selected Graphics** (nur Auswahl versenden) + zugehörig „Use Selection Origin“ (Ursprung nur aus der Auswahl rechnen).

## 8. Statusleiste (unten)

- Links daneben der *Transform Control Toggles*-Umschalter (Transform-Hilfsmarkierungen der Auswahl; Details in der Doku-Liste, hier nicht im Einzelnen erfasst).
- Statusleiste gliedert sich: **Live-Koordinaten der Maus** (X/Y der Arbeitsfläche; stoppt außerhalb), **Tool-Status** (z. B. Begrenzung der Auswahl mit Min/Max + Objektanzahl; beim Linien-Ziehen Endposition/Deltas/Länge), **Meldungsbereich** (werkzeug-/programmspezifische Hinweise; bei DSP/Galvo-Profilen auch Status der Verbindung), **Modus-Anzeiger**.

## 9. Anpassbarkeit des Layouts

- Window-Menü schaltet jedes Fenster/Leiste ein-/aus (Häkchen-Liste).
- Rechtsklick auf Leiste/Fensterkopf öffnet dieselbe Auswahlmenü.
- Fenster nach links/rechts/oben/unten docken; vertikal aufeinander stapeln; in einen Tab-Stapel zusammenfassen; Leisten an gestrichelter Linie lösen und frei platzieren.
- F12 = alle Seit-Fenster aus/an schalten (gleiche Menge wiederherstellen).
- „Reset to Default Layout“ setzt das Fenster-Standardanordnung zurück, ohne Nutzer-Einstellungen/Geräte/Bibliotheken zu berühren.

## 10. 2.2-Erkennungsmerkmale aus dem Release-Kontext

- Aus dem 2.2.00-Release-Blog (Titles, eigene Zusammenfassung): **True Nesting** (automatisches Zusammensetzen der Formen — nur mit Pro-Lizenz nutzbar; Core-Anwender bekommen eine zeitlich begrenzte Pro-Testphase), **Center-Line Trace** (Mittellinien-Erzeugung), **Multi-Page-Projekte** (siehe Region „Pages“).
- Für LaserForge notiert: Seitenkonzept in der Projektdatei als 2.2-FEATURE; eigenes Nesting später, ohne Pfad-Abhängigkeit zu LightBurns Umsetzung. Eigene Formulierungen — keine Übernahme.

## 11. Offene Punkte

- Exakte Liste der 6 rechten Tabs am echten 2.2-Client (Doku nennt als Standard-Tab-Stapel eindeutig Cuts/Layers, Move, Console, Macros; Rest über Track-A-Screenshot).
- Exakte Feldliste der Numeric-Edits-Leiste (Breite/Höhe/X/Y/Rotation + Einheitenumschalter) beim UI-Bau am echten Client verifizieren.
- Transform Control Toggles: Einzelverhalten nicht im Text erfasst (nur als Region S vermerkt).
- „Help“-/„Language“-Menü-Unterpunkte wurden nur in Kurzfassung erfasst.

---
*Dieses Dokument gibt recherchierte Fakten in eigener Formulierung wieder und ist Grundlage für das LaserForge-UI-Design. Kein LightBurn-Material wurde kopiert.*