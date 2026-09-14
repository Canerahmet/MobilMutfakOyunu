# -*- coding: utf-8 -*-
"""
xoshiro128** bagimsiz referans uygulamasi
============================================================================
Amac: C# uretecini KENDI ciktisiyla degil, ayri bir uygulamayla dogrulamak.

Bir testin beklenen degerlerini test ettigi koddan almasi hicbir sey
kanitlamaz. Bu betik ureteci sifirdan, referans C kodundan yazip
tests/golden/rng.json dosyasini uretir; C# testi onu tutturmak zorundadir.

Referans: https://prng.di.unimi.it/xoshiro128starstar.c

Calistirma:  python rng_reference.py
"""
import io
import json
import os

M32 = 0xFFFFFFFF
M64 = 0xFFFFFFFFFFFFFFFF
PHI = 0x9E3779B97F4A7C15

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
GOLDEN = os.path.join(ROOT, "tests", "golden")


def rotl32(x, k):
    x &= M32
    return ((x << k) | (x >> (32 - k))) & M32


class Xoshiro128SS(object):
    def __init__(self, s0, s1, s2, s3):
        if (s0 | s1 | s2 | s3) == 0:
            s0 = 0x9E3779B9
        self.s = [s0 & M32, s1 & M32, s2 & M32, s3 & M32]

    def next(self):
        s = self.s
        result = (rotl32((s[1] * 5) & M32, 7) * 9) & M32
        t = (s[1] << 9) & M32
        s[2] ^= s[0]
        s[3] ^= s[1]
        s[1] ^= s[2]
        s[0] ^= s[3]
        s[2] ^= t
        s[3] = rotl32(s[3], 11)
        return result

    def next_int(self, max_exclusive):
        """Lemire carp-kaydir, C# ile ayni."""
        return ((self.next() * max_exclusive) >> 32)


def splitmix_mix(z):
    """C# RngSeeder.Mix ile ayni: durumu ilerletir, bir uint dondurur."""
    z = (z + PHI) & M64
    x = z
    x = ((x ^ (x >> 30)) * 0xBF58476D1CE4E5B9) & M64
    x = ((x ^ (x >> 27)) * 0x94D049BB133111EB) & M64
    x = x ^ (x >> 31)
    return z, (x >> 16) & M32


def seed(master, stream_index):
    z = (master ^ (((stream_index + 1) * PHI) & M64)) & M64
    parts = []
    for _ in range(4):
        z, v = splitmix_mix(z)
        parts.append(v)
    return Xoshiro128SS(*parts)


STREAMS = ["Arrival", "Archetype", "Order", "StaffError",
           "Market", "Event", "Hiring", "ReviewText"]


def main():
    out = {
        "_comment": "URETILEN DOSYA. tools/balance/rng_reference.py. "
                    "Bagimsiz xoshiro128** uygulamasi; C# ureteci bunu tutturmali.",
        "reference": "https://prng.di.unimi.it/xoshiro128starstar.c",
        "direct": [],
        "seeded": [],
    }

    # Dogrudan durum verilen diziler
    for s in ([1, 2, 3, 4], [0, 0, 0, 0], [0xDEADBEEF, 0x12345678, 1, 0xFFFFFFFF]):
        r = Xoshiro128SS(*s)
        out["direct"].append({
            "state": s,
            "values": [r.next() for _ in range(8)],
        })

    # Tohumlanmis akislar
    for i, name in enumerate(STREAMS):
        r = seed(20260909, i)
        out["seeded"].append({
            "masterSeed": 20260909,
            "stream": name,
            "streamIndex": i,
            "values": [r.next() for _ in range(6)],
        })

    # NextInt dagilim ornegi
    r = seed(42, 2)  # Order akisi
    out["nextInt10"] = [r.next_int(10) for _ in range(20)]

    if not os.path.isdir(GOLDEN):
        os.makedirs(GOLDEN)
    path = os.path.join(GOLDEN, "rng.json")
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(out, f, ensure_ascii=False, indent=2)
        f.write("\n")
    print("yazildi: " + os.path.relpath(path, ROOT))
    print("ilk dizi (1,2,3,4): " + str(out["direct"][0]["values"][:5]))


if __name__ == "__main__":
    main()
