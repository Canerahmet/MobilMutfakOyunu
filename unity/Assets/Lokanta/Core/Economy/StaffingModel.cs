namespace Lokanta.Core.Economy
{
    /// <summary>One week's crew.</summary>
    public readonly struct Crew
    {
        public readonly int Cooks;
        public readonly int Hall;

        // There used to be a SalonWorkMicro field here: the peak day's hall
        // workload. It was read nowhere, and two of its four constructors
        // wrote 0 into it - so if a reader ever appeared it would read the
        // WRONG value. The workload can be recomputed from peakCustomers
        // anyway; the field was deleted.
        public Crew(int cooks, int hall)
        {
            Cooks = cooks; Hall = hall;
        }

        public int Total { get { return Cooks + Hall; } }
    }

    /// <summary>
    /// The capacity model of docs/14-staff-system.md.
    ///
    /// Two pools:
    ///   Kitchen  needed = ceil(peak / cook_capacity). The owner cannot cook.
    ///   Hall     by person-days. The owner's own contribution is deducted first.
    ///
    /// The crew is sized for the PEAK day (the weekend), and its wage is
    /// paid for seven days. That is why the first hire falls in the second
    /// week: not a script, but the workload.
    /// </summary>
    public static class StaffingModel
    {
        public static Crew Required(int peakCustomers, EconomyConfig cfg)
        {
            if (peakCustomers <= 0) return new Crew(0, 0);

            int cooks = Fx.CeilDiv(peakCustomers, cfg.CookCapacityPerDay);

            long hallWork = (long)peakCustomers * cfg.HallWorkPerCustomerMicro;
            long afterOwner = hallWork - cfg.OwnerWorkMicro;
            int hall = afterOwner <= 0 ? 0 : (int)Fx.CeilDivL(afterOwner, Fx.Micro);

            return new Crew(cooks, hall);
        }

        /// <summary>
        /// The weekly wage bill, in centi-coins. The experience rise is
        /// compound and at nano precision; exponentiating in basis points
        /// produced a drift of a few coins by the eighth week.
        /// </summary>
        /// <summary>
        /// The most weeks the rise may be raised to a power over. At 2.2%,
        /// 32 weeks passes the 2.00 multiple, so the ceiling already comes
        /// in there; this limit is the second belt, tying PowNano itself to
        /// the near side of an overflow.
        /// </summary>
        private const int MaxWageGrowthWeeks = 64;

        public static long WeeklyWageBill(Crew crew, int week, EconomyConfig cfg)
        {
            long cookBill = (long)crew.Cooks * 7 * cfg.CookDailyWage;

            // We do not round the per-head hall wage in advance: the
            // numerator is kept together and the division is done once.
            long hallBill = crew.Hall == 0
                ? 0
                : Fx.MulDiv((long)crew.Hall * 7 * cfg.HallWageNumerator,
                            1, cfg.HallWorkPerCustomerMicro);

            long baseBill = cookBill + hallBill;
            if (week <= 1) return baseBill;

            // THE RISE IS CAPPED: at most TWO TIMES.
            //
            // Growth is 2.2% a week and it is COMPOUND; because the campaign
            // is eight weeks long it comes to 1.16x there, and the balance
            // was built around that window. But the game DOES NOT END on day
            // sixty - docs/08 moves into free play - and there an uncapped
            // exponential stood against a capped income:
            //
            //     day 200  (week 28)   1.80x
            //     day 365  (week 52)   3.03x
            //     day ~728 (week 104)  PowNano OVERFLOWS the long
            //
            // The overflow threw inside CloseDay, AFTER part of the state
            // had already changed: reputation dropped, stock aged,
            // experience gained, but the stage had not moved to Evening.
            // Unity swallows the exception in a button callback, so the
            // player presses "Close the Day", nothing happens, they press
            // again - and on every press the same damage was applied ONCE
            // MORE. A permanent, irreversible lock-up.
            //
            // The cap closes both the overflow and the "income capped,
            // outgoings uncapped" asymmetry. Two times is a realistic upper
            // bound: a seniority rise does not compound forever.
            long growthNano = Fx.PowNano(
                Fx.Nano + Fx.BpToNano(cfg.WeeklyXpWageGrowthBp),
                week - 1 < MaxWageGrowthWeeks ? week - 1 : MaxWageGrowthWeeks);
            if (growthNano > 2 * Fx.Nano) growthNano = 2 * Fx.Nano;
            return Fx.MulDiv(baseBill, growthNano, Fx.Nano);
        }

        /// <summary>For information: one hall worker's daily wage, in centi-coins.</summary>
        public static long HallDailyWage(EconomyConfig cfg)
        {
            return Fx.MulDiv(cfg.HallWageNumerator, 1, cfg.HallWorkPerCustomerMicro);
        }
    }
}
