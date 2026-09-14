using System.Collections.Generic;
using Lokanta.Core.Sim;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Salonu kurar ve her karede simulasyondan OKUYARAK gunceller.
    /// Simulasyona hicbir sey yazmaz.
    ///
    /// Modeller sahnede baglaniyor (BuildGameScene onlari AssetDatabase
    /// ile buluyor), Resources'tan degil: Resources'a konan her sey
    /// derlemeye giriyor, oysa modellerin cogu tek bir sahnede kullaniliyor.
    ///
    /// Musteri figurleri HAVUZDAN geliyor. Yogun bir gunde saniyede
    /// birkac masa doluyor; her seferinde Instantiate/Destroy yapmak
    /// mobilde cop toplayiciyi tetikler ve kare atlatir.
    /// </summary>
    public sealed partial class RestaurantView : MonoBehaviour
    {
        public GameApp App;

        /// <summary>
        /// Onizleme icin dogrudan verilen simulasyon. App yoksa bu
        /// kullaniliyor.
        ///
        /// Neden var: sahneyi gozle gormek icin oyunu CALISTIRMAK
        /// gerekiyordu ve toplu kipte play mode guvenilir degil. Bu alan,
        /// editor aracinin GERCEK gorunum kodunu kosturmasini sagliyor -
        /// ayri bir onizleme kodu yazmak, gordugum seyin oyuncunun gordugu
        /// sey olmamasi demekti.
        /// </summary>
        [System.NonSerialized] public Simulation Preview;

        /// <summary>
        /// Duruslari klipten TEK KARE orneklemesi icin. Editor kipinde
        /// Animator islemiyor ve butun figurler baglanma durusunda -
        /// kollari yana acik - kaliyor. Oyunda kapali.
        /// </summary>
        [System.NonSerialized] public bool PreviewPoses;

        /// <summary>Onizlemede klibin hangi saniyesi orneklenecek.</summary>
        [System.NonSerialized] public float PreviewTime = 0.4f;

        private Simulation Source { get { return App != null && App.Sim != null ? App.Sim : Preview; } }

        [Header("Mobilya")]
        public GameObject TablePrefab;
        public GameObject ChairPrefab;
        public GameObject StovePrefab;
        public GameObject FridgePrefab;
        public GameObject CounterPrefab;
        public GameObject ShelfPrefab;
        public GameObject SinkPrefab;
        public GameObject PlantPrefab;

        /// <summary>Garsonun tasidigi tabak.</summary>
        public GameObject PlatePrefab;

        // =====================================================================
        // SAYDAM MALZEMELER SAHNEDEN GELIYOR, BURADA URETILMIYOR.
        //
        // Calisma aninda kurulan saydam bir malzeme EDITORDE dogru
        // goruunuyor ama GERCEK YAPIDA opak ciziliyor: URP saydam
        // gecisin golgelendirici varyantini, ona basvuran bir varlik
        // yoksa yapiya koymuyor. Duvarlar, kapi kanatlari ve firin cami
        // cihazda duz beyaz levhalar olarak cikiyordu.
        //
        // Ayni sinif hata daha once URP/Unlit ile yasandi (rozetler).
        // Malzemeler artik ArtPrefabs'in urettigi .mat VARLIKLARI ve
        // BuildGameScene onlari buraya bagliyor.

        /// <summary>Oda duvarlari (saydam). ArtPrefabs uretir.</summary>
        public Material WallMaterial;

        /// <summary>Kapi kanadi (saydam). ArtPrefabs uretir.</summary>
        public Material DoorMaterial;

        /// <summary>Firin cami (saydam). ArtPrefabs uretir.</summary>
        public Material GlassMaterial;

        /// <summary>Sokak lambasinin isik havuzu (isiksiz, toplayici).</summary>
        public Material GlowMaterial;

        /// <summary>
        /// Restoranin ic tavan isiklari icin toplayici malzeme.
        ///
        /// Sokaktakinden AYRI bir varlik, ayni malzemenin property block
        /// ile renklendirilmis hali degil: property block yazmak o
        /// cizicileri SRP toplu ciziminin disina atiyor ve tam genislemis
        /// bir restoranda 25 tavan isigi var.
        /// </summary>
        public Material CeilingGlowMaterial;

        /// <summary>Akan suyun saydam malzemesi. Varlik olmak zorunda.</summary>
        public Material WaterMaterial;

        /// <summary>Esik paspasinin rengi. Zeminden acik, dikkat cekmeyen.</summary>
        public Color MatColor = new Color(0.42f, 0.36f, 0.30f);

        [Header("Insanlar")]
        public GameObject[] CustomerPrefabs;
        public GameObject[] StaffPrefabs;

        /// <summary>
        /// Sandalye merkezinin masa merkezine uzakligi (m). Oturan figur
        /// de tam burada duruyor: oturma klibi govdeyi zaten sandalyeye
        /// yerlestiriyor. Once figur 0,80 m'ye, sandalyenin biraz
        /// disina konuyordu - ayakta duran bir figurun sandalyeyi
        /// kesmemesi icin. Klip gelince gerek kalmadi.
        /// </summary>
        /// <summary>
        /// Oturagin masa merkezine uzakligi. 0,48 OLCUMLE secildi.
        ///
        /// Iki kisitin arasinda tek bir dogru araligi var:
        ///   - Iki misafir BIRBIRINE girmemeli: 2r >= figur eni (0,90)
        ///   - Masa takimi KOMSU takima tasmamali: r + eni/2 <= 0,925
        ///     (hucre 1,85 m)
        /// Ikisi birden: 0,45 <= r <= 0,475. 0,48 ust sinira oturuyor ve
        /// 5 mm'lik tasma, komsu takimin ayni tarafi bos oldugunda
        /// gorunmuyor.
        /// </summary>
        /// <summary>
        /// Oturagin masa merkezine uzakligi. DORT YONDE DE AYNI.
        ///
        /// Masa KARE oldugu icin tek bir sayi yetiyor - dikdortgen
        /// masada yaricap eksene gore degismek zorundaydi ve iki taraf
        /// birden dogru olmuyordu (tepeden olculdu: kisa kenardaki
        /// sandalyeler masaya yapisik, uzun kenardakiler 0,15 m uzakta).
        ///
        /// 0,58 = 0,41 (yari en) + 0,17 pay. Ust sinir komsu masadan:
        /// 0,58 + figur eni/2 (0,32) = 0,90 <= 0,925 (hucre 1,85).
        /// Bos sandalyeler Z'de 0,58 + 0,15 = 0,73 <= 0,85 (hucre 1,70).
        ///
        /// Ayni sayi "karakterler masaya cok yakin" sikayetini de
        /// kapatiyor: masa kenarina uzaklik 0,04'ten 0,17 m'ye cikiyor.
        /// </summary>
        private const float SeatRadius = 0.58f;

        /// <summary>Olcek goruntusu ayni sayiyi kullansin diye.</summary>
        public const float SeatRadiusM = SeatRadius;

        /// <summary>
        /// Olcum goruntusu OYUNLA AYNI hesabi kullansin diye. Ayni
        /// sayiyi iki yere yazmak bu projede bes kez sessizce ayristi.
        /// </summary>
        public static Vector3 SeatAt(int k) { return Seat(k); }

        /// <summary>
        /// EKRANDA en fazla kac misafir cizilecek. Dordu degil IKISI.
        ///
        /// Bu bir taviz ve olcumle alindi. Oturan bir figurun ayak izi
        /// 0,90 x 1,01 m; komsu iki oturak arasi ise r = 0,48 m. Dort
        /// oturagi doldurmak icin figurun eni ya da derinligi r'nin
        /// altina inmeli, yani 0,48 m - bu da 0,66 m boyunda bir insan
        /// demek. Hesap her olcekte ayni cikiyor:
        ///
        ///   hucre 1,85 x 1,70 m | masa capi 0,88 | figur 0,90 x 1,01
        ///   -> 4 kisi icin gereken figur eni <= 0,545 m (boy ~0,66 m)
        ///
        /// Yani DORT KISI BU MASAYA HICBIR MAKUL OLCEKTE SIGMIYOR.
        /// Uc-dort kisilik gruplarda iki figur ciziliyor, digerleri
        /// cizilmiyor. Kaybedilen bilgi grup buyuklugu; ama o zaten
        /// okunmuyordu - dort figur tek bir kutleye donusuyordu ve
        /// masanin durumu rozette yaziyor.
        ///
        /// Simulasyon ETKILENMIYOR: grup yine dort kisilik, fisi de
        /// dort kisilik.
        /// </summary>
        private const int VisibleGuests = 2;

        /// <summary>
        /// MOBILYA MODELLERININ YEREL "ON" YONU KARAKTERIN TERSI.
        ///
        /// Olculdu (Editor/FigureShot, kirmizi kup +Z / mavi kup -Z ile,
        /// yedi mobilya tek karede):
        ///
        ///   karakter  yaw 0 -> yuzu  +Z
        ///   mobilya   yaw 0 -> onu   -Z   (sandalyenin minderi, ocagin
        ///                                  kapagi, buzdolabinin kolu,
        ///                                  lavabonun musluğu, hepsi)
        ///
        /// Kod bu farki bilmiyordu ve bir aci yazarken "karakter gibi"
        /// dusunuyordu: sonucta ocaklar duvara, tezgahlar disariya,
        /// sandalyeler masaya SIRTINI donuyordu. Kullanicinin cumlesi
        /// "sandalyeler ters" idi; sandalye yalnizca en gorunen orneğiydi,
        /// butun mobilya 180 derece ters duruyordu.
        ///
        /// Cagri yerlerindeki acilar KARAKTER kuralinda yaziliyor
        /// (0 = +Z'ye bak) ve bu sabit farki kapatiyor - boylece her
        /// cagri yerinde ayri bir 180 hatirlamak gerekmiyor.
        /// </summary>
        private const float PropYaw = 180f;
        private const int Seats = 4;

        /// <summary>
        /// Oturan figurun yerden yuksekligi (m).
        ///
        /// Tahmin degil olcum (Lokanta > Malzeme tanisi): oturma klibi
        /// govdeyi kendi icinde asagi indiriyor, yani figur oldugu yere
        /// konuldugunda zemine gomuluyor. Ayni kadar kaldirmak,
        /// kalcasini oturak yuksekligine getiriyor.
        ///
        /// 0,26 -> 0,334: ARTIK TAHMIN DEGIL, sandalyenin kendi yuzeyi.
        /// Olculdu (Editor/FigureShot -> OTURMA satirlari): minder
        /// yuzeyi 0,355 m, figurun legen alti 0,281 m - yani figur
        /// minderin 7,4 cm ALTINDA oturuyordu ve minder bacaklarin
        /// icinden geciyordu. Kullanicinin cumlesi buydu.
        ///
        /// 0,316 -> 0,410: diz kemigi eklendikten sonra yeniden
        /// olculdu. Artik uyluk YATAY duruyor ve mindere degen yuzey
        /// legenin ortasi degil UYLUKLARIN ALTI - o da kalca kemiginin
        /// 0,094 m altinda. Eski deger legenin ortasini mindere
        /// oturtuyordu, yani uyluklar minderin 9,4 cm icinden geciyordu.
        ///
        /// Ayaklar yerden ~0,23 m yukarida kaliyor: bu paketin bacaklari
        /// govdeye gore kisa (kalca-ayak 0,32 m, boyun %32'si; gercekte
        /// %52) ve ayaklari yere degdiren sandalye 0,24 m olurdu -
        /// oyuncak sandalye. Kullanici da "havada kalabilir" dedi.
        /// </summary>
        private const float SitLift = 0.410f;

        /// <summary>
        /// Oturan figurun masaya dogru kaymasi (m).
        ///
        /// Sandalyenin merkezine oturtulunca figurun SIRTI sirtligin
        /// 0,108 m icinde kaliyordu - sirtlik govdenin icinden
        /// geciyordu. Sandalye yerinde duruyor, yalnizca figur one
        /// geliyor: 0,108 + 0,022 pay.
        ///
        /// Ust siniri masa koyuyor, ve SIFIR TOPLAMLI: sandalye ile
        /// masa arasindaki bosluk 0,268 m, oturan figurun sirtindan
        /// gogsune derinligi 0,363 m. Figur 0,095 m fazla derin; biri
        /// mutlaka kesisecek.
        ///
        /// SIRTLIK secildi cunku GORUNEN o: sirtligin tepesi tablanin
        /// (0,55) ustunde, 0,636'da - govdeye giren bir sirtlik 34
        /// derecelik bakista dogrudan goruunuyor. Tablanin gobege
        /// binmesi ise tablanin ALTINDA kaliyor ve "masaya yakin
        /// oturmus" diye okunuyor.
        ///
        /// 0,15: sirtin arkasi -0,056, sirtligin onu -0,098 - 0,042 pay.
        /// Daha ileri gitmek kalcayi minderin on kenarindan disari
        /// tasiriyor; daha geri gitmek sirtligi govdenin icine sokuyor.
        /// </summary>
        private const float SitForward = 0.15f;

        /// <summary>
        /// OTURULAN sandalyenin yaricapi. Bos sandalye SeatRadius'ta,
        /// masaya yapisik.
        ///
        /// 0,65 = 0,58 + 0,07: figur yerinde kaliyor (0,65 - 0,22 =
        /// 0,43, eskiden 0,58 - 0,15), yalnizca sandalye geriye gidiyor.
        /// Ust sinir komsu hucre: 0,65 + sandalye yari derinligi (0,148)
        /// = 0,80 <= 0,85 (Z hucresi 1,70) ve <= 0,925 (X hucresi 1,85).
        /// </summary>
        private const float SeatRadiusUsed = 0.65f;

        /// <summary>Olcek goruntusu ayni sayiyi kullansin diye.</summary>
        public const float SeatRadiusUsedM = SeatRadiusUsed;

        /// <summary>Olcek goruntusu ayni sayilari kullansin diye.</summary>
        public const float SitLiftM = SitLift;

        /// <summary>Olcek goruntusu ayni sayilari kullansin diye.</summary>
        public const float SitForwardM = SitForward;

        [Header("Renkler")]
        // KAPALI ODA ARTIK CIZILMIYOR - rengi de yok.
        //
        // Once acilmamis odalar koyu gri birer levha olarak duruyordu ve
        // rengi (0,16) uc parlaklik olculerek dengelenmisti: zemin 0,061,
        // kapali 0,161, acik salon 0,258. Sayilar dogruydu, SORU yanlisti.
        //
        // Birinci kademede arsanin 172,8 m2'sinin yalnizca 102,6'si acik;
        // yani ekranin %41'i "henuz senin olmayan" levhaydi. Kullanicinin
        // cumlesi: "Bos odalar yer kaplamasin, restoran tam ekran olan
        // yerler gozuksun."
        //
        // Cizilmeyen oda cerceveye de girmiyor (CameraFit.OpenBounds) ve
        // genisleme artik gercekten bir ACILIS: oda yok iken beliriyor.
        // Dokunma carpisani da gitti - kapali odaya dokunmak kamerayi bos
        // bir levhaya goturuyordu.
        public Color RoomKitchen = new Color(0.22f, 0.24f, 0.27f);
        public Color RoomService = new Color(0.25f, 0.25f, 0.24f);

        private readonly List<Transform> _tables = new List<Transform>();
        private readonly List<TableBadge> _badges = new List<TableBadge>();
        private readonly List<GameObject> _pool = new List<GameObject>();
        private readonly Dictionary<int, GameObject> _seated = new Dictionary<int, GameObject>();
        private readonly List<GameObject> _staff = new List<GameObject>();
        private readonly List<Figure> _staffFigure = new List<Figure>();
        private int _staffBuilt = -1;

        /// <summary>
        /// Shader ozellik kimligi BIR KEZ cozuluyor. Her cagrida dizeyi
        /// yeniden karilamak, kare basina sekiz gereksiz arama demekti.
        /// </summary>
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Material _floorMat;
        private int _builtTables = -1;
        private MaterialPropertyBlock _block;

        // =====================================================================
        private void Awake()
        {
            // Shader.Find YAPIDA NULL DONEBILIR.
            //
            // URP/Lit "her zaman dahil" listesinde degil; yapiya yalnizca
            // sahnedeki bir prefabin malzemesi onu kullandigi icin
            // giriyor. O bag dolayli: sanat prefablari baska bir
            // gorsellestiriciye tasinirsa ya da varyant ayiklama devreye
            // girerse bu cagri null doner, new Material(null) gecersiz bir
            // malzeme uretir ve butun zeminler magenta cizilir. Editorde
            // asla gorulmez - yalnizca Android yapisinda.
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                Debug.LogError("SORUNLAR: URP/Lit bulunamadi; zemin cizilemiyor.");
                enabled = false;
                return;
            }

            _floorMat = new Material(lit);
            _floorMat.SetFloat("_Smoothness", 0.05f);

            // ROZET ISIKSIZ BIR MALZEME KULLANIYOR.
            //
            // Once zeminle ayni URP/Lit malzemeyi paylasiyordu ve
            // TableBadge'in yorumu "emisyon kapali oldugu icin renkler
            // isiktan bagimsiz okunuyor" diyordu. Bunun TERSI dogru:
            // emisyon kapaliysa renk tamamen isiga bagli. Olculdu -
            // yesil rozet ekranda 1,47:1 kontrastla cikiyordu; yazili
            // rengi ayni zeminde 7,57:1 verirdi. Isik rengin 5 katini
            // yiyordu.
            //
            // Sonuc: oyuncuya hangi masanin cikmak uzere oldugunu
            // soyleyen TEK kanal, anlamli bir grafik icin gereken 3:1
            // esiginin yarisindaydi.
            // GÖLGELENDIRICI YAPIYA BASVURUYLA GIRER.
            //
            // Shader.Find EDITORDE her zaman basarili - butun
            // gölgelendiriciler yuklu. Cihazda ise bir gölgelendirici
            // yapiya ancak bir varlik ona basvuruyorsa ya da
            // GraphicsSettings'in "her zaman dahil" listesindeyse girer.
            // URP/Unlit'e projede TEK BIR varlik bile basvurmuyordu:
            // Android'de Find null donecek, kod sessizce Lit malzemeye
            // dusecek ve rozet 1,47:1 kontrasta geri kacacakti - bugun
            // duzeltilen hatanin aynisi, yalnizca editorde gorunmeyen
            // hali.
            //
            // Iki koruma: gölgelendirici listeye eklendi
            // (ProjectSettings/GraphicsSettings.asset) ve burasi artik
            // SESSIZ DUSMUYOR. Sessiz geri donus, bir sonraki sefer
            // kimsenin fark etmeyecegi sey.
            // SAYDAM MALZEMELER: hepsi VARLIK, calisma aninda uretim yok.
            //
            // Eksikse SESSIZ DUSMUYORUZ. Sessiz geri donus, bir sonraki
            // sefer kimsenin fark etmeyecegi sey - ve tam bu hata
            // sinifinda (yapida opak cizim) editorde hicbir belirti
            // vermiyor.
            _glassMat = GlassMaterial;
            _wallMat = WallMaterial;
            _doorMat = DoorMaterial;
            _glowMat = GlowMaterial;
            _ceilMat = CeilingGlowMaterial;
            _waterMat = WaterMaterial;

            if (_glassMat == null || _wallMat == null
                || _doorMat == null || _glowMat == null || _ceilMat == null
                || _waterMat == null)
                Debug.LogError("SORUNLAR: saydam malzemeler bagli degil "
                               + "(ArtPrefabs.Run + BuildGameScene.Run calistir). "
                               + "Calisma aninda uretilen saydam malzeme YAPIDA OPAK cizilir.");

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null)
            {
                Debug.LogError("SORUNLAR: URP/Unlit yapida yok; rozetler "
                               + "isikli malzemeye dusuyor ve okunmuyor.");
                _badgeMat = _floorMat;
            }
            else
            {
                _badgeMat = new Material(unlit);
            }
        }

        private Material _badgeMat;
        private Material _glassMat;
        private Material _wallMat;
        private Material _doorMat;

        /// <summary>Kapi kanadinin rengi. Duvardan koyu ve daha opak.</summary>
        private static readonly Color DoorColor =
            new Color(0.62f, 0.45f, 0.30f, 0.55f);

        // DUVARIN RENGI VE SAYDAMLIGI ARTIK BIR VARLIKTA:
        // Art/Malzeme/ozel_duvar.mat, alfa 0,10.
        //
        // Burada `WallColor` diye bir alan vardi; saydam malzemeler
        // VARLIK olmak zorunda (saydam golgelendirici varyanti yapida
        // budanıyor, bkz. docs/36) ve alan silindi. Ozeti bir sure
        // SAHIPSIZ kaldi - hicbir alani anlatmayan, ustelik eski
        // degeri (0,20) tasiyan bir yorum. Tek dogru kaynak .mat.
        /// <summary>
        /// Duvarin yuksekligi (m). Karakter 1,00 m; 1,15 onun biraz
        /// ustunde - oda hattini ciziyor ama kamera 34 derecelik acidan
        /// icerisini goruyor. Tam boy bir duvar (2,4 m) on sirayi
        /// tamamen kapatirdi.
        /// </summary>
        private const float WallHeight = 1.15f;

        /// <summary>Duvarin kalinligi (m).</summary>
        private const float WallThick = 0.06f;
        private readonly List<Appliance> _stoves = new List<Appliance>();

        /// <summary>
        /// Rozet malzemesinin gölgelendirici adi. Turun sormasi icin:
        /// "isiksiz mi" sorusu yalnizca GERCEK YAPIDA anlamli, cunku
        /// Shader.Find editorde her zaman basariyor.
        /// </summary>
        public string BadgeShaderName
        {
            get
            {
                return _badgeMat != null && _badgeMat.shader != null
                    ? _badgeMat.shader.name : null;
            }
        }

        /// <summary>
        /// Calisma aninda kurulan zemin malzemesi. Yok edilmezse sahne
        /// kapandiginda siziyor.
        /// </summary>
        /// <summary>
        /// Yalnizca BURADA URETILEN malzemeler yok ediliyor.
        ///
        /// Duvar, kapi, cam ve isik havuzu artik .mat VARLIKLARI
        /// (ArtPrefabs uretiyor, BuildGameScene bagliyor) - onlari yok
        /// etmek diskteki varligin yuklu ornegini silmek olurdu ve bir
        /// sonraki sahnede malzeme kayip cikardi. Liste bu yuzden
        /// kisaldi, unutuldugu icin degil.
        /// </summary>
        private void OnDestroy()
        {
            // KAPANISTA ELLE TEMIZLIK YOK: Unity zaten her seyi
            // bosaltiyor ve o sirada Destroy cagirmak surecin cokmesine
            // yol acabiliyor (bkz. OwnedMesh).
            if (Application.isPlaying && !Application.isEditor
                && GameApp.Quitting) return;

            ClearTints();
            if (_floorMat != null) Destroy(_floorMat);
            if (_badgeMat != null && _badgeMat != _floorMat) Destroy(_badgeMat);
            if (_lampMat != null && _lampMat != _floorMat) Destroy(_lampMat);
        }

        /// <summary>
        /// Kurulan her seyi siler.
        ///
        /// EDITOR KIPINDE DestroyImmediate SART. Object.Destroy silmeyi
        /// kare sonuna erteliyor ve toplu kipte o kare hic gelmiyor -
        /// yani eski kat plani sahnede kaliyor. Uzun sure gorulmedi
        /// cunku iki mutfak da AYNI masa sayisiyla kuruluyordu ve ust
        /// uste binen ayni geometri ayni goruntuyu veriyordu. Acilis
        /// goruntusu eklenince ortaya cikti: "4 masa" yazan goruntude
        /// on masa vardi.
        /// </summary>
        public void Clear()
        {
            bool oyunda = Application.isPlaying;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject go = transform.GetChild(i).gameObject;
                if (oyunda)
                {
                    // OYUN KIPINDE Destroy KARE SONUNA ERTELENIYOR ve
                    // Rebuild() hemen ardindan yeni sahneyi kuruyor:
                    // O KARE ICINDE ESKI + YENI HER SEY SAHNEDE.
                    //
                    // Iki bedeli var. Gorunen: kademe gecisinde tek
                    // karelik iki kat cizim yuku, mobilde takilma.
                    // Sinsi olani: WallCount, WallsClear ve AccessOk
                    // cocuklari SAYIYOR - turun o karede okumasi
                    // denetimi SESSIZCE yaniltiyordu.
                    //
                    // Cozum yok etmeyi hizlandirmak degil, nesneyi
                    // AGACTAN VE GORUNTUDEN hemen cikarmak: SetParent
                    // ve SetActive aninda etki ediyor, Destroy kendi
                    // vaktinde tamamliyor.
                    go.SetActive(false);
                    go.transform.SetParent(null, false);
                    Destroy(go);
                }
                else DestroyImmediate(go);
            }

            _tables.Clear();
            _badges.Clear();
            _stoves.Clear();
            _tableSquareZ = 0f;
            _chairs.Clear();
            _potSpots.Clear();
            _potCount = 0;
            _tableFood.Clear();
            _tableTop = 0f;
            _chairBack.Clear();
            _doors.Clear();
            _roomGlow.Clear();
            _dirtyStack.Clear();
            _cleanStack.Clear();
            _foam.Clear();
            _cookRoutine.Clear();
            _staffTask.Clear();

            // SOKAKTAKILER DE TEMIZLENIYOR.
            //
            // Clear() butun cocuklari yok ediyor, yayalar da onlarin
            // arasinda - ama StreetLife kendi listesinde bes OLU kayit
            // tutmaya devam ediyordu ve her karede yok edilmis
            // nesnelere dokunuyordu.
            if (_streetLife != null) _streetLife.Clear();
            _pool.Clear();
            _seated.Clear();
            _queued.Clear();
            _leaving.Clear();
            _staff.Clear();
            _staffFigure.Clear();

            // IS KLIBI OLCUMU DE SIFIRLANIYOR.
            //
            // Sozluk personel INDEKSI ile anahtarlaniyor. Kadro
            // degisiminde indeksler yeniden kullaniliyor ama eski
            // ilerleme degeri duruyordu: yeni personel ilk karede eski
            // (buyuk) degerle karsilastiriliyor ve WorkAnimStalled
            // artiyordu. "Animasyon gercekten oynuyor mu" sorusunun TEK
            // olcusu bu sayac - kirli bir baslangic, DUZELEN bir hatayi
            // bozuk gostermeye devam eder.
            _isKlip.Clear();
            _washHold.Clear();
            _figureOf.Clear();
            _builtTables = -1;
            _staffBuilt = -1;
        }

        /// <summary>Kat planini bastan kurar. Masa sayisi degisince cagriliyor.</summary>
        public void Rebuild()
        {
            Simulation src = Source;
            if (src == null) return;
            Clear();

            int tables = src.TableCount;
            // Ton onbellegi kurulusta bosaltiliyor: kayit degisip baska
            // mutfaga gecildiginde eski renkler kalmamali. Kopyalar
            // ARTIK YOK DA EDILIYOR - bkz. ClearTints().
            ClearTints();
            BuildFloors(tables);
            BuildStreet(tables);
            BuildStreetLife();
            BuildWalls(tables);
            BuildRoomLights(tables);
            BuildRoomProps(tables);
            BuildTables(tables);
            // SAHNE SUSLEMESI EN SONDA: arka duvar, tabela, saksilar
            // ve mutfak davlumbazi. Hepsi mutfagin KIMLIGINE gore renk
            // aliyor.
            BuildDecor(tables);
            BuildPots(Pal(CuisineId));
            _builtTables = tables;
        }

        private void Update()
        {
            Simulation sim = Source;
            if (sim == null) return;
            if (sim.TableCount != _builtTables) { Rebuild(); return; }

            // Zemin YALNIZCA kurulusta boyaniyor.
            //
            // Once her karede boyaniyordu: sekiz zemin, her biri icin bir
            // property block okuma-yazma ve bir shader kimligi aramasi -
            // ustelik renkler yalnizca masa sayisi degisince degisiyor,
            // yani altmis gunde en fazla uc kez. Property block yazmak
            // ayrica o cizicileri SRP toplu ciziminin disina atiyor.
            UpdateCustomers(sim);
            UpdateStaff(sim);
            UpdatePlateStacks(sim);
            UpdateWater();
            if (WashingCount > 0) _washSeen++;
            UpdateAppliances(sim);
            UpdateDoors();
        }

        // =====================================================================
        private void BuildFloors(int tables)
        {
            if (_block == null) _block = new MaterialPropertyBlock();

            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];

                // ACILMAMIS ODA HIC KURULMUYOR.
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Oda_" + r.Name;
                floor.transform.SetParent(transform, false);
                floor.transform.localPosition = new Vector3(r.CenterX, -0.05f, r.CenterZ);
                // 4 cm bosluk: ayrim cizgisi gorunmeli, yoksa butun kat
                // tek bir zemin gibi okunuyor.
                floor.transform.localScale = new Vector3(r.W - 0.04f, 0.1f, r.D - 0.04f);

                Renderer ren = floor.GetComponent<Renderer>();
                ren.sharedMaterial = _floorMat;

                // Renk KURULUSTA veriliyor, her karede degil: bir zeminin
                // rengi yalnizca masa sayisi degisince degisebilir ve o da
                // altmis gunde en fazla uc kez oluyor. Property block
                // yazmak ayrica cizicileri SRP toplu ciziminin disina
                // atiyor, yani bedava degil.
                ren.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, RoomColor(in r));
                ren.SetPropertyBlock(_block);

                // Dokunma hedefi ODA (docs/31 olcumu). Carpisan kutu
                // zeminin ustune uzaniyor ki isin masaya degil odaya dussun.
                BoxCollider box = floor.GetComponent<BoxCollider>();
                box.size = new Vector3(1f, 14f, 1f);
                floor.AddComponent<RoomTouch>().RoomIndex = i;
            }
        }

        /// <summary>
        /// ODALARI AYIRAN SAYDAM DUVARLAR VE KAPILAR.
        ///
        /// Neden gerekli: kat plani tek bir zemin levhasi gibi
        /// okunuyordu; odalarin sinirini yalnizca 4 cm'lik bir bosluk ve
        /// renk farki soyluyordu.
        ///
        /// Neden SAYDAM: opak bir duvar 34 derecelik bakista arka
        /// odalari tamamen gizlerdi ve oyunun butun bilgisi orada.
        ///
        /// DUVARLAR HAT HAT, KAPILAR CIFT CIFT.
        ///
        /// Ilk yazim her odanin dort kenarini ayri ciziyordu ve ortak
        /// hatlar iki kez cizilip alfa ust uste biniyordu. Ikinci yazim
        /// hat basina TEK kapi koyuyordu ve bu, kat planinin mantigini
        /// bozuyordu: x = 5,2 hattinda uc ayri komsuluk var (Giris-
        /// Bulasik, Mutfak-Bulasik, Mutfak-Depo) ve tek kapi yalnizca
        /// birine yariyordu.
        ///
        /// Artik kapilar ODA CIFTLERINDEN geliyor. Hangi cift birbirine
        /// acilir, bunu Connect() soyluyor - ve orada kullanicinin
        /// istedigi kural yaziyor: DEPOYA YALNIZCA MUTFAKTAN girilir.
        /// </summary>
        private void BuildWalls(int tables)
        {
            if (_wallMat == null) return;

            Dictionary<int, List<Vector2>> dikey = new Dictionary<int, List<Vector2>>();
            Dictionary<int, List<Vector2>> yatay = new Dictionary<int, List<Vector2>>();
            Dictionary<int, List<Gap>> dikeyKapi = new Dictionary<int, List<Gap>>();
            Dictionary<int, List<Gap>> yatayKapi = new Dictionary<int, List<Gap>>();
            _gaps = 0;

            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                Add(dikey, r.X0, r.Z0, r.Z0 + r.D);
                Add(dikey, r.X0 + r.W, r.Z0, r.Z0 + r.D);
                Add(yatay, r.Z0, r.X0, r.X0 + r.W);
                Add(yatay, r.Z0 + r.D, r.X0, r.X0 + r.W);
            }

            // --- kapilar: komsu ve BIRBIRINE ACILAN her cift icin bir tane
            _links.Clear();
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room A = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in A, tables)) continue;

                for (int j = i + 1; j < RoomPlan.Rooms.Length; j++)
                {
                    RoomPlan.Room B = RoomPlan.Rooms[j];
                    if (!RoomPlan.RoomOpen(in B, tables)) continue;
                    if (!Connect(A.Name, B.Name)) continue;

                    float coord, yer;
                    bool d;
                    if (!Shared(in A, in B, out coord, out yer, out d)) continue;

                    AddDoor(d ? dikeyKapi : yatayKapi, coord, yer,
                            Paneled(A.Name, B.Name));
                    _links.Add(new Link { A = i, B = j });
                }
            }

            // ANA KAPI: on cephede, giris odasinin ortasinda. Kanadi
            // HER ZAMAN var - sokakla salon arasindaki esik bu.
            AddDoor(yatayKapi, 0f, Paths.DoorX, true);

            foreach (KeyValuePair<int, List<Vector2>> h in dikey)
                Line(h.Key * 0.01f, Merge(h.Value), true, Doors(dikeyKapi, h.Key));
            foreach (KeyValuePair<int, List<Vector2>> h in yatay)
                Line(h.Key * 0.01f, Merge(h.Value), false, Doors(yatayKapi, h.Key));
        }

        /// <summary>
        /// Iki oda birbirine aciliyor mu.
        ///
        /// DEPO YALNIZCA MUTFAKTAN. Kullanicinin kurali; gercek bir
        /// lokantada da kiler mutfagin arkasindadir, salondan ya da
        /// bulasikhaneden dogrudan girilmez. Bulasikhane mutfaga bagli -
        /// zaten bitisik.
        /// </summary>
        private static bool Connect(string a, string b)
        {
            if (a == "Depo" || b == "Depo")
            {
                string other = a == "Depo" ? b : a;
                return other == "Mutfak";
            }
            return true;
        }

        /// <summary>
        /// Bu gecidin KANADI var mi - yoksa yalnizca bosluk.
        ///
        /// Kullanicinin karari: "giris ve mutfak kapisi disindaki diger
        /// kapilari kaldirabiliriz, direk gecebilecekleri bosluk olsun
        /// odalar arasi, kapi acilip kapanmasin".
        ///
        /// Dogru bir karar ve sebebi de var: gercek bir lokantada salon
        /// ile salon arasinda kapi olmaz, acik bir gecis olur. Kanat
        /// yalnizca bir ESIGI isaretler - sokaktan salona, salondan
        /// mutfaga. Ustelik sekiz saydam kanadin 34 derecelik bir
        /// bakista surekli acilip kapanmasi hareketin kendisini
        /// gurultuye cevirmisti: goz her karede sahnenin en cok
        /// kipirdayan yerine gidiyor ve orasi oyunun bilgisi degil.
        ///
        /// MUTFAK KAPISI = Mutfak-Giris. Depo-Mutfak gecidi de kanat
        /// alabilirdi ama kullanici "giris ve mutfak" dedi; ustelik depo
        /// kapisi salonun hicbir yerinden gorunmuyor.
        /// </summary>
        private static bool Paneled(string a, string b)
        {
            return (a == "Mutfak" && b == "Giris")
                || (a == "Giris" && b == "Mutfak");
        }

        /// <summary>Bir duvar bosugu: yeri ve kanadi olup olmadigi.</summary>
        private struct Gap
        {
            public float Yer;
            public bool Kanat;
        }

        /// <summary>
        /// Iki odanin ortak kenari. coord: hattin koordinati,
        /// yer: kapinin o hat uzerindeki yeri, dikey: hat x sabit mi.
        ///
        /// Kapi ortak kenarin ORTASINA gidiyor - ama ortak kenar on
        /// koridoru (Paths.LaneZ) iceriyorsa oraya: gecis zaten oradan
        /// oluyor ve kapi baska yere konsa figurler duvardan gecerdi.
        /// </summary>
        private static bool Shared(in RoomPlan.Room A, in RoomPlan.Room B,
                                   out float coord, out float yer, out bool dikey)
        {
            coord = 0f; yer = 0f; dikey = true;

            // Dikey komsuluk: birinin sag kenari otekinin sol kenari.
            float ax1 = A.X0 + A.W, bx1 = B.X0 + B.W;
            if (Mathf.Abs(ax1 - B.X0) < 0.01f || Mathf.Abs(bx1 - A.X0) < 0.01f)
            {
                coord = Mathf.Abs(ax1 - B.X0) < 0.01f ? ax1 : bx1;
                float z0 = Mathf.Max(A.Z0, B.Z0);
                float z1 = Mathf.Min(A.Z0 + A.D, B.Z0 + B.D);
                if (z1 - z0 < DoorWidth + MinJamb * 2f) return false;
                yer = (Paths.LaneZ > z0 && Paths.LaneZ < z1)
                    ? Paths.LaneZ : (z0 + z1) * 0.5f;
                dikey = true;
                return true;
            }

            // Yatay komsuluk: birinin ust kenari otekinin alt kenari.
            float az1 = A.Z0 + A.D, bz1 = B.Z0 + B.D;
            if (Mathf.Abs(az1 - B.Z0) < 0.01f || Mathf.Abs(bz1 - A.Z0) < 0.01f)
            {
                coord = Mathf.Abs(az1 - B.Z0) < 0.01f ? az1 : bz1;
                float x0 = Mathf.Max(A.X0, B.X0);
                float x1 = Mathf.Min(A.X0 + A.W, B.X0 + B.W);
                if (x1 - x0 < DoorWidth + MinJamb * 2f) return false;
                yer = (x0 + x1) * 0.5f;
                dikey = false;
                return true;
            }
            return false;
        }

        private static void AddDoor(Dictionary<int, List<Gap>> hat, float coord,
                                    float yer, bool kanat)
        {
            int k = Mathf.RoundToInt(coord * 100f);
            List<Gap> l;
            if (!hat.TryGetValue(k, out l)) { l = new List<Gap>(); hat[k] = l; }
            l.Add(new Gap { Yer = yer, Kanat = kanat });
        }

        private static List<Gap> Doors(Dictionary<int, List<Gap>> hat, int key)
        {
            List<Gap> l;
            if (!hat.TryGetValue(key, out l)) return _empty;
            l.Sort((p, q) => p.Yer.CompareTo(q.Yer));
            return l;
        }

        private static readonly List<Gap> _empty = new List<Gap>();

        private static void Add(Dictionary<int, List<Vector2>> hat,
                                float coord, float a, float b)
        {
            int k = Mathf.RoundToInt(coord * 100f);
            List<Vector2> l;
            if (!hat.TryGetValue(k, out l)) { l = new List<Vector2>(); hat[k] = l; }
            l.Add(new Vector2(a, b));
        }

        /// <summary>Ust uste binen araliklari birlestirir.</summary>
        private static List<Vector2> Merge(List<Vector2> araliklar)
        {
            araliklar.Sort((p, q) => p.x.CompareTo(q.x));
            List<Vector2> sonuc = new List<Vector2>();
            foreach (Vector2 v in araliklar)
            {
                if (sonuc.Count > 0 && v.x <= sonuc[sonuc.Count - 1].y + 0.01f)
                {
                    Vector2 son = sonuc[sonuc.Count - 1];
                    son.y = Mathf.Max(son.y, v.y);
                    sonuc[sonuc.Count - 1] = son;
                }
                else sonuc.Add(v);
            }
            return sonuc;
        }

        /// <summary>
        /// Bir hattin duvarlarini kurar; verilen yerlerde kapi bosugu
        /// birakir. Bosluk parcanin disina tasabilir - kose hizasindaki
        /// bir aciklik dogrudur, insanlar oradan geciyor.
        /// </summary>
        private void Line(float coord, List<Vector2> parcalar, bool dikey,
                          List<Gap> kapilar)
        {
            foreach (Vector2 p in parcalar)
            {
                float imlec = p.x;
                for (int k = 0; k < kapilar.Count; k++)
                {
                    float g0 = kapilar[k].Yer - DoorWidth * 0.5f;
                    float g1 = kapilar[k].Yer + DoorWidth * 0.5f;
                    if (g1 <= p.x + 0.05f || g0 >= p.y - 0.05f) continue;

                    Slab(coord, imlec, Mathf.Min(g0, p.y), dikey);
                    imlec = Mathf.Max(imlec, Mathf.Min(g1, p.y));
                    _gaps++;

                    // BOSLUK HER GECITTE, KANAT YALNIZCA IKISINDE.
                    //
                    // Bosluk kesilmeye devam ediyor - Connect() hangi
                    // odanin hangisine acildigini soyluyor ve o kural
                    // bozulmadi (depoya yalnizca mutfaktan). Degisen
                    // tek sey o bosluga bir KANAT konup konmadigi.
                    if (!kapilar[k].Kanat) continue;

                    Vector3 yer = dikey ? new Vector3(coord, 0f, kapilar[k].Yer)
                                        : new Vector3(kapilar[k].Yer, 0f, coord);
                    Door d = Door.Create(transform, yer, dikey ? 0f : 90f,
                                         DoorWidth, WallHeight - 0.08f, _doorMat);
                    if (d != null) _doors.Add(d);
                }
                Slab(coord, imlec, p.y, dikey);
            }
        }

        private void Slab(float coord, float a, float b, bool dikey)
        {
            float uzunluk = b - a;
            if (uzunluk < 0.05f) return;

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Duvar";
            wall.transform.SetParent(transform, false);

            float orta = (a + b) * 0.5f;
            wall.transform.localPosition = dikey
                ? new Vector3(coord, WallHeight * 0.5f, orta)
                : new Vector3(orta, WallHeight * 0.5f, coord);
            wall.transform.localScale = dikey
                ? new Vector3(WallThick, WallHeight, uzunluk)
                : new Vector3(uzunluk, WallHeight, WallThick);

            // Carpisani kaldir: editorde Destroy ERTELENIYOR ve kutu
            // sahnede kaliyor. Dokunma hedefi oda zemini olmali.
            Collider col = wall.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer ren = wall.GetComponent<Renderer>();
            ren.sharedMaterial = _wallMat;

            // SAYDAM DUVAR GOLGE DUSURMUYOR.
            //
            // Kullanicinin bildirdigi sey: "odalardaki golgeler baska
            // odalara kayiyor". Sebeplerin en buyugu buydu - duvarlar
            // CAM (alfa 0,10) ama golge haritasinda KATI: 1,15 m'lik
            // bir levha, gunes 10 derecedeyken 6,5 m uzunlugunda koyu
            // bir bant birakiyor ve o bant komsu odanin yarisini
            // kapliyor.
            //
            // Yanlisligi iki katli: cam bir bolme zaten golge dusurmez,
            // ve dusurdugu golge oyuncunun BAKMASI gereken yere
            // dusuyordu.
            //
            // (Saydam bir duvarin golgesi OPAK dusuyor: URP golge gecisi
            // alfayi okumuyor.)
            ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ren.receiveShadows = false;
        }

        private struct Link { public int A, B; }

        private readonly List<Link> _links = new List<Link>();

        /// <summary>
        /// ODALARA ERISIM DENETIMI.
        ///
        /// Kullanicinin istegi: "odalari niteliklerine gore kontrol et -
        /// depo yalnizca mutfaktan erisilebilir olmali". Bir kural, ancak
        /// SINANABILIRSE kuraldir: burasi kapi grafigini gezip
        ///   1. her acik odanin giristen ulasilabilir oldugunu,
        ///   2. depoya YALNIZCA mutfaktan girildigini
        /// dogruluyor. Ikisi de sessizce bozulabilir - kat plani
        /// degistiginde kimse fark etmez.
        /// </summary>
        public bool AccessOk(out string report)
        {
            int n = RoomPlan.Rooms.Length;
            bool[] acik = new bool[n];
            for (int i = 0; i < n; i++)
                acik[i] = RoomPlan.RoomOpen(in RoomPlan.Rooms[i], _builtTables);

            // 1. Giristen her odaya ulasiliyor mu.
            int giris = -1;
            for (int i = 0; i < n; i++)
                if (RoomPlan.Rooms[i].Name == "Giris") giris = i;

            bool[] bulundu = new bool[n];
            if (giris >= 0) Flood(giris, bulundu, -1);

            var sb = new System.Text.StringBuilder();
            bool ok = true;
            for (int i = 0; i < n; i++)
            {
                if (!acik[i] || bulundu[i]) continue;
                ok = false;
                sb.Append("ulasilamiyor:" + RoomPlan.Rooms[i].Name + " ");
            }

            // 2. Depoya mutfak kapaliyken ulasilmamali.
            int depo = -1, mutfak = -1;
            for (int i = 0; i < n; i++)
            {
                if (RoomPlan.Rooms[i].Name == "Depo") depo = i;
                if (RoomPlan.Rooms[i].Name == "Mutfak") mutfak = i;
            }
            if (depo >= 0 && mutfak >= 0 && acik[depo])
            {
                bool[] mutfaksiz = new bool[n];
                if (giris >= 0) Flood(giris, mutfaksiz, mutfak);
                if (mutfaksiz[depo])
                {
                    ok = false;
                    sb.Append("depoya mutfaksiz giriliyor ");
                }
            }

            report = sb.Length == 0 ? "erisim kurallari tamam" : sb.ToString();
            return ok;
        }

        private void Flood(int from, bool[] seen, int blocked)
        {
            if (from < 0 || from == blocked || seen[from]) return;
            seen[from] = true;
            for (int i = 0; i < _links.Count; i++)
            {
                if (_links[i].A == from) Flood(_links[i].B, seen, blocked);
                else if (_links[i].B == from) Flood(_links[i].A, seen, blocked);
            }
        }

        /// <summary>
        /// KAPILAR YAKLASANA ACILIYOR.
        ///
        /// Her karede her kapi x her figur: on kapi, yedi ic kapi ve en
        /// fazla kirk figur - kare basina birkac yuz mesafe karsilastirmasi,
        /// olculemeyecek kadar ucuz. Yol bulma ile karistirmamak icin:
        /// kapi figuru DURDURMUYOR, yalnizca aciliyor.
        /// </summary>
        private void UpdateDoors()
        {
            if (_doors.Count == 0) return;

            _movers.Clear();
            for (int i = 0; i < _staff.Count; i++)
                if (_staff[i] != null && _staff[i].activeSelf)
                    _movers.Add(_staff[i].transform.localPosition);
            foreach (KeyValuePair<int, GameObject> kv in _seated)
                if (kv.Value != null && kv.Value.activeSelf)
                    _movers.Add(kv.Value.transform.localPosition);
            foreach (KeyValuePair<int, GameObject> kv in _queued)
                if (kv.Value != null && kv.Value.activeSelf)
                    _movers.Add(kv.Value.transform.localPosition);
            for (int i = 0; i < _leaving.Count; i++)
                if (_leaving[i] != null && _leaving[i].activeSelf)
                    _movers.Add(_leaving[i].transform.localPosition);

            float r2 = Door.Sense * Door.Sense;
            for (int d = 0; d < _doors.Count; d++)
            {
                if (_doors[d] == null) continue;
                Vector3 k = _doors[d].Spot;
                bool yakin = false;
                for (int m = 0; m < _movers.Count; m++)
                {
                    Vector3 fark = _movers[m] - k;
                    fark.y = 0f;
                    if (fark.sqrMagnitude <= r2) { yakin = true; break; }
                }
                _doors[d].SetOpen(yakin);
            }
        }

        /// <summary>Acik kapi sayisi. Turun sorabilmesi icin.</summary>
        public int OpenDoorCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _doors.Count; i++)
                    if (_doors[i] != null && _doors[i].IsOpen) n++;
                return n;
            }
        }

        /// <summary>
        /// Gorevi olan asci sayisi. Turun "mutfakta is yok" ile
        /// "asci gorev almiyor" arasini ayirabilmesi icin.
        /// </summary>
        public int BusyCooks
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _staffTask.Count && i < _staffCooks; i++)
                    if (_staffTask[i] >= 0) n++;
                return n;
            }
        }

        /// <summary>Ascilarin su anki duruşlari. Tanı icin.</summary>
        public string CookPoses
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < _staff.Count && i < _staffCooks; i++)
                {
                    Figure f = FigureOf(_staff[i]);
                    Walker w = WalkerOf(_staff[i]);
                    sb.Append(i + ":" + (f == null ? "yok" : f.Current.ToString())
                              + "/g" + (i < _staffTask.Count ? _staffTask[i] : -9)
                              + (w != null && w.Moving ? "/yolda" : "") + " ");
                }
                return sb.ToString();
            }
        }

        /// <summary>Kanatli kapi sayisi. Turun sorabilmesi icin.</summary>
        public int DoorCount { get { return _doors.Count; } }

        /// <summary>
        /// Duvarlarda acilan GECIS bosugu sayisi (kanatli ve kanatsiz).
        ///
        /// Kanatlar kaldirilinca "kapi sayisi" artik gecisleri
        /// olcmuyordu: kanat sayisi ikiye dustu ama odalarin birbirine
        /// acilmasi degismemeliydi. Ikisi ayri sayi olmali, yoksa
        /// kanatlari kaldiran bir degisiklik bir odayi da sessizce
        /// duvarla kapatabilir ve kimse fark etmez.
        /// </summary>
        public int GapCount { get { return _gaps; } }

        /// <summary>Birbirine acilan oda cifti sayisi.</summary>
        public int LinkCount { get { return _links.Count; } }

        private int _gaps;

        /// <summary>
        /// Arsanin DISINDA duran figur sayisi (sokakta).
        ///
        /// Turun sorabilmesi icin: "musteri sokaktan yuruyerek geliyor"
        /// ancak bir figur gercekten disarida goruldugunde dogrulanir.
        /// Yolun icinde bir sokak noktasi olmasi yetmez - Warp yanlis
        /// yere koyarsa yol yine de dogru gorunurdu.
        /// </summary>
        public int OutsideCount
        {
            get
            {
                int n = 0;
                foreach (KeyValuePair<int, GameObject> kv in _seated)
                    if (kv.Value != null && kv.Value.activeSelf
                        && kv.Value.transform.localPosition.z < -0.2f) n++;
                for (int i = 0; i < _leaving.Count; i++)
                    if (_leaving[i] != null && _leaving[i].activeSelf
                        && _leaving[i].transform.localPosition.z < -0.2f) n++;
                return n;
            }
        }

        /// <summary>
        /// SOKAKTA duran figurlerin yerel konumlari.
        ///
        /// StreetLife yayalari bunlardan uzak tutuyor: kapinin onunde
        /// bekleyen bir musterinin icinden gecen yaya, kullanicinin
        /// bildirdigi hatanin ta kendisi. Liste cagiran tarafindan
        /// geliyor - her karede yeni bir liste ayirmak, kirk figurde
        /// gorunur bir cop uretir.
        /// </summary>
        public void OutsideFigures(List<Vector3> into)
        {
            if (into == null) return;
            foreach (KeyValuePair<int, GameObject> kv in _seated)
                if (kv.Value != null && kv.Value.activeSelf
                    && kv.Value.transform.localPosition.z < -0.02f)
                    into.Add(kv.Value.transform.localPosition);
            foreach (KeyValuePair<int, GameObject> kv in _queued)
                if (kv.Value != null && kv.Value.activeSelf
                    && kv.Value.transform.localPosition.z < -0.02f)
                    into.Add(kv.Value.transform.localPosition);
            for (int i = 0; i < _leaving.Count; i++)
                if (_leaving[i] != null && _leaving[i].activeSelf
                    && _leaving[i].transform.localPosition.z < -0.02f)
                    into.Add(_leaving[i].transform.localPosition);
        }

        /// <summary>
        /// Sokakta ic ice gecmis yaya ciftleri. Turun sorabilmesi icin.
        /// </summary>
        public int StreetOverlaps
        {
            get { return _streetLife == null ? 0 : _streetLife.Overlaps; }
        }

        /// <summary>
        /// Sokak lambasi diregine girmis yaya sayisi.
        ///
        /// Gorunum ya da sokak yoksa 1 donuyor, 0 degil: "olcecek bir
        /// sey yok" ile "her sey yolunda" ayni sayiyi vermemeli.
        /// </summary>
        public int PostOverlaps
        {
            get { return _streetLife == null ? 1 : _streetLife.PostOverlaps; }
        }

        /// <summary>Sokak itismesinin bildigi direk sayisi.</summary>
        public int StreetPostsKnown
        {
            get { return _streetLife == null ? 0 : _streetLife.PostsKnown; }
        }

        private readonly List<Door> _doors = new List<Door>();
        private readonly List<Vector3> _movers = new List<Vector3>();

        /// <summary>Kapi bosugunun genisligi (m). Figur eni 0,85.</summary>
        private const float DoorWidth = 1.10f;

        /// <summary>
        /// Gecidin iki yanindaki EN AZ duvar payi (m).
        ///
        /// Once `DoorWidth + 0.3f` diye tek bir sayiydi; ayni sayi artik
        /// adiyla duruyor. DAVRANIS DEGISMEDI - refactor, duzeltme degil.
        ///
        /// Mutfak ile Bulasik'in ortak kenari TAM 1,40 m ve esik de tam
        /// 1,40: bu gecidin elenip elenmedigi kayan noktanin son
        /// basamagina bakiyor. OLCULDU - gecit VAR (bag sayisi 5; olmasa
        /// 4 olurdu), cunku 5,4f - 4,0f = 1,4000001 ve esik 1,4000000.
        ///
        /// Yani bu kenar guvenli bir paya DEGIL, tesaduften bir
        /// basamaga dayaniyor. Kat plani degisirse once buraya bakilmali:
        /// mutfakla bulasikhanenin komsulugu, oyunun en cok kullanilan
        /// gecidi (docs/36 "bulasik yikanan yer mutfaga bagli").
        ///
        /// Kose hizasindaki bir acikligin kendisi dogru (bkz. Line):
        /// bosluk kirpiliyor, elenmiyor.
        /// </summary>
        private const float MinJamb = 0.15f;

        /// <summary>
        /// RESTORANIN ONUNDEKI SOKAK.
        ///
        /// Neden gerekli: musteriler cercevenin alt kenarinda beliriyordu
        /// ve orasi hicbir sey degildi - ne kaldirim ne yol, yalnizca
        /// bosluk. "Disaridan geldi" duygusunu veren sey gelinen yerin
        /// var olmasi.
        ///
        /// Neden DAR: kamera cercevesi derinlige bagli (docs/31) ve
        /// onden eklenen her metre restorani ekranda kuculttuyor -
        /// dokunma hedefi olcumu zaten Google'in 48 dp asgarisine yakin.
        /// 1,6 m sokak, cercevede 1,1 m'lik bir genisleme demek; kaldirim
        /// ve asfaltin bir seridi goruunuyor, o kadari da yetiyor.
        ///
        /// Uc levha: kaldirim, bordur cizgisi, asfalt. Carpisan yok.
        /// </summary>
        private void BuildStreet(int tables)
        {
            _lampHeads.Clear();
            _lampGlow.Clear();
            _lampX.Clear();
            _streetSlabs.Clear();
            _streetBase.Clear();
            BuildStamp++;
            // KALDIRIM YAYALARIN SIGACAGI KADAR GENIS.
            //
            // Onceki paylar kaldirimi 0,60 m yapiyordu ve yurume
            // cizgisi (Paths.PavementZ) -1,05'teydi: yani herkes
            // ASFALTTA yuruyordu, bordurun otesinde. Simdi kaldirim
            // 1,10 m ve iki yaya seridi de onun icinde.
            //
            // PAYLAR CERCEVEYE GORE: kamera sokagin yalnizca
            // CameraFit.StreetInFrame kadarini goruyor (1,70 m) ve
            // geri kalani cizilse de goruunmuyor. Ilk yazimda kaldirim
            // 1,12 m'ye genisletildi ama cerceve 1,10 m'deydi: bordur
            // ve asfalt tamamen cercevenin disina dustu, yani "sokak"
            // asfaltsiz bir kaldirim seridine dondu.
            //
            // Kaldirim 1,40 m: iki yaya seridi (0,70 arayla) arti
            // govdelerin yarilari.
            Street("Kaldirim", -1.42f, -0.02f, new Color(0.62f, 0.60f, 0.57f));
            // Bordur: ince, acik - kaldirim ile yolu ayiran cizgi.
            Street("Bordur", -1.54f, -1.42f, new Color(0.78f, 0.76f, 0.72f));
            // Asfalt. 0,42 m'si cerceveye giriyor - "burasi bir yol"
            // demeye yeten en az miktar.
            Street("Asfalt", -2.20f, -1.54f, new Color(0.26f, 0.26f, 0.28f));

            // SOKAK LAMBALARI: BASTA, ORTADA VE SONDA.
            //
            // Once dort direk arsayi dorde boluyordu ve biri kapinin
            // onune dustugu icin 2,2 m kaydiriliyordu; araliklar 2,3 /
            // 4,5 / 4,5 m oluyordu. Bir direk dizisinin okunur tek
            // ozelligi ESIT ARALIK - kaydirilmis bir direk "sokak"
            // degil "dagilmis birkac direk" diye okunuyor.
            //
            // ARSAYA DEGIL ACIK ODALARA GORE. Ilk duzeltme uc diregi
            // arsanin uzerine esit koydu (0 / 9 / 18 m) ve kagit
            // uzerinde simetrikti - ama kamera ARSAYI degil ACIK ODALARI
            // cerceveliyor (CameraFit.OpenBounds). Acilis kademesinde
            // oyuncu 0 ile 13,4 m arasini goruyor, yani 18'deki direk
            // ekranda YOK: gorunen sey iki direk ve sola yatik bir
            // dizilis - duzeltilmek istenen sikayetin ta kendisi.
            //
            // Simdi direkler her kademede gorunen seridin basinda,
            // ortasinda ve sonunda. Restoran buyudukce sokak da
            // uzuyor - binayla birlikte buyuyen bir sokak zaten oyunun
            // kurgusu.
            // ACIK odalarin sag kenari - CameraFit.OpenBounds ile ayni
            // hesap. Kapali odalarin sol kenarina bakmak yanlis olurdu:
            // o, kademelerin hep sagdan aciliyor olmasina bel baglar.
            float son = 0f;
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tables)) continue;
                if (r.X0 + r.W > son) son = r.X0 + r.W;
            }
            if (son < 1f) son = RoomPlan.PlotW;
            for (int i = 0; i < 3; i++)
                StreetLamp(son * i * 0.5f);
        }

        /// <summary>
        /// RESTORANIN ICINI AYDINLATAN TAVAN LAMBALARI.
        ///
        /// Kullanicinin cumlesi: "aksam olunca restoranin ici karanlik
        /// oluyor, restoranin icini de isiklandiralim fakat sokaktakinden
        /// farkli olarak lambalar fiziksel olarak gozukmesin, tavanda
        /// olacaklari icin".
        ///
        /// Dogru istek ve dogru gerekce: kamera tavani olmayan bir
        /// binaya yukaridan bakiyor. Bir tavan armaturu cizilse kendi
        /// aydinlattigi yeri kapatirdi - ve zaten orada bir tavan yok,
        /// yani armatur havada asili durur.
        ///
        /// O YUZDEN YALNIZCA ISIK HAVUZU: sokak lambalarindakiyle ayni
        /// teknik (isiksiz + TOPLAYICI harmanlanan levha), cunku URP
        /// varliginda ek isiklar KAPALI (m_AdditionalLightsRenderingMode:
        /// 0, docs/19 mobil butcesi) ve sahneye konan bir spot HICBIR SEY
        /// yapmaz - uyarisiz.
        ///
        /// Odanin olcusune gore izgara: tek buyuk bir havuz, dikdortgen
        /// bir odada ortasi parlak kenari karanlik bir leke veriyor -
        /// "tavan aydinlatmasi" degil "yerde bir fener" diye okunuyor.
        ///
        /// Renk sokaktakinden FARKLI: sokak lambasi sodyum sarisi
        /// (1,00 / 0,80 / 0,45), ici sicak beyaz. Ikisi ayni renk olsa
        /// "icerisi" ile "disarisi" ayni yerin devami gibi okunurdu;
        /// ayri olunca bina kendi isigiyla duruyor.
        /// </summary>
        private void BuildRoomLights(int tables)
        {
            _roomGlow.Clear();
            if (_ceilMat == null) return;

            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                int en = Mathf.Max(1, Mathf.RoundToInt(r.W / RoomLampSpan));
                int derin = Mathf.Max(1, Mathf.RoundToInt(r.D / RoomLampSpan));
                float dx = r.W / en, dz = r.D / derin;
                // Havuzlar BIRBIRINE TASIYOR (1,55 kat): tam hucre
                // olcusunde levhalar arasinda karanlik kavsaklar
                // kaliyordu ve izgaranin kendisi goruunuyordu.
                float cap = Mathf.Min(dx, dz) * 1.55f;

                for (int cc = 0; cc < en; cc++)
                    for (int rr = 0; rr < derin; rr++)
                        RoomLamp(r.X0 + dx * (cc + 0.5f),
                                 r.Z0 + dz * (rr + 0.5f), cap);
            }
        }

        /// <summary>Tek bir tavan lambasinin yerdeki isigi.</summary>
        private void RoomLamp(float x, float z, float cap)
        {
            GameObject havuz = GameObject.CreatePrimitive(PrimitiveType.Quad);
            havuz.name = "TavanIsigi";
            havuz.transform.SetParent(transform, false);
            // 0,014 - sokak havuzunun (0,012) hemen ustunde. Ikisi ayni
            // yukseklikte olsa kapinin onunde z-kavgasi yapip
            // titresirlerdi.
            havuz.transform.localPosition = new Vector3(x, 0.014f, z);
            havuz.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            havuz.transform.localScale = new Vector3(cap, cap, 1f);

            Collider col = havuz.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer ren = havuz.GetComponent<Renderer>();
            // KENDI MALZEMESI: rengi varligin icinde, property block yok.
            // Sicak beyaz ve sokak havuzundan sonuk - sekiz odada onlarca
            // havuzun toplami zemini beyaza doyuruyordu.
            ren.sharedMaterial = _ceilMat;
            ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ren.receiveShadows = false;
            havuz.SetActive(false);
            _roomGlow.Add(havuz);
        }

        /// <summary>Iki tavan lambasi arasi hedef aralik (m).</summary>
        private const float RoomLampSpan = 2.30f;

        /// <summary>
        /// Bir sokak lambasi: sekizgen kaideli, fenerli klasik direk.
        ///
        /// KUTUDAN ORGUYE. Onceki lamba UC KUTUYDU (direk, kol, bas) ve
        /// telefon olcusunde "lamba" degil "ince bir cubugun ucundaki
        /// beyaz nokta" diye okunuyordu. Kullanicinin getirdigi ornek
        /// klasik sokak feneriydi: sekizgen kaide, inceleyerek yukselen
        /// govde, kivrilan kol ve sekizgen camli fener.
        ///
        /// Butun parcalar TEK ORGUDE birlesiyor: metal bir orgu, cam bir
        /// orgu. Onbes ayri kutu onbes cizim cagrisi demekti; birlestirme
        /// lamba basina ikiye indiriyor ve uc lamba ayni iki orguyu
        /// PAYLASIYOR (Mesh tek kez kuruluyor).
        ///
        /// Golge YOK: direk ince ve golgesi kat planinin uzerine
        /// dusuyor; oyunun okunmasina hicbir sey katmadan bir golge
        /// haritasi daha istiyor.
        /// </summary>
        private void StreetLamp(float x)
        {
            GameObject kok = new GameObject("SokakLambasi");
            kok.transform.SetParent(transform, false);
            _lampX.Add(x);
            // Direk BORDURUN uzerinde. -1,30 asfaltin icindeydi: yolun
            // ortasinda duran bir direk. Gercek sokak lambasi bordura
            // oturur.
            kok.transform.localPosition = new Vector3(x, 0f, LampPostZ);

            if (_lampMetalMesh == null) BuildLampMeshes();

            // --- metal govde ---
            GameObject metal = new GameObject("Govde");
            metal.transform.SetParent(kok.transform, false);
            MeshFilter mf = metal.AddComponent<MeshFilter>();
            mf.sharedMesh = _lampMetalMesh;
            MeshRenderer mr = metal.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _floorMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (_block == null) _block = new MaterialPropertyBlock();
            mr.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, LampIron);
            mr.SetPropertyBlock(_block);

            // --- cam fener ---
            //
            // Emisyonu olan malzeme SART: anahtar kapaliyken
            // gölgelendirici emisyon alanini hic okumuyor, yani aksam
            // "lambayi yak" diye yazilan renk hicbir sey yapmiyor.
            if (_lampMat == null && _floorMat != null)
            {
                _lampMat = new Material(_floorMat);
                _lampMat.EnableKeyword("_EMISSION");
                _lampMat.globalIlluminationFlags =
                    MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            GameObject cam = new GameObject("Cam");
            cam.transform.SetParent(kok.transform, false);
            MeshFilter cf = cam.AddComponent<MeshFilter>();
            cf.sharedMesh = _lampGlassMesh;
            MeshRenderer cr = cam.AddComponent<MeshRenderer>();
            cr.sharedMaterial = _lampMat != null ? _lampMat : _floorMat;
            cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lampHeads.Add(cr);

            // ISIK HAVUZU: GERCEK ISIK DEGIL.
            //
            // Once buraya bir nokta isigi konmustu ve HICBIR SEY
            // yapmiyordu: URP varliginda ek isiklar KAPALI
            // (m_AdditionalLightsRenderingMode: 0, docs/19 mobil
            // butcesi). Sahnede duran ama cizime hic girmeyen bir isik,
            // "lamba yaniyor" diye bakip hicbir sey gormemek demek -
            // ve hicbir sey uyarmaz.
            //
            // Yerine UC KATMAN, hepsi isiksiz ve toplayici harmanlanan:
            //
            //   1. KONI  - fenerden yere inen isik huzmesi. Geceyi
            //      "karanlik bir sokak + beyaz noktalar"dan "isik veren
            //      lambalar"a cevirecek tek parca bu: isigin KAYNAKTAN
            //      CIKTIGI ancak huzmeyle goruunuyor.
            //   2. HALE  - fenerin cevresindeki parlama. Camin kendisi
            //      kucuk; hale onu telefon olcusunde gorunur yapiyor.
            //   3. HAVUZ - kaldirimda yatan aydinlik leke.
            //
            // Ucu de aksam AYNI anda aciliyor (DayLight.Lamps).
            LampBeam(kok.transform);
            LampHalo(kok.transform);
            LampPool(kok.transform);
        }

        /// <summary>Dokme demir: referanstaki gibi neredeyse siyah.</summary>
        private static readonly Color LampIron = new Color(0.13f, 0.13f, 0.15f);

        /// <summary>
        /// Fenerin ekseni: DIREGIN USTU, yani kaydirma yok.
        ///
        /// Kivrik kol kaldirildi (kullanicinin istegi); sayi duruyor
        /// cunku isik parcalarinin hepsi ondan okunuyor - ileride fener
        /// yine kaydirilmak istenirse tek yerden kayiyor.
        /// </summary>
        private const float LampHeadZ = 0f;

        /// <summary>Cam fenerin orta yuksekligi.</summary>
        private const float LampGlassY = 1.875f;

        private Mesh _lampMetalMesh;
        private Mesh _lampGlassMesh;

        /// <summary>
        /// Fenerden yere inen isik huzmesi.
        ///
        /// Dokusu YOK denecek kadar basit bir numara: isik havuzunun
        /// yuvarlak dokusunun ORTA SATIRI okunuyor - u=0,5 merkez
        /// (parlak), u=1 kenar (saydam). Konide u yukaridan asagi
        /// buyuyor, yani huzme fenerde parlak, yerde sonuyor.
        ///
        /// Neden boyle: saydam malzeme YAPIDA ayiklaniyor; ancak bir
        /// .mat VARLIGININ isaret ettigi gölgelendirici varyanti yapiya
        /// giriyor. Yeni bir malzeme yerine var olan havuz malzemesini
        /// kullanmak, o tuzaga hic girmemek demek.
        /// </summary>
        private void LampBeam(Transform kok)
        {
            Mesh m = new Mesh();
            m.name = "LambaHuzmesi";
            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            var uv = new List<Vector2>();
            var ts = new List<int>();

            const int yan = 8;
            const float ust = 1.70f, alt = 0.02f;
            const float rUst = 0.22f, rAlt = 1.45f;
            for (int i = 0; i < yan; i++)
            {
                float a0 = Mathf.PI * 2f * i / yan;
                float a1 = Mathf.PI * 2f * (i + 1) / yan;
                Vector3 u0 = new Vector3(Mathf.Cos(a0) * rUst, ust,
                                         Mathf.Sin(a0) * rUst + LampHeadZ);
                Vector3 u1 = new Vector3(Mathf.Cos(a1) * rUst, ust,
                                         Mathf.Sin(a1) * rUst + LampHeadZ);
                Vector3 a2 = new Vector3(Mathf.Cos(a0) * rAlt, alt,
                                         Mathf.Sin(a0) * rAlt + LampHeadZ);
                Vector3 a3 = new Vector3(Mathf.Cos(a1) * rAlt, alt,
                                         Mathf.Sin(a1) * rAlt + LampHeadZ);
                int b = vs.Count;
                vs.Add(u0); vs.Add(u1); vs.Add(a2); vs.Add(a3);
                Vector3 n = Vector3.Cross(u1 - u0, a2 - u0).normalized;
                ns.Add(n); ns.Add(n); ns.Add(n); ns.Add(n);
                // u: 0,62 fenerde (parlaga yakin), 0,98 yerde (sonuk).
                uv.Add(new Vector2(0.62f, 0.5f)); uv.Add(new Vector2(0.62f, 0.5f));
                uv.Add(new Vector2(0.98f, 0.5f)); uv.Add(new Vector2(0.98f, 0.5f));
                ts.Add(b); ts.Add(b + 2); ts.Add(b + 1);
                ts.Add(b + 1); ts.Add(b + 2); ts.Add(b + 3);
                // ICTEN DE GORUNSUN: koni tek yuzlu olsaydi kamera
                // acisina gore yarisi kaybolurdu.
                ts.Add(b); ts.Add(b + 1); ts.Add(b + 2);
                ts.Add(b + 1); ts.Add(b + 3); ts.Add(b + 2);
            }
            m.SetVertices(vs);
            m.SetNormals(ns);
            m.SetUVs(0, uv);
            m.SetTriangles(ts, 0);
            m.RecalculateBounds();

            GameObject go = new GameObject("Huzme");
            go.transform.SetParent(kok, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _glowMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            // HUZME HAVUZDAN GUCLU.
            //
            // Havuz malzemesinin alfasi 0,55 ve ilk goruntude huzme
            // GORUNMUYORDU - yatan bir levha bir bakista genis, dik
            // duran bir koni ise incecik. Aynı malzeme, farkli guc:
            // property block uc ciziciyi toplu cizimin disina atiyor,
            // uc cizici icin kabul edilebilir bir bedel.
            if (_block == null) _block = new MaterialPropertyBlock();
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, new Color(1.00f, 0.82f, 0.48f, 0.62f));
            r.SetPropertyBlock(_block);
            go.SetActive(false);
            _lampGlow.Add(go);
        }

        /// <summary>
        /// Fenerin cevresindeki parlama.
        ///
        /// KAMERAYI IZLIYOR (FaceCamera). Once sabit aciya kuruluyordu
        /// ve gerekce "oyunun kamerasinin acisi sabit, yalnizca konumu
        /// degisiyor" idi - bu iki parmakla cevirme eklenince gecersiz
        /// kaldi (CameraRig +-35 derece). Uc fener icin kare basina uc
        /// donus, bedelini fazlasiyla hak ediyor.
        /// </summary>
        private void LampHalo(Transform kok)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Hale";
            go.transform.SetParent(kok, false);
            // FENERIN ONUNDE: arkasina konursa fenerin kendi govdesi
            // haleyi kapatiyor ve hale hic gorunmuyor.
            // Kameraya dogru bir tutam kaydiriliyor (kamera sokak
            // tarafinda, yani -Z'de): fenerin govdesi halenin ortasini
            // kapatmasin.
            go.transform.localPosition = new Vector3(0f, LampGlassY, LampHeadZ - 0.12f);
            // QUAD'IN YUZU -Z'YE BAKIYOR.
            //
            // Kameranin acisini dogrudan vermek levhayi TERS ceviriyor
            // ve arka yuz ayiklandigi icin hale hic cizilmiyordu -
            // goruntude "hale yok" diye gorundu, halbuki hale oradaydi
            // ve sirtini donmustu. 180 derecelik duzeltme FaceCamera'nin
            // icinde; havuz levhasi bu tuzaga dusmuyor cunku 90
            // derecelik donus onu yukari bakar hale getiriyor.
            go.transform.localRotation = CameraFit.Rotation
                                         * Quaternion.Euler(0f, 180f, 0f);
            go.AddComponent<FaceCamera>();
            // HDR KAPALI (LokantaURP m_SupportsHDR: 0), yani parlama
            // (bloom) diye bir sey yok: 1'in uzerindeki emisyon
            // yalnizca beyaza kirpiliyor. Fenerin cevresindeki parlama
            // bu levhanin KENDISI - o yuzden kucuk degil, fenerin iki
            // kati genisliginde.
            go.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            Collider c = go.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
            }
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = _glowMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (_block == null) _block = new MaterialPropertyBlock();
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, new Color(1.00f, 0.84f, 0.52f, 0.75f));
            r.SetPropertyBlock(_block);
            go.SetActive(false);
            _lampGlow.Add(go);
        }

        /// <summary>Kaldirimda yatan aydinlik leke.</summary>
        private void LampPool(Transform kok)
        {
            GameObject havuz = GameObject.CreatePrimitive(PrimitiveType.Quad);
            havuz.name = "IsikHavuzu";
            havuz.transform.SetParent(kok, false);
            // Havuz kaldirimin ORTASINA dusuyor (direk -1,52, havuz
            // +0,80 => z = -0,72 = Paths.PavementZ): isik yurunen yeri
            // aydinlatmali, yolu degil.
            // Havuz fenerin ALTINDA (direk ekseni). Direk bordurde ama
            // havuz 2,9 m derin: kaldirimin yuruyus seridi
            // (Paths.PavementZ +- 0,35, yani -1,07 ile -0,37 arasi)
            // havuzun icinde kaliyor - isik YURUNEN YERI aydinlatmali.
            havuz.transform.localPosition = new Vector3(0f, 0.012f, LampHeadZ);
            havuz.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // SOKAK BOYUNCA UZUN. Dairesel bir leke "yerde duran bir
            // daire" diye okunuyordu; gercek bir lamba isigi kaldirim
            // boyunca uzayan bir elips birakir.
            havuz.transform.localScale = new Vector3(3.60f, 2.90f, 1f);

            Collider hcol = havuz.GetComponent<Collider>();
            if (hcol != null)
            {
                if (Application.isPlaying) Destroy(hcol); else DestroyImmediate(hcol);
            }
            Renderer hr = havuz.GetComponent<Renderer>();
            hr.sharedMaterial = _glowMat;
            hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hr.receiveShadows = false;
            havuz.SetActive(false);
            _lampGlow.Add(havuz);
        }

        /// <summary>
        /// Lambanin iki orgusunu kurar: metal ve cam.
        ///
        /// Olculer figure gore: figurler 1,10 m, lamba 1,86 m. Gercek
        /// hayatta oran daha buyuk (insan 1,7 - lamba 4,5) ama oyunun
        /// butun olcegi sikisik; burada onemli olan fenerin BAS HIZASININ
        /// UZERINDE kalmasi (fenerin alti 1,31 m) - yoksa kaldirimda
        /// yuruyen figurlerin icinden geciyor gorunur.
        /// </summary>
        private void BuildLampMeshes()
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();

            // --- kaide -------------------------------------------------
            Prism(v, n, t, 0.185f, 0.165f, 0.000f, 0.050f, 0f, 0f);   // taban levhasi
            Prism(v, n, t, 0.155f, 0.088f, 0.050f, 0.310f, 0f, 0f);   // konik kaide
            Prism(v, n, t, 0.105f, 0.098f, 0.360f, 0.050f, 0f, 0f);   // bilezik
            // --- govde -------------------------------------------------
            Prism(v, n, t, 0.052f, 0.042f, 0.410f, 1.210f, 0f, 0f);   // direk
            Prism(v, n, t, 0.068f, 0.062f, 0.950f, 0.055f, 0f, 0f);   // orta bilezik
            Prism(v, n, t, 0.058f, 0.050f, 1.585f, 0.045f, 0f, 0f);   // ust bilezik
            // --- fener: DIREGIN TEPESINDE ------------------------------
            //
            // Once kivrik bir kolla kaldirimin uzerine sarkiyordu
            // (referansin ortadaki modeli). Kullanici tepeye istedi -
            // referansin en sagdaki modeli - ve oyunun kamerasi bunu
            // hakli cikariyor: kol +Z'ye, yani KAMERAYA dogru uzuyordu
            // ve sabit acida tamamen kisaliyor. Gorunmeyen bir kol,
            // feneri "direge saplanmis" gibi gosteriyordu; tepedeki
            // fener ise her acidan fener.
            //
            // Isik da lambayla birlikte tasindi (z = 0): havuz 2,8 m
            // derinliginde ve direk bordurde - yani kaldirimin yuruyus
            // seridi (Paths.PavementZ +- 0,35) havuzun ICINDE kaliyor.
            float z = LampHeadZ;
            // FENER BUYUDU (yaricaplar x1,25).
            //
            // Ilk olcude model dogruydu ama OLCEK yanlisti: oyunun
            // gercek cercevesinde lamba 40 piksel ve fener onun besde
            // biri - yani sekiz piksellik bir leke. Goruntuye bakarak
            // secildi; kagitta dogru olan oran ekranda okunmuyordu.
            Prism(v, n, t, 0.072f, 0.066f, 1.600f, 0.070f, z, 0f);    // fener yatagi
            Prism(v, n, t, 0.190f, 0.160f, 1.660f, 0.045f, z, 0f);    // alt etek
            Prism(v, n, t, 0.188f, 0.181f, 2.045f, 0.040f, z, 0f);    // kapak bilezigi
            Prism(v, n, t, 0.225f, 0.088f, 2.085f, 0.110f, z, 0f);    // konik kapak
            Prism(v, n, t, 0.038f, 0.018f, 2.195f, 0.060f, z, 0f);    // tepe susu

            _lampMetalMesh = Build("LambaMetal", v, n, t);

            // --- cam ---------------------------------------------------
            //
            // Fenerin camı: alt genis, ust dar - referanstaki sekizgen
            // fenerin kendisi. Metalin bilezikleri camin ustune ve
            // altina denk geliyor, yani cam "cerceveli" okunuyor.
            v.Clear(); n.Clear(); t.Clear();
            Prism(v, n, t, 0.178f, 0.148f, 1.705f, 0.340f, LampHeadZ, 0f);
            _lampGlassMesh = Build("LambaCam", v, n, t);

            // FENERIN ALTI OLCULUYOR, YAZILMIYOR.
            //
            // Fener kaldirimin TAM USTUNDE duruyor; altindan gecen
            // figurler 1,10 m. "Olculer dogru secildi" demek yetmez -
            // bir parcanin yuksekligini degistiren biri (mesela feneri
            // buyuten ben) bunu farkinda olmadan bozabilir. Sayi
            // ORGUDEN okunuyor, yani cizilen seyden.
            // FENERI SECEN OLCU: DIREKTEN GENIS OLMAK.
            //
            // Once "z ekseninde fenere yakin koseler" diye seciliyordu
            // ve fener KOLUN ucundayken bu dogru bir ayirimdi. Fener
            // direge cikinca ayni kosul butun lambayi seciyor - kaide
            // dahil - ve olcu 0,00 m donuyor. Kosul yerine SILUET:
            // direk yaricapi 0,052; ondan genis ve belden yukari olan
            // her sey fener govdesidir.
            float enAlt = float.MaxValue;
            ScanLantern(_lampMetalMesh, ref enAlt);
            ScanLantern(_lampGlassMesh, ref enAlt);
            LampLanternBottom = enAlt == float.MaxValue ? 0f : enAlt;
        }

        /// <summary>Fener govdesinin en alt noktasi.</summary>
        private static void ScanLantern(Mesh m, ref float enAlt)
        {
            if (m == null) return;
            Vector3[] vs = m.vertices;
            for (int i = 0; i < vs.Length; i++)
            {
                Vector3 p = vs[i];
                if (p.y < 0.90f) continue;                       // kaide ve govde
                float r = Mathf.Sqrt(p.x * p.x + (p.z - LampHeadZ) * (p.z - LampHeadZ));
                if (r < 0.10f) continue;                          // direk ve bilezikler
                if (p.y < enAlt) enAlt = p.y;
            }
        }

        /// <summary>
        /// Fenerin en alt noktasi (m). Turun sorabilmesi icin: altindan
        /// gecen figurlerin basi buranin altinda kalmali.
        /// </summary>
        public float LampLanternBottom { get; private set; }

        private static Mesh Build(string ad, List<Vector3> v, List<Vector3> n,
                                  List<int> t)
        {
            Mesh m = new Mesh();
            m.name = ad;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// Sekizgen prizma. Dik duran parcalar icin.
        ///
        /// DUZ GOLGELEME: her yuzun kendi koseleri var, yani kenarlar
        /// KESKIN. Paylasilan kose yumusak bir silindir verirdi - bu
        /// oyunun butun modelleri az yuzeyli ve sert kenarli.
        /// </summary>
        private static void Prism(List<Vector3> v, List<Vector3> n, List<int> t,
                                  float rAlt, float rUst, float y0, float h,
                                  float z, float x)
        {
            PrismAt(v, n, t, rAlt, rUst, h, new Vector3(x, y0, z),
                    Quaternion.identity);
        }

        /// <summary>Sekizgen prizma, verilen yere ve aciya.</summary>
        private static void PrismAt(List<Vector3> v, List<Vector3> n, List<int> t,
                                    float rAlt, float rUst, float h,
                                    Vector3 pos, Quaternion rot)
        {
            const int yan = 8;
            Matrix4x4 m = Matrix4x4.TRS(pos, rot, Vector3.one);

            for (int i = 0; i < yan; i++)
            {
                float a0 = Mathf.PI * 2f * i / yan;
                float a1 = Mathf.PI * 2f * (i + 1) / yan;
                Vector3 p0 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a0) * rAlt, 0f, Mathf.Sin(a0) * rAlt));
                Vector3 p1 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a1) * rAlt, 0f, Mathf.Sin(a1) * rAlt));
                Vector3 p2 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a0) * rUst, h, Mathf.Sin(a0) * rUst));
                Vector3 p3 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a1) * rUst, h, Mathf.Sin(a1) * rUst));

                int b = v.Count;
                v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
                Vector3 nn = Vector3.Cross(p1 - p0, p2 - p0).normalized;
                n.Add(nn); n.Add(nn); n.Add(nn); n.Add(nn);
                t.Add(b); t.Add(b + 2); t.Add(b + 1);
                t.Add(b + 1); t.Add(b + 2); t.Add(b + 3);
            }

            // Kapaklar: ust her zaman, alt yalnizca daralan parcalarda
            // gorunur ama ikisi de dort ucgen - saymaya degmez.
            Cap(v, n, t, m, rUst, h, true);
            Cap(v, n, t, m, rAlt, 0f, false);
        }

        private static void Cap(List<Vector3> v, List<Vector3> n, List<int> t,
                                Matrix4x4 m, float r, float y, bool ust)
        {
            const int yan = 8;
            int b = v.Count;
            Vector3 nn = m.MultiplyVector(ust ? Vector3.up : Vector3.down);
            for (int i = 0; i < yan; i++)
            {
                float a = Mathf.PI * 2f * i / yan;
                v.Add(m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)));
                n.Add(nn);
            }
            for (int i = 1; i < yan - 1; i++)
            {
                if (ust) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
                else { t.Add(b); t.Add(b + i + 1); t.Add(b + i); }
            }
        }

        /// <summary>
        /// Sokaktan gecenler. Musteri DEGIL: cekirdek onlari bilmiyor.
        ///
        /// Tohum sabit: tur her kosuda ayni sokagi gormeli, yoksa
        /// "sohbet eden ikili var mi" kontrolu bir kosuda yesil bir
        /// kosuda kirmizi olur - kararsiz bir denetim, denetim degildir.
        /// </summary>
        private void BuildStreetLife()
        {
            if (_streetLife == null) _streetLife = gameObject.GetComponent<StreetLife>();
            if (_streetLife == null) _streetLife = gameObject.AddComponent<StreetLife>();
            _streetLife.Build(transform, CustomerPrefabs, 20260912);
        }

        private StreetLife _streetLife;

        /// <summary>Sokaktaki gecen sayisi. Turun sorabilmesi icin.</summary>
        public int StreetWalkers { get { return _streetLife == null ? 0 : _streetLife.Count; } }

        /// <summary>Sohbet eden gecen sayisi. Turun sorabilmesi icin.</summary>
        public int StreetChatting
        {
            get { return _streetLife == null ? 0 : _streetLife.Chatting; }
        }

        private Material _lampMat;
        private readonly List<Renderer> _lampHeads = new List<Renderer>();
        private Material _glowMat;
        private Material _ceilMat;
        private Material _waterMat;
        private readonly List<GameObject> _lampGlow = new List<GameObject>();
        private readonly List<GameObject> _roomGlow = new List<GameObject>();
        private readonly List<float> _lampX = new List<float>();
        private readonly List<Renderer> _streetSlabs = new List<Renderer>();
        private readonly List<Color> _streetBase = new List<Color>();

        /// <summary>
        /// Kac kez kuruldu. Gun isigi bileseni sokak lambalarini buna
        /// bakarak yeniden bagliyor: restoran buyuyunce lambalar da
        /// yeniden olusuyor ve eski basvurular silinmis nesnelere
        /// isaret ediyordu - aksam lambalar yanmiyordu ve hicbir sey
        /// uyarmiyordu.
        /// </summary>
        public int BuildStamp { get; private set; }

        /// <summary>Sokak lambalari. Gun isigi bilesenine veriliyor.</summary>
        public Renderer[] LampHeads { get { return _lampHeads.ToArray(); } }

        /// <summary>Lambalarin yerdeki isik havuzlari.</summary>
        public GameObject[] LampGlow { get { return _lampGlow.ToArray(); } }

        /// <summary>Restoranin ici tavan isiklari. Govdesi yok, yalnizca isik.</summary>
        public GameObject[] RoomGlow { get { return _roomGlow.ToArray(); } }

        /// <summary>
        /// Sokak lambasi araliklarindaki EN BUYUK SAPMA (m).
        ///
        /// Kullanicinin sikayeti: "sokak isiklari basta ortada ve sonda
        /// olsun, su an simetrik degil aralarindaki mesafe o kotu
        /// gorunuyor". Bunun olculebilir hali, komsu direkler arasi
        /// mesafelerin birbirinden ne kadar farkli oldugu: simetrik bir
        /// dizide sifir.
        ///
        /// Neden bir sayiya cevrildi: eski kod dort direk koyuyor ve
        /// biri kapinin onune dustugunde onu 2,2 m kaydiriyordu -
        /// araliklar 2,3 / 4,5 / 4,5 oluyordu. Kaydirma kodda TEK SATIR
        /// ve masumca duruyordu; ekranda bozan seyin kod okunarak
        /// gorulmedigi, ancak olculunce anlasildigi bir durum.
        /// </summary>
        public float LampSpacingError
        {
            get
            {
                if (_lampX.Count < 3) return 0f;
                float enKucuk = float.MaxValue, enBuyuk = 0f;
                for (int i = 1; i < _lampX.Count; i++)
                {
                    float d = Mathf.Abs(_lampX[i] - _lampX[i - 1]);
                    if (d < enKucuk) enKucuk = d;
                    if (d > enBuyuk) enBuyuk = d;
                }
                return enBuyuk - enKucuk;
            }
        }

        /// <summary>Sokak lambasi sayisi.</summary>
        public int LampPostCount { get { return _lampX.Count; } }

        /// <summary>
        /// Lambalarin isik parcasi sayisi (huzme + hale + havuz).
        ///
        /// Uc parcadan biri unutulursa gece sessizce eksik kalir: isik
        /// yine "var" gorunur, yalnizca zayif olur - ve zayif bir isik
        /// hata gibi degil tercih gibi okunur.
        /// </summary>
        public int LampGlowCount { get { return _lampGlow.Count; } }

        /// <summary>
        /// Sokak lambasi direginin z'si: bordurun uzeri.
        ///
        /// Dis yaya seridi -1,07'de; arasindaki 0,45 m, StreetLife'in
        /// direk icin kullandigi en az mesafeden (PostClear 0,40) buyuk.
        /// Buyuk olmasi sart: kucuk olsa dis seritteki her yaya surekli
        /// iceri dogru itilir ve serit bir ise yaramazdi.
        /// </summary>
        public const float LampPostZ = -1.52f;

        /// <summary>
        /// Sokaktaki SABIT engellerin yerel konumlari - direkler.
        ///
        /// Neden gerekti: yerlesim denetimi yayalari direklerle cakisik
        /// buldu. Cakismanin buyuk kismi figur kutusunun kol acikligini
        /// olcmesinden geliyordu ama govde hizasinda gercek bir direk
        /// var ve icinden gecen bir yaya, kullanicinin bildirdigi hatanin
        /// aynisi. Yol bulma degil: itisme onlari kenara aliyor, insanlar
        /// da bir direge oyle tepki verir.
        /// </summary>
        public void StreetObstacles(List<Vector3> into)
        {
            if (into == null) return;
            for (int i = 0; i < _lampX.Count; i++)
                into.Add(new Vector3(_lampX[i], 0f, LampPostZ));

            // TERAS MASALARI DA ENGEL.
            //
            // Kaldirima iki masa kondu (susleme) ve kaldirim ayni
            // zamanda yayalarin yurudugu yer: kaydedilmeseydi gecenler
            // masanin ICINDEN gecerdi. Bir seyi sahneye koymak, onu
            // yolun bir parcasi yapmak demek.
            for (int i = 0; i < _patio.Count; i++) into.Add(_patio[i]);
        }

        private void Street(string ad, float z0, float z1, Color c)
        {
            _streetBase.Add(c);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = ad;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(
                RoomPlan.PlotW * 0.5f, -0.05f, (z0 + z1) * 0.5f);
            go.transform.localScale = new Vector3(
                RoomPlan.PlotW + 2.4f, 0.1f, z1 - z0);

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            if (_block == null) _block = new MaterialPropertyBlock();
            Renderer ren = go.GetComponent<Renderer>();
            ren.sharedMaterial = _floorMat;
            ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ren.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, c);
            ren.SetPropertyBlock(_block);
            _streetSlabs.Add(ren);
        }

        /// <summary>
        /// SOKAK LEVHALARINI GECE KOYULASTIRIR. k = 1 gunduz, 0 gece.
        ///
        /// Neden gerekti - ve bu OLCULDU, goze gore degil: aksam
        /// goruntusunde salonun ORTANCA parlakligi 46, sokagin ortalamasi
        /// 51'di. Yani restoranin ici, onundeki kaldirimdan daha
        /// karanlikti. "Icerisi yeterince aydinlik degil" sikayetinin
        /// sayisal hali bu.
        ///
        /// Sebep basit: elimdeki her kaldirac KURESEL. Ortam isigi,
        /// dolgu, sicak anahtar - hepsi kaldirimi da restoran kadar
        /// aydinlatiyor, cunku URP'de ek (yerel) isiklar kapali ve isik
        /// katmanlari da kapali. Ic mekani disariya gore parlatmanin tek
        /// yolu, DISARIYI yerel olarak karartmak.
        ///
        /// Gercekte de dogru olan bu: gece bir kaldirim, uzerine dusen
        /// isik neyse o kadar aydinliktir - gunduzku ayni asfalt gece
        /// neredeyse siyahtir. Gunduz hicbir sey degismiyor.
        /// </summary>
        public void TintStreet(float k)
        {
            _streetTint = k;
            if (_streetSlabs.Count == 0) return;
            if (_block == null) _block = new MaterialPropertyBlock();

            for (int i = 0; i < _streetSlabs.Count && i < _streetBase.Count; i++)
            {
                if (_streetSlabs[i] == null) continue;
                Color c = _streetBase[i] * k;
                c.a = 1f;
                _streetSlabs[i].GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, c);
                _streetSlabs[i].SetPropertyBlock(_block);
            }
        }

        /// <summary>Sokak levhasi sayisi. Turun sorabilmesi icin.</summary>
        public int StreetSlabCount { get { return _streetSlabs.Count; } }

        /// <summary>
        /// Sokaga son uygulanan tonlama. 1 gunduz, kucuk deger gece.
        ///
        /// Turun sorabilmesi icin: "restoranin ici disarisindan aydinlik"
        /// iddiasinin mekanizmasi bu tek sayi. Kureseli yukseltip yereli
        /// dusurmenin calistigi ancak ikisi birlikte olculunce belli
        /// oluyor - yalnizca ic isiga bakan bir kontrol, sokak da ayni
        /// oranda parlarken yesil kalirdi.
        /// </summary>
        public float StreetTint { get { return _streetTint; } }

        private float _streetTint = 1f;

        // =====================================================================
        // TURUN SORACAKLARI.
        //
        // Bu uc ozelligin hicbiri bir sayiya donusmuyordu: duvar ya
        // vardir ya yoktur, asci ya ocaga bakar ya bakmaz, animasyon ya
        // isler ya donar. Sorulabilir olmayan sey, denetlenebilir de
        // degil - ve bu projede olculmeyen her sey en az bir kez sessizce
        // bozuldu.

        /// <summary>Sahnedeki oda duvari sayisi.</summary>
        public int WallCount
        {
            get
            {
                int n = 0;
                foreach (Transform t in transform) if (t.name == "Duvar") n++;
                return n;
            }
        }

        /// <summary>
        /// Duvarlar gercekten saydam mi VE carpisansiz mi.
        ///
        /// Ikisi de sessizce bozulabilir: _Surface tek basina URP'de
        /// yalnizca denetci ayari (firin caminda tam bu oldu), ve
        /// carpisan kalirsa odaya dokunmak calismaz.
        /// </summary>
        public bool WallsClear
        {
            get
            {
                // NE SORULUYOR: malzeme SAYDAM GECISTE mi ve gercekten
                // yari saydam mi.
                //
                // Once _SrcBlend'in SrcAlpha oldugu soruluyordu ve
                // malzemeler .mat VARLIGINA tasininca kirmiziya dustu:
                // URP varlik icin harmanlama alanlarini KENDI yaziyor
                // (_Surface/_Blend'den turetiyor) ve One + OneMinusSrcAlpha
                // yazdi - yani onceden carpilmis alfa, ve ekranda
                // saydamlik dogru cizildi. Denetim, URP'nin sahip
                // oldugu bir ayrintiyi sorguluyordu.
                //
                // Yazarin kontrol ettigi gercekler: saydam yuzey tipi,
                // saydam cizim kuyrugu ve dusuk alfa.
                if (_wallMat == null) return false;
                if (!_wallMat.HasProperty("_Surface")) return false;
                if ((int)_wallMat.GetFloat("_Surface") != 1) return false;
                if (_wallMat.renderQueue < 2900) return false;
                if (_wallMat.GetColor(BaseColorId).a > 0.5f) return false;

                foreach (Transform t in transform)
                    if (t.name == "Duvar" && t.GetComponent<Collider>() != null)
                        return false;
                return true;
            }
        }

        /// <summary>
        /// Mutfak isi yapan personel sayisi ve ANIMATOR'U ISLEYEN sayisi.
        ///
        /// Ikincisi sart: Figure gecisten ~1 sn sonra Animator'i
        /// kapatiyor (oturan musteri icin dogru) ve asci o yuzden
        /// dograma klibinin ilk karesinde donup kaliyordu.
        /// </summary>
        public void KitchenWork(out int calisan, out int islenen)
        {
            calisan = 0;
            islenen = 0;
            for (int i = 0; i < _staff.Count; i++)
            {
                Figure f = FigureOf(_staff[i]);
                if (f == null) continue;
                if (f.Current != Figure.Pose.Chop && f.Current != Figure.Pose.Wash
                    && f.Current != Figure.Pose.Serve) continue;
                calisan++;
                if (f.Anim != null && f.Anim.enabled) islenen++;
            }
        }

        /// <summary>
        /// Mutfak isi yapan ascilarin ocaga bakis HATASI (derece).
        ///
        /// -1: su an calisan asci yok. Buyuk bir sayi, ascinin ocagi
        /// arkasi donuk kullandigi anlamina gelir - kullanicinin
        /// "surekli bu yone bakiyor" dedigi seyin olculebilir hali.
        /// </summary>
        public float CookFacingErrorDeg
        {
            get
            {
                float enKotu = -1f;
                for (int i = 0; i < _staff.Count && i < _staffCooks; i++)
                {
                    Figure f = FigureOf(_staff[i]);
                    if (f == null) continue;
                    if (f.Current != Figure.Pose.Chop && f.Current != Figure.Pose.Wash
                        && f.Current != Figure.Pose.Serve) continue;

                    Transform t = _staff[i].transform;

                    // HEDEF ASAMADAN GELIYOR: yikarken tezgaha,
                    // pisirirken ocaga bakiyor. Hepsini "ocak" saymak,
                    // dogru duran bir asciyi yanlis gostermek olurdu.
                    CookRoutine cr = i < _cookRoutine.Count ? _cookRoutine[i] : null;
                    Vector3 bakilan = cr != null && cr.Busy
                        ? cr.LookTarget
                        : StovePos(i, t.localPosition);
                    Vector3 hedef = bakilan - t.localPosition;
                    hedef.y = 0f;
                    if (hedef.sqrMagnitude < 0.01f) continue;

                    float aci = Vector3.Angle(t.forward, hedef);
                    if (aci > enKotu) enKotu = aci;
                }
                return enKotu;
            }
        }

        private int _staffCooks;

        /// <summary>
        /// Calisan personelin is klibinin ilerledigi ve DONDUGU kare
        /// sayisi. Turun sorabilmesi icin; kumulatif.
        /// </summary>
        public static int WorkAnimAdvanced, WorkAnimStalled;

        private readonly Dictionary<int, float> _isKlip =
            new Dictionary<int, float>();

        /// <summary>
        /// Odanin zemin rengi.
        ///
        /// Zemin artik DESENLI (FloorPattern) ve desen bu levhanin
        /// uzerinde duruyor; yine de kenarlarda ve desenin arasindan
        /// goruunuyor. Kimlik paletiyle uyumsuz bir taban rengi, butun
        /// odayi "yanlis mutfak" gosteriyordu - hizli yemekte sicak
        /// kahve bir zemin, Turk'te soguk gri bir zemin gibi.
        ///
        /// Servis odalari yine de AYRI: mutfak ve bulasik daha soguk,
        /// depo daha koyu. Oyuncunun "burasi arka taraf" ayrimini
        /// yapabilmesi gerekiyor.
        /// </summary>
        private Color RoomColor(in RoomPlan.Room r)
        {
            Palette p = Pal(CuisineId);
            if (r.IsDining) return p.FloorAlt;
            if (r.Name == "Mutfak" || r.Name == "Bulasik")
                return Color.Lerp(p.FloorAlt, RoomKitchen, 0.65f);
            return Color.Lerp(p.FloorAlt, RoomService, 0.65f);
        }

        /// <summary>
        /// Servis odalarinin esyalari. Mutfak ocak ve dolap, bulasik
        /// lavabo, depo raf. Salon odalari bos - onlarin esyasi masa.
        /// </summary>
        private void BuildRoomProps(int tables)
        {
            foreach (RoomPlan.Room r in RoomPlan.Rooms)
            {
                // KAPALI ODAYA EKIPMAN KONMUYOR.
                //
                // Bugun zararsiz: plandaki Mutfak/Giris/Bulasik/Depo
                // IsDining == false oldugu icin hep acik (RoomPlan.cs).
                // Ama plana masali bir servis odasi eklenirse kapali
                // odaya SESSIZCE ocak ve lavabo yerlesirdi - duvarsiz
                // bir bosluga asili duran ekipman. Tek satir, tuzagi
                // simdi kapatiyor.
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                switch (r.Name)
                {
                    case "Mutfak":
                        // BUZDOLABI ARKA SAG KOSEDE, ocak sirasi ona yer
                        // birakiyor.
                        //
                        // Once ikisi de arka SOL kosedeydi: yerlesim
                        // denetimi ocakla buzdolabi arasinda 0,30 m
                        // cakisma olctu - buzdolabi ilk ocagin icinde
                        // duruyordu ve genel gorunumde tek bir bicimsiz
                        // kutle olarak okunuyordu.
                        LineUp(r, StovePrefab, 3, 0.55f, 180f, rightInset: 1.15f,
                               appliance: true);
                        // ON SIRA TEZGAHLARI KALKTI.
                        //
                        // Yerine SERVIS BANKOSU geldi (RestaurantView.Decor)
                        // ve ikisi AYNI yerde duruyordu: goruntude
                        // tezgah kutulari bankonun icinden cikiyor,
                        // kaplar tezgahin uzerinde asili duruyordu.
                        // Yakin plan olmasa fark edilmezdi.
                        //
                        // Banko zaten daha iyi bir tezgah: tablasi,
                        // sirali kaplari ve cam siperi var. Ascinin
                        // calisma noktalari ocaklardan turiyor
                        // (KitchenPosts), yani tezgah sirasinin
                        // kaldirilmasi kimseyi issiz birakmiyor.
                        // Sag duvarda: yuzu -X, yani odaya donuk. Sol
                        // duvardayken 90 dogruydu, tasininca duzeltildi.
                        Place(FridgePrefab, r.X0 + r.W - 0.55f, r.Z0 + r.D - 0.7f, -90f);
                        break;
                    case "Bulasik":
                        BuildDishStation(r);
                        // ON KORIDOR BOS KALMALI (Paths.LaneZ = 0,55).
                        // Tezgah z=0,7'deydi ve tam koridorun uzerinde
                        // duruyordu; musteriler ve garsonlar oradan
                        // geciyor. Odanin ortasina alindi - bir hazirlik
                        // tezgahi olarak zaten dogru yer.
                        Place(CounterPrefab, r.CenterX, r.Z0 + 2.2f, 0f);
                        break;
                    case "Depo":
                        LineUp(r, ShelfPrefab, 2, 0.6f, 180f);
                        Place(FridgePrefab, r.X0 + r.W - 0.7f, r.Z0 + 0.8f, -90f);
                        break;
                    case "Giris":
                        // KAPI ON KENARDA, Paths.DoorX hizasinda.
                        // Musterinin girdigi yer ile kapinin durdugu yer
                        // ayni sayidan geliyor; iki yere yazmak bu
                        // projede bes kez sessizce ayristi.
                        Threshold(r);
                        Place(CounterPrefab, r.CenterX, r.Z0 + r.D - 0.8f, 180f);
                        // Saksilar ARKA koseye: on kenar artik kapi ve
                        // koridor, yani gecis yolu (Paths).
                        Place(PlantPrefab, r.X0 + 0.55f, r.Z0 + r.D - 0.7f, 0f);
                        Place(PlantPrefab, r.X0 + r.W - 0.55f, r.Z0 + r.D - 0.7f, 0f);
                        break;
                }
            }
        }

        /// <summary>
        /// BULASIKHANE: lavabolar, kirli yigin, temiz yigin, yikama yeri.
        ///
        /// Hepsi TEK FONKSIYONDA, cunku hepsi birbirinin konumundan
        /// tureiyor: yikayan figur lavabonun onunde duruyor, kirli yigin
        /// lavabonun solunda, temiz yigin saginda. Lavabolari LineUp ile
        /// koyup yiginlari ayri bir yerde hesaplamak, ayni sayiyi iki
        /// yere yazmak olurdu - bu projede bes kez sessizce ayristi.
        ///
        /// Kullanicinin tarifi: "bulasikcinin orada bos ve kirli tabaklar
        /// biriksin, bulasikci onlari lavaboda eliyle yikasin ve temiz
        /// tabaklari diger tarafa dizsin".
        /// </summary>
        private void BuildDishStation(RoomPlan.Room r)
        {
            _dirtyStack.Clear();
            _cleanStack.Clear();

            const float Inset = 0.6f;
            float z = r.Z0 + r.D - Inset;

            // TEK LAVABO, IKI DEGIL - ve bunu YERLESIM DENETIMI soyledi.
            //
            // Ilk yazim iki lavabo ve iki tezgah koydu; denetim 0,16 m
            // cakisma buldu. Sebep aritmetik: oda 3,2 m genis ve dort
            // nesnenin her biri ~0,84 m, yani 3,36 m gerekiyor. Tezgah +
            // lavabo + tezgah 2,52 m ve rahatca siginyor.
            //
            // Anlatica da dogru: kullanici "lavaboda eliyle yikasin"
            // dedi - tek bir lavabo basi.
            float lavabo = r.CenterX;
            float lavaboUst = TopOf(Place(SinkPrefab, lavabo, z, 180f));

            // YIKAYAN FIGUR lavabonun ONUNDE duruyor, arkasinda degil:
            // arkasi duvar. Yuzu lavaboya, yani +Z (yaw 0).
            _washSpot = new Vector3(lavabo, 0f, z - 0.75f);

            // Iki yigin AYRI olmali: ayni yerde biriken iki yigin,
            // "yikaniyor" degil "duruyor" diye okunuyor.
            //
            // Yiginlar TEZGAHIN USTUNDE duruyor ve yukseklik tezgahtan
            // OLCULEREK aliniyor, yazilarak degil: ilk yazimda 0,92 m
            // tahmin edildi ve tabaklar havada asili kaldi. Tezgah
            // modelinin yuksekligi degisirse yigin onunla birlikte
            // degisiyor.
            float solX = r.X0 + 0.55f;
            float sagX = r.X0 + r.W - 0.55f;
            float yigiZ = z - 0.02f;
            float ust = TopOf(Place(CounterPrefab, solX, yigiZ, 180f));
            TopOf(Place(CounterPrefab, sagX, yigiZ, 180f));

            // AKISA GORE: kirli SAGDA, temiz SOLDA.
            //
            // Kirli tabaklar salondan geliyor (salonlar x > 8,4, yani
            // sagda), temiz tabaklar mutfaga gidiyor (mutfak x < 5,2,
            // yani solda). Ilk yazim tersiydi ve her tabak odayi bosuna
            // bir kez daha kat ediyordu.
            BuildPlateStack(_cleanStack, solX, yigiZ, ust);
            BuildPlateStack(_dirtyStack, sagX, yigiZ, ust);

            // MUSLUK SUYU: yalnizca biri yikarken akiyor.
            //
            // Ince bir levha, lavabonun ustunden teknenin icine. Parcacik
            // yok - hedef dusuk seviye Adreno (docs/19) ve bu kamera
            // mesafesinde bir su huzmesi zaten birkac piksel. Yuksekligi
            // lavabodan OLCULEREK aliniyor (TopOf), yazilarak degil.
            _water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _water.name = "MuslukSuyu";
            _water.transform.SetParent(transform, false);
            _water.transform.localPosition =
                new Vector3(lavabo, lavaboUst - 0.07f, z - 0.06f);
            _water.transform.localScale = new Vector3(0.035f, 0.15f, 0.035f);
            Collider suCol = _water.GetComponent<Collider>();
            if (suCol != null)
            {
                if (Application.isPlaying) Destroy(suCol); else DestroyImmediate(suCol);
            }
            Renderer suRen = _water.GetComponent<Renderer>();
            if (_waterMat != null) suRen.sharedMaterial = _waterMat;
            suRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            suRen.receiveShadows = false;
            _water.SetActive(false);

            // KOPUK: teknenin icinde birkac kucuk beyaz kume.
            //
            // Akan su tek basina "duruluyor" diye okunuyor; kopuk
            // "yikaniyor" diyor. Parcacik yok - dort kucuk kutu, hepsi
            // kurulusta olusuyor ve yalnizca gorunurlugu degisiyor.
            // Kumelenmis ve BOYLARI FARKLI: esit boyda, esit arali dort
            // levha kopuk degil fayans gibi okunuyordu. Ust uste binen,
            // farkli boyda bes parca bir kutle veriyor.
            float[] kx = { -0.07f, 0.00f, 0.06f, -0.03f, 0.03f };
            float[] kz = { -0.02f, 0.03f, -0.01f, 0.05f, -0.04f };
            float[] ky = { 0.000f, 0.014f, 0.004f, 0.020f, 0.008f };
            float[] kb = { 0.085f, 0.070f, 0.078f, 0.055f, 0.062f };

            _foam.Clear();
            for (int i = 0; i < kx.Length; i++)
            {
                GameObject k = GameObject.CreatePrimitive(PrimitiveType.Cube);
                k.name = "Kopuk";
                k.transform.SetParent(transform, false);
                k.transform.localPosition = new Vector3(
                    lavabo + kx[i], lavaboUst - 0.062f + ky[i], z - 0.05f + kz[i]);
                k.transform.localScale = new Vector3(kb[i], 0.030f, kb[i] * 0.85f);
                k.transform.localRotation = Quaternion.Euler(0f, i * 17f, 0f);

                Collider kc = k.GetComponent<Collider>();
                if (kc != null)
                {
                    if (Application.isPlaying) Destroy(kc); else DestroyImmediate(kc);
                }
                Renderer kr = k.GetComponent<Renderer>();
                kr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                kr.receiveShadows = false;
                if (_block == null) _block = new MaterialPropertyBlock();
                kr.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, new Color(0.97f, 0.98f, 0.99f));
                kr.SetPropertyBlock(_block);
                k.SetActive(false);
                _foam.Add(k);
            }

            // ASCININ TABAK ALDIGI YER: temiz yiginin onu.
            _plateSpot = new Vector3(solX, 0f, yigiZ - 0.75f);
        }

        /// <summary>
        /// Bir tabak yigini: ust uste duran ince levhalar.
        ///
        /// Hepsi KURULUSTA olusuyor ve sonra yalnizca gorunurlugu
        /// degisiyor. Her karede nesne yaratip yok etmek, zirvede saniyede
        /// onlarca ayirma demek - ve bu sahne dusuk seviye Adreno'yu
        /// hedefliyor.
        /// </summary>
        private void BuildPlateStack(List<GameObject> into, float x, float z, float top)
        {
            if (PlatePrefab == null) return;
            for (int i = 0; i < PlateStackMax; i++)
            {
                GameObject p = Instantiate(PlatePrefab, transform);
                p.name = "TabakYigini";
                p.transform.localPosition = new Vector3(x, top + i * PlateStep, z);
                p.transform.localRotation = Quaternion.identity;
                foreach (Collider c in p.GetComponentsInChildren<Collider>())
                {
                    if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
                }
                p.SetActive(false);
                into.Add(p);
            }
        }

        /// <summary>
        /// SUNGER: yikayan figurun otekli elinde.
        ///
        /// Kucuk ve sari-yesil bir kutu. Tabak bir elde, sunger otekinde -
        /// "ovuyor" cumlesinin ekrandaki karsiligi bu ikili; tek basina
        /// tabak tutan bir figur onu tasiyor gibi duruyor.
        /// </summary>
        private void ShowSponge(GameObject staff, bool on)
        {
            if (staff == null) return;

            Transform t = staff.transform.Find("Sunger");
            if (t == null)
            {
                if (!on) return;
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.name = "Sunger";
                g.transform.SetParent(staff.transform, false);
                g.transform.localScale = new Vector3(0.10f, 0.05f, 0.07f);
                Collider c = g.GetComponent<Collider>();
                if (c != null)
                {
                    if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
                }
                Renderer ren = g.GetComponent<Renderer>();
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (_block == null) _block = new MaterialPropertyBlock();
                ren.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, new Color(0.88f, 0.82f, 0.35f));
                ren.SetPropertyBlock(_block);
                t = g.transform;
            }

            // Tabagin biraz yaninda ve altinda: iki el ayri seyler
            // tutuyor.
            t.localPosition = new Vector3(0.17f, 0.58f, 0.26f);
            if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }

        /// <summary>Yiginda gosterilen en fazla tabak. Ustu sayiyla anlatiliyor.</summary>
        /// <summary>
        /// "Lavaboda" gorev numarasi.
        ///
        /// Masa numaralari 0'dan basliyor ve -1 "bosta"; yikamanin kendi
        /// numarasi olmali ki ikisiyle karismasin.
        /// </summary>
        private const int WashTask = -7;

        /// <summary>
        /// Lavaboda GORUNUR kalma suresi (gercek saniye).
        ///
        /// 2,5: musluk acilip kapaniyor, sunger birkac kez donuyor,
        /// kopuk goruunuyor. Daha kisasi goz kirpma, daha uzunu salonu
        /// bos birakiyor.
        /// </summary>
        private const float WashVisitSeconds = 2.5f;

        /// <summary>
        /// Lavaboya yuruyus icin guvenlik suresi (gercek saniye).
        ///
        /// Yuruyus bitene kadar gorev degisikligi dinlenmiyor; bu sayi
        /// yalnizca "yol bir sekilde tamamlanmadi" durumunda figuru
        /// serbest birakiyor. Tavansiz birakmak, bir kere takilan figuru
        /// gun boyu lavaboya kilitlerdi.
        /// </summary>
        private const float WashWalkTimeout = 12f;

        private readonly List<float> _washHold = new List<float>();

        private const int PlateStackMax = 10;

        /// <summary>Iki tabak arasi yukseklik (m).</summary>
        private const float PlateStep = 0.022f;

        /// <summary>
        /// Bir nesnenin UST yuzeyinin yuksekligi (yerel y).
        ///
        /// Tahmin yerine olcum: model degisirse ustune konan sey onunla
        /// birlikte kayiyor. Nesne yoksa makul bir tezgah yuksekligi
        /// donuyor - sifir donmek tabaklari yere sererdi.
        /// </summary>
        private static float TopOf(GameObject go)
        {
            if (go == null) return 0.90f;
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return 0.90f;

            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b.max.y - go.transform.parent.position.y;
        }

        private readonly List<GameObject> _dirtyStack = new List<GameObject>();
        private readonly List<GameObject> _cleanStack = new List<GameObject>();
        private Vector3 _washSpot;

        /// <summary>
        /// ONIZLEME: bir personeli lavaboya koyup yikatir.
        ///
        /// Yalnizca goruntu almak icin. Editor kipinde kimse yikamiyor
        /// (personel bosta kuruluyor) ve "musluk akiyor mu, sunger var
        /// mi" sorusunun cevabi ancak yikayan biri varken goruluyor.
        /// </summary>
        public void PreviewWash()
        {
            if (_staff.Count == 0) return;
            int i = _staff.Count - 1;              // son kisi: salon tarafi
            Walker w = WalkerOf(_staff[i]);
            Figure f = _staffFigure[i];
            if (w == null || f == null) return;

            w.Warp(_washSpot, 0f);
            f.Sample(Figure.Pose.Wash, 0.5f);
            ShowCarry(_staff[i], 1);
            ShowSponge(_staff[i], true);
            if (_water != null) _water.SetActive(true);

            // Yiginlar da dolu gorunsun: bos bir lavabo "yikaniyor"
            // demiyor.
            Show(_dirtyStack, 5);
            Show(_cleanStack, 3);
            for (int k = 0; k < _foam.Count; k++)
                if (_foam[k] != null) _foam[k].SetActive(true);
        }

        /// <summary>
        /// YURUYEN figurlerde klip hizi ile YER HIZI arasindaki en buyuk
        /// bagil sapma. Turun sorabilmesi icin.
        ///
        /// Ayak kaymasinin OLCUSU bu: figur saniyede kac metre gidiyorsa,
        /// bacaklar o mesafeye gore donmeli. Anim.speed x WalkClipSpeed
        /// yer hizina esit olmali; degilse ayaklar kayiyor.
        ///
        /// Gozle bakilarak fark edilmesi zor bir hata - ve tam o yuzden
        /// aylarca duruyordu.
        /// </summary>
        public float WalkSlipWorst
        {
            get
            {
                float worst = 0f;
                for (int i = 0; i < _staff.Count; i++)
                {
                    Figure f = _staffFigure[i];
                    Walker w = WalkerOf(_staff[i]);
                    if (f == null || w == null || !w.Moving) continue;
                    if (f.Current != Figure.Pose.Walk) continue;
                    if (f.Anim == null || !f.Anim.enabled) continue;

                    float yer = w.LastGroundSpeed;
                    float klip = f.Anim.speed * Figure.WalkClipSpeed;
                    if (yer < 0.01f) continue;
                    float sapma = Mathf.Abs(klip - yer) / yer;
                    if (sapma > worst) worst = sapma;
                }
                return worst;
            }
        }

        /// <summary>Lavabonun onu: yikayan figurun durdugu yer.</summary>
        public Vector3 WashSpot { get { return _washSpot; } }

        /// <summary>Temiz tabak yiginin onu: ascinin tabagi aldigi yer.</summary>
        public Vector3 PlateSpot { get { return _plateSpot; } }

        private Vector3 _plateSpot;
        private GameObject _water;
        private readonly List<GameObject> _foam = new List<GameObject>();

        /// <summary>
        /// Su an lavaboda duran figur sayisi. Turun sorabilmesi icin.
        ///
        /// Simulasyonun "yikiyor" demesi ile figurun lavaboda GORUNMESI
        /// ayri iki sey; ikincisi olculmezse yikama sessizce evde
        /// oynanabilir.
        /// </summary>
        /// <summary>
        /// Bugun oyuncunun lavaboda birini GORDUGU kare sayisi.
        ///
        /// Anlik WashingCount bir pencerede orneklendiginde neredeyse hep
        /// sifir cikiyor - yikama kisa ve pencere dar. Birikmeli sayac
        /// "bugun yikama goruldu mu" sorusunun dogru olcusu.
        /// </summary>
        public int WashSeenFrames { get { return _washSeen; } }

        private int _washSeen;

        public int WashingCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _staff.Count; i++)
                {
                    Figure f = _staffFigure[i];
                    if (f == null || f.Current != Figure.Pose.Wash) continue;
                    Vector3 d = _staff[i].transform.localPosition - _washSpot;
                    d.y = 0f;
                    if (d.sqrMagnitude < 1.2f * 1.2f) n++;
                }
                return n;
            }
        }

        /// <summary>
        /// Yiginlari simulasyondaki sayiya gore gosterir.
        ///
        /// Gorunen yigin TAVANLI (PlateStackMax): elli alti tabaklik bir
        /// kule odanin tavanini asardi ve zaten okunmuyor. Oyuncunun
        /// gordugu sey "az mi cok mu" - o da on levhada rahatca
        /// okunuyor.
        /// </summary>
        private void UpdatePlateStacks(Simulation sim)
        {
            Show(_dirtyStack, sim.PlatesDirty);
            Show(_cleanStack, sim.PlatesClean);
        }

        /// <summary>
        /// Musluk YALNIZCA biri yikarken akiyor.
        ///
        /// Surekli akan bir musluk "yikaniyor" demiyor, "unutulmus" diyor -
        /// ve oyuncunun lavaboya bakmasi icin bir sebep birakmiyor.
        /// </summary>
        private void UpdateWater()
        {
            bool aksin = WashingCount > 0;
            if (_water != null && _water.activeSelf != aksin) _water.SetActive(aksin);
            for (int i = 0; i < _foam.Count; i++)
                if (_foam[i] != null && _foam[i].activeSelf != aksin)
                    _foam[i].SetActive(aksin);
        }

        private static void Show(List<GameObject> stack, int count)
        {
            if (count > PlateStackMax) count = PlateStackMax;
            for (int i = 0; i < stack.Count; i++)
            {
                if (stack[i] == null) continue;
                bool on = i < count;
                if (stack[i].activeSelf != on) stack[i].SetActive(on);
            }
        }

        /// <param name="rightInset">
        /// Sagda bos birakilacak ek pay. Ayni duvarda baska bir esya
        /// varsa (mutfakta buzdolabi) sira ona girmesin diye.
        /// </param>
        private void LineUp(RoomPlan.Room r, GameObject prefab, int count,
                            float inset, float yaw, bool back = true,
                            float rightInset = 0f, bool appliance = false)
        {
            if (prefab == null || count <= 0) return;
            float z = back ? r.Z0 + r.D - inset : r.Z0 + inset;
            float en = r.W - inset * 2f - rightInset;
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                GameObject go = Place(prefab, r.X0 + inset + en * t, z, yaw);
                if (appliance && go != null)
                {
                    // OCAGIN USTUNE TENCERE.
                    //
                    // Kullanicinin sikayeti: "mutfakta ocak uzerine
                    // tencere vs konulmuyor, gercekci bir mutfak
                    // goruntusu yok". Ocaklar bostu ve bos bir ocak,
                    // mutfagi "ekipman sergisi" gibi gosteriyordu.
                    //
                    // Tencerenin yuksekligi OCAKTAN olculuyor: paket
                    // degisirse tencere havada ya da icinde kalmasin.
                    _potSpots.Add(go.transform);
                    // ISIKSIZ malzeme: lamba ve alev ANLAM tasiyor,
                    // aydinlatma sonucu degil. Isikli malzemeyle firinin
                    // icindeki lamba karanlik bir panel olarak ciziliyordu.
                    Appliance a = Appliance.Attach(go, _badgeMat, _glassMat);
                    if (a != null) _stoves.Add(a);
                }
            }
        }

        /// <summary>Ocaklarin yeri; tencereler kurulusun sonunda konuyor.</summary>
        private readonly List<Transform> _potSpots = new List<Transform>();

        /// <summary>
        /// OCAK USTU: tencere, tava ve kapak.
        ///
        /// Susleme ile ayni yol (Modeler, renge gore tek orgu) ama
        /// KURULUS SIRASI yuzunden ayri: ocaklar BuildRoomProps'ta
        /// yerlesiyor, susleme ise ondan sonra. Tencereler ocaklarin
        /// OLCULEN ust yuzeyine oturuyor.
        /// </summary>
        private void BuildPots(Palette p)
        {
            if (_potSpots.Count == 0) return;

            Modeler m = new Modeler();
            Color celik = new Color(0.647f, 0.678f, 0.722f);
            Color koyu = new Color(0.239f, 0.255f, 0.278f);

            for (int i = 0; i < _potSpots.Count; i++)
            {
                Transform t = _potSpots[i];
                if (t == null) continue;

                // Ocagin ust yuzeyi: cizicilerin dunya kutusundan.
                float ust = 0.9f;
                bool ilk = true;
                Bounds b = new Bounds();
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
                {
                    if (ilk) { b = r.bounds; ilk = false; }
                    else b.Encapsulate(r.bounds);
                }
                if (!ilk) ust = b.max.y - transform.position.y;

                Vector3 yer = t.localPosition;

                if (i % 2 == 0)
                {
                    // Tencere: govde, kapak ve iki kulp.
                    m.Prism(10, 0.155f, 0.165f, 0.20f,
                            new Vector3(yer.x - 0.12f, ust, yer.z),
                            Quaternion.identity, celik);
                    m.Prism(10, 0.175f, 0.155f, 0.035f,
                            new Vector3(yer.x - 0.12f, ust + 0.20f, yer.z),
                            Quaternion.identity, koyu);
                    m.Box(new Vector3(yer.x - 0.12f, ust + 0.245f, yer.z),
                          new Vector3(0.05f, 0.04f, 0.05f), koyu);
                    for (int k = 0; k < 2; k++)
                        m.Box(new Vector3(yer.x - 0.12f + (k == 0 ? -0.185f : 0.185f),
                                          ust + 0.13f, yer.z),
                              new Vector3(0.06f, 0.04f, 0.10f), koyu);
                }
                else
                {
                    // Tava: sig govde ve uzun sap.
                    m.Prism(10, 0.19f, 0.21f, 0.075f,
                            new Vector3(yer.x + 0.10f, ust, yer.z),
                            Quaternion.identity, koyu);
                    m.Box(new Vector3(yer.x + 0.10f, ust + 0.055f, yer.z + 0.28f),
                          new Vector3(0.05f, 0.035f, 0.34f), koyu);
                }
            }

            if (_block == null) _block = new MaterialPropertyBlock();
            m.Build(transform, "OcakUstu", _floorMat, _block);
            _potCount = _potSpots.Count;
        }

        /// <summary>
        /// Ocaklarin ustune kap konan ocak sayisi.
        ///
        /// OLCUM ICIN VAR. Kullanici "mutfakta ocak uzerine tencere vs
        /// konulmuyor, gercekci bir mutfak goruntusu yok" dedi ve
        /// kaplar eklendi - ama HICBIR KONTROL onlari sormuyordu.
        /// Ocaklarin yerlesimi degisse, BuildPots'un cagrisi dusse ya
        /// da _potSpots bos kalsa mutfak sessizce yeniden bosalirdi ve
        /// bunu ancak kullanici gorurdu.
        /// </summary>
        public int PotCount { get { return _potCount; } }

        private int _potCount;

        /// <summary>
        /// ESIK PASPASI: kapinin nerede oldugunu soyleyen yatay isaret.
        ///
        /// Once bir kapi CERCEVESI konuldu ve okunmadi: paketin
        /// doorwayOpen modeli yalnizca iki yan direk (ust kirisi yok) ve
        /// 34 derecelik bakista yerde duran iki tahta gibi goruunuyor.
        /// wallDoorway ise tam duvar; on kenara duvar koymak Giris
        /// odasini kameradan gizler.
        ///
        /// Duvarsiz bir kat planinda kapiyi isaretlemenin dogru araci
        /// DUSEY degil YATAY. Tepeden bakan bir kamerada paspas
        /// okunuyor, direk okunmuyor.
        ///
        /// Genislik 1,6 m: iki kisinin yan yana gectigi bir kapi
        /// agzi kadar, ve Paths.DoorX ile ayni merkezde - musterinin
        /// girdigi yer ile isaretin durdugu yer ayni sayidan geliyor.
        /// </summary>
        private void Threshold(RoomPlan.Room r)
        {
            GameObject mat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mat.name = "Esik";
            mat.transform.SetParent(transform, false);
            mat.transform.localPosition = new Vector3(Paths.DoorX, -0.045f, r.Z0 + 0.45f);
            mat.transform.localScale = new Vector3(1.6f, 0.1f, 0.9f);
            // EDITOR KIPINDE Destroy ERTELENIYOR ve carpisan kutu
            // sahnede KALIYOR. Goruntu araci Rebuild'i editor kipinde
            // kosuyor; kalan kutu, odaya dokunma isinini onunde
            // kesiyordu.
            Collider matCol = mat.GetComponent<Collider>();
            if (matCol != null)
            {
                if (Application.isPlaying) Destroy(matCol); else DestroyImmediate(matCol);
            }

            Renderer ren = mat.GetComponent<Renderer>();
            ren.sharedMaterial = _floorMat;
            ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if (_block == null) _block = new MaterialPropertyBlock();
            ren.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, MatColor);
            ren.SetPropertyBlock(_block);
        }

        private GameObject Place(GameObject prefab, float x, float z, float yaw)
        {
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, transform);
            go.transform.localPosition = new Vector3(x, 0f, z);
            go.transform.localRotation = Quaternion.Euler(0f, yaw + PropYaw, 0f);
            Retint(go);
            return go;
        }

        private void BuildTables(int tables)
        {
            List<RoomPlan.TableSpot> spots = RoomPlan.TableSpots(tables);
            foreach (RoomPlan.TableSpot s in spots)
            {
                GameObject holder = new GameObject("Masa_" + _tables.Count);
                holder.transform.SetParent(transform, false);
                holder.transform.localPosition = new Vector3(s.X, 0f, s.Z);

                if (TablePrefab != null)
                {
                    GameObject t = Instantiate(TablePrefab, holder.transform);
                    t.transform.localPosition = Vector3.zero;

                    // MASA KARE OLUYOR.
                    //
                    // Paketin modeli 0,82 x 0,45 m dikdortgen; dort
                    // oturagin dort kenara ESIT uzaklikta olmasi icin
                    // derinlik enine esitleniyor. Oran KURULUSTA
                    // olculuyor, elle yazilmiyor - prefab degisirse
                    // hesap da degisir.
                    t.transform.localScale = new Vector3(1f, 1f, TableSquareZ(t));
                    Retint(t);

                    // Tabla yuksekligi: SANDALYELER EKLENMEDEN once.
                    if (_tableTop <= 0f)
                    {
                        float en = -1f;
                        foreach (Renderer rr in t.GetComponentsInChildren<Renderer>())
                        {
                            float ust = rr.bounds.max.y - holder.transform.position.y;
                            if (ust > en) en = ust;
                        }
                        if (en > 0.2f) _tableTop = en + 0.01f;
                    }
                }

                // Dort sandalye, masanin dort yaninda ve masaya donuk.
                //
                // PropYaw SART: sandalye modeli karakterin tersine
                // bakiyor. Seat(k) oturagi Rot(k*90) * (0,0,-r) ile
                // koyuyor, yani k=0 masanin -Z tarafinda; misafir +Z'ye,
                // masaya bakiyordu - dogru. Sandalye ise ayni aciyla
                // masaya SIRTINI donuyordu ve misafir sirtligin icine
                // gomulmus gorunuyordu.
                if (ChairPrefab != null)
                {
                    for (int k = 0; k < Seats; k++)
                    {
                        GameObject chair = Instantiate(ChairPrefab, holder.transform);
                        chair.transform.localPosition = Seat(k);
                        chair.transform.localRotation =
                            Quaternion.Euler(0f, k * 90f + PropYaw, 0f);
                        Retint(chair);
                        _chairs[_tables.Count * Seats + k] = chair.transform;
                    }
                }

                _badges.Add(TableBadge.Create(holder.transform, _badgeMat));
                // Dokunma hedefi: odaya yaklasildiginda masa SECILEBILIR
                // olsun. Mudahaleler artik hedefi kendileri secmiyor.
                TableTouch.Attach(holder.transform, _tables.Count);
                _tables.Add(holder.transform);
            }
        }

        // =====================================================================
        /// <summary>
        /// Oturan musteriler. Masadaki KISI SAYISI kadar figur var: iki
        /// kisilik bir cift ile dort kisilik bir aile, salona bakildiginda
        /// ayirt edilebilmeli. Once her dolu masaya tek figur konuyordu ve
        /// dolu bir salon ile kalabalik bir salon ayni gorunuyordu.
        ///
        /// Figurler havuzdan geliyor ve gizlenerek geri donuyor.
        /// </summary>
        /// <summary>
        /// Bir oturagin onundeki tabak ve yemek.
        ///
        /// HAVUZLU: tabak bir kez kuruluyor, sonra yalnizca acilip
        /// kapaniyor. Her serviste yaratip yok etmek on dort masa x
        /// dort oturak = kare basina onlarca ayirma demekti.
        ///
        /// Yemek PAKETIN malzeme modellerinden (domates, kofte,
        /// peynir): kendi malzemeleri var, yani toplu cizime giriyorlar.
        /// Prosedurel bir disk daha ucuz gorunurdu ama her birine
        /// property block yazmak gerekirdi ve o, toplu cizimi bozuyor.
        /// </summary>
        private void TablePlate(int table, int seat, bool want)
        {
            int key = table * Seats + seat;
            GameObject go;
            if (!_tableFood.TryGetValue(key, out go))
            {
                if (!want || PlatePrefab == null) return;
                if (table >= _tables.Count || _tables[table] == null) return;

                go = Instantiate(PlatePrefab, _tables[table]);
                go.name = "MasaTabak" + seat;

                // Tabak oturagin ONUNDE ve masanin USTUNDE: oturak
                // yonunde 0,30 m, tabla yuksekligi olculerek.
                Vector3 yon = Seat(seat, 0.30f);
                go.transform.localPosition = new Vector3(yon.x, TableTop(table), yon.z);
                go.transform.localRotation = Quaternion.identity;
                Retint(go);

                // Yemek: tabagin uzerinde, masaya ve oturaga gore
                // degisen bir malzeme - her tabak ayni gorunmesin.
                if (IngredientPrefabs != null && IngredientPrefabs.Length > 0)
                {
                    GameObject y = IngredientPrefabs[(table * 3 + seat)
                                                     % IngredientPrefabs.Length];
                    if (y != null)
                    {
                        GameObject yemek = Instantiate(y, go.transform);
                        yemek.name = "Yemek";
                        yemek.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                        yemek.transform.localScale = Vector3.one * 0.55f;
                    }
                }
                _tableFood[key] = go;

            }

            if (go != null && go.activeSelf != want) go.SetActive(want);
        }

        /// <summary>
        /// Masanin tablasinin yuksekligi (yerel). Tahmin degil OLCUM:
        /// paket degisirse tabak havada ya da tablanin icinde kalmasin.
        /// </summary>
        private float TableTop(int table)
        {
            // OLCUM MASANIN KENDISINDEN, TASIYICIDAN DEGIL.
            //
            // Ilk yazim tasiyicinin butun cizicilerini tariyordu ve
            // tasiyicida sandalyeler (0,9 m) ve ROZET (2,45 m) de var:
            // tabaklar masanin degil rozetin hizasina, yani havaya
            // ciktilar. Goruntude "masada yemek yok, havada beyaz
            // lekeler var" diye gorundu.
            //
            // Deger masa kurulurken, sandalyeler eklenmeden once
            // olculuyor (BuildTables) - o anda tasiyicinin icinde
            // yalnizca masa var.
            return _tableTop > 0f ? _tableTop : 0.62f;
        }

        private float _tableTop;

        private readonly Dictionary<int, GameObject> _tableFood =
            new Dictionary<int, GameObject>();

        /// <summary>Su an yemek yiyen masa sayisi. Tani ve tur icin.</summary>
        public int EatingTables { get { return _yiyenSon; } }

        /// <summary>
        /// Su an masalarda GORUNEN tabak sayisi.
        ///
        /// "Cekirdek yiyor" ile "oyuncu yemegi goruyor" iki ayri iddia
        /// (bu proje bunu bulasikta ogrendi: sim tabaklari yikiyordu,
        /// ekranda kimse lavaboya varmiyordu). Tur ikisini birden
        /// soruyor.
        /// </summary>
        public int TablePlatesVisible
        {
            get
            {
                int n = 0;
                foreach (KeyValuePair<int, GameObject> kv in _tableFood)
                    if (kv.Value != null && kv.Value.activeSelf) n++;
                return n;
            }
        }

        private int _yiyenMasa, _yiyenSon;

        private void UpdateCustomers(Simulation sim)
        {
            _yiyenMasa = 0;
            for (int t = 0; t < _tables.Count; t++)
            {
                // Masanin durumu: asama ve kalan sabir.
                if (t < _badges.Count && _badges[t] != null)
                    _badges[t].Show(sim.TableStage(t), sim.TablePatienceBp(t),
                                    App != null && App.SelectedTable == t);

                int guests = Mathf.Min(sim.TableGuests(t), VisibleGuests);
                Vector3 masa = _tables[t].localPosition;

                // MASADA YEMEK VAR MI.
                //
                // Referansin dort karesinde de masalarin uzerinde tabak
                // ve yemek var; bizim masalarimiz servis edilirken bile
                // BOSTU. Oyuncunun "su masa yiyor" bilgisini alabilecegi
                // tek yer rozetti.
                //
                // Kosul CEKIRDEKTEN: yalnizca yemegi GELMIS masada tabak
                // var. Her masaya tabak koymak daha kolay olurdu ve
                // yalan olurdu - bekleyen masa ile yiyen masa ekranda
                // ayni gorunurdu.
                CustomerStage asama = sim.TableStage(t);
                bool yemekVar = asama == CustomerStage.Eating
                                || asama == CustomerStage.WaitingToPay;
                if (yemekVar) _yiyenMasa++;

                for (int k = 0; k < Seats; k++)
                {
                    int key = t * Seats + k;
                    bool want = SeatOrder(k) < guests;
                    bool has = _seated.TryGetValue(key, out GameObject figure);

                    // Dolu sandalye geride, bos sandalye masaya yapisik.
                    //
                    // YALNIZCA DEGISINCE yaziliyor: 14 masa x 4 sandalye
                    // = kare basina 56 transform yazimi ve Unity'nin
                    // ayarlayicisi esitlik kontrolu yapmiyor - her yazim
                    // alt agacin matrisini yeniden hesaplatiyor. Deger
                    // ise masa dolup bosalmadikca degismiyor.
                    bool oncekiDolu;
                    if (!_chairBack.TryGetValue(key, out oncekiDolu)
                        || oncekiDolu != want)
                    {
                        _chairBack[key] = want;
                        if (_chairs.TryGetValue(key, out Transform chairT)
                            && chairT != null)
                            chairT.localPosition =
                                Seat(k, want ? SeatRadiusUsed : SeatRadius);
                    }

                    TablePlate(t, k, want && yemekVar);

                    if (want && !has)
                    {
                        figure = Take();
                        if (figure == null) continue;

                        // KAPIDAN GIRIYOR.
                        //
                        // Once oturaga ISINLANIYORDU: masa dolunca figur
                        // bir karede sandalyenin uzerinde beliriyordu.
                        // Simulasyon zaten "musteri geldi" ile "masaya
                        // oturdu" arasinda bir sure tutuyor; o sure
                        // ekranda hicbir sey anlatmiyordu.
                        //
                        // Figur ARTIK MASANIN COCUGU DEGIL: yurumek
                        // dunya uzayinda oluyor ve masaya baglanmis bir
                        // figur, masanin yerel uzayinda yuruyordu.
                        // Oturak konumu artik masanin konumuna eklenerek
                        // hesaplaniyor.
                        // SANDALYE YERINDE, FIGUR ONDE: oturak yaricapi
                        // sandalyenin yeri; figur ondan SitForward kadar
                        // masaya dogru kayiyor.
                        Vector3 oturak = masa + Seat(k, SeatRadiusUsed - SitForward)
                                       + new Vector3(0f, SitLift, 0f);
                        float yaw = k * 90f;

                        figure.transform.SetParent(transform, false);
                        figure.SetActive(true);

                        Walker w = WalkerOf(figure);
                        Figure f = FigureOf(figure);
                        // SOKAKTA BELIRIYOR, KAPININ ONUNDE DEGIL.
                        //
                        // Kapinin onunde belirmek "geldi" degil "belirdi"
                        // diye okunuyordu. Artik kaldirimda, kapidan
                        // birkac metre uzakta beliriyor ve yuruyerek
                        // geliyor - kapidan girme anini izlemek
                        // mumkun.
                        w.Warp(Paths.Street(t), 0f);

                        Paths.FromStreet(_path, oturak);
                        w.GoTo(_path, yaw, () => Pose(f, Figure.Pose.Sit));

                        // Onizlemede (editor goruntusu) yuruyus YOK:
                        // tek kare orneklenecegi icin herkes kapida
                        // durur ve salon bos cikardi.
                        if (PreviewPoses)
                        {
                            w.Warp(oturak, yaw);
                            Pose(f, Figure.Pose.Sit);
                        }

                        _seated[key] = figure;
                    }
                    else if (!want && has)
                    {
                        _seated.Remove(key);
                        SendHome(figure);
                    }
                }
            }

            UpdateQueue(sim);

            // Cikanlar: kapiya varinca havuza donuyorlar.
            for (int i = _leaving.Count - 1; i >= 0; i--)
            {
                GameObject go = _leaving[i];
                if (go == null) { _leaving.RemoveAt(i); continue; }
                Walker w = WalkerOf(go);
                if (w.Moving) continue;
                _leaving.RemoveAt(i);
                Give(go);
            }
            _yiyenSon = _yiyenMasa;
        }

        /// <summary>
        /// MASA BEKLEYENLER. Kapinin yaninda duruyorlar.
        ///
        /// Simulasyonda WaitingForTable diye bir asama var ve gorunum
        /// onu hic cizmiyordu: masasi olmayan musteri, sabri bitip
        /// kizgin cikana kadar EKRANDA YOKTU. Oyunun en pahali olayi
        /// (dolan salon) yalnizca aksam raporunda bir sutundu.
        ///
        /// Kac kisilik grup oldugu degil, KAC GRUP bekledigi
        /// gosteriliyor - kuyrukta herkesi cizmek kapinin onunu
        /// tikardi ve oyuncunun okumasi gereken sey sayi degil
        /// "sira var mi".
        /// </summary>
        private void UpdateQueue(Simulation sim)
        {
            int n = 0;
            for (int p = 0; p < Simulation.MaxParties && n < MaxQueue; p++)
            {
                if (sim.StageOf(p) != CustomerStage.WaitingForTable) continue;
                if (sim.TableOfParty(p) >= 0) continue;

                GameObject go;
                if (!_queued.TryGetValue(p, out go) || go == null)
                {
                    go = Take();
                    if (go == null) break;
                    go.transform.SetParent(transform, false);
                    go.SetActive(true);

                    Walker w0 = WalkerOf(go);
                    Figure f0 = FigureOf(go);
                    Vector3 yer = Paths.QueueSpot(n);

                    if (PreviewPoses)
                    {
                        w0.Warp(yer, 0f);
                        Pose(f0, Figure.Pose.Idle);
                    }
                    else
                    {
                        w0.Warp(Paths.Street(p), 0f);
                        _path.Clear();
                        _path.Add(Paths.Inside);
                        _path.Add(yer);
                        w0.GoTo(_path, 0f, () => Pose(f0, Figure.Pose.Idle));
                    }
                    _queued[p] = go;
                }
                n++;
            }

            // Kuyruktan cikanlar: masaya oturdularsa ya da gittilerse.
            _queueGone.Clear();
            foreach (KeyValuePair<int, GameObject> kv in _queued)
            {
                if (sim.StageOf(kv.Key) == CustomerStage.WaitingForTable
                    && sim.TableOfParty(kv.Key) < 0) continue;
                _queueGone.Add(kv.Key);
            }
            for (int i = 0; i < _queueGone.Count; i++)
            {
                GameObject go = _queued[_queueGone[i]];
                _queued.Remove(_queueGone[i]);
                // Masaya oturan grubun kendi figurleri ayrica
                // ciziliyor; kuyruktaki temsilci kapidan cikiyor.
                SendHome(go);
            }
        }

        /// <summary>
        /// Kapida en fazla kac grup gosterilecek. Fazlasi kapiyi
        /// tikiyor ve okunan sey "sira var" olmaktan cikip "kalabalik"
        /// oluyor.
        /// </summary>
        private const int MaxQueue = 4;

        private readonly Dictionary<int, GameObject> _queued =
            new Dictionary<int, GameObject>();
        private readonly List<int> _queueGone = new List<int>();

        /// <summary>
        /// Kalkan musteri: ayaga kalkip KAPIYA yuruyor, sonra havuza
        /// donuyor.
        ///
        /// Once masadan aninda siliniyordu. Bir musterinin gitmesi -
        /// hele kizgin gitmesi - oyunun en pahali olayi ve ekranda hic
        /// gorunmuyordu.
        /// </summary>
        private void SendHome(GameObject figure)
        {
            if (figure == null) return;

            if (PreviewPoses) { Give(figure); return; }

            Walker w = WalkerOf(figure);
            Vector3 su = figure.transform.localPosition;
            su.y = 0f;
            w.Warp(su, figure.transform.localEulerAngles.y);

            Paths.ToStreet(_path, su, _leaving.Count);
            GameObject captured = figure;
            w.GoTo(_path, float.NaN, () => { /* kapida: donguye kalir */ });
            _leaving.Add(captured);
        }

        private readonly List<GameObject> _leaving = new List<GameObject>();

        /// <summary>Yol noktalari icin TEK tampon: kare basina cop uretmiyor.</summary>
        private readonly List<Vector3> _path = new List<Vector3>(8);

        /// <summary>
        /// Figurun yurutucusu. Havuzdan gelen nesneye ilk kullanimda
        /// ekleniyor - prefablar uretilirken Walker yoktu ve prefab
        /// uretecini yeniden kosturmak butun varliklari dokunulmus
        /// gosterirdi.
        /// </summary>
        /// <summary>
        /// Su an KAC figur yuruyor. Turun sormasi icin.
        ///
        /// "Hareket var mi" sorusunun ekran goruntusuyle cevabi yok:
        /// tek kare, duran bir figurle yuruyen bir figuru ayni
        /// gosteriyor. Sayilabilir tek sey hareketin kendisi.
        /// </summary>
        /// <summary>
        /// Yolda olan figur sayisi.
        ///
        /// AYIRMA YOK. Once GetComponentsInChildren kullaniyordu ve her
        /// okumada ~60 elemanli yeni bir dizi ayiriyordu; otomatik tur
        /// bunu 1500 karelik bir donguDE HER KAREDE okuyor, yani olcum
        /// aracinin kendisi olctugu kare suresini bozuyordu. Sayilacak
        /// figurlerin hepsi zaten elde.
        /// </summary>
        public int MovingCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _staff.Count; i++) n += Moving(_staff[i]);
                foreach (KeyValuePair<int, GameObject> kv in _seated) n += Moving(kv.Value);
                foreach (KeyValuePair<int, GameObject> kv in _queued) n += Moving(kv.Value);
                for (int i = 0; i < _leaving.Count; i++) n += Moving(_leaving[i]);
                return n;
            }
        }

        private int Moving(GameObject go)
        {
            if (go == null || !go.activeSelf) return 0;
            Walker w = WalkerOf(go);
            return w != null && w.Moving ? 1 : 0;
        }

        private Walker WalkerOf(GameObject go)
        {
            Walker w = go.GetComponent<Walker>();
            if (w == null)
            {
                w = go.AddComponent<Walker>();
                w.Body = go.GetComponentInChildren<Figure>(true);
            }
            return w;
        }

        /// <summary>
        /// k numarali oturagin masa merkezine gore yeri. Sandalye ve figur
        /// ayni formulu kullaniyor; ayri yazildiklarinda figur sandalyenin
        /// icinde kaliyordu.
        /// </summary>
        /// <summary>
        /// Masanin derinligini enine esitleyen olcek carpani.
        /// Bir kez olculup saklaniyor: on dort masa icin on dort kez
        /// cizici taramak gereksiz.
        /// </summary>
        private float TableSquareZ(GameObject table)
        {
            if (_tableSquareZ > 0f) return _tableSquareZ;

            Renderer[] rs = table.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return 1f;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

            _tableSquareZ = b.size.z > 0.0001f
                ? Mathf.Clamp(b.size.x / b.size.z, 0.25f, 4f) : 1f;
            return _tableSquareZ;
        }

        private float _tableSquareZ;

        /// <summary>
        /// Sandalye govdeleri. Dolu olan GERI CEKILIYOR.
        ///
        /// Neden: iskelette DIZ YOK - bacak basina tek kemik var - yani
        /// bacak kalcadan asagi sarkmaktan baska bir sey yapamiyor.
        /// Figur minderin ustune dogru oturtulunca bu kez uyluklar
        /// minderin on kenarini kesiyordu ("dizleri sandalyenin icine
        /// girmis gibi").
        ///
        /// Cozum sandalyeyi oynatmak: bos sandalye masaya yapisik
        /// duruyor, oturulan sandalye geriye kayiyor - gercekte de
        /// oturmak icin sandalye geri cekilir. Bacaklar minderin
        /// onunde, bosluga sarkiyor.
        /// </summary>
        private readonly Dictionary<int, Transform> _chairs =
            new Dictionary<int, Transform>();

        /// <summary>Sandalyenin son konumu dolu muydu. Bkz. UpdateCustomers.</summary>
        private readonly Dictionary<int, bool> _chairBack = new Dictionary<int, bool>();

        private static Vector3 Seat(int k) { return Seat(k, SeatRadius); }

        /// <summary>
        /// Verilen yaricapta oturak yonu. Sandalye ile figur AYNI
        /// yonde ama farkli yaricapta duruyor: figur SitForward kadar
        /// one, yoksa sirtlik govdesinin icinden geciyor.
        /// </summary>
        private static Vector3 Seat(int k, float r)
        {
            // Rot(k*90) * (0,0,-r): k=0 -> -Z, k=1 -> -X,
            // k=2 -> +Z, k=3 -> +X.
            return Quaternion.Euler(0f, k * 90f, 0f)
                   * new Vector3(0f, 0f, -r);
        }

        /// <summary>
        /// Bu oturak kacinci sirada doluyor: once SOL ve SAG, sonra
        /// arka, en son kameraya sirtini donen on.
        ///
        /// Iki duzeltme birlikte:
        ///
        /// 1. Once sirayla doluyordu (0, 1, 2, 3) ve iki kisilik bir
        ///    grup YAN YANA oturuyordu: iki oturak arasi 0,88 m ve bu
        ///    paketin figurleri omuzdan neredeyse o kadar genis, yani
        ///    iki misafir tek bir kutleye donusuyordu. Karsilikli
        ///    oturunca aralarinda 1,24 m var - ayni masa, ayni
        ///    sandalyeler, sifir maliyet.
        ///
        /// 2. Karsilikli yetmiyor, EKSEN de onemli. Ilk denemede cift
        ///    0-2 idi, yani Z ekseni; kamera 34 derece egimle Z boyunca
        ///    baktigi icin iki figur ekranda UST USTE biniyordu.
        ///    Aralarindaki 1,24 m gorunmuyordu bile. 1-3 cifti X
        ///    ekseninde: ayni mesafe, ama ekranda yan yana.
        ///
        /// Kullanicinin cumlesi "ortayi cok sikisik gosteriyor" idi ve
        /// yakin plan goruntu sebebin BOY degil EN oldugunu gosterdi;
        /// bu, ene dokunmadan kalabaligi azaltan tek kaldirac.
        /// </summary>
        private static int SeatOrder(int k)
        {
            switch (k)
            {
                case 1: return 0;    // sol
                case 3: return 1;    // sag
                case 2: return 2;    // arka, yuzu kameraya donuk
                default: return 3;   // on, sirti kameraya donuk
            }
        }

        /// <summary>
        /// Personel figurleri. Ascilar mutfakta, salon kadrosu giriste.
        /// Bu dilimde hareket yok - amac KIMIN VAR OLDUGUNU gostermek.
        /// </summary>
        private void UpdateStaff(Simulation sim)
        {
            _staffCooks = sim.Cooks;
            int want = sim.Cooks + sim.SalonStaff;

            // DAMGA TOPLAMDAN DEGIL BILESIMDEN.
            //
            // Once yalnizca TOPLAM sayiya bakiliyordu ve ascilarla
            // garsonlar bagimsiz: oyuncu ayni sabah bir garson cikarip
            // bir asci alirsa toplam degismiyor, blok atlaniyor ve yeni
            // ascinin sira bileseni hic kurulmuyordu. O figur ne
            // pisiriyor ne servis yapiyor - giriste, eski yerinde,
            // hic kipirdamadan kaliyordu.
            // BULASIKCI SAYISI DA DAMGADA.
            //
            // Damga "bilesimden" diye yazilmisti ama bulasikci sayisi
            // disarida kalmisti: oyuncu bir garsonu bulasiga verdiginde
            // toplam da, asci sayisi da degismiyor - yani blok atlaniyor
            // ve KIYAFET eski rolde kaliyordu. Garson onlugu giymis bir
            // bulasikci, mekanigin kendisini gorunmez yapar.
            int damga = sim.Cooks * 1000000 + sim.SalonStaff * 1000
                        + sim.Dishwashers;

            if (damga != _staffBuilt)
            {
                while (_staff.Count > want)
                {
                    int last = _staff.Count - 1;
                    if (_staff[last] != null)
                    {
                        if (Application.isPlaying) Destroy(_staff[last]);
                        else DestroyImmediate(_staff[last]);
                    }
                    _staff.RemoveAt(last);
                    _staffFigure.RemoveAt(last);
                    // Dusen indeksin olcum kaydi da gidiyor: indeks
                    // yeniden kullanilinca eski ilerleme degeriyle
                    // karsilastirma yapilmasin.
                    _isKlip.Remove(last);
                    if (_staffTask.Count > last) _staffTask.RemoveAt(last);
                    if (_cookRoutine.Count > last) _cookRoutine.RemoveAt(last);
                    if (_washHold.Count > last) _washHold.RemoveAt(last);
                }
                while (_staff.Count < want && StaffPrefabs != null && StaffPrefabs.Length > 0)
                {
                    GameObject prefab = StaffPrefabs[_staff.Count % StaffPrefabs.Length];
                    GameObject go = Instantiate(prefab, transform);
                    _staff.Add(go);
                    // PASIF ALT NESNELER DE TARANIYOR (true).
                    //
                    // Walker.Body bunu zaten `true` ile ariyordu, bu
                    // satir ve FigureOf aramiyordu. Prefab'da Figure
                    // pasif bir dugumun altindaysa burasi null donuyor,
                    // CookRoutine.Init'e null geciliyor ve
                    // CookRoutine.Update sessizce "Bosta"ya dusuyordu:
                    // asci hicbir is yapmiyor, HATA DA VERMIYOR.
                    Figure fig = go.GetComponentInChildren<Figure>(true);
                    if (fig == null)
                        Debug.LogError("SORUNLAR: personel prefabinda Figure yok: "
                                       + go.name);
                    _staffFigure.Add(fig);
                    _staffTask.Add(int.MinValue);
                    _cookRoutine.Add(null);
                    _washHold.Add(0f);
                }

                // KIYAFET: asci kepi ve onluk.
                //
                // Sira bileseni gibi bu da kadro degisince yeniden
                // dagitiliyor - ayni GameObject bir gun asci, ertesi
                // gun garson olabiliyor.
                if (_block == null) _block = new MaterialPropertyBlock();

                // Olcum her kadro kurulusunda sifirdan: "giydirilen
                // personel N/M" satiri BU kadroyu anlatmali.
                Wardrobe.ResetCounters();

                for (int i = 0; i < _staff.Count; i++)
                {
                    // ROL: ascilar once, sonra BULASIKCILAR, sonra
                    // garsonlar. Bulasikci ayri bir rol degil - salon
                    // kadrosunun bulasiga verilmis kismi (SetDishwashers)
                    // ve listenin SONUNDAN sayiliyor, cunku cekirdek de
                    // nobeti oradan dagitiyor.
                    Wardrobe.Role rol;
                    if (i < sim.Cooks) rol = Wardrobe.Role.Cook;
                    else if (i >= _staff.Count - sim.Dishwashers)
                        rol = Wardrobe.Role.Dishwasher;
                    else rol = Wardrobe.Role.Waiter;
                    Wardrobe.Dress(_staff[i], rol, _floorMat, _block);
                }

                // SIRA BILESENI yalnizca ascilarda. Kadro degisince
                // kimin asci oldugu degisiyor, o yuzden her seferinde
                // yeniden dagitiliyor.
                for (int i = 0; i < _staff.Count; i++)
                {
                    CookRoutine cr = _staff[i].GetComponent<CookRoutine>();
                    if (i < sim.Cooks)
                    {
                        if (cr == null) cr = _staff[i].AddComponent<CookRoutine>();
                        cr.Init(WalkerOf(_staff[i]), _staffFigure[i],
                                i, KitchenPosts, IngredientPrefabs, PlatePrefab,
                                _plateSpot);
                        _cookRoutine[i] = cr;
                    }
                    else
                    {
                        if (cr != null)
                        {
                            cr.Cancel();
                            if (Application.isPlaying) Destroy(cr);
                            else DestroyImmediate(cr);
                        }
                        _cookRoutine[i] = null;
                    }
                }

                for (int i = 0; i < _staff.Count; i++)
                {
                    bool asci = i < sim.Cooks;

                    // ISINLAMADAN ONCE SIRAYI IPTAL ET.
                    //
                    // Warp yolu temizliyor ve Walker.Moving false
                    // oluyor; sira bunu "vardim" diye okuyup tavayi bos
                    // ocaga koyuyor, pisirme duruşunu evde oynuyordu.
                    if (i < _cookRoutine.Count && _cookRoutine[i] != null)
                        _cookRoutine[i].Cancel();

                    Vector3 ev = asci ? Paths.CookHome(i, Mathf.Max(1, sim.Cooks))
                                      : Paths.SalonHome(i - sim.Cooks,
                                                        Mathf.Max(1, sim.SalonStaff));
                    WalkerOf(_staff[i]).Warp(ev, asci ? 180f : 0f);
                    _staffTask[i] = int.MinValue;
                    if (i < _washHold.Count) _washHold[i] = 0f;
                }

                _staffBuilt = damga;
            }

            // PERSONEL HER KARE DEGIL, GOREVI DEGISINCE YOLA CIKIYOR.
            //
            // Simulasyon "garson 3 numarali masayla ilgileniyor", "asci
            // izgarada" diyor; nereye gidilecegi gorunum katmaninin isi.
            // Hedef ayni kaldigi surece yeni yol verilmiyor - yoksa figur
            // her karede yeniden baslar ve hic varmazdi.
            // CALISAN FIGURUN ANIMATOR'U ACIK KALIYOR - HER KAREDE.
            //
            // Bu satir once asagidaki dongunun ICINDE duruyordu ve o
            // dongu yalnizca GOREV DEGISINCE calisiyor: yani Animator
            // bir kez uyandiriliyor, bir saniye sonra kapaniyor ve asci
            // dograma klibinin ilk karesinde donup kaliyor. Ustelik
            // yanlislikla yalnizca onizleme dalindaydi - oyunda hic
            // cagrilmiyordu.
            //
            // "Duruş verildi" ile "animasyon isliyor" ayri iki sey; bu
            // dongu ikincisini sagliyor.
            for (int i = 0; i < _staff.Count; i++)
            {
                Figure af = _staffFigure[i];
                if (af == null) continue;
                Figure.Pose p = af.Current;
                if (p == Figure.Pose.Chop || p == Figure.Pose.Wash
                    || p == Figure.Pose.Serve || p == Figure.Pose.Carry)
                {
                    af.HoldAwake();

                    // IS KLIBI GERCEKTEN OYNUYOR MU.
                    //
                    // "Duruş verildi" ile "animasyon isliyor" ayri iki
                    // sey ve ikincisi ancak klip ILERLIYOR mu diye
                    // sorulunca olculuyor. Kullanicinin sikayeti tam
                    // buydu: "bulasikci bulasiklari ovalamiyor, asci
                    // yemekleri karistirmiyor". Sebep animator degil
                    // KLIPLERIN DONGUSUZ ice aktarilmasiydi - klip bir
                    // kez oynayip son karesinde duruyordu.
                    float ilerleme = af.ClipProgress;
                    if (ilerleme >= 0f)
                    {
                        float onceki;
                        if (_isKlip.TryGetValue(i, out onceki))
                        {
                            if (ilerleme > onceki + 0.0001f) WorkAnimAdvanced++;
                            else WorkAnimStalled++;
                        }
                        _isKlip[i] = ilerleme;
                    }
                }
                else _isKlip.Remove(i);
            }

            // ASCILAR HER KAREDE, GARSONLAR GOREV DEGISINCE.
            //
            // Ayri ayri olmak ZORUNDA: cekirdegin asci isi bir tikten
            // kisa surebiliyor ve "gorev degisti mi" diye bakan bir
            // dongu onu KACIRIYOR - olculdu, simulasyon on kez is verdi
            // ve gorunum sifir kez gordu. Asci sirasi zaten kendi
            // suresini isletiyor, o yuzden her karede "is var mi" diye
            // sormak ucuz ve dogru.
            for (int i = 0; i < _staff.Count && i < sim.Cooks; i++)
            {
                CookRoutine cr = i < _cookRoutine.Count ? _cookRoutine[i] : null;
                if (cr == null || cr.Busy) continue;

                int is_ = sim.CookTaskStation(i);
                if (is_ >= 0) cr.Begin(is_, StoveOf(is_));
                else
                {
                    Walker cw = WalkerOf(_staff[i]);
                    if (cw != null && !cw.Moving)
                        SendCookHome(i, sim, cw, _staffFigure[i]);
                }
            }

            for (int i = sim.Cooks; i < _staff.Count; i++)
            {
                bool asci = false;
                int sunucu = i - sim.Cooks + 1;

                // YIKAMA ZIYARETI BIR SURE TUTULUYOR.
                //
                // Cekirdegin yikama gorevi 2.000 ms ve oyun x4'te
                // kosuyor: yarim saniye. Lavaboya yuruyus ise bir kac
                // saniye - yani figur VARMADAN gorev bitiyor, gorunum
                // onu baska yere yolluyor ve oyuncu bulasigin
                // yikandigini HIC gormuyor. Olculdu: turun her
                // kosusunda "lavaboda gorulen 0".
                //
                // Ayni sinif hata ascida da vardi ve ayni sekilde
                // cozuldu (CookRoutine): gorunum kendi sirasini
                // isletiyor, cekirdegin anlik bayragini degil. Cekirdek
                // "yikandi" diyor; SUREYI gorunum anlatiyor.
                if (_washHold[i] > 0f)
                {
                    _washHold[i] -= Time.deltaTime;
                    if (_washHold[i] > 0f) continue;
                }

                // LAVABO AYRI BIR GOREV, "bosta" DEGIL.
                //
                // Yikamanin hedefi bir masa degil, yani SalonTaskTable
                // -1 donuyor - ve -1 "bosta" ile ayni sayi. Ayirt
                // edilmezse yikayan garson evine yollanir ve oyuncu
                // bulasigin yikandigini hic gormez.
                int gorev = sim.SalonWashing(sunucu) ? WashTask
                                                     : sim.SalonTaskTable(sunucu);

                if (gorev == _staffTask[i]) continue;
                _staffTask[i] = gorev;

                Walker w = WalkerOf(_staff[i]);
                Figure f = _staffFigure[i];
                if (w == null) continue;

                if (PreviewPoses)
                {
                    // Onizleme tek kare: yuruyus yok, durus yeter.
                    Pose(f, asci ? KitchenPose(gorev) : Figure.Pose.Carry);
                    continue;
                }

                ShowSponge(_staff[i], gorev == WashTask);

                if (gorev == WashTask)
                {
                    int idx = i;
                    // BULASIGA GIDIYOR, ELINDE KIRLI TABAKLARLA.
                    //
                    // Kullanicinin cumlesi: "garson yemek yenilen
                    // tabaklari alip bulasikcinin kirli tabak kismina
                    // biraksin". Tabaklar lavaboya varinca elinden
                    // birakiliyor - yigina eklenmeleri simulasyondan
                    // geliyor, burada yalnizca tasinmalari goruunuyor.
                    // TUTMA YOLA CIKARKEN BASLIYOR, VARISTA DEGIL.
                    //
                    // Ilk yazim sayaci varis geri cagrisinda basliyordu ve
                    // ISE YARAMADI - olculdu, uc kosuda da "lavaboda
                    // gorulen 0". Sebep: figur varmadan cekirdegin gorevi
                    // bitiyor, gorev degisiyor ve gorunum onu yolun
                    // ortasinda baska yere yolluyor. Yani varis geri
                    // cagrisi HIC calismiyordu.
                    //
                    // Simdi guvenlik suresiyle yola cikilyor (yuruyusu
                    // kimse kesmiyor), varinca sayac gercek bekleme
                    // suresine indiriliyor.
                    _washHold[i] = WashWalkTimeout;

                    ShowCarry(_staff[i], 2);
                    Paths.Between(_path, w.transform.localPosition, _washSpot);
                    GameObject govdeY = _staff[i];
                    w.GoTo(_path, 0f, () =>
                    {
                        Pose(f, Figure.Pose.Wash);
                        // Kirli yigin birakildi, ELDE TEK TABAK kaldi:
                        // yikanan tabak o. Bos elle ovalama hareketi
                        // yapan bir figur "yikiyor" diye okunmuyor.
                        ShowCarry(govdeY, 1);
                        // Lavaboda GORULEBILIR bir sure duruyor.
                        _washHold[idx] = WashVisitSeconds;
                    });
                    continue;
                }

                if (gorev < 0)
                {
                    ShowCarry(_staff[i], 0);
                    // Bosta: evine donuyor.
                    Vector3 ev = asci ? Paths.CookHome(i, Mathf.Max(1, sim.Cooks))
                                      : Paths.SalonHome(i - sim.Cooks,
                                                        Mathf.Max(1, sim.SalonStaff));
                    Paths.Between(_path, w.transform.localPosition, ev);

                    // NaN = son adimin yonunde kal. Sabit bir aci
                    // yazmak, bosta duran personeli hep ayni yone
                    // baktiriyordu - kullanicinin gordugu seylerden biri.
                    w.GoTo(_path, float.NaN, () => Pose(f, Figure.Pose.Idle));
                    continue;
                }

                else
                {
                    // GARSON MASAYA GIDIYOR, YEMEK TASIYORSA ELINDE
                    // TABAKLA.
                    if (gorev >= _tables.Count) continue;
                    Vector3 masa = _tables[gorev].localPosition;
                    Vector3 yan = Paths.BesideTable(masa);
                    Paths.Between(_path, w.transform.localPosition, yan);

                    // KAC TABAK: bu garsonun tasidigi bir, arti ayni
                    // anda yemek bekleyen diger masalar - tepsi
                    // kapasitesine kadar. Sayi simulasyondan okunuyor.
                    bool tasiyor = sim.SalonCarrying(i - sim.Cooks + 1);
                    ShowCarry(_staff[i], tasiyor ? 1 + WaitingForFood(sim, gorev) : 0);

                    GameObject govde = _staff[i];
                    w.GoTo(_path, Paths.FaceFrom(yan, masa), () =>
                    {
                        Pose(f, Figure.Pose.Carry);
                        // Masaya VARINCA tabak masada kaliyor: servis
                        // edilmis olmasinin goruntusu bu.
                        ShowCarry(govde, 0);
                    });
                }
            }
        }

        /// <summary>Ascinin bosta durdugu yere donmesi.</summary>
        private void SendCookHome(int index, Simulation sim, Walker w, Figure f)
        {
            Vector3 ev = Paths.CookHome(index, Mathf.Max(1, sim.Cooks));
            if ((w.transform.localPosition - ev).sqrMagnitude < 0.09f) return;
            Paths.Between(_path, w.transform.localPosition, ev);
            w.GoTo(_path, float.NaN, () => Pose(f, Figure.Pose.Idle));
        }

        /// <summary>
        /// Bu masanin disinda kac masa daha yemek bekliyor.
        ///
        /// Garsonun tepsisine kac tabak konacagini belirliyor: bos bir
        /// salonda tepsi tasimak anlamsiz, dolu bir salonda tek tabakla
        /// gidip gelmek de oyle.
        /// </summary>
        private static int WaitingForFood(Simulation sim, int exceptTable)
        {
            int n = 0;
            for (int t = 0; t < sim.TableCount; t++)
            {
                if (t == exceptTable) continue;
                if (sim.TableStage(t) == CustomerStage.WaitingForFood) n++;
            }
            return n;
        }

        /// <summary>Istasyonun ocagi (nesne olarak). Tava oraya konuyor.</summary>
        private Transform StoveOf(int station)
        {
            if (_stoves.Count == 0) return null;
            int i = (station < 0 ? 0 : station) % _stoves.Count;
            return _stoves[i] != null ? _stoves[i].transform : null;
        }

        /// <summary>
        /// Istasyonun ocaginin yeri. Ocak yoksa duvar tarafi.
        ///
        /// Esleme UpdateAppliances ile AYNI: ocak i, i'inci ve
        /// (i+3)'uncu istasyonu temsil ediyor. Ayri yazilsaydi asci
        /// yanmayan ocaga bakiyor olurdu.
        /// </summary>
        private Vector3 StovePos(int station, Vector3 fallbackFrom)
        {
            if (_stoves.Count > 0)
            {
                int i = (station < 0 ? 0 : station) % _stoves.Count;
                if (_stoves[i] != null)
                    return _stoves[i].transform.localPosition;
            }
            // Ocak yoksa arkaya (duvara) donuk: tezgah orada.
            return fallbackFrom + new Vector3(0f, 0f, 1f);
        }

        /// <summary>
        /// Istasyonun mutfak isi. Uc ayri hareket, istasyona gore sabit:
        /// ayni asci ayni ocakta hep ayni isi yapiyor, yani goruntu
        /// titremiyor ama mutfakta uc farkli sey oluyor.
        /// </summary>
        private static Figure.Pose KitchenPose(int station)
        {
            int i = station < 0 ? 0 : station;
            switch (i % 3)
            {
                case 0: return Figure.Pose.Chop;
                case 1: return Figure.Pose.Wash;
                default: return Figure.Pose.Serve;
            }
        }

        /// <summary>
        /// OCAKLAR SIMULASYONA GORE YANIYOR.
        ///
        /// Mutfak ekranin ucte birini kapliyor ve icinde hicbir sey
        /// olmuyordu. Cekirdek her an hangi istasyonda kac tabak
        /// pistigini biliyor (StationLoad); o bilgi hicbir yere
        /// cizilmiyordu. Bir yonetim oyununda "mutfak sikisti" en sik
        /// verilen karar ve oyuncunun onu gorecegi tek yer mutfagin
        /// kendisi.
        ///
        /// Ocak sayisi ile istasyon sayisi ayni DEGIL: uc ocak, alti
        /// istasyon. Ocak i, i'inci, (i+3)'uncu... istasyonlari
        /// temsil ediyor - ayni esleme ascinin gittigi tezgahi secen
        /// Paths.KitchenPost'ta da var, yani asci yanan ocaga gidiyor.
        /// </summary>
        private void UpdateAppliances(Simulation sim)
        {
            if (_stoves.Count == 0) return;

            int n = _stoves.Count;
            for (int i = 0; i < n; i++)
            {
                bool calisiyor = false;
                // Ust sinir SABIT: App onizlemede null olabiliyor ve
                // StationLoad sinir disini zaten 0 donduruyor.
                for (int st = i; st < 16; st += n)
                {
                    if (sim.StationLoad(st) <= 0) continue;
                    calisiyor = true;
                    break;
                }
                _stoves[i].SetWorking(calisiyor);
            }
        }

        /// <summary>
        /// Garsonun elindeki tabagi acar/kapatir.
        ///
        /// Tabak KEMIGE degil govdeye bagli. Bir el kemigi aramak
        /// (ad eslesmesiyle) paket degisince sessizce bozulur; govdenin
        /// onunde, gogus hizasinda duran bir tabak bu kamera
        /// mesafesinden "tepsi tasiyor" diye okunuyor ve hicbir
        /// varsayim tasimiyor.
        ///
        /// Nesne bir kez kuruluyor, sonra yalnizca gizlenip
        /// gosteriliyor: servis boyunca onlarca kez yarat-yok et,
        /// mobilde gorunur cop demek.
        /// </summary>
        /// <summary>
        /// GARSONUN ELINDEKI: TEK TABAK YA DA TEPSI.
        ///
        /// Kullanicinin istegi: "garson her seferinde belirli sayida
        /// yemek ve icecek tasiyabilsin, tepsi kullanip kullanmamasi da
        /// bu sayiyi etkilesin".
        ///
        /// KURAL: tek tabak ELDE tasiniyor; iki ve ustu TEPSIYLE. Tepsi
        /// kapasitesi TrayCapacity. Bekleyen masa sayisi bundan azsa
        /// garson yalnizca o kadarini aliyor - eli bos tepsi tasimak,
        /// "verimli calisiyor" degil "bos geziyor" diye okunurdu.
        ///
        /// NEDEN GORUNUM KATMANINDA: cekirdegin salon kapasitesi kisi-gun
        /// modeli (docs'taki kadro olcegi), sefer basina tasima degil.
        /// Buraya gercek bir kisit koymak ekonomiyi degistirir ve
        /// altmis gunluk dengenin yeniden cozulmesini gerektirir. Sayi
        /// SIMULASYONDAN okunuyor (kac masa yemek bekliyor), yani
        /// uydurma degil; yalnizca kisit degil GORUNTU.
        /// </summary>
        private void ShowCarry(GameObject staff, int count)
        {
            if (staff == null || PlatePrefab == null) return;
            count = Mathf.Clamp(count, 0, TrayCapacity);

            Transform tepsi = staff.transform.Find("Tepsi");
            if (tepsi == null && count > 1) tepsi = BuildTray(staff.transform);
            if (tepsi != null) tepsi.gameObject.SetActive(count > 1);

            for (int i = 0; i < TrayCapacity; i++)
            {
                string ad = "Tabak" + i;
                Transform t = staff.transform.Find(ad);
                bool istenen = i < count;

                if (t == null)
                {
                    if (!istenen) continue;
                    GameObject p = Instantiate(PlatePrefab, staff.transform);
                    p.name = ad;
                    p.transform.localRotation = Quaternion.identity;
                    t = p.transform;
                }

                // Tek tabak ELDE (biraz alcak), coklu tabak TEPSIDE
                // (biraz yuksek ve yana dizili).
                t.localPosition = count > 1
                    ? new Vector3((i - (count - 1) * 0.5f) * 0.17f, 0.66f, 0.30f)
                    : new Vector3(0f, 0.62f, 0.28f);

                if (t.gameObject.activeSelf != istenen)
                    t.gameObject.SetActive(istenen);
            }
        }

        /// <summary>Tepsi: paket tasimadigi icin tek kutudan.</summary>
        private Transform BuildTray(Transform staff)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Tepsi";
            go.transform.SetParent(staff, false);
            go.transform.localPosition = new Vector3(0f, 0.63f, 0.30f);
            go.transform.localScale = new Vector3(0.52f, 0.025f, 0.34f);

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = _floorMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (_block == null) _block = new MaterialPropertyBlock();
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, new Color(0.34f, 0.26f, 0.19f));
            r.SetPropertyBlock(_block);
            return go.transform;
        }

        /// <summary>
        /// Tepsiyle tasinabilecek en fazla tabak.
        ///
        /// Uc: dorduncusu figurun eninden tasiyor (tabak capi 0,17 m,
        /// figur eni 0,85 m) ve 34 derecelik bakista tepsi bir levha
        /// gibi okunuyor.
        /// </summary>
        public const int TrayCapacity = 3;

        /// <summary>Su an tepsiyle tasiyan garson sayisi. Tur icin.</summary>
        /// <summary>Sahnede kurulu personel sayisi. Tepsi olcumunun ust siniri.</summary>
        public int StaffCount { get { return _staff.Count; } }

        public int TrayCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _staff.Count; i++)
                {
                    if (_staff[i] == null) continue;
                    Transform t = _staff[i].transform.Find("Tepsi");
                    if (t != null && t.gameObject.activeSelf) n++;
                }
                return n;
            }
        }

        /// <summary>
        /// Mutfaktaki calisma noktasi sayisi. Ocak sirasi uc parca; asci
        /// istasyona gore bunlar arasinda dolasiyor.
        /// </summary>
        private const int KitchenPosts = 3;

        /// <summary>Her personelin en son gordugu gorev. int.MinValue: henuz yok.</summary>
        private readonly List<int> _staffTask = new List<int>();

        /// <summary>Her ascinin pisirme sirasi. Garsonlarda null.</summary>
        private readonly List<CookRoutine> _cookRoutine = new List<CookRoutine>();

        /// <summary>Ascinin elindeki malzemeler icin prefablar.</summary>
        public GameObject[] IngredientPrefabs;

        /// <summary>
        /// Figurun durusunu ayarlar.
        ///
        /// Figure basvurusu CAGIRANDAN geliyor, her seferinde aranmiyor:
        /// bilesen prefab kokunde degil ICINDE (klip yollari FBX kokune
        /// gore yazilmis) ve GetComponentInChildren alt agaci geziyor.
        /// </summary>
        private void Pose(Figure f, Figure.Pose pose)
        {
            if (f == null) return;
            if (PreviewPoses) f.Sample(pose, PreviewTime);
            else f.Set(pose);
        }

        // --- havuz ------------------------------------------------------------
        private GameObject Take()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (_pool[i] != null && !_pool[i].activeSelf) return _pool[i];

            if (CustomerPrefabs == null || CustomerPrefabs.Length == 0) return null;

            // Figur secimi HAVUZ SIRASINA gore, rastgele degil: rastgelelik
            // cekirdegin akislarindan gelmeli, gorunum katmani kendi zarini
            // atmamali (docs/23 tekrar oynatma).
            GameObject prefab = CustomerPrefabs[_pool.Count % CustomerPrefabs.Length];
            GameObject go = Instantiate(prefab, transform);
            go.SetActive(false);
            _pool.Add(go);
            return go;
        }

        /// <summary>
        /// Havuzdaki figurun Figure bileseni. Bir kez cozulup sozlukte
        /// tutuluyor: havuz sabit boyutlu ve ayni nesneler gun boyunca
        /// defalarca oturuyor.
        /// </summary>
        private Figure FigureOf(GameObject go)
        {
            if (_figureOf.TryGetValue(go, out Figure f)) return f;
            f = go.GetComponentInChildren<Figure>(true);
            _figureOf[go] = f;
            return f;
        }

        private readonly Dictionary<GameObject, Figure> _figureOf =
            new Dictionary<GameObject, Figure>();

        private void Give(GameObject go)
        {
            if (go == null) return;

            // Figur havuza donerken DURUSU UNUTUYOR.
            //
            // Animator gecis bitince kapaniyor (Figure.Update); havuzdan
            // yeniden alinan bir figur ayni duruşu isterse Set() erken
            // cikiyor ve Animator kapali kaliyordu - yani figur eski
            // pozunda donuyor ama yeni bir gecis hic baslamiyor.
            Figure f = FigureOf(go);
            if (f != null) f.Release();

            go.SetActive(false);
            go.transform.SetParent(transform, false);
        }
    }
}
