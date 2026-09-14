using Lokanta.Core.Sim;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// VERESIYE DEFTERI.
    ///
    /// Mekanigin dengesi bir karar uretmeye baslamisti - tahsilat sansi
    /// artik musterinin guvenine bagli - ama oyuncu defteri GOREMIYORDU:
    /// kimin ne kadar borcu var, vadesi ne zaman, guveni ne. Aksam
    /// raporunda tek bir toplam vardi ("Defterde 2.501") ve yedi gunluk
    /// bekleyis tamamen edilgendi.
    ///
    /// Daha kotusu: `CommandKind.CollectCredit` simulasyonda UYGULANIYOR
    /// ve yorumu tam bir karar tarif ediyor - "sans yariya iniyor ve
    /// tutmazsa hesap orada kapaniyor" - ama hicbir ekran onu
    /// GONDERMIYORDU. Mekanigin ikinci karari koddaydi, oyunda degildi.
    ///
    /// SANS ACIKCA YAZILIYOR. Gizli bir olasilik uzerine karar
    /// verilemez; oyuncu "bu adam oder mi" diye tahmin etmek zorunda
    /// kalirsa secim degil kumar oynar. Gosterilen sayi simulasyonun
    /// kullandigi sayinin ta kendisi (Simulation.TabChanceBp).
    /// </summary>
    public sealed class LedgerScreen : ListScreen
    {
        protected override string Title { get { return Loc.T("ui.ledger.title"); } }

        protected override string Subtitle
        {
            get { return Loc.T("ui.ledger.total", Loc.Money(App.Sim.OpenCredit)); }
        }

        protected override void Fill(VisualElement list)
        {
            Simulation sim = App.Sim;

            if (sim.TabCount == 0)
            {
                VisualElement bos = Theme.PanelBox();
                bos.Add(Theme.Text(Loc.T("ui.ledger.empty"), Theme.FontBody, Theme.InkDim));
                list.Add(bos);
                return;
            }

            for (int i = 0; i < sim.TabCount; i++)
            {
                int tab = i;
                VisualElement card = Theme.PanelBox();

                // --- kim ---------------------------------------------------
                int reg = sim.TabRegular(tab);
                string ad = reg >= 0 && reg < App.Content.Regulars.Length
                    ? Loc.T(App.Content.Regulars[reg].NameKey)
                    : Loc.T("ui.service.guest");
                card.Add(Theme.Text(ad, Theme.FontTitle, Theme.Accent));

                // --- ne kadar, ne zaman ------------------------------------
                card.Add(Theme.Field(Loc.T("ui.ledger.amount"),
                                     Loc.Money(sim.TabAmount(tab)), Theme.Ink));

                int kalan = sim.TabDaysLeft(tab);
                card.Add(Theme.Field(Loc.T("ui.ledger.due"),
                                     kalan > 0
                                         ? Loc.T("ui.ledger.days", kalan)
                                         : Loc.T("ui.ledger.today"),
                                     kalan > 1 ? Theme.InkDim : Theme.Warn));

                // --- SANS: kararin kendisi ---------------------------------
                //
                // Iki sayi yan yana duruyor cunku karar tam olarak bu:
                // beklemek daha yuksek sans ama daha gec para; kovalamak
                // yarisi kadar sans ama bugun.
                int bekle = sim.TabCollectChanceBp(tab);
                int erken = sim.TabEarlyChanceBp(tab);
                card.Add(Theme.Field(Loc.T("ui.ledger.chance_wait"),
                                     Loc.Percent(bekle),
                                     bekle >= 8000 ? Theme.Good : Theme.Warn));
                card.Add(Theme.Field(Loc.T("ui.ledger.chance_now"),
                                     Loc.Percent(erken), Theme.Bad));

                // Cay bir DURUM, dugme degil: hesap acilirken ikram
                // edilmisse sans zaten yukarida yuksek cikiyor. Yine de
                // yaziliyor ki oyuncu sansin NEDEN yuksek oldugunu
                // gorsun - sebebi gizlemek, sayiyi sihire cevirir.
                if (sim.TabHadTea(tab))
                    card.Add(Theme.Text(Loc.T("ui.ledger.had_tea"),
                                        Theme.FontSmall, Theme.InkFaint));

                card.Add(Theme.Btn(Loc.T("ui.ledger.chase"), () =>
                {
                    App.Send(CommandKind.CollectCredit, tab);
                    Sfx.Coin();
                    Ui.Refresh();
                }, wide: true, danger: true));

                list.Add(card);
            }

            VisualElement not = Theme.PanelBox();
            not.Add(Theme.Text(Loc.T("ui.ledger.note"), Theme.FontSmall, Theme.InkDim));
            list.Add(not);
        }
    }
}
