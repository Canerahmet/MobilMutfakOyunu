using System;

namespace Lokanta.Core
{
    /// <summary>
    /// One independent randomness stream per subsystem.
    /// docs/23-core-contract.md 3.2: adding a call to one system must not
    /// shift another system's sequence.
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
        Credit = 8,     // collecting on the tab; docs/07 Turkish signature mechanic
        Regular = 9,    // whether the named regular drops in or not
        Name = 10,      // staff name; kept separate so it does NOT TOUCH the trait die
        Count = 11
    }

    /// <summary>
    /// xoshiro128**. State is four uints, it allocates nothing, it needs no
    /// 64-bit multiply on 32-bit ARM, and its state can be written straight
    /// into the save.
    /// System.Random IS NOT USED: its algorithm changed between .NET versions.
    /// </summary>
    public struct Rng : IEquatable<Rng>
    {
        private uint _s0, _s1, _s2, _s3;

        public Rng(uint s0, uint s1, uint s2, uint s3)
        {
            // If every word is zero the generator locks up.
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
        /// [0, maxExclusive). Lemire multiply-shift. It has a very small bias;
        /// accepted because it is deterministic.
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

        /// <summary>A basis-point value in 0..One.</summary>
        public int NextBp()
        {
            return NextInt(Fx.One + 1);
        }

        /// <summary>Returns true with probability bp in ten thousand.</summary>
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
    /// Seeding the streams. Four uints are produced from the (masterSeed,
    /// streamId) pair with splitmix64. The streams start independent of
    /// one another.
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
