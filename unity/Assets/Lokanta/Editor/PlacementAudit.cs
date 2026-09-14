using System.Collections.Generic;
using Lokanta.Game;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Sahnedeki nesneler BIRBIRINE GIRIYOR MU.
    ///
    /// Kullanicinin cumlesi: "Restorana yerlesen modeller ust uste
    /// binmis gibi." Goz karariyla bakmak yetmiyor - 34 derecelik bir
    /// bakista arkadaki bir nesne ondekinin ustune biniyormus gibi
    /// gorunebilir, ve gercekten binen iki nesne de masum durabilir.
    /// Bu arac soruyu olcuye ceviriyor: her nesnenin GERCEK POZDAKI
    /// kutusunu cikariyor ve kesisen ciftleri yaziyor.
    ///
    /// NEDEN BakeMesh: Renderer.bounds derili bir mesh'te yalan
    /// soyluyor. Unity onu kok kemikten turetiyor ve poz degistikce
    /// guncellemiyor - ilk olcumde OTURAN bir figur 1,68 m boyunda ve
    /// 1,66 m eninde gorundu, ikisi de imkansiz. BakeMesh pozlanmis
    /// mesh'i gercekten uretiyor, yani kutusu o anda ekranda gorunen
    /// seyin kutusu.
    ///
    ///   Unity.exe -batchmode -quit -projectPath ...
    ///     -executeMethod Lokanta.EditorTools.PlacementAudit.Run
    /// </summary>
    public static class PlacementAudit
    {
        /// <summary>
        /// Kesisme sayilmasi icin gereken en kucuk ortusme.
        ///
        /// Sifir olamaz: bir sandalye masaya DEGMELI, bir tabak tezgahin
        /// USTUNDE durmali. Dokunmak ust uste binmek degil. 4 cm, low-poly
        /// bir sahnede "iki nesne ayni yeri paylasiyor" demenin esigi.
        /// </summary>
        private const float Esik = 0.04f;

        [MenuItem("Lokanta/Yerlesim denetimi")]
        public static void Run()
        {
            try
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    "Assets/Lokanta/Oyun.unity");

                Debug.Log("=== Lokanta yerlesim denetimi ===");

                foreach (string cuisine in new[] { "turk", "fastfood" })
                {
                    Core.Sim.Simulation sim = GameShot.BuildFor(cuisine);
                    if (sim == null) continue;

                    RestaurantView view = Object.FindFirstObjectByType<RestaurantView>();
                    if (view == null) { Debug.LogError("SORUNLAR: RestaurantView yok"); return; }

                    view.Preview = sim;
                    view.PreviewPoses = true;
                    GameShot.Kick(view, "Awake");
                    view.Rebuild();
                    GameShot.Kick(view, "Update");

                    Denetle(cuisine, sim, view);

                    view.Preview = null;
                    view.PreviewPoses = false;
                    view.Clear();
                }

                Debug.Log("=== yerlesim denetimi tamam ===");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError("SORUNLAR: yerlesim -> " + e.GetType().Name + ": " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(2);
            }
        }

        private struct Nesne
        {
            public string Ad;
            public Bounds Kutu;
            public bool Figur;
            /// <summary>Hangi masa takimina ait. Yoksa bos.</summary>
            public string Masa;
        }

        private static void Denetle(string cuisine, Core.Sim.Simulation sim,
                                    RestaurantView view)
        {
            List<Nesne> hepsi = new List<Nesne>();

            // FIGURLER - pozlanmis mesh'ten.
            foreach (Figure f in view.GetComponentsInChildren<Figure>(true))
            {
                if (!f.gameObject.activeInHierarchy) continue;
                Bounds? b = PozKutusu(f.transform);
                if (b == null) continue;
                // MISAFIRIN MASASI KONUMDAN BULUNUYOR.
                //
                // Once ebeveyn zincirine bakiliyordu. Musteriler
                // yurumeye baslayinca gorunum onlari masadan ayirdi -
                // yuruyus dunya uzayinda olmali - ve eslesme kopunca
                // oturan her misafir kendi sandalyesiyle "cakisik"
                // sayildi: 45 sahte kayit.
                //
                // 1,2 m esigi: oturan figur en fazla 0,48 m uzakta
                // (SeatRadius), komsu masa 1,85 m otede.
                string masa = "";
                float enYakin = 1.2f;
                foreach (Transform mt in view.transform)
                {
                    if (!mt.name.StartsWith("Masa_")) continue;
                    float d = Vector3.Distance(
                        new Vector3(mt.position.x, 0f, mt.position.z),
                        new Vector3(b.Value.center.x, 0f, b.Value.center.z));
                    if (d < enYakin) { enYakin = d; masa = mt.name; }
                }

                hepsi.Add(new Nesne
                {
                    Ad = "figur " + f.transform.parent.name + "/" + f.name,
                    Kutu = b.Value,
                    Figur = true,
                    Masa = masa,
                });
            }

            // MOBILYA VE ESYA - RestaurantView'in dogrudan cocuklari,
            // figur tasimayanlar. Zemin ve rozet disarida: zemin her
            // seyin altinda, rozet her seyin ustunde.
            foreach (Transform t in view.transform)
            {
                if (!t.gameObject.activeInHierarchy) continue;
                if (t.name.StartsWith("Oda_")) continue;

                // DUVAR DISARIDA: oda sinirinda duruyor ve o sinira
                // dayali her tezgahla tanim geregi kesisiyor. Saydam,
                // carpisansiz, 6 cm kalinliginda bir levhanin "yerlesim
                // cakismasi" diye sayilmasi, denetimi gurultuye bogar.
                if (t.name == "Duvar" || t.name == "Kapi") continue;

                // SOKAK da disarida: arsanin disinda, zemin seviyesinde
                // ve hicbir seyle yarismiyor.
                if (t.name == "Kaldirim" || t.name == "Bordur"
                    || t.name == "Asfalt") continue;

                // SOKAK LAMBASI ve TAVAN ISIGI da disarida.
                //
                // Lamba arsanin disinda, bordurun uzerinde. Govde
                // hizasindaki tek parcasi 9 cm'lik direk ve yayalar ona
                // artik ETKIN OLARAK carpmiyor (StreetLife itismesi
                // direkleri de engel sayiyor) - o mesafe ayrica kendi
                // satirinda olculuyor. Kutuya girmesinin tek sebebi
                // figur kutusunun KOL ACIKLIGINI olcmesi; 1,14 m'lik bir
                // kol acikligi dar bir kaldirimda her seye "carpiyor".
                //
                // Tavan isigi govdesiz: yalnizca yerde yatan bir isik
                // levhasi. Zemin gibi, her seyin altinda.
                if (t.name == "SokakLambasi" || t.name == "TavanIsigi") continue;
                if (t.GetComponentInChildren<Figure>(true) != null
                    && !t.name.StartsWith("Masa_")) continue;

                if (t.name.StartsWith("Masa_"))
                {
                    // Masa takimi TEK PARCA degil: masa ve dort sandalye
                    // ayri nesneler ve birbirlerine degmeleri normal.
                    // Ayri ayri toplaniyorlar ki komsu takimla cakisma
                    // gorunsun.
                    foreach (Transform c in t)
                    {
                        // Figur BU DONGUDE sayilmiyor; yukarida zaten
                        // pozlanmis hali toplandi. Ilk yazimda
                        // GetComponent kullaniliyordu ve Figure bileseni
                        // bir alt cocukta oldugu icin her misafir IKI KEZ
                        // giriyordu - arac her figuru kendisiyle cakisik
                        // buluyordu.
                        if (c.GetComponentInChildren<Figure>(true) != null) continue;
                        if (c.name == "Rozet") continue;
                        Bounds? cb = PozKutusu(c);
                        if (cb != null)
                            hepsi.Add(new Nesne { Ad = t.name + "/" + c.name,
                                                  Kutu = cb.Value, Masa = t.name });
                    }
                    continue;
                }

                Bounds? b = PozKutusu(t);
                if (b != null) hepsi.Add(new Nesne { Ad = t.name, Kutu = b.Value });
            }

            int cakisan = 0, figurCakisan = 0;
            float enBuyuk = 0f;
            string enBuyukAd = "";

            for (int i = 0; i < hepsi.Count; i++)
            {
                for (int j = i + 1; j < hepsi.Count; j++)
                {
                    // AYNI MASA TAKIMININ PARCALARI HESAPLANMIYOR.
                    //
                    // Misafir sandalyesinin uzerinde oturuyor ve dizleri
                    // masanin altinda; ikisi de tanim geregi kesisiyor.
                    // Bunlari saymak, gercek cakismalari 47 sahte
                    // kaydin arasinda gizliyordu.
                    if (!string.IsNullOrEmpty(hepsi[i].Masa)
                        && hepsi[i].Masa == hepsi[j].Masa) continue;

                    Vector3 o = Ortusme(hepsi[i].Kutu, hepsi[j].Kutu);
                    if (o.x <= Esik || o.y <= Esik || o.z <= Esik) continue;

                    // YATAY ortusme olculuyor: bir tabagin tezgahin
                    // ustunde durmasi cakisma degil, ayni YER PARCASINI
                    // paylasmak cakisma.
                    float yatay = Mathf.Min(o.x, o.z);
                    cakisan++;
                    if (hepsi[i].Figur || hepsi[j].Figur) figurCakisan++;
                    if (yatay > enBuyuk)
                    {
                        enBuyuk = yatay;
                        enBuyukAd = hepsi[i].Ad + "  x  " + hepsi[j].Ad;
                    }

                    if (cakisan <= 12)
                        Debug.Log(string.Format(
                            "  CAKISMA {0,-34} x {1,-34} ortusme {2:0.00} x {3:0.00} m",
                            Kisa(hepsi[i].Ad), Kisa(hepsi[j].Ad), o.x, o.z));
                }
            }

            Debug.Log(string.Format(
                "  SONUC {0}: {1} nesne, {2} cakisan cift ({3} figurlu), en buyuk {4:0.00} m [{5}]",
                cuisine, hepsi.Count, cakisan, figurCakisan, enBuyuk, Kisa(enBuyukAd)));

            // Bir figurun ekranda kapladigi yer: ortalama ayak izi.
            float en = 0f, derin = 0f, boy = 0f;
            int n = 0;
            foreach (Nesne x in hepsi)
            {
                if (!x.Figur) continue;
                en += x.Kutu.size.x; derin += x.Kutu.size.z; boy += x.Kutu.size.y; n++;
            }
            if (n > 0)
            {
                Debug.Log(string.Format(
                    "  FIGUR {0}: oturan ortalama en {1:0.00} derinlik {2:0.00} boy {3:0.00} m"
                    + " | oturak araligi 0,88 m | masa araligi {4:0.00} m",
                    cuisine, en / n, derin / n, boy / n, RoomPlan.CellX));

                // MASA BASLARA DEGIYOR MU.
                //
                // Kullanici "masa karakterlerin basina degiyor gibi"
                // dedi. Olculebilir hali: oturan figurun CENE hizasi
                // (kutusunun ust noktasinin biraz altindaki govde) ile
                // masa tablasinin ustu arasindaki fark. Masa tablasi
                // 0,74 m; oturan bir insanin masasi gogus hizasinda
                // olmali, cene hizasinda degil.
                foreach (Nesne x in hepsi)
                {
                    if (!x.Figur) continue;
                    float basUstu = x.Kutu.max.y;
                    float masaUstu = 0f;
                    foreach (Nesne y in hepsi)
                    {
                        if (y.Figur || y.Masa != x.Masa) continue;
                        if (y.Ad.IndexOf("table", System.StringComparison.Ordinal) < 0) continue;
                        masaUstu = y.Kutu.max.y;
                        break;
                    }
                    if (masaUstu <= 0f) continue;
                    Debug.Log(string.Format(
                        "  MASA-BAS oturan figur ustu {0:0.00} m, masa tablasi {1:0.00} m"
                        + "  -> bas masanin {2:0.00} m ustunde",
                        basUstu, masaUstu, basUstu - masaUstu));
                    break;
                }

                // DURUS GERCEKTEN UYGULANIYOR MU.
                //
                // Yakin plan goruntude misafirler sandalyelerin ONUNDE
                // AYAKTA duruyordu ve minderler bostu. Bu, "figurler
                // buyuk" sorusundan AYRI bir sey: poz hic
                // uygulanmiyorsa olcek ne olursa olsun yanlis gorunur.
                //
                // Ayni figur iki pozda olculuyor. Kutular AYNI cikarsa
                // klip etkisiz demektir; oturan bir govde ayakta
                // durandan belirgin sekilde ALCAK olmali.
                foreach (Figure f in view.GetComponentsInChildren<Figure>(true))
                {
                    if (!f.gameObject.activeInHierarchy) continue;

                    f.Sample(Figure.Pose.Idle, 0.4f);
                    Bounds? ayakta = PozKutusu(f.transform);
                    f.Sample(Figure.Pose.Sit, 0.4f);
                    Bounds? oturan = PozKutusu(f.transform);
                    if (ayakta == null || oturan == null) break;

                    Vector3 a = ayakta.Value.size, o = oturan.Value.size;
                    bool degisti = Mathf.Abs(a.y - o.y) > 0.05f
                                   || Mathf.Abs(a.z - o.z) > 0.05f;
                    Debug.Log(string.Format(
                        "  DURUS idle {0:0.00}x{1:0.00}x{2:0.00} | sit {3:0.00}x{4:0.00}x{5:0.00}"
                        + "  -> {6}",
                        a.x, a.y, a.z, o.x, o.y, o.z,
                        degisti ? "poz UYGULANIYOR" : "POZ UYGULANMIYOR - klip etkisiz"));
                    break;
                }

                // ESYANIN DOGRULTUSU.
                //
                // Kullanicinin istegi: "restoran icerisindeki esyalarin
                // yerlesimini ve dogrultularini kontrol et".
                //
                // Kural olculebilir: DUVARA DAYALI bir esya odanin
                // ICINE bakmali. Ocagin agzi duvara donmus olamaz,
                // tezgahin on yuzu duvara bakamaz. Esyanin "onu"
                // RestaurantView.PropYaw kuralindan geliyor (mobilya
                // yaw 0'da -Z'ye bakiyor), yani burada ikinci bir
                // varsayim yok.
                //
                // NEDEN OLCUM: "gozle bakildi" bir kez dogrudur; kat
                // plani ya da yerlesim degisince kimse yeniden bakmaz.
                {
                    int bakilan = 0, ters = 0;
                    var kotu = new System.Text.StringBuilder();

                    foreach (Transform t in view.transform)
                    {
                        if (!t.gameObject.activeInHierarchy) continue;
                        if (t.GetComponentInChildren<Figure>(true) != null) continue;
                        if (t.name.StartsWith("Oda_") || t.name == "Duvar"
                            || t.name == "Kapi" || t.name == "Kaldirim"
                            || t.name == "Bordur" || t.name == "Asfalt"
                            || t.name == "SokakLambasi"
                            // ESIK PASPASI yatay bir levha: "on yuzu"
                            // yok, dolayisiyla dogrultu kurali da yok.
                            // Denetim onu duvara donuk sayiyordu ve bu
                            // bir hata degil, kuralin o nesneye
                            // uymamasiydi.
                            || t.name == "Esik"
                            || t.name.StartsWith("Masa_")) continue;

                        Vector3 p = t.localPosition;
                        int oda = Lokanta.Game.Paths.RoomAt(p);
                        if (oda < 0) continue;

                        RoomPlan.Room r = RoomPlan.Rooms[oda];

                        // Hangi duvara dayali: en yakin kenar.
                        float dSol = p.x - r.X0;
                        float dSag = r.X0 + r.W - p.x;
                        float dOn = p.z - r.Z0;
                        float dArka = r.Z0 + r.D - p.z;
                        float enYakin = Mathf.Min(Mathf.Min(dSol, dSag), Mathf.Min(dOn, dArka));
                        if (enYakin > 1.0f) continue;   // ortada duran esya: kurali yok

                        Vector3 iceri;
                        if (enYakin == dSol) iceri = Vector3.right;
                        else if (enYakin == dSag) iceri = Vector3.left;
                        else if (enYakin == dOn) iceri = Vector3.forward;
                        else iceri = Vector3.back;

                        // Mobilyanin onu: yaw 0'da -Z (RestaurantView.PropYaw).
                        Vector3 on = t.localRotation * Vector3.back;
                        on.y = 0f;

                        bakilan++;
                        float aci = Vector3.Angle(on, iceri);
                        if (aci > 100f)
                        {
                            ters++;
                            if (kotu.Length < 220)
                                kotu.Append(t.name + "(" + r.Name + ","
                                            + aci.ToString("0") + ") ");
                        }
                    }

                    Debug.Log(string.Format(
                        "  YON {0} esya duvara dayali, {1} tanesi duvara donuk {2}",
                        bakilan, ters, kotu.ToString()));
                }

                // KENDINI DOGRULAMA: ayakta duran bir personel figurunun
                // olculen boyu, ArtPrefabs'in hedefiyle ayni cikmali.
                // Cikmiyorsa olculen sey bu sahnedeki figur degil.
                foreach (Figure f in view.GetComponentsInChildren<Figure>(true))
                {
                    if (!f.gameObject.activeInHierarchy) continue;
                    if (f.Current == Figure.Pose.Sit) continue;

                    // AYAKTA DURUŞU ACIKCA ORNEKLE.
                    //
                    // Onceden figur SAHNEDEKI durusuyla olculuyordu ve
                    // mutfak isleri eklenince o duruş artik "ayakta"
                    // degildi: dograyan asci 0,90 m olctu, hedef 1,00.
                    // Kontrolun adi "ayakta figur boyu" - o zaman
                    // ayakta durmasi saglanmali.
                    f.Sample(Figure.Pose.Idle, 0.4f);
                    Bounds? b = PozKutusu(f.transform);
                    if (b == null) continue;
                    Debug.Log(string.Format(
                        "  DOGRULAMA ayakta figur boyu {0:0.00} m (ArtPrefabs hedefi"
                        + " referans model icin " + ArtPrefabs.CharacterHeight.ToString("0.00")
                        + ") - sapma buyukse olcum yanlis",
                        b.Value.size.y));

                    // GOVDE GENISLIGI: sokakta iki yayanin arasindaki
                    // en az mesafeyi (StreetLife.Personal) bu sayi
                    // belirliyor.
                    //
                    // Neden olculmesi gerekti: kullanici yayalarin
                    // birbirinin icinden gectigini gordu ve cozum iki
                    // yaya seridi ile itisme oldu - ikisi de bir MESAFE
                    // istiyor. O mesafeyi tahmin etmek, ayni hatanin
                    // ikinci kez yapilmasi olurdu.
                    //
                    // NEDEN SINIRLAYICI KUTU DEGIL: ilk yazim kutuyu
                    // kullandi ve 0,60 x 1,14 m olctu - bir metre boyunda
                    // bir figur icin sacma bir ayak izi. Kutu KOL
                    // ACIKLIGINI olcuyor; bu paketin figurleri
                    // boylarindan cok enleriyle buyuk (ayni tuzak oturma
                    // olcumunde de yasandi, docs/35). Iki yayanin
                    // "birbirinin icinden gectigi" ise kollarinin degil
                    // GOVDELERININ ust uste binmesi: 34 derecelik bir
                    // bakista salinan bir kol gorulmuyor, ic ice gecen
                    // iki bacak goruluyor.
                    //
                    // IKINCI OLCUM DE YANILDI: kalca hizasi (boyun
                    // %18-%50) 1,08 m verdi, cunku bu oranlarda ELLER de
                    // o hizada duruyor. Iki bant da "makul" bir
                    // gerekceyle secilmisti ve ikisi de kol olcuyordu.
                    //
                    // Dogru bandi bulmanin tek yolu BUTUN PROFILI basmak
                    // oldu; Profil() o yuzden kalici bir tani. Kullanilan
                    // sayi kollar disinda en genis bant - bu figurlerde
                    // BAS (0,67 m), omuzlardan (0,58) genis.
                    YuruyusHizi(f);
                    f.Sample(Figure.Pose.Idle, 0.4f);
                    b = PozKutusu(f.transform);
                    if (b == null) break;

                    float govde = GovdeGenisligi(f.transform, b.Value);
                    Debug.Log(string.Format(
                        "  GOVDE kollar disinda en genis bant {0:0.00} m"
                        + " (kutu {1:0.00} x {2:0.00} - KOLLARLA)"
                        + " | yaya en az mesafesi {3:0.00} | serit araligi {4:0.00}"
                        + " - {5}",
                        govde, b.Value.size.x, b.Value.size.z,
                        StreetLife.Personal, Paths.LaneHalf * 2f,
                        (StreetLife.Personal >= govde && Paths.LaneHalf * 2f >= govde)
                            ? "mesafe yeterli"
                            : "MESAFE KISA - Personal / LaneHalf buyutulmeli"));
                    break;
                }
            }
        }

        /// <summary>
        /// KOLLAR DISINDA figurun en genis yatay bandi.
        ///
        /// Iki yayanin ne kadar yakin gecebilecegi sorusunun cevabi bu.
        /// Sinirlayici kutu kol acikligini olcuyor (1,14 m) ve el
        /// hizasindaki bantlar da (1,08 m) - ikisi de yanlis. Kollar
        /// govdenin alt yarisinda sarkiyor, o yuzden olcum GOVDENIN UST
        /// YARISINA bakiyor: omuzlar, bas ve arasi. Bu paketin
        /// oranlarinda en genis olan BAS (kafa govdenin ucte biri).
        ///
        /// Pisirilmis mesh'in koseleri okunabiliyor; paketin kendi
        /// mesh'i okunamaz (isReadable: 0) ve bu ayrimi bilmemek bu
        /// projede bir kez bos bir olcume yol acti.
        /// </summary>
        private static float GovdeGenisligi(Transform t, Bounds kutu)
        {
            Profil(t, kutu);

            float enBuyuk = 0f;
            int toplamKose = 0;

            // Ust yarinin bantlari: %50'den %100'e, onda birer.
            for (int k = 5; k < 10; k++)
            {
                float y0 = kutu.min.y + kutu.size.y * (k / 10f);
                float y1 = kutu.min.y + kutu.size.y * ((k + 1) / 10f);
                int sayi;
                float g = BantCapi(t, y0, y1, out sayi);
                toplamKose += sayi;
                if (g > enBuyuk) enBuyuk = g;
            }

            // KOSE BULUNAMADI ise sifir dondurmuyoruz: sifir "govde yok"
            // demek ve o, her esigi sessizce gecen bir sayi. Kutunun
            // kendisi donuyor - guvenli tarafta, gorunur sekilde buyuk.
            if (toplamKose < 8)
            {
                Debug.LogWarning("  GOVDE olcumu bos: " + toplamKose
                                 + " kose bulundu, kutuya donuluyor");
                return Mathf.Max(kutu.size.x, kutu.size.z);
            }
            return enBuyuk;
        }

        /// <summary>Bir y bandindaki en buyuk yatay cap.</summary>
        private static float BantCapi(Transform t, float y0, float y1, out int sayi)
        {
            float xa = float.MaxValue, xb = float.MinValue;
            float za = float.MaxValue, zb = float.MinValue;
            sayi = 0;

            foreach (SkinnedMeshRenderer smr in
                     t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null || !smr.gameObject.activeInHierarchy) continue;

                Mesh baked = new Mesh();
                smr.BakeMesh(baked, false);
                Vector3[] v = baked.vertices;
                Object.DestroyImmediate(baked);

                // Olcek BakeMesh ciktisinin icinde (kemikler olcekli
                // kokun altinda); yalnizca konum ve donme uygulaniyor -
                // PozKutusu ayni gerekceyi tasiyor.
                Matrix4x4 m = Matrix4x4.TRS(smr.transform.position,
                                            smr.transform.rotation, Vector3.one);
                for (int i = 0; i < v.Length; i++)
                {
                    Vector3 p = m.MultiplyPoint3x4(v[i]);
                    if (p.y < y0 || p.y > y1) continue;
                    if (p.x < xa) xa = p.x;
                    if (p.x > xb) xb = p.x;
                    if (p.z < za) za = p.z;
                    if (p.z > zb) zb = p.z;
                    sayi++;
                }
            }
            return sayi == 0 ? 0f : Mathf.Max(xb - xa, zb - za);
        }

        /// <summary>
        /// Figurun yukseklige gore YATAY PROFILI - tek satirlik bir tani
        /// degil, on bant.
        ///
        /// Neden kalici: bu figurlerin hangi hizada ne kadar genis
        /// oldugunu tek bir sayi soylemiyor. Kutu 1,14 dedi (kol), kalca
        /// bandi 1,08 dedi (el), omuz 0,58, bas 0,67. Bu dordu ancak yan
        /// yana gorununce anlasildi ve yaya seridi araligi o profile
        /// bakilarak secildi. Figur olcegi ya da paketi degisirse
        /// bakilacak yer bu satirlar.
        /// </summary>
        private static void Profil(Transform t, Bounds kutu)
        {
            for (int k = 0; k < 10; k++)
            {
                float y0 = kutu.min.y + kutu.size.y * (k / 10f);
                float y1 = kutu.min.y + kutu.size.y * ((k + 1) / 10f);
                int n;
                float cap = BantCapi(t, y0, y1, out n);
                Debug.Log(string.Format(
                    "  PROFIL y {0:0.00}-{1:0.00} kose {2,4} | en genis cap {3:0.00} m",
                    y0, y1, n, cap));
            }
        }

        /// <summary>
        /// YURUYUS KLIBININ DOGAL HIZI (m/sn).
        ///
        /// Neden olculmesi gerekti: figurun yer hizi (Walker.Speed x oyun
        /// hizi) ile klibin oynatma hizi BIRBIRINE BAGLI DEGILDI -
        /// Anim.speed hicbir yerde ayarlanmiyordu. Yani figur x4'te dort
        /// kat hizli gidiyor ama bacaklar ayni tempoda: ayaklar yerde
        /// kayiyor. Kullanicinin gordugu sey buydu.
        ///
        /// Duzeltmek icin klibin KENDI hizi lazim ve o sayi hicbir yerde
        /// yazmiyor (klip yerinde sayan bir dongu; kok hareketi yok).
        /// Olcum:
        ///
        ///   adim boyu = cevrim boyunca iki ayagin EN UZAK acilmasi
        ///   bir cevrim = iki adim
        ///   dogal hiz  = 2 x adim / klip suresi
        ///
        /// Ayaklar KEMIKTEN degil MESH'ten bulunuyor: bu iskelette ayak
        /// kemigi yok (bacak basina tek kemik + sonradan eklenen diz).
        /// Her bacagin etkiledigi koseler arasindan EN ALCAK olani o
        /// bacagin ayagi sayiliyor.
        /// </summary>
        private static void YuruyusHizi(Figure f)
        {
            if (f == null || f.Clips == null) return;
            int idx = (int)Figure.Pose.Walk;
            if (idx >= f.Clips.Length || f.Clips[idx] == null) return;

            AnimationClip klip = f.Clips[idx];
            float sure = klip.length;
            if (sure <= 0.001f) return;

            SkinnedMeshRenderer smr = null;
            foreach (SkinnedMeshRenderer r in f.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (r.sharedMesh != null && r.sharedMesh.isReadable) { smr = r; break; }
            if (smr == null)
            {
                Debug.Log("  YURUYUS olculemedi: okunabilir mesh yok");
                return;
            }

            int sol = BoneIndex(smr.bones, "knee-left");
            int sag = BoneIndex(smr.bones, "knee-right");
            if (sol < 0) sol = BoneIndex(smr.bones, "leg-left");
            if (sag < 0) sag = BoneIndex(smr.bones, "leg-right");
            if (sol < 0 || sag < 0)
            {
                Debug.Log("  YURUYUS olculemedi: bacak kemigi bulunamadi");
                return;
            }

            BoneWeight[] w = smr.sharedMesh.boneWeights;
            const int Ornek = 24;
            float enUzak = 0f;

            for (int i = 0; i < Ornek; i++)
            {
                f.Sample(Figure.Pose.Walk, sure * i / Ornek);

                Mesh baked = new Mesh();
                smr.BakeMesh(baked, false);
                Vector3[] v = baked.vertices;
                Object.DestroyImmediate(baked);
                if (v.Length != w.Length) return;

                Vector3 aSol = Ayak(v, w, sol);
                Vector3 aSag = Ayak(v, w, sag);
                if (aSol == Vector3.zero || aSag == Vector3.zero) continue;

                Vector3 d = aSol - aSag;
                d.y = 0f;
                if (d.magnitude > enUzak) enUzak = d.magnitude;
            }

            f.Sample(Figure.Pose.Idle, 0.4f);

            if (enUzak <= 0.001f)
            {
                Debug.Log("  YURUYUS olculemedi: adim bulunamadi");
                return;
            }

            float dogal = 2f * enUzak / sure;
            Debug.Log(string.Format(
                "  YURUYUS klip {0:0.00} sn | adim {1:0.000} m | DOGAL HIZ {2:0.00} m/sn"
                + " | Figure.WalkClipSpeed {3:0.00} - {4}",
                sure, enUzak, dogal, Figure.WalkClipSpeed,
                Mathf.Abs(dogal - Figure.WalkClipSpeed) < 0.12f
                    ? "uyumlu" : "SAPMA VAR - WalkClipSpeed guncellenmeli"));
        }

        /// <summary>Bir bacagin etkiledigi koseler arasinda EN ALCAK olan.</summary>
        private static Vector3 Ayak(Vector3[] v, BoneWeight[] w, int bone)
        {
            Vector3 best = Vector3.zero;
            float low = float.MaxValue;
            for (int i = 0; i < v.Length; i++)
            {
                if (!Etkiliyor(w[i], bone)) continue;
                if (v[i].y >= low) continue;
                low = v[i].y;
                best = v[i];
            }
            return best;
        }

        private static bool Etkiliyor(BoneWeight b, int bone)
        {
            return (b.boneIndex0 == bone && b.weight0 > 0.5f)
                || (b.boneIndex1 == bone && b.weight1 > 0.5f)
                || (b.boneIndex2 == bone && b.weight2 > 0.5f)
                || (b.boneIndex3 == bone && b.weight3 > 0.5f);
        }

        private static int BoneIndex(Transform[] bones, string ad)
        {
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null && bones[i].name == ad) return i;
            return -1;
        }

        private static string Kisa(string s)
        {
            s = s.Replace("(Clone)", "").Replace("figur ", "");
            return s.Length <= 34 ? s : s.Substring(0, 34);
        }

        /// <summary>Iki kutunun her eksende ortusme miktari.</summary>
        private static Vector3 Ortusme(Bounds a, Bounds b)
        {
            return new Vector3(
                Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x),
                Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y),
                Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z));
        }

        /// <summary>
        /// Nesnenin GERCEK POZDAKI dunya kutusu. Derili mesh'ler
        /// pozlanip olculuyor, digerleri dogrudan.
        /// </summary>
        /// <summary>
        /// Pozlanmis sinir kutusu. FigureShot da BURADAN cagiriyor:
        /// ikinci bir kopya yazmak, bu projede bes kez sessizce ayristi.
        /// </summary>
        internal static Bounds? PozKutusu(Transform t)
        {
            Bounds? acc = null;

            foreach (SkinnedMeshRenderer smr in
                     t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null || !smr.gameObject.activeInHierarchy) continue;

                // OLCEK BIR KEZ. Bu iki kez yanlis yapildi:
                //
                //   1. useScale=true + localToWorldMatrix -> olcek iki kez
                //   2. useScale=false + localToWorldMatrix -> YINE iki kez
                //
                // Sebep ikincisinde gorundu: BakeMesh mesh'i KEMIKLERE
                // gore deforme ediyor ve kemikler zaten olcekli kokun
                // altinda duruyor, yani ciktinin icinde olcek VAR.
                // useScale bayragi yalnizca RENDERER'IN kendi olcegini
                // ekliyor. Dogrusu: bayrak kapali ve donusumden olcek
                // cikarilmis - yalnizca konum ve donme.
                //
                // Bu araci yazma sebebi "yanlis seyi olcmek"ti ve arac
                // kendisi iki kez tam onu yapti. Asagidaki DOGRULAMA
                // satiri o yuzden var: olculen boy ArtPrefabs'taki
                // hedefle karsilastiriliyor, tutmuyorsa sayilar cope.
                Mesh baked = new Mesh();
                smr.BakeMesh(baked, false);
                Bounds lb = baked.bounds;
                Object.DestroyImmediate(baked);

                Matrix4x4 m = Matrix4x4.TRS(smr.transform.position,
                                            smr.transform.rotation, Vector3.one);
                acc = Birlestir(acc, Cevir(lb, m));
            }

            foreach (MeshRenderer mr in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!mr.gameObject.activeInHierarchy) continue;
                if (mr.name == "Rozet" || mr.transform.parent != null
                    && mr.transform.parent.name == "Rozet") continue;
                acc = Birlestir(acc, mr.bounds);
            }

            return acc;
        }

        private static Bounds? Birlestir(Bounds? a, Bounds b)
        {
            if (a == null) return b;
            Bounds x = a.Value;
            x.Encapsulate(b);
            return x;
        }

        private static Bounds Cevir(Bounds b, Matrix4x4 m)
        {
            Vector3 c = m.MultiplyPoint3x4(b.center);
            Vector3 e = b.extents;
            Vector3 ne = new Vector3(
                Mathf.Abs(m.m00) * e.x + Mathf.Abs(m.m01) * e.y + Mathf.Abs(m.m02) * e.z,
                Mathf.Abs(m.m10) * e.x + Mathf.Abs(m.m11) * e.y + Mathf.Abs(m.m12) * e.z,
                Mathf.Abs(m.m20) * e.x + Mathf.Abs(m.m21) * e.y + Mathf.Abs(m.m22) * e.z);
            return new Bounds(c, ne * 2f);
        }
    }
}
