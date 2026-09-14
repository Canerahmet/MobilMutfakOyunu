using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// Arayuzun gorsel dili. Renkler ve olculer TEK YERDE.
    ///
    /// USS dosyasi yerine C# olmasinin sebebi bu projeye ozel: arayuz
    /// kodla kuruluyor ve sahne uretilen bir sey (docs/34 22). Bir USS
    /// dosyasi, kodun disinda ikinci bir kaynak olurdu.
    ///
    /// Renk secimi mutfak kimliginden bagimsiz ve bilerek: mutfak kimligi
    /// SALONDA (docs/10), arayuzde degil. Arayuzun isi okunur olmak.
    ///
    /// Olculer dokunma icin: docs/16 asgari dokunma hedefi 48 dp, ve
    /// buradaki her dugme en az o kadar yuksek.
    /// </summary>
    public static class Theme
    {
        // --- renk ------------------------------------------------------------
        public static readonly Color Ink = new Color(0.94f, 0.93f, 0.90f);
        public static readonly Color InkDim = new Color(0.68f, 0.66f, 0.62f);
        // Olculdu: eski deger (0,46) panel uzerinde 3,24:1 veriyordu ve
        // WCAG AA asgarisi 4,5:1. Ustelik en cok bu renk kullaniliyor -
        // alt basliklar, servis durum satiri, kilit sebepleri. Yani
        // okunamayan kisim tam olarak aciklayici metnin kendisiydi.
        public static readonly Color InkFaint = new Color(0.60f, 0.59f, 0.57f);

        public static readonly Color Bg = new Color(0.086f, 0.094f, 0.110f);
        public static readonly Color Panel = new Color(0.137f, 0.145f, 0.165f);
        public static readonly Color PanelHi = new Color(0.180f, 0.190f, 0.215f);
        public static readonly Color Line = new Color(0.255f, 0.263f, 0.290f);

        /// <summary>Vurgu: bakir. Bir lokantanin tezgah rengi.</summary>
        public static readonly Color Accent = new Color(0.847f, 0.545f, 0.259f);
        public static readonly Color AccentDim = new Color(0.549f, 0.353f, 0.169f);

        /// <summary>
        /// SAYILARIN UZERINDE DURDUGU ACIK KAPSUL.
        ///
        /// Benzer oyunlarin taranmasinda cikan en yaygin kural bu:
        /// chrome koyu olsa bile SAYI acik bir plakanin uzerinde
        /// duruyor - Good Pizza Great Pizza, My Cafe, Cooking Diary,
        /// Cooking Fever, Idle Restaurant Tycoon, hepsi. Sebebi
        /// olculebilir: oyuncunun her uc saniyede bir yeniden okudugu
        /// sey kasa rakamidir ve bu, onu bir vurgu rengi HARCAMADAN
        /// ekrandaki en yuksek kontrastli nesne yapiyor.
        ///
        /// Beyaz degil sicak kirik beyaz: ekranin %12'si saf beyaz,
        /// gece telefonda parlama kaynagi; sicak ton ayrica chrome'u
        /// yemege bagliyor.
        ///
        /// Olculdu: Plate uzerinde PlateInk 12,9:1, PlateDim 5,1:1.
        /// Kapsulun kendisi serit zemininde 12,5:1 - yani "burada bir
        /// nesne var" ayrimi da renkten degil PARLAKLIKTAN geliyor.
        /// </summary>
        public static readonly Color Plate = new Color(0.914f, 0.898f, 0.863f);
        public static readonly Color PlateInk = new Color(0.110f, 0.125f, 0.157f);
        public static readonly Color PlateDim = new Color(0.337f, 0.376f, 0.427f);

        public static readonly Color Good = new Color(0.427f, 0.702f, 0.451f);
        public static readonly Color Warn = new Color(0.898f, 0.706f, 0.310f);
        // Olculdu: eski deger panel uzerinde 4,2:1 - esigin hemen altinda.
        public static readonly Color Bad = new Color(0.93f, 0.47f, 0.44f);

        // --- olcu ------------------------------------------------------------
        /// <summary>Asgari dokunma hedefi. Google 48 dp.</summary>
        public const int Touch = 52;
        public const int Gap = 10;
        public const int Pad = 16;
        public const int Radius = 10;

        public const int FontHuge = 42;
        public const int FontTitle = 26;
        public const int FontBody = 17;
        public const int FontSmall = 14;

        // --- yardimcilar -----------------------------------------------------
        public static VisualElement Column(float gap = Gap)
        {
            VisualElement v = new VisualElement();
            v.style.flexDirection = FlexDirection.Column;
            SetGap(v, gap, true);
            return v;
        }

        public static VisualElement Row(float gap = Gap)
        {
            VisualElement v = new VisualElement();
            v.style.flexDirection = FlexDirection.Row;
            SetGap(v, gap, false);
            return v;
        }

        private static void SetGap(VisualElement v, float gap, bool column)
        {
            // UI Toolkit'te gap yok; aralik cocuklara margin olarak
            // veriliyor. Tek yerde yapiliyor ki iki farkli aralik olusmasin.
            v.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                for (int i = 1; i < v.childCount; i++)
                {
                    if (column) v[i].style.marginTop = gap;
                    else v[i].style.marginLeft = gap;
                }
            });
        }

        public static VisualElement PanelBox()
        {
            VisualElement v = Column();

            // EZILMESIN. Esnek bir sutunun cocuklari varsayilan olarak
            // flex-shrink 1 ile geliyor; kaydirilabilir bir listede bu,
            // butun kartlarin pencereye sigacak kadar ezilmesi demek.
            // Icerik kutudan tasiyor ve satirlar UST USTE biniyordu -
            // Hal ekraninda ilk masaustu yapisinda tam olarak boyle cikti.
            v.style.flexShrink = 0;

            v.style.backgroundColor = Panel;
            v.style.paddingLeft = Pad;
            v.style.paddingRight = Pad;
            v.style.paddingTop = Pad;
            v.style.paddingBottom = Pad;
            Round(v, Radius);
            return v;
        }

        public static void Round(VisualElement v, float r)
        {
            v.style.borderTopLeftRadius = r;
            v.style.borderTopRightRadius = r;
            v.style.borderBottomLeftRadius = r;
            v.style.borderBottomRightRadius = r;
        }

        public static Label Text(string s, int size = FontBody, Color? color = null)
        {
            Label l = new Label(s);
            l.style.fontSize = size;
            l.style.color = color ?? Ink;
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        public static Label Title(string s)
        {
            Label l = Text(s, FontTitle);
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            return l;
        }

        /// <summary>
        /// Dugme. Yuksekligi ASGARI DOKUNMA HEDEFI kadar; bu deger tek
        /// yerde yaziyor ki bir ekranda kucuk bir dugme kalmasin.
        /// </summary>
        public static Button Btn(string text, System.Action onClick,
                                 bool primary = false, bool wide = false,
                                 bool danger = false)
        {
            Button b = new Button(() => { Sfx.Click(); onClick?.Invoke(); }) { text = text };
            b.style.minHeight = Touch;
            b.style.fontSize = FontBody;
            // Yazi dikeyde ORTADA. Varsayilan ust-sola yaslamak, 52 dp
            // yuksekliginde bir dokunma hedefinde yaziyi tepeye yapistiriyor
            // ve dugme bozuk gorunuyordu (ilk masaustu yapisi).
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            // Kisa metinli dugmeler de dokunma hedefi kadar GENIS.
            // "-" ve "+" dugmeleri 44 dp genislikteydi ve servis boyunca
            // en sik basilan ikisiydi.
            b.style.minWidth = Touch;
            b.style.paddingLeft = Pad;
            b.style.paddingRight = Pad;
            b.style.marginLeft = 0;
            b.style.marginRight = 0;
            b.style.marginTop = 0;
            b.style.marginBottom = 0;
            Color yuz = danger ? Bad : (primary ? Accent : PanelHi);
            b.style.backgroundColor = yuz;
            b.style.color = (primary || danger) ? Bg : Ink;
            b.style.unityFontStyleAndWeight =
                (primary || danger) ? FontStyle.Bold : FontStyle.Normal;
            b.style.borderTopWidth = 0;
            b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0;
            b.style.borderRightWidth = 0;
            if (wide)
            {
                b.style.flexGrow = 1;

                // VE DARALABILIR.
                //
                // UI Toolkit'te `flex-shrink` varsayilani CSS'in aksine
                // SIFIR: satir sigmayinca hicbir sey daralmiyor, son
                // ogeler tasip bir oncekinin USTUNE biniyor. Olculdu -
                // Ingilizce'de "Attention > worst" ile "Close the day"
                // BIRINCI GUNDE 13 dp cakisiyordu, Turk mutfaginda
                // "Ilgi > sabirsiz" ile "Veresiye ac" 87 dp.
                //
                // Serit yuksekligi ve kirpilan yazi olcumlerinin
                // IKISI DE yesildi: yukseklik dogru, hicbir YAZI
                // kirpilmamis - cunku kirpilmak yerine ust uste
                // biniyordu.
                b.style.flexShrink = 1;
            }
            Round(b, Radius);
            Press(b, yuz);
            return b;
        }

        /// <summary>
        /// Basilma geri bildirimi: koyulasma + %3 kucukme, 90 ms.
        ///
        /// NEDEN ELLE: UI Toolkit'in :active kuralini stil sayfasi
        /// veriyor, bu proje ise butun arayuzu C# ile kuruyor ve
        /// SATIR ICI stil yaziyor - satir ici stil kural sayfasini
        /// her zaman yeniyor, yani hazir basili hali hicbir zaman
        /// gorunmuyordu.
        ///
        /// Sonuc: oyundaki hicbir dugmenin basili hali YOKTU. Ses vardi
        /// ama telefonda ses genelde kapali (GameApp'in kendi notu), yani
        /// bir dokunusun karsiligi cogu oyuncu icin HICBIR SEYDI - durum
        /// degisene kadar. Tek yerde duzeltiliyor, 59 cagri yeri birden.
        ///
        /// Olcu: 0,97 - dokunmanin hissedilmesi icin yeterli, dugmenin
        /// yerinden oynadigi izlenimini vermeyecek kadar kucuk.
        /// </summary>
        internal static void PressFx(VisualElement b, Color yuz) { Press(b, yuz); }

        private static void Press(VisualElement b, Color yuz)
        {
            Color basili = new Color(yuz.r * 0.78f, yuz.g * 0.78f, yuz.b * 0.78f, yuz.a);

            b.RegisterCallback<PointerDownEvent>(_ =>
            {
                b.style.backgroundColor = basili;
                b.style.scale = new Scale(new Vector3(0.97f, 0.97f, 1f));
            });

            // Up VE Leave: parmagini dugmeden kaydirarak birakan oyuncu
            // Up almiyor ve dugme basili kaliyordu.
            EventCallback<EventBase> birak = _ =>
            {
                b.style.backgroundColor = yuz;
                b.style.scale = new Scale(Vector3.one);
            };
            b.RegisterCallback<PointerUpEvent>(e => birak(e));
            b.RegisterCallback<PointerLeaveEvent>(e => birak(e));
            b.RegisterCallback<PointerCancelEvent>(e => birak(e));
        }

        /// <summary>
        /// Kucuk dolu daire. Bir seyi ISARETLEMEK icin (favori yemek gibi).
        ///
        /// Cizilen bir oge, yazi tipi simgesi DEGIL. Once yildiz karakteri
        /// kullaniliyordu ve yazi tipinde yoktu: oyuncuya bos kutu olarak
        /// gorunurdu (tools/art/check_font.py yakaladi). Cizilen simge
        /// yazi tipinden bagimsiz ve her boyutta net.
        /// </summary>
        /// <summary>
        /// Bolum basligi. RENK DEGIL AGIRLIK.
        ///
        /// Once Accent ile yaziliyorlardi ve vurgu rengi zaten sekiz
        /// baska is yapiyordu: "bunu yap", "bu acik", "paran yetiyor",
        /// "bu ekrani kapat"... Bir turuncu cubugun ne anlama geldigi
        /// tahmin edilemez hale gelmisti.
        ///
        /// Basliklarin ayrica bir tutarsizligi vardi: ekipman
        /// ekraninda "Soguk Hava Deposu" Accent, hemen altindaki
        /// kardesi "Ocak" duz Ink idi - oyuncunun goremeyecegi bir
        /// sebeple iki farkli baslik. Tek yol birakildi.
        /// </summary>
        public static Label Head(string text)
        {
            Label l = Text(text, FontBody, Ink);
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.letterSpacing = 0.6f;
            return l;
        }

        public static VisualElement Dot(Color c, float size = 10f)
        {
            VisualElement v = new VisualElement();
            v.style.width = size;
            v.style.height = size;
            v.style.backgroundColor = c;
            v.style.alignSelf = Align.Center;
            v.style.marginRight = 6;
            v.style.flexShrink = 0;
            Round(v, size * 0.5f);
            return v;
        }

        /// <summary>
        /// Metnin okunur kaldigi en fazla genislik (dp).
        ///
        /// Olcum degil kabul: bir etiket-deger satirinda goz sol bastan
        /// sag basa gidemiyor. Yatay tutulan bir telefonda ekran 1280 dp
        /// genisliginde olabiliyor ve bir liste satiri o kadar uzun
        /// olmamali.
        /// </summary>
        public const float ReadWidth = 880f;

        /// <summary>
        /// Kademeli seviye denetimi: bir sira kutucuk, dolu olanlar
        /// vurgulu.
        ///
        /// UI Toolkit'in kendi Slider'i KULLANILMIYOR. Iki sebep:
        ///
        /// 1) Gorunumu Unity'nin varsayilan calisma zamani temasindan
        ///    geliyor ve o tema YAPIYA GIRMIYOR - ilk masaustu yapisinda
        ///    ayarlar ekranindaki iki kaydirici bombos iki kutu olarak
        ///    cikti. Ayni sebep metinlerin de gorunmemesine yol acmisti.
        ///
        /// 2) Ince bir kaydirici cubugunu parmakla tutturmak zor. On
        ///    kademeli bir sirada her kutucuk 52 dp'lik dokunma hedefinin
        ///    icinde ve seviye ekrandan OKUNABILIYOR.
        /// </summary>
        public static VisualElement Level(string label, int steps, int value,
                                          System.Action<int> onPick)
        {
            VisualElement box = Column(Gap);

            VisualElement head = Row(0);
            head.style.justifyContent = Justify.SpaceBetween;
            head.Add(Text(label, FontBody, InkDim));
            Label read = Text(value + " / " + steps, FontSmall, Ink);
            head.Add(read);
            box.Add(head);

            VisualElement bar = Row(4);
            bar.style.height = Touch;

            // KUTUCUK GENISLIGI DE DOKUNMA HEDEFI.
            //
            // Yukseklik Touch'ti ama genislik yoktu: 480 dp'lik bir
            // panelde on adim, 32 dp dolgu ve dokuz aralik dusunce
            // kutucuk basina ~41 dp kaliyordu - temanin kendi 52 dp
            // olcusunun altinda. Ses seviyesini degistirmeye calisan
            // parmak komsu kademeye dusuyordu.
            //
            // Taban genislik, adim sayisini cagiranin degil OLCUNUN
            // belirlemesini sagliyor: sigmiyorsa satir sarilir.
            bar.style.flexWrap = Wrap.Wrap;
            bar.style.height = StyleKeyword.Auto;
            bar.style.minHeight = Touch;

            for (int i = 1; i <= steps; i++)
            {
                int step = i;
                Button cell = new Button(() => { Sfx.Click(); onPick(step); })
                              { text = string.Empty };
                cell.style.flexGrow = 1;
                cell.style.flexBasis = Touch;
                cell.style.minWidth = Touch;
                cell.style.height = Touch;
                cell.style.marginLeft = 0;
                cell.style.marginRight = 0;
                cell.style.marginTop = 0;
                cell.style.marginBottom = 0;
                cell.style.borderTopWidth = 0;
                cell.style.borderBottomWidth = 0;
                cell.style.borderLeftWidth = 0;
                cell.style.borderRightWidth = 0;
                cell.style.backgroundColor = i <= value ? Accent : PanelHi;
                Round(cell, 4);
                bar.Add(cell);
            }
            box.Add(bar);
            return box;
        }

        /// <summary>
        /// Kaydirma alanini MOBILLESTIRIR: masaustu kaydirma cubugu
        /// gizleniyor, parmakla surukleme aciliyor.
        ///
        /// Unity'nin varsayilan ScrollView'i ok dugmeleri olan, acik gri,
        /// kalin bir cubuk ciziyor. Yil sonu degerlendirme ekraninin
        /// goruntusunde tam da o cubuk duruyordu: koyu temali bir mobil
        /// oyunun yaninda yabanci ve bitmemis duruyor, ustelik dokunmatikte
        /// kimse onu kullanmiyor.
        ///
        /// Surukleme acikca aciliyor: cubuk gidince kaydirmanin TEK yolu
        /// parmak oluyor ve varsayilan esik fare icin ayarli.
        /// </summary>
        public static ScrollView Mobile(ScrollView v)
        {
            v.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            v.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            v.mode = ScrollViewMode.Vertical;

            v.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
            v.scrollDecelerationRate = 0.135f;
            v.elasticity = 0.1f;
            return v;
        }

        /// <summary>Ince ayrim cizgisi.</summary>
        public static VisualElement Divider()
        {
            VisualElement v = new VisualElement();
            v.style.height = 1;
            v.style.backgroundColor = Line;
            return v;
        }

        /// <summary>Etiket ve deger, iki yana yaslanmis.</summary>
        public static VisualElement Field(string label, string value, Color? valueColor = null)
        {
            VisualElement row = Row(0);
            row.style.justifyContent = Justify.SpaceBetween;
            row.Add(Text(label, FontBody, InkDim));
            Label v = Text(value, FontBody, valueColor ?? Ink);
            v.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(v);
            return row;
        }

        /// <summary>Itibar rengi: 80 ustu iyi, 40 alti kotu.</summary>
        /// <summary>
        /// Itibarin rengi. BASLANGIC DEGERI ALARM DEGIL.
        ///
        /// Once esikler 80/40 idi ve yeni bir kampanya 30,0 ile
        /// basliyor - yani birinci gun, hicbir sey olmadan, ust
        /// seritteki en yuksek doygunluklu oge "Itibar 30,0" kirmizisi
        /// oluyordu. Oyuncunun ilk ogrendigi sey, yanlis bir alarmdi.
        ///
        /// Yeni esikler baslangici NOTR birakiyor: 30 gri, dusunce
        /// kirmizi, yukselince yesil. Renk artik bir DURUM degil bir
        /// YON bildiriyor.
        /// </summary>
        public static Color ReputationColor(int centi)
        {
            if (centi >= 7000) return Good;
            if (centi >= 4500) return Warn;
            if (centi >= 2500) return InkDim;     // baslangic bandi: sessiz
            return Bad;
        }

        /// <summary>Kasa rengi: eksi kirmizi.</summary>
        public static Color CashColor(long centi)
        {
            return centi < 0 ? Bad : Ink;
        }
    }
}
