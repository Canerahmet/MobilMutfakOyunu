using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// OYUN ICI ARAYUZUN PARCALARI: kapsul, kart, simge dugmesi, eylem.
    ///
    /// Theme "belirteclerin" yeri (renk, olcu, dugme); Kit ise OYUN
    /// EKRANININ kendi bicimi - yuvarlak koyu kartlar, sayilarin durdugu
    /// kapsuller, altta simgeli dugmeler ve sag altta tek yesil eylem.
    ///
    /// Neden ayri dosya: bu bicim yalnizca oyun ekraninda (GameScreen)
    /// kullaniliyor. Menu, market ve personel ekranlari liste ekranlari
    /// ve onlarin duzeni Theme'in kendisi. Ikisini ayni dosyaya koymak,
    /// "dugme" kelimesinin iki ayri sey demesine yol acardi.
    ///
    /// SIMGELER CIZILIYOR, YAZI TIPINDEN GELMIYOR.
    ///
    /// Bu proje bunu bir kez ogrendi: yildiz karakteri Rubik'te yok ve
    /// oyuncuya BOS KUTU olarak gorunuyordu (tools/art/check_font.py
    /// yakaladi). Bir simge yazi tipine emanet edilemez - hepsi
    /// dikdortgen, daire ve donusten kuruluyor.
    /// </summary>
    public static class Kit
    {
        // --- renk --------------------------------------------------------
        /// <summary>Kartlarin zemini. Saydam: altindaki salon sezilsin.</summary>
        public static readonly Color CardBg = new Color(0.071f, 0.086f, 0.118f, 0.90f);

        /// <summary>Kart kenari. Koyu zeminde kartin nerede bittigini soyluyor.</summary>
        public static readonly Color CardLine = new Color(0.212f, 0.251f, 0.318f, 1f);

        /// <summary>
        /// EYLEM YESILI: "oyunu ilerleten tek dugme".
        ///
        /// Bakir vurgu (Theme.Accent) sekiz ayri is yapiyordu - "bunu
        /// yap", "bu acik", "paran yetiyor", "dikkat". Asama dugmesi
        /// (servisi ac / gunu kapat) hepsinden ayri bir sey: gunun
        /// KAPISI. Ayri bir renk hak ediyor ve o renk her oyunda ayni:
        /// yesil = ilerle.
        ///
        /// Theme.Good'dan (0,43/0,70/0,45) daha doygun: Good bir DURUM
        /// bildiriyor ("kar var"), bu bir EYLEM cagiriyor.
        /// </summary>
        public static readonly Color Go = new Color(0.180f, 0.729f, 0.357f);

        /// <summary>Acik kart uzerinde okunan koyu yesil.</summary>
        public static readonly Color GoDeep = new Color(0.086f, 0.478f, 0.208f);

        /// <summary>
        /// GEZINME MAVISI: "buraya gir".
        ///
        /// Referansin alt sol dortlusu mavi. Oyunun renk dili boylece uc
        /// role ayriliyor ve her rol TEK bir sey soyluyor:
        ///
        ///   mavi  - bir ekran aciyor (Hal, Menu, Kadro, Ekipman)
        ///   yesil - oyunu ilerletiyor (servisi ac, gunu kapat)
        ///   bakir - dikkat ve deger (kalan hak, itibar, gun cubugu)
        ///
        /// Once hepsi ayni griydi ve oyuncuya "once neyi yapmaliyim"
        /// sorusunun cevabini veren hicbir sey yoktu.
        /// </summary>
        public static readonly Color Nav = new Color(0.145f, 0.384f, 0.851f);

        /// <summary>Sikke sarisi. Kasa kapsulunun simgesi.</summary>
        public static readonly Color Coin = new Color(1.000f, 0.784f, 0.251f);

        /// <summary>Itibar tasi. Kasadan ayri bir renk, cunku ayri bir kaynak.</summary>
        public static readonly Color Gem = new Color(0.639f, 0.451f, 0.941f);

        public const int CardRadius = 14;

        /// <summary>
        /// Kapsul yuksekligi. 42 -> 38: ust serit de salonun payindan
        /// yiyor ve kapsulun icindeki plaka zaten dokunma hedefi degil -
        /// yalnizca menu dugmesi dokunuluyor ve o, kapsul yuksekligini
        /// kendi Touch tabaniyla birlikte kullaniyor.
        /// </summary>
        public const int PillHeight = 38;

        // =====================================================================
        // KUTULAR
        // =====================================================================

        /// <summary>Yuvarlak koyu kutu: butun kartlarin ve kapsullerin govdesi.</summary>
        public static VisualElement Box(float radius = CardRadius)
        {
            VisualElement v = new VisualElement();
            v.style.backgroundColor = CardBg;
            v.style.borderTopWidth = 1;
            v.style.borderBottomWidth = 1;
            v.style.borderLeftWidth = 1;
            v.style.borderRightWidth = 1;
            v.style.borderTopColor = CardLine;
            v.style.borderBottomColor = CardLine;
            v.style.borderLeftColor = CardLine;
            v.style.borderRightColor = CardLine;
            Theme.Round(v, radius);
            return v;
        }

        /// <summary>
        /// SAYI KAPSULU: solda simge, sagda sayi.
        ///
        /// Sayi ACIK BIR PLAKANIN uzerinde duruyor (docs/16): oyuncunun
        /// her uc saniyede bir okudugu sey kasa rakami ve acik plaka onu
        /// bir vurgu rengi HARCAMADAN ekrandaki en kontrastli nesne
        /// yapiyor.
        /// </summary>
        public static VisualElement Pill(VisualElement icon, Label value,
                                         bool plate = true)
        {
            VisualElement box = Box(PillHeight * 0.5f);
            box.style.flexDirection = Theme.RowFlow;
            box.style.alignItems = Align.Center;
            box.style.height = PillHeight;
            box.style.paddingLeft = 6;
            box.style.paddingRight = plate ? 6 : 14;
            box.style.flexShrink = 0;

            if (icon != null) box.Add(icon);

            value.style.unityFontStyleAndWeight = FontStyle.Bold;
            value.style.unityTextAlign = TextAnchor.MiddleCenter;

            if (plate)
            {
                VisualElement p = new VisualElement();
                p.style.backgroundColor = Theme.Plate;
                p.style.paddingLeft = 10;
                p.style.paddingRight = 10;
                p.style.paddingTop = 3;
                p.style.paddingBottom = 3;
                p.style.marginLeft = 6;
                p.style.justifyContent = Justify.Center;
                Theme.Round(p, 9);
                value.style.color = Theme.PlateInk;
                p.Add(value);
                box.Add(p);
            }
            else
            {
                value.style.marginLeft = 8;
                box.Add(value);
            }
            return box;
        }

        /// <summary>
        /// BASLIKLI KART. Referanstaki sol ve sag panellerin govdesi.
        ///
        /// Baslik KUCUK VE SONUK, icerik parlak: kartin kendisi bilgi
        /// degil, icindeki sayi bilgi.
        /// </summary>
        public static VisualElement Card(string title, out VisualElement body)
        {
            // KART ACIK, CEVRESI KOYU.
            //
            // Referansta "Gunluk Hedefler" paneli bembeyaz ve gerisi
            // koyu; bu bir suslerne degil bir SIRALAMA: ekranin en acik
            // nesnesi, oyuncunun once okumasi istenen sey oluyor.
            // docs/16 ayni kurali sayilar icin zaten yaziyordu (acik
            // plaka), burada butun karta uygulaniyor.
            VisualElement box = new VisualElement();
            box.style.backgroundColor = Theme.Plate;
            Theme.Round(box, CardRadius);
            box.style.paddingLeft = 12;
            box.style.paddingRight = 12;
            box.style.paddingTop = 9;
            box.style.paddingBottom = 9;

            if (!string.IsNullOrEmpty(title))
            {
                Label t = Theme.Text(title, Theme.FontSmall, Theme.PlateDim);
                t.style.unityFontStyleAndWeight = FontStyle.Bold;
                t.style.marginBottom = 5;
                box.Add(t);
            }

            body = new VisualElement();
            box.Add(body);
            return box;
        }

        /// <summary>
        /// DURUM KARTI: solda simge, sagda baslik + buyuk sayi.
        ///
        /// Referanstaki "Saatlik Gelir" ve "Musteri Memnuniyeti"
        /// kartlari. Oyunun karsiligi CIRO ve MEMNUNIYET - ikisi de
        /// zaten cekirdekte var ve bugune kadar yalnizca AKSAM
        /// raporunda goruunuyordu, yani oyuncu gun boyunca nasil
        /// gittigini goremiyordu.
        /// </summary>
        public static VisualElement Stat(VisualElement icon, string title,
                                         Label value)
        {
            VisualElement box = Box();
            box.style.flexDirection = Theme.RowFlow;
            box.style.alignItems = Align.Center;
            box.style.paddingLeft = 10;
            box.style.paddingRight = 12;
            box.style.paddingTop = 7;
            box.style.paddingBottom = 7;
            box.style.minWidth = 148;

            if (icon != null)
            {
                icon.style.marginRight = 9;
                box.Add(icon);
            }

            VisualElement col = new VisualElement();
            Label t = Theme.Text(title, Theme.FontSmall, Theme.InkFaint);
            col.Add(t);
            value.style.fontSize = 21;
            value.style.unityFontStyleAndWeight = FontStyle.Bold;
            col.Add(value);
            box.Add(col);
            return box;
        }

        /// <summary>
        /// ILERLEME CUBUGU. Referanstaki deneyim cubugunun karsiligi
        /// SERVIS GUNU: oyuncu gunun ne kadarinin gectigini hicbir
        /// yerden okuyamiyordu.
        /// </summary>
        public static VisualElement Meter(out VisualElement fill,
                                          float width = 150f, float height = 10f)
        {
            VisualElement track = new VisualElement();
            track.style.width = width;
            track.style.height = height;
            track.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
            track.style.overflow = Overflow.Hidden;
            Theme.Round(track, height * 0.5f);

            fill = new VisualElement();
            fill.style.height = height;
            fill.style.width = Length.Percent(0);
            fill.style.backgroundColor = Theme.Accent;
            Theme.Round(fill, height * 0.5f);
            track.Add(fill);
            return track;
        }

        /// <summary>
        /// GUN ROZETI: sekizgen bir plaka ve icinde gun numarasi.
        ///
        /// Referansin seviye rozeti; bu oyunda seviye yok, GUN var -
        /// ve altmis gunluk bir kampanyada kacinci gunde oldugu, tek
        /// basina en cok anlam tasiyan sayi.
        /// </summary>
        public static VisualElement Badge(Label number)
        {
            VisualElement box = new VisualElement();
            box.style.width = 46;
            box.style.height = 46;
            box.style.backgroundColor = Theme.Accent;
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;
            box.style.flexShrink = 0;
            box.style.borderTopWidth = 2;
            box.style.borderBottomWidth = 2;
            box.style.borderLeftWidth = 2;
            box.style.borderRightWidth = 2;
            Color kenar = new Color(1f, 0.83f, 0.55f);
            box.style.borderTopColor = kenar;
            box.style.borderBottomColor = kenar;
            box.style.borderLeftColor = kenar;
            box.style.borderRightColor = kenar;
            Theme.Round(box, 13);

            number.style.fontSize = 20;
            number.style.unityFontStyleAndWeight = FontStyle.Bold;
            number.style.color = new Color(0.14f, 0.10f, 0.05f);
            box.Add(number);
            return box;
        }

        // =====================================================================
        // DUGMELER
        // =====================================================================

        /// <summary>
        /// SIMGELI DUGME: ustte simge, altinda etiket.
        ///
        /// Referansin alt sol dortlusu. Yalniz simge OLMAZ - bir sepet
        /// simgesi "Hal" mi "Magaza" mi demek, oyuncu bilemez; yalniz
        /// yazi da olmaz, cunku dort gri dikdortgen birbirinden ancak
        /// okunarak ayrilir. Ikisi birden: siluet ayirir, yazi soyler.
        /// </summary>
        public static Button IconButton(VisualElement icon, string label,
                                        System.Action onClick, bool mavi = false)
        {
            Button b = new Button(() => { Sfx.Click(); onClick?.Invoke(); });
            b.text = string.Empty;
            b.style.flexDirection = FlexDirection.Column;
            b.style.alignItems = Align.Center;
            b.style.justifyContent = Justify.Center;
            // 62 -> 54: SERIT SALONU YIYOR.
            //
            // Olculdu: ust ve alt serit 156 dp, ekran 393 - yani
            // arayuz ekranin %40'ini aliyor ve kamera kalan %60'a
            // sigdiriyor. Referansa "uzak" hissinin buyuk kismi
            // buradan geliyordu; kat planindan degil (arayuzsuz
            // cerceve, binayi ekrani doldururken gosteriyor).
            //
            // 54 dp hala Google'in 48 dp asgarisinin uzerinde.
            b.style.minHeight = 54;
            b.style.minWidth = 72;
            b.style.paddingLeft = 7;
            b.style.paddingRight = 7;
            b.style.paddingTop = 4;
            b.style.paddingBottom = 4;
            b.style.marginLeft = 0;
            b.style.marginRight = 0;
            b.style.marginTop = 0;
            b.style.marginBottom = 0;
            Color yuz = mavi ? Nav : CardBg;
            b.style.backgroundColor = yuz;
            b.style.borderTopWidth = mavi ? 0 : 1;
            b.style.borderBottomWidth = mavi ? 0 : 1;
            b.style.borderLeftWidth = mavi ? 0 : 1;
            b.style.borderRightWidth = mavi ? 0 : 1;
            b.style.borderTopColor = CardLine;
            b.style.borderBottomColor = CardLine;
            b.style.borderLeftColor = CardLine;
            b.style.borderRightColor = CardLine;
            Theme.Round(b, CardRadius);

            if (icon != null)
            {
                icon.style.marginBottom = 4;
                b.Add(icon);
            }
            Label t = Theme.Text(label, Theme.FontSmall,
                                 mavi ? Color.white : Theme.Ink);
            t.style.unityTextAlign = TextAnchor.MiddleCenter;
            b.Add(t);

            Theme.PressFx(b, yuz);
            return b;
        }

        /// <summary>
        /// ASAMA DUGMESI: sag altta, yesil, iki satirli.
        ///
        /// Oyunun her asamasinda TEK bir "ilerlet" eylemi var - sabah
        /// servisi acmak, servis bitince gunu kapatmak, aksam ertesi
        /// gune gecmek. Referansin sag alt dugmesi tam olarak bu ve
        /// oyunda bugune kadar diger alti dugmeyle ayni boyda,
        /// aralarinda kayboluyordu.
        ///
        /// Alt satir NE OLACAGINI soyluyor ("yeni gun basliyor"):
        /// geri donusu olmayan bir karara basmadan once oyuncunun
        /// okuyabilecegi tek cumle.
        /// </summary>
        public static Button Cta(string title, string sub, System.Action onClick,
                                 bool ready = true)
        {
            // YUZ RENGI KOYULASTI.
            //
            // Olculdu: `Go` uzerinde beyaz 19 dp kalin baslik 2,54:1
            // (kalin 19 dp icin esik 3:1) ve %82 saydam alt satir ~2,2:1
            // (esik 4,5:1). Bu dugme oyuncunun altmis gun boyunca en cok
            // bastigi dugme ve alt satiri geri donusu olmayan karari
            // aciklayan tek cumle.
            //
            // `GoDeep` uzerinde beyaz 7,0:1 - iki esigi de gecti. `Go`
            // duruyor ve kucuk yesil eylemlerde kullaniliyor (orada
            // uzerinde yazi yok, yalnizca beyaz simge).
            Color yuz = ready ? GoDeep : Theme.PanelHi;
            Button b = new Button(() => { Sfx.Click(); onClick?.Invoke(); });
            b.text = string.Empty;
            b.style.flexDirection = Theme.RowFlow;
            b.style.alignItems = Align.Center;
            b.style.justifyContent = Justify.Center;
            b.style.minHeight = 54;
            b.style.paddingLeft = 14;
            b.style.paddingRight = 18;
            b.style.paddingTop = 4;
            b.style.paddingBottom = 4;
            b.style.marginLeft = 0;
            b.style.marginRight = 0;
            b.style.marginTop = 0;
            b.style.marginBottom = 0;
            b.style.backgroundColor = yuz;
            b.style.borderTopWidth = 0;
            b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0;
            b.style.borderRightWidth = 0;
            Theme.Round(b, CardRadius);

            // CIFT OK: referansin "ileri sar" isareti. Tek ucgen
            // "oynat" demek; bu dugme oyunu OYNATMIYOR, gunu ilerletiyor.
            VisualElement oklar = new VisualElement();
            oklar.style.flexDirection = Theme.RowFlow;
            oklar.style.marginRight = 12;
            Color okRenk = ready ? Color.white : Theme.InkDim;
            oklar.Add(Icons.Play(okRenk, 18f));
            VisualElement ikinci = Icons.Play(okRenk, 18f);
            ikinci.style.marginLeft = -6;
            oklar.Add(ikinci);
            b.Add(oklar);

            VisualElement col = new VisualElement();
            Label t = Theme.Text(title, 19, ready ? Color.white : Theme.Ink);
            t.style.unityFontStyleAndWeight = FontStyle.Bold;
            col.Add(t);
            if (!string.IsNullOrEmpty(sub))
            {
                // SAYDAMLIK KALKTI: %82 beyaz, kontrasti esigin altina
                // dusuruyordu ve kazandirdigi tek sey "ikincil gorunum".
                // Ikincillik zaten punto farkiyla veriliyor.
                Label s = Theme.Text(sub, Theme.FontSmall,
                                     ready ? Color.white : Theme.InkDim);
                col.Add(s);
            }
            b.Add(col);

            Theme.PressFx(b, yuz);
            return b;
        }

        /// <summary>
        /// Kapsulun yanindaki kucuk yesil eylem (referanstaki "+").
        ///
        /// Oyunda satin alinacak bir sey YOK - bu dugme krediyi aciyor,
        /// yani "kasa yetmiyorsa buradan bakabilirsin". Bir oyunda
        /// olmayan bir seyi taklit etmek yerine, olani oraya koymak.
        /// </summary>
        public static Button PillAction(System.Action onClick, string tip)
        {
            Button b = new Button(() => { Sfx.Click(); onClick?.Invoke(); });
            b.text = string.Empty;
            b.tooltip = tip;
            // DOKUNMA HEDEFI 30 DP DEGIL.
            //
            // Projenin kendi olcutu 52 dp (Theme.Touch), Google'in
            // tabani 48. Bu dugme kasa kapsulunun yaninda, ekran
            // kenarina yakin, ve parasi bitmis oyuncunun tek cikis
            // kapisi - kacirilmasi en pahali dugme. Ustelik anlami
            // yalnizca `tooltip` ile veriliyordu ve DOKUNMATIKTE
            // TOOLTIP HIC GORUNMEZ.
            //
            // GORSEL 30 KALIYOR, DOKUNMA ALANI 52.
            //
            // Bunu `minWidth` ile yapmak ISE YARAMAZ: UI Toolkit'te
            // minWidth width'i EZIYOR, yani dugme gercekten 52x52
            // buyur ve kucuk yesil eylem kocaman bir kareye donerdi.
            // Dogru yol: dis dugme 52x52 ve SAYDAM, ici 30x30 yesil.
            b.style.width = Theme.Touch;
            b.style.height = Theme.Touch;
            b.style.backgroundColor = Color.clear;
            b.style.marginLeft = 0;
            b.style.marginRight = 0;
            b.style.marginTop = 0;
            b.style.marginBottom = 0;
            b.style.paddingLeft = 0;
            b.style.paddingRight = 0;
            b.style.paddingTop = 0;
            b.style.paddingBottom = 0;
            b.style.alignItems = Align.Center;
            b.style.justifyContent = Justify.Center;
            b.style.borderTopWidth = 0;
            b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0;
            b.style.borderRightWidth = 0;

            // GORUNEN parca: 30x30 yesil kare, 52x52 saydam alanin
            // ortasinda. Basma efekti de burada - dis dugmede olsaydi
            // dokunusta 52x52 yesil bir kare yanip sonerdi.
            VisualElement yuz = new VisualElement();
            yuz.style.width = 30;
            yuz.style.height = 30;
            yuz.style.backgroundColor = Go;
            yuz.style.alignItems = Align.Center;
            yuz.style.justifyContent = Justify.Center;
            Theme.Round(yuz, 9);
            yuz.Add(Icons.Plus(Color.white, 14f));

            // Ic parca isareti YUTMEMELI: tiklama 52x52'lik dis
            // dugmeye gitmeli, yoksa buyutulen dokunma alani ise
            // yaramazdi.
            yuz.pickingMode = PickingMode.Ignore;
            b.Add(yuz);

            // BASMA EFEKTI DISARIDAN TETIKLENIP ICERIDE GORUNUYOR.
            //
            // Theme.PressFx kendi ogesini boyuyor; burada dinleyen ile
            // boyanan AYRI ogeler - ic parca isaret almiyor, dis dugme
            // ise saydam. O yuzden iki satir elle yaziliyor.
            Color basili = new Color(Go.r * 0.78f, Go.g * 0.78f, Go.b * 0.78f);
            b.RegisterCallback<PointerDownEvent>(_ =>
            {
                yuz.style.backgroundColor = basili;
                yuz.style.scale = new Scale(new Vector3(0.94f, 0.94f, 1f));
            });
            EventCallback<EventBase> birak = _ =>
            {
                yuz.style.backgroundColor = Go;
                yuz.style.scale = new Scale(Vector3.one);
            };
            b.RegisterCallback<PointerUpEvent>(e => birak(e));
            b.RegisterCallback<PointerLeaveEvent>(e => birak(e));
            b.RegisterCallback<PointerCancelEvent>(e => birak(e));
            return b;
        }

        // =====================================================================
        // SATIRLAR
        // =====================================================================

        /// <summary>
        /// KONTROL SATIRI: kutucuk + yazi. Referansin "Gunluk Hedefler"i.
        ///
        /// Oyunda gorev listesi yok; sabahin KONTROL LISTESI var (menu,
        /// stok, asci) ve servis sirasinda GUNUN AKISI. Ikisi de zaten
        /// vardi - biri tek satirlik kucuk yazi, digeri ust seritte
        /// sikismis uc sayiydi.
        /// </summary>
        public static VisualElement CheckRow(bool ok, string text, Color? renk = null)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = Theme.RowFlow;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 3;
            row.Add(Icons.Check(ok, renk ?? (ok ? GoDeep : Theme.Bad), 16f));
            Label t = Theme.Text(text, Theme.FontSmall,
                                 ok ? Theme.PlateInk : Theme.Bad);
            if (!ok) t.style.unityFontStyleAndWeight = FontStyle.Bold;
            t.style.marginLeft = 7;
            row.Add(t);
            return row;
        }

        /// <summary>
        /// Kartin icinde etiket solda, sayi sagda.
        ///
        /// SAYININ RENGINI CAGIRAN DEGIL BU METOT BELIRLIYOR.
        ///
        /// Cagiran `Theme.Ink` (0,94/0,93/0,90) ile etiket kuruyordu ve
        /// kartin zemini `Theme.Plate` (0,914/0,898/0,863): olculdu,
        /// kontrast 1,08:1. Yani "Bugun" kartindaki uc sayi - agirlanan,
        /// kizgin, dolu masa - servis boyunca krem uzerinde BEYAZDI.
        /// Kizgin halindeki `Theme.Bad` de 2,22:1.
        ///
        /// Ayni karttaki CheckRow dogru yapiyordu (PlateInk), yani
        /// kartin yarisi okunuyordu - hata gozle bu yuzden yakalanmadi.
        /// Renk secimi cagirana birakildigi surece bu sinif yeniden
        /// acilir; karar artik burada.
        /// </summary>
        /// <param name="uyari">Sayi dikkat cekmeli mi (kizgin musteri gibi).</param>
        public static VisualElement CountRow(VisualElement dot, string label,
                                             Label value, bool uyari = false)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = Theme.RowFlow;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 3;
            if (dot != null) row.Add(dot);

            Label t = Theme.Text(label, Theme.FontSmall, Theme.PlateDim);
            t.style.flexGrow = 1;
            row.Add(t);

            // ACIK ZEMIN -> KOYU MUREKKEP. BadDeep, Plate uzerinde
            // 5,4:1; Theme.Bad 2,22:1 idi.
            value.style.color = uyari ? BadDeep : Theme.PlateInk;
            value.style.unityFontStyleAndWeight = FontStyle.Bold;
            value.style.marginLeft = 10;
            row.Add(value);
            return row;
        }

        /// <summary>
        /// Acik zeminde okunan KOYU kirmizi.
        ///
        /// Theme.Bad koyu panel icin secilmisti; acik "plaka" zemininde
        /// 2,22:1 veriyor ve WCAG tabani 4,5:1. GoDeep'in kirmizi esi.
        /// </summary>
        public static readonly Color BadDeep = new Color(0.647f, 0.114f, 0.114f);
    }
}
