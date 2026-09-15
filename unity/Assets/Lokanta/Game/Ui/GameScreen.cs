using Lokanta.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// Oyunun ana ekrani: ust serit + asamaya gore alt serit.
    ///
    /// Ortasi BOS birakiliyor, bilerek: orada 3B salon var ve oyuncunun
    /// bakmasi gereken sey o. docs/16 gunluk dokunus butcesi 40-60; ekrani
    /// panellerle doldurmak, oyuncunun salona hic bakmamasi demek.
    ///
    /// Ust serit her karede guncelleniyor (Tick), alt serit yalnizca asama
    /// degisince yeniden kuruluyor. Her karede butun agaci yeniden kurmak,
    /// dokunulan dugmeyi elin altindan cekmek olurdu.
    /// </summary>
    public sealed class GameScreen : UiScreen
    {
        // _tables / _staff / _flow SILINDI.
        //
        // Ust serit yeniden tasarlaninca agactan CIKARILDILAR ama
        // alanlar kaldi: her Tick'te metinleri uretiliyor (biri
        // string.Format + uc int kutulamasi), sonra kosulsuz
        // display=None yaziliyordu. Yanindaki yorum "baska ekranlar
        // okuyor" diyordu - hicbir ekran okumuyordu.
        //
        // Bu projenin kendi kurali: "cagri yerleri yalan soyleyen bir
        // alan silinir, baglanmaz".
        private Label _day, _cash, _rep, _rent;

        /// <summary>Sol ust kart (sabah kontrol listesi / servis akisi).</summary>
        private VisualElement _cards;

        /// <summary>Sag ust durum kartlari (ciro, memnuniyet).</summary>
        private VisualElement _stats;

        /// <summary>Gun ilerleme cubugunun dolan parcasi.</summary>
        private VisualElement _meterFill;

        private Label _phaseLabel, _revenue, _satisfaction;

        /// <summary>"12 / 60. gun" - kampanyanin nerede oldugu.</summary>
        private Label _season;
        private int _shownRevenueDay = -1;
        private DayPhase _builtCards = (DayPhase)(-1);
        private VisualElement _bottom;

        /// <summary>
        /// Seritlerin kapladigi dikey alan, dp. Tur bunu OLCUYOR.
        ///
        /// Kodun kendi yorumu "seritlerin toplami 129 dp'yi gecmemeli"
        /// diyor ve o sayi goz karariyla korunuyordu - yani hic
        /// korunmuyordu. Bir dugme eklemek, bir etiketi uzatmak ya da
        /// bir yaziyi sardirmak serit yuksekligini sessizce buyutuyor
        /// ve salon ekranin ucte birine dusuyor.
        /// </summary>
        /// <summary>
        /// Alt seritte YAZISI KIRPILAN dugme sayisi.
        ///
        /// Ilk yazimda "satirin istedigi genislik" olculuyordu ve YANLIS
        /// SEYI olcuyordu: esnek dugmeler kendilerine ayrilan genisligi
        /// bildirdigi icin toplam her zaman ekran genisligine esit
        /// cikiyordu. Yani olcum, tam da yakalamasi gereken durumda
        /// (tasan serit) sessiz kaliyordu.
        ///
        /// Dogru soru "satir ne kadar genis" degil, "hangi dugmenin
        /// yazisi sigmadi": kirpilma tasmanin GORUNEN belirtisi ve
        /// dogrudan olculebiliyor.
        ///
        /// Servis seridi bir sure 1103 dp istiyordu (ekran 873) ve
        /// alti dugmenin yazisi kirpiliyordu.
        /// </summary>
        public int ClippedButtons
        {
            get
            {
                // UST SERIT VE KARTLAR DA OLCULUYOR.
                //
                // Olcum yalnizca `_bottom` icinde geziyordu ve
                // `_bottom == null` ise SIFIR donuyordu - yani
                // "olculemedi" ile "temiz" ayni sonucu veriyordu.
                // Ust seritteki kapsuller iki dilli kirpilmanin en
                // riskli yeri ("Rent in {0} days" / "Kiraya {0} gun")
                // ve sol kart SABIT 190 dp genisliginde; ikisi de
                // olcumun disindaydi.
                if (_bottom == null && _top == null) return -1;

                _kirpikAyrinti = null;
                int n = 0;
                n += KirpilanSayisi(_bottom);
                n += KirpilanSayisi(_top);
                n += KirpilanSayisi(_cards);
                n += KirpilanSayisi(_stats);
                return n;
            }
        }

        /// <summary>
        /// Alt seritte EKRANIN DISINA TASAN oge sayisi.
        ///
        /// UCUNCU BIR BASARISIZLIK BICIMI. Serit yuksekligi olculuyor,
        /// kirpilan yazi olculuyor - ama ikisi de YATAY TASMAYI
        /// gormuyor. UI Toolkit'te `flex-shrink` varsayilani CSS'in
        /// aksine SIFIR: satir sigmayinca hicbir sey daralmiyor, son
        /// oge disari tasiyor ve bir oncekinin USTUNE biniyor.
        ///
        /// Turk mutfaginda tam bu oldu - veresiye dugmesi "Ilgi"
        /// dugmesinin uzerine bindi - ve iki olcum de yesil kaldi:
        /// yukseklik dogruydu, hicbir YAZI kirpilmamisti. Hatayi
        /// yalnizca magaza cozunurlugunde alinan bir render gosterdi.
        /// </summary>
        /// <summary>Son olcumdeki en belirgin cakisma; tani icin.</summary>
        public string OverflowDetail { get { return _tasmaAyrinti; } }

        private string _tasmaAyrinti;

        /// <summary>
        /// Iki DUGME birbirinin ustune biniyor mu.
        ///
        /// Neden yalnizca dugmeler: bu olcum iki kez yanlis seyi olctu.
        /// Once `_bottom`'in DOGRUDAN cocuklarina bakiyordu - orada tek
        /// bir satir kapsayicisi var, karsilastiracak kardes yok, sonuc
        /// her zaman sifir. Sonra butun ogeleri karsilastirdi ve bu kez
        /// KASITLI cakismalari saydi: `Kit.Cta`'nin cift oku iki ucgeni
        /// bilerek 6 dp bindiriyor, simgeler de icinde sekil ustune
        /// sekil koyuyor.
        ///
        /// Aranan sey bunlarin hicbiri degil: BIR OYUNCUNUN
        /// DOKUNAMADIGI DUGME. Iki dugmenin ust uste binmesi her zaman
        /// hatadir - hangisine bastigin belirsizdir. Ust uste binen iki
        /// ucgen ise bir simgedir.
        ///
        /// Olcumun adi artik olctugu seyi soyluyor.
        /// </summary>
        public int OverlappingButtons
        {
            get
            {
                if (_bottom == null) return -1;
                if (float.IsNaN(_bottom.worldBound.width)) return -1;

                _tasmaAyrinti = null;
                var d = new System.Collections.Generic.List<Button>();
                foreach (Button b in _bottom.Query<Button>().ToList())
                {
                    if (b.resolvedStyle.display == DisplayStyle.None) continue;
                    Rect r = b.worldBound;
                    if (float.IsNaN(r.width) || r.width <= 0f) continue;
                    // Ic ice dugme yok, ama olursa kardes sayilmasin.
                    if (b.GetFirstAncestorOfType<Button>() != null) continue;
                    d.Add(b);
                }

                int n = 0;
                for (int i = 0; i < d.Count; i++)
                    for (int j = i + 1; j < d.Count; j++)
                    {
                        Rect a = d[i].worldBound, b2 = d[j].worldBound;
                        float yatay = Mathf.Min(a.xMax, b2.xMax) - Mathf.Max(a.xMin, b2.xMin);
                        float dikey = Mathf.Min(a.yMax, b2.yMax) - Mathf.Max(a.yMin, b2.yMin);
                        if (yatay <= 0.5f || dikey <= 0.5f) continue;

                        n++;
                        if (_tasmaAyrinti == null)
                            _tasmaAyrinti = Ad(d[i]) + " x " + Ad(d[j])
                                            + " (" + yatay.ToString("0") + " dp)";
                    }
                return n;
            }
        }

        /// <summary>Tanida okunabilir bir ad: once isim, sonra yazi.</summary>
        private static string Ad(VisualElement v)
        {
            if (!string.IsNullOrEmpty(v.name)) return v.name;
            Button b = v as Button;
            if (b != null && !string.IsNullOrEmpty(b.text)) return b.text;
            Label l = v as Label;
            if (l != null && !string.IsNullOrEmpty(l.text)) return l.text;
            foreach (Label ic in v.Query<Label>().ToList())
                if (!string.IsNullOrEmpty(ic.text)) return ic.text;
            return v.GetType().Name;
        }

        /// <summary>Son olcumde kirpilan ilk ogenin yazisi; tani icin.</summary>
        public string ClipDetail { get { return _kirpikAyrinti; } }

        private static string _kirpikAyrinti;

        /// <summary>Bir kokun altinda yazisi kirpilan oge sayisi.</summary>
        private static int KirpilanSayisi(VisualElement kok)
        {
            if (kok == null) return 0;
            int n = 0;

            // ONCE DUGMELER: bir dugmenin ic etiketi iki kez sayilmasin
            // diye etiket taramasinda dugme altindakiler atlaniyor.
            foreach (Button b in kok.Query<Button>().ToList())
            {
                    if (!string.IsNullOrEmpty(b.text))
                    {
                        if (Clipped(b, b.text)) { n++; Kaydet(b.text); }
                        continue;
                    }

                    // YAZI ARTIK DUGMENIN ICINDEKI ETIKETTE.
                    //
                    // Yeni arayuzde simgeli dugmelerin text alani bos ve
                    // yazi bir alt etikette duruyor. Eski olcum onlari
                    // "yazisi yok" diye ATLIYORDU - yani tam da
                    // kirpilmaya en yakin dugmeler (dar, iki dilli,
                    // simgenin altinda tek satir) olcumun disinda
                    // kaliyordu.
                    foreach (Label l in b.Query<Label>().ToList())
                        if (!string.IsNullOrEmpty(l.text) && Clipped(l, l.text))
                        {
                            n++;
                            Kaydet(l.text);
                            break;
                        }
            }

            // SONRA SERBEST ETIKETLER: kapsuller, kart basliklari,
            // durum satirlari. Dugme altindakiler yukarida sayildi.
            foreach (Label l in kok.Query<Label>().ToList())
            {
                if (string.IsNullOrEmpty(l.text)) continue;
                if (l.GetFirstAncestorOfType<Button>() != null) continue;
                if (Clipped(l, l.text)) { n++; Kaydet(l.text); }
            }
            return n;
        }

        /// <summary>
        /// Kirpilan yaziyi tanıya kaydeder.
        ///
        /// Sayi tek basina "uc dugme kirpildi" diyor ve hangisi
        /// oldugunu soylemiyor - yani duzeltmeyi TAHMINE birakiyor.
        /// Bu oturumda ust uste binen dugmeler de once isimsiz
        /// sayilmisti ve ancak adlari basilinca duzeltilebildi.
        /// </summary>
        private static void Kaydet(string metin)
        {
            if (_kirpikAyrinti == null) _kirpikAyrinti = metin;
            else if (!_kirpikAyrinti.Contains(metin))
                _kirpikAyrinti += " | " + metin;
        }

        /// <summary>Bir ogenin yazisi kendi genisligine sigiyor mu.</summary>
        /// <summary>
        /// Ogenin kendisi ya da bir atasi tasani kirpiyor mu.
        ///
        /// Kirpmiyorsa yazi tasar ama OKUNUR; o zaman sorun kesilme
        /// degil, komsusuyla cakisma - ve onu ayri bir olcum ariyor.
        /// </summary>
        private static bool KirpanKutudaMi(VisualElement v)
        {
            // SATIR ICI STILDEN okunuyor: `resolvedStyle` bu Unity
            // surumunde `overflow` tasimiyor (IResolvedStyle'da yok).
            // Bu projede stiller zaten C#'ta satir ici veriliyor, yani
            // kaynak dogru yer.
            for (VisualElement e = v; e != null; e = e.parent)
            {
                StyleEnum<Overflow> o = e.style.overflow;
                if (o.keyword == StyleKeyword.Undefined
                    && o.value == Overflow.Hidden) return true;
            }
            return false;
        }

        private static bool Clipped(TextElement v, string text)
        {
            // KESILME ANCAK KIRPAN BIR KUTUDA OLUR.
            //
            // UI Toolkit'te `overflow` varsayilani GORUNUR: yazi
            // kutusunu assa bile cizilmeye devam eder, kesilmez. Bu
            // olcum ise "asti = kesildi" varsayiyordu ve bu arayuzde
            // Overflow.Hidden yalnizca IKI yerde var - bir simge kutusu
            // ve bir ilerleme cubugu; hicbir yazi etiketinde yok.
            //
            // Yani olcum, bu arayuzde VAR OLMAYAN bir hata bicimini
            // ariyordu ve yalnizca yanlis alarm uretebiliyordu. Hizli
            // yemek turu ilk kez kosturuldugunda uc etiket birden
            // kirmizi yakti; ucu de ekranda eksiksiz okunuyordu.
            //
            // Asil koruma zaten iki komsu olcumde: ust uste binen dugme
            // (Turk mutfagindaki gercek hatayi O yakaladi) ve ekranin
            // disina tasan oge. Burasi artik yalnizca GERCEKTEN kirpan
            // bir kutunun icini olcuyor - bugun sessiz, ama biri yazi
            // kabina Overflow.Hidden koydugu gun konusur.
            if (!KirpanKutudaMi(v)) return false;

            float have = v.resolvedStyle.width
                         - v.resolvedStyle.paddingLeft
                         - v.resolvedStyle.paddingRight;
            if (float.IsNaN(have) || have <= 0f) return false;

            float want = v.MeasureTextSize(
                text, 0f, VisualElement.MeasureMode.Undefined,
                0f, VisualElement.MeasureMode.Undefined).x;

            // Yarim piksel pay: olcum ile yerlesim arasindaki yuvarlama
            // farki kirpilma sayilmamali.
            if (want <= have + 0.5f) return false;

            // GENISLIK ASILDI - AMA SARAN BIR ETIKETTE BU KIRPILMA DEGIL.
            //
            // UI Toolkit'te sarma varsayilan olarak ACIK ve bu olcum
            // yaziyi HER ZAMAN tek satir varsayiyordu. Hizli yemek turu
            // ilk kez kosturuldugunda yakalandi: "Kombo kapali" iki
            // satira sariyor, ekranda EKSIKSIZ okunuyor, ve kontrol
            // "kirpilan yazi: 2" deyip turu dusuruyordu. Turkce
            // etiketler tesadufen tek satira sigdigi icin yanlis alarm
            // bugune kadar hic patlamadi.
            //
            // Olcut: oge IKI SATIR yuksekliginde mi. Sardiysa yazi
            // asagi akmis ve okunuyor demektir; sarmadiysa gercekten
            // kesiliyor.
            //
            // Once bunu "saran etiketi yukseklikle olc" diye yazdim ve
            // DAHA KOTU oldu - varsayilan sarma acik oldugu icin butun
            // etiketler o dala dustu ve yedi yanlis alarm cikti ("x4",
            // "8.000" gibi apacik sigan yazilar). Dar cozum dogru cozum.
            float satirY = v.MeasureTextSize(
                "X", 0f, VisualElement.MeasureMode.Undefined,
                0f, VisualElement.MeasureMode.Undefined).y;
            float ogeY = v.resolvedStyle.height
                         - v.resolvedStyle.paddingTop
                         - v.resolvedStyle.paddingBottom;
            if (satirY > 0f && !float.IsNaN(ogeY) && ogeY >= satirY * 1.8f)
                return false;

            return true;
        }

        public float StripHeight
        {
            get
            {
                float t = _top != null ? _top.resolvedStyle.height : 0f;
                float b = _bottom != null ? _bottom.resolvedStyle.height : 0f;
                if (float.IsNaN(t)) t = 0f;
                if (float.IsNaN(b)) b = 0f;
                return t + b;
            }
        }
        private DayPhase _builtPhase = (DayPhase)(-1);
        private VisualElement _toast;
        private VisualElement _top;

        public override VisualElement Build()
        {
            // ONBELLEK SIFIRLANIYOR.
            //
            // Build() etiketleri YENIDEN YARATIYOR - metinleri bos. Ama
            // "son yazilan deger" alanlari eski degerlerini koruyordu,
            // yani Tick() "degismemis" deyip hicbirini yazmiyordu.
            //
            // Sonuc: her Refresh()'ten sonra ust serit BOSALIYOR. Gun,
            // Kasa, Itibar ve kira geri sayimi ekrandan siliniyor; kasa
            // bir musteri odeyince geri geliyor, itibar gun sonuna, gun
            // ve kira ertesi gune kadar bos kaliyor. Kira geri sayimi
            // docs/16'nin "her an gorunur olmali" dedigi tek sey.
            //
            // Masaya dokunup mudahale hedefi secmek Refresh() cagiriyor,
            // yani oyuncu yeni mekanigi ilk denedigi anda ust serit
            // siliniyordu.
            _shownDay = _shownRep = -1;
            _shownRepCap = -1;
            _shownRentDays = -1;
            _shownCash = long.MinValue;
            _shownServed = _shownAngry = _shownOccupied = -1;
            _shownPhase = (DayPhase)(-1);

            VisualElement root = new VisualElement();
            root.style.flexGrow = 1;

            // Kokun KENDISI dokunusu yutmuyor. Cubuklari ve dugmeleri
            // yutuyor, aradaki bosluk degil: salona dokunulabilmeli.
            root.pickingMode = PickingMode.Ignore;

            _top = TopBar();
            root.Add(_top);

            // Orta: 3B sahne. Arayuz burada hicbir sey cizmiyor ama
            // dokunuslari da ENGELLEMEMELI - kamera odaya yaklasabilsin.
            VisualElement middle = new VisualElement();
            middle.style.flexGrow = 1;
            middle.style.flexDirection = FlexDirection.Row;
            middle.style.justifyContent = Justify.SpaceBetween;
            middle.style.paddingLeft = Theme.Pad;
            middle.style.paddingRight = Theme.Pad;
            middle.pickingMode = PickingMode.Ignore;

            // KARTLAR SALONUN USTUNDE YUZUYOR.
            //
            // Serit degil KART: bir serit ekranin butun genisligini ve
            // yuksekligini yiyor, kart yalnizca durdugu koseyi. Ust
            // koseler zaten BOS - bina ekranin ortasinda duruyor ve
            // ustunde gokyuzu rengi var (docs/19 gokyuzu kubbesi
            // tasimiyor). Referansin duzeni de tam olarak bu.
            //
            // Kartlarin kendisi dokunusu yutuyor (uzerlerine basilinca
            // arkadaki odaya dokunulmamali) ama ARALARINDAKI bosluk
            // yutmuyor: middle'in kendisi Ignore.
            _cards = new VisualElement();
            _cards.style.width = 190;
            _cards.style.marginTop = Theme.Gap;
            middle.Add(_cards);

            _stats = new VisualElement();
            _stats.style.marginTop = Theme.Gap;
            _stats.style.alignItems = Align.FlexEnd;
            middle.Add(_stats);

            root.Add(middle);

            _bottom = new VisualElement();
            root.Add(_bottom);
            BuildBottom();

            // Bildirim seridi alt cubuktan SONRA ekleniyor, yani onun
            // USTUNDE ciziliyor; ve yuksekligi sabit bir sayi degil,
            // cubugun OLCULEN yuksekligi.
            //
            // Once cubuktan once ekleniyor ve "bottom: 96" yaziyordu;
            // cubuk 134 dp yuksekti, yani yazilan her bildirimin %85'i
            // cubugun arkasinda kaliyordu. Oyunun tek gorsel geri
            // bildirimi hic gorunmuyordu: oyuncu dugmeye basiyor, hicbir
            // sey olmadigini saniyor, tekrar basip hakkini yakiyordu.
            _toast = new VisualElement();
            _toast.style.position = Position.Absolute;
            _toast.style.left = 0;
            _toast.style.right = 0;
            _toast.style.alignItems = Align.Center;
            _toast.pickingMode = PickingMode.Ignore;
            root.Add(_toast);

            // Kamera cubuklarin ARASINA cerceveliyor. Yukseklikler icerige
            // gore degisiyor (mudahale satiri, kalan hak), o yuzden sabit
            // sayi yazilmiyor - olculup bildiriliyor.
            // Hem kok hem ALT CUBUK izleniyor.
            //
            // Yalnizca kok izlendiginde bildirim seridi yanlis yerde
            // kaliyordu: asama degisince alt cubuk bir satirdan iki satira
            // cikiyor ama kokun olculeri degismiyor, yani olay hic
            // tetiklenmiyordu ve serit cubugun altinda kaliyordu.
            root.RegisterCallback<GeometryChangedEvent>(_ => ReportSafeArea(root));
            _bottom.RegisterCallback<GeometryChangedEvent>(_ => ReportSafeArea(root));

            // Bildirim alani artik VAR: ipucu seridi burada kuruluyor.
            // BuildBottom yukarida cagrildi ve o sirada _toast yoktu.
            RebuildNotices();

            // Kartlar EN SONDA: Build() sirasinda _cards ve _stats yeni
            // yaratildi, icleri bos. Tick ilk karede degerleri yaziyor.
            _shownProgress = -1f;
            _shownMeterPhase = (DayPhase)(-1);
            BuildCards();

            return root;
        }

        private void ReportSafeArea(VisualElement root)
        {
            float h = root.resolvedStyle.height;
            if (h <= 1f) return;

            float top = _top != null ? _top.resolvedStyle.height : 0f;
            float bottom = _bottom != null ? _bottom.resolvedStyle.height : 0f;

            // Bildirim seridi cubugun hemen ustunde dursun.
            if (_toast != null) _toast.style.bottom = bottom + Theme.Gap;

            if (App != null && App.Rig != null) App.Rig.SetSafeArea(top / h, bottom / h);
        }

        // =====================================================================
        /// <summary>
        /// UST SERIT: ROZET, CUBUK, KAPSULLER.
        ///
        /// Once tam genislikte koyu bir seritti ve icinde yedi ayri yazi
        /// yan yana diziliyordu ("Gun 1  Kasa 11.963  Itibar 24,0 / 55
        /// Kiraya 6 gun - 2.546  Aksam  Masa 4  Kadro 2"). Hepsi ayni
        /// boyda, ayni renkte ve ayni onemde gorunuyordu - yani hicbiri
        /// one cikmiyordu.
        ///
        /// Yeni duzen uc gruba ayiriyor ve gruplarin KENDISI bilgi:
        ///
        ///   SOL    - gun rozeti ve gunun ilerlemesi. "Neredeyim?"
        ///   SAG    - kasa ve itibar: oyunun iki kaynagi, kapsullerde.
        ///   EN SAG - menu.
        ///
        /// Serit zemini KALKTI: kapsuller dogrudan salonun uzerinde
        /// yuzuyor. Kazanilan yer salona gidiyor.
        /// </summary>
        private VisualElement TopBar()
        {
            VisualElement bar = new VisualElement();
            bar.style.paddingLeft = Theme.Pad;
            bar.style.paddingRight = Theme.Pad;
            bar.style.paddingTop = Theme.Gap;
            bar.style.paddingBottom = 4;
            bar.pickingMode = PickingMode.Ignore;

            VisualElement row = Theme.Row(Theme.Gap);
            row.style.alignItems = Align.Center;
            row.pickingMode = PickingMode.Ignore;

            // --- SOL: gun rozeti + gunun ilerlemesi ---------------------
            _day = Theme.Text("", Theme.FontBody, Theme.Ink);
            row.Add(Kit.Badge(_day));

            VisualElement gun = new VisualElement();
            gun.style.marginLeft = 8;
            gun.pickingMode = PickingMode.Ignore;

            // ASAMA ARTIK BIR ETIKET, KAYBOLAN BIR YAZI DEGIL.
            //
            // Gunun evresi oyunun en cok sey belirleyen durumu: hangi
            // dugmelerin calistigini, musterinin gelip gelmedigini,
            // isigin rengini o belirliyor. Ust seritte sagda kucuk gri
            // bir yaziydi.
            _phaseLabel = Theme.Text("", Theme.FontSmall, Theme.Ink);
            _phaseLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            // KAMPANYANIN HEDEFI ARTIK EKRANDA.
            //
            // Altmis gunun sonunda yedi eksende puanlaniyorsunuz ve
            // bu, 61. gune kadar HICBIR YERDE yazmiyordu: oyuncu
            // bitis tarihi olmayan bir dukkan isletip sonunda hic
            // duymadigi bir karneyle karsilasiyordu. Hedef degil,
            // surprizdi.
            //
            // Asamanin YANINDA duruyor, ayri bir satirda degil: ust
            // serit her an bakilan yer ve bir satir daha eklemek
            // salondan yer calardi.
            VisualElement asamaSatiri = Theme.Row(6);
            asamaSatiri.style.alignItems = Align.FlexEnd;
            asamaSatiri.style.marginBottom = 3;
            asamaSatiri.pickingMode = PickingMode.Ignore;
            asamaSatiri.Add(_phaseLabel);

            _season = Theme.Text("", Theme.FontSmall, Theme.InkFaint);
            asamaSatiri.Add(_season);
            gun.Add(asamaSatiri);

            VisualElement track = Kit.Meter(out _meterFill, 138f, 9f);
            gun.Add(track);
            row.Add(gun);

            // KIRA GERI SAYIMI. docs/02 haftalik kirayi "baskinin
            // metronomu" diye tanimliyor ve o metronom ekranda yoktu:
            // para yedinci gun kasadan cikiyor, oyuncu ancak sayi
            // dustukten sonra fark ediyordu.
            _rent = Theme.Text("", Theme.FontSmall, Theme.InkDim);
            _rent.style.marginLeft = Theme.Pad;
            row.Add(_rent);

            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            spacer.pickingMode = PickingMode.Ignore;
            row.Add(spacer);

            // --- SAG: iki kaynak, iki kapsul ----------------------------
            //
            // Masa ve kadro sayilari BURADAN KALKTI. Ikisi de sabahin
            // kararlari ve kendi ekranlarinda zaten yaziyor; ust serit
            // her an bakilan yer ve orada yalnizca SIK DEGISEN seyler
            // durmali. Alanlar duruyor (Tick onlari yaziyor), yalnizca
            // ekranda yer kaplamiyorlar.
            _cash = Theme.Text("", Theme.FontBody);
            VisualElement kasa = Kit.Pill(Icons.Coin(22f), _cash);
            if (App.Sim.HasLoan || App.Sim.Cash < App.Sim.WeeklyFixedCost() * 2)
                kasa.Add(Kit.PillAction(() => Ui.Push(new LoanScreen()),
                                        Loc.T("ui.morning.loan")));
            row.Add(kasa);

            _rep = Theme.Text("", Theme.FontBody);
            row.Add(Kit.Pill(Icons.Gem(22f), _rep));

            Button menu = new Button(() => { Sfx.Click(); Ui.Push(new PauseScreen()); });
            menu.text = string.Empty;
            // Metinsiz dugmenin bir ADI ve bir aciklamasi olmali: ekran
            // okuyucu ve kendi kendine gezen tur ikisi de metne bakiyor.
            menu.name = "menu";
            menu.tooltip = Loc.T("ui.hud.menu");
            menu.Add(Icons.Gear(Theme.Ink, 22f));
            menu.style.alignItems = Align.Center;
            menu.style.justifyContent = Justify.Center;
            // TAM dokunma hedefi ve kenardan uzak.
            //
            // Once 44 dp yuksekliginde ve sag kenara 16 dp mesafedeydi:
            // ekrandaki en kucuk hedef, en uzak kosede ve centigin
            // dustugu yerde. Oyundan cikmanin tek kapisi bu dugme.
            // MENU DUGMESI KAPSULDEN BUYUK: oyundan cikmanin tek
            // kapisi ve dokunma hedefi 48 dp'nin altina inmemeli.
            menu.style.minHeight = Theme.Touch - 4;
            menu.style.height = Theme.Touch - 4;
            menu.style.width = Theme.Touch;
            menu.style.marginLeft = Theme.Gap;
            menu.style.marginRight = 0;
            menu.style.marginTop = 0;
            menu.style.marginBottom = 0;
            menu.style.paddingLeft = 0;
            menu.style.paddingRight = 0;
            menu.style.backgroundColor = Kit.CardBg;
            menu.style.borderTopWidth = 1;
            menu.style.borderBottomWidth = 1;
            menu.style.borderLeftWidth = 1;
            menu.style.borderRightWidth = 1;
            menu.style.borderTopColor = Kit.CardLine;
            menu.style.borderBottomColor = Kit.CardLine;
            menu.style.borderLeftColor = Kit.CardLine;
            menu.style.borderRightColor = Kit.CardLine;
            Theme.Round(menu, (Theme.Touch - 4) * 0.5f);
            Theme.PressFx(menu, Kit.CardBg);
            row.Add(menu);

            bar.Add(row);
            return bar;
        }

        // =====================================================================
        /// <summary>
        /// YAN KARTLAR: solda gunun listesi, sagda iki durum karti.
        ///
        /// Referansin "Gunluk Hedefler" paneli ile "Saatlik Gelir /
        /// Musteri Memnuniyeti" kartlarinin karsiligi. Ikisi de UYDURMA
        /// DEGIL - oyunda zaten vardi ama gorulmuyordu:
        ///
        ///   - Sabahin hazirlik ozeti (menu, stok, asci) alt seritte tek
        ///     satirlik 14 dp'lik gri bir yaziydi; oysa servisi acmadan
        ///     once okunmasi gereken TEK sey oydu.
        ///   - Ciro ve memnuniyet YALNIZCA aksam raporunda goruunuyordu;
        ///     oyuncu gun boyunca nasil gittigini goremiyordu.
        /// </summary>
        private void BuildCards()
        {
            if (_cards == null || _stats == null || App.Sim == null) return;
            _builtCards = App.Sim.Phase;
            _cards.Clear();
            _stats.Clear();
            _servedValue = _angryValue = _occupiedValue = _turnedValue = null;
            _revenue = _satisfaction = null;

            if (_builtCards == DayPhase.Morning)
            {
                VisualElement body;
                VisualElement kart = Kit.Card(Loc.T("ui.morning.checklist"), out body);
                int yemek, kapsamaBp, asci;
                Readiness(out yemek, out kapsamaBp, out asci);
                body.Add(Kit.CheckRow(yemek > 0,
                    Loc.T("ui.morning.ready_menu", yemek)));
                // EKSIK VARSA SAYI, TAMSA SADE CUMLE.
                //
                // Ilk hali her iki durumda da kisi sayisi yaziyordu
                // ("Stok bugune yetiyor (~13 kisi)") ve tur onu
                // KIRPILMIS olarak yakaladi - iki dilde birden.
                // Yesil tikin yaninda sayi zaten bilgi tasimiyor;
                // tasidigi yer EKSIK olan durum, ve orada yuzde
                // yerine KISI yaziyor: "8 / 13" dogrudan ne kadar
                // malzeme eksik oldugunu soyluyor.
                bool stokTam = kapsamaBp >= Lokanta.Core.Fx.One;
                int bekleniyor = App.Sim.ExpectedPeopleToday();
                body.Add(Kit.CheckRow(stokTam,
                    stokTam
                        ? Loc.T("ui.morning.ready_stock_ok")
                        : Loc.T("ui.morning.ready_stock_part",
                                (int)Lokanta.Core.Fx.Bp(bekleniyor, kapsamaBp),
                                bekleniyor)));
                body.Add(Kit.CheckRow(asci > 0,
                    Loc.T("ui.morning.ready_cooks", asci)));
                _cards.Add(kart);
            }
            else if (_builtCards == DayPhase.Service)
            {
                VisualElement body;
                VisualElement kart = Kit.Card(Loc.T("ui.hud.today"), out body);

                _shownServed = _shownAngry = _shownOccupied = -1;
                // RENK ARTIK Kit.CountRow'DAN GELIYOR.
                //
                // Burasi `Theme.Ink` veriyordu ve kartin zemini
                // `Theme.Plate`: kontrast 1,08:1 - uc sayi da krem
                // uzerinde beyazdi. Renk secimini cagirana birakan
                // imza, hatanin kendisiydi.
                _servedValue = Theme.Text("", Theme.FontSmall, Theme.PlateInk);
                _angryValue = Theme.Text("", Theme.FontSmall, Theme.PlateInk);
                _occupiedValue = Theme.Text("", Theme.FontSmall, Theme.PlateInk);
                body.Add(Kit.CountRow(Theme.Dot(Kit.GoDeep, 8f),
                                      Loc.T("ui.hud.served"), _servedValue));
                // SAYI TOPLAM, o yuzden adi da toplamin adi.
                body.Add(Kit.CountRow(Theme.Dot(Kit.BadDeep, 8f),
                                      Loc.T("ui.evening.lost"), _angryValue));

                // KAPIDAN DONEN ARTIK SERVIS SIRASINDA DA GORUNUYOR.
                //
                // Ilk haftanin en sik olum bicimi bu: stok ya da menu
                // yetmiyor, musteri iceri bile girmiyor. Sayi yalnizca
                // gun raporunda vardi, yani oyuncu onu ancak gun
                // bitince - duzeltmesi imkansizken - goruyordu.
                _turnedValue = Theme.Text("", Theme.FontSmall, Theme.PlateInk);
                body.Add(Kit.CountRow(Theme.Dot(Theme.Warn, 8f),
                                      Loc.T("ui.evening.turned_away"), _turnedValue));
                body.Add(Kit.CountRow(Theme.Dot(Theme.Accent, 8f),
                                      Loc.T("ui.hud.tables"), _occupiedValue));
                _cards.Add(kart);
            }

            // Durum kartlari SABAH YOK: ciro sifir, memnuniyet sifir.
            // Sifir gosteren bir kart bilgi degil gurultu.
            if (_builtCards != DayPhase.Morning)
            {
                _revenue = Theme.Text("", 21, Theme.Ink);
                VisualElement ciro = Kit.Stat(Icons.Coin(20f),
                                              Loc.T("ui.evening.revenue"), _revenue);
                ciro.style.marginBottom = Theme.Gap;
                _stats.Add(ciro);

                _satisfaction = Theme.Text("", 21, Theme.Ink);
                _stats.Add(Kit.Stat(Icons.People(Theme.InkDim, 20f),
                                    Loc.T("ui.hud.satisfaction"), _satisfaction));
                _shownRevenueDay = -1;
            }
        }

        private Label _servedValue, _angryValue, _occupiedValue, _turnedValue;

        // =====================================================================
        /// <summary>
        /// ALT SERIT: SOLDA SIMGELI DUGMELER, SAGDA TEK YESIL EYLEM.
        ///
        /// Eski serit tam genislikte koyu bir kutuydu ve icinde yedi
        /// esdeger gri dugme yan yana duruyordu: "Hal, Menu, Kadro,
        /// Ekipman, Kredi, Servisi Ac". Hepsi ayni renk, ayni boy,
        /// ayni agirlik - yani oyuncuya "once neyi yapmaliyim"
        /// sorusunun cevabini veren hicbir sey yoktu.
        ///
        /// Referansin duzeni bu soruyu duzenin KENDISIYLE cevapliyor:
        /// sol taraf GIRILEN YERLER (dort ekran, simge + etiket), sag
        /// taraf ise gunun TEK ILERLETME eylemi - buyuk, yesil ve
        /// yalniz.
        ///
        /// Serit zemini kalkti; dugmeler dogrudan salonun uzerinde
        /// duruyor ve aralarindaki bosluktan salon goruunuyor.
        /// </summary>
        private void BuildBottom()
        {
            _bottom.Clear();
            _builtPhase = App.Sim.Phase;
            _builtServiceDone = _builtPhase == DayPhase.Service
                                && App.Sim.ServiceComplete;

            VisualElement bar = new VisualElement();
            bar.style.paddingLeft = Theme.Pad;
            bar.style.paddingRight = Theme.Pad;
            bar.style.paddingTop = 6;
            bar.style.paddingBottom = Theme.Gap;
            bar.pickingMode = PickingMode.Ignore;

            // KRIZ SERIDI: yalnizca kriz VARKEN.
            //
            // Oyunun tek vaadi "servis sirasinda yalnizca krizlere
            // mudahale edersin" ve kriz icin tek sinyal masa ustundeki
            // ~50x8 dp'lik rozetin kirmiziya donmesiydi. Serit kriz
            // yokken HIC KURULMUYOR: ortaya cikmasinin kendisi sinyal.
            if (_builtPhase == DayPhase.Service)
            {
                VisualElement kriz = CrisisBar();
                if (kriz != null) bar.Add(kriz);
            }

            VisualElement row = Theme.Row(Theme.Gap);
            row.style.alignItems = Align.FlexEnd;
            row.pickingMode = PickingMode.Ignore;

            switch (_builtPhase)
            {
                case DayPhase.Morning: MorningBar(row); break;
                case DayPhase.Service: ServiceBar(row); break;
                default: EveningBar(row); break;
            }

            bar.Add(row);
            _bottom.Add(bar);
        }

        /// <summary>Sol grubu sagdaki eylemden ayiran esnek bosluk.</summary>
        private static void Spacer(VisualElement row)
        {
            VisualElement v = new VisualElement();
            v.style.flexGrow = 1;
            v.pickingMode = PickingMode.Ignore;
            row.Add(v);
        }

        /// <summary>
        /// SABAH: dort ekran solda, servisi acmak sagda.
        ///
        /// Dort ekran (Hal, Menu, Kadro, Ekipman) sabahin butun
        /// kararlari; her biri bir SIMGE ve bir etiket tasiyor. Yalniz
        /// simge olmaz - bir sepet simgesi "Hal" mi "Magaza" mi demek,
        /// oyuncu bilemez; yalniz yazi da olmaz, cunku dort gri
        /// dikdortgen birbirinden ancak okunarak ayrilir.
        ///
        /// KREDI ARTIK BURADA DEGIL: kasa kapsulunun yanindaki yesil
        /// "+" dugmesi onu aciyor - para gereken yerde para dugmesi.
        /// Serit bir dugme daha hafifledi.
        /// </summary>
        private void MorningBar(VisualElement row)
        {
            row.Add(Kit.IconButton(Icons.Cart(Color.white), Loc.T("ui.morning.market"),
                                   () => Ui.Push(new MarketScreen()), mavi: true));
            row.Add(Kit.IconButton(Icons.List(Color.white), Loc.T("ui.morning.menu"),
                                   () => Ui.Push(new MenuBoardScreen()), mavi: true));
            row.Add(Kit.IconButton(Icons.Hat(Color.white), Loc.T("ui.morning.staff"),
                                   () => Ui.Push(new StaffScreen()), mavi: true));
            row.Add(Kit.IconButton(Icons.ArrowUp(Color.white), Loc.T("ui.morning.equipment"),
                                   () => Ui.Push(new EquipmentScreen()), mavi: true));
            Spacer(row);

            // Servisi acmak GERI DONUSU OLMAYAN tek karar.
            //
            // Alt satir ne olacagini soyluyor; eksik varsa o eksigi
            // soyluyor. Once bu uyari yalnizca BASILDIKTAN sonra bir
            // balon olarak cikiyordu - yani oyuncu once basiyor, sonra
            // ogreniyordu.
            string eksik;
            bool hazir = ReadyToOpen(out eksik);
            row.Add(Kit.Cta(Loc.T("ui.morning.open"),
                            hazir ? Loc.T("ui.morning.open_sub", App.Sim.Day) : eksik,
                            () =>
            {
                // EKSIK VARSA ONCE SORUYOR.
                //
                // Onay yalnizca eksik varken: her sabah "emin misin"
                // sormak, onayi okunmayan bir refleks haline getirirdi.
                string e2;
                if (!ReadyToOpen(out e2) && !_openConfirmed)
                {
                    _openConfirmed = true;
                    Toast(e2, rejected: true);
                    Sfx.Cancel();
                    BuildBottom();
                    return;
                }
                _openConfirmed = false;
                App.OpenService();
                BuildBottom();
                BuildCards();
            }, hazir));
        }

        /// <summary>
        /// SABAHIN UC SAYISI: menude kac yemek, stok kac gun, kac asci.
        ///
        /// Once bunlari ReadinessRow hesapliyor ve HEMEN yaziya
        /// ceviriyordu; kontrol listesi kartina tasininca ayni sayilar
        /// iki yerde gerekti. Hesap bir yerde, gorunum iki yerde.
        ///
        /// ACIK OLANLAR SAYILIYOR. IsOnMenu kilitli yemekler icin de
        /// true donebiliyor (menu anahtari ile acilma gunu ayri seyler)
        /// ve ilk gun "Menude 32 yemek" yaziyordu - oysa yalnizca
        /// altisi yapilabilir durumda.
        /// </summary>
        private void Readiness(out int menude, out int kapsamaBp, out int asci)
        {
            Simulation sim = App.Sim;
            menude = 0;
            for (int i = 0; i < sim.DishCount; i++)
                if (sim.IsOnMenu(i) && sim.IsUnlocked(i)) menude++;

            // STOK SATIRI ARTIK GUNU OLCUYOR, YEMEK SAYISINI DEGIL.
            //
            // Once "kac yemek yapilabiliyor" sayiliyordu ve tik
            // >= 1 ile yesile donuyordu: alti yemegin her birinden
            // BIRER porsiyonu olan oyuncu "hazir" gorunup servisi
            // aciyor, ilk on dakikada mal bitiyordu. Tik, olcmesi
            // gereken seyi olcmuyordu.
            kapsamaBp = sim.StockCoverageBp();
            asci = sim.Cooks;
        }

        /// <summary>Servisi acmaya hazir miyiz. Degilse dugme soruyor.</summary>
        private bool ReadyToOpen(out string eksik)
        {
            Simulation sim = App.Sim;
            eksik = null;

            int menude = 0;
            for (int i = 0; i < sim.DishCount; i++)
                if (sim.IsOnMenu(i) && sim.IsUnlocked(i)) menude++;

            if (menude == 0) eksik = Loc.T("ui.morning.ready_warn_menu");
            else if (sim.Cooks <= 0) eksik = Loc.T("ui.morning.ready_warn_cook");
            else if (sim.MakeableDishCount() < 1) eksik = Loc.T("ui.morning.ready_warn_stock");

            return eksik == null;
        }

        // =====================================================================
        /// <summary>
        /// Mudahalenin gidecegi masa: SECILI olan, yoksa sabri en az kalan.
        ///
        /// Once secim diye bir sey yoktu ve hedefi her zaman oyun
        /// seciyordu. Yani servis sirasinda oyuncunun tek karari "simdi
        /// mi, sonra mi" idi - KIME sorusunu oyun cevapliyordu ve patron
        /// olmanin butun mekanigi bir zamanlama dugmesine inmisti.
        ///
        /// Secim ZORUNLU degil: yakinlasmayan oyuncu icin eski davranis
        /// aynen duruyor, yani oyun secim yapmayani cezalandirmiyor.
        /// Secim yapan, sabri en az olani DEGIL en cok kazandiracak
        /// olani secebiliyor - ornegin sabri biraz daha fazla ama
        /// kalabalik olan masayi.
        /// </summary>
        private int Target()
        {
            // MASA -> GRUP cevirisi SART. Intervene grup bekliyor;
            // ValidSelection masa donuyor. Ceviri olmadan ikram
            // bambaska bir masaya gidiyordu.
            int sel = App.ValidSelection();
            if (sel < 0) return App.Sim.MostImpatientParty();

            int party = App.Sim.PartyAtTable(sel);
            return party >= 0 ? party : App.Sim.MostImpatientParty();
        }

        /// <summary>
        /// Mudahalenin gerceklesip gerceklesmedigini HAK SAYISINDAN
        /// anlar ve olmadiysa soyler.
        ///
        /// Once uc dugme de komutu gonderip KOSULSUZ "oldu" diyordu.
        /// Cekirdek ise dort ayri yerde sessizce reddediyor: hak bitti,
        /// grup artik yok, cay parasi kasada yok, istasyonda is yok.
        /// Oyuncu "cay ikram edildi" yazisini okuyor, sagdaki "Hak 4"
        /// hic degismiyor ve neyin yanlis gittigini hicbir yerden
        /// ogrenemiyordu.
        /// </summary>
        private bool DidIntervene(int before)
        {
            if (App.Sim.InterventionsLeft < before) return true;
            Toast(Loc.T("notice.rejected"), rejected: true);
            Sfx.Cancel();
            return false;
        }

        /// <summary>
        /// Dugme yazisi, secim varken masayi soyluyor. Oyuncu neye
        /// bastigini dugmenin USTUNDE gormeli - yaptiktan sonra
        /// bildirimde degil.
        /// </summary>
        /// <summary>
        /// Mudahalelerin SU ANKI HEDEFI - dugme yazisinda degil, kendi
        /// rozetinde.
        ///
        /// Hedef bir sure her iki dugmenin yazisina ekleniyordu
        /// ("Cay > sabirsiz", "Ilgi > sabirsiz"). Iki bedeli vardi:
        ///
        ///   1. AYNI BILGI IKI KEZ. Iki dugme de ayni hedefe gidiyor.
        ///   2. Dize uzunlugu DILE bagliydi: Ingilizce "most impatient"
        ///      Turkce "sabirsiz"dan uzun. Serit onceden de bir kez
        ///      tasmisti ve ek "en sabirsiz"dan tek kelimeye
        ///      indirilmisti - yani bu duvara ikinci kez carpiliyordu.
        ///
        /// Olculdu: ek kalkinca uc dugmenin kirpilmasi bitti. Hedef
        /// artik tek bir rozette ve dugmeler yalnizca FIILI soyluyor -
        /// bir dugmenin isi zaten odur.
        /// </summary>
        private VisualElement TargetBadge()
        {
            int sel = App.ValidSelection();
            string metin = sel >= 0
                ? "› " + (sel + 1)
                : "› " + Loc.T("ui.service.target_auto");

            Label l = Theme.Text(metin, Theme.FontSmall, Theme.InkDim);
            l.style.flexShrink = 0;
            l.style.marginLeft = 2;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            return l;
        }

        /// <summary>
        /// Bildirim MASA numarasini yaziyor, grup indisini degil.
        ///
        /// Once grup indisi "masa numarasi" diye basiliyordu ve dort
        /// masali bir dukkanda "17. masaya cay ikram edildi" cikabiliyordu.
        /// </summary>
        private string TargetToast(string ne)
        {
            // ". masaya " BIR TURKCE SIRA EKIYDI.
            //
            // Once bildirim kodda birlestiriliyordu:
            // (sel+1) + ". masaya " + "çay ikram edildi". Ingilizce
            // oynayan biri tam olarak bunu okuyordu, ve kalip
            // cevrilse bile calismazdi - masa numarasi Ingilizce'de
            // cumlenin SONUNA gidiyor. Tam cumle artik tabloda ve
            // numara bir yer tutucu.
            int sel = App.ValidSelection();
            return sel >= 0
                ? Loc.T("ui.service.done_" + ne, sel + 1)
                : Loc.T("ui.service.done_" + ne + "_any");
        }

        /// <summary>
        /// Sabri kritige inen masalarin cipleri. Kriz yoksa null.
        ///
        /// Cipe dokunmak o masayi SECIYOR - yani hedef secme yolu da
        /// burada cozuluyor. Onceden hedef secmek icin once salona
        /// yakinlasip sonra masaya dokunmak gerekiyordu; bu, ogretilmesi
        /// gereken iki adimli bir jestti ve kriz anindaki bir oyuncu onu
        /// yapmiyordu.
        /// </summary>
        private VisualElement CrisisBar()
        {
            Simulation sim = App.Sim;
            int esik = sim.PatienceWarnBp;

            // En sabirsizdan basla: ciplerin sirasi da bilgi.
            var kritik = new System.Collections.Generic.List<int>();
            for (int t = 0; t < sim.TableCount; t++)
            {
                CustomerStage st = sim.TableStage(t);
                if (st == CustomerStage.None || st == CustomerStage.Done
                    || st == CustomerStage.LeftAngry) continue;
                int bp = sim.TablePatienceBp(t);
                if (bp > esik || bp <= 0) continue;
                kritik.Add(t);
            }
            if (kritik.Count == 0) { _crisisShown = 0; return null; }

            kritik.Sort((a, b) => sim.TablePatienceBp(a).CompareTo(sim.TablePatienceBp(b)));

            // SES YALNIZCA YENI KRIZDE. Her karede calan bir uyari,
            // uyari olmaktan cikip gurultu olur.
            if (kritik.Count > _crisisShown) Sfx.Upset();
            _crisisShown = kritik.Count;

            // SERIT GERCEKTEN KURULDU.
            //
            // Tur bugune kadar "kriz gorundu mu" sorusunu SIMULASYONA
            // soruyordu (CrisisTables) - yani ekranda hicbir sey
            // olmasa da yesil kaliyordu. Sayac tam seridin kuruldugu
            // yerde duruyor: oyuncunun gordugu sey budur.
            CrisisBuilds++;

            VisualElement row = Theme.Row(6);
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;

            Label uyari = Theme.Text(Loc.T("ui.service.crisis"), Theme.FontSmall, Theme.Bad);
            uyari.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(uyari);

            int n = kritik.Count < 5 ? kritik.Count : 5;
            for (int k = 0; k < n; k++)
            {
                int masa = kritik[k];
                bool secili = App.SelectedTable == masa;

                Button cip = Theme.Btn(
                    (secili ? "› " : "")
                    + Loc.T("ui.service.table_n", masa + 1) + " "
                    + Loc.Percent(sim.TablePatienceBp(masa)),
                    () =>
                    {
                        App.SelectedTable = masa;
                        Sfx.Click();
                        Ui.Refresh();
                    });

                // SECIM YALNIZCA RENKLE OLMAZ.
                //
                // Once tek ayirt edici yazi rengiydi (kirmizi <-> krem);
                // kizil-yesil korlugunde ikisi de benzer parlaklikta
                // okunuyor ve mudahale BU secime gidiyor. Ikinci kanal:
                // isaret (›) ve cerceve.
                cip.style.color = secili ? Theme.Ink : Theme.Bad;
                cip.style.borderTopWidth = secili ? 2 : 0;
                cip.style.borderBottomWidth = secili ? 2 : 0;
                cip.style.borderLeftWidth = secili ? 2 : 0;
                cip.style.borderRightWidth = secili ? 2 : 0;
                if (secili)
                {
                    cip.style.borderTopColor = Theme.Accent;
                    cip.style.borderBottomColor = Theme.Accent;
                    cip.style.borderLeftColor = Theme.Accent;
                    cip.style.borderRightColor = Theme.Accent;
                }
                row.Add(cip);
            }
            return row;
        }

        private int _crisisShown;

        /// <summary>Kriz seridinin kac kez KURULDUGU. Turun sorabilmesi icin.</summary>
        public static int CrisisBuilds { get; private set; }

        /// <summary>Alt cubugun en son kuruldugu kritik masa sayisi.</summary>
        private int _builtCrisis = -1;

        /// <summary>Eksik varken ilk dokunus soruyor, ikincisi aciyor.</summary>
        private bool _openConfirmed;

        /// <summary>
        /// Su an kriz seridinde kac masa var. Turun sorabilmesi icin.
        ///
        /// "Kriz gorunuyor mu" ancak KRIZ VARKEN sorulabilir; sayiyi
        /// disari vermeden denetim, seridin hic kurulmadigi bir serviste
        /// de yesil kalirdi.
        /// </summary>
        public int CrisisTables
        {
            get
            {
                Simulation sim = App != null ? App.Sim : null;
                if (sim == null) return 0;
                int esik = sim.PatienceWarnBp, n = 0;
                for (int t = 0; t < sim.TableCount; t++)
                {
                    CustomerStage st = sim.TableStage(t);
                    if (st == CustomerStage.None || st == CustomerStage.Done
                        || st == CustomerStage.LeftAngry) continue;
                    int bp = sim.TablePatienceBp(t);
                    if (bp > esik || bp <= 0) continue;
                    n++;
                }
                return n;
            }
        }

        /// <summary>
        /// SERVIS: kip dugmeleri, mudahaleler, gunu kapat.
        ///
        /// Duzen uc gruba ayriliyor ve gruplar oyunun kendi ayrimini
        /// tasiyor:
        ///
        ///   KIP        - duraklat ve hiz. Oyunun saatini ayarliyorlar,
        ///                salonda hicbir sey degistirmiyorlar.
        ///   MUDAHALE   - oyunun butun fiil kumesi, ortak bir kutuda ve
        ///                kalan hak o kutuya yapisik: uc dugmeyi de ayni
        ///                kese besliyor.
        ///   ILERLET    - gunu kapat, sagda ve yalniz.
        /// </summary>
        private void ServiceBar(VisualElement row)
        {
            // DURAKLAT SIMGEYE DONDU.
            //
            // Kip dugmeleri metinle yazildiginda mudahalelerden ayirt
            // edilemiyordu; ikisi de gri dikdortgendi. Simge onlari
            // siluetiyle ayiriyor ve yer de kazandiriyor.
            //
            // Dugmenin bir ADI var: metinsiz bir dugmeyi ne ekran
            // okuyucu ne de kendi kendine gezen tur metinden bulabilir.
            Button pause = Kit.IconButton(
                App.Paused ? Icons.Play(Theme.Ink, 20f) : Icons.Pause(Theme.Ink, 20f),
                App.Paused ? Loc.T("ui.hud.resume") : Loc.T("ui.hud.pause"),
                () => { App.Paused = !App.Paused; BuildBottom(); });
            pause.name = "pause";
            row.Add(pause);

            // HIZ TEK DUGME, DONGUSEL.
            //
            // Once uc ogeydi: eksi, arti ve aralarinda bir etiket - 148 dp
            // ve dokuz ogeli bir seritte uc yer. Hiz gun icinde bir kez
            // ayarlanan bir sey. Dongu x0,5'e dondugu icin geri gitmek de
            // mumkun.
            float mult = App.TimeScale / GameApp.BaseTimeScale;
            Button hiz = Kit.IconButton(null,
                "×" + mult.ToString("0.#", Loc.Culture),
                () =>
                {
                    float next = App.TimeScale * 2f;
                    if (next > GameApp.BaseTimeScale * 16f + 0.01f)
                        next = GameApp.BaseTimeScale * 0.5f;
                    App.TimeScale = next;
                    BuildBottom();
                });
            hiz.name = "hiz";
            row.Add(hiz);

            // --- mudahaleler TEK KUTUDA --------------------------------
            int left = App.Sim.InterventionsLeft;
            bool done = App.Sim.ServiceComplete;

            VisualElement kutu = Theme.Row(6);
            kutu.style.flexGrow = 1;
            // Mudahale kutusu YERINI VERIYOR: yanindaki imza dugmesi ve
            // "Gunu kapat" sabit genislikte, kutu ise uc dugme tasiyor
            // ve daralabilir. Daralmazsa tasip ust uste biniyor.
            kutu.style.flexShrink = 1;
            // minWidth SIFIRA CEKILMIYOR.
            //
            // Bir kez oyle yazildi ve daralma dugmeleri YAZILARININ
            // ALTINA sikistirdi: olculdu, "Cay" ve "Tea" bile kirpildi.
            // Kutu yerini verebilir ama icindeki dokunma hedefleri
            // (Theme.Touch) korunmali - daralan bir serit, basilamayan
            // bir dugmeden iyidir ama okunamayan bir dugmeden degil.
            kutu.style.backgroundColor = Kit.CardBg;
            kutu.style.borderTopWidth = 1;
            kutu.style.borderBottomWidth = 1;
            kutu.style.borderLeftWidth = 1;
            kutu.style.borderRightWidth = 1;
            kutu.style.borderTopColor = Kit.CardLine;
            kutu.style.borderBottomColor = Kit.CardLine;
            kutu.style.borderLeftColor = Kit.CardLine;
            kutu.style.borderRightColor = Kit.CardLine;
            kutu.style.paddingLeft = 7;
            kutu.style.paddingRight = 7;
            kutu.style.paddingTop = 6;
            kutu.style.paddingBottom = 6;
            kutu.style.alignItems = Align.Center;
            Theme.Round(kutu, Kit.CardRadius);

            // ISTASYON ADI DUGMEDE DEGIL.
            //
            // Bir sure "Mutfagi hizlandir > Milkshake Makinesi" yaziyordu
            // ve OLCULDU: Rubik'in gercek harf genislikleriyle o tek
            // dugme 319 dp, serit toplami 1103 dp - 873 dp'lik ekranda
            // 230 dp tasma.
            Button rush = Theme.Btn(Loc.T("ui.service.rush"), () =>
            {
                int st = App.Sim.BusiestStation();
                if (st < 0) { Toast(Loc.T("ui.service.none_station"), rejected: true); Sfx.Cancel(); return; }
                int before = App.Sim.InterventionsLeft;
                App.Send(CommandKind.Intervene, st, (int)InterventionKind.RushStation);
                if (DidIntervene(before))
                    Toast(Loc.T("ui.service.done_rush",
                                Loc.T("station." + App.Content.Stations[st].Id)));
                BuildBottom();
            }, wide: true);
            rush.SetEnabled(left > 0 && !done);
            kutu.Add(rush);

            // CAY HEDEF SECMIYOR: SALONA GIDIYOR.
            //
            // Eskiden secili masaya gidiyordu ve o hali cayi olu bir
            // dugme yapiyordu - ilgi her eksende ustundu. Artik cay
            // BEKLEYEN HERKESE gidiyor, yani "bir masa krizde" degil
            // "salonun tamami sabirsiz" sorusunun cevabi. Hedef
            // secilmemis olmasi da hata degil.
            Button tea = Theme.Btn(Loc.T("ui.service.tea"), () =>
            {
                int before = App.Sim.InterventionsLeft;
                App.Send(CommandKind.Intervene, Target(), (int)InterventionKind.FreeTea);
                if (DidIntervene(before)) Toast(Loc.T("ui.service.done_tea_room"));
                else { Toast(Loc.T("ui.service.none_waiting"), rejected: true); Sfx.Cancel(); }
                BuildBottom();
            }, wide: true);
            // Bekleyen yoksa cay da yok: gonderilecek kimse olmadan
            // hak yakmanin anlami olmazdi.
            tea.SetEnabled(left > 0 && !done && App.Sim.WaitingParties > 0);
            kutu.Add(tea);

            Button care = Theme.Btn(Loc.T("ui.service.attention"), () =>
            {
                int p = Target();
                if (p < 0) { Toast(Loc.T("ui.service.none_table"), rejected: true); Sfx.Cancel(); return; }
                int before = App.Sim.InterventionsLeft;
                App.Send(CommandKind.Intervene, p, (int)InterventionKind.OwnerAttention);
                if (DidIntervene(before)) Toast(TargetToast("care"));
                BuildBottom();
            }, wide: true);
            care.SetEnabled(left > 0 && !done);
            kutu.Add(care);

            // KALAN HAK: SAYI DEGIL NOKTA.
            //
            // "Hak 4" yazisi 14 dp InkDim idi - ekrandaki en az goze
            // carpan oge, ve uc fiilin hepsini kapatan kit kaynak oydu.
            // UC MUDAHALE DUGMESI DAR DOLGULU.
            //
            // Varsayilan yatay dolgu (Theme.Pad) uc dugmede altmis dp
            // tutuyor ve serit tam o kadar tasiyordu: Turk mutfaginda
            // on dort masayla, yani veresiye dugmesinin de bulundugu
            // halde, ucu birden kirpiliyordu. Dokunma hedefi dolgudan
            // degil `minWidth`'ten geliyor, yani daraltmak basilabilirligi
            // bozmuyor.
            foreach (Button d in new[] { rush, tea, care })
            {
                d.style.paddingLeft = 8;
                d.style.paddingRight = 8;
            }

            // HEDEF ROZETI: iki dugmenin yazisindan cikti, buraya geldi.
            kutu.Add(TargetBadge());

            VisualElement pips = Theme.Row(0);
            pips.style.alignItems = Align.Center;
            pips.style.marginLeft = 4;
            pips.style.marginRight = 2;
            for (int i = 0; i < App.Sim.InterventionsPerDay; i++)
                pips.Add(Theme.Dot(i < left ? Theme.Accent : Theme.Line, 9f));
            kutu.Add(pips);
            row.Add(kutu);

            // --- imza mekanigi: mutfaga gore biri ya da otekisi --------
            if (App.Sim.HasCombo)
            {
                Button combo = Kit.IconButton(
                    Icons.Combo(App.Sim.ComboEnabled ? Kit.Go : Theme.InkDim, 20f),
                    App.Sim.ComboEnabled ? Loc.T("ui.service.combo_on")
                                         : Loc.T("ui.service.combo_off"),
                    () =>
                    {
                        App.Send(CommandKind.SetCombo, App.Sim.ComboEnabled ? 0 : 1);
                        BuildBottom();
                    });
                combo.style.flexShrink = 1;
                row.Add(combo);
            }
            if (App.Sim.HasCredit)
            {
                int asker = FirstCreditAsker();
                Button credit = Kit.IconButton(
                    Icons.Book(asker >= 0 ? Theme.Accent : Theme.InkDim, 20f),
                    Loc.T("ui.service.credit"),
                    () =>
                    {
                        int p = FirstCreditAsker();
                        if (p < 0) { Toast(Loc.T("ui.service.none_credit"), rejected: true); Sfx.Cancel(); return; }
                        App.Send(CommandKind.ExtendCredit, p);
                        Toast(Loc.T("ui.service.done_credit", RegularName(p)));
                        BuildBottom();
                    });
                credit.SetEnabled(asker >= 0);
                credit.style.flexShrink = 1;
                row.Add(credit);
            }

            Spacer(row);

            // Servis bittiginde "Gunu Kapat" TEK ANLAMLI EYLEM: yesile
            // geciyor ve mudahale dugmeleri kapaniyor. Bitmis bir
            // servise cay ikram etmek diye bir sey yok.
            row.Add(Kit.Cta(Loc.T("ui.service.close"),
                            done ? Loc.T("ui.service.close_sub")
                                 : Loc.T("ui.service.running_sub"),
                            () =>
            {
                App.CloseDay();
                BuildBottom();
                BuildCards();
            }, done));
        }

        /// <summary>
        /// AKSAM: gunun ozeti solda, ertesi gun sagda.
        ///
        /// GUN RAPORU BIRINCIL DEGIL AMA KAYBOLMUS DA DEGIL: ozet
        /// satirinin kendisi artik bir KART ve rapora giden dugme onun
        /// yaninda duruyor. Once "Gun raporu" turuncu ve tam
        /// genislikteydi, "Ertesi gun" ise griydi - yani oyunun
        /// ilerleme dugmesi, bir okuma ekranindan daha sonuk
        /// gorunuyordu.
        /// </summary>
        private void EveningBar(VisualElement row)
        {
            // OZET DE BIR KART.
            //
            // Once dogrudan sahnenin uzerinde duran serbest yaziydi:
            // koyu bir salonun uzerinde koyu bir zemin, okunmasi
            // isiktan isiga degisiyordu. Ekrandaki her bilgi bir
            // yuzeyin uzerinde durmali.
            VisualElement kutu = Kit.Box();
            kutu.style.paddingLeft = 14;
            kutu.style.paddingRight = 14;
            kutu.style.paddingTop = 8;
            kutu.style.paddingBottom = 8;
            kutu.style.flexShrink = 1;
            kutu.Add(EveningSummary());
            row.Add(kutu);

            row.Add(Kit.IconButton(Icons.List(Color.white), Loc.T("ui.evening.title"),
                                   () => Ui.Push(new EveningScreen()), mavi: true));
            Spacer(row);
            row.Add(Kit.Cta(Loc.T("ui.evening.next"),
                            Loc.T("ui.evening.next_sub", App.Sim.Day + 1),
                            () =>
            {
                App.NextDay();
                BuildBottom();
                BuildCards();
            }));
        }

        /// <summary>Gunun ozet karti: aksam seridinin sol yarisi.</summary>
        private VisualElement EveningSummary()
        {
            VisualElement col = Theme.Column(Theme.Gap);
            DayReport r = App.Sim.BuildDayReport();

            // IKI MANSET BUYUK, GERISI AYNI SATIRDA KUCUK.
            //
            // Alti sayinin ALTISI da 26 dp kalin yaziliyordu ve aralarinda
            // yalnizca renk farki vardi. Iki sonucu birden dogurdu:
            //
            // 1. Manset kayboldu. Paletin en parlak rengi Warn (0,499
            //    parlaklik, Good 0,367'nin uzerinde), yani "Bozulan 245"
            //    goze aciklamasi gereken kardan daha cok carpiyordu.
            // 2. Serit sisti. Olculdu: aksam seridi 156 dp, ust seritle
            //    birlikte 228 dp - projenin kendi 220 dp butcesinin
            //    UZERINDE ve geriye kalan 165 dp, yine projenin kendi
            //    173 dp tabaninin ALTINDA.
            //
            // ILK DENEME DAHA KOTU YAPTI: ikincil sayilari ayri bir
            // satira alinca serit 269 dp'ye cikti. Denetim yeni
            // eklenmisti ve hatayi ayni turda yakaladi - bu satirlarin
            // sebebi o.
            //
            // Simdi hepsi TEK satirda: iki manset 26 dp, dort ikincil
            // sayi 14 dp ve etiketiyle yan yana. Hicbir sey silinmedi.
            VisualElement stats = Theme.Row(Theme.Pad);
            stats.style.justifyContent = Justify.SpaceAround;
            stats.style.alignItems = Align.Center;
            stats.style.flexWrap = Wrap.Wrap;

            // MANSET NET KAR, ciro degil.
            //
            // Ciro her zaman artiyor, kar artmiyor. Bir lokanta yonetim
            // oyununda en buyuk sayinin ciro olmasi yanlis mansetti.
            stats.Add(Stat(Loc.T("ui.evening.profit"), Loc.Money(r.NetProfit),
                           r.NetProfit >= 0 ? Theme.Good : Theme.Bad));
            if (App.HasYesterday)
                stats.Add(Delta(r.NetProfit - App.Yesterday.NetProfit, para: true));

            stats.Add(Stat(Loc.T("ui.hud.served"), r.ServedPeople.ToString(), Theme.Ink));
            if (App.HasYesterday)
                stats.Add(Delta(r.ServedPeople - App.Yesterday.ServedPeople, para: false));

            stats.Add(Small(Loc.T("ui.evening.revenue"),
                            Loc.Money(r.Revenue), Theme.InkDim));
            // SERIT TOPLAMI GOSTERIYOR, o yuzden adi da toplamin adi.
            stats.Add(Small(Loc.T("ui.evening.lost"), r.AngryParties.ToString(),
                            r.AngryParties > 0 ? Theme.Bad : Theme.InkDim));
            stats.Add(Small(Loc.T("ui.evening.satisfaction"),
                            Loc.Reputation(r.AverageSatisfactionCenti),
                            Theme.ReputationColor(r.AverageSatisfactionCenti)));

            // COPE GIDEN, AKSAM OZETINDE DE.
            //
            // Yalnizca "Gun Raporu" dugmesine basan oyuncu goruyordu ve
            // o ikincil bir ekran. Oysa bu, oyunun en buyuk gorunmez
            // gideri: iyi oynayan bir oyuncu altmis gunde aldigi
            // malzemenin onemli bir kismini cope atiyor ve kasanin neden
            // dolmadigini hicbir yerde bulamiyordu.
            if (r.SpoiledValue > 0)
                stats.Add(Small(Loc.T("ui.evening.spoiled"),
                                Loc.Money(r.SpoiledValue), Theme.Warn));

            col.Add(stats);

            // GUN RAPORU BIRINCIL, "ERTESI GUN" IKINCIL.
            //
            // Once tam tersiydi: rapor gri ve ikincil, "Ertesi gun"
            // turuncu ve birincildi. Altmis gunluk bir kampanyada
            // oyuncu altmis kez turuncuya basar ve copu, moralsiz
            // personeli, mudavim sahnelerini hic gormez - yani oyunun
            // ogretici tarafi hic acilmaz.
            return col;
        }

        /// <summary>
        /// DUNE GORE FARK.
        ///
        /// Bir sayinin tek basina anlami yok: "568" iyi mi kotu mu
        /// bilinmiyor. "568 (+127)" bir KARARIN sonucu. Ogrenme
        /// dongusunu kapatan sey bu.
        /// </summary>
        private static VisualElement Delta(long fark, bool para)
        {
            string yazi;
            Color renk;
            if (fark > 0) { yazi = "+" + (para ? Loc.Money(fark) : fark.ToString()); renk = Theme.Good; }
            else if (fark < 0) { yazi = (para ? Loc.Money(fark) : fark.ToString()); renk = Theme.Bad; }
            else { yazi = Loc.T("ui.evening.same"); renk = Theme.InkFaint; }

            Label l = Theme.Text(yazi, Theme.FontSmall, renk);
            l.style.marginLeft = -6;
            l.style.marginRight = 6;
            return l;
        }

        /// <summary>Ikincil sayi: etiket ve deger AYNI satirda, kucuk.</summary>
        private static VisualElement Small(string label, string value, Color color)
        {
            VisualElement v = Theme.Row(6);
            v.style.alignItems = Align.Center;
            v.Add(Theme.Text(label, Theme.FontSmall, Theme.InkFaint));
            Label val = Theme.Text(value, Theme.FontSmall, color);
            val.style.unityFontStyleAndWeight = FontStyle.Bold;
            v.Add(val);
            return v;
        }

        /// <summary>
        /// Manset sayi: etiket kucuk, deger buyuk, AYNI SATIRDA.
        ///
        /// Once iki satirdi (deger 26 dp, altinda etiket 14 dp) ve serit
        /// 156 dp tutuyordu - ust seritle birlikte 228, butce 220.
        /// Tek satira alinca ayni iki bilgi duruyor, serit 20 dp
        /// kuculuyor ve buyuk-kucuk kontrasti sayiyi zaten one cikariyor.
        /// </summary>
        private static VisualElement Stat(string label, string value, Color color)
        {
            VisualElement v = Theme.Row(6);
            v.style.alignItems = Align.Center;
            v.Add(Theme.Text(label, Theme.FontSmall, Theme.InkFaint));
            Label big = Theme.Text(value, Theme.FontTitle, color);
            big.style.unityFontStyleAndWeight = FontStyle.Bold;
            v.Add(big);
            return v;
        }

        // =====================================================================
        // Kopya kaldirildi: ayni dongu cekirdekte var ve "uygun mu"
        // tanimi tek yerde durmali.
        private int FirstCreditAsker() { return App.Sim.FirstCreditAsker(); }

        private string RegularName(int party)
        {
            int reg = App.Sim.PartyRegular(party);
            if (reg < 0) return Loc.T("ui.service.guest");
            return Loc.T(App.Content.Regulars[reg].NameKey);
        }

        /// <summary>
        /// Oyuncunun kendi eyleminin karsiligi. Simulasyon olaylari ayri
        /// bir yoldan geliyor (RebuildNotices).
        /// </summary>
        private void Toast(string text, bool rejected = false)
        {
            _ownText = text;
            _ownRejected = rejected;
            _ownLeft = 2.2f;
            RebuildNotices();
        }

        private string _ownText;
        private bool _ownRejected;
        private float _ownLeft;

        /// <summary>
        /// Bildirim seridini yeniden kurar: en ustte oyuncunun kendi
        /// eylemi, altinda simulasyondan gelen en son uc haber.
        ///
        /// Yeniden kurma YALNIZCA degisince: bir serit her karede
        /// yeniden kurulursa, ayrilan ogeler kare basina coplenir ve
        /// mobilde toplayiciyi tetikler.
        /// </summary>
        private void RebuildNotices()
        {
            // ILK KURULUMDA _toast HENUZ YOK.
            //
            // Build() once alt seridi kuruyor (BuildBottom) ve bildirim
            // alanini SONRA yaratiyor; BuildBottom buradan RebuildNotices
            // cagirinca _toast null oluyordu ve butun ekran agaci yarim
            // kaliyordu. Tur bunu bir NullReferenceException ile yakaladi
            // ve dokuz dakika takildi - cunku kirilan sey ekranin
            // KENDISIYDI, tek bir kontrol degil.
            if (_toast == null) return;

            _toast.Clear();

            // IPUCU EN USTTE ve balonlardan farkli: kendiliginden
            // kaybolmuyor, kapatilinca bir daha gelmiyor.
            Hints.Hint hint = Hints.Current(App.Sim);
            if (hint != null)
            {
                VisualElement strip = Hints.Strip(hint, App.Sim, RebuildNotices);
                if (strip != null) _toast.Add(strip);
            }

            // KABUL VE RED AYNI RENKTE OLAMAZ.
            //
            // Kendi eyleminin karsiligi kosulsuz Accent ile
            // yaziliyordu, yani "cay ikram edildi" ile "istek
            // reddedildi" ayni renkte, ayni yerde, ayni suredeydi.
            // Oyuncu kit bir mudahale hakkini harciyor ve gittigini mi
            // gitmedigini mi RENKTEN anlayamiyordu - tam da DidIntervene
            // yorumunun metinde cozdugu sorun, gorsel kanalda duruyordu.
            if (!string.IsNullOrEmpty(_ownText))
                _toast.Add(Bubble(_ownText, _ownRejected ? Theme.Bad : Theme.Accent));

            for (int i = 0; i < App.NoticeCount; i++)
                _toast.Add(Bubble(App.NoticeTextAt(i), ToneColor(App.NoticeToneAt(i))));
        }

        private static VisualElement Bubble(string text, Color color)
        {
            VisualElement box = Theme.PanelBox();
            box.style.backgroundColor = new Color(Theme.PanelHi.r, Theme.PanelHi.g,
                                                  Theme.PanelHi.b, 0.95f);
            box.style.paddingTop = Theme.Gap;
            box.style.paddingBottom = Theme.Gap;
            box.style.marginTop = 4;
            box.Add(Theme.Text(text, Theme.FontBody, color));
            return box;
        }

        private static Color ToneColor(NoticeTone tone)
        {
            switch (tone)
            {
                case NoticeTone.Good: return Theme.Good;
                case NoticeTone.Warn: return Theme.Warn;
                case NoticeTone.Bad:  return Theme.Bad;
                default:              return Theme.Ink;
            }
        }

        // =====================================================================
        // Son yazilan degerler. Ust serit YALNIZCA degisince yeniden
        // yaziliyor.
        //
        // Once her kare yazilyordu: alti alan, her biri birlestirme ve
        // sayi bicimleme, yani saniyede yaklasik dokuz yuz kucuk ayirma.
        // Degerler ayni kalsa bile metin URETILIYOR, cunku karsilastirma
        // ancak uretildikten sonra yapilabiliyor. Mobilde bu, birkac
        // saniyede bir cop toplama ve gorunur kare atlamasi demek.
        private int _shownDay = -1, _shownRep = -1;
        private float _shownProgress = -1f;
        private DayPhase _shownMeterPhase = (DayPhase)(-1);
        private int _shownRentDays = -1;
        private int _shownRepCap = -1;
        private long _shownCash = long.MinValue;

        // Akis satirinin BILESENLERI ayri tutuluyor: birlestirilmis
        // dizeyi karsilastirmak, once onu URETMEYI gerektiriyordu.
        private int _shownServed = -1, _shownAngry = -1, _shownOccupied = -1;
        private DayPhase _shownPhase = (DayPhase)(-1);

        /// <summary>Serit ServiceComplete durumunda mi kuruldu.</summary>
        private bool _builtServiceDone;

        private IVisualElementScheduledItem _cashRun;

        /// <summary>
        /// Kasa sayaci DEGERE ATLAMIYOR, sayarak gidiyor.
        ///
        /// Once deger aninda degisiyordu ve 1.630 ¤'luk bir kira
        /// tahsilati ile 12 ¤'luk bir satis ekranda AYNI seydi: sadece
        /// artik baska bir sayi. Bir yonetim oyununda kasa, geri
        /// bildirimin kendisi - sayarak gitmesi "bir sayi degisti"yi
        /// "bir sey kazandin"a ceviriyor.
        ///
        /// Kurallar:
        ///   - Kucuk fark (10 ¤ altinda) ya da ilk yazim SAYILMIYOR;
        ///     her kucuk satista titreyen bir sayac gurultudur.
        ///   - Sure 360 ms, yaklasik 11 kare. Daha kisasi 30 fps'te
        ///     birkac kareye dusuyor ve sayma gorunmuyor.
        ///   - Yalnizca METIN degisiyor; genislik zaten sabit degil ama
        ///     ust serit tek satir ve komsulari esnek, yani yerlesim
        ///     yeniden hesaplansa bile tek bir satirla sinirli.
        ///   - Yeni bir degisim gelirse eskisi IPTAL ediliyor, yoksa iki
        ///     sayac ayni etikete yaziyor.
        /// </summary>
        private void CashTo(long from, long to)
        {
            _cashRun?.Pause();
            _cashRun = null;

            if (from == long.MinValue || System.Math.Abs(to - from) < 1000)
            {
                _cash.text = Loc.Money(to);
                return;
            }

            const float Sure = 0.36f;
            float basladi = Time.unscaledTime;
            _cashRun = _cash.schedule.Execute(() =>
            {
                float t = Mathf.Clamp01((Time.unscaledTime - basladi) / Sure);
                // Yavaslayarak: sayi once hizli akiyor, sonra oturuyor.
                float k = 1f - (1f - t) * (1f - t) * (1f - t);
                long v = from + (long)((to - from) * k);
                _cash.text = Loc.Money(t >= 1f ? to : v);
                if (t >= 1f) { _cashRun?.Pause(); _cashRun = null; }
            }).Every(33);
        }

        public override void Tick()
        {
            Simulation sim = App.Sim;
            if (sim == null) return;

            // SERVIS BITISI ASAMA DEGISTIRMIYOR: _phase hala Service.
            // Serit yalnizca asamayi izliyordu, yani bitis ekrana hic
            // yansimiyordu.
            bool done = sim.Phase == DayPhase.Service && sim.ServiceComplete;
            if (done != _builtServiceDone)
            {
                _builtServiceDone = done;
                BuildBottom();
            }

            if (sim.Day != _shownDay)
            {
                _shownDay = sim.Day;
                // ROZETTE YALNIZCA SAYI: "Gun" kelimesi 46 dp'lik bir
                // rozete sigmaz ve zaten asama etiketi hemen yaninda.
                _day.text = sim.Day.ToString(Loc.Culture);
                if (_season != null)
                    _season.text = sim.SeasonOver
                        ? Loc.T("ui.hud.free_play")
                        : Loc.T("ui.hud.season", sim.Day, sim.CampaignDays);
            }
            if (sim.Cash != _shownCash)
            {
                long onceki = _shownCash;
                _shownCash = sim.Cash;
                CashTo(onceki, sim.Cash);
                // Plakanin uzerinde KOYU metin. Borctayken kirmizi
                // kalmali ama plakanin uzerinde okunan bir kirmizi.
                _cash.style.color = sim.Cash < 0
                    ? new Color(0.69f, 0.12f, 0.14f) : Theme.PlateInk;
            }
            // TAVAN DA YAZILIYOR.
            //
            // Simulation.ReputationCapCenti'nin yorumu "arayuz bunu
            // gostermeli" diyordu ve hicbir ekran okumuyordu. Tavana
            // dayanan oyuncu, iyi servis yapmaya devam ederken sayinin
            // durdugunu goruyor ve sebebini hicbir yerden ogrenemiyordu.
            if (sim.ReputationCenti != _shownRep
                || sim.ReputationCapCenti != _shownRepCap)
            {
                _shownRep = sim.ReputationCenti;
                _shownRepCap = sim.ReputationCapCenti;

                // Sayinin ADI da yaziyor. Once yalnizca "30,0" gorunuyordu
                // ve iyi mi kotu mu bilgisini TEK BASINA renk tasiyordu -
                // renk koru bir oyuncu icin hicbir sey ifade etmiyordu.
                //
                // Ve TAVAN da yaziyor: "Itibar 75,0 / 75". Tavana
                // dayanmis bir oyuncu, servisi ne kadar iyi yaparsa
                // yapsin sayinin kipirdamadigini goruyordu ve sebebini
                // hicbir yerden ogrenemiyordu. Olculdu: iyi oynayan bir
                // oyuncu yedi masada 75'e dayanip 32 gun orada kaliyor.
                bool tavanda = sim.ReputationCenti >= sim.ReputationCapCenti;
                // KAPSULDE ETIKET YOK: simge zaten "itibar" diyor.
                // Tavan duruyor - tavana dayanmis oyuncu sayinin neden
                // kipirdamadigini ancak boyle goruyor.
                _rep.text = Loc.Reputation(sim.ReputationCenti)
                            + " / " + (sim.ReputationCapCenti / 100);
                // Tavandayken renk bir DURUM degil bir YON bildiriyor:
                // burasi kotu bir yer degil, buyumeden gecilemeyen bir yer.
                _rep.style.color = tavanda
                    ? Theme.Warn : Theme.ReputationColor(sim.ReputationCenti);
            }
            int toRent = sim.DaysToRent;
            if (toRent != _shownRentDays)
            {
                _shownRentDays = toRent;
                _rent.text = toRent == 0
                    ? Loc.T("ui.hud.rent_today", Loc.Money(sim.WeeklyBill))
                    : Loc.T("ui.hud.rent_in", toRent, Loc.Money(sim.WeeklyBill));

                // Son iki gunde turuncu, kasa yetmiyorsa kirmizi: uyari
                // ZAMANINDA gelmeli, fatura geldikten sonra degil.
                _rent.style.color = sim.Cash < sim.WeeklyBill && toRent <= 2
                    ? Theme.Bad
                    : toRent <= 2 ? Theme.Warn : Theme.InkDim;
            }

            // AKIS SATIRI: dize ancak SAYILAR DEGISINCE uretiliyor.
            //
            // Ust seritteki diger alti alan zaten korumaliydi, bu biri
            // degildi: her karede Loc.T(key, a, b, c) cagriliyor ve o
            // string.Format demek - object[] dizisi, uc int kutulamasi
            // ve bicimlenmis bir dize, degerler hic degismese bile.
            // Kare basina ~150-200 bayt, sekiz dakikalik bir serviste
            // ~3 MB cop, yani birkac dakikada bir gorunur bir cop
            // toplama duraklamasi.
            //
            // Ustelik OccupiedTables bir ozellik degil bir DONGU: her
            // karede on dort masa taraniyordu.
            bool service = sim.Phase == DayPhase.Service;

            // KRIZ SERIDI KENDILIGINDEN BELIRIYOR.
            //
            // Alt cubuk yalnizca DEGISIMLERDE kuruluyor (asama degisti,
            // bir dugmeye basildi, servis bitti) ve kriz seridi o
            // cubugun icinde. Yani hicbir seye dokunmayan bir oyuncuya
            // "SABRI TUKENIYOR" uyarisi HIC gorunmuyordu - ve uyari
            // sesi (Sfx.Upset) de calmiyordu, cunku o da serit kurulunca
            // caliyor. Oyunun tek acil uyari kanali, oyuncunun zaten
            // ekrana dokundugu anlara bagliydi.
            //
            // Tur bunu goremezdi: kontrol seridin KURULUP kurulmadigini
            // degil, simulasyonun kritik masasi olup olmadigini
            // soruyordu.
            //
            // Kosul SAYININ DEGISMESI: her karede degil, kritige bir
            // masa girip ciktikca kuruluyor.
            if (service)
            {
                int kritik = CrisisTables;
                if (kritik != _builtCrisis)
                {
                    _builtCrisis = kritik;
                    BuildBottom();
                }
            }
            else if (_builtCrisis != -1) _builtCrisis = -1;

            if (service)
            {
                int served = sim.ServedParties;
                int angry = sim.AngryParties;
                int occupied = sim.OccupiedTables;

                if (served != _shownServed || angry != _shownAngry
                    || occupied != _shownOccupied || _shownPhase != DayPhase.Service)
                {
                    _shownServed = served;
                    _shownAngry = angry;
                    _shownOccupied = occupied;
                    _shownPhase = DayPhase.Service;
                    // "Bugun" KARTI: ust seritte sikismis tek satirin
                    // yerini aldi. Ayni uc sayi, artik okunabilir.
                    if (_servedValue != null)
                        _servedValue.text = served.ToString(Loc.Culture);
                    if (_angryValue != null)
                    {
                        _angryValue.text = angry.ToString(Loc.Culture);
                        // ACIK ZEMINDE KOYU KIRMIZI: Theme.Bad burada
                        // 2,22:1 veriyordu, BadDeep 5,4:1.
                        _angryValue.style.color = angry > 0
                            ? Kit.BadDeep : Theme.PlateInk;
                    }
                    if (_occupiedValue != null)
                        _occupiedValue.text = occupied.ToString(Loc.Culture)
                                              + " / " + sim.TableCount;
                    if (_turnedValue != null)
                    {
                        int donen = sim.TurnedAwayParties;
                        _turnedValue.text = donen.ToString(Loc.Culture);
                        _turnedValue.style.color = donen > 0
                            ? Kit.BadDeep : Theme.PlateInk;
                    }
                }
            }
            // Servis disi asamalarda yalnizca DAMGA tazeleniyor: ustteki
            // blok "_shownPhase == Service" karsilastirmasiyla calisiyor,
            // yani asama degisimi burada kaydedilmezse servise donuldugunde
            // "Bugun" karti bir kare geride kalir.
            else if (sim.Phase != _shownPhase) _shownPhase = sim.Phase;

            // ASAMA ETIKETI VE GUN CUBUGU.
            //
            // Cubuk servis gununun ne kadarinin gectigini gosteriyor -
            // oyunda bu sayi vardi (ServiceProgressBp; isik, golge ve
            // sokak lambalari ondan okunuyor) ama oyuncuya HIC
            // gosterilmiyordu. "Ne kadar kaldi" sorusunun cevabi
            // yalnizca gokyuzunun renginde duruyordu.
            if (_phaseLabel != null && _meterFill != null)
            {
                float oran = sim.Phase == DayPhase.Service
                    ? sim.ServiceProgressBp / 10000f
                    : (sim.Phase == DayPhase.Evening ? 1f : 0f);
                if (Mathf.Abs(oran - _shownProgress) > 0.004f
                    || sim.Phase != _shownMeterPhase)
                {
                    _shownProgress = oran;
                    _shownMeterPhase = sim.Phase;
                    _meterFill.style.width = Length.Percent(oran * 100f);
                    // Aksamda cubuk DOLU ve sonuk: gun bitti demek.
                    // Theme.Line ile doldurulunca "bos cubuk" gibi
                    // okunuyordu - dolu ile bos ayirt edilemiyordu.
                    _meterFill.style.backgroundColor =
                        sim.Phase == DayPhase.Service ? Theme.Accent
                        : (sim.Phase == DayPhase.Evening ? Theme.AccentDim
                                                         : Theme.Line);
                    _phaseLabel.text = Loc.T("ui.phase." + PhaseKey(sim.Phase));
                }
            }

            // DURUM KARTLARI: ciro ve memnuniyet.
            //
            // Ikisi de gun raporunda vardi, yani gun BITTIKTEN sonra.
            // Oyuncunun servis sirasinda "iyi mi gidiyor" sorusuna
            // bakacagi hicbir sey yoktu.
            if (_revenue != null)
            {
                int imza = (int)(sim.Revenue % 1000000L) * 100 + sim.AverageSatisfactionCenti;
                if (imza != _shownRevenueDay)
                {
                    _shownRevenueDay = imza;
                    _revenue.text = Loc.Money(sim.Revenue);
                    if (_satisfaction != null)
                    {
                        int m = sim.AverageSatisfactionCenti;
                        _satisfaction.text = Loc.Reputation(m);
                        _satisfaction.style.color = m >= 7000 ? Theme.Good
                            : (m >= 5000 ? Theme.Warn : Theme.Bad);
                    }
                }
            }

            if (sim.Phase != _builtPhase) BuildBottom();
            if (sim.Phase != _builtCards) BuildCards();

            // Bekleyen hikaye sahnesi varsa aksamda aciliyor.
            //
            // ASAMA DEGISIMINE bagli DEGIL: "Gunu Kapat" dugmesi
            // BuildBottom'i kendi icinde cagiriyor, yani _builtPhase
            // Tick calismadan once guncelleniyor ve gecis hic
            // gorunmuyordu. Kosul durumun KENDISI olmali, degisimi degil.
            if (sim.Phase == DayPhase.Evening && App.HasStory && Ui.Top == this)
                Ui.Push(new StoryScreen());

            // Bildirimler: kendi eylemimin balonu sure dolunca gidiyor,
            // simulasyondan gelenleri GameApp yasllandiriyor.
            bool dirty = App.NoticesChanged;
            if (_ownLeft > 0f)
            {
                _ownLeft -= Time.deltaTime;
                if (_ownLeft <= 0f) { _ownText = null; dirty = true; }
            }
            if (dirty)
            {
                App.NoticesSeen();
                RebuildNotices();
            }
        }

        private static string PhaseKey(DayPhase p)
        {
            switch (p)
            {
                case DayPhase.Morning: return "morning";
                case DayPhase.Service: return "service";
                default: return "evening";
            }
        }

        // Oyun ekraninda geri tusu duraklatma menusunu aciyor, oyundan
        // atmiyor. Kazayla kampanyadan cikmak kabul edilemez.
        public override bool OnBack()
        {
            Ui.Push(new PauseScreen());
            return false;
        }
    }
}
