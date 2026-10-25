#!/usr/bin/env bash
# lauf.sh - Orchestrator des M5-QA-Zyklus: bauen + testen, GUI starten, Capture holen (laeuft auf kibot).
# Nutzt: SSH-Alias stand-pc (User Fabia, Key-Auth). Alle .ps1 bleiben ASCII-only (PowerShell 5.1 ANSI-Falle).
# Zyklus: scp -> baute_test.ps1 -> bei Fehler Log-Schwanz + exit 2 -> gui_start.ps1 -> sleep 12
#         -> gui_capture.ps1 -> PNGs per scp nach docs/qa/mein-gui -> Zeilen-Report.
# Exit: 0 = Zyklus gruen ; 2 = Bau/Test fehlgeschlagen ; 3 = GUI-Kette fehlgeschlagen
# Aufruf: ./lauf.sh [zielordner-fuer-pngs]
set -u

wurzel="$(cd "$(dirname "$0")/../../" && pwd)"     # skript liegt in tools/qa -> wurzel = /root/laserforge
ssh_cmd="ssh -o BatchMode=yes -o ConnectTimeout=15 stand-pc"
qa_proc="LaserForge.QA"
fern_ps="powershell -NoProfile -ExecutionPolicy Bypass -File"
fern_qa="C:\\Users\\Fabia\\LaserForge\\tools\\qa"
fern_log="C:\\Users\\Fabia\\LaserForge\\qa"
fern_cap="C:/Users/Fabia/.forge-qa"
ziel_pngs="${1:-$wurzel/docs/qa/mein-gui"

echo "=== LASERFORGE M5-QA: AUTOMATIK-ZYKLUS ($(date '+%Y-%m-%d %H:%M:%S')) ==="

# --- 1/6: QA-Skripte uebertragen (capture_windows.ps1 NICHT anfassen - dort schon vorhanden) ---
echo "### 0/6 QUELLE-Quersync: src/tests/tools + Projektdateien (immer frisch!)"
scp -q -r "$wurzel/src" "$wurzel/tests" "$wurzel/tools" "stand-pc:C:/Users/Fabia/LaserForge/" \
    || { echo "FEHLER: scp der Quelle gescheitert"; exit 3; }
scp -q "$wurzel/LaserForge.sln" "$wurzel/Directory.Build.props" "$wurzel/.gitignore" "$wurzel/README.md" "$wurzel/LICENSE" \
    "stand-pc:C:/Users/Fabia/LaserForge/" || { echo "FEHLER: scp Projektdateien gescheitert"; exit 3; }
echo "OK: Quellquelle frisch (src/tests/tools/sln/props)"

echo "### 1/6 QA-Skripte per scp zum Stand-PC"
$ssh_cmd "if not exist C:\\Users\\Fabia\\LaserForge\\tools\\qa\\ mkdir C:\\Users\\Fabia\\LaserForge\\tools\\qa" > /dev/null 2>&1
scp -q "$wurzel/tools/qa/baute_test.ps1" "$wurzel/tools/qa/gui_start.ps1" "$wurzel/tools/qa/gui_capture.ps1" \
    "stand-pc:C:/Users/Fabia/LaserForge/tools/qa/" || { echo "FEHLER: scp der QA-Skripte gescheitert"; exit 3; }
echo "OK: 3 Skripte auf fernem tools/qa (baute_test, gui_start, gui_capture)"

# --- 2/6: bauen + testen ---
echo "### 2/6 dotnet build + dotnet test am Stand-PC"
if ! $ssh_cmd "$fern_ps $fern_qa\\baute_test.ps1 -ProcName $qa_proc"; then
    echo "!!!! BAU/TEST GESCHEITERT - Log-Schwanz:"
    echo "--- build.log (letzte 30 Zeilen) ---"
    $ssh_cmd "type $fern_log\\build.log" 2>&1 | tail -n 30
    echo "--- test.log (letzte 20 Zeilen) ---"
    $ssh_cmd "type $fern_log\\test.log" 2>&1 | tail -n 20
    echo "=== ZYKLUS ABBRUCH (exit 2) ==="
    exit 2
fi

# --- 3/6: GUI starten ---
echo "### 3/6 LaserForge.Desktop starten (schtasks, Session 1)"
if ! $ssh_cmd "$fern_ps $fern_qa\\gui_start.ps1 -ProcName $qa_proc"; then
    echo "!!! GUI-START GESCHEITERT - Diagnose:"
    $ssh_cmd "tasklist /FI \"IMAGENAME eq LaserForge.Desktop.exe\" /FO CSV" 2>&1 | tail -n 5
    echo "=== ZYKLUS ABBRUCH (exit 3) ==="
    exit 3
fi

# --- 4/6: Fenster aufbauen lassen ---
echo "### 4/6 12 s warten (Avalonia-Fensteraufbau)"
sleep 12

# --- 5/6: Capture per Task ---
echo "### 5/6 Fenster-Capture per schtasks-Task"
if ! $ssh_cmd "$fern_ps $fern_qa\\gui_capture.ps1 -ProcName $qa_proc"; then
    echo "!!! CAPTURE GESCHEITERT - Diagnose (Verzeichnis out):"
    $ssh_cmd "dir C:\\Users\\Fabia\\.forge-qa\\out" 2>&1 | tail -n 10
    echo "=== ZYKLUS ABBRUCH (exit 3) ==="
    exit 3
fi

# --- 6/6: Beweise holen ---
echo "### 6/6 PNG-Beweise nach $ziel_pngs"
mkdir -p "$ziel_pngs"
scp -q "stand-pc:$fern_cap/out/*.png" "$ziel_pngs/" 2>/dev/null
scp -q "stand-pc:$fern_cap/out/winlist.txt" "$ziel_pngs/" 2>/dev/null || true
if ! ls "$ziel_pngs"/*.png > /dev/null 2>&1; then
    echo "HINWEIS: Sammel-scp leer - einzelnes Nachholen:"
    namen=$($ssh_cmd 'powershell -NoProfile -Command "(Get-ChildItem C:\Users\Fabia\.forge-qa\out -Filter *.png).Name"' 2>/dev/null | tr -d '\r')
    for n in $namen; do
        [ -n "$n" ] && scp -q "stand-pc:C:/Users/Fabia/.forge-qa/out/$n" "$ziel_pngs/$n" 2>/dev/null || true
    done
fi
if ! ls "$ziel_pngs"/*.png > /dev/null 2>&1; then
    echo "=== ZYKLUS ABBRUCH: keine PNG beim Stand-PC angekommen (exit 3) ==="
    exit 3
fi

# --- Zeilen-Report (deutsch) ---
echo "=== LAUF-BERICHT ==="
echo "1) Bauen:        OK (Log: C:\\Users\\Fabia\\LaserForge\\qa\\build.log)"
echo "2) Tests:        OK (Log: C:\\Users\\Fabia\\LaserForge\\qa\\test.log)"
echo "3) GUI-Start:    OK (LaserForge.Desktop.exe in Session 1, PID siehe oben)"
echo "4) Capture:      PNGs geholt nach $ziel_pngs :"
ls -l "$ziel_pngs" | grep -v '^total' | awk 'NF>=9 {print "   " $9 " (" $5 " Bytes)"}'
echo "5) Blick (Parent): PNGs per Vision pruefen - Fensterinhalt vs. Erwartung (M5-QA)"
echo "ZYKLUS OK"
exit 0