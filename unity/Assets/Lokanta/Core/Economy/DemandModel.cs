namespace Lokanta.Core.Economy
{
    /// <summary>
    /// docs/12-economy.md 5.1
    ///   customers = tables x base x (0.5 + reputation/100) x day_factor
    ///
    /// The integer translation: reputation is kept in centi-points (30 = 3000)
    /// and the ratio reputation/100 expressed in basis points is exactly equal
    /// to the centi-point figure, because reputation_centi = reputation x 100
    /// and (reputation/100) x 10000 = reputation x 100.
    /// So the demand multiplier = 5000 + reputationCenti basis points.
    /// </summary>
    public static class DemandModel
    {
        /// <summary>
        /// The demand multiplier, in basis points. Reputation 30 -> 8000 (0.80).
        ///
        /// The linear part had a FLOOR and it was too high: even with
        /// reputation down at zero the restaurant still took half of base
        /// demand. So a shop nobody was talking about still behaved as if it
        /// were half full, and the death spiral of neglect could not be felt.
        ///
        /// The break point is 20 points, BELOW the starting reputation (30).
        ///
        /// It was first put at 30 and a test caught it: reputation starts
        /// eroding from the first day, so every player entered the steep
        /// region as early as the second day and the opening week collapsed
        /// for everyone. The break must sit below the starting value, so that
        /// only a REAL collapse is punished.
        /// </summary>
        public static int DemandMultiplierBp(int reputationCenti)
        {
            int linear = (Fx.One / 2) + reputationCenti;
            if (reputationCenti >= StartReputationCenti) return linear;

            // 10% at zero, 100% at the starting reputation.
            int k = FloorBp + (int)Fx.MulDiv(Fx.One - FloorBp,
                                             reputationCenti, StartReputationCenti);
            return (int)Fx.MulDiv(linear, k, Fx.One);
        }

        /// <summary>
        /// THE DIRECT EFFECT OF PRICE ON DEMAND.
        ///
        /// This channel once DID NOT EXIST AT ALL, and it left the biggest
        /// hole in the game. Price's only route was satisfaction ->
        /// reputation; and reputation is CLAMPED to the ceiling of the table
        /// tier. So for a player pressed up against that ceiling a loss of
        /// satisfaction bought nothing, and a small price rise was FREE.
        ///
        /// Measured (24 seeds, 60 days, fast food): a bot pricing 10% above
        /// the market finished on 27,849 coins - the game's most advanced
        /// strategy managed 25,092, the baseline strategy 18,670. So one
        /// button pressed once in the morning beat everything, with FEWER
        /// tables and a SMALLER crew. The punishment existed only outside the
        /// band (at a 30% rise reputation zeroes out and the shop goes under);
        /// in between there was nothing.
        ///
        /// The channel is also READABILITY: a player who raises prices now
        /// sees fewer customers the next day. Before, no screen told them that
        /// the rise had a cost.
        ///
        /// There is a floor and a ceiling, because the elasticity is linear:
        /// a 50% discount must not double demand, and a 60% rise must not
        /// empty the shop in a single day. A reputation collapse is already a
        /// separate punishment.
        /// </summary>
        public static int ApplyPrice(int people, long priceDiffBp, int elasticityBp)
        {
            if (people <= 0 || elasticityBp <= 0 || priceDiffBp == 0) return people;

            long multBp = Fx.One - Fx.MulDiv(priceDiffBp, elasticityBp, Fx.One);
            if (multBp < PriceFloorBp) multBp = PriceFloorBp;
            if (multBp > PriceCeilBp) multBp = PriceCeilBp;
            return (int)Fx.MulDiv(people, multBp, Fx.One);
        }

        /// <summary>The lowest ratio the price channel can drive demand down to.</summary>
        private const int PriceFloorBp = 3000;

        /// <summary>The highest ratio the price channel can lift demand to.</summary>
        private const int PriceCeilBp = 13000;

        /// <summary>The curve's break point, in centi-points. 20 points.</summary>
        private const int StartReputationCenti = 2000;

        /// <summary>The share of demand that remains when reputation is zero, in basis points.</summary>
        private const int FloorBp = 1000;

        /// <summary>
        /// One day's customer count. A single rounding happens right at the
        /// end; there is NO rounding in the intermediate steps, because the
        /// Python model also rounds only once.
        /// </summary>
        public static int CustomersPerDay(int tables, int reputationCenti,
                                          int basePerTable, int dayFactorBp)
        {
            long seats = (long)tables * basePerTable;
            long numerator = seats * DemandMultiplierBp(reputationCenti) * dayFactorBp;
            return (int)Fx.MulDiv(numerator, 1, (long)Fx.One * Fx.One);
        }

        public static int WeekdayCustomers(int tables, int reputationCenti, EconomyConfig cfg)
        {
            return CustomersPerDay(tables, reputationCenti,
                                   cfg.CustomerBasePerTable, cfg.WeekdayMultiplierBp);
        }

        public static int WeekendCustomers(int tables, int reputationCenti, EconomyConfig cfg)
        {
            return CustomersPerDay(tables, reputationCenti,
                                   cfg.CustomerBasePerTable, cfg.WeekendMultiplierBp);
        }

        /// <summary>The week's total customers. Weekend days carry their own factor.</summary>
        public static int WeekCustomers(int tables, int reputationCenti, EconomyConfig cfg)
        {
            int weekday = WeekdayCustomers(tables, reputationCenti, cfg);
            int weekend = WeekendCustomers(tables, reputationCenti, cfg);
            int weekendDays = cfg.WeekendDaysPerWeek;
            return weekday * (7 - weekendDays) + weekend * weekendDays;
        }
    }
}
