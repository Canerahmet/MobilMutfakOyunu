namespace Lokanta.Core.Sim
{
    /// <summary>
    /// Events flowing one way, from the core to the view.
    /// The payload is integers; there is no text. The view turns the id into
    /// a key and the key into text.
    /// docs/23-core-contract.md 6.3.
    /// </summary>
    public enum SimEventKind
    {
        None = 0,
        DayOpened = 1,          // A day
        ServiceOpened = 2,
        DayClosed = 3,          // A day
        CustomerArrived = 4,    // A customer, B archetype, C party size
        CustomerSeated = 5,     // A customer, B table
        OrderTaken = 6,         // A customer, B dish
        FoodReady = 7,          // A customer, B dish
        FoodServed = 8,         // A customer, B table
        CustomerPaid = 9,       // A customer, B centi-coins (fits in an int), C satisfaction centi
        CustomerLeftAngry = 10, // A customer, B stage, C ms waited
        TableCleared = 11,      // A table
        PatienceWarning = 12,   // A customer, B percentage left (bp)
        // THE EVENT'S NAME WAS NOT SAYING WHAT IT DID.
        //
        // Its name was "StockOut" and its comment read "A dish"; but the
        // line that raised it handed over the PARTY index in A instead
        // (Simulation.cs). The UI in turn took A for a dish and printed a
        // name, so the player was shown an unrelated dish name - and because
        // party slots are handed out from the low numbers, usually a VALID
        // but WRONG name. Nothing gave an error.
        //
        // The event was never "an ingredient ran out" in the first place:
        // when the customer cannot find a main dish on the menu that can
        // actually be made, they TURN BACK AT THE DOOR.
        TurnedAway = 13,        // A party, B archetype, C people
        ReputationChanged = 14, // A new reputation centi, B change centi
        CommandRejected = 15,   // A command kind, B reason
        WeeklyCostsPaid = 16,   // A rent, B wages, C till remaining (centi, fits in an int)
        CashWentNegative = 17,  // A day, B debt
        EquipmentBought = 18,   // A station, B new tier
        StorageBought = 19,     // A new tier, B keepBp
        DishRequested = 20,     // A dish: the customer asked, but it cannot be made
        StationRushed = 21,     // A station, B how many jobs were hurried along
        StaffLeveledUp = 22,    // A pool (0 kitchen, 1 hall), B new level
        CreditExtended = 23,    // A table/group, B amount (centi)
        CreditCollected = 24,   // A amount (centi), B outstanding tab remaining
        CreditDefaulted = 25,   // A amount (centi), B reputation penalty
        ComboOrdered = 26,      // A group, B combo price (centi)
        RegularVisited = 27,    // A regular, B their satisfaction
        RegularStoryBeat = 28,  // A regular, B the beat that opened
        RegularUpset = 29,      // A regular, B how many days they will stay away
        StaffResigned = 30,     // A pool (0 kitchen, 1 hall), B index
        WagesLate = 31,         // A day, B the amount still short (centi)
        DishUnlocked = 32,      // A dish: unlocked today
        EquipmentSold = 33,     // A station, B new tier (the ladder down)
        Downsized = 34,         // A new table count, B new tier

        /// <summary>
        /// OUT OF CLEAN PLATES: the kitchen cannot send out the food it has
        /// cooked. A the number of dirty plates, B how many people are at
        /// the sink.
        ///
        /// Why an event: the bottleneck has to be VISIBLE to the player. If
        /// service slows for no reason the player reads it not as a mechanic
        /// but as a BUG - docs/14 describes the dishwasher as a bottleneck
        /// that is "invisible, but noticed the moment it clogs", and the
        /// "noticed" part only happens if it is said out loud.
        /// </summary>
        PlatesOut = 35,

        /// <summary>
        /// A BADGE WAS EARNED. A the badge index (Badges.*).
        ///
        /// Not a quest but RECOGNITION: the event comes out at the close of
        /// the day, after the player has ALREADY done the thing.
        /// </summary>
        BadgeEarned = 36,

        /// <summary>
        /// STAFF TENURE. A pool (0 kitchen, 1 hall), B the number of days.
        ///
        /// The same family as a badge: not a quest but RECOGNITION. The
        /// player is not doing anything; it is said because something IS the
        /// case - somebody has been here a long time.
        ///
        /// Why this moment was picked (docs/53): twenty regulars had three
        /// beats each, the staff had zero lines. Across the twelve games the
        /// research swept it could not find a single line WRITTEN for long
        /// tenure - so the ground is unclaimed. And it lines up with the real
        /// complaint in the Turkish sources: the dishwasher calls himself
        /// "the heart of the restaurant" but "we look as if we do nothing".
        /// A line that says "see me" lands harder than one that says "give
        /// me a rise".
        /// </summary>
        StaffTenure = 37,

        /// <summary>
        /// A charge of the owner's attention came back. A is how many are in
        /// hand now.
        ///
        /// It exists so the tour can assert the REGENERATION rather than the
        /// button: spend a pip, run 130 sim-seconds, the count must go UP.
        /// A check that only watched the count go down would pass on a pool
        /// that never refills.
        /// </summary>
        InterventionRegained = 38,

        /// <summary>Last orders: the door is shut, nobody is thrown out.</summary>
        LastOrders = 39,
        Count = 40
    }

    public readonly struct SimEvent
    {
        public readonly long Tick;
        public readonly SimEventKind Kind;
        public readonly int A;
        public readonly int B;
        public readonly int C;
        public readonly int D;

        public SimEvent(long tick, SimEventKind kind, int a = 0, int b = 0, int c = 0, int d = 0)
        {
            Tick = tick; Kind = kind; A = a; B = b; C = c; D = d;
        }

        public override string ToString()
        {
            return "[" + Tick + "] " + Kind + "(" + A + "," + B + "," + C + "," + D + ")";
        }
    }

    /// <summary>
    /// A fixed-capacity ring buffer. If it fills, the oldest entry drops.
    /// Producing 4096 events in one batch of ticks would be a design error
    /// in itself.
    /// </summary>
    public sealed class EventBuffer
    {
        private readonly SimEvent[] _items;
        private int _head;
        private int _count;
        private int _dropped;

        public EventBuffer(int capacity = 4096)
        {
            _items = new SimEvent[capacity];
        }

        public int Count { get { return _count; } }
        public int Dropped { get { return _dropped; } }

        public void Push(in SimEvent e)
        {
            if (_count == _items.Length)
            {
                _head = (_head + 1) % _items.Length;
                _count--;
                _dropped++;
            }
            int tail = (_head + _count) % _items.Length;
            _items[tail] = e;
            _count++;
        }

        /// <summary>Drains the buffer into the given array. Only as many entries as the returned count are valid.</summary>
        public int Drain(SimEvent[] destination)
        {
            int n = _count < destination.Length ? _count : destination.Length;
            for (int i = 0; i < n; i++)
                destination[i] = _items[(_head + i) % _items.Length];
            _head = (_head + n) % _items.Length;
            _count -= n;
            return n;
        }

        public void Clear()
        {
            _head = 0; _count = 0; _dropped = 0;
        }
    }
}
