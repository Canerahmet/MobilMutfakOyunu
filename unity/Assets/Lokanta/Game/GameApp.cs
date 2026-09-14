using System;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Lokanta.Game.Ui;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Oyunun tamami: icerik, simulasyon, ekranlar ve kayit.
    ///
    /// Tek bir MonoBehaviour olmasinin sebebi Unity'nin yasam dongusu.
    /// Icerik bir kez yukleniyor ve mutfak degistiginde YALNIZCA mutfaga
    /// bagli kisim yeniden yukleniyor; ekonomi (economy.json, huylar,
    /// personel rolleri) mutfaktan bagimsiz.
    ///
    /// Gorunum katmani simulasyonu OKUR, ona yazmaz. Yazan tek sey komut
    /// (docs/23). Bu sinifin Send() disinda simulasyona dokunan bir uyesi
    /// yok - AdvanceToNextDay haric, o da bir asama gecisi.
    /// </summary>
    public sealed class GameApp : MonoBehaviour
    {
        [Header("Baglantilar")]
        public UiRoot Ui;
        public RestaurantView View;
        public CameraRig Rig;

        /// <summary>
        /// DUNKU gun raporu. Yoksa Gecerli false.
        ///
        /// Neden var: oyunda tek bir gun-onceki-gun karsilastirmasi
        /// yoktu. Bir yonetim oyununda ogrenmenin tek yolu "bir seyi
        /// degistir, ertesi gun bak" - besinci gunde fiyati 51'den
        /// 58'e cikaran oyuncu altinci gunde "568" goruyor ve bunun iyi
        /// mi kotu mu oldugunu BILMIYOR. Ogrenme durunca ikinci gune
        /// donmek icin sebep kalmiyor.
        ///
        /// Kayda YAZILMIYOR: yalnizca oturum icinde bir kiyas. Kayittan
        /// donen oyuncu bir gun kiyassiz gecirir, sonra yeniden baslar.
        /// </summary>
        public DayReport Yesterday;

        /// <summary>Dunku rapor var mi.</summary>
        public bool HasYesterday;

        /// <summary>Gunun saatini ekrana ceviren bilesen.</summary>
        public DayLight Light;
        public Music Music;

        [Header("Hiz")]
        [Tooltip("Bir gercek saniyede kac simulasyon saniyesi")]
        /// <summary>
        /// Kac simulasyon milisaniyesi, bir gercek milisaniyede. 1 =
        /// gercek zaman.
        ///
        /// VARSAYILAN 60 IDI VE OYUNU OYNANAMAZ HALE GETIRIYORDU.
        /// Servis penceresi 480.000 sim-ms; 60'ta bu SEKIZ GERCEK
        /// SANIYE eder. docs/16 servis icin 90-180 sn diyor, yani oyun
        /// tasarlandigi surenin on besde birinde akiyordu.
        ///
        /// Altinda kalan her sey coktu: en sabirsiz musterinin TUM
        /// sabri 10.000 sim-ms, yani 0,17 gercek saniye. Masa rozetinin
        /// mavi-sari-kirmizi gecisi goz kirpmasindan kisa; masaya
        /// dokunup mudahale hedefi secmek (kamera gecisi tek yon 0,35
        /// sn) servisin %10'unu yiyor; bildirim balonu 3,5 saniye
        /// yasiyor, yani servisin %44'u. Servis asamasinin butun
        /// arayuzu insan tepki suresine gore yazilmisti ve hicbiri
        /// yetismiyordu.
        ///
        /// 4 = 120 saniye, docs/16 bandinin ortasi. Eski 60, artik
        /// hiz dugmesinin en ustu: "x15".
        /// </summary>
        public float TimeScale = 4f;

        /// <summary>
        /// Uygulama kapaniyor mu. Temizlik yollari buna bakiyor:
        /// kapanista Unity zaten her seyi bosaltiyor ve o sirada elle
        /// Destroy cagirmak surecin cokmesine yol acabiliyor.
        /// </summary>
        public static bool Quitting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void IzleKapanis()
        {
            Quitting = false;
            Application.quitting += () => { Quitting = true; };
        }

        /// <summary>
        /// Hiz etiketinin "x1" saydigi olcek. Oyuncuya gosterilen sayi
        /// TimeScale / BaseTimeScale.
        /// </summary>
        public const float BaseTimeScale = 4f;
        public bool Paused = true;

        public Simulation Sim { get; private set; }
        public ContentSet Content { get; private set; }
        public EconomyConfig Economy { get; private set; }
        public string Cuisine { get; private set; }
        public int Slot { get; private set; } = -1;
        public string LoadError { get; private set; }

        private IContentSource _src;
        private float _accumulator;
        private int _lastDay = -1;

        // Olay tamponu BOSALTILIYOR (Drain), indisle okunmuyor: tampon
        // halka ve dolunca bastan yaziyor, yani sakladigimiz bir indis
        // sessizce gecersizlesir.
        private readonly SimEvent[] _events = new SimEvent[256];

        public bool InGame { get { return Sim != null; } }

        // =====================================================================
        private void Awake()
        {
            // KARE HIZI 30, VE vSYNC KAPALI.
            //
            // vSyncCount != 0 iken Android targetFrameRate'i yok
            // sayiyor; kalite ayarindaki varsayilan 1'di, yani hedef
            // hicbir zaman uygulanmiyordu ve oyun ekranin yenileme
            // hizina (90/120 Hz olabilir) kosuyordu.
            //
            // Hedef 30: docs/19 kare hizi tabani bu, ve oyun statik bir
            // sahneye bakiyor - 60 fps'in getirdigi hicbir sey yok,
            // goturdugu sey pil ve isi.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;

            try
            {
                _src = new ResourcesContentSource();
                Loc.Load(_src);
                Economy = ContentLoader.LoadEconomy(_src);
            }
            catch (Exception e)
            {
                LoadError = e.Message;
                Debug.LogError("Icerik yuklenemedi: " + e);
            }
        }

        private void Start()
        {
            AudioSource sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            Sfx.Init(sfx);

            if (Ui != null)
            {
                Ui.App = this;
                Ui.Replace(string.IsNullOrEmpty(LoadError)
                    ? (UiScreen)new MainMenuScreen()
                    : new ErrorScreen(LoadError));
            }

            // Kendi kendine gezen tur. Yalnizca komut satirinda istenirse.
            if (Autopilot.Requested && Ui != null)
                gameObject.AddComponent<Autopilot>()
                          .Begin(this, Ui.GetComponent<UnityEngine.UIElements.UIDocument>());
        }

        // =====================================================================
        /// <summary>Yeni kampanya. Yuva -1 ise henuz kaydedilmemis.</summary>
        public bool StartNew(string cuisine, int slot)
        {
            if (!LoadCuisine(cuisine)) return false;
            Slot = slot;
            Sim = NewSimulation();
            AfterSimChanged();
            return true;
        }

        /// <summary>Kayitli oyunu yukler.</summary>
        public bool LoadSlot(int slot)
        {
            SlotInfo info = SaveStore.Read(slot);
            if (!info.Exists || info.Broken) return false;
            if (!LoadCuisine(info.Cuisine)) return false;

            Simulation sim = NewSimulation();
            if (!SaveStore.Load(slot, sim)) return false;

            Slot = slot;
            Sim = sim;
            AfterSimChanged();
            return true;
        }

        public bool SaveToSlot(int slot)
        {
            if (Sim == null) return false;

            bool ok = SaveStore.Save(slot, Sim, Cuisine);
            if (ok)
            {
                Slot = slot;
                _saveFailed = false;
                return true;
            }

            // KAYIT BASARISIZLIGI SESSIZ KALMAMALI.
            //
            // SaveStore.Save her istisnayi yutup false donuyordu ve bu
            // false'u HICBIR CAGRI YERI okumuyordu. Cihaz depolamasi
            // dolu bir oyuncu otuz gun oynar, uygulamadan cikar ve
            // hicbir sey bulamazdi - tek bir uyari gormeden.
            //
            // Bildirim GUNDE BIR: her gun basinda kaydediliyor ve altmis
            // gun boyunca ayni balonu basmak, uyariyi gurultuye cevirir.
            if (!_saveFailed)
            {
                _saveFailed = true;
                PushNotice(Loc.T("notice.save_failed"), NoticeTone.Bad);
                Sfx.Cancel();
            }
            return false;
        }

        /// <summary>
        /// Kayit en son basarisiz mi oldu. Uyariyi tekrarlamamak icin.
        /// </summary>
        private bool _saveFailed;

        /// <summary>Oyunu birakip menuye doner.</summary>
        public void LeaveGame()
        {
            if (Sim != null && Slot >= 0) SaveToSlot(Slot);
            Sim = null;
            Slot = -1;
            Paused = true;
            if (View != null) View.Clear();
        }

        private bool LoadCuisine(string cuisine)
        {
            try
            {
                Content = ContentSetLoader.Load(_src, cuisine);
                Cuisine = cuisine;
                return true;
            }
            catch (Exception e)
            {
                LoadError = e.Message;
                Debug.LogError("Mutfak yuklenemedi (" + cuisine + "): " + e);
                return false;
            }
        }

        private Simulation NewSimulation()
        {
            TimingConfig timing = Content.SlotDurationsBp != null
                ? TimingConfig.Default()
                    .WithSlotDurations(Content.SlotDurationsBp)
                    .WithEatMs(Content.EatMs)
                : TimingConfig.Default();

            // Tohum SAATTEN: her yeni kampanya farkli olmali. Kaydedilen
            // tohum kayitla birlikte geliyor, yani yuklenen oyun ayni
            // kampanyayi surduruyor.
            // UtcNow: yerel saat yaz saati gecisinde ve kullanici saati
            // degistirdiginde GERIYE gidebiliyor; tohum icin sicrayan bir
            // saat, iki kampanyanin ayni tohumla baslamasi demek.
            ulong seed = unchecked((ulong)DateTime.UtcNow.Ticks);
            return new Simulation(Economy, Content, timing, seed);
        }

        private void AfterSimChanged()
        {
            Paused = true;
            _accumulator = 0f;
            _lastDay = Sim.Day;
            if (View != null) View.Rebuild();
            if (Rig != null)
            {
                Rig.OpenTables = Sim.TableCount;
                Rig.Overview();
                Rig.Snap();          // oyuna girerken kamera ucmasin
            }
        }

        // =====================================================================
        private void Update()
        {
            if (Sim == null) return;

            // Genisleyince kamera yeni kanadi da cerceveliyor. Tek bir
            // int karsilastirmasi; degisim altmis gunde en fazla uc kez.
            if (Rig != null) Rig.OpenTables = Sim.TableCount;

            // YURUYUS HIZI OYUNUN SAATIYLE AYNI TEMPODA.
            //
            // Oyuncu x16'ya basinca simulasyon on alti kat hizli akiyor.
            // Yuruyus gercek zamanda kalsaydi figurler olan bitenin
            // onlarca saniye gerisinde kalir, ekranda gordugu sey
            // simulasyonla ilgisiz olurdu. Duraklatildiginda da
            // yuruyus durmali: duran bir dunyada yuruyen garson,
            // duraklamanin ne ise yaradigini bozar.
            Walker.GameSpeed = Paused ? 0f : TimeScale / BaseTimeScale;

            // GUNUN SAATI: golgeler, isigin rengi, arka plan ve sokak
            // lambalari tek bir sayidan (servis ilerlemesi) okunuyor.
            if (Light != null) Light.Apply(Sim.Phase, Sim.ServiceProgressBp / 10000f);

            if (!Paused && Sim.Phase == DayPhase.Service)
            {
                _accumulator += Time.deltaTime * TimeScale * 1000f;

                // BUTCE 40, 400 DEGIL.
                //
                // Time.deltaTime maximumDeltaTime (0,333 sn) ile
                // sinirli; en yuksek hizda (x16) bu tek karede 213 tick
                // demek ve butce 400'e izin veriyordu - yani 22 ms'lik
                // tek bir kare mumkundu. Ve bu KENDINI BESLIYOR: uzun
                // kare -> daha buyuk birikim -> daha uzun kare. Bir
                // kayit yazimi ya da bir cop toplama tetiklemeye yeter.
                //
                // x16'da bir karede islenmesi gereken en fazla ~21 tick;
                // 40 iki kat pay birakiyor. Fazla biriken zaman
                // ATILIYOR ve dogrusu bu: hiz tusu "gunu hizla gec"
                // demek, tam simulasyon dogrulugu degil.
                // BIRIKEN BORC GERCEKTEN ATILIYOR.
                //
                // Yorum "Fazla biriken zaman ATILIYOR" diyordu ama kod
                // bunu YAPMIYORDU: butce dolunca `_accumulator`'da
                // kalan kismi kimse silmiyor, yalnizca
                // AfterSimChanged sifirliyordu. Sonucu olcum katmanini
                // yaniltiyordu - x240'ta biriken borc, tur x1'e
                // dustukten SONRA da kare basina 40 tick (~4 sim
                // saniyesi) hizinda akmaya devam ediyor. Canlilik
                // penceresinin butun mantigi "burada x1'de olcuyoruz"
                // varsayimina dayaniyor ve o varsayim yanlisti.
                //
                // Tavan: bir karede islenecek tick sayisi kadar birikim
                // tutuluyor, gerisi atiliyor. Hiz tusu "gunu hizla gec"
                // demek, tam simulasyon dogrulugu degil.
                const int budgetMax = 40;
                float tavan = budgetMax * TimingConfig.TickMs;
                if (_accumulator > tavan) _accumulator = tavan;

                int budget = budgetMax;
                while (_accumulator >= TimingConfig.TickMs && budget-- > 0)
                {
                    _accumulator -= TimingConfig.TickMs;
                    Sim.Tick();
                    if (Sim.ServiceComplete)
                    {
                        // SERVIS BITTIGINI SOYLEYEN BIR SEY OLMALI.
                        //
                        // Once servis penceresi dolup salon bosalinca oyun
                        // sessizce duruyordu: bildirim yok, serit yeniden
                        // kurulmuyor, asama hala "Servis", "Duraklat"
                        // dugmesi hala "Duraklat" yaziyor - ustelik
                        // Paused zaten true. Oyuncu donmus bir salona
                        // bakiyor ve "Gunu Kapat"i kendi bulmak zorunda
                        // kaliyordu.
                        Paused = true;
                        if (!_serviceEndAnnounced)
                        {
                            _serviceEndAnnounced = true;
                            PushNotice(Loc.T("notice.service_done"), NoticeTone.Good);
                        }
                        break;
                    }
                }
            }

            ReadEvents();
            AgeNotices();
            UpdateMusic();
            CheckSeasonEnd();
        }

        /// <summary>
        /// Simulasyon olaylarini SESE ve YAZIYA cevirir.
        ///
        /// Once yalnizca sese ceviriyordu ve bu oyunun en buyuk eksigiydi:
        /// cekirdek otuz uc tur olay uretiyor, gorunum yedisini okuyup ses
        /// caliyordu. Stok bitmesi, personel istifasi, kizgin cikis, kusen
        /// musteri, gecikmis maas - hepsi hesaplaniyor ve atiliyordu.
        /// Ustelik mobilde ses cogunlukla KAPALI, yani pratikte geri
        /// bildirim sifirdi.
        ///
        /// Olaylar cekirdegin tek disari konusma yolu (docs/23 5) ve
        /// gorunum katmani onlari yalnizca OKUYOR.
        /// </summary>
        private void ReadEvents()
        {
            int n = Sim.Events.Drain(_events);

            // Ses SINIRLI: hizli kipte tek karede yuzlerce tick isleniyor
            // ve ayni sesi elli kez ust uste calmak kirpilma ve ani islemci
            // yuku demek.
            int bell = 0, coin = 0, upset = 0, pour = 0;

            for (int i = 0; i < n; i++)
            {
                SimEvent e = _events[i];
                switch (e.Kind)
                {
                    case SimEventKind.CustomerSeated: if (bell++ < 2) Sfx.DoorBell(); break;
                    case SimEventKind.CustomerPaid: if (coin++ < 2) Sfx.Coin(); break;
                    case SimEventKind.CustomerLeftAngry: if (upset++ < 2) Sfx.Upset(); break;
                    case SimEventKind.FoodServed: if (pour++ < 2) Sfx.Pour(); break;
                    case SimEventKind.StaffLeveledUp: Sfx.LevelUp(); break;
                    case SimEventKind.CreditCollected: if (coin++ < 2) Sfx.Coin(); break;
                    case SimEventKind.DayOpened: Sfx.DayChange(); break;
                    case SimEventKind.TurnedAway: Sfx.Cancel(); break;
                    case SimEventKind.StaffResigned: Sfx.Upset(); break;
                    case SimEventKind.DishUnlocked: Sfx.LevelUp(); break;
                }

                // Hikaye sahnesi BILDIRIM DEGIL: uc saniyelik bir serit,
                // oyunun en iyi cumlesini gecistirmek olurdu. Aksama
                // saklaniyor ve orada tam ekran bir kart oluyor.
                if (e.Kind == SimEventKind.RegularStoryBeat)
                {
                    _pendingStory = e.A;
                    _pendingBeat = e.B;
                    continue;
                }

                if (Notices.Describe(in e, Content, Sim, out string text, out NoticeTone tone))
                    PushNotice(text, tone);
            }

            if (Sim.Day != _lastDay) _lastDay = Sim.Day;
        }

        // =====================================================================
        /// <summary>
        /// Ekranda bekleyen bildirimler. Halka degil KISA BIR LISTE: ayni
        /// anda ucten fazlasini gostermek okunmuyor, ve en yenisi en
        /// onemlisi.
        /// </summary>
        public const int MaxNotices = 3;

        private readonly string[] _noticeText = new string[MaxNotices];
        private readonly NoticeTone[] _noticeTone = new NoticeTone[MaxNotices];
        private readonly float[] _noticeLeft = new float[MaxNotices];

        public int NoticeCount { get; private set; }
        public string NoticeTextAt(int i) { return _noticeText[i]; }
        public NoticeTone NoticeToneAt(int i) { return _noticeTone[i]; }

        /// <summary>Ekran bunu okuyup kendini yeniden kurmali mi.</summary>
        public bool NoticesChanged { get; private set; }
        public void NoticesSeen() { NoticesChanged = false; }

        /// <summary>
        /// Servis bitisi bir kez duyuruldu mu. Her karede yeniden
        /// duyurmak balonlari doldururdu.
        /// </summary>
        private bool _serviceEndAnnounced;

        private void PushNotice(string text, NoticeTone tone)
        {
            // Ayni metin ust uste gelirse tekrarlanmiyor: on dort masalik
            // bir salonda "malzeme bitti" ayni saniyede uc kez dusebiliyor.
            if (NoticeCount > 0 && _noticeText[0] == text)
            {
                _noticeLeft[0] = NoticeSeconds;
                return;
            }

            for (int i = MaxNotices - 1; i > 0; i--)
            {
                _noticeText[i] = _noticeText[i - 1];
                _noticeTone[i] = _noticeTone[i - 1];
                _noticeLeft[i] = _noticeLeft[i - 1];
            }
            _noticeText[0] = text;
            _noticeTone[0] = tone;
            _noticeLeft[0] = NoticeSeconds;

            if (NoticeCount < MaxNotices) NoticeCount++;
            NoticesChanged = true;
        }

        private const float NoticeSeconds = 3.5f;

        private void AgeNotices()
        {
            bool dropped = false;
            for (int i = 0; i < NoticeCount; i++)
            {
                _noticeLeft[i] -= Time.deltaTime;
                if (_noticeLeft[i] <= 0f) { NoticeCount = i; dropped = true; break; }
            }
            if (dropped) NoticesChanged = true;
        }

        /// <summary>
        /// Kampanya doldu mu. Dolduysa yil sonu degerlendirmesi aciliyor.
        ///
        /// Bu bag bir zamanlar HIC YOKTU: EndScreen yazilmisti, puanlama
        /// tasarlanmisti, CampaignDays icerikte duruyordu - ve altmisinci
        /// gun gelip geciyor, hicbir sey olmuyordu. Oyun sonsuza kadar
        /// suruyor, kapanisi olmuyordu.
        ///
        /// Bayrak SIMULASYONDA ve kayda giriyor; ikinci acilista ayni ekran
        /// tekrar cikmiyor.
        /// </summary>
        private void CheckSeasonEnd()
        {
            if (Sim == null || Ui == null) return;
            if (!Sim.SeasonJustEnded) return;

            // BAYRAK EKRAN KAPANINCA KONUYOR, ACILINCA DEGIL.
            //
            // Once sirayla: isaretle, KAYDET, sonra ekrani ac. Yani
            // "gorulmus" bilgisi, oyuncu daha tek satir okumadan diske
            // yaziliyordu. Telefon o anda kilitlenirse (ya da biri
            // arayip uygulama arkaya atilirsa) OnApplicationPause bir
            // kez daha kaydediyor, Android uygulamayi olduruyor ve
            // SeasonJustEnded bir daha true donmuyor.
            //
            // Kaybedilen sey kucuk degil: yedi eksenli degerlendirme,
            // plaket, altmis gunun tek kapanisi. Ve EndScreen'i acan
            // baska hicbir cagri yeri yoktu.
            // EKRAN BIR KEZ ACILIYOR.
            //
            // Yukaridaki duzeltme (bayragi kapanista koymak) dogruydu ama
            // bir kapi acti: SeasonJustEnded, ekran KAPANANA KADAR true
            // kaliyor ve bu metot Update'ten KOSULSUZ cagriliyor. Yani
            // 61. gunden itibaren her karede yeni bir EndScreen
            // kuruluyordu - 30 fps'te dakikada 1800 tane, her biri
            // sim.Score() cagirip oyunun en agir panelini insa ederek.
            //
            // Oyuncu icin sonucu daha da kotuydu: "Serbest oyuna devam"
            // tek bir Ui.Pop() yapiyor ve altindan AYNI ekran cikiyor.
            // Altmis gunluk kampanya, cikilamayan bir ekranda bitiyordu.
            //
            // GameScreen'in hikaye kartinda ayni koruma zaten var
            // (Ui.Top == this); burada yoktu cunku bu metot GameApp'ten,
            // yani ustte ne oldugundan bagimsiz cagriliyor.
            if (Ui.Top is EndScreen) return;

            Paused = true;
            Ui.Push(new EndScreen());
        }

        /// <summary>
        /// Bugun acilan hikaye sahnesi; yoksa -1.
        ///
        /// Gunde EN FAZLA BIR tane: iki sahne ust uste gelirse ikincisi
        /// ertesi gunu bekliyor. Ard arda iki kart, ikisini de okunmaz
        /// yapar.
        /// </summary>
        private int _pendingStory = -1;
        private int _pendingBeat;

        public bool HasStory { get { return _pendingStory >= 0; } }
        public int StoryRegular { get { return _pendingStory; } }
        public int StoryBeat { get { return _pendingBeat; } }

        public void StorySeen() { _pendingStory = -1; }

        private void UpdateMusic()
        {
            if (Music == null) return;
            // Yogunluk = salonun dolulugu. Muzik oyunun durumunu
            // izliyor; sabit bir dongu, servis kilitlendiginde de ayni
            // sakinlikte calardi.
            float busy = Sim.TableCount > 0
                ? Sim.OccupiedTables / (float)Sim.TableCount : 0f;
            Music.Intensity = Mathf.Lerp(Music.Intensity, busy, Time.deltaTime * 0.6f);
        }

        // =====================================================================
        /// <summary>
        /// Uygulama arka plana alininca KAYDET.
        ///
        /// Android arka plandaki oyunlari duzenli olarak olduruyor. Bu
        /// kanca olmadan, telefon caldiginda ya da uygulama
        /// degistirildiginde O GUNUN TAMAMI gidiyordu: alinan malzeme,
        /// kurulan menu, ise alinan personel, gunun cirosu. Mobil yonetim
        /// oyunlarinda tek yildizli yorumlarin bir numarali sebebi bu.
        ///
        /// OnApplicationQuit degil OnApplicationPause: Android'de cikis
        /// kancasi guvenilir degil, duraklatma kancasi guvenilir.
        ///
        /// Yazmanin kendisi atomik (SaveStore.WriteAtomic), yani sik
        /// tetiklemek bozuk kayit riski yaratmiyor.
        /// </summary>
        /// <summary>
        /// Arka plana atilirken BIR KEZ kaydediliyor.
        ///
        /// Android'de uygulamayi arka plana atmak OnApplicationPause(true)
        /// VE OnApplicationFocus(false) geri cagrilarinin IKISINI BIRDEN
        /// tetikliyor, yani kayit iki kez yaziliyordu - tam da isletim
        /// sisteminin uygulamayi oldurmeye hazirlandigi anda.
        ///
        /// Kayit ucuz degil: simulasyon durumu ~12.500 sayisal alan ve
        /// yazim ana is parcaciginda, dusuk seviye bir telefonda 30-60
        /// ms. Iki kati, tam olarak kaybedilecek en kotu anda.
        ///
        /// One donunce bayrak dusuyor, yani bir sonraki arka plana
        /// atilma yine kaydediyor.
        /// </summary>
        private bool _savedOnBackground;

        private void OnApplicationPause(bool paused)
        {
            if (!paused) { _savedOnBackground = false; return; }
            SaveOnBackground();
        }

        /// <summary>
        /// BELLEK BASKISINDA DA KAYIT.
        ///
        /// Android ON PLANDAYKEN de bellek baskisi altinda uygulamayi
        /// oldurebiliyor; hedef cihaz 3 GB (docs/19). O yolda son kayit
        /// gun basindakidir - bir gunluk oynanis gider ve oyuncu neden
        /// gittigini anlamaz.
        ///
        /// SaveOnBackground zaten cift kayda karsi korumali.
        /// </summary>
        private void OnEnable()
        {
            Application.lowMemory += OnLowMemory;
        }

        private void OnDisable()
        {
            Application.lowMemory -= OnLowMemory;

            // RENDER OLCEGI GERI KONUYOR.
            //
            // Quality.ApplyZoom URP varliginin renderScale'ini
            // degistiriyor ve o varlik projede TEK dosya. Editorde
            // oyuncu yakinlastirip play'den cikinca deger 1,0'da
            // kaliyor, bir sonraki varlik kaydinda DISKE yaziliyor ve
            // 0,8'lik mobil butce sessizce kayboluyor. Restore yazilmis
            // ama hicbir yerden cagrilmiyordu.
            Quality.Restore();
        }

        private void OnLowMemory()
        {
            SaveOnBackground();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) { _savedOnBackground = false; return; }
            SaveOnBackground();
        }

        /// <summary>
        /// Duman turu kosuyor mu.
        ///
        /// Tek etkisi: ODAK KAYBINDA DURAKLATMAYI kapatmak. Oyunun
        /// kendisi icin dogru davranis (telefonda uygulama arka plana
        /// alininca oyun durmali ve kaydetmeli), ama tur masaustunde
        /// kosuyor ve baska bir pencere one geldiginde olcum donmus bir
        /// dunyayi olcuyor.
        /// </summary>
        public static bool SmokeTour;

        private void SaveOnBackground()
        {
            if (SmokeTour) return;
            Paused = true;
            if (_savedOnBackground) return;
            _savedOnBackground = true;
            if (Sim != null && Slot >= 0) SaveToSlot(Slot);
        }

        /// <summary>
        /// Mudahalelerin hedefledigi masa. -1 = secim yok.
        ///
        /// GORUNUM DURUMU, simulasyon durumu degil: docs/23 7.2 kamera ve
        /// secimi komut saymiyor, yani kaydedilmiyor ve tekrar oynatmayi
        /// etkilemiyor. Kaydedilseydi ayni komut dizisi iki farkli
        /// sonuc verebilirdi.
        ///
        /// Secim yoksa mudahaleler eski davranisa donuyor: sabri en az
        /// kalan masa. Yani yakinlasmadan da oynanabiliyor - secim bir
        /// ZORUNLULUK degil, bir INCELIK.
        /// </summary>
        public int SelectedTable = -1;

        /// <summary>
        /// Secimi gecerli tutar. Masa bosaldiysa ya da servis bittiyse
        /// secim dusuyor - bos bir masayi hedefleyen bir "Cay ikram"
        /// dugmesi, oyuncuya hicbir sey soylemeden hakkini yerdi.
        /// </summary>
        public int ValidSelection()
        {
            if (Sim == null || SelectedTable < 0) return -1;
            if (Sim.Phase != DayPhase.Service) { SelectedTable = -1; return -1; }
            if (SelectedTable >= Sim.TableCount) { SelectedTable = -1; return -1; }

            CustomerStage st = Sim.TableStage(SelectedTable);
            if (st == CustomerStage.None || st == CustomerStage.Done
                || st == CustomerStage.LeftAngry)
            {
                SelectedTable = -1;
                return -1;
            }
            return SelectedTable;
        }

        public void Send(CommandKind kind, int a = 0, int b = 0, int c = 0)
        {
            if (Sim == null) return;
            Sim.Apply(new Command(Sim.TickIndex, kind, a, b, c));
        }

        public void OpenService()
        {
            _serviceEndAnnounced = false;
            Send(CommandKind.OpenService);
            Paused = false;
            Sfx.Confirm();
        }

        public void CloseDay()
        {
            Send(CommandKind.CloseDay);
            Paused = true;
            Sfx.Confirm();
        }

        public void NextDay()
        {
            if (Sim == null) return;

            // BUGUNKU RAPOR DUNE TASINIYOR.
            //
            // Gun ilerlemeden once alinmali: AdvanceToNextDay sayaclari
            // sifirliyor ve rapor bir daha kurulamiyor.
            Yesterday = Sim.BuildDayReport();
            HasYesterday = true;

            Sim.AdvanceToNextDay();
            if (Slot >= 0) SaveToSlot(Slot);      // her gun basinda kaydet
            if (View != null) View.Rebuild();
        }

        public void RestockRecommended()
        {
            if (Sim == null) return;
            long before = Sim.Cash;

            // TEK KOMUT, elli degil.
            //
            // Once malzeme basina bir OrderIngredient gonderiliyordu ve
            // gunluk komut siniri 256: bu dugmeye bes kez basmak gunun
            // butcesini bitiriyor, sonrasinda fiyat, menu, ise alim,
            // ekipman, genisleme, mudahale ve veresiye dahil HER komut
            // sessizce reddediliyordu. Parasi yetmeyen bir oyuncu icin
            // daha da kolaydi: alim reddedilse bile komut GUNLUGE
            // yaziliyor, stok dolmadigi icin "eksik" hala eksik ve
            // oyuncu tekrar basiyor.
            Send(CommandKind.OrderRecommended);

            if (Sim.Cash != before) Sfx.Coin();
        }
    }
}
