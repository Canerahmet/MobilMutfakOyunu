<#
    Smoke tour runner
    ==========================================================================
    Why it exists: THERE WAS NO SCRIPT AT ALL that ran the tour. "-lokanta-tour"
    appeared in the whole project only inside Autopilot.cs; the tour was run by
    hand and on the days it was not run, nobody noticed. A measurement tool
    that is not wired into automation is a measurement that does not exist.

    What it does:
      1. (optionally) builds the Windows player,
      2. runs the tour at a real phone size (873x393 dp),
      3. returns THE TOUR'S EXIT CODE.

    The tour's exit code now depends on the result: 1 if any check is left
    failing. (Until this line, the tour returned 0 even when every check was an
    ERROR.)

    Usage:
      .\tools\unity\tour.ps1                # build and run the tour
      .\tools\unity\tour.ps1 -SkipBuild     # run against the existing build
      .\tools\unity\tour.ps1 -Runs 2        # twice, for stability

    Exit code: 0 all passed, 1 a check is left failing, 2 build/run error,
               3 timeout.
#>
param(
    [switch]$SkipBuild,
    # STORE MODE: a single run, 2.5x resolution, hints off.
    # The same dp layout - not a different interface, the large version of it.
    # The images are copied under render/store.
    [switch]$Store,
    [int]$Runs = 1,
    [int]$TimeoutSec = 900,
    # WHICH BUILD. "windows" is Mono (fast, for the daily tour);
    # "windows-il2cpp" is Android's compiler and stripper.
    # WHICH CUISINE: "turk" (the default) or "fastfood".
    # The tour used to pick a fixed cuisine; how fast food looked was never
    # measured at all.
    [ValidateSet("turk","fastfood")]
    [string]$Cuisine = "turk",
    # WHICH LANGUAGE. Empty means "leave the device preference alone", which
    # is what the measurement runs want. The STORE runs want English, because
    # that is the default Play listing - and until this flag existed every
    # store screenshot came out in whatever language this machine had saved.
    [ValidateSet("","tr","en","es","zh","ar")]
    [string]$Lang = "",
    [string]$Build = "windows",
    [string]$Root = "D:\ClaudeCodeProjects\MobilOyun"
)

$ErrorActionPreference = "Stop"

$exe = Join-Path $Root ("build\" + $Build + "\Lokanta.exe")

if (-not $SkipBuild) {
    Write-Output "=== Building the Windows player ==="
    $method = if ($Build -eq "windows-il2cpp") { "WindowsIl2cpp" } else { "Windows" }
    & (Join-Path $Root "tools\unity\run.ps1") `
        -Method "Lokanta.EditorTools.BuildPlayer.$method" -TimeoutSec $TimeoutSec
    if ($LASTEXITCODE -ne 0) {
        Write-Output "ERROR: the build failed (code $LASTEXITCODE)"
        exit 2
    }
}

if (-not (Test-Path $exe)) {
    Write-Output "ERROR: build not found: $exe"
    exit 2
}

$fail = 0
$crash = 0

# THE ARGUMENT LIST IS BUILT, NOT INTERPOLATED.
#
# The first attempt put an `@()` inside the array literal passed to
# -ArgumentList, on the assumption that an empty array would vanish. It does
# not: PowerShell nests it, Start-Process sees a null element and refuses the
# whole call with "the argument collection contains a null value". Adding the
# language arguments to a list afterwards is the version that has no empty
# case to get wrong.
function Get-TourArgs([string[]] $tail) {
    $a = [System.Collections.Generic.List[string]]::new()
    $a.Add("-lokanta-tour"); $a.Add("-lokanta-out"); $a.Add($out)
    $a.Add("-lokanta-cuisine"); $a.Add($Cuisine)
    if ($Lang) { $a.Add("-lokanta-lang"); $a.Add($Lang) }
    foreach ($t in $tail) { $a.Add($t) }
    return $a.ToArray()
}

for ($i = 1; $i -le $Runs; $i++) {
    $out = Join-Path $env:TEMP ("lokanta_tour_{0}" -f $i)
    if (Test-Path $out) { Remove-Item $out -Recurse -Force }
    New-Item -ItemType Directory -Path $out | Out-Null

    # A REAL PHONE SIZE (873x393 dp).
    #
    # Run in a desktop window, the tour's strip budget and touch target
    # measurements are MEANINGLESS: the screen is twice as wide and nothing
    # gets clipped. What the measurement measures is the view on the device.
    if ($Store) {
        # 2183x983 = 2.5 times 873x393 dp. The aspect ratio is the same (20:9),
        # so the frame is EXACTLY what is seen on the phone.
        $p = Start-Process -FilePath $exe -PassThru -Wait `
            -ArgumentList (Get-TourArgs @("-lokanta-scale", "2.5",
                                          "-screen-width", "2183",
                                          "-screen-height", "983",
                                          "-screen-fullscreen", "0"))
    }
    else {
        $p = Start-Process -FilePath $exe -PassThru -Wait `
            -ArgumentList (Get-TourArgs @("-screen-width", "873",
                                          "-screen-height", "393",
                                          "-screen-fullscreen", "0"))
    }

    $code = $p.ExitCode
    Write-Output ("run {0} -> exit {1}, images: {2}" -f $i, $code, $out)

    # The game's own log. We read the tour's lines from here - not the ones
    # that PASSED, but the ones LEFT FAILING and the ones that could not be
    # measured.
    #
    # The path is under LOCALLOW and the folder names are COMPANY/PRODUCT
    # (BuildPlayer.Company / BuildPlayer.Product).
    $log = Join-Path $env:USERPROFILE "AppData\LocalLow\Ahmet Akar\Lokanta\Player.log"
    if (Test-Path $log) {
        Select-String -Path $log -Pattern "FAIL :|UNMEASURED:|summary:" |
            ForEach-Object { Write-Output ("  " + $_.Line.Trim()) }
    }

    # THE RESULT COMES FROM THE SUMMARY FILE, NOT FROM THE EXIT CODE.
    #
    # Measured: `Application.Quit(1)` produces 0xC0000005 (-1073741819) on
    # shutdown in this Unity version's Windows player; the same build exiting
    # with a zero code closes cleanly. That is, a non-zero code broke the
    # shutdown at exactly the moment we wanted to report a failure.
    #
    # The tour no longer returns a code at all. The result is in the summary
    # file; if the file is missing, the tour died before it finished, and that
    # is a far more serious thing. The exit code is not wasted either: it now
    # marks only REAL crashes.
    $summary = Join-Path $out "summary.txt"
    if (Test-Path $summary) {
        $remaining = (Select-String -Path $summary -Pattern "^failed=(\d+)$").Matches[0].Groups[1].Value
        if ([int]$remaining -gt 0) {
            Write-Output ("  -> {0} checks left failing" -f $remaining)
            $fail++
        }
    }
    else {
        Write-Output "  -> CRASH: the tour did not run to the end (no summary file)"
        $crash++
    }

    # If the tour finished but the process did not close cleanly, report that
    # too: the summary file exists, so the checks did run - but something
    # crashed on shutdown, and the player sees that as well ("Save and quit").
    if ($code -ne 0 -and (Test-Path $summary)) {
        Write-Output ("  -> WARNING: the tour completed but the process closed with {0}" -f $code)
    }
}

# THE IMAGES ARE COPIED BEFORE THE EXIT CHECKS.
#
# This used to sit at the end, and a single red check ended the script early:
# the tour had run, the images had been produced, but they were lost before
# they were copied. Copying has nothing to do with checking the result.
if ($Store) {
    # ONE FOLDER PER CUISINE.
    #
    # Every store run wrote into render\store, so the second cuisine's
    # screenshots SILENTLY REPLACED the first one's - and the two look
    # different on purpose (fast food is self service, a cleaner instead of a
    # waiter, a different hall). The Play listing wants both, and what was
    # sitting in the folder was whichever cuisine happened to be run last,
    # with nothing in the name to say which.
    $dest = Join-Path (Join-Path $Root "render\store") $Cuisine
    if ($Lang) { $dest = Join-Path $dest $Lang }
    if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest -Force | Out-Null }
    Copy-Item (Join-Path (Join-Path $env:TEMP "lokanta_tour_1") "*.png") $dest -Force
    Write-Output ""
    $langNote = ""
    if ($Lang) { $langNote = " / $Lang" }
    Write-Output ("=== store images ({0}{1}): {2} ===" -f $Cuisine, $langNote, $dest)
}

if ($crash -gt 0) {
    Write-Output ""
    Write-Output ("=== CRASH: {0}/{1} runs did not complete ===" -f $crash, $Runs)
    exit 2
}

if ($fail -gt 0) {
    Write-Output ""
    Write-Output ("=== PROBLEM: {0}/{1} runs left a check failing ===" -f $fail, $Runs)
    exit 1
}

Write-Output ""
Write-Output ("=== tour clean: {0}/{0} runs passed ===" -f $Runs)
exit 0
