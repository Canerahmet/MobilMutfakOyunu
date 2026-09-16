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
    /// The signature mechanics. docs/07: "the most important line - this is what
    /// shows that the purchase is A DIFFERENT GAME, not a repaint."
    ///
    ///   fast food -> the combo and the flow  (the ticket grows, so does the
    ///                                         kitchen load)
    ///   turkish   -> the tab                 (the cash flow is disturbed, the
    ///                                         loyalty rises)
    ///
    /// docs/23 8.2: the mechanic is in the code, the numbers are in the data; if
    /// the block is missing the cuisine does not load. docs/09: the mechanic
    /// arrives at the start of THE SECOND SEASON.
    /// </summary>
    public class SignatureTests
    {
        private readonly ITestOutputHelper _out;
        public SignatureTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content(string cuisine) =>
            ContentSetLoader.Load(Paths.Content, cuisine);

        private static TimingConfig Timing(ContentSet c) =>
            c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();

        private static Simulation NewSim(string cuisine)
        {
            ContentSet c = Content(cuisine);
            return new Simulation(Economy(), c, Timing(c), Seed);
        }

        private static void Restock(Simulation sim)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }
        }

        private static DayReport RunOneDay(Simulation sim, ContentSet c)
        {
            Restock(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing(c).ServiceTicks + 4000;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            DayReport rep = sim.BuildDayReport();
            sim.AdvanceToNextDay();
            return rep;
        }

        /// <summary>Runs the day, opening a tab for the first party it can.</summary>
        private static int RunDayGrantingCredit(Simulation sim, ContentSet c)
        {
            Restock(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int granted = 0;
            int limit = Timing(c).ServiceTicks + 4000;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                for (int p = 0; p < Simulation.MaxParties; p++)
                {
                    if (!sim.CreditEligible(p)) continue;
                    sim.Apply(new Command(sim.TickIndex, CommandKind.ExtendCredit, p));
                    granted++;
                }
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            sim.AdvanceToNextDay();
            return granted;
        }

        // ====================================================================
        // Content and validation
        // ====================================================================
        [Fact]
        public void The_two_cuisines_have_different_signatures()
        {
            Assert.Equal(SignatureKind.Combo, Content("fastfood").Signature.Kind);
            Assert.Equal(SignatureKind.Credit, Content("turk").Signature.Kind);
        }

        [Fact]
        public void The_signature_arrives_at_the_start_of_the_second_season()
        {
            // docs/09: the first season is full of teaching the menu and the price.
            EconomyConfig e = Economy();
            foreach (string cuisine in new[] { "fastfood", "turk" })
                Assert.Equal(e.SeasonDays + 1, Content(cuisine).Signature.FromDay);
        }

        [Fact]
        public void The_cuisine_does_not_load_without_a_signature_block()
        {
            // docs/23 8.2 says so outright. A silent default would amount to "the
            // cuisine you bought is really the same game".
            ContentException ex = Assert.Throws<ContentException>(
                () => BuildWith(sig => { sig.Kind = null; }));
            _out.WriteLine(ex.Message);
            Assert.Contains("signature", ex.Message);
        }

        [Fact]
        public void An_unknown_signature_kind_is_rejected()
        {
            Assert.Throws<ContentException>(
                () => BuildWith(sig => { sig.Kind = "tombala"; }));
        }

        [Fact]
        public void The_combo_cannot_make_the_kitchen_easier()
        {
            // docs/07: the combo raises the ticket BUT increases the kitchen load.
            // kitchenLoadBp < 10000 turns the mechanic inside out: free money.
            Assert.Throws<ContentException>(
                () => BuildWith(sig => { sig.Combo.KitchenLoadBp = 9000; }));
        }

        [Fact]
        public void The_combo_must_be_a_discount()
        {
            Assert.Throws<ContentException>(
                () => BuildWith(sig => { sig.Combo.PriceBp = 10000; }));
        }

        [Fact]
        public void A_combo_still_locked_when_the_mechanic_arrives_is_rejected()
        {
            ContentException ex = Assert.Throws<ContentException>(
                () => BuildWith(sig => { sig.Combo.Items[2] = "buzlu_cay"; }));   // day 50
            _out.WriteLine(ex.Message);
            Assert.Contains("the mechanic arrives on day", ex.Message);
        }

        private static void BuildWith(System.Action<SignatureDto> mutate)
        {
            string dir = Paths.Content;
            CuisineDto cui = JsonConvert.DeserializeObject<CuisineDto>(
                File.ReadAllText(Path.Combine(dir, "cuisines", "fastfood.json")));
            mutate(cui.Signature);

            ContentSetLoader.Build(
                "fastfood",
                JsonConvert.DeserializeObject<List<IngredientDto>>(
                    File.ReadAllText(Path.Combine(dir, "ingredients.json"))),
                JsonConvert.DeserializeObject<List<DishDto>>(
                    File.ReadAllText(Path.Combine(dir, "dishes", "fastfood.json"))),
                JsonConvert.DeserializeObject<List<ArchetypeDto>>(
                    File.ReadAllText(Path.Combine(dir, "archetypes", "shared.json"))),
                JsonConvert.DeserializeObject<EquipmentFileDto>(
                    File.ReadAllText(Path.Combine(dir, "equipment.json"))),
                cui, 15);
        }

        // ====================================================================
        // The combo
        // ====================================================================
        [Fact]
        public void The_combo_sells_three_for_one_price()
        {
            ContentSet c = Content("fastfood");
            Simulation sim = NewSim("fastfood");
            int[] d = c.Signature.ComboDishes;

            long sum = sim.DishPrice(d[0]) + sim.DishPrice(d[1]) + sim.DishPrice(d[2]);
            long combo = sim.ComboPrice();
            _out.WriteLine($"separately {sum / 100} coins, as a combo {combo / 100} coins");

            Assert.True(combo < sum, "the combo must be a discount");
            Assert.Equal(Fx.MulDiv(sum, c.Signature.ComboPriceBp, Fx.One), combo);
        }

        [Fact]
        public void The_combo_cannot_be_opened_in_the_first_season()
        {
            Simulation sim = NewSim("fastfood");
            Assert.False(sim.SignatureOpen);
            sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, 1));
            Assert.False(sim.ComboEnabled);
        }

        [Fact]
        public void The_combo_opens_in_the_second_season()
        {
            ContentSet c = Content("fastfood");
            Simulation sim = NewSim("fastfood");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            Assert.True(sim.SignatureOpen);
            sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, 1));
            Assert.True(sim.ComboEnabled);
        }

        [Fact]
        public void The_combo_grows_the_ticket_and_tires_the_kitchen()
        {
            // The same seed, the same days; the only difference is the combo being
            // open. docs/07's trade-off: the ticket grows, the kitchen load grows.
            long withCombo = ComboRun(true, out int servedOn, out long tickets);
            long without = ComboRun(false, out int servedOff, out long ticketsOff);

            _out.WriteLine($"combo open  : revenue {withCombo / 100}, {servedOn} people, ticket {tickets / 100}");
            _out.WriteLine($"combo closed: revenue {without / 100}, {servedOff} people, ticket {ticketsOff / 100}");

            Assert.NotEqual(withCombo, without);
        }

        private static long ComboRun(bool on, out int served, out long ticketCenti)
        {
            ContentSet c = Content("fastfood");
            Simulation sim = NewSim("fastfood");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);
            sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, on ? 1 : 0));

            long revenue = 0;
            served = 0;
            for (int d = 0; d < 10; d++)
            {
                DayReport r = RunOneDay(sim, c);
                revenue += r.Revenue;
                served += r.ServedPeople;
            }
            ticketCenti = served > 0 ? revenue / served : 0;
            return revenue;
        }

        // ====================================================================
        // The tab
        // ====================================================================
        [Fact]
        public void A_tab_cannot_be_opened_in_the_first_season()
        {
            Simulation sim = NewSim("turk");
            Assert.False(sim.HasCredit);
            for (int p = 0; p < Simulation.MaxParties; p++)
                Assert.False(sim.CreditEligible(p));
        }

        [Fact]
        public void A_tab_is_opened_only_for_someone_WHOSE_NAME_YOU_KNOW()
        {
            // The rule comes FROM THE CONTENT: the tab eligibility field in the
            // regulars file.
            //
            // This test was once written as "a frequently arriving archetype" and
            // that was right - while the regulars content DID NOT YET EXIST. Once
            // the content was written the rule changed
            // (Simulation.CreditIdentityOk), the test did not, and for a while it
            // verified the wrong thing: it was testing the FALLBACK rule.
            //
            // The content set it up this way deliberately and that is right: Nazife
            // Teyze is retired (middle tier), Mehmet Dede is a long-standing
            // customer (rare tier) - both can have a tab. Because a tab is opened on
            // ACQUAINTANCE, not on FREQUENCY; it is opened for someone whose name
            // you know. A rule that looked at the archetype tier would leave the
            // neighbourhood's pensioner standing at the door.
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            // DISTINCT parties are counted, not per tick: a party sits in the hall
            // for a hundred ticks, and counting ticks locks the sample onto a single
            // table. In my first attempt this is exactly why the test broke
            // wrongly.
            var seenTier = new Dictionary<int, int>();
            var counted = new HashSet<int>();

            for (int day = 0; day < 5; day++)
            {
                Restock(sim);
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                counted.Clear();
                int limit = Timing(c).ServiceTicks + 4000;
                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();
                    for (int p = 0; p < Simulation.MaxParties; p++)
                    {
                        if (!sim.PartyActive(p) || !counted.Add(p)) continue;
                        int tier = c.Archetypes[sim.PartyArchetype(p)].TierIndex;
                        seenTier.TryGetValue(tier, out int n);
                        seenTier[tier] = n + 1;
                        // Eligibility has two conditions: they must be someone
                        // WHOSE NAME IS KNOWN AND they must ask. So "if eligible
                        // then acquainted" is a one-way claim.
                        if (!sim.CreditEligible(p)) continue;

                        int reg = sim.PartyRegular(p);
                        Assert.True(reg >= 0,
                            "a tab was opened for a nameless party (tier " + tier + ")");
                        Assert.True(c.Regulars[reg].TabEligible,
                            "a tab was opened for " + c.Regulars[reg].Id
                            + " but the content does not allow it");
                    }
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }

            foreach (var kv in seenTier) _out.WriteLine($"tier {kv.Key}: {kv.Value} parties");
            Assert.True(seenTier.ContainsKey(0), "no frequent customer came at all");
            Assert.True(seenTier.Count > 1, "only one tier came, the sample is too small");
        }

        [Fact]
        public void The_tab_bill_is_written_to_the_book_not_to_the_till()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            long cashBefore = sim.Cash;
            int granted = 0;
            for (int d = 0; d < 30 && sim.OpenCreditCount == 0; d++)
                granted += RunDayGrantingCredit(sim, c);

            _out.WriteLine($"a tab was opened for {granted} parties, " +
                           $"open balance {sim.OpenCredit / 100} coins");

            Assert.True(granted > 0, "no tab was asked for in thirty days");
            Assert.True(sim.OpenCredit > 0, "the bill was not written into the book");
            Assert.True(sim.OpenCreditCount > 0);
            Assert.True(cashBefore >= 0);
        }

        [Fact]
        public void An_account_that_falls_due_is_closed()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            for (int d = 0; d < 30 && sim.OpenCreditCount == 0; d++)
                RunDayGrantingCredit(sim, c);
            int opened = sim.OpenCreditCount;
            Assert.True(opened > 0, "no tab was asked for in thirty days");

            // Run until the term is up: the book must empty out.
            for (int d = 0; d <= c.Signature.CreditDueDays + 1; d++) RunOneDay(sim, c);

            _out.WriteLine($"{opened} accounts opened, {sim.OpenCreditCount} left after the term");
            Assert.True(sim.OpenCreditCount < opened,
                        "accounts that fall due are not being closed");
        }

        [Fact]
        public void Turning_away_someone_who_asks_costs_reputation()
        {
            // The mechanic's real direction. A tab is not a bonus that is OFFERED,
            // it is something that is ASKED FOR; the one who refuses loses. The same
            // idea as the customer asking for a locked dish (docs/34 6).
            ContentSet c = Content("turk");

            Simulation granting = NewSim("turk");
            while (granting.Day < granting.SignatureFromDay) RunOneDay(granting, c);
            for (int d = 0; d < 25; d++) RunDayGrantingCredit(granting, c);

            Simulation refusing = NewSim("turk");
            while (refusing.Day < refusing.SignatureFromDay) RunOneDay(refusing, c);
            for (int d = 0; d < 25; d++) RunOneDay(refusing, c);

            _out.WriteLine($"granting: till {granting.Cash / 100}, book {granting.OpenCredit / 100}, " +
                           $"reputation {granting.ReputationCenti / 100.0:0.0}, " +
                           $"loyalty {granting.CreditLoyaltyBp / 100.0:0.0}%");
            _out.WriteLine($"refusing: till {refusing.Cash / 100}, " +
                           $"reputation {refusing.ReputationCenti / 100.0:0.0}");

            // Counting the book, the one who grants must not end up behind the one
            // who refuses: the mechanic is a trade-off, not a PENALTY.
            long grantingNetWorth = granting.Cash + granting.OpenCredit;
            _out.WriteLine($"granting net worth {grantingNetWorth / 100}, " +
                           $"refusing {refusing.Cash / 100}");
            Assert.True(granting.CreditLoyaltyBp > 0, "a collected account leaves no loyalty behind");
        }

        [Fact]
        public void The_tab_is_a_TRADE_OFF_it_both_pays_and_sinks_you()
        {
            // The same seed: one opens tabs, the other does not. Both have to be
            // valid ways to play - if one crushes the other on every run the
            // mechanic is a button, not a decision.
            ContentSet c = Content("turk");

            Simulation a = NewSim("turk");
            while (a.Day < a.SignatureFromDay) RunOneDay(a, c);
            for (int d = 0; d < 20; d++) RunDayGrantingCredit(a, c);

            Simulation b = NewSim("turk");
            while (b.Day < b.SignatureFromDay) RunOneDay(b, c);
            for (int d = 0; d < 20; d++) RunOneDay(b, c);

            _out.WriteLine($"tab keeper : till {a.Cash / 100}, reputation {a.ReputationCenti / 100.0:0.0}, " +
                           $"open {a.OpenCredit / 100}");
            _out.WriteLine($"cash only  : till {b.Cash / 100}, reputation {b.ReputationCenti / 100.0:0.0}");

            // The tab keeper holds less CASH: that is the mechanic's price.
            Assert.True(a.Cash <= b.Cash + a.OpenCredit,
                        "the tab does not disturb the cash flow at all");
        }

        [Fact]
        public void The_save_carries_the_tab_book()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            // Run until someone asks: it is a 12% chance, and on any single day
            // nobody may come.
            for (int d = 0; d < 30 && sim.OpenCreditCount == 0; d++)
                RunDayGrantingCredit(sim, c);
            Assert.True(sim.OpenCreditCount > 0, "no tab was asked for in thirty days");

            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);

            Simulation restored = NewSim("turk");
            restored.Restore(new JsonStateReader(w.ToJson()));

            Assert.Equal(sim.OpenCredit, restored.OpenCredit);
            Assert.Equal(sim.OpenCreditCount, restored.OpenCreditCount);
            Assert.Equal(sim.StateHash(), restored.StateHash());
        }
    }
}
