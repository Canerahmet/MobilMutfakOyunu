namespace Lokanta.Core.Sim
{
    /// <summary>
    /// BADGES - not a quest, RECOGNITION.
    ///
    /// That distinction is this file's entire justification. A quest says
    /// "do this tomorrow" and turns the player into the employee of an
    /// invisible boss; whereas the game's one-sentence promise is "You are
    /// the owner, not the chef". A badge says "today you pulled this off" -
    /// it looks backwards, and so it NEVER clashes with the player's own
    /// plan. A player deliberately running short-handed is punished by a
    /// quest and rewarded by a badge.
    ///
    /// The second justification comes from measurement: this project's law
    /// is "a reward paid into a saturated axis is invisible". The classic
    /// daily-quest reward is money, and according to the harness a good
    /// player finishes day sixty on a till of ~21,000 - so the reward is not
    /// felt at exactly the moment it was meant to be. A badge's reward is
    /// NOT money: it is being seen.
    ///
    /// All of them were built out of things the simulation ALREADY knows;
    /// the only new piece of tracking is the "has the book ever been opened"
    /// flag. There is no invented condition, because an invented condition
    /// cannot be measured.
    /// </summary>
    public static class Badges
    {
        /// <summary>
        /// On the peak day nobody was turned away at the door and nobody
        /// left a table angry. The peak condition matters: doing this on a
        /// weekday is easy, and something that is easy does not deserve
        /// recognition.
        /// </summary>
        public const int EverybodyFed = 0;

        /// <summary>
        /// Got through the peak BELOW the crew it needed, and still nobody
        /// left a table angry. It rewards the game's central trade-off
        /// directly - crew means capacity, but crew also means money.
        /// </summary>
        public const int PeakShortHanded = 1;

        /// <summary>
        /// The book was opened and every bit of it was collected. The
        /// signature mechanic of Turkish cuisine; a player on fast food
        /// never sees this one, and should not.
        /// </summary>
        public const int TabBookClosed = 2;

        /// <summary>A regular's first story beat has opened.</summary>
        public const int FirstStoryBeat = 3;

        /// <summary>Ten thousand coins in the till for the first time.</summary>
        public const int FirstTenThousand = 4;

        /// <summary>
        /// The shop grew for the first time.
        ///
        /// These two (expansion and reputation) were added BY MEASUREMENT.
        /// With the first five badges the reasonable player picked up three
        /// pieces of recognition on days 5, 6 and 8, and then there were
        /// FIFTY-TWO DAYS of silence - that is, the whole achievement curve
        /// happened in the first week. A badge's job is to spread across the
        /// campaign.
        /// </summary>
        public const int FirstExpansion = 5;

        /// <summary>
        /// Reputation reached 90. It comes late because reputation is
        /// clamped to the ceiling of the table tier: seeing 90 requires
        /// EXPANDING first.
        /// </summary>
        public const int TalkOfTheNeighbourhood = 6;

        public const int Count = 7;

        /// <summary>The reputation threshold, in CENTI. A single source.</summary>
        public const int ReputationMilestoneCenti = 9000;

        /// <summary>
        /// The till threshold, in CENTI-COINS. A single source: both the
        /// condition and the text come from here.
        ///
        /// The unit was fixed by measurement. It used to read 10000, which
        /// in the game's currency means 100 coins: since the starting till
        /// is 800,000 centi (8,000 coins), the badge was being handed out at
        /// the end of the FIRST DAY. Nothing broke - it was just that a
        /// piece of recognition called "the first ten thousand" was being
        /// given away without ever being earned.
        /// </summary>
        public const long CashMilestone = 1000000;   // 10,000 coins

        /// <summary>
        /// The badge's name. The view resolves the text from Loc - there is
        /// NO text in the core (docs/23 6.3).
        /// </summary>
        public static string NameKey(int i)
        {
            switch (i)
            {
                case EverybodyFed: return "badge.full_house";
                case PeakShortHanded: return "badge.short_peak";
                case TabBookClosed: return "badge.book_closed";
                case FirstStoryBeat: return "badge.first_beat";
                case FirstTenThousand: return "badge.first_ten_k";
                case FirstExpansion: return "badge.first_expand";
                default: return "badge.renowned";
            }
        }

        /// <summary>The single line of explanation under the badge.</summary>
        public static string NoteKey(int i)
        {
            return NameKey(i) + ".note";
        }
    }
}
