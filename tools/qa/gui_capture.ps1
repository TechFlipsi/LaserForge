# gui_capture.ps1 - Startet das Fenster-Capture als Session-1-Task und wartet auf frische PNGs.
# ASCII-only (PowerShell 5.1 ANSI-Falle). Nutzt die VORHANDENE Datei
# C:\Users\Fabia\.forge-qa\capture_windows.ps1 - wird NICHT veraendert.
# Rezept: schtasks /Create /TN forge_cap /SC ONCE /ST 23:59 /TR "powershell -WindowStyle Hidden -NoProfile -ExecutionPolicy Bypass -File <script>" /F /RU Fabia /IT ; schtasks /Run /TN forge_cap
# Wichtig: ohne -WindowStyle Hidden verdeckt die Task-Konsole genau das QA-Ziel.
# Exit: 0 = neue PNGs da ; 2 = kein PNG entstanden ; 3 = Capture-Skript fehlt ; 4 = schtasks-Fehler

param(
    [string]$ProcName = "LaserForge.QA",
    [string]$CaptureScript = "C:\Users\Fabia\.forge-qa\capture_windows.ps1",
    [string]$OutDir = "C:\Users\Fabia\.forge-qa\out",
    [int]$WarteSek = 40
)

$ErrorActionPreference = "Continue"
$taskName = "forge_cap_" + $ProcName

if (-not (Test-Path $CaptureScript)) { Write-Output ("CAPTURE_FEHLT: " + $CaptureScript); exit 3 }
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Force -Path $OutDir | Out-Null }

# alte Proben wegraeumen, damit nur frische Beweise zaehlen
Get-ChildItem -Path $OutDir -Filter *.png -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

# Task mit dem vorhandenen Capture-Skript anlegen und starten (Session 1, ohne Konsolenfenster)
$tr = "powershell -WindowStyle Hidden -NoProfile -ExecutionPolicy Bypass -File " + $CaptureScript + " -ProcName " + $ProcName

& schtasks /Create /TN $taskName /SC ONCE /ST 23:59 /TR $tr /F /RU Fabia /IT | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Output ("CAPTURE_FEHLER: schtasks /Create exit=" + $LASTEXITCODE); exit 4 }

& schtasks /Run /TN $taskName | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Output ("CAPTURE_FEHLER: schtasks /Run exit=" + $LASTEXITCODE)
    & schtasks /Delete /TN $taskName /F | Out-Null
    exit 4
}

# auf frische PNGs warten (Fenster-PNG + desktop.png erwartet)
$startZeit = (Get-Date).AddSeconds(-2)
$frisch = @()
$deadline = (Get-Date).AddSeconds($WarteSek)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    $frisch = @(Get-ChildItem -Path $OutDir -Filter *.png -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -ge $startZeit })
    if ($frisch.Count -ge 2) { break }
}
& schtasks /Delete /TN $taskName /F | Out-Null

Write-Output ("PNG_FRISCH=" + $frisch.Count)
foreach ($f in $frisch) { Write-Output ("PNG: " + $f.Name + " (" + [int]($f.Length / 1024) + " KB)") }

$winlist = Join-Path $OutDir "winlist.txt"
if (Test-Path $winlist) {
    Write-Output "--- winlist.txt ---"
    foreach ($z in (Get-Content $winlist)) { Write-Output ("winlist| " + $z) }
}

if ($frisch.Count -ge 1) { Write-Output "CAPTURE_OK"; exit 0 }
Write-Output "CAPTURE_LEER"
exit 2