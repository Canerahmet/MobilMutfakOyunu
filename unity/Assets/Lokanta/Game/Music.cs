using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Muzik de KODLA URETILIYOR. Tek bir ses dosyasi yok.
    ///
    /// Yaklasim: kisa bir dongu degil, SUREKLI SENTEZ. OnAudioFilterRead
    /// her ses karesinde cagriliyor ve notalari oracikta uretiyoruz.
    /// Bunun iki faydasi var: APK'da sifir bayt, ve muzik oyunun durumuna
    /// gore degisebiliyor - servis yogunlastikca tempo artiyor.
    ///
    /// Muzikal secim mutfak kimliginden bagimsiz tutuldu. Bir Turk
    /// lokantasina "Turk muzigi" koymak ucuz bir kestirme olurdu; kimlik
    /// SALONDA yasiyor (docs/10). Buradaki sey sakin, tekrarli, on plana
    /// cikmayan bir zemin - yonetim oyunu saatlerce oynaniyor ve one
    /// cikan bir melodi ucuncu saatte dusman olur.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class Music : MonoBehaviour
    {
        public float Volume = 0.22f;

        /// <summary>0 sakin, 1 yogun. Servis doluluguna gore ayarlaniyor.</summary>
        public float Intensity = 0f;

        private double _phase;
        private double _sampleRate = 48000.0;
        private int _step;
        private double _stepTime;
        private float _current;

        // Do minor pentatonik: sade, hicbir yere "gitmeyen" bir dizi.
        // Cozulmeyen bir dizi, arka planda kalmasi gereken muzik icin dogru.
        private static readonly float[] Scale =
        {
            130.81f, 155.56f, 174.61f, 196.00f, 233.08f,      // C3 Eb3 F3 G3 Bb3
            261.63f, 311.13f, 349.23f, 392.00f, 466.16f,      // C4 ...
        };

        // Adim dizisi elle yazildi: rastgele nota, uzun dinlemede
        // rastgele DUYULUYOR. Tekrarli bir sey daha cabuk siradanlasiyor
        // ama daha az rahatsiz ediyor.
        private static readonly int[] Steps =
        { 0, 3, 2, 5, 3, 7, 5, 3, 2, 0, 3, 5, 7, 5, 3, 2 };

        private void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            AudioSource src = GetComponent<AudioSource>();
            src.playOnAwake = true;
            src.loop = true;
            src.spatialBlend = 0f;
            // Bos bir klip: OnAudioFilterRead'in cagrilmasi icin kaynagin
            // CALIYOR olmasi gerekiyor.
            src.clip = AudioClip.Create("sessiz", 1024, 1, (int)_sampleRate, false);
            src.Play();
        }

        /// <summary>
        /// Kapanista SES ONCE SUSUYOR.
        ///
        /// OnAudioFilterRead SES IS PARCACIGINDAN cagriliyor, yani ana
        /// parcaciktan bagimsiz. Uygulama kapanirken AudioSource ve klip
        /// yok edilirken bu geri cagri hala icerideyse erisim ihlali
        /// (0xC0000005) oluyor - Windows yapisi kapanista tam bunu
        /// yapiyordu ve kimse gormuyordu, cunku turun cikis koduna
        /// BAKAN BIR SEY YOKTU (bkz. tools/unity/tour.ps1).
        ///
        /// Oyuncu icin de onemli: kapanista coken bir oyun, Windows'un
        /// "program calismayi durdurdu" penceresini gosterir.
        ///
        /// Bayrak `volatile`: iki ayri parcacik okuyup yaziyor.
        /// </summary>
        private volatile bool _sustu;

        private void OnApplicationQuit()
        {
            // UC ADIM, CUNKU IKISI YETMEDI.
            //
            // Yalnizca bayrak + Stop denendi ve kapanistaki erisim
            // ihlali surdu. Bilesenin KENDISI kapatiliyor (Unity artik
            // geri cagriyi hic cagirmiyor) ve dinleyici duraklatiliyor:
            // ses is parcaciginin kapanis temizligiyle ust uste
            // gelebilecegi bir pencere kalmasin.
            _sustu = true;
            AudioSource src = GetComponent<AudioSource>();
            if (src != null) src.Stop();
            enabled = false;
            AudioListener.pause = true;
        }

        private void OnDisable() { _sustu = true; }

        // Yeniden etkinlestirilirse muzik geri gelsin: OnDisable bayragi
        // kaldiriyor ve bir daha indirmezsek bilesen sessiz kalirdi.
        private void OnEnable() { _sustu = false; }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_sustu)
            {
                for (int i = 0; i < data.Length; i++) data[i] = 0f;
                return;
            }

            // Nota suresi yogunlukla kisaliyor: sakin 0,62 sn, yogun 0,40.
            double noteSeconds = Mathf.Lerp(0.62f, 0.40f, Mathf.Clamp01(Intensity));
            double inc = 1.0 / _sampleRate;

            for (int i = 0; i < data.Length; i += channels)
            {
                _stepTime += inc;
                if (_stepTime >= noteSeconds)
                {
                    _stepTime -= noteSeconds;
                    _step = (_step + 1) % Steps.Length;
                    _phase = 0.0;
                }

                float hz = Scale[Steps[_step] % Scale.Length];
                _phase += hz * inc;

                float t = (float)(_stepTime / noteSeconds);
                // Yumusak zarf: her nota sesizce giriyor ve sonuyor.
                float env = Mathf.Sin(t * Mathf.PI);
                env *= env;

                // Ucgen dalga, sinusten biraz daha "ahsap" duyuluyor ve
                // kare dalga kadar sert degil.
                float phase = (float)(_phase - System.Math.Floor(_phase));
                float tri = 4f * Mathf.Abs(phase - 0.5f) - 1f;

                // Bir oktav alt, cok kisik: zemin.
                float bass = Mathf.Sin((float)(_phase * 0.5f) * 2f * Mathf.PI) * 0.30f;

                _current = Mathf.Lerp(_current, (tri * 0.55f + bass) * env, 0.25f);
                float v = _current * Volume;

                for (int c = 0; c < channels; c++) data[i + c] = v;
            }
        }
    }
}
