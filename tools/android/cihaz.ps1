<#
    GERCEK CIHAZDA KOSTURMA.

    Neden var: turun tamami Windows yapisini kosuyor ve gonderdigimiz
    ikili o degil - Android yapisi IL2CPP + ARM64 + budama High.
    Emulator bu bosluğu kapatamiyor: emulator sistem goruntuleri
    x86_64, bizim APK ise YALNIZCA ARM64 (Play 64 bit sart kosuyor),
    yani gonderdigimiz dosya bir emulatore kurulamaz bile. Emulator
    kullanmak, yayinlamadigimiz ikinci bir yapiyi test etmek demek.

    Emulatorun yapisal olarak veremedigi ucu daha: kare suresi,
    isinma, ve iki parmak kamera.

    adb ZATEN kurulu - Unity'nin Android moduluyle geliyor, ayrica
    bir sey indirmek gerekmiyor.

    Kullanim:
      .\tools\android\cihaz.ps1              # kur, calistir, gunluk al
      .\tools\android\cihaz.ps1 -Sure 120    # iki dakika izle
      .\tools\android\cihaz.ps1 -Kaldir      # uygulamayi sil

    Cikis kodu: 0 temiz, 1 hata/cokme gorundu, 2 cihaz yok.
#>
param(
    [int]$Sure = 60,
    [switch]$Kaldir,
    [switch]$SadeceKur,
    [string]$Root = "D:\ClaudeCodeProjects\MobilOyun"
)

$ErrorActionPreference = "Stop"
$Paket = "com.ahmetakar.lokanta"

# adb'yi Unity'nin SDK'sindan buluyoruz. Sabit yol yazmiyoruz cunku
# Unity surumu degisince yol degisiyor ve "adb bulunamadi" hatasi
# cihaz yokluguyla karisiyor.
$adb = Get-ChildItem "C:\Program Files\Unity\Hub\Editor\*\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" -ErrorAction SilentlyContinue |
       Select-Object -First 1 -ExpandProperty FullName
if (-not $adb) {
    Write-Output "HATA: adb bulunamadi. Unity Android modulu kurulu mu?"
    exit 2
}

$cihazlar = & $adb devices | Select-Object -Skip 1 | Where-Object { $_ -match "\tdevice$" }
if (-not $cihazlar) {
    Write-Output "HATA: bagli cihaz yok."
    Write-Output ""
    Write-Output "  1. Telefonda Ayarlar > Telefon hakkinda > Yapi numarasina 7 kez dokun"
    Write-Output "  2. Gelistirici secenekleri > USB hata ayiklama -> ac"
    Write-Output "  3. USB ile bagla, telefondaki 'Izin ver' kutusunu onayla"
    Write-Output ""
    Write-Output "  Kontrol: & '$adb' devices"
    exit 2
}
$ad = (& $adb shell getprop ro.product.model).Trim()
$sur = (& $adb shell getprop ro.build.version.release).Trim()
$abi = (& $adb shell getprop ro.product.cpu.abi).Trim()
Write-Output "Cihaz: $ad (Android $sur, $abi)"

# ABI KONTROLU. APK yalnizca arm64-v8a tasiyor; x86_64 bir cihazda
# (yani emulatorde) kurulum "INSTALL_FAILED_NO_MATCHING_ABIS" ile
# duser ve bu hata mesaji sebebini soylemiyor.
if ($abi -notmatch "arm64") {
    Write-Output "HATA: cihaz ABI'si '$abi' - APK yalnizca arm64-v8a."
    Write-Output "      Bu bir emulator ise: bizim yapimiz orada kosmaz."
    exit 2
}

if ($Kaldir) {
    & $adb uninstall $Paket
    exit 0
}

$apk = Join-Path $Root "build\android\Lokanta.apk"
if (-not (Test-Path $apk)) {
    Write-Output "HATA: APK yok: $apk"
    Write-Output "      Once: .\tools\unity\run.ps1 -Method Lokanta.EditorTools.BuildPlayer.Android"
    exit 2
}
$mb = [math]::Round((Get-Item $apk).Length / 1MB, 1)
Write-Output "APK  : $apk ($mb MB, $((Get-Item $apk).LastWriteTime))"

Write-Output "=== kuruluyor ==="
$cikti = & $adb install -r $apk 2>&1 | Out-String
Write-Output $cikti.Trim()
if ($cikti -notmatch "Success") {
    Write-Output "HATA: kurulum basarisiz."
    exit 1
}
if ($SadeceKur) { exit 0 }

# Gunluk oncesi TEMIZLENIYOR: eski kosunun cokmesi yeni kosununki
# gibi gorunuyordu.
& $adb logcat -c

Write-Output "=== calistiriliyor, $Sure sn izleniyor ==="
& $adb shell monkey -p $Paket -c android.intent.category.LAUNCHER 1 | Out-Null

$gunluk = Join-Path $env:TEMP "lokanta_logcat.txt"
$is = Start-Process -FilePath $adb -ArgumentList "logcat -v time" `
      -RedirectStandardOutput $gunluk -NoNewWindow -PassThru
Start-Sleep -Seconds $Sure

# Ekran goruntusu: oyunun gercekten CIZDIGINI gormenin tek yolu.
# "Surec yasiyor" yetmiyor - siyah ekranda da yasiyor.
$png = Join-Path $Root "render\cihaz.png"
& $adb exec-out screencap -p > $png
& $adb shell am force-stop $Paket
Stop-Process -Id $is.Id -Force -ErrorAction SilentlyContinue

$satirlar = Get-Content $gunluk -ErrorAction SilentlyContinue
$bizim = $satirlar | Where-Object { $_ -match "Unity|lokanta|il2cpp|AndroidRuntime|DEBUG" }
$kotu  = $satirlar | Where-Object {
    $_ -match "FATAL|AndroidRuntime|libil2cpp\.so|NullReference|Exception|beginning of crash"
}

Write-Output ""
Write-Output "Gunluk : $gunluk ($($satirlar.Count) satir, $($bizim.Count) bizden)"
Write-Output "Goruntu: $png"

if ($kotu) {
    Write-Output ""
    Write-Output "=== HATA GORUNDU ($($kotu.Count) satir) ==="
    $kotu | Select-Object -First 25 | ForEach-Object { Write-Output "  $_" }
    exit 1
}

# SESSIZ BASARISIZLIK TUZAGI: hic Unity satiri yoksa uygulama
# acilmamis olabilir ve "hata yok" bunu basari gibi gosterir.
if ($bizim.Count -lt 5) {
    Write-Output ""
    Write-Output "OLCULEMEDI: gunlukte yalnizca $($bizim.Count) Unity satiri var -"
    Write-Output "            uygulama acilmamis olabilir. Goruntuye bak."
    exit 1
}

Write-Output ""
Write-Output "=== temiz: cokme yok, $($bizim.Count) Unity satiri ==="
exit 0
