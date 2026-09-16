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
        internal static VisualElement Backdrop()
        {
            ScrollView v = Theme.Mobile(new ScrollView(ScrollViewMode.Vertical));
            v.style.flexGrow = 1;
            v.style.minHeight = 0;
            v.style.backgroundColor = Theme.Bg;
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
            VisualElement root = ErrorScreen.Backdrop();

            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 420;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            Label title = Theme.Text(Loc.T("ui.game.title"), Theme.FontHuge, Theme.Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = Theme.Pad;
            col.Add(title);

            Label sub = Theme.Text(Loc.T("ui.menu.tagline"), Theme.FontBody, Theme.InkDim);
            sub.style.unityTextAlign = TextAnchor.MiddleCenter;
            sub.style.marginBottom = Theme.Pad * 2;
            col.Add(sub);

            if (AnySave())
                col.Add(Theme.Btn(Loc.T("ui.menu.continue"),
                    () => Ui.Push(new SlotScreen(SlotScreen.Mode.Load)), primary: true));

            col.Add(Theme.Btn(Loc.T("ui.menu.new"),
                () => Ui.Push(new CuisineScreen())));
            col.Add(Theme.Btn(Loc.T("ui.menu.settings"),
                () => Ui.Push(new SettingsScreen())));
            col.Add(Theme.Btn(Loc.T("ui.menu.credits"),
                () => Ui.Push(new CreditsScreen())));

#if UNITY_STANDALONE || UNITY_EDITOR
            col.Add(Theme.Btn(Loc.T("ui.menu.quit"), Quit));
#endif

            root.Add(col);
            return root;
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
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 560;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            col.Add(Theme.Title(Loc.T("ui.cuisine.pick")));

            col.Add(Card("fastfood", Loc.T("cuisine.fastfood"),
                Loc.T("ui.cuisine.combo"),
                Loc.T("ui.cuisine.fastfood_desc")));
            col.Add(Card("turk", Loc.T("cuisine.turk"),
                Loc.T("ui.cuisine.credit"),
                Loc.T("ui.cuisine.turk_desc")));

            col.Add(Theme.Btn(Loc.T("ui.hud.back"), () => Ui.Pop()));
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
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 560;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            col.Add(Theme.Title(Loc.T("ui.slot.title")));

            for (int i = 0; i < SaveStore.SlotCount; i++) col.Add(Row(i));

            col.Add(Theme.Btn(Loc.T("ui.hud.back"), () => Ui.Pop()));
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
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 480;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

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
