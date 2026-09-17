// WARNING: THIS FILE'S COMMENTS WERE LOST.
//
// 13 September 2026: a script writing to the file was interrupted and
// the file system filled all 130,716 bytes with NUL - the source was
// destroyed completely. The project had no version control, no shadow
// copy and no file history.
//
// The logic was recovered by decompiling
// Library/ScriptAssemblies/Lokanta.Game.dll, which had been built
// seven minutes before the corruption (ilspycmd). What was recovered
// is the BEHAVIOUR; all of the comments were gone.
//
// Most of the lost reasoning lives SOMEWHERE ELSE and can be read
// there: docs/43-review-and-measurement.md (the tour's whole
// measurement story) and docs/45-design-review.md. The rewritten
// comments below are being added starting from the ones whose source
// is known.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Lokanta.Game.Ui;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Lokanta.Game
{
    public sealed class Autopilot : MonoBehaviour
    {
        public const string Flag = "-lokanta-tour";

        private const string OutFlag = "-lokanta-out";

        private GameApp _app;

        private UIDocument _doc;

        private string _dir;

        private int _shot;

        private readonly Dictionary<string, string> _shotHash = new Dictionary<string, string>();

        private string _shotDup;

        private int _stories;

        private readonly List<string> _log = new List<string>();

        private int _crisisSeen;

        private int _selectedTable = -1;

        private bool _quitting;

        private int _passed;

        private int _failed;

        private int _unmeasured;

        private int _occupiedMost;

        public static bool Requested
        {
            get
            {
                string[] commandLineArgs = Environment.GetCommandLineArgs();
                for (int i = 0; i < commandLineArgs.Length; i++)
                {
                    if (commandLineArgs[i] == "-lokanta-tour")
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void Begin(GameApp app, UIDocument doc)
        {
            _app = app;
            _doc = doc;
            _dir = OutDir();
            Directory.CreateDirectory(_dir);
            MatchPhoneDp();
            ApplyLanguageFlag();
            if (ScaleFlag() > 1.01f)
            {
                Hints.MarkAllSeen();
            }
            else
            {
                Hints.Reset();
            }
            Application.runInBackground = true;
            GameApp.SmokeTour = true;
            ((MonoBehaviour)this).StartCoroutine(Tour());
        }

        private void MatchPhoneDp()
        {
            if (!(_doc == null) && !(_doc.panelSettings == null))
            {
                float num = ScaleFlag();
                float num2 = ((Screen.dpi > 1f) ? Screen.dpi : 96f);
                _doc.panelSettings.referenceDpi = num2 / num;
                _doc.panelSettings.fallbackDpi = num2 / num;
                Debug.Log((object)("  tour scale : " + num.ToString("0.##") + " pixels = 1 dp (screen " + Screen.width + "x" + Screen.height + ", density " + num2.ToString("0") + ", so " + ((float)Screen.width / num).ToString("0") + "x" + ((float)Screen.height / num).ToString("0") + " dp)"));
            }
        }

        /// <summary>
        /// WHICH CUISINE IS PLAYED. 0 fast food, 1 the Turkish restaurant.
        ///
        /// The tour chose the cuisine as a FIXED 1 (Turkish). So the look of
        /// the fast food cuisine - its own wall, its palette, its floor
        /// pattern, the colour of its outside - became something the tour
        /// NEVER saw. docs/48-49 counts how many bugs of this same family have
        /// come out of this project: a check that does not run looks exactly
        /// like a check that passes.
        ///
        /// The default was left at 1: if the flag is not given the behaviour
        /// is exactly what it was, so the existing runs and screenshots do not
        /// shift.
        /// </summary>
        private static int CuisineFlag()
        {
            string[] a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] != "-lokanta-cuisine") continue;
                string v = a[i + 1];
                if (v == "fastfood") return 0;
                if (v == "turk") return 1;
                Debug.LogWarning("Tour: could not read the -lokanta-cuisine value ("
                                 + v + "), using turk");
                return 1;
            }
            return 1;
        }

        /// <summary>
        /// -lokanta-lang: which language the tour runs in.
        ///
        /// WHY IT HAD TO EXIST. The game opens in the device's preferred
        /// language, falling back to English; this machine has Turkish saved
        /// in PlayerPrefs, so every screenshot the store tour has ever
        /// produced is a TURKISH interface. The default Play listing is
        /// English. An English-speaking visitor would have opened the listing
        /// and seen "Servis" ("Service"), "Bugun"
        /// ("Today") and "Gunu kapat" ("Close the day").
        ///
        /// Nothing was wrong with the game, the tour or the translation. The
        /// screenshots simply inherited the developer's own preference,
        /// silently, because there was no way to say otherwise.
        ///
        /// It uses `Loc.UseLanguage`, which applies WITHOUT persisting: a tour
        /// run must not leave the developer's own language changed behind it.
        /// With no flag nothing happens at all and the tour behaves exactly as
        /// it did, so the measurement runs are unaffected.
        /// </summary>
        private static void ApplyLanguageFlag()
        {
            int index = LanguageFlag();
            if (index == Loc.Language) return;
            Loc.UseLanguage(index);
            Debug.Log("  tour language: " + Loc.LanguageCode);
        }

        /// <summary>
        /// The language index the tour should be in: the flag's, or whatever
        /// is already current when there is no flag.
        ///
        /// It is a QUERY rather than a stored field because the tour changes
        /// the language several times on purpose - the number-format check,
        /// the five-language strip check - and each of those has to put back
        /// the language the RUN asked for, not the one the machine had saved.
        /// A single place to ask stops the next one getting it wrong.
        /// </summary>
        private static int LanguageFlag()
        {
            string[] a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] != "-lokanta-lang") continue;
                int index = Loc.IndexOf(a[i + 1]);
                if (index >= 0) return index;
                Debug.LogWarning("Tour: unknown -lokanta-lang value ("
                                 + a[i + 1] + "), leaving the language alone");
                return Loc.Language;
            }
            return Loc.Language;
        }

        private static float ScaleFlag()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length - 1; i++)
            {
                if (!(commandLineArgs[i] != "-lokanta-scale"))
                {
                    if (float.TryParse(commandLineArgs[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && result >= 0.5f && result <= 6f)
                    {
                        return result;
                    }
                    Debug.LogWarning((object)("Tour: could not read the -lokanta-scale value (" + commandLineArgs[i + 1] + "), using 1"));
                    return 1f;
                }
            }
            return 1f;
        }

        private static float Luma(Color c)
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_0011: Unknown result type (might be due to invalid IL or missing references)
            //IL_001e: Unknown result type (might be due to invalid IL or missing references)
            return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        }

        private static float CurrentRenderScale()
        {
            UniversalRenderPipelineAsset universalRenderPipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (!(universalRenderPipelineAsset != null))
            {
                return -1f;
            }
            return universalRenderPipelineAsset.renderScale;
        }

        private bool StillOccupied(int t)
        {
            if (t < 0 || t >= _app.Sim.TableCount)
            {
                return false;
            }
            CustomerStage customerStage = _app.Sim.TableStage(t);
            if (customerStage != CustomerStage.None && customerStage != CustomerStage.Done)
            {
                return customerStage != CustomerStage.LeftAngry;
            }
            return false;
        }

        private int OccupiedTable()
        {
            for (int i = 0; i < _app.Sim.TableCount; i++)
            {
                CustomerStage customerStage = _app.Sim.TableStage(i);
                if (customerStage != CustomerStage.None && customerStage != CustomerStage.Done && customerStage != CustomerStage.LeftAngry)
                {
                    return i;
                }
            }
            return -1;
        }

        private static string OutDir()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length - 1; i++)
            {
                if (commandLineArgs[i] == "-lokanta-out")
                {
                    return commandLineArgs[i + 1];
                }
            }
            return Path.Combine(Application.dataPath, "..", "render");
        }

        private IEnumerator Tour()
        {
            // ARE ALL FIVE OF THE TABLES LOADED?
            //
            // There used to be two languages and the two were compared. With
            // five languages "two of them work" says nothing: the missing one
            // could be the third. Three things are looked for together - did
            // the table load (the same number of keys), was the text REALLY
            // translated (different from the previous language) and was the
            // key found (no square brackets). Without all three a language
            // does not count as "there".
            //
            // UseLanguage: the tour DOES NOT WRITE the player's choice of
            // language TO DISK.
            string previousText = null;
            int previousCount = -1;
            bool languagesOk = true;
            bool culturesOk = true;
            string cultureSummary = string.Empty;
            string languageSummary = string.Empty;
            for (int d = 0; d < Loc.Languages.Length; d++)
            {
                Loc.UseLanguage(d);

                // WAS THE CULTURE REALLY BUILT?
                //
                // CultureInfo depends on the ICU data on the device and that
                // data can be stripped. Loc drops a culture it cannot build to
                // the invariant culture - the game opens but the numbers are in
                // the wrong format. The fall back may not happen on the desktop
                // but on a phone; let it at least be visible here.
                System.Globalization.CultureInfo k = Loc.Culture;
                if (k == null || k.Equals(System.Globalization.CultureInfo.InvariantCulture))
                    culturesOk = false;
                cultureSummary += (d > 0 ? ", " : string.Empty)
                               + Loc.Languages[d] + ":" + (k == null ? "none" : k.Name);

                string m = Loc.T("ui.menu.new");
                int n = Loc.Count;
                bool good = !m.StartsWith("[")
                             && (previousText == null || m != previousText)
                             && (previousCount < 0 || n == previousCount);
                if (!good) languagesOk = false;
                languageSummary += (d > 0 ? ", " : string.Empty) + Loc.Languages[d] + ":" + m;
                previousText = m;
                previousCount = n;
            }
            Note(languagesOk, "All five languages' text loaded (" + previousCount + " keys - " + languageSummary + ")");
            Note(culturesOk, "All five languages' number format built (" + cultureSummary + ")");

            // The number format follows the language too: Turkish 8.000, English 8,000.
            Loc.UseLanguage(1);
            string text3 = Loc.Money(800000L);
            Loc.UseLanguage(0);
            string text4 = Loc.Money(800000L);
            Note(text3 != text4, "The number format follows the language (" + text4 + " / " + text3 + ")");
            // BACK TO THE TOUR'S LANGUAGE, not to Turkish.
            //
            // This said `Loc.UseLanguage(0)` - index 0 is Turkish - so the
            // format check above left the whole rest of the tour in Turkish
            // whatever had been asked for. It is why the first `-Lang en` run
            // produced a set of English-flagged screenshots with Turkish
            // buttons on them.
            Loc.UseLanguage(LanguageFlag());
            if (_app != null && _app.Ui != null)
            {
                _app.Ui.ApplyLanguage();
                _app.Ui.Refresh();
            }
            yield return Settle();
            for (int i = 0; i < 4; i++)
            {
                SaveStore.Delete(i);
            }
            yield return BrokenSlot();
            _app.Ui.Replace(new MainMenuScreen());
            yield return Settle();
            yield return Shot("01-main-menu");
            CheckScreenLayout("main menu");
            Note(Click(Loc.T("ui.menu.new")), "New game button");
            yield return Settle();
            yield return Shot("02-cuisine-choice");
            CheckScreenLayout("cuisine choice");
            int cuisine = CuisineFlag();
            Note(Click(Loc.T("ui.cuisine.start"), cuisine),
                 cuisine == 0 ? "Fast food button" : "Turkish restaurant button");
            yield return Settle();
            yield return Shot("03-slot-choice");
            CheckScreenLayout("slot choice");
            Note(Click(Loc.T("ui.cuisine.start")), "First slot");
            yield return Settle();
            Note(_app.InGame, "The game started");
            yield return Shot("04-game-morning");
            CheckDefaultLanguage();
            yield return CheckArabicShaping();
            yield return ShotAllLanguages("05-language");
            yield return CheckStripsAllLanguages("morning");
            // THE SCREENSHOT NAME MUST NOT FOLLOW THE LANGUAGE.
            //
            // The file name used to be slugged from the BUTTON'S TEXT, so
            // the same screen was written as `05-hal.png` in Turkish and
            // `05-market.png` in English. The store images are committed,
            // and README links them by name - so a tour run in the other
            // language quietly orphaned every link.
            //
            // The label still comes from Loc (the tour has to click what
            // the player sees); only the file name is now fixed.
            string[] array = new string[4]
            {
                Loc.T("ui.morning.market"),
                Loc.T("ui.morning.menu"),
                Loc.T("ui.morning.staff"),
                Loc.T("ui.morning.equipment")
            };
            string[] shotNames = new string[4]
            { "market", "menu", "crew", "equipment" };
            int shotIndex = 0;
            int before;
            bool pressed;
            foreach (string s in array)
            {
                int shotIndexNow = shotIndex++;
                if (!Click(s))
                {
                    Note(ok: false, s + " button");
                    continue;
                }
                yield return Settle();
                if (s == Loc.T("ui.morning.staff"))
                {
                    before = _app.Sim.Dishwashers;
                    pressed = Click(Loc.T("ui.staff.sink_add"));
                    yield return Settle();
                    Note(pressed && _app.Sim.Dishwashers == before + 1, "Assigned to dishwashing duty (" + before + " -> " + _app.Sim.Dishwashers + ")");
                    Click(Loc.T("ui.staff.sink_remove"));
                    yield return Settle();
                    Note(_app.Sim.Dishwashers == before, "Dishwashing duty taken back (" + _app.Sim.Dishwashers + ")");

                    // THE HALL ROLE'S NAME FOLLOWS THE CUISINE.
                    //
                    // Fast food is self-service: no waiter comes to the table,
                    // the person in the hall collects the trays - that is, a
                    // BUSSER (docs/51). If the name does not change on screen
                    // the player cannot know what the person they hired does.
                    //
                    // The check looks for the other direction BY CUISINE too:
                    // it also verifies that the wrong label is NOT VISIBLE,
                    // otherwise a screen printing both names would pass green.
                    bool self = _app.Content != null && _app.Content.SelfService;
                    string right = self ? Loc.T("ui.staff.busser") : Loc.T("role.garson");
                    string wrong = self ? Loc.T("role.garson") : Loc.T("ui.staff.busser");
                    Note(HasText(right) && !HasText(wrong),
                         "The hall role's name fits the cuisine (" + right + ")");

                    // IS THE TRAIT'S VOICE ON SCREEN?
                    //
                    // The text was written, generated, bound to the card - and
                    // NOT SEEN. In this very session, in exactly this order, I
                    // said "the screen says Busser" and thought it did; it did
                    // not, because the card was showing the name.
                    //
                    // The measure is the text ITSELF: does the voice line of
                    // the first trait of the first person in the candidate pool
                    // appear on screen? With no candidate it is NOT MEASURED -
                    // not red.
                    int candidate = _app.Sim.CandidateTrait(0, 0, 0);
                    string voiceKey = candidate >= 0
                        ? _app.Economy.TraitAt(candidate).NameKey + ".voice"
                        : null;
                    string voice = voiceKey != null ? Loc.T(voiceKey) : null;

                    // A MISSING TRANSLATION HAS TO BE CAUGHT TOO.
                    //
                    // The first version was `HasText(Loc.T(key))` and that was a
                    // TAUTOLOGY: Loc.T returns "[key]" for a missing key, the
                    // card makes the SAME call, so with no translation at all
                    // both sides produced "[trait.x.voice]" and the check passed
                    // GREEN. Exactly the empty coverage this session has accused
                    // three times over.
                    //
                    // Both conditions at once: the text WILL be on screen and the
                    // missing-key marker WILL NOT be.
                    bool translated = !string.IsNullOrEmpty(voice)
                                     && !voice.StartsWith("[");
                    NoteIf(voiceKey != null,
                           translated && HasText(voice),
                           "The trait's voice line shows on the candidate card");
                }
                yield return Shot("05-" + shotNames[shotIndexNow]);
                Back();
                yield return Settle();
            }
            Hints.Hint hint = Hints.Current(_app.Sim);
            NoteIf(ScaleFlag() <= 1.01f, hint != null, "There is a hint on the morning of day 1 (" + ((hint != null) ? hint.Id : "none") + ")");
            bool openCommand = Click(Loc.T("ui.morning.open"));
            yield return Settle();
            bool asked = _app.Sim.Phase == DayPhase.Morning;
            if (asked)
            {
                Click(Loc.T("ui.morning.open"));
                yield return Settle();
            }
            Note(openCommand && _app.Sim.Phase == DayPhase.Service, "Open service" + (asked ? " (the readiness warning asked, the second touch opened it)" : ""));
            yield return Settle();
            yield return Shot("06-service-start");
            float was = _app.TimeScale;
            _app.TimeScale = 240f;
            float guard = 0f;
            while (_app.Sim.OccupiedTables < 2 && guard < 12f && _app.Sim.ServiceProgressBp < 2500)
            {
                guard += Time.deltaTime;
                yield return null;
            }
            Debug.Log((object)("  DIAG waiting for the hall to fill: " + guard.ToString("0.0") + " s, service " + _app.Sim.ServiceProgressBp / 100 + "%, occupied tables " + _app.Sim.OccupiedTables));
            bool wasPaused = _app.Paused;
            _app.Paused = true;
            yield return Shot("07-service-busy");
            _app.TimeScale = 16f;
            yield return CheckStripsAllLanguages("service");
            if (_app.Rig != null)
            {
                Vector3 once2 = ((Component)_app.Rig).transform.position;
                for (int k = 0; k < 40; k++)
                {
                    _app.Rig.ApplyGesture(0.2f, 0f);
                }
                yield return null;
                Note(_app.Rig.Zoom >= CameraRig.MinZoomLimit - 0.001f, "The zoom limit holds (" + _app.Rig.Zoom.ToString("0.00") + ")");
                Note(Vector3.Distance(((Component)_app.Rig).transform.position, once2) > 0.5f, "The zoom really brought the camera closer");
                Note(CurrentRenderScale() > 0.95f, "The render scale rose on zooming in (" + CurrentRenderScale().ToString("0.00") + ")");
                _app.Rig.ApplyGesture(0f, 5f);
                yield return null;
                Note(_app.Rig.YawOffset > 0.1f, "Two fingers turn the camera (" + _app.Rig.YawOffset.ToString("0.0") + " degrees)");
                for (int l = 0; l < 40; l++)
                {
                    _app.Rig.ApplyGesture(0f, 5f);
                }
                yield return null;
                Note(Mathf.Approximately(_app.Rig.YawOffset, CameraRig.MaxYawLimit), "The turn limit holds (" + _app.Rig.YawOffset.ToString("0") + " / " + CameraRig.MaxYawLimit.ToString("0") + " degrees)");
                _app.Rig.Overview();
                yield return Settled();
                Note(Mathf.Approximately(_app.Rig.Zoom, 1f) && Mathf.Approximately(_app.Rig.YawOffset, 0f), "The overview reset the zoom and the turn");
                float num = Quaternion.Angle(((Component)_app.Rig).transform.rotation, CameraFit.Rotation);
                Note(num < 1f, "The camera really came back to the base angle (" + num.ToString("0.0") + " degrees of deviation)");
                float num2 = Vector3.Distance(((Component)_app.Rig).transform.position, once2);
                Note(num2 < 0.5f, "The camera came back to the overview framing (" + num2.ToString("0.00") + " m)");
                Note(CurrentRenderScale() < 0.95f, "The render scale fell back on zooming out (" + CurrentRenderScale().ToString("0.00") + ")");
            }
            _app.Paused = wasPaused;
            float tourSpeed = _app.TimeScale;
            _app.TimeScale = GameApp.BaseTimeScale;
            float wait = 0f;
            int j = 0;
            while (_app.Sim.OccupiedTables < 2 && wait < 20f && _app.Sim.ServiceProgressBp < 3000)
            {
                if (_app.Sim.OccupiedTables > j)
                {
                    j = _app.Sim.OccupiedTables;
                }
                wait += Time.deltaTime;
                yield return null;
            }
            if (_app.Sim.OccupiedTables > j)
            {
                j = _app.Sim.OccupiedTables;
            }
            Debug.Log((object)("  DIAG waiting for a second table: " + wait.ToString("0.0") + " s, service " + _app.Sim.ServiceProgressBp / 100 + "%, occupied tables " + _app.Sim.OccupiedTables + " (at most " + j + ")"));
            _occupiedMost = j;
            RestaurantView cv = UnityEngine.Object.FindFirstObjectByType<RestaurantView>();
            j = 0;
            before = 0;
            int outside = 0;
            int pedestrianOverlaps = 0;
            int postOverlaps = 0;
            int dirtyMost = 0;
            int washing = 0;
            int eatingTables = 0;
            int tablePlates = 0;
            wait = 0f;
            int cleanLeast = int.MaxValue;
            int working = 0;
            int processed = 0;
            int busyCooks = 0;
            int simTasks = 0;
            float angleWorst = -1f;
            int platelessFrames = 0;
            int eatingFrames = 0;
            int idleWorkFrames = 0;
            int workingFrames = 0;
            Walker.AnimAdvanced = 0;
            Walker.AnimStalled = 0;
            RestaurantView.WorkAnimAdvanced = 0;
            RestaurantView.WorkAnimStalled = 0;
            int dayStart = _app.Sim.ServiceProgressBp;
            float elapsed = 0f;
            pressed = false;
            // THE WINDOW STOPS HALFWAY THROUGH THE DAY.
            //
            // The ceiling used to be 80% and there was no problem while the
            // day was FLAT: because the tables stayed full all day, the
            // table checks that follow could run at any moment. Once the day
            // was sharpened (docs/48) that assumption collapsed - the peak
            // happens between 12% and 40%, and with the window running up to
            // 80% the table hunt arrived at an empty hall: "service 70%,
            // today 12 people were expected".
            //
            // The tour now leaves the peak to the LATER checks. The real-time
            // budget (40 s) has not changed; only where in the day it stops.
            //
            // THE EXTENSION: IF THE MEASUREMENT COULD NOT BE MADE YET.
            //
            // The 40 s had quietly become the DEFINITION of the measurement
            // on its own. The Turkish cuisine's peak was moved from the 1st
            // slot to the 2nd in the previous session (export.py:
            // [1200,4800,2500,1500] -> [1200,2800,4500,1500]) and the Turkish
            // tour was never run after that session. When it was run:
            //
            //   DIAG liveliness window: 37.7 s, service 30% -> 61%
            //   FAIL : Work is being done in the kitchen (0 people; the
            //          simulation gave work 156 times)
            //
            // The same build PASSED on a second run. So both the red and the
            // green were the result not of the measurement but of SAMPLING
            // LUCK: a single cook spends most of its time WALKING to the
            // station (the simulation is giving work, the pose is Walk), and
            // the window may or may not fall on that narrow interval before
            // it closes.
            //
            // The answer is NOT to lengthen the window for everyone - that
            // would bring back the old behaviour that ran to 80% and ate the
            // peak. The extension only comes into play if the thing to be
            // measured HAS NOT BEEN SEEN YET and the simulation really is
            // giving work. If the check has already been satisfied the window
            // closes at 40 s as before, so the later checks find the peak
            // exactly as they did.
            // THE EXTENSION NOW COVERS EVERY HEADLINE CHECK, not just the
            // kitchen one.
            //
            // The rule stated above is right - "the extension only comes into
            // play if the thing to be measured HAS NOT BEEN SEEN YET" - and it
            // was written for exactly one thing, the cook. Everything else in
            // this window still closed at a flat 40 real seconds, and on
            // 17 September a fast food run came back with FIVE reds at once:
            //
            //   FAIL : Service produces occupied tables (at most 0)
            //   FAIL : There is movement in the hall (0 figures on their way)
            //   FAIL : An approaching figure opened the door (0 doors)
            //   FAIL : The guest comes in from the street (0 figures outside)
            //   FAIL : Work is being done in the kitchen (0 people; ... 0 times)
            //
            // The SAME BINARY then passed twice - once at phone scale and once
            // at store scale - so it was neither a code regression nor the
            // render resolution, which is what I suspected first and checked
            // second. It was this window closing before a thin day-one hall
            // (four tables, twelve guests) happened to show any of it.
            //
            // A check that comes back red on one run in three is worse than no
            // check: it teaches the reader to re-run until it is green, and
            // then it can never fail for a real reason again.
            //
            // The extension does NOT lower a bar. It keeps looking, to the same
            // 75 s ceiling, while something it is supposed to see is still
            // unseen - and once everything has been seen the window closes at
            // 40 s exactly as before, so the table checks that follow still
            // find the peak.
            bool allSeen() => _occupiedMost > 0 && j > 0 && before > 0
                              && outside > 0 && working > 0;
            while ((elapsed < 40f || (!allSeen() && elapsed < 75f))
                   && cv != null)
            {
                // THE WINDOW WATCHES A CONDITION, NOT A FIXED SLICE.
                //
                // The ceiling used to be 80% and it ate the peak, leaving the
                // table checks that follow to an empty hall; I pulled it to 50%
                // and this time the OPPOSITE happened - after the day was
                // sharpened, the first day means four tables and twelve people,
                // so the moment the kitchen is working may not fall inside a
                // narrow window. One run came out with "Work is being done in
                // the kitchen (0 people; the simulation gave work 536 times)":
                // there was work, the window was looking elsewhere.
                //
                // The right thing is not a fixed slice but a CONDITION: the
                // window stops once it has seen what it had to see. If it has
                // not seen it, it goes on looking up to 80% of the day - so the
                // old ceiling only comes into play in the WORST case and the
                // table checks normally take their turn early.
                bool seenIt = busyCooks > 0 && j > 0 && eatingTables > 0;
                if (seenIt && _app.Sim.ServiceProgressBp > 5000)
                {
                    pressed = true;
                    break;
                }
                if (_app.Sim.ServiceProgressBp > 8000)
                {
                    pressed = true;
                    break;
                }
                j = Mathf.Max(j, cv.MovingCount);
                before = Mathf.Max(before, cv.OpenDoorCount);
                outside = Mathf.Max(outside, cv.OutsideCount);
                busyCooks = Mathf.Max(busyCooks, cv.BusyCooks);
                pedestrianOverlaps = Mathf.Max(pedestrianOverlaps, cv.StreetOverlaps);
                postOverlaps = Mathf.Max(postOverlaps, cv.PostOverlaps);
                eatingTables = Mathf.Max(eatingTables, cv.EatingTables);
                tablePlates = Mathf.Max(tablePlates, cv.TablePlatesVisible);
                if (cv.EatingTables > 0)
                {
                    eatingFrames++;
                    if (cv.TablePlatesVisible == 0)
                    {
                        platelessFrames++;
                    }
                }
                if (_app.Sim.OccupiedTables > _occupiedMost)
                {
                    _occupiedMost = _app.Sim.OccupiedTables;
                }
                dirtyMost = Mathf.Max(dirtyMost, _app.Sim.PlatesDirty);
                cleanLeast = Mathf.Min(cleanLeast, _app.Sim.PlatesClean);
                washing = Mathf.Max(washing, cv.WashingCount);
                wait = Mathf.Max(wait, cv.WalkSlipWorst);
                GameScreen gameScreen = ((_app.Ui != null) ? (_app.Ui.Top as GameScreen) : null);
                if (gameScreen != null)
                {
                    _crisisSeen = Mathf.Max(_crisisSeen, gameScreen.CrisisTables);
                }
                cv.KitchenWork(out var workingNow, out var processedNow);
                working = Mathf.Max(working, workingNow);
                processed = Mathf.Max(processed, processedNow);
                if (workingNow > 0)
                {
                    workingFrames++;
                    if (processedNow == 0)
                    {
                        idleWorkFrames++;
                    }
                }
                float cookFacingErrorDeg = cv.CookFacingErrorDeg;
                if (cookFacingErrorDeg >= 0f && cookFacingErrorDeg > angleWorst)
                {
                    angleWorst = cookFacingErrorDeg;
                }
                for (int m = 0; m < _app.Sim.Cooks; m++)
                {
                    if (_app.Sim.CookTaskStation(m) >= 0)
                    {
                        simTasks++;
                    }
                }
                _selectedTable = OccupiedTable();
                if (j > 0 && before > 0 && outside > 0 && working > 0 && processed > 0 && _selectedTable >= 0 && RestaurantView.WorkAnimAdvanced + RestaurantView.WorkAnimStalled >= 30)
                {
                    break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            Debug.Log((object)("  DIAG liveliness window: " + elapsed.ToString("0.0") + " s, service " + dayStart / 100 + "% -> " + _app.Sim.ServiceProgressBp / 100 + "%" + (pressed ? " (DAY CEILING)" : "")));
            Note(dayStart < 3500, "The liveliness window started early in the day (" + dayStart / 100 + "%)");
            Note(_occupiedMost > 0, "Service produces occupied tables (at most " + _occupiedMost + ")");
            Note(j > 0, "There is movement in the hall (" + j + " figures on their way)");
            Note(cv != null && cv.WallCount >= 8, "The room walls were built (" + ((cv != null) ? cv.WallCount : 0) + " slabs)");
            Note(cv != null && cv.WallsClear, "The walls are transparent and have no colliders");
            Note(cv != null && cv.DoorCount == 2, "Only the entrance and the kitchen have a swinging door (" + ((cv != null) ? cv.DoorCount : 0) + ")");
            Note(cv != null && cv.GapCount >= cv.LinkCount + 1, "There is a gap for every neighbouring pair (" + ((cv != null) ? cv.GapCount : 0) + " gaps / " + ((cv != null) ? cv.LinkCount : 0) + " neighbouring pairs)");
            Note(before > 0, "An approaching figure opened the door (" + before + " doors)");
            Note(outside > 0, "The guest comes in from the street (" + outside + " figures outside)");
            Note(working > 0, "Work is being done in the kitchen (" + working + " people; the simulation gave work " + simTasks + " times)");
            Debug.Log((object)("  DIAG cook poses: " + ((cv != null) ? cv.CookPoses : "none")));
            NoteIf(workingFrames > 0, idleWorkFrames == 0, "A working figure's animation runs IN THE SAME FRAME (" + idleWorkFrames + " workless frames / " + workingFrames + ")");
            string report = "no view";
            bool ok = cv != null && cv.AccessOk(out report);
            Note(ok, "Room access rules (" + report + ")");
            Note(cv != null && cv.TrayCount <= cv.StaffCount, "The tray count does not exceed the crew (" + ((cv != null) ? cv.TrayCount : (-1)) + " trays / " + ((cv != null) ? cv.StaffCount : 0) + " staff)");
            Note(cv != null && cv.PotCount > 0, "There is a pan on top of the stoves (" + ((cv != null) ? cv.PotCount : 0) + " stoves)");
            NoteIf(Wardrobe.Attempted > 0, Wardrobe.Dressed == Wardrobe.Attempted, "All the staff were dressed (" + Wardrobe.Dressed + "/" + Wardrobe.Attempted + ")");
            Note(cv != null && cv.StreetWalkers >= 3, "There are people passing along the street (" + ((cv != null) ? cv.StreetWalkers : 0) + " people)");
            Note(cv != null && wait < 0.15f, "The feet do not slide while walking (at worst " + (wait * 100f).ToString("0") + "% deviation)");
            Note(cv != null && pedestrianOverlaps == 0, "The pedestrians do not pass through each other (at worst " + pedestrianOverlaps + " pairs)");
            Debug.Log((object)("  DIAG plates (window): least clean " + ((cleanLeast != int.MaxValue) ? cleanLeast : 0) + "/" + _app.Sim.PlatesTotal + ", most dirty " + dirtyMost + ", seen at the sink " + washing));
            Note(cv != null && postOverlaps == 0 && cv.StreetPostsKnown >= 3, "The pedestrians do not walk into a lamp post (at worst " + postOverlaps + " people, " + ((cv != null) ? cv.StreetPostsKnown : 0) + " posts known)");
            Note(cv != null && cv.LampPostCount >= 3 && cv.LampSpacingError < 0.05f, "The street lamps are evenly spaced (" + ((cv != null) ? cv.LampPostCount : 0) + " posts, deviation " + ((cv != null) ? cv.LampSpacingError.ToString("0.00") : "?") + " m)");
            int animAdvanced = Walker.AnimAdvanced;
            int animStalled = Walker.AnimStalled;
            int num3 = animAdvanced + animStalled;
            Debug.Log((object)("  DIAG walk clip: " + animAdvanced + " advanced, " + animStalled + " frozen"));
            NoteIf(num3 > 0, animAdvanced * 100 / Mathf.Max(1, num3) > 60, "A walking figure's clip advances (" + ((num3 != 0) ? (animAdvanced * 100 / num3) : 0) + "%, " + num3 + " frames)");
            int workAnimAdvanced = RestaurantView.WorkAnimAdvanced;
            int workAnimStalled = RestaurantView.WorkAnimStalled;
            int num4 = workAnimAdvanced + workAnimStalled;
            Debug.Log((object)("  DIAG work clip: " + workAnimAdvanced + " advanced, " + workAnimStalled + " frozen"));
            NoteIf(num4 > 0, workAnimAdvanced * 100 / Mathf.Max(1, num4) > 60, "A working staff member's clip advances (" + ((num4 != 0) ? (workAnimAdvanced * 100 / num4) : 0) + "%, " + num4 + " frames)");
            NoteIf(eatingFrames > 0, platelessFrames == 0, "An eating table has a plate IN THE SAME FRAME (" + platelessFrames + " plateless frames / " + eatingFrames + ")");
            Note(cv != null && cv.LampLanternBottom > 1.3f, "The lantern is above head height (" + ((cv != null) ? cv.LampLanternBottom.ToString("0.00") : "?") + " m)");
            Note(cv != null && cv.LampGlowCount == cv.LampPostCount * 3, "Every lamp has its beam, its halo and its pool (" + ((cv != null) ? cv.LampGlowCount : 0) + " parts, " + ((cv != null) ? cv.LampPostCount : 0) + " lamps)");
            NoteIf(angleWorst >= 0f, angleWorst < 45f, "The cook faces the stove (" + ((angleWorst < 0f) ? "nobody working" : (angleWorst.ToString("0") + " degrees")) + ")");
            if (_app.Light != null)
            {
                DayLight light = _app.Light;
                light.Apply(DayPhase.Morning, 0f);
                Color val = ((Camera.main != null) ? Camera.main.backgroundColor : Color.black);
                Quaternion val2 = ((light.Sun != null) ? ((Component)light.Sun).transform.rotation : Quaternion.identity);
                float num5 = ((light.Sun != null) ? light.Sun.intensity : 0f);
                bool lampsOn = light.LampsOn;
                bool roomLightsOn = light.RoomLightsOn;
                RestaurantView restaurantView = UnityEngine.Object.FindFirstObjectByType<RestaurantView>();
                float num6 = ((restaurantView != null) ? restaurantView.StreetTint : (-1f));
                float num7 = Luma(RenderSettings.ambientLight);
                light.Apply(DayPhase.Evening, 1f);
                Color val3 = ((Camera.main != null) ? Camera.main.backgroundColor : Color.black);
                float num8 = ((light.Sun != null) ? light.Sun.intensity : 0f);
                float num9 = ((light.Sun != null) ? Quaternion.Angle(val2, ((Component)light.Sun).transform.rotation) : 0f);
                float num10 = Mathf.Abs(val.r - val3.r) + Mathf.Abs(val.g - val3.g) + Mathf.Abs(val.b - val3.b);
                Note(num10 > 0.25f, "The morning and the evening backgrounds differ (" + num10.ToString("0.00") + ")");
                Note(num9 > 20f, "The sun's angle changes during the day (" + num9.ToString("0") + " degrees)");
                Note(num5 > num8 * 1.8f, "The sun weakens in the evening (" + num5.ToString("0.00") + " -> " + num8.ToString("0.00") + ")");
                Note(!lampsOn && light.LampsOn, "The street lamps light only in the evening");
                Note(light.LampCount >= 3, "Street lamps were built (" + light.LampCount + " of them)");
                Note(light.RoomLightCount >= 8, "Inside ceiling lights were built (" + light.RoomLightCount + " of them)");
                Note(!roomLightsOn && light.RoomLightsOn, "The inside lights come on only as the day goes on");
                Note(light.Warm != null && ((Behaviour)light.Warm).enabled && light.Warm.intensity > 1.5f, "The evening inside fill is lit (" + ((light.Warm != null) ? light.Warm.intensity.ToString("0.00") : "none") + ")");
                // THE CONSTANTS CAME BACK: THE DECOMPILE HAD KILLED THIS CHECK.
                //
                // The original was DayLight.RoomThreshold < DayLight.LampThreshold;
                // because both are const, the compiler did the comparison AT
                // COMPILE TIME and baked the result in as "true". The check stayed
                // permanently green - it measured nothing, and nothing would have
                // said so. This project's most frequent class of bug, this time by
                // the compiler's own hand.
                Note(DayLight.RoomThreshold < DayLight.LampThreshold,
                     "The inside lights come on before the street lamps ("
                     + DayLight.RoomThreshold.ToString("0.00") + " < "
                     + DayLight.LampThreshold.ToString("0.00") + ")");
                float num11 = ((restaurantView != null) ? restaurantView.StreetTint : (-1f));
                float num12 = Luma(RenderSettings.ambientLight);
                Note(restaurantView != null && num6 > 0.99f && num11 < 0.5f, "The street goes darker at night (morning " + num6.ToString("0.00") + " -> evening " + num11.ToString("0.00") + ")");
                Note(num12 > num7 * 1.3f, "The evening ambient stands in for the bounce (" + num7.ToString("0.00") + " -> " + num12.ToString("0.00") + ")");
                light.Apply(_app.Sim.Phase, (float)_app.Sim.ServiceProgressBp / 10000f);
            }
            RestaurantView restaurantView2 = UnityEngine.Object.FindFirstObjectByType<RestaurantView>();
            string text5 = ((restaurantView2 != null) ? restaurantView2.BadgeShaderName : null);
            Note(text5 == "Universal Render Pipeline/Unlit", "The badge material is unlit (" + (text5 ?? "none") + ")");
            int num13 = (StillOccupied(_selectedTable) ? _selectedTable : OccupiedTable());
            float bekleme = 0f;
            while (num13 < 0 && bekleme < 12f && _app.Sim.ServiceProgressBp < 7000)
            {
                bekleme += Time.deltaTime;
                yield return null;
                num13 = OccupiedTable();
            }
            if (num13 >= 0)
            {
                pressed = _app.Paused;
                _app.Paused = true;
                _app.SelectedTable = num13;
                Note(_app.ValidSelection() == num13, "The table selection holds");
                dayStart = _app.Sim.InterventionsLeft;
                if (_app.Ui != null)
                {
                    _app.Ui.Refresh();
                }
                yield return Settle();
                yield return Shot("16-table-selected");
                elapsed = 0f;
                while (_app.Sim.WaitingParties <= 0 && elapsed < 12f)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                if (_app.Ui != null)
                {
                    _app.Ui.Refresh();
                }
                yield return Settle();
                workingFrames = _app.Sim.WaitingParties;
                NoteIf(workingFrames > 0, Click(Loc.T("ui.service.tea")), "Tea to the hall (" + workingFrames + " waiting tables)");
                yield return Settle();
                NoteIf(workingFrames > 0, _app.Sim.InterventionsLeft < dayStart, "The intervention allowance went down");
                _app.SelectedTable = _app.Sim.TableCount + 5;
                Note(_app.ValidSelection() < 0, "An invalid selection is dropped");
                _app.SelectedTable = -1;
                _app.Paused = pressed;
            }
            else
            {
                // "NOT FOUND" CAN BE TWO DIFFERENT THINGS.
                //
                // If no table has filled while the day is still young this is a
                // FAILURE: it means service is not working. But if the wait ran
                // out because the day is up against 70%, it means it could not be
                // measured - the day is about to close and the last guests have
                // left.
                //
                // Folding the two into a single red was misleading: once daily
                // variation was added to demand, a quiet first day would drop
                // this check and the reason would read as "service is broken".
                NoteIf(_app.Sim.ServiceProgressBp < 7000, false,
                       "No occupied table was found during service (service "
                       + (_app.Sim.ServiceProgressBp / 100) + "%, today "
                       + _app.Sim.PlannedPeopleToday + " people were expected)");
            }
            _app.TimeScale = tourSpeed;
            if (_app.Rig != null)
            {
                Vector3 once2 = ((Component)_app.Rig).transform.position;
                workingFrames = RoomPlan.FirstDiningRoom();
                _app.Rig.FocusOn(workingFrames);
                yield return Settle();
                yield return Settled();
                Note(_app.Rig.FocusRoom == workingFrames, "Zoomed in on the room");
                Note(Vector3.Distance(((Component)_app.Rig).transform.position, once2) > 1f, "The camera really moved");
                yield return Shot("17-room-view");
                _app.Rig.Overview();
                yield return Settled();
                float num14 = Vector3.Distance(((Component)_app.Rig).transform.position, _app.Rig.OverviewPosition);
                Note(_app.Rig.FocusRoom < 0 && num14 < 0.5f, "Came back to the overview (" + num14.ToString("0.00") + " m)");
            }
            else
            {
                Note(ok: false, "The camera was not found");
            }
            _app.TimeScale = was;
            yield return CheckAudio();
            if (ClickNamed("pause"))
            {
                yield return Settle();
            }
            Note(ClickNamed("menu"), "Menu button");
            yield return Settle();
            yield return Shot("08-pause");
            if (Click(Loc.T("ui.menu.settings")))
            {
                yield return Settle();
                yield return Shot("09-settings");
                CheckScreenLayout("settings");
                Back();
                yield return Settle();
            }
            Back();
            yield return Settle();
            bool paused = _app.Paused;
            Note(paused, "Pause stopped the game");
            if (paused)
            {
                Note(ClickNamed("pause"), "Resume button");
                yield return Settle();
                Note(!_app.Paused, "The game is running again");
            }
            if (_app.Ui != null)
            {
                _app.Ui.Push(new LoanScreen());
            }
            yield return Settle();
            Note(HasText(Loc.T("ui.loan.repay")), "Repayment on the loan screen");
            Note(HasText(Loc.T("ui.loan.installment")), "Instalment on the loan screen");
            pressed = _app.Sim.HasLoan;
            Note(Click(Loc.T("ui.loan.take")), "Take-a-loan button");
            yield return Settle();
            Note(_app.Sim.HasLoan && !pressed, "The loan was taken");
            Note(HasText(Loc.T("ui.loan.one_at_a_time")), "A second loan is refused");
            Back();
            yield return Settle();
            elapsed = _app.TimeScale;
            _app.TimeScale = 16f;
            _app.Paused = false;
            workingFrames = _app.Sim.ServiceProgressBp;
            angleWorst = 0f;
            while (!_app.Sim.ServiceComplete && angleWorst < 60f)
            {
                angleWorst += Time.deltaTime;
                yield return null;
            }
            _app.TimeScale = elapsed;
            Debug.Log((object)("  DIAG end of day: " + angleWorst.ToString("0.0") + " s, service " + workingFrames / 100 + "% -> " + _app.Sim.ServiceProgressBp / 100 + "%, complete=" + _app.Sim.ServiceComplete));
            Note(_app.Sim.ServiceComplete, "Service reached the end of the day (" + _app.Sim.ServiceProgressBp / 100 + "%)");
            yield return Settle();
            int angrySeatedParties = _app.Sim.AngrySeatedParties;
            int crisisBuilds = GameScreen.CrisisBuilds;
            Debug.Log((object)("  DIAG crisis strip: " + angrySeatedParties + " angry at their tables, " + _app.Sim.AngryParties + " angry in total, the strip was built " + crisisBuilds + " times, " + _crisisSeen + " critical tables seen"));
            // THE TWO CHECKS THAT WERE HERE WERE DELETED - THEY COULD NOT RUN,
            // STRUCTURALLY.
            //
            // The first day has four tables and ~12 people: a crisis is
            // impossible. Both of them said "0 angry, 0 critical tables" on
            // every run and came back NOT MEASURED, and that produced an
            // ILLUSION of coverage for months - the crisis strip's interface
            // was thought to be tested when it never was.
            //
            // A check that can never run is WORSE than a check that does not
            // exist: the second at least says it is missing.
            //
            // In its place there is now a single check measuring over the whole
            // campaign (below, at the end of the loop): it sees the 60 days and
            // the weekend on days 20-21 that is deliberately worked
            // short-handed. On its first run it found 30 angry guests and the
            // strip built 186 times.
            //
            // The diagnostic line stays: the quiet of the first day is
            // information too.
            _ = angrySeatedParties;
            Note(Click(Loc.T("ui.service.close")), "Close the day");
            int platesDirtiedToday = _app.Sim.PlatesDirtiedToday;
            int platesWashedToday = _app.Sim.PlatesWashedToday;
            int platesTotal = _app.Sim.PlatesTotal;
            Note(platesDirtiedToday > 0, "Plates are getting dirty (" + platesDirtiedToday + " of them)");
            RestaurantView restaurantView3 = UnityEngine.Object.FindFirstObjectByType<RestaurantView>();
            Note(restaurantView3 != null && restaurantView3.WashSeenFrames > 0, "Someone was seen washing at the sink (" + ((restaurantView3 != null) ? restaurantView3.WashSeenFrames : 0) + " frames)");
            Note(platesWashedToday > 0, "Plates are being washed (" + platesWashedToday + " of them)");
            Note(_app.Sim.PlatesClean + _app.Sim.PlatesInUse + _app.Sim.PlatesDirty == platesTotal, "The plate count is conserved (" + _app.Sim.PlatesClean + "+" + _app.Sim.PlatesInUse + "+" + _app.Sim.PlatesDirty + "=" + platesTotal + ")");
            yield return Settle();
            yield return Shot("10-evening");
            yield return CheckStripsAllLanguages("evening");
            if (_app.Sim.BuildDayReport().SpoiledValue > 0)
            {
                Note(HasText(Loc.T("ui.evening.spoiled")), "What went in the bin, in the evening strip");
                if (Click(Loc.T("ui.evening.title")))
                {
                    yield return Settle();
                    Note(HasText(Loc.T("ui.evening.spoiled")), "What went in the bin, in the day report");
                    // THE BOOK CHECK WAS MOVED AWAY FROM HERE.
                    //
                    // This is the FIRST day's report; the tab mechanic opens on day
                    // 16, so the book is ALWAYS empty here and the check came back
                    // NOT MEASURED on every run. Lost coverage is invisible in a
                    // green tour: the summary still says "0 failed". The check has
                    // moved into the campaign loop, to after a tab HAS BEEN opened.
                    Back();
                    yield return Settle();
                }
            }
            if (_app.InGame)
            {
                Note(_app.SaveToSlot(0), "Saved into a slot");
                int day = _app.Sim.Day;
                long cash = _app.Sim.Cash;
                Note(_app.LoadSlot(0), "Loaded from a slot");
                Note(_app.InGame && _app.Sim.Day == day && _app.Sim.Cash == cash, "The save gives the same state back");

                // A TORN SAVE OPENS FROM THE PREVIOUS ONE.
                //
                // The atomic write proves the file is WHOLE, and nothing proved
                // it was READABLE. Storage that lies about a flush, a truncating
                // file system, a bug in Write - each of those installs a
                // complete, unopenable save over a good one, and before
                // SaveStore.BackupPath there was nothing behind it.
                //
                // THIS IS THE ONLY PLACE THE FALLBACK CAN BE MEASURED. It lives
                // in Lokanta.Game, which the core test project cannot reach, and
                // it only runs against real files on a real disk.
                //
                // THE SIM IS TICKED BY HAND. The two saves have to DIFFER, or
                // "it opened the previous one" is indistinguishable from "it
                // opened the torn one" - and whether the clock is running here
                // depends on the pause state, the speed key and whether the
                // service has finished. Sixty ticks under our own control is the
                // difference; nothing else in the tour depends on the clock.
                long backupTick = _app.Sim.TickIndex;
                for (int t = 0; t < 60; t++) _app.Sim.Tick();
                long tornTick = _app.Sim.TickIndex;
                Note(tornTick != backupTick,
                    "The two saves differ - without this the backup check is empty");

                Note(_app.SaveToSlot(0), "Saved a second time, so a backup exists");
                try
                {
                    File.WriteAllText(SaveStore.StatePath(0), "{ this is not a save");
                }
                catch (Exception e)
                {
                    Debug.LogError("could not tear the save file: " + e);
                }

                bool opened = _app.LoadSlot(0);
                Note(opened, "A torn save still opens - the backup is read");
                Note(opened && _app.Sim.TickIndex == backupTick,
                    "And it is the PREVIOUS save, not the torn one");

                // Leave the slot coherent for the rest of the tour: until
                // something rewrites it, the state file on disk is still the
                // torn one. This save pushes the torn file into the backup for
                // ONE generation, which is the honest cost of the mechanism and
                // not a problem the tour has to clean up after.
                Note(_app.SaveToSlot(0), "The slot is whole again");
            }
            else
            {
                Note(ok: false, "Save attempt - the game had not started");
            }
            yield return Settle();
            yield return LongRun((_app.Sim != null) ? (_app.Sim.CampaignDays + 1) : 61);
            _app.Ui.Replace(new MainMenuScreen());
            yield return Settle();
            if (Click(Loc.T("ui.menu.credits")))
            {
                yield return Settle();
                yield return Shot("11-credits");
                // THE BUTTON IS FOUND BY ITS TRANSLATED LABEL, NOT A TURKISH ONE.
                //
                // This read `Click("lisans")` - a Turkish word. It worked
                // only because the tour happened to run at language index
                // 0; in any other language the licence screen was never
                // opened and the check silently measured nothing.
                if (Click(Loc.T("ui.credits.licenses")))
                {
                    yield return Settle();
                    Note(LicenseTextsLoaded(), "The licence texts are in the build");
                    yield return Shot("14-licences");
                    Back();
                    yield return Settle();
                }
                Back();
                yield return Settle();
            }
            NoteIf(_shotHash.Count >= 2, _shotDup == null, (_shotDup == null) ? ("Every screenshot is a different screen (" + _shot + " screenshots, " + _shotHash.Count + " distinct images)") : ("The same image under two names: " + _shotDup));
            Debug.Log((object)"=== Lokanta tour ===");
            foreach (string item in _log)
            {
                Debug.Log((object)("  " + item));
            }
            Debug.Log((object)("  " + _shot + " screenshots: " + _dir));
            Debug.Log((object)("  summary: " + _passed + " passed, " + _failed + " failed, " + _unmeasured + " unmeasured"));
            if (_failed > 0)
            {
                Debug.LogError((object)("PROBLEMS: the tour left " + _failed + " checks failing"));
            }
            try
            {
                File.WriteAllText(Path.Combine(_dir, "summary.txt"), "passed=" + _passed + Environment.NewLine + "failed=" + _failed + Environment.NewLine + "unmeasured=" + _unmeasured + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogWarning((object)("Tour summary could not be written: " + ex.Message));
            }
            Debug.Log((object)"=== tour done ===");
            yield return (object)new WaitForSeconds(0.5f);
            _quitting = true;
        }

        private void Update()
        {
            if (_quitting)
            {
                _quitting = false;
                Application.Quit();
            }
        }

        private IEnumerator LongRun(int days)
        {
            if (!_app.InGame)
            {
                Note(ok: false, "Long run - there is no game");
                yield break;
            }
            float was = _app.TimeScale;
            _app.TimeScale = 1800f;
            int halfDays = 0;
            int restockFailed = 0;
            int servedParties = 0;
            int startTables = _app.Sim.TableCount;
            bool storeShotTaken = false;
            bool creditOpened = false;
            bool comboToggled = false;
            bool tenureMeasured = false;
            bool tenureMomentSeen = false;
            bool ledgerMeasured = false;
            int angryTotal = 0;
            bool qualityMeasured = false;
            bool reportMeasured = false;
            bool badgeMeasured = false;
            int started = _app.Sim.Day;
            string trouble = null;
            for (int d = 0; d < days; d++)
            {
                if (trouble != null)
                {
                    break;
                }
                if (_app.Sim.Day > _app.Sim.CampaignDays)
                {
                    break;
                }
                yield return ToGameScreen();
                if (_app.Sim.Phase == DayPhase.Evening)
                {
                    Click(Loc.T("ui.evening.next"));
                    yield return Settle();
                    yield return ToGameScreen();
                }
                // WE RUN THE CRISIS PATH DELIBERATELY.
                //
                // The tour always hires the crew it needs, and a full crew
                // absorbs a crisis - which is why the checks "the crisis strip
                // was built if a guest left angry from a table" and "the strip
                // was built when a critical table was seen" said "0 angry, 0
                // critical tables" FOR MONTHS and went past unmeasured. The
                // crisis strip's interface had never been tested.
                //
                // Days 20 and 21 are the weekend (the last two days of the week)
                // and the shop has grown by then: working one waiter short
                // produces a real queue. This is also the play the game rewards -
                // the "you got through the peak short-handed" badge (docs/47)
                // recognises exactly this.
                //
                // The crew is restored by itself on day 22 by Grow(); nothing
                // else has to be done.
                bool runCrisis = _app.Sim.Day == 20 || _app.Sim.Day == 21;
                if (runCrisis && _app.Sim.HallStaff > 1)
                    _app.Send(CommandKind.Fire, 1, _app.Sim.HallStaff - 1);
                else
                    Grow();

                if (Click(Loc.T("ui.morning.market")))
                {
                    yield return Settle();

                    // INGREDIENT QUALITY: the mechanic was complete in the core
                    // and had no button on any screen (docs/49). The tour now
                    // REALLY changes the tier and verifies that it reached the
                    // simulation - the button existing is not enough, and the
                    // same bug has come out twice in this project as "there is a
                    // button but the command does not go through".
                    if (!qualityMeasured)
                    {
                        int previous = _app.Sim.Quality;
                        int target = previous == 2 ? 1 : 2;
                        if (Click(Loc.T(target == 2 ? "ui.quality.2" : "ui.quality.1")))
                        {
                            yield return Settle();
                            Note(_app.Sim.Quality == target,
                                 "The quality tier changes at the market (" + previous
                                 + " -> " + _app.Sim.Quality + ")");
                            // It goes back to the old tier: the tour measures the
                            // rest of the campaign, and changing the quality for good
                            // would shift every number that comes after it.
                            Click(Loc.T(previous == 0 ? "ui.quality.0"
                                      : previous == 1 ? "ui.quality.1" : "ui.quality.2"));
                            yield return Settle();
                            Note(_app.Sim.Quality == previous,
                                 "The quality tier can be set back");
                            qualityMeasured = true;
                        }
                    }

                    if (!Click(Loc.T("ui.morning.restock")))
                    {
                        restockFailed++;
                    }
                    yield return Settle();
                    Click(Loc.T("ui.common.ok"));
                    yield return Settle();
                }
                // IS THE TENURE RIGHT ON SCREEN - INSIDE THE CAMPAIGN.
                //
                // In the day 1 inspection the staff card says "0 days" and that
                // is RIGHT: nobody has worked a day yet. So the tenure bug is
                // invisible there - the card could say "0 days" for days on end
                // and the first day's frame would look the same.
                //
                // That was the bug itself: `StaffDaysWorked` was returning the
                // experience, and an `experienced` staff member still showed "0
                // days" after sixty days. The place it sees the screen has to be
                // later in the campaign.
                if (!tenureMeasured && _app.Sim.Day >= 5
                    && Click(Loc.T("ui.morning.staff")))
                {
                    yield return Settle();
                    int real = _app.Sim.StaffDaysWorked(0, 0);
                    Note(real > 0,
                         "Tenure advances (day " + _app.Sim.Day + ", "
                         + real + " days)");
                    // The measure is the text ON SCREEN: does the card write the real number?
                    Note(HasText(Loc.T("ui.staff.days",
                                       _app.Sim.StaffLevel(0, 0), real)),
                         "The staff card shows the real tenure number");
                    tenureMeasured = true;
                    Back();
                    yield return Settle();
                }

                if (!Click(Loc.T("ui.morning.open")))
                {
                    trouble = "service did not open, day " + _app.Sim.Day;
                    break;
                }
                yield return Settle();
                if (!storeShotTaken && ScaleFlag() > 1.01f && _app.Sim.Day >= 40)
                {
                    float oldSpeed = _app.TimeScale;
                    float ahead = 0f;
                    int targetTables = _app.Sim.TableCount / 2;
                    while (ahead < 45f && _app.Sim.ServiceProgressBp < 8000 && (_app.Sim.OccupiedTables < targetTables || _app.Sim.BuildDayReport().Revenue <= 0))
                    {
                        ahead += Time.deltaTime;
                        yield return null;
                    }
                    _app.TimeScale = GameApp.BaseTimeScale;
                    if (_app.Ui != null)
                    {
                        _app.Ui.Refresh();
                    }
                    yield return Settle();
                    // THE SEARCH PATIENCE: 30 -> 75 SECONDS.
                    //
                    // The bar was NOT LOWERED, the search was lengthened.
                    // Self-service speeds the turnover of tables up (there is no
                    // waiting for service or for payment), so the number of
                    // tables occupied at any one moment falls and the "half
                    // full" moment happens in a NARROWER window. One run ran out
                    // of time at 4/14; other runs of the same build saw 8/14 and
                    // 13/14, so the threshold is reachable - what was missing
                    // was patience.
                    //
                    // Lowering the threshold would have been wrong: the store
                    // screenshot's job is to show a full restaurant, and saying
                    // "it is empty in self-service anyway" would be settling for
                    // the game's quietest moment.
                    float waited = 0f;
                    while (waited < 75f && (_app.Sim.OccupiedTables < _app.Sim.TableCount / 2 || _app.Sim.BuildDayReport().Revenue <= 0 || _app.NoticeCount > 0))
                    {
                        waited += Time.deltaTime;
                        yield return null;
                    }
                    yield return Shot("20-store-service");
                    Note(_app.NoticeCount == 0, "The hall is unobstructed in the store screenshot (" + _app.NoticeCount + " bubbles)");
                    Note(_app.Sim.OccupiedTables >= _app.Sim.TableCount / 2, "The hall is full in the store screenshot (" + _app.Sim.OccupiedTables + "/" + _app.Sim.TableCount + " tables)");
                    GameScreen gameScreen = ((_app.Ui != null) ? (_app.Ui.Top as GameScreen) : null);
                    if (gameScreen != null)
                    {
                        int overlappingButtons = gameScreen.OverlappingButtons;
                        Note(overlappingButtons == 0, "overlapping buttons (day " + _app.Sim.Day + ", " + _app.Sim.TableCount + " tables): " + overlappingButtons + " pairs" + ((overlappingButtons > 0) ? (" - " + gameScreen.OverflowDetail) : ""));
                        int clippedButtons = gameScreen.ClippedButtons;
                        Note(clippedButtons == 0, "clipped text (day " + _app.Sim.Day + "): " + clippedButtons + ((clippedButtons > 0) ? (" - " + gameScreen.ClipDetail) : ""));
                    }
                    Debug.Log((object)("  STORE screenshot: day " + _app.Sim.Day + ", " + _app.Sim.TableCount + " tables, " + _app.Sim.OccupiedTables + " occupied"));
                    _app.TimeScale = oldSpeed;
                    storeShotTaken = true;
                }
                float budget = 6f + (float)_app.Sim.TableCount * 0.8f;
                float guard = 0f;
                while (!_app.Sim.ServiceComplete && guard < budget)
                {
                    // THE TAB: INSIDE THE DAY, NOT AT THE OPENING OF SERVICE.
                    //
                    // I put it in the wrong place twice. First it was on day 1
                    // and the mechanic opens on day 16; then I moved it to the
                    // opening of service, and at that moment no guest has SAT
                    // DOWN yet, so nobody is asking for a tab either. In both
                    // cases the block was skipped silently, the book stayed
                    // empty and the book SCREEN was never tested - the tour said
                    // "0 failed" every time. Lost coverage is invisible in a
                    // green tour.
                    //
                    // The right place is INSIDE the day: a guest is sitting,
                    // asking, and the tour catches that moment.
                    // THE COMBO BUTTON: THE MEASURE IS THE COMMAND GETTING
                    // THROUGH.
                    //
                    // The button was in GameScreen and THE TOUR NEVER PRESSED IT.
                    // Exactly the same empty coverage has come out twice in this
                    // project (SetQuality, CollectCredit): the mechanic complete
                    // in the core, a button on the screen, and nobody measuring
                    // that the command really goes through. An existence test
                    // cannot see that bug.
                    //
                    // The combo is now a decision that HAS TO BE MEASURED: once
                    // self-service had emptied the hall the bottleneck moved to
                    // the kitchen, and closing the combo at the peak beats
                    // keeping it open all day, 23,474 to 22,163 (docs/53). So
                    // this button is the only door to the game's best play - if
                    // the command does not go through, that play cannot be
                    // played and nothing would say so.
                    //
                    // Two-way: it switches it and switches it back. One way would
                    // say "the button does something"; two ways say "it does
                    // exactly what it is asked to". And the rest of the campaign
                    // carries on in the same state, so the later numbers do not
                    // shift.
                    if (!comboToggled && _app.Sim.HasCombo)
                    {
                        bool wasOn = _app.Sim.ComboEnabled;
                        if (Click(Loc.T(wasOn ? "ui.service.combo_on"
                                              : "ui.service.combo_off")))
                        {
                            yield return Settle();
                            Note(_app.Sim.ComboEnabled != wasOn,
                                 "The combo button reaches the simulation ("
                                 + wasOn + " -> " + _app.Sim.ComboEnabled + ")");

                            Click(Loc.T(_app.Sim.ComboEnabled
                                        ? "ui.service.combo_on"
                                        : "ui.service.combo_off"));
                            yield return Settle();
                            Note(_app.Sim.ComboEnabled == wasOn,
                                 "The combo can be switched back");
                            comboToggled = true;
                        }
                    }

                    if (!creditOpened && _app.Sim.HasCredit
                        && _app.Sim.FirstCreditAsker() >= 0)
                    {
                        // It is paused: the button is BUILT from "is anyone
                        // asking" and a frame passes between that build and the
                        // click. If the guest gets up in that frame the command is
                        // rejected and what is measured is not the mechanic but
                        // the tour's own latency.
                        bool pausedBefore = _app.Paused;
                        _app.Paused = true;
                        _app.SelectedTable = -1;
                        if (_app.Ui != null)
                        {
                            _app.Ui.Refresh();
                        }
                        yield return Settle();

                        int asker = _app.Sim.FirstCreditAsker();
                        if (asker >= 0 && Click(Loc.T("ui.service.credit")))
                        {
                            yield return Settle();

                            // THE MEASURE IS NOT "MONEY IN THE BOOK" BUT THE PARTY'S
                            // MARK. ExtendCredit does not write to the book
                            // IMMEDIATELY; the record appears when the bill IS PAID.
                            // Looking at OpenCredit meant thinking the command had been
                            // rejected - I did exactly that once and the tour went red.
                            Note(_app.Sim.PartyHasCredit(asker),
                                 "A tab was opened for the table");
                            creditOpened = true;
                        }
                        _app.Paused = pausedBefore;
                    }

                    guard += Time.deltaTime;
                    yield return null;
                }
                if (!_app.Sim.ServiceComplete)
                {
                    halfDays++;
                }
                servedParties += _app.Sim.BuildDayReport().ServedParties;

                if (!Click(Loc.T("ui.service.close")))
                {
                    trouble = "the day did not close, day " + _app.Sim.Day;
                    break;
                }
                yield return Settle();
                if (_app.Ui.Top is StoryScreen)
                {
                    _stories++;
                    if (_stories == 1)
                    {
                        yield return Shot("15-story");
                    }
                    Click(Loc.T("ui.story.continue"));
                    yield return Settle();
                }
                // THE TAB BOOK SCREEN.
                //
                // HERE, because the "Day report" button is on the EVENING
                // screen - after the day has closed. My previous attempt
                // looked for it before closing the day and the button was not
                // there: the block was skipped silently, the check never ran
                // and the tour again said "0 failed".
                //
                // The book's record appears when the bill IS PAID, that is, at
                // the end of the day the tab was opened on; which is why
                // OpenCredit is what is looked at.
                if (!ledgerMeasured && _app.Sim.OpenCredit > 0
                    && Click(Loc.T("ui.evening.title")))
                {
                    yield return Settle();
                    if (Click(Loc.T("ui.ledger.title")))
                    {
                        yield return Settle();
                        Note(HasText(Loc.T("ui.ledger.chance_wait")),
                             "The book gives the chance of payment ("
                             + _app.Sim.TabCount + " tabs)");
                        Note(HasText(Loc.T("ui.ledger.chase")),
                             "The book has a chase button");

                        // THE BUTTON IS PRESSED - seeing that it exists is not
                        // enough.
                        //
                        // For years this check verified that the button was ON THE
                        // SCREEN and nothing ever PRESSED it. And yet "there is a
                        // button but the command does not go through" has come out
                        // twice in this project; an existence test could not see
                        // that bug.
                        //
                        // Chasing CLOSES the tab (SettleTab halfChance) - whether
                        // or not it is collected. That is why the measure is the
                        // number of tabs: a change in the amount depends on the
                        // collection succeeding, and that would tie the check to a
                        // dice roll.
                        int tabsBefore = _app.Sim.TabCount;
                        if (Click(Loc.T("ui.ledger.chase")))
                        {
                            yield return Settle();
                            Note(_app.Sim.TabCount < tabsBefore,
                                 "Chasing closes the tab (" + tabsBefore
                                 + " -> " + _app.Sim.TabCount + " tabs)");
                        }
                        // CheckStrips IS THE WRONG CHECK HERE: it measures the
                        // bottom STRIP, and the book is a list screen with no strip.
                        // It gave a red "strip not measured" - the fault was in the
                        // measurement, not in the screen.
                        ledgerMeasured = true;
                        Back();
                        yield return Settle();
                    }
                    Back();
                    yield return Settle();
                }

                // THE WEEKLY REPORT CARD AND THE BADGES.
                //
                // Both are on the EVENING screen, behind the "Day report"
                // button - that is, after the day has closed and before moving
                // on to the next day. The lesson learned from putting the book
                // check in the wrong place four times: this is the ONLY moment
                // at which the check can run.
                //
                // The "earned today" mark is cleared in AdvanceToNextDay, so
                // looking the next day would always see zero and the check
                // would silently never run.
                // IT IS COUNTED AFTER THE DAY HAS CLOSED. The first time I
                // wrote it I put it in the morning, and because AdvanceToNextDay
                // had just reset the counter the total always stayed 0 - the
                // check would run but measure nothing.
                angryTotal += _app.Sim.AngrySeatedParties;

                bool reportReady = _app.Sim.WeekReportReady;
                bool badgeToday = false;
                for (int b = 0; b < _app.Sim.BadgeCount; b++)
                    if (_app.Sim.BadgeEarnedToday(b)) { badgeToday = true; break; }

                if ((reportReady && !reportMeasured) || (badgeToday && !badgeMeasured))
                {
                    if (Click(Loc.T("ui.evening.title")))
                    {
                        yield return Settle();
                        if (reportReady && !reportMeasured)
                        {
                            Note(HasText(Loc.T("ui.week.note")),
                                 "The weekly report card is in the evening report (week " 
                                 + _app.Sim.WeekNumber + ")");
                            // The axis's NAME has to be visible too: the heading alone
                            // does not catch the report card coming out EMPTY.
                            Note(HasText(Loc.T(SeasonScore.AxisKey(1))),
                                 "The report card names the reputation axis");
                            reportMeasured = true;
                        }
                        if (badgeToday && !badgeMeasured)
                        {
                            Note(HasText(Loc.T("ui.badge.earned")),
                                 "A badge earned shows in the evening report");
                            badgeMeasured = true;
                        }
                        Back();
                        yield return Settle();
                    }
                }

                // IS THE TENURE MOMENT IN THE STRIP?
                //
                // A unit test holds that it fires in the core; this asks
                // whether it REACHES the player. The strip is a short list (at
                // most three) and it fades with time, which is why it is looked
                // at right after the day closes.
                //
                // THE MEASURE IS THE MOMENT, NOT THE PERSON. In my first
                // version I built the expected text from `StaffName(0, 0)` and
                // the check went red; the diagnostic showed that the notice WAS
                // THERE, but with the name of the person IN THE HALL ("Asli has
                // been here 30 days"), because two people cross the threshold
                // on the same day and a three-slot strip leaves one of them
                // out. Which NAME is left is not the tour's business; the
                // question is "did a line like this reach the player".
                if (!tenureMomentSeen)
                {
                    string key = _app.Content != null && _app.Content.SelfService
                        ? "notice.tenure_zincir" : "notice.tenure_lokanta";
                    for (int pool = 0; pool < 2 && !tenureMomentSeen; pool++)
                    {
                        int people = pool == 0 ? _app.Sim.Cooks : _app.Sim.HallStaff;
                        for (int k = 0; k < people && !tenureMomentSeen; k++)
                        {
                            string wait = Loc.T(key,
                                                 _app.Sim.StaffName(pool, k),
                                                 Simulation.TenureDays);
                            for (int n = 0; n < _app.NoticeCount; n++)
                            {
                                if (_app.NoticeTextAt(n) != wait) continue;
                                tenureMomentSeen = true;
                                Debug.Log("  DIAG tenure moment: " + wait);
                                break;
                            }
                        }
                    }

                    // If the threshold day has come and it was still not found,
                    // the strip is printed: "it never comes" and "it comes but
                    // the text does not match" look the same from outside, and I
                    // had to tell them apart once.
                    if (!tenureMomentSeen && _app.Sim.Day == Simulation.TenureDays)
                    {
                        string dump = "";
                        for (int n = 0; n < _app.NoticeCount; n++)
                            dump += " | " + _app.NoticeTextAt(n);
                        Debug.Log("  DIAG no tenure moment, strip:" + dump);
                    }
                }

                if (!Click(Loc.T("ui.evening.next")))
                {
                    trouble = "did not move on to the next day, day " + _app.Sim.Day;
                    break;
                }
                yield return Settle();
            }
            _app.TimeScale = was;
            int num = _app.Sim.Day - started;
            Note(trouble == null, trouble ?? (num + " days played without a break"));
            Note(_app.Sim.Day > started, "The day advanced: " + started + " -> " + _app.Sim.Day);
            Note(_stories > 0, _stories + " regular story beats seen");
            Note(halfDays == 0, "Service finished every day (" + halfDays + " days cut short)");
            Note(restockFailed == 0, "The stock was refilled every morning (" + restockFailed + " days could not buy)");
            Note(servedParties > 0, "Guests were served over the sixty days (" + servedParties + " parties)");
            Note(_app.Sim.TableCount > 4, "It expanded over the campaign (" + startTables + " -> " + _app.Sim.TableCount + " tables)");

            // IF THE REPORT CARD AND THE BADGE WERE NEVER MEASURED, SAY SO.
            //
            // If the flag stays false the block inside the loop never ran -
            // and a check that does not run looks, from outside, THE SAME as
            // a check that passes. Staying silent would mean believing the
            // feature had been tested.
            NoteIf(qualityMeasured, qualityMeasured,
                   "The quality selector was seen at the market");
            // The Turkish cuisine has no combo, so this check has to say NOT
            // MEASURED there - not red. That is exactly what NoteIf is for.
            NoteIf(comboToggled, comboToggled,
                   "The combo button was pressed during service");
            NoteIf(tenureMeasured, tenureMeasured,
                   "Tenure was measured on the staff screen");

            // THE TAB BOOK VANISHED FROM THE SUMMARY ENTIRELY.
            //
            // `creditOpened` and `ledgerMeasured` were set and never read.
            // Their four checks only run in the Turkish restaurant, so on a
            // fast-food run they simply did not happen - and the summary
            // still said "0 unmeasured". Every sibling flag above has a line
            // here; these two were the ones that did not, which is the exact
            // shape of "a check that does not run looks, from outside,
            // exactly like one that passes".
            NoteIf(creditOpened, creditOpened,
                   "The tab was opened during service");
            NoteIf(ledgerMeasured, ledgerMeasured,
                   "The tab book was measured");

            // DID THE LONG-TENURE MOMENT REACH THE PLAYER?
            //
            // A unit test holds that it fires in the core. This asks a
            // separate question: was it SEEN IN THE NOTICE STRIP? "The
            // mechanic is complete in the core and never reaches the player"
            // has come out three times in this project (SetQuality,
            // CollectCredit, the combo button).
            NoteIf(_app.Sim.Day > Simulation.TenureDays, tenureMomentSeen,
                   "The long-tenure moment was seen in the notice strip");
            NoteIf(reportMeasured, reportMeasured,
                   "The weekly report card was seen (at least one week in 60 days)");
            NoteIf(badgeMeasured, badgeMeasured,
                   "A badge earned was seen in the evening report");
            Note(_app.Sim.BadgesEarned > 0,
                 "A badge was earned in the campaign (" + _app.Sim.BadgesEarned
                 + " / " + _app.Sim.BadgeCount + ")");

            // THE CRISIS STRIP IS MEASURED OVER THE WHOLE CAMPAIGN.
            //
            // The same check is in the day 1 inspection block too, but it can
            // NEVER run there: the first day has four tables and ~12 people,
            // so a crisis is structurally impossible. For months it said "0
            // angry, 0 critical tables" and went past unmeasured - the crisis
            // strip's interface had never been tested.
            //
            // This one sees the 60 days, including the weekend on days 20-21
            // that is deliberately worked short-handed. Because CrisisBuilds
            // is a cumulative counter, it covers the whole campaign.
            Debug.Log("  DIAG campaign crisis: " + angryTotal
                      + " angry at their tables, the strip " + GameScreen.CrisisBuilds + " times");
            NoteIf(angryTotal > 0, GameScreen.CrisisBuilds > 0,
                   "The crisis strip was built when guests got angry in the campaign ("
                   + angryTotal + " angry, the strip "
                   + GameScreen.CrisisBuilds + " times)");
            int num2 = 0;
            long num3 = 0L;
            Renderer[] array = UnityEngine.Object.FindObjectsByType<Renderer>((FindObjectsSortMode)0);
            foreach (Renderer val in array)
            {
                if (!val.enabled || !((Component)val).gameObject.activeInHierarchy)
                {
                    continue;
                }
                num2++;
                Mesh val2 = null;
                MeshFilter component = ((Component)val).GetComponent<MeshFilter>();
                if (component != null)
                {
                    val2 = component.sharedMesh;
                }
                SkinnedMeshRenderer val3 = (SkinnedMeshRenderer)(object)((val is SkinnedMeshRenderer) ? val : null);
                if (val3 != null)
                {
                    val2 = val3.sharedMesh;
                }
                if (!(val2 == null))
                {
                    for (int j = 0; j < val2.subMeshCount; j++)
                    {
                        num3 += val2.GetIndexCount(j) / 3;
                    }
                }
            }
            Debug.Log((object)("  DIAG scene budget: " + num2 + " renderers, " + num3 + " triangles (" + _app.Sim.TableCount + " tables)"));
            Note(num2 < 400, "The base scene's renderer count is within budget (" + num2 + " < 400, " + _app.Sim.TableCount + " tables)");
            Note(num3 < 80000, "The base scene's triangle count is within budget (" + num3 + " < 80k, " + _app.Sim.TableCount + " tables)");
            yield return Settle();
            bool flag = _app.Ui.Top is EndScreen;
            Note(flag, "The year-end evaluation opened");
            Note(_app.Ui.Depth == 2, "The year-end screen opened ONCE (stack " + _app.Ui.Depth + ", expected 2)");
            if (!flag)
            {
                yield break;
            }
            yield return Shot("13-review");
            // THE NUMBER OF AXES IS SYMBOLIC. After the decompile there was a
            // "7" CONSTANT here (in three places): the loop, the index of the
            // last axis and the assertion. If the number of axes changed the
            // tour would NEVER look for the new axis and would talk in terms
            // of the old number.
            int visible = 0;
            for (int k = 0; k < SeasonScore.AxisCount; k++)
            {
                string key = (k == SeasonScore.AxisCount - 1)
                    ? _app.Content.ScoreAxis.NameKey
                    : SeasonScore.AxisKey(k);
                if (VisibleText(Loc.T(key)))
                {
                    visible++;
                }
            }
            Note(visible == SeasonScore.AxisCount,
                 $"{visible}/{SeasonScore.AxisCount} axes visible in the evaluation");
            SeasonScore seasonScore = _app.Sim.Score();
            Debug.Log((object)$"  score: total {seasonScore.Total}, plaque {seasonScore.Plaque}  (wealth {seasonScore.Wealth} reputation {seasonScore.Reputation} regulars {seasonScore.Regulars} crew {seasonScore.Crew} place {seasonScore.Place} resilience {seasonScore.Resilience} signature {seasonScore.Signature})");
        }

        private IEnumerator ToGameScreen()
        {
            int guard = 0;
            while (_app.Ui.Depth > 1 && guard++ < 8)
            {
                _app.Ui.Pop();
                yield return null;
            }
            yield return Settle();
        }

        private IEnumerator CheckAudio()
        {
            AudioSource sfx = null;
            AudioSource[] components = ((Component)_app).GetComponents<AudioSource>();
            foreach (AudioSource val in components)
            {
                if (!val.loop)
                {
                    sfx = val;
                }
            }
            Note(sfx != null, "The audio source is set up");
            if (sfx != null)
            {
                Sfx.Coin();
                yield return null;
                Note(sfx.isPlaying, "A sound effect plays");
            }
            Log($"sound files: {Sfx.FileBackedCount()}/10 " + "(the rest synthesised; Art/ATTRIBUTION.md)");
            Note(_app.Music != null, "The music component is there");
            if (_app.Music != null)
            {
                AudioSource component = ((Component)_app.Music).GetComponent<AudioSource>();
                Note(component != null && component.clip != null, "The music clip was produced");
                Note(component != null && component.isPlaying, "The music is playing");
            }
            yield return null;
        }

        private IEnumerator BrokenSlot()
        {
            string text = Path.Combine(Application.persistentDataPath, "kayit");
            Directory.CreateDirectory(text);
            CultureInfo invariantCulture = CultureInfo.InvariantCulture;
            // THE VERSION HAS TO BE SYMBOLIC.
            //
            // After the decompile there was a "19" CONSTANT here: C# bakes a
            // const INTO THE CALLER and SaveVersion was 18 that day. Once the
            // number stopped rising with SaveVersion, the line that writes "a
            // wrong version" started writing the RIGHT version and three
            // checks broke.
            string contents = string.Join("\u001f", "turk", "12", "500", "7000", DateTime.UtcNow.Ticks.ToString(invariantCulture), (Simulation.SaveVersion + 1).ToString(invariantCulture));
            File.WriteAllText(Path.Combine(text, "yuva" + 3 + ".ozet.json"), contents);
            File.WriteAllText(Path.Combine(text, "yuva" + 3 + ".json"), "{}");
            _app.Ui.Replace(new MainMenuScreen());
            yield return Settle();
            Note(Click(Loc.T("ui.menu.continue")), "Continue in the main menu with a broken save");
            yield return Settle();
            Note(HasText(Loc.T("ui.slot.broken")), "A broken slot looks broken");
            Note(HasText(Loc.T("ui.slot.unloadable")), "The broken slot's button says it cannot be loaded");
            Note(!Click(Loc.T("ui.menu.continue")), "A broken slot does not load");
            yield return Settle();
            SaveStore.Delete(3);
            Note(!SaveStore.Read(3).Exists, "A broken slot can be deleted");
        }

        private IEnumerator Settled()
        {
            float waited = 0f;
            while (_app != null && _app.Rig != null && _app.Rig.Moving && waited < 3f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            yield return null;
        }

        private IEnumerator Settle()
        {
            yield return null;
            yield return null;
            yield return (object)new WaitForEndOfFrame();
        }

        private void RecordShot(string name, byte[] png)
        {
            if (png == null || png.Length == 0)
            {
                return;
            }
            ulong num = 14695981039346656037uL;
            for (int i = 0; i < png.Length; i++)
            {
                num ^= png[i];
                num *= 1099511628211L;
            }
            string key = png.Length + ":" + num.ToString("x16");
            if (_shotHash.TryGetValue(key, out var value))
            {
                if (_shotDup == null)
                {
                    _shotDup = value + " = " + name;
                }
            }
            else
            {
                _shotHash[key] = name;
            }
        }

        private IEnumerator Shot(string name)
        {
            float speed = ((_app != null) ? _app.TimeScale : 0f);
            if (_app != null && speed > 64f)
            {
                _app.TimeScale = GameApp.BaseTimeScale;
                if (_app.Ui != null)
                {
                    _app.Ui.Refresh();
                }
                yield return null;
            }
            yield return (object)new WaitForEndOfFrame();
            Texture2D obj = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] array = ImageConversion.EncodeToPNG(obj);
            File.WriteAllBytes(Path.Combine(_dir, name + ".png"), array);
            RecordShot(name, array);
            UnityEngine.Object.Destroy(obj);
            _shot++;
            if (_app != null && !Mathf.Approximately(_app.TimeScale, speed))
            {
                _app.TimeScale = speed;
                if (_app.Ui != null)
                {
                    _app.Ui.Refresh();
                    yield return Settle();
                }
            }
        }

        /// <summary>
        /// WHICH LANGUAGE DOES A DEVICE WITH NO SAVED CHOICE OPEN IN?
        ///
        /// The decision was plain: "by default the game should start in
        /// English". But that decision lives in a single "return 1;" line,
        /// and above that line there used to be code that guessed the
        /// device's language. Its coming back is possible by accident, and
        /// when it comes back NOTHING raises an error - only a Turkish
        /// phone opens the game in Turkish, which looks right on the
        /// developer's own phone.
        ///
        /// The measurement runs the REAL path: the saved choice is deleted,
        /// the preference logic is called again and the language that comes
        /// out is looked at. Then the saved choice is put back exactly as it
        /// was - the tour does not change the player's choice.
        /// </summary>
        private void CheckDefaultLanguage()
        {
            const string key = "lokanta.dil";
            bool had = PlayerPrefs.HasKey(key);
            int old = had ? PlayerPrefs.GetInt(key) : 0;
            int current = Loc.Language;

            PlayerPrefs.DeleteKey(key);
            Loc.ApplyPreferred();
            string opened = Loc.LanguageCode;

            if (had) PlayerPrefs.SetInt(key, old);
            else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Loc.UseLanguage(current);

            Note(opened == "en", "A device with nothing saved opens in English (" + opened + ")");
        }

        /// <summary>
        /// ARE THERE EMPTY BOXES ON THE CHINESE SCREEN?
        ///
        /// When the language is Chinese the whole tree is drawn with Noto
        /// Sans SC, and that font HAS NO Latin Extended-A - it is not in the
        /// source font either, so it cannot be solved by adding it to the
        /// subset. Sixteen of the ninety-six names in the staff name pool
        /// carry these letters; Loc.PersonName folds them.
        ///
        /// This check measures not that the folding WORKS but its result ON
        /// SCREEN: every visible label is scanned. If the folding is one day
        /// forgotten at a call site, that letter lands here.
        ///
        /// The font checker (tools/art/check_font.py) measures the tables;
        /// this one measures the screen. They are separate questions - the
        /// screen can be broken while the tables are clean, and so it was.
        /// </summary>
        private void CheckNoTofu()
        {
            VisualElement root = ((_app != null && _app.Ui != null) ? _app.Ui.TopView : null);
            if (root == null)
            {
                NoteIf(measured: false, ok: false, "Empty boxes on the Chinese screen: NOT MEASURED");
                return;
            }

            const string banned = "ğĞıİşŞ";
            int labels = 0;
            string found = null;
            foreach (Label l in UQueryExtensions.Query<Label>(root, (string)null, (string)null).ToList())
            {
                string t = ((TextElement)l).text;
                if (string.IsNullOrEmpty(t)) continue;
                labels++;
                if (found != null) continue;
                for (int i = 0; i < t.Length; i++)
                    if (banned.IndexOf(t[i]) >= 0) { found = t; break; }
            }

            NoteIf(labels > 0, found == null,
                   "No undrawable letter on the Chinese screen (" + labels + " labels"
                   + (found == null ? "" : ", example: " + found) + ")");

            // THE LABELS ON SCREEN ARE NOT ENOUGH.
            //
            // There are one or two staff names on the screen at that moment;
            // there are ninety-six in the pool. If the check only looked at
            // what is visible, its saying "clean" would come down to luck -
            // and that is precisely the bug we want to catch. The WHOLE pool
            // goes through the same gate.
            string[] pool = ((_app != null && _app.Content != null)
                              ? _app.Content.StaffNames : null);
            int broken = 0;
            string sample = null;
            if (pool != null)
                for (int i = 0; i < pool.Length; i++)
                {
                    string ad = Loc.PersonName(pool[i]);
                    if (string.IsNullOrEmpty(ad)) continue;
                    for (int j = 0; j < ad.Length; j++)
                        if (banned.IndexOf(ad[j]) >= 0)
                        {
                            broken++;
                            if (sample == null) sample = pool[i] + " -> " + ad;
                            break;
                        }
                }

            NoteIf(pool != null && pool.Length > 0, broken == 0,
                   "The whole Chinese name pool can be drawn ("
                   + (pool == null ? 0 : pool.Length) + " names"
                   + (sample == null ? "" : ", example: " + sample) + ")");
        }

        /// <summary>
        /// DID THE ARABIC LETTERS JOIN UP?
        ///
        /// This hangs on a one-line setting (Edit > Project Settings > UI
        /// Toolkit > Enable Advanced Text Generator) and while the setting
        /// is off NOTHING RAISES AN ERROR: the text is drawn, the letters
        /// are visible, the checks stay green. Only somebody who reads
        /// Arabic sees that the writing is unjoined and in reverse order.
        /// The lesson "a check that does not run looks exactly like a check
        /// that passes" was learned in precisely this form in this project.
        ///
        /// THE MEASUREMENT: the same Arabic word is written twice - once
        /// with the standard generator, once with the advanced one. When
        /// the letters JOIN, the word gets SHORTER (isolated forms are
        /// wider than connected ones). If the two widths are the same there
        /// was no shaping; either the setting is off or the font does not
        /// carry the Arabic tables.
        ///
        /// Why the width: there is no API we can ask about the shaping
        /// itself. The width is the mark the mechanic itself leaves behind.
        /// </summary>
        private IEnumerator CheckArabicShaping()
        {
            VisualElement root = ((_app != null && _app.Ui != null) ? _app.Ui.TopView : null);
            if (root == null)
            {
                NoteIf(measured: false, ok: false, "Arabic shaping: NOT MEASURED, there is no screen");
                yield break;
            }

            // The language's own name: a word that is known to be in the font
            // and whose four letters join to one another.
            string word = Loc.LanguageNames[4];

            Label standard = new Label(word);
            standard.style.unityTextGenerator = TextGeneratorType.Standard;
            standard.style.position = Position.Absolute;
            standard.style.left = -4000f;

            Label advanced = new Label(word);
            advanced.style.unityTextGenerator = TextGeneratorType.Advanced;
            advanced.languageDirection = LanguageDirection.RTL;
            advanced.style.position = Position.Absolute;
            advanced.style.left = -4000f;

            root.Add(standard);
            root.Add(advanced);
            yield return Settle();

            float a = standard.resolvedStyle.width;
            float b = advanced.resolvedStyle.width;
            root.Remove(standard);
            root.Remove(advanced);

            bool measured = a > 1f && b > 1f;
            NoteIf(measured, measured && b < a - 1f,
                   "The Arabic letters join up (isolated " + a.ToString("0.0") +
                   " dp -> connected " + b.ToString("0.0") + " dp)");
            yield return Settle();
        }

        /// <summary>
        /// Takes one screenshot in every language.
        ///
        /// Whether Chinese and Arabic are drawn correctly can only be told
        /// BY LOOKING: an empty box is a character too, and a line in
        /// reverse order is a line too. What can be measured with a number
        /// is measured already; these screenshots are there to be looked
        /// at.
        /// </summary>
        private IEnumerator ShotAllLanguages(string prefix)
        {
            int previous = Loc.Language;
            for (int i = 0; i < Loc.Languages.Length; i++)
            {
                Loc.UseLanguage(i);
                if (_app != null && _app.Ui != null)
                {
                    _app.Ui.ApplyLanguage();
                    _app.Ui.Refresh();
                }
                yield return Settle();
                if (Loc.Languages[i] == "zh") CheckNoTofu();
                yield return Shot(prefix + "-" + Loc.Languages[i]);
            }
            Loc.UseLanguage(previous);
            if (_app != null && _app.Ui != null)
            {
                _app.Ui.ApplyLanguage();
                _app.Ui.Refresh();
            }
            yield return Settle();
        }

        /// <summary>
        /// Measures the strip budget in ALL the languages.
        ///
        /// There used to be two languages and both were measured. With five,
        /// "it fits in Turkish and English" no longer proves anything: the
        /// strip overflows on the longest text and we do not know which
        /// language that text is in. Spanish is longer than English, Chinese
        /// is very short, Arabic in between - rather than guessing which one
        /// overflows we measure all five.
        ///
        /// The ApplyLanguage() call is ESSENTIAL: when the language changes
        /// the font changes too (Chinese) and so does the direction
        /// (Arabic). Calling Refresh() alone meant measuring Chinese with
        /// Rubik - that is, measuring the width of empty boxes.
        /// </summary>
        private IEnumerator CheckStripsAllLanguages(string phase)
        {
            int previous = Loc.Language;
            for (int i = 0; i < Loc.Languages.Length; i++)
            {
                Loc.UseLanguage(i);
                if (_app != null && _app.Ui != null)
                {
                    _app.Ui.ApplyLanguage();
                    _app.Ui.Refresh();
                }
                yield return Settle();
                CheckStrips(phase + "/" + Loc.Languages[i]);
            }
            Loc.UseLanguage(previous);
            if (_app != null && _app.Ui != null)
            {
                _app.Ui.ApplyLanguage();
                _app.Ui.Refresh();
            }
            yield return Settle();
        }

        /// <summary>
        /// EVERY SCREEN, NOT JUST THE GAME SCREEN.
        ///
        /// `CheckStrips`, `ClippedButtons` and `OverlappingButtons` all walk
        /// `GameScreen`'s own `_top` / `_bottom` / `_cards` / `_stats`. Not
        /// one menu, list, settings or dialog screen has ever been measured by
        /// anything - and on 17 September four of them turned out to be
        /// hiding controls below an invisible fold, including ALL FIVE
        /// language buttons on a screen belonging to a five-language game.
        ///
        /// This walks the whole visible tree of whatever screen the tour is
        /// standing on and asks two questions of it:
        ///
        ///   1. Is every visible Button at least 48 dp in BOTH axes? Google's
        ///      floor, and the project's own Theme.Touch is 52. A row that is
        ///      873 dp wide and 44 dp tall fails, because width does not buy
        ///      height - which is exactly the argument the dish rows carried
        ///      in a comment for weeks.
        ///   2. Does every visible element stay inside the panel? An element
        ///      whose bottom edge is past the panel's is content the player
        ///      cannot see and, with the scrollbars hidden, cannot know about.
        ///
        /// IT REPORTS THE WORST OFFENDER BY NAME. "3 controls are too small"
        /// sends the reader hunting; "ui.settings.language 873x44" does not.
        ///
        /// A tolerance of 1 dp on the panel bounds: a control whose edge lands
        /// a rounding error outside is not a defect, and a check that cries
        /// wolf gets switched off.
        /// </summary>
        private void CheckScreenLayout(string where)
        {
            VisualElement root = (_app != null && _app.Ui != null) ? _app.Ui.TopView : null;
            if (root == null) { Note(ok: false, "layout " + where + ": no screen"); return; }

            Rect panel = root.worldBound;
            if (float.IsNaN(panel.width) || panel.width <= 0f)
            {
                Note(ok: false, "layout " + where + ": the screen has no size yet");
                return;
            }

            // NO SCALE DIVISION. `worldBound` is in the PANEL's coordinate
            // space, and MatchPhoneDp has already made that space dp - the
            // panel is 873 x 393 whether the window is 873 px or 2183.
            //
            // The first version divided by the render scale as well, so at 1x
            // it was right by accident and at store scale every size came out
            // 2.5 times too small: a 52 dp button reported as 21 dp and four
            // screens went red for controls that were the correct size all
            // along. A unit error that only shows in one of two run modes is
            // the worst kind, because the mode it is right in is the one that
            // runs most often.

            int small = 0, outside = 0;
            string smallWorst = null, outsideWorst = null;
            float smallest = float.MaxValue, furthest = 0f;

            foreach (VisualElement v in root.Query<VisualElement>().Build())
            {
                if (v.resolvedStyle.display == DisplayStyle.None) continue;
                if (v.resolvedStyle.opacity <= 0.01f) continue;
                Rect r = v.worldBound;
                if (float.IsNaN(r.width) || r.width <= 0f || r.height <= 0f) continue;

                float w = r.width, h = r.height;

                if (v is Button && v.enabledInHierarchy)
                {
                    float least = Mathf.Min(w, h);
                    if (least < 48f)
                    {
                        small++;
                        if (least < smallest)
                        {
                            smallest = least;
                            smallWorst = Name(v) + " " + Mathf.RoundToInt(w)
                                         + "x" + Mathf.RoundToInt(h) + " dp";
                        }
                    }
                }

                // EVERY EDGE, and only for things that carry something. A
                // decorative element bleeding past an edge is a background; a
                // button or a label past one is lost content.
                //
                // THE FIRST VERSION MEASURED THE BOTTOM ONLY, and it was
                // wrong within the hour: the fix for the four overflowing
                // screens made them wrap sideways, the cuisine screen's title
                // and Back button went off the RIGHT edge, and this check
                // reported the screen clean. A check that watches one edge
                // teaches you that the other three are safe.
                if (!(v is Button) && !(v is Label)) continue;
                float over = Mathf.Max(
                    Mathf.Max(r.yMax - panel.yMax, panel.yMin - r.yMin),
                    Mathf.Max(r.xMax - panel.xMax, panel.xMin - r.xMin));
                if (over > 1f)
                {
                    outside++;
                    if (over > furthest)
                    {
                        furthest = over;
                        outsideWorst = Name(v) + " "
                                       + Mathf.RoundToInt(over) + " dp outside";
                    }
                }
            }

            Note(small == 0, "layout " + where + ": every control is 48 dp or more ("
                 + small + " under" + (smallWorst != null ? " - worst " + smallWorst : "") + ")");
            Note(outside == 0, "layout " + where + ": nothing is off the screen ("
                 + outside + (outsideWorst != null ? " - worst " + outsideWorst : "") + ")");
        }

        /// <summary>A name for a report line: the text if it has any, else the type.</summary>
        private static string Name(VisualElement v)
        {
            string t = (v as TextElement)?.text;
            // SQUARE BRACKETS, NOT QUOTATION MARKS. `tools/art/check_font.py`
            // scans the C# string literals for characters the shipped font has
            // to carry, and it cannot tell a label the player reads from a
            // line that only ever reaches a log. A plain " is not in the CJK
            // subset, so writing one here turned the font check red.
            if (!string.IsNullOrEmpty(t))
                return "[" + (t.Length > 24 ? t.Substring(0, 24) : t) + "]";
            if (!string.IsNullOrEmpty(v.name)) return v.name;
            return v.GetType().Name;
        }

        private void CheckStrips(string phase)
        {
            GameScreen gameScreen = ((_app != null && _app.Ui != null) ? (_app.Ui.Top as GameScreen) : null);
            if (gameScreen == null)
            {
                Note(ok: false, "strip not measured: " + phase);
                return;
            }
            float stripHeight = gameScreen.StripHeight;
            Note(stripHeight > 0f && stripHeight <= 220f, $"strip budget ({phase}): {stripHeight:0} dp");
            int overlappingButtons = gameScreen.OverlappingButtons;
            Note(overlappingButtons == 0, (overlappingButtons < 0) ? ("overlapping buttons (" + phase + "): NOT MEASURED, the strip was not built") : ($"overlapping buttons ({phase}): {overlappingButtons} pairs" + ((overlappingButtons > 0) ? (" - " + gameScreen.OverflowDetail) : "")));
            int clippedButtons = gameScreen.ClippedButtons;
            Note(clippedButtons == 0, (clippedButtons < 0) ? ("clipped text (" + phase + "): NOT MEASURED, the strip was not built") : ($"clipped text ({phase}): {clippedButtons}" + ((clippedButtons > 0) ? (" - " + gameScreen.ClipDetail) : "")));
        }

        private bool VisibleText(string contains)
        {
            //IL_0051: Unknown result type (might be due to invalid IL or missing references)
            //IL_0056: Unknown result type (might be due to invalid IL or missing references)
            //IL_0090: Unknown result type (might be due to invalid IL or missing references)
            //IL_0095: Unknown result type (might be due to invalid IL or missing references)
            VisualElement val = ((_app != null && _app.Ui != null) ? _app.Ui.TopView : null);
            if (val == null || string.IsNullOrEmpty(contains))
            {
                return false;
            }
            float height = val.resolvedStyle.height;
            foreach (Label item in UQueryExtensions.Query<Label>(val, (string)null, (string)null).ToList())
            {
                if (!string.IsNullOrEmpty(((TextElement)item).text) && ((TextElement)item).text.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Rect worldBound = ((VisualElement)item).worldBound;
                    if (!(worldBound.height <= 0f) && !(worldBound.yMax <= 0f) && (!(height > 0f) || !(worldBound.yMin >= height)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool HasText(string contains)
        {
            //IL_0045: Unknown result type (might be due to invalid IL or missing references)
            //IL_004a: Unknown result type (might be due to invalid IL or missing references)
            VisualElement val = ((_app != null && _app.Ui != null) ? _app.Ui.TopView : null);
            if (val == null || string.IsNullOrEmpty(contains))
            {
                return false;
            }
            foreach (Label item in UQueryExtensions.Query<Label>(val, (string)null, (string)null).ToList())
            {
                if (!string.IsNullOrEmpty(((TextElement)item).text) && ((TextElement)item).text.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private bool Click(string contains, int index = 0)
        {
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0044: Unknown result type (might be due to invalid IL or missing references)
            VisualElement val = ((_app != null && _app.Ui != null) ? _app.Ui.TopView : null);
            if (val == null)
            {
                return false;
            }
            int num = 0;
            foreach (Button item in UQueryExtensions.Query<Button>(val, (string)null, (string)null).ToList())
            {
                string text = ButtonText(item);
                if (!string.IsNullOrEmpty(text) && text.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0 && num++ == index)
                {
                    NavigationSubmitEvent pooled = NavigationEventBase<NavigationSubmitEvent>.GetPooled((EventModifiers)0);
                    try
                    {
                        ((EventBase)pooled).target = (IEventHandler)(object)item;
                        ((CallbackEventHandler)item).SendEvent((EventBase)(object)pooled);
                    }
                    finally
                    {
                        ((IDisposable)pooled)?.Dispose();
                    }
                    return true;
                }
            }
            return false;
        }

        private static string ButtonText(Button b)
        {
            //IL_0019: Unknown result type (might be due to invalid IL or missing references)
            //IL_001e: Unknown result type (might be due to invalid IL or missing references)
            if (!string.IsNullOrEmpty(((TextElement)b).text))
            {
                return ((TextElement)b).text;
            }
            string text = null;
            foreach (Label item in UQueryExtensions.Query<Label>((VisualElement)(object)b, (string)null, (string)null).ToList())
            {
                if (!string.IsNullOrEmpty(((TextElement)item).text))
                {
                    text = ((text == null) ? ((TextElement)item).text : (text + " " + ((TextElement)item).text));
                }
            }
            return text ?? string.Empty;
        }

        private bool ClickNamed(string name)
        {
            VisualElement val = ((_app != null && _app.Ui != null) ? _app.Ui.TopView : null);
            if (val == null)
            {
                return false;
            }
            Button val2 = UQueryExtensions.Q<Button>(val, name, (string)null);
            if (val2 == null)
            {
                return false;
            }
            NavigationSubmitEvent pooled = NavigationEventBase<NavigationSubmitEvent>.GetPooled((EventModifiers)0);
            try
            {
                ((EventBase)pooled).target = (IEventHandler)(object)val2;
                ((CallbackEventHandler)val2).SendEvent((EventBase)(object)pooled);
            }
            finally
            {
                ((IDisposable)pooled)?.Dispose();
            }
            return true;
        }

        private static bool LicenseTextsLoaded()
        {
            string[] array = new string[4] { "licenses/rubik-ofl", "licenses/noto-sans-sc-ofl", "licenses/kenney-cc0", "licenses/engine-components" };
            foreach (string text in array)
            {
                TextAsset val = Resources.Load<TextAsset>(text);
                if (val == null || val.text.Length < 200)
                {
                    Debug.LogWarning((object)("licence text missing: " + text));
                    return false;
                }
                Resources.UnloadAsset(val);
            }
            return true;
        }

        private void Grow()
        {
            Simulation sim = _app.Sim;
            for (int i = 1; i < sim.TierCount; i++)
            {
                if (sim.TablesAtTier(i) > sim.TableCount)
                {
                    long num = sim.UpgradeCostFor(i);
                    if (num > 0 && sim.Cash >= num * 3)
                    {
                        _app.Send(CommandKind.Expand, i);
                    }
                    break;
                }
            }
            Crew crew = sim.RequiredCrewTomorrow();
            int num2 = 0;
            while (sim.Cooks < crew.Cooks && sim.Cooks + sim.HallStaff < sim.StaffCap && num2++ < 12)
            {
                _app.Send(CommandKind.Hire);
            }
            while (sim.HallStaff < crew.Hall && sim.Cooks + sim.HallStaff < sim.StaffCap && num2++ < 24)
            {
                _app.Send(CommandKind.Hire, 1);
            }
        }

        private void Back()
        {
            if (_app != null && _app.Ui != null && _app.Ui.Depth > 1)
            {
                _app.Ui.Pop();
            }
        }

        private void Log(string what)
        {
            _log.Add("note : " + what);
        }

        private void Note(bool ok, string what)
        {
            if (ok)
            {
                _passed++;
            }
            else
            {
                _failed++;
            }
            _log.Add((ok ? "ok   : " : "FAIL : ") + what);
            if (!ok)
            {
                Debug.LogWarning((object)("Tour: " + what + " did not run"));
            }
        }

        private void Skip(string what)
        {
            _unmeasured++;
            _log.Add("UNMEASURED: " + what);
        }

        private void NoteIf(bool measured, bool ok, string what)
        {
            if (measured)
            {
                Note(ok, what);
            }
            else
            {
                Skip(what);
            }
        }

    }
}
