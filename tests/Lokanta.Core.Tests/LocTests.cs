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
    /// Yerellestirme tamligi.
    ///
    /// Uretec (tools/content/gen_loc.py) zaten dogruluyor, ama uretec
    /// ELLE calistiriliyor. Yeni bir yemek eklenip metin tablosu
    /// yenilenmezse oyunda "dish.pide" yazan bir dugme cikar - ve bu,
    /// hicbir sey kirilmadan olur. Test o araligi kapatiyor.
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

        /// <summary>Icerikteki butun *Key alanlari.</summary>
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
        public void Icerigin_istedigi_her_metin_var()
        {
            var table = Table();
            var needed = RequiredKeys();

            List<string> missing = needed.Where(k => !table.ContainsKey(k)).OrderBy(k => k).ToList();
            _out.WriteLine($"{needed.Count} anahtar isteniyor, tabloda {table.Count} var");

            Assert.True(missing.Count == 0,
                "Eksik metin:\n" + string.Join("\n", missing.Take(20)));
        }

        [Fact]
        public void Icerikte_olmayan_metin_yok()
        {
            // Silinen bir yemegin metni tabloda kalirsa kimse fark etmez -
            // ama bir sonraki okuyan onun hala kullanildigini sanir.
            var table = Table();
            var needed = RequiredKeys();

            // Ekranin kendi metin aileleri icerikte gecmez; onlar
            // fazlalik degil. "ui." arayuz, "notice." olay bildirimleri,
            // "score." yil sonu eksenleri, ".desc" aciklamalar.
            //
            // AYNI LISTE tools/content/gen_loc.py:SCREEN_KEY icinde de
            // duruyor ve ikisi AYRISABILIR - IKI KEZ AYRISTI: once
            // ".desc" uretecte eklendi burada eklenmedi (on iki metin
            // "fazlalik" sayildi), sonra "badge." ayni sekilde.
            //
            // Ucuncusu icin beklemedik: gen_loc.py artik bu dosyayi
            // OKUYUP iki listeyi karsilastiriyor ve ayrisirsa uretimi
            // reddediyor. Yani bu yorumun "birini degistiren otekini de
            // degistirmeli" uyarisi artik bir dilek degil, bir kontrol.
            List<string> orphan = table.Keys
                .Where(k => !k.StartsWith("ui.")
                            && !k.StartsWith("notice.")
                            && !k.StartsWith("score.")
                            && !k.StartsWith("badge.")
                            && !k.EndsWith(".desc")
                            && !needed.Contains(k))
                .OrderBy(k => k).ToList();

            Assert.True(orphan.Count == 0,
                "Icerikte karsiligi olmayan metin:\n" + string.Join("\n", orphan.Take(20)));
        }

        [Fact]
        public void Hicbir_metin_bos_degil()
        {
            var blank = Table().Where(kv => string.IsNullOrWhiteSpace(kv.Value))
                               .Select(kv => kv.Key).OrderBy(k => k).ToList();
            Assert.True(blank.Count == 0, "Bos metin:\n" + string.Join("\n", blank));
        }

        [Fact]
        public void Duzenli_musterilerin_UC_sahnesi_de_yazili()
        {
            // docs/09: "her birinin uc ile dort sahnelik hikayesi var."
            // Sahne esigi icerikte, METNI burada; ikisi ayrisirsa oyuncu
            // hak ettigi sahneyi bos bir kutu olarak gorur.
            var table = Table();
            foreach (string cuisine in new[] { "fastfood", "turk" })
            {
                ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);
                foreach (RegularDef r in c.Regulars)
                {
                    Assert.True(table.ContainsKey(r.NameKey), r.Id + " adi yok");
                    Assert.True(table.ContainsKey(r.JobKey), r.Id + " meslegi yok");
                    foreach (StoryBeat b in r.Story)
                        Assert.True(table.ContainsKey(b.TextKey),
                                    r.Id + " " + b.Beat + ". sahnesi yok");
                }
            }
        }

        [Fact]
        public void Turkce_karakterler_kayipsiz()
        {
            // Bu test bir kodlama hatasini yakalamak icin: dosya UTF-8
            // yazilmazsa "Çorbası" -> "Ã‡orbasÄ±" olur ve JSON gecerli
            // kaldigi icin hicbir sey kirilmaz.
            var table = Table();
            Assert.Equal("Mercimek Çorbası", table["dish.mercimek_corbasi"]);
            Assert.Equal("Yoğurt", table["ingredient.yogurt"]);
            Assert.Equal("İnşaat İşçisi", table["archetype.insaat_iscisi"]);

            int withDiacritics = table.Values.Count(
                v => v.IndexOfAny(new[] { 'ç', 'ğ', 'ı', 'ö', 'ş', 'ü',
                                          'Ç', 'Ğ', 'İ', 'Ö', 'Ş', 'Ü' }) >= 0);
            _out.WriteLine($"{withDiacritics} metinde Turkce karakter var");
            Assert.True(withDiacritics > 150);
        }
    }
}
