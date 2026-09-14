using System;

namespace Lokanta.Core
{
    /// <summary>
    /// Cekirdegin TEK aritmetik yardimcisi.
    /// docs/23-cekirdek-sozlesmesi.md 2.3: Math.Round, Math.Floor ve (int) ile
    /// kesme yasak. Butun bolme ve yuvarlama buradan gecer.
    ///
    /// Yuvarlama kurali: yarisi SIFIRDAN UZAGA. Banker's rounding kullanilmaz,
    /// cunku .NET'in varsayilani odur ve iki gelistirici ikisini karistirir.
    /// </summary>
    public static class Fx
    {
        /// <summary>1,0 baz puan cinsinden. Oran ve carpanlarin olcegi.</summary>
        public const int One = 10_000;

        /// <summary>1 is-gunu, mikro-is-gunu cinsinden. Personel yuku bu olcekte.</summary>
        public const int Micro = 1_000_000;

        /// <summary>Birikimli carpanlar icin ic hassasiyet. Disariya sizmaz.</summary>
        public const long Nano = 1_000_000_000L;

        /// <summary>1 sikke, santi-sikke cinsinden. Butun para bu olcekte.</summary>
        public const int Coin = 100;

        /// <summary>
        /// a * b / c, yarisi sifirdan uzaga yuvarlanmis.
        /// Ara carpim checked: sessiz tasma, yanlis sonuctan daha kotudur.
        /// </summary>
        public static long MulDiv(long a, long b, long c)
        {
            if (c == 0) throw new DivideByZeroException("Fx.MulDiv: c sifir");

            long p;
            checked { p = a * b; }

            long q = p / c;
            long r = p - q * c;
            if (r == 0) return q;

            long ar = r < 0 ? -r : r;
            long ac = c < 0 ? -c : c;

            long twice;
            checked { twice = ar * 2; }
            if (twice >= ac)
                q += ((p < 0) == (c < 0)) ? 1 : -1;

            return q;
        }

        /// <summary>Degeri baz puan carpaniyla olcekler. bp = One ise degismez.</summary>
        public static long Bp(long value, int bp)
        {
            return MulDiv(value, bp, One);
        }

        /// <summary>Yukari yuvarlayan tamsayi bolme. Negatif b desteklenmez.</summary>
        public static int CeilDiv(int a, int b)
        {
            if (b <= 0) throw new ArgumentOutOfRangeException(nameof(b));
            if (a <= 0) return 0;
            return (a + b - 1) / b;
        }

        /// <summary>Yukari yuvarlayan tamsayi bolme, long.</summary>
        public static long CeilDivL(long a, long b)
        {
            if (b <= 0) throw new ArgumentOutOfRangeException(nameof(b));
            if (a <= 0) return 0;
            return (a + b - 1) / b;
        }

        /// <summary>
        /// baseNano'nun exp. kuvveti, nano olceginde.
        /// Birikimli zam icin. Baz puanla ussalmak %0,03 sapma uretiyordu,
        /// bu da sekizinci haftada birkac sikkeye denk geliyor.
        /// </summary>
        public static long PowNano(long baseNano, int exp)
        {
            if (exp < 0) throw new ArgumentOutOfRangeException(nameof(exp));
            long r = Nano;
            for (int i = 0; i < exp; i++)
                r = MulDiv(r, baseNano, Nano);
            return r;
        }

        /// <summary>Baz puan cinsinden bir orani nano olcegine tasir.</summary>
        public static long BpToNano(int bp)
        {
            return (long)bp * (Nano / One);
        }
    }
}
