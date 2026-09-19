using System;
using System.Collections.Generic;
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
    /// The docs/23-core-contract.md 7.6 verification list.
    /// The interruption test is this file's reason to exist: a sixty-day run,
    /// saved and loaded at random points, must end BYTE FOR BYTE the same as an
    /// uninterrupted one.
    /// </summary>
    public class SaveTests
    {
        private readonly ITestOutputHelper _out;
        public SaveTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260911UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "fastfood");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim(ulong seed = Seed)
        {
            return new Simulation(Economy(), Content(), Timing(), seed);
        }

        /// <summary>Saves and loads into a fresh simulation.</summary>
        private static Simulation RoundTrip(Simulation sim, out int bytes)
        {
            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);
            string json = w.ToJson();
            bytes = json.Length;

            Simulation restored = NewSim();
            restored.Restore(new JsonStateReader(json));
            return restored;
        }

        // ====================================================================
        [Fact]
        public void The_same_state_gives_the_same_hash()
        {
            Simulation a = NewSim();
            Simulation b = NewSim();
            Assert.Equal(a.StateHash(), b.StateHash());
        }

        [Fact]
        public void A_single_tick_changes_the_hash()
        {
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.OpenService));
            ulong before = sim.StateHash();
            sim.Tick();
            Assert.NotEqual(before, sim.StateHash());
        }

        [Fact]
        public void Save_and_load_preserves_the_hash()
        {
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.OpenService));
            for (int i = 0; i < 900; i++) sim.Tick();

            ulong before = sim.StateHash();
            Simulation restored = RoundTrip(sim, out int bytes);

            _out.WriteLine($"save size {bytes / 1024} KB");
            Assert.Equal(before, restored.StateHash());
        }

        [Fact]
        public void The_save_size_is_within_budget()
        {
            // docs/23 7.3: the target is under 40 KB for an uncompressed snapshot.
            // The JSON text form is larger; the pre-gzip ceiling is kept loose.
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.OpenService));
            for (int i = 0; i < 2000; i++) sim.Tick();

            RoundTrip(sim, out int bytes);
            _out.WriteLine($"uncompressed JSON {bytes / 1024} KB");
            Assert.True(bytes < 400 * 1024,
                $"the save is too big: {bytes / 1024} KB");
        }

        [Fact]
        public void It_carries_on_identically_after_a_load()
        {
            Simulation a = NewSim();
            a.Apply(new Command(0, CommandKind.OpenService));
            for (int i = 0; i < 500; i++) a.Tick();

            Simulation b = RoundTrip(a, out _);

            // Advance both by the same amount: the hashes must stay equal
            for (int i = 0; i < 1500; i++) { a.Tick(); b.Tick(); }

            Assert.Equal(a.StateHash(), b.StateHash());
            Assert.Equal(a.ServedParties, b.ServedParties);
            Assert.Equal(a.Cash, b.Cash);
        }

        /// <summary>
        /// The fields each version ADDED, keyed by the version that added them.
        ///
        /// This is the table `Simulation.Save.cs` keeps in prose at the top of the
        /// file, in a form a test can walk. All of them are in the "restaurant"
        /// section.
        /// </summary>
        private static readonly Dictionary<int, string[]> FieldsAddedIn =
            new Dictionary<int, string[]>
            {
                { 21, new[] { "badges", "badgesToday", "creditEverOpened",
                              "weekReportDay", "weekAxis", "weekAxisPrev" } },
                { 22, new[] { "cookTenure", "salonTenure" } },
                { 25, new[] { "regDefaults" } },
                { 26, new[] { "cookRaise", "hallRaise", "cookOff", "hallOff",
                              "cooksResting", "hallResting" } },
                // 23 added no FIELD - it changed the SHAPE of two existing
                // arrays, because a station was inserted in the middle of the
                // closed list. The migration test still needs a row so it can
                // build a version 22 save; there is simply nothing to strip.
                { 23, new string[0] },
                // 24: the attention pool REGENERATES instead of being handed
                // out at the door, and the door can be shut without ending
                // the day.
                { 24, new[] { "interventionMs", "doorsClosed" } },
            };

        /// <summary>
        /// The save migration's copy of the fryer's index matches the loader's.
        ///
        /// Simulation.FryerIndex has to exist because the core cannot see the
        /// content loader, and a number written in two places is a number that
        /// will disagree with itself - this project has been bitten by that
        /// five times. If these two drift, every pre-23 save is silently
        /// shifted onto the wrong stations and opens looking fine.
        /// </summary>
        [Fact]
        public void The_migration_and_the_loader_agree_on_where_the_fryer_went()
        {
            int loader = System.Array.IndexOf(
                Lokanta.Content.ContentSetLoader.StationIds, "fritoz");
            Assert.True(loader >= 0, "the closed station list has no 'fritoz'");
            Assert.Equal(loader, Simulation.FryerIndex);
        }

        /// <summary>Every version the gate claims to read, oldest first.</summary>
        public static IEnumerable<object[]> ReadableVersions()
        {
            for (int v = Simulation.MinReadableVersion; v < Simulation.SaveVersion; v++)
                yield return new object[] { v };
        }

        /// <summary>
        /// THE TABLE COVERS THE WHOLE READABLE RANGE.
        ///
        /// The previous migration test wound the version back by
        /// `Simulation.SaveVersion - 1`, so it SLID FORWARD with every release: at
        /// SaveVersion 22 it tested 22 -> 21 and never once loaded a version 20
        /// save - `MinReadableVersion = 20`, the number the gate actually enforces,
        /// was a claim no test backed. It is the familiar shape: a check that does
        /// not run looks exactly like one that passes.
        ///
        /// Walking a table fixes that only as long as the table keeps up with
        /// `SaveVersion`, so this test is the part that cannot be forgotten - it
        /// goes red the moment the version moves without a row being written.
        /// </summary>
        [Fact]
        public void Every_readable_version_is_covered()
        {
            for (int v = Simulation.MinReadableVersion + 1; v <= Simulation.SaveVersion; v++)
                Assert.True(FieldsAddedIn.ContainsKey(v),
                    "version " + v + " is inside the readable range and no row in "
                    + "FieldsAddedIn says what it added - the migration test cannot "
                    + "build a version " + (v - 1) + " save without one");

            foreach (int v in FieldsAddedIn.Keys)
                Assert.True(v > Simulation.MinReadableVersion && v <= Simulation.SaveVersion,
                    "FieldsAddedIn has a row for version " + v + ", outside the "
                    + "readable range " + Simulation.MinReadableVersion + "-"
                    + Simulation.SaveVersion);
        }

        /// <summary>
        /// AN OLD VERSION SAVE OPENS - every version in the readable range, and the
        /// mechanism REALLY runs.
        ///
        /// If `SaveVersion` goes up, every player's sixty-day campaign is lost;
        /// docs/README wrote that down as a condition before the first update and
        /// the migration path had not been written. The file's own rule ("new
        /// fields are read through Has()") had been applied in TWO of 126 reads -
        /// that is, another guard argued for in reasoning and never once run.
        ///
        /// The test takes a current save, DELETES every field added after the
        /// version under test and stamps that version on the header - that is, it
        /// builds exactly the situation that comes after release: an old save in
        /// hand, new code. Then it loads it.
        ///
        /// The criterion has two sides: the save MUST OPEN (no exception, the game
        /// carries on) and the missing fields MUST STAY AT THEIR DEFAULTS. Asking
        /// only the first would have passed a migration path that reset everything.
        /// </summary>
        [Theory]
        [MemberData(nameof(ReadableVersions))]
        public void An_old_version_save_opens(int version)
        {
            Simulation a = NewSim();
            for (int i = 0; i < 400; i++) a.Tick();

            JsonStateWriter w = new JsonStateWriter();
            a.Write(w);
            Newtonsoft.Json.Linq.JObject root =
                Newtonsoft.Json.Linq.JObject.Parse(w.ToJson());

            // THE VERSION IS IN "header", THE FIELDS ARE IN "restaurant". In my
            // first attempt I looked for both in the header and the test's own
            // validation line stopped me - the "old save" I had built was not
            // realistic and would have passed green without testing the mechanism
            // at all.
            Newtonsoft.Json.Linq.JObject restaurant =
                (Newtonsoft.Json.Linq.JObject)root["restaurant"];
            int removed = 0;
            for (int v = version + 1; v <= Simulation.SaveVersion; v++)
                foreach (string field in FieldsAddedIn[v])
                {
                    Assert.True(restaurant[field] != null,
                        "a field that should be in a version " + Simulation.SaveVersion
                        + " save is missing: " + field + " (the table says version "
                        + v + " added it) - the 'old save' the test builds is not "
                        + "realistic");
                    restaurant.Remove(field);
                    removed++;
                }

            // VERSION 23 CHANGED A SHAPE, NOT A FIELD.
            //
            // A station (`fritoz`) was inserted in the middle of the closed
            // list, so a pre-23 save's `tier` array is one SHORTER and every
            // entry from the fryer's index onwards means a different station.
            // Removing a field cannot express that; the synthetic old save has
            // to be shortened the same way a real one is, or the migration is
            // handed a current-shaped array and never exercised.
            if (version < 23)
            {
                // The arrays live in the "stations" section, not in
                // "restaurant" - the writer opens a Begin("stations") for the
                // equipment and the cooking jobs.
                Newtonsoft.Json.Linq.JObject stations =
                    (Newtonsoft.Json.Linq.JObject)root["stations"];
                Assert.NotNull(stations);

                Newtonsoft.Json.Linq.JArray tier =
                    (Newtonsoft.Json.Linq.JArray)stations["tier"];
                Assert.NotNull(tier);
                tier.RemoveAt(Simulation.FryerIndex);
                removed++;

                Newtonsoft.Json.Linq.JArray jobs =
                    (Newtonsoft.Json.Linq.JArray)stations["jobStation"];
                Assert.NotNull(jobs);
                for (int j = 0; j < jobs.Count; j++)
                {
                    int st = (int)jobs[j];
                    if (st >= Simulation.FryerIndex) jobs[j] = st - 1;
                }
            }

            // WITHOUT THIS LINE the arm for version SaveVersion - 0 would hand
            // Restore an untouched CURRENT save and pass without the gate ever
            // being reached.
            Assert.True(removed > 0,
                "nothing was removed for version " + version + " - the save handed "
                + "to Restore is the current one and the version gate never runs");

            ((Newtonsoft.Json.Linq.JObject)root["header"])["version"] = version;

            Simulation b = NewSim();
            b.Restore(new JsonStateReader(root));

            // The save opened: the game carries on where it left off.
            Assert.Equal(a.Day, b.Day);
            Assert.Equal(a.Cash, b.Cash);
            Assert.Equal(a.ServedParties, b.ServedParties);

            // The missing fields are at their defaults: tenure counts from zero.
            if (restaurant["cookTenure"] == null) Assert.Equal(0, b.StaffDaysWorked(0, 0));

            // And it can carry on - the loaded state is in a runnable condition.
            for (int i = 0; i < 200; i++) b.Tick();
        }

        /// <summary>
        /// TENURE IS SEPARATE FROM EXPERIENCE - and the screen shows the tenure.
        ///
        /// `StaffDaysWorked` used to return `_cookXpDays`, and that is EXPERIENCE:
        /// it depends on the trait. The experienced hand's XpBp is 0, while the
        /// apprentice's is 2x. So the staff card said "0 days" for an experienced
        /// hand who had worked for sixty days - a number the simulation itself
        /// contradicted.
        ///
        /// This test holds the two numbers APART: an experienced hand's tenure must
        /// rise while their experience stays put.
        /// </summary>
        [Fact]
        public void Tenure_is_separate_from_experience()
        {
            // THE DAY DOES NOT ADVANCE BY ITSELF. In my first attempt I wrote
            // `while (day < 12) { sim.Tick(); }` and the test went into an INFINITE
            // LOOP - the day only turns with OpenService + CloseDay +
            // AdvanceToNextDay. RunCampaign's pattern is used here.
            const int dayCount = 12;
            Simulation sim = NewSim();
            TimingConfig timing = Timing();
            int limit = timing.ServiceTicks + 6000;

            for (int day = 1; day <= dayCount; day++)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }

            // The inherited cook has been here since day one.
            int tenure = sim.StaffDaysWorked(0, 0);
            int experience = sim.StaffXpDays(0, 0);
            _out.WriteLine($"tenure {tenure}, experience {experience}");

            // The tenure is the number of days worked - independent of the trait.
            Assert.Equal(dayCount, tenure);

            // THE INHERITED COOK IS NOT ENOUGH.
            //
            // They have no trait, so their experience rises as fast as their tenure
            // - a regression that tied `StaffDaysWorked` back to `_cookXpDays`
            // would come out EQUAL here and the test would quietly pass. That the
            // two numbers DIVERGE can only be shown with someone who gains no
            // experience: an experienced hand (`tecrubeli`, XpBp 0).
            Assert.Equal(tenure, experience);      // a person with no trait: equal, and right

            // Look for an experienced hand in the candidate pools and hire them.
            int experiencedTrait = -1;
            for (int t = 0; t < Economy().TraitCount; t++)
                if (Economy().TraitAt(t).Id == "tecrubeli") experiencedTrait = t;
            Assert.True(experiencedTrait >= 0,
                "the content has no 'tecrubeli' trait - the test cannot know what it measures");

            int hired = -1;
            for (int day = dayCount + 1; day <= 50 && hired < 0; day++)
            {
                for (int slot = 0; slot < 3 && hired < 0; slot++)
                {
                    bool hasTrait = sim.CandidateTrait(0, slot, 0) == experiencedTrait
                                    || sim.CandidateTrait(0, slot, 1) == experiencedTrait;
                    if (!hasTrait) continue;
                    int cooksBefore = sim.Cooks;
                    sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, slot));
                    if (sim.Cooks > cooksBefore) hired = sim.Cooks - 1;
                }

                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }

            Assert.True(hired >= 0,
                "no 'tecrubeli' candidate appeared in fifty days - the test could not "
                + "measure anything, its passing proves nothing");

            int newTenure = sim.StaffDaysWorked(0, hired);
            int newExperience = sim.StaffXpDays(0, hired);
            _out.WriteLine($"experienced hand: tenure {newTenure}, experience {newExperience}");

            Assert.True(newTenure > 0,
                "the experienced hand's tenure did not rise (" + newTenure + ")");
            Assert.Equal(0, newExperience);      // XpBp 0: gains no experience
            Assert.NotEqual(newTenure, newExperience);
        }

        /// <summary>
        /// THE LONG TENURE MOMENT FIRES - and EXACTLY ONCE.
        ///
        /// Twenty regulars had three beats each, the staff had zero lines
        /// (docs/53). This event closes that gap and belongs to the same family as
        /// the badges: not a task but RECOGNITION.
        ///
        /// A two-sided measurement is essential. Saying "it fired at least once"
        /// would also have passed a threshold that fires every day - and that would
        /// have filled the notification strip with a single sentence. The same
        /// mistake was made once in this project with the plate notification, which
        /// is why the threshold is written with `==` and the test holds it to
        /// EXACTLY ONCE.
        /// </summary>
        [Fact]
        public void The_long_tenure_moment_fires_exactly_one_single_time()
        {
            const int dayCount = Simulation.TenureDays + 8;
            Simulation sim = NewSim();
            TimingConfig timing = Timing();
            int limit = timing.ServiceTicks + 6000;

            // Counted PER PERSON, not in total.
            //
            // In my first attempt I held the total to "exactly once" and the test
            // went red: on day 30 TWO people cross the threshold at once (the
            // inherited cook and the one in the hall). Two notifications are RIGHT -
            // two separate human beings. What was wrong was the test's expectation.
            //
            // What was really meant to be held is per person anyway: if the
            // threshold behaves like ">=" it fires every day for THE SAME person.
            var fireCount = new Dictionary<int, int>();
            var seenOnDay = new Dictionary<int, int>();
            SimEvent[] buffer = new SimEvent[256];

            for (int day = 1; day <= dayCount; day++)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));

                int n = sim.Events.Drain(buffer);
                for (int i = 0; i < n; i++)
                {
                    if (buffer[i].Kind != SimEventKind.StaffTenure) continue;
                    int person = buffer[i].A * 100 + buffer[i].B;
                    fireCount[person] = fireCount.TryGetValue(person, out int seen) ? seen + 1 : 1;
                    seenOnDay[person] = sim.Day;
                    _out.WriteLine($"tenure moment: day {sim.Day}, pool {buffer[i].A}, index {buffer[i].B}");
                }

                sim.AdvanceToNextDay();
            }

            Assert.True(fireCount.Count > 0,
                "the tenure moment never fired (" + dayCount + " days were run, "
                + "threshold " + Simulation.TenureDays + ")");

            foreach (var kv in fireCount)
            {
                // If the threshold behaves like ">=" it fires every day for the same
                // person and the notification strip fills with a single sentence.
                Assert.True(kv.Value == 1,
                    "it fired " + kv.Value + " times for the same person "
                    + "(pool " + (kv.Key / 100) + ", index " + (kv.Key % 100)
                    + ") - the threshold behaves like '>=' rather than '=='");

                // And on EXACTLY the threshold day: a counter that fires early or
                // late would also make the notification that prints the number a
                // liar.
                Assert.Equal(Simulation.TenureDays, seenOnDay[kv.Key]);
            }
        }

        /// <summary>
        /// Anything OUTSIDE the readable range is rejected.
        ///
        /// A one-sided migration test would also have passed an implementation of
        /// the form "accept every version and leave the fields empty". This arm
        /// says the gate is still a gate.
        /// </summary>
        [Fact]
        public void A_version_that_is_too_old_is_rejected()
        {
            Simulation a = NewSim();
            JsonStateWriter w = new JsonStateWriter();
            a.Write(w);
            Newtonsoft.Json.Linq.JObject root =
                Newtonsoft.Json.Linq.JObject.Parse(w.ToJson());
            ((Newtonsoft.Json.Linq.JObject)root["header"])["version"] =
                Simulation.MinReadableVersion - 1;

            Simulation b = NewSim();
            Assert.ThrowsAny<Exception>(() => b.Restore(new JsonStateReader(root)));
        }

        [Fact]
        public void The_sixty_day_interruption_test()
        {
            // docs/23 7.6: a 60-day run, saved and loaded at random points; the
            // final hash must equal that of an uninterrupted run.
            const int days = 60;
            const int interruptions = 200;

            ulong clean = RunCampaign(days, null);
            ulong interrupted = RunCampaign(days, BuildInterruptionPoints(interruptions, days));

            _out.WriteLine($"uninterrupted {clean:X16}");
            _out.WriteLine($"interrupted   {interrupted:X16}");
            Assert.Equal(clean, interrupted);
        }

        /// <summary>
        /// Runs the campaign. If interruptAt is not null it saves and loads in the
        /// middle of service on those days.
        /// </summary>
        private ulong RunCampaign(int days, HashSet<int> interruptAt)
        {
            Simulation sim = NewSim();
            TimingConfig timing = Timing();
            int limit = timing.ServiceTicks + 6000;

            for (int day = 1; day <= days; day++)
            {
                // A plain but realistic player: goes to the market every morning.
                for (int i = 0; i < sim.IngredientCount; i++)
                {
                    int need = sim.RecommendedRestock(i);
                    if (need > 0)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
                }

                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));

                int cut = interruptAt != null && interruptAt.Contains(day)
                    ? 200 + (day * 37) % 2000
                    : -1;

                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();
                    if (t == cut)
                        sim = RoundTrip(sim, out _);
                    if (sim.ServiceComplete) break;
                }

                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }

            return sim.StateHash();
        }

        private static HashSet<int> BuildInterruptionPoints(int count, int days)
        {
            // A deterministic spread; the test itself has to be repeatable too.
            HashSet<int> set = new HashSet<int>();
            Rng rng = RngSeeder.Create(4242UL, RngStream.Event);
            for (int i = 0; i < count; i++) set.Add(rng.NextInt(days) + 1);
            return set;
        }

        [Fact]
        public void The_command_log_is_recorded()
        {
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.OrderIngredient, 0, 500));
            sim.Apply(new Command(0, CommandKind.OpenService));
            sim.Apply(new Command(5, CommandKind.SetPrice, 0, 5000));

            Assert.Equal(3, sim.CommandCount);
            Assert.Equal(CommandKind.OrderIngredient, sim.CommandAt(0).Kind);
            Assert.Equal(CommandKind.SetPrice, sim.CommandAt(2).Kind);

            Command[] copy = sim.CopyCommandLog();
            Assert.Equal(3, copy.Length);
        }

        [Fact]
        public void The_command_log_is_cleared_at_the_start_of_the_day()
        {
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.OpenService));
            sim.Apply(new Command(0, CommandKind.CloseDay));
            Assert.True(sim.CommandCount > 0);

            sim.AdvanceToNextDay();
            Assert.Equal(0, sim.CommandCount);
        }

        [Fact]
        public void A_command_past_the_daily_limit_is_rejected()
        {
            Simulation sim = NewSim();

            // Up to the limit: the price changes.
            sim.Apply(new Command(0, CommandKind.SetPrice, 0, 4000));
            Assert.Equal(4000, sim.DishPrice(0));

            for (int i = 1; i < Simulation.MaxCommandsPerDay; i++)
                sim.Apply(new Command(0, CommandKind.SetPrice, 0, 4000));

            // PAST the limit: the command is not APPLIED either.
            //
            // The test's name said "is rejected" from the start, but it only
            // measured THE LOG'S LENGTH; the command did not enter the log and yet
            // still changed the state. So the contract "same seed + same log = same
            // state" could be broken and the test did not see it.
            sim.Apply(new Command(0, CommandKind.SetPrice, 0, 9999));
            Assert.Equal(Simulation.MaxCommandsPerDay, sim.CommandCount);
            Assert.Equal(4000, sim.DishPrice(0));
        }

        [Fact]
        public void A_corrupt_save_is_not_silently_accepted()
        {
            Simulation sim = NewSim();
            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);

            Newtonsoft.Json.Linq.JObject root = w.Root;
            ((Newtonsoft.Json.Linq.JObject)root["header"])["version"] = 99;

            Simulation target = NewSim();
            Assert.Throws<InvalidOperationException>(
                () => target.Restore(new JsonStateReader(root)));
        }

        [Fact]
        public void A_missing_field_is_not_silently_skipped()
        {
            Simulation sim = NewSim();
            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);

            Newtonsoft.Json.Linq.JObject root = w.Root;
            ((Newtonsoft.Json.Linq.JObject)root["restaurant"]).Remove("cash");

            Simulation target = NewSim();
            Assert.Throws<ContentException>(
                () => target.Restore(new JsonStateReader(root)));
        }
    }
}
