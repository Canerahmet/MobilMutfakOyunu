using System;
using Lokanta.Core;
using Xunit;

namespace Lokanta.Core.Tests
{
    public class FxTests
    {
        [Theory]
        // exact divisions
        [InlineData(100, 1, 1, 100)]
        [InlineData(10, 10, 5, 20)]
        // half AWAY from zero, not banker's
        [InlineData(1, 1, 2, 1)]      // 0.5  -> 1   (banker's would give 0)
        [InlineData(3, 1, 2, 2)]      // 1.5  -> 2
        [InlineData(5, 1, 2, 3)]      // 2.5  -> 3   (banker's would give 2)
        [InlineData(-1, 1, 2, -1)]    // -0.5 -> -1
        [InlineData(-3, 1, 2, -2)]    // -1.5 -> -2
        [InlineData(-5, 1, 2, -3)]    // -2.5 -> -3
        // below and above a half
        [InlineData(1, 1, 3, 0)]      // 0.333 -> 0
        [InlineData(2, 1, 3, 1)]      // 0.667 -> 1
        public void MulDiv_rounds_half_away_from_zero(long a, long b, long c, long expected)
        {
            Assert.Equal(expected, Fx.MulDiv(a, b, c));
        }

        [Fact]
        public void MulDiv_is_consistent_with_a_negative_divisor_too()
        {
            // -a/-b must give the same result as a/b
            Assert.Equal(Fx.MulDiv(5, 1, 2), Fx.MulDiv(-5, 1, -2));
            Assert.Equal(Fx.MulDiv(-5, 1, 2), Fx.MulDiv(5, 1, -2));
        }

        [Fact]
        public void MulDiv_throws_on_division_by_zero()
        {
            Assert.Throws<DivideByZeroException>(() => Fx.MulDiv(1, 1, 0));
        }

        [Fact]
        public void MulDiv_does_not_silently_return_a_wrong_result_on_overflow()
        {
            Assert.Throws<OverflowException>(() => Fx.MulDiv(long.MaxValue, 2, 1));
        }

        [Theory]
        [InlineData(10000, 10000, 10000)]   // x1.0
        [InlineData(10000, 3200, 3200)]     // 32%
        [InlineData(10000, 12500, 12500)]   // x1.25
        [InlineData(4342500, 3200, 1389600)] // week 8 ingredients, centi-coins
        public void Bp_applies_the_ratio(long value, int bp, long expected)
        {
            Assert.Equal(expected, Fx.Bp(value, bp));
        }

        [Theory]
        [InlineData(0, 5, 0)]
        [InlineData(1, 5, 1)]
        [InlineData(5, 5, 1)]
        [InlineData(6, 5, 2)]
        [InlineData(97, 28, 4)]   // week 8 cook count
        [InlineData(17, 28, 1)]   // week 1
        public void CeilDiv_rounds_up(int a, int b, int expected)
        {
            Assert.Equal(expected, Fx.CeilDiv(a, b));
        }

        [Fact]
        public void CeilDiv_throws_on_a_non_positive_divisor()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Fx.CeilDiv(10, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Fx.CeilDiv(10, -1));
        }

        [Fact]
        public void PowNano_the_zeroth_power_is_one()
        {
            Assert.Equal(Fx.Nano, Fx.PowNano(1_022_000_000L, 0));
        }

        [Fact]
        public void PowNano_raises_the_experience_rise_with_enough_precision()
        {
            // 2.2% compounded, seven weeks. Expected 1.022^7 = 1.16423614...
            long r = Fx.PowNano(1_022_000_000L, 7);
            double expected = Math.Pow(1.022, 7) * 1e9;

            // On the nano scale a drift of under 100 units = 1e-7 relative error.
            Assert.InRange(r, (long)expected - 100, (long)expected + 100);
        }

        [Fact]
        public void PowNano_is_more_precise_than_basis_points()
        {
            // Why compounding in basis points is not enough: by the eighth week
            // the drift came to a few coins. This test records that difference.
            long bpChain = Fx.One;
            for (int i = 0; i < 7; i++) bpChain = Fx.Bp(bpChain, 10220);

            long nano = Fx.PowNano(1_022_000_000L, 7);
            long nanoAsBp = Fx.MulDiv(nano, Fx.One, Fx.Nano);

            double exact = Math.Pow(1.022, 7) * Fx.One;
            double bpError = Math.Abs(bpChain - exact);
            double nanoError = Math.Abs(nanoAsBp - exact);

            Assert.True(nanoError < bpError,
                $"the nano error {nanoError} must be smaller than the basis-point error {bpError}");
        }

        [Fact]
        public void BpToNano_carries_the_scale_correctly()
        {
            Assert.Equal(Fx.Nano, Fx.BpToNano(Fx.One));
            Assert.Equal(22_000_000L, Fx.BpToNano(220));
        }
    }
}
