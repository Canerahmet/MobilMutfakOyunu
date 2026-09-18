using Lokanta.Core;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// A full-screen panel with a scrollable list inside it.
    ///
    /// A shared base, because all four morning screens are the same shape:
    /// heading, list, close at the bottom. Writing it out in each of them
    /// means the four drift apart from one another over time.
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
            // OPAQUE. This was Bg at alpha 0.97 - the only colour in this
            // file with no comment on it, which is the signature of a value
            // typed while testing and never taken out.
            //
            // Three per cent of a lit 3D scene is not nothing when the panel
            // is near-black: the scroll body measured (26,26,29) against a
            // head band of (22,24,28), so the ghost of the kitchen was
            // brighter than the panel it showed through, and the list rows -
            // already only 1.19:1 against their background - lost most of
            // what separated them. It read as an unfinished screen.
            //
            // ErrorScreen.Backdrop (MenuScreens.cs) has always used opaque
            // Bg. Two backdrops in one codebase disagreeing is the proof that
            // 0.97 was never a decision.
            outer.style.backgroundColor = Theme.Bg;
            outer.style.alignItems = Align.Center;

            // The readable width limit. On a phone 1280 wide, in a row that
            // runs right across the screen, the eye cannot travel from the
            // label at the far left to the value at the far right.
            VisualElement root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.width = Length.Percent(100);
            root.style.maxWidth = Theme.ReadWidth;

            // --- the heading --------------------------------------------------
            // The heading and its second line ARE NOT SQUASHED. The
            // scrollable list takes all the free space and, with the default
            // flex-shrink of 1, it was pushing the heading off the screen: in
            // the first desktop build the "Market" heading did not appear at
            // all and its second line was cut in half.
            // The strips HAVE A BACKGROUND. Left transparent, the scrolling
            // list showed through them and half-finished cards could be read
            // around the "OK" button.
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

            // --- the list -----------------------------------------------------
            ScrollView scroll = Theme.Mobile(new ScrollView(ScrollViewMode.Vertical));
            scroll.style.flexGrow = 1;

            // minHeight has to be ZERO.
            //
            // In a flex layout an element's minimum height defaults to ITS
            // CONTENT; that is, a scroll area carrying a long list REFUSES to
            // get smaller. The result: the area spilled below the bottom
            // strip and the cards carried on behind the bottom button.
            scroll.style.minHeight = 0;

            scroll.style.paddingLeft = Theme.Pad;
            scroll.style.paddingRight = Theme.Pad;

            VisualElement list = Theme.Column(Theme.Gap);
            list.style.flexShrink = 0;

            // BOTTOM PADDING: so the last card does not end up behind the
            // fixed bottom strip. At the end of the scroll the last card's
            // button sat under the bar and could not be pressed.
            list.style.paddingBottom = Theme.Touch + Theme.Pad;

            Fill(list);
            scroll.Add(list);
            root.Add(scroll);

            // --- the foot -----------------------------------------------------
            VisualElement foot = new VisualElement();
            foot.style.flexShrink = 0;
            foot.style.backgroundColor = Theme.Bg;
            foot.style.paddingLeft = Theme.Pad;
            foot.style.paddingRight = Theme.Pad;
            foot.style.paddingTop = Theme.Gap;
            foot.style.paddingBottom = Theme.Pad;
            // "OK" IS NOT THE PRIMARY. Closing a screen is not the
            // recommended action - the player came here to do something.
            // Made primary, the loudest button on the shopping screen was
            // the one that CLOSES it.
            foot.Add(Theme.Btn(Loc.T("ui.common.ok"), () => Ui.Pop(), wide: true));
            root.Add(foot);

            outer.Add(root);
            return outer;
        }
    }

    /// <summary>
    /// The market. docs/12 §3: "catching a cheap day is not luck, it is a
    /// matter of KEEPING TRACK."
    ///
    /// So the screen's job is to COMPARE THE PRICE: where today's price
    /// sits against the year's average, and whether the ingredient keeps.
    /// A cheap day is an opportunity if you can store it; if you cannot, it
    /// is only a wobble in your costs.
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

            // INGREDIENT QUALITY - THE MECHANIC WAS IN THE CODE AND NOT IN
            // THE GAME.
            //
            // `CommandKind.SetQuality` was written out in full in the core,
            // the balance bot measured it with a cheap-ingredients strategy
            // and it had tests - and there was no button for it ON ANY
            // SCREEN. So a three-step decision axis that costs reputation
            // was closed to the player.
            // Tab collection had been found in exactly the same way
            // (docs/45).
            //
            // It lives HERE because the choice changes THIS SCREEN itself:
            // `IngredientPriceToday` already computes with `_quality`, so
            // pressing a step moves every price below it immediately. There
            // is no need to explain the effect on another screen - it is
            // seen.
            list.Add(QualityPicker(sim));

            list.Add(Theme.Btn(Loc.T("ui.morning.restock"), () =>
            {
                App.RestockRecommended();
                Ui.Refresh();
            }, primary: true, wide: true));

            for (int i = 0; i < sim.IngredientCount; i++)
            {
                // Only the ingredients used IN THIS CUISINE that mean
                // something today. Seventy-seven rows cannot be read in a
                // forty-tap day (docs/16).
                int need = sim.RecommendedRestock(i);
                int stock = sim.StockOf(i);
                if (need <= 0 && stock <= 0) continue;

                list.Add(Row(i, need, stock));
            }
        }

        /// <summary>
        /// The text key for a quality step.
        ///
        /// The key is WRITTEN OUT IN FULL (not "ui.quality." + q): the text
        /// generator scans the screens and rejects any text that is not
        /// used, and it cannot see a computed key. The same rule applies to
        /// the accolades (Badges.NameKey). A side benefit: someone grepping
        /// finds it too.
        /// </summary>
        private static string QualityKey(int q)
        {
            switch (q)
            {
                case 0: return "ui.quality.0";
                case 1: return "ui.quality.1";
                default: return "ui.quality.2";
            }
        }

        /// <summary>
        /// Three-step ingredient quality.
        ///
        /// The effect works BOTH WAYS and both are shown:
        ///   price        - seen immediately on every card below
        ///   satisfaction - invisible, so it is written out HERE
        ///
        /// The satisfaction number is NOT MADE UP: the per-ingredient effect
        /// splits into three separate moulds (-5.0 on cheap, -20.0 on
        /// expensive) and a single average would mislead. Instead the real
        /// effect on the dishes ON THE PLAYER'S MENU is computed - with the
        /// simulation's own `DishQualityCentiOf` measure, so the number the
        /// screen writes is the very number the service uses.
        /// </summary>
        private VisualElement QualityPicker(Simulation sim)
        {
            // THE CARD IS KEPT NARROW.
            //
            // The first version used a large heading via `Theme.Head` and it
            // was measured in a store screenshot: the card ate more than a
            // third of the screen and pushed THE INGREDIENT LIST below the
            // fold - so the market screen's actual content was not visible at
            // all when it opened. Quality is not a heading, it is a setting;
            // its size should say so.
            // NO PANEL, THE LABEL SITS INSIDE THE ROW.
            //
            // Measured twice: first a panel with a large heading ate more
            // than a third of the screen, then even in its reduced form THE
            // INGREDIENT LIST stayed below the fold. The first thing someone
            // opening the market screen sees should be THE MARKET - not two
            // controls.
            //
            // Quality is not something that changes often; the room it takes
            // should match.
            VisualElement card = Theme.Column(2);

            VisualElement row = Theme.Row(Theme.Gap);
            row.style.alignItems = Align.Center;
            Label label = Theme.Text(Loc.T("ui.morning.quality"), Theme.FontSmall,
                                     Theme.InkDim);
            label.style.minWidth = 96;
            label.style.flexShrink = 0;
            row.Add(label);
            for (int q = 0; q < Simulation.QualityCount; q++)
            {
                int level = q;
                Button b = Theme.Btn(Loc.T(QualityKey(q)), () =>
                {
                    App.Send(CommandKind.SetQuality, level);
                    Sfx.Click();
                    Ui.Refresh();
                }, primary: q == sim.Quality);
                b.style.flexGrow = 1;
                b.style.flexBasis = 0;
                row.Add(b);
            }
            card.Add(row);

            // The effect on the menu, as an average in centi-points.
            int total = 0, count = 0;
            for (int d = 0; d < sim.DishCount; d++)
            {
                if (!sim.IsOnMenu(d) || !sim.IsUnlocked(d)) continue;
                total += sim.DishQualityCentiOf(d);
                count++;
            }
            int mean = count > 0 ? total / count : 0;

            // THE TEXT SEPARATES THREE STATES.
            //
            // The first version confused two of them: because the effect is
            // zero on the standard step it said "Menu empty - no effect" and
            // the menu WAS NOT empty. The screen was telling the player
            // something that was not so. An empty menu (count == 0) and a
            // zero effect (the standard step) are different things.
            if (count == 0)
                card.Add(Theme.Text(Loc.T("ui.morning.quality_none"),
                                    Theme.FontSmall, Theme.InkFaint));
            else if (mean != 0)
                card.Add(Theme.Text(
                    Loc.T(mean > 0 ? "ui.morning.quality_up"
                                   : "ui.morning.quality_down",
                          Loc.Reputation(mean < 0 ? -mean : mean)),
                    Theme.FontSmall, mean > 0 ? Theme.Good : Theme.Bad));
            // On the standard step there is no row at all: that is the
            // benchmark, so there is no difference to report.
            return card;
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

            // The price as a DIFFERENCE against the year's average: two
            // absolute numbers told the same comparison twice and made the
            // card longer.
            VisualElement facts = Theme.Row(Theme.Pad);
            facts.style.justifyContent = Justify.SpaceBetween;
            facts.Add(Theme.Text(Loc.Money(today) + " / kg", Theme.FontBody, Theme.Ink));
            // "Year average +9%" WAS NOT A SENTENCE: it read as though the
            // average itself were 9%. Now it says what it is.
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

            // Shelf life has three states: never spoils (salt, flour, oil),
            // a number of days with cold storage, and gone by morning.
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

            // --- THE QUANTITY DECISION -----------------------------------
            //
            // There used to be a single button and it bought exactly the
            // "recommended" amount. So when the "cheap today" tag appeared
            // the player could do NOTHING with it: they could not buy extra
            // and they could not store it. For the same reason the cold
            // store upgrade had no payoff either - there is no extra stock
            // to keep longer.
            //
            // Now it is bought in days, and the ceiling is set by the cold
            // storage tier: three days for what does not spoil, as many as
            // it keeps for what does.
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

                    // UNDER ONE COIN IS NOT A BUTTON.
                    //
                    // With the stock nearly full the remaining need drops to
                    // a few grams: for salt the three-day shortfall is 2
                    // grams, that is 2 centi. What was left on screen was a
                    // button reading "3 days - 0 [coin]" - it looks free and
                    // pressing it changes nothing. The threshold has to be
                    // "ONE COIN" rather than "zero", because the interface
                    // shows coins and writes 99 centi as 0 as well.
                    //
                    // These items are not lost: "Buy the recommended stock"
                    // takes all of them at once.
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
    /// The menu board.
    ///
    /// There are two decisions and both have to be visible: which dish is
    /// ON THE MENU, and WHY the locked ones are locked. Showing a lock with
    /// no reason is telling the player "something is missing" and not
    /// saying what.
    /// </summary>
    public sealed class MenuBoardScreen : ListScreen
    {
        protected override string Title { get { return Loc.T("ui.morning.menu"); } }
        protected override string Subtitle
        {
            get { return Loc.T("ui.menu.subtitle"); }
        }

        /// <summary>The dish currently opened out. -1: none of them.</summary>
        private int _open = -1;

        /// <summary>Is the locked list expanded.</summary>
        private bool _showLocked;

        /// <summary>
        /// The list is ORDERED and COMPRESSED.
        ///
        /// Measured (a tour screenshot taken in a real 873x393 phone
        /// window): ONE dish card was visible on screen, in a list of
        /// thirty-two dishes. And the list was in raw content order; opening
        /// the screen on day one the player saw one available dish and,
        /// directly beneath it, SIX LOCKED ROWS IN A ROW - a screen that
        /// says "90% of this game is locked".
        ///
        /// Three changes, no new mechanic:
        ///   1. ORDER: what is on the menu, then what is available, then the
        ///      TWO that open soonest, then the locked ones folded away.
        ///   2. COMPRESSION: only the dish being TOUCHED opens as a card;
        ///      the rest are single rows. That puts seven or eight dishes on
        ///      the screen instead of one.
        ///   3. HEADINGS: each section says what it is.
        /// </summary>
        protected override void Fill(VisualElement list)
        {
            Simulation sim = App.Sim;

            var onMenu = new System.Collections.Generic.List<int>();
            var available = new System.Collections.Generic.List<int>();
            var locked = new System.Collections.Generic.List<int>();

            for (int i = 0; i < sim.DishCount; i++)
            {
                if (!sim.IsUnlocked(i)) locked.Add(i);
                else if (sim.IsOnMenu(i)) onMenu.Add(i);
                else available.Add(i);
            }

            // The locked ones go by THE DAY THEY OPEN: let "soon" show what
            // is really soon.
            locked.Sort((a, b) => App.Content.Dishes[a].UnlockDay
                                    .CompareTo(App.Content.Dishes[b].UnlockDay));

            if (onMenu.Count > 0)
            {
                list.Add(Heading(Loc.T("ui.menu.group_on") + " (" + onMenu.Count + ")"));
                foreach (int i in onMenu) list.Add(Row(i));
            }
            if (available.Count > 0)
            {
                list.Add(Heading(Loc.T("ui.menu.group_open") + " (" + available.Count + ")"));
                foreach (int i in available) list.Add(Row(i));
            }

            int soon = locked.Count < 2 ? locked.Count : 2;
            if (soon > 0)
            {
                list.Add(Heading(Loc.T("ui.menu.group_soon")));
                for (int k = 0; k < soon; k++) list.Add(Row(locked[k]));
            }

            int rest = locked.Count - soon;
            if (rest > 0)
            {
                Button more = Theme.Btn(
                    Loc.T("ui.menu.group_locked") + " (" + rest + ")"
                    // A word, NOT an arrow glyph: the font coverage check
                    // caught U+25BE - the shipped font does not have that
                    // glyph and it would be drawn as a box.
                    // AND IT GOES THROUGH Loc LIKE EVERY OTHER WORD.
                    //
                    // This was a bare literal appended to a translated
                    // label: Turkish players read an English word, and
                    // after the five-language work so did Spanish,
                    // Chinese and Arabic ones. A string on screen that
                    // never touches Loc cannot be found by any of the
                    // text checks either.
                    + "  " + Loc.T(_showLocked ? "ui.common.hide"
                                               : "ui.common.show"),
                    () => { _showLocked = !_showLocked; Ui.Refresh(); });
                more.style.marginTop = Theme.Gap;
                list.Add(more);

                if (_showLocked)
                    for (int k = soon; k < locked.Count; k++) list.Add(Row(locked[k]));
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
        /// A collapsed dish row: name, price, state. Touching it opens it
        /// out.
        ///
        /// The card is 150 dp; this row is 40 dp. Seven or eight dishes fit
        /// on the same screen instead of one and the player sees their menu
        /// AT A GLANCE - which is the menu screen's entire job.
        /// </summary>
        private VisualElement CompactRow(int i)
        {
            Simulation sim = App.Sim;
            DishDef d = App.Content.Dishes[i];

            // THE ROW ITSELF IS THE BUTTON.
            //
            // Putting a separate "open" button on it made the row 64 dp (a
            // button is 52 tall for the touch target) and only three rows fit
            // the 219 dp visible area. With the whole row touchable the row
            // comes down to 44 dp AND the touch target GROWS - a strip 873 dp
            // wide.
            Button row = new Button(() => { _open = i; Ui.Refresh(); });
            row.text = string.Empty;
            row.style.flexDirection = Theme.RowFlow;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = Theme.Pad;
            row.style.paddingRight = Theme.Pad;
            row.style.paddingTop = 5;
            row.style.paddingBottom = 5;
            // 44 dp WAS UNDER THE FLOOR, on the most-tapped row in the game.
            //
            // The row is built with `new Button(...)` rather than Theme.Btn,
            // so it never received `minHeight = Theme.Touch` and came out at
            // 5 + 17 + 5 = 44 dp. The comment above justified that with "the
            // touch target GROWS - a strip 873 dp wide", and that is the wrong
            // shape of argument: the 48 dp floor is a floor on BOTH axes, and
            // width does not buy height. With marginBottom at 2 the centres of
            // two rows were 46 dp apart, so a finger aiming at one dish opened
            // its neighbour - the very failure Theme.Level's comment records
            // being caught for the volume steps, and missed here.
            //
            // The menu has 32 dishes. Three rows per screen becomes four at
            // 52 dp, which is the honest cost.
            row.style.minHeight = Theme.Touch;
            row.style.marginBottom = Theme.Gap - 6;
            row.style.marginTop = 0;
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

            // Anything not opened out is a single row; the opened one is a
            // full card.
            if (unlocked && i != _open) return CompactRow(i);

            // A LOCKED DISH IS A SINGLE ROW, NOT A CARD.
            //
            // Measured: an available dish's card is 232 dp on the target
            // phone and the list's visible area is 219 dp - so not even ONE
            // dish fitted, in a list of thirty-two. The locked dishes took a
            // full card too, and what they carried was two lines: the name,
            // and when it opens.
            if (!unlocked) return LockedRow(i, d);

            VisualElement card = Theme.PanelBox();

            VisualElement head = Theme.Row(Theme.Gap);
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;

            // The name and the station ON ONE LINE. Stacked, the card went
            // over 250 dp and on a 393 dp phone ONE card filled the screen -
            // it stopped being a list.
            VisualElement left = Theme.Row(Theme.Gap);
            left.style.alignItems = Align.Center;
            left.Add(Theme.Text(Loc.T(d.NameKey), Theme.FontBody));
            left.Add(Theme.Text(Loc.T("station." + App.Content.Stations[d.StationIndex].Id),
                                Theme.FontSmall, Theme.InkFaint));
            head.Add(left);

            // A FAVOURITE DISH: THE NAME, NOT A DOT.
            //
            // It used to be an orange dot and a tooltip. A TOOLTIP NEVER
            // APPEARS ON A TOUCHSCREEN - so the one menu signal where the
            // story meets the mechanic was in effect invisible. The text
            // ("{0} loves this") was ALREADY WRITTEN in the table and no
            // code read it.
            //
            // The dot stays: it is the mark that can be scanned at a glance.
            // Beside it, it now says whose favourite it is.
            int fan = sim.FavouriteRegularOf(i);
            if (fan >= 0)
            {
                head.Add(Theme.Dot(Theme.Accent, 10f));
                head.Add(Theme.Text(
                    Loc.T("ui.menu.favourite_of", Loc.T(App.Content.Regulars[fan].NameKey)),
                    Theme.FontSmall, Theme.Accent));
            }

            // THE ON-THE-MENU TOGGLE IS IN THE HEADING ROW AND IS NOT THE
            // ACCENT COLOUR.
            //
            // It used to be a FULL-WIDTH orange bar at the bottom of the
            // card. Two problems at once: it added 62 dp to the card, and it
            // put the accent colour to a ninth job - the same orange cannot
            // mean both "the one recommended action" and "this one is on".
            // A toggle that is on is now marked WITH GREEN TEXT; the colour
            // reports a STATE, not a call to act.
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

            // --- THE PRICE DECISION --------------------------------------
            //
            // This row was the game's most owner-like decision and it was NOT
            // IN the interface: the SetPrice command is written and applied
            // in the core, the price-sensitivity formula is in docs/12, the
            // per-archetype coefficients are in the content, the balance tool
            // even has a strategy that does nothing but raise prices - but the
            // player
            // could not touch this axis. Not being able to set a price in a
            // tycoon game is a racing game with no steering wheel.
            //
            // The step is 5% OF THE MARKET: a fixed coin step would be
            // enormous on a cheap drink and invisible on an expensive main.
            long step = market / 20;
            if (step < 50) step = 50;                 // at least half a coin

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

            // Where we stand against the market - the decision's FEEDBACK.
            // The number on its own is not information; "12% above the
            // market" is. Three numbers ON ONE ROW: against the market, the
            // cost, the margin. Three separate rows ate half the card's
            // height.
            VisualElement facts = Theme.Row(Theme.Pad);
            facts.style.justifyContent = Justify.SpaceBetween;
            facts.Add(Theme.Text(
                Loc.T("ui.menu.vs_market") + " "
                // The second argument is Loc.Percent's "put a + in front"
                // flag. Passed positionally on purpose: Loc.cs is owned
                // elsewhere and a named argument would break here the moment
                // that parameter is renamed.
                + Loc.Percent(vsMarketBp, true),
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
        /// A locked dish: one row, no box. The name, the station and THE
        /// REASON FOR THE LOCK, side by side.
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

            string reason;
            if (d.UnlockDay > sim.Day)
                reason = Loc.T("ui.menu.unlock_day", d.UnlockDay);
            else if (sim.ReputationCenti < d.UnlockReputationCenti)
                reason = Loc.T("ui.menu.needs_reputation",
                               Loc.Reputation(d.UnlockReputationCenti));
            else if (d.RequiresStationTier > sim.StationTier(d.StationIndex))
                reason = Loc.T("ui.menu.needs_equipment",
                               Loc.T("station." + App.Content.Stations[d.StationIndex].Id));
            else
                reason = Loc.T("ui.menu.locked");

            row.Add(Theme.Text(reason, Theme.FontSmall, Theme.Warn));
            return row;
        }

        /// <summary>
        /// The price colour against the market. Green cheap, amber dear, red
        /// very dear - the thresholds sit in the same places as the price
        /// sensitivity breaks in docs/12 §5.3.
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
    /// The crew. docs/14: three candidates are visible and the player
    /// chooses.
    ///
    /// The candidates' TRAITS are written out plainly - a choice is only a
    /// decision if you can see what you are choosing. Blind hiring was
    /// measured and took a good player's reputation from 96.5 down to 87
    /// (docs/34 §21).
    /// </summary>
    public sealed class StaffScreen : ListScreen
    {
        private int _confirmFire = -1;

        protected override string Title { get { return Loc.T("ui.morning.staff"); } }
        protected override string Subtitle
        {
            get
            {
                // NOT "REQUIRED" BUT "TO SERVE EVERYONE".
                //
                // The number is a capacity calculation: how many people it
                // takes to keep up with ALL of today's demand. It is NOT the
                // best number for profit - the balance tool measured it: a
                // player running one waiter short earns about 4,000 coins
                // more over 60 days and, in exchange, cannot seat close to a
                // hundred people.
                //
                // REPUTATION DOES NOT CHANGE. This comment said "drops the
                // reputation a little" for a while; docs/42 §4 MEASURED it
                // and disproved it (78 / 78). What drops is the CREW score -
                // so the price is paid in the year-end evaluation, not in the
                // daily reputation.
                //
                // So there is a DECISION here and the game should leave it to
                // the player. Saying "required" hid the decision: the player
                // matched the number, lost money, and had nowhere to learn
                // why.
                //
                // The weekend is written out separately: a crew decision is
                // made by looking at THE WEEK, not at the weekdays.
                Crew need = App.Sim.RequiredCrewToday();
                Crew peak = App.Sim.RequiredCrewPeak();
                string peakText = peak.Hall != need.Hall || peak.Cooks != need.Cooks
                    ? string.Format("   ·   {0}: {1} + {2}",
                                    Loc.T("ui.staff.weekend"), peak.Cooks, peak.Hall)
                    : string.Empty;
                return string.Format("{0}: {1} + {2}{3}   ·   {4}: {5}",
                    Loc.T("ui.staff.to_serve_all"), need.Cooks, need.Hall, peakText,
                    Loc.T("ui.staff.cap"), App.Sim.StaffCap);
            }
        }

        /// <summary>
        /// The NAME of the hall worker changes with the cuisine.
        ///
        /// Fast food is SELF SERVICE: no waiter comes to the table, ordering
        /// and paying happen at the counter. What is left to do in the hall
        /// is clearing trays - so that person is not a waiter but a BUSSER.
        /// The name should say so, because the player needs to know WHAT the
        /// person they hired actually does.
        ///
        /// The keys are written out in full: the text generator scans the
        /// screens and rejects text that is not used, and it cannot see a
        /// computed key (see Badges.NameKey, QualityKey).
        ///
        /// The busser's key is in the "ui." family rather than "role.":
        /// "role.*" names come FROM THE CONTENT (staff-roles.json nameKey)
        /// and staff-roles has no busser role - fast food's hall is a
        /// cashier plus a dishwasher. What is chosen here is not the role
        /// itself but the name SHOWN to the player.
        /// </summary>
        // NOT STATIC: `App` is an INSTANCE member of UiScreen.
        private string HallRoleKey()
        {
            return App.Content != null && App.Content.SelfService
                ? "ui.staff.busser" : "role.garson";
        }

        protected override void Fill(VisualElement list)
        {
            list.Add(Theme.Head(Loc.T("ui.staff.cook")));
            for (int i = 0; i < App.Sim.Cooks; i++) list.Add(Person(0, i));


            list.Add(Theme.Head(Loc.T("ui.staff.hall")));
            for (int i = 0; i < App.Sim.HallStaff; i++) list.Add(Person(1, i));

            // THE EMPTY STATE is written out. There used to be nothing under
            // the heading and the player could not tell three things apart:
            // is the crew zero, did the list fail to load, or is this an
            // error.
            if (App.Sim.HallStaff == 0)
            {
                Crew n = App.Sim.RequiredCrewToday();
                list.Add(n.Hall > 0
                    ? Theme.Text(Loc.T("ui.staff.hall_needed", n.Hall),
                                 Theme.FontSmall, Theme.Warn)
                    : Theme.Text(Loc.T("ui.staff.hall_none"),
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
        /// SINK DUTY.
        ///
        /// The user's own sentence: "once you take on a dishwasher everyone
        /// does their own job". A dishwasher is NOT a separate crew -
        /// docs/14 sets the hall up as "waiter + dishwasher + cashier, one
        /// work pool" and the wage comes out of that blend. A separate pool
        /// would be counting the same person in two wage tables.
        ///
        /// The player's decision is the same: they set one person's worth of
        /// crew aside for the sink. If they do not, then once the plates pile
        /// up a waiter goes over to the sink of their own accord and the
        /// service falters - the price is visible, the choice is real.
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
            Button fewer = Theme.Btn(Loc.T("ui.staff.sink_remove"), () =>
            {
                App.Send(CommandKind.SetDishwashers, App.Sim.Dishwashers - 1);
                Ui.Refresh();
            });
            fewer.SetEnabled(n > 0);
            row.Add(fewer);

            Button more = Theme.Btn(Loc.T("ui.staff.sink_add"), () =>
            {
                App.Send(CommandKind.SetDishwashers, App.Sim.Dishwashers + 1);
                Ui.Refresh();
            });
            // The WHOLE hall crew cannot be put on the sink: nobody would be
            // serving. The core clips at the same limit; a disabled button is
            // better than a tap that gets rejected.
            more.SetEnabled(n < App.Sim.HallStaff);
            row.Add(more);
            card.Add(row);

            card.Add(Theme.Text(Loc.T("ui.staff.sink_hint"),
                                Theme.FontSmall, Theme.InkFaint));

            // CLEAN PLATES: the bottleneck's number. The answer to the
            // player's "why is the kitchen waiting" sits right here.
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
            // THE NAME if there is one, otherwise a numbered label.
            //
            // The sentence "Cook 2 has resigned" is the same thing as
            // "capacity -28"; "Nurten Abla has left" is not. The whole
            // emotional weight of the staff system is in that one line.
            string name = Loc.StaffName(sim.StaffNameIndex(pool, index));
            head.Add(Theme.Text(
                string.IsNullOrEmpty(name)
                    ? Loc.T(pool == 0 ? "role.asci" : HallRoleKey()) + " " + (index + 1)
                    : name,
                Theme.FontBody));

            int morale = sim.StaffMorale(pool, index);
            head.Add(Theme.Text(Loc.T("ui.staff.morale") + " " + morale,
                                Theme.FontSmall, MoraleColor(morale)));
            card.Add(head);

            // THE ROLE NAME SITS NEXT TO THE TRAIT.
            //
            // The card shows THE NAME, and the role name only came out as a
            // fallback when there was no name - so because the staff have
            // names, "Busser" NEVER appeared. That is, in fast food the
            // player could not learn that the person in the hall was a busser
            // and NOT a waiter; the visible half of the self-service change
            // (docs/51) was missing.
            //
            // The tour caught it: the label check passed on Turkish cuisine
            // and STAYED on fast food. When I first wrote it I said "the
            // screen says Busser" and I had not looked.
            card.Add(Theme.Text(
                Loc.T(pool == 0 ? "role.asci" : HallRoleKey())
                    + " · " + TraitText(pool, index),
                Theme.FontSmall, Theme.InkDim));
            card.Add(Theme.Field(Loc.T("ui.staff.level"),
                                 Loc.T("ui.staff.days",
                                       sim.StaffLevel(pool, index),
                                       sim.StaffDaysWorked(pool, index)),
                                 Theme.InkDim));

            // It is the card's OWN PERSON who is let go, and CONFIRMATION is
            // asked for.
            //
            // The index was not being sent at first: the button on the "Cook
            // 2" card let Cook 1 go. And there was no confirmation either -
            // one tap and a member of staff who had built up levels and days
            // was gone.
            int key = pool * 100 + index;
            if (_confirmFire == key)
            {
                card.Add(Theme.Text(Loc.T("ui.staff.fire_confirm"),
                                    Theme.FontSmall, Theme.Bad));
                // CANCEL FIRST AND AS THE PRIMARY, DISMISS IN RED.
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

            card.Add(Theme.Head(Loc.T(pool == 0 ? "role.asci" : HallRoleKey())));

            // THE TRAIT'S NAME IS NOT ENOUGH, WHAT IT DOES IS NEEDED.
            //
            // The card showed only the name and the wage/speed percentages.
            // The four traits that touch neither speed nor wage ("Calm",
            // "Hardy", "Good With Guests", "Surly") appeared on the card as
            // "normal / normal" - so the player could not tell someone who
            // sends a guest away +8 points happier from someone who does
            // nothing at all.
            // THE TRAIT'S VOICE: one sentence, at the top of the card.
            //
            // Twenty regulars had three beats each; the staff had ZERO lines
            // (docs/53). The candidate card is the only moment the player
            // reads a member of staff CAREFULLY - that is where a sentence
            // does the most work.
            //
            // The sentence comes from the FIRST trait, not from both: two
            // voices on top of one another and you read a list, not a person.
            {
                int first = sim.CandidateTrait(pool, slot, 0);
                if (first >= 0)
                {
                    Label voice = Theme.Text(
                        Loc.T(App.Economy.TraitAt(first).NameKey + ".voice"),
                        Theme.FontSmall, Theme.InkFaint);
                    voice.style.whiteSpace = WhiteSpace.Normal;
                    voice.style.marginBottom = 6;
                    card.Add(voice);
                }
            }

            for (int w = 0; w < 2; w++)
            {
                int t = sim.CandidateTrait(pool, slot, w);
                if (t < 0) continue;
                TraitDef d = App.Economy.TraitAt(t);
                card.Add(Theme.Text("• " + Loc.T(d.NameKey), Theme.FontSmall, Theme.Ink));
                Label what = Theme.Text(Loc.T(d.NameKey + ".desc"),
                                        Theme.FontSmall, Theme.InkFaint);
                what.style.marginLeft = 14;
                what.style.whiteSpace = WhiteSpace.Normal;
                card.Add(what);
            }

            int wage = sim.CandidateWageBp(pool, slot);
            int speed = sim.CandidateSpeedBp(pool, slot);
            // The `true` is Loc.Percent's "put a + in front" flag; see the
            // note on the menu card for why it is positional.
            card.Add(Theme.Field(Loc.T("ui.staff.wage"),
                                 (wage == 0 ? Loc.T("ui.staff.normal") : Loc.Percent(wage, true)),
                                 wage > 0 ? Theme.Bad : (wage < 0 ? Theme.Good : Theme.InkDim)));
            card.Add(Theme.Field(Loc.T("ui.hud.speed"),
                                 (speed == 0 ? Loc.T("ui.staff.normal") : Loc.Percent(speed, true)),
                                 speed > 0 ? Theme.Good : (speed < 0 ? Theme.Bad : Theme.InkDim)));

            bool room = sim.Cooks + sim.HallStaff < sim.StaffCap;
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
            // NO TRAIT = INHERITED. But there are TWO people inherited: the
            // cook and the one in the hall (Simulation.cs, both with trait
            // -1). Using a single piece of text put "the cook you inherited"
            // on the waiter's card - in five languages at once.
            if (a == null && b == null)
                return Loc.T(pool == 0
                    ? "ui.staff.inherited"
                    : "ui.staff.inherited_hall");
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
    /// Equipment. docs/27 Decision D: an upgrade either adds a slot or lets
    /// the cook go earlier; it DOES NOT TOUCH a dish's cooking time. The
    /// screen says it that way too, because saying "makes it faster" would
    /// be wrong.
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

            // Cold storage first: it buys menu breadth.
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

            // ONLY THE STATIONS THIS CUISINE COOKS ON.
            //
            // This listed all of them, and the simulation refuses to sell the
            // ones nothing cooks on (BuyEquipment, reason 4). So the Turkish
            // player was shown an OVEN card and, since the fryer arrived, a
            // FRYER card as well - each with a live "Upgrade" button and a
            // price - and pressing it produced the generic rejection notice,
            // which names no reason, and no money moved. The kitchen was
            // right all along: RestaurantView filters on the same flag, so
            // the shop was offering a machine the room does not contain.
            for (int st = 0; st < sim.StationCount; st++)
            {
                if (!sim.IsStationUsed(st)) continue;
                list.Add(Station(st));
            }

            // Expansion at the very bottom: the most expensive and the least
            // reversible decision.
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
            // "cuisine only" is a LABEL, not a call to act - muted ink rather
            // than the accent colour.
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
                // THE TIER INDEX IS ESSENTIAL.
                //
                // This line went without an index for a while and the command
                // was being silently rejected every time: the default tier 0
                // is already four tables, and Expand says "the target tier
                // must be greater than the current one". So the restaurant
                // COULD NEVER GROW - the game's entire progression axis was
                // closed and nothing raised an error.
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
