using Lokanta.Game;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// TEK figur, TEK sandalye, yandan. Baska hicbir sey yok.
    ///
    /// Neden gerekti: "misafirler oturuyor mu, yoksa sandalyenin onunde
    /// ayakta mi duruyorlar" sorusu dolu bir salonda cevaplanamiyor -
    /// 34 derecelik bakista onde duran bir figur oturuyormus gibi de
    /// gorunebiliyor, oturan bir figur ayaktaymis gibi de. Olcum araci
    /// da celisik sayilar verdi (ayni figur bir yerde 1,27 m, baska bir
    /// yerde 1,60 m).
    ///
    /// Bu goruntude belirsizlik kalmiyor: kamera yandan bakiyor,
    /// sandalyenin minderi ve figurun kalcasi ayni karede.
    ///
    ///   -executeMethod Lokanta.EditorTools.FigureShot.Capture
    /// </summary>
    public static class FigureShot
    {
        private const string PrefabDir = "Assets/Lokanta/Art/Prefab";

        [MenuItem("Lokanta/Figur olcek goruntusu")]
        public static void Capture()
        {
            try
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    "Assets/Lokanta/Oyun.unity");

                GameObject sandalye = Load("Mobilya/chairCushion");
                GameObject karakter = Load("Karakter/character-male-a");
                if (sandalye == null || karakter == null)
                {
                    Debug.LogError("SORUNLAR: prefab yok");
                    if (Application.isBatchMode) EditorApplication.Exit(2);
                    return;
                }

                GameObject kok = new GameObject("OlcekSahnesi");

                // Zemin: olcegi okuyabilmek icin 1 m'lik kareler.
                for (int i = -2; i <= 2; i++)
                {
                    Slab(kok.transform, new Vector3(i * 1.0f, -0.01f, 0f),
                         new Vector3(0.02f, 0.02f, 4f), new Color(0.5f, 0.5f, 0.5f));
                    Slab(kok.transform, new Vector3(0f, -0.01f, i * 1.0f),
                         new Vector3(4f, 0.02f, 0.02f), new Color(0.5f, 0.5f, 0.5f));
                }
                Slab(kok.transform, new Vector3(0f, -0.06f, 0f),
                     new Vector3(6f, 0.1f, 6f), new Color(0.30f, 0.25f, 0.20f));

                // Sandalye orijinde, arkasi kameranin solunda.
                // +180: oyundaki ile AYNI. Sandalye modeli karakterin
                // tersine bakiyor (asagidaki yon goruntusu).
                GameObject s = Object.Instantiate(sandalye, kok.transform);
                s.transform.localPosition = Vector3.zero;
                s.transform.localRotation = Quaternion.Euler(0f, 90f + 180f, 0f);

                // Figur: oyundaki ILE AYNI hesap - oturak yeri ve kaldirma
                // RestaurantView'den okunuyor, burada ikinci bir sayi
                // yazmiyoruz. Ayni sayiyi iki yere yazmak bu projede
                // bes kez sessizce ayristi.
                // Sandalye +X'e bakiyor (90+180), yani masa +X'te:
                // figurun one kaymasi da +X yonunde.
                GameObject k = Object.Instantiate(karakter, kok.transform);
                k.transform.localPosition = new Vector3(
                    RestaurantView.SitForwardM, RestaurantView.SitLiftM, 0f);
                k.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

                Figure f = k.GetComponentInChildren<Figure>(true);
                if (f != null) f.Sample(Figure.Pose.Sit, 0.4f);

                // Cerceve DAR: figurun bacagi ile sandalyenin minderi
                // arasindaki birkac santim, 2,6 m'lik bir kutuda zaten
                // okunmuyordu - "oturuyor mu" sorusunu cevaplamak icin
                // yazilmis bir goruntu, o kadar uzaktan cevaplamiyor.
                Bounds hedef = new Bounds(new Vector3(0f, 0.55f, 0f),
                                          new Vector3(1.3f, 1.1f, 1.3f));
                GameShot.Shoot("olcek_oturma.png", hedef, 1280, 720,
                               pitch: 8f, yaw: 0f);

                // SANDALYESIZ OTURUS: duruşun kendisi, hicbir sey
                // onunde durmadan. Sandalye siluetin yarisini kapatiyor
                // ve "uyluk mu baldir mi" sorusu cevapsiz kaliyor.
                s.SetActive(false);
                GameShot.Shoot("olcek_oturma_yalin.png", hedef, 1280, 720,
                               pitch: 8f, yaw: 0f);
                s.SetActive(true);

                // BACAK ACILARI SUPURULUYOR.
                //
                // Tek bir goruntuden "uyluk mu baldir mi dondu" sorusu
                // cevaplanamadi: sandalye siluetin yarisini kapatiyor ve
                // kol ile bacak ayni renkte. Dort ayri aci yan yana
                // konunca mekanizma bir bakista okunuyor.
                {
                    float eskiU = Figure.ThighAngle, eskiB = Figure.ShinTilt;
                    float[,] deneme = { { 0f, 0f }, { -45f, 0f },
                                        { -78f, 0f }, { -78f, -40f } };
                    for (int d = 0; d < 4; d++)
                    {
                        Figure.ThighAngle = deneme[d, 0];
                        Figure.ShinTilt = deneme[d, 1];
                        GameObject kopya = Object.Instantiate(karakter, kok.transform);
                        kopya.transform.localPosition =
                            new Vector3(-1.2f + d * 0.8f, RestaurantView.SitLiftM, 2.5f);
                        kopya.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        Figure kf = kopya.GetComponentInChildren<Figure>(true);
                        if (kf != null) kf.Sample(Figure.Pose.Sit, 0.4f);
                    }
                    Figure.ThighAngle = eskiU;
                    Figure.ShinTilt = eskiB;

                    GameShot.Shoot("olcek_bacak_supurme.png",
                                   new Bounds(new Vector3(0.05f, 0.55f, 2.5f),
                                              new Vector3(3.6f, 1.2f, 1.2f)),
                                   1600, 620, pitch: 8f, yaw: 0f);
                }

                if (f != null) f.Sample(Figure.Pose.Idle, 0.4f);
                k.transform.localPosition = new Vector3(-0.9f, 0f, 0f);
                GameShot.Shoot("olcek_ayakta.png", hedef, 1280, 720,
                               pitch: 8f, yaw: 0f);

                // --- SANDALYE HANGI YONE BAKIYOR ---------------------
                //
                // "Sandalyeler ters" gozle soylenebilir ama hangi eksene
                // gore ters oldugu soylenemez. Bu goruntu onu kesin
                // yapiyor: sandalye DONDURULMEDEN (yaw 0) duruyor,
                // KIRMIZI kup +Z'de, MAVI kup -Z'de. Sirtlik hangi
                // kupun tarafindaysa sandalyenin "arkasi" o yon.
                //
                // RestaurantView oturaklari Seat(k) = rot(k*90) *
                // (0,0,-r) ile diziyor, yani k=0 sandalyesi -Z'de ve
                // masaya bakmasi icin +Z'ye donuk olmali.
                // Tersten: DestroyImmediate cocuk listesini ANINDA
                // degistiriyor, ileri giden bir dongu yarisini atliyor.
                for (int c = kok.transform.childCount - 1; c >= 0; c--)
                    Object.DestroyImmediate(kok.transform.GetChild(c).gameObject);

                Slab(kok.transform, new Vector3(0f, -0.06f, 0f),
                     new Vector3(6f, 0.1f, 6f), new Color(0.30f, 0.25f, 0.20f));

                GameObject s2 = Object.Instantiate(sandalye, kok.transform);
                s2.transform.localPosition = Vector3.zero;
                s2.transform.localRotation = Quaternion.identity;   // yaw 0

                Slab(kok.transform, new Vector3(0f, 0.12f, 0.62f),
                     new Vector3(0.22f, 0.22f, 0.22f), new Color(0.90f, 0.20f, 0.18f));
                Slab(kok.transform, new Vector3(0f, 0.12f, -0.62f),
                     new Vector3(0.22f, 0.22f, 0.22f), new Color(0.20f, 0.45f, 0.95f));

                GameShot.Shoot("olcek_sandalye.png",
                               new Bounds(new Vector3(0f, 0.45f, 0f),
                                          new Vector3(2.0f, 1.2f, 2.0f)),
                               1280, 720, pitch: 12f, yaw: 90f);

                // FIGUR HANGI YONE BAKIYOR - ayni yontem.
                //
                // Sandalyeyle figur AYNI aciyi (k*90) kullaniyor; ikisinin
                // yerel "on" yonu ayni olmak ZORUNDA degil ve bunu
                // varsaymak, ikisini birden ters cevirmek demek olurdu.
                Object.DestroyImmediate(s2);
                GameObject k2 = Object.Instantiate(karakter, kok.transform);
                k2.transform.localPosition = Vector3.zero;
                k2.transform.localRotation = Quaternion.identity;   // yaw 0
                Figure f2 = k2.GetComponentInChildren<Figure>(true);
                if (f2 != null) f2.Sample(Figure.Pose.Idle, 0.4f);

                GameShot.Shoot("olcek_figur_yon.png",
                               new Bounds(new Vector3(0f, 0.55f, 0f),
                                          new Vector3(2.0f, 1.4f, 2.0f)),
                               1280, 720, pitch: 12f, yaw: 90f);

                // BUTUN MOBILYA, AYNI ISARETLE, TEK KAREDE.
                //
                // Sandalyenin ters oldugu ortaya cikinca ayni sinifin
                // baska ornekleri olup olmadigi soruldu: her modelin
                // yerel "on" yonu ayri ayri olculmeli, varsayilmamali.
                // Hepsi yaw 0'da duruyor; kirmizi kup her birinin +Z'si.
                for (int c = kok.transform.childCount - 1; c >= 0; c--)
                    Object.DestroyImmediate(kok.transform.GetChild(c).gameObject);

                Slab(kok.transform, new Vector3(0f, -0.06f, 0f),
                     new Vector3(14f, 0.1f, 6f), new Color(0.30f, 0.25f, 0.20f));

                string[] adlar =
                {
                    "Mobilya/chairCushion", "Mobilya/tableRound",
                    "Mobilya/kitchenStove", "Mobilya/kitchenBar",
                    "Mobilya/kitchenSink", "Mobilya/kitchenFridgeLarge",
                    "Mobilya/bookcaseClosedDoors",
                };
                for (int i = 0; i < adlar.Length; i++)
                {
                    GameObject p = Load(adlar[i]);
                    if (p == null) { Debug.LogWarning("prefab yok: " + adlar[i]); continue; }
                    float x = (i - (adlar.Length - 1) * 0.5f) * 1.7f;

                    GameObject g = Object.Instantiate(p, kok.transform);
                    g.transform.localPosition = new Vector3(x, 0f, 0f);
                    g.transform.localRotation = Quaternion.identity;

                    Slab(kok.transform, new Vector3(x, 0.10f, 0.75f),
                         new Vector3(0.20f, 0.20f, 0.20f), new Color(0.90f, 0.20f, 0.18f));
                }

                GameShot.Shoot("olcek_mobilya_yon.png",
                               new Bounds(new Vector3(0f, 0.7f, 0f),
                                          new Vector3(13f, 2.2f, 3.0f)),
                               1600, 600, pitch: 18f, yaw: 0f);

                // OCAK TEPEDEN: alev gozun uzerinde mi.
                //
                // "Alev biraz kaymis gibi" goz karariyla soylenebilir
                // ama ne kadar kaydigi soylenemez - 34 derecelik bakista
                // ust yuzeydeki her sey egik goruunuyor. Tam tepeden
                // bakinca goz ile alev ayni karede, ayni duzlemde.
                for (int c = kok.transform.childCount - 1; c >= 0; c--)
                    Object.DestroyImmediate(kok.transform.GetChild(c).gameObject);

                GameObject ocakPrefab = Load("Mobilya/kitchenStove");
                if (ocakPrefab != null)
                {
                    GameObject ocak = Object.Instantiate(ocakPrefab, kok.transform);
                    ocak.transform.localPosition = Vector3.zero;
                    ocak.transform.localRotation = Quaternion.identity;

                    Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                    Material m = unlit != null ? new Material(unlit) : null;
                    Appliance ap = Appliance.Attach(ocak, m, null);
                    if (ap != null) ap.SetWorking(true);

                    GameShot.Shoot("olcek_ocak_ustten.png",
                                   new Bounds(new Vector3(0f, 0.5f, 0f),
                                              new Vector3(1.1f, 0.4f, 1.1f)),
                                   900, 900, pitch: 89.9f, yaw: 0f);
                }

                // MASA TEPEDEN: oturaklar kenarda mi kosede mi.
                //
                // Masa alti kenarli (low-poly yuvarlak) ve oturaklar
                // 90 derece araliklarla diziliyor - altigenin kenarlari
                // ise 60 derecede. Ikisi hizalanmazsa misafir masanin
                // KOSESINE oturuyor. Bu ancak tam tepeden bakinca
                // olculebilir.
                for (int c = kok.transform.childCount - 1; c >= 0; c--)
                    Object.DestroyImmediate(kok.transform.GetChild(c).gameObject);

                GameObject masaPrefab = Load("Mobilya/table");
                if (masaPrefab != null)
                {
                    GameObject m2 = Object.Instantiate(masaPrefab, kok.transform);
                    m2.transform.localPosition = Vector3.zero;
                    m2.transform.localRotation = Quaternion.identity;
                    // Oyunla AYNI: masa karelestiriliyor.
                    Renderer[] mr = m2.GetComponentsInChildren<Renderer>(true);
                    if (mr.Length > 0)
                    {
                        Bounds mb = mr[0].bounds;
                        for (int i = 1; i < mr.Length; i++) mb.Encapsulate(mr[i].bounds);
                        if (mb.size.z > 0.0001f)
                            m2.transform.localScale =
                                new Vector3(1f, 1f, mb.size.x / mb.size.z);
                    }

                    for (int oturak = 0; oturak < 4; oturak++)
                    {
                        GameObject ch = Object.Instantiate(sandalye, kok.transform);
                        // Oyunla AYNI hesap: yaricap eksene gore
                        // degisiyor (dortgen masa).
                        ch.transform.localPosition = RestaurantView.SeatAt(oturak);
                        ch.transform.localRotation =
                            Quaternion.Euler(0f, oturak * 90f + 180f, 0f);
                    }

                    GameShot.Shoot("olcek_masa_ustten.png",
                                   new Bounds(new Vector3(0f, 0.35f, 0f),
                                              new Vector3(2.0f, 0.7f, 2.0f)),
                                   900, 900, pitch: 89.9f, yaw: 0f);

                    // OYUN ACISINDAN da: tepeden dogru goruunen bir
                    // hizalama, 34 derecede Z ekseni 0,56 kat
                    // sikistigi icin baska okunabiliyor. Karar oyunun
                    // baktigi acidan verilmeli.
                    // OYUN ACISINDAN da: tepeden dogru goruunen bir
                    // hizalama, 34 derecede Z ekseni 0,56 kat
                    // sikistigi icin baska okunabiliyor.
                    GameShot.Shoot("olcek_masa_oyun.png",
                                   new Bounds(new Vector3(0f, 0.35f, 0f),
                                              new Vector3(1.9f, 0.9f, 1.9f)),
                                   900, 620);
                }

                Object.DestroyImmediate(kok);
                SeatProfile(sandalye, karakter);
                Debug.Log("=== olcek goruntuleri alindi: render/olcek_*.png ===");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError("SORUNLAR: figur olcegi -> " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(2);
            }
        }

        // =====================================================================
        /// <summary>
        /// SANDALYE PROFILI VE OTURAN FIGUR - goruntuyle degil SAYIYLA.
        ///
        /// Kullanicinin cumlesi: "karakterler sandalyeye oturunca sirt
        /// ve arka taraflari sandalyenin ustune geliyor". Yandan alinan
        /// goruntu bunu gosteriyor ama NE KADAR oldugunu soylemiyor -
        /// ve duzeltmenin dogru olup olmadigi ancak sayiyla anlasilir.
        ///
        /// Isin yok, carpisan yok: kosleri dogrudan mesh'ten okuyup
        /// dunya uzayina cevriliyor. Sandalye OYUNDAKI acida (yaw
        /// PropYaw) duruyor, yani onu +Z'ye bakiyor ve sirtlik -Z'de.
        /// Figur de oyundaki gibi yaw 0 (yuzu +Z).
        /// </summary>
        private static void SeatProfile(GameObject sandalyePrefab,
                                        GameObject karakterPrefab)
        {
            GameObject kok = new GameObject("OturmaOlcumu");

            GameObject s = Object.Instantiate(sandalyePrefab, kok.transform);
            s.transform.localPosition = Vector3.zero;
            s.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // --- sandalyenin orta serit profili ---
            //
            // SERIT MODELIN MERKEZINDEN geciyor, dunya sifirindan degil:
            // ilk yazimda |x| < 0,06 denmisti ve HIC kose bulamadi -
            // paketin sandalye pivotu ortalanmamis. Sinir kutusunun
            // merkezi soruluyor, boylece pivotun nerede oldugu onemsiz.
            Bounds sb = new Bounds(s.transform.position, Vector3.zero);
            bool ilk = true;
            foreach (Renderer r in s.GetComponentsInChildren<Renderer>(true))
            {
                if (ilk) { sb = r.bounds; ilk = false; }
                else sb.Encapsulate(r.bounds);
            }
            Debug.Log(string.Format(
                "OTURMA sandalye kutusu: merkez ({0:0.000} {1:0.000} {2:0.000}) "
                + "boy ({3:0.000} {4:0.000} {5:0.000})",
                sb.center.x, sb.center.y, sb.center.z,
                sb.size.x, sb.size.y, sb.size.z));

            // KOSE OKUMA DEGIL ISIN.
            //
            // Paketin mesh'leri Read/Write KAPALI (isReadable: 0), yani
            // .vertices editorde de bos donuyor - ilk yazim tam bu yuzden
            // hic kose bulamadi. MeshCollider'in ihtiyaci yok: carpisan
            // veri ice aktarmada pisiriliyor. Yukaridan inen bir isin
            // sandalyenin yan siluetini dogrudan veriyor.
            foreach (MeshFilter mf in s.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
            Physics.SyncTransforms();

            float yari = sb.size.z * 0.5f + 0.02f;
            var profil = new System.Collections.Generic.List<Vector2>();
            for (float z = -yari; z <= yari + 0.001f; z += 0.01f)
            {
                RaycastHit h;
                if (Physics.Raycast(new Vector3(sb.center.x, sb.max.y + 1f,
                                                sb.center.z + z),
                                    Vector3.down, out h, 3f))
                    profil.Add(new Vector2(z, h.point.y));
            }

            if (profil.Count == 0)
            {
                Debug.LogWarning("OTURMA: sandalye silueti olculemedi");
                Object.DestroyImmediate(kok);
                return;
            }

            float tepe = 0f;
            for (int i = 0; i < profil.Count; i++) tepe = Mathf.Max(tepe, profil[i].y);

            var satir = new System.Text.StringBuilder(
                "OTURMA sandalye silueti (z -> yuzey y):");
            for (int i = 0; i < profil.Count; i += 3)
                satir.Append("  " + profil[i].x.ToString("0.00")
                             + ":" + profil[i].y.ToString("0.00"));
            Debug.Log(satir.ToString());

            // OTURAK: sirtligin ONUNDE kalan duzluk. Sandalye yaw
            // 180'de, yani onu +Z; sirtlik -Z tarafinda.
            float oturakY = 0f;
            for (int i = 0; i < profil.Count; i++)
                if (profil[i].y < tepe * 0.80f)
                    oturakY = Mathf.Max(oturakY, profil[i].y);

            // SIRTLIGIN ON YUZU: sirtlik yuksekligindeki en ondeki z.
            // Figurun sirti bundan geride kalirsa sirtligin icine girer.
            float sirtOnZ = -99f;
            for (int i = 0; i < profil.Count; i++)
                if (profil[i].y > tepe * 0.80f)
                    sirtOnZ = Mathf.Max(sirtOnZ, profil[i].x);

            // MINDERIN ON KENARI: uyluklarin bundan ONDE kalmasi
            // gerekiyor, yoksa bacaklar minderin icinden gecer.
            float oturakOnZ = -99f;
            for (int i = 0; i < profil.Count; i++)
                if (profil[i].y > oturakY - 0.02f && profil[i].y < tepe * 0.80f)
                    oturakOnZ = Mathf.Max(oturakOnZ, profil[i].x);

            Debug.Log(string.Format(
                "OTURMA sandalye: tepe {0:0.000} m, oturak yuzeyi {1:0.000} m, "
                + "sirtlik on yuzu z {2:0.000}",
                tepe, oturakY, sirtOnZ));

            // SANDALYENIN CARPISANLARI KALKIYOR.
            //
            // Kalmalari, figur olcumunu SESSIZCE sandalyeye cevirdi:
            // asagidan gelen isin once sandalyenin kayitini, arkadan
            // gelen isin once sirtligi buluyordu. "Legen altinda 0,281"
            // satiri figuru hic gormemisti ve figur 7,4 cm yukari
            // kaldirilinca bile AYNI sayiyi vermisti - degismeyen bir
            // olcum, olcmedigi seyin habercisi.
            foreach (MeshCollider mc in s.GetComponentsInChildren<MeshCollider>(true))
                Object.DestroyImmediate(mc);
            Physics.SyncTransforms();

            // --- oturan figur ---
            // Oyundakinin AYNISI: kaldirma da one kayma da
            // RestaurantView'den okunuyor. Burada ikinci bir sayi
            // yazmak, olcumun gercek oyunu degil kendini olcmesi olurdu.
            GameObject k = Object.Instantiate(karakterPrefab, kok.transform);
            k.transform.localPosition = new Vector3(
                0f, RestaurantView.SitLiftM, RestaurantView.SitForwardM);
            k.transform.localRotation = Quaternion.identity;
            // Sandalye sayilarinin sifiri MODELIN merkezi; figurunki de
            // ayni yere tasinmali, yoksa iki sayi ayni eksende degil.
            float kaydir = sb.center.z;

            Figure f = k.GetComponentInChildren<Figure>(true);
            if (f != null) f.Sample(Figure.Pose.Sit, 0.4f);
            {
                Transform dz = null, bc = null, ay = null;
                foreach (Transform t in k.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "knee-left") dz = t;
                    if (t.name == "leg-left") bc = t;
                }
                Debug.Log("OTURMA diz sayisi " + (f != null ? f.KneeCount : -1)
                    + "; bacak " + (bc == null ? "yok" : bc.position.ToString("0.000"))
                    + "; diz " + (dz == null ? "yok" : dz.position.ToString("0.000")));
                if (ay != null) { }
            }

            Bounds? fb = PlacementAudit.PozKutusu(k.transform);
            if (fb != null)
            {
                Bounds b = fb.Value;
                Debug.Log(string.Format(
                    "OTURMA figur (oturur): y {0:0.000}..{1:0.000}, "
                    + "z {2:0.000}..{3:0.000}, en {4:0.000}",
                    b.min.y, b.max.y, b.min.z - kaydir, b.max.z - kaydir,
                    b.size.x));

            }

            // --- GOVDE YUZEYLERI: KOSE DEGIL ISIN ---
            //
            // Iki yanlis yontem denendi:
            //   1. Sinir kutusu -> eni 1,01 m cikti, o KOLLAR; kutunun
            //      alti da bacak, arkasi da omuz olabiliyor.
            //   2. Kose ornekleme -> DUSUK POLIGONDA calismiyor: kutu
            //      bir govdenin yalnizca sekiz kosesi var, ortasinda
            //      hicbir kose yok. Profilde z = -0,08 ve -0,03
            //      dilimleri bombos cikti ve bu "orada govde yok"
            //      degil "orada KOSE yok" demekti.
            //
            // Dogrusu: pozlanmis mesh BakeMesh ile aliniyor (o mesh
            // BIZIM, paketin Read/Write ayari engel degil), gecici bir
            // MeshCollider'a takiliyor ve yuzeyler isinla okunuyor.
            var gecici = new System.Collections.Generic.List<GameObject>();
            foreach (SkinnedMeshRenderer smr in
                     k.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                Mesh pozlu = new Mesh();
                smr.BakeMesh(pozlu, false);

                GameObject g = new GameObject("Pozlu");
                g.transform.position = smr.transform.position;
                g.transform.rotation = smr.transform.rotation;
                g.AddComponent<MeshCollider>().sharedMesh = pozlu;
                gecici.Add(g);
            }
            Physics.SyncTransforms();

            // ALT SILUET: asagidan yukari isin. Bacaklar one egimli
            // sarktigi icin kalca ARKA dilimlerde, ayaklar onde.
            var alt = new System.Text.StringBuilder("OTURMA figur alti (z -> y):");
            // AYAK: x=0'daki isin iki bacagin ARASINDAN geciyor ve
            // legeni buluyor - ayak yuksekligi oradan okunamaz.
            // Pozlanmis kutunun tabani dogru cevap.
            float ayakAlt = fb != null ? fb.Value.min.y : 99f;
            float kalcaAlt = 99f;
            for (float z = -0.30f; z <= 0.301f; z += 0.03f)
            {
                RaycastHit h;
                if (!Physics.Raycast(new Vector3(0f, -1f, kaydir + z),
                                     Vector3.up, out h, 4f)) continue;
                alt.Append("  " + z.ToString("0.00") + ":" + h.point.y.ToString("0.00"));
                // Legen: FIGURUN kendi yerine gore, sabit bir z'ye gore
                // degil. Figur one kaydirilinca pencere bosta kaldi ve
                // olcum 99 dondu - sabit pencere, tasinan bir olcum.
                if (Mathf.Abs(z - RestaurantView.SitForwardM) < 0.06f)
                    kalcaAlt = Mathf.Min(kalcaAlt, h.point.y);
            }
            Debug.Log(alt.ToString());

            // SIRT: arkadan one isin, govde yuksekliginde.
            float sirtArka = 99f;
            for (float y = oturakY + 0.06f; y <= oturakY + 0.34f; y += 0.02f)
            {
                RaycastHit h;
                if (Physics.Raycast(new Vector3(0f, y, kaydir - 1f),
                                    Vector3.forward, out h, 2f))
                    sirtArka = Mathf.Min(sirtArka, h.point.z - kaydir);
            }

            // BACAGIN ALTI: minderin UZERINDE mi.
            //
            // Bacaklar minderin uzerinde uzaniyor (klibin duruşu), yani
            // soru yatay degil DIKEY: minderin ustundeki z araliginda
            // bacagin en alt noktasi minder yuzeyinin altina iniyor mu.
            // Isin x = +-0,06'dan: x = 0 iki bacagin ARASINDAN geciyor.
            float bacakAlt = 99f;
            for (float x = -0.07f; x <= 0.071f; x += 0.14f)
                for (float z = -0.05f; z <= oturakOnZ + 0.001f; z += 0.02f)
                {
                    RaycastHit h;
                    if (Physics.Raycast(new Vector3(x, -1f, kaydir + z),
                                        Vector3.up, out h, 4f))
                        bacakAlt = Mathf.Min(bacakAlt, h.point.y);
                }

            // GOGUS ONU: masaya girip girmedigi. Figur one kaydirilinca
            // sirtlik sorunu bitiyor ama masa tablasi govdeyi kesebilir;
            // ikisi ayni eksende yarisiyor.
            // ON PROFIL: yukseklige gore. Tek sayi yetmiyordu - band
            // dizin hizasindan basladigi icin en one cikan sey DIZ
            // oluyordu ve "gogus masaya giriyor" diye okunuyordu.
            var on = new System.Text.StringBuilder("OTURMA figur onu (y -> z):");
            float gogusOn = -99f;
            for (float y = 0.10f; y <= 1.00f; y += 0.05f)
            {
                RaycastHit h;
                if (!Physics.Raycast(new Vector3(0f, y, kaydir + 1f),
                                     Vector3.back, out h, 2f)) continue;
                float z = h.point.z - kaydir;
                on.Append("  " + y.ToString("0.00") + ":" + z.ToString("0.00"));
                // YALNIZCA TABLANIN KENDI SERIDI. "Tablanin ustu"
                // demek, y=0,90'daki KAFAYI olcmek oluyordu - kafa
                // masanin cok ustunde ve serbestce one tasabilir.
                if (y >= TableTop - 0.06f && y <= TableTop + 0.02f)
                    gogusOn = Mathf.Max(gogusOn, z);
            }
            Debug.Log(on.ToString());

            for (int i = 0; i < gecici.Count; i++) Object.DestroyImmediate(gecici[i]);

            if (kalcaAlt < 90f)
                Debug.Log(string.Format(
                    "OTURMA -> kalcanin alti {0:0.000} m, oturak {1:0.000} m "
                    + "=> {2:0.000} m (eksi: gomuk, arti: havada)",
                    kalcaAlt, oturakY, kalcaAlt - oturakY));
            if (ayakAlt < 90f)
                Debug.Log(string.Format(
                    "OTURMA -> ayaklar yerden {0:0.000} m yukarida", ayakAlt));
            if (sirtArka < 90f)
                Debug.Log(string.Format(
                    "OTURMA -> sirtin arkasi z {0:0.000}, sirtligin onu z {1:0.000} "
                    + "=> {2:0.000} m ICINDE (eksi: onunde, saglikli)",
                    sirtArka, sirtOnZ, sirtOnZ - sirtArka));

            if (gogusOn > -90f)
            {
                // Masa tablasi merkezden ne kadar uzaga uzaniyor,
                // figurun gogsu ne kadar yaklasiyor.
                float masaYari = 0.41f;
                float bosluk = RestaurantView.SeatRadiusUsedM - gogusOn - masaYari;
                Debug.Log(string.Format(
                    "OTURMA -> gogsun onu z {0:0.000} (tabla {1:0.00} m); "
                    + "masa kenarina {2:0.000} m (eksi: tabla govdeyi kesiyor)",
                    gogusOn, TableTop, bosluk));
            }

            if (bacakAlt < 90f)
                Debug.Log(string.Format(
                    "OTURMA -> bacagin alti {0:0.000} m, minder yuzeyi {1:0.000} m "
                    + "=> {2:0.000} m pay (eksi: minder bacagin icinden geciyor)",
                    bacakAlt, oturakY, bacakAlt - oturakY));

            // SONUC: iki sayi da tamam mi. Araci calistirip satirlari
            // tek tek okumak yerine tek bakisla gorulsun diye.
            // OLCUT: MINDERE DEGEN YUZEY UYLUKLARIN ALTI.
            //
            // Once legenin ORTASI mindere oturtuluyordu ve sayi yesildi
            // ama uyluklar minderin 9,4 cm icinden geciyordu: bu modelde
            // legenin orta alti kalca kemigi hizasinda, uyluklar ise
            // ondan asagida. Degen yuzey hangisiyse olculecek olan o.
            // Legenin ortasi artik yalnizca "minderin ALTINA inmesin"
            // diye bakiliyor.
            bool legenTamam = kalcaAlt < 90f && kalcaAlt >= oturakY - 0.01f;
            // Sirt: sirtligin ONUNDE kalmali. Masa ile sirtlik ayni
            // eksende yaristigi icin pay kucuk - kural "icine girmesin".
            bool sirtTamam = sirtArka < 90f && sirtArka >= sirtOnZ;
            bool bacakTamam = bacakAlt < 90f
                              && Mathf.Abs(bacakAlt - oturakY) < 0.02f;
            Debug.Log((legenTamam && sirtTamam && bacakTamam
                       ? "OTURMA SONUC TAMAM" : "OTURMA SONUC BOZUK")
                      + string.Format(
                          ": legen sapmasi {0:0.000} m, sirt payi {1:0.000} m, "
                          + "bacak payi {2:0.000} m",
                          kalcaAlt - oturakY, sirtArka - sirtOnZ,
                          bacakAlt - oturakY));

            // BACAK GEOMETRISI: dizden kirilabilir mi.
            //
            // Kemik eklemek tek basina yetmiyor - bacak kutusunun
            // uzunlugu boyunca ARA KOSE HALKASI yoksa yeni kemik
            // bukmez, yalnizca kutuyu carpitir. Bacagin boyu boyunca
            // kac ayri y seviyesinde kose oldugu sayiliyor.
            {
                GameObject ayakta = Object.Instantiate(karakterPrefab, kok.transform);
                ayakta.transform.localPosition = new Vector3(0f, 0f, -1.5f);
                Figure af = ayakta.GetComponentInChildren<Figure>(true);
                if (af != null) af.Sample(Figure.Pose.Idle, 0f);

                var seviye = new System.Collections.Generic.List<float>();
                int toplam = 0;
                foreach (SkinnedMeshRenderer smr in
                         ayakta.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.sharedMesh == null) continue;
                    Mesh mp = new Mesh();
                    smr.BakeMesh(mp, false);
                    Vector3[] vv = mp.vertices;
                    toplam += vv.Length;
                    for (int i = 0; i < vv.Length; i++)
                    {
                        // Bacak bolgesi: govdenin altinda, yanlarda.
                        if (vv[i].y > 0.30f || Mathf.Abs(vv[i].x) < 0.02f) continue;
                        bool yeni = true;
                        for (int j = 0; j < seviye.Count; j++)
                            if (Mathf.Abs(seviye[j] - vv[i].y) < 0.005f) { yeni = false; break; }
                        if (yeni) seviye.Add(vv[i].y);
                    }
                    Object.DestroyImmediate(mp);
                }
                seviye.Sort();
                var bacakSatir = new System.Text.StringBuilder(
                    "OTURMA bacak kose seviyeleri (" + toplam + " kose, model):");
                for (int i = 0; i < seviye.Count; i++)
                    bacakSatir.Append(" " + seviye[i].ToString("0.000"));
                Debug.Log(bacakSatir.ToString());
                Object.DestroyImmediate(ayakta);
            }

            var kemikler = new System.Text.StringBuilder("OTURMA kemikler:");
            foreach (Transform t in k.GetComponentsInChildren<Transform>(true))
                kemikler.Append(" " + t.name);
            Debug.Log(kemikler.ToString());

            Transform kalca = null;
            foreach (Transform t in k.GetComponentsInChildren<Transform>(true))
            {
                string ad = t.name.ToLowerInvariant();
                if (ad.Contains("hip") || ad.Contains("pelvis")
                    || ad == "torso" || ad == "root")
                {
                    Debug.Log("OTURMA kemik " + t.name + ": y "
                              + t.position.y.ToString("0.000")
                              + ", z " + (t.position.z - kaydir).ToString("0.000"));
                    if (kalca == null && (ad.Contains("hip") || ad.Contains("pelvis")))
                        kalca = t;
                }
            }
            if (kalca != null)
                Debug.Log(string.Format(
                    "OTURMA -> kalca oturagin {0:0.000} m USTUNDE "
                    + "(0 ise tam oturuyor)", kalca.position.y - oturakY));

            Object.DestroyImmediate(kok);
        }

        /// <summary>Masa tablasinin yuksekligi (ArtPrefabs hedefi).</summary>
        private const float TableTop = 0.58f;

        private static GameObject Load(string rel)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabDir + "/" + rel + ".prefab");
        }

        private static void Slab(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());

            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            go.GetComponent<Renderer>().sharedMaterial = m;
        }
    }
}
