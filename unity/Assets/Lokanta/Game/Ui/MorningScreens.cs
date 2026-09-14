using Lokanta.Core;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// Icinde kaydirilabilir bir liste olan tam ekran panel.
    ///
    /// Ortak taban, cunku dort sabah ekrani da ayni sekilde: baslik,
    /// liste, altta kapat. Her birinde ayri yazmak, dordunun zamanla
    /// birbirinden ayrismasi demek.
    /// </summary>
    public abstract class ListScreen : UiScreen
    {
        protected abstract string Title { get; }
        protected virtual string Subtitle { get { return null; } }
        protected abstract void Fill(VisualElement list);

        public override VisualElement Build()
        {
            VisualElement outer = new VisualElement();
            outer.style.flexGrow = 1;
            outer.style.backgroundColor = new Color(Theme.Bg.r, Theme.Bg.g, Theme.Bg.b, 0.97f);
            outer.style.alignItems = Align.Center;

            // Okunur genislik siniri. 1280 genisliginde bir telefonda
            // ekrani bastan basa kaplayan bir satirda goz, sol bastaki
            // etiketten sag bastaki degere gidemiyor.
            VisualElement root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.width = Length.Percent(100);
            root.style.maxWidth = Theme.ReadWidth;

            // --- baslik -------------------------------------------------------
            // Baslik ve alt satir EZILMEZ. Kaydirilabilir liste butun
            // bos alani aliyor ve varsayilan flex-shrink 1 ile baslik
            // ekranin disina tasiniyordu: ilk masaustu yapisinda "Hal"
            // basligi hic gorunmuyor, alt satiri da yariya kesiliyordu.
            // Seritlerin ZEMINI var. Saydam birakildiginda kaydirilan
            // liste onlarin altindan gorunuyor ve "Tamam" dugmesinin
            // etrafinda yarim kalmis kartlar okunuyordu.
            VisualElement head = new VisualElement();
            head.style.flexShrink = 0;
            head.style.backgroundColor = Theme.Bg;
            head.style.paddingLeft = Theme.Pad;
            head.style.paddingRight = Theme.Pad;
            head.style.paddingTop = Theme.Pad;
            head.style.paddingBottom = Theme.Gap;
            head.Add(Theme.Title(Title));
            if (!string.IsNullOrEmpty(Subtitle))
                head.Add(Theme.Text(Subtitle, Theme.FontSmall, Theme.InkDim));
            root.Add(head);

            // --- liste --------------------------------------------------------
            ScrollView scroll = Theme.Mobile(new ScrollView(ScrollViewMode.Vertical));
            scroll.style.flexGrow = 1;

            // minHeight SIFIR olmali.
            //
            // Esnek yerlesimde bir ogenin en az yuksekligi varsayilan
            // olarak ICERIGI kadardir; yani uzun bir listeyi tasiyan
            // kaydirma alani kuculmeyi REDDEDIYOR. Sonuc: alan alt
            // seridin altina tasiyor ve kartlar alt dugmenin arkasindan
            // devam ediyordu.
            scroll.style.minHeight = 0;

            scroll.style.paddingLeft = Theme.Pad;
            scroll.style.paddingRight = Theme.Pad;

            VisualElement list = Theme.Column(Theme.Gap);
            list.style.flexShrink = 0;

            // ALT DOLGU: son kart, sabit alt seridin arkasinda kalmasin.
            // Kaydirma sonuna gelindiginde son kartin dugmesi cubugun
            // altinda kaliyordu ve basilamiyordu.
            list.style.paddingBottom = Theme.Touch + Theme.Pad;

            Fill(list);
            scroll.Add(list);
            root.Add(scroll);

            // --- alt ----------------------------------------------------------
            VisualElement foot = new VisualElement();
            foot.style.flexShrink = 0;
            foot.style.backgroundColor = Theme.Bg;
            foot.style.paddingLeft = Theme.Pad;
            foot.style.paddingRight = Theme.Pad;
            foot.style.paddingTop = Theme.Gap;
            foot.style.paddingBottom = Theme.Pad;
            // "TAMAM" VURGULU DEGIL. Bir ekrani kapatmak tavsiye
            // edilen eylem degil - oyuncu zaten oraya bir sey yapmaya
            // geldi. Vurgulu olunca alisveris ekranindaki en gurultulu
            // dugme, ekrani KAPATAN dugme oluyordu.
            foot.Add(Theme.Btn(Loc.T("ui.common.ok"), () => Ui.Pop(), wide: true));
            root.Add(foot);

            outer.Add(root);
            return outer;
        }
    }

    /// <summary>
    /// Hal. docs/12 3: "ucuz gune denk gelmek sans degil, TAKIP meselesi."
    ///
    /// O yuzden ekranin isi FIYATI KARSILASTIRMAK: bugunku fiyatin yil
    /// ortalamasina gore nerede durdugu, ve malzemenin dayanip
    /// dayanmadigi. Ucuz bir gun, saklayabiliyorsan firsat; saklayamiyorsan
    /// yalnizca bir gider oynamasi.
    /// </summary>
    public sealed class MarketScreen : ListScreen
    {
        protected override string Title { get { return Loc.T("ui.morning.market"); } }
        protected override string Subtitle
        {
            get
            {
                return string.Format("{0}: {1}   ·   {2}",
                    Loc.T("ui.hud.cash"), Loc.Money(App.Sim.Cash),
                    App.Sim.StorageTier > 0
                        ? Loc.T("ui.morning.storage_tier", App.Sim.StorageTier)
                        : Loc.T("ui.morning.storage_none"));
            }
        }

        protected override void Fill(VisualElement list)
        {
            Simulation sim = App.Sim;

            list.Add(Theme.Btn(Loc.T("ui.morning.restock"), () =>
            {
                App.RestockRecommended();
                Ui.Refresh();
            }, primary: true, wide: true));

            for (int i = 0; i < sim.IngredientCount; i++)
            {
                // Yalnizca BU MUTFAKTA kullanilan ve bugun anlamli olan
                // malzemeler. Yetmis yedi satir, kirk dokunusluk bir gunde
                // okunamaz (docs/16).
                int need = sim.RecommendedRestock(i);
                int stock = sim.StockOf(i);
                if (need <= 0 && stock <= 0) continue;

                list.Add(Row(i, need, stock));
            }
        }

        private VisualElement Row(int i, int need, int stock)
        {
            Simulation sim = App.Sim;
            IngredientDef def = App.Content.Ingredients[i];

            long today = sim.IngredientPriceToday(i);
            long mean = sim.IngredientPriceMean(i);
            int bp = mean > 0 ? (int)(today * 10000 / mean) : 10000;

            VisualElement card = Theme.PanelBox();

            VisualElement head = Theme.Row(0);
            head.style.justifyContent = Justify.SpaceBetween;
            head.Add(Theme.Text(Loc.T(def.NameKey), Theme.FontBody));

            string tag;
            Color tagColor;
            if (bp <= 9200) { tag = Loc.T("ui.morning.cheap"); tagColor = Theme.Good; }
            else if (bp >= 10800) { tag = Loc.T("ui.morning.expensive"); tagColor = Theme.Bad; }
            else { tag = "—"; tagColor = Theme.InkFaint; }
            head.Add(Theme.Text(tag, Theme.FontSmall, tagColor));
            card.Add(head);

            // Fiyat, yil ortalamasina gore FARK olarak: mutlak iki sayi
            // ayni karsilastirmayi iki kez anlatiyordu ve kart uzuyordu.
            VisualElement facts = Theme.Row(Theme.Pad);
            facts.style.justifyContent = Justify.SpaceBetween;
            facts.Add(Theme.Text(Loc.Money(today) + " / kg", Theme.FontBody, Theme.Ink));
            // "Yil ortalamasi +%9" CUMLE DEGILDI: ortalamanin kendisi
            // %9'mus gibi okunuyordu. Simdi ne oldugunu soyluyor.
            int diffPct = (bp - 10000) / 100;
            facts.Add(Theme.Text(
                diffPct == 0
                    ? Loc.T("ui.morning.average_same")
                    : Loc.T(diffPct > 0 ? "ui.morning.average_over"
                                        : "ui.morning.average_under",
                            diffPct > 0 ? diffPct : -diffPct),
                Theme.FontSmall, Theme.InkDim));
            facts.Add(Theme.Text(Loc.T("ui.morning.stock") + " " + stock + " g",
                                 Theme.FontSmall, Theme.InkDim));
            card.Add(facts);

            // Raf omru uc durum: hic bozulmayan (tuz, un, yag), soguk
            // havayla gun sayisi, ve gece biten.
            if (!sim.IsPerishable(i))
            {
                facts.Add(Theme.Text(Loc.T("ui.morning.never_spoils"),
                                     Theme.FontSmall, Theme.Good));
            }
            else
            {
                bool keeps = sim.CanKeep(i);
                facts.Add(Theme.Text(
                    keeps ? sim.KeepDays(i) + " " + Loc.T("ui.morning.days_keep")
                          : Loc.T("ui.morning.spoils_tonight"),
                    Theme.FontSmall, keeps ? Theme.Good : Theme.Warn));
            }

            // --- MIKTAR KARARI -------------------------------------------
            //
            // Once tek bir dugme vardi ve tam olarak "onerilen" kadar
            // aliyordu. Yani "bugun ucuz" etiketi cikinca oyuncu HICBIR SEY
            // yapamiyordu: fazladan alamiyor, saklayamiyordu. Ayni sebeple
            // soguk hava deposu yukseltmesinin de karsiligi yoktu - daha
            // uzun saklayacak fazla mali alamiyorsun.
            //
            // Simdi gun cinsinden aliniyor ve tavani soguk hava kademesi
            // belirliyor: bozulmayanda uc gun, bozulabilende sakladigi
            // kadar.
            int daily = sim.DailyNeed(i);
            int maxDays = sim.MaxUsefulDays(i);

            if (daily > 0)
            {
                VisualElement buys = Theme.Row(Theme.Gap);
                for (int day = 1; day <= maxDays; day++)
                {
                    int want = daily * day - stock;
                    if (want <= 0) continue;

                    long cost = Fx.MulDiv(today, want, 1000);

                    // BIR SIKKENIN ALTI DUGME OLMAZ.
                    //
                    // Stok neredeyse doluyken kalan ihtiyac birkac grama
                    // iniyor: tuzda uc gunluk eksik 2 gram, yani 2 santi.
                    // Ekranda "3 gunluk - 0 [sikke]" yazan bir dugme
                    // kaliyordu - bedava gorunuyor, basinca hicbir sey
                    // degismiyor. Esik "sifir" degil "BIR SIKKE" olmali,
                    // cunku arayuz sikke gosteriyor ve 99 santi de 0
                    // yaziyor.
                    //
                    // Bu kalemler kaybolmuyor: "Onerilen stogu al"
                    // hepsini birden aliyor.
                    if (cost < 100) continue;
                    int amount = want;
                    Button b = Theme.Btn(
                        Loc.T("ui.morning.buy_days", day, Loc.Money(cost)),
                        () =>
                        {
                            App.Send(CommandKind.OrderIngredient, i, amount);
                            Sfx.Coin();
                            Ui.Refresh();
                        }, primary: day == 1 && need > 0, wide: true);
                    b.SetEnabled(sim.Cash >= cost);
                    buys.Add(b);
                }
                if (buys.childCount > 0) card.Add(buys);
            }

            if (maxDays == 1 && sim.IsPerishable(i))
                card.Add(Theme.Text(Loc.T("ui.morning.no_storage"),
                                    Theme.FontSmall, Theme.InkFaint));

            return card;
        }
    }

    /// <summary>
    /// Menu tahtasi.
    ///
    /// Iki karar var ve ikisi de gorunur olmali: hangi yemek MENUDE, ve
    /// kilitli olanlar NEDEN kilitli. Kilidi sebepsiz gostermek, oyuncuya
    /// "bir sey eksik" deyip ne oldugunu soylememek olur.
    /// </summary>
    public sealed class MenuBoardScreen : ListScreen
    {
        protected override string Title { get { return Loc.T("ui.morning.menu"); } }
        protected override string Subtitle
        {
            get { return Loc.T("ui.menu.subtitle"); }
        }

        /// <summary>Su an acilmis yemek. -1: hicbiri.</summary>
        private int _open = -1;

        /// <summary>Kilitli liste acik mi.</summary>
        private bool _showLocked;

        /// <summary>
        /// Liste SIRALI ve SIKISTIRILMIS.
        ///
        /// Olculdu (873x393 gercek telefon penceresinde alinan tur
        /// goruntusu): ekranda TEK yemek karti goruunuyordu, otuz iki
        /// yemeklik bir listede. Ustelik liste ham icerik sirasindaydi;
        /// oyuncu birinci gun ekrani actiginda bir acik yemek ve hemen
        /// altinda ART ARDA ALTI KILITLI satir goruyordu - "bu oyunun
        /// %90'i kilitli" diyen bir ekran.
        ///
        /// Uc degisiklik, sifir yeni mekanik:
        ///   1. SIRA: menude olanlar, sonra acik olanlar, sonra yakinda
        ///      acilacak IKI tane, sonra kilitliler katlanmis.
        ///   2. SIKISTIRMA: yalnizca DOKUNULAN yemek kart olarak
        ///      aciliyor; digerleri tek satir. Boylece ekrana bir yemek
        ///      yerine yedi sekiz yemek giriyor.
        ///   3. BASLIK: her bolumun ustunde ne oldugu yaziyor.
        /// </summary>
        protected override void Fill(VisualElement list)
        {
            Simulation sim = App.Sim;

            var menude = new System.Collections.Generic.List<int>();
            var acik = new System.Collections.Generic.List<int>();
            var kilitli = new System.Collections.Generic.List<int>();

            for (int i = 0; i < sim.DishCount; i++)
            {
                if (!sim.IsUnlocked(i)) kilitli.Add(i);
                else if (sim.IsOnMenu(i)) menude.Add(i);
                else acik.Add(i);
            }

            // Kilitliler ACILIS GUNUNE gore: "yakinda" gercekten yakin
            // olani gostersin.
            kilitli.Sort((a, b) => App.Content.Dishes[a].UnlockDay
                                     .CompareTo(App.Content.Dishes[b].UnlockDay));

            if (menude.Count > 0)
            {
                list.Add(Heading(Loc.T("ui.menu.group_on") + " (" + menude.Count + ")"));
                foreach (int i in menude) list.Add(Row(i));
            }
            if (acik.Count > 0)
            {
                list.Add(Heading(Loc.T("ui.menu.group_open") + " (" + acik.Count + ")"));
                foreach (int i in acik) list.Add(Row(i));
            }

            int yakin = kilitli.Count < 2 ? kilitli.Count : 2;
            if (yakin > 0)
            {
                list.Add(Heading(Loc.T("ui.menu.group_soon")));
                for (int k = 0; k < yakin; k++) list.Add(Row(kilitli[k]));
            }

            int kalan = kilitli.Count - yakin;
            if (kalan > 0)
            {
                Button more = Theme.Btn(
                    Loc.T("ui.menu.group_locked") + " (" + kalan + ")"
                    // Ok isareti DEGIL yazi: yazi tipi kapsama
                    // denetimi U+25BE'yi yakaladi - paketin yazi tipinde
                    // o glif yok ve kutu olarak cizilirdi.
                    + (_showLocked ? "  gizle" : "  goster"),
                    () => { _showLocked = !_showLocked; Ui.Refresh(); });
                more.style.marginTop = Theme.Gap;
                list.Add(more);

                if (_showLocked)
                    for (int k = yakin; k < kilitli.Count; k++) list.Add(Row(kilitli[k]));
            }
        }

        private static Label Heading(string text)
        {
            Label l = Theme.Text(text, Theme.FontSmall, Theme.InkDim);
            l.style.marginTop = Theme.Gap;
            l.style.marginBottom = 2;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            return l;
        }

        /// <summary>
        /// Kapali yemek satiri: ad, fiyat, durum. Dokununca aciliyor.
        ///
        /// Kart 150 dp; bu satir 40 dp. Ayni ekrana bir yemek yerine
        /// yedi sekiz yemek giriyor ve oyuncu menusunu BIR BAKISTA
        /// goruyor - menu ekraninin butun isi bu.
        /// </summary>
        private VisualElement CompactRow(int i)
        {
            Simulation sim = App.Sim;
            DishDef d = App.Content.Dishes[i];

            // SATIRIN KENDISI DUGME.
            //
            // Ayri bir "ac" dugmesi koymak satiri 64 dp yapiyordu
            // (dokunma hedefi icin dugme yuksekligi 52) ve 219 dp'lik
            // gorunur alana yalnizca uc satir giriyordu. Satirin
            // tamami dokunulabilir olunca hem satir 44 dp'ye iniyor hem
            // de dokunma hedefi BUYUYOR - 873 dp genisliginde bir serit.
            Button row = new Button(() => { _open = i; Ui.Refresh(); });
            row.text = string.Empty;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = Theme.Pad;
            row.style.paddingRight = Theme.Pad;
            row.style.paddingTop = 5;
            row.style.paddingBottom = 5;
            row.style.marginTop = 0;
            row.style.marginBottom = 2;
            row.style.marginLeft = 0;
            row.style.marginRight = 0;
            row.style.backgroundColor = Theme.Panel;
            row.style.borderTopWidth = 0;
            row.style.borderBottomWidth = 0;
            row.style.borderLeftWidth = 0;
            row.style.borderRightWidth = 0;

            row.Add(Theme.Text(Loc.T(d.NameKey), Theme.FontBody));

            int fan = sim.FavouriteRegularOf(i);
            if (fan >= 0) row.Add(Theme.Dot(Theme.Accent, 8f));

            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            row.Add(spacer);

            row.Add(Theme.Text(Loc.Money(sim.DishPrice(i)), Theme.FontBody, Theme.InkDim));

            if (sim.IsOnMenu(i))
                row.Add(Theme.Text(Loc.T("ui.menu.onmenu"), Theme.FontSmall, Theme.Good));

            row.Add(Theme.Text("›", Theme.FontBody, Theme.InkFaint));
            return row;
        }

        private VisualElement Row(int i)
        {
            Simulation sim = App.Sim;
            DishDef d = App.Content.Dishes[i];
            bool unlocked = sim.IsUnlocked(i);

            // Acilmamis olan tek satir; acilan tam kart.
            if (unlocked && i != _open) return CompactRow(i);

            // KILITLI YEMEK KART DEGIL, TEK SATIR.
            //
            // Olculdu: acik bir yemek karti hedef telefonda 232 dp ve
            // listenin gorunur alani 219 dp - yani BIR yemek bile
            // sigmiyordu, otuz iki yemeklik bir listede. Kilitli
            // yemekler de tam kart kapliyordu ve tasidiklari bilgi iki
            // satirdi: adi, ve ne zaman acilacagi.
            if (!unlocked) return LockedRow(i, d);

            VisualElement card = Theme.PanelBox();

            VisualElement head = Theme.Row(Theme.Gap);
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;

            // Ad ve istasyon TEK SATIR. Alt alta konulduklarinda kart
            // 250 dp'yi asiyordu ve 393 dp'lik bir telefonda ekrana TEK
            // kart sigiyordu - bir liste olmaktan cikiyordu.
            VisualElement left = Theme.Row(Theme.Gap);
            left.style.alignItems = Align.Center;
            left.Add(Theme.Text(Loc.T(d.NameKey), Theme.FontBody));
            left.Add(Theme.Text(Loc.T("station." + App.Content.Stations[d.StationIndex].Id),
                                Theme.FontSmall, Theme.InkFaint));
            head.Add(left);

            // SEVILEN YEMEK: NOKTA DEGIL AD.
            //
            // Once turuncu bir nokta ve bir tooltip vardi. DOKUNMATIK
            // EKRANDA TOOLTIP HIC GORUNMEZ - yani hikaye ile mekanigin
            // bulustugu tek menu sinyali fiilen gorunmuyordu. Metin
            // ("{0} bunu seviyor") tabloda zaten YAZILIYDI ve hicbir
            // kod onu okumuyordu.
            //
            // Nokta duruyor: bir bakista taranabilen isaret o. Yaninda
            // artik kimin sevdigi yaziyor.
            int fan = sim.FavouriteRegularOf(i);
            if (fan >= 0)
            {
                head.Add(Theme.Dot(Theme.Accent, 10f));
                head.Add(Theme.Text(
                    Loc.T("ui.menu.favourite_of", Loc.T(App.Content.Regulars[fan].NameKey)),
                    Theme.FontSmall, Theme.Accent));
            }

            // MENUDE ANAHTARI BASLIK SATIRINDA VE VURGU RENGI DEGIL.
            //
            // Once kartin altinda TAM GENISLIKTE bir turuncu cubuktu.
            // Iki sorun birden: karta 62 dp ekliyordu, ve vurgu rengini
            // dokuzuncu bir ise kosuyordu - ayni turuncu hem "tek
            // onerilen eylem" hem "bu acik" demek olamaz. Acik olan
            // anahtar artik YESIL YAZIYLA isaretleniyor; renk bir DURUM
            // bildiriyor, bir cagri degil.
            bool on = sim.IsOnMenu(i);
            Button toggle = Theme.Btn(
                on ? Loc.T("ui.menu.onmenu") : Loc.T("ui.menu.add"),
                () =>
                {
                    App.Send(CommandKind.SetMenuSlot, i, on ? 0 : 1);
                    Ui.Refresh();
                });
            if (on) toggle.style.color = Theme.Good;
            head.Add(toggle);

            card.Add(head);


            long price = sim.DishPrice(i);
            long cost = d.IngredientCost;
            long market = sim.BasePriceOf(i);
            int marginBp = price > 0 ? (int)((price - cost) * 10000 / price) : 0;
            int vsMarketBp = market > 0 ? (int)((price - market) * 10000 / market) : 0;

            // --- FIYAT KARARI --------------------------------------------
            //
            // Bu satir oyunun en patronca karariydi ve arayuzde YOKTU:
            // SetPrice komutu cekirdekte yazili ve uygulaniyor, fiyat
            // duyarliligi formulu docs/12'de, arketip basina katsayilar
            // icerikte, denge aracinda "yuksek_fiyat" diye bir strateji
            // bile var - ama oyuncu bu eksene dokunamiyordu. Bir tycoon
            // oyununda fiyat koyamamak, yaris oyununda direksiyon
            // olmamasidir.
            //
            // Adim PIYASANIN %5'i: mutlak bir sikke adimi ucuz bir icecekte
            // devasa, pahali bir ana yemekte gorunmez olurdu.
            long step = market / 20;
            if (step < 50) step = 50;                 // en az yarim sikke

            VisualElement priceRow = Theme.Row(Theme.Gap);
            priceRow.style.alignItems = Align.Center;

            Label priceLabel = Theme.Text(Loc.T("ui.menu.price"), Theme.FontBody, Theme.InkDim);
            priceLabel.style.flexGrow = 1;
            priceRow.Add(priceLabel);

            Button down = Theme.Btn("−", () =>
            {
                App.Send(CommandKind.SetPrice, i, (int)System.Math.Max(step, price - step));
                Sfx.Click();
                Ui.Refresh();
            });
            down.SetEnabled(price - step >= step);
            priceRow.Add(down);

            Label priceText = Theme.Text(Loc.Money(price), Theme.FontBody, Theme.Ink);
            priceText.style.unityFontStyleAndWeight = FontStyle.Bold;
            priceText.style.minWidth = 96;
            priceText.style.unityTextAlign = TextAnchor.MiddleCenter;
            priceRow.Add(priceText);

            Button up = Theme.Btn("+", () =>
            {
                App.Send(CommandKind.SetPrice, i, (int)(price + step));
                Sfx.Click();
                Ui.Refresh();
            });
            priceRow.Add(up);
            card.Add(priceRow);

            // Piyasaya gore nerede duruyoruz - kararin GERI BILDIRIMI.
            // Sayinin kendisi bilgi degil; "piyasanin %12 ustunde" bilgi.
            // Uc sayi TEK SATIRDA: piyasaya gore, maliyet, marj. Uc ayri
            // satir, kartin yuksekliginin yarisini yiyordu.
            VisualElement facts = Theme.Row(Theme.Pad);
            facts.style.justifyContent = Justify.SpaceBetween;
            facts.Add(Theme.Text(
                Loc.T("ui.menu.vs_market") + " "
                + Loc.Percent(vsMarketBp, isaretli: true),
                Theme.FontSmall, MarketColor(vsMarketBp)));
            facts.Add(Theme.Text(Loc.T("ui.menu.cost") + " " + Loc.Money(cost),
                                 Theme.FontSmall, Theme.InkDim));
            facts.Add(Theme.Text(Loc.T("ui.menu.margin") + " " + Loc.Percent(marginBp),
                                 Theme.FontSmall,
                                 marginBp >= 6000 ? Theme.Good : Theme.Warn));
            card.Add(facts);

            return card;
        }

        /// <summary>
        /// Kilitli yemek: tek satir, kutu yok. Ad, istasyon ve KILIDIN
        /// SEBEBI yan yana.
        /// </summary>
        private VisualElement LockedRow(int i, DishDef d)
        {
            Simulation sim = App.Sim;

            VisualElement row = Theme.Row(Theme.Gap);
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = Theme.Pad;
            row.style.paddingRight = Theme.Pad;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.opacity = 0.72f;

            row.Add(Theme.Text(Loc.T(d.NameKey), Theme.FontBody, Theme.InkDim));
            row.Add(Theme.Text(Loc.T("station." + App.Content.Stations[d.StationIndex].Id),
                               Theme.FontSmall, Theme.InkFaint));

            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            row.Add(spacer);

            string sebep;
            if (d.UnlockDay > sim.Day)
                sebep = Loc.T("ui.menu.unlock_day", d.UnlockDay);
            else if (sim.ReputationCenti < d.UnlockReputationCenti)
                sebep = Loc.T("ui.menu.needs_reputation",
                              Loc.Reputation(d.UnlockReputationCenti));
            else if (d.RequiresStationTier > sim.StationTier(d.StationIndex))
                sebep = Loc.T("ui.menu.needs_equipment",
                              Loc.T("station." + App.Content.Stations[d.StationIndex].Id));
            else
                sebep = Loc.T("ui.menu.locked");

            row.Add(Theme.Text(sebep, Theme.FontSmall, Theme.Warn));
            return row;
        }

        /// <summary>
        /// Piyasaya gore fiyat rengi. Yesil ucuz, sari pahali, kirmizi cok
        /// pahali - esikler docs/12 5.3 fiyat duyarliligi kirilmalariyla
        /// ayni yerde duruyor.
        /// </summary>
        private static Color MarketColor(int bp)
        {
            if (bp > 1200) return Theme.Bad;
            if (bp > 500) return Theme.Warn;
            if (bp < -800) return Theme.InkDim;
            return Theme.Good;
        }
    }

    /// <summary>
    /// Personel. docs/14: uc aday gorunur, oyuncu secer.
    ///
    /// Adaylarin HUYLARI acikca yaziyor - secim ancak neyi sectigini
    /// gorursen karar olur. Kor bir ise alim, olculdu ve iyi oyuncunun
    /// itibarini 96,5'ten 87'ye indirdi (docs/34 21).
    /// </summary>
    public sealed class StaffScreen : ListScreen
    {
        private int _confirmFire = -1;

        protected override string Title { get { return Loc.T("ui.morning.staff"); } }
        protected override string Subtitle
        {
            get
            {
                // "GEREKEN" DEGIL "HERKESE YETISMEK ICIN".
                //
                // Sayi bir kapasite hesabi: bugunku talebin TAMAMINA
                // yetismek icin kac kisi lazim. Kar icin en iyi sayi
                // DEGIL - denge araci olctu: bir garson eksik calisan
                // oyuncu 60 gunde yaklasik 4.000 sikke daha fazla
                // kazaniyor ve karsiliginda yuze yakin kisiyi
                // agirlayamiyor.
                //
                // ITIBAR DEGISMIYOR. Bu yorum bir sure "itibari biraz
                // dusuyor" diyordu; docs/42 §4 onu OLCUP curuttu (78 /
                // 78). Dusen sey EKIP puani - yani bedel yil sonu
                // degerlendirmesinde, gunluk itibarda degil.
                //
                // Yani burada bir KARAR var ve oyun onu oyuncuya
                // birakmali. "Gereken" demek karari gizliyordu:
                // oyuncu sayiyi tutturuyor, parayi kaybediyor ve neden
                // kaybettigini hicbir yerden ogrenemiyordu.
                //
                // Hafta sonu ayrica yaziyor: kadro karari hafta icine
                // degil HAFTAYA bakilarak verilir.
                Crew need = App.Sim.RequiredCrewToday();
                Crew peak = App.Sim.RequiredCrewPeak();
                string zirve = peak.Salon != need.Salon || peak.Cooks != need.Cooks
                    ? string.Format("   ·   {0}: {1} + {2}",
                                    Loc.T("ui.staff.weekend"), peak.Cooks, peak.Salon)
                    : string.Empty;
                return string.Format("{0}: {1} + {2}{3}   ·   {4}: {5}",
                    Loc.T("ui.staff.to_serve_all"), need.Cooks, need.Salon, zirve,
                    Loc.T("ui.staff.cap"), App.Sim.StaffCap);
            }
        }

        protected override void Fill(VisualElement list)
        {
            list.Add(Theme.Head(Loc.T("ui.staff.cook")));
            for (int i = 0; i < App.Sim.Cooks; i++) list.Add(Person(0, i));

            list.Add(Theme.Head(Loc.T("ui.staff.salon")));
            for (int i = 0; i < App.Sim.SalonStaff; i++) list.Add(Person(1, i));

            // BOS DURUM yaziliyor. Once basligin altinda hicbir sey
            // yoktu ve oyuncu uc seyi ayirt edemiyordu: kadro sifir mi,
            // liste yuklenemedi mi, hata mi.
            if (App.Sim.SalonStaff == 0)
            {
                Crew n = App.Sim.RequiredCrewToday();
                list.Add(n.Salon > 0
                    ? Theme.Text(Loc.T("ui.staff.salon_needed", n.Salon),
                                 Theme.FontSmall, Theme.Warn)
                    : Theme.Text(Loc.T("ui.staff.salon_none"),
                                 Theme.FontSmall, Theme.InkDim));
            }

            list.Add(Theme.Divider());
            list.Add(SinkDuty());

            list.Add(Theme.Divider());
            list.Add(Theme.Head(Loc.T("ui.staff.candidates")));

            bool any = false;
            for (int pool = 0; pool < 2; pool++)
                for (int slot = 0; slot < Simulation.CandidateSlots; slot++)
                    if (App.Sim.CandidateTrait(pool, slot, 0) >= 0)
                    {
                        list.Add(Candidate(pool, slot));
                        any = true;
                    }
            if (!any)
                list.Add(Theme.Text(Loc.T("ui.staff.no_candidate"),
                                    Theme.FontSmall, Theme.InkDim));
        }

        /// <summary>
        /// BULASIK NOBETI.
        ///
        /// Kullanicinin cumlesi: "bulasikci alinca herkes kendi isini
        /// yapar". Bulasikci ayri bir kadro DEGIL - docs/14 salonu
        /// "garson + bulasikci + kasiyer, tek is havuzu" diye kuruyor ve
        /// maas o harmandan geliyor. Ayri bir havuz, ayni kisiyi iki
        /// ucret tablosunda saymak olurdu.
        ///
        /// Oyuncunun karari ayni: bir kisilik kadroyu lavaboya ayiriyor.
        /// Ayirmazsa bulasik birikince garson kendiliginden lavaboya
        /// geciyor ve servis aksiyor - bedeli gorunur, secim gercek.
        /// </summary>
        private VisualElement SinkDuty()
        {
            VisualElement card = Theme.PanelBox();
            card.Add(Theme.Head(Loc.T("ui.staff.sink")));

            int n = App.Sim.Dishwashers;
            card.Add(n > 0
                ? Theme.Text(Loc.T("ui.staff.sink_n", n), Theme.FontSmall, Theme.Ink)
                : Theme.Text(Loc.T("ui.staff.sink_none"), Theme.FontSmall, Theme.InkDim));

            VisualElement row = Theme.Row(Theme.Gap);
            Button az = Theme.Btn(Loc.T("ui.staff.sink_remove"), () =>
            {
                App.Send(CommandKind.SetDishwashers, App.Sim.Dishwashers - 1);
                Ui.Refresh();
            });
            az.SetEnabled(n > 0);
            row.Add(az);

            Button cok = Theme.Btn(Loc.T("ui.staff.sink_add"), () =>
            {
                App.Send(CommandKind.SetDishwashers, App.Sim.Dishwashers + 1);
                Ui.Refresh();
            });
            // Salon kadrosunun TAMAMI lavaboya verilemiyor: o zaman kimse
            // servis yapmaz. Cekirdek de ayni sinirla kirpiyor; dugmenin
            // kapali olmasi, reddedilen bir dokunustan iyi.
            cok.SetEnabled(n < App.Sim.SalonStaff);
            row.Add(cok);
            card.Add(row);

            card.Add(Theme.Text(Loc.T("ui.staff.sink_hint"),
                                Theme.FontSmall, Theme.InkFaint));

            // TEMIZ TABAK: darbogazin sayisi. Oyuncunun "neden mutfak
            // bekliyor" sorusunun cevabi burada duruyor.
            card.Add(Theme.Field(Loc.T("ui.hud.plates"),
                                 App.Sim.PlatesClean + " / " + App.Sim.PlatesTotal));
            return card;
        }

        private VisualElement Person(int pool, int index)
        {
            Simulation sim = App.Sim;
            VisualElement card = Theme.PanelBox();

            VisualElement head = Theme.Row(0);
            head.style.justifyContent = Justify.SpaceBetween;
            // ADI varsa AD, yoksa sirali etiket.
            //
            // "Asci 2 istifa etti" cumlesi "kapasite -28" ile ayni sey;
            // "Nurten Abla birakti" degil. Personel sisteminin butun
            // duygusal agirligi bu tek satirda.
            string name = sim.StaffName(pool, index);
            head.Add(Theme.Text(
                string.IsNullOrEmpty(name)
                    ? Loc.T(pool == 0 ? "role.asci" : "role.garson") + " " + (index + 1)
                    : name,
                Theme.FontBody));

            int morale = sim.StaffMorale(pool, index);
            head.Add(Theme.Text(Loc.T("ui.staff.morale") + " " + morale,
                                Theme.FontSmall, MoraleColor(morale)));
            card.Add(head);

            card.Add(Theme.Text(TraitText(pool, index), Theme.FontSmall, Theme.InkDim));
            card.Add(Theme.Field(Loc.T("ui.staff.level"),
                                 Loc.T("ui.staff.days",
                                       sim.StaffLevel(pool, index),
                                       sim.StaffDaysWorked(pool, index)),
                                 Theme.InkDim));

            // Kartin SAHIBI cikariliyor ve ONAY isteniyor.
            //
            // Once indis gonderilmiyordu: "Asci 2" kartindaki dugme Asci
            // 1'i cikariyordu. Ustelik onay da yoktu - tek dokunusla,
            // seviye ve gun biriktirmis bir personel gidiyordu.
            int key = pool * 100 + index;
            if (_confirmFire == key)
            {
                card.Add(Theme.Text(Loc.T("ui.staff.fire_confirm"),
                                    Theme.FontSmall, Theme.Bad));
                // VAZGEC ONCE VE VURGULU, KOV KIRMIZI.
                VisualElement ask = Theme.Row(Theme.Gap);
                ask.Add(Theme.Btn(Loc.T("ui.common.cancel"),
                    () => { _confirmFire = -1; Ui.Refresh(); },
                    primary: true, wide: true));
                ask.Add(Theme.Btn(Loc.T("ui.staff.fire"), () =>
                {
                    App.Send(CommandKind.Fire, pool, index);
                    Sfx.Cancel();
                    _confirmFire = -1;
                    Ui.Refresh();
                }, wide: true, danger: true));
                card.Add(ask);
            }
            else
            {
                card.Add(Theme.Btn(Loc.T("ui.staff.fire"), () =>
                {
                    _confirmFire = key;
                    Ui.Refresh();
                }, wide: true));
            }
            return card;
        }

        private VisualElement Candidate(int pool, int slot)
        {
            Simulation sim = App.Sim;
            VisualElement card = Theme.PanelBox();
            card.style.backgroundColor = Theme.PanelHi;

            card.Add(Theme.Head(Loc.T(pool == 0 ? "role.asci" : "role.garson")));

            // HUYUN ADI YETMIYOR, NE YAPTIGI LAZIM.
            //
            // Kart yalnizca adi ve %ucret/%hiz gosteriyordu. Hiza ve
            // ucrete dokunmayan dort huy ("Sakin", "Dayanikli",
            // "Musteriyle Iyi Anlasan", "Suratsiz") kartta
            // "normal / normal" diye goruunuyordu - yani oyuncu
            // hesabi alirken musteriyi +8 puan memnun eden biriyle
            // hicbir sey yapmayanini ayirt edemiyordu.
            for (int w = 0; w < 2; w++)
            {
                int t = sim.CandidateTrait(pool, slot, w);
                if (t < 0) continue;
                TraitDef d = App.Economy.TraitAt(t);
                card.Add(Theme.Text("• " + Loc.T(d.NameKey), Theme.FontSmall, Theme.Ink));
                Label ne = Theme.Text(Loc.T(d.NameKey + ".desc"),
                                      Theme.FontSmall, Theme.InkFaint);
                ne.style.marginLeft = 14;
                ne.style.whiteSpace = WhiteSpace.Normal;
                card.Add(ne);
            }

            int wage = sim.CandidateWageBp(pool, slot);
            int speed = sim.CandidateSpeedBp(pool, slot);
            card.Add(Theme.Field(Loc.T("ui.staff.wage"),
                                 (wage == 0 ? Loc.T("ui.staff.normal") : Loc.Percent(wage, isaretli: true)),
                                 wage > 0 ? Theme.Bad : (wage < 0 ? Theme.Good : Theme.InkDim)));
            card.Add(Theme.Field(Loc.T("ui.hud.speed"),
                                 (speed == 0 ? Loc.T("ui.staff.normal") : Loc.Percent(speed, isaretli: true)),
                                 speed > 0 ? Theme.Good : (speed < 0 ? Theme.Bad : Theme.InkDim)));

            bool room = sim.Cooks + sim.SalonStaff < sim.StaffCap;
            Button hire = Theme.Btn(Loc.T("ui.staff.hire"), () =>
            {
                App.Send(CommandKind.Hire, pool, slot);
                Sfx.Confirm();
                Ui.Refresh();
            }, primary: true, wide: true);
            hire.SetEnabled(room);
            card.Add(hire);
            if (!room)
                card.Add(Theme.Text(Loc.T("ui.staff.cap_full"), Theme.FontSmall, Theme.Warn));

            return card;
        }

        private string TraitText(int pool, int index)
        {
            string a = TraitName(App.Sim.StaffTrait(pool, index, 0));
            string b = TraitName(App.Sim.StaffTrait(pool, index, 1));
            if (a == null && b == null) return Loc.T("ui.staff.inherited");
            if (b == null) return a;
            return a + " · " + b;
        }

        private string TraitName(int trait)
        {
            return trait < 0 ? null : Loc.T(App.Economy.TraitAt(trait).NameKey);
        }

        private static Color MoraleColor(int m)
        {
            if (m >= 70) return Theme.Good;
            if (m >= 30) return Theme.Warn;
            return Theme.Bad;
        }
    }

    /// <summary>
    /// Ekipman. docs/27 Karar D: yukseltme ya yuva ekler ya asciyi erken
    /// birakir; yemegin pisme suresine DOKUNMAZ. Ekran da bunu boyle
    /// anlatiyor, cunku "hizlandirir" demek yanlis olurdu.
    /// </summary>
    public sealed class EquipmentScreen : ListScreen
    {
        protected override string Title { get { return Loc.T("ui.morning.equipment"); } }
        protected override string Subtitle
        {
            get { return Loc.T("ui.hud.cash") + ": " + Loc.Money(App.Sim.Cash); }
        }

        protected override void Fill(VisualElement list)
        {
            Simulation sim = App.Sim;

            // Soguk hava once: menu genisligi satin aliyor.
            long cold = sim.NextStoragePrice();
            VisualElement storage = Theme.PanelBox();
            storage.Add(Theme.Head(Loc.T("storage.soguk_hava")));
            storage.Add(Theme.Text(Loc.T("ui.storage.desc"),
                                   Theme.FontSmall, Theme.InkDim));
            storage.Add(Theme.Field(Loc.T("ui.common.tier"),
                                    sim.StorageTier.ToString(Loc.Culture)));
            if (cold >= 0)
            {
                Button coldBuy = Theme.Btn(Loc.T("ui.common.upgrade", Loc.Money(cold)), () =>
                {
                    App.Send(CommandKind.BuyStorage);
                    Sfx.Coin();
                    Ui.Refresh();
                }, wide: true);
                coldBuy.SetEnabled(sim.Cash >= cold);
                storage.Add(coldBuy);
            }
            else
                storage.Add(Theme.Text(Loc.T("ui.common.top_tier"), Theme.FontSmall, Theme.Good));
            list.Add(storage);

            list.Add(Theme.Divider());

            for (int st = 0; st < sim.StationCount; st++) list.Add(Station(st));

            // Genisleme en altta: en pahali ve en geri donulmez karar.
            list.Add(Theme.Divider());
            list.Add(Expansion());
        }

        private VisualElement Station(int st)
        {
            Simulation sim = App.Sim;
            StationDef def = App.Content.Stations[st];
            VisualElement card = Theme.PanelBox();

            VisualElement head = Theme.Row(0);
            head.style.justifyContent = Justify.SpaceBetween;
            head.Add(Theme.Head(Loc.T(def.NameKey)));
            // "mutfaga ozel" bir ETIKET, bir cagri degil - vurgu rengi
            // yerine soluk murekkep.
            if (!def.Shared)
                head.Add(Theme.Text(Loc.T("ui.station.cuisine_only"),
                                    Theme.FontSmall, Theme.InkFaint));
            card.Add(head);

            int tier = sim.StationTier(st);
            card.Add(Theme.Field(Loc.T("ui.common.tier"),
                                 tier + " / " + def.MaxTier, Theme.InkDim));
            card.Add(Theme.Field(Loc.T("ui.station.slots"),
                                 def.Tiers[tier].Slots.ToString(Loc.Culture),
                                 Theme.InkDim));

            int needed = sim.RequiredStationTier(st);
            if (needed > tier)
                card.Add(Theme.Text(Loc.T("ui.station.too_low"), Theme.FontSmall, Theme.Warn));

            long price = sim.NextEquipmentPrice(st);
            if (price < 0)
            {
                card.Add(Theme.Text(Loc.T("ui.common.top_tier"),
                                    Theme.FontSmall, Theme.Good));
                return card;
            }

            Button buy = Theme.Btn(Loc.T("ui.common.upgrade", Loc.Money(price)), () =>
            {
                App.Send(CommandKind.BuyEquipment, st);
                Sfx.Coin();
                Ui.Refresh();
            }, wide: true);
            buy.SetEnabled(sim.Cash >= price);
            card.Add(buy);
            return card;
        }

        private VisualElement Expansion()
        {
            Simulation sim = App.Sim;
            VisualElement card = Theme.PanelBox();
            card.Add(Theme.Head(Loc.T("ui.expand.title")));

            int tier = -1;
            for (int t = 0; t < sim.TierCount; t++)
                if (sim.TablesAtTier(t) == sim.TableCount) { tier = t; break; }

            if (tier < 0 || tier + 1 >= sim.TierCount)
            {
                card.Add(Theme.Text(Loc.T("ui.expand.last"), Theme.FontSmall, Theme.Good));
                return card;
            }

            long price = sim.UpgradeCostFor(tier + 1);
            card.Add(Theme.Field(Loc.T("ui.expand.tables"),
                                 sim.TableCount + " › " + sim.TablesAtTier(tier + 1)));
            card.Add(Theme.Text(Loc.T("ui.expand.warn"),
                                Theme.FontSmall, Theme.InkDim));

            Button buy = Theme.Btn(Loc.T("ui.expand.buy", Loc.Money(price)), () =>
            {
                // KADEME INDISI SART.
                //
                // Bu satir bir sure indissiz duruyordu ve komut sessizce
                // her seferinde reddediliyordu: varsayilan 0 kademesi
                // zaten dort masa, ve Expand "hedef kademe mevcuttan
                // buyuk olmali" diyor. Yani restoran HIC BUYUYEMIYORDU -
                // oyunun butun ilerleme ekseni kapaliydi ve hicbir sey
                // hata vermiyordu.
                App.Send(CommandKind.Expand, tier + 1);
                Sfx.Confirm();
                Ui.Refresh();
            }, wide: true);
            buy.SetEnabled(sim.Cash >= price);
            card.Add(buy);
            return card;
        }
    }
}
