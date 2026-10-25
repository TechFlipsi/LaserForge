# gui_start.ps1 - Beendet alte GUI-Instanz und startet LaserForge.Desktop sichtbar in Session 1.
# ASCII-only (PowerShell 5.1 ANSI-Falle: keine Umlaute in Skriptdateien).
# Rezept: schtasks /Create /TN forge_lf /SC ONCE /ST 23:59 /TR "\"<exe>\"" /F /RU Fabia /IT  ;  schtasks /Run /TN forge_lf
# Wichtig: /RU Fabia /IT - sonst laeuft der Task in Session 0 (unsichtbar + EXE gesperrt).
# Exit: 0 = gelungen ; 3 = EXE fehlt ; 4 = schtasks-Fehler ; 5 = Prozess nicht in Session 1

param([string]$ExePath = "")

$ErrorActionPreference = "Continue"
$procName = "LaserForge.Desktop"
$taskName = "forge_lf"

# 1) alte Instanz beenden (inkl. Session-0-Zombies)
taskkill /F /IM ($procName + ".exe") /T 2>$null | Out-Null
Start-Sleep -Milliseconds 500

# 2) EXE finden: Standard-Pfad, sonst neuste unter bin\ (Fallback)
if ([string]::IsNullOrWhiteSpace($ExePath)) {
    $basis = "C:\Users\Fabia\LaserForge\src\LaserForge.Desktop\bin\Debug\net10.0"
    $verdacht = Join-Path $basis ($procName + ".exe")
    if (Test-Path $verdacht) {
        $ExePath = $verdacht
    } else {
        $treffer = Get-ChildItem -Path "C:\Users\Fabia\LaserForge\src\LaserForge.Desktop\bin" -Recurse -Filter ($procName + ".exe") -ErrorAction SilentlyContinue |
                   Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($treffer) { $ExePath = $treffer.FullName }
    }
}
if (-not ($ExePath -and (Test-Path $ExePath))) {
    Write-Output ("GUI_FEHLER: " + $procName + ".exe fehlt, gesucht=" + $ExePath)
    exit 3
}
Write-Output ("EXE: " + $ExePath)

# 3) Task anlegen und starten
& schtasks /Create /TN $taskName /SC ONCE /ST 23:59 /TR ('"' + $ExePath + '"') /F /RU Fabia /IT | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Output ("GUI_FEHLER: schtasks /Create exit=" + $LASTEXITCODE); exit 4 }

& schtasks /Run /TN $taskName | Out-Null
$runCode = $LASTEXITCODE
if ($runCode -ne 0) {
    Write-Output ("GUI_FEHLER: schtasks /Run exit=" + $runCode)
    & schtasks /Delete /TN $taskName /F | Out-Null
    exit 4
}

# 4) Warten bis der Prozesseintrag da ist, Session belegen, Task aufraeumen
$pidGef = 0
$sessionGef = -1
for ($i = 0; $i -lt 10; $i++) {
    Start-Sleep -Seconds 1
    $p = Get-Process -Name $procName -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) { $pidGef = $p.Id; $sessionGef = $p.SessionId; break }
}
& schtasks /Delete /TN $taskName /F | Out-Null

if ($sessionGef -lt 0) { Write-Output "GUI_FEHLER: Prozess startete nicht (Aufbau abgebrochen?)"; exit 5 }
Write-Output ("GUI_GESTARTET: pid=" + $pidGef + " session=" + $sessionGef)
if ($sessionGef -ne 1) { Write-Output "GUI_FEHLER: Prozess NICHT in Session 1 (Desktop)"; exit 5 }

Write-Output "GUI_OK"
exit 0