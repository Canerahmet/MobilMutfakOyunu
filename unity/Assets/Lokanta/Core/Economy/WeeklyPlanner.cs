using System;

namespace Lokanta.Core.Economy
{
    /// <summary>A week's planned state: tier, reputation, average ticket.</summary>
    public readonly struct WeekPlan
    {
        public readonly int Week;
        public readonly int Tables;
        public readonly int ReputationCenti;
        public readonly long Ticket;       // average ticket, centi-coins

        public WeekPlan(int week, int tables, int reputationCenti, long ticket)
        {
            Week = week; Tables = tables; ReputationCenti = reputationCenti; Ticket = ticket;
        }
    }

    /// <summary>A week's books. Every money field is in centi-coins.</summary>
    public readonly struct WeekResult
    {
        public readonly int Week;
        public readonly int Tables;
        public readonly int WeekdayCustomers;
        public readonly int WeekendCustomers;
        public readonly int WeekCustomers;
        public readonly Crew Crew;
        public readonly int StaffCap;
        public readonly long Revenue;
        public readonly long Ingredients;
        public readonly long Wages;
        public readonly long Rent;
        public readonly long Expansion;
        public readonly long Net;
        public readonly long Cash;

        public WeekResult(int week, int tables, int weekdayCustomers, int weekendCustomers,
                          int weekCustomers, Crew crew, int staffCap, long revenue,
                          long ingredients, long wages, long rent, long expansion,
                          long net, long cash)
        {
            Week = week; Tables = tables;
            WeekdayCustomers = weekdayCustomers; WeekendCustomers = weekendCustomers;
            WeekCustomers = weekCustomers; Crew = crew; StaffCap = staffCap;
            Revenue = revenue; Ingredients = ingredients; Wages = wages;
            Rent = rent; Expansion = expansion; Net = net; Cash = cash;
        }

        public bool CrewWithinCap { get { return Crew.Total <= StaffCap; } }
    }

    /// <summary>
    /// The closed-form weekly plan model.
    ///
    /// This does NOT REPLACE the tick simulation; it is the target the tick
    /// simulation HAS TO REACH. It must produce the same result as
    /// tools/balance/model.py, and GoldenWeeklyTests verifies that. If the
    /// two diverge, it means one of them is wrong.
    /// </summary>
    public static class WeeklyPlanner
    {
        public static WeekResult Compute(in WeekPlan plan, EconomyConfig cfg,
                                         int previousTables, long cashBefore)
        {
            TierConfig tier = cfg.TierForTables(plan.Tables);

            int weekday = DemandModel.WeekdayCustomers(plan.Tables, plan.ReputationCenti, cfg);
            int weekend = DemandModel.WeekendCustomers(plan.Tables, plan.ReputationCenti, cfg);
            int weekendDays = cfg.WeekendDaysPerWeek;
            int weekCustomers = weekday * (7 - weekendDays) + weekend * weekendDays;

            // The crew is sized for the peak day.
            Crew crew = StaffingModel.Required(weekend, cfg);

            // Not demand, but REALISED revenue. See EconomyConfig.RealisationBp.
            long demandRevenue = (long)weekCustomers * plan.Ticket;
            long revenue = Fx.Bp(demandRevenue, cfg.RealisationBp);
            long ingredients = Fx.Bp(revenue, cfg.IngredientRateBp);
            long wages = StaffingModel.WeeklyWageBill(crew, plan.Week, cfg);
            long rent = tier.Rent;
            long expansion = plan.Tables != previousTables ? tier.Upgrade : 0L;

            long net = revenue - ingredients - wages - rent - expansion;
            long cash = cashBefore + net;

            return new WeekResult(plan.Week, plan.Tables, weekday, weekend, weekCustomers,
                                  crew, tier.StaffCap, revenue, ingredients, wages,
                                  rent, expansion, net, cash);
        }

        /// <summary>Computes a sequence of plans end to end, accumulating the till.</summary>
        public static WeekResult[] Run(WeekPlan[] plans, EconomyConfig cfg)
        {
            if (plans == null) throw new ArgumentNullException(nameof(plans));

            WeekResult[] results = new WeekResult[plans.Length];
            long cash = cfg.StartingCash;
            int previousTables = plans.Length > 0 ? plans[0].Tables : 0;

            for (int i = 0; i < plans.Length; i++)
            {
                WeekResult r = Compute(plans[i], cfg, previousTables, cash);
                cash = r.Cash;
                previousTables = plans[i].Tables;
                results[i] = r;
            }
            return results;
        }
    }
}
