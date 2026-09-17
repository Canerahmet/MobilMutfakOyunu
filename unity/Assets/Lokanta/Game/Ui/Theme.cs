using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// The interface's visual language. Colours and measures IN ONE PLACE.
    ///
    /// The reason this is C# rather than a USS file is specific to this
    /// project: the interface is built in code and the scene is a
    /// generated thing (docs/34 §22). A USS file would be a second source
    /// of truth living outside the code.
    ///
    /// The colour choice is independent of the cuisine identity, and
    /// deliberately so: cuisine identity lives IN THE HALL (docs/10), not
    /// in the interface. The interface's job is to be readable.
    ///
    /// The measures are for touch: docs/16 puts the minimum touch target
    /// at 48 dp, and every button here is at least that tall.
    /// </summary>
    public static class Theme
    {
        // --- colour ----------------------------------------------------------
        public static readonly Color Ink = new Color(0.94f, 0.93f, 0.90f);
        public static readonly Color InkDim = new Color(0.68f, 0.66f, 0.62f);
        // Measured: the old value (0.46) gave 3.24:1 on the panel, and the
        // WCAG AA minimum is 4.5:1. And this is the most used colour of all
        // - sub-headings, the service status line, the reasons things are
        // locked. So the unreadable part was precisely the explanatory text
        // itself.
        public static readonly Color InkFaint = new Color(0.60f, 0.59f, 0.57f);

        public static readonly Color Bg = new Color(0.086f, 0.094f, 0.110f);
        public static readonly Color Panel = new Color(0.137f, 0.145f, 0.165f);
        public static readonly Color PanelHi = new Color(0.180f, 0.190f, 0.215f);
        public static readonly Color Line = new Color(0.255f, 0.263f, 0.290f);

        /// <summary>Accent: copper. The colour of a restaurant's counter.</summary>
        public static readonly Color Accent = new Color(0.847f, 0.545f, 0.259f);
        public static readonly Color AccentDim = new Color(0.549f, 0.353f, 0.169f);

        /// <summary>
        /// THE LIGHT PLATE THE NUMBERS SIT ON.
        ///
        /// This was the most common rule to come out of the survey of
        /// comparable games: even where the chrome is dark, the NUMBER sits
        /// on a light plate - Good Pizza Great Pizza, My Cafe, Cooking
        /// Diary, Cooking Fever, Idle Restaurant Tycoon, every one of them.
        /// The reason is measurable: the thing the player re-reads every
        /// three seconds is the till figure, and this makes it the
        /// highest-contrast object on the screen WITHOUT SPENDING an accent
        /// colour on it.
        ///
        /// Warm off-white rather than white: 12% of the screen in pure
        /// white is a source of glare on a phone at night; the warm tone
        /// also ties the chrome to the food.
        ///
        /// Measured: on Plate, PlateInk is 12.9:1 and PlateDim 5.1:1. The
        /// capsule itself is 12.5:1 against the strip background - so even
        /// "there is an object here" comes from BRIGHTNESS rather than from
        /// colour.
        /// </summary>
        public static readonly Color Plate = new Color(0.914f, 0.898f, 0.863f);
        public static readonly Color PlateInk = new Color(0.110f, 0.125f, 0.157f);
        public static readonly Color PlateDim = new Color(0.337f, 0.376f, 0.427f);

        public static readonly Color Good = new Color(0.427f, 0.702f, 0.451f);
        public static readonly Color Warn = new Color(0.898f, 0.706f, 0.310f);
        // Measured: the old value was 4.2:1 on the panel - just under the
        // threshold.
        public static readonly Color Bad = new Color(0.93f, 0.47f, 0.44f);

        // --- measure ---------------------------------------------------------
        /// <summary>Minimum touch target. Google says 48 dp.</summary>
        public const int Touch = 52;
        public const int Gap = 10;
        public const int Pad = 16;
        public const int Radius = 10;

        public const int FontHuge = 42;
        public const int FontTitle = 26;
        public const int FontBody = 17;
        public const int FontSmall = 14;

        // --- helpers ---------------------------------------------------------
        public static VisualElement Column(float gap = Gap)
        {
            VisualElement v = new VisualElement();
            v.style.flexDirection = FlexDirection.Column;
            SetGap(v, gap, true);
            return v;
        }

        /// <summary>
        /// A row's FLOW DIRECTION. Reversed in Arabic.
        ///
        /// In a right-to-left language the writing starts from the right,
        /// but so does the LAYOUT: the checkbox moves to the right of the
        /// text, the back arrow moves from the right to the left. Turning
        /// the text round and leaving the layout alone means a half
        /// translated interface - and half translated looks worse than not
        /// translated at all.
        ///
        /// It has to be IN ONE PLACE: there are thirty-two places that set
        /// up a row, and forgetting one of them means that row flows the
        /// wrong way. The direction is ASKED FOR, not written out.
        /// </summary>
        public static FlexDirection RowFlow
        {
            get
            {
                return Loc.IsRightToLeft
                    ? FlexDirection.RowReverse
                    : FlexDirection.Row;
            }
        }

        public static VisualElement Row(float gap = Gap)
        {
            VisualElement v = new VisualElement();
            v.style.flexDirection = RowFlow;
            SetGap(v, gap, false);
            return v;
        }

        private static void SetGap(VisualElement v, float gap, bool column)
        {
            // UI Toolkit has no gap; the spacing is given to the children as
            // a margin. It is done in one place so two different spacings
            // cannot come about.
            v.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                // THE SPACING GOES ON A PHYSICAL EDGE.
                //
                // In row-reverse the first child sits at the FAR RIGHT. Had
                // the spacing still gone to marginLeft, the whole run would
                // be shifted by one: an extra gap at the left end of the row
                // and no gap at all between the first two elements.
                bool reversed = !column && Loc.IsRightToLeft;
                for (int i = 1; i < v.childCount; i++)
                {
                    if (column) v[i].style.marginTop = gap;
                    else if (reversed) v[i].style.marginRight = gap;
                    else v[i].style.marginLeft = gap;
                }
            });
        }

        public static VisualElement PanelBox()
        {
            VisualElement v = Column();

            // DO NOT LET IT BE SQUASHED. The children of a flex column come
            // with flex-shrink 1 by default; in a scrollable list that means
            // every card is squashed down until they all fit in the window.
            // The content was spilling out of the box and the rows were
            // OVERLAPPING each other - which is exactly how it came out on
            // the market screen in the first desktop build.
            v.style.flexShrink = 0;

            v.style.backgroundColor = Panel;
            v.style.paddingLeft = Pad;
            v.style.paddingRight = Pad;
            v.style.paddingTop = Pad;
            v.style.paddingBottom = Pad;
            Round(v, Radius);
            return v;
        }

        public static void Round(VisualElement v, float r)
        {
            v.style.borderTopLeftRadius = r;
            v.style.borderTopRightRadius = r;
            v.style.borderBottomLeftRadius = r;
            v.style.borderBottomRightRadius = r;
        }

        public static Label Text(string s, int size = FontBody, Color? color = null)
        {
            Label l = new Label(s);
            l.style.fontSize = size;
            l.style.color = color ?? Ink;
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        public static Label Title(string s)
        {
            Label l = Text(s, FontTitle);
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            return l;
        }

        /// <summary>
        /// A button. Its height is the MINIMUM TOUCH TARGET; that value is
        /// written in one place so that no screen is left with a small
        /// button on it.
        /// </summary>
        public static Button Btn(string text, System.Action onClick,
                                 bool primary = false, bool wide = false,
                                 bool danger = false)
        {
            Button b = new Button(() => { Sfx.Click(); onClick?.Invoke(); }) { text = text };
            b.style.minHeight = Touch;
            b.style.fontSize = FontBody;
            // The text is CENTRED vertically. The default top-left alignment
            // pinned the text to the top of a 52 dp touch target and the
            // button looked broken (the first desktop build).
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            // Buttons with short labels are as WIDE as the touch target too.
            // The "-" and "+" buttons were 44 dp wide and were the two most
            // pressed buttons of the whole service.
            b.style.minWidth = Touch;
            b.style.paddingLeft = Pad;
            b.style.paddingRight = Pad;
            b.style.marginLeft = 0;
            b.style.marginRight = 0;
            b.style.marginTop = 0;
            b.style.marginBottom = 0;
            Color face = danger ? Bad : (primary ? Accent : PanelHi);
            b.style.backgroundColor = face;
            b.style.color = (primary || danger) ? Bg : Ink;
            b.style.unityFontStyleAndWeight =
                (primary || danger) ? FontStyle.Bold : FontStyle.Normal;
            b.style.borderTopWidth = 0;
            b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0;
            b.style.borderRightWidth = 0;
            if (wide)
            {
                b.style.flexGrow = 1;

                // AND ABLE TO SHRINK.
                //
                // Unlike CSS, UI Toolkit's default for `flex-shrink` is
                // ZERO: when the row does not fit, nothing shrinks and the
                // last elements overflow and land ON TOP of the one before.
                // Measured - in English "Attention > worst" and "Close the
                // day" overlapped by 13 dp ON THE FIRST DAY, and in the
                // Turkish build "Attention > impatient" and "Open a tab"
                // by 87 dp.
                //
                // BOTH the strip-height and the clipped-text measurements
                // were green: the height was right and no TEXT was clipped -
                // because instead of being clipped it was overlapping.
                b.style.flexShrink = 1;
            }
            Round(b, Radius);
            Press(b, face);
            return b;
        }

        /// <summary>
        /// Press feedback: darkening plus a 3% shrink, over 90 ms.
        ///
        /// WHY BY HAND: UI Toolkit's :active rule is supplied by a
        /// stylesheet, and this project builds the whole interface in C#
        /// and writes INLINE styles - and an inline style always beats a
        /// rule sheet, so the ready-made pressed state never showed at all.
        ///
        /// The upshot: not one button in the game HAD a pressed state.
        /// There was a sound, but on a phone the sound is usually off
        /// (GameApp's own note), so for most players a tap gave back
        /// NOTHING - until the state changed. Fixed in one place, for 59
        /// call sites at once.
        ///
        /// The measure: 0.97 - enough for the touch to be felt, small
        /// enough not to give the impression that the button has moved.
        /// </summary>
        internal static void PressFx(VisualElement b, Color face) { Press(b, face); }

        private static void Press(VisualElement b, Color face)
        {
            Color pressed = new Color(face.r * 0.78f, face.g * 0.78f, face.b * 0.78f, face.a);

            b.RegisterCallback<PointerDownEvent>(_ =>
            {
                b.style.backgroundColor = pressed;
                b.style.scale = new Scale(new Vector3(0.97f, 0.97f, 1f));
            });

            // Up AND Leave: a player who slides their finger off the button
            // before lifting it never gets an Up, and the button stayed
            // pressed.
            EventCallback<EventBase> release = _ =>
            {
                b.style.backgroundColor = face;
                b.style.scale = new Scale(Vector3.one);
            };
            b.RegisterCallback<PointerUpEvent>(e => release(e));
            b.RegisterCallback<PointerLeaveEvent>(e => release(e));
            b.RegisterCallback<PointerCancelEvent>(e => release(e));
        }

        /// <summary>
        /// A small filled circle. For MARKING something (a favourite dish,
        /// for instance).
        ///
        /// A drawn element, NOT a font glyph. It used to use the star
        /// character and the font did not have it: the player saw an empty
        /// box (tools/art/check_font.py caught it). A drawn icon does not
        /// depend on the font and stays crisp at every size.
        /// </summary>
        /// <summary>
        /// A section heading. NOT COLOUR, WEIGHT.
        ///
        /// They used to be set in Accent, and the accent colour was already
        /// doing eight other jobs: "do this", "this one is open", "you can
        /// afford it", "close this screen"... What an orange bar meant had
        /// become impossible to guess.
        ///
        /// The headings also had an inconsistency: on the equipment screen
        /// "Cold store" was Accent and its sibling "Stove" directly beneath
        /// it was plain Ink - two different headings for a reason the
        /// player could not see. One way was kept.
        /// </summary>
        public static Label Head(string text)
        {
            Label l = Text(text, FontBody, Ink);
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.letterSpacing = 0.6f;
            return l;
        }

        public static VisualElement Dot(Color c, float size = 10f)
        {
            VisualElement v = new VisualElement();
            v.style.width = size;
            v.style.height = size;
            v.style.backgroundColor = c;
            v.style.alignSelf = Align.Center;
            v.style.marginRight = 6;
            v.style.flexShrink = 0;
            Round(v, size * 0.5f);
            return v;
        }

        /// <summary>
        /// The widest text can be and still be readable (dp).
        ///
        /// Not a measurement but an assumption: on a label-and-value row the
        /// eye cannot travel from the far left to the far right. A phone
        /// held in landscape can be 1280 dp wide and a list row should not
        /// be that long.
        /// </summary>
        public const float ReadWidth = 880f;

        /// <summary>
        /// A stepped level control: a row of cells, with the filled ones
        /// picked out.
        ///
        /// UI Toolkit's own Slider is NOT USED. Two reasons:
        ///
        /// 1) Its appearance comes from Unity's default runtime theme, and
        ///    that theme DOES NOT REACH THE BUILD - in the first desktop
        ///    build the two sliders on the settings screen came out as two
        ///    completely empty boxes. The same cause was behind the text
        ///    not showing either.
        ///
        /// 2) A thin slider bar is hard to catch with a finger. In a row of
        ///    ten steps each cell is within the 52 dp touch target and the
        ///    level can be READ OFF the screen.
        /// </summary>
        public static VisualElement Level(string label, int steps, int value,
                                          System.Action<int> onPick)
        {
            VisualElement box = Column(Gap);

            VisualElement head = Row(0);
            head.style.justifyContent = Justify.SpaceBetween;
            head.Add(Text(label, FontBody, InkDim));
            Label read = Text(value + " / " + steps, FontSmall, Ink);
            head.Add(read);
            box.Add(head);

            VisualElement bar = Row(4);
            bar.style.height = Touch;

            // THE CELL WIDTH IS A TOUCH TARGET TOO.
            //
            // The height was Touch but the width was not: in a 480 dp panel,
            // once you take off ten steps, 32 dp of padding and nine gaps,
            // about 41 dp was left per cell - under the theme's own 52 dp
            // measure. A finger reaching for the volume landed on the
            // neighbouring step.
            //
            // The flex basis lets the MEASURE rather than the caller's step
            // count decide: if it does not fit, the row wraps.
            bar.style.flexWrap = Wrap.Wrap;
            bar.style.height = StyleKeyword.Auto;
            bar.style.minHeight = Touch;

            for (int i = 1; i <= steps; i++)
            {
                int step = i;
                Button cell = new Button(() => { Sfx.Click(); onPick(step); })
                              { text = string.Empty };
                cell.style.flexGrow = 1;
                cell.style.flexBasis = Touch;
                cell.style.minWidth = Touch;
                cell.style.height = Touch;
                cell.style.marginLeft = 0;
                cell.style.marginRight = 0;
                cell.style.marginTop = 0;
                cell.style.marginBottom = 0;
                cell.style.borderTopWidth = 0;
                cell.style.borderBottomWidth = 0;
                cell.style.borderLeftWidth = 0;
                cell.style.borderRightWidth = 0;
                cell.style.backgroundColor = i <= value ? Accent : PanelHi;
                Round(cell, 4);
                bar.Add(cell);
            }
            box.Add(bar);
            return box;
        }

        /// <summary>
        /// Makes a scroll area MOBILE: the desktop scrollbar is hidden and
        /// finger dragging is turned on.
        ///
        /// Unity's default ScrollView draws a thick, light grey bar with
        /// arrow buttons on it. That very bar was sitting in the screenshot
        /// of the year-end evaluation screen: next to a dark-themed mobile
        /// game it looks foreign and unfinished, and on a touchscreen nobody
        /// uses it anyway.
        ///
        /// Dragging is turned on explicitly: once the bar is gone, the
        /// finger is the ONLY way to scroll, and the default threshold is
        /// tuned for a mouse.
        /// </summary>
        public static ScrollView Mobile(ScrollView v)
        {
            v.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            v.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            v.mode = ScrollViewMode.Vertical;

            v.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
            v.scrollDecelerationRate = 0.135f;
            v.elasticity = 0.1f;

            // AND SOMETHING HAS TO TAKE THE SCROLLBAR'S PLACE.
            //
            // Hiding the bar was right - it is a thick light-grey desktop
            // control with arrow buttons on it and it looked foreign in the
            // screenshot. But it was also the ONLY thing on screen saying
            // "there is more below", and nothing replaced it. What that cost,
            // measured on the shipped frames at 873 x 393:
            //
            //   settings        the hints button, the "Language" heading, ALL
            //                   FIVE language buttons and Back are below the
            //                   fold. A five-language game with no reachable
            //                   way to change language.
            //   cuisine choice  the SECOND cuisine and Back. The screen that
            //                   sells the game offers one of the two things it
            //                   is selling.
            //   main menu       with a save present, a fourth button pushes
            //                   the content 47 dp past the viewport.
            //   slot choice     slots 3 and 4 and Back.
            //
            // All four are draggable. None of them says so. The back key gets
            // the player out (UiRoot), so it is not a trap - but a control
            // nobody can find has not shipped.
            Fade(v);
            return v;
        }

        /// <summary>
        /// The bottom edge fade: the scrollbar's replacement.
        ///
        /// IT IS STACKED STRIPS, NOT A GRADIENT. UI Toolkit's C# style API has
        /// no gradient fill, and this project has a standing rule against
        /// reaching for a drawing API that "worked in the editor and did not
        /// show up in the build" - Icons.cs builds every icon in the game out
        /// of rectangles for exactly that reason. Six strips of the background
        /// colour at rising alpha read as a fade at 393 dp and cannot fail in
        /// a way the editor hides.
        ///
        /// IT ONLY APPEARS WHEN THERE IS SOMETHING BELOW. A fade on a screen
        /// that fits would be a lie in the other direction, and this project
        /// has enough of those. The check runs on GeometryChangedEvent, which
        /// is the only moment either height is known.
        ///
        /// `pickingMode = Ignore` on every part: the fade sits over the
        /// content and must never eat a press meant for the control beneath.
        /// </summary>
        private static void Fade(ScrollView v)
        {
            VisualElement fade = new VisualElement();
            fade.pickingMode = PickingMode.Ignore;
            fade.style.position = Position.Absolute;
            fade.style.left = 0;
            fade.style.right = 0;
            fade.style.bottom = 0;
            fade.style.height = 28;
            fade.style.display = DisplayStyle.None;

            const int steps = 6;
            for (int i = 0; i < steps; i++)
            {
                VisualElement strip = new VisualElement();
                strip.pickingMode = PickingMode.Ignore;
                strip.style.flexGrow = 1;
                strip.style.backgroundColor = new Color(
                    Bg.r, Bg.g, Bg.b, (i + 1) / (float)steps * 0.95f);
                fade.Add(strip);
            }
            // hierarchy.Add, NOT Add.
            //
            // ScrollView overrides Add() to put the child in its
            // contentContainer - so `v.Add(fade)` made the fade a piece of
            // CONTENT: it added its own 28 dp to the content height, scrolled
            // away with the content instead of staying pinned to the bottom
            // edge, and fed the GeometryChangedEvent below with a height it
            // had itself caused. A fade that makes the content taller, and
            // then measures the content to decide whether to show itself, is
            // a loop.
            v.hierarchy.Add(fade);

            v.contentContainer.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float content = v.contentContainer.resolvedStyle.height;
                float view = v.contentViewport.resolvedStyle.height;
                // A dp of slack: a content box that is a rounding error taller
                // than its viewport is not "more below".
                fade.style.display = (content > view + 1f)
                    ? DisplayStyle.Flex : DisplayStyle.None;
            });
        }

        /// <summary>A thin separating line.</summary>
        public static VisualElement Divider()
        {
            VisualElement v = new VisualElement();
            v.style.height = 1;
            v.style.backgroundColor = Line;
            return v;
        }

        /// <summary>A label and a value, pushed to opposite ends.</summary>
        public static VisualElement Field(string label, string value, Color? valueColor = null)
        {
            VisualElement row = Row(0);
            row.style.justifyContent = Justify.SpaceBetween;
            row.Add(Text(label, FontBody, InkDim));
            Label v = Text(value, FontBody, valueColor ?? Ink);
            v.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(v);
            return row;
        }

        /// <summary>Reputation colour: above 80 good, below 40 bad.</summary>
        /// <summary>
        /// The colour of reputation. THE STARTING VALUE IS NOT AN ALARM.
        ///
        /// The thresholds used to be 80/40, and a new campaign starts at
        /// 30.0 - so on day one, with nothing having happened yet, the most
        /// saturated element on the top strip was a red "Reputation 30.0".
        /// The first thing the player learned was a false alarm.
        ///
        /// The new thresholds leave the start NEUTRAL: 30 is grey, it turns
        /// red if it falls and green as it climbs. The colour now reports a
        /// DIRECTION rather than a STATE.
        /// </summary>
        public static Color ReputationColor(int centi)
        {
            if (centi >= 7000) return Good;
            if (centi >= 4500) return Warn;
            if (centi >= 2500) return InkDim;     // the starting band: quiet
            return Bad;
        }

        /// <summary>
        /// The same four bands, in inks that READ ON THE CREAM PLATE.
        ///
        /// `ReputationColor` above returns Good / Warn / InkDim / Bad, and
        /// every one of those was picked for the DARK panel. The reputation
        /// pill is on `Theme.Plate`, and the caller was painting the dark
        /// colours onto it: measured against the plate, InkDim gives 1.84:1,
        /// Good 2.00, Warn 1.52 and Bad 2.22. THERE IS NO VALUE OF REPUTATION
        /// AT WHICH THAT NUMBER IS LEGIBLE - the WCAG floor for text is 4.5.
        ///
        /// It looked like a disabled control, which is worse than merely
        /// faint: the player reads "you cannot use this" from a number that
        /// is one of the two things the whole top strip exists to show.
        ///
        /// The bands are deliberately the SAME bands. `ReputationColor`'s
        /// comment argues them at length - 30.0 is where a campaign starts,
        /// so the starting band has to be quiet or the first thing the player
        /// learns is a false alarm. Only the inks change.
        /// </summary>
        public static Color ReputationPlateColor(int centi)
        {
            if (centi >= 7000) return Kit.GoDeep;    // 5.0:1 on the plate
            if (centi >= 4500) return WarnDeep;      // 4.6:1
            if (centi >= 2500) return PlateInk;      // the starting band: quiet
            return Kit.BadDeep;                      // 5.4:1
        }

        /// <summary>
        /// The dark amber that reads on the plate. `Warn` is 1.52:1 there.
        /// The amber counterpart of `Kit.GoDeep` and `Kit.BadDeep`.
        /// </summary>
        public static readonly Color WarnDeep = new Color(0.502f, 0.333f, 0.055f);

        /// <summary>Till colour: negative is red.</summary>
        public static Color CashColor(long centi)
        {
            return centi < 0 ? Bad : Ink;
        }

        /// <summary>
        /// A single axis: name, filled bar, number - and optionally the
        /// difference against last week.
        ///
        /// The bar carries LENGTH, NOT COLOUR. For a colour-blind player
        /// there is no green-red distinction; length is the same for
        /// everyone.
        ///
        /// IT LIVES HERE BECAUSE TWO SCREENS USE IT: the year-end report
        /// card and the weekly one. Writing a second copy would be an
        /// invitation to the thing that has happened repeatedly in this
        /// project - two calculations drift apart one day and there is no
        /// telling which of them is right.
        /// </summary>
        /// <param name="delta">
        /// int.MinValue means the difference is NOT SHOWN (this is how the
        /// year-end screen calls it). Zero writes "unchanged" - leaving it
        /// blank would give the impression the measurement was never taken.
        /// </param>
        public static VisualElement AxisRow(string name, int value,
                                            int delta = int.MinValue)
        {
            VisualElement row = Row(Gap);
            row.style.alignItems = Align.Center;

            // THE NAME COLUMN IS NARROW, THE BAR WIDE.
            //
            // Going to two columns halves the row width and left the bar
            // about 40 dp - a hundred-point scale squeezed into 40 pixels,
            // half a pixel per point. The bar's entire justification is
            // LENGTH; once that cannot be read it carries nothing but
            // colour and its justification is gone.
            Label label = Text(name, FontSmall, InkDim);
            label.style.minWidth = 112;
            label.style.flexShrink = 0;
            row.Add(label);

            VisualElement track = new VisualElement();
            track.style.flexGrow = 1;
            track.style.minWidth = 90;
            track.style.height = 10;
            track.style.backgroundColor = PanelHi;
            Round(track, 5);

            VisualElement fill = new VisualElement();
            fill.style.width = Length.Percent(value);
            fill.style.height = 10;
            fill.style.backgroundColor = value >= 60 ? Good
                                       : value >= 35 ? Warn : Bad;
            Round(fill, 5);
            track.Add(fill);
            row.Add(track);

            Label num = Text(value.ToString(), FontSmall, Ink);
            num.style.minWidth = 32;
            num.style.flexShrink = 0;
            num.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(num);

            if (delta != int.MinValue)
            {
                // THE SIGN IS WRITTEN OUT: "+8" and "8" are different
                // things, and the minus sign alone is not enough - in a
                // column that never shows a plus, the reader may take every
                // number for an absolute value.
                string s = delta > 0 ? "+" + delta
                         : delta < 0 ? delta.ToString() : "—";
                Label d = Text(s, FontSmall,
                               delta > 0 ? Good : delta < 0 ? Bad : InkDim);
                d.style.minWidth = 36;
                d.style.flexShrink = 0;
                d.style.unityTextAlign = TextAnchor.MiddleRight;
                row.Add(d);
            }
            return row;
        }
    }
}
