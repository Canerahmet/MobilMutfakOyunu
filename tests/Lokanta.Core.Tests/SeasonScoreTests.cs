using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// Yil sonu degerlendirmesi. docs/08-endgame.md.
    ///
    /// Bu ozellik bir zamanlar TASARLANMIS ama HIC BAGLANMAMISTI: ekran
    /// yaziliydi, CampaignDays icerikte duruyordu, ve altmisinci gun gelip
    /// geciyordu. Testler o bosluğu gormedi cunku hicbiri kampanyanin
    /// sonuna kadar oynamiyordu.
    /// </summary>
    public sealed class SeasonScoreTests
    {
        private readonly ITestOutputHelper _out;
        public SeasonScoreTests(ITestOutputHelper o) { _out = o; }

        private static Simulation NewSim(string cuisine = "fastfood")
        {
            EconomyConfig economy = ContentLoader.LoadEconomy(Paths.Content);
            ContentSet content = ContentSetLoader.Load(Paths.Content, cuisine);
            TimingConfig timing = content.SlotDurationsBp != null
                ? TimingConfig.Default()
                    .WithSlotDurations(content.SlotDurationsBp)
                    .WithEatMs(content.EatMs)
                : TimingConfig.Default();
            return new Simulation(economy, content, timing, 20260911UL);
        }

        [Fact]
        public void Kampanya_bitmeden_degerlendirme_acilmiyor()
        {
            Simulation sim = NewSim();
            Assert.False(sim.SeasonJustEnded);
            Assert.True(sim.CampaignDays >= 30, "kampanya uzunlugu icerikten gelmiyor");
        }

        [Fact]
        public void Degerlendirme_bir_kez_aciliyor()
        {
            Simulation sim = NewSim();
            RunTo(sim, sim.CampaignDays + 1);

            Assert.True(sim.SeasonJustEnded, "kampanya doldu ama degerlendirme acilmiyor");

            sim.MarkSeasonScored();
            Assert.False(sim.SeasonJustEnded, "degerlendirme ikinci kez aciliyor");
        }

        [Fact]
        public void Yedi_eksen_de_sinirlar_icinde()
        {
            Simulation sim = NewSim();
            RunTo(sim, sim.CampaignDays + 1);

            SeasonScore s = sim.Score();
            for (int i = 0; i < SeasonScore.AxisCount; i++)
            {
                int v = s.AxisAt(i);
                Assert.True(v >= 0 && v <= 100,
                            SeasonScore.AxisKey(i) + " ekseni sinir disi: " + v);
            }
            Assert.True(s.Total >= 0 && s.Total <= 100);
            Assert.True(s.Plaque >= 0 && s.Plaque <= 3);

            _out.WriteLine($"toplam {s.Total}, plaket {s.Plaque}");
            for (int i = 0; i < SeasonScore.AxisCount; i++)
                _out.WriteLine($"  {SeasonScore.AxisKey(i)} = {s.AxisAt(i)}");
        }

        [Fact]
        public void Hicbir_sey_yapmayan_oyuncu_tam_puan_almiyor()
        {
            // Pasif kosu: stok alinmiyor, kimse ise alinmiyor, genisleme
            // yok. Puan dusuk OLMALI - yoksa degerlendirme hicbir sey
            // olcmuyor demektir.
            Simulation sim = NewSim();
            RunTo(sim, sim.CampaignDays + 1);

            SeasonScore s = sim.Score();
            Assert.True(s.Total < 60,
                        "hicbir sey yapmayan oyuncu " + s.Total + " puan aliyor");
            Assert.True(s.Place < 50, "genislemeyen oyuncunun mekan puani yuksek");
        }

        [Fact]
        public void Merdivene_inmek_saglamligi_dusuruyor()
        {
            Simulation sim = NewSim();
            RunTo(sim, sim.CampaignDays + 1);

            SeasonScore s = sim.Score();
            if (sim.DebtRungs > 0)
                Assert.True(s.Resilience < 100,
                            "merdivene inildigi halde saglamlik tam puan");
            else
                Assert.True(s.Resilience >= 85,
                            "hic borca dusulmedigi halde saglamlik dusuk");
        }

        /// <summary>Hicbir sey yapmadan gun gun ilerler.</summary>
        private static void RunTo(Simulation sim, int day)
        {
            while (sim.Day < day)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < 5000; t++)
                {
                    sim.Tick();
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }
        }
    }
}
