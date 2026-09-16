using Lokanta.Core.Content;
using Lokanta.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// The day's report. docs/02, the evening phase.
    ///
    /// The screen's job is not to list figures but to tell WHAT THE DAY
    /// WAS: where the money went, who came, who was let down. Showing
    /// takings alone does not tell the player what to change tomorrow.
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

            // --- accolades and the report card AT THE VERY TOP -----------------
            //
            // The order is deliberate: these are the day's REWARD. Put
            // beneath the figures, a player who does not scroll down that
            // screen would never see them - and recognition that is not seen
            // is not recognition.
            //
            // Both are RARE: a report card every seven days, an accolade at
            // most five times in a campaign. Had they been a box sitting at
            // the top every evening they would be noise; their absence here
            // means nothing was earned today.
            VisualElement badges = TodaysBadges();
            if (badges != null) list.Add(badges);

            VisualElement week = WeekReport();
            if (week != null) list.Add(week);

            // --- money -------------------------------------------------------
            VisualElement money = Theme.PanelBox();
            money.Add(Theme.Head(Loc.T("ui.evening.money")));
            money.Add(Theme.Field(Loc.T("ui.evening.revenue"),
                                  Loc.Money(r.Revenue), Theme.Good));
            money.Add(Theme.Field(Loc.T("ui.evening.ingredients"),
                                  "−" + Loc.Money(r.IngredientCost), Theme.Bad));

            // WHAT GOES IN THE BIN, DIRECTLY BENEATH the ingredients.
            //
            // It was the game's biggest invisible cost: a player playing
            // reasonably throws away 57% of the ingredients they buy over
            // sixty days - more money than the year's net profit - and that
            // number was on no screen at all. The text ("ui.evening.spoiled")
            // had already been written; not one line read it.
            //
            // The sign is NOT NEGATIVE: this is not a payment, it is stock
            // bought and not used. It does not enter the net profit - if it
            // did it would be counted twice. It sits below the ingredients
            // because that is its CAUSE.
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
            // WAGES AND RENT ARE VISIBLE TOO.
            //
            // The number once shown as "Profit" was takings minus
            // ingredients; wages and rent were on no screen. The player
            // would hire someone, see the takings go up, see the "profit" go
            // up, then watch the till empty with no way of finding out why.
            // The game's basic tension - crew means capacity BUT it means
            // money - was invisible.
            if (r.WageCost > 0)
                money.Add(Theme.Field(Loc.T("ui.evening.wages"),
                                      "−" + Loc.Money(r.WageCost), Theme.Bad));
            if (r.RentCost > 0)
                money.Add(Theme.Field(Loc.T("ui.evening.rent"),
                                      "−" + Loc.Money(r.RentCost), Theme.Bad));
            else
                // THE LABEL DOES NOT CONTRADICT ITSELF.
                //
                // When the rent had not been paid the row came out as "Rent
                // paid | 1,200 in 4 days": the left-hand side says paid, the
                // right-hand side says not paid.
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

            // THE BOOK CAN NOW BE OPENED.
            //
            // This showed a single total and the seven-day wait was
            // entirely passive: who owed the money, when it fell due, the
            // chance of being paid - none of it was visible. And the
            // early-collection command IS IMPLEMENTED in the simulation
            // while no screen sent it.
            if (sim.OpenCredit > 0)
            {
                money.Add(Theme.Field(Loc.T("ui.evening.in_book"),
                                      Loc.Money(sim.OpenCredit), Theme.Warn));
                money.Add(Theme.Btn(Loc.T("ui.ledger.title"),
                                    () => Ui.Push(new LedgerScreen()), wide: true));
            }
            list.Add(money);

            // --- the hall ----------------------------------------------------
            VisualElement hall = Theme.PanelBox();
            hall.Add(Theme.Head(Loc.T("ui.evening.hall")));
            hall.Add(Theme.Field(Loc.T("ui.hud.served"),
                                 Loc.T("ui.evening.served_n",
                                       r.ServedPeople, r.ServedParties)));
            // THOSE WHO LEFT A TABLE ANGRY - those turned away at the door
            // are on a separate row. The two are NOT THE SAME thing: one is
            // a service problem, the other a capacity problem, and what the
            // player can do about them differs. The report folded both into
            // a single number.
            // THE LABEL WAS SPLIT TOO, LIKE THE NUMBER.
            //
            // The split had been made, but both places used the same key
            // ("ui.hud.angry"): the evening strip showed the TOTAL and the
            // report only those who had been SEATED. On the same day, under
            // the same word, the player saw two different numbers and
            // assumed one of them was broken.
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

            // --- the regulars -------------------------------------------------
            VisualElement regulars = Regulars();
            if (regulars != null) list.Add(regulars);

            // --- the crew -----------------------------------------------------
            VisualElement crew = Theme.PanelBox();
            crew.Add(Theme.Head(Loc.T("ui.morning.staff")));
            crew.Add(Theme.Field(Loc.T("ui.staff.cook"), sim.Cooks.ToString(), Theme.InkDim));
            crew.Add(Theme.Field(Loc.T("ui.staff.hall"), sim.HallStaff.ToString(), Theme.InkDim));

            int lowMorale = 0;
            for (int pool = 0; pool < 2; pool++)
            {
                int n = pool == 0 ? sim.Cooks : sim.HallStaff;
                for (int i = 0; i < n; i++)
                    if (sim.StaffMorale(pool, i) < 30) lowMorale++;
            }
            if (lowMorale > 0)
                crew.Add(Theme.Text(Loc.T("ui.evening.low_morale", lowMorale),
                                    Theme.FontSmall, Theme.Bad));
            list.Add(crew);
        }

        /// <summary>
        /// The accolades earned TODAY; null if there are none.
        ///
        /// Only the ones earned today - listing every accolade already
        /// earned, every evening, would turn recognition into an inventory.
        /// An accolade is seen once, and after that it goes still.
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
                Label name = Theme.Text(Loc.T(Badges.NameKey(i)), Theme.FontBody,
                                        Theme.Accent);
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                row.Add(name);

                Label note = Theme.Text(Loc.T(Badges.NoteKey(i)), Theme.FontSmall,
                                        Theme.InkDim);
                note.style.whiteSpace = WhiteSpace.Normal;
                row.Add(note);
                box.Add(row);
            }

            if (box != null)
                box.Add(Theme.Text(
                    Loc.T("ui.badge.progress", sim.BadgesEarned, sim.BadgeCount),
                    Theme.FontSmall, Theme.InkDim));
            return box;
        }

        /// <summary>
        /// The weekly report card: seven axes and THE DIFFERENCE AGAINST
        /// LAST WEEK.
        ///
        /// The difference is the whole point of the report card. Showing
        /// the values alone tells the player "this is where you are now";
        /// the difference tells them "this is what you did this week" - and
        /// in a sixty-day game the second is the one that is felt.
        ///
        /// Without this screen the player saw the seven axes EXACTLY ONCE,
        /// on day sixty. You cannot feel progress in something you cannot
        /// see, and you cannot play towards a measure you learn too late.
        /// </summary>
        private VisualElement WeekReport()
        {
            Simulation sim = App.Sim;
            if (!sim.WeekReportReady) return null;

            VisualElement box = Theme.PanelBox();
            box.Add(Theme.Head(Loc.T("ui.week.title", sim.WeekNumber)));
            box.Add(Theme.Text(Loc.T("ui.week.note"), Theme.FontSmall,
                               Theme.InkDim));

            // THE SAME row format as the year-end report card
            // (Theme.AxisRow): on day sixty the player is not learning a new
            // table, they are seeing the table they have been looking at for
            // nine weeks.
            for (int i = 0; i < SeasonScore.AxisCount; i++)
            {
                string name = i == SeasonScore.AxisCount - 1
                    ? Loc.T(App.Content.ScoreAxis.NameKey)
                    : Loc.T(SeasonScore.AxisKey(i));
                box.Add(Theme.AxisRow(name, sim.WeekAxis(i), sim.WeekAxisDelta(i)));
            }
            return box;
        }

        /// <summary>
        /// The named guests who came in today, and any story beats they
        /// have opened.
        ///
        /// The beat's text is shown here because the evening is the only
        /// moment the player stops to read. A piece of text appearing during
        /// service would be dismissed unread.
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
    /// The pause menu. Leaving the game happens HERE, not through the back
    /// key. Saving is automatic (at the start of each day) but "save and
    /// quit" still has to be visible: the player must KNOW they have been
    /// saved.
    /// </summary>
    /// <summary>
    /// A regular's story beat. It INTERRUPTS the evening.
    ///
    /// Each of the twenty named guests has a three-beat story written for
    /// them, and these were only seen if the player pressed the "Day
    /// Report" button in the evening. So most players would have finished
    /// sixty days WITHOUT EVER SEEING the one warm spot in the game:
    ///
    ///   "He does not order any more. He sits down, and you know."
    ///
    /// The best material we had was behind the display case. On the day a
    /// beat opens it now takes the place of "Next Day": at most one a day,
    /// full width, dismissed with a single button.
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

            // The line itself. Large type and able to wrap: setting the ONE
            // piece of text in the game we want read in small type would not
            // do.
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

        /// <summary>A beat that was not dismissed must not be lost: the back key counts as having seen it too.</summary>
        public override void OnClosed() { App.StorySeen(); }
    }

    public sealed class PauseScreen : UiScreen
    {
        /// <summary>
        /// The pause state from before this screen opened.
        ///
        /// The screen is called "pause" but it WAS NOT PAUSING: while the
        /// player went into the settings to turn the sound down, the day
        /// went on running. On mobile the game is interrupted at any moment;
        /// opening a menu should not cost time.
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

            // The column is ON THE LEFT, not in the middle.
            //
            // Held in landscape, the thumbs rest in the bottom left and
            // bottom right corners; the middle of the screen is the point
            // furthest from both. Keep the vertical centring, drop the
            // horizontal.
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 400;
            col.style.alignSelf = Align.FlexStart;
            col.style.marginLeft = Theme.Pad * 2;
            col.style.width = Length.Percent(100);

            col.Add(Theme.Btn(Loc.T("ui.pause.resume"), () => Ui.Pop(), primary: true));

            // THE EVALUATION CAN BE READ AGAIN.
            //
            // The screen that is the payoff for sixty days was once a ONE
            // SHOT and nothing else opened it: it appeared once when the
            // campaign ended and, dismissed, was never seen again. A plaque
            // should not be a thing you cannot look at twice.
            if (App.Sim != null && App.Sim.SeasonOver)
                col.Add(Theme.Btn(Loc.T("ui.pause.season"),
                                  () => Ui.Push(new EndScreen())));

            // ACCOLADES: the unearned ones as much as the earned ones.
            //
            // The evening screen shows only what was earned that day; this
            // is where the goal lives. Showing the unearned ones by name is
            // deliberate: to choose their own goal the player has to see
            // what is possible - but this is not a TASK LIST, because none
            // of them says "do this today" and none of them has a deadline.
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

        /// <summary>The pause state is handed back on the way out.</summary>
        public override void OnClosed()
        {
            App.Paused = _was;
        }
    }

    /// <summary>
    /// The end of the campaign. docs/08: a scored evaluation on day sixty,
    /// then FREE PLAY - the game does not end, the campaign does.
    /// </summary>
    /// <summary>
    /// The year-end evaluation. docs/08-endgame.md.
    ///
    /// The game DOES NOT END, IT IS EVALUATED. The save is not deleted and
    /// nothing is taken away; the player can carry on and stay for a second
    /// year aiming at a better score.
    ///
    /// The score is not one number but SEVEN AXES, and the seventh is
    /// specific to the cuisine: in fast food it is the busiest cover count
    /// of any one day, in Turkish cuisine the rate at which tabs are
    /// collected. What separates the cuisines shows up in the OUTCOME as
    /// much as in the play.
    ///
    /// This screen was written once but WAS NEVER CALLED: day sixty came
    /// and went and nothing happened. The game had no closing.
    /// </summary>
    public sealed class EndScreen : UiScreen
    {
        /// <summary>
        /// It counts as "seen" when the screen CLOSES, and the save is
        /// refreshed at that point. Marking it on opening meant an
        /// evaluation that could vanish unread.
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

            // --- the newspaper headline ----------------------------------
            //
            // The headline changes with the SCORE: at the end of the year
            // the neighbourhood's food critic writes it up (docs/08).
            // Showing everyone the same text would reduce a sixty-day game
            // to one fixed sentence.
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

            // --- seven axes, TWO COLUMNS ---------------------------------
            //
            // In one column the seven axes come to 287 dp; together with the
            // heading and the buttons that did not fit a 393 dp phone and
            // FIVE of them were visible. And because the cut fell exactly at
            // the panel's edge it read as "that is all" rather than "there
            // is more".
            //
            // This is the game's REWARD SCREEN - the payoff for sixty days.
            // A reward you can only half see is not a reward.
            //
            // The screen is 873 dp wide and landscape: two columns use the
            // width that was going spare and all seven axes are seen at
            // once.
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

            // --- carry on -------------------------------------------------
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
