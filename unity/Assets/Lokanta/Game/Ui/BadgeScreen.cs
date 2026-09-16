using Lokanta.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// ACCOLADES - the ones already earned and the ones still open.
    ///
    /// This is NOT A TASK LIST, and the distinction rests on the game's
    /// one-sentence promise: "You are the owner, not the chef." A task
    /// list tells the player what to do tomorrow and turns them into the
    /// employee of an invisible boss. Not one line here says "do this
    /// today", none of them has a deadline and none of them can be
    /// missed.
    ///
    /// So why are the unearned ones visible at all: to choose their OWN
    /// goal the player has to know what is possible. A hidden accolade
    /// is a surprise on the day it lands, but it does nothing for the
    /// whole game before that.
    ///
    /// The reward is NOT MONEY. This project's law: a reward paid into a
    /// saturated axis is invisible, and by the harness a good player
    /// ends day sixty with roughly 21,000 in the till. The reward for an
    /// accolade is being seen.
    /// </summary>
    public sealed class BadgeScreen : ListScreen
    {
        protected override string Title { get { return Loc.T("ui.badge.title"); } }

        protected override string Subtitle
        {
            get
            {
                return Loc.T("ui.badge.progress",
                             App.Sim.BadgesEarned, App.Sim.BadgeCount);
            }
        }

        protected override void Fill(VisualElement list)
        {
            Simulation sim = App.Sim;

            for (int i = 0; i < sim.BadgeCount; i++)
            {
                bool earned = sim.HasBadge(i);

                VisualElement card = Theme.PanelBox();

                VisualElement head = Theme.Row(Theme.Gap);
                head.style.justifyContent = Justify.SpaceBetween;
                head.style.alignItems = Align.Center;

                // AN UNEARNED ACCOLADE STILL SHOWS ITS NAME, only faded.
                //
                // Hiding the name behind "???" would stop the player
                // picking a goal - and picking a goal is this screen's
                // only job.
                Label name = Theme.Text(Loc.T(Badges.NameKey(i)), Theme.FontBody,
                                        earned ? Theme.Accent : Theme.InkFaint);
                if (earned) name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.whiteSpace = WhiteSpace.Normal;
                name.style.flexShrink = 1;
                head.Add(name);

                head.Add(Theme.Text(
                    Loc.T(earned ? "ui.badge.have" : "ui.badge.open"),
                    Theme.FontSmall, earned ? Theme.Good : Theme.InkFaint));
                card.Add(head);

                Label note = Theme.Text(Loc.T(Badges.NoteKey(i)), Theme.FontSmall,
                                        earned ? Theme.InkDim : Theme.InkFaint);
                note.style.whiteSpace = WhiteSpace.Normal;
                card.Add(note);

                list.Add(card);
            }
        }
    }
}
