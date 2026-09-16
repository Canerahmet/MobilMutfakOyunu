using System;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Sesler once DOSYADAN, yoksa koddan.
    ///
    /// `Resources/audio/<ad>` altinda bir klip varsa o calinyor; yoksa ayni
    /// isimli sentezlenmis ton devreye giriyor. Yani ses dosyasi eklemek
    /// KOD DEGISIKLIGI ISTEMIYOR - dosyayi klasore koymak yetiyor.
    ///
    /// Neden boyle: bir sure butun sesler yalnizca kodla uretiliyordu ve
    /// gerekcesi ticariydi ("indirilen her dosya bir lisans karari").
    /// Gerekce yarim dogru: lisans gercek bir risk ama sentez de gercek
    /// bir maliyet - basit dalga bicimleri bir lokanta oyununda ucuz
    /// duyuluyor ve mutfak, kapi zili, kalabalik gibi sesler sentezle
    /// inandirici olmuyor.
    ///
    /// Iki katmanli cozum ikisini de cozuyor: sentez YEDEK olarak
    /// duruyor (yani oyun ses dosyasi olmadan da tam calisiyor ve
    /// APK'ya sifir bayt ekliyor), dosya konulursa o kazaniyor.
    /// Konulacak dosyalarin listesi ve kaynaklari: Art/ATTRIBUTION.md.
    ///
    /// Sentez basit ama kasitli: her ses bir ZARF (yukselis-sonus) ile
    /// carpiliyor, cunku zarfsiz bir ton "bip" gibi duyuluyor. Tiklamalar
    /// kisa ve yumusak; mutfak sesleri gurultulu; para sesi iki tonlu.
    /// </summary>
    public static class Sfx
    {
        /// <summary>
        /// Ornekleme hizi. 22050, 44100 degil.
        ///
        /// Sesler tik, onay, para, zil gibi kisa ve dar bantli seyler;
        /// 22 kHz'de fark duyulmuyor ama bellek ve sentez suresi
        /// YARILANIYOR. Onceki degerde on klip 181.000 ornek demekti ve
        /// hepsi acilista, bir delege uzerinden, cogu birkac Mathf.Sin
        /// cagirarak uretiliyordu - dusuk seviye bir telefonda ilk
        /// karede 20-40 ms.
        ///
        /// Klip verisi de 724 KB'dan 362 KB'a iniyor ve gecici float
        /// dizileri buyuk nesne yiginina dusmuyor.
        /// </summary>
        public const int SampleRate = 22050;

        private static AudioSource _source;
        private static AudioClip _click, _confirm, _cancel, _coin, _bell,
                                 _sizzle, _pour, _upset, _levelUp, _day;
        private static float _volume = 0.7f;
        private static bool _built;

        public static float Volume
        {
            get { return _volume; }
            set { _volume = Mathf.Clamp01(value); }
        }

        /// <summary>
        /// Dosya varsa onu, yoksa sentezlenmis klibi dondurur.
        ///
        /// Resources.Load klasor yoksa da sessizce null donuyor, yani
        /// ses dosyasi hic eklenmemis bir projede hata cikmiyor.
        /// </summary>
        /// <summary>
        /// Dosya varsa onu, yoksa SENTEZLER.
        ///
        /// Sentez artik ancak gerektiginde calisiyor: once dosyaya
        /// bakiliyor, bulunmazsa uretiliyor. Onceden on klibin hepsi
        /// kosulsuz sentezleniyor, sonra dosya varsa CÖPE ATILIYORDU -
        /// yani yapilan isin bir kismi bastan bosunaydi.
        /// </summary>
        private static AudioClip Prefer(string file, System.Func<AudioClip> synth)
        {
            AudioClip c = Resources.Load<AudioClip>("ses/" + file);
            return c != null ? c : synth();
        }

        /// <summary>Sahnede bir kez cagriliyor.</summary>
        public static void Init(AudioSource source)
        {
            _source = source;
            if (_built) return;

            // HER KLIP ICIN ONCE DOSYA, YOKSA SENTEZ.
            //
            // Isimler Art/ATTRIBUTION.md'deki listeyle ayni; oradaki tablo
            // hangi dosyanin nereye dusecegini soyluyor.
            _click = Prefer("tik", () => Tone("tik", 0.045f, (t, n) =>
                Env(t, n, 0.004f, 0.040f) * Sine(t, 880f) * 0.35f));

            _confirm = Prefer("onay", () => Tone("onay", 0.22f, (t, n) =>
                Env(t, n, 0.010f, 0.20f) *
                (Sine(t, 587f) * 0.5f + Sine(t, 880f) * 0.35f) * 0.4f));

            _cancel = Prefer("iptal", () => Tone("iptal", 0.18f, (t, n) =>
                Env(t, n, 0.006f, 0.17f) *
                (Sine(t, 392f) * 0.5f + Sine(t, 294f) * 0.35f) * 0.4f));

            // Para: iki hizli ton, ikincisi yukarida. "Kasa" duygusu.
            _coin = Prefer("para", () => Tone("para", 0.26f, (t, n) =>
            {
                float a = Env(t, n, 0.004f, 0.09f) * Sine(t, 1047f);
                float b = t > 0.07f
                    ? Env(t - 0.07f, n, 0.004f, 0.15f) * Sine(t - 0.07f, 1568f)
                    : 0f;
                return (a * 0.45f + b * 0.5f) * 0.4f;
            }));

            // Zil: kapi cani. Uc harmonikli, uzun sonus.
            _bell = Prefer("kapi-zili", () => Tone("zil", 0.85f, (t, n) =>
                Env(t, n, 0.003f, 0.80f) *
                (Sine(t, 1319f) * 0.5f + Sine(t, 1976f) * 0.28f
                 + Sine(t, 2637f) * 0.14f) * 0.32f));

            // Cizirti: gurultu, alcak gecirgen his icin yumusatilmis.
            _sizzle = Prefer("cizirti", () => Tone("cizirti", 0.55f, (t, n) =>
                Env(t, n, 0.05f, 0.45f) * Noise(t) * 0.22f));

            // Dokme: gurultu + alcalan ton.
            _pour = Prefer("dokme", () => Tone("dokme", 0.45f, (t, n) =>
                Env(t, n, 0.03f, 0.40f) *
                (Noise(t) * 0.35f + Sine(t, 320f - 120f * (t / n)) * 0.25f) * 0.3f));

            // Kizgin musteri: alcalan iki ton.
            _upset = Prefer("kizgin", () => Tone("kizgin", 0.35f, (t, n) =>
                Env(t, n, 0.006f, 0.32f) *
                Sine(t, 330f - 90f * (t / n)) * 0.42f));

            // Seviye atlama: yukselen uclu.
            _levelUp = Prefer("seviye", () => Tone("seviye", 0.5f, (t, n) =>
            {
                float f = t < 0.14f ? 523f : (t < 0.28f ? 659f : 784f);
                return Env(t, n, 0.006f, 0.45f) * Sine(t, f) * 0.4f;
            }));

            // Gun donumu: yumusak, alcak, iki tonlu.
            _day = Prefer("gun-donumu", () => Tone("gun", 0.7f, (t, n) =>
                Env(t, n, 0.06f, 0.62f) *
                (Sine(t, 262f) * 0.4f + Sine(t, 349f) * 0.3f) * 0.35f));

            _built = true;
        }

        /// <summary>
        /// Kac ses DOSYADAN geliyor. Tur bunu raporluyor: "sesler tamam"
        /// demek, sentezlenmis on tonun da gecerli sayilmasi demekti.
        /// </summary>
        public static int FileBackedCount()
        {
            string[] names = { "tik", "onay", "iptal", "para", "kapi-zili",
                               "cizirti", "dokme", "kizgin", "seviye",
                               "gun-donumu" };
            int n = 0;
            for (int i = 0; i < names.Length; i++)
                if (Resources.Load<AudioClip>("ses/" + names[i]) != null) n++;
            return n;
        }

        // --- calma ------------------------------------------------------------
        private static void Play(AudioClip c, float scale = 1f)
        {
            if (_source == null || c == null) return;
            _source.PlayOneShot(c, _volume * scale);
        }

        public static void Click() { Play(_click); }
        public static void Confirm() { Play(_confirm); }
        public static void Cancel() { Play(_cancel); }
        public static void Coin() { Play(_coin); }
        public static void DoorBell() { Play(_bell, 0.6f); }
        public static void Sizzle() { Play(_sizzle, 0.5f); }
        public static void Pour() { Play(_pour, 0.6f); }
        public static void Upset() { Play(_upset, 0.8f); }
        public static void LevelUp() { Play(_levelUp); }
        public static void DayChange() { Play(_day); }

        // --- sentez -----------------------------------------------------------
        private static AudioClip Tone(string name, float seconds, Func<float, float, float> f)
        {
            int count = Mathf.CeilToInt(seconds * SampleRate);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(f(t, seconds), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sine(float t, float hz)
        {
            return Mathf.Sin(t * hz * 2f * Mathf.PI);
        }

        /// <summary>
        /// Belirlenimci gurultu. Random KULLANILMIYOR: ayni tohum ayni
        /// dalga demek, ve sesin kosudan kosuya degismesinin bir anlami
        /// yok. Ayrica cekirdegin rastgele akislarina hic dokunmuyor.
        /// </summary>
        private static float Noise(float t)
        {
            float x = t * 12345.678f;
            return Mathf.Repeat(Mathf.Sin(x) * 43758.5453f, 2f) - 1f;
        }

        /// <summary>Yukselis-sonus zarfi. Zarfsiz ton "bip" gibi duyuluyor.</summary>
        private static float Env(float t, float total, float attack, float release)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / attack;
            float rest = total - attack;
            if (rest <= 0f) return 0f;
            float k = (t - attack) / Mathf.Max(0.0001f, release);
            return Mathf.Exp(-3f * k);
        }
    }
}
