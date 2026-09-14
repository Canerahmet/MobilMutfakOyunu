using System.Collections.Generic;
using System.IO;
using Lokanta.Game;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Her model icin, MALZEMESI - OLCEGI - DURUSU PISMIS bir prefab uretir.
    ///
    /// Neden prefab: iceri alma yeniden eslemesi (ModelImporter.AddRemap)
    /// gorunmez bir katman. Malzeme prefabin icinde durursa sonuc
    /// okunabilir oluyor - dosyayi acip hangi malzemeyi kullandigini
    /// gorebiliyorsun.
    ///
    /// Olcek ONCEDEN YAZILAN BIR CARPAN DEGIL, MODELIN OLCULMUS
    /// BOYUTUNDAN turetiliyor. Once klasor basina tek carpan vardi
    /// (Mobilya 0,20); sandalyeler dogru boydaydi ama masa 1,6 m capinda
    /// cikti ve sandalyeleri yuttu - paketin modelleri kendi aralarinda
    /// ayni olcekte degil. Burada her model icin GERCEK DUNYA HEDEFI
    /// yaziliyor ve carpan olculen boyuta bolunerek bulunuyor.
    ///
    /// Karakterler ayrica ANIMASYON aliyor. Paket 32 klip tasiyor ve
    /// iskelet butun figurlerde ayni; tek bir denetleyici hepsini
    /// suruyor. Klipsiz birakildiklarinda figurler baglanma durusunda
    /// kaliyor - kollar yana acik, 1,94 m kanat acikligi.
    /// </summary>
    public static class ArtPrefabs
    {
        private const string Art = "Assets/Lokanta/Art";
        private const string MatDir = Art + "/Malzeme";
        private const string PrefabDir = Art + "/Prefab";
        private const string AnimDir = Art + "/Animator";
        private const string ControllerPath = AnimDir + "/Karakter.controller";

        /// <summary>Klipleri ve olcegi bu modelden alinan referans figur.</summary>
        private const string RefCharacter = "character-male-a";

        // =====================================================================
        /// <summary>Hedef boyut: olculecek eksen ve metre cinsinden deger.</summary>
        private struct Target
        {
            public bool Width;      // true ise X/Z'nin buyugu, degilse Y
            public float Metres;
            /// <summary>Sifirdan buyukse Y bagimsiz olceklenir.</summary>
            public float StretchY;

            public Target(bool width, float metres, float stretchY = 0f)
            {
                Width = width; Metres = metres; StretchY = stretchY;
            }
        }

        private static Target Tall(float m) { return new Target(false, m); }
        private static Target Wide(float m) { return new Target(true, m); }

        /// <summary>
        /// Tabani capa, yuksekligi ayri hedefe oturtur.
        ///
        /// Tek bir model icin gerekti: paketin yuvarlak masasi kendi
        /// sandalyesine gore dogru oranda ama 1,4 m capinda - masa
        /// takiminin sigmasi gereken hucre 1,85 x 1,70 m ve sandalyelere
        /// yer kalmiyor. Capi gercekci 0,88 m'ye indirince masa 0,40 m
        /// yuksekliginde bir sehpaya donuyordu. Bacaklarini uzatmak,
        /// dusuk poligonlu bir modelde gorunmeyen ama oraniyla dogru
        /// sonucu veren cozum.
        /// </summary>
        private static Target WideTall(float wide, float tall)
        {
            return new Target(true, wide, tall);
        }

        /// <summary>
        /// Model basina gercek dunya hedefi. Mobilya yuksekliklerinin cogu
        /// standarttir ve insan boyuyla iliskilidir - tezgah 0,92 m,
        /// buzdolabi 1,80 m - onun icin olculen eksen genelde Y. Masa
        /// istisna: yuksekligi degil CAPI belirleyici, cunku salona kac
        /// masa sigacagini o belirliyor (RoomPlan.CellX/CellZ).
        /// </summary>
        private static readonly Dictionary<string, Target> Targets =
            new Dictionary<string, Target>
            {
                // --- salon ---
                // YEMEK TAKIMI KARAKTERE GORE OLCULU.
                //
                // Olculdu: 0,74 m'lik tablada oturan figurun basi
                // masanin yalnizca 0,37 m ustunde kaliyordu (gercek
                // insanda 0,51). Mobilya gercek olcekte, karakterler
                // yarisinda - ve bu tutarsizlik en cok masada
                // goruunuyor, cunku oyuncunun baktigi yer orasi.
                //
                // Cap 0,88'de KALIYOR: masa takimi 1,85 m'lik hucreye
                // sigmali ve capi kucultmek iki misafiri birbirine
                // yaklastirirdi.
                { "tableRound", WideTall(0.88f, 0.58f) },
                // Sandalye de masayla ayni oranda: sirtligi 0,92 iken
                // 0,94 m'lik figurle AYNI boydaydi.
                { "chairCushion", Tall(0.68f) },
                { "chair", Tall(0.68f) },
                { "chairRounded", Tall(0.68f) },
                // DORTGEN MASA. Dort oturak dort KENARA oturuyor;
                // altigende ikisi koseye dusuyordu.
                //
                // Yukseklik yuvarlak masayla ayni sebepten 0,55:
                // mobilya gercek olcekte, karakterler yarisinda ve
                // 0,74'lük bir tabla oturan figurun cenesine geliyordu
                // (olculdu: bas masanin 0,37 m ustunde, gercekte 0,51).
                { "table", WideTall(0.82f, 0.58f) },
                { "tableCloth", Tall(0.76f) },
                { "tableCrossCloth", Tall(0.76f) },
                { "stoolBar", Tall(0.75f) },
                { "rugRectangle", Wide(3.00f) },
                { "rugRounded", Wide(2.40f) },
                { "pottedPlant", Tall(0.85f) },
                { "lampSquareCeiling", Tall(0.40f) },

                // --- mutfak ---
                { "kitchenFridgeLarge", Tall(1.80f) },
                { "kitchenFridge", Tall(1.40f) },
                { "kitchenCabinetUpper", Tall(0.70f) },
                { "kitchenCoffeeMachine", Tall(0.45f) },
                { "kitchenMicrowave", Tall(0.32f) },
                { "kitchenBar", Tall(1.10f) },
                { "kitchenBarEnd", Tall(1.10f) },
                { "bookcaseClosedDoors", Tall(1.80f) },

                // --- yapi ---
                // KAPI, KARAKTERE GORE.
                //
                // 2,10 m gercek bir kapi olcusu ama bu paketin
                // figurleri 1,10 m; yanlarinda kapi bir zafer takina
                // donuyordu. Gercek oranda (kapi/insan = 1,17) karsiligi
                // 1,29 m; 1,45 biraz pay birakiyor ve hala "kapi" diye
                // okunuyor. Mobilyanin geri kalani gercekci olcude
                // kaliyor - kapi tek istisna, cunku tek gecilen sey o.
                { "doorwayOpen", Tall(1.45f) },
                { "wallDoorway", Tall(2.60f) },

                // --- yemek ---
                // Tabak capindan yola cikiliyor: bir hamburger tabaga
                // sigmali, bir bardak tabaktan kucuk olmali. Hepsine tek
                // hedef verildiginde yumurta tabak kadar oluyordu.
                { "plate", Wide(0.26f) },
                { "plate-deep", Wide(0.26f) },
                { "plate-dinner", Wide(0.26f) },
                { "bowl", Wide(0.16f) },
                { "bowl-soup", Wide(0.16f) },
                { "salad", Wide(0.16f) },
                { "cup", Wide(0.08f) },
                { "cup-tea", Wide(0.08f) },
                { "cup-saucer", Wide(0.13f) },
                { "glass", Wide(0.07f) },
                { "soda-glass", Wide(0.08f) },
                { "burger", Wide(0.12f) },
                { "burger-cheese", Wide(0.12f) },
                { "meat-patty", Wide(0.11f) },
                { "meat-cooked", Wide(0.16f) },
                { "fries", Tall(0.13f) },
                { "bread", Wide(0.18f) },
                { "cake", Wide(0.20f) },
                { "cheese", Wide(0.14f) },
                { "egg", Wide(0.05f) },
                { "onion", Wide(0.07f) },
                { "tomato", Wide(0.06f) },
                { "rice-ball", Wide(0.06f) },
            };

        /// <summary>Listede olmayanlar icin klasor varsayilani.</summary>
        private static readonly Dictionary<string, Target> FolderTarget =
            new Dictionary<string, Target>
            {
                { "Mobilya", Tall(0.92f) },
                // 1,28 m, gercek 1,70 m DEGIL.
                //
                // Olculdu: figurler 1,55 m'de bile mobilyanin yaninda dev
                // duruyordu. Sebep olcek hatasi degil USLUP farki - paketin
                // figurleri iri kafali (kafa boyun %35'i), mobilya ise
                // gercekci oranli (sandalye 0,92 x 0,40 m). Iki uslubu
                // birlestirmenin yolu figuru biraz kucultmek; boyuna degil
                // HACME bakiliyor.
                //
                // 11 Eylul 2026: 1,45 -> 1,28 -> 1,10.
                //
                // Ilk indirimde olcut "figur/sandalye boy orani" idi ve o
                // olcut YANLIS SORUYU soruyordu: bu paketin figurleri
                // boylarindan cok ENLERIYLE buyuk (kafa govdenin ucte
                // biri). Boy orani duzgun gorunurken oturan bir figurun
                // ayak izi 1,05 x 1,18 m cikiyordu - iki oturak arasi
                // 0,88 m, yani komsular tanim geregi birbirine giriyordu.
                //
                // Dogru olcut, YERLESIM DENETIMI (Editor/PlacementAudit):
                // sahnedeki her nesnenin gercek pozdaki kutusu ve kesisen
                // ciftler. 1,45'te 68 cakisan cift vardi; 1,10 + oturak
                // duzeni + personel araligi ile SIFIR.
                //
                // 1,10 m ayrica ayri bir goruntuyle dogrulandi
                // (Editor/FigureShot): tek figur, tek sandalye, 1 m'lik
                // izgaranin uzerinde, yandan. Figur sandalyeye oturuyor
                // ve boyu sandalyeyle orantili okunuyor.
                { "Karakter", Tall(CharacterHeight) },
                { "Yemek", Wide(0.22f) },
            };

        /// <summary>
        /// Klasorun olcegini TEK BIR modelden alan referanslar.
        ///
        /// Karakterlerde sart: her figuru ayri ayri 1,70 m'ye zorlamak,
        /// saci veya sapkasi yuzunden daha yuksek olculen figuru
        /// kucultuyor. Sonuc, hepsi ayni boyda ama govdeleri farkli
        /// olculerde bir kadro. Hepsi ayni carpani kullanirsa paketin
        /// kendi icindeki boy farklari korunuyor.
        /// </summary>
        private static readonly Dictionary<string, string> FolderReference =
            new Dictionary<string, string> { { "Karakter", RefCharacter } };

        // =====================================================================
        // Kenney mobilya paleti. Dokusuz bir FBX'in rengi yalnizca
        // malzemenin ADINDA duruyor.
        private static readonly Dictionary<string, Color> Palette =
            new Dictionary<string, Color>
            {
                { "wood", Hex(0x9C6B45) },
                { "woodDark", Hex(0x6B472E) },
                { "woodLight", Hex(0xC79A6B) },
                { "metal", Hex(0x9AA0A6) },
                { "metalDark", Hex(0x5C6166) },
                { "metalMedium", Hex(0x7D848A) },
                { "metalLight", Hex(0xC2C8CE) },
                { "carpet", Hex(0x8E5B5B) },
                { "carpetWhite", Hex(0xD9D2C7) },
                { "carpetDark", Hex(0x5E3E3E) },
                { "carpetDarker", Hex(0x4A3030) },
                { "glass", Hex(0xBFD8E0) },
                { "plastic", Hex(0xD8D3C8) },
                { "leather", Hex(0x6E4B3A) },
                { "fabric", Hex(0xB2A894) },
                { "stone", Hex(0xA8A49C) },
                { "white", Hex(0xE8E4DC) },
                { "black", Hex(0x33363A) },
                { "green", Hex(0x6E9E58) },
                { "red", Hex(0xB3524A) },
                { "lamp", Hex(0xF0E2BC) },
                { "plant", Hex(0x5E8C46) },
                { "_defaultMat", Hex(0xB9B2A6) },
            };

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f,
                             ((rgb >> 8) & 0xFF) / 255f,
                             (rgb & 0xFF) / 255f);
        }

        // =====================================================================
        [MenuItem("Lokanta/Model prefablarini uret")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Art))
            {
                Debug.LogWarning("Art klasoru yok. 'python tools/art/import_vendor.py' calistir.");
                return;
            }
            MakeFolder(Art, "Malzeme");
            MakeFolder(Art, "Prefab");
            MakeFolder(Art, "Animator");
            MakeFolder(Art, "Mesh");
            MakeCharactersReadable();

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) { Debug.LogError("URP Lit shader yok."); return; }

            // Malzemeler ve denetleyici ONCE ve tek seferde kaydediliyor:
            // prefab bunlara baglanacak, yani diskte olmalilar.
            Dictionary<string, Material> cache = BuildMaterials(lit);
            BuildSpecialMaterials(lit);
            AnimationClip[] clips = FindClips();
            AnimatorController ctrl = BuildController(clips);
            AssetDatabase.SaveAssets();

            Dictionary<string, float> folderScale = ReferenceScales();

            int made = 0;
            foreach (string g in AssetDatabase.FindAssets("t:Model", new[] { Art }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (MakePrefab(path, cache, ctrl, clips, folderScale)) made++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(string.Format(
                "{0} prefab uretildi, {1} URP malzemesi kuruldu, {2} klip baglandi.",
                made, cache.Count, ClipCount(clips)));
        }

        private static int ClipCount(AnimationClip[] clips)
        {
            int n = 0;
            if (clips != null)
                foreach (AnimationClip c in clips) if (c != null) n++;
            return n;
        }

        // =====================================================================
        /// <summary>
        /// Modellerin kullandigi her malzeme adi icin bir URP malzemesi
        /// kurar.
        ///
        /// Adlar ALT VARLIKLARDAN DEGIL, MODELIN CIZICILERINDEN okunuyor.
        /// Fark onemli: bir model daha once disaridaki bir malzemeye
        /// baglandiysa gomulu malzemesi artik yok, LoadAllAssetsAtPath hic
        /// malzeme bulamiyor ve arac "0 malzeme" deyip geciyordu. Cizici
        /// her iki durumda da dogru adi veriyor.
        /// </summary>
        /// <summary>
        /// SAYDAM VE ISIKSIZ MALZEMELER - VARLIK OLARAK.
        ///
        /// NEDEN VARLIK, NEDEN CALISMA ANINDA DEGIL:
        ///
        /// Duvarlar, kapi kanatlari, firin cami ve sokak lambasinin isik
        /// havuzu calisma aninda `new Material(...)` ile kuruluyordu ve
        /// EDITORDE dogru goruunuyordu. Gercek yapida hepsi OPAK cikti:
        /// URP saydam gecisin golgelendirici varyantini, ona basvuran
        /// bir varlik yoksa yapiya KOYMUYOR. Ayni sinif hata daha once
        /// URP/Unlit'te yasandi (rozetler).
        ///
        /// Malzeme bir .mat varligi olunca Unity varyanti topluyor.
        /// Bunlar ayrica sahneye baglaniyor (BuildGameScene), yani
        /// gercekten "basvurulan" varliklar.
        ///
        /// Bu hata sinifi EDITORDE GORUNMEZ - yalnizca yapida.
        /// </summary>
        private static void BuildSpecialMaterials(Shader lit)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");

            // DUVAR DAHA SILIK: 0,16 -> 0,10.
            //
            // Sahne suslendikce (zemin deseni, hali, sarkit, mobilya
            // tonu) duvarlarin sutlu beyazi butun salonun uzerine bir
            // PUS bindiriyordu: alttaki renkler soluyor, oda ayrimi ise
            // zaten zeminden ve esyadan okunuyor. Duvarin isi siniri
            // CIZMEK, alani boyamak degil.
            //
            // Not: RestaurantView'de bir "WallColor" alani vardi ve
            // hicbir yerden okunmuyordu - gercek renk BURADA, cunku
            // saydam malzeme bir .mat VARLIGI olmak zorunda (docs/37).
            Transparent(lit, "duvar", new Color(0.74f, 0.78f, 0.86f, 0.10f), 0.10f);
            Transparent(lit, "kapi", new Color(0.52f, 0.37f, 0.24f, 0.55f), 0.15f);
            Transparent(lit, "cam", new Color(0.14f, 0.16f, 0.18f, 0.45f), 0.75f);
            // AKAN SU. Saydam ve parlak.
            //
            // VARLIK olmak ZORUNDA: URP saydam gecisin golgelendirici
            // varyantini ona basvuran bir varlik yoksa yapiya koymuyor ve
            // calisma aninda kurulan saydam malzeme cihazda OPAK
            // ciziliyor - editorde hicbir belirti vermeden. Bu proje o
            // hatayi duvarlarda bir kez yasadi.
            Transparent(lit, "su", new Color(0.62f, 0.84f, 0.96f, 0.42f), 0.90f);
            if (unlit != null) Additive(unlit, "isikhavuzu",
                                        new Color(1.00f, 0.80f, 0.45f, 0.55f));
            // TAVAN ISIGI AYRI BIR VARLIK, property block DEGIL.
            //
            // Once ayni malzeme bir MaterialPropertyBlock ile
            // renklendiriliyordu. Iki bedeli vardi: property block yazmak
            // o cizicileri SRP toplu ciziminin DISINA atiyor (bu proje
            // bunu bir kez zemin levhalarinda ogrendi) ve tam genislemis
            // bir restoranda 25 tavan isigi var - hepsi ayri cizim.
            //
            // Renk sokaktakinden farkli: sokak lambasi sodyum sarisi,
            // icerisi sicak beyaz ve daha sonuk (sekiz odada onlarca
            // havuzun toplami zemini beyaza doyuruyordu).
            if (unlit != null) Additive(unlit, "tavanisigi",
                                        new Color(1.00f, 0.88f, 0.70f, 0.34f));
        }

        private static Material Transparent(Shader lit, string ad, Color c, float smooth)
        {
            string path = MatDir + "/ozel_" + ad + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(lit);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.shader != lit) m.shader = lit;

            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_AlphaClip", 0f);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend",
                       (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Additive(Shader unlit, string ad, Color c)
        {
            string path = MatDir + "/ozel_" + ad + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(unlit);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.shader != unlit) m.shader = unlit;

            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 1f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3100;
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetColor("_BaseColor", c);
            m.SetTexture("_BaseMap", GlowTexture());
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Isik havuzunun yumusak daire dokusu - VARLIK olarak.
        ///
        /// Duz renkli bir levha KARE bir isik havuzu veriyor. Doku
        /// calisma aninda uretilebilir ama o zaman malzeme de calisma
        /// aninda kurulmak zorunda kalir; varlik olunca ikisi de yapiya
        /// giriyor.
        /// </summary>
        private static Texture2D GlowTexture()
        {
            string path = MatDir + "/ozel_isikhavuzu_doku.asset";
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t != null) return t;

            const int N = 64;
            t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            t.name = "isikhavuzu";
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;

            Color32[] px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f;
                    float dy = (y + 0.5f) / N * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // KENARDA TAMAMEN SIFIR. Ucuncu kuvvet, kare
                    // olanin aksine kosede gorunur bir artik
                    // birakmiyor - toplayici harmanlamada o artik
                    // levhanin KARE oldugunu ele veriyordu.
                    // SONUM RENGE DE YAZILIYOR, YALNIZCA ALFAYA DEGIL.
                    //
                    // Alfa tek basina ise yaramadi: levha kenarindan
                    // sonmeden kare bir yama olarak ciziliyordu.
                    // TOPLAYICI harmanlamada (SrcAlpha + One) katki
                    // src.rgb * src.a; rengi de sondurunce kenar,
                    // alfanin nasil ele alindigindan BAGIMSIZ olarak
                    // siyaha gidiyor ve siyah hicbir sey eklemiyor.
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    byte v = (byte)(a * 255f);
                    px[y * N + x] = new Color32(v, v, v, v);
                }
            t.SetPixels32(px);
            t.Apply(false, false);
            AssetDatabase.CreateAsset(t, path);
            return t;
        }

        private static Dictionary<string, Material> BuildMaterials(Shader lit)
        {
            Dictionary<string, Material> cache = new Dictionary<string, Material>();

            foreach (string g in AssetDatabase.FindAssets("t:Model", new[] { Art }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                string folder = FolderOf(path);
                string texture = FindTexture(Art + "/" + folder);

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) continue;

                foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
                foreach (Material src in r.sharedMaterials)
                {
                    if (src == null) continue;

                    string name = Clean(src.name, folder);
                    string key = folder + "/" + name;
                    if (cache.ContainsKey(key)) continue;

                    string matPath = MatDir + "/" + folder + "_" + name + ".mat";
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (m == null)
                    {
                        m = new Material(lit);
                        AssetDatabase.CreateAsset(m, matPath);
                    }
                    if (m.shader != lit) m.shader = lit;

                    if (!string.IsNullOrEmpty(texture))
                    {
                        Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(texture);
                        if (t != null)
                        {
                            m.SetTexture("_BaseMap", t);
                            m.SetColor("_BaseColor", Color.white);
                        }
                    }
                    else if (Palette.TryGetValue(name, out Color c))
                    {
                        m.SetTexture("_BaseMap", null);
                        m.SetColor("_BaseColor", c);
                    }
                    else
                    {
                        m.SetColor("_BaseColor", Hex(0xB9B2A6));
                        Debug.LogWarning("Palette yok: " + key);
                    }

                    // Parlaklik dusuk: dusuk poligonlu bir salonda parlak
                    // yuzeyler yuzey kirilmalarini abartiyor.
                    m.SetFloat("_Smoothness", 0.10f);
                    m.SetFloat("_Metallic", 0f);
                    EditorUtility.SetDirty(m);
                    cache[key] = m;
                }
            }
            return cache;
        }

        // =====================================================================
        /// <summary>
        /// Referans figurun kliplerini bulur. Figure.Pose sirasinda
        /// donuyor; bulunamayan yerde null kaliyor.
        /// </summary>
        /// <summary>
        /// KLIPLERI DONGUYE ALIR.
        ///
        /// Kullanicinin iki ayri sikayeti TEK sebepten geliyordu:
        /// "karakterler adim atmiyor, zemin uzerinde kayiyor gibiler" ve
        /// "asci yemekleri karistirmiyor, bulasikci ovalamiyor".
        ///
        /// Klipler FBX'ten `clipAnimations: []` ile geliyordu, yani
        /// Unity'nin varsayilan ice aktarma ayarlariyla - ve orada
        /// loopTime KAPALI. Bir klip bir kez oynayip SON KARESINDE
        /// donuyor:
        ///
        ///   - yuruyus klibi ~1 saniye; figur dokuz saniye yuruyorsa
        ///     sekiz saniye boyunca donmus bacaklarla KAYIYOR,
        ///   - dograma klibi bir kez iniyor ve satir havada kaliyor,
        ///   - yikama klibi bir kez ugrasip duruyor.
        ///
        /// Animator'un acik kalmasi (HoldAwake) bu sorunu cozmuyordu -
        /// "animator calisiyor" ile "klip donguye giriyor" ayri iki sey.
        /// Kod tarafinda aylarca aranan seyin cevabi ICE AKTARMA
        /// ayarindaydi.
        ///
        /// Yalnizca KULLANDIGIMIZ klipler donguye aliniyor; paketin
        /// otuz iki klibinin geri kalanina dokunulmuyor.
        /// </summary>
        private static void LoopClips()
        {
            string path = Art + "/Karakter/" + RefCharacter + ".fbx";
            ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null)
            {
                Debug.LogWarning("Ice aktarici yok: " + path);
                return;
            }

            ModelImporterClipAnimation[] defs = mi.clipAnimations;
            if (defs == null || defs.Length == 0) defs = mi.defaultClipAnimations;
            if (defs == null || defs.Length == 0)
            {
                Debug.LogWarning("Klip tanimi yok: " + path);
                return;
            }

            int donen = 0;
            for (int i = 0; i < defs.Length; i++)
            {
                bool bizim = false;
                for (int k = 0; k < Figure.ClipNames.Length; k++)
                    if (defs[i].name == Figure.ClipNames[k]) { bizim = true; break; }
                if (!bizim) continue;

                if (defs[i].loopTime) { donen++; continue; }
                defs[i].loopTime = true;
                defs[i].loopPose = true;
                donen++;
            }

            mi.clipAnimations = defs;
            mi.SaveAndReimport();
            Debug.Log("  klip dongusu: " + donen + " klip donguye alindi ("
                      + Figure.ClipNames.Length + " isteniyor)");
        }

        private static AnimationClip[] FindClips()
        {
            LoopClips();
            string path = Art + "/Karakter/" + RefCharacter + ".fbx";
            AnimationClip[] found = new AnimationClip[Figure.ClipNames.Length];

            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (all == null || all.Length == 0)
            {
                Debug.LogWarning("Referans figur yok: " + path);
                return found;
            }

            foreach (Object o in all)
            {
                AnimationClip c = o as AnimationClip;
                if (c == null) continue;
                for (int i = 0; i < Figure.ClipNames.Length; i++)
                    if (found[i] == null && c.name == Figure.ClipNames[i]) found[i] = c;
            }

            for (int i = 0; i < found.Length; i++)
                if (found[i] == null)
                    Debug.LogWarning("Klip bulunamadi: " + Figure.ClipNames[i]);
            return found;
        }

        /// <summary>
        /// Tek bir denetleyici: her durus bir durum, gecisler kodda
        /// CrossFade ile yapiliyor.
        ///
        /// Kosul ve parametre YOK. Once "durum" adinda bir tamsayi
        /// parametresi ve Any State gecisleri dusunuldu; bes durus icin
        /// bes gecis ve bes kosul, hepsi CrossFade'in zaten yaptigi seyi
        /// yapmak icin. Ada gore CrossFade cagirmak ayni isi tek satirda
        /// goruyor ve denetleyici okunur kaliyor.
        /// </summary>
        private static AnimatorController BuildController(AnimationClip[] clips)
        {
            AnimatorController c =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (c == null) c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            AnimatorStateMachine sm = c.layers[0].stateMachine;
            for (int i = sm.states.Length - 1; i >= 0; i--)
                sm.RemoveState(sm.states[i].state);

            for (int i = 0; i < Figure.StateNames.Length; i++)
            {
                AnimatorState s = sm.AddState(Figure.StateNames[i]);
                s.motion = i < clips.Length ? clips[i] : null;
                s.writeDefaultValues = true;
                if (i == 0) sm.defaultState = s;
            }

            EditorUtility.SetDirty(c);
            return c;
        }

        // =====================================================================
        /// <summary>Referansi olan klasorler icin tek bir olcek hesaplar.</summary>
        private static Dictionary<string, float> ReferenceScales()
        {
            Dictionary<string, float> scales = new Dictionary<string, float>();

            foreach (KeyValuePair<string, string> kv in FolderReference)
            {
                string path = Art + "/" + kv.Key + "/" + kv.Value + ".fbx";
                GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (src == null) { Debug.LogWarning("Referans yok: " + path); continue; }

                GameObject tmp = Object.Instantiate(src);
                tmp.transform.position = Vector3.zero;
                tmp.transform.rotation = Quaternion.identity;
                tmp.transform.localScale = Vector3.one;

                Bounds b = Measure(tmp);
                float s = ScaleFor(kv.Value, kv.Key, b);
                Object.DestroyImmediate(tmp);

                scales[kv.Key] = s;
                Debug.Log(string.Format(
                    "  {0} klasoru {1} referansiyla olceklendi: {2:0.000}"
                    + "   (olculen {3:0.00} x {4:0.00} x {5:0.00} m)",
                    kv.Key, kv.Value, s, b.size.x, b.size.y, b.size.z));
            }
            return scales;
        }

        private static bool MakePrefab(string modelPath, Dictionary<string, Material> cache,
                                       AnimatorController ctrl, AnimationClip[] clips,
                                       Dictionary<string, float> folderScale)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (source == null) return false;

            string folder = FolderOf(modelPath);
            string name = Path.GetFileNameWithoutExtension(modelPath);
            bool character = folder == "Karakter";

            // Modelin ICINE degil, USTUNE bir kok konuyor.
            //
            // Sebep olcum: bazi modellerin donme noktasi govdenin
            // ortasinda - yuvarlak masa 0,32 m zemine gomuluyordu. Tabana
            // oturtmak icin modeli kaydirmak gerekiyor, ama kokun kendisi
            // kaydirilamaz: yerlestirme onu kullaniyor. Ayri bir kok ikisini
            // birbirinden ayiriyor.
            GameObject root = new GameObject(name);
            GameObject model = Object.Instantiate(source);
            model.name = "model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            Bounds b = Measure(model);
            float scale;
            if (!folderScale.TryGetValue(folder, out scale))
                scale = ScaleFor(name, folder, b);

            float scaleY = scale;
            Target tgt;
            if (Targets.TryGetValue(name, out tgt) && tgt.StretchY > 0f && b.size.y > 0.0001f)
                scaleY = tgt.StretchY / b.size.y;

            model.transform.localScale = new Vector3(scale, scaleY, scale);

            // Yatayda ortalama YALNIZCA esyada. Karakterlerde baglanma
            // durusu asimetrik olabiliyor (bir figurun eli bir sey
            // tutuyor) ve ortalamak govdeyi sandalyeden kaydiriyor.
            model.transform.localPosition = new Vector3(
                character ? 0f : -b.center.x * scale,
                -b.min.y * scaleY,
                character ? 0f : -b.center.z * scale);

            if (character) ShrinkHead(model);
            if (character) AddKnees(model, name);

            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = folder + "/" + Clean(mats[i].name, folder);
                    if (cache.TryGetValue(key, out Material m)) mats[i] = m;
                }
                r.sharedMaterials = mats;

                // Golgeyi yalnizca insanlar veriyor. Kucuk esyalarin golgesi
                // kat planini kirletiyor ve 34 derecelik bakista bir
                // sandalyenin golgesi zaten okunmuyor; mobilde de her golge
                // veren nesne ayri bir cizim demek.
                r.shadowCastingMode = character
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            if (character) Animate(model, ctrl, clips);

            Debug.Log(string.Format(
                "  {0,-24} {1:0.00} x {2:0.00} x {3:0.00} m  ->  olcek {4:0.000}"
                + "  ({5:0.00} x {6:0.00} x {7:0.00} m)",
                name, b.size.x, b.size.y, b.size.z, scale,
                b.size.x * scale, b.size.y * scaleY, b.size.z * scale));

            MakeFolder(PrefabDir, folder);
            string path = PrefabDir + "/" + folder + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return true;
        }

        /// <summary>
        /// Karakterin boyu (m). Yerlesim denetimi de BURADAN soruyor:
        /// sayiyi iki yere yazmak, birinin eskimesi demek - denetim
        /// uzun sure "hedef 1,28" diye yazdi, hedef coktan degismisti.
        /// </summary>
        public const float CharacterHeight = 1.00f;

        /// <summary>Kafanin govdeye orani. 1 = paketin kendi orani.</summary>
        private const float HeadScale = 0.80f;

        /// <summary>
        /// Kafa kemigini kucultur.
        ///
        /// Butun figuru kucultmek yerine: olculdu, figurler mobilyaya
        /// gore zaten gercegin yarisi kadar (ayakta 0,94 m, sandalye
        /// sirtligi 0,92 m; gercek oran 1,9). Buyuk duran sey kafa -
        /// govdenin %36'si, gercek insanda %13.
        ///
        /// Kemik olcegi derili mesh'e tasiniyor, yani yalnizca ona
        /// bagli koseler kuculuyor: govde, kollar ve bacaklar
        /// degismiyor. Sapka ve sac kafa kemigine bagli oldugu icin
        /// onlar da birlikte kuculuyor - ayri ayri ele almak gerekmiyor.
        /// </summary>
        private static void ShrinkHead(GameObject model)
        {
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "head") continue;
                t.localScale = t.localScale * HeadScale;
                return;
            }
            Debug.LogWarning("head kemigi bulunamadi: " + model.name);
        }

        // =====================================================================
        // DIZ KEMIGI.
        //
        // Paketin iskeletinde bacak basina TEK kemik var (root, leg-left,
        // leg-right, torso, arm-left, arm-right, head) - yani bacak
        // dizden kirilamiyor. Sandalyede bunun bedeli olculdu: kalca
        // minderin ustunde duracaksa, kalcadan asagi inen tek parca
        // bacak minderin icinden gecmek ZORUNDA. Paketin kendi oturma
        // klibi bu yuzden uylugu yatay tutuyor ve ayaklar one uzaniyor -
        // "sandalyede oturan insan" degil "yere bagdas kurmus insan".
        //
        // Cozum kemigi EKLEMEK. Bacak agi buna elverisli: bacak boyunca
        // 22 ayri kose seviyesi var, yani yeni kemik gercekten bukuyor,
        // kutuyu carpitmiyor.
        //
        // NEDEN URETIM HATTINDA: tek seferlik bir duzenleme bir sonraki
        // "Model prefablarini uret" calismasinda silinirdi. Prefab
        // uretimi bu projede tek dogruluk kaynagi.
        // =====================================================================

        /// <summary>Diz kemiginin adi. Figure de bunu ariyor.</summary>
        public const string KneeLeft = "knee-left";

        /// <summary>Diz kemiginin adi. Figure de bunu ariyor.</summary>
        public const string KneeRight = "knee-right";

        private static readonly string[] LegBones = { "leg-left", "leg-right" };
        private static readonly string[] KneeBones = { KneeLeft, KneeRight };

        /// <summary>
        /// Karakter modellerinde mesh okumayi acar.
        ///
        /// Agirlik ve kose okunmadan yeniden agirliklandirma yapilamaz
        /// ve paket varsayilan olarak KAPALI geliyor (isReadable: 0) -
        /// ilk denemede .vertices bos dondu ve sebebi buydu.
        ///
        /// Bedeli: mesh verisinin bir kopyasi bellekte kaliyor. 771
        /// koseli bir figur icin olculemez; yalnizca Karakter klasoru
        /// aciliyor, mobilya kapali kaliyor.
        /// </summary>
        private static void MakeCharactersReadable()
        {
            int acilan = 0;
            foreach (string g in AssetDatabase.FindAssets(
                         "t:Model", new[] { Art + "/Karakter" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null || mi.isReadable) continue;
                mi.isReadable = true;
                mi.SaveAndReimport();
                acilan++;
            }
            if (acilan > 0) Debug.Log("  mesh okuma acildi: " + acilan + " karakter");
        }

        /// <summary>
        /// Her bacaga bir diz kemigi ekler ve diz altindaki koseleri ona
        /// baglar.
        ///
        /// AYIRMA DUZLEMI iki kose halkasinin ARASINDAN geciyor, halkanin
        /// uzerinden degil: halkanin uzerinden gecerse duz golgeli bir
        /// modelde ayni noktada duran iki kose farkli kemige duser ve
        /// yuzey ACILIR. Aralarindan gecince yalnizca TEK bir dortgen
        /// geriliyor - low-poly bir dizin istedigi tam olarak bu.
        /// </summary>
        private static void AddKnees(GameObject model, string name)
        {
            foreach (SkinnedMeshRenderer smr in
                     model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh src = smr.sharedMesh;
                if (src == null) continue;
                if (!src.isReadable)
                {
                    Debug.LogWarning("Mesh okunamiyor, diz eklenemedi: " + name);
                    continue;
                }

                Transform[] bones = smr.bones;
                if (BoneIndex(bones, KneeLeft) >= 0) continue;

                int[] leg = new int[2];
                bool tamam = true;
                for (int i = 0; i < 2; i++)
                {
                    leg[i] = BoneIndex(bones, LegBones[i]);
                    if (leg[i] < 0) tamam = false;
                }
                if (!tamam) continue;

                Vector3[] verts = src.vertices;
                BoneWeight[] w = src.boneWeights;
                Matrix4x4[] binds = src.bindposes;
                if (w.Length != verts.Length || binds.Length != bones.Length) continue;

                List<Transform> yeniKemik = new List<Transform>(bones);
                List<Matrix4x4> yeniBind = new List<Matrix4x4>(binds);

                // FORMULU MODELIN KENDI MATRISIYLE SINA.
                //
                // Yeni kemigin baglanma matrisi elle kuruluyor ve yanlis
                // kurulursa mesh SESSIZCE kayiyor - yerlesim denetimi
                // figuru 0,90 m olctu, hedef 1,00 idi. Ayni formul
                // paketin KENDI bacak kemigine uygulanip modelin
                // getirdigi matrisle karsilastiriliyor: tutmuyorsa
                // formul yanlis, uretilen kemik de yanlis olurdu.
                {
                    Matrix4x4 sinama = bones[leg[0]].worldToLocalMatrix
                                       * smr.transform.localToWorldMatrix;
                    float enBuyuk = 0f;
                    for (int e = 0; e < 16; e++)
                        enBuyuk = Mathf.Max(enBuyuk,
                                            Mathf.Abs(sinama[e] - binds[leg[0]][e]));
                    if (enBuyuk > 0.001f)
                        Debug.LogWarning("DIZ baglanma matrisi sinamasi: sapma "
                                         + enBuyuk.ToString("0.0000") + " (" + name + ")");
                    else
                        Debug.Log("  DIZ baglanma matrisi sinamasi TAMAM (" + name + ")");
                }

                for (int i = 0; i < 2 && tamam; i++)
                {
                    float duzlem = KneePlane(verts, w, leg[i]);
                    if (float.IsNaN(duzlem)) { tamam = false; break; }

                    Transform diz = NewBone(bones[leg[i]], KneeBones[i], duzlem, smr);
                    int dizIndex = yeniKemik.Count;
                    yeniKemik.Add(diz);
                    yeniBind.Add(diz.worldToLocalMatrix * smr.transform.localToWorldMatrix);

                    int tasinan = 0;
                    for (int v = 0; v < verts.Length; v++)
                    {
                        if (verts[v].y >= duzlem) continue;
                        BoneWeight bw = w[v];
                        if (Influences(bw, leg[i])) tasinan++;
                        if (bw.boneIndex0 == leg[i]) bw.boneIndex0 = dizIndex;
                        if (bw.boneIndex1 == leg[i]) bw.boneIndex1 = dizIndex;
                        if (bw.boneIndex2 == leg[i]) bw.boneIndex2 = dizIndex;
                        if (bw.boneIndex3 == leg[i]) bw.boneIndex3 = dizIndex;
                        w[v] = bw;
                    }
                    Debug.Log("  DIZ " + name + " " + KneeBones[i] + ": duzlem y "
                        + duzlem.ToString("0.0000") + " (bacak " + LegSpan(verts, w, leg[i])
                        + "), " + tasinan + " kose tasindi");
                }
                if (!tamam) continue;

                // Mesh KOPYASI: paketin varligi degistirilmiyor, ve
                // prefab'in baglanabilmesi icin diskte bir varlik olmali.
                Mesh kopya = Object.Instantiate(src);
                kopya.name = src.name;
                kopya.boneWeights = w;
                kopya.bindposes = yeniBind.ToArray();

                string mp = Art + "/Mesh/" + name + "-" + src.name + ".asset";
                AssetDatabase.DeleteAsset(mp);
                AssetDatabase.CreateAsset(kopya, mp);

                smr.sharedMesh = kopya;
                smr.bones = yeniKemik.ToArray();
            }
        }

        /// <summary>
        /// Dizin yuksekligi (mesh uzayi). Bacagin kendi koselerinin
        /// ortasina en yakin IKI HALKANIN ARASI.
        /// </summary>
        private static float KneePlane(Vector3[] verts, BoneWeight[] w, int leg)
        {
            List<float> seviye = new List<float>();
            float enAlt = float.MaxValue, enUst = float.MinValue;
            for (int v = 0; v < verts.Length; v++)
            {
                if (!Influences(w[v], leg)) continue;
                float y = verts[v].y;
                if (y < enAlt) enAlt = y;
                if (y > enUst) enUst = y;

                bool yeni = true;
                for (int j = 0; j < seviye.Count; j++)
                    if (Mathf.Abs(seviye[j] - y) < 0.0005f) { yeni = false; break; }
                if (yeni) seviye.Add(y);
            }
            if (seviye.Count < 3) return float.NaN;

            seviye.Sort();
            float orta = (enAlt + enUst) * 0.5f;

            float alt = seviye[0], ust = seviye[seviye.Count - 1];
            for (int j = 0; j < seviye.Count - 1; j++)
                if (seviye[j] <= orta && seviye[j + 1] >= orta)
                {
                    alt = seviye[j];
                    ust = seviye[j + 1];
                    break;
                }
            return (alt + ust) * 0.5f;
        }

        /// <summary>Bacak koselerinin y araligi. Duzlemin dogru yerde
        /// olup olmadigi ancak buna gore soylenebilir.</summary>
        private static string LegSpan(Vector3[] verts, BoneWeight[] w, int leg)
        {
            float a = float.MaxValue, b = float.MinValue;
            int n = 0;
            for (int v = 0; v < verts.Length; v++)
            {
                if (!Influences(w[v], leg)) continue;
                n++;
                if (verts[v].y < a) a = verts[v].y;
                if (verts[v].y > b) b = verts[v].y;
            }
            return a.ToString("0.000") + ".." + b.ToString("0.000") + " / " + n + " kose";
        }

        private static bool Influences(BoneWeight bw, int bone)
        {
            return (bw.boneIndex0 == bone && bw.weight0 > 0.01f)
                || (bw.boneIndex1 == bone && bw.weight1 > 0.01f)
                || (bw.boneIndex2 == bone && bw.weight2 > 0.01f)
                || (bw.boneIndex3 == bone && bw.weight3 > 0.01f);
        }

        private static int BoneIndex(Transform[] bones, string ad)
        {
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null && bones[i].name == ad) return i;
            return -1;
        }

        /// <summary>
        /// Diz kemigi: bacagin cocugu, ayirma duzleminin yuksekliginde,
        /// bacakla AYNI yone bakiyor.
        ///
        /// Ayni yone bakmasi onemli: Figure dizin dinlenme acisini
        /// figure gore saklayip oturuşta geri yaziyor. Kemigin kendi
        /// eksenlerinin nereye baktigini bilmek gerekmiyor.
        /// </summary>
        private static Transform NewBone(Transform bacak, string ad, float duzlemY,
                                         SkinnedMeshRenderer smr)
        {
            GameObject go = new GameObject(ad);
            go.transform.SetParent(bacak, false);

            Vector3 bacakMesh = smr.transform.InverseTransformPoint(bacak.position);
            go.transform.position = smr.transform.TransformPoint(
                new Vector3(bacakMesh.x, duzlemY, bacakMesh.z));
            go.transform.rotation = bacak.rotation;
            return go.transform;
        }

        /// <summary>
        /// Bacak kemiklerini ve BAGLANMA acilarini prefab'a yazar.
        ///
        /// Buradan yazilmasinin sebebi zamanlama: model su anda
        /// kesinlikle baglanma durusunda (bacaklar asagi). Calisma
        /// aninda okumaya calismak, klibin coktan degistirdigi bir pozu
        /// "baglanma acisi" sanmak demekti ve tam o oldu - baldir
        /// asagiya degil uylugun yonune gidiyordu.
        /// </summary>
        private static void BindLegs(GameObject model, Figure f)
        {
            List<Transform> uyluk = new List<Transform>();
            List<Transform> baldir = new List<Transform>();
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                for (int i = 0; i < 2; i++)
                {
                    if (t.name == LegBones[i]) uyluk.Add(t);
                    else if (t.name == KneeBones[i]) baldir.Add(t);
                }
            }

            Quaternion kokTers = Quaternion.Inverse(model.transform.rotation);
            f.Legs = uyluk.ToArray();
            f.LegRest = new Quaternion[f.Legs.Length];
            for (int i = 0; i < f.Legs.Length; i++)
                f.LegRest[i] = kokTers * f.Legs[i].rotation;

            f.Knees = baldir.ToArray();
            f.KneeRest = new Quaternion[f.Knees.Length];
            for (int i = 0; i < f.Knees.Length; i++)
                f.KneeRest[i] = kokTers * f.Knees[i].rotation;

            if (f.Knees.Length != 2)
                Debug.LogWarning("Diz kemigi eksik: " + model.name
                                 + " (" + f.Knees.Length + ")");
        }

        private static void Animate(GameObject model, AnimatorController ctrl,
                                    AnimationClip[] clips)
        {
            // Animator MODELIN kokunde olmali: kliplerdeki yollar
            // ("root/torso/arm-left") FBX kokune gore yazilmis.
            Animator a = model.GetComponent<Animator>();
            if (a == null) a = model.AddComponent<Animator>();
            a.runtimeAnimatorController = ctrl;
            a.applyRootMotion = false;
            // Gorunmeyen figurun iskeletini isletmeye gerek yok; on dort
            // masalik bir salonda bu fark ediyor.
            a.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            Figure f = model.GetComponent<Figure>();
            if (f == null) f = model.AddComponent<Figure>();
            f.Anim = a;
            f.Clips = clips;
            BindLegs(model, f);
        }

        // =====================================================================
        /// <summary>
        /// Modelin sinir kutusu, KOKUNE GORE.
        ///
        /// Renderer.bounds KULLANILMIYOR. Deriye bagli aglarda o deger
        /// kok kemigin uzayindan geliyor ve gercegi yansitmiyor:
        /// karakterler 0,67 m olculdu, buna gore 2,53 ile olceklendi ve
        /// salonda uc metre boyunda dev figurler olarak cizildi. Agin
        /// kendi sinir kutusunu modelin kokune tasimak her iki cizici
        /// turunde de dogru sonucu veriyor.
        /// </summary>
        private static Bounds Measure(GameObject go)
        {
            Matrix4x4 toRoot = go.transform.worldToLocalMatrix;
            Bounds b = new Bounds();
            bool any = false;

            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null)
                    Add(ref b, ref any, mf.sharedMesh.bounds,
                        toRoot * mf.transform.localToWorldMatrix);

            foreach (SkinnedMeshRenderer sk in
                     go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (sk.sharedMesh != null)
                    Add(ref b, ref any, sk.sharedMesh.bounds,
                        toRoot * sk.transform.localToWorldMatrix);

            return any ? b : new Bounds(Vector3.zero, Vector3.one);
        }

        /// <summary>Yerel kutunun sekiz kosesini tasiyip birlestirir.</summary>
        private static void Add(ref Bounds b, ref bool any, Bounds local, Matrix4x4 m)
        {
            Vector3 c = local.center, e = local.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = m.MultiplyPoint3x4(new Vector3(
                    c.x + ((i & 1) == 0 ? -e.x : e.x),
                    c.y + ((i & 2) == 0 ? -e.y : e.y),
                    c.z + ((i & 4) == 0 ? -e.z : e.z)));
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                else b.Encapsulate(p);
            }
        }

        private static float ScaleFor(string name, string folder, Bounds b)
        {
            Target t;
            if (!Targets.TryGetValue(name, out t)
                && !FolderTarget.TryGetValue(folder, out t))
                return 1f;

            float measured = t.Width ? Mathf.Max(b.size.x, b.size.z) : b.size.y;
            if (measured <= 0.0001f) return 1f;
            return t.Metres / measured;
        }

        // =====================================================================
        /// <summary>
        /// "wood (Instance)" ve "Mobilya_wood" gibi adlari sade "wood"a
        /// indirir. Klasor onekini de atiyor ki arac kendi urettigi
        /// malzemeyi ikinci kosuda "Mobilya_Mobilya_wood" diye
        /// cogaltmasin.
        /// </summary>
        private static string Clean(string name, string folder)
        {
            int p = name.IndexOf(' ');
            if (p > 0) name = name.Substring(0, p);
            if (folder.Length > 0 && name.StartsWith(folder + "_"))
                name = name.Substring(folder.Length + 1);
            return name.Replace(".", "_");
        }

        private static string FolderOf(string path)
        {
            string[] parts = path.Split('/');
            for (int i = 0; i < parts.Length - 1; i++)
                if (parts[i] == "Art") return parts[i + 1];
            return "";
        }

        /// <summary>
        /// Klasoru olusturur. Directory.CreateDirectory DEGIL - o yolla
        /// acilan klasorun varlik veritabaninda karsiligi olmuyor ve
        /// CreateAsset sessizce basarisiz oluyor.
        /// </summary>
        private static void MakeFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static string FindTexture(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder + "/Textures")) return null;
            foreach (string g in AssetDatabase.FindAssets(
                         "t:Texture2D", new[] { folder + "/Textures" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.ToLower().Contains("colormap")) return p;
            }
            return null;
        }
    }
}
