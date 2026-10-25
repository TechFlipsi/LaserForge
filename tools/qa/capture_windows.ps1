# capture_windows.ps1 — Fenster-Liste + PNG-Capture eines Zielprozesses (Session-1-tauglich).
# Aufruf: powershell -NoProfile -ExecutionPolicy Bypass -File capture_windows.ps1 -ProcName lightburn -OutDir C:\Users\Fabia\.forge-qa\out
# Deutsch-kommentiert; ASCII-only (PowerShell 5.1 ANSI-Falle).
param(
    [string]$ProcName = "lightburn",
    [string]$OutDir = "C:\Users\Fabia\.forge-qa\out"
)

$ErrorActionPreference = "Continue"
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Force -Path $OutDir | Out-Null }

Add-Type -AssemblyName System.Drawing

Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public class WinCap {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lp);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lp);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder t, int c);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int a, out RECT r, int s);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll", SetLastError=true)] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    public struct RECT { public int L; public int T; public int R; public int B; }
}
"@

$ergebnis = New-Object System.Collections.ArrayList
$fenster = New-Object System.Collections.ArrayList

$cb = [WinCap+EnumWindowsProc]{
    param($hWnd, $lp)
    $prId = 0
    [void][WinCap]::GetWindowThreadProcessId($hWnd, [ref]$prId)
    if ($prId -ne 0) {
        $p = Get-Process -Id $prId -ErrorAction SilentlyContinue
        if ($p -and ($p.ProcessName -match $ProcName)) {
            $sb = New-Object System.Text.StringBuilder 512
            [void][WinCap]::GetWindowText($hWnd, $sb, 512)
            $titel = $sb.ToString()
            $rect = New-Object WinCap+RECT
            [void][WinCap]::GetWindowRect($hWnd, [ref]$rect)
            $dwr = New-Object WinCap+RECT
            $dwmOk = ([WinCap]::DwmGetWindowAttribute($hWnd, 9, [ref]$dwr, 16) -eq 0)
            $sichtbar = [WinCap]::IsWindowVisible($hWnd)
            $minimal = [WinCap]::IsIconic($hWnd)
            $r = $rect
            if ($dwmOk -and ($dwr.R - $dwr.L) -gt 0) { $r = $dwr }
            [void]$fenster.Add(@{ hWnd = $hWnd; Titel = $titel; Sichtbar = $sichtbar; Minimiert = $minimal; L = $r.L; T = $r.T; R = $r.R; B = $r.B })
        }
    }
    return $true
}
[void][WinCap]::EnumWindows($cb, [IntPtr]::Zero)

function Capture-Fenster {
    param($f, $procName, $outDir, $index)
    $w = $f.R - $f.L
    $h = $f.B - $f.T
    [void]$ergebnis.Add("fenster $index`: hwnd=$($f.hWnd) titel='$($f.Titel)' sichtbar=$($f.Sichtbar) minimiert=$($f.Minimiert) groesse=${w}x${h}")
    if ($f.Minimiert) {
        [void][WinCap]::ShowWindow($f.hWnd, 9)  # SW_RESTORE
        Start-Sleep -Milliseconds 500
        [void][WinCap]::SetForegroundWindow($f.hWnd)
        Start-Sleep -Milliseconds 300
        $n = New-Object WinCap+RECT
        [void][WinCap]::DwmGetWindowAttribute($f.hWnd, 9, [ref]$n, 16)
        if (($n.R - $n.L) -gt 0) { $f.L = $n.L; $f.T = $n.T; $f.R = $n.R; $f.B = $n.B }
        $w = $f.R - $f.L; $h = $f.B - $f.T
        [void]$ergebnis.Add("wiederhergestellt: groesse=${w}x${h}")
    }
    if ($w -gt 40 -and $h -gt 40) {
        $bmp = New-Object System.Drawing.Bitmap($w, $h)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $hdc = $g.GetHdc()
        $okPrint = [WinCap]::PrintWindow($f.hWnd, $hdc, 2)  # 2 = PW_RENDERFULLCONTENT
        $g.ReleaseHdc($hdc)
        $g.Dispose()
        $pfad = Join-Path $outDir ("{0}_{1}_{2}.png" -f $procName, $index, $f.hWnd)
        $bmp.Save($pfad, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        [void]$ergebnis.Add("gespeichert: $pfad printwindow=$okPrint")
    }
}

$i = 0
$hatSichtbares = $false
foreach ($f in $fenster) {
    if ($f.Sichtbar -or $f.Minimiert) {
        Capture-Fenster $f $ProcName $OutDir $i
        if (($f.R - $f.L) -gt 40) { $hatSichtbares = $true }
    }
    $i++
}

# Desktop-Fallback: gesamter virtueller Bildschirm (unabhaengig vom Zielprozess)
try {
    $bildschirm = [System.Windows.Forms.Screen]::AllScreens 2>$null
} catch {}
Add-Type -AssemblyName System.Windows.Forms 2>$null | Out-Null
try {
    $vx = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $dbmp = New-Object System.Drawing.Bitmap($vx.Width, $vx.Height)
    $dg = [System.Drawing.Graphics]::FromImage($dbmp)
    $dg.CopyFromScreen($vx.X, $vx.Y, 0, 0, $dbmp.Size)
    $dg.Dispose()
    $dpfad = Join-Path $OutDir "desktop.png"
    $dbmp.Save($dpfad, [System.Drawing.Imaging.ImageFormat]::Png)
    $dbmp.Dispose()
    [void]$ergebnis.Add("desktop_gespeichert: $dpfad ($($vx.Width)x$($vx.Height))")
} catch { [void]$ergebnis.Add("desktop_fehler: $($_.Exception.Message)") }

if ($fenster.Count -eq 0) { [void]$ergebnis.Add("KEIN Fenster fuer Prozess '$ProcName' gefunden.") }
$ergebnis | Out-File -FilePath (Join-Path $OutDir "winlist.txt") -Encoding ascii
Write-Output ("FENSTER_GEGEBEN=" + $fenster.Count + " BEWIES=$(($ergebnis | Out-String))")