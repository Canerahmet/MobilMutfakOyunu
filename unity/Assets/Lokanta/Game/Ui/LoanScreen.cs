using Lokanta.Core;
using Lokanta.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// Kredi. docs/12 §4.
    ///
    /// NEDEN VAR: mekanik cekirdekte eksiksiz yaziliydi - `TakeLoan`
    /// komutu, `loanOptions`, sekiz haftalik taksit, `WeeklyBill`'in
    /// taksiti sayması, `HasLoan`/`LoanWeeksLeft`/`LoanInstallment`
    /// okuma yuzeyi - ve `unity/Assets/Lokanta/Game/` altinda `TakeLoan`
    /// gecen TEK BIR SATIR yoktu. Hicbir ekranda dugmesi olmayan bir
    /// mekanik, olmayan bir mekaniktir.
    ///
    /// Eksikligin bedeli somut: oyuncunun nakit sikisikligina karsi
    /// hicbir araci yoktu. Kasa eksiye dustugu an batma merdiveni
    /// KENDILIGINDEN isliyor - ekipman satiliyor, dukkan kuculuyor,
    /// itibar gidiyor ve yil sonu saglamlik ekseninden otuz puan
    /// eksiliyor - oyuncuya hicbir secim sunulmadan. docs/12 4'un tarif
    /// ettigi "simdi borclan, sekiz hafta ode" takasi yapida yoktu.
    ///
    /// Ekran BILINCLI OLARAK SERT: toplam geri odeme, haftalik taksit ve
    /// mevcut haftalik gider yan yana duruyor. Kredi bir kurtarma degil
    /// bir TAKAS ve oyuncu takasin iki tarafini da gormeden imzalamamali.
    /// </summary>
    public sealed class LoanScreen : ListScreen
    {
        protected override string Title { get { return Loc.T("ui.loan.title"); } }

        protected override string Subtitle
        {
            get
            {
                Simulation sim = App.Sim;
                return Loc.T("ui.loan.weekly", Loc.Money(sim.WeeklyFixedCost()));
            }
        }

        protected override void Fill(VisualElement list)
        {
            Simulation sim = App.Sim;

            // --- acik kredi ---------------------------------------------
            if (sim.HasLoan)
            {
                VisualElement open = Theme.PanelBox();
                open.Add(Theme.Text(Loc.T("ui.loan.open"), Theme.FontBody, Theme.Warn));
                open.Add(Theme.Field(Loc.T("ui.loan.installment"),
                                     Loc.Money(sim.LoanInstallment), Theme.Bad));
                open.Add(Theme.Field(Loc.T("ui.loan.weeks_left"),
                                     sim.LoanWeeksLeft.ToString(), Theme.Ink));
                // Ayni anda tek kredi tasinabiliyor; sebebini soylemek,
                // gri bir dugmeye bakip sebebini aramaktan iyi.
                open.Add(Theme.Text(Loc.T("ui.loan.one_at_a_time"),
                                    Theme.FontSmall, Theme.InkDim));
                list.Add(open);
                return;
            }

            // --- secenekler ---------------------------------------------
            long[] options = App.Economy.LoanOptions;
            for (int i = 0; i < options.Length; i++)
            {
                int option = i;
                long principal = options[i];
                long repay = Fx.Bp(principal, App.Economy.LoanMultiplierBp);
                long weekly = Fx.CeilDivL(repay, App.Economy.LoanWeeks);

                VisualElement card = Theme.PanelBox();
                card.Add(Theme.Text(Loc.Money(principal), Theme.FontTitle, Theme.Accent));

                // UC SAYI BIRDEN. Yalnizca anaparayi gostermek, krediyi
                // bedava para gibi okutur.
                card.Add(Theme.Field(Loc.T("ui.loan.repay"),
                                     Loc.Money(repay), Theme.Bad));
                card.Add(Theme.Field(Loc.T("ui.loan.installment"),
                                     Loc.Money(weekly) + " × " + App.Economy.LoanWeeks,
                                     Theme.Bad));

                card.Add(Theme.Btn(Loc.T("ui.loan.take"), () =>
                {
                    App.Send(CommandKind.TakeLoan, option);
                    Sfx.Coin();
                    Ui.Refresh();
                }, primary: true, wide: true));

                list.Add(card);
            }

            VisualElement note = Theme.PanelBox();
            note.Add(Theme.Text(Loc.T("ui.loan.warning"), Theme.FontSmall, Theme.InkDim));
            list.Add(note);
        }
    }
}
