# LaserForge — Parity-Checkliste (LightBurn 2.2 → M1..M8)

> **Zweck:** Funktionsgenaue Abbildung von **LightBurn 2.2.00** auf die LaserForge-Phasen **M1..M8** unter dem Ziel
> „**maximum functional parity**“ — ergänzt um **automatisierte GUI-Verifikation** und **automatisiertes GitHub-Publishing**.
> **UI-Design-Ziel:** exaktes LightBurn-2.2-Bild — helle Arbeitsfläche im dunklen Fenster, 6 Rechts-Tabs, rotes
> Konsole-Band „Auf Verbindung warten…“, Laser-Panel mit COM-/Hersteller-Dropdown, 9-Kreis-Job-Ausgangsposition-Grid,
> Layer-Leiste unten 00–30 + T1/T2.

Stand: 25.10.2026 (Initial) · Nach jeder Phasen-Fertigstellung Statusspalte pflegen. Sprache: Deutsch (Projektregel).

---

## 1. Quellen
| Quelle | Inhalt / Nutzung |
|---|---|
| `/mnt/truenas/Daten/jarvis-files/Fabian/LightBurn/references/rea-lightburn-core-20261025.md` | REA-Befund: LightBurn **Core 2.2.00**; Gerät „Elegoo“: **GRBL, Serial, COM3 @ 115200**, **HomeOnStartup=true**, **Workspace 400×400 mm**, units mm/mm-min, `supports_z=true`; Jog-Vorgaben 10 mm @ 6000 mm/min (XY), 10 mm @ 600 mm/min (Z); Kamera (USB) und Rotary (rotarySteps 40, Ø 16,8/68) vorhanden; **REST-API** Port 19520, Pairing localhost + Consent (30 s), HMAC-Minuten-Token, Endpunktliste (§ 10); `/api/cuts`-JSON: mode cut, frequency 20000, dot_width 0.08, fiber_pulse_width 100, gantry/blower-Anhang; 2.2-ChangeLog: True Nest, Center Line Trace, REST API, 3D-Import (+Depth maps), Custom-GCode-Flavors (Creality Falcon HTTP, Duet/RRF HTTP, WeCreat), xTool F1/F2/P2 (WS :8081), LiDAR-Höhenkompensation, Emblaser-Pro-Rotary, Gerber/PCB-Import, Epilog, Multi-Page-Projekte, Camera Print & Cut (Labs), ACCELaser HD1, Bambu R1 |
| `/mnt/truenas/Daten/jarvis-files/Fabian/LightBurn/references/lightburn-format-spec.md` | **.lbrn/.lbrn2** XML: `<LightBurnProject>`; Shapes **Rect** (W/H/Cr), **Ellipse** (Rx/Ry), **Polygon** (Rx/Ry/N), **Path** (`<V>` vx/vy/c0x/c0y/c1x/c1y/S + `<P>` T=L&#124;B, p0, p1), **Text** (Font/Str/H/LS/LnS/Ah/Av/Weld), **Group** (Children); **XForm** a b c d e f; **CutSetting** (Cut/Scan/Fill/Score/KerfTest/Material; index, name, minPower, maxPower, minPower2, maxPower2, speed, priority, runBlower, zOffset, numPasses, zPerPass, enableLaser1/2, lineSpacing, scanAngle, hatchEdge, doOutput, kerfWidth, overcut, frequency, rampLength); Thumbnail (250×250 PNG, Base64); UIPrefs (Optimize_ByLayer/-ByGroup/-ByPriority); VariableText; Notes; .lbrn2 = kompakte VertList/PrimList-Strings |
| `docs/recherche/grbl-gcode-flavor.md` | GCode-Erzeugungs-Spec **GRBL 1.1** (M3/M4-Umsetzung), verifiziert gegen das GRBL-Wiki (gnea/grbl), Stand 25.10.2026 |

**Regeln:**
1. Funktionsparität ≠ Quelltext-Parität: **niemals** LightBurn-Code/Assets/Strings kopieren — eigene Implementierung, eigene Icons, eigene (deutsche) Texte.
2. Intern ausschließlich mm (REA `/api/project`: units mm/mm-min).
3. Keine Secrets/Token im Repo oder in Dokumenten.

---

## 2. Phasenmodell & Gates
| Phase | Titel | Enthält | Gate („Phase fertig wenn“) |
|---|---|---|---|
| **M1** | Skeleton & Geometrie | Projektstruktur, Datenmodell (Shapes + XForm), Canvas-Rendering, Zeichen-/Editier-Basis | Alle MUST-Zeilen § 5–6 erledigt; erster Screenshot-Vergleich (helle Arbeitsfläche in dunklem Fenster) |
| **M2** | lbrn2-IO | .lbrn/.lbrn2 lesen+schreiben (Round-trip), SVG/DXF/Bild-Import, GCode-Export-Vorbereitung, Golden-Tests-IO | Round-trip-Tests grün; alle MUST-Zeilen § 4 erledigt |
| **M3** | Optimierung & GCode | Cut-Optimierer, GCode-Erzeugung nach `docs/recherche/grbl-gcode-flavor.md`, Golden-Tests-GCode | MUST-Zeilen § 5 + § 7 erledigt; erzeugte Programme bestehen `$C`-Parse-Prüfung |
| **M4** | GRBL-Live | Serial COM/Baud-Auto, HomeOnStartup, Status-Poll, Jog/Frame/Home, Machine-Settings, $32-Check | Live an Elegoo (COM3 @ 115200): Verbindung, Rahmen, kleiner Job, Machine-Settings-Dialog |
| **M5** | GUI-Hauptfenster | Exaktes LightBurn-2.2-Bild, alle Panels, automatisierte GUI-Verifikation | Screenshot-Verifikation gegen Design-Ziel-Bild: alle MUST-Zeilen § 10 ✅ |
| **M6** | Preview & Dialoge | Burn-Preview (Material-Maßstab, Fahrtzeit-Abschätzung), CutSettings-Dialog, Material-Dialoge | MUST-Zeilen § 9 erledigt; Preview stimmt mit Job-GCode überein |
| **M7** | API / CLI / MCP | Headless-Steuerung, REST-API-Parität (2.2), Zusatz-Maschinen-Flavors | MUST-Zeilen § 11 erledigt; Headless-Job an Elegoo |
| **M8** | Publish | Automatisiertes GitHub-Publishing + GUI-Regression als Gate | Tag → automatischer Build → Release + Changelog; GUI-Regression grün |

---

## 3. Legende für alle Tabellen
| Spalte | Bedeutung |
|---|---|
| **Phase** | M1..M8 (siehe § 2); Kombination „M2/M6“ = Modell-Teil früher, Dialog/Detail später |
| **Priorität** | **MUST** = Pflicht für das M-Ziel (maximum functional parity) · *später* = Ausbaustufe nach M8 / Backlog-Slot |
| **Status** | ☐ offen · ◐ in Arbeit · ✅ erledigt (Startstand: alles ☐) |

---

## 4. Dateiformate & I/O
| LightBurn-2.2-Funktion | LaserForge-Anforderung | Phase | Priorität | Status |
|---|---|---|---|---|
| .lbrn öffnen | Legacy-XML (FormatVersion="0"): Shapes mit vollständigen `<V>`/`<P>`-Tags und `<XForm>` | M2 | **MUST** | ✅ |
| .lbrn2 öffnen | FormatVersion="1"; kompakte VertList/PrimList-Strings — exakte Kodierung via Probe klären (Offen-Punkt O2) | M2 | **MUST** | ◐ |
| .lbrn/.lbrn2 schreiben | Round-trip-Sicherheit: lesen→schreiben→lesen identisch; **unbekannte Attribute 1:1 erhalten** | M2 | **MUST** | ✅ |
| Thumbnail im Projekt (Thumbnail-Auto) | 250×250 PNG als Base64 beim Speichern automatisch erzeugen/aktualisieren | M2 | **MUST** | ☐ |
| SVG importieren | Linien/Polygonzüge/Bezier → Path-Shapes | M2 | **MUST** | ✅ |
| DXF importieren | Konturen/Kreise; Ebenen→Layer-Zuordnung | M2 | **MUST** | ✅ |
| Bild importieren (PNG/JPG/BMP) | Raster-Objekt, an Scan-Layer gebunden (Gravur) | M2 | **MUST** | ✅ |
| VariableText-Feld erhalten | Start/End/Current/Increment/AutoAdvance persistent (Round-trip) | M2 | **MUST** | ✅ |
| Notes (ShowOnLoad) | Projekt-Notizen persistent | M2 | **MUST** | ✅ |
| UIPrefs erhalten | Optimize_ByLayer/-ByGroup/-ByPriority usw. persistent | M2 | **MUST** | ✅ |
| CutSettings-Struktur abgleichen | Feldmenge gegen REA-`/api/cuts`-JSON validieren (mode, frequency, dot_width, fiber_pulse_width, gantry/blower) | M2 | **MUST** | ✅ |
| AI importieren | PDF-Abkömmling (Interpreter wiederverwenden) | M2 | später | ☐ |
| PDF importieren | Vektoren extrahieren | M2 | später | ☐ |
| Gerber/PCB importieren (2.2-Neuheit) | Ausbaustufe | M2/M6 | später | ☐ |
| 3D-/Tiefen-Import (+Depth maps, 2.2) | Ausbaustufe | M7 | später | ☐ |
| GCode exportieren | Alle Layer + Optimierung, Flavor GRBL — ohne Maschine | M3 | **MUST** | ✅ |
| SVG/DXF exportieren | Ausbaustufe | M7 | später | ☐ |

## 5. Layer & CutSettings (Vollumfang)
| LightBurn-2.2-Funktion | LaserForge-Anforderung | Phase | Priorität | Status |
|---|---|---|---|---|
| Layer-Modell 00–30 + T1/T2 | Farbindex je Layer (REA `/api/layers`: 0=Schwarz, 1=Blau, 2=Rot, 3=Grün, 4=Gelb, 5=Orange, 6=Cyan …) | M1 | **MUST** | ✅ |
| CutSetting-Typen | Cut / Scan / Fill / Score / KerfTest / Material (Format-Spec § 4) | M2 | **MUST** | ✅ |
| CutSetting-Basisfelder | index, name, minPower, maxPower, minPower2, maxPower2, speed, priority | M2 | **MUST** | ✅ |
| Scan-Felder | lineSpacing (mm), scanAngle (°), hatchEdge | M2 | **MUST** | ✅ |
| numPasses | Passe-Wiederholung in GCode (Flavor-Spec § 5) | M3 | **MUST** | ✅ |
| doOutput | Aus deaktivierten Layern erzeugt kein GCode | M3 | **MUST** | ✅ |
| Power → S-Wert | Power % × $30-Skala → S-Wert im GCode (M4-Flavor, Spec § 4) | M3 | **MUST** | ✅ |
| Speed → mm/min | mm/s × 60 → F-Sätze | M3 | **MUST** | ✅ |
| zOffset / zPerPass | GCode-Z-Bewegungen je Passe (Gerät `supports_z=true`); Persistenz M2 | M3 | später | ☐ |
| runBlower / Blower | Flag je Layer, Ansteuerung nur bei Geräten mit Blower-Ausgang | M3 | später | ☐ |
| enableLaser1 / enableLaser2 | Mehrkanal-Ausgänge | M3 | später | ☐ |
| kerfWidth / overcut | Kerf-Kompensation / Überstechen | M3 | später | ☐ |
| frequency / dot_width / fiber_pulse_width | Felder erhalten (M2); Ansteuerung je Maschinenklasse | M3 | später | ☐ |
| rampLength | Scan-Rampen | M3 | später | ☐ |
| CutSettings-Dialog | Vollformular aller Felder, je Layer 00–30 + T1/T2 | M6 | **MUST** | ☐ |
| Bild-Scan-Optionen | Dither-Modi, Helligkeit/Kontrast | M6 | später | ☐ |
| Material-Test | Kraft/Geschwindigkeit-Raster-Matrix | M6 | später | ☐ |
| Abgleich gegen Live-/api/cuts | Struktur/Defaultwerte mit dem Elegoo-Profil verifizieren | M6 | später | ☐ |

## 6. Geometrie & Werkzeuge
| LightBurn-2.2-Funktion | LaserForge-Anforderung | Phase | Priorität | Status |
|---|---|---|---|---|
| Auswahl | Klick, Shift/Strg-Mehrfachauswahl, Gummiband | M1 | **MUST** | ☐ |
| Linie | Geradensegment; Achsen-/Winkelfang mit Shift | M1 | **MUST** | ☐ |
| Rechteck | W/H, Eckradius `Cr` | M1 | **MUST** | ☐ |
| Ellipse/Kreis | Rx/Ry | M1 | **MUST** | ☐ |
| Polygon | N-Ecken (Rx/Ry); Sterne/Zackenformen später | M1 | **MUST** | ☐ |
| Text | Font-String, H, LS/LnS, Ausrichtung Ah/Av, Weld | M1 | **MUST** | ☐ |
| Gruppierung | Group/Ungroup, verschachtelt (Group mit Children) | M1 | **MUST** | ☐ |
| Undo/Redo | Stack, konfigurierbare Grenze | M1 | **MUST** | ☐ |
| Position/Size-Felder | X/Y absolut, W/H, Ratio-Lock, 9-fach Ankerpunkt | M1 | **MUST** | ☐ |
| Transform per Anfasser | Verschieben/Rotieren/Skalieren/Spiegeln — XForm a b c d e f korrekt komponieren | M1 | **MUST** | ☐ |
| Clipboard | Kopieren/Einfügen/Duplizieren/Löschen | M1 | **MUST** | ☐ |
| Node-Bearbeitung | Knoten verschieben, Bezier-Tangenten c0/c1, Smooth-Flag `S`, Segmente einfügen/löschen | M2 | **MUST** | ☐ |
| Anordnen | Ausrichten (links/mitte/rechts/oben/unten/…), Verteilen, Reihenfolge vor/zurück | M2 | **MUST** | ☐ |
| Weld / Union / Subtract / Intersect | Boolesche Operationen | M2 | später | ☐ |
| Offset (Kontur ± mm) | Außen-/Innenversatz | M2 | später | ☐ |
| Freihand-/Bezier-Zeichnen | Erweiterter Pfad-Modus | M2 | später | ☐ |
| Grid-/Radius-Array (Duplizieren) | Step & Repeat | M6 | später | ☐ |

## 7. Optimierung (alle M3)
| LightBurn-2.2-Funktion | LaserForge-Anforderung | Phase | Priorität | Status |
|---|---|---|---|---|
| By-Layer-Reihenfolge | Layer nacheinander (UIPrefs Optimize_ByLayer) | M3 | **MUST** | ✅ |
| By-Group-Reihenfolge | Gruppenreihenfolge (Optimize_ByGroup) | M3 | **MUST** | ◐ |
| By-Priority-Reihenfolge | Cut-Priority steuert Reihenfolge (Optimize_ByPriority) | M3 | **MUST** | ✅ |
| Inner First | Innenzüge zuerst, Außenkonturen zuletzt | M3 | **MUST** | ✅ |
| Pfad-Vereinfachung | Kollinear-Filter, Mindestsegmentlänge — deterministisch | M3 | **MUST** | ✅ |
| Arbeitsrichtung (Direction) | Konstante Faserrichtung als Basis; Zufall/„Conveyor/Linear“-Varianten | M3 | später | ☐ |
| Group Inner First | Innen-zuerst innerhalb jeder Gruppe | M3 | später | ☐ |
| Verbundene Pfade zusammenfassen | Join nearby paths mit Lückentoleranz | M3 | später | ☐ |
| Wander (Nearest-Neighbor) | Optimale Anfahrtreihenfolge | M3 | später | ✅ |
| True Nest (Pro, 2.2) | Verschachtelung auf Werkstück | M6 | später | ☐ |

## 8. Maschinen & Verbindung (GRBL & Co.)
| LightBurn-2.2-Funktion | LaserForge-Anforderung | Phase | Priorität | Status |
|---|---|---|---|---|
| Geräte-Bibliothek | Anlegen/Bearbeiten/Löschen; Profil wie REA-prefs.ini (DisplayName „Elegoo“, Typ GRBL Serial, Port COM3, Baud 115200, HomeOnStartup, Rotary, Kamera) | M4 | **MUST** | ◐ |
| COM-Scan | Verfügbare Ports listen; „Port belegt“-Hinweis (REA: COM3-Konflikt am 25.10.) | M4 | **MUST** | ✅ |
| Baud-Auto | 115200 Standard; Auto-Erkennung (9600/57600/115200/250000) über Willkommensstring | M4 | **MUST** | ✅ |
| Verbindung auf-/abbauen | Eingangspuffer leeren, Willkommensstring parsen („Grbl X.Xx …“), Reconnect-Loop | M4 | **MUST** | ✅ |
| HomeOnStartup | true → nach Verbindung `$H`, auf ok warten (Timeout 60 s); false → keine Befehle | M4 | **MUST** | ✅ |
| Alarmzustand | `ALARM:N` erkennen; `$H` als Ablösung; `$X` nur mit Warnung | M4 | **MUST** | ✅ |
| Status-Poll | `?` → Statusreport `<Idle&#124;MPos:…&#124;…>`; ≤ 5 Hz (GRBL-Empfehlung), konfigurierbar (Spec § 9) | M4 | **MUST** | ✅ |
| Positionsanzeige | MPos/WPos-DRO live im Laser-Panel | M4 | **MUST** | ◐ |
| Jog | `$J=`-Jogging (v1.1), Jog-Aus via Realtime-Code; XY-/Z-Buttons, Custom-Schritte; Vorgaben: 10 mm @ 6000 mm/min (XY), 10 mm @ 600 mm/min (Z) | M4 | **MUST** | ✅ |
| Frame (Rahmen) | Rechteck um Job-Bounding-Box mit niedriger Frame-Power (Spec § 7) | M4 | **MUST** | ✅ |
| Home manuell | `$H`-Button | M4 | **MUST** | ✅ |
| Job-Ausgangspunkt | 3×3-Grid setzt Job-Origin („Start von hier“) | M4 | **MUST** | ☐ |
| Move (abs/rel) | Positionsfahrt im Move-Panel | M4 | **MUST** | ☐ |
| Machine-Settings-Dialog | `$$` lesen, `$x=val` setzen, Backup speichern/laden (EEPROM-Zugriffe nur per Send-Response-Streaming) | M4 | **MUST** | ◐ |
| Laser-Modus-Prüfung | $32≠1 → Warnbanner + 1-Klick-„$32=1“ (mit Bestätigung) | M4 | **MUST** | ✅ |
| Workpiece-Absicherung | Soft-Limits gegen Workspace 400×400 prüfen bevor Job startet | M4 | **MUST** | ◐ |
| Feed-/Power-Override | Realtime-Override-Codes + Slider | M4 | später | ☐ |
| **Ruida-Netzwerk (RDC6442 …)** | **Ausbaustufe/Backlog-Slot:** eigener Treiber wird vorbereitet, im M4-Skelett nur als Platzhalter | M4+ | später | ☐ |
| ESP32-GRBL (WLAN/HTTP) | Ausbaustufe | M7+ | später | ☐ |
| Custom-GCode-Flavors (2.2) | Creality Falcon HTTP (Creality Falcon HTTP), Duet/RRF HTTP, WeCreat | M7 | später | ☐ |
| xTool F1/F2/P2 (WS :8081) | Netzwerktreiber inkl. Beacon „Find My Laser“ | M7 | später | ☐ |
| LiDAR-Höhenkompensation (2.2) | Ausbaustufe | M7 | später | ☐ |

## 9. Preview (alle M6)
| LightBurn-2.2-Funktion | LaserForge-Anforderung | Phase | Priorität | Status |
|---|---|---|---|---|
| Simulierter Burn | Konturen + Scan-Raster auf Materialfarbe gerendert (identisch zum GCode-Layer-Zuschnitt) | M6 | **MUST** | ☐ |
| Material-Maßstab | Material-/Job-Skalierung, Ausrichtungs-Vorschau | M6 | **MUST** | ☐ |
| Fahrtzeit-Abschätzung | Zeit aus Speed/Fahrt je Layer (+Approx-Puffer) | M6 | **MUST** | ☐ |
| Passe-/Z-Offset-Anzeige | Visualisierung der numPasses/zPerPass | M6 | später | ☐ |
| Live-Position im Preview | Aktueller Laserfokus live | M6 | später | ☐ |

## 10. UI — exaktes LightBurn-2.2-Bild (M5, alle **MUST**)
| LightBurn-2.2-Element | LaserForge-Anforderung | Phase | Priorität | Status |
|---|---|---|---|---|
| Dunkles Hauptfenster + helle Arbeitsfläche | Theme: dunkler chrom, Canvas hell (Kontrast-Ziel exakt wie Design-Ziel-Bild) | M5 | **MUST** | ☐ |
| Menüleiste | Datei/Bearbeiten/Ansicht/Layer-Grundfunktionen +DE | M5 | **MUST** | ☐ |
| Tools-Leiste | Auswahl, Linie, Rechteck, Ellipse, Polygon, Text, Node-Edit-Zugriff | M5 | **MUST** | ☐ |
| Eigenschaften-Leiste | X/Y, W/H, Ratio-Lock, Layer-Auswahl, kontextuell | M5 | **MUST** | ☐ |
| 6 Rechts-Tabs | Konsole, Vorschau, Bewegen/Move, Laser, Ebenen (Cuts/Layers), (6. Tab gemäß Design-Abgleich — Offen-Punkt O1) | M5 | **MUST** | ☐ |
| Rotes Konsole-Band „Auf Verbindung warten…“ | Rot solange keine Verbindung (REA-Zustand 25.10.), wechselt nach Verbindungsaufbau | M5 | **MUST** | ☐ |
| Konsole-Panel | Verbindungs-/Befehlslog, Freitext-Sendefeld, Leeren | M4+M5 | **MUST** | ☐ |
| Laser-Panel | COM-/Hersteller-Dropdown, Verbindung/Zerlegen-Button, Geräteverwaltung-Btn, Start/Stop, Frame-Btn | M5 | **MUST** | ☐ |
| 9-Kreis-Job-Ausgangsposition-Grid | 3×3-Kreis-Raster im Laser-Panel setzt Job-Origin | M5 | **MUST** | ☐ |
| Layer-Leiste unten | 00–30 + T1/T2 Farbkacheln, Auswahl/Output-Schalter | M5 | **MUST** | ☐ |
| Bewegen-Panel (Jog-UI) | XY/Z-Steuerknöpfe, Custom-Schritte, abs/rel | M5 | **MUST** | ☐ |
| Preview-Tab-Chrome | Tab/Panel-Gehäuse M5; Inhalt M6 | M5 | **MUST** | ☐ |
| Eigene Assets & Texte | Eigene Icons, deutschsprachige Texte, keine LB-Assets/Strings | M5 | **MUST** | ☐ |
| **Automatisierte GUI-Verifikation** | Screenshot-/Vision-QA-Suite gegen Design-Ziel-Bild (Pixel-Differenz-Report); Gate M5, Regression-Gate M8 | M5+M8 | **MUST** | ☐ |

## 11. Zusatz & Automatisierung
| LightBurn-2.2-Funktion | LaserForge-Anforderung | Phase | Priorität | Status |
|---|---|---|---|---|
| Material-Library inkl. Restmaterial | Materialbibliothek mit Restmaterial-Erfassung (Restmenge, Kommentar, Wiederverwertung) | M6 | später | ☐ |
| Variable-Text-Nummerierung | Start/End/Current/Increment/AutoAdvance (Serialisierung) | M6 | später | ☐ |
| Thumbnail-Anzeige | Vorschau der Projektdateien in Datei-Dialoge/-listen | M6 | später | ☐ |
| Kamera-Ausrichtung/-Overlay | USB-Kamera (REA vorhanden), Ausrichten via Marker (2.2 „Camera Print & Cut“, Labs) | M6/M7 | später | ☐ |
| Rotary/Kugelrotary-Setup | Rotary-Wert (rotarySteps 40, Ø 16,8/68) — Y-Achsen-Skalierung | M6 | später | ☐ |
| **REST-API (2.2-Parität)** | Port 19520, Pairing nur localhost + Consent (30 s), HMAC-Minuten-Token Bearer; Endpunkte /api/status, /api/position, /api/jog/settings, /api/goto/saved, /api/units, /api/file/upload, /api/file/open, /api/project, /api/layers, /api/cuts(/index), /api/overlay(/metadata), /api/material-library, /api/events (SSE), /api/events/poll | M7 | **MUST** | ◐ |
| CLI (Batch-GCode, Job) | Headless: Datei → GCode, Job-Start/Status ohne GUI | M7 | **MUST** | ◐ |
| Unit-/Golden-Tests | IO-Round-trip (M2) + GCode-Set (M3) deterministisch | M2+M3 | **MUST** | ✅ |
| **Automatisiertes GitHub-Publishing** | Tag → automatischer Build → Release mit Changelog (kein manueller Upload); GUI-Regression als Gate | M8 | **MUST** | ☐ |

---

## 12. Offene Punkte (Design-/Live-Abgleich)
| ID | Frage | Weg |
|---|---|---|
| O1 | Exakte Namen/Reihenfolge der **6 Rechts-Tabs** im LightBurn-2.2-Bild fixieren | Design-Abgleich gegen Stand-PC-Screenshot (25.10.) |
| O2 | Exakte VertList/PrimList-Kodierung des kompakten .lbrn2 | Minimale Probe-Datei über Live-REST `/api/project` ziehen (Format-Spec § 9) |
| O3 | Job-Ende-Verhalten LightBurn (Rückfahrt Startpunkt vs. `$H`) | Serielles Log am Stand-PC mitschneiden (nur Funktionsbeobachtung) |
| O4 | Elegoo-GRBL-Version, $30-Skala | Willkommensstring + `$$` am Elegoo lesen |
| O5 | 9-Kreis-Grid: exakte Optik/Anordnung | Design-Abgleich (Screenshot) |

---

## 13. Status-Log (automatisch gepflegt)
- 25.10.2026: M1 Geometrie/XForm/Modell ✓ · M2 Formate (lbrn legacy+Tokens/Verlustfreiheit/Thumbnail+PNG-Enkoder/DXF/Bild) ✓ ·
  M3 Optimierung (Innen-Erst/NN/By-Layer+Priority/Vereinfachung) + Goldene GCode ✓ · M4-Kern (Verbindung/Status/Jog/Frame/Home/$$-Einstellungen/
  ALARM/Baud-Auto/COM-Scan) ✓ · M7-Basis (REST-Protokollparität 19540 + CLI + MCP-stdio) ✓ — Beweis: 55/55 Stand-PC-Tests.
- GUI-Zeile (M5/M6) = Sub-Agent-3-Lauf (live, Screenshot-Iteration); COM3/Elegoo-Freigabe + echtes .lbrn2 = Sir-Offenpunkte (O2/O4).
