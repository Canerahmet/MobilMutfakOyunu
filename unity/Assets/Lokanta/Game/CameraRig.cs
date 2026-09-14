using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Iki kademeli kamera. docs/31-oda-ve-kamera.md ve kullanicinin
    /// kendi cumlesi: "tum her sey ayni anda goruldugu durumda oyuncu
    /// dokunarak kamerayi o moduler kisma yaklastirmis olur."
    ///
    ///   GENEL   butun arsa gorunuyor. Dokunma hedefi ODA.
    ///   ODA     tek odaya yaklasilmis. Dokunma hedefi MASA TAKIMI.
    ///
    /// Aci, gorus acisi ve mesafe CameraFit'ten geliyor - dokunma hedefi
    /// olcumunun kullandigi hesabin ta kendisi. Burada ayri sayi yazmak
    /// olcumu gecersiz kilar; ilk yazimda oyle oldu ve restoran karenin
    /// yalnizca %38'ini kapliyordu.
    ///
    /// Genel gorunum ACIK odalari cerceveliyor, butun arsayi degil -
    /// acilmamis odalar cizilmiyor bile (RestaurantView.BuildFloors).
    ///
    /// Dokunma hedefi bundan ZARAR GORMUYOR, tersine: olcum (docs/31'in
    /// araci, Editor/RoomLayout.cs) en kucuk acik odanin arayuz
    /// cubuklariyla birlikte 71 dp oldugunu ve bunun kademeyle
    /// DEGISMEDIGINI soyluyor. Eski ayarda ayni sayi 52'den 48'e
    /// duserek Google'in asgarisine tam tamina degiyordu.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        public float MoveSeconds = 0.35f;

        /// <summary>
        /// Kayan gecis suruyor mu.
        ///
        /// Turun sormasi icin: gecisin bittigini SURE ile beklemek
        /// (MoveSeconds + 0,1) bir kosuda yetmedi ve "kamera genel
        /// cerceveye geri geldi" kontrolu 13 m sapmayla kirmiziya
        /// dustu - kamera yolun ortasindayken olculmustu. Sureyi
        /// beklemek yerine DURUMU beklemek gerekiyor.
        /// </summary>
        public bool Moving { get { return _t < 1f; } }

        /// <summary>-1 ise genel gorunum.</summary>
        public int FocusRoom { get; private set; } = -1;

        private Vector3 _from, _to;
        private Quaternion _fromRot = Quaternion.identity;
        private float _t = 1f;
        private Camera _cam;
        private float _aspect;

        /// <summary>
        /// Arayuzun ust ve altta kapladigi oran. Oyun ekrani kendi
        /// cubuklarini olcup buraya bildiriyor - kamera sabit bir sayi
        /// varsaymiyor, cunku cubuklarin yuksekligi icerige gore
        /// degisiyor (kalan hak satiri, mudahale dugmeleri).
        /// </summary>
        private float _top01, _bottom01;

        public void SetSafeArea(float top01, float bottom01)
        {
            if (Mathf.Abs(top01 - _top01) < 0.002f
                && Mathf.Abs(bottom01 - _bottom01) < 0.002f) return;

            _top01 = top01;
            _bottom01 = bottom01;
            Begin();
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();

            // Aci, gorus acisi ve mesafe OLCUMLE AYNI kaynaktan geliyor
            // (CameraFit). Burada ayri sayilar yazmak, dokunma hedefi
            // olcumunu gecersiz kilar - ilk yazimda tam bu oldu ve
            // restoran karenin %38'ini kapliyordu.
            _cam.fieldOfView = CameraFit.FieldOfView;
            transform.rotation = LookRotation;

            _aspect = _cam.aspect;
            transform.position = TargetPosition(-1);
            _from = _to = transform.position;
        }

        private void Update()
        {
            // Ekran donerse cerceve yeniden hesaplaniyor. Sabit bir konum
            // yazsaydik, yatay-dikey gecisinde arsa cerceveden tasardi.
            if (!Mathf.Approximately(_aspect, _cam.aspect))
            {
                _aspect = _cam.aspect;
                Begin();
            }

            if (_t < 1f)
            {
                _t += Time.deltaTime / Mathf.Max(0.01f, MoveSeconds);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_t));
                transform.position = Vector3.Lerp(_from, _to, k);
                transform.rotation = Quaternion.Slerp(_fromRot, LookRotation, k);
            }

            // Arayuze dusen dokunus kameraya GITMIYOR.
            //
            // Once kosulsuz gidiyordu: "Servisi Ac" dugmesine basmak ayni
            // anda arkadaki odayi da seciyor ve kamera oraya ucuyordu.
            HandlePinch();

            if (TouchDown(out Vector3 screen) && !OverUi(screen)) HandleTouch(screen);
        }

        // ---------------------------------------------------------------------
        // IKI PARMAK: YAKINLASTIRMA VE DONDURME.
        //
        // Sinirlar bilincli:
        //
        //   YAKINLASTIRMA 0,45 - 1,00. 1,00, olculmus varsayilan cerceve
        //   (docs/31); daha UZAGA cikilamiyor cunku o cerceve zaten her
        //   seyi gosteriyor ve uzaklasmak yalnizca dokunma hedefini
        //   kucultur. 0,45 yaklasik 2,2 kat buyutme.
        //
        //   DONDURME +-35 derece. Tamamen serbest birakmak iki seyi
        //   bozuyor: dokunma hedefi olcumu belli bir acida yapildi
        //   (docs/31), ve arkadan bakildiginda mutfak salonun onune
        //   geciyor. +-35, "obur taraftan bakayim" istegini karsilarken
        //   kat planini okunur birakiyor.
        //
        // Genel gorunume donmek ikisini de SIFIRLIYOR: oyuncunun her
        // zaman bilinen bir yere donebilecegi bir yol olmali.
        private float _zoom = 1f;
        private float _yawOffset;

        private const float MinZoom = 0.45f;
        private const float MaxZoom = 1.00f;
        private const float MaxYaw = 35f;

        /// <summary>Yakinlastirma orani: 1 varsayilan cerceve.</summary>
        public float Zoom { get { return _zoom; } }

        /// <summary>Oyuncunun cevirdigi aci, derece.</summary>
        public float YawOffset { get { return _yawOffset; } }

        private Vector3 TargetPosition(int room)
        {
            Bounds b = room < 0
                ? CameraFit.OpenBounds(_tables)
                : CameraFit.RoomBounds(room);

            Vector3 p = CameraFit.Position(b, _aspect, _top01, _bottom01);
            Vector3 merkez = b.center;

            // Yakinlastirma kamerayi hedefe DOGRU cekiyor. Gorus acisini
            // daraltmak da yaklastirirdi ama perspektifi degistirir ve
            // dokunma hedefi olcumunun kullandigi hesabi gecersiz kilar.
            p = merkez + (p - merkez) * _zoom;

            if (Mathf.Abs(_yawOffset) > 0.01f)
                p = merkez + Quaternion.Euler(0f, _yawOffset, 0f) * (p - merkez);

            return p;
        }

        /// <summary>Kameranin baktigi yon: temel aci arti oyuncunun donusu.</summary>
        private Quaternion LookRotation
        {
            get { return Quaternion.Euler(0f, _yawOffset, 0f) * CameraFit.Rotation; }
        }

        private bool _pinching;
        private float _lastPinchDist, _lastPinchAngle;

        private void HandlePinch()
        {
            float zoomDelta, twist;
            if (!Gesture(out zoomDelta, out twist))
            {
                _pinching = false;
                return;
            }
            ApplyGesture(zoomDelta, twist);
        }

        /// <summary>
        /// Hareketi uygular. Parmak da tur da BURADAN geciyor.
        ///
        /// Ayri bir giris yolu birakmak, denetimin hicbir zaman gercek
        /// kodu olcmemesi demekti: tur dokunmatik uretemiyor ve
        /// yakinlastirma smiriyla ilgili her sey olculmeden kalirdi.
        /// </summary>
        public void ApplyGesture(float zoomDelta, float twist)
        {
            float oncekiZoom = _zoom;
            float oncekiYaw = _yawOffset;
            _zoom = Mathf.Clamp(_zoom - zoomDelta, MinZoom, MaxZoom);
            _yawOffset = Mathf.Clamp(_yawOffset + twist, -MaxYaw, MaxYaw);

            if (Mathf.Approximately(oncekiZoom, _zoom)
                && Mathf.Approximately(oncekiYaw, _yawOffset)) return;

            // ANINDA, yumusatma yok: parmagin altindaki goruntunun
            // gecikmesi yakinlastirmayi agir hissettiriyor.
            _to = TargetPosition(FocusRoom);
            transform.position = _to;
            transform.rotation = LookRotation;
            _from = _to;
            _t = 1f;

            Quality.ApplyZoom(_zoom);
        }

        /// <summary>Yakinlastirmanin alt siniri. Turun sormasi icin.</summary>
        public static float MinZoomLimit { get { return MinZoom; } }

        /// <summary>Donusun siniri, derece. Turun sormasi icin.</summary>
        public static float MaxYawLimit { get { return MaxYaw; } }

        /// <summary>
        /// Iki parmak (ya da fare tekerlegi) hareketi.
        /// zoom pozitif = yaklastir. twist = derece.
        /// </summary>
        private bool Gesture(out float zoom, out float twist)
        {
            zoom = 0f;
            twist = 0f;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null && ts.touches.Count >= 2
                && ts.touches[0].press.isPressed && ts.touches[1].press.isPressed)
            {
                Vector2 a = ts.touches[0].position.ReadValue();
                Vector2 b = ts.touches[1].position.ReadValue();
                Read(a, b, ref zoom, ref twist);
                return true;
            }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                float w = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(w) > 0.01f)
                {
                    zoom = Mathf.Clamp(w, -1f, 1f) * 0.06f;
                    return true;
                }
            }
            return false;
#else
            if (Input.touchCount >= 2)
            {
                Read(Input.GetTouch(0).position, Input.GetTouch(1).position,
                     ref zoom, ref twist);
                return true;
            }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)
            {
                zoom = Mathf.Clamp(wheel, -1f, 1f) * 0.06f;
                return true;
            }
            return false;
#endif
        }

        /// <summary>Iki parmagin arasindaki mesafe ve acidan degisimi okur.</summary>
        private void Read(Vector2 a, Vector2 b, ref float zoom, ref float twist)
        {
            float d = Vector2.Distance(a, b);
            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

            if (_pinching)
            {
                // Ekran kosegenine BOLUNUYOR: ayni parmak hareketi her
                // cozunurlukte ayni kadar yakinlastirmali.
                float kosegen = Mathf.Sqrt(
                    Screen.width * (float)Screen.width
                    + Screen.height * (float)Screen.height);
                zoom = (d - _lastPinchDist) / Mathf.Max(1f, kosegen) * 2.2f;
                twist = Mathf.DeltaAngle(_lastPinchAngle, ang);
            }
            _lastPinchDist = d;
            _lastPinchAngle = ang;
            _pinching = true;
        }

        public void FocusOn(int room)
        {
            FocusRoom = room;
            Begin();
        }

        /// <summary>
        /// Kayan gecisi baslatir: NEREDEN ve HANGI ACIDAN.
        ///
        /// Aci da tasinmak ZORUNDA. Onceden yalnizca konum kayiyordu ve
        /// aciyi yalnizca parmak hareketi yaziyordu - yani oyuncu
        /// kamerayi cevirip "genel gorunum"e bastiginda kamera dogru
        /// yere gidiyor ama YAN BAKMAYA devam ediyordu. Cevirdigi
        /// kamerayi duzeltmenin caresi kalmiyordu.
        ///
        /// Turun kontrolu bunu gormemisti cunku alanlara (Zoom,
        /// YawOffset) bakiyordu; ikisi de sifirlaniyordu. Olculmesi
        /// gereken sey kameranin KENDISI.
        /// </summary>
        private void Begin()
        {
            _from = transform.position;
            _fromRot = transform.rotation;
            _to = TargetPosition(FocusRoom);
            _t = 0f;
        }

        /// <summary>
        /// Genel gorunum. Yakinlastirma ve donus de SIFIRLANIYOR:
        /// oyuncunun her zaman bilinen bir yere donebilecegi bir yol
        /// olmali, yoksa cevirdigi kamerayi duzeltmenin caresi kalmiyor.
        /// </summary>
        public void Overview()
        {
            _zoom = 1f;
            _yawOffset = 0f;
            Quality.ApplyZoom(_zoom);
            FocusOn(-1);
        }

        /// <summary>
        /// Acik masa sayisi. Genel cerceve buna gore kuruluyor: kapali
        /// kanatlar ekranda yer kaplamiyor.
        ///
        /// Baslangic degeri ilk kademenin masa sayisi; AfterSimChanged
        /// gercek degeri yaziyor ve Snap ile aninda oturtuyor, yani
        /// oyuna girerken kamera ucmuyor. Genisleme sirasindaki degisim
        /// ise KAYARAK oluyor - oyuncu restoranin buyudugunu gormeli.
        /// </summary>
        public int OpenTables
        {
            get { return _tables; }
            set
            {
                if (value == _tables) return;
                _tables = value;
                if (FocusRoom < 0) FocusOn(-1);
            }
        }

        private int _tables = 4;

        /// <summary>
        /// Genel gorunumun HEDEF konumu.
        ///
        /// Denetim icin: "kamera genel gorunume dondu mu" sorusu, daha
        /// once kaydedilmis bir konumla degil SU ANKI hedefle
        /// karsilastirilmali. Restoran buyudugunde genel cerceve de
        /// degisiyor ve eski konum artik dogru cevap degil - denetim
        /// 3,74 m sapma olcup kirmiziya dustu, oysa kamera tam olmasi
        /// gereken yerdeydi.
        /// </summary>
        public Vector3 OverviewPosition { get { return TargetPosition(-1); } }

        /// <summary>Kayan gecisi atlar; kamera hedefine simdi oturur.</summary>
        public void Snap()
        {
            _to = TargetPosition(FocusRoom);
            transform.position = _to;
            transform.rotation = LookRotation;
            _from = _to;
            _fromRot = transform.rotation;
            _t = 1f;
        }

        /// <summary>Bu ekran noktasinda bir arayuz ogesi var mi.</summary>
        private bool OverUi(Vector3 screen)
        {
            if (_ui == null) _ui = FindFirstObjectByType<Ui.UiRoot>();
            return _ui != null && _ui.BlocksPoint(screen);
        }

        private Ui.UiRoot _ui;

        private void HandleTouch(Vector3 screen)
        {
            Ray ray = _cam.ScreenPointToRay(screen);
            bool anyHit = Physics.Raycast(ray, out RaycastHit hit, 200f);

            if (FocusRoom >= 0)
            {
                // ODADAYKEN once MASA araniyor.
                //
                // Bir masaya dokunmak onu mudahalelerin hedefi yapiyor;
                // baska bir yere dokunmak eskisi gibi geri cikariyor.
                // Geri dugmesi ARAMAK mobilde en sik sikayet edilen sey,
                // o yuzden "bos yere dokun = geri" kurali duruyor.
                if (anyHit)
                {
                    TableTouch tt = hit.collider.GetComponentInParent<TableTouch>();
                    if (tt != null && SelectTable(tt.TableIndex)) return;

                    // MASAYI ISKALAMAK GERI CIKARMIYOR.
                    //
                    // Once her iska Overview() cagiriyordu ve deneme
                    // yanilma cezalandiriliyordu: masaya dokunmayi
                    // ogrenmeye calisan oyuncu her isada basa donuyor,
                    // kamera geri ucuyor, tekrar yaklasmasi gerekiyordu.
                    //
                    // Simdi ODANIN ICI guvenli: zemine dokunmak hicbir sey
                    // yapmiyor. Geri cikmak icin odanin DISINA dokunmak
                    // gerekiyor - ve "geri dugmesi aramamak" kurali da
                    // boylece duruyor.
                    RoomTouch inside = hit.collider.GetComponentInParent<RoomTouch>();
                    if (inside != null && inside.RoomIndex == FocusRoom) return;
                }
                Overview();
                return;
            }

            if (!anyHit) return;
            RoomTouch t = hit.collider.GetComponentInParent<RoomTouch>();
            if (t != null) FocusOn(t.RoomIndex);
        }

        /// <summary>
        /// Masayi mudahale hedefi yapar. Yalnizca SERVIS sirasinda ve
        /// yalnizca DOLU masada; bos bir masayi secmek oyuncuya hicbir
        /// sey kazandirmaz ve geri cikma hareketini calardi.
        /// </summary>
        private bool SelectTable(int index)
        {
            if (_ui == null) _ui = FindFirstObjectByType<Ui.UiRoot>();
            GameApp app = _ui != null ? _ui.App : null;
            if (app == null || app.Sim == null) return false;
            if (app.Sim.Phase != Lokanta.Core.Sim.DayPhase.Service) return false;
            if (index < 0 || index >= app.Sim.TableCount) return false;

            Lokanta.Core.Sim.CustomerStage st = app.Sim.TableStage(index);
            if (st == Lokanta.Core.Sim.CustomerStage.None
                || st == Lokanta.Core.Sim.CustomerStage.Done
                || st == Lokanta.Core.Sim.CustomerStage.LeftAngry)
                return false;

            // Ayni masaya ikinci dokunus secimi BIRAKIYOR: secimden
            // cikmanin yolu, secmenin yoluyla ayni olmali.
            app.SelectedTable = app.SelectedTable == index ? -1 : index;
            _ui.Refresh();
            return true;
        }

        /// <summary>
        /// Dokunma veya fare. Yeni Input System paketi projede var ama
        /// eski girdi de acik; ikisini de destekleyen en kisa yol bu.
        /// </summary>
        private static bool TouchDown(out Vector3 screen)
        {
            screen = default;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
            {
                screen = ts.primaryTouch.position.ReadValue();
                return true;
            }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screen = mouse.position.ReadValue();
                return true;
            }
            return false;
#else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                screen = Input.GetTouch(0).position;
                return true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                screen = Input.mousePosition;
                return true;
            }
            return false;
#endif
        }
    }
}
