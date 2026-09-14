using Lokanta.Core.Sim;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// GUNUN SAATI EKRANDA.
    ///
    /// Kullanicinin cumlesi: "oyun icerisinde sabah ogle ve aksam
    /// ayirimi belli degil". Dogruydu - gunun evresi yalnizca arayuzdeki
    /// bir yazidan okunuyordu, salon her saatte ayni aydinliktaydi.
    ///
    /// Dort kanal birden degisiyor, cunku tek kanal (orn. yalnizca isik
    /// siddeti) "aksam oldu" degil "biri lambayi kistı" diye okunuyor:
    ///
    ///   1. GUNESIN ACISI  - sabah dogudan yatik, ogle tepede, aksam
    ///      batidan yatik. Golgelerin yonu ve boyu gunun en guclu
    ///      saat isareti.
    ///   2. ISIGIN RENGI   - sabah soguk, ogle notr, ikindi sicak,
    ///      aksam mor-mavi.
    ///   3. ARKA PLAN      - gokyuzu yerine duz renk (docs/19 bütçesi);
    ///      gunduz acik gri-mavi, gece neredeyse siyah.
    ///   4. LAMBALAR       - sokak lambalari, restoranin ic tavan
    ///      isiklari ve icerinin sicak dolgusu aksam yaniyor.
    ///
    /// IC ISIKLAR SOKAKTAKINDEN ONCE YANIYOR (RoomThreshold 0,62 -
    /// LampThreshold 0,76). Ikisi ayni anahtarda oldugunda aksam ustu
    /// salon karanlik kaliyordu; kullanicinin "aksam olunca restoranin
    /// ici karanlik oluyor" sikayeti tam olarak o araligi tarif
    /// ediyordu. Bir lokanta zaten kendi isigini sokak lambalarindan
    /// once acar.
    ///
    /// DEGERLER ARA DEGERLENIYOR: gun icinde kesme gecis yok, yoksa
    /// "saat 14 oldu" diye bir kare atliyor. Servis ilerlemesi 0-1
    /// arasi bir orana cevriliyor ve butun kanallar o orandan okunuyor.
    /// </summary>
    public sealed class DayLight : MonoBehaviour
    {
        public Light Sun;
        public Light Fill;
        public Light Warm;      // aksam ici dolgu
        public Camera Cam;

        /// <summary>Sokak lambalarinin isikli parcalari.</summary>
        public Renderer[] LampHeads;

        /// <summary>Lambalarin yerdeki isik havuzlari.</summary>
        public GameObject[] LampGlow;

        /// <summary>
        /// RESTORANIN ICI tavan isiklari.
        ///
        /// Sokak lambalarindan ayri tutuluyor, cunku ikisi ayri zamanda
        /// yaniyor: bir lokanta kendi isigini sokak lambalarindan ONCE
        /// aciyor (ortalik karardiginda degil, kararmaya basladiginda).
        /// Ayni anahtara baglanmis olsalar aksam ustu salon karanlik
        /// kalirdi - kullanicinin bildirdigi sey tam olarak bu.
        /// </summary>
        public GameObject[] RoomGlow;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _block;
        private bool _lampsOn;
        private bool _roomOn;
        private float _applied = -1f;

        /// <summary>Gunun orani, 0 sabah - 1 gece. Turun sorabilmesi icin.</summary>
        public float DayProgress { get; private set; }

        /// <summary>Lambalar yaniyor mu. Turun sorabilmesi icin.</summary>
        public bool LampsOn { get { return _lampsOn; } }

        /// <summary>Sokak lambalarinin yanmaya basladigi oran.</summary>
        public const float LampThreshold = 0.76f;

        /// <summary>
        /// Restoranin ic isiklarinin yanmaya basladigi oran.
        ///
        /// Sokak lambalarindan ONCE: gun ortasindan sonra salonun
        /// icindeki isik gunesten degil kendi lambalarindan geliyor.
        /// 0,62 servisin yaklasik ucte ikisi - ikindi.
        /// </summary>
        public const float RoomThreshold = 0.62f;

        /// <summary>Ic isiklar yaniyor mu. Turun sorabilmesi icin.</summary>
        public bool RoomLightsOn { get { return _roomOn; } }

        /// <summary>Bagli ic tavan isigi sayisi. Turun sorabilmesi icin.</summary>
        public int RoomLightCount { get { return RoomGlow == null ? 0 : RoomGlow.Length; } }

        // =====================================================================
        /// <summary>
        /// Evreyi ve servis ilerlemesini gunun oranina cevirir.
        ///
        /// Sabah 0,00-0,12 arasinda duruyor (servis acilmadan once kisa
        /// bir sabah), servis 0,12-0,88'i kapliyor, aksam 0,88-1,00.
        /// Boylece servisin kendisi gunun govdesi oluyor ve oyuncu
        /// servisi acar acmaz "gun basladi" hissini aliyor.
        /// </summary>
        public void Apply(DayPhase phase, float serviceProgress01)
        {
            BindLamps();

            float t;
            switch (phase)
            {
                case DayPhase.Morning: t = 0.10f; break;
                case DayPhase.Service:
                    t = 0.12f + Mathf.Clamp01(serviceProgress01) * 0.76f;
                    break;
                case DayPhase.Evening: t = 0.94f; break;
                default: t = 0.10f; break;
            }
            DayProgress = t;

            // Ayni kareyi iki kez yazmak yok: isik ayarlari ucuz degil
            // ve gun orani karede binde bir degisiyor.
            if (Mathf.Abs(t - _applied) < 0.002f) return;
            _applied = t;

            if (Sun != null)
            {
                // EGIM BIR BANTTA KALIYOR: 44-66 derece.
                //
                // Kullanicinin bildirdigi sey: "odalardaki golgeler
                // baska odalara kayiyor". Ikinci sebep buydu - egim
                // sabah 26, aksam 10 dereceydi ve golge boyu h/tan(aci):
                // 1,8 m'lik bir buzdolabi aksam 10 derecede 10 metre
                // golge birakiyor, yani uc odayi birden gecen koyu bir
                // bant.
                //
                // Bant icinde en uzun golge ~1,9 m; odalarin en dari
                // 3,2 m, yani golge kendi odasinda kaliyor.
                //
                // GUNUN SAATI KAYBOLMUYOR: yon (azimut) 148'den 268'e
                // donmeye devam ediyor ve saati asil o soyluyor -
                // golgeler sabah bir yana, aksam ote yana uzuyor. Boy
                // degil YON okunuyor.
                Sun.transform.rotation = Quaternion.Euler(
                    Curve(t, 44f, 66f, 52f, 46f),
                    Curve(t, 148f, 208f, 246f, 268f), 0f);

                // AKSAM GOLGESI SILIKLESIYOR.
                //
                // Gece salonu aydinlatan sey yonlu gunes degil, ic
                // isiklar (havuzlar + sicak dolgu). Tam guclu bir
                // yonlu golge o isigin altinda yanlis duruyor: isik
                // tavandan geliyor ama golge yandan.
                Sun.shadowStrength = Curve(t, 0.85f, 1.00f, 0.90f, 0.35f);
                Sun.color = Mix(t,
                    new Color(1.00f, 0.88f, 0.74f),
                    new Color(1.00f, 0.97f, 0.93f),
                    new Color(1.00f, 0.85f, 0.66f),
                    new Color(0.62f, 0.55f, 0.72f));
                Sun.intensity = Curve(t, 1.05f, 1.55f, 1.25f, 0.32f);
            }

            // DOLGU ISIGI = YANSIMANIN TAKLIDI.
            //
            // Kullanicinin sorusu: "normalde isik yansiyarak diger
            // kisimlari da aydinlatmaz mi gercekte". Evet - ve bu
            // sahnede HIC yansima yok: kuresel aydinlatma yok, isik
            // haritasi da PISIRILEMEZ, cunku butun restoran calisma
            // aninda kuruluyor (RestaurantView geometriyi masa sayisina
            // gore uretiyor). Yansimanin yerini tutacak tek sey, ters
            // yonden gelen golgesiz bir dolgu ile ortam isigi.
            //
            // Gece bu dolgu MAVIYDI (0,42 / 0,46 / 0,70) ve 0,22
            // siddetindeydi. Yani sicak anahtarin vurmadigi her yuzey
            // SOGUK bir isikla dolduruluyordu - sicak bir salonun icinde
            // yansimanin yapacaginin tam tersi. "Icerisi yeterince aydinlik
            // degil" sikayetinin buyuk kismi buradan geliyordu: govdelerin
            // anahtara bakmayan yarisi hem karanlik hem yanlis renkti.
            //
            // Gece artik sicak ve guclu: gercek bir yansima da tavandan
            // ve duvarlardan gelen SICAK isiktir.
            if (Fill != null)
            {
                Fill.color = Mix(t,
                    new Color(0.70f, 0.78f, 0.95f),
                    new Color(0.74f, 0.80f, 0.94f),
                    new Color(0.86f, 0.76f, 0.72f),
                    new Color(0.94f, 0.78f, 0.60f));
                Fill.intensity = Curve(t, 0.50f, 0.58f, 0.52f, 0.75f);
            }

            RenderSettings.ambientLight = Mix(t,
                new Color(0.30f, 0.32f, 0.39f),
                new Color(0.38f, 0.39f, 0.43f),
                new Color(0.35f, 0.31f, 0.31f),
                // Gece ortami TAMAMEN kararmiyor: oyuncunun hangi
                // masanin dolu oldugunu gormesi gerekiyor. Karanlik bir
                // atmosfer, okunmayan bir salon pahasina olmamali.
                //
                // SICAK ve daha parlak (0,21/0,20/0,25 -> 0,32/0,28/0,26).
                // Ortam isigi bu boru hattinda yansimanin tek karsiligi:
                // kuresel aydinlatma yok ve geometri calisma aninda
                // uretildigi icin isik haritasi pisirilemiyor. Soguk bir
                // ortam, sicak isikli bir salonda yanlis cevap.
                //
                // Ortam GLOBAL: sokagi da vuruyor. Kabul edilebilir ve
                // hatta dogru - aydinlik bir lokantanin onundeki kaldirim
                // gercekte de vitrinden sizan isikla aydinlanir. Karsitligi
                // koruyan sey ortam degil, gokyuzunun gece neredeyse
                // siyah olmasi.
                new Color(0.66f, 0.58f, 0.50f));

            // ARKA PLAN GOKYUZU YERINE GECIYOR.
            //
            // docs/19 gokyuzu kubbesi tasimiyor (doldurma butcesi); arka
            // plan duz renk. Sokak goruununce o duz rengin "disarisi"
            // olmasi gerekti - once neredeyse siyahti ve gunduz bile
            // gece gibi okunuyordu. Simdi sabah soluk mavi, ogle acik
            // mavi, ikindi sicak, aksam gercekten karanlik.
            if (Cam != null)
                Cam.backgroundColor = Mix(t,
                    new Color(0.30f, 0.38f, 0.48f),
                    new Color(0.38f, 0.51f, 0.65f),
                    new Color(0.44f, 0.34f, 0.31f),
                    new Color(0.04f, 0.05f, 0.09f));

            // ICERININ SICAK DOLGUSU: salonun kendi isigi. Disarisi
            // soguyup kararirken icerinin sicak kalmasi, "acik bir
            // lokanta" goruntusunun butun anlami.
            //
            // IC ISIKLARLA AYNI ANDA BASLIYOR VE ESKIDEN DAHA GUCLU.
            //
            // Havuzlar yalnizca ZEMINI aydinlatiyor: masa, sandalye ve
            // figurler onlardan hicbir sey almiyor. Kullanicinin gordugu
            // "restoranin ici karanlik" sikayetinin asil sebebi buydu -
            // zemin aydinlaniyordu ama uzerindeki her sey karanlikta
            // kaliyordu. Yon neredeyse tepeden (62 derece) ve golge yok,
            // yani tavandan gelen bir aydinlatma gibi okunuyor.
            //
            // Yonlu isik disariyi da vuruyor (URP'de isik katmanlari
            // kapali, m_SupportsLightLayers: 0) ama sokakta govde yok:
            // yalnizca uc levha ve uc direk. Karsit etki, gokyuzunun
            // gece neredeyse siyah olmasiyla zaten kuruluyor.
            if (Warm != null)
            {
                float w = Mathf.InverseLerp(RoomThreshold - 0.06f, 1f, t);
                Warm.enabled = w > 0.01f;
                Warm.intensity = w * 3.20f;
            }

            // SOKAK GECE KOYULASIYOR.
            //
            // Kureseli yukseltip yereli dusurmek: ortam ve sicak dolgu
            // kaldirimi da aydinlatiyor (yerel isik yok), o yuzden
            // disarisi kendi malzemesinden karartiliyor. Olcum bunu
            // gerektirdi - salonun ortanca parlakligi sokagin
            // ortalamasindan DUSUKTU.
            if (_view != null)
                _view.TintStreet(Mathf.Lerp(1f, 0.28f,
                    Mathf.InverseLerp(RoomThreshold - 0.06f, 0.95f, t)));

            bool yanmali = t >= LampThreshold;
            if (yanmali != _lampsOn) Lamps(yanmali);

            bool icYanmali = t >= RoomThreshold;
            if (icYanmali != _roomOn) RoomLights(icYanmali);
        }

        // =====================================================================
        /// <summary>
        /// Sokak lambalarini bagliyor.
        ///
        /// Restoran buyudugunde gorunum yeniden kuruluyor ve lambalar da
        /// yeniden olusuyor; eski basvurular silinmis nesnelere isaret
        /// eder ve aksam hicbir sey yanmazdi - uyarisiz. Kurulus damgasi
        /// degisince yeniden bagliyor.
        /// </summary>
        private void BindLamps()
        {
            if (_view == null) _view = FindFirstObjectByType<RestaurantView>();
            if (_view == null) return;
            if (_view.BuildStamp == _bound) return;

            _bound = _view.BuildStamp;
            LampHeads = _view.LampHeads;
            LampGlow = _view.LampGlow;
            RoomGlow = _view.RoomGlow;
            _applied = -1f;       // yeni lambalara durumu yeniden yaz
            _lampsOn = !_lampsOn; // Lamps() cagrilsin diye zorluyoruz
            _roomOn = !_roomOn;   // RoomLights() de
        }

        private RestaurantView _view;
        private int _bound = -1;

        /// <summary>Bagli sokak lambasi sayisi. Turun sorabilmesi icin.</summary>
        public int LampCount { get { return LampHeads == null ? 0 : LampHeads.Length; } }

        private void Lamps(bool on)
        {
            _lampsOn = on;

            if (_block == null) _block = new MaterialPropertyBlock();

            // SONUK LAMBA DA BIR SEY: BEYAZ CAM.
            //
            // Kapaliyken bas KOYU GRIYDI (0,24) - yani gunduz lambanin
            // camı ile demiri ayni renkti ve fener "ucu kalinlasmis bir
            // direk" diye okunuyordu. Gercek bir fenerin camı gunduz de
            // beyazdir; referans gorselde de oyle.
            Color c = on ? new Color(1.00f, 0.86f, 0.52f)
                         : new Color(0.86f, 0.87f, 0.85f);

            if (LampHeads != null)
                for (int i = 0; i < LampHeads.Length; i++)
                {
                    if (LampHeads[i] == null) continue;
                    LampHeads[i].GetPropertyBlock(_block);
                    _block.SetColor(BaseColorId, c);
                    // 2,2 -> 3,4: cam artik kucuk bir levha degil bir
                    // fener govdesi ve huzmenin ciktigi yer olarak
                    // okunmasi gerekiyor. Emisyon 1'in uzerinde olmali,
                    // yoksa parlama degil yalnizca "acik renk" olur.
                    _block.SetColor(EmissionId, on ? c * 2.6f : Color.black);
                    LampHeads[i].SetPropertyBlock(_block);
                }

            if (LampGlow != null)
                for (int i = 0; i < LampGlow.Length; i++)
                    if (LampGlow[i] != null) LampGlow[i].SetActive(on);
        }

        /// <summary>
        /// Ic tavan isiklarini acar/kapatir.
        ///
        /// Govdesi olmayan isiklar: yalnizca yerdeki havuz. Bir
        /// armaturun kendisi cizilmiyor - kamera tavani olmayan bir
        /// binaya bakiyor ve orada asili bir kutu, aydinlattigi yeri
        /// kapatmaktan baska bir sey yapmazdi.
        /// </summary>
        private void RoomLights(bool on)
        {
            _roomOn = on;
            if (RoomGlow == null) return;
            for (int i = 0; i < RoomGlow.Length; i++)
                if (RoomGlow[i] != null) RoomGlow[i].SetActive(on);
        }

        /// <summary>Dort duraga gore ara deger. Sabah, ogle, ikindi, aksam.</summary>
        private static float Curve(float t, float a, float b, float c, float d)
        {
            if (t < 0.40f) return Mathf.Lerp(a, b, t / 0.40f);
            if (t < 0.72f) return Mathf.Lerp(b, c, (t - 0.40f) / 0.32f);
            return Mathf.Lerp(c, d, Mathf.Clamp01((t - 0.72f) / 0.28f));
        }

        private static Color Mix(float t, Color a, Color b, Color c, Color d)
        {
            if (t < 0.40f) return Color.Lerp(a, b, t / 0.40f);
            if (t < 0.72f) return Color.Lerp(b, c, (t - 0.40f) / 0.32f);
            return Color.Lerp(c, d, Mathf.Clamp01((t - 0.72f) / 0.28f));
        }
    }
}
