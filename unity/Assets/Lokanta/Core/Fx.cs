using System;

namespace Lokanta.Core
{
    /// <summary>
    /// The core's ONE arithmetic helper.
    /// docs/23-core-contract.md 2.3: Math.Round, Math.Floor and truncation
    /// with (int) are banned. Every division and every rounding goes
    /// through here.
    ///
    /// The rounding rule: half AWAY FROM ZERO. Banker's rounding is not used,
    /// because it is .NET's default and two developers will mix the two up.
    /// </summary>
    public static class Fx
    {
        /// <summary>1.0 in basis points. The scale for ratios and multipliers.</summary>
        public const int One = 10_000;

        /// <summary>One person-day, in micro person-days. Staff workload is on this scale.</summary>
        public const int Micro = 1_000_000;

        /// <summary>Internal precision for compounding multipliers. It never leaks outwards.</summary>
        public const long Nano = 1_000_000_000L;

        /// <summary>One coin, in centi-coins. All money is on this scale.</summary>
        public const int Coin = 100;

        /// <summary>
        /// a * b / c, rounded half away from zero.
        /// The intermediate product is checked: a silent overflow is worse
        /// than a wrong result.
        /// </summary>
        public static long MulDiv(long a, long b, long c)
        {
            if (c == 0) throw new DivideByZeroException("Fx.MulDiv: c is zero");

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

        /// <summary>Scales a value by a basis-point multiplier. If bp = One it is unchanged.</summary>
        public static long Bp(long value, int bp)
        {
            return MulDiv(value, bp, One);
        }

        /// <summary>Integer division rounding upwards. A negative b is not supported.</summary>
        public static int CeilDiv(int a, int b)
        {
            if (b <= 0) throw new ArgumentOutOfRangeException(nameof(b));
            if (a <= 0) return 0;
            return (a + b - 1) / b;
        }

        /// <summary>Integer division rounding upwards, long.</summary>
        public static long CeilDivL(long a, long b)
        {
            if (b <= 0) throw new ArgumentOutOfRangeException(nameof(b));
            if (a <= 0) return 0;
            return (a + b - 1) / b;
        }

        /// <summary>
        /// baseNano raised to the power exp, on the nano scale.
        /// For compounding rises. Exponentiating in basis points produced a
        /// 0.03% drift, which by the eighth week comes to a few coins.
        /// </summary>
        public static long PowNano(long baseNano, int exp)
        {
            if (exp < 0) throw new ArgumentOutOfRangeException(nameof(exp));
            long r = Nano;
            for (int i = 0; i < exp; i++)
                r = MulDiv(r, baseNano, Nano);
            return r;
        }

        /// <summary>Carries a basis-point ratio up to the nano scale.</summary>
        public static long BpToNano(int bp)
        {
            return (long)bp * (Nano / One);
        }
    }
}
