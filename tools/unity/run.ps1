<#
    Unity toplu kip calistiricisi
    ==========================================================================
    Neden var: Unity -batchmode -quit, DERLEME HATASI OLSA BILE 0 donuyor.
    10 Eylul 2026'da ProjectSetup.cs dort API hatasi verdi, -executeMethod hic
    calismadi ve cikis kodu yine de 0'di. Bu betik gunlugu okuyup gercek
    sonucu donduruyor.

    Kullanim:
      .\tools\unity\run.ps1 -Method Lokanta.EditorTools.ProjectSetup.ApplyAll
      .\tools\unity\run.ps1 -Method ... -TimeoutSec 900
      .\tools\unity\run.ps1 -ExtraArgs @("-buildTarget","Android")

    Cikis kodu: 0 basarili, 1 derleme hatasi, 2 Unity hatasi, 3 zaman asimi.
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
    Write-Output "HATA: Unity bulunamadi: $Editor"
    exit 2
}

$unityArgs = @("-batchmode", "-quit", "-nographics", "-projectPath", $ProjectPath,
               "-logFile", $LogPath)
if ($Method -ne "") { $unityArgs += @("-executeMethod", $Method) }
$unityArgs += $ExtraArgs

Write-Output "Unity  : $Editor"
Write-Output "Proje  : $ProjectPath"
Write-Output "Metot  : $(if ($Method -eq '') { '(yok)' } else { $Method })"
Write-Output "Gunluk : $LogPath"
Write-Output "---"

# --- Isinma turu (derleme) --------------------------------------------------
# Unity, -executeMethod'u BETIK DERLEMESI BITMEDEN calistirabiliyor ve o zaman
# calisan kod ESKI surum olur. shot.ps1 bunu 10 Eylul'de ogrenip isinma turu
# ekledi; run.ps1'e eklenmedi ve 12 Eylul'de tam bunu yasadi: arayuz bastan
# yazildi, yapi "tamam" dedi, ama kurulan oyun ESKI arayuzu gosteriyordu.
# Yapinin kendisi sessizce eskiydi - ve hicbir sey uyarmiyordu.
#
# Ilk tur yalnizca derliyor ve cikiyor; ikinci tur guncel derlemeyle calisiyor.
if ($Method -ne "") {
    $WarmLog = Join-Path $env:TEMP ("unity_warm_" + (Get-Date -Format "yyyyMMdd_HHmmss") + ".log")
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
        Write-Output "=== DERLEME HATASI ($($warmErrors.Count) benzersiz) ==="
        $warmErrors | Select-Object -First 20 | ForEach-Object { Write-Output "  $_" }
        exit 1
    }
}

$sw = [Diagnostics.Stopwatch]::StartNew()
$proc = Start-Process -FilePath $Editor -ArgumentList $unityArgs -NoNewWindow -PassThru
if (-not $proc.WaitForExit($TimeoutSec * 1000)) {
    try { $proc.Kill() } catch {}
    $sw.Stop()
    Write-Output "ZAMAN ASIMI: $TimeoutSec sn doldu"
    exit 3
}
# Zaman asimli WaitForExit ExitCode'u her zaman doldurmuyor; parametresiz
# cagri ile surecin tam olarak kapanmasini bekleyip kodu oyle okuyoruz.
$proc.WaitForExit()
$sw.Stop()
$unityExit = $proc.ExitCode
if ($null -eq $unityExit) { $unityExit = 0 }

Write-Output ("Unity cikis kodu : {0}" -f $unityExit)
Write-Output ("Sure             : {0:N1} sn" -f $sw.Elapsed.TotalSeconds)

if (-not (Test-Path $LogPath)) {
    Write-Output "HATA: gunluk dosyasi olusmadi"
    exit 2
}

# --- Derleme hatalari -------------------------------------------------------
$compileErrors = Select-String -Path $LogPath -Pattern "error CS\d+" |
                 ForEach-Object { $_.Line.Trim() } |
                 Sort-Object -Unique

if ($compileErrors.Count -gt 0) {
    Write-Output ""
    Write-Output "=== DERLEME HATASI ($($compileErrors.Count) benzersiz) ==="
    $compileErrors | Select-Object -First 20 | ForEach-Object { Write-Output "  $_" }
    exit 1
}

# --- Betigin kendi raporu ---------------------------------------------------
# BAGLAM 400, 60 DEGIL.
#
# Tur 107 kontrol basiyor; -Context 0, 60 ile gunlugun ancak yarisi
# ekrana cikiyor ve gerisi HIC OKUNMUYORDU. Bir olcum aracinin
# ciktisini kirpmak, olculeni gormemekle ayni sey.
$report = Select-String -Path $LogPath -Pattern "=== Lokanta" -Context 0, 400
if ($report) {
    Write-Output ""
    $report[0].Context.PostContext | Where-Object { $_ -match "^\s{2}\S" -or $_ -match "^===" } |
        ForEach-Object { Write-Output $_ }
}

# --- Unity tarafi hatalari --------------------------------------------------
# "HATA  :" DESENI DE OLUMCUL.
#
# Turun Note() cagrisi kirmizi kontrolleri "HATA  : ..." diye yaziyor
# ama bu desen listede yoktu: 107 kontrolun hepsi kalsa bile betik 0
# donuyordu. Turun kendisi artik sifir disi kod donduruyor, bu satir
# ikinci kapi - gunluge bakan biri de gormeli.
$fatal = Select-String -Path $LogPath -Pattern "SORUNLAR:|HATA  :|Fatal Error|Aborting batchmode" |
         ForEach-Object { $_.Line.Trim() }
if ($fatal.Count -gt 0) {
    Write-Output ""
    Write-Output "=== SORUN ==="
    $fatal | Select-Object -First 10 | ForEach-Object { Write-Output "  $_" }
    exit 2
}

if ($unityExit -ne 0) {
    Write-Output "Unity sifir disi kod dondurdu"
    exit 2
}

Write-Output ""
Write-Output "TAMAM"
exit 0
