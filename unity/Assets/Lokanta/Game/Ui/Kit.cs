using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// THE PIECES OF THE IN-GAME INTERFACE: pill, card, icon button, action.
    ///
    /// Theme is where the "tokens" live (colour, measure, button); Kit is
    /// the GAME SCREEN's own style - rounded dark cards, the pills the
    /// numbers sit in, the icon buttons along the bottom and the single
    /// green action at the bottom right.
    ///
    /// Why a separate file: this style is used on the game screen
    /// (GameScreen) and nowhere else. The menu, market and staff screens
    /// are list screens and their layout is Theme itself. Putting the two
    /// in the same file would make the word "button" mean two different
    /// things.
    ///
    /// THE ICONS ARE DRAWN, THEY DO NOT COME FROM THE FONT.
    ///
    /// This project learned that once: Rubik has no star glyph and the
    /// player saw an EMPTY BOX instead (tools/art/check_font.py caught
    /// it). An icon cannot be entrusted to a font - all of them are built
    /// out of rectangles, circles and rotation.
    /// </summary>
    public static class Kit
    {
        // --- colour ------------------------------------------------------
        /// <summary>The cards' background. Translucent, so the hall behind shows through.</summary>
        public static readonly Color CardBg = new Color(0.071f, 0.086f, 0.118f, 0.90f);

        /// <summary>The card edge. On a dark background it says where the card ends.</summary>
        public static readonly Color CardLine = new Color(0.212f, 0.251f, 0.318f, 1f);

        /// <summary>
        /// ACTION GREEN: "the one button that moves the game on".
        ///
        /// The copper accent (Theme.Accent) was doing eight separate jobs -
        /// "do this", "this one is open", "you can afford it", "careful".
        /// The phase button (open service / close the day) is a different
        /// thing from all of them: it is the day's DOOR. It deserves a
        /// colour of its own, and that colour is the same in every game:
        /// green = go on.
        ///
        /// More saturated than Theme.Good (0.43/0.70/0.45): Good reports a
        /// STATE ("you are in profit"), this one calls for an ACTION.
        /// </summary>
        public static readonly Color Go = new Color(0.180f, 0.729f, 0.357f);

        /// <summary>The dark green that reads on a light card.</summary>
        public static readonly Color GoDeep = new Color(0.086f, 0.478f, 0.208f);

        /// <summary>
        /// NAVIGATION BLUE: "go in here".
        ///
        /// The reference's bottom-left group of four is blue. That splits
        /// the game's colour language into three roles, and each role says
        /// ONE thing:
        ///
        ///   blue   - opens a screen (Market, Menu, Crew, Equipment)
        ///   green  - moves the game on (open service, close the day)
        ///   copper - attention and value (allowance left, reputation, the
        ///            day bar)
        ///
        /// They all used to be the same grey, and nothing on the screen
        /// answered the player's question of "what should I do first".
        /// </summary>
        public static readonly Color Nav = new Color(0.145f, 0.384f, 0.851f);

        /// <summary>Coin yellow. The icon on the till pill.</summary>
        public static readonly Color Coin = new Color(1.000f, 0.784f, 0.251f);

        /// <summary>The reputation gem. A separate colour from the till, because it is a separate resource.</summary>
        public static readonly Color Gem = new Color(0.639f, 0.451f, 0.941f);

        public const int CardRadius = 14;

        /// <summary>
        /// Pill height. 42 -> 38: the top strip eats into the hall's share
        /// too, and the plate inside the pill is not a touch target anyway -
        /// only the menu button is touched, and that uses the pill height
        /// together with its own Touch floor.
        /// </summary>
        public const int PillHeight = 38;

        // =====================================================================
        // BOXES
        // =====================================================================

        /// <summary>The rounded dark box: the body of every card and every pill.</summary>
        public static VisualElement Box(float radius = CardRadius)
        {
            VisualElement v = new VisualElement();
            v.style.backgroundColor = CardBg;
            v.style.borderTopWidth = 1;
            v.style.borderBottomWidth = 1;
            v.style.borderLeftWidth = 1;
            v.style.borderRightWidth = 1;
            v.style.borderTopColor = CardLine;
            v.style.borderBottomColor = CardLine;
            v.style.borderLeftColor = CardLine;
            v.style.borderRightColor = CardLine;
            Theme.Round(v, radius);
            return v;
        }

        /// <summary>
        /// THE NUMBER PILL: icon on the left, number on the right.
        ///
        /// The number sits ON A LIGHT PLATE (docs/16): the thing the player
        /// reads every three seconds is the till figure, and the light
        /// plate makes it the highest-contrast object on the screen WITHOUT
        /// SPENDING an accent colour on it.
        /// </summary>
        public static VisualElement Pill(VisualElement icon, Label value,
                                         bool plate = true)
        {
            VisualElement box = Box(PillHeight * 0.5f);
            box.style.flexDirection = Theme.RowFlow;
            box.style.alignItems = Align.Center;
            box.style.height = PillHeight;
            box.style.paddingLeft = 6;
            box.style.paddingRight = plate ? 6 : 14;
            box.style.flexShrink = 0;

            if (icon != null) box.Add(icon);

            value.style.unityFontStyleAndWeight = FontStyle.Bold;
            value.style.unityTextAlign = TextAnchor.MiddleCenter;

            if (plate)
            {
                VisualElement p = new VisualElement();
                p.style.backgroundColor = Theme.Plate;
                p.style.paddingLeft = 10;
                p.style.paddingRight = 10;
                p.style.paddingTop = 3;
                p.style.paddingBottom = 3;
                p.style.marginLeft = 6;
                p.style.justifyContent = Justify.Center;
                Theme.Round(p, 9);
                value.style.color = Theme.PlateInk;
                p.Add(value);
                box.Add(p);
            }
            else
            {
                value.style.marginLeft = 8;
                box.Add(value);
            }
            return box;
        }

        /// <summary>
        /// A CARD WITH A HEADING. The body of the left and right panels in
        /// the reference.
        ///
        /// The heading is SMALL AND MUTED, the content bright: the card
        /// itself is not the information, the number inside it is.
        /// </summary>
        public static VisualElement Card(string title, out VisualElement body)
        {
            // THE CARD IS LIGHT, EVERYTHING AROUND IT DARK.
            //
            // In the reference the "Daily Goals" panel is pure white and
            // the rest is dark; that is not decoration but an ORDERING: the
            // lightest object on the screen becomes the thing the player is
            // meant to read first. docs/16 already wrote the same rule down
            // for numbers (the light plate); here it is applied to the whole
            // card.
            VisualElement box = new VisualElement();
            box.style.backgroundColor = Theme.Plate;
            Theme.Round(box, CardRadius);
            box.style.paddingLeft = 12;
            box.style.paddingRight = 12;
            box.style.paddingTop = 9;
            box.style.paddingBottom = 9;

            if (!string.IsNullOrEmpty(title))
            {
                Label t = Theme.Text(title, Theme.FontSmall, Theme.PlateDim);
                t.style.unityFontStyleAndWeight = FontStyle.Bold;
                t.style.marginBottom = 5;
                box.Add(t);
            }

            body = new VisualElement();
            box.Add(body);
            return box;
        }

        /// <summary>
        /// A STAT CARD: icon on the left, heading plus a big number on the
        /// right.
        ///
        /// The "Hourly Revenue" and "Customer Satisfaction" cards in the
        /// reference. This game's equivalents are TAKINGS and SATISFACTION
        /// - both already exist in the core and until now appeared only in
        /// the EVENING report, so the player could not see how the day was
        /// going while it went.
        /// </summary>
        public static VisualElement Stat(VisualElement icon, string title,
                                         Label value)
        {
            VisualElement box = Box();
            box.style.flexDirection = Theme.RowFlow;
            box.style.alignItems = Align.Center;
            box.style.paddingLeft = 10;
            box.style.paddingRight = 12;
            box.style.paddingTop = 7;
            box.style.paddingBottom = 7;
            box.style.minWidth = 148;

            if (icon != null)
            {
                icon.style.marginRight = 9;
                box.Add(icon);
            }

            VisualElement col = new VisualElement();
            Label t = Theme.Text(title, Theme.FontSmall, Theme.InkFaint);
            col.Add(t);
            value.style.fontSize = 21;
            value.style.unityFontStyleAndWeight = FontStyle.Bold;
            col.Add(value);
            box.Add(col);
            return box;
        }

        /// <summary>
        /// A PROGRESS BAR. The equivalent of the reference's experience bar
        /// is the SERVICE DAY: there was nowhere the player could read off
        /// how much of the day had gone.
        /// </summary>
        public static VisualElement Meter(out VisualElement fill,
                                          float width = 150f, float height = 10f)
        {
            VisualElement track = new VisualElement();
            track.style.width = width;
            track.style.height = height;
            track.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
            track.style.overflow = Overflow.Hidden;
            Theme.Round(track, height * 0.5f);

            fill = new VisualElement();
            fill.style.height = height;
            fill.style.width = Length.Percent(0);
            fill.style.backgroundColor = Theme.Accent;
            Theme.Round(fill, height * 0.5f);
            track.Add(fill);
            return track;
        }

        /// <summary>
        /// THE DAY BADGE: an octagonal plate with the day number in it.
        ///
        /// The reference's level badge; this game has no levels, it has
        /// DAYS - and in a sixty-day campaign which day you are on is the
        /// single number that carries the most meaning on its own.
        /// </summary>
        public static VisualElement Badge(Label number)
        {
            VisualElement box = new VisualElement();
            box.style.width = 46;
            box.style.height = 46;
            box.style.backgroundColor = Theme.Accent;
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;
            box.style.flexShrink = 0;
            box.style.borderTopWidth = 2;
            box.style.borderBottomWidth = 2;
            box.style.borderLeftWidth = 2;
            box.style.borderRightWidth = 2;
            Color edge = new Color(1f, 0.83f, 0.55f);
            box.style.borderTopColor = edge;
            box.style.borderBottomColor = edge;
            box.style.borderLeftColor = edge;
            box.style.borderRightColor = edge;
            Theme.Round(box, 13);

            number.style.fontSize = 20;
            number.style.unityFontStyleAndWeight = FontStyle.Bold;
            number.style.color = new Color(0.14f, 0.10f, 0.05f);
            box.Add(number);
            return box;
        }

        // =====================================================================
        // BUTTONS
        // =====================================================================

        /// <summary>
        /// AN ICON BUTTON: icon on top, label underneath.
        ///
        /// The reference's bottom-left group of four. The icon ALONE will
        /// not do - the player cannot tell whether a trolley icon means
        /// "Market" or "Shop"; text alone will not do either, because four
        /// grey rectangles can only be told apart by reading them. Both
        /// together: the silhouette distinguishes, the text tells.
        /// </summary>
        public static Button IconButton(VisualElement icon, string label,
                                        System.Action onClick, bool blue = false)
        {
            Button b = new Button(() => { Sfx.Click(); onClick?.Invoke(); });
            b.text = string.Empty;
            b.style.flexDirection = FlexDirection.Column;
            b.style.alignItems = Align.Center;
            b.style.justifyContent = Justify.Center;
            // 62 -> 54: THE STRIPS ARE EATING THE HALL.
            //
            // Measured: the top and bottom strips came to 156 dp on a 393
            // screen - so the interface takes 40% of the screen and the
            // camera has to fit into the remaining 60%. Most of the "far
            // from the reference" feeling came from this, not from the floor
            // plan (a frame with no interface shows the building filling
            // the screen).
            //
            // 54 dp is still above Google's 48 dp minimum.
            b.style.minHeight = 54;
            b.style.minWidth = 72;
            b.style.paddingLeft = 7;
            b.style.paddingRight = 7;
            b.style.paddingTop = 4;
            b.style.paddingBottom = 4;
            b.style.marginLeft = 0;
            b.style.marginRight = 0;
            b.style.marginTop = 0;
            b.style.marginBottom = 0;
            Color face = blue ? Nav : CardBg;
            b.style.backgroundColor = face;
            b.style.borderTopWidth = blue ? 0 : 1;
            b.style.borderBottomWidth = blue ? 0 : 1;
            b.style.borderLeftWidth = blue ? 0 : 1;
            b.style.borderRightWidth = blue ? 0 : 1;
            b.style.borderTopColor = CardLine;
            b.style.borderBottomColor = CardLine;
            b.style.borderLeftColor = CardLine;
            b.style.borderRightColor = CardLine;
            Theme.Round(b, CardRadius);

            if (icon != null)
            {
                icon.style.marginBottom = 4;
                b.Add(icon);
            }
            Label t = Theme.Text(label, Theme.FontSmall,
                                 blue ? Color.white : Theme.Ink);
            t.style.unityTextAlign = TextAnchor.MiddleCenter;
            b.Add(t);

            Theme.PressFx(b, face);
            return b;
        }

        /// <summary>
        /// THE PHASE BUTTON: bottom right, green, two lines.
        ///
        /// At every phase of the game there is exactly ONE "move on"
        /// action - open service in the morning, close the day when service
        /// ends, go to the next day in the evening. The reference's bottom
        /// right button is precisely this, and in the game up to now it was
        /// the same size as the other six buttons and got lost among them.
        ///
        /// The second line says WHAT WILL HAPPEN ("a new day begins"): the
        /// one sentence the player can read before pressing a decision that
        /// has no way back.
        /// </summary>
        public static Button Cta(string title, string sub, System.Action onClick,
                                 bool ready = true)
        {
            // THE FACE COLOUR WAS DARKENED.
            //
            // Measured: white on `Go` gave 2.54:1 for the bold 19 dp title
            // (the threshold for bold 19 dp is 3:1) and about 2.2:1 for the
            // 82%-transparent second line (threshold 4.5:1). This is the
            // button the player presses more than any other across sixty
            // days, and its second line is the one sentence explaining a
            // decision that has no way back.
            //
            // White on `GoDeep` is 7.0:1 - past both thresholds. `Go`
            // remains and is used for the small green actions (there is no
            // text on those, only a white icon).
            Color face = ready ? GoDeep : Theme.PanelHi;
            Button b = new Button(() => { Sfx.Click(); onClick?.Invoke(); });
            b.text = string.Empty;
            b.style.flexDirection = Theme.RowFlow;
            b.style.alignItems = Align.Center;
            b.style.justifyContent = Justify.Center;
            b.style.minHeight = 54;
            b.style.paddingLeft = 14;
            b.style.paddingRight = 18;
            b.style.paddingTop = 4;
            b.style.paddingBottom = 4;
            b.style.marginLeft = 0;
            b.style.marginRight = 0;
            b.style.marginTop = 0;
            b.style.marginBottom = 0;
            b.style.backgroundColor = face;
            b.style.borderTopWidth = 0;
            b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0;
            b.style.borderRightWidth = 0;
            Theme.Round(b, CardRadius);

            // A DOUBLE ARROW: the reference's "fast forward" mark. A single
            // triangle means "play"; this button does not PLAY the game, it
            // moves the day on.
            VisualElement arrows = new VisualElement();
            arrows.style.flexDirection = Theme.RowFlow;
            arrows.style.marginRight = 12;
            Color arrowColor = ready ? Color.white : Theme.InkDim;
            arrows.Add(Icons.Play(arrowColor, 18f));
            VisualElement second = Icons.Play(arrowColor, 18f);
            second.style.marginLeft = -6;
            arrows.Add(second);
            b.Add(arrows);

            VisualElement col = new VisualElement();
            Label t = Theme.Text(title, 19, ready ? Color.white : Theme.Ink);
            t.style.unityFontStyleAndWeight = FontStyle.Bold;
            col.Add(t);
            if (!string.IsNullOrEmpty(sub))
            {
                // THE TRANSPARENCY IS GONE: 82% white pushed the contrast
                // below the threshold, and all it bought was a "secondary"
                // look. Secondariness is already carried by the type size.
                Label s = Theme.Text(sub, Theme.FontSmall,
                                     ready ? Color.white : Theme.InkDim);
                col.Add(s);
            }
            b.Add(col);

            Theme.PressFx(b, face);
            return b;
        }

        /// <summary>
        /// The small green action beside the pill (the "+" in the
        /// reference).
        ///
        /// There is NOTHING to buy in this game - this button opens the
        /// loan, that is, "if the till is short you can look in here".
        /// Rather than imitating something the game does not have, put
        /// there what it does.
        /// </summary>
        public static Button PillAction(System.Action onClick, string tip)
        {
            Button b = new Button(() => { Sfx.Click(); onClick?.Invoke(); });
            b.text = string.Empty;
            b.tooltip = tip;
            // THE TOUCH TARGET IS NOT 30 DP.
            //
            // The project's own measure is 52 dp (Theme.Touch) and Google's
            // floor is 48. This button sits next to the till pill, close to
            // the screen edge, and it is the only way out for a player who
            // has run out of money - the most expensive button there is to
            // miss. On top of that its meaning was carried only by a
            // `tooltip`, and A TOOLTIP NEVER APPEARS ON A TOUCHSCREEN.
            //
            // THE VISUAL STAYS 30, THE TOUCH AREA IS 52.
            //
            // Doing this with `minWidth` DOES NOT WORK: in UI Toolkit
            // minWidth OVERRIDES width, so the button really would grow to
            // 52x52 and the small green action would turn into a huge
            // square. The right way: the outer button is 52x52 and
            // TRANSPARENT, with a 30x30 green face inside it.
            b.style.width = Theme.Touch;
            b.style.height = Theme.Touch;
            b.style.backgroundColor = Color.clear;
            b.style.marginLeft = 0;
            b.style.marginRight = 0;
            b.style.marginTop = 0;
            b.style.marginBottom = 0;
            b.style.paddingLeft = 0;
            b.style.paddingRight = 0;
            b.style.paddingTop = 0;
            b.style.paddingBottom = 0;
            b.style.alignItems = Align.Center;
            b.style.justifyContent = Justify.Center;
            b.style.borderTopWidth = 0;
            b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0;
            b.style.borderRightWidth = 0;

            // The VISIBLE part: a 30x30 green square in the middle of the
            // 52x52 transparent area. The press effect lives here too - had
            // it been on the outer button, a 52x52 green square would flash
            // on every touch.
            VisualElement face = new VisualElement();
            face.style.width = 30;
            face.style.height = 30;
            face.style.backgroundColor = Go;
            face.style.alignItems = Align.Center;
            face.style.justifyContent = Justify.Center;
            Theme.Round(face, 9);
            face.Add(Icons.Plus(Color.white, 14f));

            // The inner part MUST NOT SWALLOW the pick: the click has to
            // reach the 52x52 outer button, otherwise the enlarged touch
            // area would do nothing.
            face.pickingMode = PickingMode.Ignore;
            b.Add(face);

            // THE PRESS EFFECT IS TRIGGERED OUTSIDE AND SHOWN INSIDE.
            //
            // Theme.PressFx paints its own element; here the element that
            // listens and the element that is painted are DIFFERENT - the
            // inner part takes no picks and the outer button is
            // transparent. So these two lines are written out by hand.
            Color pressed = new Color(Go.r * 0.78f, Go.g * 0.78f, Go.b * 0.78f);
            b.RegisterCallback<PointerDownEvent>(_ =>
            {
                face.style.backgroundColor = pressed;
                face.style.scale = new Scale(new Vector3(0.94f, 0.94f, 1f));
            });
            EventCallback<EventBase> release = _ =>
            {
                face.style.backgroundColor = Go;
                face.style.scale = new Scale(Vector3.one);
            };
            b.RegisterCallback<PointerUpEvent>(e => release(e));
            b.RegisterCallback<PointerLeaveEvent>(e => release(e));
            b.RegisterCallback<PointerCancelEvent>(e => release(e));
            return b;
        }

        // =====================================================================
        // ROWS
        // =====================================================================

        /// <summary>
        /// A CHECK ROW: a checkbox plus text. The reference's "Daily Goals".
        ///
        /// This game has no task list; it has the morning's CHECKLIST
        /// (menu, stock, cook) and, during service, THE SHAPE OF THE DAY.
        /// Both already existed - one as a single line of small text, the
        /// other as three numbers crammed into the top strip.
        /// </summary>
        public static VisualElement CheckRow(bool ok, string text, Color? color = null)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = Theme.RowFlow;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 3;
            row.Add(Icons.Check(ok, color ?? (ok ? GoDeep : Theme.Bad), 16f));
            Label t = Theme.Text(text, Theme.FontSmall,
                                 ok ? Theme.PlateInk : Theme.Bad);
            if (!ok) t.style.unityFontStyleAndWeight = FontStyle.Bold;
            t.style.marginLeft = 7;
            row.Add(t);
            return row;
        }

        /// <summary>
        /// Inside a card: label on the left, number on the right.
        ///
        /// THE NUMBER'S COLOUR IS DECIDED BY THIS METHOD, NOT BY THE CALLER.
        ///
        /// The caller was building the label with `Theme.Ink`
        /// (0.94/0.93/0.90) and the card's background is `Theme.Plate`
        /// (0.914/0.898/0.863): measured, that is a contrast of 1.08:1. So
        /// the three numbers on the "Today" card - served, angry, tables
        /// occupied - were WHITE ON CREAM for the whole service. `Theme.Bad`
        /// for the angry state was 2.22:1.
        ///
        /// The CheckRow on the same card was doing it right (PlateInk), so
        /// half the card was readable - which is why the fault was not
        /// caught by eye. As long as the colour choice is left to the
        /// caller this class of bug reopens; the decision now lives here.
        /// </summary>
        /// <param name="warn">Should the number draw attention (an angry guest, say).</param>
        public static VisualElement CountRow(VisualElement dot, string label,
                                             Label value, bool warn = false)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = Theme.RowFlow;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 3;
            if (dot != null) row.Add(dot);

            Label t = Theme.Text(label, Theme.FontSmall, Theme.PlateDim);
            t.style.flexGrow = 1;
            row.Add(t);

            // LIGHT BACKGROUND -> DARK INK. BadDeep is 5.4:1 on Plate;
            // Theme.Bad was 2.22:1.
            value.style.color = warn ? BadDeep : Theme.PlateInk;
            value.style.unityFontStyleAndWeight = FontStyle.Bold;
            value.style.marginLeft = 10;
            row.Add(value);
            return row;
        }

        /// <summary>
        /// The DARK red that reads on a light background.
        ///
        /// Theme.Bad was chosen for the dark panel; on the light "plate"
        /// background it gives 2.22:1 and the WCAG floor is 4.5:1. The red
        /// counterpart of GoDeep.
        /// </summary>
        public static readonly Color BadDeep = new Color(0.647f, 0.114f, 0.114f);
    }
}
