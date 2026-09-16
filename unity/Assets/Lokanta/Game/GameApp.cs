using System;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Lokanta.Game.Ui;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// The whole game: the content, the simulation, the screens and the
    /// save.
    ///
    /// The reason it is a single MonoBehaviour is Unity's life cycle. The
    /// content is loaded once and, when the cuisine changes, ONLY the part
    /// tied to the cuisine is loaded again; the economy (economy.json, the
    /// traits, the staff roles) is independent of the cuisine.
    ///
    /// The view layer READS the simulation, it does not write to it. The
    /// only thing that writes is a command (docs/23). This class has no
    /// member that touches the simulation apart from Send() - except
    /// AdvanceToNextDay, and that is a change of phase.
    /// </summary>
    public sealed class GameApp : MonoBehaviour
    {
        [Header("Links")]
        public UiRoot Ui;
        public RestaurantView View;
        public CameraRig Rig;

        /// <summary>
        /// YESTERDAY's day report. Valid is false if there is none.
        ///
        /// Why it exists: the game had no comparison at all between a day and
        /// the day before it. In a management game the only way to learn is
        /// "change something, look the next day" - a player who raises the
        /// price from 51 to 58 on the fifth day sees "568" on the sixth and
        /// DOES NOT KNOW whether that is good or bad. When the learning
        /// stops, there is no reason left to come back for a second day.
        ///
        /// IT IS NOT WRITTEN TO THE SAVE: it is a comparison within the
        /// session only. A player coming back from a save spends one day
        /// without a comparison, and then it starts again.
        /// </summary>
        public DayReport Yesterday;

        /// <summary>Is there a report from yesterday?</summary>
        public bool HasYesterday;

        /// <summary>The component that turns the time of day into a picture.</summary>
        public DayLight Light;
        public Music Music;

        [Header("Speed")]
        [Tooltip("How many simulation seconds in one real second")]
        /// <summary>
        /// How many simulation milliseconds per real millisecond. 1 = real
        /// time.
        ///
        /// THE DEFAULT WAS 60 AND IT MADE THE GAME UNPLAYABLE. The service
        /// window is 480,000 sim-ms; at 60 that comes to EIGHT REAL SECONDS.
        /// docs/16 says 90-180 s for service, so the game ran in a fifteenth
        /// of the time it was designed for.
        ///
        /// Everything underneath collapsed: the ENTIRE patience of the most
        /// impatient guest is 10,000 sim-ms, that is 0.17 real seconds. The
        /// table badge's blue-yellow-red transition is shorter than a blink;
        /// touching a table to pick an intervention target (a camera
        /// transition is 0.35 s one way) eats 10% of the service; a notice
        /// bubble lives 3.5 seconds, which is 44% of the service. The whole
        /// interface of the service phase was written to human reaction
        /// times and none of it could keep up.
        ///
        /// 4 = 120 seconds, the middle of the docs/16 band. The old 60 is now
        /// the top of the speed button: "x15".
        /// </summary>
        public float TimeScale = 4f;

        /// <summary>
        /// Is the application quitting? The cleanup paths look at this: on
        /// quit Unity is unloading everything anyway, and calling Destroy by
        /// hand at that moment can make the process crash.
        public static bool Quitting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void WatchQuit()
        {
            Quitting = false;
            Application.quitting += () => { Quitting = true; };
        }

        /// <summary>
        /// The scale the speed label counts as "x1". The number shown to the
        /// player is TimeScale / BaseTimeScale.
        /// </summary>
        public const float BaseTimeScale = 4f;
        public bool Paused = true;

        public Simulation Sim { get; private set; }
        public ContentSet Content { get; private set; }
        public EconomyConfig Economy { get; private set; }
        public string Cuisine { get; private set; }
        public int Slot { get; private set; } = -1;
        public string LoadError { get; private set; }

        private IContentSource _src;
        private float _accumulator;
        private int _lastDay = -1;

        // The event buffer is DRAINED, not read by index: the buffer is a
        // ring and writes over itself when it fills, so an index we kept
        // would silently become invalid.
        private readonly SimEvent[] _events = new SimEvent[256];

        public bool InGame { get { return Sim != null; } }

        // =====================================================================
        private void Awake()
        {
            // THE FRAME RATE IS 30, AND vSYNC IS OFF.
            //
            // While vSyncCount != 0, Android ignores targetFrameRate; the
            // default in the quality setting was 1, so the target was never
            // applied and the game ran at the screen's refresh rate (which can
            // be 90 or 120 Hz).
            //
            // The target is 30: that is the frame-rate floor of docs/19, and
            // the game is looking at a static scene - 60 fps brings nothing,
            // and it costs battery and heat.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;

            try
            {
                _src = new ResourcesContentSource();
                Loc.Load(_src);
                Economy = ContentLoader.LoadEconomy(_src);
            }
            catch (Exception e)
            {
                LoadError = e.Message;
                Debug.LogError("could not load the content: " + e);
            }
        }

        private void Start()
        {
            AudioSource sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            Sfx.Init(sfx);

            if (Ui != null)
            {
                Ui.App = this;
                Ui.Replace(string.IsNullOrEmpty(LoadError)
                    ? (UiScreen)new MainMenuScreen()
                    : new ErrorScreen(LoadError));
            }

            // The self-playing tour. Only when it is asked for on the command line.
            if (Autopilot.Requested && Ui != null)
                gameObject.AddComponent<Autopilot>()
                          .Begin(this, Ui.GetComponent<UnityEngine.UIElements.UIDocument>());
        }

        // =====================================================================
        /// <summary>A new campaign. Slot -1 means it has not been saved yet.</summary>
        public bool StartNew(string cuisine, int slot)
        {
            if (!LoadCuisine(cuisine)) return false;
            Slot = slot;
            Sim = NewSimulation();
            AfterSimChanged();
            return true;
        }

        /// <summary>Loads a saved game.</summary>
        public bool LoadSlot(int slot)
        {
            SlotInfo info = SaveStore.Read(slot);
            if (!info.Exists || info.Broken) return false;
            if (!LoadCuisine(info.Cuisine)) return false;

            Simulation sim = NewSimulation();
            if (!SaveStore.Load(slot, sim)) return false;

            Slot = slot;
            Sim = sim;
            AfterSimChanged();
            return true;
        }

        public bool SaveToSlot(int slot)
        {
            if (Sim == null) return false;

            bool ok = SaveStore.Save(slot, Sim, Cuisine);
            if (ok)
            {
                Slot = slot;
                _saveFailed = false;
                return true;
            }

            // A FAILED SAVE MUST NOT STAY SILENT.
            //
            // SaveStore.Save swallowed every exception and returned false, and
            // NO CALL SITE read that false. A player whose device storage was
            // full would play for thirty days, leave the application and find
            // nothing there - without ever seeing a warning.
            //
            // The notice comes ONCE A DAY: the save happens at the start of
            // every day, and printing the same bubble for sixty days turns the
            // warning into noise.
            if (!_saveFailed)
            {
                _saveFailed = true;
                PushNotice(Loc.T("notice.save_failed"), NoticeTone.Bad);
                Sfx.Cancel();
            }
            return false;
        }

        /// <summary>
        /// Did the last save fail? So that the warning is not repeated.
        /// </summary>
        private bool _saveFailed;

        /// <summary>Leaves the game and goes back to the menu.</summary>
        public void LeaveGame()
        {
            if (Sim != null && Slot >= 0) SaveToSlot(Slot);
            Sim = null;
            Slot = -1;
            Paused = true;
            if (View != null) View.Clear();
        }

        private bool LoadCuisine(string cuisine)
        {
            try
            {
                Content = ContentSetLoader.Load(_src, cuisine);
                Cuisine = cuisine;
                return true;
            }
            catch (Exception e)
            {
                LoadError = e.Message;
                Debug.LogError("could not load the cuisine (" + cuisine + "): " + e);
                return false;
            }
        }

        private Simulation NewSimulation()
        {
            TimingConfig timing = Content.SlotDurationsBp != null
                ? TimingConfig.Default()
                    .WithSlotDurations(Content.SlotDurationsBp)
                    .WithEatMs(Content.EatMs)
                : TimingConfig.Default();

            // The seed comes FROM THE CLOCK: every new campaign has to be
            // different. The saved seed comes back with the save, so a loaded
            // game carries the same campaign on.
            // UtcNow: local time can go BACKWARDS at a daylight-saving change
            // and when the user changes the clock; a jumping clock for a seed
            // means two campaigns starting from the same seed.
            ulong seed = unchecked((ulong)DateTime.UtcNow.Ticks);
            return new Simulation(Economy, Content, timing, seed);
        }

        private void AfterSimChanged()
        {
            Paused = true;
            _accumulator = 0f;
            _lastDay = Sim.Day;
            if (View != null) View.Rebuild();
            if (Rig != null)
            {
                Rig.OpenTables = Sim.TableCount;
                Rig.Overview();
                Rig.Snap();          // so the camera does not fly about as you enter the game
            }
        }

        // =====================================================================
        private void Update()
        {
            if (Sim == null) return;

            // When it expands the camera frames the new wing too. A single int
            // comparison; it changes at most three times in sixty days.
            if (Rig != null) Rig.OpenTables = Sim.TableCount;

            // THE WALKING SPEED KEEPS THE SAME TEMPO AS THE GAME CLOCK.
            //
            // When the player presses x16 the simulation runs sixteen times
            // faster. If the walking stayed in real time the figures would fall
            // tens of seconds behind what is going on and what is on screen
            // would have nothing to do with the simulation. The walking has to
            // stop when the game is paused as well: a waiter walking in a
            // stopped world ruins what pausing is for.
            Walker.GameSpeed = Paused ? 0f : TimeScale / BaseTimeScale;

            // THE TIME OF DAY: the shadows, the colour of the light, the
            // background and the street lamps are all read from a single number
            // (the service progress).
            if (Light != null) Light.Apply(Sim.Phase, Sim.ServiceProgressBp / 10000f);

            if (!Paused && Sim.Phase == DayPhase.Service)
            {
                _accumulator += Time.deltaTime * TimeScale * 1000f;

                // THE BUDGET IS 40, NOT 400.
                //
                // Time.deltaTime is limited by maximumDeltaTime (0.333 s); at the
                // highest speed (x16) that means 213 ticks in a single frame, and
                // the budget allowed 400 - so a single 22 ms frame was possible.
                // And it FEEDS ITSELF: a long frame -> a bigger accumulation -> a
                // longer frame. Enough to set off a save being written or a
                // garbage collection.
                //
                // At x16 the most that has to be processed in one frame is ~21
                // ticks; 40 leaves twice the margin. Time accumulated beyond that
                // is THROWN AWAY, and that is right: the speed key means "get
                // through the day quickly", not full simulation accuracy.
                // THE ACCUMULATED DEBT REALLY IS THROWN AWAY.
                //
                // The comment said "time accumulated beyond that is THROWN AWAY"
                // but the code WAS NOT DOING IT: when the budget filled up nobody
                // cleared what was left in `_accumulator` - only AfterSimChanged
                // reset it. The result was misleading the measurement layer - the
                // debt accumulated at x240 goes on running at 40 ticks a frame (~4
                // sim seconds) even AFTER the tour has dropped back to x1. The
                // whole logic of the liveliness window rests on the assumption
                // "here we are measuring at x1", and that assumption was wrong.
                //
                // The cap: as much accumulation is kept as can be processed in one
                // frame, and the rest is thrown away. The speed key means "get
                // through the day quickly", not full simulation accuracy.
                const int budgetMax = 40;
                float cap = budgetMax * TimingConfig.TickMs;
                if (_accumulator > cap) _accumulator = cap;

                int budget = budgetMax;
                while (_accumulator >= TimingConfig.TickMs && budget-- > 0)
                {
                    _accumulator -= TimingConfig.TickMs;
                    Sim.Tick();
                    if (Sim.ServiceComplete)
                    {
                        // SOMETHING HAS TO SAY THE SERVICE IS OVER.
                        //
                        // The game used to stop silently once the service window had
                        // filled and the hall had emptied: no notice, the strip not
                        // rebuilt, the phase still "Service", the "Pause" button still
                        // saying "Pause" - and Paused already true. The player was
                        // looking at a frozen hall and had to find "Close the day" for
                        // themselves.
                        Paused = true;
                        if (!_serviceEndAnnounced)
                        {
                            _serviceEndAnnounced = true;
                            PushNotice(Loc.T("notice.service_done"), NoticeTone.Good);
                        }
                        break;
                    }
                }
            }

            ReadEvents();
            AgeNotices();
            UpdateMusic();
            CheckSeasonEnd();
        }

        /// <summary>
        /// Turns simulation events into SOUND and TEXT.
        ///
        /// It used to turn them into sound only, and that was the game's
        /// biggest gap: the core produces thirty-three kinds of event and the
        /// view read seven of them to play a sound. Running out of stock, a
        /// staff resignation, an angry exit, an offended guest, late wages -
        /// all of them worked out and thrown away. And on mobile the sound is
        /// mostly OFF, so in practice the feedback was nothing at all.
        ///
        /// Events are the core's only way of speaking outwards (docs/23 5)
        /// and the view layer only READS them.
        /// </summary>
        private void ReadEvents()
        {
            int n = Sim.Events.Drain(_events);

            // The sound is LIMITED: in fast mode hundreds of ticks are
            // processed in a single frame, and playing the same sound fifty
            // times over means clipping and a sudden load on the processor.
            int bell = 0, coin = 0, upset = 0, pour = 0;

            for (int i = 0; i < n; i++)
            {
                SimEvent e = _events[i];
                switch (e.Kind)
                {
                    case SimEventKind.CustomerSeated: if (bell++ < 2) Sfx.DoorBell(); break;
                    case SimEventKind.CustomerPaid: if (coin++ < 2) Sfx.Coin(); break;
                    case SimEventKind.CustomerLeftAngry: if (upset++ < 2) Sfx.Upset(); break;
                    case SimEventKind.FoodServed: if (pour++ < 2) Sfx.Pour(); break;
                    case SimEventKind.StaffLeveledUp: Sfx.LevelUp(); break;
                    case SimEventKind.CreditCollected: if (coin++ < 2) Sfx.Coin(); break;
                    case SimEventKind.DayOpened: Sfx.DayChange(); break;
                    case SimEventKind.TurnedAway: Sfx.Cancel(); break;
                    case SimEventKind.StaffResigned: Sfx.Upset(); break;
                    case SimEventKind.DishUnlocked: Sfx.LevelUp(); break;
                }

                // A story beat IS NOT A NOTICE: a three-second strip would be
                // brushing the game's best line aside. It is kept for the evening
                // and becomes a full-screen card there.
                if (e.Kind == SimEventKind.RegularStoryBeat)
                {
                    _pendingStory = e.A;
                    _pendingBeat = e.B;
                    continue;
                }

                if (Notices.Describe(in e, Content, Sim, out string text, out NoticeTone tone))
                    PushNotice(text, tone);
            }

            if (Sim.Day != _lastDay) _lastDay = Sim.Day;
        }

        // =====================================================================
        /// <summary>
        /// The notices waiting on screen. Not a ring but A SHORT LIST:
        /// showing more than three at once does not get read, and the newest
        /// is the most important.
        /// </summary>
        public const int MaxNotices = 3;

        private readonly string[] _noticeText = new string[MaxNotices];
        private readonly NoticeTone[] _noticeTone = new NoticeTone[MaxNotices];
        private readonly float[] _noticeLeft = new float[MaxNotices];

        public int NoticeCount { get; private set; }
        public string NoticeTextAt(int i) { return _noticeText[i]; }
        public NoticeTone NoticeToneAt(int i) { return _noticeTone[i]; }

        /// <summary>Should the screen read this and rebuild itself?</summary>
        public bool NoticesChanged { get; private set; }
        public void NoticesSeen() { NoticesChanged = false; }

        /// <summary>
        /// Has the end of service been announced once? Announcing it again
        /// every frame would fill the screen with bubbles.
        /// </summary>
        private bool _serviceEndAnnounced;

        private void PushNotice(string text, NoticeTone tone)
        {
            // The same text is not repeated when it comes twice in a row: in
            // a hall of fourteen tables "out of ingredients" can land three
            // times in the same second.
            if (NoticeCount > 0 && _noticeText[0] == text)
            {
                _noticeLeft[0] = NoticeSeconds;
                return;
            }

            for (int i = MaxNotices - 1; i > 0; i--)
            {
                _noticeText[i] = _noticeText[i - 1];
                _noticeTone[i] = _noticeTone[i - 1];
                _noticeLeft[i] = _noticeLeft[i - 1];
            }
            _noticeText[0] = text;
            _noticeTone[0] = tone;
            _noticeLeft[0] = NoticeSeconds;

            if (NoticeCount < MaxNotices) NoticeCount++;
            NoticesChanged = true;
        }

        private const float NoticeSeconds = 3.5f;

        private void AgeNotices()
        {
            bool dropped = false;
            for (int i = 0; i < NoticeCount; i++)
            {
                _noticeLeft[i] -= Time.deltaTime;
                if (_noticeLeft[i] <= 0f) { NoticeCount = i; dropped = true; break; }
            }
            if (dropped) NoticesChanged = true;
        }

        /// <summary>
        /// Is the campaign over? If it is, the year-end evaluation opens.
        ///
        /// This link once DID NOT EXIST AT ALL: EndScreen had been written,
        /// the scoring designed, CampaignDays sat in the content - and the
        /// sixtieth day came and went with nothing happening. The game ran on
        /// for ever and had no close.
        ///
        /// The flag is IN THE SIMULATION and goes into the save; the same
        /// screen does not come up again the second time the game is opened.
        /// </summary>
        private void CheckSeasonEnd()
        {
            if (Sim == null || Ui == null) return;
            if (!Sim.SeasonJustEnded) return;

            // THE FLAG IS SET WHEN THE SCREEN CLOSES, NOT WHEN IT OPENS.
            //
            // The order used to be: mark it, SAVE, then open the screen. So
            // the "seen" fact was written to disk before the player had read a
            // single line. If the phone locks at that moment (or someone rings
            // and the application is pushed to the background),
            // OnApplicationPause saves once more, Android kills the
            // application and SeasonJustEnded never returns true again.
            //
            // What is lost is not small: a seven-axis evaluation, the plaque,
            // the single close of sixty days. And there was no other call site
            // that opened EndScreen.
            // THE SCREEN IS OPENED ONCE.
            //
            // The fix above (setting the flag when the screen closes) was
            // right, but it opened a door: SeasonJustEnded stays true UNTIL
            // THE SCREEN CLOSES, and this method is called from Update
            // UNCONDITIONALLY. So from day 61 onwards a new EndScreen was
            // built every frame - 1800 of them a minute at 30 fps, each one
            // calling sim.Score() and building the game's heaviest panel.
            //
            // For the player the result was worse still: "carry on in free
            // play" does a single Ui.Pop() and THE SAME screen appears
            // underneath. A sixty-day campaign ended on a screen you could not
            // get out of.
            //
            // GameScreen's story card already has the same guard (Ui.Top ==
            // this); it was missing here because this method is called from
            // GameApp, that is, regardless of what is on top.
            if (Ui.Top is EndScreen) return;

            Paused = true;
            Ui.Push(new EndScreen());
        }

        /// <summary>
        /// The story beat opened today; -1 if there is none.
        ///
        /// AT MOST ONE a day: if two beats fall together the second waits for
        /// the next day. Two cards one after the other make both of them
        /// unreadable.
        /// </summary>
        private int _pendingStory = -1;
        private int _pendingBeat;

        public bool HasStory { get { return _pendingStory >= 0; } }
        public int StoryRegular { get { return _pendingStory; } }
        public int StoryBeat { get { return _pendingBeat; } }

        public void StorySeen() { _pendingStory = -1; }

        private void UpdateMusic()
        {
            if (Music == null) return;
            // The intensity = how full the hall is. The music follows the
            // state of the game; a fixed loop would play just as calmly when
            // service had jammed.
            float busy = Sim.TableCount > 0
                ? Sim.OccupiedTables / (float)Sim.TableCount : 0f;
            Music.Intensity = Mathf.Lerp(Music.Intensity, busy, Time.deltaTime * 0.6f);
        }

        // =====================================================================
        /// <summary>
        /// SAVE WHEN THE APPLICATION GOES INTO THE BACKGROUND.
        ///
        /// Android kills games in the background regularly. Without this
        /// hook, THE WHOLE OF THAT DAY was lost when the phone rang or the
        /// user switched applications: the ingredients bought, the menu
        /// built, the staff hired, the day's takings. This is the number one
        /// cause of one-star reviews on mobile management games.
        ///
        /// OnApplicationPause rather than OnApplicationQuit: on Android the
        /// quit hook is not reliable, the pause hook is.
        ///
        /// The write itself is atomic (SaveStore.WriteAtomic), so triggering
        /// it often carries no risk of a corrupt save.
        /// </summary>
        /// <summary>
        /// It saves ONCE on the way into the background.
        ///
        /// On Android, putting the application into the background triggers
        /// BOTH the OnApplicationPause(true) AND the OnApplicationFocus(false)
        /// callbacks, so the save was written twice - at exactly the moment
        /// the operating system is getting ready to kill the application.
        ///
        /// A save is not cheap: the simulation state is ~12,500 numeric
        /// fields and the write is on the main thread, 30-60 ms on a low-end
        /// phone. Twice that, at exactly the worst moment to lose.
        ///
        /// The flag drops when it comes back to the front, so the next trip
        /// into the background saves again.
        /// </summary>
        private bool _savedOnBackground;

        private void OnApplicationPause(bool paused)
        {
            if (!paused) { _savedOnBackground = false; return; }
            SaveOnBackground();
        }

        /// <summary>
        /// A SAVE UNDER MEMORY PRESSURE TOO.
        ///
        /// Android can kill an application under memory pressure even IN THE
        /// FOREGROUND; the target device has 3 GB (docs/19). On that path the
        /// last save is the one from the start of the day - a day's play is
        /// lost and the player does not understand why.
        ///
        /// SaveOnBackground already guards against a double save.
        /// </summary>
        private void OnEnable()
        {
            Application.lowMemory += OnLowMemory;
        }

        private void OnDisable()
        {
            Application.lowMemory -= OnLowMemory;

            // THE RENDER SCALE IS PUT BACK.
            //
            // Quality.ApplyZoom changes the URP asset's renderScale, and that
            // asset is a SINGLE file in the project. In the editor, if the
            // player zooms in and then leaves play mode, the value stays at
            // 1.0, is written TO DISK at the next asset save, and the 0.8
            // mobile budget quietly disappears. Restore had been written but
            // was never called from anywhere.
            Quality.Restore();
        }

        private void OnLowMemory()
        {
            SaveOnBackground();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) { _savedOnBackground = false; return; }
            SaveOnBackground();
        }

        /// <summary>
        /// Is the smoke tour running?
        ///
        /// Its only effect: switching off PAUSE ON LOSING FOCUS. That is the
        /// right behaviour for the game itself (when an application is put
        /// into the background on a phone the game should stop and save), but
        /// the tour runs on the desktop and, when another window comes to the
        /// front, the measurement measures a frozen world.
        /// </summary>
        public static bool SmokeTour;

        private void SaveOnBackground()
        {
            if (SmokeTour) return;
            Paused = true;
            if (_savedOnBackground) return;
            _savedOnBackground = true;
            if (Sim != null && Slot >= 0) SaveToSlot(Slot);
        }

        /// <summary>
        /// The table the interventions are aimed at. -1 = no selection.
        ///
        /// VIEW STATE, not simulation state: docs/23 7.2 does not count the
        /// camera and the selection as commands, so it is not saved and does
        /// not affect replay. Had it been saved, the same sequence of
        /// commands could give two different results.
        ///
        /// With no selection the interventions fall back to the old
        /// behaviour: the table with the least patience left. So it can be
        /// played without zooming in - the selection is not an OBLIGATION but
        /// a REFINEMENT.
        /// </summary>
        public int SelectedTable = -1;

        /// <summary>
        /// Keeps the selection valid. If the table has emptied or service
        /// has ended the selection drops - an "offer tea" button aimed at an
        /// empty table would eat the player's allowance without telling them
        /// anything.
        public int ValidSelection()
        {
            if (Sim == null || SelectedTable < 0) return -1;
            if (Sim.Phase != DayPhase.Service) { SelectedTable = -1; return -1; }
            if (SelectedTable >= Sim.TableCount) { SelectedTable = -1; return -1; }

            CustomerStage st = Sim.TableStage(SelectedTable);
            if (st == CustomerStage.None || st == CustomerStage.Done
                || st == CustomerStage.LeftAngry)
            {
                SelectedTable = -1;
                return -1;
            }
            return SelectedTable;
        }

        public void Send(CommandKind kind, int a = 0, int b = 0, int c = 0)
        {
            if (Sim == null) return;
            Sim.Apply(new Command(Sim.TickIndex, kind, a, b, c));
        }

        public void OpenService()
        {
            _serviceEndAnnounced = false;
            Send(CommandKind.OpenService);
            Paused = false;
            Sfx.Confirm();
        }

        public void CloseDay()
        {
            Send(CommandKind.CloseDay);
            Paused = true;
            Sfx.Confirm();
        }

        public void NextDay()
        {
            if (Sim == null) return;

            // TODAY'S REPORT MOVES TO YESTERDAY.
            //
            // It has to be taken before the day advances: AdvanceToNextDay
            // resets the counters and the report can no longer be built.
            Yesterday = Sim.BuildDayReport();
            HasYesterday = true;

            Sim.AdvanceToNextDay();
            if (Slot >= 0) SaveToSlot(Slot);      // save at the start of every day
            if (View != null) View.Rebuild();
        }

        public void RestockRecommended()
        {
            if (Sim == null) return;
            long before = Sim.Cash;

            // ONE COMMAND, not fifty.
            //
            // One OrderIngredient used to be sent per ingredient, and the daily
            // command limit is 256: pressing this button five times used up the
            // day's budget, and after that EVERY command - the price, the menu,
            // hiring, equipment, expansion, interventions and the book included
            // - was silently rejected. For a player short of money it was
            // easier still: even when the purchase is rejected the command IS
            // WRITTEN TO THE LOG, the stock is not filled so the "shortfall" is
            // still there, and the player presses again.
            Send(CommandKind.OrderRecommended);

            if (Sim.Cash != before) Sfx.Coin();
        }
    }
}
