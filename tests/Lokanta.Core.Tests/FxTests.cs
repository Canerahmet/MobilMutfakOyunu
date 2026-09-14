using System;
using Lokanta.Core;
using Xunit;

namespace Lokanta.Core.Tests
{
    public class FxTests
    {
        [Theory]
        // tam bolunenler
        [InlineData(100, 1, 1, 100)]
        [InlineData(10, 10, 5, 20)]
        // yarisi sifirdan UZAGA, banker's degil
        [InlineData(1, 1, 2, 1)]      // 0,5  -> 1   (banker's 0 verirdi)
        [InlineData(3, 1, 2, 2)]      // 1,5  -> 2
        [InlineData(5, 1, 2, 3)]      // 2,5  -> 3   (banker's 2 verirdi)
        [InlineData(-1, 1, 2, -1)]    // -0,5 -> -1
        [InlineData(-3, 1, 2, -2)]    // -1,5 -> -2
        [InlineData(-5, 1, 2, -3)]    // -2,5 -> -3
        // yarim altinda ve ustunde
        [InlineData(1, 1, 3, 0)]      // 0,333 -> 0
        [InlineData(2, 1, 3, 1)]      // 0,667 -> 1
        public void MulDiv_yarisi_sifirdan_uzaga_yuvarlar(long a, long b, long c, long expected)
        {
            Assert.Equal(expected, Fx.MulDiv(a, b, c));
        }

        [Fact]
        public void MulDiv_negatif_bolen_ile_de_tutarli()
        {
            // -a/-b ile a/b ayni sonucu vermeli
            Assert.Equal(Fx.MulDiv(5, 1, 2), Fx.MulDiv(-5, 1, -2));
            Assert.Equal(Fx.MulDiv(-5, 1, 2), Fx.MulDiv(5, 1, -2));
        }

        [Fact]
        public void MulDiv_sifira_bolmede_atar()
        {
            Assert.Throws<DivideByZeroException>(() => Fx.MulDiv(1, 1, 0));
        }

        [Fact]
        public void MulDiv_tasmada_sessizce_yanlis_sonuc_vermez()
        {
            Assert.Throws<OverflowException>(() => Fx.MulDiv(long.MaxValue, 2, 1));
        }

        [Theory]
        [InlineData(10000, 10000, 10000)]   // x1,0
        [InlineData(10000, 3200, 3200)]     // %32
        [InlineData(10000, 12500, 12500)]   // x1,25
        [InlineData(4342500, 3200, 1389600)] // hafta 8 malzeme, santi-sikke
        public void Bp_oran_uygular(long value, int bp, long expected)
        {
            Assert.Equal(expected, Fx.Bp(value, bp));
        }

        [Theory]
        [InlineData(0, 5, 0)]
        [InlineData(1, 5, 1)]
        [InlineData(5, 5, 1)]
        [InlineData(6, 5, 2)]
        [InlineData(97, 28, 4)]   // hafta 8 asci sayisi
        [InlineData(17, 28, 1)]   // hafta 1
        public void CeilDiv_yukari_yuvarlar(int a, int b, int expected)
        {
            Assert.Equal(expected, Fx.CeilDiv(a, b));
        }

        [Fact]
        public void CeilDiv_negatif_bolende_atar()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Fx.CeilDiv(10, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Fx.CeilDiv(10, -1));
        }

        [Fact]
        public void PowNano_sifirinci_kuvvet_birdir()
        {
            Assert.Equal(Fx.Nano, Fx.PowNano(1_022_000_000L, 0));
        }

        [Fact]
        public void PowNano_deneyim_zammini_yeterli_hassasiyetle_ussler()
        {
            // %2,2 birikimli, yedi hafta. Beklenen 1,022^7 = 1,16423614...
            long r = Fx.PowNano(1_022_000_000L, 7);
            double expected = Math.Pow(1.022, 7) * 1e9;

            // Nano olceginde 100 birimden az sapma = 1e-7 bagil hata.
            Assert.InRange(r, (long)expected - 100, (long)expected + 100);
        }

        [Fact]
        public void PowNano_baz_puandan_daha_hassas()
        {
            // Baz puanla ussalmak neden yetmiyor: sapma sekizinci haftada
            // birkac sikkeye denk geliyordu. Bu test o farki belgeliyor.
            long bpChain = Fx.One;
            for (int i = 0; i < 7; i++) bpChain = Fx.Bp(bpChain, 10220);

            long nano = Fx.PowNano(1_022_000_000L, 7);
            long nanoAsBp = Fx.MulDiv(nano, Fx.One, Fx.Nano);

            double exact = Math.Pow(1.022, 7) * Fx.One;
            double bpError = Math.Abs(bpChain - exact);
            double nanoError = Math.Abs(nanoAsBp - exact);

            Assert.True(nanoError < bpError,
                $"nano hatasi {nanoError} baz puan hatasindan {bpError} kucuk olmali");
        }

        [Fact]
        public void BpToNano_olcegi_dogru_tasir()
        {
            Assert.Equal(Fx.Nano, Fx.BpToNano(Fx.One));
            Assert.Equal(22_000_000L, Fx.BpToNano(220));
        }
    }
}
