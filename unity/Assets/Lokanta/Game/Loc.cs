using System.Collections.Generic;
using Lokanta.Content;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Every piece of text in the game. Key -> the text of the chosen
    /// language.
    ///
    /// The table is generated along with the content
    /// (tools/content/gen_loc.py) and that tool SCANS THE CONTENT: instead
    /// of keeping a list of which keys are needed by hand, it takes them
    /// out of the dish, ingredient, archetype and regular files. If a key
    /// is missing or spare the generator fails - so a button that reads
    /// "dish.hamburger" is not possible.
    ///
    /// A missing key IS NOT SILENT here either: the key itself comes back
    /// in square brackets and a warning is printed once.
    /// </summary>
    public static class Loc
    {
        private static Dictionary<string, string> _table;
        private static readonly HashSet<string> Warned = new HashSet<string>();

        public static bool Loaded { get { return _table != null; } }
        public static int Count { get { return _table == null ? 0 : _table.Count; } }

        /// <summary>
        /// The formatting culture. It follows the GAME'S language, not the
        /// device's.
        ///
        /// It was not set before and the device's culture was used: a player
        /// whose interface was Turkish but whose phone was English saw
        /// "8,000" and "30.0", and one with a Turkish phone "8.000" and
        /// "30,0". The same game, the same language, two different ways of
        /// writing a number.
        ///
        /// And worse: on a French device the thousands separator is U+202F (a
        /// narrow no-break space) and that character IS NOT in the font - the
        /// player's till would have read "8[]000".
        /// </summary>
        public static System.Globalization.CultureInfo Culture { get; private set; }
            = new System.Globalization.CultureInfo("tr-TR");

        /// <summary>
        /// The supported languages. The INDEX in this array is what is saved,
        /// not the code: changing the order changes the choice on devices that
        /// already have one - so a language is added at the end, never in the
        /// middle.
        /// </summary>
        public static readonly string[] Languages = { "tr", "en", "es", "zh", "ar" };

        /// <summary>The language's own name for itself. Writing a language in ITS OWN
        /// language is essential: anyone looking for a line that says "Turkish" already knows English.</summary>
        public static readonly string[] LanguageNames =
            { "Türkçe", "English", "Español", "中文", "العربية" };

        /// <summary>Each language's formatting culture.</summary>
        private static readonly string[] Cultures =
            { "tr-TR", "en-GB", "es-ES", "zh-CN", "ar-EG" };

        /// <summary>
        /// Is the language written RIGHT TO LEFT?
        ///
        /// Arabic only. Direction is a property of the LANGUAGE, not a screen
        /// setting - which is why it sits here, next to the language.
        /// </summary>
        private static readonly bool[] Rtl = { false, false, false, false, true };

        /// <summary>Is the current language written right to left?</summary>
        public static bool IsRightToLeft { get { return Rtl[Language]; } }

        /// <summary>The index of the current language.</summary>
        public static int Language { get; private set; }

        /// <summary>The code of the current language (tr, en, es, zh, ar).</summary>
        public static string LanguageCode { get { return Languages[Language]; } }

        private const string PrefKey = "lokanta.dil";
        private static IContentSource _src;

        /// <summary>
        /// The saved language; the DEVICE's language if there is none.
        ///
        /// Guessing rather than asking at the first launch is the right call:
        /// a wrong guess is put right from Settings with one touch, but a
        /// screen that asks for a language at startup is an obstacle
        /// everybody has to get past at every install.
        /// </summary>
        private static int Preferred()
        {
            if (PlayerPrefs.HasKey(PrefKey))
            {
                int i = PlayerPrefs.GetInt(PrefKey);
                if (i >= 0 && i < Languages.Length) return i;
            }
            // THE DEFAULT IS ENGLISH - THE DEVICE IS NOT CONSULTED.
            //
            // The device's language used to be guessed, and a Turkish phone
            // opened the game in Turkish. The user's decision: "by default the
            // game should start in English".
            //
            // The price is single and small: someone who is going to play in
            // Turkish chooses it once in Settings and the choice is saved. What
            // it buys is a first screen EVERYONE who opens the game can read -
            // English is the common denominator of the five languages.
            return 1;
        }

        /// <summary>
        /// Builds the culture object; falls back to the INVARIANT culture if
        /// it cannot.
        ///
        /// WHY THE GUARD: CultureInfo depends on the ICU data on the device
        /// and that data can be stripped. A culture that cannot be built
        /// throws CultureNotFoundException - from inside Apply(), that is, AS
        /// THE GAME OPENS. If one of the five languages' data is missing on a
        /// device, the game never opens at all on that device.
        ///
        /// The fall back IS NOT SILENT: a warning is printed. A silent fall
        /// back would leave "why are the numbers in the English format"
        /// unanswered. The invariant culture means the wrong format; it does
        /// not mean a game that will not open.
        /// </summary>
        private static System.Globalization.CultureInfo MakeCulture(string name)
        {
            try
            {
                return new System.Globalization.CultureInfo(name);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("could not build the culture (" + name + "): " + e.Message
                                 + " - fell back to the invariant format.");
                return System.Globalization.CultureInfo.InvariantCulture;
            }
        }

        public static void Load(IContentSource src)
        {
            _src = src;
            Apply(Preferred());
        }

        /// <summary>
        /// Changes the language and RELOADS THE TABLE.
        ///
        /// The screens read their text from Loc every time they are built, so
        /// reloading is enough - the caller refreshes the screen.
        /// </summary>
        public static void SetLanguage(int index)
        {
            if (index < 0 || index >= Languages.Length) return;
            if (index == Language && _table != null) return;
            PlayerPrefs.SetInt(PrefKey, index);
            PlayerPrefs.Save();
            Apply(index);
        }

        /// <summary>
        /// Changes the language TEMPORARILY - the choice IS NOT SAVED.
        ///
        /// The tour measures the strip and takes a screenshot in every
        /// language. Doing that with SetLanguage meant the tour changing the
        /// player's own choice of language: the tour walked through five
        /// languages and wrote the last one to disk. Trying something and
        /// CHOOSING it are different things; they have separate doors.
        /// </summary>
        public static void UseLanguage(int index)
        {
            if (index < 0 || index >= Languages.Length) return;
            Apply(index);
        }

        /// <summary>
        /// Runs the preference logic again: the saved choice if there is one,
        /// otherwise the default.
        ///
        /// It exists so the tour can REALLY ask "which language does a device
        /// with nothing saved open in". Reading the constant and checking
        /// whether it is 1 would be measuring the constant itself - not the
        /// path taken at startup.
        /// </summary>
        public static void ApplyPreferred()
        {
            Apply(Preferred());
        }

        private static void Apply(int index)
        {
            Language = index;
            Culture = MakeCulture(Cultures[index]);
            Warned.Clear();
            if (_src != null)
                _table = ContentLoader.ReadStringMap(
                    _src, "loc/" + Languages[index] + ".json");
        }

        /// <summary>
        /// Makes a PERSON'S NAME fit to be shown on screen.
        /// </summary>
        /// <remarks>
        /// When the language is Chinese the whole interface is drawn with
        /// Noto Sans SC, and that font HAS NO Latin Extended-A: no g-breve, no
        /// dotted I, no dotless i and no S-cedilla. They are not in the source
        /// font either, so it cannot be solved by adding them to the subset.
        ///
        /// SIXTEEN of the ninety-six names in the staff name pool carry these
        /// letters (Ayse, Ibrahim, Yagmur, Sila...). A player playing in
        /// Chinese saw one in every six staff as "Ay[]e" - and no check caught
        /// it, because the names are not in the localisation table.
        ///
        /// The answer is the very rule the Chinese CONTENT TABLE already
        /// applies: Turkish marks are dropped from Latin proper nouns. The
        /// same thing here, for the same reason.
        ///
        /// In the other four languages the text comes back AS IT IS - Rubik
        /// carries every one of these letters.
        /// </remarks>
        public static string PersonName(string name)
        {
            if (string.IsNullOrEmpty(name) || LanguageCode != "zh") return name;

            System.Text.StringBuilder sb = null;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                char d = c;
                switch (c)
                {
                    case 'ğ': d = 'g'; break;   // g breve
                    case 'Ğ': d = 'G'; break;
                    case 'ı': d = 'i'; break;   // dotless i
                    case 'İ': d = 'I'; break;   // dotted I
                    case 'ş': d = 's'; break;   // s cedilla
                    case 'Ş': d = 'S'; break;
                }
                if (d != c && sb == null) sb = new System.Text.StringBuilder(name, 0, i, name.Length);
                if (sb != null) sb.Append(d);
            }
            return sb == null ? name : sb.ToString();
        }

        /// <summary>The key's text. [key] if there is none.</summary>
        public static string T(string key)
        {
            if (_table == null) return key;
            if (_table.TryGetValue(key, out string v)) return v;

            if (Warned.Add(key)) Debug.LogWarning("no text for: " + key);
            return "[" + key + "]";
        }

        /// <summary>Puts values in place of {0}, {1}...</summary>
        public static string T(string key, params object[] args)
        {
            return string.Format(Culture, T(key), args);
        }

        /// <summary>
        /// Money. Centi-coins -> visible text.
        ///
        /// The core holds money in CENTI (docs/23 2.2) because integer
        /// arithmetic is deterministic. The player never sees the fractions:
        /// the division happens only here, at the moment of display.
        /// </summary>
        public static string Money(long centi)
        {
            return (centi / 100).ToString("N0", Culture) + " " + T("ui.common.coin");
        }

        /// <summary>
        /// A percentage. THE POSITION OF THE SIGN COMES FROM THE LANGUAGE.
        ///
        /// For a while the code wrote `"%" + (bp / 100)`, and that buried the
        /// TURKISH position in the code: in English it came out as "Margin
        /// %62" where the right form is "62%". Loc resolves the choice of
        /// culture (tr-TR / en-GB) carefully and warns in its own comment
        /// about "the same game, two different ways of writing a number" -
        /// the percentage format had been left outside that system.
        ///
        /// .NET's "P" format is not used: it expects a fraction (0.62) and we
        /// hold basis points (6200); and nobody wants the decimals shown.
        /// Only THE POSITION OF THE SIGN is taken from the culture.
        /// </summary>
        /// <param name="bp">Basis points (6200 = 62%).</param>
        /// <param name="signed">Whether to put a + in front (when showing a difference).</param>
        public static string Percent(int bp, bool signed = false)
        {
            int percent = bp / 100;
            string text = (signed && percent > 0 ? "+" : "")
                          + percent.ToString(Culture);

            // PercentPositivePattern: 0 -> "n %", 1 -> "n%", 2 -> "%n",
            // 3 -> "% n". Turkish is 2 ("%62"), English 1 ("62%").
            switch (Culture.NumberFormat.PercentPositivePattern)
            {
                case 0: return text + " %";
                case 2: return "%" + text;
                case 3: return "% " + text;
                default: return text + "%";
            }
        }

        /// <summary>Reputation. Centi-points -> 0-100 with one decimal.</summary>
        public static string Reputation(int centi)
        {
            // The decimal with INTEGER arithmetic: floating point is needless
            // even in the view layer, and the separator comes from the culture.
            return (centi / 100) + Culture.NumberFormat.NumberDecimalSeparator
                   + ((centi / 10) % 10);
        }
    }
}
