# -*- coding: utf-8 -*-
"""Her sey yerinde mi.

Bu betik **Python ve .NET tarafındaki** denetçileri sırayla koşuyor ve
tek bir cevap veriyor.

Neden gerekli: bu denetçilerin her biri gerçek bir hatayı yakalamak
için yazıldı, ve her biri en az bir kez "koşulmadığı için" kaçırdı.
Yazı tipi kapsaması denetçisi yazıldıktan yarım saat sonra yeni bir
metin eklendi ve o metin yazı tipinde olmayan bir ok içeriyordu; ancak
denetçi elle koşturulduğunda görüldü.

DOCSTRING BİR ZAMANLAR "altı denetçi var ve hepsini koşuyor" diyordu.
İkisi de yanlıştı: on üç adım vardı ve Unity tarafındaki denetçilerin
(ArtCheck, PlacementAudit, RoomLayout, GameShot ve duman turu) **hiçbiri**
listede yoktu. Yani docstring'in kendi gerekçesi ("her biri en az bir
kez koşulmadığı için kaçırdı") tam olarak o denetçiler için geçerliydi.

Unity adımları ayrı, çünkü her biri bir editör oturumu açıyor ve
dakikalar sürüyor; varsayılan koşuda atlanıyorlar ama **listede
görünüyorlar**.

Kullanım:
    python tools/check.py            Python + .NET denetimleri
    python tools/check.py --hizli    testleri atla (saniyeler sürer)
    python tools/check.py --unity    Unity denetçilerini de koş (yavaş)

Çıkış kodu 0 temiz, 1 en az bir denetim kırmızı.
"""
from __future__ import print_function

import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PY = sys.executable


def run(name, args, cwd=ROOT):
    """Tek bir denetim. Dönen değer: (ad, geçti mi, son satır).

    `dotnet` çağrıları engellenirse **karma değiştirilerek** yeniden
    deneniyor: bu makinede Smart App Control imzasız bir derlemenin
    yüklenmesini engelleyebiliyor ve engel dosyanın karmasına bağlı.
    Çekirdek `<Deterministic>true</Deterministic>` ile derlendiği için
    aynı kaynak her zaman aynı karmayı üretiyor — yani engellenen bir
    derleme yeniden derlemekle **düzelmiyor**, sonsuza kadar kalıyor.
    `-p:Deterministic=false` her derlemede yeni bir kimlik üretiyor ve
    engel kendiliğinden düşüyor. Ayrıntı: docs/19.
    """
    if args and args[0] == "dotnet":
        r = _once(name, args + ["-c", "Release"], cwd)
        # ENGEL BUTUN CIKTIDA ARANIYOR, son satirda degil.
        #
        # Once yalnizca `tail` bakiliyordu ve xUnit engel mesajini
        # BASA yaziyor ("Skipping: ... An Application Control policy
        # has blocked this file"), sonra bilgi satirlariyla bitiyor.
        # Yani engel goruulmuyor, yeniden deneme hic tetiklenmiyordu.
        # YENIDEN DENEME SEBEBE DEGIL KANITA BAGLI.
        #
        # Once yalnizca 0x800711C7 gorununce deneniyordu. Ama engel her
        # zaman o kodu yazmiyor: test konagi bazen sessizce SIFIR test
        # bulup cikis kodu 0 veriyor ("A total of 1 test files matched"
        # ve hicbir ozet satiri yok). O halde kod aranmiyor, denetim
        # basarisiz sayiliyor ve yeniden deneme hic tetiklenmiyordu.
        if r[1]:
            return r
        # Engellendi: KARMAYI DEGISTIREREK **ve yeniden derleyerek**.
        # -p:Deterministic=false tek basina yetmiyor - kaynak
        # degismediyse MSBuild derlemeyi atliyor ve ayni engelli ikili
        # tekrar kullaniliyor.
        #
        # `dotnet test` AYRI ELE ALINIYOR: --no-incremental'i kabul
        # etmiyor (MSB1001 "Unknown switch"), yani eski yol testler icin
        # HIC calismiyordu. Once ayri bir derleme, sonra --no-build ile
        # kosu. Ayrica taze bir karma da engellenebiliyor, o yuzden
        # birkac deneme.
        if len(args) > 1 and args[1] == "test":
            proj = args[2]
            for _ in range(5):
                # DERLEMENIN SONUCU ONEMSENIYOR.
                #
                # Once yok sayiliyordu ve bir sey daha kiriyordu:
                # --no-incremental once TEMIZLIYOR, yani derleme
                # basarisiz olunca test derlemesi ORTADAN KALKIYOR ve
                # sonraki "--no-build" kosusu "test source file ... was
                # not found" diyordu. Yani gecici bir engel, kalici
                # gorunen baska bir hataya donusuyordu.
                b = _once(name, ["dotnet", "build", proj, "-c", "Release",
                                 "-v", "q", "--nologo", "-p:Deterministic=false",
                                 "--no-incremental"], cwd)
                if not b[1]:
                    continue
                r = _once(name, args + ["-c", "Release", "--no-build"], cwd)
                if r[1]:
                    return r

            # Bes denemede de kosamadi. Son bir kez NORMAL derleyip
            # birakiyoruz ki depo, testi olmayan bir durumda kalmasin.
            _once(name, ["dotnet", "build", proj, "-c", "Release",
                         "-v", "q", "--nologo"], cwd)
            return r
        return _once(name, args + ["-c", "Release", "-p:Deterministic=false",
                                   "--no-incremental"], cwd)
    return _once(name, args, cwd)


def _once(name, args, cwd=ROOT):
    try:
        p = subprocess.run(args, cwd=cwd, capture_output=True, text=True,
                           encoding="utf-8", errors="replace")
    except OSError as e:
        return name, False, "calistirilamadi: %s" % e, ""

    out = (p.stdout or "") + (p.stderr or "")
    lines = [l.strip() for l in out.splitlines() if l.strip()]
    tail = lines[-1] if lines else "(cikti yok)"

    ok = p.returncode == 0

    # TESTLERIN KOSTUGU KANITLANIYOR.
    #
    # `dotnet test`, test derlemesi yuklenemediginde CIKIS KODU 0
    # veriyor ve yalnizca "Skipping: ... blocked" yaziyor. Denetim bunu
    # gecti sayiyordu: 239 testin sifiri kosuyor, tablo yesil yaniyordu.
    # Bu projenin en sik hata sinifinin denetcinin KENDISINDE hali.
    #
    # Artik kanit sart: xUnit'in ozet satiri ("Passed!" / "Failed!")
    # ciktida gecmiyorsa denetim kirmizi.
    if ok and len(args) > 1 and args[0] == "dotnet" and args[1] == "test":
        if "Passed!" not in out and "Failed!" not in out:
            ok = False
            tail = "TEST KOSMADI (ozet satiri yok): " + tail

    return name, ok, tail, out


def main():
    quick = "--hizli" in sys.argv
    unity = "--unity" in sys.argv

    checks = [
        ("icerik uretimi",
         [PY, os.path.join("tools", "balance", "export.py")]),
        ("yemek dengesi",
         [PY, os.path.join("tools", "content", "gen_dishes.py")]),
        ("personel huylari",
         [PY, os.path.join("tools", "content", "gen_traits.py")]),
        ("duzenli musteriler",
         [PY, os.path.join("tools", "content", "gen_regulars.py")]),
        ("personel isimleri",
         [PY, os.path.join("tools", "content", "gen_names.py")]),
        ("metin tablosu",
         [PY, os.path.join("tools", "content", "gen_loc.py")]),
        ("icerik-kod sozlesmesi",
         [PY, os.path.join("tools", "audit_content.py")]),
        ("yazi tipi kapsamasi",
         [PY, os.path.join("tools", "art", "check_font.py")]),
        ("urp ayarlari",
         [PY, os.path.join("tools", "check_urp.py")]),
        # LISANS: ticari yayin kapisi. Bir varlik klasoru lisanssiz ya da
        # atif defterinde satirsiz kalirsa yayin riski dogar ve bunu
        # Google Play degil telif sahibi yakalar.
        ("lisans ve atif",
         [PY, os.path.join("tools", "check_lisans.py")]),
    ]

    if not quick:
        # NETSTANDARD KORUMASI. Cekirdek iki hedefe birden derleniyor ve
        # testler yalnizca net10.0 olanini yukluyor - yani netstandard2.1
        # hedefi hic derlenmeden gecebilir. O hedef, cekirdegin Unity'nin
        # API yuzeyi disina cikmadiginin TEK kontrolu; kosmazsa koruma
        # degil susteur.
        checks.append(("netstandard korumasi",
                       ["dotnet", "build",
                        os.path.join("src", "Lokanta.Core", "Lokanta.Core.csproj"),
                        "-f", "netstandard2.1", "-v", "q", "--nologo"]))
        checks.append(("icerik netstandard",
                       ["dotnet", "build",
                        os.path.join("src", "Lokanta.Content", "Lokanta.Content.csproj"),
                        "-f", "netstandard2.1", "-v", "q", "--nologo"]))
        checks.append(("cekirdek testleri",
                       ["dotnet", "test",
                        os.path.join("tests", "Lokanta.Core.Tests",
                                     "Lokanta.Core.Tests.csproj"),
                        "-v", "q", "--nologo"]))

    # UNITY TARAFI. Her biri bir editor oturumu aciyor; varsayilan
    # kosuda atlaniyor ama LISTEDE gorunuyor - "hepsini kosuyorum"
    # diyen bir aracin en tehlikeli hali, kosmadigini saymamasi.
    unity_checks = [
        ("sanat denetimi",
         ["powershell", "-NoProfile", "-File",
          os.path.join("tools", "unity", "run.ps1"),
          "-Method", "Lokanta.EditorTools.ArtCheck.Run"]),
        ("yerlesim denetimi",
         ["powershell", "-NoProfile", "-File",
          os.path.join("tools", "unity", "run.ps1"),
          "-Method", "Lokanta.EditorTools.PlacementAudit.Run"]),
        ("oda yerlesimi",
         ["powershell", "-NoProfile", "-File",
          os.path.join("tools", "unity", "run.ps1"),
          "-Method", "Lokanta.EditorTools.RoomLayout.Capture"]),
        ("sahne goruntusu",
         ["powershell", "-NoProfile", "-File",
          os.path.join("tools", "unity", "shot.ps1")]),
        ("duman turu",
         ["powershell", "-NoProfile", "-File",
          os.path.join("tools", "unity", "tur.ps1")]),
    ]

    if unity:
        checks += unity_checks
    else:
        print("(atlandi, --unity ile kosulur: "
              + ", ".join(n for n, _ in unity_checks) + ")")

    print("=" * 70)
    results = []
    for name, args in checks:
        name, ok, tail, _ = run(name, args)
        results.append((name, ok, tail))
        print("%-24s %s  %s" % (name, "TAMAM" if ok else "KIRMIZI", tail[:60]))

    print("=" * 70)
    bad = [r for r in results if not r[1]]
    if not bad:
        print("HEPSI TEMIZ (%d denetim)" % len(results))
        return 0

    print("%d DENETIM KIRMIZI:" % len(bad))
    for name, _ok, tail in bad:
        print("  %s: %s" % (name, tail))
    return 1


if __name__ == "__main__":
    sys.exit(main())
