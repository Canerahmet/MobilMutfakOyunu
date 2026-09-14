using System.Collections.Generic;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// SOKAKTAN GECENLER.
    ///
    /// Kullanicinin cumlesi: "sokaktan gecen karakterlerin tamami
    /// lokantaya gitmesin, bazilari yola devam etsin veya kendi
    /// aralarinda konusup sonra yola devam etsinler".
    ///
    /// Neden onemli: her gecen kisinin ICERI girdigi bir sokak, sokak
    /// degil bir kuyruk. Gecip giden birinin varligi, restoranin
    /// KENDISINI bir secim haline getiriyor - burasi bir dunyanin
    /// icinde ve herkes buraya gelmiyor.
    ///
    /// SIMULASYONA HIC DOKUNMUYOR. Bunlar musteri DEGIL: cekirdek
    /// onlari bilmiyor, sayilari ekonomiyi etkilemiyor, hicbiri masaya
    /// oturmuyor. Gorunum katmaninin susu - ve o yuzden rastgeleligi de
    /// kendi tohumunda, cekirdegin RNG'sine dokunmadan.
    ///
    /// BIRBIRLERININ ICINDEN GECMIYORLAR. Ilk yazimda geciyorlardi ve
    /// kullanici bunu gordu. Iki katmanli cozum:
    ///
    ///   1. SERIT - karsi yonde yuruyen iki figur ayni cizgide olursa
    ///      karsilasma kacinilmaz. Iki ayri serit (Paths.PavementLane)
    ///      bu durumu YAPISAL olarak yok ediyor.
    ///   2. ITISME - ayni seritte birbirine yetisenler ve sohbet icin
    ///      duranlar icin, her karenin sonunda govdeleri ayiran kucuk
    ///      bir duzeltme (LateUpdate). Yol bulma degil: yalnizca iki
    ///      govdenin ust uste binmesini engelliyor.
    ///
    /// Ikisi ayri olmali: yalnizca itisme birakilsa karsidan gelen ikili
    /// birbirini frenleyerek gecerdi (kaldirim tikanir); yalnizca serit
    /// birakilsa ayni yondeki hizli biri yavas olanin icinden gecerdi.
    ///
    /// UCUZ: en fazla bes figur; itisme O(n^2) ama n = 5 + disarida
    /// bekleyen birkac musteri.
    /// </summary>
    public sealed class StreetLife : MonoBehaviour
    {
        /// <summary>Ayni anda sokaktaki en fazla kisi.</summary>
        private const int MaxWalkers = 5;

        /// <summary>
        /// Sokagin uydugu en yuksek zaman carpani.
        ///
        /// Oyuncu x16'ya bastiginda salon on alti kat hizli akiyor ama
        /// SOKAK akmiyor: sokaktan gecenlerin simulasyonda karsiligi yok
        /// ve "hizli ileri sardim" demek "kaldirim bosaldi" demek
        /// olmamali.
        ///
        /// Asil sebep bir hataydi ve olculerek bulundu: Walker, carpan
        /// TeleportAbove'u (4,5) gectiginde yuruyusu cizmeyip yolun
        /// sonuna isinliyor. Sokakta yolun sonu yalnizca IKI nokta
        /// (saga gidenin cikisi, sola gidenin cikisi), yani yuksek hizda
        /// bes yaya o iki noktada ust uste yigiliyordu - uc + iki, tam
        /// dort ic ice cift. Turun "yayalar birbirinin icinden gecmiyor"
        /// kontrolu bunu ilk kosusunda yakaladi.
        ///
        /// 4: TeleportAbove'un hemen altinda, yani isinlanma hic
        /// devreye girmiyor.
        /// </summary>
        public const float MaxSpeed = 4f;

        /// <summary>Sokagin su anki zaman carpani.</summary>
        private static float Clock
        {
            get { return Mathf.Min(MaxSpeed, Mathf.Max(1f, Walker.GameSpeed)); }
        }

        /// <summary>Sohbetin suresi (sn).</summary>
        private const float ChatMin = 2.2f;
        private const float ChatMax = 4.5f;

        /// <summary>Iki kisinin sohbet icin yeterince yaklasmasi (m).</summary>
        private const float ChatRange = 1.25f;

        /// <summary>
        /// Sohbetin EN YAKIN mesafesi (m).
        ///
        /// Alt sinir olmazsa dip dibe duran iki figur konusuyor degil
        /// ic ice gecmis gibi duruyor. Personal'den BUYUK olmali: kucuk
        /// olsa itisme, sohbet suren boyunca iki konusani birbirinden
        /// uzaklastirmaya calisirdi.
        /// </summary>
        private const float ChatNear = 0.75f;

        /// <summary>
        /// Iki govdenin merkezleri arasi EN AZ mesafe (m).
        ///
        /// 0,68 OLCULDU: Editor/PlacementAudit figurun yukseklige gore
        /// yatay profilini basiyor ve kollar disinda en genis bant BAS
        /// hizasi (y 0,61-0,72) - 0,67 m. Bu paketin oranlarinda kafa
        /// govdenin ucte biri, yani omuzlardan (0,58) genis; omuz
        /// olcusu kullanilsa iki bas 7 cm ortusurdu.
        ///
        /// ILK OLCUM YANLIS SEYI OLCTU ve sayi absurt cikinca anlasildi:
        /// sinirlayici kutu 1,14 m veriyordu - bir metre boyunda bir
        /// figur icin ayak izi olamayacak bir sayi. Kutu KOL ACIKLIGINI
        /// olcuyor ve bu paketin figurlerinin kollari govdeden acik
        /// duruyor. Ustelik ikinci olcum de yanildi: kalca hizasina
        /// (y 0,18-0,50) bakti ve 1,08 m buldu, cunku bu oranlarda ELLER
        /// de o hizada. Dogru bandi bulmanin tek yolu butun profili
        /// basmak oldu.
        ///
        /// Serit araligi da ayni sayidan geliyor (Paths.LaneHalf * 2 =
        /// 0,60): karsidan gelen iki govde birbirine degmeden geciyor.
        /// Ikisi ayni sayiyi paylasiyor ama iki AYRI ise yariyor - serit
        /// karsilasmayi yapisal olarak onluyor, bu esik ise ayni seritte
        /// yetisenleri ve duranlari ayiriyor.
        /// </summary>
        public const float Personal = 0.68f;

        /// <summary>
        /// Itismenin en hizli duzeltme hizi (m/sn) - TAVAN, hedef degil.
        ///
        /// Duzeltmenin kendisi ORANTILI: iki govde ne kadar ic ice
        /// girmisse o kadar ayriliyor, yani teget gecerken duzeltme
        /// sifira yakin (titreme yok) ve gercekten ust uste binmislerse
        /// tek karede acilir. Bu tavan yalnizca beklenmedik bir sicramayi
        /// sinirliyor.
        ///
        /// 1,6'dan 4,0'a cikti ve sebebi olculdu: turun ikinci kosusunda
        /// "en kotu 1 cift" cikti. Sabit hizli itisme, kaldirimi capraz
        /// gecen bir MUSTERININ ittigi yayaya yetisemiyordu - musteri
        /// itiliyor degil (yolu simulasyona bagli), yaya iki yandan
        /// birden bastiriliyor ve zincirin sonu bir kare geriden
        /// cozuluyordu.
        /// </summary>
        private const float PushSpeed = 4.0f;

        /// <summary>
        /// Karsilikli ayirmada bir karedeki EN FAZLA gevseme gecisi.
        ///
        /// Dongu erken cikiyor: bir gecis hicbir cifti duzeltmediyse
        /// durum yakinsamis demektir. Bes kisi ve on cift icin gecis
        /// basina maliyet on mesafe karsilastirmasi - tavani yuksek
        /// tutmanin bedeli yok, dusuk tutmanin bedeli cozulmemis bir
        /// zincir.
        /// </summary>
        private const int Passes = 6;

        /// <summary>
        /// Sokak lambasi diregine EN AZ mesafe (m).
        ///
        /// Direk ince (9 cm): govdenin yarisi (0,34) + diregin yarisi
        /// (0,045) + pay. Personal'den KUCUK olmasi sart - direk
        /// kaldirimin dis kenarinda ve dis serit ona 0,45 m uzakta; esik
        /// Personal olsa dis seritteki her yaya SUREKLI iceri dogru
        /// itilirdi, yani serit bir ise yaramazdi.
        /// </summary>
        private const float PostClear = 0.40f;

        /// <summary>
        /// Itismenin en az YANAL orani.
        ///
        /// Neden: ayni seritte arkadan yetisen biri icin iki govdeyi
        /// ayiran dogru neredeyse tamamen yurume eksenindedir. O yonde
        /// itmek one gideni hizlandirip arkadakini yavaslatir - ikisi
        /// hic yan yana gelmez, yani kimse kimseyi GECEMEZ. Itismeyi
        /// yana dogru egince arkadaki yana kayip geciyor; serit sinirlari
        /// da onu sonra yerine cekiyor.
        /// </summary>
        private const float SideBias = 0.62f;

        private sealed class Pedestrian
        {
            public GameObject Body;
            public Walker Walk;
            public Figure Fig;
            public float ChatLeft;
            public int Dir;              // +1 saga, -1 sola
            public float NextChat;       // bu sureden once tekrar sohbet etmesin
            public Pedestrian Partner;   // sohbet ettigi kisi
            public int Side;             // itismede hangi yana kaydigi (+1/-1)
        }

        private readonly List<Pedestrian> _people = new List<Pedestrian>();
        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly List<Vector3> _others = new List<Vector3>();
        private readonly List<Vector3> _posts = new List<Vector3>();
        private System.Random _rng;
        private RestaurantView _view;
        private int _postStamp = -1;

        /// <summary>Sokakta su an kac kisi var. Turun sorabilmesi icin.</summary>
        public int Count { get { return _people.Count; } }

        /// <summary>Itismenin bildigi engel (direk) sayisi.</summary>
        public int PostsKnown { get { return _posts.Count; } }

        /// <summary>Su an sohbet eden kac kisi var. Turun sorabilmesi icin.</summary>
        public int Chatting
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _people.Count; i++)
                    if (_people[i].ChatLeft > 0f) n++;
                return n;
            }
        }

        /// <summary>
        /// CIZILEN karede ic ice gecmis kac IKILI vardi.
        ///
        /// "Birbirlerinin icinden geciyorlar" sikayetinin OLCULEBILIR
        /// hali: sifirdan buyukse gecmisler.
        ///
        /// HESAPLANMIYOR, SAKLANIYOR - ve sebebi bir yanlis olcum oldu.
        /// Once bu ozellik soruldugu anda hesapliyordu ve tur rastgele
        /// "en kotu 1 cift" diye kirmiziya dusuyordu. Sebep sokakta
        /// degil OLCUM ANINDAYDI: Unity'de `yield return null` bir
        /// coroutine'i Update ile LateUpdate ARASINDA uyandiriyor, yani
        /// tur konumlari Walker tasidiktan SONRA ama itisme duzelttiginden
        /// ONCE okuyordu. Olculen kare hic cizilmiyordu.
        ///
        /// Simdi sayi LateUpdate'in sonunda, duzeltme bittikten sonra
        /// yaziliyor: yani oyuncunun gordugu karenin sayisi.
        /// </summary>
        public int Overlaps { get { return _overlaps; } }

        private int _overlaps;
        private int _postOverlaps;

        /// <summary>Cizilen karede ic ice gecme sayilir.</summary>
        private void Olc()
        {
            // ESIGIN ALTINDA bir pay: itisme figurleri tam esikte
            // durduruyor ve kayan nokta gurultusu kontrolu kararsiz
            // yapardi.
            float esik = Personal - 0.06f;
            int n = 0;
            for (int i = 0; i < _people.Count; i++)
            {
                if (_people[i].Body == null) continue;
                Vector3 a = _people[i].Body.transform.localPosition;
                for (int j = i + 1; j < _people.Count; j++)
                {
                    if (_people[j].Body == null) continue;
                    Vector3 d = a - _people[j].Body.transform.localPosition;
                    d.y = 0f;
                    if (d.sqrMagnitude < esik * esik) n++;
                }
            }
            _overlaps = n;

            // DIREK YOKSA SIFIR DEGIL, BIR.
            //
            // Once "0" donuyordu ve kontrol, direkler hic yuklenmemis
            // olsa da YESIL kaliyordu - yani itismenin calistigini degil,
            // olcecek bir sey olmadigini olcuyordu.
            if (_posts.Count == 0) { _postOverlaps = 1; return; }

            float pEsik = PostClear - 0.06f;
            n = 0;
            for (int i = 0; i < _people.Count; i++)
            {
                if (_people[i].Body == null) continue;
                Vector3 a = _people[i].Body.transform.localPosition;
                for (int k = 0; k < _posts.Count; k++)
                {
                    Vector3 d = a - _posts[k];
                    d.y = 0f;
                    if (d.sqrMagnitude < pEsik * pEsik) { n++; break; }
                }
            }
            _postOverlaps = n;
        }

        /// <summary>
        /// CIZILEN karede bir sokak lambasi diregine girmis kac yaya
        /// vardi.
        ///
        /// Neden ayri olculuyor: yayalarin BIRBIRINDEN kacmasi ile
        /// diregin icinden gecmemesi ayri iki mekanizma (biri karsilikli
        /// itisme, oteki tek tarafli) ve tek bir sayi ikisini birden
        /// olcemez.
        /// </summary>
        public int PostOverlaps { get { return _postOverlaps; } }

        // =====================================================================
        /// <summary>
        /// Sokagi doldurur. prefabs: musteri figurleri (ayni paket).
        /// </summary>
        public void Build(Transform root, GameObject[] prefabs, int seed)
        {
            _rng = new System.Random(seed);

            Clear();
            if (prefabs == null || prefabs.Length == 0) return;

            for (int i = 0; i < MaxWalkers; i++)
            {
                GameObject go = Instantiate(prefabs[i % prefabs.Length], root);
                go.name = "Yoldan";
                Pedestrian p = new Pedestrian
                {
                    Body = go,
                    Walk = go.GetComponent<Walker>() ?? go.AddComponent<Walker>(),
                    Fig = go.GetComponentInChildren<Figure>(true),
                    Dir = (i % 2 == 0) ? 1 : -1,
                    Side = (i % 2 == 0) ? 1 : -1,
                };
                // Walker'in govdesi baglanmali: GoTo/Update figurun
                // durusunu onun uzerinden suruyor. Baglanmazsa yuruyus
                // durusu sessizce devre disi kaliyor.
                p.Walk.Body = p.Fig;

                // HIZLAR FARKLI.
                //
                // Hepsi ayni hizda yurudugunde kaldirim bir bant gibi
                // akiyor: kimse kimseye yetismiyor, kimse kimseyi
                // gecmiyor. Fark, sokagi kalabalik degil CANLI yapan sey.
                p.Walk.Speed = 0.92f + (float)_rng.NextDouble() * 0.42f;
                p.Walk.SpeedCap = MaxSpeed;

                // Baslangicta kaldirima dagiliyorlar: hepsi ayni
                // kenardan girerse sokak bir kapidan bosalan kalabalik
                // gibi duruyor.
                float x = Lerp(i / (float)Mathf.Max(1, MaxWalkers - 1));
                p.Walk.Warp(new Vector3(x, 0f, Paths.PavementLane(p.Dir)),
                            p.Dir > 0 ? 90f : -90f);
                _people.Add(p);
                Send(p);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _people.Count; i++)
                if (_people[i].Body != null)
                {
                    if (Application.isPlaying) Destroy(_people[i].Body);
                    else DestroyImmediate(_people[i].Body);
                }
            _people.Clear();
        }

        // =====================================================================
        private void Update()
        {
            if (_people.Count == 0) return;

            // DURAKLATINCA SOKAK DA DURUYOR.
            //
            // Asagidaki Mathf.Max(0.25f, ...) tabani sifiri yutuyordu:
            // duraklatilmis bir dunyada yayalar yerinde sayarak yurume
            // animasyonu oynatiyordu.
            if (Walker.GameSpeed <= 0.001f) return;

            // Sohbet sayaclari da TAVANLI saatle akiyor: yurume
            // tavanliyken sohbet tavansiz olsa, x16'da iki kisi goz
            // kirpacak kadar durup devam ederdi.
            float dt = Time.deltaTime * Clock;

            for (int i = 0; i < _people.Count; i++)
            {
                Pedestrian p = _people[i];
                if (p.Body == null) continue;

                if (p.NextChat > 0f) p.NextChat -= dt;

                if (p.ChatLeft > 0f)
                {
                    p.ChatLeft -= dt;
                    // YUZ HER KAREDE YENIDEN CEVRILIYOR: itisme iki
                    // konusani biraz ayiriyor ve sohbet basinda bir kez
                    // yazilan yon kayiyordu - birbirine degil yanina
                    // bakan iki figur "konusuyor" diye okunmuyor.
                    Face(p);
                    if (p.ChatLeft <= 0f) { p.Partner = null; Send(p); }
                    continue;
                }

                if (p.Walk.Moving)
                {
                    if (p.Fig != null) p.Fig.HoldAwake();
                    continue;
                }

                // Kenara vardi: karsi kenardan devam. Yonu degisince
                // seridi de degisiyor - donus arsanin disinda oldugu
                // icin serit degisimi ekranda gorunmuyor.
                p.Dir = -p.Dir;
                Send(p);
            }

            Gossip();
        }

        // =====================================================================
        /// <summary>
        /// GOVDELERI AYIRAN DUZELTME.
        ///
        /// Walker'dan SONRA kosmali: Walker her karede konumu dogrudan
        /// yaziyor, o yuzden duzeltme Update icinde yapilsa bir sonraki
        /// karede silinirdi. LateUpdate'te yazilan konum o karenin
        /// cizilen konumu oluyor.
        ///
        /// YALNIZCA YAYALAR ITILIYOR, MUSTERILER DEGIL: musterinin yolu
        /// simulasyonun bir olayina bagli (masaya oturma, cikis) ve onu
        /// itmek varis olcumunu bozabilir. Ustelik dogrusu da bu -
        /// lokantaya giren kendi cizgisini korur, yoldan gecen kenara
        /// cekilir.
        /// </summary>
        private void LateUpdate()
        {
            if (_people.Count == 0) return;
            if (Walker.GameSpeed <= 0.001f) return;

            // HIZLANDIRILMIS oyunda duzeltme de hizlaniyor: yoksa x4'te
            // figurler itismenin yetisemedigi kadar hizli ust uste
            // biniyor. Yuruyusle AYNI tavani kullaniyor - itisme
            // yuruyusten yavas kalsa hicbir sey ayiramaz.
            float dt = Time.deltaTime * Clock;
            float en = PushSpeed * dt;

            // Disarida duran musteriler de hesaba giriyor: kapinin
            // onunde bekleyen bir musterinin icinden gecen yaya, ayni
            // hatanin ta kendisi.
            _others.Clear();
            if (_view == null) _view = GetComponent<RestaurantView>();
            if (_view != null) _view.OutsideFigures(_others);

            // Direkler KURULUSTA bir kez okunuyor: sabit duruyorlar ve
            // her karede sekiz nesnelik bir liste kurmak bedava degil.
            // Kurulus damgasi degisince yeniden okunuyor.
            if (_view != null && _postStamp != _view.BuildStamp)
            {
                _postStamp = _view.BuildStamp;
                _posts.Clear();
                _view.StreetObstacles(_posts);
            }

            // SIRA ONEMLI VE BIR HATAYLA OGRENILDI.
            //
            // Ilk yazimda her kisi icin once karsilikli ayirma, sonra
            // engel itmesi yapiliyordu. Yani bir kisinin son islemi
            // "musteriden/direkten uzaklas" oluyordu ve o itme onu
            // ZATEN AYRILMIS oldugu komsusunun icine geri sokabiliyordu -
            // kimse bir daha bakmiyordu. Tur bunu kararsiz bir kirmiziyla
            // gosterdi: uc kosudan birinde "en kotu 1 cift".
            //
            // Dogru sira: once tek tarafli kisitlar (engeller, serit),
            // EN SON karsilikli ayirma. Boylece cizilen karede son sozu
            // soyleyen sey, kontrolun olctugu sey oluyor.

            // --- 1. sabit engeller: musteriler ve direkler -------------
            for (int i = 0; i < _people.Count; i++)
            {
                Pedestrian a = _people[i];
                if (a.Body == null) continue;

                for (int k = 0; k < _others.Count; k++)
                    Push(a, _others[k], en, Personal);
                for (int k = 0; k < _posts.Count; k++)
                    Push(a, _posts[k], en, PostClear);
            }

            // --- 2. serit bandi ----------------------------------------
            for (int i = 0; i < _people.Count; i++)
            {
                Pedestrian a = _people[i];
                if (a.Body == null) continue;

                // SERIDIN ICINDE KALIYOR. Itisme figuru kaldirimin
                // disina atabilirdi: yola ya da binanin icine. Serit
                // bandi onu geri cekiyor - ve yalnizca banttan TASMISSA,
                // yoksa itismeyle catisip titrerdi.
                Vector3 yer = a.Body.transform.localPosition;
                float serit = Paths.PavementLane(a.Dir);
                if (Mathf.Abs(yer.z - serit) > Paths.LaneHalf + 0.30f)
                {
                    yer.z = Mathf.MoveTowards(yer.z, serit, en);
                    a.Body.transform.localPosition = yer;
                }
            }

            // --- 3. karsilikli ayirma, yakinsayana kadar ---------------
            //
            // Zincir: musteri A'yi itiyor, A B'ye giriyor, B C'ye. Tek
            // gecis zinciri bir halka ilerletiyor. Dongu bir gecis hic
            // duzeltme yapmadiginda ERKEN cikiyor, yani sakin bir
            // kaldirimda bedeli tek gecis.
            for (int gecis = 0; gecis < Passes; gecis++)
            {
                bool degisti = false;
                for (int i = 0; i < _people.Count; i++)
                {
                    Pedestrian a = _people[i];
                    if (a.Body == null) continue;

                    for (int j = i + 1; j < _people.Count; j++)
                    {
                        Pedestrian b = _people[j];
                        if (b.Body == null) continue;
                        if (Nudge(a, b, en)) degisti = true;
                    }
                }
                if (!degisti) break;
            }

            // OLCUM EN SONDA: bu kare artik cizilecek ve sayi o karenin
            // sayisi.
            Olc();
        }

        /// <summary>
        /// Iki yayayi karsilikli ayirir. Duzeltme EKSIK MESAFENIN
        /// yarisi, tavani e. Bir duzeltme yaptiysa true.
        /// </summary>
        private static bool Nudge(Pedestrian a, Pedestrian b, float e)
        {
            Vector3 pa = a.Body.transform.localPosition;
            Vector3 pb = b.Body.transform.localPosition;
            Vector3 d = pa - pb;
            d.y = 0f;
            float u = d.magnitude;
            if (u >= Personal) return false;

            float itme = Mathf.Min(e, (Personal - u) * 0.5f);
            Vector3 yon = Direction(d, u, a.Side);
            a.Body.transform.localPosition = pa + yon * itme;
            b.Body.transform.localPosition = pb - yon * itme;
            return true;
        }

        /// <summary>
        /// Yayayi sabit bir noktadan uzaklastirir. Oteki taraf
        /// kimildamadigi icin duzeltme eksik mesafenin TAMAMI.
        /// </summary>
        private static void Push(Pedestrian a, Vector3 other, float e, float clear)
        {
            Vector3 pa = a.Body.transform.localPosition;
            Vector3 d = pa - other;
            d.y = 0f;
            float u = d.magnitude;
            if (u >= clear) return;

            float itme = Mathf.Min(e, clear - u);
            a.Body.transform.localPosition = pa + Direction(d, u, a.Side) * itme;
        }

        /// <summary>
        /// Ayirma yonu - en az SideBias kadari YANAL.
        ///
        /// u sifira cok yakinsa (tam ust uste) yon tanimsiz kaliyor; o
        /// zaman figurun kendi tarafina kaydiriliyor. Tarafi SABIT, yani
        /// ayni figur her zaman ayni yana cekiliyor - kararsiz bir taraf
        /// secimi titreme uretir.
        /// </summary>
        private static Vector3 Direction(Vector3 d, float u, int side)
        {
            if (u < 0.001f) return new Vector3(0f, 0f, side);

            Vector3 n = d / u;
            // Yurume ekseni x, yanal eksen z. Yanal bilesen tabana
            // cekiliyor, isareti korunuyor.
            float z = n.z;
            if (Mathf.Abs(z) < SideBias) z = SideBias * (z < 0f ? -1f : 1f);

            Vector3 r = new Vector3(n.x, 0f, z);
            float m = r.magnitude;
            return m < 0.001f ? new Vector3(0f, 0f, side) : r / m;
        }

        // =====================================================================
        /// <summary>
        /// Yan yana gelen ikiliyi konusturur.
        ///
        /// Kosullar: ikisi de yuruyor olmali, yeterince yakin ama COK
        /// yakin OLMAMALI ve ikisi de yakin zamanda konusmamis olmali.
        /// Son kosul olmazsa ayni iki kisi kaldirimda takilip kaliyor -
        /// "sohbet" degil "tikanma" diye okunuyor.
        /// </summary>
        private void Gossip()
        {
            for (int i = 0; i < _people.Count; i++)
            {
                Pedestrian a = _people[i];
                // GOVDE KONTROLU BURADA DA SART.
                //
                // Update dongusu kendini koruyordu ama burasi
                // korumuyordu: ana menuye donunce gorunum butun
                // cocuklarini yok ediyor, _people icinde bes olu kayit
                // kaliyor ve a.Body.transform her karede istisna
                // atiyordu. Android'de yigin izi uretmek pahali.
                if (a.Body == null || a.Walk == null) continue;
                if (a.ChatLeft > 0f || a.NextChat > 0f || !a.Walk.Moving) continue;

                for (int j = i + 1; j < _people.Count; j++)
                {
                    Pedestrian b = _people[j];
                    if (b.Body == null || b.Walk == null) continue;
                    if (b.ChatLeft > 0f || b.NextChat > 0f || !b.Walk.Moving) continue;

                    Vector3 d = a.Body.transform.localPosition
                                - b.Body.transform.localPosition;
                    d.y = 0f;
                    float u2 = d.sqrMagnitude;
                    if (u2 > ChatRange * ChatRange) continue;
                    if (u2 < ChatNear * ChatNear) continue;
                    if (_rng.NextDouble() > 0.35) continue;

                    float sure = ChatMin + (float)_rng.NextDouble() * (ChatMax - ChatMin);
                    Chat(a, b, sure);
                    Chat(b, a, sure);
                    break;
                }
            }
        }

        private void Chat(Pedestrian who, Pedestrian other, float seconds)
        {
            if (who.Body == null || who.Walk == null) return;

            who.ChatLeft = seconds;
            who.NextChat = seconds + 8f;
            who.Partner = other;
            who.Walk.Stop();
            if (who.Fig != null) who.Fig.Set(Figure.Pose.Idle);
            Face(who);
        }

        /// <summary>
        /// Konusani karsisindakine dondurur.
        ///
        /// Ayni yone bakan iki figur "konusuyor" degil "sirada
        /// bekliyor" diye okunuyor.
        /// </summary>
        private static void Face(Pedestrian who)
        {
            if (who.Body == null) return;
            if (who.Partner == null || who.Partner.Body == null) return;

            float yaw = Paths.FaceFrom(who.Body.transform.localPosition,
                                       who.Partner.Body.transform.localPosition);
            who.Body.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>Kaldirimda karsi kenara dogru yollar.</summary>
        private void Send(Pedestrian p)
        {
            if (p.Body == null || p.Walk == null) return;

            float hedef = p.Dir > 0 ? RoomPlan.PlotW + 1.1f : -1.1f;
            _path.Clear();
            // Hedef kendi SERIDINDE: sohbetten ya da itismeden sonra
            // seridin disinda kalan figur yurudukce kendiliginden
            // seride donuyor - bir sicrama olmadan.
            _path.Add(new Vector3(hedef, 0f, Paths.PavementLane(p.Dir)));
            p.Walk.GoTo(_path, float.NaN, null);
            if (p.Fig != null) p.Fig.Set(Figure.Pose.Walk);
        }

        private static float Lerp(float t)
        {
            return Mathf.Lerp(-0.8f, RoomPlan.PlotW + 0.8f, t);
        }
    }
}
