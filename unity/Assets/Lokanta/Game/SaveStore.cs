using System;
using System.IO;
using Lokanta.Content;
using Lokanta.Core.Save;
using Lokanta.Core.Sim;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>Bir kayit yuvasinin ozeti; yuva ekrani bunu gosteriyor.</summary>
    public struct SlotInfo
    {
        public bool Exists;
        public string Cuisine;
        public int Day;
        public long Cash;
        public int ReputationCenti;
        public DateTime Saved;
        /// <summary>Dosya var ama okunamiyor. Sessizce bos gostermek yanlis.</summary>
        public bool Broken;
    }

    /// <summary>
    /// Dort kayit yuvasi, diskte. docs/21 dort yuvali tasarim.
    ///
    /// Iki dosya yaziliyor: durum ve OZET. Ozet ayri, cunku yuva ekrani
    /// dort kaydin tamamini yuklemek zorunda kalmamali - tam kayit ~100 KB
    /// ve dordunu birden okumak menuyu yavaslatirdi.
    ///
    /// Yazma ATOMIK: once .tmp, sonra yer degistirme. Kayit sirasinda
    /// olen bir uygulama, yarim bir dosya birakmamali; yarim kayit,
    /// oyuncunun butun kampanyasi demek.
    /// </summary>
    public static class SaveStore
    {
        public const int SlotCount = 4;

        private static string Dir
        {
            get { return Path.Combine(Application.persistentDataPath, "kayit"); }
        }

        private static string StatePath(int slot)
        {
            return Path.Combine(Dir, "yuva" + slot + ".json");
        }

        private static string InfoPath(int slot)
        {
            return Path.Combine(Dir, "yuva" + slot + ".ozet.json");
        }

        // ---------------------------------------------------------------------
        public static SlotInfo Read(int slot)
        {
            SlotInfo info = new SlotInfo();
            string path = InfoPath(slot);
            if (!File.Exists(path)) return info;

            try
            {
                string[] parts = File.ReadAllText(path).Split('');
                info.Exists = true;
                info.Cuisine = parts[0];
                // DEGISMEZ kultur: kayit dosyasi makineler arasi tasinabilir
                // olmali ve cihazin dili degisince okunamaz hale gelmemeli.
                System.Globalization.CultureInfo inv =
                    System.Globalization.CultureInfo.InvariantCulture;

                info.Day = int.Parse(parts[1], inv);
                info.Cash = long.Parse(parts[2], inv);
                info.ReputationCenti = int.Parse(parts[3], inv);
                info.Saved = new DateTime(long.Parse(parts[4], inv), DateTimeKind.Utc);

                // SURUM OZETTE DE DURUYOR.
                //
                // Durum dosyasi ile ozet AYRI yaziliyor ve ozet surumu
                // tasimiyordu. Oyun guncellenip SaveVersion artinca yuva
                // karti saglikli bir kampanya gosteriyor - gun, kasa,
                // itibar - oyuncu "Devam"a basiyor ve Restore firlatiyor.
                // Yuva bir sonraki acilista yine saglikli goruunuyor.
                //
                // Alan ESKI kayitlarda yok; yoksa surum kontrolu
                // yapilmiyor ve davranis eskisi gibi kaliyor. Varsa ve
                // tutmuyorsa yuva DURUSTCE bozuk goruunuyor.
                if (parts.Length > 5)
                {
                    int v;
                    if (int.TryParse(parts[5], System.Globalization.NumberStyles.Integer,
                                     inv, out v)
                        && v != Simulation.SaveVersion)
                    {
                        info.Broken = true;
                    }
                }
            }
            catch (Exception e)
            {
                // Bozuk ozet, bozuk kayit demek DEGIL - ama ikisini de
                // ayirt edemedigimiz icin yuvayi "bozuk" gosteriyoruz.
                // Sessizce "bos" gostermek, oyuncunun uzerine yazmasina
                // ve gercekten kaybetmesine yol acardi.
                Debug.LogWarning("Kayit ozeti okunamadi (yuva " + slot + "): " + e.Message);
                info.Exists = true;
                info.Broken = true;
            }
            return info;
        }

        public static bool Save(int slot, Simulation sim, string cuisine)
        {
            try
            {
                Directory.CreateDirectory(Dir);

                JsonStateWriter w = new JsonStateWriter();
                sim.Write(w);
                WriteAtomic(StatePath(slot), w.ToJson());

                System.Globalization.CultureInfo inv =
                    System.Globalization.CultureInfo.InvariantCulture;

                string info = string.Join("", new[]
                {
                    cuisine,
                    sim.Day.ToString(inv),
                    sim.Cash.ToString(inv),
                    sim.ReputationCenti.ToString(inv),
                    DateTime.UtcNow.Ticks.ToString(inv),
                    Simulation.SaveVersion.ToString(inv),
                });
                WriteAtomic(InfoPath(slot), info);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Kayit yazilamadi (yuva " + slot + "): " + e);
                return false;
            }
        }

        /// <summary>
        /// Yuvayi yukler. Simulasyon ONCEDEN dogru mutfakla kurulmus
        /// olmali; Restore mutfak uyusmazsa hata firlatiyor.
        /// </summary>
        public static bool Load(int slot, Simulation sim)
        {
            try
            {
                string path = StatePath(slot);
                if (!File.Exists(path)) return false;
                sim.Restore(new JsonStateReader(File.ReadAllText(path)));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Kayit yuklenemedi (yuva " + slot + "): " + e);
                return false;
            }
        }

        public static void Delete(int slot)
        {
            try
            {
                if (File.Exists(StatePath(slot))) File.Delete(StatePath(slot));
                if (File.Exists(InfoPath(slot))) File.Delete(InfoPath(slot));
            }
            catch (Exception e)
            {
                Debug.LogError("Kayit silinemedi (yuva " + slot + "): " + e);
            }
        }

        /// <summary>
        /// GERCEKTEN atomik yazma.
        ///
        /// Onceki hali atomik DEGILDI ve adi oyle diyordu:
        ///
        ///     File.WriteAllText(tmp, text);              // diske zorlanmiyor
        ///     if (File.Exists(path)) File.Delete(path);  // ESKI KAYIT SILINDI
        ///     File.Move(tmp, path);
        ///
        /// Iki ayri kayip yolu vardi. Birincisi: silme ile tasima
        /// arasinda uygulama olurse diskte ne yeni kayit var ne eski -
        /// kampanya gitti. Ikincisi: WriteAllText veri sayfa onbellegine
        /// dustugunde donuyor, yani pil o anda biterse .tmp yarim kaliyor
        /// ve bir sonraki acilista o yarim dosya gercek kaydin yerine
        /// geciyor - ustelik iyi kopya bir onceki adimda silinmis oluyor.
        ///
        /// Su anki hali: once diske ZORLA yaz (Flush(true) isletim
        /// sistemine kadar gidiyor), sonra TEK BIR yeniden adlandirmayla
        /// yer degistir. Yeniden adlandirma dosya sisteminde bolunmez;
        /// hangi anda olursek olalim diskte ya eski kayit ya yeni kayit
        /// duruyor, ikisinin arasinda bir durum yok.
        /// </summary>
        private static void WriteAtomic(string path, string text)
        {
            string tmp = path + ".tmp";

            using (FileStream fs = new FileStream(tmp, FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs, Utf8))
            {
                sw.Write(text);
                sw.Flush();
                fs.Flush(true);
            }

            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        /// <summary>
        /// BOM'suz UTF-8. StreamWriter'in varsayilani BOM yaziyor ve o uc
        /// bayt JObject.Parse'i bozuyor.
        /// </summary>
        private static readonly System.Text.UTF8Encoding Utf8 =
            new System.Text.UTF8Encoding(false);
    }
}
