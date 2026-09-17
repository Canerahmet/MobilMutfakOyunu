using System.Collections.Generic;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// WALKS a figure along a set of waypoints.
    ///
    /// Why a separate component: Figure knows WHAT a figure is doing (its
    /// pose), Walker knows WHERE IT IS. The two are separate because a
    /// cook standing still and a seated guest both use Figure, but only
    /// one of them walks.
    ///
    /// THE SIMULATION KNOWS NOTHING ABOUT METRES. The core says "the
    /// waiter is dealing with table 3"; where that table is and how to get
    /// there is entirely the view layer's business (docs/23: no Unity in
    /// the core, no floating point). This class writes NOTHING back to the
    /// simulation - it only walks towards the target it reads.
    ///
    /// THE SPEED SCALES WITH THE GAME'S SPEED. When the player presses x16
    /// the simulation runs sixteen times faster; if the walking stayed in
    /// real time the figures would fall tens of seconds behind the
    /// simulation and what the player sees on screen would no longer have
    /// anything to do with what is happening. The multiplier has a
    /// ceiling: at very high speed the figures look as if they are
    /// sliding, so beyond a certain point they TELEPORT (Warp) - a walk
    /// too fast to see is not a walk, it is a judder.
    /// </summary>
    public sealed class Walker : MonoBehaviour
    {
        /// <summary>The component driving this body's pose.</summary>
        public Figure Body;

        /// <summary>Metres per second, at x1 speed. A calm service walk.</summary>
        public float Speed = 1.15f;

        /// <summary>
        /// The game's speed multiplier. Written from GameApp every frame;
        /// static, because every figure in the scene uses the same clock and
        /// holding one link per figure means fifty references at fifty
        /// objects.
        public static float GameSpeed = 1f;

        /// <summary>
        /// Above this multiplier the walk IS NOT DRAWN, it teleports.
        ///
        /// At x4 a figure covers ~4.6 m a second; at 30 fps that is 15 cm a
        /// frame. Above that the distance between steps grows larger than
        /// the figure itself and the movement reads as a "jump" rather than
        /// a walk.
        /// </summary>
        public const float TeleportAbove = 4.5f;

        /// <summary>
        /// The highest time multiplier THIS figure may follow.
        ///
        /// The default is infinite: everyone in the hall follows the game
        /// clock, because what they are doing is work that has a counterpart
        /// in the simulation, and their falling behind would be a lie on
        /// screen.
        ///
        /// What passes along the street has NO counterpart in the simulation
        /// - they are scenery. The cap is there for them and the reason was
        /// measured: when the player took the speed above TeleportAbove,
        /// Walker stopped drawing the walk and teleported straight to the
        /// end of the path; on the street the end of the path is two points,
        /// so five pedestrians piled up ON TOP OF one another at those two
        /// points. The tour measured it as "4 pairs interpenetrating at
        /// worst".
        /// </summary>
        public float SpeedCap = float.PositiveInfinity;

        /// <summary>
        /// The ground speed actually used THIS FRAME (m/s).
        ///
        /// The check reads this; it does not RECALCULATE Speed x GameSpeed.
        /// The reason was measured: GameApp.Update writes GameSpeed and its
        /// order within the frame is not guaranteed relative to
        /// Walker.Update. A check that recalculated measured the clip's
        /// tempo against the previous speed on the frame the game speed
        /// changed and reported 123% "slip" - when in fact the figure had
        /// covered the right distance on that frame and the next frame put
        /// it right anyway.
        ///
        /// What has to be measured is "did the clip's tempo match the
        /// distance COVERED" - and that is exactly this field.
        /// </summary>
        public float LastGroundSpeed { get; private set; }

        private readonly List<Vector3> _path = new List<Vector3>(4);
        private int _at;
        private System.Action _onArrive;
        private float _faceYaw;

        /// <summary>Has it yet to arrive?</summary>
        public bool Moving { get { return _at < _path.Count; } }

        /// <summary>Where the current path began. The figure's own position if it has none.</summary>
        public Vector3 Origin { get; private set; }

        /// <summary>The last point of the current path; where it stands if there is no path.</summary>
        public Vector3 Destination
        {
            get
            {
                return _path.Count > 0 ? _path[_path.Count - 1]
                                       : transform.localPosition;
            }
        }

        // =====================================================================
        /// <summary>Places it instantly; cancels the path if there is one.</summary>
        public void Warp(Vector3 local, float yaw)
        {
            if (Body != null) Body.ResetPlaybackSpeed();
            _path.Clear();
            _at = 0;
            _onArrive = null;
            transform.localPosition = local;
            _faceYaw = yaw;
            transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>
        /// Walks along the given path and calls onArrive at the end.
        ///
        /// finalYaw: the direction it turns to on arrival. If it is NaN it
        /// stays facing the direction of the last step - which is the right
        /// thing for a waiter who has walked up to a table.
        /// </summary>
        public void GoTo(List<Vector3> waypoints, float finalYaw,
                         System.Action onArrive)
        {
            // WHERE THIS WALK STARTED. Kept because the table a figure GOT UP
            // FROM is inside its own chairs by definition, exactly as the one
            // it is walking to is - see RestaurantView.Intruding, which
            // excuses both and nothing else.
            Origin = transform.localPosition;
            _path.Clear();
            if (waypoints != null) _path.AddRange(waypoints);
            _at = 0;
            _onArrive = onArrive;
            _finalYaw = finalYaw;

            if (_path.Count == 0)
            {
                Arrive();
                return;
            }
            if (Body != null) Body.Set(Figure.Pose.Walk);
        }

        private float _finalYaw = float.NaN;

        /// <summary>Cuts the walk off and leaves it where it stands.</summary>
        public void Stop()
        {
            _path.Clear();
            _at = 0;
            _onArrive = null;
            if (Body != null) Body.ResetPlaybackSpeed();
        }

        // =====================================================================
        private void Update()
        {
            if (_at >= _path.Count) return;

            // When the game is paused THE WALK STOPS TOO (GameSpeed = 0).
            if (GameSpeed <= 0.001f) return;
            float multiplier = Mathf.Min(SpeedCap, Mathf.Max(0.5f, GameSpeed));
            Vector3 target = _path[_at];
            Vector3 here = transform.localPosition;

            // At high speed the walk does not read: teleport.
            if (multiplier > TeleportAbove)
            {
                transform.localPosition = _path[_path.Count - 1];
                _at = _path.Count;
                Arrive();
                return;
            }

            Vector3 delta = target - here;
            delta.y = 0f;
            float distance = delta.magnitude;
            float step = Speed * multiplier * Time.deltaTime;

            if (distance <= step)
            {
                transform.localPosition = new Vector3(target.x, here.y, target.z);
                _at++;
                if (_at >= _path.Count) Arrive();
                return;
            }

            Vector3 direction = delta / distance;
            transform.localPosition = here + direction * step;

            // THE FACE TURNS THE WAY IT IS GOING, NOT INSTANTLY.
            //
            // Turning instantly makes the figure "jump" at corners; the
            // smoothing gives a turn that takes eight frames and reads at the
            // same rhythm as the walk clip.
            _faceYaw = Mathf.LerpAngle(
                _faceYaw, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg,
                Mathf.Clamp01(Time.deltaTime * 10f));
            transform.localRotation = Quaternion.Euler(0f, _faceYaw, 0f);

            // The Animator has to stay AWAKE for the whole walk. Figure
            // switches it off 0.9 s after the transition - on a long walk the
            // figure would freeze and slide along as a still pose.
            //
            // THE CLIP SPEED FOLLOWS THE GROUND SPEED.
            //
            // At first Anim.speed was not set anywhere: at x4 game speed a
            // figure covers 4.6 m a second but the legs played at 1x tempo -
            // the feet slid along the ground. The pedestrians on the street
            // have random speeds too (0.92-1.34) and did not match either.
            if (Body != null)
            {
                Body.HoldAwake();
                LastGroundSpeed = Speed * multiplier;
                Body.SetGroundSpeed(LastGroundSpeed);

                // IS THE CLIP REALLY ADVANCING?
                //
                // A walking figure's clip has to advance every frame. If it does
                // not, the figure is SLIDING: the legs frozen, the body moving.
                // This is what the user reported, and the cause was that the
                // clips had been imported without looping - the clip played once
                // and stopped on its last frame.
                //
                // The counter is STATIC and cumulative: the tour asks for the
                // total.
                float p = Body.ClipProgress;
                if (p >= 0f)
                {
                    if (p > _lastClipProgress + 0.0001f) AnimAdvanced++;
                    else AnimStalled++;
                    _lastClipProgress = p;
                }
            }
        }

        /// <summary>
        /// The number of frames, over walking figures, in which the clip
        /// advanced and in which it was FROZEN. So the tour can ask;
        /// cumulative and static.
        public static int AnimAdvanced, AnimStalled;

        private float _lastClipProgress = -1f;

        private void Arrive()
        {
            // The walk is over: the clip speed goes back to normal. A standing
            // figure's sitting or chopping clip must not be scaled by the
            // ground speed.
            if (Body != null) Body.ResetPlaybackSpeed();

            if (!float.IsNaN(_finalYaw))
            {
                _faceYaw = _finalYaw;
                transform.localRotation = Quaternion.Euler(0f, _finalYaw, 0f);
            }
            System.Action f = _onArrive;
            _onArrive = null;
            if (f != null) f();
        }
    }
}
