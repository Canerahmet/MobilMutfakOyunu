using System;

namespace Lokanta.Core
{
    /// <summary>
    /// Alt sistem basina bagimsiz rastgelelik akisi.
    /// docs/23-core-contract.md 3.2: bir sisteme cagri eklemek
    /// digerinin dizisini kaydirmamali.
    /// </summary>
    public enum RngStream
    {
        Arrival = 0,
        Archetype = 1,
        Order = 2,
        StaffError = 3,
        Market = 4,
        Event = 5,
        Hiring = 6,
        ReviewText = 7,
        Credit = 8,     // veresiye tahsilati; docs/07 Turk imza mekanigi
        Regular = 9,    // isimli duzenli musterinin ugrayip ugramadigi
        Name = 10,      // personel ismi; huy zarina DOKUNMASIN diye ayri
        Count = 11
    }

    /// <summary>
    /// xoshiro128**. Durumu dort uint, ayirma yapmaz, 32 bit ARM'de
    /// 64 bit carpma gerektirmez, kaydi dogrudan yazilabilir.
    /// System.Random KULLANILMAZ: .NET surumleri arasinda algoritmasi degisti.
    /// </summary>
    public struct Rng : IEquatable<Rng>
    {
        private uint _s0, _s1, _s2, _s3;

        public Rng(uint s0, uint s1, uint s2, uint s3)
        {
            // Hepsi sifir olursa uretec kilitlenir.
            if ((s0 | s1 | s2 | s3) == 0) s0 = 0x9E3779B9u;
            _s0 = s0; _s1 = s1; _s2 = s2; _s3 = s3;
        }

        public uint S0 { get { return _s0; } }
        public uint S1 { get { return _s1; } }
        public uint S2 { get { return _s2; } }
        public uint S3 { get { return _s3; } }

        private static uint RotL(uint x, int k)
        {
            return (x << k) | (x >> (32 - k));
        }

        public uint Next()
        {
            uint result = RotL(unchecked(_s1 * 5u), 7);
            unchecked { result *= 9u; }

            uint t = _s1 << 9;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = RotL(_s3, 11);
            return result;
        }

        /// <summary>
        /// [0, maxExclusive). Lemire carp-kaydir. Cok kucuk bir sapmasi var;
        /// deterministik oldugu icin kabul edildi.
        /// </summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            ulong m = (ulong)Next() * (ulong)(uint)maxExclusive;
            return (int)(m >> 32);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return minInclusive + NextInt(maxExclusive - minInclusive);
        }

        /// <summary>0..One arasi baz puan.</summary>
        public int NextBp()
        {
            return NextInt(Fx.One + 1);
        }

        /// <summary>bp binde on bin olasilikla dogru doner.</summary>
        public bool Chance(int bp)
        {
            if (bp <= 0) return false;
            if (bp >= Fx.One) return true;
            return NextInt(Fx.One) < bp;
        }

        public bool Equals(Rng other)
        {
            return _s0 == other._s0 && _s1 == other._s1
                && _s2 == other._s2 && _s3 == other._s3;
        }

        public override bool Equals(object obj)
        {
            return obj is Rng && Equals((Rng)obj);
        }

        public override int GetHashCode()
        {
            unchecked { return (int)(_s0 ^ _s1 ^ _s2 ^ _s3); }
        }
    }

    /// <summary>
    /// Akislarin tohumlanmasi. (masterSeed, streamId) ciftinden splitmix64 ile
    /// dort uint uretilir. Akislar birbirinden bagimsiz baslar.
    /// </summary>
    public static class RngSeeder
    {
        private const ulong Phi = 0x9E3779B97F4A7C15UL;

        public static Rng Create(ulong masterSeed, RngStream stream)
        {
            ulong z = masterSeed ^ (((ulong)(int)stream + 1UL) * Phi);
            uint a = Mix(ref z);
            uint b = Mix(ref z);
            uint c = Mix(ref z);
            uint d = Mix(ref z);
            return new Rng(a, b, c, d);
        }

        private static uint Mix(ref ulong z)
        {
            unchecked
            {
                z += Phi;
                ulong x = z;
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                x = x ^ (x >> 31);
                return (uint)(x >> 16);
            }
        }
    }
}
