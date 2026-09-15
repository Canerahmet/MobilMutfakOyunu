using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// SAHNE SUSLEMESI VE MUTFAK KIMLIGI.
    ///
    /// Kullanicinin getirdigi referans dort mutfagi YAN YANA gosteriyor:
    /// ayni arayuz, ayni acili kamera, ama dort ayri lokanta. Farki
    /// yapan sey mimari degil - duvarin rengi, tabelanin isigi, tavandan
    /// sarkan lambalar, yerdeki hali ve saksilar.
    ///
    /// docs/25 bunu zaten kural olarak yazmisti: "mutfaklar ORTAM, ISIK,
    /// SILUET ve KIYAFETLE ayrisir; yuz hatlariyla asla". Burasi o
    /// kuralin ortam ayagi.
    ///
    /// Hepsi PROSEDUREL (Modeler) ve RENGE GORE tek orguye toplaniyor:
    /// butun susleme, kac parcadan olusursa olussun, bir avuc cizim.
    /// </summary>
    public sealed partial class RestaurantView
    {
        /// <summary>
        /// Bir mutfagin ortam paleti.
        ///
        /// Renkler oyunun kendi malzemesinden (URP/Lit + property block)
        /// geciyor, yani doku yok - ayrim tamamen RENK ve BICIM.
        /// </summary>
        public struct Palette
        {
            public Color Wall;       // arka duvar
            public Color WallTrim;   // supurgelik ve cerceve
            public Color Wood;       // ahsap: tezgah, raf, sarkit govdesi
            public Color Metal;      // metal: davlumbaz, ayak
            public Color Accent;     // kimligin rengi: neon / bakir
            public Color Sign;       // tabelanin isigi (emissive)
            public Color Lamp;       // sarkit lambanin isigi (emissive)
            public Color Plant;      // yaprak
            public Color Rug;        // hali / zemin kaplamasi
            public Color Seat;       // sandalye minderi / sedir
            public Color Floor;      // zemin deseninin acik tonu
            public Color FloorAlt;   // zemin deseninin koyu tonu
            public bool HasRug;      // hali var mi
            public bool Planks;      // zemin deseni: ahsap tahta mi, fayans mi

            // --- DUVARIN YUZEYI ---------------------------------------
            //
            // Duvar tek bir duz kutuydu: govde + supurgelik + korniz.
            // Iki mutfak ayni duvari farkli renkte gosteriyordu, yani
            // "baska bir yere girdim" hissinin yarisi eksikti.
            //
            // Arastirmadan gelen ayrim (kaynaklar docs/50):
            //   Turk lokantasi - AHSAP LAMBRI. Geleneksel esnaf
            //   lokantasinin tanimlayici yuzeyi; ustunde sivali duvar.
            //   Hizli yemek   - PANEL DERZI + CELIK BANT. Sert, silinir
            //   yuzeyler; yatay celik serit ve duşey panel ekleri.
            //
            // Ikisi de Modeler kutusu, yani yeni varlik yok - indirilen
            // doku da yok (lisans defterine girecek bir sey eklenmiyor).

            /// <summary>Lambri yuksekligi, metre. 0 ise lambri yok.</summary>
            public float Wainscot;

            /// <summary>Lambri / panel rengi.</summary>
            public Color WainscotColor;

            /// <summary>Lambrinin ust kenarindaki ince kusak rengi.</summary>
            public Color WainscotCap;

            /// <summary>Duşey derz araligi, metre. 0 ise derz yok.</summary>
            public float SeamStep;

            /// <summary>Disarinin tonu; arka plan gokyuzune karisiyor.</summary>
            public Color Sky;
        }

        /// <summary>
        /// MUTFAGIN PALETI.
        ///
        /// Hizli yemek: tuglasi koyu, isigi NEON KIRMIZI, metali celik.
        /// Referansin birinci karesi tam olarak bu - kirmizi tabela,
        /// paslanmaz tezgah, siyah-kirmizi zemin.
        ///
        /// Turk: duvari sicak tugla ve ahsap, isigi BAKIR, yerde hali.
        /// Referansin ikinci karesi: pirinc fenerler, kilim, ahsap
        /// kafes.
        /// </summary>
        public static Palette Pal(string cuisine)
        {
            if (cuisine == "turk")
            {
                return new Palette
                {
                    Wall = new Color(0.290f, 0.196f, 0.137f),
                    WallTrim = new Color(0.176f, 0.118f, 0.082f),
                    Wood = new Color(0.424f, 0.282f, 0.173f),
                    Metal = new Color(0.706f, 0.545f, 0.267f),
                    Accent = new Color(0.804f, 0.561f, 0.239f),
                    Sign = new Color(1.000f, 0.729f, 0.322f),
                    Lamp = new Color(1.000f, 0.843f, 0.596f),
                    Seat = new Color(0.451f, 0.192f, 0.176f),
                    Plant = new Color(0.267f, 0.427f, 0.243f),
                    Rug = new Color(0.478f, 0.161f, 0.133f),
                    Floor = new Color(0.404f, 0.290f, 0.196f),
                    FloorAlt = new Color(0.341f, 0.239f, 0.161f),
                    HasRug = true,
                    Planks = true,

                    // AHSAP LAMBRI: geleneksel esnaf lokantasinin
                    // tanimlayici yuzeyi. Ustu sivali duvar, ust kenarda
                    // ince bakir kusak.
                    //
                    // 1,05 m secildi cunku OTURAN kisinin sirti o
                    // hizada: lambri gercek hayatta da sandalye
                    // yuksekligini korumak icin var, sus degil.
                    Wainscot = 1.05f,
                    WainscotColor = new Color(0.361f, 0.235f, 0.141f),
                    WainscotCap = new Color(0.706f, 0.545f, 0.267f),
                    SeamStep = 0.85f,
                    // Disarisi da sicak: mahalle, tozlu ogle isigi.
                    Sky = new Color(0.82f, 0.68f, 0.48f),
                };
            }

            return new Palette
            {
                Wall = new Color(0.173f, 0.184f, 0.212f),
                WallTrim = new Color(0.106f, 0.114f, 0.133f),
                Wood = new Color(0.361f, 0.251f, 0.180f),
                Metal = new Color(0.616f, 0.651f, 0.702f),
                Accent = new Color(0.847f, 0.239f, 0.196f),
                Sign = new Color(1.000f, 0.314f, 0.251f),
                // LAMBA ISIGI TABELADAN AYRI.
                //
                // Ilk denemede sarkitlarin agzi da tabela rengiyle
                // yaniyordu ve hizli yemek salonu PEMBE bir isikla
                // doluyordu. Tabela kimligin rengi (neon kirmizi), lamba
                // ise her lokantada ayni sey: sicak beyaz.
                Lamp = new Color(1.000f, 0.898f, 0.749f),
                // Hizli yemek sandalyesi KIRMIZI: referansin birinci
                // karesinde salonun rengini o veriyor. Turk tarafinda
                // ise koyu bordo - ayni aile, farkli ton.
                Seat = new Color(0.729f, 0.220f, 0.192f),
                Plant = new Color(0.286f, 0.478f, 0.290f),
                Rug = new Color(0.216f, 0.231f, 0.267f),
                // ZEMIN ACILDI: 0,32/0,24 -> 0,42/0,33.
                //
                // Koyu fayans yakin planda "gece cekilmis" gibi
                // duruyordu ve uzerindeki koyu mobilyayi yutuyordu.
                // Referansin hizli yemek karesinde zemin ORTA tonda;
                // kimligi veren sey zeminin koyulugu degil, neon
                // kirmizi ve celik.
                Floor = new Color(0.420f, 0.435f, 0.467f),
                FloorAlt = new Color(0.325f, 0.341f, 0.373f),
                HasRug = false,
                Planks = false,

                // PANEL DERZI + CELIK BANT.
                //
                // Hizli yemek salonunun yuzeyi "silinir" olmali:
                // laminat panel ekleri ve tezgah hizasinda paslanmaz
                // bir serit. Lambri YOK - o baska bir mekanin dili.
                Wainscot = 1.15f,
                WainscotColor = new Color(0.137f, 0.145f, 0.169f),
                WainscotCap = new Color(0.616f, 0.651f, 0.702f),
                SeamStep = 1.15f,
                // Disarisi soguk ve sehirli: cadde, asfalt, cam.
                Sky = new Color(0.58f, 0.68f, 0.82f),
            };
        }

        /// <summary>
        /// MOBILYA DA KIMLIK TASIYOR.
        ///
        /// Paketin malzemelerinin DOKUSU YOK - hepsi duz bir
        /// _BaseColor (Mobilya_wood, Mobilya_metal, ...). Yani mutfaga
        /// gore renklendirmek icin dokuyla ugrasmak gerekmiyor:
        /// malzemenin bir KOPYASI cikariliyor ve rengi paletten
        /// yaziliyor.
        ///
        /// KOPYA MALZEME BASINA BIR TANE. Alternatifi her ciziciye
        /// property block yazmakti ve o, cizicileri SRP toplu ciziminin
        /// DISINA atiyor (bu proje bunu zemin levhalarinda ogrendi):
        /// yuz parca mobilya, yuz ayri cizim demekti. Tek kopya, ayni
        /// malzemeyi paylasan butun mobilyayi birden boyuyor.
        /// </summary>
        private Material Tinted(Material src)
        {
            if (src == null) return null;
            if (_tinted.TryGetValue(src, out Material hazir)) return hazir;

            Palette p = Pal(CuisineId);
            string ad = src.name.ToLowerInvariant();
            Color? renk = null;

            if (ad.Contains("carpet")) renk = p.Seat;
            else if (ad.Contains("wooddark")) renk = p.Wood * 0.72f;
            else if (ad.Contains("wood")) renk = p.Wood;
            // METAL BOYANMIYOR.
            //
            // Ilk denemede paletin metali (Turk'te PIRINC) butun metal
            // parcalara gitti ve mutfak ALTIN oldu: lavabo, tezgah,
            // buzdolabi. Davlumbazda ogrenilen sey burada da gecerli -
            // ekipman her lokantada paslanmazdir; pirinc bir SUSLEME
            // rengi (korkuluk, fener) ve orada kaliyor.
            else if (ad.Contains("lamp")) renk = p.Sign;
            else if (ad.Contains("plant")) renk = p.Plant;

            Material m = src;
            if (renk.HasValue)
            {
                Color c = renk.Value;
                c.a = 1f;
                m = new Material(src);
                m.name = src.name + "_" + CuisineId;
                m.SetColor(BaseColorId, c);
            }
            _tinted[src] = m;
            return m;
        }

        private readonly System.Collections.Generic.Dictionary<Material, Material>
            _tinted = new System.Collections.Generic.Dictionary<Material, Material>();

        /// <summary>
        /// Ton kopyalarini yok eder ve onbellegi bosaltir.
        ///
        /// Kopyalar `new Material(src)` ile uretiliyor, yani her biri
        /// AYRI bir UnityEngine.Object. Onbellek Rebuild()'te
        /// bosaltiliyordu ama kopyalar yok edilmiyordu: kademe basina
        /// bes kadar malzeme kalici olarak siziyordu, kimlik degisince
        /// (PreviewCuisine) editor araclarinda katlaniyordu.
        ///
        /// KAYNAGIN KENDISI YOK EDILMIYOR: `Tinted` renk gerekmeyen
        /// malzeme icin kaynagi oldugu gibi donduruyor ve o bir DISK
        /// VARLIGI - onu silmek bir sonraki sahnede malzemeyi kayip
        /// ederdi. OnDestroy'daki liste tam bu yuzden kisa.
        /// </summary>
        private void ClearTints()
        {
            foreach (System.Collections.Generic.KeyValuePair<Material, Material> kv in _tinted)
            {
                if (kv.Value == null || kv.Value == kv.Key) continue;
                if (Application.isPlaying) Destroy(kv.Value);
                else DestroyImmediate(kv.Value);
            }
            _tinted.Clear();
        }

        /// <summary>
        /// Bir prefab kopyasinin butun cizicilerini kimligin tonuna
        /// cevirir. Yerlestiren her yerden cagriliyor.
        /// </summary>
        private void Retint(GameObject go)
        {
            if (go == null) return;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                bool degisti = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material t = Tinted(ms[i]);
                    if (t == ms[i]) continue;
                    ms[i] = t;
                    degisti = true;
                }
                if (degisti) r.sharedMaterials = ms;
            }
        }

        /// <summary>Goruntu araci icin: kimlik oyun disinda da secilebilsin.</summary>
        [System.NonSerialized] public string PreviewCuisine;

        private string CuisineId
        {
            get
            {
                if (App != null && App.Content != null) return App.Content.Cuisine;
                return string.IsNullOrEmpty(PreviewCuisine) ? "fastfood" : PreviewCuisine;
            }
        }

        /// <summary>Arka duvarin yuksekligi. Odalarin duvarindan cok daha yuksek.</summary>
        private const float BackWallHeight = 2.60f;

        // =====================================================================
        // TAVAN LAMBASI CIZILMIYOR - ISIK CIZILIYOR.
        //
        // Burada bir kez sarkit lamba VARDI ve kullanici iki kez ayni
        // seyi soyledi: "lambalar fiziksel olarak gozukmesin, tavanda
        // olacaklari icin" (docs/38) ve sonra referans gorsellerinden
        // sonra yeniden: "tavandaki isiklar gozukmesin".
        //
        // Arada referansa bakip sarkitlari eklemistim; referansin
        // kamerasi daha alcak ve orada sarkit mekanin yarisi. Bizim
        // kameramiz 34 dereceden ve TAVANSIZ bir binaya bakiyor -
        // sarkitlar salonun uzerinde asili duran, aydinlattigi yeri
        // KAPATAN nesnelere donuyordu.
        //
        // Isik duruyor: yerdeki havuzlar (BuildRoomLights) ve aksamin
        // sicak dolgusu. Oyuncunun gordugu sey ZATEN isikti, lambanin
        // kendisi degil.
        //
        // Bos govdeli Pendants() metodu ve PendantY sabiti SILINDI:
        // "cagiranlarina yalan soyleyen bir alan silinir, baglanmaz".
        private void BuildDecor(int tables)
        {
            Palette p = Pal(CuisineId);
            if (_block == null) _block = new MaterialPropertyBlock();

            // ACIK ODALARIN SINIRI: susleme de kademeyle birlikte
            // buyuyor. Sokak lambalari ayni hesabi kullaniyor.
            float sol = float.MaxValue, sag = 0f, arka = 0f;
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tables)) continue;
                if (r.X0 < sol) sol = r.X0;
                if (r.X0 + r.W > sag) sag = r.X0 + r.W;
                if (r.Z0 + r.D > arka) arka = r.Z0 + r.D;
            }
            if (sol > sag) return;

            // DISARISI DA MUTFAGA AIT.
            //
            // Arka plan gokyuzu yerine geciyor ve iki mutfakta birebir
            // ayniydi. Burada baglaniyor cunku mutfak CALISMA ZAMANINDA
            // seciliyor - sahne kurulurken (BuildGameScene) daha bilinmiyor.
            DayLight gun = FindFirstObjectByType<DayLight>();
            if (gun != null) gun.SkyTint = p.Sky;

            Modeler m = new Modeler();
            Modeler isik = new Modeler();

            FloorPattern(m, p, tables);
            Backdrop(m, p, sol, sag, arka);
            Planters(m, p, sol, sag);
            KitchenHood(m, p, tables);
            ServiceCounter(m, isik, p, tables);
            Shelves(m, p, tables);
            Booths(m, p, tables);
            Boards(m, isik, p, sol, sag, arka, tables);
            Rugs(m, p, tables);
            Terrace(m, p, sol, sag);
            Storefront(m, p, sol, sag);
            PatioSeats(m, p, sol, sag);

            _decor = m.Build(transform, "Susleme", _floorMat, _block);

            // ISIKLI PARCALAR AYRI MALZEMEDE: emisyon anahtari kapaliyken
            // gölgelendirici o alani hic okumuyor, yani tabelaya yazilan
            // renk hicbir sey yapmaz.
            if (_lampMat == null && _floorMat != null)
            {
                _lampMat = new Material(_floorMat);
                _lampMat.EnableKeyword("_EMISSION");
                _lampMat.globalIlluminationFlags =
                    MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            // ISIKLI PARCALARIN RENGI ORGUDEN GELIYOR.
            //
            // Modeler renge gore ayirdigi icin her isikli renk kendi
            // ciziciSINDE: tabela kirmizi yanarken sarkitlar sicak beyaz
            // yanabiliyor. Emisyon, cizicinin KENDI temel renginden
            // turetiliyor - iki yere ayri renk yazmak gerekmiyor.
            _decorGlow = isik.Build(transform, "SuslemeIsik", _lampMat, _block);
            foreach (Renderer r in _decorGlow.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(_block);
                Color c = _block.GetColor(BaseColorId);
                if (c.a <= 0f) c = p.Lamp;
                _block.SetColor(BaseColorId, c);
                _block.SetColor(Shader.PropertyToID("_EmissionColor"), c * 1.9f);
                r.SetPropertyBlock(_block);
            }
        }

        private GameObject _decor, _decorGlow;

        /// <summary>Susleme parcasi sayisi (cizim). Turun sorabilmesi icin.</summary>
        public int DecorDrawCount
        {
            get
            {
                int n = _decor != null ? _decor.transform.childCount : 0;
                return n + (_decorGlow != null ? _decorGlow.transform.childCount : 0);
            }
        }

        // =====================================================================
        /// <summary>
        /// ARKA PERDE: yuksek ve DOLU bir duvar, iki yanda donusler.
        ///
        /// Oda duvarlari 1,15 m ve saydam - 34 derecelik bakista salonun
        /// icini gorebilmek icin oyle. Ama o yuzden lokantanin ARKASI da
        /// bostu: binanin bittigi yerde gokyuzu basliyordu ve mekan bir
        /// "kat plani" gibi okunuyordu.
        ///
        /// Referansin dordunde de arka duvar DOLU: tugla, ahsap, uzerinde
        /// tabela ve raflar. Arka duvar hicbir seyi kapatmiyor - kamera
        /// ona zaten arkadan bakiyor - ama mekani bir ODA yapiyor.
        /// </summary>
        private void Backdrop(Modeler m, Palette p, float sol, float sag, float arka)
        {
            float genislik = sag - sol;
            float orta = (sol + sag) * 0.5f;

            // Duvar govdesi
            m.Box(new Vector3(orta, BackWallHeight * 0.5f, arka + 0.09f),
                  new Vector3(genislik + 0.36f, BackWallHeight, 0.18f), p.Wall);

            // Supurgelik ve ust korniz: iki ince serit duvarin duz
            // yuzeyini kiriyor ve olcegi okunur yapiyor.
            m.Box(new Vector3(orta, 0.09f, arka + 0.05f),
                  new Vector3(genislik + 0.40f, 0.18f, 0.26f), p.WallTrim);
            m.Box(new Vector3(orta, BackWallHeight - 0.07f, arka + 0.05f),
                  new Vector3(genislik + 0.40f, 0.14f, 0.26f), p.WallTrim);

            // --- YUZEY: LAMBRI / PANEL --------------------------------
            //
            // Duvar bu satirlara kadar TEK DUZ KUTUYDU ve iki mutfak
            // ayni yuzeyi farkli renkte gosteriyordu. Ayrim artik
            // malzemede: Turk'te ahsap lambri, hizli yemekte panel
            // derzi + celik bant.
            //
            // Hepsi ince kutu; yeni varlik yok, indirilen doku yok.
            if (p.Wainscot > 0f)
            {
                // Govde: duvarin onune 2 cm cikinti - duz bir renk
                // degisiminden farki bu, kenarinda golge olusuyor.
                m.Box(new Vector3(orta, p.Wainscot * 0.5f, arka + 0.02f),
                      new Vector3(genislik + 0.36f, p.Wainscot, 0.06f),
                      p.WainscotColor);

                // Ust kusak: lambriyi bitiren ince serit (Turk'te bakir,
                // hizli yemekte paslanmaz). Referansta ikisi de var.
                m.Box(new Vector3(orta, p.Wainscot + 0.02f, arka + 0.00f),
                      new Vector3(genislik + 0.38f, 0.05f, 0.10f),
                      p.WainscotCap);
            }

            // Duşey derzler: lambriyi tahtalara, paneli levhalara boluyor.
            // Olcegi okunur yapan sey bu - duz bir yuzeyde goz mesafeyi
            // tartamiyor.
            if (p.SeamStep > 0.2f && p.Wainscot > 0f)
            {
                int adet = Mathf.FloorToInt(genislik / p.SeamStep);
                for (int i = 1; i <= adet; i++)
                {
                    float x = sol + p.SeamStep * i;
                    if (x > sag - 0.1f) break;
                    m.Box(new Vector3(x, p.Wainscot * 0.5f, arka - 0.01f),
                          new Vector3(0.025f, p.Wainscot - 0.04f, 0.04f),
                          p.WallTrim);
                }
            }

            // Yan donusler: mekan iki yandan da kapaniyor.
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? sol - 0.09f : sag + 0.09f;
                m.Box(new Vector3(x, BackWallHeight * 0.5f, arka - 0.85f),
                      new Vector3(0.18f, BackWallHeight, 1.9f), p.Wall);
                m.Box(new Vector3(x, 0.09f, arka - 0.85f),
                      new Vector3(0.24f, 0.18f, 1.9f), p.WallTrim);

                // Yan duvarlarda da ayni yuzey: biri lambrili biri duz
                // olsaydi mekan yarim kalmis gorunurdu.
                if (p.Wainscot > 0f)
                {
                    float xi = i == 0 ? x + 0.10f : x - 0.10f;
                    m.Box(new Vector3(xi, p.Wainscot * 0.5f, arka - 0.85f),
                          new Vector3(0.06f, p.Wainscot, 1.9f), p.WainscotColor);
                    m.Box(new Vector3(xi, p.Wainscot + 0.02f, arka - 0.85f),
                          new Vector3(0.10f, 0.05f, 1.9f), p.WainscotCap);
                }
            }
        }

        /// <summary>
        /// SAKSILAR: cepheye ve terasin kenarina.
        ///
        /// Referansin dordunde de yesillik var ve hepsinde ayni ise
        /// yariyor: binanin duz kenarini kiriyor, "burasi bakimli bir
        /// yer" diyor. Bitki ayrica mutfaga gore degismiyor - dordunde
        /// de var, cunku evrensel.
        /// </summary>
        private void Planters(Modeler m, Palette p, float sol, float sag)
        {
            // Cephe boyunca: kaldirimla bina arasi (z = -0,10).
            int adet = Mathf.Max(2, Mathf.RoundToInt((sag - sol) / 3.2f));
            float aralik = (sag - sol) / adet;

            for (int i = 0; i <= adet; i++)
            {
                float x = sol + aralik * i;
                // Kapinin onunu kapatma: giris Paths.DoorX'te.
                if (Mathf.Abs(x - Paths.DoorX) < 1.3f) continue;
                Planter(m, p, x, -0.26f);
            }
        }

        private void Planter(Modeler m, Palette p, float x, float z)
        {
            // Kasa: alti dar, ustu genis - dokme bir saksi silueti.
            m.Prism(6, 0.17f, 0.22f, 0.30f, new Vector3(x, 0f, z),
                    Quaternion.identity, p.WallTrim);
            m.Box(new Vector3(x, 0.31f, z), new Vector3(0.46f, 0.05f, 0.46f), p.Wood);

            // Yapraklar: uc ayri yukseklikte kume. Tek bir kure
            // "top" gibi okunuyordu; uc kume "calilik" gibi.
            m.Prism(6, 0.20f, 0.05f, 0.34f, new Vector3(x, 0.32f, z),
                    Quaternion.identity, p.Plant);
            m.Prism(5, 0.13f, 0.03f, 0.26f, new Vector3(x - 0.12f, 0.33f, z + 0.06f),
                    Quaternion.identity, p.Plant);
            m.Prism(5, 0.12f, 0.03f, 0.22f, new Vector3(x + 0.11f, 0.33f, z - 0.05f),
                    Quaternion.identity, p.Plant);
        }

        /// <summary>
        /// DAVLUMBAZ: ocak sirasinin uzerinde.
        ///
        /// Referansin dordunde de mutfagin ustunde paslanmaz bir
        /// davlumbaz var ve "burasi mutfak" diyen tek parca o. Bizim
        /// mutfakta ocaklar vardi ama ustu bostu.
        /// </summary>
        private void KitchenHood(Modeler m, Palette p, int tables)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (r.Name != "Mutfak") continue;
                if (!RoomPlan.RoomOpen(in r, tables)) return;

                float z = r.Z0 + r.D - 0.75f;
                float x = r.CenterX - 0.30f;
                float w = r.W - 1.9f;

                // Govde: asagi genisleyen bir huni.
                // DAVLUMBAZ HER MUTFAKTA CELIK.
                //
                // Ilk denemede kimligin metal rengini aliyordu ve Turk
                // mutfaginda ALTIN bir kutu olarak duruyordu. Davlumbaz
                // bir sus degil ekipman; her lokantada paslanmazdir.
                Color celik = new Color(0.576f, 0.612f, 0.659f);
                m.Box(new Vector3(x, 1.62f, z), new Vector3(w, 0.34f, 0.86f), celik);
                m.Box(new Vector3(x, 1.82f, z), new Vector3(w * 0.55f, 0.30f, 0.50f),
                      celik);
                // Alt agiz: koyu bir serit, huninin agzi.
                m.Box(new Vector3(x, 1.44f, z), new Vector3(w - 0.12f, 0.06f, 0.74f),
                      p.WallTrim);
                return;
            }
        }

        /// <summary>
        /// TABELA VE MENU TAHTASI.
        ///
        /// Ikisi de referansin her karesinde var ve ikisi de bir
        /// LOKANTAYI lokanta yapan seyler: disarida adin, iceride ne
        /// sattigin yaziyor.
        ///
        /// YAZI YOK. Oyunun yazi tipi UI Toolkit'e bagli; dunyada metin
        /// cizmek icin ayri bir paket (TextMeshPro) gerekir ve o paket
        /// bu projede yok. Tabela bunun yerine RENK ve ISIKLA
        /// konusuyor: hizli yemekte neon kirmizi bir cerceve, Turk
        /// mutfaginda bakir bir levha. Menu tahtasi da satirlarini acik
        /// seritlerle gosteriyor - uzaktan bir menu tahtasi tam olarak
        /// boyle okunuyor zaten.
        /// </summary>
        private void Boards(Modeler m, Modeler isik, Palette p,
                            float sol, float sag, float arka, int tables)
        {
            // --- CEPHE TABELASI: kapinin uzerinde, sokaga bakiyor -----
            //
            // Ilk denemede 3,10 m genisliginde ve cerceveSIZDI: ekranda
            // "isikli bir kalas" gibi duruyordu. Tabela bir LEVHA -
            // koyu bir cercevenin icinde duran isikli bir yuz.
            float tabelaX = Paths.DoorX;
            const float tw = 2.30f, th = 0.46f;
            const float tz = -0.055f;
            m.Box(new Vector3(tabelaX, 1.86f, tz),
                  new Vector3(tw, th, 0.10f), p.WallTrim);

            // ISIK LEVHANIN TAMAMI DEGIL, CERCEVESI.
            //
            // Ilk denemede levhanin butun yuzu yaniyordu ve ekranda
            // "isikli sari bir kalas" olarak okunuyordu - tabela degil
            // lamba. Gercek tabelalarda yanan sey YAZI ve cerceve;
            // yaziyi bu boru hattinda cizemiyoruz (dunyada metin icin
            // TextMeshPro gerekir, projede yok), ama NEON CERCEVE ayni
            // isi goruyor: koyu bir levha, kenarinda yanan bir hat.
            const float boru = 0.05f;
            isik.Box(new Vector3(tabelaX, 1.86f + th * 0.5f - boru, tz - 0.06f),
                     new Vector3(tw - 0.10f, boru, 0.04f), p.Sign);
            isik.Box(new Vector3(tabelaX, 1.86f - th * 0.5f + boru, tz - 0.06f),
                     new Vector3(tw - 0.10f, boru, 0.04f), p.Sign);
            for (int i = 0; i < 2; i++)
                isik.Box(new Vector3(tabelaX + (i == 0 ? -1f : 1f) * (tw * 0.5f - 0.05f),
                                     1.86f, tz - 0.06f),
                         new Vector3(boru, th - 0.10f, 0.04f), p.Sign);

            // Ortada kucuk bir amblem: bir daire. Hangi mutfak olursa
            // olsun bir tabelanin ortasinda bir sey olur.
            isik.Prism(10, 0.11f, 0.11f, 0.03f,
                       new Vector3(tabelaX, 1.86f, tz - 0.075f),
                       Quaternion.Euler(-90f, 0f, 0f), p.Sign);

            // Tabelanin ustunde kucuk bir saçak: isigi asagi tutuyor ve
            // levhayi duvara BAGLIYOR - havada duran bir levha degil.
            m.Box(new Vector3(tabelaX, 2.13f, -0.14f),
                  new Vector3(tw + 0.24f, 0.07f, 0.30f), p.Accent);
            for (int i = 0; i < 2; i++)
                m.Box(new Vector3(tabelaX + (i == 0 ? -1f : 1f) * (tw * 0.5f - 0.06f),
                                  2.02f, -0.10f),
                      new Vector3(0.06f, 0.24f, 0.06f), p.Accent);

            // --- MENU TAHTASI: salonun arka duvarinda ----------------
            float menuX = float.NaN;
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!r.Name.StartsWith("Salon")) continue;
                if (!RoomPlan.RoomOpen(in r, tables)) continue;
                if (Mathf.Abs(r.Z0 + r.D - arka) > 0.05f) continue;
                menuX = r.CenterX;
                break;
            }
            if (float.IsNaN(menuX)) return;

            m.Box(new Vector3(menuX, 1.70f, arka - 0.02f),
                  new Vector3(1.70f, 1.05f, 0.06f), p.WallTrim);
            m.Box(new Vector3(menuX, 1.70f, arka - 0.06f),
                  new Vector3(1.52f, 0.90f, 0.03f), new Color(0.09f, 0.10f, 0.11f));

            // Satirlar: bes acik serit. Ustteki uzun (baslik), alttakiler
            // kisa ve saga dogru bir "fiyat" sutunu birakiyor.
            for (int i = 0; i < 5; i++)
            {
                float y = 2.03f - i * 0.16f;
                float w = i == 0 ? 0.90f : 0.72f;
                m.Box(new Vector3(menuX - 0.28f, y, arka - 0.08f),
                      new Vector3(w, i == 0 ? 0.07f : 0.045f, 0.02f),
                      i == 0 ? p.Sign : new Color(0.78f, 0.76f, 0.72f));
                if (i == 0) continue;
                m.Box(new Vector3(menuX + 0.52f, y, arka - 0.08f),
                      new Vector3(0.22f, 0.045f, 0.02f), p.Accent);
            }

            // Duvarda iki cerceve: referansin her karesinde var.
            for (int i = 0; i < 2; i++)
            {
                float x = menuX + (i == 0 ? -1.45f : 1.45f);
                if (x < sol + 0.3f || x > sag - 0.3f) continue;
                m.Box(new Vector3(x, 1.72f, arka - 0.02f),
                      new Vector3(0.52f, 0.68f, 0.05f), p.Wood);
                m.Box(new Vector3(x, 1.72f, arka - 0.06f),
                      new Vector3(0.40f, 0.54f, 0.02f), p.Accent);
            }
        }

        /// <summary>
        /// ZEMIN DESENI: tahta ya da fayans.
        ///
        /// Oda zeminleri tek duz renkti ve 34 derecelik bir bakista o
        /// renk BOS bir alan olarak okunuyordu - referansta zeminin
        /// kendisi mekanin yarisi. Desen ayrica OLCEK veriyor: bir
        /// tahtanin genisligini bilen goz, odanin ne kadar buyuk
        /// oldugunu da anliyor.
        ///
        /// Turk: uzun ahsap tahtalar, sokak yonunde.
        /// Hizli yemek: kare fayans, iki tonda dama.
        ///
        /// Hepsi TEK ORGUYE giriyor (renge gore), yani desen bedava:
        /// yuzlerce serit iki cizim.
        /// </summary>
        private void FloorPattern(Modeler m, Palette p, int tables)
        {
            // DESEN ZEMININ USTUNDE, USTUNE YAPISIK DEGIL.
            //
            // Ilk olcude desen levhalarinin ALT yuzu zemin levhasinin
            // UST yuzuyle ayni duzlemdeydi (ikisi de y=0) ve sonuc
            // z-kavgasiydi: yakin planda zeminde duzensiz koyu lekeler
            // beliriyordu. Uzaktan "golge kirintisi" gibi okunuyor ve
            // golge ayarlarinda aranmasina yol aciyordu.
            //
            // Artik 6 mm yukarida basliyor; kamera 26-35 m'den bakiyor,
            // yani fark goruunmuyor ama kavga bitiyor.
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                float w = r.W - 0.10f, d = r.D - 0.10f;

                if (p.Planks)
                {
                    // Tahta: 0,42 m eninde, oda boyunca uzun.
                    const float tahta = 0.42f;
                    int n = Mathf.Max(1, Mathf.RoundToInt(d / tahta));
                    float dz = d / n;
                    for (int k = 0; k < n; k++)
                    {
                        float z = r.Z0 + 0.05f + dz * (k + 0.5f);
                        m.Box(new Vector3(r.CenterX, 0.016f, z),
                              new Vector3(w, 0.020f, dz - 0.035f),
                              (k % 2 == 0) ? p.Floor : p.FloorAlt);
                    }
                    continue;
                }

                // Fayans: 0,62 m kare, dama.
                const float kare = 0.62f;
                int nx = Mathf.Max(1, Mathf.RoundToInt(w / kare));
                int nz = Mathf.Max(1, Mathf.RoundToInt(d / kare));
                float sx = w / nx, sz = d / nz;
                for (int a = 0; a < nx; a++)
                    for (int b = 0; b < nz; b++)
                        m.Box(new Vector3(r.X0 + 0.05f + sx * (a + 0.5f), 0.016f,
                                          r.Z0 + 0.05f + sz * (b + 0.5f)),
                              new Vector3(sx - 0.03f, 0.020f, sz - 0.03f),
                              ((a + b) % 2 == 0) ? p.Floor : p.FloorAlt);
            }
        }

        /// <summary>
        /// SERVIS BANKOSU: mutfagin onunde, sicak tesirhir tezgahi.
        ///
        /// Referansin dort karesinde de var ve mutfagi "mutfak" yapan
        /// parca o: uzun bir tezgah, uzerinde sirali yemek kaplari ve
        /// ustunde cam siper. Oyunda mutfak ocaklardan ve dolaplardan
        /// ibaretti - yani bir MUTFAK gibi degil bir DEPO gibi
        /// duruyordu.
        ///
        /// Kaplarin rengi degil SAYISI bilgi tasimiyor: bu bir susleme,
        /// simulasyona bagli degil. Bagli olsaydi her karede yeniden
        /// kurulmasi gerekirdi.
        /// </summary>
        private void ServiceCounter(Modeler m, Modeler isik, Palette p, int tables)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (r.Name != "Mutfak") continue;
                if (!RoomPlan.RoomOpen(in r, tables)) return;

                // Odanin ON kenarinda, koridoru kapatmadan (Paths.LaneZ).
                float z = r.Z0 + 0.62f;
                float x = r.CenterX;
                float w = r.W - 1.5f;

                Color celik = new Color(0.588f, 0.624f, 0.671f);

                // Govde ve tezgah tablasi.
                m.Box(new Vector3(x, 0.42f, z), new Vector3(w, 0.84f, 0.62f), p.Wood);
                m.Box(new Vector3(x, 0.86f, z), new Vector3(w + 0.08f, 0.06f, 0.70f),
                      celik);

                // Sicak kaplar: tezgahin uzerinde sirali.
                int kap = Mathf.Max(3, Mathf.RoundToInt(w / 0.55f));
                float dx = w / kap;
                for (int k = 0; k < kap; k++)
                {
                    float kx = x - w * 0.5f + dx * (k + 0.5f);
                    m.Box(new Vector3(kx, 0.91f, z), new Vector3(dx - 0.07f, 0.05f, 0.44f),
                          celik);
                    // Icindeki yemek: kimligin vurgu renginde. Yemekleri
                    // tek tek modellemek gerekmiyor - uzaktan bir tesirhir
                    // tezgahi zaten renkli bir sira olarak okunuyor.
                    m.Box(new Vector3(kx, 0.945f, z),
                          new Vector3(dx - 0.13f, 0.03f, 0.36f),
                          k % 2 == 0 ? p.Accent : p.Sign);
                }

                // Cam siper: iki ince ayak ve isikli bir levha. Saydam
                // malzeme YAPIDA opak cizilebiliyor (docs/37), o yuzden
                // cam yerine SOLUK ISIKLI bir levha - ayni siluet,
                // sifir risk.
                for (int k = 0; k < 2; k++)
                    m.Box(new Vector3(x + (k == 0 ? -1f : 1f) * (w * 0.5f - 0.05f),
                                      1.12f, z - 0.24f),
                          new Vector3(0.05f, 0.46f, 0.05f), celik);
                m.Box(new Vector3(x, 1.34f, z - 0.10f),
                      new Vector3(w, 0.05f, 0.34f), celik);
                return;
            }
        }

        /// <summary>
        /// DUVAR RAFLARI: mutfakta ve depoda.
        ///
        /// Referansta mutfagin arkasi doluydu - tabaklar, kavanozlar,
        /// baharat. Bizim arka duvarimiz bombostu. Raflar ayni isi
        /// goruyor ve mekanin "calisilan bir yer" oldugunu soyluyor.
        /// </summary>
        private void Shelves(Modeler m, Palette p, int tables)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (r.Name != "Mutfak" && r.Name != "Depo") continue;
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                float z = r.Z0 + r.D - 0.16f;
                float w = Mathf.Min(r.W - 1.2f, 3.4f);
                if (w < 0.8f) continue;

                for (int k = 0; k < 2; k++)
                {
                    float y = 1.16f + k * 0.44f;
                    m.Box(new Vector3(r.CenterX, y, z),
                          new Vector3(w, 0.05f, 0.30f), p.Wood);

                    // Uzerindeki kaplar: iki boy, degisen araliklarla.
                    int kap = Mathf.Max(3, Mathf.RoundToInt(w / 0.42f));
                    float dx = w / kap;
                    for (int j = 0; j < kap; j++)
                    {
                        if ((j + k) % 3 == 0) continue;      // bosluk da desen
                        float h = (j % 2 == 0) ? 0.20f : 0.14f;
                        m.Box(new Vector3(r.CenterX - w * 0.5f + dx * (j + 0.5f),
                                          y + 0.03f + h * 0.5f, z),
                              new Vector3(dx * 0.55f, h, 0.20f),
                              (j % 2 == 0) ? p.Metal : p.WallTrim);
                    }
                }
            }
        }

        /// <summary>
        /// SEDIR: salonun arka duvari boyunca oturma.
        ///
        /// Referansin dordunde de duvar boyunca banklar var ve
        /// masalarin yarisi onlarin onunde. Oyunun masalari dort
        /// sandalyeli ve oyle kalıyor (oturma noktalari simulasyonun
        /// isi); sedir yalnizca duvarin dibini dolduruyor - bos bir
        /// duvar dibi, mekanin en cok "kurulmamis" duran yeri.
        /// </summary>
        private void Booths(Modeler m, Palette p, int tables)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!r.Name.StartsWith("Salon")) continue;
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                // Yalnizca ARKA duvari plot sinirinda olan salonlarda:
                // ic duvarlar gecis, sedir gecisi kapatmamali.
                if (r.Z0 + r.D < RoomPlan.PlotD - 0.05f) continue;

                float z = r.Z0 + r.D - 0.34f;
                float w = r.W - 1.0f;
                if (w < 1.0f) continue;

                m.Box(new Vector3(r.CenterX, 0.22f, z),
                      new Vector3(w, 0.10f, 0.52f), p.Accent);       // oturak
                m.Box(new Vector3(r.CenterX, 0.11f, z),
                      new Vector3(w - 0.2f, 0.22f, 0.42f), p.WallTrim); // kaide
                m.Box(new Vector3(r.CenterX, 0.44f, z + 0.22f),
                      new Vector3(w, 0.44f, 0.10f), p.Accent);       // sirtlik
            }
        }

        /// <summary>
        /// TERAS KORKULUGU: cephe ile kaldirim arasinda.
        ///
        /// Referansin dordunde de bina ile sokak arasinda alcak bir
        /// sinir var - saksi, korkuluk ya da cit. Isi belli: "burasi
        /// lokantanin onu". Bizde bina dogrudan kaldirima aciliyordu.
        /// </summary>
        private void Terrace(Modeler m, Palette p, float sol, float sag)
        {
            const float z = -0.42f;
            const float y = 0.36f;

            // Ust ray boyunca; kapinin onu ACIK kaliyor.
            float bosluk0 = Paths.DoorX - 0.95f, bosluk1 = Paths.DoorX + 0.95f;
            Dikme(m, p, sol + 0.05f, z);
            Dikme(m, p, sag - 0.05f, z);

            Ray(m, p, sol + 0.05f, bosluk0, z, y);
            Ray(m, p, bosluk1, sag - 0.05f, z, y);

            // Kapinin iki yanindaki dikmeler: bosluk "kirik" degil
            // "gecit" olarak okunmali.
            Dikme(m, p, bosluk0, z);
            Dikme(m, p, bosluk1, z);
        }

        private void Dikme(Modeler m, Palette p, float x, float z)
        {
            m.Box(new Vector3(x, 0.20f, z), new Vector3(0.07f, 0.40f, 0.07f),
                  p.WallTrim);
        }

        private void Ray(Modeler m, Palette p, float x0, float x1, float z, float y)
        {
            float w = x1 - x0;
            if (w < 0.3f) return;
            float orta = (x0 + x1) * 0.5f;

            m.Box(new Vector3(orta, y, z), new Vector3(w, 0.06f, 0.07f), p.Metal);
            m.Box(new Vector3(orta, y - 0.16f, z), new Vector3(w, 0.04f, 0.05f),
                  p.WallTrim);

            // Aradaki dikmeler: 1,6 m'de bir.
            int n = Mathf.Max(1, Mathf.RoundToInt(w / 1.6f));
            for (int i = 1; i < n; i++)
                Dikme(m, p, x0 + w * i / n, z);
        }

        /// <summary>
        /// CEPHE: VITRIN KASASI.
        ///
        /// Referansin dordunde de bina sokaga BUYUK CAMLARLA bakiyor ve
        /// camlari bolen dikmeler cephenin butun ritmini kuruyor.
        /// Bizim cephemiz saydam bir levhaydi - camdan cok "duvar
        /// yokmus" gibi okunuyordu.
        ///
        /// Cam eklemiyoruz (saydam malzeme yapida opak cizilebiliyor,
        /// docs/37); eklenen sey KASA: alt bordur, ust kiris ve
        /// aralarindaki dikmeler. Cam, aradaki bosluk.
        /// </summary>
        private void Storefront(Modeler m, Palette p, float sol, float sag)
        {
            const float z = -0.02f;

            // KIRIS ALCAK, YOKSA SALONU KAPATIYOR.
            //
            // Ilk denemede ust kiris 2,05 m'deydi ve goruntude salonun
            // on sirasinin onunden gecen KOYU BIR BANT olarak cikti -
            // oyuncunun masalari gordugu yeri kapatiyordu. Oda
            // duvarlari zaten 1,15 m (kamera 34 dereceden icini
            // gorsun diye); cephe de o hattin uzerine cikmamali.
            const float ust = 1.34f;

            // Ust kiris ve alt bordur.
            float w = sag - sol;
            m.Box(new Vector3((sol + sag) * 0.5f, ust, z),
                  new Vector3(w, 0.10f, 0.15f), p.WallTrim);
            m.Box(new Vector3((sol + sag) * 0.5f, 0.22f, z),
                  new Vector3(w, 0.44f, 0.14f), p.Wall);
            m.Box(new Vector3((sol + sag) * 0.5f, 0.46f, z),
                  new Vector3(w, 0.07f, 0.17f), p.WallTrim);

            // Dikmeler: 1,9 m'de bir, kapinin onu bos.
            int n = Mathf.Max(2, Mathf.RoundToInt(w / 1.9f));
            for (int i = 0; i <= n; i++)
            {
                float x = sol + w * i / n;
                if (Mathf.Abs(x - Paths.DoorX) < 0.85f) continue;
                m.Box(new Vector3(x, 0.90f, z), new Vector3(0.09f, 0.90f, 0.12f),
                      p.WallTrim);
            }

            // Kapinin iki yani: daha kalin dikme - GIRIS burasi.
            for (int i = 0; i < 2; i++)
                m.Box(new Vector3(Paths.DoorX + (i == 0 ? -1f : 1f) * 0.78f,
                                  0.90f, z),
                      new Vector3(0.13f, 0.90f, 0.14f), p.Accent);
        }

        /// <summary>
        /// TERAS OTURMASI: kaldirimda iki kucuk masa.
        ///
        /// Referansin birinci ve ucuncu karesinde disarida oturan
        /// musteriler var. Bizde SIMULASYON disarida servis yapmiyor -
        /// bu yuzden buradaki masalar BOS ve oyle kalacak: susleme,
        /// oyun durumu degil. Bos bir teras masasi yine de "burasi bir
        /// lokanta" diyor; dolusu yalan soylerdi.
        /// </summary>
        private void PatioSeats(Modeler m, Palette p, float sol, float sag)
        {
            _patio.Clear();
            float[] yerler = { Paths.DoorX - 2.6f, Paths.DoorX + 2.6f };
            foreach (float x in yerler)
            {
                if (x < sol + 0.6f || x > sag - 0.6f) continue;
                PatioSet(m, p, x, -0.95f);
                // Yayalarin dolanmasi icin: masa da direk gibi engel.
                _patio.Add(new Vector3(x, 0f, -0.95f));
            }
        }

        /// <summary>Teras masalarinin yeri. Yaya dolasimi icin engel.</summary>
        private readonly System.Collections.Generic.List<Vector3> _patio =
            new System.Collections.Generic.List<Vector3>();

        private void PatioSet(Modeler m, Palette p, float x, float z)
        {
            // Masa: yuvarlak tabla, tek ayak, tabanlik.
            m.Prism(10, 0.36f, 0.36f, 0.05f, new Vector3(x, 0.62f, z),
                    Quaternion.identity, p.Wood);
            m.Prism(6, 0.05f, 0.05f, 0.62f, new Vector3(x, 0f, z),
                    Quaternion.identity, p.WallTrim);
            m.Prism(8, 0.20f, 0.16f, 0.04f, new Vector3(x, 0f, z),
                    Quaternion.identity, p.WallTrim);

            // Iki sandalye: karsilikli, masaya donuk.
            PatioChair(m, p, x - 0.62f, z, 90f);
            PatioChair(m, p, x + 0.62f, z, -90f);
        }

        private void PatioChair(Modeler m, Palette p, float x, float z, float aci)
        {
            Quaternion r = Quaternion.Euler(0f, aci, 0f);
            Vector3 c = new Vector3(x, 0f, z);

            // Oturak ve sirtlik, sandalyenin kendi ekseninde.
            m.BoxAt(c + r * new Vector3(0f, 0.40f, 0f),
                    new Vector3(0.36f, 0.05f, 0.36f), r, p.Accent);
            m.BoxAt(c + r * new Vector3(0f, 0.60f, -0.16f),
                    new Vector3(0.36f, 0.36f, 0.05f), r, p.WallTrim);
            for (int i = 0; i < 4; i++)
            {
                float ax = (i % 2 == 0) ? -0.14f : 0.14f;
                float az = (i < 2) ? -0.14f : 0.14f;
                m.BoxAt(c + r * new Vector3(ax, 0.19f, az),
                        new Vector3(0.045f, 0.38f, 0.045f), r, p.WallTrim);
            }
        }

        /// <summary>
        /// HALI: yalnizca Turk mutfaginda.
        ///
        /// Referansin ikinci karesinde masalarin altinda kilim var ve o
        /// tek parca, mekani "kebapci" yapan seylerin basinda geliyor.
        /// Hizli yemek tarafinda hali YOK - orasi silinebilir bir zemin
        /// ister; kimlik farki tam olarak bu.
        /// </summary>
        private void Rugs(Modeler m, Palette p, int tables)
        {
            if (!p.HasRug) return;

            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!r.Name.StartsWith("Salon")) continue;
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                float w = r.W - 0.9f, d = r.D - 0.9f;
                if (w < 0.5f || d < 0.5f) continue;

                // Zeminin 2 cm ustunde: z-kavgasi olmasin.
                m.Box(new Vector3(r.CenterX, 0.02f, r.Z0 + r.D * 0.5f),
                      new Vector3(w, 0.02f, d), p.Rug);
                // Ic bordur: tek renk bir dikdortgen "hali" degil
                // "boyanmis zemin" diye okunuyordu.
                m.Box(new Vector3(r.CenterX, 0.025f, r.Z0 + r.D * 0.5f),
                      new Vector3(w - 0.34f, 0.02f, d - 0.34f), p.Accent);
                m.Box(new Vector3(r.CenterX, 0.03f, r.Z0 + r.D * 0.5f),
                      new Vector3(w - 0.52f, 0.02f, d - 0.52f), p.Rug);
            }
        }
    }
}
