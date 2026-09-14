using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// ASCININ YEMEK YAPMA SIRASI: malzemeyi al, yika, dogra, pisir,
    /// tabaga cek.
    ///
    /// NEDEN BIR SIRA, NEDEN HER KAREDE SIMULASYONA BAKMIYOR:
    ///
    /// Olculdu (Autopilot, "simulasyon is verdi 145 kez"): cekirdek
    /// ascinin isini karelerin ancak %10'unda acik tutuyor - bir is
    /// birkac yuz milisaniye suruyor. Gorunum bunu dogrudan izleyince
    /// asci buzdolabina dogru iki adim atip geri donuyordu; hicbir
    /// istasyona VARAMIYOR, dolayisiyla hicbir calisma duruşuna
    /// giremiyordu. Mutfak bombostu ve sebebi buydu.
    ///
    /// Cozum: cekirdek bir is verdiginde gorunum BIR SIRA BASLATIYOR ve
    /// o sirayi sonuna kadar oynatiyor. Ekonomi degismiyor - burasi
    /// cekirdege hicbir sey yazmiyor, yalnizca okudugunu insan hizinda
    /// anlatiyor.
    ///
    /// Bu ayni zamanda "asamalari gorelim" isteginin karsiligi: her
    /// asamanin kendi yeri, kendi duruşu ve kendi nesnesi var.
    /// </summary>
    public sealed class CookRoutine : MonoBehaviour
    {
        public enum Stage
        {
            Bosta, TabagaGit, TabagiAl, DolabaGit, Al, TezgahaGit, Yika, Dogra,
            OcagaGit, Pisir, Tabakla, PasaGit
        }

        private Walker _walk;
        private Figure _fig;
        private int _post;          // ascinin sirasi (tezgah dagitimi)
        private int _posts = 3;

        private Stage _stage = Stage.Bosta;
        private float _left;
        private int _station = -1;
        private bool _issued;       // bu asamanin yurume emri verildi mi

        private GameObject _held;   // elindeki malzeme/tabak
        private GameObject _pan;    // ocaktaki tava
        private Transform _stove;   // tavanin konacagi ocak

        private GameObject[] _ingredients;
        private GameObject _platePrefab;

        /// <summary>Temiz tabak yiginin onu. Gorunumden geliyor.</summary>
        private Vector3 _plateSpot;

        /// <summary>Su an bir sira isliyor mu.</summary>
        public bool Busy { get { return _stage != Stage.Bosta; } }

        /// <summary>Su anki asama. Turun sorabilmesi icin.</summary>
        public Stage Current { get { return _stage; } }

        // =====================================================================
        public void Init(Walker walk, Figure fig, int post, int posts,
                         GameObject[] ingredients, GameObject platePrefab,
                         Vector3 plateSpot)
        {
            _walk = walk;
            _fig = fig;
            _post = post;
            _posts = Mathf.Max(1, posts);
            _ingredients = ingredients;
            _platePrefab = platePrefab;
            _plateSpot = plateSpot;
        }

        /// <summary>
        /// Yeni is. Sira zaten isliyorsa DOKUNULMUYOR: yarim kalmis bir
        /// pisirme, yeni bir siparis geldi diye bastan baslamamali.
        /// </summary>
        public void Begin(int station, Transform stove)
        {
            if (_stage != Stage.Bosta) return;
            _station = station;
            _stove = stove;
            Go(Stage.TabagaGit);
        }

        /// <summary>Sirayi keser ve elindekini birakir. Gun bitince.</summary>
        public void Cancel()
        {
            _stage = Stage.Bosta;
            // YURUYUSU DE KES: yarim kalmis bir yol, figuru sirasi
            // iptal edilmis halde yurumeye devam ettiriyordu.
            if (_walk != null) _walk.Stop();
            Drop();
            Pan(false);
        }

        // =====================================================================
        private void Update()
        {
            if (_stage == Stage.Bosta) return;
            if (_walk == null || _fig == null) { _stage = Stage.Bosta; return; }

            // DURAKLATINCA MUTFAK DA DURUYOR.
            //
            // Asagidaki Mathf.Max(0.25f, ...) tabani sifiri YUTUYORDU:
            // duraklatilmis bir dunyada asci ceyrek hizda dogramaya
            // devam ediyordu. Duraklatma ekrani oyunun buyuk bir
            // kismini kapliyor (sabah, aksam, ayarlar), yani bu surekli
            // ekrandaydi.
            if (Walker.GameSpeed <= 0.001f) return;

            // Calisan figurun Animator'u acik kalmali: Figure gecisten
            // ~1 sn sonra kapatiyor ve dograma klibi ilk karesinde
            // donuyor.
            _fig.HoldAwake();

            _stageAge += Time.deltaTime;
            if (_stageAge > StageTimeout)
            {
                // Sira takildi: birak, temizle, bosta kal. Bir sonraki
                // is yeniden baslatir. Sessizce kilitli kalmaktan iyi.
                Debug.LogWarning("Asci sirasi zaman asimina ugradi: " + _stage);
                Cancel();
                Pose(Figure.Pose.Idle);
                return;
            }

            switch (_stage)
            {
                case Stage.TabagaGit:
                    // TEMIZ TABAK ONCE ALINIYOR.
                    //
                    // Kullanicinin cumlesi: "asci oradan tabagi alip
                    // yemek koysun". Sirada EN BASA konuyor, pisirmenin
                    // ortasina degil: sicak tavayi birakip odadan cikan
                    // bir asci, "tabagi aliyor" degil "bir yere gitti"
                    // diye okunur. Gercek bir mutfakta da tabak onceden
                    // hazirlanir.
                    //
                    // Bu yol Mutfak-Bulasik gecidinden geciyor ve o
                    // gecit kil payi var: ortak kenar 1,40 m, esik de tam
                    // 1,40 (bkz. RestaurantView.MinJamb). Kat plani
                    // degisirse asci buraya Giris'ten dolasmaya baslar.
                    if (Walk(_plateSpot, _plateSpot + new Vector3(0f, 0f, 1f)))
                        Go(Stage.TabagiAl);
                    break;

                case Stage.TabagiAl:
                    if (Enter()) Pose(Figure.Pose.Pick);
                    if (Tick(0.8f)) Go(Stage.DolabaGit);
                    break;

                case Stage.DolabaGit:
                    // Buzdolabi sag duvarda ve odaya (-X) bakiyor
                    // (RestaurantView.BuildRoomProps); asci ona donuk
                    // durmali. Bakis hedefi +Z yazilmisti, yani asci
                    // dolaba 90 derece YAN dururken "alma" oynatiyordu.
                    if (Walk(Paths.Fridge, Paths.FridgeFace)) Go(Stage.Al);
                    break;

                case Stage.Al:
                    if (Enter()) { Pose(Figure.Pose.Pick); Hold(Ingredient()); }
                    if (Tick(1.0f)) Go(Stage.TezgahaGit);
                    break;

                case Stage.TezgahaGit:
                    if (Walk(Paths.PrepPost(_post, _posts),
                             Paths.PrepCounter(_post, _posts))) Go(Stage.Yika);
                    break;

                case Stage.Yika:
                    if (Enter()) Pose(Figure.Pose.Wash);
                    if (Tick(1.5f)) Go(Stage.Dogra);
                    break;

                case Stage.Dogra:
                    if (Enter()) Pose(Figure.Pose.Chop);
                    if (Tick(1.9f)) Go(Stage.OcagaGit);
                    break;

                case Stage.OcagaGit:
                    if (Walk(Paths.KitchenPost(_station, _posts), StovePos()))
                    {
                        Drop();
                        Pan(true);
                        Go(Stage.Pisir);
                    }
                    break;

                case Stage.Pisir:
                    if (Enter()) Pose(Figure.Pose.Serve);
                    if (Tick(2.8f)) Go(Stage.Tabakla);
                    break;

                case Stage.Tabakla:
                    if (Enter()) { Pose(Figure.Pose.Pick); }
                    if (Tick(1.0f))
                    {
                        Pan(false);
                        Hold(_platePrefab);
                        Go(Stage.PasaGit);
                    }
                    break;

                case Stage.PasaGit:
                    if (Walk(Paths.PrepPost(_post, _posts),
                             Paths.PrepCounter(_post, _posts)))
                    {
                        Drop();
                        Pose(Figure.Pose.Idle);
                        _stage = Stage.Bosta;
                    }
                    break;
            }
        }

        // =====================================================================
        private void Go(Stage s)
        {
            _stage = s;
            _left = -1f;
            _issued = false;
            _stageAge = 0f;
        }

        /// <summary>Bu asamaya YENI mi girildi. Duruş bir kez veriliyor.</summary>
        private bool Enter()
        {
            if (_issued) return false;
            _issued = true;
            return true;
        }

        private bool Tick(float seconds)
        {
            if (_left < 0f) _left = seconds;
            _left -= Time.deltaTime * Mathf.Max(0.25f, Walker.GameSpeed);
            return _left <= 0f;
        }

        /// <summary>
        /// Hedefe yurur; varinca true. Emir BIR KEZ veriliyor, yoksa
        /// figur her karede bastan baslar ve hic varmaz.
        /// </summary>
        /// <summary>
        /// Asamanin BAKILAN hedefi. Denetim buna gore olcuyor.
        ///
        /// Once "calisan asci ocaga bakar" varsayiliyordu ve asamalar
        /// eklenince o varsayim yanlis oldu: yikarken ve dograrken
        /// tezgaha bakiyor, ocaga degil. Denetim 176 derece sapma
        /// olctu ve HAKLIYDI - yanlis hedefe bakiyordu.
        /// </summary>
        public Vector3 LookTarget { get; private set; }

        private bool Walk(Vector3 target, Vector3 lookAt)
        {
            if (!_issued)
            {
                _issued = true;
                LookTarget = lookAt;
                _target = target;
                _stageAge = 0f;
                _path.Clear();
                Paths.Between(_path, transform.localPosition, target);
                _walk.GoTo(_path, Paths.FaceFrom(target, lookAt), null);
                Pose(Figure.Pose.Walk);
                return false;
            }

            // VARIS MESAFEYLE OLCULUYOR, BAYRAKLA DEGIL.
            //
            // Once yalnizca !Moving'e bakiliyordu ve Warp (kadro yeniden
            // dizilince) yolu temizleyip Moving'i false yapiyor - yani
            // EVINE isinlanan asci "ocaga vardim" diyor, tava bos ocaga
            // konuyor ve pisirme duruşu evde oynuyordu.
            if (_walk.Moving) return false;
            return (transform.localPosition - _target).sqrMagnitude < 0.16f;
        }

        /// <summary>
        /// Bu asamada gecen sure. Zaman asimi icin: hic varamayan bir
        /// yuruyus sirayi sonsuza kadar kilitlerdi ve asci o ana kadar
        /// ne yapiyorsa oyle donup kalirdi.
        /// </summary>
        private float _stageAge;
        private Vector3 _target;

        /// <summary>Bir asamanin en fazla suresi (sn).</summary>
        private const float StageTimeout = 18f;

        private static readonly System.Collections.Generic.List<Vector3> _path =
            new System.Collections.Generic.List<Vector3>();

        private void Pose(Figure.Pose p) { if (_fig != null) _fig.Set(p); }

        private Vector3 StovePos()
        {
            if (_stove != null) return _stove.localPosition;
            return Paths.KitchenPost(_station, _posts) + new Vector3(0f, 0f, 1f);
        }

        private GameObject Ingredient()
        {
            if (_ingredients == null || _ingredients.Length == 0) return null;
            int i = Mathf.Abs(_station + _post * 7) % _ingredients.Length;
            return _ingredients[i];
        }

        /// <summary>Eline bir sey verir. Oncekini siler.</summary>
        private void Hold(GameObject prefab)
        {
            Drop();
            if (prefab == null) return;
            _held = Instantiate(prefab, transform);
            _held.name = "Elinde";
            _held.transform.localPosition = new Vector3(0f, 0.60f, 0.26f);
            _held.transform.localRotation = Quaternion.identity;
        }

        private void Drop()
        {
            if (_held == null) return;
            if (Application.isPlaying) Destroy(_held); else DestroyImmediate(_held);
            _held = null;
        }

        /// <summary>
        /// OCAGA TAVA. Paket tava modeli tasimiyor; iki kutudan
        /// yapiliyor - govde ve sap. Ustunde de pisen sey: kucuk,
        /// sicak renkli bir kutu.
        /// </summary>
        private void Pan(bool on)
        {
            // TAVA BIR KEZ KURULUYOR, SONRA ACILIP KAPANIYOR.
            //
            // Once her pisirmede `new GameObject` + uc `CreatePrimitive`
            // ile kuruluyor, `Tabakla` asamasinda yok ediliyordu.
            // Malzeme sizintisi bu dosyada zaten duzeltilmisti ama NESNE
            // copu duruyordu: uc asci x dakikada birkac tabak = duzenli
            // GC baskisi. Projenin geri kalani (tabak yigini, masa
            // tabagi, tepsi, musteri figuru) hep havuz kullaniyor ve
            // gerekcesini yaziyor - "her karede nesne yaratip yok etmek
            // zirvede saniyede onlarca ayirma demek".
            if (!on)
            {
                if (_pan != null && _pan.activeSelf) _pan.SetActive(false);
                return;
            }
            if (_stove == null) return;

            if (_pan == null)
            {
                if (PanMaterial == null) return;
                _pan = new GameObject("Tava");
                Box(_pan.transform, new Vector3(0f, 0.02f, 0f),
                    new Vector3(0.26f, 0.04f, 0.26f), new Color(0.16f, 0.16f, 0.18f));
                Box(_pan.transform, new Vector3(0f, 0.02f, -0.22f),
                    new Vector3(0.05f, 0.03f, 0.18f), new Color(0.14f, 0.14f, 0.15f));
                Box(_pan.transform, new Vector3(0f, 0.05f, 0f),
                    new Vector3(0.18f, 0.03f, 0.18f), new Color(0.85f, 0.50f, 0.22f));
            }

            // OCAK HER ISTE DEGISEBILIR (Begin yeni bir ocak veriyor),
            // o yuzden havuzdan cikan tava her seferinde YENIDEN
            // konumlaniyor. Bu satirlar olmadan tava ilk ocakta kalir
            // ve asci baska ocakta bos tencere karistirirdi.
            _pan.transform.SetParent(_stove.parent, false);
            Vector3 p = _stove.localPosition;
            _pan.transform.localPosition = new Vector3(p.x, StoveTop, p.z - 0.05f);
            if (!_pan.activeSelf) _pan.SetActive(true);
        }

        /// <summary>Ocak tablasinin yuksekligi (m). ArtPrefabs hedefi 0,92.</summary>
        private const float StoveTop = 0.94f;

        /// <summary>
        /// Tavanin bir parcasi.
        ///
        /// MALZEME PAYLASILIYOR, HER KUTUDA YENI URETILMIYOR.
        ///
        /// Ilk yazim her kutu icin `Shader.Find` + `new Material`
        /// yapiyordu: tava basina uc malzeme, ve `Pan(false)` yalnizca
        /// nesneyi yok ettigi icin malzemeler yetim kaliyordu. Bu, asci
        /// basina HER TABAKTA uc sizinti demek - altmis gunluk bir
        /// kampanyada binlerce malzeme ornegi. Dusuk seviye bir
        /// Adreno'da bu yavas bir bellek tukenmesi.
        ///
        /// Renk MaterialPropertyBlock ile veriliyor; projenin geri
        /// kalani zaten boyle yapiyor (Appliance.Paint).
        /// </summary>
        private static void Box(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = PanMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if (_paint == null) _paint = new MaterialPropertyBlock();
            r.GetPropertyBlock(_paint);
            _paint.SetColor(BaseColorId, c);
            r.SetPropertyBlock(_paint);
        }

        private static MaterialPropertyBlock _paint;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Material _panMat;

        /// <summary>
        /// Tavanin paylasilan malzemesi. Bir kez kuruluyor.
        ///
        /// Gorunum katmaninin kendi malzemesini kurdugu son yer;
        /// saydam degil, yani golgelendirici varyanti budanmiyor
        /// (saydam malzemeler varlik olmak zorunda - bkz. docs/36).
        /// </summary>
        private static Material PanMaterial
        {
            get
            {
                if (_panMat != null) return _panMat;
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null)
                {
                    // SESSIZ GERI DONUS YOK.
                    //
                    // Once `return null` deniyordu ve Box() malzemeyi
                    // null yaziyordu: cihazda tava VARSAYILAN (magenta)
                    // ciziliyor, editorde asla gorulmuyor.
                    // RestaurantView.Awake ayni durumda LogError basip
                    // kendini kapatiyor ve gerekcesini yaziyor:
                    // "sessiz geri donus, bir sonraki sefer kimsenin
                    // fark etmeyecegi sey".
                    Debug.LogError("SORUNLAR: URP/Lit golgelendiricisi bulunamadi, "
                                   + "tava cizilemiyor");
                    return null;
                }
                _panMat = new Material(sh) { name = "TavaPaylasilan" };
                return _panMat;
            }
        }
    }
}
