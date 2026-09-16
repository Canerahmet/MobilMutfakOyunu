<#
    RUNNING ON A REAL DEVICE.

    Why it exists: the whole tour runs the Windows build, and that is not
    the binary we ship - the Android build is IL2CPP + ARM64 + High
    stripping. An emulator cannot close that gap: emulator system images
    are x86_64 while our APK is ARM64 ONLY (Play requires 64 bit), so the
    file we ship cannot even be installed on an emulator. Using an
    emulator means testing a second build that we do not publish.

    Three more things an emulator structurally cannot give: frame time,
    thermal behaviour, and two-finger camera control.

    adb IS ALREADY INSTALLED - it comes with Unity's Android module,
    nothing else needs downloading.

    Usage:
      .\tools\android\cihaz.ps1                 # install, run, take a log
      .\tools\android\cihaz.ps1 -Seconds 120    # watch for two minutes
      .\tools\android\cihaz.ps1 -Uninstall      # remove the app

    Exit code: 0 clean, 1 an error/crash appeared, 2 no device.
#>
param(
    [int]$Seconds = 60,
    [switch]$Uninstall,
    [switch]$InstallOnly,
    [string]$Root = "D:\ClaudeCodeProjects\MobilOyun"
)

$ErrorActionPreference = "Stop"
$Package = "com.ahmetakar.lokanta"

# We find adb inside Unity's SDK. We do not write a fixed path, because
# the path changes when the Unity version changes and the resulting "adb
# not found" error gets confused with there being no device.
$adb = Get-ChildItem "C:\Program Files\Unity\Hub\Editor\*\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" -ErrorAction SilentlyContinue |
       Select-Object -First 1 -ExpandProperty FullName
if (-not $adb) {
    Write-Output "ERROR: adb not found. Is the Unity Android module installed?"
    exit 2
}

$devices = & $adb devices | Select-Object -Skip 1 | Where-Object { $_ -match "\tdevice$" }
if (-not $devices) {
    Write-Output "ERROR: no device connected."
    Write-Output ""
    Write-Output "  1. On the phone: Settings > About phone > tap Build number 7 times"
    Write-Output "  2. Developer options > USB debugging -> on"
    Write-Output "  3. Connect over USB, confirm the 'Allow' dialog on the phone"
    Write-Output ""
    Write-Output "  Check with: & '$adb' devices"
    exit 2
}
$model = (& $adb shell getprop ro.product.model).Trim()
$release = (& $adb shell getprop ro.build.version.release).Trim()
$abi = (& $adb shell getprop ro.product.cpu.abi).Trim()
Write-Output "Device: $model (Android $release, $abi)"

# THE ABI CHECK. The APK carries arm64-v8a only; on an x86_64 device
# (that is, on an emulator) the install fails with
# "INSTALL_FAILED_NO_MATCHING_ABIS", and that message does not say why.
if ($abi -notmatch "arm64") {
    Write-Output "ERROR: the device ABI is '$abi' - the APK is arm64-v8a only."
    Write-Output "       If this is an emulator: our build does not run there."
    exit 2
}

if ($Uninstall) {
    & $adb uninstall $Package
    exit 0
}

$apk = Join-Path $Root "build\android\Lokanta.apk"
if (-not (Test-Path $apk)) {
    Write-Output "ERROR: no APK at: $apk"
    Write-Output "       First: .\tools\unity\run.ps1 -Method Lokanta.EditorTools.BuildPlayer.Android"
    exit 2
}
$mb = [math]::Round((Get-Item $apk).Length / 1MB, 1)
Write-Output "APK   : $apk ($mb MB, $((Get-Item $apk).LastWriteTime))"

Write-Output "=== installing ==="
$output = & $adb install -r $apk 2>&1 | Out-String
Write-Output $output.Trim()
if ($output -notmatch "Success") {
    Write-Output "ERROR: the install failed."
    exit 1
}
if ($InstallOnly) { exit 0 }

# CLEARED BEFORE THE LOG: a crash from the previous run looked like one
# from this run.
& $adb logcat -c

Write-Output "=== running, watching for $Seconds s ==="
& $adb shell monkey -p $Package -c android.intent.category.LAUNCHER 1 | Out-Null

$log = Join-Path $env:TEMP "lokanta_logcat.txt"
$proc = Start-Process -FilePath $adb -ArgumentList "logcat -v time" `
      -RedirectStandardOutput $log -NoNewWindow -PassThru
Start-Sleep -Seconds $Seconds

# A screenshot: the only way to see that the game really is DRAWING.
# "The process is alive" is not enough - it is alive on a black screen too.
$png = Join-Path $Root "render\cihaz.png"
& $adb exec-out screencap -p > $png
& $adb shell am force-stop $Package
Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue

$lines = Get-Content $log -ErrorAction SilentlyContinue
$ours = $lines | Where-Object { $_ -match "Unity|lokanta|il2cpp|AndroidRuntime|DEBUG" }
$bad  = $lines | Where-Object {
    $_ -match "FATAL|AndroidRuntime|libil2cpp\.so|NullReference|Exception|beginning of crash"
}

Write-Output ""
Write-Output "Log        : $log ($($lines.Count) lines, $($ours.Count) ours)"
Write-Output "Screenshot : $png"

if ($bad) {
    Write-Output ""
    Write-Output "=== AN ERROR APPEARED ($($bad.Count) lines) ==="
    $bad | Select-Object -First 25 | ForEach-Object { Write-Output "  $_" }
    exit 1
}

# THE SILENT-FAILURE TRAP: if there is no Unity line at all, the app may
# never have opened, and "no errors" makes that look like success.
if ($ours.Count -lt 5) {
    Write-Output ""
    Write-Output "COULD NOT MEASURE: the log holds only $($ours.Count) Unity lines -"
    Write-Output "                   the app may not have opened. Look at the screenshot."
    exit 1
}

Write-Output ""
Write-Output "=== clean: no crash, $($ours.Count) Unity lines ==="
exit 0
