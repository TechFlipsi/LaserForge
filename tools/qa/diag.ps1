# diag.ps1 — Sitzungs- und Fensterdiagnose (Session 1, ASCII-only).
# Ziel: Beweis-Fakten ueber LightBurn-Fensterzustand und Desktop-Capability in dieser Sitzung.
$ErrorActionPreference = "Continue"
$out = "C:\Users\Fabia\.forge-qa\out\diag.txt"
Remove-Item $out -ErrorAction SilentlyContinue

"whoami: $(whoami)" | Out-File $out -Encoding ascii -Append
"zeit: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File $out -Encoding ascii -Append
"session: $(Get-Process -Id $PID | Select-Object -ExpandProperty SessionId)" | Out-File $out -Encoding ascii -Append

try {
    Add-Type -AssemblyName System.Windows.Forms
    "forms: OK" | Out-File $out -Encoding ascii -Append
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    "primary_bounds: $($b.Width)x$($b.Height)" | Out-File $out -Encoding ascii -Append
} catch {
    "forms: FEHLER $($_.Exception.Message)" | Out-File $out -Encoding ascii -Append
}

"--- LightBurn-Prozess-Fakten ---" | Out-File $out -Encoding ascii -Append
Get-Process LightBurn -ErrorAction SilentlyContinue | ForEach-Object {
    "lb pid=$($_.Id) sess=$($_.SessionId) handle=$($_.MainWindowHandle) threads=$($_.Threads.Count) cpu=$([math]::Round($_.CPU,1)) memMB=$([int]($_.WorkingSet64/1MB)) start=$($_.StartTime)" | Out-File $out -Encoding ascii -Append
}

"--- Prozesse mit Hauptfenster (Stichprobe, Session-Matching) ---" | Out-File $out -Encoding ascii -Append
Get-Process | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object Id, ProcessName, MainWindowTitle | ForEach-Object {
    "win: pid=$($_.Id) proc=$($_.ProcessName) titel='$($_.MainWindowTitle)'" | Out-File $out -Encoding ascii -Append
} | Out-Null

"--- Desktop-CopyFromScreen-Test ---" | Out-File $out -Encoding ascii -Append
try {
    Add-Type -AssemblyName System.Drawing
    $b2 = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap($b2.Width, $b2.Height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen(0, 0, 0, 0, $bmp.Size)
    $g.Dispose()
    $dp = "C:\Users\Fabia\.forge-qa\out\diag_desktop.png"
    $bmp.Save($dp, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    "desktop_copy: GESPEICHERT $dp" | Out-File $out -Encoding ascii -Append
} catch {
    "desktop_copy: FEHLER $($_.Exception.Message)" | Out-File $out -Encoding ascii -Append
}
"kopf_alle_schluessel:" | Out-File $out -Encoding ascii -Append