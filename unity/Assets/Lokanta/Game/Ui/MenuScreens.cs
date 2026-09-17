using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// The content could not be loaded. The game DOES NOT OPEN (docs/23
    /// §9.1) and the reason is written ON THE SCREEN - the user should not
    /// have to read the Unity log.
    /// </summary>
    public sealed class ErrorScreen : UiScreen
    {
        private readonly string _message;
        public ErrorScreen(string message) { _message = message; }

        public override VisualElement Build()
        {
            VisualElement root = Backdrop();
            VisualElement card = Theme.PanelBox();
            card.style.maxWidth = 640;
            card.style.alignSelf = Align.Center;
            card.style.marginTop = 80;

            Label title = Theme.Title(Loc.T("ui.error.title"));
            title.style.color = Theme.Bad;
            card.Add(title);
            card.Add(Theme.Text(Loc.T("ui.error.content"),
                                Theme.FontBody, Theme.InkDim));
            card.Add(Theme.Divider());

            Label detail = Theme.Text(_message, Theme.FontSmall, Theme.Warn);
            detail.style.whiteSpace = WhiteSpace.Normal;
            card.Add(detail);

            // A WAY OUT. THIS SCREEN HAD NONE.
            //
            // It was built with a title, two labels and a divider - no
            // control of any kind. At boot it is opened with Replace, so it
            // is the only screen on the stack: pressing back popped it and
            // found nothing underneath, leaving an empty root with no screen
            // and no way to get one. The one screen that exists to explain a
            // failure was itself a dead end.
            //
            // Closing is the only honest offer. The content did not load, so
            // there is no game behind this to return to.
            card.Add(Theme.Divider());
            card.Add(Theme.Btn(Loc.T("ui.error.quit"), () =>
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }, wide: true));

            root.Add(card);
            return root;
        }

        /// <summary>
        /// The common backdrop for the menu pages. SCROLLABLE.
        ///
        /// It used to be a plain box with the content CENTRED vertically.
        /// When it did not fit it was cut off at both ends at once, and
        /// with no scrolling it became unreachable: on the save-slot screen
        /// the fourth slot and the "Back" button were off the screen - so
        /// the player could not get into the game at all. And this is the
        /// game's SECOND screen.
        ///
        /// Centring is only safe when the content fits, and the layout does
        /// not know in advance whether it will.
        /// </summary>
        /// <summary>
        /// A COLUMN THAT SPILLS INTO THE NEXT ONE.
        ///
        /// The game is LANDSCAPE: 873 x 393 dp. Every menu page was built as
        /// a single centred column capped at 420-560 dp, so a page with more
        /// than about five rows ran off the bottom of a screen that had four
        /// hundred dp of unused width on either side of it. The tour's new
        /// layout check put numbers on it:
        ///
        ///     main menu        [Quit] 36 dp past the edge
        ///     cuisine choice   [Back] 316 dp past, 6 elements
        ///     slot choice      [Back] 506 dp past, 8 elements
        ///     settings         [Back] 189 dp past, 8 elements
        ///
        /// And because Theme.Mobile hides the scrollbars, none of it said so.
        ///
        /// `flexWrap` on a column turns the height into the wrapping axis:
        /// children fill the first column, then start a second beside it. One
        /// property, no per-screen layout code, and a page that already fits
        /// is untouched - wrapping only happens when something does not fit.
        ///
        /// THE HEIGHT HAS TO COME FROM THE VIEWPORT, and the first attempt
        /// got this wrong in a way worth recording: `height = 100%` inside a
        /// ScrollView resolves against the CONTENT container, and that grows
        /// with its content. A column asking to be as tall as the box its own
        /// content defines has no bound at all, so `flexWrap` had nothing to
        /// wrap against and the four screens came back with byte-identical
        /// failures. The bound is `contentViewport`, which is the window the
        /// content scrolls behind and does not move with it - read in
        /// WrapWidth once the column is in the tree.
        /// </summary>
        internal static VisualElement WrapColumn()
        {
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.flexWrap = Wrap.Wrap;
            col.style.alignSelf = Align.Center;
            col.style.alignContent = Align.Center;
            col.style.justifyContent = Justify.Center;
            col.style.maxWidth = Length.Percent(100);
            col.style.flexShrink = 0;

            // Each child is given the column's width rather than the container
            // being given one: in a wrapping column the container's width is
            // what the children make it, so capping the container would cap
            // the whole set of columns and squeeze them all.
            col.style.width = StyleKeyword.Auto;
            return col;
        }

        /// <summary>
        /// Gives every child of a wrapping column its width. Called once,
        /// after the children are in.
        ///
        /// The width goes on the CHILDREN, not on the container: in a wrapping
        /// column the container's width is whatever its columns add up to, so
        /// capping the container would cap the whole set and squeeze them
        /// together. One walk at the end rather than a call per child, so the
        /// screens keep reading as a plain list of what is on them.
        /// </summary>
        internal static void WrapWidth(VisualElement col, float width)
        {
            for (int i = 0; i < col.childCount; i++)
            {
                VisualElement c = col[i];
                c.style.width = width;
                c.style.flexShrink = 0;
            }

            // AND THE HEIGHT, from the viewport, once there is one.
            //
            // The callback is on the VIEWPORT, not on the column: the
            // viewport's height depends on the screen and the column's
            // depends on what we are about to put in it, so listening to the
            // viewport cannot feed itself. Listening to the column would.
            ScrollView sv = col.GetFirstAncestorOfType<ScrollView>();
            if (sv == null) return;
            sv.contentViewport.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float h = sv.contentViewport.resolvedStyle.height;
                if (h > 1f) col.style.height = h;
            });
        }

        /// <summary>
        /// A title and a Back button on ONE line.
        ///
        /// Stacked, they cost 52 dp of height plus a gap, and on a 393 dp
        /// landscape screen that was exactly the difference between the
        /// cuisine and slot screens fitting and their Back buttons hanging
        /// below the bottom edge. Side by side they cost nothing: the title
        /// was never using the width.
        ///
        /// It also puts Back in the top-right, which in landscape is the
        /// corner a thumb does NOT reach - correct for a control you want
        /// findable and not easy to hit by accident. The Android back key
        /// remains the quick way out (UiRoot).
        /// </summary>
        internal static VisualElement Header(string title, System.Action back)
        {
            VisualElement row = Theme.Row(Theme.Gap);
            row.style.alignItems = Align.Center;
            row.style.flexShrink = 0;

            Label t = Theme.Title(title);
            t.style.flexGrow = 1;
            t.style.marginBottom = 0;
            row.Add(t);

            Button b = Theme.Btn(Loc.T("ui.hud.back"), back);
            b.style.width = 140;
            b.style.flexShrink = 0;
            row.Add(b);
            return row;
        }

        internal static VisualElement Backdrop() { return Backdrop(opaque: true); }

        /// <param name="opaque">
        /// FALSE lets the 3D scene behind show through a scrim.
        ///
        /// Only the MAIN MENU uses it, and only because there is a restaurant
        /// behind it to show (GameApp.ShowMenuScene). Every other menu page
        /// stays opaque: a settings list over a moving hall is the "ghosting"
        /// fault this project already fixed once on the morning screens, where
        /// three per cent of a lit scene leaking through was enough to make the
        /// rows unreadable.
        ///
        /// 0.55 rather than the pause screen's 0.72: the pause scrim's job is
        /// to say "the game has stopped", and this one's is the opposite - the
        /// hall should still read as alive underneath.
        /// </param>
        internal static VisualElement Backdrop(bool opaque)
        {
            ScrollView v = Theme.Mobile(new ScrollView(ScrollViewMode.Vertical));
            v.style.flexGrow = 1;
            v.style.minHeight = 0;
            v.style.backgroundColor = opaque
                ? Theme.Bg
                : new Color(Theme.Bg.r, Theme.Bg.g, Theme.Bg.b, 0.55f);
            v.style.paddingLeft = Theme.Pad * 2;
            v.style.paddingRight = Theme.Pad * 2;
            v.style.paddingTop = Theme.Pad * 2;
            v.style.paddingBottom = Theme.Pad * 2;

            // The content MAY sit centred vertically, but when it does not
            // fit it has to start from the top; as the ScrollView's
            // container grows the centring lifts of its own accord.
            v.contentContainer.style.flexGrow = 1;
            v.contentContainer.style.justifyContent = Justify.Center;
            return v;
        }
    }

    /// <summary>
    /// The main menu.
    ///
    /// "Continue" is at the top and only appears IF THERE IS A SAVE. The
    /// first thing a player returning to a management game wants is where
    /// they left off; putting the new game first means risking the wrong
    /// button on every launch.
    /// </summary>
    public sealed class MainMenuScreen : UiScreen
    {
        public override VisualElement Build()
        {
            // THE HALL SHOWS THROUGH. GameApp builds a real preview
            // simulation at boot - four tables, fast food, the figures walking
            // - so the first frame of the game finally has the game in it.
            VisualElement root = ErrorScreen.Backdrop(opaque: false);

            // LANDSCAPE: it spills into a second column instead of off the
            // bottom. Backdrop.WrapColumn, and WrapWidth after the children
            // are in.
            // ONE PRIMARY, AND THE REST QUIET.
            //
            // It was five buttons of equal weight in a wrapping column, and at
            // 873 x 393 with the title and the tagline above them the set did
            // not fit: the column spilled and "Quit" ended up in a second
            // column beside the others. Five equally loud choices is also not
            // what a menu is - the player came to play, and everything else on
            // this screen is somewhere they go once.
            //
            // Continue when there is a save, New Game when there is not. The
            // other one joins the quiet row, so the row has three or four items
            // and the column always has exactly two.
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.alignSelf = Align.Center;
            col.style.alignItems = Align.Center;
            col.style.flexShrink = 0;

            Label title = Theme.Text(Loc.T("ui.game.title"), Theme.FontHuge, Theme.Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 2;
            col.Add(title);

            Label sub = Theme.Text(Loc.T("ui.menu.tagline"), Theme.FontBody, Theme.Ink);
            sub.style.unityTextAlign = TextAnchor.MiddleCenter;
            sub.style.marginBottom = Theme.Pad;
            col.Add(sub);

            bool saved = AnySave();
            Button primary = saved
                ? Theme.Btn(Loc.T("ui.menu.continue"),
                            () => Ui.Push(new SlotScreen(SlotScreen.Mode.Load)),
                            primary: true)
                : Theme.Btn(Loc.T("ui.menu.new"),
                            () => Ui.Push(new CuisineScreen()), primary: true);
            primary.style.width = 320;
            primary.style.flexShrink = 0;
            col.Add(primary);

            // THE QUIET ROW. Small, level with each other, and nowhere near as
            // loud as the one button the player came for.
            VisualElement rest = Theme.Row(Theme.Gap);
            rest.style.justifyContent = Justify.Center;
            rest.style.flexWrap = Wrap.Wrap;

            if (saved) Minor(rest, Loc.T("ui.menu.new"), () => Ui.Push(new CuisineScreen()));
            Minor(rest, Loc.T("ui.menu.settings"), () => Ui.Push(new SettingsScreen()));
            Minor(rest, Loc.T("ui.menu.credits"), () => Ui.Push(new CreditsScreen()));
#if UNITY_STANDALONE || UNITY_EDITOR
            Minor(rest, Loc.T("ui.menu.quit"), Quit);
#endif
            col.Add(rest);

            root.Add(col);
            return root;
        }

        /// <summary>
        /// A secondary menu entry: the ordinary button treatment at a width
        /// that fits its own label, so four of them sit on one line.
        ///
        /// It keeps `Theme.Btn` rather than being a bare Label, because that is
        /// where the 52 dp touch floor and the pressed state live - the two
        /// things this project has already lost once by building a control out
        /// of `new Button(...)`.
        /// </summary>
        private static void Minor(VisualElement row, string label, System.Action onClick)
        {
            Button b = Theme.Btn(label, onClick);
            b.style.paddingLeft = Theme.Pad;
            b.style.paddingRight = Theme.Pad;
            b.style.flexShrink = 0;
            row.Add(b);
        }

        private static bool AnySave()
        {
            for (int i = 0; i < SaveStore.SlotCount; i++)
                if (SaveStore.Read(i).Exists) return true;
            return false;
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // On the main menu the back key is NOT quit: throwing someone out
        // of the game by accident is the most annoying thing there is on
        // mobile.
        public override bool OnBack() { return false; }
    }

    /// <summary>
    /// Choosing the cuisine. docs/07: the choice is TIED to the save and
    /// cannot be changed, so at the moment of choosing the player has to
    /// see what they are taking on - the signature mechanic included.
    /// </summary>
    public sealed class CuisineScreen : UiScreen
    {
        public override VisualElement Build()
        {
            VisualElement root = ErrorScreen.Backdrop();

            // THE TWO CARDS SIT SIDE BY SIDE. This screen is not a list, it
            // is a COMPARISON: two cuisines, two signature mechanics, one
            // irreversible choice tied to the save (docs/07). Stacked in a
            // portrait column on an 873 dp-wide landscape phone, the second
            // one was below the fold - so the screen that sells the game
            // offered one of the two things it is selling.
            //
            // Generic wrapping was tried first and it is the wrong tool here:
            // it packs by height and does not know that the title belongs
            // above BOTH cards and the Back button below both, so it pushed
            // them into a third column and off the side of the screen. The
            // tour's layout check did not catch that either, because it was
            // only watching the bottom edge - both were fixed together.
            // width, NOT maxWidth. A column with an auto width and a
            // maxWidth of 100% sizes itself to its CONTENT and the cap never
            // binds - the two cards pushed it wider than the screen and the
            // buttons inside them landed 47 dp outside. The same trap as the
            // height in WrapColumn, one property along.
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            col.Add(ErrorScreen.Header(Loc.T("ui.cuisine.pick"), () => Ui.Pop()));

            VisualElement row = Theme.Row(Theme.Gap);
            row.style.alignItems = Align.FlexStart;
            foreach (VisualElement card in new[]
            {
                Card("fastfood", Loc.T("cuisine.fastfood"),
                     Loc.T("ui.cuisine.combo"),
                     Loc.T("ui.cuisine.fastfood_desc")),
                Card("turk", Loc.T("cuisine.turk"),
                     Loc.T("ui.cuisine.credit"),
                     Loc.T("ui.cuisine.turk_desc")),
            })
            {
                // Equal halves rather than a fixed width: Spanish is the
                // longest of the five languages and the card has to be able
                // to grow taller rather than wider.
                card.style.flexGrow = 1;
                card.style.flexBasis = 0;
                card.style.marginBottom = 0;
                row.Add(card);
            }
            col.Add(row);
            root.Add(col);
            return root;
        }

        private VisualElement Card(string id, string name, string signature, string flavour)
        {
            VisualElement card = Theme.PanelBox();

            Label t = Theme.Text(name, Theme.FontTitle, Theme.Accent);
            t.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.Add(t);
            card.Add(Theme.Text(flavour, Theme.FontSmall, Theme.InkDim));
            card.Add(Theme.Divider());
            card.Add(Theme.Text(Loc.T("ui.cuisine.signature"), Theme.FontSmall, Theme.InkFaint));
            card.Add(Theme.Text(signature, Theme.FontBody));
            card.Add(Theme.Btn(Loc.T("ui.cuisine.start"),
                () => Ui.Push(new SlotScreen(SlotScreen.Mode.New, id)), primary: true));

            return card;
        }
    }

    /// <summary>
    /// The save slots. docs/21, four slots.
    ///
    /// Writing over a full slot asks for CONFIRMATION. A campaign is sixty
    /// days; deleting it without asking is not acceptable.
    /// </summary>
    public sealed class SlotScreen : UiScreen
    {
        public enum Mode { New, Load }

        private readonly Mode _mode;
        private readonly string _cuisine;
        private int _confirmDelete = -1;
        private int _confirmSlot = -1;

        public SlotScreen(Mode mode, string cuisine = null)
        {
            _mode = mode;
            _cuisine = cuisine;
        }

        public override VisualElement Build()
        {
            VisualElement root = ErrorScreen.Backdrop();

            // FOUR SLOTS IN A 2 x 2 GRID. Stacked in a portrait column, slot 2
            // was cut mid-word and slots 3 and 4 were off the screen entirely
            // - on the game's SECOND screen, and Backdrop's own docstring
            // records this exact screen once stranding the player.
            //
            // Generic wrapping was tried first and it moved the problem rather
            // than solving it: it packs purely by height, so it pushed the
            // title and the Back button into a third column and 367 dp off the
            // side. A title belongs above ALL the slots and Back below them,
            // and only the slots themselves want to be a grid - which is
            // something the screen knows and a generic helper cannot.
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            col.Add(ErrorScreen.Header(Loc.T("ui.slot.title"), () => Ui.Pop()));

            VisualElement grid = Theme.Row(Theme.Gap);
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.alignItems = Align.FlexStart;
            for (int i = 0; i < SaveStore.SlotCount; i++)
            {
                VisualElement slot = Row(i);
                // ALL FOUR ON ONE LINE, not two by two.
                //
                // 2 x 2 was tried and the second row's cards ran 45 dp below
                // the screen: the header takes 52 dp and what is left does not
                // divide into two slot cards. Four across is the shape the
                // screen actually has - 873 dp of width and 393 of height -
                // and it gives every slot the same prominence, which is right
                // for four things the player is choosing between.
                //
                // Just under a quarter: the three gaps between four cards have
                // to come out of the width, or the fourth wraps.
                slot.style.width = Length.Percent(23.5f);
                slot.style.flexShrink = 0;
                slot.style.marginBottom = 0;
                grid.Add(slot);
            }
            col.Add(grid);
            root.Add(col);
            return root;
        }

        private VisualElement Row(int slot)
        {
            SlotInfo info = SaveStore.Read(slot);
            VisualElement card = Theme.PanelBox();

            VisualElement head = Theme.Row(0);
            head.style.justifyContent = Justify.SpaceBetween;
            head.Add(Theme.Text((slot + 1) + ". " + Loc.T("ui.slot.title"),
                                Theme.FontBody, Theme.InkDim));

            if (info.Broken)
                head.Add(Theme.Text(Loc.T("ui.slot.broken"), Theme.FontSmall, Theme.Bad));
            else if (info.Exists)
                head.Add(Theme.Text(info.Saved.ToLocalTime().ToString("dd.MM.yyyy HH:mm", Loc.Culture),
                                    Theme.FontSmall, Theme.InkFaint));
            card.Add(head);

            if (info.Exists && !info.Broken)
            {
                card.Add(Theme.Text(Loc.T("cuisine." + info.Cuisine), Theme.FontTitle));
                VisualElement stats = Theme.Row(Theme.Pad);
                stats.Add(Theme.Text(Loc.T("ui.slot.day", info.Day), Theme.FontSmall, Theme.InkDim));
                stats.Add(Theme.Text(Loc.Money(info.Cash), Theme.FontSmall,
                                     Theme.CashColor(info.Cash)));
                stats.Add(Theme.Text(Loc.T("ui.hud.reputation") + " "
                                     + Loc.Reputation(info.ReputationCenti),
                                     Theme.FontSmall,
                                     Theme.ReputationColor(info.ReputationCenti)));
                card.Add(stats);
            }
            else if (!info.Broken)
            {
                card.Add(Theme.Text(Loc.T("ui.slot.empty"), Theme.FontTitle, Theme.InkFaint));
            }

            // --- the actions --------------------------------------------------
            if (_confirmDelete == slot)
            {
                card.Add(Theme.Text(Loc.T("ui.slot.delete_confirm"),
                                    Theme.FontSmall, Theme.Bad));
                VisualElement ask = Theme.Row(Theme.Gap);
                ask.Add(Theme.Btn(Loc.T("ui.common.cancel"),
                    () => { _confirmDelete = -1; Ui.Refresh(); },
                    primary: true, wide: true));
                ask.Add(Theme.Btn(Loc.T("ui.slot.delete"), () =>
                {
                    SaveStore.Delete(slot);
                    Sfx.Cancel();
                    _confirmDelete = -1;
                    Ui.Refresh();
                }, wide: true, danger: true));
                card.Add(ask);
                return card;
            }

            if (_confirmSlot == slot)
            {
                card.Add(Theme.Text(Loc.T("ui.slot.overwrite"), Theme.FontSmall, Theme.Warn));
                VisualElement ask = Theme.Row(Theme.Gap);
                ask.Add(Theme.Btn(Loc.T("ui.common.cancel"),
                    () => { _confirmSlot = -1; Ui.Refresh(); },
                    primary: true, wide: true));
                ask.Add(Theme.Btn(Loc.T("ui.common.yes"), () => Begin(slot),
                                  wide: true, danger: true));
                card.Add(ask);
            }
            else
            {
                VisualElement actions = Theme.Row(Theme.Gap);
                if (_mode == Mode.Load)
                {
                    if (info.Exists && !info.Broken)
                        actions.Add(Theme.Btn(Loc.T("ui.menu.continue"),
                            () => Begin(slot), primary: true, wide: true));
                    else
                        // A broken slot is NOT "Empty". The badge at the top
                        // said "broken save" while the disabled button below
                        // it said "Empty"; the two halves of the same card
                        // contradicted each other, and this was the screen
                        // the player was reading at the very moment they
                        // could not find their campaign.
                        actions.Add(Disabled(Loc.T(info.Broken
                            ? "ui.slot.unloadable" : "ui.slot.empty")));
                }
                else
                {
                    actions.Add(Theme.Btn(info.Exists ? Loc.T("ui.menu.new") : Loc.T("ui.cuisine.start"),
                        () =>
                        {
                            if (info.Exists) { _confirmSlot = slot; Ui.Refresh(); }
                            else Begin(slot);
                        }, primary: true, wide: true));
                }

                // Deleting asks for CONFIRMATION too.
                //
                // Overwriting asked, deleting did not - of two equally
                // destructive actions one was guarded and the other wiped a
                // sixty-day campaign with a single tap.
                if (info.Exists)
                    actions.Add(Theme.Btn(Loc.T("ui.slot.delete"), () =>
                    {
                        _confirmDelete = slot;
                        Ui.Refresh();
                    }));
                card.Add(actions);
            }

            return card;
        }

        private static VisualElement Disabled(string text)
        {
            Label l = Theme.Text(text, Theme.FontBody, Theme.InkFaint);
            l.style.minHeight = Theme.Touch;
            l.style.unityTextAlign = TextAnchor.MiddleLeft;
            l.style.flexGrow = 1;
            return l;
        }

        private void Begin(int slot)
        {
            bool ok = _mode == Mode.New
                ? App.StartNew(_cuisine, slot)
                : App.LoadSlot(slot);

            if (!ok)
            {
                Sfx.Cancel();
                Ui.Push(new ErrorScreen(App.LoadError ?? Loc.T("ui.error.save")));
                return;
            }
            if (_mode == Mode.New) App.SaveToSlot(slot);

            Sfx.Confirm();
            Ui.Replace(new GameScreen());
        }
    }

    /// <summary>Settings. The volumes persist; the player should not have to set them on every launch.</summary>
    public sealed class SettingsScreen : UiScreen
    {
        // THESE ARE PlayerPrefs KEYS, so they keep their original spelling:
        // renaming one would lose every player's saved setting.
        private const string KeySfx = "lokanta.ses";
        private const string KeyMusic = "lokanta.muzik";

        public static void Apply(Music music)
        {
            Sfx.Volume = PlayerPrefs.GetFloat(KeySfx, 0.7f);
            if (music != null) music.Volume = PlayerPrefs.GetFloat(KeyMusic, 0.22f);
        }

        public override VisualElement Build()
        {
            VisualElement root = ErrorScreen.Backdrop();
            // LANDSCAPE: it spills into a second column instead of off the
            // bottom. Backdrop.WrapColumn, and WrapWidth after the children
            // are in.
            VisualElement col = ErrorScreen.WrapColumn();

            col.Add(Theme.Title(Loc.T("ui.settings.title")));

            col.Add(Level(Loc.T("ui.settings.sound"),
                PlayerPrefs.GetFloat(KeySfx, 0.7f), v =>
                {
                    PlayerPrefs.SetFloat(KeySfx, v);
                    Sfx.Volume = v;
                    Sfx.Click();            // let the new level be heard AT ONCE
                }));

            col.Add(Level(Loc.T("ui.settings.music"),
                PlayerPrefs.GetFloat(KeyMusic, 0.22f), v =>
                {
                    PlayerPrefs.SetFloat(KeyMusic, v);
                    if (App.Music != null) App.Music.Volume = v;
                }));

            // Once a hint has been seen it never comes back, and that is
            // stored ON THE DEVICE, not in the save. A player showing the
            // game to someone, or coming back after a long break, needs a
            // way to bring them back - otherwise those first sentences can
            // never be read again.
            col.Add(Theme.Btn(Loc.T("ui.settings.hints_reset"),
                              () => Hints.Reset()));

            // THE LANGUAGE IS NOW A CHOICE.
            //
            // It used to be a ROW that only said what the language was - the
            // game had one language and the row was not even information, it
            // was a promise.
            //
            // Each language is written in ITS OWN name: anyone looking for a
            // row that says "Turkish" already reads English.
            col.Add(Theme.Head(Loc.T("ui.settings.language")));
            VisualElement languages = Theme.Row(Theme.Gap);
            // THE STRIP WRAPS: five languages do not fit on one line.
            //
            // With two they fitted and the row was fixed. With five it would
            // overflow at phone width (~400 dp) - and the tour's
            // "element off the edge of the screen" check would catch the
            // overflowing element.
            languages.style.flexWrap = Wrap.Wrap;

            for (int i = 0; i < Loc.Languages.Length; i++)
            {
                int idx = i;
                bool selected = Loc.Language == i;
                Button button = Theme.Btn(Loc.LanguageNames[i], () =>
                {
                    Loc.SetLanguage(idx);
                    // The font depends on the language too: switching to
                    // Chinese without changing the root element's font gives
                    // empty boxes instead of text.
                    Ui.ApplyLanguage();
                    // Every screen reads its text when it is built:
                    // refreshing the stack carries the new language
                    // everywhere.
                    Ui.Refresh();
                }, primary: selected, wide: true);

                // EACH LANGUAGE WRITES ITS OWN NAME IN A FONT THAT CAN SHOW IT.
                //
                // Rubik DOES NOT HAVE the glyphs for the Chinese name. With
                // the game in Turkish, that button showed empty boxes - so
                // someone wanting to pick Chinese could not see which button
                // to press. The font check caught exactly this (U+4E2D,
                // U+6587 - Loc.cs).
                //
                // The fix is LOCAL: that one button only. Turning the whole
                // screen over to the CJK font would mean changing the font
                // of the Turkish screen because of one language button.
                if (Loc.Languages[idx] == "zh" && Ui.FontCJK != null)
                    button.style.unityFontDefinition =
                        FontDefinition.FromFont(Ui.FontCJK);

                // THE SAME REASONING, A DIFFERENT OBSTACLE: Rubik DOES have
                // the letters of the Arabic language name, but the standard
                // generator does not join them and lays them out backwards.
                // Someone who reads Arabic would see scattered letters on
                // the very button they were looking for. The direction and
                // the generator are turned round for that button alone - the
                // rest of the screen is left as it is.
                if (Loc.Languages[idx] == "ar")
                {
                    button.style.unityTextGenerator = TextGeneratorType.Advanced;
                    button.languageDirection = LanguageDirection.RTL;
                    // AND THE FONT TOO: with the game in Chinese the root
                    // element is drawn with Noto Sans SC, and that font has
                    // NO Arabic. A player who reads Arabic would see their
                    // language as seven empty boxes in a Chinese interface.
                    // Rubik carries Arabic - it is given locally.
                    if (Ui.Font != null)
                        button.style.unityFontDefinition =
                            FontDefinition.FromFont(Ui.Font);
                }

                languages.Add(button);
            }
            col.Add(languages);

            col.Add(Theme.Btn(Loc.T("ui.settings.back"), () => Ui.Pop()));
            root.Add(col);
            ErrorScreen.WrapWidth(col, 400);
            return root;
        }

        /// <summary>
        /// Binds a zero-to-one level to a stepped control. The number of
        /// steps is TEN: finer adjustment on mobile can be neither read nor
        /// caught with a finger.
        /// </summary>
        /// <summary>
        /// The number of volume steps.
        ///
        /// 10 -> 5: MEASURED. Ten cells at 52 dp base width DO NOT FIT a
        /// 480 dp panel and the row wrapped - eight cells on top, two
        /// underneath, and because of flexGrow those two turned into two
        /// empty boxes each half the screen wide. In a screenshot that
        /// reads as a BUG, not as a volume control.
        ///
        /// Five steps both fit and are enough: volume is not something set
        /// to a precision of 10%.
        /// </summary>
        private const int Steps = 5;

        private VisualElement Level(string label, float value, Action<float> onChange)
        {
            VisualElement box = Theme.PanelBox();
            int start = Mathf.Clamp(Mathf.RoundToInt(value * Steps), 0, Steps);

            box.Add(Theme.Level(label, Steps, start, step =>
            {
                onChange(step / (float)Steps);
                PlayerPrefs.Save();
                Ui.Refresh();
            }));
            return box;
        }
    }

    /// <summary>
    /// The open source licences.
    ///
    /// A screen of its own, because the texts THEMSELVES are required: the
    /// OFL asks for the copyright notice, MIT asks for the licence text to
    /// be copied, Apache-2.0 asks for the notice. Writing "Rubik - SIL OFL
    /// 1.1" gives the licence's NAME, not its terms.
    ///
    /// The texts live under Resources, which means they GO INTO THE BUILD.
    /// Found in review: the font's binary data was inside the APK but not
    /// one licence text was.
    /// </summary>
    public sealed class LicenseScreen : UiScreen
    {
        private static readonly string[] Files =
        {
            "licenses/rubik-ofl",
            // A SECOND FONT = A SECOND LICENCE TEXT.
            //
            // Noto Sans SC is the work of a different copyright holder, and
            // the OFL asks for the text to be distributed WITH THE PRODUCT.
            // Saying "there is a Rubik OFL and this is OFL too" does not
            // satisfy the licence - they are two separate notices.
            "licenses/noto-sans-sc-ofl",
            "licenses/kenney-cc0",
            "licenses/engine-components",
        };

        public override VisualElement Build()
        {
            VisualElement root = ErrorScreen.Backdrop();
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = Theme.ReadWidth;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            col.Add(Theme.Title(Loc.T("ui.credits.licenses")));

            foreach (string f in Files)
            {
                TextAsset t = Resources.Load<TextAsset>(f);
                if (t == null) continue;

                VisualElement box = Theme.PanelBox();
                Label body = Theme.Text(t.text, Theme.FontSmall, Theme.InkDim);
                body.style.whiteSpace = WhiteSpace.Normal;
                box.Add(body);
                col.Add(box);
                Resources.UnloadAsset(t);
            }

            col.Add(Theme.Btn(Loc.T("ui.settings.back"), () => Ui.Pop()));
            root.Add(col);
            return root;
        }
    }

    /// <summary>
    /// The credits. Attribution is not REQUIRED, but it is written anyway -
    /// not thanking people for CC0 assets would be cheap
    /// (vendor/ATTRIBUTION.md).
    /// </summary>
    public sealed class CreditsScreen : UiScreen
    {
        public override VisualElement Build()
        {
            VisualElement root = ErrorScreen.Backdrop();
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 560;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            col.Add(Theme.Title(Loc.T("ui.menu.credits")));

            VisualElement box = Theme.PanelBox();
            box.Add(Theme.Text(Loc.T("ui.credits.design"), Theme.FontSmall, Theme.InkFaint));
            box.Add(Theme.Text(Loc.T("ui.credits.author"), Theme.FontBody));
            box.Add(Theme.Divider());
            box.Add(Theme.Text(Loc.T("ui.credits.models"), Theme.FontSmall, Theme.InkFaint));
            box.Add(Theme.Text(Loc.T("ui.credits.models_by"), Theme.FontBody));
            box.Add(Theme.Divider());
            box.Add(Theme.Text(Loc.T("ui.credits.audio"), Theme.FontSmall, Theme.InkFaint));
            box.Add(Theme.Text(Loc.T("ui.credits.audio_by"), Theme.FontBody));
            box.Add(Theme.Divider());
            box.Add(Theme.Text(Loc.T("ui.credits.font"), Theme.FontSmall, Theme.InkFaint));
            box.Add(Theme.Text(Loc.T("ui.credits.font_by"), Theme.FontBody));
            box.Add(Theme.Text(Loc.T("ui.credits.font_copyright"),
                               Theme.FontSmall, Theme.InkFaint));
            col.Add(box);

            col.Add(Theme.Btn(Loc.T("ui.credits.licenses"),
                              () => Ui.Push(new LicenseScreen())));
            col.Add(Theme.Btn(Loc.T("ui.hud.back"), () => Ui.Pop()));
            root.Add(col);
            return root;
        }
    }
}
