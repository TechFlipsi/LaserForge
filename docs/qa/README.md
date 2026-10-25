# LaserForge M5-QA-Harniss (automatisierte Bau-/Test-/GUI-Capture-Suite)

Ziel: Jeder GUI-/Code-Zyklus lässt sich mit **einem** Skript bauen, testen, launchen und capturen — alle Schritte mit echtem Proof (Logs + Screenshots), Berichte auf Deutsch.

## Aufbau

| Datei | Ort | Aufgabe |
|---|---|---|
| `tools/qa/lauf.sh` | kibot (dieser Rechner) | Orchestrator: scp → bauen+testen → GUI-Start → Capture → PNG-Holung → Zeilen-Report |
| `tools/qa/baute_test.ps1` | Stand-PC | `dotnet build` (Solution) + `dotnet test` (Tests-Projekt), Logs + Exit-Codes |
| `tools/qa/gui_start.ps1` | Stand-PC | Alte GUI-Instanz beenden + LaserForge.Desktop.exe sichtbar in Session 1 starten (schtasks-Rezept) |
| `tools/qa/gui_capture.ps1` | Stand-PC | Capture-Task starten (nutzt die bestehende `C:\Users\Fabia\.forge-qa\capture_windows.ps1`, wird nicht verändert) und auf frische PNGs warten |
| `tools/qa/capture_windows.ps1` | Stand-PC `.forge-qa` | Bereits vorhanden: Fenster-Liste + PrintWindow-Capture (PW_RENDERFULLCONTENT) + Desktop-Fallback |
| `docs/qa/mein-gui/` | kibot | Hergeholte PNG-Beweise + `winlist.txt` je Lauf |

## Der Zyklus (ein Befehl)

```bash
./tools/qa/lauf.sh            # Zielordner für PNGs optional: ./tools/qa/lauf.sh [ordner]
```

Ablauf:
1. Die drei QA-Skripte per scp nach `stand-pc:C:/Users/Fabia/LaserForge/tools/qa/` (die vorhandene `capture_windows.ps1` wird dabei **nicht** angefasst).
2. `baute_test.ps1` am Stand-PC: Build + Tests.
3. Bei Fehlschlag: Log-Schwänze von `build.log`/`test.log` ausgeben, Abbruch mit Exit 2.
4. `gui_start.ps1`: Task `forge_lf` anlegen/run (`/RU Fabia /IT` → Session 1 am Desktop), PID + Session prüfen, Task aufräumen.
5. 12 s WaitForLoad (Avalonia-Fensteraufbau).
6. `gui_capture.ps1`: Task `forge_cap` mit `powershell -WindowStyle Hidden …`, 40 s Polling auf frische PNGs, `winlist.txt` ausgeben.
7. PNGs (+ `winlist.txt`) per scp nach `docs/qa/mein-gui/`, Zeilen-Report (deutsch).

Exit-Codes `lauf.sh`: `0` = Zyklus grün · `2` = Build/Test fehlgeschlagen · `3` = GUI-Kette fehlgeschlagen (Start oder Capture).

## Beweis-Ablage

| Beweis | Pfad |
|---|---|
| Build-Log | `stand-pc: C:\Users\Fabia\LaserForge\qa\build.log` |
| Test-Log | `stand-pc: C:\Users\Fabia\LaserForge\qa\test.log` |
| Fenster-PNG (verdeckungsunabhängig via PrintWindow) | `docs/qa/mein-gui/LaserForge.Desktop_<index>_<hwnd>.png` |
| Desktop-PNG (Fallback, ganzer Bildschirm) | `docs/qa/mein-gui/desktop.png` |
| Fenster-Liste | `docs/qa/mein-gui/winlist.txt` |
| PNGs am Stand-PC | `C:\Users\Fabia\.forge-qa\out\` |

Nach dem Lauf prüft das Parent (Vision-Angleich) die PNGs: Fensterinhalt gegen Erwartung (Titelzeile, Text, Layout) — kein leerer Screenshot akzeptiert.

## Einzelne Baustufen (ohne Orchestrator)

```bash
# nur bauen+testen:
ssh stand-pc "powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\Fabia\LaserForge\tools\qa\baute_test.ps1"
# nur GUI starten:
ssh stand-pc "powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\Fabia\LaserForge\tools\qa\gui_start.ps1"
# nur Capture:
ssh stand-pc "powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\Fabia\LaserForge\tools\qa\gui_capture.ps1"
```

## Exit-Codes der PowerShell-Skripte

- `baute_test.ps1`: `0` grün · `1` Build-Fehler · `2` Test-Fehler · `3` Arbeitsumgebung fehlt
- `gui_start.ps1`: `0` gelungen · `3` EXE fehlt · `4` schtasks-Fehler · `5` Prozess nicht in Session 1
- `gui_capture.ps1`: `0` neue PNGs · `2` kein PNG · `3` Capture-Skript fehlt · `4` schtasks-Fehler

## Fallstricke (eingebaut, nicht entfernen!)

- **ASCII-only in allen `.ps1`**: PowerShell 5.1 liest Dateien im ANSI-Codepage-Kontext — Umlaute im Skripttext zerbrechen das Skript. Deutsche Texte im Skript nur als ae/oe/ue.
- **`/RU Fabia /IT` bei jedem schtasks**: Ohne `/RU` läuft der Task in Session 0 — Fenster unsichtbar UND die EXE bleibt gelockt (LNK1104 beim nächsten Build). `gui_start.ps1` killt die alte Instanz vor dem Build (siehe `baute_test` Schritt 0, taskkill).
- **`-WindowStyle Hidden` beim Capture-Task**: Sonst verdeckt die Task-Konsole genau das QA-Ziel im Desktop-Fallback.
- **Fenster-PNG vor Desktop-PNG glauben**: `PrintWindow` ist gegen Verdeckung/Minimierungs-Restore robust (`printwindow=True` in winlist.txt ist das Merkmal); CopyFromScreen ist nur Fallback.
- `capture_windows.ps1` existiert am Stand-PC bereits und wird nie überschrieben; nur verwendet.

## Probenlauf (25.10.2026, Beweise im Repo)

- Build: **0 Warnungen, 0 Fehler** (`build.log` exit=0)
- Tests: **36/36 bestanden** (`test.log`, „Bestanden! - Fehler: 0, erfolgreich: 36 …")
- GUI: `GUI_GESTARTET: pid=20720 session=1` → `tasklist` bestätigte LaserForge.Desktop.exe in Session 1
- Capture: `LaserForge.Desktop_1_23662172.png` (Fenster 1602×932, printwindow=True, Inhalt gerendert) + `desktop.png` (1920×1080) + `winlist.txt`

Offene Punkte: Auf dem Desktop lag/liegt ein fremdes Konsole-Fenster (cmd.exe, Titel „C:\WINDOWS\System32\Wbem") im Vordergrund — nicht aus dem QA-Zyklus (meine Task-Instanzen sind beendet), wurde nicht angetastet (Killen von unbekannten Session-1-Prozessen ist zu riskant). Für den Desktop-Fallback-PNG kann es davor liegen; das Fenster-PNG bleibt verdeckungsunabhängig.