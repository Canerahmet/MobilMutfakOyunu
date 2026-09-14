using System.Collections.Generic;
using System.IO;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// Personel huylari ve moral. docs/14.
    ///
    /// Tasarim niyeti orada yazili: "hicbir huy saf iyi veya saf kotu
    /// degil. Cirak ucuz ama yavas, tecrubeli hizli ama pahali."
    ///
    /// Moral kismindaki en onemli sey docs/14'un kendi cumlesi: "sayi
    /// kaybetmek soyut, adini bildigin bir calisanin istifa etmesi somut."
    /// </summary>
    public class TraitTests
    {
        private readonly ITestOutputHelper _out;
        public TraitTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "fastfood");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim() =>
            new Simulation(Economy(), Content(), Timing(), Seed);

        private static void Restock(Simulation sim)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }
        }

        private static DayReport RunOneDay(Simulation sim)
        {
            Restock(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing().ServiceTicks + 4000;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            DayReport r = sim.BuildDayReport();
            sim.AdvanceToNextDay();
            return r;
        }

        // ====================================================================
        // Icerik
        // ====================================================================
        [Fact]
        public void On_iki_huy_yukleniyor()
        {
            EconomyConfig e = Economy();
            Assert.Equal(12, e.TraitCount);       // docs/09 envanteri
        }

        [Fact]
        public void Cakismalar_SIMETRIK()
        {
            // Tek yonlu yazilmis bir cakisma, ise alim kodunda sessizce
            // calismayan bir kural birakir: iki cakisan huyu tasiyan bir
            // personel uretilebilirdi.
            EconomyConfig e = Economy();
            for (int i = 0; i < e.TraitCount; i++)
            {
                TraitDef t = e.TraitAt(i);
                foreach (int other in t.ConflictsWith)
                    Assert.True(e.TraitAt(other).ConflictsWithIndex(i),
                                t.Id + " <-> " + e.TraitAt(other).Id + " tek yonlu");
            }
        }

        [Fact]
        public void Tek_yonlu_cakisma_reddediliyor()
        {
            List<TraitDto> traits = JsonConvert.DeserializeObject<List<TraitDto>>(
                File.ReadAllText(Path.Combine(Paths.Content, "staff-traits.json")));
            traits[0].ConflictsWith.Clear();      // digeri hala onu isaret ediyor

            EconomyDto eco = JsonConvert.DeserializeObject<EconomyDto>(
                File.ReadAllText(Path.Combine(Paths.Content, "economy.json")));
            List<StaffRoleDto> roles = JsonConvert.DeserializeObject<List<StaffRoleDto>>(
                File.ReadAllText(Path.Combine(Paths.Content, "staff-roles.json")));

            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.Build(eco, roles, traits));
            _out.WriteLine(ex.Message);
            Assert.Contains("tek yonlu", ex.Message);
        }

        [Fact]
        public void Cirak_ve_tecrubeli_zit_yazilmis()
        {
            // docs/14: cirak ucuz ama yavas, tecrubeli hizli ama pahali.
            // Ikisi de bir seyi VERIYOR ve bir seyi ALIYOR.
            EconomyConfig e = Economy();
            TraitDef cirak = null, tecrubeli = null;
            for (int i = 0; i < e.TraitCount; i++)
            {
                if (e.TraitAt(i).Id == "cirak") cirak = e.TraitAt(i);
                if (e.TraitAt(i).Id == "tecrubeli") tecrubeli = e.TraitAt(i);
            }
            Assert.NotNull(cirak);
            Assert.NotNull(tecrubeli);

            Assert.True(cirak.WageBp < 0 && cirak.SpeedBp < 0 && cirak.XpBp > 10000);
            Assert.True(tecrubeli.WageBp > 0 && tecrubeli.SpeedBp > 0 && tecrubeli.XpBp == 0);
        }

        // ====================================================================
        // Davranis
        // ====================================================================
        [Fact]
        public void Herkes_iki_huy_aliyor_ve_cakismiyorlar()
        {
            EconomyConfig e = Economy();
            Simulation sim = NewSim();

            // ONCE GENISLE, SONRA ISE AL.
            //
            // Birinci kademe kadro tavani UC (docs/14) ve oyun artik iki
            // kisiyle basliyor (bir asci, bir garson) - yani tavan
            // genislemeden yalnizca TEK ise alim sigiyor ve test bir
            // kisiyi sinayip "hepsi iki huyla geldi" diyordu. Bir kisilik
            // ornek, "herkes" iddiasini tasimiyor.
            //
            // Genisleme tavani buyutuyor; sinanan sey yine sayi degil
            // HER personelin iki uyumlu huyla gelmesi.
            sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, 1));
            for (int i = 0; i < 6; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, i % 2, i % 3));

            int people = 0;
            for (int pool = 0; pool < 2; pool++)
            {
                int count = pool == 0 ? sim.Cooks : sim.SalonStaff;
                for (int i = 0; i < count; i++)
                {
                    // DEVRALINAN KADRO huysuz basliyor (ayri test): hem
                    // ilk asci hem ilk garson. Karakter, SECTIGIN
                    // kisilerle geliyor - yalnizca ISE ALINANLAR
                    // sinaniyor.
                    if (i == 0) continue;
                    int a = sim.StaffTrait(pool, i, 0);
                    int b = sim.StaffTrait(pool, i, 1);
                    Assert.True(a >= 0, "personel huysuz kaldi");
                    Assert.True(b >= 0, "personel tek huyla kaldi");
                    Assert.NotEqual(a, b);
                    Assert.False(e.TraitAt(a).ConflictsWithIndex(b),
                                 e.TraitAt(a).Id + " ile " + e.TraitAt(b).Id + " birlikte olamaz");
                    people++;
                }
            }
            // Kadro tavani yine sinirliyor (docs/14), ama genislemeden
            // sonra birkac kisi sigiyor. Sinanan sey sayi degil, HER
            // personelin iki uyumlu huyla gelmesi.
            _out.WriteLine($"{people} personel, hepsi iki uyumlu huyla");
            Assert.True(people >= 2);
        }

        [Fact]
        public void Baslangic_ascisi_HUYSUZ_ama_moralli_basliyor()
        {
            // Iki ayri sey, ikisi de olculerek karara baglandi.
            //
            // MORAL: RollTraits yalnizca Hire icinden cagriliyordu, oysa
            // oyun bir asciyla BASLIYOR. O ascinin morali 0 ile basliyordu -
            // istifa esiginin ALTINDA - ve restoran IKINCI GUN ascisiz
            // kaliyordu.
            //
            // HUY: baslangic ascisini oyuncu SECMIYOR. Ona rastgele huy
            // atmak, kampanyanin ilk gununde gorunmez bir zar atmak demek;
            // olcumde kotu huylu bir baslangic ascisi ceken kosu altmis gun
            // boyunca toparlanamiyordu. Devraldigin asci SIRADAN; karakter,
            // SECTIGIN kisilerle geliyor.
            Simulation sim = NewSim();
            Assert.Equal(1, sim.Cooks);
            Assert.Equal(-1, sim.StaffTrait(0, 0, 0));
            Assert.Equal(-1, sim.StaffTrait(0, 0, 1));
            Assert.Equal(Economy().StartingMorale, sim.StaffMorale(0, 0));
        }

        [Fact]
        public void Aday_havuzu_uc_kisi_ve_uc_gunde_bir_yenileniyor()
        {
            // docs/14: "Ise alim ekraninda ayni anda uc aday gorunur...
            // Aday havuzu her uc gunde bir yenilenir. Begenmedigin adayi
            // reddedebilirsin ama yenisi hemen gelmez."
            Simulation sim = NewSim();
            for (int slot = 0; slot < Simulation.CandidateSlots; slot++)
                Assert.True(sim.CandidateTrait(1, slot, 0) >= 0,
                            "aday havuzu bos");

            // Alinan aday havuzdan cikiyor ve yeri hemen dolmuyor.
            int taken = 0;
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, taken));
            Assert.Equal(-1, sim.CandidateTrait(1, taken, 0));

            // Ertesi gun hala bos: havuz uc gunde bir yenileniyor.
            RunOneDay(sim);
            Assert.Equal(-1, sim.CandidateTrait(1, taken, 0));

            for (int d = 0; d < 3; d++) RunOneDay(sim);
            Assert.True(sim.CandidateTrait(1, taken, 0) >= 0, "havuz hic yenilenmedi");
        }

        [Fact]
        public void Iyi_yonetilen_dukkanda_kadro_istifa_etmiyor()
        {
            // docs/14 moral tablosu bir OLAY listesi, sürükleniş modeli
            // degil. Yalnizca olaylari uygulayinca merdiven tek yonlu asagi
            // gidiyordu ve butun kadro bir ayda istifa ediyordu.
            Simulation sim = NewSim();
            int startCooks = sim.Cooks;

            for (int d = 0; d < 40; d++) RunOneDay(sim);

            _out.WriteLine($"kirk gun sonra asci {sim.Cooks}, " +
                           $"moral {sim.StaffMorale(0, 0)}, kasa {sim.Cash / 100}");
            Assert.Equal(startCooks, sim.Cooks);
            Assert.True(sim.StaffMorale(0, 0) >= Economy().MoraleQuitThreshold);
        }

        [Fact]
        public void Maas_odenemezse_moral_dusuyor()
        {
            // docs/14: "maas gecikti -25", ve bu batma merdiveninin ucuncu
            // kademesi. Kasayi bosaltip haftalik odemeyi bekliyoruz.
            Simulation sim = NewSim();
            int before = sim.StaffMorale(0, 0);

            // Tavana kadar ise al: maas faturasi kasayi asiyor.
            for (int i = 0; i < 8; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1));

            // Butun parayi malzemeye yatir, sonra hafta sonunu bekle.
            for (int d = 0; d < 8; d++)
            {
                for (int i = 0; i < sim.IngredientCount; i++)
                {
                    int need = sim.RecommendedRestock(i);
                    if (need > 0)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient,
                                              i, need * 6));
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }

            _out.WriteLine($"moral {before} -> {sim.StaffMorale(0, 0)}, kasa {sim.Cash / 100}");
            Assert.True(sim.StaffMorale(0, 0) < before,
                        "maas odenemedigi halde moral dusmedi");
        }

        [Fact]
        public void Huy_ucrete_yansiyor()
        {
            // docs/14: cirak -%25, tecrubeli +%30. Kadro carpani ortalama.
            Simulation sim = NewSim();
            int bp = sim.TraitWageMultiplierBp();
            _out.WriteLine($"tek ascili kadronun ucret carpani {bp} bp");
            Assert.True(bp >= 1000);

            // Kadro buyudukce carpan 1'e yaklasmali: farkli huylar birbirini
            // dengeliyor.
            for (int i = 0; i < 8; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1));
            int wide = sim.TraitWageMultiplierBp();
            _out.WriteLine($"dokuz kisilik kadroda {wide} bp");
            Assert.True(System.Math.Abs(wide - 10000) <= System.Math.Abs(bp - 10000) + 1500);
        }

        [Fact]
        public void Kayit_huy_ve_morali_tasiyor()
        {
            Simulation sim = NewSim();
            for (int i = 0; i < 3; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1));
            for (int d = 0; d < 12; d++) RunOneDay(sim);

            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);
            Simulation restored = NewSim();
            restored.Restore(new JsonStateReader(w.ToJson()));

            for (int pool = 0; pool < 2; pool++)
            {
                int count = pool == 0 ? sim.Cooks : sim.SalonStaff;
                for (int i = 0; i < count; i++)
                {
                    Assert.Equal(sim.StaffTrait(pool, i, 0), restored.StaffTrait(pool, i, 0));
                    Assert.Equal(sim.StaffTrait(pool, i, 1), restored.StaffTrait(pool, i, 1));
                    Assert.Equal(sim.StaffMorale(pool, i), restored.StaffMorale(pool, i));
                }
            }
            Assert.Equal(sim.StateHash(), restored.StateHash());
        }
    }
}
