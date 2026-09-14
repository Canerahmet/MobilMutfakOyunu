using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Moduler ODA TABANLI yerlesim ve dokunma hedefi olcumu.
    ///
    /// Tur 1 (acik salon, RestaurantScene.cs): masa her kademede 15-20 dp,
    /// asgari 48 dp. Basarisiz.
    ///
    /// Tur 2 (odalar tek sira): restoran 25 x 4,6 m bir koridora dondu,
    /// 20:9 karenin yarisi bos kaldi ve kamera her kademede geri cekildigi
    /// icin dokunma hedefi buyumeyle KUCULUYORDU. Oda fikri dogru, dizilim
    /// yanlis.
    ///
    /// Tur 3 (2x2 esit izgara): sayilar tuttu ama render yapay durdu.
    /// Butun odalar ayni olcude, butun ayrim cizgileri hizali.
    ///
    /// Tur 4, bu dosya: FARKLI OLCUDE dikdortgenler bir arsayi kapliyor.
    ///   - Arsa sabit 18,0 x 9,6 m. Kamera ACIK odalari cerceveliyor
    ///     ve mesafe kademeler arasinda degismiyor, yani dokunma hedefi
    ///     kademeden bagimsiz kaliyor.
    ///
    /// Tur 5: kamera artik OYUNUN kendi hesabini kullaniyor
    /// (Lokanta.Game.CameraFit). Onceden burada ayri acilar ve ayri bir
    /// mesafe formulu vardi; ikisi ayrismisti ve olcum, oyunun
    /// gosterdiginden %30 uzak bir kameradan bakiyordu.
    ///   - Odalarin olculeri farkli ve ayrim cizgileri hizali degil
    ///     (sol yarida z=4,4, sag yarida z=5,0). Gercek bir kat plani
    ///     boyle okunuyor.
    ///   - Duvarlar odanin KENARINDAN uretiliyor: komsusu yapilmis kenar
    ///     alcak bolme (0,85 m), yapilmamis veya disari bakan arka ve sol
    ///     kenar tam duvar (2,6 m), kameraya bakan on ve sag kenar acik.
    ///     Bina bu yuzden odalar eklendikce kendiliginden buyuyor.
    ///
    /// Dogrulama sahneleri UNLIT: editor toplu kipinde URP Lit calismiyor.
    /// </summary>
    public static class RoomLayout
    {
        private const string OutDir = "../tools/art/out/unity";
        private const int ShotW = 960;
        private const int ShotH = 432;      // 20:9 telefon yatay

        // EN DAR KARE ORANI DA OLCULUYOR.
        //
        // Telefonlar 16:9 ile 21:9 arasinda; dar olanda kamera ayni
        // derinligi sigdirmak icin DAHA UZAGA gidiyor (31,7 m vs 28,4)
        // ve dokunma hedefi kuculuyor. Yalnizca 20:9 olcmek, en kotu
        // durumu hic gormemek demekti.
        private const int NarrowW = 960;
        private const int NarrowH = 540;    // 16:9

        private const float CellX = Lokanta.Game.RoomPlan.CellX;  // masa takimi araligi, en
        private const float CellZ = Lokanta.Game.RoomPlan.CellZ;  // masa takimi araligi, derinlik
        private const float Margin = Lokanta.Game.RoomPlan.Margin; // odanin masasiz kenar payi

        private const float WallT = 0.14f;
        private const float WallH = 2.60f;
        // 1,10 m denendi ve arka siradaki sandalyelerin sirtini kesti.
        private const float PartH = 0.85f;
        private const float DoorW = 1.30f;

        private const float PlotW = Lokanta.Game.RoomPlan.PlotW;
        private const float PlotD = Lokanta.Game.RoomPlan.PlotD;

        private static readonly Color FloorWood = new Color(0.50f, 0.36f, 0.24f);
        private static readonly Color FloorWood2 = new Color(0.45f, 0.32f, 0.21f);
        private static readonly Color FloorKitchen = new Color(0.74f, 0.76f, 0.74f);
        private static readonly Color FloorWet = new Color(0.60f, 0.68f, 0.70f);
        private static readonly Color FloorStore = new Color(0.48f, 0.45f, 0.41f);
        private static readonly Color FloorEntry = new Color(0.68f, 0.63f, 0.55f);
        private static readonly Color FloorEmpty = new Color(0.62f, 0.60f, 0.56f);
        private static readonly Color Wall = new Color(0.88f, 0.86f, 0.80f);
        private static readonly Color Part = new Color(0.80f, 0.77f, 0.70f);
        private static readonly Color Wood = new Color(0.38f, 0.23f, 0.13f);
        private static readonly Color Seat = new Color(0.72f, 0.18f, 0.14f);
        private static readonly Color Metal = new Color(0.62f, 0.64f, 0.66f);
        private static readonly Color Dark = new Color(0.30f, 0.32f, 0.34f);
        private static readonly Color Outline = new Color(0.50f, 0.48f, 0.44f);
        private static readonly Color Temp = new Color(0.78f, 0.72f, 0.58f);

        private struct Room
        {
            public string Name;
            public float X0, Z0, W, D;
            public Color Floor;
            public int Tier;      // 0 = her zaman var, 1..4 = o kademede aciliyor
            public int Tables;    // salon degilse 0

            public float X1 { get { return X0 + W; } }
            public float Z1 { get { return Z0 + D; } }
            public Vector3 Center
            {
                get { return new Vector3(X0 + W * 0.5f, 0.55f, Z0 + D * 0.5f); }
            }
        }

        /// <summary>
        /// Kat plani. TEK KAYNAK: Lokanta.Game.RoomPlan.
        ///
        /// Bu dizi bir zamanlar burada, kendi sayilariyla duruyordu ve
        /// calisma zamani plani hic bilmiyordu. Ayni sayilari iki yere
        /// yazmak bu projede dort kez sessizce ayristi (bkz. docs/34);
        /// o yuzden plan calisma zamanina tasindi ve bu arac ondan
        /// TURETIYOR. Burada kalan tek sey RENK - yani gorunum.
        ///
        /// Plandaki dikdortgenler arsayi bosluksuz kapliyor ama olculeri
        /// farkli; sol yarinin ayrim cizgisi z=4,4, sag yarinin z=5,0.
        /// Depo ARKADA ve mutfagin sag kenarina yapisik: teslimat arkadan
        /// girer, depoya iner, mutfaga cikar.
        /// </summary>
        private static Room[] Plan
        {
            get
            {
                var src = Lokanta.Game.RoomPlan.Rooms;
                Room[] plan = new Room[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    plan[i] = new Room
                    {
                        Name = src[i].Name,
                        X0 = src[i].X0, Z0 = src[i].Z0,
                        W = src[i].W, D = src[i].D,
                        Tier = src[i].Tier,
                        Tables = src[i].Tables,
                        Floor = FloorOf(src[i].Name),
                    };
                }
                return plan;
            }
        }

        private static Color FloorOf(string room)
        {
            switch (room)
            {
                case "Mutfak": return FloorKitchen;
                case "Giris": return FloorEntry;
                case "Bulasik": return FloorWet;
                case "Depo": return FloorStore;
                case "Salon1":
                case "Salon4": return FloorWood;
                case "Salon2":
                case "Salon3": return FloorWood2;
                default: return FloorEmpty;
            }
        }

        private static readonly int[] TierTables = { 4, 7, 10, 14 };

        /// <summary>
        /// Oyun ekranindaki ust seridin ve eylem cubugunun ekrandan
        /// aldigi oran. Oyunda bu sayilar OLCULEREK kameraya bildiriliyor
        /// (GameScreen -> CameraRig.SetSafeArea); burada temsili deger
        /// duruyor, cunku olcum arayuzu kurmuyor. Cubuk buyurse hedef
        /// kuculur, o yuzden bu iki sayi arayuzun UST SINIRI sayilmali.
        /// </summary>
        // OLCULEN DEGERLER, TAHMIN DEGIL.
        //
        // Eski 0,13 / 0,17 (toplam %30) arayuz yeniden tasarlanmadan
        // once yazilmisti ve "ust sinir" oldugu soyleniyordu. Tur artik
        // seridi GERCEKTEN olcuyor: toplam 156 dp / 393 dp = %40. Yani
        // sayi ust sinir degil, ALT sinirdi - olcum, hedefin gercekte
        // olduğundan BUYUK oldugunu soyluyordu.
        //
        // Ust serit 56 dp (kapsul 42 + dolgu), alt 100 dp (simge dugmesi
        // 62 + dolgu + kriz seridi payi).
        // BU IKI SAYI TURUN OLCTUGU SERIDIN KOPYASI.
        //
        // Tur `GameScreen.StripHeight`'i gercekten olcuyor; burasi
        // editor kipinde calistigi icin arayuzu kuramiyor ve elle
        // yazilmis bir kopya tutmak zorunda. Tehlike de burada: turun
        // serit BUTCESI 220 dp'ye izin veriyor, bu kopya ise
        // %40 (~157 dp) varsayiyor. Serit 200 dp'ye ciksa tur yesil
        // kalir, dokunma hedefi olcumu hala 157 dp'ye gore hesaplardi.
        //
        // Bu yuzden kopya artik TURUN BUTCESIYLE ayni tavana bagli ve
        // asagidaki kontrol ikisinin uyustugunu sinaniyor.
        private const float BandTop = 0.145f;       // 57 dp
        private const float BandBottom = 0.293f;    // 115 dp

        /// <summary>
        /// Turun EN KOTU asamada olctugu serit yuksekligi (dp).
        ///
        /// 13 Eylul 2026 olcumu: sabah 154, servis 154, aksam 172 - iki
        /// dilde de ayni. Kopya bu sayiya gore kuruldu.
        ///
        /// Turun BUTCESI (220 dp) ile karistirilmamali: butce izin
        /// verilen tavan, bu ise BUGUN OLCULEN deger. Kopyayi butceye
        /// gore kurmak, olmayan bir serit icin yer ayirip dokunma
        /// hedefini oldugundan KUCUK bildirmek olurdu; olculenin altina
        /// kurmak ise oldugundan BUYUK bildirir. Asagidaki kontrol
        /// ikincisini yakaliyor.
        ///
        /// Serit degisince: turu kos, "serit butcesi" satirlarindaki en
        /// buyugu buraya yaz, bandi da ona gore ayarla.
        /// </summary>
        private const float TourMeasuredStripDp = 172f;

        /// <summary>Telefonun kisa kenari, dp (873x393).</summary>
        private const float PhoneShortDp = 393f;

        private static readonly List<Bounds> DiningRooms = new List<Bounds>();
        private static int _wallSeq;

        [MenuItem("Lokanta/Oda yerlesimini render et")]
        public static void Capture()
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDir));
                Directory.CreateDirectory(dir);
                string stamp = DateTime.Now.ToString("HHmmss");

                Debug.Log("=== Lokanta oda yerlesimi ===");
                Debug.Log(string.Format("  OLCUM arsa {0:0.0} x {1:0.0} m, sabit", PlotW, PlotD));
                Debug.Log("  OLCUM asgari dokunma hedefi 48 dp (Google), 44 pt (Apple)");
                Debug.Log("  OLCUM olcek: 960 px render -> 2400 px telefon, dp = px * 2,5 / 2,75");
                CheckPlan();

                // KOPYA ILE BUTCE UYUSUYOR MU.
                //
                // Bu arac seridi kuramiyor (editor kipi) ve elle
                // yazilmis bir orana gore olcuyor. O oran turun serit
                // butcesinden KUCUK kalirsa, tur yesil kalirken bu
                // olcum fazla yer varsayar ve dokunma hedefini oldugundan
                // buyuk bildirir.
                float kopyaDp = (BandTop + BandBottom) * PhoneShortDp;
                Debug.Log(string.Format(
                    "  OLCUM serit kopyasi {0:0} dp, turun olctugu {1:0} dp",
                    kopyaDp, TourMeasuredStripDp));
                if (kopyaDp < TourMeasuredStripDp - 1f)
                    Debug.LogError(string.Format(
                        "SORUNLAR: serit kopyasi ({0:0} dp) turun olctugunden "
                        + "({1:0} dp) kucuk - dokunma hedefi oldugundan BUYUK "
                        + "olculuyor", kopyaDp, TourMeasuredStripDp));

                for (int tier = 1; tier <= 4; tier++)
                {
                    Build(tier);
                    int tables = TierTables[tier - 1];

                    Vector3 near = Shoot(
                        Path.Combine(dir, string.Format("kat_{0:00}_tekoda_{1}.png", tables, stamp)),
                        Pad(DiningRooms[0], 0.5f), DiningRooms[0], null);

                    // GENEL GORUNUM ACIK ODALARI cerceveliyor, butun
                    // arsayi degil - oyun da oyle yapiyor. Kapali kanadi
                    // cerceveye katmak hedefi oldugundan KUCUK olcuyordu.
                    List<string> roomDp = new List<string>();
                    Bounds acik = Lokanta.Game.CameraFit.OpenBounds(tables);
                    Vector3 far = Shoot(
                        Path.Combine(dir, string.Format("kat_{0:00}_hepsi_{1}.png", tables, stamp)),
                        acik, DiningRooms[0], roomDp);

                    // Ve bir de ARAYUZ CUBUKLARI VARKEN: oyunda ust serit
                    // ve eylem cubugu ekranin bir kismini aliyor, kamera da
                    // kalan seride sigdiriyor. Hedefin gercek tabani bu.
                    List<string> bandDp = new List<string>();
                    Vector3 band = Shoot(
                        Path.Combine(dir, string.Format("kat_{0:00}_serit_{1}.png", tables, stamp)),
                        acik, DiningRooms[0], bandDp, BandTop, BandBottom);

                    Debug.Log(string.Format(
                        "  OLCUM {0,2} masa | ODA: masa {1:0} dp, masa+sandalye {2:0} dp"
                        + " || GENEL: masa {3:0} dp, ilk salon {4:0} dp"
                        + " || SERITLI: ilk salon {5:0} dp",
                        tables, Dp(near.x), Dp(near.y), Dp(far.x), Dp(far.z), Dp(band.z)));
                    Debug.Log("  OLCUM   genel gorunumde odalar: "
                              + string.Join(", ", roomDp.ToArray()));

                    // TABAN: en kucuk ACIK odanin seritli degeri. Asgari
                    // 48 dp'yi tutan ya da tutmayan sayi bu - ortalama
                    // degil, en kotu oda.
                    List<string> darDp = new List<string>();
                    Shoot(Path.Combine(dir,
                              string.Format("kat_{0:00}_dar_{1}.png", tables, stamp)),
                          acik, DiningRooms[0], darDp, BandTop, BandBottom,
                          NarrowW, NarrowH);

                    string taban209 = Smallest(bandDp, tables);
                    string taban169 = Smallest(darDp, tables);
                    Debug.Log("  OLCUM   seritli odalar: "
                              + string.Join(", ", bandDp.ToArray())
                              + "  -> TABAN 20:9 " + taban209
                              + " dp, 16:9 " + taban169 + " dp");

                    // 48 dp ESIGI ARTIK IDDIA EDILIYOR.
                    //
                    // Ustte bir satir metin olarak yaziliydi
                    // ("asgari dokunma hedefi 48 dp") ama HICBIR
                    // kosulda karsilastirilmiyordu. Hafizadaki
                    // "gercek zemin 48 dp'ydi" bulgusu sessizce geri
                    // gelebilirdi - tam da bu aracin yakalamasi
                    // gereken sey.
                    Esik(taban209, tables, "20:9");
                    Esik(taban169, tables, "16:9");
                }

                Debug.Log("=== oda yerlesimi tamam ===");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("SORUNLAR: oda yerlesimi -> " + e.GetType().Name + ": " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(2);
            }
        }

        /// <summary>
        /// Kat plani arsayi bosluksuz ve ust uste binmeden kapliyor mu, ve
        /// her odaya istenen masa sigiyor mu. Elle yazilan dikdortgenlerde
        /// bir ondalik kaymasi sessizce bosluk birakiyor.
        /// </summary>
        private static void CheckPlan()
        {
            float area = 0f;
            for (int i = 0; i < Plan.Length; i++)
            {
                area += Plan[i].W * Plan[i].D;
                for (int j = i + 1; j < Plan.Length; j++)
                {
                    bool overlap =
                        Plan[i].X0 < Plan[j].X1 - 0.001f && Plan[j].X0 < Plan[i].X1 - 0.001f &&
                        Plan[i].Z0 < Plan[j].Z1 - 0.001f && Plan[j].Z0 < Plan[i].Z1 - 0.001f;
                    if (overlap)
                        throw new Exception("odalar cakisiyor: " + Plan[i].Name + " / " + Plan[j].Name);
                }

                int cols, rows;
                Fit(Plan[i], out cols, out rows);
                if (Plan[i].Tables > cols * rows)
                    throw new Exception(string.Format("{0}: {1} masa istiyor, {2} siga",
                                                      Plan[i].Name, Plan[i].Tables, cols * rows));
            }
            if (Mathf.Abs(area - PlotW * PlotD) > 0.01f)
                throw new Exception(string.Format(
                    "arsa kaplanmadi: odalar {0:0.00} m2, arsa {1:0.00} m2", area, PlotW * PlotD));

            Debug.Log(string.Format("  OLCUM kat plani tutarli: {0} oda, {1:0.0} m2",
                                    Plan.Length, area));
        }

        /// <summary>
        /// Odaya kac sutun kac satir masa siger. Epsilon sart: 4,6 - 0,9 =
        /// 3,6999998 cikiyor ve 1,85'e bolununce 1,99999 oluyor, taban
        /// alinca iki yerine bir sutun. Kontrol bunu yakaladi.
        /// </summary>
        private static void Fit(Room r, out int cols, out int rows)
        {
            cols = Mathf.Max(1, Mathf.FloorToInt((r.W - Margin) / CellX + 0.002f));
            rows = Mathf.Max(1, Mathf.FloorToInt((r.D - Margin) / CellZ + 0.002f));
        }

        // ---------------------------------------------------------------------
        private static void Build(int tier)
        {
            Clear();
            DiningRooms.Clear();
            _wallSeq = 0;

            List<Room> built = new List<Room>();
            foreach (Room r in Plan) if (r.Tier <= tier) built.Add(r);

            int tableIndex = 0;
            foreach (Room r in Plan)
            {
                if (r.Tier > tier)
                {
                    Slab(r.Name + "Arsa", r.X0, r.Z0, r.W, r.D, FloorEmpty);
                    Frame(r.Name + "Cerceve", r.X0, r.Z0, r.W, r.D);
                    continue;
                }

                Slab(r.Name + "Zemin", r.X0, r.Z0, r.W, r.D, r.Floor);
                Edges(r, built);

                if (r.Tables > 0)
                {
                    int cols, rows;
                    Fit(r, out cols, out rows);
                    float sx = r.X0 + (r.W - cols * CellX) * 0.5f;
                    float sz = r.Z0 + (r.D - rows * CellZ) * 0.5f;

                    int made = 0;
                    for (int rr = 0; rr < rows && made < r.Tables; rr++)
                    {
                        // Eksik kalan son satir ortalansin; kenara yaslanmis
                        // tek masa unutulmus gibi duruyor.
                        int inRow = Mathf.Min(cols, r.Tables - made);
                        float off = (cols - inRow) * CellX * 0.5f;
                        for (int cc = 0; cc < inRow; cc++, made++)
                            Table(tableIndex++, new Vector3(
                                sx + off + CellX * (cc + 0.5f), 0f,
                                sz + CellZ * (rr + 0.5f)));
                    }

                    DiningRooms.Add(new Bounds(r.Center, new Vector3(r.W, 1.60f, r.D)));
                }
                else
                {
                    Props(r);
                }
            }

        }

        /// <summary>Servis odalarinin icindekiler.</summary>
        private static void Props(Room r)
        {
            switch (r.Name)
            {
                case "Mutfak":
                    Box("Ocak", new Vector3(r.X0 + 1.30f, 0.43f, r.Z1 - 0.75f),
                        new Vector3(1.80f, 0.86f, 0.72f), Dark);
                    Box("Tezgah", new Vector3(r.X0 + 3.50f, 0.45f, r.Z1 - 0.75f),
                        new Vector3(2.20f, 0.90f, 0.66f), Metal);
                    Box("Davlumbaz", new Vector3(r.X0 + 1.30f, 1.95f, r.Z1 - 0.75f),
                        new Vector3(1.90f, 0.45f, 0.85f), Metal);
                    // Buzdolabi mutfaktan KALDIRILDI: soguk saklama artik
                    // deponun isi ve orada gorunur bir yukseltme merdiveni
                    // var. Ikisini birden gostermek yalan olurdu.
                    break;
                case "Bulasik":
                    Box("Evye", new Vector3(r.X0 + r.W * 0.5f, 0.45f, r.Z1 - 0.70f),
                        new Vector3(2.40f, 0.90f, 0.64f), Metal);
                    Box("BulasikRaf", new Vector3(r.X1 - 0.40f, 0.60f, r.Z0 + r.D * 0.40f),
                        new Vector3(0.50f, 1.20f, 2.20f), Wood);
                    break;
                case "Depo":
                    // Soguk hava odasi arkada, kuru raf yan duvarda. Ikisi
                    // malzeme listesinin bozulabilir / bozulmaz ayrimina
                    // karsilik geliyor; dekor degil.
                    Box("SogukHava", new Vector3(r.X0 + r.W * 0.5f, 1.05f, r.Z1 - 0.65f),
                        new Vector3(r.W - 0.50f, 2.10f, 0.90f), Metal);
                    Box("SogukKapi", new Vector3(r.X0 + r.W * 0.5f, 0.95f, r.Z1 - 1.12f),
                        new Vector3(0.85f, 1.80f, 0.08f), Dark);
                    Box("KuruRaf", new Vector3(r.X1 - 0.40f, 0.70f, r.Z0 + 1.15f),
                        new Vector3(0.50f, 1.40f, 1.60f), Wood);
                    Box("Sandik1", new Vector3(r.X0 + 0.80f, 0.30f, r.Z0 + 0.70f),
                        new Vector3(0.90f, 0.60f, 0.80f), Wood);
                    break;
                case "Giris":
                    Box("Kasa", new Vector3(r.X0 + 1.50f, 0.50f, r.Z0 + 1.40f),
                        new Vector3(1.80f, 1.00f, 0.70f), Wood);
                    Box("Kapi", new Vector3(r.X1 - 1.30f, 1.05f, r.Z0 + 0.08f),
                        new Vector3(1.30f, 2.10f, 0.10f), Wood);
                    break;
            }
        }

        // ---------------------------------------------------------------------
        /// <summary>
        /// Odanin dort kenarini yurur ve her parcayi siniflar: komsusu
        /// yapilmis ise alcak bolme, degilse tam duvar. Kameraya bakan on
        /// (-Z) ve sag (+X) kenarlara tam duvar konmuyor, yoksa sahneyi
        /// kapatiyor. Ic kenarlar iki kez cizilmesin diye bolme yalnizca
        /// +Z ve +X kenarlarindan uretiliyor.
        /// </summary>
        private static void Edges(Room r, List<Room> built)
        {
            EdgeRun(r, built, true, r.Z1, r.X0, r.X1, +1f, true);    // arka
            EdgeRun(r, built, true, r.Z0, r.X0, r.X1, -1f, false);   // on
            EdgeRun(r, built, false, r.X0, r.Z0, r.Z1, -1f, true);   // sol
            EdgeRun(r, built, false, r.X1, r.Z0, r.Z1, +1f, false);  // sag
        }

        private static void EdgeRun(Room r, List<Room> built, bool horizontal,
                                    float fixedC, float a0, float a1, float dir,
                                    bool wallIfOpen)
        {
            const float step = 0.20f;
            int n = Mathf.Max(1, Mathf.RoundToInt((a1 - a0) / step));
            int runStart = 0;
            int runKind = -2;

            for (int i = 0; i <= n; i++)
            {
                int kind = -2;
                if (i < n)
                {
                    float mid = a0 + (i + 0.5f) * (a1 - a0) / n;
                    float px = horizontal ? mid : fixedC + dir * 0.10f;
                    float pz = horizontal ? fixedC + dir * 0.10f : mid;
                    bool neighbour = InBuilt(built, px, pz);
                    bool outside = px < 0f || px > PlotW || pz < 0f || pz > PlotD;
                    // 0 = bolme, 1 = tam duvar, 2 = gecici duvar, -1 = hicbir sey
                    if (neighbour) kind = 0;
                    else if (outside) kind = wallIfOpen ? 1 : -1;
                    else kind = 2;      // arsa icinde ama henuz yapilmamis
                    if (kind == 0 && dir < 0f) kind = -1;   // ic kenar tek seferde
                }

                if (kind != runKind)
                {
                    if (runKind >= 0)
                        EdgeSegment(horizontal, fixedC,
                                    a0 + runStart * (a1 - a0) / n,
                                    a0 + i * (a1 - a0) / n,
                                    runKind, r.Name);
                    runStart = i;
                    runKind = kind;
                }
            }
        }

        /// <summary>
        /// kind: 0 ic bolme, 1 arsa sinirinda tam duvar, 2 gecici duvar.
        ///
        /// Gecici duvar, arsanin icinde olup henuz yapilmamis odaya bakan
        /// kenar. Tam duvar denendi ve genisleme alanini tamamen gizledi;
        /// oyuncu nereye buyuyecegini goremiyordu. Alcak ve ayri renkte:
        /// "burasi acilacak" diyor.
        /// </summary>
        private static void EdgeSegment(bool horizontal, float fixedC,
                                        float b0, float b1, int kind, string owner)
        {
            float len = b1 - b0;
            if (len < 0.05f) return;
            float h = kind == 1 ? WallH : PartH;
            Color c = kind == 1 ? Wall : (kind == 2 ? Temp : Part);

            // Uzun her bolmenin ortasinda gecit; yoksa odalar kapali kaliyor.
            if (kind == 0 && len > DoorW + 0.8f)
            {
                float half = (len - DoorW) * 0.5f;
                EdgeBox(horizontal, fixedC, b0, b0 + half, h, c, owner);
                EdgeBox(horizontal, fixedC, b1 - half, b1, h, c, owner);
                return;
            }
            EdgeBox(horizontal, fixedC, b0, b1, h, c, owner);
        }

        private static void EdgeBox(bool horizontal, float fixedC,
                                    float b0, float b1, float h, Color c, string owner)
        {
            float len = b1 - b0;
            if (len < 0.05f) return;
            Vector3 pos = horizontal
                ? new Vector3((b0 + b1) * 0.5f, h * 0.5f, fixedC)
                : new Vector3(fixedC, h * 0.5f, (b0 + b1) * 0.5f);
            Vector3 size = horizontal
                ? new Vector3(len, h, WallT)
                : new Vector3(WallT, h, len);
            Box("Duvar" + (_wallSeq++) + "_" + owner, pos, size, c);
        }

        private static bool InBuilt(List<Room> built, float x, float z)
        {
            foreach (Room r in built)
                if (x > r.X0 && x < r.X1 && z > r.Z0 && z < r.Z1) return true;
            return false;
        }

        // ---------------------------------------------------------------------
        private static void Slab(string name, float x0, float z0, float w, float d, Color c)
        {
            // Iki santim bosluk: bitisik zeminler arasinda ince bir cizgi
            // kaliyor, odanin siniri renk farkindan bagimsiz okunuyor.
            Box(name, new Vector3(x0 + w * 0.5f, -0.05f, z0 + d * 0.5f),
                new Vector3(w - 0.02f, 0.10f, d - 0.02f), c);
        }

        private static void Frame(string name, float x0, float z0, float w, float d)
        {
            const float t = 0.10f;
            Box(name + "A", new Vector3(x0 + w * 0.5f, 0.01f, z0 + t * 0.5f),
                new Vector3(w, 0.06f, t), Outline);
            Box(name + "B", new Vector3(x0 + w * 0.5f, 0.01f, z0 + d - t * 0.5f),
                new Vector3(w, 0.06f, t), Outline);
            Box(name + "C", new Vector3(x0 + t * 0.5f, 0.01f, z0 + d * 0.5f),
                new Vector3(t, 0.06f, d), Outline);
            Box(name + "D", new Vector3(x0 + w - t * 0.5f, 0.01f, z0 + d * 0.5f),
                new Vector3(t, 0.06f, d), Outline);
        }

        private static void Table(int i, Vector3 at)
        {
            Box("Masa" + i, at + new Vector3(0f, 0.74f, 0f),
                new Vector3(0.86f, 0.06f, 0.86f), Wood);
            Box("MasaAyak" + i, at + new Vector3(0f, 0.36f, 0f),
                new Vector3(0.12f, 0.72f, 0.12f), Wood);
            Chair("Sandalye" + i + "a", at + new Vector3(0f, 0f, 0.62f), 1f);
            Chair("Sandalye" + i + "b", at + new Vector3(0f, 0f, -0.62f), -1f);
        }

        private static void Chair(string name, Vector3 at, float away)
        {
            Box(name, at + new Vector3(0f, 0.44f, 0f),
                new Vector3(0.42f, 0.06f, 0.42f), Seat);
            Box(name + "Ayak", at + new Vector3(0f, 0.21f, 0f),
                new Vector3(0.10f, 0.42f, 0.10f), Dark);
            Box(name + "Sirt", at + new Vector3(0f, 0.68f, away * 0.18f),
                new Vector3(0.42f, 0.46f, 0.06f), Seat);
        }

        private static Bounds Pad(Bounds b, float m)
        {
            Bounds r = b;
            r.Expand(new Vector3(m, 0f, m));
            return r;
        }

        private static float Dp(float renderPx) { return renderPx * 2.5f / 2.75f; }

        /// <summary>
        /// Acik odalarin en kucuk dp degeri. Kapali oda sayilmiyor -
        /// oyuncu ona dokunamiyor, cunku artik cizilmiyor.
        /// </summary>
        /// <summary>
        /// Dokunma hedefi esikleri. Sayi zaten hesaplaniyordu; eksik
        /// olan tek sey onu bir IDDIAYA baglamakti - 48 dp bir satir
        /// METIN olarak gunluge yaziliyor ve hicbir kosulda
        /// karsilastirilmiyordu.
        ///
        /// IKI ESIK, CUNKU 48 BURADA KIRMIZI OLAMAZ.
        ///
        /// Google'in tabani 48 dp (Apple 44 pt) ve proje kamerayi 10
        /// derece dondurdugunde 20:9'da 45 dp'ye dustugunu OLCUP kabul
        /// etti (docs/41): "tabanin altinda kalan sey bir dugme degil,
        /// bes metrelik bir odanin ekrandaki KISA kenari; uzun kenari
        /// iki katindan fazla ve oyuncu iki parmakla yaklasabiliyor."
        /// 48'i kirmizi yapmak, verilmis bir karari her kosuda hata
        /// diye bildirmek olurdu.
        ///
        ///   - 48 dp alti: UYARI (sektor tabani, bilinen bedel)
        ///   - 40 dp alti: KIRMIZI (kabul edilen 45'ten belirgin
        ///     gerileme; artik "yaklasip dokunulur" da denemez)
        /// </summary>
        private static void Esik(string dpMetin, int tables, string en)
        {
            if (!int.TryParse(dpMetin, out int dp)) return;
            if (dp < 40)
            {
                Debug.LogError("SORUNLAR: dokunma hedefi " + dp + " dp (< 40), "
                               + tables + " masa, " + en);
                return;
            }
            if (dp < 48)
                Debug.LogWarning("Dokunma hedefi " + dp + " dp, sektor tabani 48 - "
                                 + tables + " masa, " + en
                                 + " (docs/41: bilinen ve kabul edilmis bedel)");
        }

        private static string Smallest(List<string> dp, int tables)
        {
            int best = int.MaxValue;
            for (int i = 0; i < Plan.Length && i < dp.Count; i++)
            {
                Lokanta.Game.RoomPlan.Room r = Lokanta.Game.RoomPlan.Rooms[i];
                if (!Lokanta.Game.RoomPlan.RoomOpen(in r, tables)) continue;

                string[] parts = dp[i].Split(' ');
                if (parts.Length < 2) continue;
                if (int.TryParse(parts[parts.Length - 1], out int v) && v < best) best = v;
            }
            return best == int.MaxValue ? "?" : best.ToString();
        }

        // ---------------------------------------------------------------------
        private static void Clear()
        {
            foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(
                         FindObjectsSortMode.None))
            {
                if (go.hideFlags == HideFlags.None && go.scene.IsValid())
                    UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static GameObject Box(string name, Vector3 pos, Vector3 size, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = size;

            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material m = new Material(sh);
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>
        /// Cerceveler, render eder ve olcumleri doner.
        /// x = masanin ekran boyu, y = masa arti sandalye, z = odanin boyu.
        /// roomDp verilirse butun odalarin dp degeri de yaziliyor.
        /// </summary>
        private static Vector3 Shoot(string path, Bounds target, Bounds roomBounds,
                                     List<string> roomDp,
                                     float top01 = 0f, float bottom01 = 0f,
                                     int w = ShotW, int h = ShotH)
        {
            GameObject camGo = new GameObject("Kamera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.94f, 0.91f);
            // ACILAR VE MESAFE OYUNUN KENDISINDEN.
            //
            // Burada bir zamanlar `32f`, `Euler(34, -12, 0)` ve kapali bir
            // mesafe formulu YAZILIYDI. CameraFit'in varlik sebebi tam
            // olarak bu: ayni hesabin iki kopyasi sessizce ayrisiyor - ve
            // ayristi da. Oyun ikili arama ile en kucuk mesafeyi buluyordu,
            // buradaki formul ise her terimde guvenli tarafa yanilip %30
            // fazla mesafe veriyordu. Yani OLCUM, oyunun gosterdiginden
            // KUCUK bir hedef bildiriyordu; sayilar guvenli taraftaydi ama
            // olculen sey oyun degildi.
            cam.fieldOfView = Lokanta.Game.CameraFit.FieldOfView;
            cam.aspect = w / (float)h;

            camGo.transform.rotation = Lokanta.Game.CameraFit.Rotation;
            camGo.transform.position =
                Lokanta.Game.CameraFit.Position(target, cam.aspect, top01, bottom01);

            RenderTexture rt = new RenderTexture(w, h, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D shot = new Texture2D(w, h, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            shot.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, shot.EncodeToPNG());

            float px = 0f, pxSet = 0f;
            GameObject probe = GameObject.Find("Masa0");
            if (probe != null)
            {
                px = Smaller(cam, probe.transform.position, 0.43f, 0.43f);
                pxSet = Smaller(cam, probe.transform.position, 0.93f, 0.93f);
            }
            float pxRoom = Smaller(cam, roomBounds.center,
                                   roomBounds.extents.x, roomBounds.extents.z);

            if (roomDp != null)
                foreach (Room r in Plan)
                    roomDp.Add(string.Format("{0} {1:0}", r.Name,
                        Dp(Smaller(cam, r.Center, r.W * 0.5f, r.D * 0.5f))));

            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(shot);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(camGo);
            return new Vector3(px, pxSet, pxRoom);
        }

        /// <summary>
        /// Hedefin ekrandaki iki ekseninin KUCUGU. Derinlik yonu 34 derece
        /// bakista 0,56 kat kisaliyor; yalnizca yatay olcmek hedefi
        /// oldugundan buyuk gosteriyor.
        /// </summary>
        private static float Smaller(Camera cam, Vector3 c, float halfX, float halfZ)
        {
            Vector3 a = cam.WorldToScreenPoint(c + new Vector3(-halfX, 0f, 0f));
            Vector3 b = cam.WorldToScreenPoint(c + new Vector3(halfX, 0f, 0f));
            float wide = Mathf.Abs(b.x - a.x);

            Vector3 p = cam.WorldToScreenPoint(c + new Vector3(0f, 0f, -halfZ));
            Vector3 q = cam.WorldToScreenPoint(c + new Vector3(0f, 0f, halfZ));
            float deep = Mathf.Abs(q.y - p.y);

            return Mathf.Min(wide, deep);
        }
    }
}
