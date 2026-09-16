using System.IO;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Lokanta.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Salonun goruntusunu alir - GERCEK gorunum kodunu kostararak.
    ///
    /// Amac gozle bakabilmek: bir kat plani sayilarla dogru olabilir ve
    /// yine de yanlis okunabilir. Bu projede uc yerlesim tam olarak boyle
    /// elendi (RoomLayout.cs) ve kameranin cerceveleme hatasi da boyle
    /// bulundu (docs/34 22).
    ///
    /// Play mode kullanilmiyor: toplu kipte guvenilir calismiyor ve
    /// takiliyor. Onun yerine gercek bir Simulation kuruluyor ve
    /// RestaurantView.Preview'e veriliyor; cizilen sey oyundaki kodun
    /// ta kendisi.
    ///
    ///   Unity.exe -batchmode -quit -projectPath ...
    ///     -executeMethod Lokanta.EditorTools.GameShot.Capture
    /// </summary>
    public static class GameShot
    {
        [MenuItem("Lokanta/Salon goruntusu al")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/Lokanta/Game.unity");

            foreach (string cuisine in new[] { "turk", "fastfood" })
            foreach (var kademe in new[]
                     { new { Ad = "acilis", Masa = 4 },
                       new { Ad = "genel",  Masa = 99 } })
            {
                Simulation sim = Build(cuisine, kademe.Masa, out ContentSet content);
                if (sim == null) continue;

                RestaurantView view = Object.FindFirstObjectByType<RestaurantView>();
                if (view == null) { Debug.LogError("RestaurantView yok"); return; }

                view.Preview = sim;
                view.PreviewPoses = true;
                // KIMLIK GORUNTUYE DE GECIYOR: susleme mutfaga gore
                // renk aliyor ve arac oyunun gostereceginin aynisini
                // gostermeli.
                view.PreviewCuisine = cuisine;
                // Self servis bayragi palette degil ICERIKTE; arac
                // onu vermezse kare oyunun gosterdiginden farkli olur.
                view.PreviewContent = content;
                Invoke(view, "Awake");
                view.Rebuild();

                // GUN ISIGI ONCE UYGULANIYOR.
                //
                // Yoksa butun goruntuler gun isigi hic kosmamis halde
                // cikiyor: arka plan siyah, golgeler varsayilan acida.
                // Arac, oyunun gosterecegi seyi gostermeli - servisin
                // ortasi (0,35) varsayilan bakis.
                {
                    DayLight g0 = Object.FindFirstObjectByType<DayLight>();
                    if (g0 != null)
                        g0.Apply(Lokanta.Core.Sim.DayPhase.Service, 0.35f);
                }
                Invoke(view, "Update");

                Audit(view);
                UnityEngine.Debug.Log("  TANI masa: yiyen " + view.EatingTables);

                // SAHNE BUTCESI TAM GENISLEMIS HALDE (docs/19 B5).
                //
                // Tur de ayni iki sayiyi olcuyor ama DORT MASADA: tur
                // altmis gunu oynuyor, genislemiyor. Tavani gormek icin
                // olcumun burada da olmasi gerekiyor - bu arac sahneyi
                // 99 masa isteyerek, yani her oda acik kuruyor.
                {
                    int cizici = 0, blokla = 0;
                    long ucgen = 0;
                    foreach (Renderer rr in Object.FindObjectsByType<Renderer>(
                                 FindObjectsSortMode.None))
                    {
                        if (!rr.enabled || !rr.gameObject.activeInHierarchy) continue;
                        cizici++;

                        // TOPLU CIZIMIN DISINDA KALANLAR.
                        //
                        // MaterialPropertyBlock yazilan bir cizici SRP
                        // toplu cizimine giremiyor - projenin kendisi
                        // bunu defalarca yazip zemini ve oda isigini
                        // buna gore tasarlamisti. Ama uc yeni sistem
                        // (rozet, kiyafet, ocak ustu) ayni bedeli
                        // odemeye devam ediyor ve HIC OLCULMUYORDU.
                        // Olculmedigi icin de kimse fark etmiyordu.
                        if (rr.HasPropertyBlock()) blokla++;

                        Mesh mm = null;
                        MeshFilter mf2 = rr.GetComponent<MeshFilter>();
                        if (mf2 != null) mm = mf2.sharedMesh;
                        SkinnedMeshRenderer sm = rr as SkinnedMeshRenderer;
                        if (sm != null) mm = sm.sharedMesh;
                        if (mm == null) continue;
                        for (int i = 0; i < mm.subMeshCount; i++)
                            ucgen += (long)(mm.GetIndexCount(i) / 3);
                    }
                    UnityEngine.Debug.Log("  OLCUM sahne butcesi (" + cuisine + ", "
                                          + kademe.Ad + ", " + sim.TableCount
                                          + " masa): " + cizici + " cizici, "
                                          + ucgen + " ucgen, "
                                          + blokla + " toplu cizim disi");

                    // ESIK BURADA - TURDA DEGIL.
                    //
                    // Turdaki esik (400 / 80.000) DORT MASALIK taban
                    // sahnede olculuyor ve tur hic genislemiyor: on dort
                    // masalik sahne butceyi assa hicbir sey uyarmazdi.
                    // Burasi sahneyi 99 masa isteyerek kuruyor, yani
                    // TAVANI olcuyor - eshiklerin yeri burasi.
                    //
                    // Sayilar docs/19 B5'ten degil OLCUMDEN: bugunku
                    // tavan ~270 cizici / ~45.000 ucgen. Esikler
                    // uzerine pay birakiyor ki kucuk eklemeler kirmasin,
                    // ama sessiz bir siserme yakalansin.
                    if (cizici > 360)
                        UnityEngine.Debug.LogError("SORUNLAR: cizici tavani asildi ("
                                                   + cizici + " > 360, " + cuisine + " "
                                                   + kademe.Ad + ")");
                    if (ucgen > 70000)
                        UnityEngine.Debug.LogError("SORUNLAR: ucgen tavani asildi ("
                                                   + ucgen + " > 70000, " + cuisine + " "
                                                   + kademe.Ad + ")");
                    if (blokla > 220)
                        UnityEngine.Debug.LogError("SORUNLAR: toplu cizim disi cizici "
                                                   + "cok fazla (" + blokla + " > 220, "
                                                   + cuisine + " " + kademe.Ad + ")");
                }

                // Cerceve OYUNDAKIYLE AYNI kaynaktan: acik odalar.
                // PlotBounds kullaniyordu ve o yuzden goruntu, oyuncunun
                // gordugunden daha genisti - ekranin sagdaki ucte biri
                // bos arsaydi ve goruntude oyle gorunmuyordu.
                Shoot("salon_" + cuisine + "_" + kademe.Ad + ".png",
                      CameraFit.OpenBounds(sim.TableCount), 1280, 576);

                // Bir de ODAYA yaklasmis hali: dokunma hedefi bu olcekte
                // masa takimi olacak (docs/31), yani bu cerceve de
                // gorulmeli. Oda cercevesi masa sayisindan bagimsiz,
                // yalnizca bir kez.
                if (kademe.Masa > 4)
                {
                    int salon = FindRoom("Salon1");
                    Shoot("salon_" + cuisine + "_oda.png",
                          CameraFit.RoomBounds(salon), 1280, 576);

                    // MUTFAK YAKIN PLAN: kiyafet (asci kepi, onluk) ve
                    // servis bankosu ancak bu olcekte gorulebiliyor.
                    int mutfak = FindRoom("Mutfak");
                    Shoot("mutfak_" + cuisine + ".png",
                          CameraFit.RoomBounds(mutfak), 1100, 700);

                    // TEK MASA, YAKINDAN.
                    //
                    // "Modeller ust uste binmis gibi" sorusunu ancak bu
                    // olcekte cevaplamak mumkun: genel gorunumde 34
                    // derecelik bakis zaten her seyi ust uste
                    // gosteriyor, yani hem gercek cakismayi hem masum
                    // derinligi ayni sekilde cizyor.
                    Transform masa = FindTable(view);
                    if (masa != null)
                    {
                        Shoot("salon_" + cuisine + "_masa.png",
                              new Bounds(masa.position + new Vector3(0f, 0.6f, 0f),
                                         new Vector3(2.6f, 1.6f, 2.6f)),
                              1280, 720);

                        // EN YAKIN: oyuncunun iki parmakla gelebilecegi
                        // sinir (CameraRig 0,45). Buradan sonrasi
                        // gorulmuyor, yani modellerin dogru gorunmesi
                        // gereken en zor olcek bu.
                        Shoot("salon_" + cuisine + "_yakin.png",
                              new Bounds(masa.position + new Vector3(0f, 0.45f, 0f),
                                         new Vector3(1.25f, 0.9f, 1.25f)),
                              1280, 720);
                    }
                }

                // AKSAM GORUNTUSU.
                //
                // Gun isigi GameApp'ten suruluyor ve editor kipinde o
                // islemiyor; gun icinde neyin degistigi ancak burada
                // gorulebilir. Sabah ve aksam AYNI cerceveden cekiliyor
                // ki fark yalnizca isiktan gelsin.
                DayLight gun = Object.FindFirstObjectByType<DayLight>();
                if (gun != null && kademe.Ad == "genel")
                {
                    Bounds cerceve = CameraFit.OpenBounds(kademe.Masa);

                    gun.Apply(Lokanta.Core.Sim.DayPhase.Morning, 0f);
                    Shoot("salon_" + cuisine + "_sabah.png", cerceve, 1280, 560);

                    gun.Apply(Lokanta.Core.Sim.DayPhase.Service, 0.5f);
                    Shoot("salon_" + cuisine + "_ogle.png", cerceve, 1280, 560);

                    gun.Apply(Lokanta.Core.Sim.DayPhase.Evening, 1f);
                    Shoot("salon_" + cuisine + "_aksam.png", cerceve, 1280, 560);

                    // SOKAK LAMBASI YAKIN PLAN, GECE VE GUNDUZ.
                    //
                    // Lamba genel cercevede 40 piksel: modelin dogru
                    // gorunup gorunmedigi o olcekte anlasilmaz. Iki kez
                    // cekiliyor cunku iki ayri soru var - gunduz MODEL
                    // (fener okunuyor mu, kol nereye bakiyor), gece ISIK
                    // (huzme, hale, havuz birlikte ne yapiyor).
                    Vector3 direk = new Vector3(
                        CameraFit.OpenBounds(kademe.Masa).center.x,
                        1.05f, RestaurantView.LampPostZ + 0.35f);
                    Bounds yakin = new Bounds(direk, new Vector3(3.2f, 2.4f, 3.2f));

                    // KAMERA ACISI DENEMESI.
                    //
                    // Kullanici "referanstaki gibi bir aci" istedi ve
                    // secim goz karariyla yapilamaz: donme (yaw) hem
                    // kareyi doldurmayi hem de DOKUNMA HEDEFINI
                    // degistiriyor (docs/31: -12 derece donme tabani
                    // 71 dp'den 48 dp'ye dusurmustu). Once BAKILIYOR.
                    if (cuisine == "turk")
                    {
                        Bounds cer2 = CameraFit.OpenBounds(kademe.Masa);
                        float[,] acilar = { {34f, 0f}, {34f, 20f}, {34f, 30f},
                                            {40f, 30f}, {30f, 45f}, {45f, 45f} };
                        for (int a = 0; a < acilar.GetLength(0); a++)
                            Shoot("aci_" + (int)acilar[a, 0] + "_"
                                  + (int)acilar[a, 1] + ".png", cer2, 873, 393,
                                  acilar[a, 0], acilar[a, 1]);
                    }

                    gun.Apply(Lokanta.Core.Sim.DayPhase.Service, 0.35f);
                    Shoot("lamba_" + cuisine + "_gunduz.png", yakin, 900, 700);

                    gun.Apply(Lokanta.Core.Sim.DayPhase.Evening, 1f);
                    Shoot("lamba_" + cuisine + "_gece.png", yakin, 900, 700);

                    gun.Apply(Lokanta.Core.Sim.DayPhase.Service, 0.35f);
                }

                // BULASIKHANE YAKIN PLAN.
                //
                // Lavaboyu yalnizca biri yikarken gormek gerekiyor ve
                // editor kipinde kimse yikamiyor: sahne kuruldugunda
                // personel bosta. Burada ELLE kuruluyor - bir figur
                // lavaboya konup yikama duruşuna sokuluyor, musluk ve
                // sunger aciliyor.
                //
                // Neden gerekli: "su akiyor mu, sunger var mi" sorusunun
                // cevabi bir sayida degil, goruntude. Olcum burada
                // BAKMAK.
                if (kademe.Ad == "genel")
                {
                    view.PreviewWash();
                    Vector3 ls = view.WashSpot;
                    Shoot("bulasik_" + cuisine + ".png",
                          new Bounds(ls + new Vector3(0f, 0.7f, 0.55f),
                                     new Vector3(2.6f, 1.6f, 2.6f)),
                          1280, 720);
                }

                view.Preview = null;
                view.PreviewPoses = false;
                view.Clear();
            }

            Debug.Log("Goruntuler alindi: render/");
        }

        /// <summary>
        /// Kurulmus sahnedeki GERCEK olculeri yazar.
        ///
        /// Prefabin olcusu dogru olabilir ve sahnedeki nesne yine de yanlis
        /// boyda cikabilir: aradaki her ebeveynin olcegi carpiliyor ve
        /// animasyon ornekleme kok donusumune dokunabiliyor. Bu, iki
        /// olcumu yan yana koyup farki gorunur kiliyor.
        /// </summary>
        private static void Audit(RestaurantView view)
        {
            int n = 0;
            foreach (Figure f in view.GetComponentsInChildren<Figure>(true))
            {
                if (n++ > 3) break;
                Transform t = f.transform;
                Debug.Log(string.Format(
                    "  figur  durus={0,-5} ebeveyn {1,-22} olcek {2:0.000}"
                    + "  y={3:0.00}  {4}  {5}",
                    f.Current, t.parent == null ? "-" : t.parent.name,
                    t.lossyScale.y, t.position.y, Box(t), BindBox(t)));
            }

            // KALABALIK OLCUSU. Kullanicinin cumlesi: "karakterler biraz
            // buyuk gibi, ortayi cok sikisik gosteriyor." Sayiya
            // cevrilebilir tek hali: bir masa takiminin ayak izine
            // oturanlarin ne kadari giriyor.
            foreach (Figure f0 in view.GetComponentsInChildren<Figure>(true))
            {
                Bounds? bb = null;
                foreach (SkinnedMeshRenderer smr in
                         f0.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.sharedMesh == null) continue;
                    Bounds mb = smr.sharedMesh.bounds;
                    Vector3 sc = smr.transform.lossyScale;
                    Bounds w = new Bounds(Vector3.Scale(mb.center, sc),
                                          Vector3.Scale(mb.size, sc));
                    if (bb == null) bb = w; else { Bounds a = bb.Value; a.Encapsulate(w); bb = a; }
                }
                if (bb == null) break;

                float en = bb.Value.size.x;
                Debug.Log(string.Format(
                    "  KALABALIK figur eni {0:0.00} m | masa araligi {1:0.00} m"
                    + " | iki figur {2:0.00} m = araligin %{3:0}"
                    + " | sandalye yuksekligi 0,92 m, figur boyu {4:0.00} m,"
                    + " oran {5:0.00} (gercek insan 1,85)",
                    en, RoomPlan.CellX, en * 2f, en * 2f / RoomPlan.CellX * 100f,
                    bb.Value.size.y, bb.Value.size.y / 0.92f));
                break;
            }

            foreach (Transform t in view.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("chairCushion")) continue;
                Debug.Log(string.Format(
                    "  sandalye ebeveyn {0,-22} olcek {1:0.000}  y={2:0.00}  {3}",
                    t.parent == null ? "-" : t.parent.name,
                    t.lossyScale.y, t.position.y, Box(t)));
                break;
            }
        }

        private static bool HasDirectionalLight()
        {
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.enabled && l.intensity > 0.01f)
                    return true;
            return false;
        }

        private static string Box(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return "cizici yok";
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return string.Format("kutu {0:0.00} x {1:0.00} x {2:0.00} taban={3:0.00}",
                                 b.size.x, b.size.y, b.size.z, b.min.y);
        }

        /// <summary>
        /// BAGLAMA DURUSUNDAKI gercek olcu.
        ///
        /// Renderer.bounds derili bir mesh'te YALAN SOYLUYOR: Unity onu
        /// kok kemikten turetiyor ve poz degistikce guncellemiyor. Ilk
        /// olcumde oturan bir figur 1,68 m boyunda ve 1,66 m eninde
        /// gorundu - ikisi de imkansiz. sharedMesh.bounds ise modelin
        /// kendi kutusu; dunya olcegiyle carpinca gercek boy cikiyor.
        /// </summary>
        private static string BindBox(Transform t)
        {
            Bounds? acc = null;
            foreach (SkinnedMeshRenderer smr in
                     t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                Bounds mb = smr.sharedMesh.bounds;
                Vector3 sc = smr.transform.lossyScale;
                Bounds w = new Bounds(
                    Vector3.Scale(mb.center, sc), Vector3.Scale(mb.size, sc));
                if (acc == null) acc = w; else { Bounds a = acc.Value; a.Encapsulate(w); acc = a; }
            }
            if (acc == null) return "derili mesh yok";
            Vector3 z = acc.Value.size;
            return string.Format("BAGLAMA en {0:0.00} boy {1:0.00} derinlik {2:0.00} m",
                                 z.x, z.y, z.z);
        }

        // =====================================================================
        /// <param name="maxTables">
        /// Bu sayidan sonra genisleme denenmiyor. Iki goruntu icin var:
        /// ACILIS (dort masa) ve BUYUMUS restoran. Kamera cercevesi acik
        /// odalara gore kuruldugu icin ikisi ayni cerceveyi vermiyor ve
        /// ikisi de gorulmeli.
        /// </param>
        private static Simulation Build(string cuisine, int maxTables,
                                        out ContentSet content)
        {
            content = null;
            try
            {
                IContentSource src = new ResourcesContentSource();
                Loc.Load(src);
                EconomyConfig economy = ContentLoader.LoadEconomy(src);
                content = ContentSetLoader.Load(src, cuisine);

                TimingConfig timing = content.SlotDurationsBp != null
                    ? TimingConfig.Default()
                        .WithSlotDurations(content.SlotDurationsBp)
                        .WithEatMs(content.EatMs)
                    : TimingConfig.Default();

                Simulation sim = new Simulation(economy, content, timing, 20260911UL);

                // Birkac gun oyna ve salonu genislet: bos bir dort masalik
                // dukkanin goruntusu oyunu anlatmiyor.
                for (int day = 0; day < 12; day++)
                {
                    for (int i = 0; i < sim.IngredientCount; i++)
                    {
                        int need = sim.RecommendedRestock(i);
                        if (need > 0)
                            sim.Apply(new Command(sim.TickIndex,
                                CommandKind.OrderIngredient, i, need));
                    }
                    // Buyume DENENIYOR, gune sabitlenmis DEGIL: ilk
                    // yazimda 3. gunde genislemesi soylenmisti, o gun kasa
                    // yetmedi ve komut sessizce dustu - goruntu on ikinci
                    // gunde hala dort masaydi.
                    if (sim.Cooks < 2)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, 0));
                    if (sim.SalonStaff < 3)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, 0));

                    int tier = 0;
                    while (tier < sim.TierCount
                           && sim.TablesAtTier(tier) <= sim.TableCount) tier++;
                    if (tier < sim.TierCount
                        && sim.TablesAtTier(tier) <= maxTables
                        && sim.Cash > sim.UpgradeCostFor(tier) * 2)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, tier));
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));

                    // Servisin ORTASINDA duruyoruz: salon dolu olsun.
                    int stop = timing.ServiceTicks / 2;
                    for (int t = 0; t < stop; t++)
                    {
                        sim.Tick();
                        if (sim.ServiceComplete) break;
                    }
                    if (day == 11) break;             // son gunde servis acik kalsin

                    for (int t = 0; t < timing.ServiceTicks; t++)
                    {
                        sim.Tick();
                        if (sim.ServiceComplete) break;
                    }
                    sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                    sim.AdvanceToNextDay();
                }

                Debug.Log(string.Format(
                    "{0}: {1}. gun, {2} masa, {3} dolu, {4} kadro, itibar {5:0.0}",
                    cuisine, sim.Day, sim.TableCount, sim.OccupiedTables,
                    sim.Cooks + sim.SalonStaff, sim.ReputationCenti / 100f));
                return sim;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Onizleme kurulamadi (" + cuisine + "): " + e.Message);
                return null;
            }
        }

        /// <summary>Yerlesim denetiminin ayni onizlemeyi kurmasi icin.</summary>
        internal static Simulation BuildFor(string cuisine)
        {
            return Build(cuisine, 99, out _);
        }

        /// <summary>Ayni sebeple: ozel Awake/Update'i disaridan tetiklemek.</summary>
        internal static void Kick(object target, string method)
        {
            Invoke(target, method);
        }

        /// <summary>Misafiri olan ilk masa; yoksa ilk masa.</summary>
        private static Transform FindTable(RestaurantView view)
        {
            Transform ilk = null;
            foreach (Transform t in view.transform)
            {
                if (!t.name.StartsWith("Masa_")) continue;
                if (ilk == null) ilk = t;
                if (t.GetComponentInChildren<Figure>(true) != null) return t;
            }
            return ilk;
        }

        private static int FindRoom(string name)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
                if (RoomPlan.Rooms[i].Name == name) return i;
            return 0;
        }

        /// <summary>
        /// MonoBehaviour'un ozel metodunu cagirir. Editor kipinde Awake ve
        /// Update calismiyor; onizlemenin gercek kodu kosturmasi icin
        /// elle tetikleniyor.
        /// </summary>
        private static void Invoke(object target, string method)
        {
            var m = target.GetType().GetMethod(method,
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public);
            m?.Invoke(target, null);
        }

        // =====================================================================
        internal static void Shoot(string name, Bounds target, int width, int height,
                                   float pitch = float.NaN, float yaw = float.NaN)
        {
            bool batcher = SrpBatcher(false);
            try { Draw(name, target, width, height, pitch, yaw); }
            finally { SrpBatcher(batcher); }
        }

        /// <summary>
        /// SRP toplu cizimini gecici olarak kapatir, onceki halini dondurur.
        ///
        /// Neden: toplu kipte elle cagrilan Camera.Render(), toplu cizicinin
        /// malzeme basina sabit tamponunu DOLDURMUYOR. Butun modeller tek
        /// bir renge cikiyordu - bir kosuda pembe, digerinde siyah; yani
        /// okunan sey artik veriydi. Zeminlerin dogru cikmasi da bunu
        /// dogruluyor: onlarin rengi MaterialPropertyBlock'tan geliyor ve
        /// o yol toplu cizimi zaten devre disi birakiyor.
        ///
        /// Oyunun kendisi bundan etkilenmiyor (gercek kosuda tampon
        /// doluyor); kapatilan yalnizca GORUNTU ALMA yolu.
        /// </summary>
        private static bool SrpBatcher(bool on)
        {
            RenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline;
            if (asset == null) return true;

            SerializedObject so = new SerializedObject(asset);
            SerializedProperty p = so.FindProperty("m_UseSRPBatcher");
            if (p == null) return true;

            bool was = p.boolValue;
            if (was != on)
            {
                p.boolValue = on;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return was;
        }

        private static void Draw(string name, Bounds target, int width, int height,
                                 float pitch = float.NaN, float yaw = float.NaN)
        {
            GameObject camGo = new GameObject("GoruntuKamerasi");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;

            // ARKA PLAN SAHNENIN KAMERASINDAN.
            //
            // Burada sabit bir renk yaziliydi ve gun isigi eklendiginde
            // arac SESSIZCE yanlis seyi gosterdi: oyunda gokyuzu sabah
            // mavi, aksam siyah oluyor ama goruntude her saat ayni koyu
            // gri kaliyordu. Arac, oyunun gosterecegi seyi gostermeli.
            Camera sahne = Object.FindFirstObjectByType<Lokanta.Game.CameraRig>()
                           != null
                ? Object.FindFirstObjectByType<Lokanta.Game.CameraRig>()
                        .GetComponent<Camera>()
                : null;
            cam.backgroundColor = sahne != null
                ? sahne.backgroundColor
                : new Color(0.055f, 0.062f, 0.075f);

            // CameraRig ile AYNI hesap: gordugumuz sey oyuncunun gordugu
            // sey olmali. Ilk yazimda ayri sayilar vardi ve render
            // restorani karenin %38'inde gosterdi.
            cam.fieldOfView = CameraFit.FieldOfView;
            cam.aspect = width / (float)height;

            // Aci verilmediyse OYUNUN acisi. Verildiginde yalnizca
            // inceleme goruntuleri icin: "oturuyor mu" sorusu tepeden
            // bakarak cevaplanamiyor, yandan bakmak gerekiyor.
            bool ozel = !float.IsNaN(pitch) || !float.IsNaN(yaw);
            Quaternion rot = ozel
                ? Quaternion.Euler(float.IsNaN(pitch) ? CameraFit.Pitch : pitch,
                                   float.IsNaN(yaw) ? CameraFit.Yaw : yaw, 0f)
                : CameraFit.Rotation;
            camGo.transform.rotation = rot;

            if (ozel)
            {
                float tanV = Mathf.Tan(CameraFit.FieldOfView * Mathf.Deg2Rad * 0.5f);
                float yayilim = Mathf.Max(target.extents.y,
                                          target.extents.x / cam.aspect);
                float d = yayilim / tanV + target.extents.z + 0.4f;
                camGo.transform.position = target.center - rot * Vector3.forward * d;
            }
            else
            {
                camGo.transform.position = CameraFit.Position(target, cam.aspect);
            }

            // Sahnede zaten isik varsa YENISI EKLENMIYOR.
            //
            // Once kosulsuz ekleniyordu ve sonuc iki kat aydinlik bir
            // kareydi. Yani onizlemede dogru gorunen aydinlatma, oyunda
            // yarisi kadardi ve ilk masaustu yapisinda salon karanlik cikti.
            // Onizlemenin isi oyunu GOSTERMEK, guzellestirmek degil.
            GameObject sun = null, fillGo = null;
            if (!HasDirectionalLight())
            {
                sun = new GameObject("Gunes");
                Light key = sun.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.45f;
                key.color = new Color(1f, 0.96f, 0.90f);
                sun.transform.rotation = Quaternion.Euler(52f, 208f, 0f);

                fillGo = new GameObject("Dolgu");
                Light fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.55f;
                fill.color = new Color(0.72f, 0.78f, 0.92f);
                fillGo.transform.rotation = Quaternion.Euler(28f, 40f, 0f);
            }

            RenderTexture rt = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;

            string dir = Path.Combine(
                Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath)), "render");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name), shot.EncodeToPNG());

            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            if (sun != null) Object.DestroyImmediate(sun);
            if (fillGo != null) Object.DestroyImmediate(fillGo);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(shot);
        }
    }
}
