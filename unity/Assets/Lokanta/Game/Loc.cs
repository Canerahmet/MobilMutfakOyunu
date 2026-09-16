using System.Collections.Generic;
using Lokanta.Content;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Oyundaki butun metin. Anahtar -> Turkce.
    ///
    /// Tablo icerikle birlikte uretiliyor (tools/content/gen_loc.py) ve o
    /// arac ICERIGI TARIYOR: hangi anahtarlarin gerektigini elle liste
    /// tutmak yerine yemek, malzeme, arketip ve duzenli musteri
    /// dosyalarindan cikariyor. Eksik ya da fazla anahtar varsa uretec
    /// hata veriyor - yani "dish.hamburger" yazan bir dugme mumkun degil.
    ///
    /// Eksik anahtar burada da SESSIZ DEGIL: anahtarin kendisi kose
    /// parantez icinde donuyor ve bir kez uyari basiliyor.
    /// </summary>
    public static class Loc
    {
        private static Dictionary<string, string> _table;
        private static readonly HashSet<string> Warned = new HashSet<string>();

        public static bool Loaded { get { return _table != null; } }
        public static int Count { get { return _table == null ? 0 : _table.Count; } }

        /// <summary>
        /// Bicimleme kulturu. Oyunun DILINE bagli, cihazin diline degil.
        ///
        /// Once belirtilmiyordu ve cihazin kulturu kullaniliyordu: arayuz
        /// Turkce oldugu halde telefonu Ingilizce olan bir oyuncu
        /// "8,000" ve "30.0", Turkce olan "8.000" ve "30,0" goruyordu.
        /// Ayni oyun, ayni dil, iki farkli sayi yazimi.
        ///
        /// Dahasi: Fransizca bir cihazda binlik ayraci U+202F (dar
        /// bolunmez bosluk) oluyor ve o karakter yazi tipinde YOK -
        /// oyuncunun kasasinda "8[]000" yazardi.
        /// </summary>
        public static System.Globalization.CultureInfo Culture { get; private set; }
            = new System.Globalization.CultureInfo("tr-TR");

        /// <summary>
        /// Desteklenen diller. Dizin SIRASI kaydediliyor, kodu degil:
        /// sirayi degistiren, eski cihazlardaki secimi de degistirir -
        /// o yuzden sona eklenir, araya degil.
        /// </summary>
        public static readonly string[] Languages = { "tr", "en", "es", "zh", "ar" };

        /// <summary>Dilin kendi adi. Bir dili KENDI dilinde yazmak sarttir:
        /// "Turkish" yazan bir satiri arayan kisi zaten Ingilizce biliyordur.</summary>
        public static readonly string[] LanguageNames =
            { "Türkçe", "English", "Español", "中文", "العربية" };

        /// <summary>Her dilin bicimleme kulturu.</summary>
        private static readonly string[] Cultures =
            { "tr-TR", "en-GB", "es-ES", "zh-CN", "ar-EG" };

        /// <summary>
        /// Dilin yazi yonu SAGDAN SOLA mi.
        ///
        /// Yalnizca Arapca. Yon bir DIL ozelligi, bir ekran ayari degil -
        /// o yuzden burada, dilin yaninda duruyor.
        /// </summary>
        private static readonly bool[] Rtl = { false, false, false, false, true };

        /// <summary>Su anki dil sagdan sola mi yaziliyor.</summary>
        public static bool IsRightToLeft { get { return Rtl[Language]; } }

        /// <summary>Su anki dilin dizini.</summary>
        public static int Language { get; private set; }

        /// <summary>Su anki dilin kodu (tr, en, es, zh, ar).</summary>
        public static string LanguageCode { get { return Languages[Language]; } }

        private const string PrefKey = "lokanta.dil";
        private static IContentSource _src;

        /// <summary>
        /// Kaydedilmis dil; yoksa CIHAZIN dili.
        ///
        /// Ilk acilista sormak yerine tahmin etmek dogru: yanlis tahmin
        /// Ayarlar'dan tek dokunusla duzeliyor, ama acilista dil soran
        /// bir ekran herkesin her kurulumda gectigi bir engel.
        /// </summary>
        private static int Preferred()
        {
            if (PlayerPrefs.HasKey(PrefKey))
            {
                int i = PlayerPrefs.GetInt(PrefKey);
                if (i >= 0 && i < Languages.Length) return i;
            }
            // VARSAYILAN INGILIZCE - CIHAZA BAKILMIYOR.
            //
            // Once cihazin dili tahmin ediliyordu ve Turkce bir telefon
            // oyunu Turkce aciyordu. Kullanicinin karari: "default olarak
            // oyun ingilizce baslasin".
            //
            // Bedeli tek ve kucuk: Turkce oynayacak kisi Ayarlar'dan bir
            // kez seciyor ve secim kaydediliyor. Karsiligi, oyunu ilk
            // acan HERKESIN okuyabildigi bir ilk ekran - bes dilin
            // ortak paydasi Ingilizce.
            return 1;
        }

        /// <summary>
        /// Kultur nesnesini kurar; kuramazsa DEGISMEZ kulture duser.
        ///
        /// NEDEN KORUMA: CultureInfo cihazda ICU verisine bagli ve o veri
        /// budanabiliyor. Kurulamayan bir kultur CultureNotFoundException
        /// atar - Apply() icinden, yani OYUN ACILIRKEN. Bes dilden birinin
        /// verisi bir cihazda yoksa, o cihazda oyun hic acilmaz.
        ///
        /// Dusus SESSIZ DEGIL: bir uyari basiliyor. Sessiz bir dusus,
        /// "sayilar neden Ingilizce bicimde" sorusunu cevapsiz birakirdi.
        /// Degismez kultur yanlis bicim demek, acilmayan oyun demek degil.
        /// </summary>
        private static System.Globalization.CultureInfo MakeCulture(string name)
        {
            try
            {
                return new System.Globalization.CultureInfo(name);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Kultur kurulamadi (" + name + "): " + e.Message
                                 + " - degismez bicime dusuldu.");
                return System.Globalization.CultureInfo.InvariantCulture;
            }
        }

        public static void Load(IContentSource src)
        {
            _src = src;
            Apply(Preferred());
        }

        /// <summary>
        /// Dili degistirir ve TABLOYU YENIDEN YUKLER.
        ///
        /// Ekranlar metni her kurulusta Loc'tan okuyor, yani yeniden
        /// yuklemek yetiyor - cagiran taraf ekrani tazeliyor.
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
        /// Dili GECICI olarak degistirir - secim KAYDEDILMEZ.
        ///
        /// Tur, her dilde serit olcup goruntu aliyor. Bunu SetLanguage
        /// ile yapmak, turun oyuncunun dil secimini degistirmesi
        /// demekti: tur bes dili gezip sonuncusunu diske yaziyordu.
        /// Denemek ile SECMEK ayri seyler; ayri kapilari var.
        /// </summary>
        public static void UseLanguage(int index)
        {
            if (index < 0 || index >= Languages.Length) return;
            Apply(index);
        }

        /// <summary>
        /// Tercih mantigini yeniden kosar: kayitli secim varsa o, yoksa
        /// varsayilan.
        ///
        /// Turun "kayitsiz bir cihaz hangi dille aciliyor" sorusunu
        /// GERCEKTEN sorabilmesi icin var. Sabiti okuyup "1 mi" diye
        /// bakmak, sabitin kendisini olcmek olurdu - acilis yolunu degil.
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
        /// Bir KISI ADINI ekranda gosterilecek hale getirir.
        /// </summary>
        /// <remarks>
        /// Dil Cince'yken butun arayuz Noto Sans SC ile ciziliyor ve o
        /// yazi tipinde Latin Extended-A YOK: g-breve, noktali I,
        /// noktasiz i ve S-cedilla bulunmuyor. Kaynak yazi tipinde de
        /// yok, yani alt kumeye eklenerek cozulemez.
        ///
        /// Personel isim havuzundaki doksan alti isimden ON ALTISI bu
        /// harfleri tasiyor (Ayse, Ibrahim, Yagmur, Sila...). Cince
        /// oynayan bir oyuncu her alti personelden birini "Ay[]e" diye
        /// goruyordu - ve hicbir kontrol bunu yakalamiyordu, cunku
        /// isimler yerellestirme tablosunda degil.
        ///
        /// Cozum Cince ICERIK TABLOSUNUN zaten uyguladigi kuralin ta
        /// kendisi: Latin ozel adlardan Turkce isaretler dusuyor.
        /// Burada da ayni sey, ayni sebeple.
        ///
        /// Diger dort dilde metin OLDUGU GIBI donuyor - Rubik bu
        /// harflerin hepsini tasiyor.
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
                    case 'ı': d = 'i'; break;   // noktasiz i
                    case 'İ': d = 'I'; break;   // noktali I
                    case 'ş': d = 's'; break;   // s cedilla
                    case 'Ş': d = 'S'; break;
                }
                if (d != c && sb == null) sb = new System.Text.StringBuilder(name, 0, i, name.Length);
                if (sb != null) sb.Append(d);
            }
            return sb == null ? name : sb.ToString();
        }

        /// <summary>Anahtarin metni. Yoksa [anahtar].</summary>
        public static string T(string key)
        {
            if (_table == null) return key;
            if (_table.TryGetValue(key, out string v)) return v;

            if (Warned.Add(key)) Debug.LogWarning("Metin yok: " + key);
            return "[" + key + "]";
        }

        /// <summary>{0}, {1}... yerine deger koyar.</summary>
        public static string T(string key, params object[] args)
        {
            return string.Format(Culture, T(key), args);
        }

        /// <summary>
        /// Para. Santi-sikke -> gorunur metin.
        ///
        /// Cekirdek parayi SANTI tutuyor (docs/23 2.2) cunku tamsayi
        /// aritmetigi belirlenimci. Oyuncu kurus gormuyor: bolme yalnizca
        /// burada, gosterim aninda yapiliyor.
        /// </summary>
        public static string Money(long centi)
        {
            return (centi / 100).ToString("N0", Culture) + " " + T("ui.common.coin");
        }

        /// <summary>
        /// Yuzde. ISARETIN YERI DILDEN GELIYOR.
        ///
        /// Kod bir sure `"%" + (bp / 100)` yaziyordu ve bu TURKCE
        /// konumu koda gomuyordu: Ingilizce'de "Margin %62" cikiyordu,
        /// dogrusu "62%". Loc kultur secimini (tr-TR / en-GB) titizlikle
        /// cozuyor ve yorumunda "ayni oyun, iki farkli sayi yazimi" diye
        /// uyariyor - yuzde bicimi o sistemin disinda kalmisti.
        ///
        /// .NET'in "P" bicimi kullanilmiyor: o bir kesri (0,62) bekliyor
        /// ve biz bin-puan (6200) tutuyoruz; ayrica ondalik gostermek
        /// isteyen yok. Yalnizca ISARETIN YERI kulturden aliniyor.
        /// </summary>
        /// <param name="bp">Bin-puan (6200 = %62).</param>
        /// <param name="isaretli">Basina + konsun mu (fark gosterirken).</param>
        public static string Percent(int bp, bool isaretli = false)
        {
            int yuzde = bp / 100;
            string sayi = (isaretli && yuzde > 0 ? "+" : "")
                          + yuzde.ToString(Culture);

            // PercentPositivePattern: 0 -> "n %", 1 -> "n%", 2 -> "%n",
            // 3 -> "% n". Turkce 2 ("%62"), Ingilizce 1 ("62%").
            switch (Culture.NumberFormat.PercentPositivePattern)
            {
                case 0: return sayi + " %";
                case 2: return "%" + sayi;
                case 3: return "% " + sayi;
                default: return sayi + "%";
            }
        }

        /// <summary>Itibar. Santi-puan -> 0-100 arasi tek ondalik.</summary>
        public static string Reputation(int centi)
        {
            // Ondalik TAMSAYI aritmetigiyle: kayan nokta gorunum
            // katmaninda bile gereksiz, ve ayraci kulturden geliyor.
            return (centi / 100) + Culture.NumberFormat.NumberDecimalSeparator
                   + ((centi / 10) % 10);
        }
    }
}
