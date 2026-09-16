namespace Lokanta.Core.Sim
{
    /// <summary>
    /// The year-end evaluation. docs/08-endgame.md.
    ///
    /// NOT a single number but seven axes: so that different playing styles
    /// can reach a good result by different routes. One player should be
    /// able to score highly by growing, another by running a small but
    /// well-loved shop.
    ///
    /// The seventh is CUISINE-SPECIFIC and rewards the signature mechanic
    /// directly: on fast food it is the highest number of people served in a
    /// single day, on Turkish cuisine the tab collection rate. What sets the
    /// cuisines apart from one another shows up not only in the play but in
    /// the RESULT as well.
    ///
    /// Each axis is 0-100. The score is an integer: like the whole core
    /// (docs/23 2.2).
    /// </summary>
    public readonly struct SeasonScore
    {
        /// <summary>The number of axes. The UI walks through them in order.</summary>
        public const int AxisCount = 7;

        public readonly int Wealth;      // wealth
        public readonly int Reputation;  // reputation
        public readonly int Regulars;    // regular customers
        public readonly int Crew;        // the crew
        public readonly int Place;       // the place itself
        public readonly int Resilience;  // resilience
        public readonly int Signature;   // cuisine-specific

        /// <summary>The mean of the seven axes, 0-100.</summary>
        public readonly int Total;

        /// <summary>The plaque tier, 0-3. The UI resolves the text.</summary>
        public readonly int Plaque;

        public SeasonScore(int wealth, int reputation, int regulars, int crew,
                           int place, int resilience, int signature)
        {
            Wealth = Clamp(wealth);
            Reputation = Clamp(reputation);
            Regulars = Clamp(regulars);
            Crew = Clamp(crew);
            Place = Clamp(place);
            Resilience = Clamp(resilience);
            Signature = Clamp(signature);

            Total = (Wealth + Reputation + Regulars + Crew
                     + Place + Resilience + Signature) / AxisCount;

            // The plaque thresholds: 40 / 60 / 80.
            //
            // The first threshold is 40, because finishing sixty days should
            // mean something on its own - docs/08, "nothing is taken away
            // from you".
            Plaque = Total >= 80 ? 3 : Total >= 60 ? 2 : Total >= 40 ? 1 : 0;
        }

        public int AxisAt(int i)
        {
            switch (i)
            {
                case 0: return Wealth;
                case 1: return Reputation;
                case 2: return Regulars;
                case 3: return Crew;
                case 4: return Place;
                case 5: return Resilience;
                default: return Signature;
            }
        }

        /// <summary>The axis's text key. The UI resolves it from Loc.</summary>
        public static string AxisKey(int i)
        {
            switch (i)
            {
                case 0: return "score.wealth";
                case 1: return "score.reputation";
                case 2: return "score.regulars";
                case 3: return "score.crew";
                case 4: return "score.place";
                case 5: return "score.resilience";
                default: return "score.signature";
            }
        }

        private static int Clamp(int v)
        {
            return v < 0 ? 0 : v > 100 ? 100 : v;
        }
    }
}
