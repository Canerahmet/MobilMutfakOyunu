using Lokanta.Core;
using Lokanta.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// The loan. docs/12 §4.
    ///
    /// WHY THIS EXISTS: the mechanic was written out in full in the core
    /// - the `TakeLoan` command, `loanOptions`, the eight-week
    /// instalment, `WeeklyBill` counting that instalment, the
    /// `HasLoan`/`LoanWeeksLeft`/`LoanInstallment` read surface - and
    /// there was NOT ONE LINE under `unity/Assets/Lokanta/Game/` that
    /// mentioned `TakeLoan`. A mechanic with no button on any screen is
    /// a mechanic that does not exist.
    ///
    /// The cost of that gap was concrete: the player had no tool at all
    /// against a cash squeeze. The moment the till goes negative the
    /// downward ladder runs BY ITSELF - equipment is sold, the shop
    /// shrinks, reputation goes and thirty points come off the year-end
    /// soundness axis - without the player being offered a single
    /// choice. The "borrow now, pay over eight weeks" trade that
    /// docs/12 §4 describes was not in the build.
    ///
    /// The screen is DELIBERATELY BLUNT: total repayment, weekly
    /// instalment and the current weekly outgoings sit side by side. A
    /// loan is not a rescue, it is a TRADE, and the player should not
    /// sign it without seeing both sides of it.
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

            // --- the loan already running -------------------------------
            if (sim.HasLoan)
            {
                VisualElement open = Theme.PanelBox();
                open.Add(Theme.Text(Loc.T("ui.loan.open"), Theme.FontBody, Theme.Warn));
                open.Add(Theme.Field(Loc.T("ui.loan.installment"),
                                     Loc.Money(sim.LoanInstallment), Theme.Bad));
                open.Add(Theme.Field(Loc.T("ui.loan.weeks_left"),
                                     sim.LoanWeeksLeft.ToString(), Theme.Ink));
                // Only one loan can be carried at a time; saying so beats
                // staring at a greyed-out button hunting for the reason.
                open.Add(Theme.Text(Loc.T("ui.loan.one_at_a_time"),
                                    Theme.FontSmall, Theme.InkDim));
                list.Add(open);
                return;
            }

            // --- the options --------------------------------------------
            long[] options = App.Economy.LoanOptions;
            for (int i = 0; i < options.Length; i++)
            {
                int option = i;
                long principal = options[i];
                long repay = Fx.Bp(principal, App.Economy.LoanMultiplierBp);
                long weekly = Fx.CeilDivL(repay, App.Economy.LoanWeeks);

                VisualElement card = Theme.PanelBox();
                card.Add(Theme.Text(Loc.Money(principal), Theme.FontTitle, Theme.Accent));

                // ALL THREE NUMBERS AT ONCE. Showing the principal on its
                // own reads the loan as free money.
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
