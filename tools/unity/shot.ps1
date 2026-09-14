<#
    Unity'den bassiz ekran goruntusu.

    run.ps1'den farki: -nographics bayragi YOK. O bayrak render'i tamamen
    kapatiyor ve ekran goruntusu bos cikiyor. Toplu kipte grafik baglami
    acik kaliyor, cizim RenderTexture'a yapiliyor.
#>
param(
    [string]$Method = "Lokanta.EditorTools.SceneShot.Capture",
    [string]$Editor = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe",
    [string]$ProjectPath = "D:\ClaudeCodeProjects\MobilOyun\unity",
    [int]$TimeoutSec = 900,
    [string]$Grep = "=== Lokanta|yazildi|OLCUM|boyut|tamam ===|SORUNLAR"
)

$ErrorActionPreference = "Stop"
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$LogPath = Join-Path $env:TEMP "unity_shot_$stamp.log"
$WarmLog = Join-Path $env:TEMP "unity_warm_$stamp.log"

# --- Isinma turu ------------------------------------------------------------
# Unity, -executeMethod'u betik derlemesi BITMEDEN calistirabiliyor. 10 Eylul'de
# tam bunu yapti: gunluk "Requested script compilation" yaziyor, DLL zaman
# damgasi kosunun sonunu gosteriyor, ama calisan kod eski surumdu ve
# duzeltmeler goruntuye yansimadi.
#
# Ilk tur yalnizca derliyor ve cikiyor; ikinci tur guncel derlemeyle calisiyor.
Write-Output "Isinma turu (derleme)..."
$warm = Start-Process -FilePath $Editor -NoNewWindow -PassThru -ArgumentList @(
    "-batchmode", "-quit", "-nographics", "-projectPath", $ProjectPath,
    "-logFile", $WarmLog)
if (-not $warm.WaitForExit(600 * 1000)) { try { $warm.Kill() } catch {} }
$warm.WaitForExit()

$warmErrors = Select-String -Path $WarmLog -Pattern "error CS\d+" -ErrorAction SilentlyContinue |
              ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
if ($warmErrors.Count -gt 0) {
    Write-Output ""
    Write-Output "=== DERLEME HATASI ==="
    $warmErrors | Select-Object -First 12 | ForEach-Object { Write-Output "  $_" }
    exit 1
}

$unityArgs = @("-batchmode", "-quit", "-projectPath", $ProjectPath,
               "-executeMethod", $Method, "-logFile", $LogPath)

Write-Output "Metot  : $Method"
Write-Output "Gunluk : $LogPath"
Write-Output "---"

$sw = [Diagnostics.Stopwatch]::StartNew()
$proc = Start-Process -FilePath $Editor -ArgumentList $unityArgs -NoNewWindow -PassThru
if (-not $proc.WaitForExit($TimeoutSec * 1000)) {
    try { $proc.Kill() } catch {}
    Write-Output "ZAMAN ASIMI"
    exit 3
}
$proc.WaitForExit()
$sw.Stop()

Write-Output ("Sure : {0:N1} sn" -f $sw.Elapsed.TotalSeconds)

$compileErrors = Select-String -Path $LogPath -Pattern "error CS\d+" |
                 ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
if ($compileErrors.Count -gt 0) {
    Write-Output ""
    Write-Output "=== DERLEME HATASI ==="
    $compileErrors | Select-Object -First 12 | ForEach-Object { Write-Output "  $_" }
    exit 1
}

Select-String -Path $LogPath -Pattern $Grep |
    ForEach-Object { Write-Output ("  " + $_.Line.Trim()) }

$fatal = Select-String -Path $LogPath -Pattern "SORUNLAR|Fatal Error"
if ($fatal.Count -gt 0) { exit 2 }

Write-Output "TAMAM"
exit 0
