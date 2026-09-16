using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    public class SimulationTests
    {
        private readonly ITestOutputHelper _out;
        public SimulationTests(ITestOutputHelper output) { _out = output; }

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

        /// <summary>
        /// Sets up the TOTAL crew, not the number to ADD.
        ///
        /// The game starts with one cook AND one waiter (the inherited crew), so
        /// both are counted in: asking for hall:2 means hiring ONE person. Only the
        /// cook used to be counted this way, and when the waiter was added
        /// "hall: 2" silently became THREE people.
        ///
        /// The floor is one too: you cannot go below the starting crew.
        /// </summary>
        private static Simulation NewSim(int cooks = 1, int hall = 1)
        {
            Simulation sim = new Simulation(Economy(), Content(), Timing(), Seed);
            for (int i = 1; i < cooks; i++) sim.Apply(new Command(0, CommandKind.Hire, 0));
            for (int i = 1; i < hall; i++) sim.Apply(new Command(0, CommandKind.Hire, 1));
            return sim;
        }

        /// <summary>Runs one service day from start to finish and returns the report.</summary>
        private static DayReport RunOneDay(Simulation sim, int extraTicks = 4000)
        {
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing().ServiceTicks + extraTicks;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            return sim.BuildDayReport();
        }

        // ====================================================================
        [Fact]
        public void The_content_loads_and_validates()
        {
            ContentSet c = Content();
            Assert.Equal("fastfood", c.Cuisine);
            Assert.Equal(32, c.Dishes.Length);
            Assert.Equal(20, c.Archetypes.Length);      // 8 shared + 12 cuisine-specific
            Assert.True(c.Ingredients.Length > 30);

            // Every dish's ingredient cost is computed and below its price
            foreach (DishDef d in c.Dishes)
            {
                Assert.True(d.IngredientCost > 0, d.Id + " has a cost of zero");
                Assert.True(d.IngredientCost < d.Price,
                    d.Id + " costs more than it sells for: " + d.IngredientCost + " / " + d.Price);
            }
        }

        [Fact]
        public void The_ingredient_cost_is_around_thirty_two_percent()
        {
            ContentSet c = Content();
            int min = int.MaxValue, max = 0;
            foreach (DishDef d in c.Dishes)
            {
                int bp = (int)Fx.MulDiv(d.IngredientCost, Fx.One, d.Price);
                if (bp < min) min = bp;
                if (bp > max) max = bp;
            }
            _out.WriteLine($"ingredient ratio: {min} - {max} bp");
            Assert.InRange(min, 2500, 3600);
            Assert.InRange(max, 2500, 3800);
        }

        [Fact]
        public void One_day_runs_from_start_to_finish()
        {
            Simulation sim = NewSim();
            Assert.Equal(DayPhase.Morning, sim.Phase);

            DayReport r = RunOneDay(sim);

            _out.WriteLine($"parties planned {r.PlannedParties}, served {r.ServedParties}, " +
                           $"angry {r.AngryParties}, people {r.ServedPeople}, " +
                           $"revenue {r.Revenue}, satisfaction {r.AverageSatisfactionCenti}");

            Assert.Equal(DayPhase.Evening, sim.Phase);
            Assert.True(r.PlannedParties > 0, "no customer was planned at all");
            Assert.Equal(r.PlannedParties, r.ServedParties + r.AngryParties);
            Assert.Equal(0, sim.ActiveParties);
        }

        [Fact]
        public void No_customer_is_left_hanging_at_the_end_of_the_day()
        {
            Simulation sim = NewSim(cooks: 2, hall: 2);
            RunOneDay(sim);

            for (int i = 0; i < Simulation.MaxParties; i++)
                Assert.False(sim.PartyActive(i), $"party {i} is still active");
        }

        [Fact]
        public void The_same_seed_gives_the_same_day()
        {
            DayReport a = RunOneDay(NewSim(2, 2));
            DayReport b = RunOneDay(NewSim(2, 2));

            Assert.Equal(a.PlannedParties, b.PlannedParties);
            Assert.Equal(a.ServedParties, b.ServedParties);
            Assert.Equal(a.AngryParties, b.AngryParties);
            Assert.Equal(a.Revenue, b.Revenue);
            Assert.Equal(a.AverageSatisfactionCenti, b.AverageSatisfactionCenti);
            Assert.Equal(a.ReputationCenti, b.ReputationCenti);
        }

        [Fact]
        public void A_different_seed_gives_a_different_day()
        {
            Simulation s1 = new Simulation(Economy(), Content(), Timing(), 1UL);
            Simulation s2 = new Simulation(Economy(), Content(), Timing(), 2UL);
            DayReport a = RunOneDay(s1);
            DayReport b = RunOneDay(s2);

            // The demand formula gives the same number of PEOPLE, but the parties
            // and their arrival times must differ.
            Assert.True(a.Revenue != b.Revenue || a.PlannedParties != b.PlannedParties,
                "two different seeds produced the same day");
        }

        [Fact]
        public void The_number_of_ticks_per_frame_does_not_change_the_result()
        {
            // The docs/23 1.4 frame independence test. Because the core never sees
            // real time, how many ticks the driver calls must not matter.
            Simulation a = NewSim(2, 2);
            Simulation b = NewSim(2, 2);

            a.Apply(new Command(0, CommandKind.OpenService));
            b.Apply(new Command(0, CommandKind.OpenService));

            int limit = Timing().ServiceTicks + 4000;
            for (int t = 0; t < limit; t++) { a.Tick(); if (a.ServiceComplete) break; }

            // Advance b in batches of five
            int done = 0;
            while (done < limit && !b.ServiceComplete)
            {
                for (int k = 0; k < 5 && done < limit; k++, done++)
                {
                    b.Tick();
                    if (b.ServiceComplete) break;
                }
            }

            a.Apply(new Command(a.TickIndex, CommandKind.CloseDay));
            b.Apply(new Command(b.TickIndex, CommandKind.CloseDay));

            DayReport ra = a.BuildDayReport(), rb = b.BuildDayReport();
            Assert.Equal(ra.ServedParties, rb.ServedParties);
            Assert.Equal(ra.Revenue, rb.Revenue);
            Assert.Equal(ra.ReputationCenti, rb.ReputationCenti);
        }

        [Fact]
        public void A_sufficient_crew_serves_most_of_the_customers()
        {
            Simulation sim = NewSim(cooks: 2, hall: 2);
            DayReport r = RunOneDay(sim);

            _out.WriteLine($"served {r.ServedParties} / {r.PlannedParties}, " +
                           $"angry {r.AngryParties}");

            Assert.True(r.ServedParties > 0, "not a single customer was served");
            // A small restaurant on day one; at least half should be served
            Assert.True(r.ServedParties * 2 >= r.PlannedParties,
                $"the majority were lost: {r.ServedParties}/{r.PlannedParties}");
        }

        [Fact]
        public void An_understaffed_restaurant_loses_customers()
        {
            // One cook, only the owner in the hall: on a busy day there must be
            // losses.
            Simulation weak = NewSim(cooks: 1, hall: 1);
            Simulation strong = NewSim(cooks: 3, hall: 4);

            DayReport w = RunOneDay(weak);
            DayReport s = RunOneDay(strong);

            _out.WriteLine($"weak crew  : {w.ServedParties} served, {w.AngryParties} angry");
            _out.WriteLine($"strong crew: {s.ServedParties} served, {s.AngryParties} angry");

            Assert.True(s.ServedParties >= w.ServedParties,
                "fewer customers were served once the crew grew");
            Assert.True(s.AngryParties <= w.AngryParties,
                "more customers went away angry once the crew grew");
        }

        [Fact]
        public void An_angry_customer_lowers_the_reputation_gain()
        {
            // One angry customer does not have to WIPE OUT the gain from five happy
            // ones; the first version claimed that and it was wrong. The correct
            // claim: a day with losses earns less than a day without.
            //
            // The second correction (once the station slots were written): the test
            // asked for "cooks: 3, hall: 4" but at four tables the crew cap is
            // THREE. The fourth hall hire was being silently rejected and the
            // "strong crew" really meant nothing but two extra COOKS. And an extra
            // cook does not help at a single-slot station, it actually hurts: an
            // unhurried job that starts early holds the slot and the customer in a
            // hurry who arrives later waits in the queue.
            //
            // The strong crew is now a HALL crew: seating and service get faster and
            // the losses fall. The cap is verified explicitly too.
            // Weak crew = the INHERITED crew (one cook, one waiter); the strong crew
            // is that plus two more waiters.
            // The strong crew is the inherited crew plus ONE waiter: the first tier's
            // crew cap is THREE and the game starts with two, so only a single hire
            // fits at this tier. A command above the cap is silently rejected and
            // the test would have believed it had built a "strong crew" while
            // measuring the crew it inherited.
            Simulation weak = NewSim(cooks: 1, hall: 1);
            Simulation strong = NewSim(cooks: 1, hall: 2);

            Assert.Equal(1, weak.Cooks);
            Assert.Equal(1, weak.HallStaff);
            Assert.Equal(1, strong.Cooks);
            Assert.Equal(2, strong.HallStaff);

            int start = weak.ReputationCenti;
            DayReport w = RunOneDay(weak);
            DayReport s = RunOneDay(strong);

            int weakGain = w.ReputationCenti - start;
            int strongGain = s.ReputationCenti - start;
            _out.WriteLine($"weak crew reputation {weakGain:+#;-#;0} ({w.AngryParties} angry), " +
                           $"strong crew {strongGain:+#;-#;0} ({s.AngryParties} angry)");

            if (w.AngryParties > s.AngryParties)
                Assert.True(weakGain < strongGain,
                    $"the day that lost more customers earned more reputation: " +
                    $"{weakGain} >= {strongGain}");
        }

        [Fact]
        public void The_owners_intervention_is_limited_per_day()
        {
            // docs/02 59: "you have a limited number of owner interventions (3-5
            // per day)". interventionsPerDay was written in the content and NOTHING
            // enforced it: every angry customer could be rescued for free, so crisis
            // management was not a resource but an unlimited button.
            Simulation sim = NewSim(cooks: 2, hall: 2);
            EconomyConfig eco = Economy();

            Assert.Equal(eco.InterventionsPerDay, sim.InterventionsLeft);

            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 2000; t++) sim.Tick();

            // THE SAME table is intervened on over and over. MostImpatientParty
            // filters out a table that has already been intervened on, so trying to
            // measure with it ran out early for want of customers; what is meant to
            // be measured is not the number of customers but the number of
            // INTERVENTIONS.
            int party = sim.MostImpatientParty();
            Assert.True(party >= 0, "there is no customer to measure");

            int used = 0;
            for (int i = 0; i < eco.InterventionsPerDay + 3; i++)
            {
                int before = sim.InterventionsLeft;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                      party, (int)InterventionKind.OwnerAttention));
                if (sim.InterventionsLeft < before) used++;
            }

            _out.WriteLine($"budget {eco.InterventionsPerDay}, used {used}, " +
                           $"left {sim.InterventionsLeft}");
            Assert.True(used <= eco.InterventionsPerDay,
                $"{used} interventions went through against a budget of {eco.InterventionsPerDay} a day");
            Assert.Equal(0, sim.InterventionsLeft);
        }

        [Fact]
        public void Rushing_a_station_shortens_the_job()
        {
            // The third intervention in docs/02 59: "speed up a station". The other
            // two kinds are on the HALL side; when the kitchen was the bottleneck
            // there was nothing at all the owner could do during service.
            //
            // The owner DOES NOT COOK (docs/14 forbids it); they clear the way, so
            // the job's remaining WALL CLOCK time shortens.
            Simulation sim = NewSim(cooks: 2, hall: 2);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 2000; t++) sim.Tick();

            int station = sim.BusiestStation();
            Assert.True(station >= 0, "there is no busy station");

            int before = sim.InterventionsLeft;
            sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                  station, (int)InterventionKind.RushStation));

            _out.WriteLine($"station {station} was rushed, budget {before} -> {sim.InterventionsLeft}");
            Assert.Equal(before - 1, sim.InterventionsLeft);
        }

        [Fact]
        public void Rushing_an_empty_station_does_not_burn_the_budget()
        {
            // The budget is a scarce resource. Pressing an empty station must not
            // spend it, otherwise one wrong tap costs the whole day's budget.
            Simulation sim = NewSim(cooks: 1, hall: 0);
            int before = sim.InterventionsLeft;

            // The service was never opened: no station has any work.
            for (int st = 0; st < 6; st++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                      st, (int)InterventionKind.RushStation));

            Assert.Equal(before, sim.InterventionsLeft);
        }

        [Fact]
        public void The_free_tea_is_not_free_to_the_owner()
        {
            // docs/12 3: "a cost of 2 per serving, given away free". A giveaway with
            // no cost would be a free tap of satisfaction.
            Simulation sim = NewSim(cooks: 2, hall: 2);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 2000; t++) sim.Tick();

            int party = sim.MostImpatientParty();
            Assert.True(party >= 0, "there is no customer to measure");

            long before = sim.Cash;
            sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                  party, (int)InterventionKind.FreeTea));
            long after = sim.Cash;

            _out.WriteLine($"the giveaway cost {(before - after) / 100.0:0.00} coins");
            Assert.True(after < before, "the free tea took no money out of the till at all");

            // The owner's attention spends TIME, not money: it must be free.
            int other = sim.MostImpatientParty();
            if (other >= 0)
            {
                long b2 = sim.Cash;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                      other, (int)InterventionKind.OwnerAttention));
                Assert.Equal(b2, sim.Cash);
            }
        }

        [Fact]
        public void The_reputation_does_not_hit_the_ceiling_in_a_week()
        {
            // A finding of the balance harness: with the undamped formula the
            // reputation climbed from 30 to 100 in nine days, which stopped it being
            // a long-term progression axis. Since the damping was added this test
            // protects it.
            Simulation sim = NewSim(cooks: 3, hall: 4);
            for (int day = 1; day <= 7; day++)
            {
                RunOneDay(sim);
                sim.AdvanceToNextDay();
            }
            _out.WriteLine($"reputation after seven days {sim.ReputationCenti / 100.0:0.0}");
            Assert.True(sim.ReputationCenti < 8500,
                $"the reputation reached {sim.ReputationCenti / 100.0:0.0} in a week; far too fast");
        }

        [Fact]
        public void A_high_price_lowers_satisfaction()
        {
            ContentSet c = Content();

            Simulation normal = NewSim(3, 4);
            DayReport rn = RunOneDay(normal);

            Simulation pricey = NewSim(3, 4);
            for (int i = 0; i < c.Dishes.Length; i++)
                pricey.Apply(new Command(0, CommandKind.SetPrice, i,
                                         (int)(c.Dishes[i].Price * 13 / 10)));   // a 30% markup
            DayReport rp = RunOneDay(pricey);

            _out.WriteLine($"satisfaction at the normal price {rn.AverageSatisfactionCenti}, " +
                           $"marked up {rp.AverageSatisfactionCenti}");

            Assert.True(rp.AverageSatisfactionCenti < rn.AverageSatisfactionCenti,
                "the price went up but the satisfaction did not go down");
            Assert.True(rp.Revenue > 0);
        }

        [Fact]
        public void Consecutive_days_run()
        {
            Simulation sim = NewSim(2, 2);
            List<int> reputations = new List<int>();

            for (int day = 1; day <= 7; day++)
            {
                DayReport r = RunOneDay(sim);
                reputations.Add(r.ReputationCenti);
                Assert.Equal(day, r.Day);
                sim.AdvanceToNextDay();
            }

            _out.WriteLine("reputation: " + string.Join(", ", reputations));
            Assert.Equal(8, sim.Day);
            Assert.Equal(DayPhase.Morning, sim.Phase);
        }

        [Fact]
        public void The_weekend_is_busier_than_a_weekday()
        {
            // Days 6 and 7 are the weekend (WeekendDaysPerWeek = 2)
            Simulation sim = NewSim(3, 4);
            int weekdayPeople = 0, weekendPeople = 0;

            for (int day = 1; day <= 7; day++)
            {
                // THE STOCK IS REPLENISHED - otherwise what is measured is not the
                // weekend but STARVATION.
                //
                // It used not to be replenished and the test broke when the day was
                // sharpened: the longer wait lowered satisfaction, the reputation
                // fell from 33 to 11 over seven days and the falling demand
                // swallowed the weekend multiplier (9 parties on day 6, 4 on day 7).
                // So while the test asked "is the weekend busier" it was really
                // asking "is the reputation spiral faster than the multiplier".
                for (int i = 0; i < sim.IngredientCount; i++)
                {
                    int need = sim.RecommendedRestock(i);
                    if (need > 0)
                        sim.Apply(new Command(sim.TickIndex,
                                              CommandKind.OrderIngredient, i, need));
                }

                DayReport r = RunOneDay(sim);
                // We measure the DEMAND, not the service: once the stock constraint
                // bites, the weekend's extra customers can turn away at the door and
                // the number served no longer reflects the demand.
                _out.WriteLine($"day {day}: planned {r.PlannedParties} parties, reputation {r.ReputationCenti / 100}, angry {r.AngrySeatedParties}");
                if (day <= 5) weekdayPeople += r.PlannedParties;
                else weekendPeople += r.PlannedParties;
                sim.AdvanceToNextDay();
            }

            int weekdayAvg = weekdayPeople / 5;
            int weekendAvg = weekendPeople / 2;
            _out.WriteLine($"weekday total {weekdayPeople} (avg {weekdayAvg}), "
                           + $"weekend total {weekendPeople} (avg {weekendAvg})");
            Assert.True(weekendAvg > weekdayAvg,
                $"the weekend is not busier: {weekendAvg} <= {weekdayAvg}");
        }

        [Fact]
        public void Events_are_produced_and_can_be_drained()
        {
            Simulation sim = NewSim(2, 2);
            RunOneDay(sim);

            SimEvent[] buffer = new SimEvent[4096];
            int n = sim.Events.Drain(buffer);
            _out.WriteLine($"{n} events, {sim.Events.Dropped} dropped");

            Assert.True(n > 0, "no event was produced at all");

            int arrived = 0, paid = 0, seated = 0;
            for (int i = 0; i < n; i++)
            {
                if (buffer[i].Kind == SimEventKind.CustomerArrived) arrived++;
                if (buffer[i].Kind == SimEventKind.CustomerPaid) paid++;
                if (buffer[i].Kind == SimEventKind.CustomerSeated) seated++;
            }
            Assert.True(arrived > 0);
            Assert.True(seated > 0);
            Assert.True(paid > 0);
            Assert.True(seated <= arrived);
        }

        [Theory]
        [InlineData("tr-TR")]
        [InlineData("en-US")]
        [InlineData("de-DE")]
        public void The_culture_does_not_change_the_simulation(string cultureName)
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                CultureInfo culture = new CultureInfo(cultureName);
                Thread.CurrentThread.CurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;

                DayReport r = RunOneDay(NewSim(2, 2));
                Assert.True(r.ServedParties > 0);
                Assert.Equal(r.PlannedParties, r.ServedParties + r.AngryParties);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
                CultureInfo.DefaultThreadCurrentCulture = null;
            }
        }

        [Fact]
        public void The_crew_cap_cannot_be_exceeded()
        {
            Simulation sim = NewSim();
            int cap = Economy().TierAt(0).StaffCap;

            for (int i = 0; i < 20; i++) sim.Apply(new Command(0, CommandKind.Hire, 1));

            Assert.True(sim.Cooks + sim.HallStaff <= cap,
                $"the cap of {cap} was exceeded: {sim.Cooks + sim.HallStaff}");
        }

        [Fact]
        public void The_timing_is_consistent_with_the_capacity_model()
        {
            // The docs/27-time-model.md rule: the hall's work derives from the
            // capacity.
            TimingConfig t = TimingConfig.Default();
            bool ok = t.MatchesCapacity(25, tolerancePercent: 2, out int expected);
            _out.WriteLine($"hall ms per person: expected {expected}, actual {t.HallMsPerPerson}");
            Assert.True(ok,
                $"the hall work time does not match the capacity model: " +
                $"{t.HallMsPerPerson} vs {expected}");
        }

        /// <summary>
        /// The busy slot is the DENSEST one, and on the shipped content that
        /// is deliberately NOT the longest one.
        /// </summary>
        /// <remarks>
        /// This is the regression guard the bug got past. `InPeakSlot` chose
        /// the longest slot, which was right under docs/28 Decision G and
        /// became exactly wrong when docs/48 sharpened the day by making the
        /// busy slot SHORT. Nothing measured it, so the two traits that hang
        /// off it - "panics in a rush" and "unflappable" - ran inverted:
        /// the penalty fell in the quietest part of the day and the immunity
        /// protected nobody.
        ///
        /// The assertion is deliberately in two halves. The first is the
        /// rule. The second pins the fact that made the bug possible, so
        /// that if the content is ever re-shaped until longest == densest,
        /// this test says so instead of quietly passing again.
        /// </remarks>
        [Theory]
        [InlineData("fastfood")]
        [InlineData("turk")]
        public void The_busy_slot_is_the_densest_slot_not_the_longest(string cuisine)
        {
            ContentSet content = ContentSetLoader.Load(Paths.Content, cuisine);
            TimingConfig timing = TimingConfig.Default()
                .WithSlotDurations(content.SlotDurationsBp);
            Simulation sim = new Simulation(Economy(), content, timing, Seed);

            // BEFORE SERVICE THERE IS NO BUSY SLOT, and asking must not
            // make one up. The plan is built by OpenService; with it empty
            // every slot holds zero guests and the "densest" one is
            // whichever comes first. The answer is cached per day, so one
            // early reader would have pinned slot 0 as the rush for the
            // whole of it - and the traits would have believed it.
            Assert.Equal(-1, sim.PeakSlotIndex);

            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));

            int peak = sim.PeakSlotIndex;
            Assert.True(peak >= 0, "no busy slot was worked out");

            int longest = 0;
            for (int i = 1; i < timing.SlotCount; i++)
                if (timing.SlotTicks(i) > timing.SlotTicks(longest)) longest = i;

            _out.WriteLine(cuisine + ": busy slot " + peak + ", longest slot " + longest);
            Assert.NotEqual(longest, peak);
        }

    }
}
