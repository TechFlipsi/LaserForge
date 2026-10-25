# baute_test.ps1 - Baut LaserForge am Stand-PC und laesst die Tests laufen (ASCII-only, PowerShell 5.1).
# Aufruf: powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\Fabia\LaserForge\tools\qa\baute_test.ps1
# Logs:   C:\Users\Fabia\LaserForge\qa\build.log und qa\test.log
# Exit-Codes: 0 = Build + Test gruen ; 1 = Build fehlgeschlagen ; 2 = Test fehlgeschlagen ; 3 = Arbeitsumgebung fehlt

param(
    [string]$ProjektPath = "C:\Users\Fabia\LaserForge"
)

$ErrorActionPreference = "Continue"
$qaDir    = Join-Path $ProjektPath "qa"
$logBuild = Join-Path $qaDir "build.log"
$logTest  = Join-Path $qaDir "test.log"

if (-not (Test-Path $ProjektPath)) { Write-Output ("FEHLER: Projektordner fehlt: " + $ProjektPath); exit 3 }
if (-not (Test-Path (Join-Path $ProjektPath "LaserForge.sln"))) { Write-Output "FEHLER: LaserForge.sln fehlt"; exit 3 }
if (-not (Test-Path $qaDir)) { New-Item -ItemType Directory -Force -Path $qaDir | Out-Null }

# dotnet finden (nicht-interaktive SSH-Sitzung: PATH kann schmal sein)
$dot = $null
$cmdDot = Get-Command dotnet -ErrorAction SilentlyContinue
if ($cmdDot) { $dot = $cmdDot.Source }
if (-not $dot -or -not (Test-Path $dot)) {
    $kandidat = "C:\Program Files\dotnet\dotnet.exe"
    if (Test-Path $kandidat) { $dot = $kandidat }
}
if (-not $dot) { Write-Output "FEHLER: dotnet.exe nicht gefunden"; exit 3 }
Write-Output ("DOTNET: " + $dot)

# Schritt 0: laufende GUI-Instanz beenden - sonst sperrt die EXE die Ausgabedatei (LNK1104)
taskkill /F /IM LaserForge.Desktop.exe /T 2>$null | Out-Null
Start-Sleep -Milliseconds 500

Set-Location $ProjektPath

Write-Output "=== BUILD PROBE ==="
$buildAusgabe = & $dot build LaserForge.sln --nologo 2>&1
$buildCode = $LASTEXITCODE
$buildAusgabe | Out-File -FilePath $logBuild -Encoding utf8
Write-Output ("BUILD exit=" + $buildCode + " log=" + $logBuild)
foreach ($z in (Get-Content $logBuild -Tail 4)) { Write-Output ("  build.log| " + $z) }
if ($buildCode -ne 0) { Write-Output "ERGEBNIS: BUILD FEHLGESCHLAGEN"; exit 1 }

Write-Output "=== TEST PROBE ==="
$testProjekt = "tests\LaserForge.Tests\LaserForge.Tests.csproj"
$testAusgabe = & $dot test $testProjekt --no-build --nologo 2>&1
$testCode = $LASTEXITCODE
$testAusgabe | Out-File -FilePath $logTest -Encoding utf8
Write-Output ("TEST exit=" + $testCode + " log=" + $logTest)
foreach ($z in (Get-Content $logTest -Tail 6)) { Write-Output ("  test.log| " + $z) }
if ($testCode -ne 0) { Write-Output "ERGEBNIS: TEST FEHLGESCHLAGEN"; exit 2 }

Write-Output "ERGEBNIS: BAU+TEST GRUEN"
exit 0