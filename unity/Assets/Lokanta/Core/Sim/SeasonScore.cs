namespace Lokanta.Core.Sim
{
    /// <summary>
    /// Yil sonu degerlendirmesi. docs/08-oyun-sonu.md.
    ///
    /// Tek sayi DEGIL yedi eksen: farkli oyun tarzlari farkli yollardan
    /// iyi sonuc alabilsin. Bir oyuncu buyuyerek, bir baskasi kucuk ama
    /// sevilen bir dukkan isleterek yuksek puan alabilmeli.
    ///
    /// Yedincisi MUTFAGA OZEL ve imza mekanigini dogrudan odullendiriyor:
    /// fast food'da bir gunde agirlanan en yuksek kisi sayisi, Turk
    /// mutfaginda veresiye tahsilat orani. Mutfaklari birbirinden ayiran
    /// sey yalnizca oynanista degil SONUCTA da gorunuyor.
    ///
    /// Her eksen 0-100. Puan tamsayi: butun cekirdek gibi (docs/23 2.2).
    /// </summary>
    public readonly struct SeasonScore
    {
        /// <summary>Eksen sayisi. Arayuz bunlari sirayla geziyor.</summary>
        public const int AxisCount = 7;

        public readonly int Wealth;      // varlik
        public readonly int Reputation;  // itibar
        public readonly int Regulars;    // duzenli musteriler
        public readonly int Crew;        // ekip
        public readonly int Place;       // mekan
        public readonly int Resilience;  // saglamlik
        public readonly int Signature;   // mutfaga ozel

        /// <summary>Yedi eksenin ortalamasi, 0-100.</summary>
        public readonly int Total;

        /// <summary>Plaket kademesi 0-3. Metni arayuz cozuyor.</summary>
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

            // Plaket esikleri: 40 / 60 / 80.
            //
            // Ilk esik 40, cunku altmis gunu tamamlamak tek basina bir sey
            // ifade etmeli - docs/08 "hicbir sey elinden alinmiyor".
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

        /// <summary>Eksenin metin anahtari. Arayuz Loc'tan cozuyor.</summary>
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
