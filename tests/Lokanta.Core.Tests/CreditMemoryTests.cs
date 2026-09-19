using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// A DEFAULTER IS REMEMBERED BY THE REGULAR WHO DEFAULTED.
    ///
    /// A bad account used to touch the shop's reputation and a global demand
    /// bonus and write nothing to the person: the same regular, the same
    /// odds, tomorrow. Now every account of theirs that goes bad takes two
    /// visits' worth of trust off that regular alone, and the ledger's chance
    /// shows it. The relationship asserted: for a regular with at least one
    /// bad account, trust is below what their visits alone would earn; for
    /// one with none, it is exactly that. Proved red by removing the penalty.
    /// </summary>
    public sealed class CreditMemoryTests
    {
        private readonly ITestOutputHelper _out;
        public CreditMemoryTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260919UL;

        private static Simulation Fresh(out ContentSet c)
        {
            EconomyConfig eco = ContentLoader.LoadEconomy(Paths.Content);
            c = ContentSetLoader.Load(Paths.Content, "turk");
            TimingConfig timing = c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
            return new Simulation(eco, c, timing, Seed);
        }

        private static void RunDayGrantingCredit(Simulation sim, ContentSet c)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0) sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = (c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default()).ServiceTicks + 4000;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                for (int p = 0; p < Simulation.MaxParties; p++)
                    if (sim.CreditEligible(p))
                        sim.Apply(new Command(sim.TickIndex, CommandKind.ExtendCredit, p));
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            sim.AdvanceToNextDay();
        }

        [Fact]
        public void A_bad_account_lowers_that_regulars_trust_and_nobody_elses()
        {
            Simulation sim = Fresh(out ContentSet c);
            int perVisit = c.Signature.CreditTrustPerVisitBp;
            int cap = c.Signature.CreditTrustCapBp;

            // Grant credit until somebody's account has gone bad, or the
            // campaign runs out. Sixty per cent of accounts settle, so a
            // default arrives well inside the season the tab is open.
            int days = 0, defaulter = -1;
            while (days < 58 && defaulter < 0)
            {
                RunDayGrantingCredit(sim, c);
                days++;
                for (int r = 0; r < sim.RegularCount; r++)
                    if (sim.RegularDefaults(r) > 0) { defaulter = r; break; }
            }
            _out.WriteLine($"after {days} days: regular {defaulter} has {(defaulter >= 0 ? sim.RegularDefaults(defaulter) : 0)} bad account(s)");
            Assert.True(defaulter >= 0, "no account went bad in " + days + " days, so there is nothing to remember");

            int earned = 0, remembered = 0;
            for (int r = 0; r < sim.RegularCount; r++)
            {
                long fromVisits = (long)sim.RegularVisits(r) * perVisit;
                int expectedClean = (int)(fromVisits > cap ? cap : fromVisits);
                int trust = sim.RegularTrustBp(r);
                if (sim.RegularDefaults(r) == 0)
                {
                    Assert.True(trust == expectedClean,
                        $"regular {r} has no bad account and trust {trust} against {expectedClean} from visits");
                    earned++;
                }
                else
                {
                    _out.WriteLine($"  regular {r}: visits {sim.RegularVisits(r)}, defaults {sim.RegularDefaults(r)}, trust {trust} (visits alone {expectedClean})");
                    Assert.True(trust < expectedClean || expectedClean == 0,
                        $"regular {r} defaulted and still has the trust of their visits ({trust}): the default was not remembered");
                    remembered++;
                }
            }
            Assert.True(remembered > 0 && earned > 0, "both kinds of regular are needed for the comparison");
        }
    }
}
