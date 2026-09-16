using System;

namespace Lokanta.Core.Sim
{
    /// <summary>
    /// The time model of the service day. Every duration is in milliseconds
    /// of simulation time.
    ///
    /// These numbers are DERIVED FROM THE CAPACITY MODEL, not chosen:
    ///   hall_ms_per_person     = service_day_ms / waiter_capacity
    ///   kitchen_ms_per_person  = service_day_ms / cook_capacity
    ///
    /// Otherwise the two models contradict one another. The first prepMs
    /// values in the generated content (a hamburger at 75,000 ms) did not
    /// pass this check: in an 8-minute day one cook could make six
    /// hamburgers a day.
    ///
    /// Detail and derivation: docs/27-time-model.md
    /// </summary>
    public sealed class TimingConfig
    {
        /// <summary>One tick of simulation time. docs/23 1.2.</summary>
        public const int TickMs = 100;

        public int ServiceDayMs { get; }
        public int SlotCount { get; }

        // --- Slot durations, BY CUISINE --------------------------------------
        //
        // docs/28-peak-decision.md Decision G. The slots are NOT EQUAL.
        //
        // The clash was this: 60% of a Turkish restaurant's customers arrive
        // in the lunch slot (an identity pillar), but with equal slots that
        // cannot physically be served; it needs 16 staff and 17 tables, and
        // the caps are 12 and 14. The fix is to vary not the SHARES but the
        // DURATIONS by cuisine: on Turkish the lunch slot covers 48% of the
        // day, while its share stays at 60%.
        private readonly int[] _slotTicks;
        private readonly int[] _slotStartTick;

        // --- The hall pool, per person ---------------------------------------
        public int SeatOrderMs { get; }
        public int ServeMs { get; }
        public int PayMs { get; }
        public int ClearMs { get; }

        /// <summary>
        /// How long ONE PLATE takes to wash by hand.
        ///
        /// A third of ClearMs: clearing a table takes in the walking, the
        /// tray and the wiping down; washing a plate is a single movement.
        /// The number is DERIVED, not a separate constant - if the two were
        /// tuned separately, a change to one would silently make nonsense of
        /// the other.
        /// </summary>
        public int WashMs { get { return ClearMs / 3; } }

        /// <summary>
        /// How long A DEDICATED DISHWASHER takes to wash one plate.
        ///
        /// Somebody dedicated to the sink is a SPECIALIST, and the content
        /// already says so: in staff-roles.json the dishwasher role's daily
        /// capacity is 48 and the waiter's is 26 - that is, "the person
        /// whose job this is" is more than one and a half times as
        /// productive. The simulation was making NO use of that difference:
        /// the dedicated dishwasher and the waiter dashing over to help both
        /// washed at the same WashMs.
        ///
        /// The consequence had been measured: setting a dishwasher aside
        /// RAISED waiting-on-plates from 263 to 349, because one person
        /// washes less than the three waiters who all run to the sink at
        /// once in a crisis (docs/49).
        ///
        /// The ratio is DERIVED FROM THE ROLE TABLE, not invented: 26/48.
        /// </summary>
        public int DishwasherWashMs
        {
            get
            {
                int ms = (int)Core.Fx.MulDiv(WashMs, DishwasherSpeedBp, Core.Fx.One);
                return ms < 1 ? 1 : ms;
            }
        }

        /// <summary>
        /// The dedicated dishwasher's wash-time multiplier, in basis points.
        /// 10000 = no difference. The 26/48 ratio from the role table is ~5400.
        /// </summary>
        public int DishwasherSpeedBp { get; }

        // --- The kitchen pool -------------------------------------------------
        /// <summary>The average number of plates per person (a combo enlarges this).</summary>
        public int DishesPerPersonBp { get; }

        // --- The customer's own time (consumes no pool) -----------------------
        public int EatMs { get; }

        /// <summary>When patience falls to this ratio, the view is warned.</summary>
        public int PatienceWarnBp { get; }

        // --- Patience drain rates, by stage -----------------------------------
        //
        // Why it is not a constant: docs/12 5.2 gives patience as 8-40
        // seconds, but the same section says a service takes ~120 seconds.
        // With a single rate those two contradict each other; every customer
        // would walk out every time. The first simulation run showed exactly
        // that: six parties out of six left angry.
        //
        // The right reading: patience is a tolerance for BEING IGNORED. It
        // drains at full rate while waiting for a table and while waiting for
        // the order to be taken, slowly while waiting for the food, and not
        // at all while the waiter is at the table.
        public int DrainWaitingTableBp { get; }
        public int DrainWaitingOrderBp { get; }
        public int DrainWaitingFoodBp { get; }
        public int DrainWaitingPayBp { get; }

        /// <summary>
        /// A customer does not order a dish they cannot wait for. The
        /// candidates are the dishes whose prepMs &lt;= patience x this factor.
        /// Realistic and cheap: a customer in a hurry takes a quick item.
        /// </summary>
        public int OrderPatienceFactorBp { get; }

        public TimingConfig(int serviceDayMs, int seatOrderMs, int serveMs, int payMs,
                            int clearMs, int eatMs, int dishesPerPersonBp,
                            int[] slotDurationsBp = null,
                            int slotCount = 4, int patienceWarnBp = 3000,
                            int drainWaitingTableBp = 10_000,
                            int drainWaitingOrderBp = 10_000,
                            // 3500 -> 500: THE RESULT OF A MEASUREMENT.
                            //
                            // Patience used to FREEZE while the food was
                            // cooking (the party was marked "in hand"), and
                            // that covered 87% of the "waiting for food"
                            // ticks; so 3500 was in practice behaving as
                            // ~465. Once the freeze was removed, the same
                            // number became 7.5 times as harsh and nobody
                            // could be served in a restaurant with a single
                            // cook (the experience test caught it: after a
                            // hundred days the cook was still at level 0).
                            //
                            // The new value keeps the old EFFECT, but is now
                            // TIED to the cooking time: a dish that takes
                            // long to cook really does eat more patience,
                            // and equipment and cook experience show up on
                            // the customer's side. This was what docs/27
                            // Decision D wanted and could not get.
                            int drainWaitingFoodBp = 500,
                            int drainWaitingPayBp = 5_000,
                            int orderPatienceFactorBp = 20_000,
                            // From the role table: waiter 26 / dishwasher
                            // 48 daily capacity -> 26/48 = 5417 bp.
                            int dishwasherSpeedBp = 5_417)
        {
            if (dishwasherSpeedBp <= 0)
                throw new ArgumentOutOfRangeException(nameof(dishwasherSpeedBp));
            DishwasherSpeedBp = dishwasherSpeedBp;
            DrainWaitingTableBp = drainWaitingTableBp;
            DrainWaitingOrderBp = drainWaitingOrderBp;
            DrainWaitingFoodBp = drainWaitingFoodBp;
            DrainWaitingPayBp = drainWaitingPayBp;
            OrderPatienceFactorBp = orderPatienceFactorBp;
            if (serviceDayMs <= 0) throw new ArgumentOutOfRangeException(nameof(serviceDayMs));
            if (serviceDayMs % TickMs != 0)
                throw new ArgumentException("The service day must divide exactly into ticks", nameof(serviceDayMs));

            ServiceDayMs = serviceDayMs;
            SeatOrderMs = seatOrderMs;
            ServeMs = serveMs;
            PayMs = payMs;
            ClearMs = clearMs;
            EatMs = eatMs;
            DishesPerPersonBp = dishesPerPersonBp;
            SlotCount = slotCount;
            PatienceWarnBp = patienceWarnBp;
            // The slot shares are KEPT: derived copies such as WithEatMs
            // have to hand them over again.
            _slotDurationsBp = slotDurationsBp;

            int ticks = serviceDayMs / TickMs;
            _slotTicks = new int[slotCount];
            _slotStartTick = new int[slotCount];

            if (slotDurationsBp == null)
            {
                // The default equal slots. Used when no cuisine content is loaded.
                int each = ticks / slotCount;
                for (int i = 0; i < slotCount; i++) _slotTicks[i] = each;
                _slotTicks[slotCount - 1] += ticks - each * slotCount;
            }
            else
            {
                if (slotDurationsBp.Length != slotCount)
                    throw new ArgumentException(
                        "slotDurationsBp must hold " + slotCount + " values", nameof(slotDurationsBp));

                int sum = 0;
                for (int i = 0; i < slotCount; i++) sum += slotDurationsBp[i];
                if (sum != Fx.One)
                    throw new ArgumentException(
                        "slotDurationsBp sums to " + sum + ", it must be 10000",
                        nameof(slotDurationsBp));

                int used = 0;
                for (int i = 0; i < slotCount - 1; i++)
                {
                    _slotTicks[i] = (int)Fx.MulDiv(ticks, slotDurationsBp[i], Fx.One);
                    used += _slotTicks[i];
                }
                // The last slot takes the remainder: a rounding leftover must
                // not shorten the day.
                _slotTicks[slotCount - 1] = ticks - used;
            }

            int acc = 0;
            for (int i = 0; i < slotCount; i++)
            {
                _slotStartTick[i] = acc;
                acc += _slotTicks[i];
            }
        }

        public int ServiceTicks { get { return ServiceDayMs / TickMs; } }

        /// <summary>The slot's length in ticks. The slots are not equal.</summary>
        public int SlotTicks(int slot) { return _slotTicks[slot]; }

        /// <summary>The slot's starting tick within the service day.</summary>
        public int SlotStartTick(int slot) { return _slotStartTick[slot]; }

        /// <summary>A copy of the same settings with cuisine-specific slot durations.</summary>
        /// <summary>
        /// Takes the eating time from the cuisine. The content said 38,000
        /// while the default was using 45,000; the difference is 18% on the
        /// table turnover rate.
        /// </summary>
        private readonly int[] _slotDurationsBp;

        public TimingConfig WithEatMs(int eatMs)
        {
            if (eatMs <= 0) return this;
            return new TimingConfig(
                ServiceDayMs, SeatOrderMs, ServeMs, PayMs, ClearMs, eatMs,
                DishesPerPersonBp, _slotDurationsBp, SlotCount, PatienceWarnBp,
                DrainWaitingTableBp, DrainWaitingOrderBp, DrainWaitingFoodBp,
                DrainWaitingPayBp, OrderPatienceFactorBp);
        }

        public TimingConfig WithSlotDurations(int[] slotDurationsBp)
        {
            return new TimingConfig(
                ServiceDayMs, SeatOrderMs, ServeMs, PayMs, ClearMs, EatMs,
                DishesPerPersonBp, slotDurationsBp, SlotCount, PatienceWarnBp,
                DrainWaitingTableBp, DrainWaitingOrderBp, DrainWaitingFoodBp,
                DrainWaitingPayBp, OrderPatienceFactorBp);
        }

        /// <summary>The total hall work per person.</summary>
        public int HallMsPerPerson
        {
            get { return SeatOrderMs + ServeMs + PayMs + ClearMs; }
        }

        /// <summary>Kitchen work per person, scaled by the average plate count.</summary>
        public int KitchenMsPerPerson(int averagePrepMs)
        {
            return (int)Fx.MulDiv(averagePrepMs, DishesPerPersonBp, Fx.One);
        }

        /// <summary>
        /// The consistency check against the capacity model. Called during
        /// loading; if it does not hold the content is rejected, rather than
        /// carrying on in silence.
        /// </summary>
        public bool MatchesCapacity(int hallCapacityPerDay, int tolerancePercent, out int expectedMs)
        {
            expectedMs = ServiceDayMs / hallCapacityPerDay;
            int actual = HallMsPerPerson;
            int diff = actual > expectedMs ? actual - expectedMs : expectedMs - actual;
            return diff * 100 <= expectedMs * tolerancePercent;
        }

        /// <summary>
        /// A temporary default. Once docs/27-time-model.md is finished this
        /// will be replaced with the numbers from there; until then the
        /// values used are derived straight from the capacity model.
        ///   service day 480,000 ms, waiter capacity 25 -> 19,200 ms per person
        /// </summary>
        public static TimingConfig Default()
        {
            return new TimingConfig(
                serviceDayMs: 480_000,
                seatOrderMs: 5_000,
                serveMs: 4_000,
                payMs: 4_200,
                clearMs: 6_000,     // 19,200 in total = 480,000 / 25
                eatMs: 45_000,
                dishesPerPersonBp: 14_000);   // 1.4 plates per person
        }
    }
}
