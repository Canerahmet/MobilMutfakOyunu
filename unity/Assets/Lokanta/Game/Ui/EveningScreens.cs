using Lokanta.Core.Content;
using Lokanta.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// Gun raporu. docs/02 aksam asamasi.
    ///
    /// Ekranin isi rakam listelemek degil, GUNUN NE OLDUGUNU anlatmak:
    /// para nereye gitti, kim geldi, kim kirildi. Sadece ciro gostermek,
    /// oyuncuya ertesi gun neyi degistirecegini soylemiyor.
    /// </summary>
    public sealed class EveningScreen : ListScreen
    {
        protected override string Title { get { return Loc.T("ui.evening.title"); } }
        protected override string Subtitle
        {
            get { return Loc.T("ui.slot.day", App.Sim.Day); }
        }

        protected override void Fill(VisualElement list)
        {
            Simulation sim = App.Sim;
            DayReport r = sim.BuildDayReport();

            // --- nisanlar ve karne EN USTTE -----------------------------------
            //
            // Sira bilinerek boyle: bunlar gunun ODULU. Rakamlarin altina
            // konsaydi, ayni ekranda asagi kaydirmayan oyuncu onlari hic
            // gormezdi - ve gorulmeyen bir tanima tanima degildir.
            //
            // Ikisi de SEYREK: karne yedi gunde bir, nisan kampanyada en
            // fazla bes kez. Her aksam tepede duran bir kutu olsalardi
            // gurultu olurlardi; burada yoklar demek, bugun kazanilacak
            // bir sey olmadi demek.
            VisualElement badges = TodaysBadges();
            if (badges != null) list.Add(badges);

            VisualElement week = WeekReport();
            if (week != null) list.Add(week);

            // --- para --------------------------------------------------------
            VisualElement money = Theme.PanelBox();
            money.Add(Theme.Head(Loc.T("ui.evening.money")));
            money.Add(Theme.Field(Loc.T("ui.evening.revenue"),
                                  Loc.Money(r.Revenue), Theme.Good));
            money.Add(Theme.Field(Loc.T("ui.evening.ingredients"),
                                  "−" + Loc.Money(r.IngredientCost), Theme.Bad));

            // COPE GIDEN, malzemenin HEMEN ALTINDA.
            //
            // Oyunun en buyuk gorunmez gideriydi: makul oynayan bir oyuncu
            // altmis gunde aldigi malzemenin %57'sini cope atiyor - yilin
            // net karindan fazla bir para - ve bu sayi hicbir ekranda
            // yoktu. Metni ("ui.evening.spoiled") yazilmisti bile; hicbir
            // satir onu okumuyordu.
            //
            // Isaret EKSI DEGIL: bu bir odeme degil, satin alinmis ve
            // kullanilmamis stok. Net kara girmiyor - girse iki kere
            // sayilirdi. Malzemenin altinda duruyor cunku SEBEBI o.
            if (r.SpoiledValue > 0)
            {
                int sharePct = r.IngredientCost > 0
                    ? (int)(r.SpoiledValue * 100 / r.IngredientCost)
                    : 0;
                money.Add(Theme.Field(
                    Loc.T("ui.evening.spoiled"),
                    Loc.Money(r.SpoiledValue)
                        + (sharePct > 0 ? "  %" + sharePct : ""),
                    Theme.Warn));
            }
            // UCRET VE KIRA DA GORUNUYOR.
            //
            // Once "Kar" diye gosterilen sayi ciro eksi malzemeydi; ucret
            // ve kira hicbir ekranda yoktu. Oyuncu personel aliyor, cironun
            // arttigini goruyor, "kar"in da arttigini goruyor, sonra kasa
            // bosaliyor ve sebebini bulamiyordu. Oyunun temel gerilimi -
            // kadro kapasite demek AMA para demek - gorunmuyordu.
            if (r.WageCost > 0)
                money.Add(Theme.Field(Loc.T("ui.evening.wages"),
                                      "−" + Loc.Money(r.WageCost), Theme.Bad));
            if (r.RentCost > 0)
                money.Add(Theme.Field(Loc.T("ui.evening.rent"),
                                      "−" + Loc.Money(r.RentCost), Theme.Bad));
            else
                // ETIKET KENDISIYLE CELISMIYOR.
                //
                // Kira odenmediginde satir "Kira odendi | 4 gun sonra
                // 1.200" diye cikiyordu: sol taraf odendi diyor, sag
                // taraf odenmedi diyor.
                money.Add(Theme.Field(Loc.T("ui.evening.rent_next"),
                                      Loc.T("ui.evening.rent_in", sim.DaysToRent,
                                            Loc.Money(sim.WeeklyBill)), Theme.InkDim));

            money.Add(Theme.Divider());
            long gross = r.Revenue - r.IngredientCost;
            money.Add(Theme.Field(Loc.T("ui.evening.gross"), Loc.Money(gross),
                                  gross >= 0 ? Theme.Good : Theme.Bad));

            long net = r.NetProfit;
            Label netLabel = Theme.Text(Loc.T("ui.evening.profit"), Theme.FontBody,
                                        Theme.InkDim);
            VisualElement netRow = Theme.Row(0);
            netRow.style.justifyContent = Justify.SpaceBetween;
            netRow.Add(netLabel);
            Label netValue = Theme.Text(Loc.Money(net), Theme.FontTitle,
                                        net >= 0 ? Theme.Good : Theme.Bad);
            netValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            netRow.Add(netValue);
            money.Add(netRow);
            money.Add(Theme.Field(Loc.T("ui.hud.cash"), Loc.Money(sim.Cash),
                                  Theme.CashColor(sim.Cash)));

            // DEFTER ARTIK ACILABILIYOR.
            //
            // Burasi tek bir toplam gosteriyordu ve yedi gunluk
            // bekleyis tamamen edilgendi: kimin borcu oldugu, vadesi,
            // odeme sansi - hicbiri gorunmuyordu. Ustelik erken
            // tahsilat komutu simulasyonda UYGULANIYOR ama hicbir
            // ekran onu gondermiyordu.
            if (sim.OpenCredit > 0)
            {
                money.Add(Theme.Field(Loc.T("ui.evening.in_book"),
                                      Loc.Money(sim.OpenCredit), Theme.Warn));
                money.Add(Theme.Btn(Loc.T("ui.ledger.title"),
                                    () => Ui.Push(new LedgerScreen()), wide: true));
            }
            list.Add(money);

            // --- salon -------------------------------------------------------
            VisualElement hall = Theme.PanelBox();
            hall.Add(Theme.Head(Loc.T("ui.evening.hall")));
            hall.Add(Theme.Field(Loc.T("ui.hud.served"),
                                 Loc.T("ui.evening.served_n",
                                       r.ServedPeople, r.ServedParties)));
            // MASADAN KIZGIN AYRILAN - kapidan donen ayri satirda.
            // Ikisi AYNI sey degil: biri servis sorunu, oteki kapasite
            // sorunu, ve oyuncunun yapabilecegi sey de farkli. Rapor
            // ikisini tek sayiya katliyordu.
            // ETIKET DE AYRILDI, SAYI GIBI.
            //
            // Ayrim yapilmisti ama iki yer de ayni anahtari
            // ("ui.hud.angry") kullaniyordu: akSam seridi TOPLAMI,
            // rapor yalnizca OTURMUS olanlari yaziyordu. Oyuncu ayni
            // gun, ayni kelimenin altinda iki farkli sayi goruyordu
            // ve birinin bozuk oldugunu dusunuyordu.
            hall.Add(Theme.Field(Loc.T("ui.evening.left_table"),
                                 r.AngrySeatedParties.ToString(Loc.Culture),
                                 r.AngrySeatedParties > 0 ? Theme.Bad : Theme.InkDim));
            if (r.TurnedAwayParties > 0)
                hall.Add(Theme.Field(Loc.T("ui.evening.turned_away"),
                                     r.TurnedAwayParties.ToString(Loc.Culture),
                                     Theme.Bad));
            hall.Add(Theme.Field(Loc.T("ui.evening.satisfaction"),
                                 Loc.Reputation(r.AverageSatisfactionCenti),
                                 Theme.ReputationColor(r.AverageSatisfactionCenti)));
            hall.Add(Theme.Field(Loc.T("ui.hud.reputation"),
                                 Loc.Reputation(sim.ReputationCenti),
                                 Theme.ReputationColor(sim.ReputationCenti)));
            list.Add(hall);

            // --- duzenli musteriler -------------------------------------------
            VisualElement regulars = Regulars();
            if (regulars != null) list.Add(regulars);

            // --- kadro --------------------------------------------------------
            VisualElement crew = Theme.PanelBox();
            crew.Add(Theme.Head(Loc.T("ui.morning.staff")));
            crew.Add(Theme.Field(Loc.T("ui.staff.cook"), sim.Cooks.ToString(), Theme.InkDim));
            crew.Add(Theme.Field(Loc.T("ui.staff.salon"), sim.SalonStaff.ToString(), Theme.InkDim));

            int lowMorale = 0;
            for (int pool = 0; pool < 2; pool++)
            {
                int n = pool == 0 ? sim.Cooks : sim.SalonStaff;
                for (int i = 0; i < n; i++)
                    if (sim.StaffMorale(pool, i) < 30) lowMorale++;
            }
            if (lowMorale > 0)
                crew.Add(Theme.Text(Loc.T("ui.evening.low_morale", lowMorale),
                                    Theme.FontSmall, Theme.Bad));
            list.Add(crew);
        }

        /// <summary>
        /// BUGUN kazanilan nisanlar; yoksa null.
        ///
        /// Yalnizca bugun kazanilanlar - kazanilmis hepsini her aksam
        /// listelemek, tanimayi bir envantere cevirirdi. Bir nisan bir
        /// kez gorulur, sonra duraganlasir.
        /// </summary>
        private VisualElement TodaysBadges()
        {
            Simulation sim = App.Sim;
            VisualElement box = null;

            for (int i = 0; i < sim.BadgeCount; i++)
            {
                if (!sim.BadgeEarnedToday(i)) continue;

                if (box == null)
                {
                    box = Theme.PanelBox();
                    box.Add(Theme.Head(Loc.T("ui.badge.earned")));
                }

                VisualElement row = Theme.Column(2);
                Label ad = Theme.Text(Loc.T(Badges.NameKey(i)), Theme.FontBody,
                                      Theme.Accent);
                ad.style.unityFontStyleAndWeight = FontStyle.Bold;
                row.Add(ad);

                Label not = Theme.Text(Loc.T(Badges.NoteKey(i)), Theme.FontSmall,
                                       Theme.InkDim);
                not.style.whiteSpace = WhiteSpace.Normal;
                row.Add(not);
                box.Add(row);
            }

            if (box != null)
                box.Add(Theme.Text(
                    Loc.T("ui.badge.progress", sim.BadgesEarned, sim.BadgeCount),
                    Theme.FontSmall, Theme.InkDim));
            return box;
        }

        /// <summary>
        /// Haftalik karne: yedi eksen ve GECEN HAFTAYA GORE FARK.
        ///
        /// Farkin kendisi karnenin butun anlami. Yalnizca degerleri
        /// gostermek, oyuncuya "su an buradasin" der; fark "bu hafta ne
        /// yaptin" der - ve altmis gunluk oyunda hissedilen sey ikincisi.
        ///
        /// Bu ekran olmadan oyuncu yedi ekseni TAM BIR KEZ goruyordu,
        /// altmisinci gunde. Goremedigin bir seyde ilerleme
        /// hissedemezsin, ve gec ogrenilen bir olcute gore oynanamaz.
        /// </summary>
        private VisualElement WeekReport()
        {
            Simulation sim = App.Sim;
            if (!sim.WeekReportReady) return null;

            VisualElement box = Theme.PanelBox();
            box.Add(Theme.Head(Loc.T("ui.week.title", sim.WeekNumber)));
            box.Add(Theme.Text(Loc.T("ui.week.note"), Theme.FontSmall,
                               Theme.InkDim));

            // Yil sonu karnesiyle AYNI satir bicimi (Theme.AxisRow):
            // oyuncu altmisinci gunde yeni bir tablo ogrenmiyor, dokuz
            // hafta boyunca gordugu tabloyu goruyor.
            for (int i = 0; i < SeasonScore.AxisCount; i++)
            {
                string ad = i == SeasonScore.AxisCount - 1
                    ? Loc.T(App.Content.ScoreAxis.NameKey)
                    : Loc.T(SeasonScore.AxisKey(i));
                box.Add(Theme.AxisRow(ad, sim.WeekAxis(i), sim.WeekAxisDelta(i)));
            }
            return box;
        }

        /// <summary>
        /// Bugun ugrayan isimli musteriler ve acilmis hikaye sahneleri.
        ///
        /// Sahne metni burada gosteriliyor cunku aksam, oyuncunun okumak
        /// icin durdugu tek an. Servis sirasinda cikan bir metin
        /// okunmadan kapatilirdi.
        /// </summary>
        private VisualElement Regulars()
        {
            Simulation sim = App.Sim;
            if (sim.RegularCount == 0) return null;

            VisualElement box = null;
            for (int i = 0; i < sim.RegularCount; i++)
            {
                if (sim.RegularVisits(i) == 0) continue;

                RegularDef r = App.Content.Regulars[i];
                int beat = sim.RegularBeat(i);

                if (box == null)
                {
                    box = Theme.PanelBox();
                    box.Add(Theme.Head(Loc.T("ui.evening.regulars")));
                }

                VisualElement row = Theme.Column(2);
                VisualElement head = Theme.Row(Theme.Gap);
                head.style.justifyContent = Justify.SpaceBetween;
                head.Add(Theme.Text(Loc.T(r.NameKey), Theme.FontBody));
                head.Add(Theme.Text(Loc.T("ui.evening.visits", sim.RegularVisits(i)),
                                    Theme.FontSmall, Theme.InkFaint));
                row.Add(head);
                row.Add(Theme.Text(Loc.T(r.JobKey), Theme.FontSmall, Theme.InkFaint));

                if (sim.RegularAway(i))
                    row.Add(Theme.Text(Loc.T("ui.evening.away"), Theme.FontSmall, Theme.Bad));
                else if (beat > 0)
                    row.Add(Theme.Text(Loc.T(r.Story[beat - 1].TextKey),
                                       Theme.FontSmall, Theme.Ink));

                box.Add(row);
                box.Add(Theme.Divider());
            }
            return box;
        }
    }

    /// <summary>
    /// Duraklatma menusu. Oyundan cikis BURADAN, geri tusundan degil.
    /// Kaydetme otomatik (her gun basi) ama "kaydet ve cik" yine de
    /// gorunur olmali: oyuncu kaydedildigini BILMELI.
    /// </summary>
    /// <summary>
    /// Bir muudavimin hikaye sahnesi. Aksami KESIYOR.
    ///
    /// Yirmi isimli musterinin her birinin uc sahnelik yazilmis hikayesi
    /// var ve bunlar yalnizca aksam "Gun Raporu" dugmesine basilirsa
    /// goruluyordu. Yani oyuncularin cogu altmis gunu, oyunun tek sicak
    /// noktasini HIC GORMEDEN bitirecekti:
    ///
    ///   "Artik siparisini soylemiyor. Oturuyor, siz biliyorsunuz."
    ///
    /// Elde en iyi malzeme vardi ve vitrinin arkasina konmustu. Sahne
    /// acildigi gun artik "Ertesi Gun"un yerini aliyor: gunde en fazla
    /// bir tane, tam genislikte, tek dugmeyle geciliyor.
    /// </summary>
    public sealed class StoryScreen : UiScreen
    {
        public override VisualElement Build()
        {
            RegularDef r = App.Content.Regulars[App.StoryRegular];
            int beat = App.StoryBeat;

            VisualElement root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.backgroundColor = new Color(0f, 0f, 0f, 0.86f);
            root.style.justifyContent = Justify.Center;
            root.style.paddingLeft = Theme.Pad * 2;
            root.style.paddingRight = Theme.Pad * 2;

            VisualElement card = Theme.PanelBox();
            card.style.maxWidth = Theme.ReadWidth;
            card.style.alignSelf = Align.Center;
            card.style.width = Length.Percent(100);

            Label name = Theme.Text(Loc.T(r.NameKey), Theme.FontTitle, Theme.Accent);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.Add(name);

            card.Add(Theme.Text(Loc.T(r.JobKey), Theme.FontSmall, Theme.InkFaint));
            card.Add(Theme.Divider());

            // Repligin kendisi. Buyuk punto ve kirilabilir: oyunun
            // okunmasini istedigimiz TEK metni kucuk yazmak olmazdi.
            int i = beat - 1;
            string key = i >= 0 && i < r.Story.Length
                ? r.Story[i].TextKey : null;
            Label line = Theme.Text(key != null ? Loc.T(key) : "", Theme.FontBody);
            line.style.whiteSpace = WhiteSpace.Normal;
            card.Add(line);

            card.Add(Theme.Btn(Loc.T("ui.story.continue"), () =>
            {
                App.StorySeen();
                Sfx.Confirm();
                Ui.Pop();
            }, primary: true, wide: true));

            root.Add(card);
            return root;
        }

        /// <summary>Gecilmemis bir sahne kaybolmasin: geri tusu de gormus sayiyor.</summary>
        public override void OnClosed() { App.StorySeen(); }
    }

    public sealed class PauseScreen : UiScreen
    {
        /// <summary>
        /// Bu ekran acilmadan onceki duraklatma durumu.
        ///
        /// Ekranin adi "duraklatma" ama DURAKLATMIYORDU: oyuncu ayarlara
        /// girip sesi kisarken gun akmaya devam ediyordu. Mobilde oyun her
        /// an bolunuyor; bir menu acmak zaman kaybettirmemeli.
        /// </summary>
        private bool _was;

        public override VisualElement Build()
        {
            _was = App.Paused;
            App.Paused = true;

            VisualElement root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
            root.style.justifyContent = Justify.Center;
            root.style.paddingLeft = Theme.Pad * 2;
            root.style.paddingRight = Theme.Pad * 2;

            // Sutun SOLDA, ortada degil.
            //
            // Yatay tutusta bas parmaklar sol ve sag ALT koselerde duruyor;
            // ekranin ortasi ikisinin de en uzak oldugu nokta. Dikey
            // ortalama kalsin, yatay ortalama gitsin.
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 400;
            col.style.alignSelf = Align.FlexStart;
            col.style.marginLeft = Theme.Pad * 2;
            col.style.width = Length.Percent(100);

            col.Add(Theme.Btn(Loc.T("ui.pause.resume"), () => Ui.Pop(), primary: true));

            // DEGERLENDIRME YENIDEN OKUNABILIR.
            //
            // Altmis gunun karsiligi olan ekran bir zamanlar TEK
            // ATISLIKTI ve onu acan baska hicbir yer yoktu: kampanya
            // bitince bir kez cikiyor, kapaninca bir daha gorunmuyordu.
            // Bir plaket, bir daha bakilamayan bir sey olmamali.
            if (App.Sim != null && App.Sim.SeasonOver)
                col.Add(Theme.Btn(Loc.T("ui.pause.season"),
                                  () => Ui.Push(new EndScreen())));

            // NISANLAR: kazanilmis olanlar kadar KAZANILMAMIS olanlar da.
            //
            // Aksam ekrani yalnizca o gun kazanilani gosteriyor; burasi
            // hedefin durdugu yer. Kazanilmamis olani da adiyla gostermek
            // bilincli: oyuncunun kendi hedefini secebilmesi icin neyin
            // mumkun oldugunu gormesi gerekiyor - ama bu bir GOREV LISTESI
            // degil, cunku hicbiri "bugun sunu yap" demiyor ve hicbirinin
            // suresi yok.
            if (App.Sim != null)
                col.Add(Theme.Btn(Loc.T("ui.badge.title"),
                                  () => Ui.Push(new BadgeScreen())));

            col.Add(Theme.Btn(Loc.T("ui.menu.settings"),
                              () => Ui.Push(new SettingsScreen())));
            col.Add(Theme.Btn(Loc.T("ui.pause.save_quit"), () =>
            {
                App.LeaveGame();
                Sfx.Confirm();
                Ui.Replace(new MainMenuScreen());
            }));

            root.Add(col);
            return root;
        }

        /// <summary>Kapanirken duraklatma durumu geri veriliyor.</summary>
        public override void OnClosed()
        {
            App.Paused = _was;
        }
    }

    /// <summary>
    /// Kampanya sonu. docs/08: altmisinci gunde puanlanmis degerlendirme,
    /// sonra SERBEST OYUN - oyun bitmiyor, kampanya bitiyor.
    /// </summary>
    /// <summary>
    /// Yil sonu degerlendirmesi. docs/08-oyun-sonu.md.
    ///
    /// Oyun BITMIYOR, DEGERLENDIRILIYOR. Kayit silinmiyor, hicbir sey
    /// elinden alinmiyor; oyuncu devam edebiliyor ve ikinci bir yila
    /// kalip daha iyi bir puan hedefleyebiliyor.
    ///
    /// Puan tek sayi degil YEDI EKSEN, ve yedincisi mutfaga ozel: fast
    /// food'da bir gunun en kalabalik kuveri, Turk mutfaginda veresiye
    /// tahsilat orani. Mutfaklari ayiran sey oynanista oldugu kadar
    /// SONUCTA da gorunuyor.
    ///
    /// Bu ekran bir zamanlar yaziliydi ama HIC CAGRILMIYORDU: altmisinci
    /// gun gelip geciyor, hicbir sey olmuyordu. Oyunun kapanisi yoktu.
    /// </summary>
    public sealed class EndScreen : UiScreen
    {
        /// <summary>
        /// Ekran KAPANINCA "gorulmus" sayiliyor ve kayit o zaman
        /// tazeleniyor. Acilista isaretlemek, okunmadan kaybolan bir
        /// degerlendirme demekti.
        /// </summary>
        public override void OnClosed()
        {
            if (App.Sim == null) return;
            App.Sim.MarkSeasonScored();
            if (App.Slot >= 0) App.SaveToSlot(App.Slot);
        }

        public override VisualElement Build()
        {
            Simulation sim = App.Sim;
            SeasonScore score = sim.Score();

            VisualElement root = ErrorScreen.Backdrop();
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 560;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            // --- gazete manseti ------------------------------------------
            //
            // Baslik PUANA gore degisiyor: yil sonunda semtin yemek
            // elestirmeni yaziyor (docs/08). Ayni metni herkese gostermek,
            // altmis gunluk oyunu tek bir sabit cumleye indirmek olurdu.
            Label title = Theme.Text(Loc.T("ui.end.plaque" + score.Plaque),
                                     Theme.FontHuge, Theme.Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.whiteSpace = WhiteSpace.Normal;
            col.Add(title);

            Label lede = Theme.Text(Loc.T("ui.end.lede", sim.Day - 1, score.Total),
                                    Theme.FontBody, Theme.InkDim);
            lede.style.unityTextAlign = TextAnchor.MiddleCenter;
            lede.style.whiteSpace = WhiteSpace.Normal;
            col.Add(lede);

            // --- yedi eksen, IKI SUTUN -----------------------------------
            //
            // Tek sutunda yedi eksen 287 dp tutuyor; baslik ve dugmelerle
            // birlikte 393 dp'lik bir telefona sigmiyordu ve BESI
            // gorunuyordu. Kesilen yer de panelin tam kenari oldugu icin
            // "devami var" gibi degil "bitti" gibi okunuyordu.
            //
            // Bu oyunun ODUL EKRANI - altmis gunun karsiligi. Yarisi
            // gorunmeyen bir odul, odul degil.
            //
            // Ekran 873 dp genis ve yatay: iki sutun bosa duran genisligi
            // kullaniyor ve yedi eksen birden goruluyor.
            VisualElement box = Theme.PanelBox();
            VisualElement cols = Theme.Row(Theme.Pad);
            cols.style.alignItems = Align.FlexStart;

            VisualElement left = Theme.Column(0);
            VisualElement right = Theme.Column(0);
            left.style.flexGrow = 1;
            left.style.flexBasis = 0;
            right.style.flexGrow = 1;
            right.style.flexBasis = 0;

            int half = (SeasonScore.AxisCount + 1) / 2;
            for (int i = 0; i < SeasonScore.AxisCount; i++)
            {
                string name = i == SeasonScore.AxisCount - 1
                    ? Loc.T(App.Content.ScoreAxis.NameKey)
                    : Loc.T(SeasonScore.AxisKey(i));
                (i < half ? left : right).Add(Theme.AxisRow(name, score.AxisAt(i)));
            }
            cols.Add(left);
            cols.Add(right);
            box.Add(cols);
            col.Add(box);

            // --- devam ----------------------------------------------------
            col.Add(Theme.Btn(Loc.T("ui.end.continue"), () => Ui.Pop(), primary: true));
            col.Add(Theme.Btn(Loc.T("ui.end.menu"), () =>
            {
                App.LeaveGame();
                Ui.Replace(new MainMenuScreen());
            }));

            root.Add(col);
            return root;
        }

    }
}
