using System.Text;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Lokanta.Game;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// A run-time loading exam: does content load from RESOURCES, and can
    /// the simulation run a day inside Unity.
    ///
    /// Why a separate exam: the unit tests read content FROM DISK. The game
    /// will read it from Resources, and that is the route that faces Android.
    /// Assuming the two routes give the same answer means finding out when it
    /// opens on a phone.
    ///
    /// It runs in batch mode too:
    ///   Unity.exe -batchmode -quit -executeMethod Lokanta.EditorTools.SmokeTest.Run
    /// </summary>
    public static class SmokeTest
    {
        [MenuItem("Lokanta/Run-time exam")]
        public static void Run()
        {
            StringBuilder log = new StringBuilder();
            bool ok = true;

            foreach (string cuisine in new[] { "fastfood", "turk" })
            {
                try
                {
                    IContentSource src = new ResourcesContentSource();
                    EconomyConfig economy = ContentLoader.LoadEconomy(src);
                    ContentSet content = ContentSetLoader.Load(src, cuisine);

                    TimingConfig timing = content.SlotDurationsBp != null
                        ? TimingConfig.Default()
                            .WithSlotDurations(content.SlotDurationsBp)
                            .WithEatMs(content.EatMs)
                        : TimingConfig.Default();

                    Simulation sim = new Simulation(economy, content, timing, 20260911UL);

                    // We run a whole day end to end: not just the loading but
                    // the TICK loop is put to the test as well.
                    for (int i = 0; i < sim.IngredientCount; i++)
                    {
                        int need = sim.RecommendedRestock(i);
                        if (need > 0)
                            sim.Apply(new Command(sim.TickIndex,
                                CommandKind.OrderIngredient, i, need));
                    }
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                    for (int t = 0; t < timing.ServiceTicks + 4000; t++)
                    {
                        sim.Tick();
                        if (sim.ServiceComplete) break;
                    }
                    sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                    DayReport r = sim.BuildDayReport();

                    log.AppendFormat(
                        "{0,-9} {1} dishes, {2} archetypes, {3} regulars, {4} traits | " +
                        "day 1: {5} parties served, takings {6}, satisfaction {7:0.0}\n",
                        cuisine, content.Dishes.Length, content.Archetypes.Length,
                        content.Regulars.Length, economy.TraitCount,
                        r.ServedParties, r.Revenue / 100,
                        r.AverageSatisfactionCenti / 100.0);

                    if (r.ServedParties == 0)
                    {
                        ok = false;
                        log.AppendLine("  ERROR: not a single party was served");
                    }
                }
                catch (System.Exception e)
                {
                    ok = false;
                    log.AppendFormat("{0,-9} ERROR: {1}\n", cuisine, e.Message);
                }
            }

            // Testing here as well that the floor plan and the economy say
            // the same thing is cheap: the unit test looks at the content on
            // disk, this one looks at the Resources copy.
            try
            {
                EconomyConfig cfg = ContentLoader.LoadEconomy(new ResourcesContentSource());
                int running = 0, tier = 0;
                foreach (RoomPlan.Room room in RoomPlan.Rooms)
                {
                    if (!room.IsDining) continue;
                    running += room.Tables;
                    if (cfg.TierAt(tier).Tables != running)
                    {
                        ok = false;
                        log.AppendFormat("  ERROR: room {0} gives {1} tables, but tier {2} says {3}\n",
                            room.Name, running, tier, cfg.TierAt(tier).Tables);
                    }
                    tier++;
                }
                log.AppendFormat("floor plan {0} tiers, in step with the tier table\n", tier);
            }
            catch (System.Exception e)
            {
                ok = false;
                log.AppendLine("floor plan ERROR: " + e.Message);
            }

            if (ok) Debug.Log("THE RUN-TIME EXAM PASSED\n" + log);
            else Debug.LogError("THE RUN-TIME EXAM FAILED\n" + log);
        }
    }
}
