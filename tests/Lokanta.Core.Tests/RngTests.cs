using System;
using System.Collections.Generic;
using Lokanta.Core;
using Xunit;

namespace Lokanta.Core.Tests
{
    public class RngTests
    {
        [Fact]
        public void Ayni_tohum_ayni_diziyi_verir()
        {
            Rng a = RngSeeder.Create(20260909UL, RngStream.Arrival);
            Rng b = RngSeeder.Create(20260909UL, RngStream.Arrival);

            for (int i = 0; i < 1000; i++)
                Assert.Equal(a.Next(), b.Next());
        }

        [Fact]
        public void Farkli_akislar_farkli_dizi_verir()
        {
            Rng arrival = RngSeeder.Create(20260909UL, RngStream.Arrival);
            Rng market = RngSeeder.Create(20260909UL, RngStream.Market);

            int same = 0;
            for (int i = 0; i < 500; i++)
                if (arrival.Next() == market.Next()) same++;

            Assert.True(same < 5, $"Akislar cakisiyor: 500 cekimde {same} ayni deger");
        }

        [Fact]
        public void Bir_akisa_cagri_eklemek_digerini_kaydirmaz()
        {
            // docs/23 3.4: akis bagimsizlik testi.
            Rng arrivalBefore = RngSeeder.Create(7UL, RngStream.Arrival);
            uint[] expected = new uint[20];
            for (int i = 0; i < expected.Length; i++) expected[i] = arrivalBefore.Next();

            // Baska bir akista bin cagri yap
            Rng noisy = RngSeeder.Create(7UL, RngStream.Event);
            for (int i = 0; i < 1000; i++) noisy.Next();

            Rng arrivalAfter = RngSeeder.Create(7UL, RngStream.Arrival);
            for (int i = 0; i < expected.Length; i++)
                Assert.Equal(expected[i], arrivalAfter.Next());
        }

        [Fact]
        public void Dogrudan_diziler_bagimsiz_referansla_esitr()
        {
            // Beklenen degerler C# ciktisindan degil, ayri bir Python
            // uygulamasindan geliyor (tools/balance/rng_reference.py).
            // Bir testin beklenen degerini test ettigi koddan almasi
            // hicbir sey kanitlamaz.
            RngReference reference = RngReference.Load();

            foreach (RngDirectCase c in reference.Direct)
            {
                Rng r = new Rng(c.State[0], c.State[1], c.State[2], c.State[3]);
                for (int i = 0; i < c.Values.Count; i++)
                {
                    uint got = r.Next();
                    Assert.True(c.Values[i] == got,
                        $"durum [{string.Join(",", c.State)}] cekim {i}: " +
                        $"referans {c.Values[i]}, C# {got}");
                }
            }
        }

        [Fact]
        public void Tohumlanmis_akislar_bagimsiz_referansla_esitr()
        {
            RngReference reference = RngReference.Load();

            foreach (RngSeededCase c in reference.Seeded)
            {
                Rng r = RngSeeder.Create(c.MasterSeed, (RngStream)c.StreamIndex);
                for (int i = 0; i < c.Values.Count; i++)
                {
                    uint got = r.Next();
                    Assert.True(c.Values[i] == got,
                        $"akis {c.Stream} cekim {i}: referans {c.Values[i]}, C# {got}");
                }
            }
        }

        [Fact]
        public void NextInt_bagimsiz_referansla_esitr()
        {
            RngReference reference = RngReference.Load();
            Rng r = RngSeeder.Create(42UL, RngStream.Order);

            for (int i = 0; i < reference.NextInt10.Count; i++)
            {
                int got = r.NextInt(10);
                Assert.True(reference.NextInt10[i] == got,
                    $"NextInt(10) cekim {i}: referans {reference.NextInt10[i]}, C# {got}");
            }
        }

        [Fact]
        public void Hepsi_sifir_tohum_kilitlenmez()
        {
            Rng r = new Rng(0, 0, 0, 0);
            bool anyNonZero = false;
            for (int i = 0; i < 10; i++) if (r.Next() != 0) anyNonZero = true;
            Assert.True(anyNonZero, "Sifir durum ureteci kilitledi");
        }

        [Fact]
        public void NextInt_sinirlari_asmaz()
        {
            Rng r = RngSeeder.Create(42UL, RngStream.Order);
            for (int i = 0; i < 10000; i++)
            {
                int v = r.NextInt(7);
                Assert.InRange(v, 0, 6);
            }
        }

        [Fact]
        public void NextInt_araligi_dogru_calisir()
        {
            Rng r = RngSeeder.Create(42UL, RngStream.Order);
            for (int i = 0; i < 10000; i++)
            {
                int v = r.NextInt(10, 20);
                Assert.InRange(v, 10, 19);
            }
        }

        [Fact]
        public void NextInt_gecersiz_sinirda_atar()
        {
            Rng r = RngSeeder.Create(1UL, RngStream.Order);
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(-1));
        }

        [Fact]
        public void Chance_uc_noktada_makul_davranir()
        {
            Rng r = RngSeeder.Create(99UL, RngStream.StaffError);
            Assert.False(r.Chance(0));
            Assert.True(r.Chance(Fx.One));

            int hits = 0;
            const int n = 20000;
            for (int i = 0; i < n; i++) if (r.Chance(2500)) hits++;

            // %25 bekleniyor; istatistiksel pay birakilarak
            Assert.InRange(hits, (int)(n * 0.23), (int)(n * 0.27));
        }

        [Fact]
        public void Dagilim_kaba_bir_tekduzelik_testini_gecer()
        {
            Rng r = RngSeeder.Create(20260910UL, RngStream.Archetype);
            int[] buckets = new int[10];
            const int n = 100000;
            for (int i = 0; i < n; i++) buckets[r.NextInt(10)]++;

            foreach (int b in buckets)
                Assert.InRange(b, (int)(n * 0.09), (int)(n * 0.11));
        }

        [Fact]
        public void Butun_akislar_farkli_baslangic_durumu_alir()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < (int)RngStream.Count; i++)
            {
                Rng r = RngSeeder.Create(20260909UL, (RngStream)i);
                string key = $"{r.S0}-{r.S1}-{r.S2}-{r.S3}";
                Assert.True(seen.Add(key), $"Akis {i} baska bir akisla ayni durumda basliyor");
            }
        }

        [Fact]
        public void Durum_kayittan_geri_yuklenebilir()
        {
            Rng r = RngSeeder.Create(5UL, RngStream.Hiring);
            for (int i = 0; i < 37; i++) r.Next();

            // Kayit dosyasina yazilan dort uint
            Rng restored = new Rng(r.S0, r.S1, r.S2, r.S3);

            for (int i = 0; i < 50; i++)
                Assert.Equal(r.Next(), restored.Next());
        }
    }
}
