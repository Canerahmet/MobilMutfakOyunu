using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Save;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// BADGES AND THE WEEKLY REPORT CARD.
    ///
    /// The two of them close the same gap: the game scored on seven axes and the
    /// player saw them EXACTLY ONCE - on the sixtieth day.
    ///
    /// This file's real job is to hold down a class of SILENT BUG. The danger with
    /// badges is not that they break but that they are HANDED OUT UNEARNED: if one
    /// condition is written wrongly the game awards a badge on day one, nothing
    /// errors, and the recognition becomes worthless. "The tab book is closed"
    /// carries exactly that trap - a player who has never opened a tab also has an
    /// open balance of zero.
    /// </summary>
    public sealed class BadgeTests
    {
        private readonly ITestOutputHelper _out;
        public BadgeTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260914UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);

        private static ContentSet Content(string cuisine) =>
            ContentSetLoader.Load(Paths.Content, cuisine);

        private static TimingConfig Timing(string cuisine)
        {
            ContentSet c = Content(cuisine);
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim(string cuisine = "fastfood") =>
            new Simulation(Economy(), Content(cuisine), Timing(cuisine), Seed);

        /// <summary>Advances day by day without doing anything.</summary>
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

        /// <summary>
        /// THE TAB BOOK BADGE IS NOT HANDED OUT UNEARNED.
        ///
        /// The bug this test holds down is silent: if the condition were only "the
        /// open balance is zero", a player who never opened a tab would get the
        /// badge at the end of DAY ONE. Nothing breaks, no exception is thrown -
        /// only the recognition becomes worthless.
        /// </summary>
        [Fact]
        public void A_book_that_was_never_opened_does_not_count_as_closed()
        {
            Simulation sim = NewSim("turk");
            RunTo(sim, 5);

            _out.WriteLine($"day {sim.Day}, open tab {sim.OpenCredit}");

            // LIVENESS FIRST: the book really must be empty, otherwise the test
            // would pass for the wrong reason rather than the right one.
            Assert.Equal(0, sim.OpenCredit);
            Assert.False(sim.HasBadge(Badges.TabBookClosed),
                "the 'tab book closed' badge was handed out without a tab ever being opened");
        }

        /// <summary>
        /// A badge is not marked "earned today" a SECOND time.
        ///
        /// The evening screen only shows what was earned today; because the
        /// condition keeps being met every day (for example as long as the till
        /// stays above ten thousand) the badge would come up as "new" again every
        /// evening.
        /// </summary>
        [Fact]
        public void A_badge_is_earned_a_single_time()
        {
            Simulation sim = NewSim();

            // "EARNED TODAY" CAN ONLY BE READ IN THE EVENING.
            //
            // AdvanceToNextDay resets it, so the value lives between CloseDay and
            // the opening of the next day - exactly the window in which the evening
            // screen is shown. The first test I wrote skipped that and looked AFTER
            // the day had advanced, and it always saw zero: the badge was working
            // correctly, the measurement was looking in the wrong place.
            int earnedOnDay = 0;
            for (int day = 1; day <= 40 && earnedOnDay == 0; day++)
            {
                ReasonableMorning(sim);
                RunTheDay(sim);
                if (sim.BadgeEarnedToday(Badges.FirstTenThousand)) earnedOnDay = sim.Day;
                sim.AdvanceToNextDay();
            }

            _out.WriteLine($"the ten thousand badge on day {earnedOnDay}, till {sim.Cash}");
            Assert.True(earnedOnDay > 0,
                "the till never reached ten thousand in forty days - the measurement did not run");

            // The next day: the badge IS STILL THERE but is no longer "today's".
            RunTheDay(sim);
            Assert.True(sim.HasBadge(Badges.FirstTenThousand), "an earned badge disappeared");
            Assert.False(sim.BadgeEarnedToday(Badges.FirstTenThousand),
                "the badge was marked 'earned today' a second time");
        }

        /// <summary>
        /// THE REASONABLE PLAYER: buys stock and builds the crew tomorrow needs.
        ///
        /// Measuring with the passive bot misled: a player who does nothing meets
        /// the peak with one cook and one waiter and, naturally, nobody leaves
        /// happy. A result of "no badge was earned" showed NOT that the badges were
        /// unreachable but that the measuring player was playing badly.
        /// </summary>
        private static void ReasonableMorning(Simulation sim)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0)
                    sim.Apply(new Command(sim.TickIndex,
                                          CommandKind.OrderIngredient, i, need));
            }

            // EXPANDING IS PART OF REASONABLE PLAY TOO.
            //
            // It was missing in the first measurement and the "expansion" and
            // "reputation 90" badges appeared NEVER to be earned. The reason was
            // not the badges but the measuring player: in a restaurant that never
            // grows, the reputation is clamped to the table tier's ceiling, so 90 is
            // impossible from the start.
            // The rule is the same as the tour's: if three times the cost is in the
            // till.
            // NO EXPANSION IN THE FIRST WEEK.
            //
            // Measured: the 3x rule is ALREADY met on day one (8,000 coins to start,
            // the first tier 2,500), and when this measuring player expands that day
            // it finishes the sixtieth day on four tables, zero reputation and zero
            // in the till - the rent has gone from 850 to 1,950 while there are not
            // yet the customers to fill it.
            //
            // This is not a BALANCE finding but a result that shows the measuring
            // tool's limits: the tour uses the same 3x rule and reaches 14 tables and
            // 96.8 reputation by the fortieth day - because it does other things
            // right as well. The player here only knows about stock and crew, so it
            // is given a week's patience.
            if (sim.Day >= 8)
                for (int i = 1; i < sim.TierCount; i++)
                {
                    if (sim.TablesAtTier(i) <= sim.TableCount) continue;
                    long cost = sim.UpgradeCostFor(i);
                    if (cost > 0 && sim.Cash >= cost * 3)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, i));
                    break;
                }

            Crew needed = sim.RequiredCrewTomorrow();
            while (sim.Cooks < needed.Cooks && sim.Cooks < sim.StaffCap)
            {
                int before = sim.Cooks;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, 0));
                if (sim.Cooks == before) break;         // the cap, or the money
            }
            while (sim.HallStaff < needed.Hall && sim.Cooks + sim.HallStaff < sim.StaffCap)
            {
                int before = sim.HallStaff;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, 0));
                if (sim.HallStaff == before) break;
            }
        }

        /// <summary>Opens the day, finishes the service, closes it - DOES NOT ADVANCE.</summary>
        private static void RunTheDay(Simulation sim)
        {
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 5000; t++) { sim.Tick(); if (sim.ServiceComplete) break; }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
        }

        /// <summary>
        /// The weekly report card comes out AT THE CLOSE of the seventh day - not on
        /// the sixth and not on the eighth.
        /// </summary>
        [Fact]
        public void The_report_card_comes_out_on_the_seventh_day()
        {
            Simulation sim = NewSim();

            RunTo(sim, 6);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 5000; t++) { sim.Tick(); if (sim.ServiceComplete) break; }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            Assert.False(sim.WeekReportReady, "the report card came out on the sixth day");

            sim.AdvanceToNextDay();
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 5000; t++) { sim.Tick(); if (sim.ServiceComplete) break; }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));

            Assert.Equal(7, sim.Day);
            Assert.True(sim.WeekReportReady, "the report card did not come out on the seventh day");
            Assert.Equal(1, sim.WeekNumber);
        }

        /// <summary>
        /// THE FIRST REPORT CARD'S DELTA DOES NOT COUNT WHAT WAS INHERITED.
        ///
        /// This too would have been a silent bug: without the day-zero snapshot,
        /// last week would count as zero and on the seventh day the player would see
        /// a jump such as "Place +33" that THEY DID NOT MAKE. The score of the
        /// four-table restaurant they inherited is not their gain.
        /// </summary>
        [Fact]
        public void The_first_report_cards_delta_does_not_count_what_was_inherited()
        {
            Simulation sim = NewSim();
            RunTo(sim, 7);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 5000; t++) { sim.Tick(); if (sim.ServiceComplete) break; }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));

            Assert.True(sim.WeekReportReady, "the report card did not come out - the measurement did not run");

            // The place axis does not change in the first week (there was no
            // expansion): the delta must be ZERO. Without the day-zero snapshot the
            // value here would be the axis itself.
            int place = sim.WeekAxis(4);
            int delta = sim.WeekAxisDelta(4);
            _out.WriteLine($"place axis {place}, first week delta {delta}");

            Assert.True(place > 0, "the place axis is zero - the measurement did not run");
            Assert.Equal(0, delta);
        }

        /// <summary>
        /// DO THE BADGES DISCRIMINATE.
        ///
        /// The danger with recognition is not that it breaks but that it BECOMES
        /// WORTHLESS: if they all fall out by themselves in the first week the
        /// player sees five notifications and the campaign's remaining fifty-three
        /// days are empty. This is the same face of this project's law - a reward
        /// paid into a saturated axis is invisible.
        ///
        /// The test checks a DISTRIBUTION, not a value: the passive player must not
        /// collect them all.
        /// </summary>
        [Fact]
        public void The_badges_are_not_all_handed_out_in_the_first_week()
        {
            Simulation sim = NewSim();
            int[] earnedOn = new int[sim.BadgeCount];

            for (int g = 1; g <= sim.CampaignDays; g++)
            {
                ReasonableMorning(sim);
                RunTheDay(sim);
                for (int i = 0; i < sim.BadgeCount; i++)
                    if (earnedOn[i] == 0 && sim.BadgeEarnedToday(i)) earnedOn[i] = sim.Day;
                sim.AdvanceToNextDay();
            }

            for (int i = 0; i < sim.BadgeCount; i++)
                _out.WriteLine($"{Badges.NameKey(i),-22} {(earnedOn[i] == 0 ? "never" : "day " + earnedOn[i])}");
            _out.WriteLine($"final: tables {sim.TableCount}, reputation {sim.ReputationCenti / 100}, till {sim.Cash / 100}");

            int inFirstWeek = 0;
            for (int i = 0; i < sim.BadgeCount; i++)
                if (earnedOn[i] > 0 && earnedOn[i] <= 7) inFirstWeek++;

            Assert.True(inFirstWeek < sim.BadgeCount,
                "every badge was handed out in the first week - the recognition is worthless");
        }

        /// <summary>
        /// The badges and the report card survive a save.
        ///
        /// If they were not saved the symptom would be silent: the player would lose
        /// their badges on closing and reopening the game, and because the
        /// conditions still hold some of them would come up as a "new badge" all
        /// over again.
        /// </summary>
        [Fact]
        public void The_badges_survive_a_save()
        {
            Simulation sim = NewSim();
            RunTo(sim, 8);

            int badges = sim.BadgesEarned;
            int week = sim.WeekNumber;
            int axis0 = sim.WeekAxis(0);
            _out.WriteLine($"before the save: {badges} badges, week {week}, axis0 {axis0}");

            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);

            Simulation restored = NewSim();
            restored.Restore(new JsonStateReader(w.ToJson()));

            Assert.Equal(badges, restored.BadgesEarned);
            Assert.Equal(week, restored.WeekNumber);
            Assert.Equal(axis0, restored.WeekAxis(0));
            for (int i = 0; i < sim.BadgeCount; i++)
                Assert.Equal(sim.HasBadge(i), restored.HasBadge(i));
        }
    }
}
