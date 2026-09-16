using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// One screen. It builds its own tree and is cleaned up when closed.
    ///
    /// It is called "UiScreen", not "Screen": UnityEngine.Screen already
    /// exists and the same name would set two types against each other -
    /// I would have to spell out which one I meant in every file.
    /// </summary>
    public abstract class UiScreen
    {
        public UiRoot Ui { get; internal set; }
        public GameApp App { get { return Ui.App; } }

        /// <summary>Builds the screen's tree.</summary>
        public abstract VisualElement Build();

        /// <summary>Called every frame; only for the TOPMOST screen.</summary>
        public virtual void Tick() { }

        /// <summary>The back key. Returning false keeps the screen open.</summary>
        public virtual bool OnBack() { return true; }

        /// <summary>
        /// The screen HAS CLOSED - no matter how it closed.
        ///
        /// The back key, a button, or the stack being cleared; all three
        /// routes come through here. A screen that changes a global state
        /// (pausing, for instance) gives it back here. Trusting OnBack
        /// alone was not enough: the "Continue" button calls Pop directly
        /// and the pause was never lifted.
        /// </summary>
        public virtual void OnClosed() { }
    }

    /// <summary>
    /// The screen stack. Menu -> cuisine choice -> save slot -> game,
    /// stacked one on top of another, with the back key closing the
    /// topmost one.
    ///
    /// The reason it is a stack is Android: the hardware back key has to
    /// work on every screen and "back" always has to be THE PREVIOUS
    /// screen. In a design that writes out each transition by hand, that
    /// becomes something to be thought through again every single time.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiRoot : MonoBehaviour
    {
        public GameApp App;

        /// <summary>
        /// The font for the whole interface (Rubik, SIL OFL 1.1).
        ///
        /// Bound EXPLICITLY, not left to the theme. Unity's default
        /// runtime theme works IN THE EDITOR but could not resolve the
        /// font IN A BUILD: in the first desktop build the buttons were
        /// drawn with no text on them at all. Giving the font to the root
        /// element inherits down the whole tree and does not depend on
        /// the theme.
        /// </summary>
        public Font Font;

        /// <summary>
        /// The CJK font (Noto Sans SC, SIL OFL 1.1).
        ///
        /// Rubik carries Latin, Cyrillic, Hebrew and ARABIC - but not
        /// CJK. It failed to cover 765 characters of the Chinese table
        /// (tools/art/check_font.py measures this). A second font was
        /// added as a SUBSET: from a 10.5 MB font down to the 827
        /// characters the game actually uses -> 227 KB.
        ///
        /// When the language is Chinese the WHOLE tree is drawn with it,
        /// which is why the subset also carries the Latin letters, the
        /// digits and the currency glyph - otherwise "12 ¤" would be an
        /// empty box on the Chinese screen.
        /// </summary>
        public Font FontCJK;

        /// <summary>
        /// The font for the current language.
        ///
        /// THE DECISION LIVES IN THE LANGUAGE, NOT IN THE SCREEN: having
        /// each screen ask "am I Chinese" separately meant one screen
        /// would be forgotten.
        /// </summary>
        public Font FontForLanguage
        {
            get
            {
                if (Loc.LanguageCode == "zh" && FontCJK != null) return FontCJK;
                return Font;
            }
        }

        /// <summary>
        /// Sets the root element up for the current language: font, text
        /// direction and text generator.
        ///
        /// Called when the language changes. Giving it to the root element
        /// inherits down the whole tree - the screens do not touch it one
        /// by one. All three live AT THE ROOT because all three are
        /// properties of the language; leaving one of them to a screen
        /// means that screen gets forgotten.
        /// </summary>
        public void ApplyLanguage()
        {
            if (_root == null) return;

            Font f = FontForLanguage;
            if (f != null)
                _root.style.unityFontDefinition = FontDefinition.FromFont(f);

            // TEXT DIRECTION and TEXT GENERATOR.
            //
            // Arabic letters JOIN UP: the same letter takes a different
            // shape at the start, in the middle and at the end of a word.
            // The standard generator lays the letters out one by one and
            // left to right - what comes out is not Arabic, it is a list
            // of Arabic letters.
            //
            // The advanced generator (ATG) does the joining, the
            // bidirectional ordering and the line breaking. But it is
            // turned on FOR ARABIC ONLY: four languages work with the
            // standard generator and have been measured doing so. There is
            // nothing to be gained from risking the four that work for the
            // sake of the fifth.
            bool rtl = Loc.IsRightToLeft;
            _root.style.unityTextGenerator = rtl
                ? TextGeneratorType.Advanced
                : TextGeneratorType.Standard;
            _root.languageDirection = rtl
                ? LanguageDirection.RTL
                : LanguageDirection.LTR;
        }

        private readonly List<UiScreen> _stack = new List<UiScreen>();
        private readonly List<VisualElement> _views = new List<VisualElement>();
        private VisualElement _root;

        public UiScreen Top { get { return _stack.Count > 0 ? _stack[_stack.Count - 1] : null; } }


        /// <summary>The topmost screen's tree. Only this one is visible and clickable.</summary>
        public VisualElement TopView
        {
            get { return _views.Count > 0 ? _views[_views.Count - 1] : null; }
        }

        public int Depth { get { return _stack.Count; } }

        private void Awake()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.style.flexGrow = 1;
            _root.style.backgroundColor = Color.clear;

            if (FontForLanguage == null)
                Debug.LogWarning("No interface font bound; text may not appear.");
            ApplyLanguage();

            ApplySafeArea();
            _root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea());
        }

        private Rect _safe;

        /// <summary>
        /// Applies the device's SAFE AREA to the root element as padding.
        ///
        /// Held in landscape, the notch or camera cut-out is on the LEFT or
        /// RIGHT edge and typically eats 30-45 dp inwards. In the current
        /// layout the day and till sit at the far left and the menu button
        /// at the far right - all three would end up under the notch.
        /// Losing the menu button means the player cannot get out of the
        /// game.
        ///
        /// The conversion: the safe area is measured in PHYSICAL pixels,
        /// the panel in its own units. The ratio is the root's resolved
        /// width divided by the screen width.
        /// </summary>
        private void ApplySafeArea()
        {
            Rect safe = Screen.safeArea;
            if (safe == _safe) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            float w = _root.resolvedStyle.width;
            if (w <= 1f) return;                       // not laid out yet

            _safe = safe;
            float k = w / Screen.width;

            _root.style.paddingLeft = safe.xMin * k;
            _root.style.paddingRight = (Screen.width - safe.xMax) * k;
            _root.style.paddingTop = (Screen.height - safe.yMax) * k;
            _root.style.paddingBottom = safe.yMin * k;
        }

        private void Update()
        {
            Top?.Tick();

            // The Android back key, and Esc on the desktop.
            if (BackPressed() && _stack.Count > 0)
            {
                if (Top.OnBack()) Pop();
            }
        }

        /// <summary>
        /// Is there a clickable interface element at this screen point.
        ///
        /// The camera asks this: pressing a button on the action bar was
        /// also selecting the room behind it and flying the camera over
        /// there.
        ///
        /// The panel's own PICKER is used (Pick), not a rectangle
        /// calculation of our own: the bars' heights change with their
        /// content and a hand-written boundary would inevitably drift
        /// apart from them.
        /// </summary>
        public bool BlocksPoint(Vector2 screenPoint)
        {
            if (_root == null || _root.panel == null) return false;

            // The panel's y axis runs the OPPOSITE way to the screen's.
            Vector2 flipped = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            Vector2 local = RuntimePanelUtils.ScreenToPanel(_root.panel, flipped);

            VisualElement hit = _root.panel.Pick(local);
            if (hit == null) return false;

            // The root element itself is transparent and covers the whole
            // screen; counting it as a blocker would mean the hall could
            // never be touched at all.
            return hit != _root;
        }

        private static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        // ---------------------------------------------------------------------
        public void Push(UiScreen s)
        {
            // The screen underneath is HIDDEN, not merely covered over.
            //
            // At first a new screen was only added on top; the old tree
            // stayed where it was and its buttons were still pickable. That
            // is exactly why the self-driving tour pressed the wrong button
            // (the "Start" on the cuisine screen underneath). For a player,
            // keyboard/gamepad navigation could land on an invisible button
            // in the same way.
            //
            // The side benefit is measurable on mobile: a hidden tree is
            // neither laid out nor drawn.
            if (_views.Count > 0) _views[_views.Count - 1].style.display = DisplayStyle.None;

            s.Ui = this;
            VisualElement v = s.Build();
            Fill(v);

            _stack.Add(s);
            _views.Add(v);
            _root.Add(v);
            Enter(v);
        }

        /// <summary>
        /// The entrance motion for a screen being opened: opacity 0 -> 1
        /// and 12 px upwards from below.
        ///
        /// WHY BY HAND AND NOT WITH USS: this project has no stylesheet,
        /// the whole interface is built in C#. UI Toolkit's transitions
        /// are reachable through IStyle as well, so exactly the same thing
        /// can be done without UXML/USS.
        ///
        /// TRANSFORM AND OPACITY ONLY. Unity's own documentation says that
        /// transitioning LAYOUT properties such as width/height makes the
        /// layout be recalculated and drops the frame rate; translate,
        /// scale and opacity do not regenerate the geometry. On a low-end
        /// phone that is the only thing that matters.
        ///
        /// THE DURATIONS ARE SET AGAINST 30 fps. One frame is 33 ms; a
        /// 100 ms transition is three frames and in practice is not seen.
        /// We go one step up Material's scale: 220 ms in (decelerating),
        /// 140 ms out. That the entrance and the exit do not share a curve
        /// is a rule - what arrives settles by slowing down, what leaves
        /// goes by speeding up.
        /// </summary>
        private static void Enter(VisualElement v)
        {
            // usageHints is given UP FRONT: given after the transition has
            // started, Unity regenerates the draw data for the whole
            // subtree on that frame.
            v.usageHints |= UsageHints.DynamicTransform;

            v.style.opacity = 0f;
            v.style.translate = new Translate(0, 12);

            v.schedule.Execute(() =>
            {
                v.style.transitionProperty =
                    new List<StylePropertyName> { "opacity", "translate" };
                v.style.transitionDuration =
                    new List<TimeValue> { new TimeValue(220, TimeUnit.Millisecond),
                                          new TimeValue(220, TimeUnit.Millisecond) };
                v.style.transitionTimingFunction =
                    new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutCubic),
                                               new EasingFunction(EasingMode.EaseOutCubic) };
                v.style.opacity = 1f;
                v.style.translate = new Translate(0, 0);
            }).StartingIn(0);
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;
            int i = _stack.Count - 1;
            _stack[i].OnClosed();

            // THE CLOSING SCREEN LEAVES THE TREE IMMEDIATELY; what fades
            // away is only what you see.
            //
            // DEFERRING the removal to the end of the transition would be
            // wrong: as long as the screen is still in the tree its buttons
            // stay pickable, and the tour lived through exactly this once
            // (a button on the screen underneath was pressed). So the real
            // element leaves and no fading COPY is put in its place - the
            // plain and honest thing is to show the exit through the
            // ENTRANCE of the screen underneath.
            VisualElement leaving = _views[i];
            _root.Remove(leaving);
            _views.RemoveAt(i);
            _stack.RemoveAt(i);

            if (_views.Count > 0)
            {
                VisualElement below = _views[_views.Count - 1];
                below.style.display = DisplayStyle.Flex;
                Enter(below);
            }
        }

        private static void Fill(VisualElement v)
        {
            v.style.position = Position.Absolute;
            v.style.left = 0;
            v.style.right = 0;
            v.style.top = 0;
            v.style.bottom = 0;
            v.style.display = DisplayStyle.Flex;
        }

        /// <summary>Empties the stack and starts again with a single screen.</summary>
        public void Replace(UiScreen s)
        {
            while (_stack.Count > 0) Pop();
            Push(s);
        }

        /// <summary>Rebuilds the top screen. For when the data changes.</summary>
        public void Refresh()
        {
            if (_stack.Count == 0) return;
            UiScreen s = Top;
            int i = _stack.Count - 1;
            _root.Remove(_views[i]);

            VisualElement v = s.Build();
            Fill(v);
            _views[i] = v;
            _root.Add(v);
        }
    }
}
