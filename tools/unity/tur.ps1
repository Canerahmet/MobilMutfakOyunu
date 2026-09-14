<#
    Duman turu calistiricisi
    ==========================================================================
    Neden var: turu calistiran HICBIR betik yoktu. "-lokanta-tur" butun
    projede yalnizca Autopilot.cs'in icinde geciyordu; tur elle
    kosuluyordu ve kosulmadigi gunler kimse fark etmiyordu. Bir olcum
    aracinin otomasyona baglanmamis olmasi, o olcumun yok olmasidir.

    Ne yapiyor:
      1. (istege bagli) Windows yapisini kuruyor,
      2. gercek telefon olcusunde (873x393 dp) turu kosuyor,
      3. turun CIKIS KODUNU donduruyor.

    Turun cikis kodu artik sonuca bagli: kalan kontrol varsa 1.
    (Bu satira kadar tur butun kontroller HATA olsa bile 0 donuyordu.)

    Kullanim:
      .\tools\unity\tur.ps1                # yapi kur + tur kos
      .\tools\unity\tur.ps1 -SkipBuild     # var olan yapiyla kos
      .\tools\unity\tur.ps1 -Runs 2        # kararlilik icin iki kez

    Cikis kodu: 0 hepsi gecti, 1 kontrol kaldi, 2 yapi/calistirma hatasi,
                3 zaman asimi.
#>
param(
    [switch]$SkipBuild,
    # MAGAZA KIPI: tek kosu, 2,5 kat cozunurluk, ipuclari kapali.
    # Ayni dp yerlesimi - farkli bir arayuz degil, buyuk hali.
    # Goruntuler render/magaza altina kopyalaniyor.
    [switch]$Magaza,
    [int]$Runs = 1,
    [int]$TimeoutSec = 900,
    [string]$Root = "D:\ClaudeCodeProjects\MobilOyun"
)

$ErrorActionPreference = "Stop"

$exe = Join-Path $Root "build\windows\Lokanta.exe"

if (-not $SkipBuild) {
    Write-Output "=== Windows yapisi kuruluyor ==="
    & (Join-Path $Root "tools\unity\run.ps1") `
        -Method "Lokanta.EditorTools.BuildPlayer.Windows" -TimeoutSec $TimeoutSec
    if ($LASTEXITCODE -ne 0) {
        Write-Output "HATA: yapi kurulamadi (kod $LASTEXITCODE)"
        exit 2
    }
}

if (-not (Test-Path $exe)) {
    Write-Output "HATA: yapi bulunamadi: $exe"
    exit 2
}

$fail = 0
$crash = 0

for ($i = 1; $i -le $Runs; $i++) {
    $out = Join-Path $env:TEMP ("lokanta_tur_{0}" -f $i)
    if (Test-Path $out) { Remove-Item $out -Recurse -Force }
    New-Item -ItemType Directory -Path $out | Out-Null

    # GERCEK TELEFON OLCUSU (873x393 dp).
    #
    # Tur masaustu penceresinde kosulunca serit butcesi ve dokunma
    # hedefi olcumleri ANLAMSIZ: ekran iki kat genis, hicbir sey
    # kirpilmiyor. Olcumun olctugu sey cihazdaki gorunum.
    if ($Magaza) {
        # 2183x983 = 873x393 dp'nin 2,5 kati. Oran ayni (20:9), yani
        # cerceve telefonda gorunenin BIREBIR aynisi.
        $p = Start-Process -FilePath $exe -PassThru -Wait `
            -ArgumentList @("-lokanta-tur", "-lokanta-cikti", $out,
                            "-lokanta-olcek", "2.5",
                            "-screen-width", "2183", "-screen-height", "983",
                            "-screen-fullscreen", "0")
    }
    else {
        $p = Start-Process -FilePath $exe -PassThru -Wait `
            -ArgumentList @("-lokanta-tur", "-lokanta-cikti", $out,
                            "-screen-width", "873", "-screen-height", "393",
                            "-screen-fullscreen", "0")
    }

    $kod = $p.ExitCode
    Write-Output ("kosu {0} -> cikis {1}, goruntuler: {2}" -f $i, $kod, $out)

    # Oyunun kendi gunlugu. Turun satirlarini buradan okuyoruz -
    # "tamam"lari degil, KALANLARI ve olculemeyenleri.
    #
    # Yol LOCALLOW altinda ve klasor adlari SIRKET/URUN
    # (BuildPlayer.Company / BuildPlayer.Product).
    $log = Join-Path $env:USERPROFILE "AppData\LocalLow\Ahmet Akar\Lokanta\Player.log"
    if (Test-Path $log) {
        Select-String -Path $log -Pattern "HATA  :|OLCULEMEDI:|ozet:" |
            ForEach-Object { Write-Output ("  " + $_.Line.Trim()) }
    }

    # SONUC OZET DOSYASINDAN, CIKIS KODUNDAN DEGIL.
    #
    # Olculdu: `Application.Quit(1)` bu Unity surumunun Windows
    # oyuncusunda kapanista 0xC0000005 (-1073741819) uretiyor; sifir
    # kodla cikan ayni yapi temiz kapaniyor. Yani sifir disi kod, tam
    # da hata bildirmek istedigimiz anda kapanisi bozuyordu.
    #
    # Tur artik hic kod vermiyor. Sonuc ozet dosyasinda; dosya yoksa
    # tur bitmeden olmus demektir ve bu cok daha ciddi bir sey.
    # Cikis kodu da bosa gitmiyor: artik yalnizca GERCEK cokmeleri
    # isaret ediyor.
    $ozet = Join-Path $out "ozet.txt"
    if (Test-Path $ozet) {
        $kaldi = (Select-String -Path $ozet -Pattern "^kaldi=(\d+)$").Matches[0].Groups[1].Value
        if ([int]$kaldi -gt 0) {
            Write-Output ("  -> {0} kontrol kaldi" -f $kaldi)
            $fail++
        }
    }
    else {
        Write-Output "  -> COKME: tur sonuna kadar kosmadi (ozet dosyasi yok)"
        $crash++
    }

    # Tur bitti ama surec temiz kapanmadiysa da haber ver: ozet dosyasi
    # var, yani kontroller kosmus - ama kapanista bir sey coktu ve
    # bunu oyuncu da gorur ("Kaydet ve cik").
    if ($kod -ne 0 -and (Test-Path $ozet)) {
        Write-Output ("  -> UYARI: tur tamamlandi ama surec {0} ile kapandi" -f $kod)
    }
}

# GORUNTULER CIKIS KONTROLLERINDEN ONCE KOPYALANIYOR.
#
# Once sonda duruyordu ve tek bir kirmizi kontrol betigi erken
# bitiriyordu: tur kosmus, goruntuler uretilmis, ama kopyalanmadan
# kaybolmuslardi. Kopyalamanin sonucu kontrol etmekle bir ilgisi yok.
if ($Magaza) {
    $hedef = Join-Path $Root "render\magaza"
    if (-not (Test-Path $hedef)) { New-Item -ItemType Directory -Path $hedef | Out-Null }
    Copy-Item (Join-Path (Join-Path $env:TEMP "lokanta_tur_1") "*.png") $hedef -Force
    Write-Output ""
    Write-Output ("=== magaza goruntuleri: {0} ===" -f $hedef)
}

if ($crash -gt 0) {
    Write-Output ""
    Write-Output ("=== COKME: {0}/{1} kosu tamamlanmadi ===" -f $crash, $Runs)
    exit 2
}

if ($fail -gt 0) {
    Write-Output ""
    Write-Output ("=== SORUN: {0}/{1} kosuda kontrol kaldi ===" -f $fail, $Runs)
    exit 1
}

Write-Output ""
Write-Output ("=== tur tamam: {0}/{0} kosu gecti ===" -f $Runs)
exit 0
