<#
    A headless screenshot from Unity.

    How it differs from run.ps1: there is NO -nographics flag. That flag turns
    rendering off completely and the screenshot comes out empty. In batch mode
    the graphics context stays open and the drawing goes to a RenderTexture.

    The -Grep default is A CONTRACT with the log lines of
    unity/Assets/Lokanta/Editor/*.cs. Those files now log in English, so the
    English markers lead the alternation:

        "=== Lokanta"  the report header      (unchanged)
        "written"      <- "yazildi"
        "MEASURED"     <- "OLCUM"
        "size"         <- "boyut"
        "done ==="     <- "tamam ==="
        "PROBLEMS"     <- "SORUNLAR"

    THE TURKISH ALTERNATIVES STAY. unity/Assets/Lokanta/Game/*.cs still prints
    "SORUNLAR" (Autopilot, RestaurantView, CookRoutine) and it is those lines
    the fatal check below has to catch. Dropping them would turn this gate into
    a no-op that always passes.
#>
param(
    [string]$Method = "Lokanta.EditorTools.SceneShot.Capture",
    [string]$Editor = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe",
    [string]$ProjectPath = "D:\ClaudeCodeProjects\MobilOyun\unity",
    [int]$TimeoutSec = 900,
    [string]$Grep = "=== Lokanta|written|MEASURED|size|done ===|PROBLEMS|yazildi|OLCUM|boyut|tamam ===|SORUNLAR"
)

$ErrorActionPreference = "Stop"
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$LogPath = Join-Path $env:TEMP "unity_shot_$stamp.log"
$WarmLog = Join-Path $env:TEMP "unity_warm_$stamp.log"

# --- Warm-up run ------------------------------------------------------------
# Unity can run -executeMethod BEFORE the script compilation has finished. On
# 10 September it did exactly that: the log says "Requested script
# compilation", the DLL timestamp points to the end of the run, but the code
# that ran was the old version and the fixes did not show up in the image.
#
# The first run only compiles and exits; the second runs against the current
# build.
Write-Output "Warm-up run (compiling)..."
$warm = Start-Process -FilePath $Editor -NoNewWindow -PassThru -ArgumentList @(
    "-batchmode", "-quit", "-nographics", "-projectPath", $ProjectPath,
    "-logFile", $WarmLog)
if (-not $warm.WaitForExit(600 * 1000)) { try { $warm.Kill() } catch {} }
$warm.WaitForExit()

$warmErrors = Select-String -Path $WarmLog -Pattern "error CS\d+" -ErrorAction SilentlyContinue |
              ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
if ($warmErrors.Count -gt 0) {
    Write-Output ""
    Write-Output "=== COMPILE ERROR ==="
    $warmErrors | Select-Object -First 12 | ForEach-Object { Write-Output "  $_" }
    exit 1
}

$unityArgs = @("-batchmode", "-quit", "-projectPath", $ProjectPath,
               "-executeMethod", $Method, "-logFile", $LogPath)

Write-Output "Method : $Method"
Write-Output "Log    : $LogPath"
Write-Output "---"

$sw = [Diagnostics.Stopwatch]::StartNew()
$proc = Start-Process -FilePath $Editor -ArgumentList $unityArgs -NoNewWindow -PassThru
if (-not $proc.WaitForExit($TimeoutSec * 1000)) {
    try { $proc.Kill() } catch {}
    Write-Output "TIMEOUT"
    exit 3
}
$proc.WaitForExit()
$sw.Stop()

Write-Output ("Duration : {0:N1} s" -f $sw.Elapsed.TotalSeconds)

$compileErrors = Select-String -Path $LogPath -Pattern "error CS\d+" |
                 ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
if ($compileErrors.Count -gt 0) {
    Write-Output ""
    Write-Output "=== COMPILE ERROR ==="
    $compileErrors | Select-Object -First 12 | ForEach-Object { Write-Output "  $_" }
    exit 1
}

Select-String -Path $LogPath -Pattern $Grep |
    ForEach-Object { Write-Output ("  " + $_.Line.Trim()) }

# "PROBLEMS" is what Editor/*.cs prints now; "SORUNLAR" is what Game/*.cs still
# prints. Both have to be here or the gate stops finding anything.
$fatal = Select-String -Path $LogPath -Pattern "PROBLEMS|SORUNLAR|Fatal Error"
if ($fatal.Count -gt 0) { exit 2 }

Write-Output "OK"
exit 0
