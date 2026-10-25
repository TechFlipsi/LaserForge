# Protokollrecherche: GRBL 1.1 (primär, unser Gerät) & Ruida-Überblick (sekundär)

**Stand:** 25.10.2026 · **Zweck:** funktionale Referenz für LaserForge (Clean-Room)
**Regel:** Alles in eigenen Worten zusammengetragen; keine Codeübernahme aus den zitierten Projekten. Faktische Unsicherheiten sind ausdrücklich markiert.

## Quellen (abgerufen am 25.10.2026)

- GRBL-Firmware-Wiki des Referenzrepositoys: `github.com/gnea/grbl/wiki/Grbl-v1.1-Commands`, `...Grbl-v1.1-Interface`, `...Grbl-v1.1-Configuration`, `...Grbl-v1.1-Jogging`
- Ruida-Umsetzung: `github.com/jnweiger/ruida-laser/blob/master/doc/protocol.md` (Empfangsnachweis per Wireshark), EduTech-Wiki der Uni Genf (`edutechwiki.unige.ch/en/Ruida`), Paketbibliothek `ruida-pa` (pypi.org/project/ruida-pa), FabLab-Fallstudie (wiki.fablab-nuernberg.de, Diskussionsseite Nova 35)
- MeerK40t-Modulstruktur (Referenz für eine saubere Treiber-Einheit, siehe Datei `open-source-referenzen.md`): `meerk40t/grbl/` (driver/controller/emulator), `meerk40t/ruida/` (control/controller/driver/emulator)

---

# Teil 1 — GRBL 1.1 (unser Primärprotokoll: Elegoo-Gerät via USB-CDC)

## 1.1 Transport und Zeilenformat

- Öffnung: serielle Anbindung, **115 200 Baud, 8-Datenbits, keine Parität, 1 Stoppbit** (Doku-Wert ab v1.1). Bei Diodenlasern erscheint das Board unter Windows als virtueller **COM-Port (USB-CDC)** — genau das trifft auf unser Track-A-Gerät zu (Elegoo, Profil „GRBL“, COM3 @115 200 Baud, Arbeitsbereich 400×400 mm).
- Befehle werden als **Zeile gesendet und mit einem Carriage Return (CR) abgeschlossen**; Kommentare/Leerzeichen dürfen vorkommen. Antworten des Controllers enden in der Praxis mit CR/LF — Empfängercode sollte robust auf CR/LF und LF auslegen.
- Beim Verbindungsaufbau meldet sich die Firmware mit einem **Startbanner** im Stil `Grbl 1.1f ['$' für Hilfe]` — das ist das natürliche Erkennungsmerkmal für „kompatible Firmware gefunden“ (Firmware-Familie + Versionsstand). Die LightBurn-Doku bestätigt für GCode-Maschinen sinngemäß: die typische Firmware ist GRBL in Version 1.1f oder später.
- `$` für sich eingetippt liefert eine Kurzhilfe; aus ihr lassen sich die vorhandenen Systembefehle ablesen (in 1.1 u. a. `$$ $# $G $I $N $x=Wert $Nx=Zeile $J=Zeile $SLP $C $X $H` sowie `~ ! ? Strg+X`).

## 1.2 Nachrichtenklassen (aus Controller-Sicht)

- **Antwortmeldung** pro bearbeiteter Zeile:
  - `ok` — Zeile angenommen/eingerüstet.
  - `error:N` — Zeile enthält Fehler Nr. `N` und wurde verworfen; die Fehlerkataloge (numerische Codes mit Kategorien von Syntax bis Alarm-Ursachen) sind im Wiki festgehalten. Für die UI: ein überschaubares Mapping Code → Klartext reicht (LaserGRBL zeigt genau diese Kataloge mit Tooltips — siehe Datei open-source-referenzen.md).
- **Fremdmeldungen (Push-Nachrichten)** — erkennbar daran, dass sie **nicht** mit `ok`/`error` einsetzen; typischerweise in eckigen/spitzen Klammern: `[MSG:…]` Meldungen, `[HLP:…]`-Hilfe, `<…>` Statusberichte, `ALARM:N` Alarme. Alarme sperren den Maschinenzustand; Entsperr-Kommando ist `$X` (bei Alarm-Ursachen wie Bewegungsfehler/Homing-Abbruch).
- **Statusabfrage `?`** — **ohne** Zeilenende sendbar; liefert jederzeit (Ausnahme: während Homing-/Alarmzustand) einen Kurzbericht in spitzen Klammern, z. B. (Doku-Form, leicht vereinfacht):
  `<Idle|MPos:0.000,0.000,0.000|WPos:0.000,0.000,0.000|Bf:15,128|WCO:0.000,0.000,0.000|FS:12000,100|Ov:100,100,100>`
  - **Idle/Run/Jog/Alarm/…** Zustand; **MPos** Maschinen- vs. **W Pos** Arbeits-Koordinaten (Auswahl via `$10`-Bitmaske / `$13` Maßsystem); **Bf** freie Plätze (Planer, Empfangspuffer — Standard-Empfangspuffer 128 Zeichen); **WCO** Arbeitsbereichs-Versatz (tritt nur bei Änderung auf — spart Telegramm!), **FS** aktuelle Vorschub-/Leistungswerte (nur wenn Spindel läuft), **Ov** Override-Stände (nur bei Änderung).
  - UI-Anwendung: Poll-Intervall (typ. 100–250 ms) + ggf. aufwendiges „nur bei Änderung“-Muster, wie es GRBL einsetzt, sparen Bandbreite — für unser Seriell-Poll lohnt ein einfacher, fixer Takt.

## 1.3 Echtzeit-Befehle (einzelne Zeichen, ohne Zeilenende, jederzeit)

Einfach-ASCII: `?` Statusabfrage · `!` Vorschub-Halt (Feed-Hold) · `~` Zyklus-Start/Weitelauf · `0x18 (Strg-X)` **Soft-Reset** (bricht ab und alarmiert, wenn in Bewegung).
Erweitert-ASCII:
- `0x85` **Jog-Abbruch** (bricht Jog-Vorschübe ab und leert die wartenden Vorschübe — danach Statusabfrage für die tatsächliche Position).
- **Overrides** im Bereich `0x90–0x9E`: für jede der drei Regler (Vorschub, Rapid, Spindel/PWM) gibt es je ein „auf 100 % zurücksetzen“-Byte und feine/grobtaktige Hoch-/Runter-Schritte (bis ±10 %); `0x9E` schaltet im Laser-Modus den Strahl zum manuellen Test an/aus. *Exakte Byte→Schritt-Zuordnung steht im gnea-Wiki; für unsere UI reichen 100%-Reset + Hoch/Runter-Paare.*
- Alle Echtzeit-Befehle können mitten in Telegrammstroms ankommen (kein Anstand, kein Zeilenende nötig); mehrfach gleiche Befehle werden ignoriert statt angereichert.

## 1.4 Streaming-Strategien

- **Einfaches Senden-Waiten** (empfohlen, robust): eine Zeile senden, auf `ok`/`error` warten, dann weiter — trägt die Firmware-Reserve nicht voll, aber einfach und sicher. **Für LaserForge als V1 sinnvoll.**
- **Zeichenzähl-Streaming**: Host zählt Bytes im Empfangspuffer nach, nutzt die `Bf`-Felder im Statusbericht, und füllt den Puffer (standard 128 Zeichen) laufend nach — deutlich schneller, aber fehleranfälliger. (Doku erwähnt weitere synchrone Schemata im selben Kapitel.)
- `$C`-Modus: Zeilen nur prüfen (kein Ausführen) — praktische UI-Funktion („Vorschau des GCode-Laufs“).

## 1.5 Jogging (Positionieren)

Struktur: Befehl **muss** mit `$J=` anheben; danach wirkt er wie ein `G1`-Satz. Regeln:
- **F ist Pflicht und nicht modal** (pro Jog setzen).
- Optional überschreibbar: `G20/G21` (Zoll/Millimeter) und `G90/G91` (absolut/relativ) — nur für **diesen** Satz; dazu `G53` (Maschinen-Koordinaten). **`S`/`T` sind unzulässig**; sonstige G-/M-Codes werden verworfen.
- Beispiele aus der Doku-Struktur: `$J=X10.0 Y-1.5 F100` (absolut zur Arbeitsposition), `$J=G91 G20 X0.5 F10` (relativ, Zoll — nur für diesen Satz), `$J=G53 Y5.0 F10` (Maschinen-Koordinaten).
- Zustandsregeln: Jog nur im Zustand Idle (bzw. Jog); nicht mit GCode-Programmlauf mischbar (Fehler), Jog-Abbruch via `0x85` leert wartende Sätze; danach die GUI-Position über eine Statusabfrage auffrischen. Soft-Limit-Verletzungen liefern bei Jog **error statt Alarm** — angenehm für den Nutzer.
- Vorschubangabe in **Millimeter proMinute** (`G94` — in v1.1 ist die Einheit mm/min, anders als in alten Versionen).

## 1.6 `$`-Systembefehle (Auswahl für unsere App)

- `$$` Einstellwerte ausgeben · `$#` Arbeits-Koordinaten-/Versatzspeicher · `$G` Parser-Moduszustand (hilfreich nach Reset) · `$I` Firmware-/Versions-Info/Version · `$N` Startzeilen ansehen/schreiben · `$SLP` Schlummer
- `$H` **Homing-Zyklus** starten (nur bei vorhandenen Grenzschaltern — im UI vor Abwarnung warnen!) · `$X` Freischatung nach Alarm · `$C` Prüfmodus an/aus
- Einstellwerte `$$`-Katalog (Auswahl, die Laser direkt betrifft): `$0/$1/$2` Schrittimpulse, `$20/$21` Soft-/Hard-Limits, `$22` Homing-Regime, `$110..$112` Maximal-Temp in mm/min, `$120..$122` Beschleunigung, `$130..$132` Arbeitsbereich, **`$30/$31` Spindel- (also PWM-Leistungs-) Grenzwerte**, **`$32` Lasermodus (0/1)**.
- Direkt-Reset-Befehle (aus LightBurn-Doku-Liste, gültig für GRBL 1.1): `RST=#` Versätze zurück, `RST=$` Einstellungen Werkstand, `RST=*` ganzer EEPROM-Reset — im UI **nur hinter Doppelbestätigung** anbieten.

## 1.7 Bewegungs-/Laser-Codes

- `G0` = Eilgang (im Lasermodus ohne Strahl), `G1` = Arbeitsgang, `G2/G3` = Bögen; `F` Vorschub (mm/min), `S` Leistung/Wert (PWM), `M3` Leistung dauerhaft an, **`M4` dynamischer Modus (Leistung hängt von der Fahrtgeschwindigkeit ab)** — verhindert bei Tempo-Abbrüchen Brandspuren-/Dunkelspuren —, `M5` Strahl/Laser aus. `M8/M9` Kühlmittel.
- **`$32=1` (Lasermodus)**: ohne Pause zwischen Arbeitsgängen wird der PWM-Wert zwischen Zügen direkt aktualisiert; Eilgänge feuern nicht. Ohne Lasermodus stoppt die Maschine bei jedem `S`-Wechsel (Sinnfall für Fräser). Für unsere Dioden-Standards ist `$32=1` plus `M3/M4` die Ziel-Konfiguration.
- Positionsbezug: `MPos`/`WPos` wie in Abschnitt 1.2; Parser-Zustand via `$G` (siehe 1.6); nach Soft-Reset Positionsbezug ggf. neu herstellen (ohne Homing-Sensor keine sichere Basis).

## 1.8 Elegoo konkret (Kontext Track A)

- unser Track-A-Gerät: **Elegoo, Profil „GRBL“, COM3 @115 200 Baud, 400×400 mm Arbeitsbereich** → klassischer Diodenlaser: Firmware ist ein GRBL-1.1-artiger Aufstieg, über **USB-CDC** als virtueller Seriell-Port angesprochen. Das obige Protokollset ist damit unser Pflicht-Support-Pfad.
- Wichtigster Praxispunkt: Verbindungs-Erkennung über das Startbanner + `$`-Kurzhilfe; Zeilenende CR (senden) / CR+LF (empfangen) tolerieren; Poll der Statusabfrage mit festem Rhythmus; Overrides und Jog-Befehle als Echtzeit-Bytes in den Ausfluss einmischen (keine Zeilenenden nötig).

---

# Teil 2 — Ruida-Überblick (Sekundär-Ziel: Ethernet/Galvo/CO₂-Familie)

## 2.1 Controller und Kanäle

- Ruida-Platine-Familien (RDC6442/6445 & Co.): „Download & Execute“-Maschine — sie akzeptieren eine kompilierte Jobdatei (`.rd`) und führen sie aus; vier Achsen (X, Y, Z, U). Bedienung klassisch über RDWorks; LightBurn bringt eigene Umsetzung mit.
- **Netzwerk-Kanäle (Standardbelegung laut Reverse-Engineering-Quellen):**
  - **UDP 50200** (Hauptkanal zum Controller; Klassischer Absender-Port 40200) — Jobdatei-Ablage + Status-/Leseanfragen.
  - **UDP 50207** (Absender 40207) — „Fernsteuerungs-/App-Kanal“ (einfache Kommandos des Handheld-/App-Panels, uncodiert).
  -einzelne Karten sprechen den Hauptkanal auch per **TCP 50200** (z. B. eine RDC8445S-artige Karte laut `ruida-pa`) — Portbelegung also je Karte prüfen.
- USB-Übertragung: klassisch über einen FT245R-artigen Parallel-Chip mit 19200 Baud / 8N1 (kaum relevant für uns, aber erklärt USB-Dateiübertragungen älterer Maschinen).

## 2.2 Paket- und Byteaufbau (Hauptkanal 50200)

- **Byteform**: jedes Byte trägt 1 Bit „Befehlsstart-Marker“ im höchsten Bit + 7 Nutzbits — der Strom setzt sich aus solchen kurzen Meldungen zusammen.
- **Prüfsumme je UDP-Paket**: 2 Bytes **Großes-Ende (MSB zuerst) vor der Nachricht**; Inhalt ist im Wesentlichen die Summe der Nutzbytes modulo 65536 (alle Quellen hier gleich).
- **Paketgröße**: Höchstgröße ~1470 bis 1474 Byte; moderne Umsetzungen brechen bei ~1000 Byte ab. Trennung kann auch mitten in einem Befehl passieren, geschieht in modernen Umsetzungen aber an Befehlsrändern.
- **Bestätigungsablauf je Paket**: kein Handshake/Kopfteil; jedes Paket mit Prüfsumme+Nutzinhalts wird bestätigt. **Achtung — Quellenwiderspruch**:
  - jnweiger (Wireshark-Auswertung an realer Maschine): ACK = **0xC6** (alles gut), Fehler = **0x46**.
  - EduTech-Wiki / tatarize-Umsetzung: ACK = **0xCC** (Prüfsummen passt) / **0xCF** (Prüfsummenfehlt, muss wiederholt werden).
  - LaserForge-Ansatz: beide Varianten im Treiber tolerieren (empfangene Byte ansehen, im Zweifel Paket erneut senden); mit unserem ersten Ruida-Testgerät verifizieren. Timeout im Bereich **~4 Sekunden**; erstes Paket wird bei Fehler wiederholt, weitere Pakete dann abbrechen.

## 2.3 „Swizzling“ (Bytewert-Zerpflückung)

- Die Nutz-/Befehlsbytes sind je Kartenfamilie leicht verschoben: grob: Byte −1, XOR mit family-spezifischer „Magie“, 7-Bit-Rundumsdreh, +1 (Umkehrung umgekehrt). Die **Magie-Werte** stehen tabellarisch je Kartenmodell (Standardfall 0x88; z. B. 0x11 für einen 634XG-artigen Aufstieg; etliche Modelle mit eigenen Werten).
- Umsetzungshinweis: die Prüfsumme wird in den Quellen unterschiedlich gezeigten (einmal auf den geschwizzlten, einmal auf den ungeschwizzlten Bytes gerechnet) — **zweiter Quellenwiderspruch**, ebenfalls am Gerät auszulegen.

## 2.4 Befehls-/Wertformate (Beispiele aus den Reverse-Engineering-Tabellen)

Wertformat: Positionen absolut (5 Byte, in Mikrometern); relative 2-Byte mit Vorzeichen; Geschwindigkeit 5-Byte µm/s; Leistung 2-Byte in 1/16 384-%-Schritten (100 % = 2^14); Zeichenkette 0-terminiert.
Ausgewählte Befehle (mit der von den Quellen selbst angegebenen Zuverlässigkeit):
- `A8/A9` — Arbeits-Schnitt zu absoluter/relativer Position (Geschwindigkeit/Leistung vorher gesetzt, hoch bewertet)
- `88/89` — Eilgang-Fahrt absolut/relativ ohne Strahl
- `D9 00 02..05` — Achs-Vershubeinstellungen X/Y/Z/U; `C6/C7/C9`-Familie: min-/max-Leistung bzw. Tempo-Werte setzen
- `DA 00 XX XX` — Lesen eines Registers (Antwortform `DA 01 …`), z. B. Anzahl der gespeicherten Jobs — je Karte manchmal fehleranfällig (bestimmte Varianten brechen bei Wert 0 ab — Quellennotiz!)
- `CC/CD` — Maschinen-ACK bzw. Fehler-Meldung
- `E8 02 …` — **Dateiname setzen** für die unmittelbar folgende Übertragung
- `E7 …` — Begrenzungsrahmen-artige (niedrig bewertete) Register

## 2.5 Datei-Upload-Ablauf (eigene Synthese der Quellen)

1. **Dateinamen vorgeben** — kurz vor dem Datenversand (die Karte erwartet die Übertragung zeitnah darauf).
2. **Daten abliefern**: Jobdatei (Hauptkopf + Layer-Köpfe + Arbeitsinhalt + Endmarke; inhaltlich wie RDWorks-Struktur: Hauptkopf mit Ursprung/Bounding-Bereiche, je Layer eigene Kopfdaten mit Tempo/Min-Leistung/Max-Leistung/Material-/Farb-/Frequenzwerten) in Chunks ≤ ~1470 Byte; je Chunk 2-Byte-Prüfsumme **vor** der Nutzlast, je Chunk eine ACK-Antwort.
3. **Endmarke (EOF-Befehl)** im Nutzinhalt besorgt das Ende.
4. **Dateisumme** (`SET_FILE_SUM` laut `ruida-pa`): Host rechnet seine Summe über die schneide-/graviere relevanten Befehlsbytes mit; die Karte vergleicht — die exakte Berechnungsweise ist **nicht voll geklärt** (LightBurn-Auswertungen zeigten Restdifferenzen).
5. **Start der Datei** (Play-/Ausführkommando); danach Status/Position über Lesefragen des 50200-Kanals beobachtbar.
- Für die eigene Umsetzung gilt: UDP mit je-Paket-ACK, kein langer Handshake — aber jedes Detail (ACK-Byteform, Prüfsumme-Reihenfolge/Swizzle-Reihenfolge) über das **Wireshark-Capture am echten Gerät** bestätigen, ehe der Treiber festgenagelt wird.

## 2.6 „App-Kanal“ (UDP 50207)

- Nicht geschwizzelte, einfache Fernsteuer-Befehle (Panel-Emulation mit Down/Up-Paaren für jeden Jog-Schritt in X/Y/Z/U, Start/Halt, Reset, Rahmen, Geschwindigkeitswahl, Pulse …) plus periodisches Keep-Alive (`0xCE`) und 0xCC-Handshake. Für LaserForge aktuell ohne Nutzen (unser Gerät ist GRBL), aber dokumentiert für spätere CO₂/Galvo-Ausblicke.

## 2.7 Unsicherheiten / To-do bei erstem Ruida-Gerät

- ACK-Byteform 0xC6/0x46 vs. 0xCC/0xCF (Konflikt — aus Capture klären)
- Prüfsumme: vor oder nach Swizzle gerechnet, exakte Reihenfolge (Konflikt)
- Exakte Opcodes für Datei-Start/-Stop je Kartenmodell; TCP vs. UDP je Karte
- Kein offizielles Protokolldokument von Ruida — alles ist Reverse-Engineering-Stand der Community; `ruida-pa` und MeerK40t (MIT-Lizenz, siehe open-source-referenzen.md) sind die besten frei zugänglichen Referenzimplementierung

---
*Dieses Dokument gibt recherchierte Fakten in eigener Formulierung wieder. Für unsere App (GRBL-Elegoo-Gerät via USB-CDC) ist Teil 1 maßgeblich; Teil 2 dient der späteren Erweiterung.*