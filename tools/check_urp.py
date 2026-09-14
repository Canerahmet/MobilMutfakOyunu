# -*- coding: utf-8 -*-
"""ProjectSetup.cs ile LokantaURP.asset aynı şeyi mi söylüyor.

Neden var: render ayarları **iki yerde** yazılı. `LokantaURP.asset`
Unity'nin okuduğu dosya; `ProjectSetup.ConfigureUrp` ise aynı alanları
kurulum sırasında yeniden yazan kod. İkisi ayrıştığında hiçbir test
kırılmıyor — oyun sessizce eski ayarla derleniyor.

Ayrıştı da: performans turu `.asset` dosyasını elle düzeltti (MSAA 4→1,
render ölçeği 1→0,8), `ProjectSetup` ise eski değerleri yazmaya devam
etti. `ApplyAll` koşturan biri bütün performans işini geri alıyordu ve
bunu görmenin hiçbir yolu yoktu.

Aynı sayıyı iki yere yazmak bu projede beşinci kez ayrıştı; bu betik
altıncıyı yakalamak için.

Çıkış kodu 0 temiz, 1 ayrışma var.
"""
from __future__ import print_function

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SETUP = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Editor", "ProjectSetup.cs")
ASSET = os.path.join(ROOT, "unity", "Assets", "Settings", "LokantaURP.asset")

# SetUrpField(so, "m_X", p => p.<tur>Value = <deger>, "...")
CALL = re.compile(
    r'SetUrpField\(\s*so\s*,\s*"(?P<alan>\w+)"\s*,\s*'
    r'p\s*=>\s*p\.(?P<tur>\w+)\s*=\s*(?P<deger>[^,]+?)\s*,',
    re.S)


def beklenen(tur, deger):
    """C# tarafındaki değeri, .asset dosyasındaki sayıya çevirir."""
    deger = deger.strip()
    if tur == "boolValue":
        return 1.0 if deger == "true" else 0.0
    if tur in ("intValue", "enumValueIndex"):
        return float(int(deger))
    if tur == "floatValue":
        return float(deger.rstrip("fF"))
    return None                       # tanimadigimiz tur: atla


def main():
    for p in (SETUP, ASSET):
        if not os.path.exists(p):
            print("dosya yok: %s" % p)
            return 1

    kod = io.open(SETUP, encoding="utf-8").read()
    varlik = io.open(ASSET, encoding="utf-8").read()

    # .asset satirlari: "  m_MSAA: 1"
    icinde = {}
    for satir in varlik.splitlines():
        m = re.match(r"\s*(m_\w+):\s*([-\d.]+)\s*$", satir)
        if m:
            icinde[m.group(1)] = float(m.group(2))

    kirik, bakilan, atlanan = [], 0, []
    for m in CALL.finditer(kod):
        alan, tur, deger = m.group("alan"), m.group("tur"), m.group("deger")
        bek = beklenen(tur, deger)
        if bek is None:
            atlanan.append("%s (%s)" % (alan, tur))
            continue
        if alan not in icinde:
            # Alan .asset'te skaler degil (nesne, dizi) ya da bu surumde yok.
            atlanan.append("%s (varlikta skaler degil)" % alan)
            continue
        bakilan += 1
        if abs(icinde[alan] - bek) > 1e-6:
            kirik.append("  %-36s kod %-8g varlik %-8g"
                         % (alan, bek, icinde[alan]))

    if kirik:
        print("URP AYARLARI AYRISMIS (%d alan):" % len(kirik))
        for k in kirik:
            print(k)
        print("\n  ProjectSetup.ConfigureUrp ve Assets/Settings/LokantaURP.asset")
        print("  ayni sayiyi soylemeli. ApplyAll kosturmak aksi halde")
        print("  varliktaki degeri sessizce geri aliyor.")
        return 1

    print("sonuc     : %d URP alani ayni%s" % (
        bakilan, (", %d atlandi" % len(atlanan)) if atlanan else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
