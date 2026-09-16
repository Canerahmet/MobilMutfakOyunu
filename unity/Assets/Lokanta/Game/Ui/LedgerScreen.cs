using Lokanta.Core.Sim;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// THE BOOK - the running tabs.
    ///
    /// The mechanic's balance had begun to produce a decision - the
    /// chance of collecting now depends on the guest's trust - but the
    /// player COULD NOT SEE THE BOOK: who owes what, when it falls due,
    /// how far they are trusted. The evening report carried a single
    /// total ("2,501 on the book") and the seven-day wait was entirely
    /// passive.
    ///
    /// Worse: `CommandKind.CollectCredit` IS IMPLEMENTED in the
    /// simulation and its comment describes a complete decision - "the
    /// chance halves, and if it does not come off the account closes
    /// right there" - but no screen SENT it. The mechanic's second
    /// decision was in the code and not in the game.
    ///
    /// THE CHANCE IS WRITTEN OUT IN PLAIN SIGHT. You cannot decide on a
    /// hidden probability; a player forced to guess "will this one pay"
    /// is gambling, not choosing. The number shown is the very number
    /// the simulation uses (Simulation.TabChanceBp).
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
                VisualElement empty = Theme.PanelBox();
                empty.Add(Theme.Text(Loc.T("ui.ledger.empty"), Theme.FontBody, Theme.InkDim));
                list.Add(empty);
                return;
            }

            for (int i = 0; i < sim.TabCount; i++)
            {
                int tab = i;
                VisualElement card = Theme.PanelBox();

                // --- who ---------------------------------------------------
                int reg = sim.TabRegular(tab);
                string name = reg >= 0 && reg < App.Content.Regulars.Length
                    ? Loc.T(App.Content.Regulars[reg].NameKey)
                    : Loc.T("ui.service.guest");
                card.Add(Theme.Text(name, Theme.FontTitle, Theme.Accent));

                // --- how much, and by when ---------------------------------
                card.Add(Theme.Field(Loc.T("ui.ledger.amount"),
                                     Loc.Money(sim.TabAmount(tab)), Theme.Ink));

                int daysLeft = sim.TabDaysLeft(tab);
                card.Add(Theme.Field(Loc.T("ui.ledger.due"),
                                     daysLeft > 0
                                         ? Loc.T("ui.ledger.days", daysLeft)
                                         : Loc.T("ui.ledger.today"),
                                     daysLeft > 1 ? Theme.InkDim : Theme.Warn));

                // --- THE CHANCE: the decision itself ------------------------
                //
                // The two numbers sit side by side because that is exactly
                // what the decision is: waiting is a better chance but
                // later money; chasing is half the chance but today.
                int waiting = sim.TabCollectChanceBp(tab);
                int chasing = sim.TabEarlyChanceBp(tab);
                card.Add(Theme.Field(Loc.T("ui.ledger.chance_wait"),
                                     Loc.Percent(waiting),
                                     waiting >= 8000 ? Theme.Good : Theme.Warn));
                card.Add(Theme.Field(Loc.T("ui.ledger.chance_now"),
                                     Loc.Percent(chasing), Theme.Bad));

                // Tea is a STATE, not a button: if it was offered while the
                // tab was being opened, the chance above already comes out
                // higher. It is still written down so the player can see WHY
                // the chance is high - hiding the reason turns the number
                // into magic.
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

            VisualElement note = Theme.PanelBox();
            note.Add(Theme.Text(Loc.T("ui.ledger.note"), Theme.FontSmall, Theme.InkDim));
            list.Add(note);
        }
    }
}
