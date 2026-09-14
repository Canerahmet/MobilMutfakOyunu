using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// Bir ekran. Kendi agacini kurar, kapatilinca temizlenir.
    ///
    /// Adi "UiScreen", "Screen" degil: UnityEngine.Screen zaten var ve
    /// ayni ad iki tipi kapistirir - her dosyada hangisini kastettigimi
    /// yazmak zorunda kalirdim.
    /// </summary>
    public abstract class UiScreen
    {
        public UiRoot Ui { get; internal set; }
        public GameApp App { get { return Ui.App; } }

        /// <summary>Ekranin agacini kurar.</summary>
        public abstract VisualElement Build();

        /// <summary>Her karede cagriliyor; yalnizca EN USTTEKI ekran icin.</summary>
        public virtual void Tick() { }

        /// <summary>Geri tusu. false donerse ekran kapanmiyor.</summary>
        public virtual bool OnBack() { return true; }

        /// <summary>
        /// Ekran KAPANDI - nasil kapandigi fark etmeksizin.
        ///
        /// Geri tusu, dugme ya da yigin temizligi; uc yol da buradan
        /// geciyor. Kuresel bir durumu degistiren bir ekran (duraklatma
        /// gibi) onu burada geri veriyor. Yalnizca OnBack'e guvenmek
        /// yetmiyordu: "Devam" dugmesi dogrudan Pop cagiriyor ve
        /// duraklatma geri alinmiyordu.
        /// </summary>
        public virtual void OnClosed() { }
    }

    /// <summary>
    /// Ekran yigini. Menu -> mutfak secimi -> yuva -> oyun seklinde
    /// ust uste biniyor, geri tusu en ustekini kapatiyor.
    ///
    /// Yigin olmasinin sebebi Android: donanim geri tusu her ekranda
    /// calismali ve "geri" her zaman BIR ONCEKI ekran olmali. Ekranlar
    /// arasi elle gecis yazan bir yapida bu her seferinde yeniden
    /// dusunulmesi gereken bir sey oluyor.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiRoot : MonoBehaviour
    {
        public GameApp App;

        /// <summary>
        /// Butun arayuzun yazi tipi (Rubik, SIL OFL 1.1).
        ///
        /// ACIKCA baglaniyor, temaya birakilmiyor. Unity'nin varsayilan
        /// calisma zamani temasi EDITORDE calisiyor ama YAPIDA yazi tipini
        /// cozemedi: ilk masaustu yapisinda dugmeler ciziliyor, uzerlerinde
        /// hicbir yazi gorunmuyordu. Kok ogeye yazi tipi vermek butun
        /// agaca miras kaliyor ve temadan bagimsiz.
        /// </summary>
        public Font Font;

        private readonly List<UiScreen> _stack = new List<UiScreen>();
        private readonly List<VisualElement> _views = new List<VisualElement>();
        private VisualElement _root;

        public UiScreen Top { get { return _stack.Count > 0 ? _stack[_stack.Count - 1] : null; } }


        /// <summary>En ustteki ekranin agaci. Yalnizca bu gorunur ve tiklanir.</summary>
        public VisualElement TopView
        {
            get { return _views.Count > 0 ? _views[_views.Count - 1] : null; }
        }

        public int Depth { get { return _stack.Count; } }

        private void Awake()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.style.flexGrow = 1;
            _root.style.backgroundColor = Color.clear;

            if (Font != null)
                _root.style.unityFontDefinition = FontDefinition.FromFont(Font);
            else
                Debug.LogWarning("Arayuz yazi tipi baglanmadi; metinler gorunmeyebilir.");

            ApplySafeArea();
            _root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea());
        }

        private Rect _safe;

        /// <summary>
        /// Cihazin GUVENLI ALANINI kok ogeye dolgu olarak uygular.
        ///
        /// Yatay tutusta centik ya da kamera deligi SOL veya SAG kenarda
        /// oluyor ve tipik olarak 30-45 dp iceri giriyor. Su anki yerlesimde
        /// gun/kasa sol basta, menu dugmesi sag basta - ucu de centigin
        /// altinda kalirdi. Menu dugmesinin kaybolmasi, oyuncunun oyundan
        /// cikamamasi demek.
        ///
        /// Donusum: guvenli alan FIZIKSEL pikselde, panel ise kendi
        /// biriminde olcuyor. Oran, kokun olculen genisliginin ekran
        /// genisligine bolumu.
        /// </summary>
        private void ApplySafeArea()
        {
            Rect safe = Screen.safeArea;
            if (safe == _safe) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            float w = _root.resolvedStyle.width;
            if (w <= 1f) return;                       // henuz duzenlenmedi

            _safe = safe;
            float k = w / Screen.width;

            _root.style.paddingLeft = safe.xMin * k;
            _root.style.paddingRight = (Screen.width - safe.xMax) * k;
            _root.style.paddingTop = (Screen.height - safe.yMax) * k;
            _root.style.paddingBottom = safe.yMin * k;
        }

        private void Update()
        {
            Top?.Tick();

            // Android geri tusu ve masaustunde Esc.
            if (BackPressed() && _stack.Count > 0)
            {
                if (Top.OnBack()) Pop();
            }
        }

        /// <summary>
        /// Bu ekran noktasinda tiklanabilir bir arayuz ogesi var mi.
        ///
        /// Kamera bunu soruyor: eylem cubugundaki bir dugmeye basmak,
        /// arkadaki odayi da secip kamerayi oraya ucuruyordu.
        ///
        /// Panelin kendi SECICISI kullaniliyor (Pick), kendi yazdigimiz
        /// bir dikdortgen hesabi degil: cubuklarin yuksekligi icerige gore
        /// degisiyor ve elle yazilan bir sinir kaciniIlmaz olarak
        /// ayrisirdi.
        /// </summary>
        public bool BlocksPoint(Vector2 screenPoint)
        {
            if (_root == null || _root.panel == null) return false;

            // Panelin y ekseni ekranin TERSI yonunde.
            Vector2 flipped = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            Vector2 local = RuntimePanelUtils.ScreenToPanel(_root.panel, flipped);

            VisualElement hit = _root.panel.Pick(local);
            if (hit == null) return false;

            // Kok ogenin kendisi saydam ve butun ekrani kapliyor; onu
            // engel saymak, salona hic dokunulamamasi demek olurdu.
            return hit != _root;
        }

        private static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        // ---------------------------------------------------------------------
        public void Push(UiScreen s)
        {
            // Alttaki ekran GIZLENIYOR, sadece ustu ortulmuyor.
            //
            // Once yalnizca ustune yeni bir ekran ekleniyordu; eski agac
            // yerinde kaliyor, dugmeleri hala secilebiliyordu. Kendi kendine
            // gezen tur tam bu yuzden yanlis dugmeye bastı (alttaki mutfak
            // ekranindaki "Basla"ya). Oyuncuda da klavye/oyun kolu gezinmesi
            // gorunmeyen bir dugmeye dusebilirdi.
            //
            // Yan faydasi mobilde olcum: gizli bir agac duzenlenmiyor ve
            // cizilmiyor.
            if (_views.Count > 0) _views[_views.Count - 1].style.display = DisplayStyle.None;

            s.Ui = this;
            VisualElement v = s.Build();
            Fill(v);

            _stack.Add(s);
            _views.Add(v);
            _root.Add(v);
            Enter(v);
        }

        /// <summary>
        /// Acilan ekran icin giris hareketi: saydamlik 0 -> 1 ve
        /// asagidan 12 px yukari.
        ///
        /// NEDEN ELLE, USS ILE DEGIL: bu projede stil sayfasi yok, butun
        /// arayuz C# ile kuruluyor. UI Toolkit'in gecisleri IStyle
        /// uzerinden de erisilebiliyor, yani UXML/USS olmadan da tam
        /// olarak ayni sey yapilabiliyor.
        ///
        /// YALNIZCA DONUSUM VE SAYDAMLIK. Unity'nin kendi belgesi
        /// genislik/yukseklik gibi YERLESIM ozelliklerinin gecisinde
        /// yerlesimin yeniden hesaplandigini ve kare hizinin dustugunu
        /// soyluyor; translate/scale/opacity ise geometriyi yeniden
        /// uretmiyor. Dusuk seviye bir telefonda tek onemli olan bu.
        ///
        /// SURELER 30 fps'e GORE. Bir kare 33 ms; 100 ms'lik bir gecis
        /// uc kare demek ve pratikte gorulmuyor. Material'in olceginde
        /// bir basamak yukari cikiliyor: giris 220 ms (yavaslayarak),
        /// cikis 140 ms. Giris ve cikisin ayni egriyi kullanmamasi
        /// kural - gelen sey yavaslayarak yerlesir, giden hizlanarak
        /// cikar.
        /// </summary>
        private static void Enter(VisualElement v)
        {
            // usageHints ONCEDEN veriliyor: gecis basladiktan sonra
            // verilirse Unity o kare icin butun alt agacin cizim
            // verisini yeniden uretiyor.
            v.usageHints |= UsageHints.DynamicTransform;

            v.style.opacity = 0f;
            v.style.translate = new Translate(0, 12);

            v.schedule.Execute(() =>
            {
                v.style.transitionProperty =
                    new List<StylePropertyName> { "opacity", "translate" };
                v.style.transitionDuration =
                    new List<TimeValue> { new TimeValue(220, TimeUnit.Millisecond),
                                          new TimeValue(220, TimeUnit.Millisecond) };
                v.style.transitionTimingFunction =
                    new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutCubic),
                                               new EasingFunction(EasingMode.EaseOutCubic) };
                v.style.opacity = 1f;
                v.style.translate = new Translate(0, 0);
            }).StartingIn(0);
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;
            int i = _stack.Count - 1;
            _stack[i].OnClosed();

            // KAPANAN EKRAN AGACTAN HEMEN CIKIYOR, gorsel olarak
            // solarak gidiyor.
            //
            // Kaldirmayi gecisin sonuna ERTELEMEK yanlis olurdu: ekran
            // hala agacta oldugu surece dugmeleri secilebilir kalir ve
            // tur bunun aynisini bir kez yasadi (alttaki ekranin
            // dugmesine basildi). O yuzden asil oge cikiyor, yerine
            // yalnizca solup yok olan bir KOPYA konmuyor - sade ve
            // dogru olan, cikisi altttaki ekranin GIRISIYLE
            // gostermek.
            VisualElement giden = _views[i];
            _root.Remove(giden);
            _views.RemoveAt(i);
            _stack.RemoveAt(i);

            if (_views.Count > 0)
            {
                VisualElement alt = _views[_views.Count - 1];
                alt.style.display = DisplayStyle.Flex;
                Enter(alt);
            }
        }

        private static void Fill(VisualElement v)
        {
            v.style.position = Position.Absolute;
            v.style.left = 0;
            v.style.right = 0;
            v.style.top = 0;
            v.style.bottom = 0;
            v.style.display = DisplayStyle.Flex;
        }

        /// <summary>Yigini bosaltip tek bir ekranla basliyor.</summary>
        public void Replace(UiScreen s)
        {
            while (_stack.Count > 0) Pop();
            Push(s);
        }

        /// <summary>Ustteki ekrani yeniden kurar. Veri degisince.</summary>
        public void Refresh()
        {
            if (_stack.Count == 0) return;
            UiScreen s = Top;
            int i = _stack.Count - 1;
            _root.Remove(_views[i]);

            VisualElement v = s.Build();
            Fill(v);
            _views[i] = v;
            _root.Add(v);
        }
    }
}
