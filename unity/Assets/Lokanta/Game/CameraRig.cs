using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// A two-step camera. docs/31-rooms-and-camera.md and the user's own
    /// sentence: "when everything is seen at once, the player touches and
    /// so brings the camera closer to that modular part."
    ///
    ///   OVERVIEW  the whole plot is visible. The touch target is a ROOM.
    ///   ROOM      zoomed in on one room. The touch target is a TABLE SET.
    ///
    /// The angle, the field of view and the distance come from CameraFit -
    /// the very calculation the touch-target measurement uses. Writing
    /// separate numbers here invalidates that measurement; that is what
    /// happened in the first version and the restaurant covered only 38%
    /// of the frame.
    ///
    /// The overview frames the OPEN rooms, not the whole plot - rooms that
    /// have not been opened are not even drawn
    /// (RestaurantView.BuildFloors).
    ///
    /// The touch target DOES NOT SUFFER from this, quite the opposite: the
    /// measurement (the tool of docs/31, Editor/RoomLayout.cs) says the
    /// smallest open room is 71 dp with the interface bars in place, and
    /// that it DOES NOT CHANGE with the tier. With the old setting that
    /// same number fell from 52 to 48, touching Google's minimum exactly.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        public float MoveSeconds = 0.35f;

        /// <summary>
        /// Is a sliding transition under way?
        ///
        /// So the tour can ask: waiting out the transition BY TIME
        /// (MoveSeconds + 0.1) was not enough on one run and the "the camera
        /// came back to the overview framing" check went red with a 13 m
        /// deviation - it had been measured while the camera was halfway
        /// along. Instead of waiting for the time, you have to wait for the
        /// STATE.
        /// </summary>
        public bool Moving { get { return _t < 1f; } }

        /// <summary>-1 means the overview.</summary>
        public int FocusRoom { get; private set; } = -1;

        private Vector3 _from, _to;
        private Quaternion _fromRot = Quaternion.identity;
        private float _t = 1f;
        private Camera _cam;
        private float _aspect;

        /// <summary>
        /// The fraction the interface covers at the top and at the bottom.
        /// The game screen measures its own bars and reports them here - the
        /// camera does not assume a fixed number, because the height of the
        /// bars changes with the content (the remaining-allowance line, the
        /// intervention buttons).
        private float _top01, _bottom01;

        public void SetSafeArea(float top01, float bottom01)
        {
            if (Mathf.Abs(top01 - _top01) < 0.002f
                && Mathf.Abs(bottom01 - _bottom01) < 0.002f) return;

            _top01 = top01;
            _bottom01 = bottom01;
            Begin();
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();

            // The angle, the field of view and the distance come from THE SAME
            // source as the measurement (CameraFit). Writing separate numbers
            // here invalidates the touch-target measurement - which is exactly
            // what happened in the first version, and the restaurant covered
            // 38% of the frame.
            _cam.fieldOfView = CameraFit.FieldOfView;
            transform.rotation = LookRotation;

            _aspect = _cam.aspect;
            transform.position = TargetPosition(-1);
            _from = _to = transform.position;
        }

        private void Update()
        {
            // If the screen is rotated the framing is worked out again. Had we
            // written a fixed position, the plot would spill out of the frame
            // on a landscape-portrait change.
            if (!Mathf.Approximately(_aspect, _cam.aspect))
            {
                _aspect = _cam.aspect;
                Begin();
            }

            if (_t < 1f)
            {
                _t += Time.deltaTime / Mathf.Max(0.01f, MoveSeconds);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_t));
                transform.position = Vector3.Lerp(_from, _to, k);
                transform.rotation = Quaternion.Slerp(_fromRot, LookRotation, k);
            }

            // A TOUCH THAT LANDS ON THE INTERFACE DOES NOT REACH THE CAMERA.
            //
            // It used to reach it unconditionally: pressing the "Open service"
            // button also selected the room behind it and the camera flew
            // there.
            HandlePinch();

            if (TouchDown(out Vector3 screen) && !OverUi(screen)) HandleTouch(screen);
        }

        // ---------------------------------------------------------------------
        // TWO FINGERS: ZOOM AND TURN.
        //
        // The limits are deliberate:
        //
        //   ZOOM 0.45 - 1.00. The 1.00 is the measured default framing
        //   (docs/31); you cannot go FURTHER OUT because that framing
        //   already shows everything and pulling back would only make the
        //   touch target smaller. 0.45 is about 2.2x magnification.
        //
        //   TURN +-35 degrees. Leaving it completely free breaks two
        //   things: the touch-target measurement was made at a particular
        //   angle (docs/31), and seen from behind the kitchen comes in
        //   front of the hall. +-35 satisfies the "let me look from the
        //   other side" wish while leaving the floor plan readable.
        //
        // Going back to the overview RESETS both: there has to be a way for
        // the player to get back to somewhere known at any time.
        private float _zoom = 1f;
        private float _yawOffset;

        private const float MinZoom = 0.45f;
        private const float MaxZoom = 1.00f;
        private const float MaxYaw = 35f;

        /// <summary>The zoom ratio: 1 is the default framing.</summary>
        public float Zoom { get { return _zoom; } }

        /// <summary>The angle the player has turned to, in degrees.</summary>
        public float YawOffset { get { return _yawOffset; } }

        private Vector3 TargetPosition(int room)
        {
            Bounds b = room < 0
                ? CameraFit.OpenBounds(_tables)
                : CameraFit.RoomBounds(room);

            Vector3 p = CameraFit.Position(b, _aspect, _top01, _bottom01);
            Vector3 center = b.center;

            // The zoom pulls the camera TOWARDS the target. Narrowing the
            // field of view would zoom in as well, but it changes the
            // perspective and invalidates the calculation the touch-target
            // measurement uses.
            p = center + (p - center) * _zoom;

            if (Mathf.Abs(_yawOffset) > 0.01f)
                p = center + Quaternion.Euler(0f, _yawOffset, 0f) * (p - center);

            return p;
        }

        /// <summary>The direction the camera looks: the base angle plus the player's turn.</summary>
        private Quaternion LookRotation
        {
            get { return Quaternion.Euler(0f, _yawOffset, 0f) * CameraFit.Rotation; }
        }

        private bool _pinching;
        private float _lastPinchDist, _lastPinchAngle;

        private void HandlePinch()
        {
            float zoomDelta, twist;
            if (!Gesture(out zoomDelta, out twist))
            {
                _pinching = false;
                return;
            }
            ApplyGesture(zoomDelta, twist);
        }

        /// <summary>
        /// Applies the gesture. Both a finger and the tour come THROUGH
        /// HERE.
        ///
        /// Leaving a separate input path would have meant the check never
        /// measuring the real code: the tour cannot produce touches, and
        /// everything to do with the zoom limit would have gone unmeasured.
        /// </summary>
        public void ApplyGesture(float zoomDelta, float twist)
        {
            float previousZoom = _zoom;
            float previousYaw = _yawOffset;
            _zoom = Mathf.Clamp(_zoom - zoomDelta, MinZoom, MaxZoom);
            _yawOffset = Mathf.Clamp(_yawOffset + twist, -MaxYaw, MaxYaw);

            if (Mathf.Approximately(previousZoom, _zoom)
                && Mathf.Approximately(previousYaw, _yawOffset)) return;

            // IMMEDIATE, with no smoothing: a lag in the picture under the
            // finger makes the zoom feel heavy.
            _to = TargetPosition(FocusRoom);
            transform.position = _to;
            transform.rotation = LookRotation;
            _from = _to;
            _t = 1f;

            Quality.ApplyZoom(_zoom);
        }

        /// <summary>The lower limit of the zoom. So the tour can ask.</summary>
        public static float MinZoomLimit { get { return MinZoom; } }

        /// <summary>The limit of the turn, in degrees. So the tour can ask.</summary>
        public static float MaxYawLimit { get { return MaxYaw; } }

        /// <summary>
        /// A two-finger (or mouse wheel) gesture.
        /// zoom positive = closer. twist = degrees.
        /// </summary>
        private bool Gesture(out float zoom, out float twist)
        {
            zoom = 0f;
            twist = 0f;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null && ts.touches.Count >= 2
                && ts.touches[0].press.isPressed && ts.touches[1].press.isPressed)
            {
                Vector2 a = ts.touches[0].position.ReadValue();
                Vector2 b = ts.touches[1].position.ReadValue();
                Read(a, b, ref zoom, ref twist);
                return true;
            }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                float w = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(w) > 0.01f)
                {
                    zoom = Mathf.Clamp(w, -1f, 1f) * 0.06f;
                    return true;
                }
            }
            return false;
#else
            if (Input.touchCount >= 2)
            {
                Read(Input.GetTouch(0).position, Input.GetTouch(1).position,
                     ref zoom, ref twist);
                return true;
            }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)
            {
                zoom = Mathf.Clamp(wheel, -1f, 1f) * 0.06f;
                return true;
            }
            return false;
#endif
        }

        /// <summary>Reads the change from the distance and the angle between the two fingers.</summary>
        private void Read(Vector2 a, Vector2 b, ref float zoom, ref float twist)
        {
            float d = Vector2.Distance(a, b);
            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

            if (_pinching)
            {
                // DIVIDED BY THE SCREEN DIAGONAL: the same finger movement has
                // to zoom by the same amount at every resolution.
                float diagonal = Mathf.Sqrt(
                    Screen.width * (float)Screen.width
                    + Screen.height * (float)Screen.height);
                zoom = (d - _lastPinchDist) / Mathf.Max(1f, diagonal) * 2.2f;
                twist = Mathf.DeltaAngle(_lastPinchAngle, ang);
            }
            _lastPinchDist = d;
            _lastPinchAngle = ang;
            _pinching = true;
        }

        public void FocusOn(int room)
        {
            FocusRoom = room;
            Begin();
        }

        /// <summary>
        /// Starts the sliding transition: FROM WHERE and FROM WHICH ANGLE.
        ///
        /// The angle HAS TO travel too. Only the position used to slide, and
        /// the angle was written by the finger gesture alone - so when the
        /// player had turned the camera and then pressed "overview", the
        /// camera went to the right place but WENT ON LOOKING SIDEWAYS.
        /// There was no cure for a camera you had turned.
        ///
        /// The tour's check had not seen this because it looked at the
        /// fields (Zoom, YawOffset); both of those were being reset. What
        /// has to be measured is the camera ITSELF.
        /// </summary>
        private void Begin()
        {
            _from = transform.position;
            _fromRot = transform.rotation;
            _to = TargetPosition(FocusRoom);
            _t = 0f;
        }

        /// <summary>
        /// The overview. The zoom and the turn are RESET too: there has to
        /// be a way for the player to get back to somewhere known at any
        /// time, otherwise there is no cure for a camera they have turned.
        /// </summary>
        public void Overview()
        {
            _zoom = 1f;
            _yawOffset = 0f;
            Quality.ApplyZoom(_zoom);
            FocusOn(-1);
        }

        /// <summary>
        /// The number of open tables. The overview framing is built from it:
        /// the closed wings do not take up room on screen.
        ///
        /// The initial value is the first tier's table count; AfterSimChanged
        /// writes the real value and settles it at once with Snap, so the
        /// camera does not fly about as you enter the game. The change during
        /// an expansion, though, SLIDES - the player should see the
        /// restaurant grow.
        /// </summary>
        public int OpenTables
        {
            get { return _tables; }
            set
            {
                if (value == _tables) return;
                _tables = value;
                if (FocusRoom < 0) FocusOn(-1);
            }
        }

        private int _tables = 4;

        /// <summary>
        /// The TARGET position of the overview.
        ///
        /// For the check: the question "has the camera come back to the
        /// overview" has to be compared with the CURRENT target, not with a
        /// position recorded earlier. When the restaurant grows the overview
        /// framing changes too and the old position is no longer the right
        /// answer - the check measured a 3.74 m deviation and went red, when
        /// the camera was exactly where it should have been.
        /// </summary>
        public Vector3 OverviewPosition { get { return TargetPosition(-1); } }

        /// <summary>Skips the sliding transition; the camera settles on its target now.</summary>
        public void Snap()
        {
            _to = TargetPosition(FocusRoom);
            transform.position = _to;
            transform.rotation = LookRotation;
            _from = _to;
            _fromRot = transform.rotation;
            _t = 1f;
        }

        /// <summary>Is there an interface element at this point on screen?</summary>
        private bool OverUi(Vector3 screen)
        {
            if (_ui == null) _ui = FindFirstObjectByType<Ui.UiRoot>();
            return _ui != null && _ui.BlocksPoint(screen);
        }

        private Ui.UiRoot _ui;

        private void HandleTouch(Vector3 screen)
        {
            Ray ray = _cam.ScreenPointToRay(screen);
            bool anyHit = Physics.Raycast(ray, out RaycastHit hit, 200f);

            if (FocusRoom >= 0)
            {
                // WHILE IN A ROOM, A TABLE IS LOOKED FOR FIRST.
                //
                // Touching a table makes it the target of the interventions;
                // touching anywhere else takes you back out as before. HAVING TO
                // LOOK for a back button is the most complained-about thing on
                // mobile, which is why the "touch an empty place = back" rule
                // stands.
                if (anyHit)
                {
                    TableTouch tt = hit.collider.GetComponentInParent<TableTouch>();
                    if (tt != null && SelectTable(tt.TableIndex)) return;

                    // MISSING THE TABLE DOES NOT TAKE YOU BACK OUT.
                    //
                    // Every miss used to call Overview() and trial and error was
                    // punished: a player learning to touch a table went back to
                    // the start on every miss, the camera flew back and they had
                    // to zoom in again.
                    //
                    // Now the INSIDE OF THE ROOM is safe: touching the floor does
                    // nothing. To go back out you have to touch OUTSIDE the room -
                    // and so the "no hunting for a back button" rule still
                    // stands.
                    RoomTouch inside = hit.collider.GetComponentInParent<RoomTouch>();
                    if (inside != null && inside.RoomIndex == FocusRoom) return;
                }
                Overview();
                return;
            }

            if (!anyHit) return;
            RoomTouch t = hit.collider.GetComponentInParent<RoomTouch>();
            if (t != null) FocusOn(t.RoomIndex);
        }

        /// <summary>
        /// Makes the table the target of an intervention. Only DURING
        /// SERVICE and only on an OCCUPIED table; selecting an empty table
        /// gains the player nothing and would steal the gesture for going
        /// back out.
        /// </summary>
        private bool SelectTable(int index)
        {
            if (_ui == null) _ui = FindFirstObjectByType<Ui.UiRoot>();
            GameApp app = _ui != null ? _ui.App : null;
            if (app == null || app.Sim == null) return false;
            if (app.Sim.Phase != Lokanta.Core.Sim.DayPhase.Service) return false;
            if (index < 0 || index >= app.Sim.TableCount) return false;

            Lokanta.Core.Sim.CustomerStage st = app.Sim.TableStage(index);
            if (st == Lokanta.Core.Sim.CustomerStage.None
                || st == Lokanta.Core.Sim.CustomerStage.Done
                || st == Lokanta.Core.Sim.CustomerStage.LeftAngry)
                return false;

            // A second touch on the same table DROPS the selection: the way
            // out of a selection should be the same as the way into it.
            app.SelectedTable = app.SelectedTable == index ? -1 : index;
            _ui.Refresh();
            return true;
        }

        /// <summary>
        /// A touch or the mouse. The new Input System package is in the
        /// project but the old input is switched on too; this is the
        /// shortest way of supporting both.
        /// </summary>
        private static bool TouchDown(out Vector3 screen)
        {
            screen = default;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
            {
                screen = ts.primaryTouch.position.ReadValue();
                return true;
            }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screen = mouse.position.ReadValue();
                return true;
            }
            return false;
#else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                screen = Input.GetTouch(0).position;
                return true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                screen = Input.mousePosition;
                return true;
            }
            return false;
#endif
        }
    }
}
