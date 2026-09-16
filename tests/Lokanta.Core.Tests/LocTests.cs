using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lokanta.Content;
using Lokanta.Core.Content;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// Localisation completeness.
    ///
    /// The generator (tools/content/gen_loc.py) already validates this, but the
    /// generator is run BY HAND. If a new dish is added and the string table is
    /// not regenerated, a button reading "dish.pide" appears in the game - and
    /// it happens without anything breaking. This test closes that gap.
    /// </summary>
    public class LocTests
    {
        private readonly ITestOutputHelper _out;
        public LocTests(ITestOutputHelper output) { _out = output; }

        private static Dictionary<string, string> Table()
        {
            return ContentLoader.ReadStringMap(
                new DirectoryContentSource(Paths.Content), "loc/tr.json");
        }

        /// <summary>Every *Key field in the content.</summary>
        private static HashSet<string> RequiredKeys()
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (string path in Directory.GetFiles(Paths.Content, "*.json",
                                                       SearchOption.AllDirectories))
            {
                if (path.Replace('\\', '/').Contains("/loc/")) continue;
                var token = Newtonsoft.Json.Linq.JToken.Parse(File.ReadAllText(path));
                Collect(token, keys);
            }
            return keys;
        }

        private static void Collect(Newtonsoft.Json.Linq.JToken t, HashSet<string> keys)
        {
            if (t is Newtonsoft.Json.Linq.JObject o)
            {
                foreach (var p in o.Properties())
                {
                    if (p.Name.EndsWith("Key")
                        && p.Value.Type == Newtonsoft.Json.Linq.JTokenType.String)
                        keys.Add(p.Value.ToString());
                    Collect(p.Value, keys);
                }
            }
            else if (t is Newtonsoft.Json.Linq.JArray a)
            {
                foreach (var v in a) Collect(v, keys);
            }
        }

        [Fact]
        public void Every_string_the_content_asks_for_exists()
        {
            var table = Table();
            var needed = RequiredKeys();

            List<string> missing = needed.Where(k => !table.ContainsKey(k)).OrderBy(k => k).ToList();
            _out.WriteLine($"{needed.Count} keys are required, the table holds {table.Count}");

            Assert.True(missing.Count == 0,
                "Missing string:\n" + string.Join("\n", missing.Take(20)));
        }

        [Fact]
        public void There_is_no_string_without_a_counterpart_in_the_content()
        {
            // If a deleted dish's string stays in the table nobody notices - but
            // the next person to read it assumes it is still in use.
            var table = Table();
            var needed = RequiredKeys();

            // The screen's own string families never appear in the content;
            // they are not surplus. "ui." the interface, "notice." the event
            // notifications, "score." the year-end axes, ".desc" the
            // descriptions.
            //
            // THE SAME LIST also sits in tools/content/gen_loc.py:SCREEN_KEY and
            // the two CAN DRIFT APART - THEY DRIFTED TWICE: first ".desc" was
            // added to the generator and not here (twelve strings were counted
            // as "surplus"), then "badge." in the same way.
            //
            // We did not wait for a third: gen_loc.py now READS this file and
            // compares the two lists, and refuses to generate if they have
            // drifted. So this comment's warning that "whoever changes one must
            // change the other" is no longer a wish, it is a check.
            List<string> orphan = table.Keys
                .Where(k => !k.StartsWith("ui.")
                            && !k.StartsWith("notice.")
                            && !k.StartsWith("score.")
                            && !k.StartsWith("badge.")
                            && !k.EndsWith(".desc")
                            && !k.EndsWith(".voice")
                            && !needed.Contains(k))
                .OrderBy(k => k).ToList();

            Assert.True(orphan.Count == 0,
                "String with no counterpart in the content:\n" + string.Join("\n", orphan.Take(20)));
        }

        /// <summary>
        /// DOES EVERY TRAIT HAVE A VOICE - IN BOTH LANGUAGES.
        ///
        /// The `.voice` family was inside no check's scope at all: the content
        /// files do not ask for `.voice` (it is not a nameKey), both of the
        /// orphan checks EXEMPT the family, and the smoke tour's check could not
        /// catch it either - `Loc.T` returns "[key]" for a missing key and the
        /// card makes the same call, so both sides produced the same wrong string
        /// and passed green.
        ///
        /// So if a thirteenth trait were added, the player would be shown
        /// "[trait.x.voice]" and not one of the thirteen checks would speak up.
        /// </summary>
        [Fact]
        public void Every_trait_has_a_voice()
        {
            List<TraitDto> traits = Newtonsoft.Json.JsonConvert
                .DeserializeObject<List<TraitDto>>(
                    File.ReadAllText(Path.Combine(Paths.Content,
                                                  "staff-traits.json")));

            foreach (string language in new[] { "tr", "en" })
            {
                var table = ContentLoader.ReadStringMap(
                    new DirectoryContentSource(Paths.Content), "loc/" + language + ".json");

                List<string> missing = traits
                    .Select(t => "trait." + t.Id + ".voice")
                    .Where(k => !table.ContainsKey(k)
                                || string.IsNullOrWhiteSpace(table[k]))
                    .ToList();

                Assert.True(missing.Count == 0,
                    "trait with no voice in " + language + ": " + string.Join(", ", missing));
            }
        }

        [Fact]
        public void No_string_is_blank()
        {
            var blank = Table().Where(kv => string.IsNullOrWhiteSpace(kv.Value))
                               .Select(kv => kv.Key).OrderBy(k => k).ToList();
            Assert.True(blank.Count == 0, "Blank string:\n" + string.Join("\n", blank));
        }

        [Fact]
        public void All_THREE_beats_of_the_regulars_are_written()
        {
            // docs/09: "each of them has a story of three or four beats."
            // The beat's threshold is in the content, its TEXT is here; if the
            // two drift apart the player sees the beat they have earned as an
            // empty box.
            var table = Table();
            foreach (string cuisine in new[] { "fastfood", "turk" })
            {
                ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);
                foreach (RegularDef r in c.Regulars)
                {
                    Assert.True(table.ContainsKey(r.NameKey), r.Id + " has no name");
                    Assert.True(table.ContainsKey(r.JobKey), r.Id + " has no job");
                    foreach (StoryBeat b in r.Story)
                        Assert.True(table.ContainsKey(b.TextKey),
                                    r.Id + " has no beat " + b.Beat);
                }
            }
        }

        [Fact]
        public void Turkish_characters_survive_intact()
        {
            // This test is here to catch an encoding bug: if the file is not
            // written as UTF-8, every Turkish letter in a dish name comes back as
            // two Latin-1 characters (a C-cedilla turns into "A-tilde" plus a
            // control picture), and because the JSON stays valid nothing breaks.
            var table = Table();
            Assert.Equal("Mercimek Çorbası", table["dish.mercimek_corbasi"]);
            Assert.Equal("Yoğurt", table["ingredient.yogurt"]);
            Assert.Equal("İnşaat İşçisi", table["archetype.insaat_iscisi"]);

            int withDiacritics = table.Values.Count(
                v => v.IndexOfAny(new[] { 'ç', 'ğ', 'ı', 'ö', 'ş', 'ü',
                                          'Ç', 'Ğ', 'İ', 'Ö', 'Ş', 'Ü' }) >= 0);
            _out.WriteLine($"{withDiacritics} strings contain Turkish characters");
            Assert.True(withDiacritics > 150);
        }
    }
}
