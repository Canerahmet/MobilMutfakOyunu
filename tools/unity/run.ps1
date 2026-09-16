<#
    Unity batch-mode runner
    ==========================================================================
    Why it exists: Unity -batchmode -quit returns 0 EVEN WHEN THERE IS A
    COMPILE ERROR. On 10 September 2026 ProjectSetup.cs produced four API
    errors, -executeMethod never ran, and the exit code was still 0. This
    script reads the log and returns the real result.

    Usage:
      .\tools\unity\run.ps1 -Method Lokanta.EditorTools.ProjectSetup.ApplyAll
      .\tools\unity\run.ps1 -Method ... -TimeoutSec 900
      .\tools\unity\run.ps1 -ExtraArgs @("-buildTarget","Android")

    Exit code: 0 success, 1 compile error, 2 Unity error, 3 timeout.
#>
param(
    [string]$Method = "",
    [string]$Editor = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe",
    [string]$ProjectPath = "D:\ClaudeCodeProjects\MobilOyun\unity",
    [string]$LogPath = "",
    [int]$TimeoutSec = 900,
    [string[]]$ExtraArgs = @()
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($LogPath)) {
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $LogPath = Join-Path $env:TEMP "unity_$stamp.log"
}
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

if (-not (Test-Path $Editor)) {
    Write-Output "ERROR: Unity not found: $Editor"
    exit 2
}

$unityArgs = @("-batchmode", "-quit", "-nographics", "-projectPath", $ProjectPath,
               "-logFile", $LogPath)
if ($Method -ne "") { $unityArgs += @("-executeMethod", $Method) }
$unityArgs += $ExtraArgs

Write-Output "Unity   : $Editor"
Write-Output "Project : $ProjectPath"
Write-Output "Method  : $(if ($Method -eq '') { '(none)' } else { $Method })"
Write-Output "Log     : $LogPath"
Write-Output "---"

# --- Warm-up run (compilation) ----------------------------------------------
# Unity can run -executeMethod BEFORE THE SCRIPT COMPILATION HAS FINISHED, and
# in that case the code that runs is the OLD version. shot.ps1 learned this on
# 10 September and added a warm-up run; it was not added to run.ps1, and on 12
# September exactly that happened: the interface was rewritten from scratch,
# the build said "fine", but the installed game showed the OLD interface. The
# build itself was silently stale - and nothing warned about it.
#
# The first run only compiles and exits; the second runs against the current
# build.
if ($Method -ne "") {
    $WarmLog = Join-Path $env:TEMP ("unity_warm_" + (Get-Date -Format "yyyyMMdd_HHmmss") + ".log")
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
        Write-Output "=== COMPILE ERROR ($($warmErrors.Count) unique) ==="
        $warmErrors | Select-Object -First 20 | ForEach-Object { Write-Output "  $_" }
        exit 1
    }
}

$sw = [Diagnostics.Stopwatch]::StartNew()
$proc = Start-Process -FilePath $Editor -ArgumentList $unityArgs -NoNewWindow -PassThru
if (-not $proc.WaitForExit($TimeoutSec * 1000)) {
    try { $proc.Kill() } catch {}
    $sw.Stop()
    Write-Output "TIMEOUT: $TimeoutSec s elapsed"
    exit 3
}
# WaitForExit with a timeout does not always populate ExitCode; we call it
# without a parameter to wait for the process to close completely, and read
# the code after that.
$proc.WaitForExit()
$sw.Stop()
$unityExit = $proc.ExitCode
if ($null -eq $unityExit) { $unityExit = 0 }

Write-Output ("Unity exit code : {0}" -f $unityExit)
Write-Output ("Duration        : {0:N1} s" -f $sw.Elapsed.TotalSeconds)

if (-not (Test-Path $LogPath)) {
    Write-Output "ERROR: the log file was not created"
    exit 2
}

# --- Compile errors ---------------------------------------------------------
$compileErrors = Select-String -Path $LogPath -Pattern "error CS\d+" |
                 ForEach-Object { $_.Line.Trim() } |
                 Sort-Object -Unique

if ($compileErrors.Count -gt 0) {
    Write-Output ""
    Write-Output "=== COMPILE ERROR ($($compileErrors.Count) unique) ==="
    $compileErrors | Select-Object -First 20 | ForEach-Object { Write-Output "  $_" }
    exit 1
}

# --- The script's own report ------------------------------------------------
# CONTEXT 400, NOT 60.
#
# The tour prints 107 checks; with -Context 0, 60 only half the log reached the
# screen and the rest WAS NEVER READ. Truncating the output of a measurement
# tool is the same thing as not looking at what it measured.
$report = Select-String -Path $LogPath -Pattern "=== Lokanta" -Context 0, 400
if ($report) {
    Write-Output ""
    $report[0].Context.PostContext | Where-Object { $_ -match "^\s{2}\S" -or $_ -match "^===" } |
        ForEach-Object { Write-Output $_ }
}

# --- Errors from the Unity side ---------------------------------------------
# THE "HATA  :" PATTERN IS FATAL TOO.
#
# The tour's Note() call writes red checks as "HATA  : ...", but that pattern
# was not in the list: even if all 107 checks failed the script still returned
# 0. The tour itself now returns a non-zero code; this line is the second gate
# - whoever reads the log should see it as well.
#
# THE PATTERNS ARE A CONTRACT WITH THE C# THAT WRITES THESE LINES.
#
# unity/Assets/Lokanta/Editor/*.cs now logs in English and prints "PROBLEMS:"
# where it used to print "SORUNLAR:". unity/Assets/Lokanta/Game/*.cs still
# prints "SORUNLAR:" (Autopilot, RestaurantView, CookRoutine) and the tour's
# Note() still writes its red checks as "HATA  : ...", so BOTH SPELLINGS STAY.
# Dropping either one would quietly stop this gate from finding anything.
$fatal = Select-String -Path $LogPath -Pattern "PROBLEMS:|SORUNLAR:|HATA  :|Fatal Error|Aborting batchmode" |
         ForEach-Object { $_.Line.Trim() }
if ($fatal.Count -gt 0) {
    Write-Output ""
    Write-Output "=== PROBLEM ==="
    $fatal | Select-Object -First 10 | ForEach-Object { Write-Output "  $_" }
    exit 2
}

if ($unityExit -ne 0) {
    Write-Output "Unity returned a non-zero code"
    exit 2
}

Write-Output ""
Write-Output "OK"
exit 0
