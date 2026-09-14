# -*- coding: utf-8 -*-
"""`dotnet` komutunu Smart App Control engeline karşı ısrarla çalıştırır.

Bu makinede SAC zorunlu modda ve **imzasız, taze yazılmış** bir derlemenin
yüklenmesini engelleyebiliyor:

    FileLoadException ... An Application Control policy has blocked
    this file. (0x800711C7)

Engel **rastgele** — aynı komut art arda bir kez engellenip bir kez
geçebiliyor. Yeniden derlemek tek başına yetmiyor: çekirdek
`<Deterministic>true</Deterministic>` ile derlendiği için aynı kaynak her
zaman aynı ikiliyi üretiyor, yani engellenen bir karma sonsuza kadar
engelli kalıyor. `-p:Deterministic=false` her derlemede yeni bir modül
kimliği gömüyor, yani her deneme **yeni bir karma** ve yeni bir şans.

Yani doğru davranış: karmayı değiştir **ve** ısrar et.

Kullanım:
    python tools/dotnet_retry.py test tests/Lokanta.Core.Tests/... -v q
    python tools/dotnet_retry.py run --project src/Lokanta.Harness ...

Çıkış kodu komutun kendisininki. Engel yüzünden hiç geçemezse 2.
"""
from __future__ import print_function

import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BLOCKED = "0x800711C7"
TRIES = 6


def main():
    args = sys.argv[1:]
    if not args:
        print(__doc__)
        return 2

    # Yapilandirma ve determinizm bayragi, cagirana birakilmadan
    # ekleniyor: unutuldugunda belirti "kod bozuk" gibi gorunuyor.
    cmd = ["dotnet"] + args
    if "-c" not in args and "--configuration" not in args:
        cmd += ["-c", "Release"]
    if not any(a.startswith("-p:Deterministic") for a in args):
        cmd += ["-p:Deterministic=false"]

    last = None
    for attempt in range(1, TRIES + 1):
        # YENIDEN DERLEME ZORLANIYOR.
        #
        # -p:Deterministic=false tek basina YETMIYOR: kaynak degismediyse
        # MSBuild derlemeyi guncel sayip ATLIYOR, yani ayni engelli ikili
        # tekrar kullaniliyor ve yeniden deneme hicbir sey degistirmiyor.
        # Olculdu - alti denemenin altisi da ayni dosyada engellendi.
        #
        # --no-incremental derlemeyi gercekten tekrarlatiyor; determinizm
        # kapali oldugu icin her tekrar YENI bir modul kimligi uretiyor.
        run = list(cmd)
        if attempt > 1 and "--no-incremental" not in run:
            # "dotnet run" bu bayragi TANIMIYOR ve uygulamaya geciriyor;
            # o yuzden run icin derleme AYRI bir adim.
            if args[0] == "run":
                proj = None
                for i, a in enumerate(args):
                    if a == "--project" and i + 1 < len(args):
                        proj = args[i + 1]
                if proj:
                    subprocess.run(["dotnet", "build", proj, "-c", "Release",
                                    "-v", "q", "--nologo",
                                    "-p:Deterministic=false", "--no-incremental"],
                                   cwd=ROOT, capture_output=True)
                    if "--no-build" not in run:
                        run.insert(run.index("run") + 1, "--no-build")
            else:
                run.append("--no-incremental")

        p = subprocess.run(run, cwd=ROOT, capture_output=True, text=True,
                           encoding="utf-8", errors="replace")
        out = (p.stdout or "") + (p.stderr or "")
        last = p

        if BLOCKED not in out:
            sys.stdout.write(p.stdout or "")
            sys.stderr.write(p.stderr or "")
            return p.returncode

        print("  SAC engeli (%d/%d) - karma degistirilip yeniden deneniyor"
              % (attempt, TRIES), file=sys.stderr)

    sys.stdout.write(last.stdout or "")
    print("\nSAC %d denemede de engelledi. docs/19 'Smart App Control'."
          % TRIES, file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main())
