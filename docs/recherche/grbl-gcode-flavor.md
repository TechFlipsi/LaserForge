# GCode-Flavor „GRBL 1.1“ — Erzeugungs-Spec für LaserForge

> **Zweck:** Wie LaserForge (M3 GCode-Erzeugung, M4 Live-Steuerung) für **GRBL 1.1** GCode/Steuerung erzeugt.
> **Stand:** 25.10.2026 · Recherche-Dokument (Recherche only) · Zentrale Punkte gegen GRBL-Wiki (github.com/gnea/grbl) verifiziert.
> **Regel:** Funktionsparität — **niemals** LightBurn-Code/Strings/Assets kopieren; eigene Formulierungen, LightBurn-Näherung wo verifiziert („LightBurn-nah“ markiert).

## 0. Quellen (verifiziert am 25.10.2026)
- https://github.com/gnea/grbl/wiki/Grbl-v1.1-Commands — `$`-Systembefehle (`$H`, `$$`, `$J=`, `$C`, `$X`), M-Code-Gruppen (M3/M4/M5), Realtime-Befehle/Overrides
- https://github.com/gnea/grbl/wiki/Grbl-v1.1-Interface — Streaming-Protokolle, Statusreport, ok/error, Willkommensstring
- https://github.com/gnea/grbl/wiki/Grbl-v1.1-Configuration — `$30/$31/$32`, Homing-Settings `$22–$27`, Max-Raten `$110–$112`, Travels `$130–$132`
- https://github.com/gnea/grbl/wiki/Grbl-v1.1-Laser-Mode — von `$32` verlinkt; M4-Dynamik-Detail [OFFEN: vor M4-Umsetzung lesen]
- REA-Befund (`/mnt/truenas/Daten/jarvis-files/Fabian/LightBurn/references/rea-lightburn-core-20261025.md`)
- Format-Spec (`/mnt/truenas/Daten/jarvis-files/Fabian/LightBurn/references/lightburn-format-spec.md`) — CutSettings-Quelle (Power %, speed, numPasses, lineSpacing, scanAngle …)

## 1. Bezugsgerät (REA-Befund)
| Punkt | Wert |
|---|---|
| Gerät | „Elegoo“ bei **GRBL**, Serial **COM3 @ 115200 baud** (8-N-1) |
| Workspace | **400×400 mm** |
| HomeOnStartup | **true** |
| Z-Achse | `supports_z=true` |
| Jog-Richtwerte | 10 mm @ 6000 mm/min (XY), 10 mm @ 600 mm/min (Z) |
| Einheiten | mm / mm-min (aus `/api/project`) → **Feed immer mm/min** |

---

## 2. Serielle Basis (GRBL-Wiki-verifiziert)
- **Baud/Format:** 115200, 8-N-1 (Wiki „Grbl v1.1 Commands“).
- **Zeilenenden:** Jede Zeile mit **`\r\n`** senden (GRBL akzeptiert `\r` oder `\n`; LaserForge sendet konsistent `\r\n`).
- **Realtime-Zeichen** (ohne Zeilenende, jederzeit erlaubt): `~` (Cycle-Start/Resume), `!` (Feed Hold), `^X` (0x18 Soft-Reset), `?` (Statusreport), 0x84 (Safety Door), 0x85 (Jog-Abbruch), Overrides 0x94–0x9E (u. a. 0x99 = Spindle 100 %, 0x9A/0x9B ±10 %, 0x9C/0x9D ±1 %).
- **Antworten:** `ok` (verarbeitet) · `error:<N>` (Abfrage verweigert) · `ALARM:<N>` · `[MSG:…]` · `[GC:…]` Parser-Modi · `<…>` Statusreport.
- **Streaming:** „**Simple Send-Response**“ ist GRBL-Empfehlung: Zeile senden → auf `ok`/`error:` warten → nächste Zeile. Für sehr schnelle Laser mit vielen kurzen G1-Segmenten „Character-Counting“ (RX-Puffer 128 Zeichen) als spätere Ausbaustufe; XON/XOFF entfällt (aus GRBL-Code entfernt).
- **Regel:** Einstellwert-Schreibbefehle (`$x=`, `$I=`, `$Nx=`) nur im Send-Response-Modus absetzen (EEPROM-Schritte sperren den RX-Interrupt kurzfristig).
- **`$C` Check-Modus:** GCode vorab parsen ohne Bewegung (nur ok/error-Feedback). LaserForge nutzt das als „**Prüf-Absicht**“-Funktion (§ 10).

## 3. Verbindungsbeginn (M4)
1. Port öffnen (115200), Eingangspuffer leeren, **Willkommensstring** abwarten: `Grbl X.Xx ['$' for help]` — Versionsnummern parsen.
2. **HomeOnStartup=true** (Elegoo): `$H` senden → auf `ok` warten (Homing-Zyklus; Settings `$22–$27`: aktivier, Richtungs-Maske, Such-/Suchgeschwindigkeit, Pull-off). Timeout ~60 s; sonst Alarm-Handling.
3. **HomeOnStartup=false:** *keine* Befehle senden (nur Willkommensstring und ggf. Statusreport abwarten).
4. Nach Boot mit `$22=1` ist GRBL im **Alarmzustand** bis `$H` lief — der Homing-Pfad löst das ab; Alternative `$X`(Notaus mit Warnung).
5. **`$$` lesen** und cachen: `$30` (S-Max), `$31`, `$32`, `$22–$27`, `$110–$112`, `$130–$132`.
6. **$32≠1** → Warnbanner „Lasermodus aus“ + Schaltfläche „$32=1“ (Erst-Nutzerzustimmung, dann Send-Response).
7. Notaus: `^X` Soft-Reset jederzeit möglich; Feed Hold `!` → Resume `~`.

## 4. Modal-Startblock (Arbeitsplan-Startzeilen)
```
G21        ; Einheiten mm
G90        ; absolute Positionierung (Workpiece G54 bleibt Standard, kein explizites G54)
M4         ; dynamischer Laser-Modus (GRBL v1.1) — erfordert $32=1
S0         ; Leistung 0 (Startzustand), danach je Layer S<val>
```
- **S-Skala:** S in `[0 .. $30]`. Umrechnung: `S = Power% × $30 / 100` — Beispiel: 80 % bei `$30=1000` → `S800.0`. `$30` aus `$$`-Cache; unbekannt → 1000 nachfragen/prüfen.
- **Feed:** `F = speed(mm/s) × 60` → mm/min (modal).
- **M4-Semantik** (verifiziert): M-Codes **M3/M4/M5** bilden die Spindle-Gruppe; bei `$32=1` läuft GRBL durch aufeinanderfolgende `G1/G2/G3` mit aktualisierten `S`-Werten **ohne Stopp** je S-Zeile (PWM sofort je Bewegung). `$32=0` = Stopp je S-Zeile — Laserforge verweigert Job-Start dann (Warnung), weil sonst bei S≠0 im `G0` brandgefährlich gelasert wird.
- **M4-Dynamik-Detail** (Power ∝ Fahrgeschwindigkeit): Wiki-Seite „Grbl-v1.1-Laser-Mode“ vor M4-Umsetzung verifizieren [OFFEN, § 12].

## 5. Kontur-Schnitt (Cut/Linie)
Geometrien werden zu Polylinien aufgelöst (deterministisch): Koordinaten 3 Nachkommastellen (mm); Bezier-Tessellierung mit konfigurierbarer Toleranz (Standard 0,01 mm); Kollinear-/Mindestlängen-Filter (§ Optimierung, Checklist).

Pro Kontur (geschlossener oder offener Pfad, XForm bereits angewandt):
```
G0 X<sx> Y<sy>      ; Anfahrt Startpunkt — im Lasermodus lasert G0 nicht
M4 S<v>             ; Power je Layer (aus CutSetting maxPower)
G1 F<f>             ; Feed je Layer (mm/min, modal)
G1 X… Y…            ; Folgezüge (modal F/S)
…                   ; je Original-Punkt/Kollinear-Filter
```
- **Passe-Wiederholung:** gesamte Kontur `numPasses×` wiederholen; optional `zOffset`/`zPerPass` via `G0/G1 Z<z>` vor jeder Passe (nur bei `supports_z`).
- Layer-Reihenfolge = Cut-Priority/Inner-First etc. (siehe Optimierung in Parity-Checkliste § 7).

## 6. Scan-Gravur (Raster)
Parameter aus CutSetting: **lineSpacing** (mm, Zeilenabstand), **scanAngle** (°), Typ `Scan`/`Fill` (Format-Spec).

- Zeilengeometrie: horizontale Parallelen im Objektkoordinatensystem; bei `scanAngle` alle Zeilenkoordinaten um das Objekt-/Job-Zentrum unter Winkel rotieren erzeugen.
- **bidi** (bidirektional, Standard an): Zeile i gerade → Scan von links nach rechts; ungerade → von rechts nach links (Punktreihenfolge der Zeile umdrehen). Deaktivierbar (unidirektional).
- Pro Zeile:
```
G0 X<x_start> Y<y>
M4 S<row_power>     ; Leistung je Zeile (Graustufe/„Power per row“) — nur senden, wenn S-Wert wechselt
G1 X<x_end> F<f>
```
- Fahrt zur nächsten Zeile via `G0` (Laser aus im Lasermodus); kein `S0` zwischen Zeilen nötig (Lasermodus regelt).
- „Scan+Shift“ (Zeilenversatz pro Durchgang), Crosshatch (±90°) und Dither-Verarbeitung: spätere Ausbaustufe.

## 7. Frame (Rahmen-Vorschaufahrt)
Rechteck um die Job-Bounding-Box, **Frame-Power** niedrig (LaserForge-Standard 5 %, konfigurierbar, Laser-Frame-Speed-Standard 600 mm/min):
```
G0 X<x0> Y<y0>
M4 S<frame>         ; z. B. 5 % @ $30=1000 → S50.0
G1 F<f_frame>
G1 X<x1>
G1 Y<y1>
G1 X<x0>
G1 Y<y0>
M5
```
- Frame funktioniert nur bei bestehender Verbindung; danach Modalblock für späteren Job neu (§ 4).

## 8. Job-Ende
1. **`M5`** immer zuerst (Laser aus) — Pflicht unabhängig von Rückfahrt-Option.
2. Rückfahrt auf den **Job-Ausgangspunkt** (9-Kreis-Grid): `G0 X<ox> Y<oy>`.
   Alternativen (konfigurierbar): `G0 X0 Y0` (Workpiece-Null, nur wenn Job-Origin auf 0/0) oder `$H` (Home; LightBurn-Näherung „Return to origin/home“ — genaues LightBurn-Werkverhalten offen, siehe Offen-Punkt O3 in Parity-Checkliste § 12).
3. Danach keine weiteren Befehle; Parser-Modi (G90/G21) bleiben modal für den nächsten Job.

---

## 9. Status-Poll & Realtime (M4-Verhalten)
- **Abfrage:** `?` ohne Zeilenende, jederzeit erlaubt.
- **Report:** immer in `<>` eingehüllt: `<Idle|MPos:0.000,0.000,0.000|FS:0.0,0>` — Zustandswort (`Idle/Run/Hold/Door/Home/Alarm/Jog`), MPos/WPos/WCO, Bf (Puffer), FS (Fahrt/Spindle), Ov (Overrides). WCO nur bei Änderung (Statusreport-Maske `$10` steuert Felder).
- **Frequenz:** GRBL-Empfehlung **≤ 5 Hz** (10 Hz „können“ — abnehmender Nutzen, CPU-Belastung). LaserForge-Standard: **5 Hz Standby und Job**; konfigurierbar 1–10 Hz; während `$H` kein Poll (Homing-Exception im Wiki).
- **Über-Verbindung-Fehler**: Poll bei Abbruch → Verbindung-Verlust-Banner (rot) + Reconnect-Loop.

## 10. Prüf-Absicht ok / error
- **Live:** jede sendete Zeile → `ok` (verarbeitet) oder `error:<N>` (Abfrage verweigert). Codes nach Wiki „Message Summary“ (u. a. error:1 = erwartete/fehlende Wortgruppe; detaillierte Code-Tabelle gegen `Grbl-v1.1-Interface#error-codes` abzugleichen [OFFEN]) — LaserForge: Log in Konsole, bei Job-Mitte: Stop + „$32“/`^X`-Vorschlag.
- **Vorab (Golden-Tests M3):** erzeugte GCode-Dateien deterministisch gegen Spec-Ausgaben (§ 11) prüfen — ohne Maschine.
- **Vorab (Live, `$C`):** `$C` Check-Modus — Programm parse-prüfen (nur ok/error, keine Bewegung); danach `$C` aus (soft-reset) → dann Live-Stream per Send-Response.
- **Regeln:** Bei `error:` während des Jobs abbrechen, nicht weitersenden (GRBL-Empfehlung: halt — schadensverhindernd). `ALARM:N` = Notaus-Zustand (§ 3).

## 11. Referenz-Ausgaben (Golden-Test-Formate)
Koordinaten `%.3f`, `F %.1f` mm/min, `S %.1f` (0–$30).
```
— Beispiel Kontur 80 % @ 20 mm/s ($30=1000) —
G21
G90
M4
S0
G0 X10.000 Y10.000
M4 S800.0
G1 F1200.0
G1 X80.000
G1 Y80.000
G1 X10.000
G1 Y10.000

— Beispiel Scan 20 % @ 30 mm/s, lineSpacing 0,1 mm, bidi —
G0 X10.000 Y10.000
M4 S200.0
G1 F1800.0 X60.000
G0 Y10.100
G1 F1800.0 X10.000   ; alternierende Richtung (bidi)
…

— Beispiel Frame 5 % —
G0 X5.000 Y5.000
M4 S50.0
G1 F600.0 X85.000
G1 Y75.000
G1 X5.000
G1 Y5.000
M5

— Jobende —
M5
G0 X0.000 Y0.000
```

## 12. Offene Punkte (verifizieren)
| ID | Punkt | Weg |
|---|---|---|
| L1 | M4-Dynamik-Formel (Power ∝ Geschwindigkeit) exakt | Wiki „Grbl-v1.1-Laser-Mode“ lesen (laut `$32`-Doku dort dokumentiert) |
| L2 | Job-Ende LightBurn-Werkverhalten (Startpunkt vs. `$H`) | Serielles Log am Stand-PC mitschneiden (nur Beobachtung, kein Code-Mitnahme) |
| L3 | Exakte $30-Default (1000) und GRBL-Version des Elegoo | `$$`/Willkommensstring am Gerät lesen |
| L4 | error-Code-Tabelle vollständig | Wiki „Grbl-v1.1-Interface#error-codes“ |
| L5 | `$J`-Jogging-Syntax-Details | Wiki „Grbl-v1.1-Jogging“ |