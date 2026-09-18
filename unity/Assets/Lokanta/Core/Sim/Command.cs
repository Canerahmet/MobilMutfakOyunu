namespace Lokanta.Core.Sim
{
    /// <summary>
    /// The command list of docs/23-core-contract.md 7.2.
    /// Only player inputs that CHANGE the simulation are commands.
    /// Speed, pause, camera and screen transitions are NOT commands: they
    /// are view state, and they are not saved.
    /// </summary>
    public enum CommandKind
    {
        None = 0,
        OpenService = 1,
        CloseDay = 2,
        SetPrice = 3,
        SetMenuSlot = 4,
        // 5, 9 and 16 are EMPTY: SetDailySpecial, AssignStation and
        // RefillBroth were deleted. All three were defined, were SENT from
        // nowhere, and were absent from Apply's switch too - that is, had
        // one been sent it would have fallen through to default and been
        // rejected. The promise of three mechanics (dish of the day,
        // station assignment, topping up the pot) was nowhere in the code;
        // the enum was showing them as PRESENT.
        //
        // The numbers ARE NOT RENUMBERED: the command kind travels as a
        // number in saved games and in the event stream.
        OrderIngredient = 6,
        Hire = 7,
        Fire = 8,
        Intervene = 10,
        Expand = 11,
        BuyEquipment = 12,
        TakeLoan = 13,
        ExtendCredit = 14,
        CollectCredit = 15,
        BuyStorage = 17,          // the cold-store tier
        SetQuality = 18,          // A: 0 low, 1 standard, 2 high
        SetCombo = 19,            // A: 0 off, 1 on. The fast food signature mechanic

        /// <summary>
        /// How many hall staff are dedicated TO THE SINK. A: the count.
        ///
        /// A dishwasher is NOT a separate staff pool, but a hall worker
        /// dedicated to the sink. The reason is the design itself: docs/14
        /// builds the hall as "waiter + dishwasher + cashier, one work
        /// pool", and the wage comes out of that same blend. A separate
        /// pool would mean counting the same person in two wage tables.
        ///
        /// For the player the decision is the same: they set one head of
        /// crew aside for the washing-up. If they do not, then once the
        /// plates pile up a waiter moves to the sink on their own and
        /// service starts limping.
        /// </summary>
        SetDishwashers = 21,

        /// <summary>
        /// The WHOLE of the recommended stock, in one command.
        ///
        /// The UI used to send one OrderIngredient per ingredient: the
        /// content holds seventy-seven ingredients and close to fifty of
        /// them can come up "short" in a single morning. The daily command
        /// limit is 256, so pressing the "Buy the recommended stock" button
        /// five times ate the day's entire budget - and after that EVERY
        /// command was silently rejected, price, menu, hiring, equipment,
        /// expansion, intervention and the tab included. The player saw
        /// "the buttons do not work" and could learn the reason nowhere.
        ///
        /// The simulation works out the quantity itself, so a replay gives
        /// the same result too.
        /// </summary>
        OrderRecommended = 20,

        /// <summary>
        /// LAST ORDERS: no more parties come in, and nobody already inside is
        /// disturbed.
        ///
        /// CloseDay (2) ends the day by sending every seated party away
        /// angry, which measured out as a move that is never right on any day
        /// in either cuisine - the tail after the arrival window carries 27%
        /// of a fast food day's revenue. So the game shipped a button that is
        /// always wrong to press, with a confirmation toast standing in for a
        /// design decision.
        ///
        /// This is the decision that button was pretending to be. Plates
        /// gone, one hand short, ninety seconds of arrivals still to come:
        /// give up the revenue, keep the reputation and the plate cycle.
        /// </summary>
        LastOrders = 22,
        Count = 23
    }

    /// <summary>The owner's intervention at a table. docs/12 5.4.</summary>
    public enum InterventionKind
    {
        /// <summary>
        /// Invalid. It used to be "Apology": it was defined, it was sent
        /// from NOWHERE, and had it been sent it would silently have taken
        /// THE TEA's effect - what is more, without paying for the tea, so
        /// a ghost strictly better than the tea button.
        ///
        /// 0 is not left vacant but kept as None: so that if the default
        /// value (default(InterventionKind)) reaches somewhere, it does not
        /// turn into a valid intervention but is REJECTED.
        /// </summary>
        None = 0,
        FreeTea = 1,      // on the house
        OwnerAttention = 2, // the owner saw to them personally: +20 points

        /// <summary>
        /// Hurries a station along. docs/02 59 counts three interventions,
        /// and this was the third: "speed up a station".
        ///
        /// The owner DOES NOT COOK -- docs/14 forbids that outright. What
        /// they do is clear the path: fetch the ingredient, take the plate,
        /// join the queue. The mechanical equivalent is wiping out part of
        /// the remaining WALL CLOCK of the jobs cooking at that station.
        ///
        /// Why it was needed: the other two kinds are both on the HALL side.
        /// When the kitchen was the bottleneck there was nothing at all the
        /// owner could do during service.
        /// </summary>
        RushStation = 3
    }

    /// <summary>
    /// Twenty bytes, integers throughout. The command log in the save file
    /// is made of these, and on load they are replayed in order.
    /// </summary>
    public readonly struct Command
    {
        public readonly long Tick;
        public readonly CommandKind Kind;
        public readonly int A;
        public readonly int B;
        public readonly int C;

        public Command(long tick, CommandKind kind, int a = 0, int b = 0, int c = 0)
        {
            Tick = tick; Kind = kind; A = a; B = b; C = c;
        }

        public override string ToString()
        {
            return "[" + Tick + "] " + Kind + "(" + A + "," + B + "," + C + ")";
        }
    }
}
