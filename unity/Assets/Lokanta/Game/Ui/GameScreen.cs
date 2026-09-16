using Lokanta.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// The game's main screen: a top strip plus a bottom strip that
    /// follows the phase.
    ///
    /// The middle is left EMPTY on purpose: the 3D hall is there and that
    /// is what the player should be looking at. docs/16 puts the daily tap
    /// budget at 40-60; filling the screen with panels means the player
    /// never looks at the hall at all.
    ///
    /// The top strip updates every frame (Tick); the bottom strip is only
    /// rebuilt when the phase changes. Rebuilding the whole tree every
    /// frame would be pulling the button out from under the finger that is
    /// pressing it.
    /// </summary>
    public sealed class GameScreen : UiScreen
    {
        // _tables / _staff / _flow WERE DELETED.
        //
        // When the top strip was redesigned they were TAKEN OUT of the
        // tree, but the fields stayed: every Tick produced their text (one
        // of them a string.Format plus three int boxings) and then wrote
        // display=None unconditionally. The comment beside them said "other
        // screens read this" - no screen read it.
        //
        // This project's own rule: "a field its call sites lie about gets
        // deleted, not wired up".
        private Label _day, _cash, _rep, _rent;

        /// <summary>The top-left card (morning checklist / service flow).</summary>
        private VisualElement _cards;

        /// <summary>The top-right stat cards (takings, satisfaction).</summary>
        private VisualElement _stats;

        /// <summary>The filled part of the day progress bar.</summary>
        private VisualElement _meterFill;

        private Label _phaseLabel, _revenue, _satisfaction;

        /// <summary>"day 12 / 60" - where the campaign has got to.</summary>
        private Label _season;
        private int _shownRevenueDay = -1;
        private DayPhase _builtCards = (DayPhase)(-1);
        private VisualElement _bottom;

        /// <summary>
        /// The vertical space the strips take up, in dp. The tour MEASURES
        /// this.
        ///
        /// The code's own comment says "the strips together must not go
        /// over 129 dp" and that number was being protected by eye - that
        /// is, not protected at all. Adding a button, lengthening a label
        /// or letting a piece of text wrap quietly grows the strip height
        /// and the hall drops to a third of the screen.
        /// </summary>
        /// <summary>
        /// How many buttons in the bottom strip have their TEXT CLIPPED.
        ///
        /// My first version measured "the width the row wants" and it was
        /// measuring THE WRONG THING: because flexible buttons report the
        /// width they have been given, the total always came out equal to
        /// the screen width. So the measurement stayed silent in exactly
        /// the case it was there to catch (an overflowing strip).
        ///
        /// The right question is not "how wide is the row" but "whose
        /// label did not fit": clipping is the VISIBLE symptom of overflow
        /// and can be measured directly.
        ///
        /// For a while the service strip was asking for 1103 dp (the
        /// screen is 873) and six buttons had their text clipped.
        /// </summary>
        public int ClippedButtons
        {
            get
            {
                // THE TOP STRIP AND THE CARDS ARE MEASURED TOO.
                //
                // The measurement only walked inside `_bottom` and returned
                // ZERO when `_bottom == null` - so "could not measure" and
                // "clean" gave the same answer. The pills in the top strip
                // are where two-language clipping is most likely ("Rent in
                // {0} days") and the left card is a FIXED 190 dp wide;
                // neither was being measured.
                if (_bottom == null && _top == null) return -1;

                _clipDetail = null;
                int n = 0;
                n += ClippedCount(_bottom);
                n += ClippedCount(_top);
                n += ClippedCount(_cards);
                n += ClippedCount(_stats);
                return n;
            }
        }

        /// <summary>
        /// How many elements in the bottom strip spill OFF THE SCREEN.
        ///
        /// A THIRD FORM OF FAILURE. The strip height is measured, clipped
        /// text is measured - and neither of them sees HORIZONTAL
        /// OVERFLOW. Unlike CSS, UI Toolkit's default for `flex-shrink` is
        /// ZERO: when the row does not fit nothing shrinks, the last
        /// element spills out and lands ON TOP of the one before it.
        ///
        /// That is exactly what happened in Turkish cuisine - the tab
        /// button landed on top of the "Attention" button - and both
        /// measurements stayed green: the height was right and no TEXT was
        /// clipped. Only a render taken at store resolution showed the
        /// fault.
        /// </summary>
        /// <summary>The clearest overlap in the last measurement; for diagnosis.</summary>
        public string OverflowDetail { get { return _overlapDetail; } }

        private string _overlapDetail;

        /// <summary>
        /// Are two BUTTONS sitting on top of each other.
        ///
        /// Why buttons only: this measurement measured the wrong thing
        /// twice. First it looked at `_bottom`'s DIRECT children - there is
        /// a single row container there, no sibling to compare against, so
        /// the answer was always zero. Then it compared every element, and
        /// this time it counted DELIBERATE overlaps: `Kit.Cta`'s double
        /// arrow overlaps its two triangles by 6 dp on purpose, and the
        /// icons put shape on shape inside themselves.
        ///
        /// What is wanted is none of those: A BUTTON THE PLAYER CANNOT
        /// TOUCH. Two buttons overlapping is always a fault - there is no
        /// telling which one you pressed. Two triangles overlapping is an
        /// icon.
        ///
        /// The measurement's name now says what it measures.
        /// </summary>
        public int OverlappingButtons
        {
            get
            {
                if (_bottom == null) return -1;
                if (float.IsNaN(_bottom.worldBound.width)) return -1;

                _overlapDetail = null;
                var d = new System.Collections.Generic.List<Button>();
                foreach (Button b in _bottom.Query<Button>().ToList())
                {
                    if (b.resolvedStyle.display == DisplayStyle.None) continue;
                    Rect r = b.worldBound;
                    if (float.IsNaN(r.width) || r.width <= 0f) continue;
                    // There are no nested buttons, but if there were, one
                    // should not count as its own sibling.
                    if (b.GetFirstAncestorOfType<Button>() != null) continue;
                    d.Add(b);
                }

                int n = 0;
                for (int i = 0; i < d.Count; i++)
                    for (int j = i + 1; j < d.Count; j++)
                    {
                        Rect a = d[i].worldBound, b2 = d[j].worldBound;
                        float overlapX = Mathf.Min(a.xMax, b2.xMax) - Mathf.Max(a.xMin, b2.xMin);
                        float overlapY = Mathf.Min(a.yMax, b2.yMax) - Mathf.Max(a.yMin, b2.yMin);
                        if (overlapX <= 0.5f || overlapY <= 0.5f) continue;

                        n++;
                        if (_overlapDetail == null)
                            _overlapDetail = NameOf(d[i]) + " x " + NameOf(d[j])
                                             + " (" + overlapX.ToString("0") + " dp)";
                    }
                return n;
            }
        }

        /// <summary>A name that reads in a diagnostic: the name first, then the text.</summary>
        private static string NameOf(VisualElement v)
        {
            if (!string.IsNullOrEmpty(v.name)) return v.name;
            Button b = v as Button;
            if (b != null && !string.IsNullOrEmpty(b.text)) return b.text;
            Label l = v as Label;
            if (l != null && !string.IsNullOrEmpty(l.text)) return l.text;
            foreach (Label inner in v.Query<Label>().ToList())
                if (!string.IsNullOrEmpty(inner.text)) return inner.text;
            return v.GetType().Name;
        }

        /// <summary>The text of the first element clipped in the last measurement; for diagnosis.</summary>
        public string ClipDetail { get { return _clipDetail; } }

        private static string _clipDetail;

        /// <summary>How many elements under a root have their text clipped.</summary>
        private static int ClippedCount(VisualElement root)
        {
            if (root == null) return 0;
            int n = 0;

            // BUTTONS FIRST: so that a button's inner label is not counted
            // twice, the label sweep skips anything under a button.
            foreach (Button b in root.Query<Button>().ToList())
            {
                    if (!string.IsNullOrEmpty(b.text))
                    {
                        if (Clipped(b, b.text)) { n++; RecordClipped(b.text); }
                        continue;
                    }

                    // THE TEXT NOW LIVES IN A LABEL INSIDE THE BUTTON.
                    //
                    // In the new interface an icon button's text field is
                    // empty and the writing sits in a child label. The old
                    // measurement SKIPPED those as "has no text" - so the
                    // buttons closest to being clipped (narrow, in two
                    // languages, one line under an icon) were exactly the
                    // ones left out of the measurement.
                    foreach (Label l in b.Query<Label>().ToList())
                        if (!string.IsNullOrEmpty(l.text) && Clipped(l, l.text))
                        {
                            n++;
                            RecordClipped(l.text);
                            break;
                        }
            }

            // THEN THE FREE LABELS: pills, card headings, status rows.
            // Anything under a button was counted above.
            foreach (Label l in root.Query<Label>().ToList())
            {
                if (string.IsNullOrEmpty(l.text)) continue;
                if (l.GetFirstAncestorOfType<Button>() != null) continue;
                if (Clipped(l, l.text)) { n++; RecordClipped(l.text); }
            }
            return n;
        }

        /// <summary>
        /// Records the clipped text into the diagnostic.
        ///
        /// On its own the count says "three buttons were clipped" and does
        /// not say which - that is, it leaves the fix to GUESSWORK. In this
        /// session the overlapping buttons were counted anonymously at
        /// first too, and could only be fixed once their names were
        /// printed.
        /// </summary>
        private static void RecordClipped(string text)
        {
            if (_clipDetail == null) _clipDetail = text;
            else if (!_clipDetail.Contains(text))
                _clipDetail += " | " + text;
        }

        /// <summary>Does an element's text fit inside its own width.</summary>
        /// <summary>
        /// Does the element itself, or one of its ancestors, clip the
        /// overflow.
        ///
        /// If nothing clips, the text spills but is READABLE; the problem
        /// is then not truncation but a collision with its neighbour - and
        /// a separate measurement looks for that.
        /// </summary>
        private static bool InsideClippingBox(VisualElement v)
        {
            // Read from THE INLINE STYLE: `resolvedStyle` does not carry
            // `overflow` in this version of Unity (it is not on
            // IResolvedStyle). In this project the styles are given inline
            // in C# anyway, so that is the right source.
            for (VisualElement e = v; e != null; e = e.parent)
            {
                StyleEnum<Overflow> o = e.style.overflow;
                if (o.keyword == StyleKeyword.Undefined
                    && o.value == Overflow.Hidden) return true;
            }
            return false;
        }

        private static bool Clipped(TextElement v, string text)
        {
            // TRUNCATION ONLY HAPPENS INSIDE A CLIPPING BOX.
            //
            // UI Toolkit's default for `overflow` is VISIBLE: text carries
            // on being drawn past its box rather than being cut. This
            // measurement assumed "went past = was cut", and in this
            // interface Overflow.Hidden exists in exactly TWO places - an
            // icon box and a progress bar; on no text label at all.
            //
            // So the measurement was looking for a form of failure that
            // DOES NOT EXIST in this interface, and could only produce
            // false alarms. The first time the fast food tour was run it
            // lit three labels red at once; all three read in full on
            // screen.
            //
            // The real protection is in the two neighbouring measurements:
            // overlapping buttons (THAT one caught the real fault in
            // Turkish cuisine) and elements off the edge of the screen.
            // This one now only measures inside a box that REALLY clips -
            // silent today, but it will speak on the day someone puts
            // Overflow.Hidden on a text container.
            if (!InsideClippingBox(v)) return false;

            float have = v.resolvedStyle.width
                         - v.resolvedStyle.paddingLeft
                         - v.resolvedStyle.paddingRight;
            if (float.IsNaN(have) || have <= 0f) return false;

            float want = v.MeasureTextSize(
                text, 0f, VisualElement.MeasureMode.Undefined,
                0f, VisualElement.MeasureMode.Undefined).x;

            // Half a pixel of slack: a rounding difference between the
            // measurement and the layout should not count as clipping.
            if (want <= have + 0.5f) return false;

            // THE WIDTH WAS EXCEEDED - BUT ON A WRAPPING LABEL THAT IS NOT
            // CLIPPING.
            //
            // Wrapping is ON by default in UI Toolkit and this measurement
            // ALWAYS assumed one line. It was caught the first time the
            // fast food tour was run: "Combo off" wraps to two lines, reads
            // IN FULL on screen, and the check said "clipped text: 2" and
            // failed the tour. Because the Turkish labels happened to fit on
            // one line, the false alarm had never gone off before.
            //
            // The test: is the element TWO LINES tall. If it wrapped, the
            // text flowed downwards and is readable; if it did not, it
            // really is being cut.
            //
            // I first wrote this as "measure a wrapping label by its
            // height" and it was WORSE - because wrapping is on by default
            // every label fell down that branch and seven false alarms came
            // out ("x4", "8,000" and other obviously fitting text). The
            // narrow fix is the right fix.
            float lineHeight = v.MeasureTextSize(
                "X", 0f, VisualElement.MeasureMode.Undefined,
                0f, VisualElement.MeasureMode.Undefined).y;
            float elementHeight = v.resolvedStyle.height
                                  - v.resolvedStyle.paddingTop
                                  - v.resolvedStyle.paddingBottom;
            if (lineHeight > 0f && !float.IsNaN(elementHeight)
                && elementHeight >= lineHeight * 1.8f)
                return false;

            return true;
        }

        public float StripHeight
        {
            get
            {
                float t = _top != null ? _top.resolvedStyle.height : 0f;
                float b = _bottom != null ? _bottom.resolvedStyle.height : 0f;
                if (float.IsNaN(t)) t = 0f;
                if (float.IsNaN(b)) b = 0f;
                return t + b;
            }
        }
        private DayPhase _builtPhase = (DayPhase)(-1);
        private VisualElement _toast;
        private VisualElement _top;

        public override VisualElement Build()
        {
            // THE CACHE IS RESET.
            //
            // Build() RECREATES the labels - with empty text. But the "last
            // written value" fields kept their old values, so Tick() said
            // "unchanged" and wrote none of them.
            //
            // The result: after every Refresh() the top strip EMPTIES. Day,
            // Till, Reputation and the rent countdown are wiped off the
            // screen; the till comes back when a guest pays, reputation at
            // the end of the day, and the day and the rent not until
            // tomorrow. The rent countdown is the one thing docs/16 says
            // "must be visible at all times".
            //
            // Touching a table to pick an intervention target calls
            // Refresh(), so the top strip was being wiped at the very
            // moment the player first tried the new mechanic.
            _shownDay = _shownRep = -1;
            _shownRepCap = -1;
            _shownRentDays = -1;
            _shownCash = long.MinValue;
            _shownServed = _shownAngry = _shownOccupied = -1;
            _shownPhase = (DayPhase)(-1);

            VisualElement root = new VisualElement();
            root.style.flexGrow = 1;

            // The root ITSELF does not swallow touches. It swallows them on
            // the bars and the buttons, not in the space between: the hall
            // has to be touchable.
            root.pickingMode = PickingMode.Ignore;

            _top = TopBar();
            root.Add(_top);

            // The middle: the 3D scene. The interface draws nothing here,
            // but it MUST NOT BLOCK touches either - the camera has to be
            // able to come in close on a room.
            VisualElement middle = new VisualElement();
            middle.style.flexGrow = 1;
            middle.style.flexDirection = Theme.RowFlow;
            middle.style.justifyContent = Justify.SpaceBetween;
            middle.style.paddingLeft = Theme.Pad;
            middle.style.paddingRight = Theme.Pad;
            middle.pickingMode = PickingMode.Ignore;

            // THE CARDS FLOAT OVER THE HALL.
            //
            // A CARD, not a strip: a strip eats the screen's whole width
            // and height, a card only the corner it sits in. The top
            // corners are EMPTY anyway - the building stands in the middle
            // of the screen with sky colour above it (docs/19, no sky dome).
            // The reference's layout is exactly this too.
            //
            // The cards themselves swallow touches (pressing on one must
            // not touch the room behind) but the space BETWEEN them does
            // not: middle itself is Ignore.
            _cards = new VisualElement();
            _cards.style.width = 190;
            _cards.style.marginTop = Theme.Gap;
            middle.Add(_cards);

            _stats = new VisualElement();
            _stats.style.marginTop = Theme.Gap;
            _stats.style.alignItems = Align.FlexEnd;
            middle.Add(_stats);

            root.Add(middle);

            _bottom = new VisualElement();
            root.Add(_bottom);
            BuildBottom();

            // The notice strip is added AFTER the bottom bar, so it is
            // drawn ON TOP of it; and its height is not a fixed number but
            // the bar's MEASURED height.
            //
            // It used to be added before the bar with "bottom: 96" written
            // out; the bar was 134 dp tall, so 85% of every notice written
            // ended up behind it. The game's only visual feedback was
            // invisible: the player pressed a button, assumed nothing had
            // happened, pressed again and burned their allowance.
            _toast = new VisualElement();
            _toast.style.position = Position.Absolute;
            _toast.style.left = 0;
            _toast.style.right = 0;
            _toast.style.alignItems = Align.Center;
            _toast.pickingMode = PickingMode.Ignore;
            root.Add(_toast);

            // The camera frames BETWEEN the bars. The heights change with
            // the content (the intervention row, the allowance left), so no
            // fixed number is written down - it is measured and reported.
            // BOTH the root AND THE BOTTOM BAR are watched.
            //
            // Watching only the root left the notice strip in the wrong
            // place: when the phase changes the bottom bar goes from one row
            // to two, but the root's measurements do not change, so the
            // event never fired and the strip stayed behind the bar.
            root.RegisterCallback<GeometryChangedEvent>(_ => ReportSafeArea(root));
            _bottom.RegisterCallback<GeometryChangedEvent>(_ => ReportSafeArea(root));

            // The notice area NOW EXISTS: the hint strip is built here.
            // BuildBottom was called above and _toast did not exist then.
            RebuildNotices();

            // The cards LAST: during Build() _cards and _stats were newly
            // created and are empty. Tick writes the values on the first
            // frame.
            _shownProgress = -1f;
            _shownMeterPhase = (DayPhase)(-1);
            BuildCards();

            return root;
        }

        private void ReportSafeArea(VisualElement root)
        {
            float h = root.resolvedStyle.height;
            if (h <= 1f) return;

            float top = _top != null ? _top.resolvedStyle.height : 0f;
            float bottom = _bottom != null ? _bottom.resolvedStyle.height : 0f;

            // Let the notice strip sit just above the bar.
            if (_toast != null) _toast.style.bottom = bottom + Theme.Gap;

            if (App != null && App.Rig != null) App.Rig.SetSafeArea(top / h, bottom / h);
        }

        // =====================================================================
        /// <summary>
        /// THE TOP STRIP: BADGE, BAR, PILLS.
        ///
        /// It used to be a full-width dark strip with seven separate pieces
        /// of text lined up inside it ("Day 1  Till 11,963  Reputation 24.0
        /// / 55  Rent in 6 days - 2,546  Evening  Tables 4  Crew 2"). They
        /// were all the same size, the same colour and looked equally
        /// important - so none of them stood out.
        ///
        /// The new layout splits into three groups and THE GROUPS
        /// THEMSELVES are information:
        ///
        ///   LEFT      - the day badge and the day's progress. "Where am I?"
        ///   RIGHT     - till and reputation: the game's two resources, in
        ///               pills.
        ///   FAR RIGHT - the menu.
        ///
        /// The strip's background IS GONE: the pills float directly over
        /// the hall. The space that wins goes to the hall.
        /// </summary>
        private VisualElement TopBar()
        {
            VisualElement bar = new VisualElement();
            bar.style.paddingLeft = Theme.Pad;
            bar.style.paddingRight = Theme.Pad;
            bar.style.paddingTop = Theme.Gap;
            bar.style.paddingBottom = 4;
            bar.pickingMode = PickingMode.Ignore;

            VisualElement row = Theme.Row(Theme.Gap);
            row.style.alignItems = Align.Center;
            row.pickingMode = PickingMode.Ignore;

            // --- LEFT: the day badge and the day's progress -------------
            _day = Theme.Text("", Theme.FontBody, Theme.Ink);
            row.Add(Kit.Badge(_day));

            VisualElement dayGroup = new VisualElement();
            dayGroup.style.marginLeft = 8;
            dayGroup.pickingMode = PickingMode.Ignore;

            // THE PHASE IS NOW A LABEL, NOT A PIECE OF TEXT THAT GETS LOST.
            //
            // The day's phase is the state that determines more than any
            // other: which buttons work, whether guests arrive, the colour
            // of the light. It was a small grey piece of text on the right
            // of the top strip.
            _phaseLabel = Theme.Text("", Theme.FontSmall, Theme.Ink);
            _phaseLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            // THE CAMPAIGN'S GOAL IS NOW ON THE SCREEN.
            //
            // At the end of sixty days you are scored on seven axes, and
            // that was written NOWHERE until day 61: the player ran a shop
            // with no closing date and was met at the end with a report card
            // they had never heard of. It was not a goal, it was a surprise.
            //
            // It sits BESIDE the phase, not on a line of its own: the top
            // strip is looked at constantly and one more line would steal
            // space from the hall.
            VisualElement phaseRow = Theme.Row(6);
            phaseRow.style.alignItems = Align.FlexEnd;
            phaseRow.style.marginBottom = 3;
            phaseRow.pickingMode = PickingMode.Ignore;
            phaseRow.Add(_phaseLabel);

            _season = Theme.Text("", Theme.FontSmall, Theme.InkFaint);
            phaseRow.Add(_season);
            dayGroup.Add(phaseRow);

            VisualElement track = Kit.Meter(out _meterFill, 138f, 9f);
            dayGroup.Add(track);
            row.Add(dayGroup);

            // THE RENT COUNTDOWN. docs/02 calls the weekly rent "the
            // metronome of the pressure", and that metronome was not on the
            // screen: the money leaves the till on the seventh day and the
            // player only noticed after the figure had dropped.
            _rent = Theme.Text("", Theme.FontSmall, Theme.InkDim);
            _rent.style.marginLeft = Theme.Pad;
            row.Add(_rent);

            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            spacer.pickingMode = PickingMode.Ignore;
            row.Add(spacer);

            // --- RIGHT: two resources, two pills ------------------------
            //
            // The table and crew counts HAVE GONE FROM HERE. Both are
            // morning decisions and both are already written on their own
            // screens; the top strip is looked at constantly and only things
            // that CHANGE OFTEN belong there. The fields remain (Tick writes
            // them), they just take up no room on the screen.
            _cash = Theme.Text("", Theme.FontBody);
            VisualElement till = Kit.Pill(Icons.Coin(22f), _cash);
            if (App.Sim.HasLoan || App.Sim.Cash < App.Sim.WeeklyFixedCost() * 2)
                till.Add(Kit.PillAction(() => Ui.Push(new LoanScreen()),
                                        Loc.T("ui.morning.loan")));
            row.Add(till);

            _rep = Theme.Text("", Theme.FontBody);
            row.Add(Kit.Pill(Icons.Gem(22f), _rep));

            Button menu = new Button(() => { Sfx.Click(); Ui.Push(new PauseScreen()); });
            menu.text = string.Empty;
            // A button with no text needs a NAME and a description: both the
            // screen reader and the self-driving tour look at text.
            menu.name = "menu";
            menu.tooltip = Loc.T("ui.hud.menu");
            menu.Add(Icons.Gear(Theme.Ink, 22f));
            menu.style.alignItems = Align.Center;
            menu.style.justifyContent = Justify.Center;
            // A FULL touch target, and away from the edge.
            //
            // It used to be 44 dp tall and 16 dp from the right edge: the
            // smallest target on the screen, in the furthest corner, in the
            // place the notch falls. This button is the only way out of the
            // game.
            // THE MENU BUTTON IS BIGGER THAN A PILL: it is the only way out
            // of the game and its touch target must not go under 48 dp.
            menu.style.minHeight = Theme.Touch - 4;
            menu.style.height = Theme.Touch - 4;
            menu.style.width = Theme.Touch;
            menu.style.marginLeft = Theme.Gap;
            menu.style.marginRight = 0;
            menu.style.marginTop = 0;
            menu.style.marginBottom = 0;
            menu.style.paddingLeft = 0;
            menu.style.paddingRight = 0;
            menu.style.backgroundColor = Kit.CardBg;
            menu.style.borderTopWidth = 1;
            menu.style.borderBottomWidth = 1;
            menu.style.borderLeftWidth = 1;
            menu.style.borderRightWidth = 1;
            menu.style.borderTopColor = Kit.CardLine;
            menu.style.borderBottomColor = Kit.CardLine;
            menu.style.borderLeftColor = Kit.CardLine;
            menu.style.borderRightColor = Kit.CardLine;
            Theme.Round(menu, (Theme.Touch - 4) * 0.5f);
            Theme.PressFx(menu, Kit.CardBg);
            row.Add(menu);

            bar.Add(row);
            return bar;
        }

        // =====================================================================
        /// <summary>
        /// THE SIDE CARDS: the day's list on the left, two stat cards on
        /// the right.
        ///
        /// The equivalent of the reference's "Daily Goals" panel and its
        /// "Hourly Revenue / Customer Satisfaction" cards. Neither is MADE
        /// UP - both were already in the game but could not be seen:
        ///
        ///   - The morning's readiness summary (menu, stock, cook) was a
        ///     single line of 14 dp grey text in the bottom strip; and yet
        ///     it was THE one thing to read before opening service.
        ///   - Takings and satisfaction appeared ONLY in the evening
        ///     report; the player could not see how the day was going while
        ///     it went.
        /// </summary>
        private void BuildCards()
        {
            if (_cards == null || _stats == null || App.Sim == null) return;
            _builtCards = App.Sim.Phase;
            _cards.Clear();
            _stats.Clear();
            _servedValue = _angryValue = _occupiedValue = _turnedValue = null;
            _revenue = _satisfaction = null;

            if (_builtCards == DayPhase.Morning)
            {
                VisualElement body;
                VisualElement card = Kit.Card(Loc.T("ui.morning.checklist"), out body);
                int dishes, coverageBp, cooks;
                Readiness(out dishes, out coverageBp, out cooks);
                body.Add(Kit.CheckRow(dishes > 0,
                    Loc.T("ui.morning.ready_menu", dishes)));
                // A NUMBER IF SOMETHING IS SHORT, A PLAIN SENTENCE IF NOT.
                //
                // The first version wrote a head count in both cases
                // ("Stock is enough for today (~13 people)") and the tour
                // caught it as CLIPPED - in two languages at once. Next to
                // a green tick the number carries no information anyway;
                // where it does carry some is the SHORT case, and there it
                // writes PEOPLE rather than a percentage: "8 / 13" says
                // directly how much stock is missing.
                bool stockFull = coverageBp >= Lokanta.Core.Fx.One;
                int expected = App.Sim.ExpectedPeopleToday();
                body.Add(Kit.CheckRow(stockFull,
                    stockFull
                        ? Loc.T("ui.morning.ready_stock_ok")
                        : Loc.T("ui.morning.ready_stock_part",
                                (int)Lokanta.Core.Fx.Bp(expected, coverageBp),
                                expected)));
                body.Add(Kit.CheckRow(cooks > 0,
                    Loc.T("ui.morning.ready_cooks", cooks)));
                _cards.Add(card);
            }
            else if (_builtCards == DayPhase.Service)
            {
                VisualElement body;
                VisualElement card = Kit.Card(Loc.T("ui.hud.today"), out body);

                _shownServed = _shownAngry = _shownOccupied = -1;
                // THE COLOUR NOW COMES FROM Kit.CountRow.
                //
                // This passed `Theme.Ink` and the card's background is
                // `Theme.Plate`: a contrast of 1.08:1 - all three numbers
                // were white on cream. The signature that left the colour
                // choice to the caller was the fault itself.
                _servedValue = Theme.Text("", Theme.FontSmall, Theme.PlateInk);
                _angryValue = Theme.Text("", Theme.FontSmall, Theme.PlateInk);
                _occupiedValue = Theme.Text("", Theme.FontSmall, Theme.PlateInk);
                body.Add(Kit.CountRow(Theme.Dot(Kit.GoDeep, 8f),
                                      Loc.T("ui.hud.served"), _servedValue));
                // THE NUMBER IS THE TOTAL, so its name is the total's name.
                body.Add(Kit.CountRow(Theme.Dot(Kit.BadDeep, 8f),
                                      Loc.T("ui.evening.lost"), _angryValue));

                // THOSE TURNED AWAY AT THE DOOR ARE NOW VISIBLE DURING
                // SERVICE TOO.
                //
                // This is the commonest way to die in the first week: stock
                // or menu falls short and the guest does not even come in.
                // The number was only in the day's report, so the player saw
                // it once the day had ended - when it was impossible to fix.
                _turnedValue = Theme.Text("", Theme.FontSmall, Theme.PlateInk);
                body.Add(Kit.CountRow(Theme.Dot(Theme.Warn, 8f),
                                      Loc.T("ui.evening.turned_away"), _turnedValue));
                body.Add(Kit.CountRow(Theme.Dot(Theme.Accent, 8f),
                                      Loc.T("ui.hud.tables"), _occupiedValue));
                _cards.Add(card);
            }

            // NO STAT CARDS IN THE MORNING: takings zero, satisfaction
            // zero. A card showing zero is noise, not information.
            if (_builtCards != DayPhase.Morning)
            {
                _revenue = Theme.Text("", 21, Theme.Ink);
                VisualElement revenueCard = Kit.Stat(Icons.Coin(20f),
                                                     Loc.T("ui.evening.revenue"), _revenue);
                revenueCard.style.marginBottom = Theme.Gap;
                _stats.Add(revenueCard);

                _satisfaction = Theme.Text("", 21, Theme.Ink);
                _stats.Add(Kit.Stat(Icons.People(Theme.InkDim, 20f),
                                    Loc.T("ui.hud.satisfaction"), _satisfaction));
                _shownRevenueDay = -1;
            }
        }

        private Label _servedValue, _angryValue, _occupiedValue, _turnedValue;

        // =====================================================================
        /// <summary>
        /// THE BOTTOM STRIP: ICON BUTTONS ON THE LEFT, ONE GREEN ACTION ON
        /// THE RIGHT.
        ///
        /// The old strip was a full-width dark box with seven equivalent
        /// grey buttons lined up inside it: "Market, Menu, Crew, Equipment,
        /// Loan, Open Service". All the same colour, the same size, the same
        /// weight - so nothing answered the player's question of "what
        /// should I do first".
        ///
        /// The reference's layout answers that question with THE LAYOUT
        /// ITSELF: the left side is PLACES YOU GO INTO (four screens, icon
        /// plus label), the right side is the day's ONE MOVE-ON action -
        /// large, green and alone.
        ///
        /// The strip's background is gone; the buttons sit directly over the
        /// hall and the hall shows through the gaps between them.
        /// </summary>
        private void BuildBottom()
        {
            _bottom.Clear();
            _builtPhase = App.Sim.Phase;
            _builtServiceDone = _builtPhase == DayPhase.Service
                                && App.Sim.ServiceComplete;

            VisualElement bar = new VisualElement();
            bar.style.paddingLeft = Theme.Pad;
            bar.style.paddingRight = Theme.Pad;
            bar.style.paddingTop = 6;
            bar.style.paddingBottom = Theme.Gap;
            bar.pickingMode = PickingMode.Ignore;

            // THE CRISIS STRIP: only WHEN THERE IS a crisis.
            //
            // The game's one promise is "during service you only step in on
            // a crisis", and the only signal for a crisis was a ~50x8 dp
            // badge above a table turning red. The strip is NOT BUILT AT ALL
            // when there is no crisis: its appearing is itself the signal.
            if (_builtPhase == DayPhase.Service)
            {
                VisualElement crisis = CrisisBar();
                if (crisis != null) bar.Add(crisis);
            }

            VisualElement row = Theme.Row(Theme.Gap);
            row.style.alignItems = Align.FlexEnd;
            row.pickingMode = PickingMode.Ignore;

            switch (_builtPhase)
            {
                case DayPhase.Morning: MorningBar(row); break;
                case DayPhase.Service: ServiceBar(row); break;
                default: EveningBar(row); break;
            }

            bar.Add(row);
            _bottom.Add(bar);
        }

        /// <summary>The flexible gap separating the left group from the action on the right.</summary>
        private static void Spacer(VisualElement row)
        {
            VisualElement v = new VisualElement();
            v.style.flexGrow = 1;
            v.pickingMode = PickingMode.Ignore;
            row.Add(v);
        }

        /// <summary>
        /// MORNING: four screens on the left, opening service on the right.
        ///
        /// The four screens (Market, Menu, Crew, Equipment) are all of the
        /// morning's decisions; each carries an ICON and a label. The icon
        /// alone will not do - the player cannot tell whether a trolley icon
        /// means "Market" or "Shop"; text alone will not do either, because
        /// four grey rectangles can only be told apart by reading them.
        ///
        /// THE LOAN IS NO LONGER HERE: the green "+" beside the till pill
        /// opens it - the money button where the money is needed. The strip
        /// is one button lighter.
        /// </summary>
        private void MorningBar(VisualElement row)
        {
            row.Add(Kit.IconButton(Icons.Cart(Color.white), Loc.T("ui.morning.market"),
                                   () => Ui.Push(new MarketScreen()), blue: true));
            row.Add(Kit.IconButton(Icons.List(Color.white), Loc.T("ui.morning.menu"),
                                   () => Ui.Push(new MenuBoardScreen()), blue: true));
            row.Add(Kit.IconButton(Icons.Hat(Color.white), Loc.T("ui.morning.staff"),
                                   () => Ui.Push(new StaffScreen()), blue: true));
            row.Add(Kit.IconButton(Icons.ArrowUp(Color.white), Loc.T("ui.morning.equipment"),
                                   () => Ui.Push(new EquipmentScreen()), blue: true));
            Spacer(row);

            // Opening service is the only decision with NO WAY BACK.
            //
            // The second line says what will happen; if something is short,
            // it says what. This warning used to appear only as a bubble
            // AFTER the button had been pressed - so the player pressed
            // first and learned afterwards.
            string missing;
            bool ready = ReadyToOpen(out missing);
            row.Add(Kit.Cta(Loc.T("ui.morning.open"),
                            ready ? Loc.T("ui.morning.open_sub", App.Sim.Day) : missing,
                            () =>
            {
                // IF SOMETHING IS SHORT IT ASKS FIRST.
                //
                // The confirmation only appears when something is short:
                // asking "are you sure" every single morning would turn the
                // confirmation into a reflex nobody reads.
                string second;
                if (!ReadyToOpen(out second) && !_openConfirmed)
                {
                    _openConfirmed = true;
                    Toast(second, rejected: true);
                    Sfx.Cancel();
                    BuildBottom();
                    return;
                }
                _openConfirmed = false;
                App.OpenService();
                BuildBottom();
                BuildCards();
            }, ready));
        }

        /// <summary>
        /// THE MORNING'S THREE NUMBERS: how many dishes on the menu, how
        /// many days of stock, how many cooks.
        ///
        /// ReadinessRow used to compute these and turn them straight into
        /// text; once they moved to the checklist card the same numbers were
        /// needed in two places. The calculation in one place, the
        /// presentation in two.
        ///
        /// THE UNLOCKED ONES ARE COUNTED. IsOnMenu can return true for
        /// locked dishes too (the menu toggle and the unlock day are
        /// separate things) and on day one it said "32 dishes on the menu" -
        /// when only six of them can actually be made.
        /// </summary>
        private void Readiness(out int onMenu, out int coverageBp, out int cooks)
        {
            Simulation sim = App.Sim;
            onMenu = 0;
            for (int i = 0; i < sim.DishCount; i++)
                if (sim.IsOnMenu(i) && sim.IsUnlocked(i)) onMenu++;

            // THE STOCK ROW NOW MEASURES THE DAY, NOT THE DISH COUNT.
            //
            // It used to count "how many dishes can be made" and the tick
            // went green at >= 1: a player with ONE portion each of six
            // dishes looked "ready", opened service and ran out in the first
            // ten minutes. The tick was not measuring what it was for.
            coverageBp = sim.StockCoverageBp();
            cooks = sim.Cooks;
        }

        /// <summary>Are we ready to open service. If not, the button asks.</summary>
        private bool ReadyToOpen(out string missing)
        {
            Simulation sim = App.Sim;
            missing = null;

            int onMenu = 0;
            for (int i = 0; i < sim.DishCount; i++)
                if (sim.IsOnMenu(i) && sim.IsUnlocked(i)) onMenu++;

            if (onMenu == 0) missing = Loc.T("ui.morning.ready_warn_menu");
            else if (sim.Cooks <= 0) missing = Loc.T("ui.morning.ready_warn_cook");
            else if (sim.MakeableDishCount() < 1) missing = Loc.T("ui.morning.ready_warn_stock");

            return missing == null;
        }

        // =====================================================================
        /// <summary>
        /// Where an intervention goes: the SELECTED table, or failing that
        /// the one with the least patience left.
        ///
        /// There used to be no selection at all and the game always chose
        /// the target. So during service the player's only decision was "now
        /// or later" - the game answered the WHO, and the whole mechanic of
        /// being the owner had come down to a timing button.
        ///
        /// Choosing is NOT compulsory: for a player who does not zoom in,
        /// the old behaviour is unchanged, so the game does not punish
        /// someone who makes no choice. Someone who does choose can pick NOT
        /// the least patient but the one that will earn the most - the table
        /// with a little more patience left but more people on it, for
        /// instance.
        /// </summary>
        private int Target()
        {
            // The TABLE -> PARTY conversion IS ESSENTIAL. Intervene expects
            // a party; ValidSelection returns a table. Without the
            // conversion the tea went to an entirely different table.
            int sel = App.ValidSelection();
            if (sel < 0) return App.Sim.MostImpatientParty();

            int party = App.Sim.PartyAtTable(sel);
            return party >= 0 ? party : App.Sim.MostImpatientParty();
        }

        /// <summary>
        /// Works out FROM THE ALLOWANCE COUNT whether the intervention
        /// actually happened, and says so if it did not.
        ///
        /// All three buttons used to send the command and then say "done"
        /// UNCONDITIONALLY. The core meanwhile rejects it silently in four
        /// separate places: the allowance is gone, the party has left, there
        /// is no money in the till for the tea, there is no work at the
        /// station. The player read "tea was offered", the "Allowance 4" on
        /// the right never moved, and there was nowhere to learn what had
        /// gone wrong.
        /// </summary>
        private bool DidIntervene(int before)
        {
            if (App.Sim.InterventionsLeft < before) return true;
            Toast(Loc.T("notice.rejected"), rejected: true);
            Sfx.Cancel();
            return false;
        }

        /// <summary>
        /// The button's text says which table when there is a selection. The
        /// player should see what they are pressing ON the button - not in a
        /// notice afterwards.
        /// </summary>
        /// <summary>
        /// The interventions' CURRENT TARGET - on a badge of its own rather
        /// than in the button text.
        ///
        /// For a while the target was appended to both buttons' text ("Tea >
        /// most impatient", "Attention > most impatient"). That cost two
        /// things:
        ///
        ///   1. THE SAME INFORMATION TWICE. Both buttons go to the same
        ///      target.
        ///   2. The string length depended on THE LANGUAGE: English "most
        ///      impatient" is longer than the Turkish. The strip had already
        ///      overflowed once and the suffix had been cut from "most
        ///      impatient" down to a single word - so this was the second
        ///      time we hit the same wall.
        ///
        /// Measured: with the suffix gone, three buttons stopped being
        /// clipped. The target is now on one badge and the buttons say only
        /// the VERB - which is a button's job anyway.
        /// </summary>
        private VisualElement TargetBadge()
        {
            int sel = App.ValidSelection();
            string text = sel >= 0
                ? "› " + (sel + 1)
                : "› " + Loc.T("ui.service.target_auto");

            Label l = Theme.Text(text, Theme.FontSmall, Theme.InkDim);
            l.style.flexShrink = 0;
            l.style.marginLeft = 2;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            return l;
        }

        /// <summary>
        /// The notice writes the TABLE number, not the party index.
        ///
        /// The party index used to be printed as "the table number" and a
        /// four-table shop could produce "tea was offered at table 17".
        /// </summary>
        private string TargetToast(string what)
        {
            // THE TABLE NUMBER WAS GLUED IN WITH A TURKISH ORDINAL SUFFIX.
            //
            // The notice used to be assembled in code: (sel+1) plus a
            // Turkish "at table N" fragment plus "tea was offered". Someone
            // playing in English read exactly that, and even translating the
            // fragment would not work - in English the table number goes at
            // the END of the sentence. The whole sentence is now in the
            // table and the number is a placeholder.
            int sel = App.ValidSelection();
            return sel >= 0
                ? Loc.T("ui.service.done_" + what, sel + 1)
                : Loc.T("ui.service.done_" + what + "_any");
        }

        /// <summary>
        /// Chips for the tables whose patience has fallen to critical. Null
        /// if there is no crisis.
        ///
        /// Touching a chip SELECTS that table - so this is also how a target
        /// gets picked. Previously picking a target meant zooming into the
        /// hall first and then touching a table; that was a two-step gesture
        /// that had to be taught, and a player in the middle of a crisis was
        /// not doing it.
        /// </summary>
        private VisualElement CrisisBar()
        {
            Simulation sim = App.Sim;
            int threshold = sim.PatienceWarnBp;

            // Start from the least patient: the chips' order is information
            // too.
            var critical = new System.Collections.Generic.List<int>();
            for (int t = 0; t < sim.TableCount; t++)
            {
                CustomerStage st = sim.TableStage(t);
                if (st == CustomerStage.None || st == CustomerStage.Done
                    || st == CustomerStage.LeftAngry) continue;
                int bp = sim.TablePatienceBp(t);
                if (bp > threshold || bp <= 0) continue;
                critical.Add(t);
            }
            if (critical.Count == 0) { _crisisShown = 0; return null; }

            critical.Sort((a, b) => sim.TablePatienceBp(a).CompareTo(sim.TablePatienceBp(b)));

            // SOUND ONLY ON A NEW CRISIS. A warning that plays every frame
            // stops being a warning and becomes noise.
            if (critical.Count > _crisisShown) Sfx.Upset();
            _crisisShown = critical.Count;

            // THE STRIP WAS REALLY BUILT.
            //
            // Until now the tour asked THE SIMULATION whether a crisis had
            // appeared (CrisisTables) - so it stayed green even with nothing
            // on the screen. The counter sits at exactly the point the strip
            // is built: this is the thing the player sees.
            CrisisBuilds++;

            VisualElement row = Theme.Row(6);
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;

            Label warning = Theme.Text(Loc.T("ui.service.crisis"), Theme.FontSmall, Theme.Bad);
            warning.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(warning);

            int n = critical.Count < 5 ? critical.Count : 5;
            for (int k = 0; k < n; k++)
            {
                int table = critical[k];
                bool selected = App.SelectedTable == table;

                Button chip = Theme.Btn(
                    (selected ? "› " : "")
                    + Loc.T("ui.service.table_n", table + 1) + " "
                    + Loc.Percent(sim.TablePatienceBp(table)),
                    () =>
                    {
                        App.SelectedTable = table;
                        Sfx.Click();
                        Ui.Refresh();
                    });

                // A SELECTION CANNOT BE CARRIED BY COLOUR ALONE.
                //
                // The only thing that told them apart was the text colour
                // (red <-> cream); under red-green colour blindness the two
                // read at a similar brightness, and the intervention goes to
                // THIS selection. A second channel: the mark (›) and a
                // border.
                chip.style.color = selected ? Theme.Ink : Theme.Bad;
                chip.style.borderTopWidth = selected ? 2 : 0;
                chip.style.borderBottomWidth = selected ? 2 : 0;
                chip.style.borderLeftWidth = selected ? 2 : 0;
                chip.style.borderRightWidth = selected ? 2 : 0;
                if (selected)
                {
                    chip.style.borderTopColor = Theme.Accent;
                    chip.style.borderBottomColor = Theme.Accent;
                    chip.style.borderLeftColor = Theme.Accent;
                    chip.style.borderRightColor = Theme.Accent;
                }
                row.Add(chip);
            }
            return row;
        }

        private int _crisisShown;

        /// <summary>How many times the crisis strip HAS BEEN BUILT. So the tour can ask.</summary>
        public static int CrisisBuilds { get; private set; }

        /// <summary>The critical-table count the bottom bar was last built at.</summary>
        private int _builtCrisis = -1;

        /// <summary>With something short, the first tap asks and the second opens.</summary>
        private bool _openConfirmed;

        /// <summary>
        /// How many tables are on the crisis strip right now. So the tour
        /// can ask.
        ///
        /// "Is the crisis visible" can only be asked WHEN THERE IS a
        /// crisis; without exposing the number, the check would stay green
        /// through a service in which the strip was never built at all.
        /// </summary>
        public int CrisisTables
        {
            get
            {
                Simulation sim = App != null ? App.Sim : null;
                if (sim == null) return 0;
                int threshold = sim.PatienceWarnBp, n = 0;
                for (int t = 0; t < sim.TableCount; t++)
                {
                    CustomerStage st = sim.TableStage(t);
                    if (st == CustomerStage.None || st == CustomerStage.Done
                        || st == CustomerStage.LeftAngry) continue;
                    int bp = sim.TablePatienceBp(t);
                    if (bp > threshold || bp <= 0) continue;
                    n++;
                }
                return n;
            }
        }

        /// <summary>
        /// SERVICE: the mode buttons, the interventions, close the day.
        ///
        /// The layout splits into three groups and the groups carry the
        /// game's own distinction:
        ///
        ///   MODE         - pause and speed. They set the game's clock and
        ///                  change nothing in the hall.
        ///   INTERVENTION - the game's whole set of verbs, in a shared box
        ///                  with the remaining allowance stuck to it: all
        ///                  three buttons feed from the same purse.
        ///   MOVE ON      - close the day, on the right and alone.
        /// </summary>
        private void ServiceBar(VisualElement row)
        {
            // PAUSE BECAME AN ICON.
            //
            // Written out in text, the mode buttons could not be told apart
            // from the interventions; both were grey rectangles. The icon
            // separates them by silhouette and saves space as well.
            //
            // The button has a NAME: neither a screen reader nor the
            // self-driving tour can find a text-less button by its text.
            Button pause = Kit.IconButton(
                App.Paused ? Icons.Play(Theme.Ink, 20f) : Icons.Pause(Theme.Ink, 20f),
                App.Paused ? Loc.T("ui.hud.resume") : Loc.T("ui.hud.pause"),
                () => { App.Paused = !App.Paused; BuildBottom(); });
            pause.name = "pause";
            row.Add(pause);

            // SPEED IS ONE BUTTON, AND IT CYCLES.
            //
            // It used to be three elements: minus, plus and a label between
            // them - 148 dp and three places in a nine-element strip. Speed
            // is something set once during a day. Because the cycle comes
            // back round to x0.5, going backwards is possible too.
            float mult = App.TimeScale / GameApp.BaseTimeScale;
            Button speed = Kit.IconButton(null,
                "×" + mult.ToString("0.#", Loc.Culture),
                () =>
                {
                    float next = App.TimeScale * 2f;
                    if (next > GameApp.BaseTimeScale * 16f + 0.01f)
                        next = GameApp.BaseTimeScale * 0.5f;
                    App.TimeScale = next;
                    BuildBottom();
                });
            speed.name = "speed";
            row.Add(speed);

            // --- the interventions IN ONE BOX --------------------------
            int left = App.Sim.InterventionsLeft;
            bool done = App.Sim.ServiceComplete;

            VisualElement box = Theme.Row(6);
            box.style.flexGrow = 1;
            // The intervention box GIVES UP ITS SPACE: the signature button
            // next to it and "Close the day" are fixed width, while the box
            // carries three buttons and can shrink. If it does not shrink it
            // overflows and lands on top of them.
            box.style.flexShrink = 1;
            // minWidth IS NOT PULLED TO ZERO.
            //
            // It was written that way once and the shrinking squeezed the
            // buttons UNDER THEIR OWN LABELS: measured, even "Tea" was
            // clipped. The box may give up space, but the touch targets
            // inside it (Theme.Touch) have to be kept - a shrinking strip is
            // better than a button that cannot be pressed, but not better
            // than one that cannot be read.
            box.style.backgroundColor = Kit.CardBg;
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderTopColor = Kit.CardLine;
            box.style.borderBottomColor = Kit.CardLine;
            box.style.borderLeftColor = Kit.CardLine;
            box.style.borderRightColor = Kit.CardLine;
            box.style.paddingLeft = 7;
            box.style.paddingRight = 7;
            box.style.paddingTop = 6;
            box.style.paddingBottom = 6;
            box.style.alignItems = Align.Center;
            Theme.Round(box, Kit.CardRadius);

            // THE STATION NAME IS NOT ON THE BUTTON.
            //
            // For a while it read "Chase the kitchen > Milkshake Machine"
            // and it WAS MEASURED: at Rubik's real letter widths that single
            // button was 319 dp and the strip came to 1103 dp - a 230 dp
            // overflow on an 873 dp screen.
            Button rush = Theme.Btn(Loc.T("ui.service.rush"), () =>
            {
                int st = App.Sim.BusiestStation();
                if (st < 0) { Toast(Loc.T("ui.service.none_station"), rejected: true); Sfx.Cancel(); return; }
                int before = App.Sim.InterventionsLeft;
                App.Send(CommandKind.Intervene, st, (int)InterventionKind.RushStation);
                if (DidIntervene(before))
                    Toast(Loc.T("ui.service.done_rush",
                                Loc.T("station." + App.Content.Stations[st].Id)));
                BuildBottom();
            }, wide: true);
            rush.SetEnabled(left > 0 && !done);
            box.Add(rush);

            // TEA PICKS NO TARGET: IT GOES TO THE WHOLE HALL.
            //
            // It used to go to the selected table, and in that form tea was
            // a dead button - attention beat it on every axis. Now tea goes
            // to EVERYONE WAITING, that is, it answers "the whole hall is
            // impatient" rather than "one table is in crisis". Having no
            // target selected is not a fault either.
            Button tea = Theme.Btn(Loc.T("ui.service.tea"), () =>
            {
                int before = App.Sim.InterventionsLeft;
                App.Send(CommandKind.Intervene, Target(), (int)InterventionKind.FreeTea);
                if (DidIntervene(before)) Toast(Loc.T("ui.service.done_tea_room"));
                else { Toast(Loc.T("ui.service.none_waiting"), rejected: true); Sfx.Cancel(); }
                BuildBottom();
            }, wide: true);
            // No one waiting, no tea: burning an allowance with nobody to
            // send it to would make no sense.
            tea.SetEnabled(left > 0 && !done && App.Sim.WaitingParties > 0);
            box.Add(tea);

            Button care = Theme.Btn(Loc.T("ui.service.attention"), () =>
            {
                int p = Target();
                if (p < 0) { Toast(Loc.T("ui.service.none_table"), rejected: true); Sfx.Cancel(); return; }
                int before = App.Sim.InterventionsLeft;
                App.Send(CommandKind.Intervene, p, (int)InterventionKind.OwnerAttention);
                if (DidIntervene(before)) Toast(TargetToast("care"));
                BuildBottom();
            }, wide: true);
            care.SetEnabled(left > 0 && !done);
            box.Add(care);

            // THE ALLOWANCE LEFT: DOTS, NOT A NUMBER.
            //
            // The words "Allowance 4" were 14 dp InkDim - the least
            // noticeable element on the screen, and it was the scarce
            // resource gating all three verbs.
            // THE THREE INTERVENTION BUTTONS HAVE NARROW PADDING.
            //
            // The default horizontal padding (Theme.Pad) comes to sixty dp
            // across three buttons, and that was exactly the strip's
            // overflow: in Turkish cuisine with fourteen tables - that is,
            // with the tab button present too - all three were being
            // clipped. The touch target comes from `minWidth` rather than
            // from the padding, so narrowing it does not hurt pressability.
            foreach (Button d in new[] { rush, tea, care })
            {
                d.style.paddingLeft = 8;
                d.style.paddingRight = 8;
            }

            // THE TARGET BADGE: it came off two buttons' labels and landed
            // here.
            box.Add(TargetBadge());

            VisualElement pips = Theme.Row(0);
            pips.style.alignItems = Align.Center;
            pips.style.marginLeft = 4;
            pips.style.marginRight = 2;
            for (int i = 0; i < App.Sim.InterventionsPerDay; i++)
                pips.Add(Theme.Dot(i < left ? Theme.Accent : Theme.Line, 9f));
            box.Add(pips);
            row.Add(box);

            // --- the signature mechanic: one or the other, by cuisine ---
            if (App.Sim.HasCombo)
            {
                Button combo = Kit.IconButton(
                    Icons.Combo(App.Sim.ComboEnabled ? Kit.Go : Theme.InkDim, 20f),
                    App.Sim.ComboEnabled ? Loc.T("ui.service.combo_on")
                                         : Loc.T("ui.service.combo_off"),
                    () =>
                    {
                        App.Send(CommandKind.SetCombo, App.Sim.ComboEnabled ? 0 : 1);
                        BuildBottom();
                    });
                combo.style.flexShrink = 1;
                row.Add(combo);
            }
            if (App.Sim.HasCredit)
            {
                int asker = FirstCreditAsker();
                Button credit = Kit.IconButton(
                    Icons.Book(asker >= 0 ? Theme.Accent : Theme.InkDim, 20f),
                    Loc.T("ui.service.credit"),
                    () =>
                    {
                        int p = FirstCreditAsker();
                        if (p < 0) { Toast(Loc.T("ui.service.none_credit"), rejected: true); Sfx.Cancel(); return; }
                        App.Send(CommandKind.ExtendCredit, p);
                        Toast(Loc.T("ui.service.done_credit", RegularName(p)));
                        BuildBottom();
                    });
                credit.SetEnabled(asker >= 0);
                credit.style.flexShrink = 1;
                row.Add(credit);
            }

            Spacer(row);

            // Once service is over, "Close the day" is THE ONLY MEANINGFUL
            // ACTION: it turns green and the intervention buttons close.
            // There is no such thing as offering tea to a finished service.
            row.Add(Kit.Cta(Loc.T("ui.service.close"),
                            done ? Loc.T("ui.service.close_sub")
                                 : Loc.T("ui.service.running_sub"),
                            () =>
            {
                App.CloseDay();
                BuildBottom();
                BuildCards();
            }, done));
        }

        /// <summary>
        /// EVENING: the day's summary on the left, the next day on the
        /// right.
        ///
        /// THE DAY REPORT IS NOT PRIMARY, BUT IT IS NOT LOST EITHER: the
        /// summary line is itself a CARD now, and the button that leads to
        /// the report sits beside it. "Day report" used to be orange and
        /// full width while "Next day" was grey - so the game's move-on
        /// button looked duller than a reading screen.
        /// </summary>
        private void EveningBar(VisualElement row)
        {
            // THE SUMMARY IS A CARD TOO.
            //
            // It used to be free text sitting straight on the scene: a dark
            // background over a dark hall, its readability changing with the
            // light. Every piece of information on the screen should sit on
            // a surface.
            VisualElement box = Kit.Box();
            box.style.paddingLeft = 14;
            box.style.paddingRight = 14;
            box.style.paddingTop = 8;
            box.style.paddingBottom = 8;
            box.style.flexShrink = 1;
            box.Add(EveningSummary());
            row.Add(box);

            row.Add(Kit.IconButton(Icons.List(Color.white), Loc.T("ui.evening.title"),
                                   () => Ui.Push(new EveningScreen()), blue: true));
            Spacer(row);
            row.Add(Kit.Cta(Loc.T("ui.evening.next"),
                            Loc.T("ui.evening.next_sub", App.Sim.Day + 1),
                            () =>
            {
                App.NextDay();
                BuildBottom();
                BuildCards();
            }));
        }

        /// <summary>The day's summary card: the left half of the evening strip.</summary>
        private VisualElement EveningSummary()
        {
            VisualElement col = Theme.Column(Theme.Gap);
            DayReport r = App.Sim.BuildDayReport();

            // TWO HEADLINES LARGE, THE REST SMALL AND ON THE SAME ROW.
            //
            // ALL SIX of the six numbers were set in 26 dp bold with nothing
            // but colour between them. That produced two results at once:
            //
            // 1. The headline disappeared. The brightest colour in the
            //    palette is Warn (0.499 luminance, above Good's 0.367), so
            //    "Spoiled 245" hit the eye harder than the profit it was
            //    supposed to explain.
            // 2. The strip swelled. Measured: the evening strip was 156 dp
            //    and, with the top strip, 228 dp - OVER the project's own
            //    220 dp budget, and the 165 dp left over was UNDER the
            //    project's own 173 dp floor.
            //
            // THE FIRST ATTEMPT MADE IT WORSE: moving the secondary numbers
            // to a row of their own took the strip to 269 dp. The check had
            // just been added and caught the fault on the same run - these
            // lines exist because of it.
            //
            // Now they are all on ONE row: two headlines at 26 dp, four
            // secondary numbers at 14 dp beside their labels. Nothing was
            // deleted.
            VisualElement stats = Theme.Row(Theme.Pad);
            stats.style.justifyContent = Justify.SpaceAround;
            stats.style.alignItems = Align.Center;
            stats.style.flexWrap = Wrap.Wrap;

            // THE HEADLINE IS NET PROFIT, not takings.
            //
            // Takings always go up; profit does not. In a restaurant
            // management game, making takings the biggest number was the
            // wrong headline.
            stats.Add(Stat(Loc.T("ui.evening.profit"), Loc.Money(r.NetProfit),
                           r.NetProfit >= 0 ? Theme.Good : Theme.Bad));
            if (App.HasYesterday)
                stats.Add(Delta(r.NetProfit - App.Yesterday.NetProfit, money: true));

            stats.Add(Stat(Loc.T("ui.hud.served"), r.ServedPeople.ToString(), Theme.Ink));
            if (App.HasYesterday)
                stats.Add(Delta(r.ServedPeople - App.Yesterday.ServedPeople, money: false));

            stats.Add(Small(Loc.T("ui.evening.revenue"),
                            Loc.Money(r.Revenue), Theme.InkDim));
            // THE STRIP SHOWS THE TOTAL, so its name is the total's name.
            stats.Add(Small(Loc.T("ui.evening.lost"), r.AngryParties.ToString(),
                            r.AngryParties > 0 ? Theme.Bad : Theme.InkDim));
            stats.Add(Small(Loc.T("ui.evening.satisfaction"),
                            Loc.Reputation(r.AverageSatisfactionCenti),
                            Theme.ReputationColor(r.AverageSatisfactionCenti)));

            // WHAT GOES IN THE BIN, IN THE EVENING SUMMARY TOO.
            //
            // Only a player who pressed the "Day Report" button saw it, and
            // that is a secondary screen. And yet this is the game's biggest
            // invisible cost: a player playing well throws away a
            // substantial part of the ingredients they buy over sixty days
            // and had nowhere to find out why the till was not filling.
            if (r.SpoiledValue > 0)
                stats.Add(Small(Loc.T("ui.evening.spoiled"),
                                Loc.Money(r.SpoiledValue), Theme.Warn));

            col.Add(stats);

            // THE DAY REPORT IS PRIMARY, "NEXT DAY" SECONDARY.
            //
            // It used to be the other way round: the report grey and
            // secondary, "Next day" orange and primary. In a sixty-day
            // campaign the player presses orange sixty times and never sees
            // the waste, the demoralised staff or the regulars' beats - so
            // the teaching side of the game never opens at all.
            return col;
        }

        /// <summary>
        /// THE DIFFERENCE AGAINST YESTERDAY.
        ///
        /// A number on its own means nothing: there is no telling whether
        /// "568" is good or bad. "568 (+127)" is the result of A DECISION.
        /// That is what closes the learning loop.
        /// </summary>
        private static VisualElement Delta(long delta, bool money)
        {
            string text;
            Color color;
            if (delta > 0) { text = "+" + (money ? Loc.Money(delta) : delta.ToString()); color = Theme.Good; }
            else if (delta < 0) { text = (money ? Loc.Money(delta) : delta.ToString()); color = Theme.Bad; }
            else { text = Loc.T("ui.evening.same"); color = Theme.InkFaint; }

            Label l = Theme.Text(text, Theme.FontSmall, color);
            l.style.marginLeft = -6;
            l.style.marginRight = 6;
            return l;
        }

        /// <summary>A secondary number: label and value on THE SAME row, small.</summary>
        private static VisualElement Small(string label, string value, Color color)
        {
            VisualElement v = Theme.Row(6);
            v.style.alignItems = Align.Center;
            v.Add(Theme.Text(label, Theme.FontSmall, Theme.InkFaint));
            Label val = Theme.Text(value, Theme.FontSmall, color);
            val.style.unityFontStyleAndWeight = FontStyle.Bold;
            v.Add(val);
            return v;
        }

        /// <summary>
        /// A headline number: small label, large value, ON THE SAME ROW.
        ///
        /// It used to be two lines (the value at 26 dp with the label at 14
        /// dp beneath it) and the strip came to 156 dp - 228 with the top
        /// strip, against a budget of 220. On one line the same two pieces
        /// of information are still there, the strip loses 20 dp, and the
        /// large-against-small contrast already brings the number forward.
        /// </summary>
        private static VisualElement Stat(string label, string value, Color color)
        {
            VisualElement v = Theme.Row(6);
            v.style.alignItems = Align.Center;
            v.Add(Theme.Text(label, Theme.FontSmall, Theme.InkFaint));
            Label big = Theme.Text(value, Theme.FontTitle, color);
            big.style.unityFontStyleAndWeight = FontStyle.Bold;
            v.Add(big);
            return v;
        }

        // =====================================================================
        // A copy was removed: the same loop is in the core and the
        // definition of "eligible" should live in one place.
        private int FirstCreditAsker() { return App.Sim.FirstCreditAsker(); }

        private string RegularName(int party)
        {
            int reg = App.Sim.PartyRegular(party);
            if (reg < 0) return Loc.T("ui.service.guest");
            return Loc.T(App.Content.Regulars[reg].NameKey);
        }

        /// <summary>
        /// The answer to the player's own action. Simulation events arrive
        /// by a separate route (RebuildNotices).
        /// </summary>
        private void Toast(string text, bool rejected = false)
        {
            _ownText = text;
            _ownRejected = rejected;
            _ownLeft = 2.2f;
            RebuildNotices();
        }

        private string _ownText;
        private bool _ownRejected;
        private float _ownLeft;

        /// <summary>
        /// Rebuilds the notice strip: the player's own action at the top,
        /// the three most recent items from the simulation beneath it.
        ///
        /// Rebuilt ONLY on a change: a strip rebuilt every frame leaves the
        /// detached elements as garbage every frame and triggers the
        /// collector on mobile.
        /// </summary>
        private void RebuildNotices()
        {
            // ON THE FIRST BUILD _toast DOES NOT EXIST YET.
            //
            // Build() builds the bottom strip first (BuildBottom) and
            // creates the notice area AFTERWARDS; when BuildBottom called
            // RebuildNotices from in there, _toast was null and the whole
            // screen tree was left half-built. The tour caught it with a
            // NullReferenceException and got stuck for nine minutes -
            // because what broke was THE SCREEN ITSELF, not a single check.
            if (_toast == null) return;

            _toast.Clear();

            // THE HINT GOES AT THE TOP and differs from the bubbles: it does
            // not disappear by itself, and once dismissed it never comes
            // back.
            Hints.Hint hint = Hints.Current(App.Sim);
            if (hint != null)
            {
                VisualElement strip = Hints.Strip(hint, App.Sim, RebuildNotices);
                if (strip != null) _toast.Add(strip);
            }

            // ACCEPTED AND REJECTED CANNOT BE THE SAME COLOUR.
            //
            // The answer to the player's own action was written in Accent
            // unconditionally, so "tea was offered" and "the request was
            // rejected" were the same colour, in the same place, for the
            // same length of time. The player spent a scarce intervention
            // allowance and could not tell FROM THE COLOUR whether it had
            // gone through - exactly the problem DidIntervene's comment
            // solved in the text, still standing in the visual channel.
            if (!string.IsNullOrEmpty(_ownText))
                _toast.Add(Bubble(_ownText, _ownRejected ? Theme.Bad : Theme.Accent));

            for (int i = 0; i < App.NoticeCount; i++)
                _toast.Add(Bubble(App.NoticeTextAt(i), ToneColor(App.NoticeToneAt(i))));
        }

        private static VisualElement Bubble(string text, Color color)
        {
            VisualElement box = Theme.PanelBox();
            box.style.backgroundColor = new Color(Theme.PanelHi.r, Theme.PanelHi.g,
                                                  Theme.PanelHi.b, 0.95f);
            box.style.paddingTop = Theme.Gap;
            box.style.paddingBottom = Theme.Gap;
            box.style.marginTop = 4;
            box.Add(Theme.Text(text, Theme.FontBody, color));
            return box;
        }

        private static Color ToneColor(NoticeTone tone)
        {
            switch (tone)
            {
                case NoticeTone.Good: return Theme.Good;
                case NoticeTone.Warn: return Theme.Warn;
                case NoticeTone.Bad:  return Theme.Bad;
                default:              return Theme.Ink;
            }
        }

        // =====================================================================
        // The last values written. The top strip is rewritten ONLY when
        // something changes.
        //
        // It used to be written every frame: six fields, each with
        // concatenation and number formatting, so roughly nine hundred small
        // allocations a second. Even when the values were unchanged the text
        // was still PRODUCED, because the comparison can only be made after
        // producing it. On mobile that means a garbage collection every few
        // seconds and a visible dropped frame.
        private int _shownDay = -1, _shownRep = -1;
        private float _shownProgress = -1f;
        private DayPhase _shownMeterPhase = (DayPhase)(-1);
        private int _shownRentDays = -1;
        private int _shownRepCap = -1;
        private long _shownCash = long.MinValue;

        // The flow row's COMPONENTS are held separately: comparing the
        // joined-up string required PRODUCING it first.
        private int _shownServed = -1, _shownAngry = -1, _shownOccupied = -1;
        private DayPhase _shownPhase = (DayPhase)(-1);

        /// <summary>Was the strip built in the ServiceComplete state.</summary>
        private bool _builtServiceDone;

        private IVisualElementScheduledItem _cashRun;

        /// <summary>
        /// The till counter DOES NOT JUMP to the value, it counts up to it.
        ///
        /// The value used to change instantly, and a 1,630 ¤ rent collection
        /// and a 12 ¤ sale were THE SAME thing on screen: just a different
        /// number now. In a management game the till is the feedback itself
        /// - counting turns "a number changed" into "you earned something".
        ///
        /// The rules:
        ///   - A small difference (under 10 ¤) or the first write DOES NOT
        ///     COUNT; a counter that flickers on every small sale is noise.
        ///   - The duration is 360 ms, about 11 frames. Anything shorter
        ///     comes down to a few frames at 30 fps and the counting cannot
        ///     be seen.
        ///   - Only the TEXT changes; the width is not fixed as such, but
        ///     the top strip is one row and its neighbours are flexible, so
        ///     even a relayout is confined to that one row.
        ///   - If a new change arrives the old one is CANCELLED, or two
        ///     counters write to the same label.
        /// </summary>
        private void CashTo(long from, long to)
        {
            _cashRun?.Pause();
            _cashRun = null;

            if (from == long.MinValue || System.Math.Abs(to - from) < 1000)
            {
                _cash.text = Loc.Money(to);
                return;
            }

            const float Duration = 0.36f;
            float started = Time.unscaledTime;
            _cashRun = _cash.schedule.Execute(() =>
            {
                float t = Mathf.Clamp01((Time.unscaledTime - started) / Duration);
                // Decelerating: the number runs fast at first, then settles.
                float k = 1f - (1f - t) * (1f - t) * (1f - t);
                long v = from + (long)((to - from) * k);
                _cash.text = Loc.Money(t >= 1f ? to : v);
                if (t >= 1f) { _cashRun?.Pause(); _cashRun = null; }
            }).Every(33);
        }

        public override void Tick()
        {
            Simulation sim = App.Sim;
            if (sim == null) return;

            // THE END OF SERVICE DOES NOT CHANGE THE PHASE: _phase is still
            // Service. The strip only watched the phase, so the ending was
            // never reflected on the screen at all.
            bool done = sim.Phase == DayPhase.Service && sim.ServiceComplete;
            if (done != _builtServiceDone)
            {
                _builtServiceDone = done;
                BuildBottom();
            }

            if (sim.Day != _shownDay)
            {
                _shownDay = sim.Day;
                // JUST THE NUMBER ON THE BADGE: the word "Day" will not fit
                // a 46 dp badge and the phase label is right beside it
                // anyway.
                _day.text = sim.Day.ToString(Loc.Culture);
                if (_season != null)
                    _season.text = sim.SeasonOver
                        ? Loc.T("ui.hud.free_play")
                        : Loc.T("ui.hud.season", sim.Day, sim.CampaignDays);
            }
            if (sim.Cash != _shownCash)
            {
                long previous = _shownCash;
                _shownCash = sim.Cash;
                CashTo(previous, sim.Cash);
                // DARK text on the plate. In debt it must stay red, but a
                // red that reads on the plate.
                _cash.style.color = sim.Cash < 0
                    ? new Color(0.69f, 0.12f, 0.14f) : Theme.PlateInk;
            }
            // THE CEILING IS WRITTEN OUT TOO.
            //
            // Simulation.ReputationCapCenti's comment said "the interface
            // should show this" and no screen read it. A player pressed up
            // against the ceiling sees the number stop while they carry on
            // running a good service, with nowhere to learn why.
            if (sim.ReputationCenti != _shownRep
                || sim.ReputationCapCenti != _shownRepCap)
            {
                _shownRep = sim.ReputationCenti;
                _shownRepCap = sim.ReputationCapCenti;

                // The number's NAME is written too. Only "30.0" used to
                // appear and colour ALONE carried whether that was good or
                // bad - which meant nothing to a colour-blind player.
                //
                // And THE CEILING is written: "Reputation 75.0 / 75". A
                // player pressed against the ceiling watched the number
                // refuse to move however well they ran the service, with
                // nowhere to learn why. Measured: a player playing well hits
                // 75 on seven tables and sits there for 32 days.
                bool atCap = sim.ReputationCenti >= sim.ReputationCapCenti;
                // NO LABEL IN THE PILL: the icon already says "reputation".
                // The ceiling stays - it is the only way a player at the
                // ceiling sees why the number is not moving.
                _rep.text = Loc.Reputation(sim.ReputationCenti)
                            + " / " + (sim.ReputationCapCenti / 100);
                // At the ceiling the colour reports a DIRECTION rather than
                // a STATE: this is not a bad place, it is a place you cannot
                // get past without growing.
                _rep.style.color = atCap
                    ? Theme.Warn : Theme.ReputationColor(sim.ReputationCenti);
            }
            int toRent = sim.DaysToRent;
            if (toRent != _shownRentDays)
            {
                _shownRentDays = toRent;
                _rent.text = toRent == 0
                    ? Loc.T("ui.hud.rent_today", Loc.Money(sim.WeeklyBill))
                    : Loc.T("ui.hud.rent_in", toRent, Loc.Money(sim.WeeklyBill));

                // Amber for the last two days, red if the till is short: the
                // warning has to come IN TIME, not after the bill has
                // arrived.
                _rent.style.color = sim.Cash < sim.WeeklyBill && toRent <= 2
                    ? Theme.Bad
                    : toRent <= 2 ? Theme.Warn : Theme.InkDim;
            }

            // THE FLOW ROW: the string is only produced WHEN THE NUMBERS
            // CHANGE.
            //
            // The other six fields in the top strip were already guarded,
            // this one was not: Loc.T(key, a, b, c) was being called every
            // frame, and that is a string.Format - an object[] array, three
            // int boxings and a formatted string, even when the values had
            // not changed at all. Around 150-200 bytes a frame, so about 3
            // MB of garbage in an eight-minute service, which is a visible
            // collection pause every few minutes.
            //
            // And OccupiedTables is not a property but a LOOP: fourteen
            // tables were being scanned every frame.
            bool service = sim.Phase == DayPhase.Service;

            // THE CRISIS STRIP NOW APPEARS OF ITS OWN ACCORD.
            //
            // The bottom bar is only built ON CHANGES (the phase changed, a
            // button was pressed, service ended) and the crisis strip lives
            // inside that bar. So a player who touched nothing NEVER saw the
            // "PATIENCE RUNNING OUT" warning - and the warning sound
            // (Sfx.Upset) did not play either, because that too plays when
            // the strip is built. The game's only urgent warning channel
            // depended on the moments the player was already touching the
            // screen.
            //
            // The tour could not see this: the check asked whether the
            // simulation had a critical table, not whether the strip HAD
            // BEEN BUILT.
            //
            // The condition is THE COUNT CHANGING: not every frame, but as
            // tables move into and out of critical.
            if (service)
            {
                int critical = CrisisTables;
                if (critical != _builtCrisis)
                {
                    _builtCrisis = critical;
                    BuildBottom();
                }
            }
            else if (_builtCrisis != -1) _builtCrisis = -1;

            if (service)
            {
                int served = sim.ServedParties;
                int angry = sim.AngryParties;
                int occupied = sim.OccupiedTables;

                if (served != _shownServed || angry != _shownAngry
                    || occupied != _shownOccupied || _shownPhase != DayPhase.Service)
                {
                    _shownServed = served;
                    _shownAngry = angry;
                    _shownOccupied = occupied;
                    _shownPhase = DayPhase.Service;
                    // The "Today" CARD: it took the place of the single
                    // cramped row in the top strip. The same three numbers,
                    // now readable.
                    if (_servedValue != null)
                        _servedValue.text = served.ToString(Loc.Culture);
                    if (_angryValue != null)
                    {
                        _angryValue.text = angry.ToString(Loc.Culture);
                        // DARK RED ON A LIGHT BACKGROUND: Theme.Bad gave
                        // 2.22:1 here, BadDeep gives 5.4:1.
                        _angryValue.style.color = angry > 0
                            ? Kit.BadDeep : Theme.PlateInk;
                    }
                    if (_occupiedValue != null)
                        _occupiedValue.text = occupied.ToString(Loc.Culture)
                                              + " / " + sim.TableCount;
                    if (_turnedValue != null)
                    {
                        int turnedAway = sim.TurnedAwayParties;
                        _turnedValue.text = turnedAway.ToString(Loc.Culture);
                        _turnedValue.style.color = turnedAway > 0
                            ? Kit.BadDeep : Theme.PlateInk;
                    }
                }
            }
            // Outside service only the STAMP is refreshed: the block above
            // works off the "_shownPhase == Service" comparison, so if the
            // phase change is not recorded here the "Today" card lags a
            // frame behind when service comes round again.
            else if (sim.Phase != _shownPhase) _shownPhase = sim.Phase;

            // THE PHASE LABEL AND THE DAY BAR.
            //
            // The bar shows how much of the service day has gone - the game
            // had this number (ServiceProgressBp; the light, the shadows and
            // the street lamps all read from it) but it was NEVER shown to
            // the player. The answer to "how long is left" existed only in
            // the colour of the sky.
            if (_phaseLabel != null && _meterFill != null)
            {
                float ratio = sim.Phase == DayPhase.Service
                    ? sim.ServiceProgressBp / 10000f
                    : (sim.Phase == DayPhase.Evening ? 1f : 0f);
                if (Mathf.Abs(ratio - _shownProgress) > 0.004f
                    || sim.Phase != _shownMeterPhase)
                {
                    _shownProgress = ratio;
                    _shownMeterPhase = sim.Phase;
                    _meterFill.style.width = Length.Percent(ratio * 100f);
                    // In the evening the bar is FULL and muted: the day is
                    // over. Filled with Theme.Line it read as "an empty bar"
                    // - full and empty could not be told apart.
                    _meterFill.style.backgroundColor =
                        sim.Phase == DayPhase.Service ? Theme.Accent
                        : (sim.Phase == DayPhase.Evening ? Theme.AccentDim
                                                         : Theme.Line);
                    _phaseLabel.text = Loc.T("ui.phase." + PhaseKey(sim.Phase));
                }
            }

            // THE STAT CARDS: takings and satisfaction.
            //
            // Both were in the day report, that is, AFTER the day had ended.
            // There was nothing for the player to look at during service to
            // answer "is this going well".
            if (_revenue != null)
            {
                int signature = (int)(sim.Revenue % 1000000L) * 100 + sim.AverageSatisfactionCenti;
                if (signature != _shownRevenueDay)
                {
                    _shownRevenueDay = signature;
                    _revenue.text = Loc.Money(sim.Revenue);
                    if (_satisfaction != null)
                    {
                        int m = sim.AverageSatisfactionCenti;
                        _satisfaction.text = Loc.Reputation(m);
                        _satisfaction.style.color = m >= 7000 ? Theme.Good
                            : (m >= 5000 ? Theme.Warn : Theme.Bad);
                    }
                }
            }

            if (sim.Phase != _builtPhase) BuildBottom();
            if (sim.Phase != _builtCards) BuildCards();

            // A pending story beat opens in the evening.
            //
            // NOT tied to THE PHASE CHANGING: the "Close the day" button
            // calls BuildBottom itself, so _builtPhase is updated before
            // Tick runs and the transition was never seen. The condition has
            // to be THE STATE ITSELF, not a change in it.
            if (sim.Phase == DayPhase.Evening && App.HasStory && Ui.Top == this)
                Ui.Push(new StoryScreen());

            // Notices: the bubble for my own action goes when its time is
            // up; the ones from the simulation are aged by GameApp.
            bool dirty = App.NoticesChanged;
            if (_ownLeft > 0f)
            {
                _ownLeft -= Time.deltaTime;
                if (_ownLeft <= 0f) { _ownText = null; dirty = true; }
            }
            if (dirty)
            {
                App.NoticesSeen();
                RebuildNotices();
            }
        }

        private static string PhaseKey(DayPhase p)
        {
            switch (p)
            {
                case DayPhase.Morning: return "morning";
                case DayPhase.Service: return "service";
                default: return "evening";
            }
        }

        // On the game screen the back key opens the pause menu rather than
        // throwing you out of the game. Leaving a campaign by accident is
        // not acceptable.
        public override bool OnBack()
        {
            Ui.Push(new PauseScreen());
            return false;
        }
    }
}
