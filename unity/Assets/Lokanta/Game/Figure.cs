using System.Collections.Generic;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// The POSE of a human figure. A guest sits, a cook works at the
    /// counter, a waiter carries a plate.
    ///
    /// Why a separate component: the figures come out of a pool, and the
    /// same object is a guest first and idle later in the same day.
    /// Keeping the pose on the object itself means "what is this figure
    /// doing right now" is answered in one place.
    ///
    /// The clips come ready in the asset pack (Kenney Mini Characters,
    /// CC0); because the skeleton is the same on every figure, a single
    /// controller drives them all.
    /// </summary>
    public sealed class Figure : MonoBehaviour
    {
        public enum Pose { Idle, Walk, Sit, Serve, Carry, Chop, Wash, Pick }

        /// <summary>The state names in the controller. Same order as Pose.</summary>
        public static readonly string[] StateNames =
            { "idle", "walk", "sit", "serve", "carry", "chop", "wash", "pick" };

        /// <summary>
        /// The clip names in the pack. Same order as Pose.
        ///
        /// THE KITCHEN JOBS are built out of the pack's ready-made clips; no
        /// new animation is produced:
        ///   chop  = attack-melee-right - an arm coming down from above.
        ///           Which is the chopping motion itself.
        ///   wash  = interact-left      - working at something in front of
        ///           you with both hands.
        ///   pick  = pick-up            - bending down and picking something
        ///           up; the cook does this in front of the fridge.
        public static readonly string[] ClipNames =
            { "idle", "walk", "sit", "interact-right", "holding-both",
              "attack-melee-right", "interact-left", "pick-up" };

        public Animator Anim;

        /// <summary>
        /// The clips, for the EDITOR PREVIEW. In the game the controller
        /// drives them; in editor mode the Animator does not run, so the clip
        /// is sampled by hand.
        public AnimationClip[] Clips;

        private Pose _pose = Pose.Idle;
        private bool _started;

        // =====================================================================
        // THE KNEE BENDS WHEN SITTING.
        //
        // There was no knee in the pack's skeleton; there was one bone per
        // leg, and a seated figure either went through the cushion or stuck
        // its legs out in front as if it were sitting on the floor. The knee
        // bone is now added AT BUILD TIME (Editor/ArtPrefabs.AddKnees) and no
        // clip animates it - so every old pose stands exactly as it was, and
        // only this place interferes.
        //
        // The rule is one sentence: THE SHIN IS VERTICAL. The thigh stays
        // where the clip puts it (the sitting clip stretches it forward),
        // while the knee turns the shin downwards in world space on every
        // frame. The feet then hang in front of the cushion, towards the
        // floor.
        //
        // Why in world space: there is no need to know where the bone's own
        // axes point. Whatever angle the knee bone sits at relative to its
        // root in the bind pose (legs down), that same angle is written when
        // sitting.
        //
        // Why LateUpdate: the Animator writes the pose AFTER Update.
        /// <summary>The thigh bones. Bound when the prefab is built.</summary>
        public Transform[] Legs;

        /// <summary>The shin bones. Bound when the prefab is built.</summary>
        public Transform[] Knees;

        /// <summary>
        /// The BIND angles of the leg bones, relative to the figure's root.
        ///
        /// WHY THEY ARE NOT READ AT RUNTIME: the first version read them at
        /// first use, and what it read was NOT the bind angle - by then the
        /// clip had long since changed the pose. The result: the shin goes
        /// where "down" is written, but that place is now the thigh's
        /// direction. When the prefab is built the model is definitely in the
        /// bind pose; that is the right moment.
        /// </summary>
        public Quaternion[] LegRest;

        /// <summary>The shins' bind angles. See LegRest.</summary>
        public Quaternion[] KneeRest;

        // =====================================================================
        // THE KNEE BENDS WHEN SITTING.
        //
        // There was NO knee in the pack's skeleton; there was one bone per
        // leg, and a seated figure either went through the cushion or stuck
        // its legs out in front as if it were sitting on the floor. The knee
        // bone is now added at build time (Editor/ArtPrefabs.AddKnees) and no
        // clip animates it - so every old pose stands exactly as it was, and
        // only this place interferes.
        //
        // The rule is two sentences: THE THIGH FORWARD, THE SHIN DOWN. Both
        // in the figure's own space, measured from the bind angle.
        //
        // Why LateUpdate: the Animator writes the pose AFTER Update.

        /// <summary>
        /// The thigh's rotation from its bind direction (down), in degrees.
        /// -90 is fully horizontal; -78 tips the end slightly down, like a
        /// person who is sitting.
        ///
        /// A STATIC FIELD, NOT A CONSTANT: so that the measuring tool can
        /// sweep these two angles and put them side by side in one frame.
        /// The game does not change them.
        /// </summary>
        public static float ThighAngle = -78f;

        /// <summary>The shin's forward tilt from vertical, in degrees. Negative = forwards.</summary>
        public static float ShinTilt = -6f;

        private void LateUpdate() { BendKnees(); }

        /// <summary>
        /// Turns the thigh forward and the shin down while sitting. The
        /// editor preview calls it too - so that there are not two separate
        /// calculations in two places.
        /// </summary>
        public void BendKnees()
        {
            if (_pose != Pose.Sit || Knees == null || Knees.Length == 0) return;
            if (LegRest == null || KneeRest == null) return;

            // A GUARD FOR LEGS TOO.
            //
            // There was one for three fields but not for Legs; the loop below
            // reads `Legs.Length`. If the prefab builder binds Knees+KneeRest
            // but not Legs (they are separate steps in AddKnees), the moment
            // the hall fills up there are DOZENS of NullReferenceExceptions
            // per frame - LateUpdate runs on every seated figure on every
            // frame.
            if (Legs == null) return;

            Quaternion root = transform.rotation;

            Vector3 thighDir = root * (Quaternion.Euler(ThighAngle, 0f, 0f) * Vector3.down);
            for (int i = 0; i < Legs.Length && i < LegRest.Length; i++)
                Aim(Legs[i], LegRest[i], thighDir);

            // THE SHIN: down in world space, wherever the thigh goes.
            Vector3 shinDir = root * (Quaternion.Euler(ShinTilt, 0f, 0f) * Vector3.down);
            for (int i = 0; i < Knees.Length && i < KneeRest.Length; i++)
                Aim(Knees[i], KneeRest[i], shinDir);
        }

        /// <summary>
        /// Turns the bone so that the axis which pointed down in its BIND
        /// pose points along the given world direction.
        ///
        /// WHY AIMING AND NOT AN ANGLE: setting it up with an angle ("turn
        /// this much in the root's space") requires knowing where the bone's
        /// own axes point, and in this pack the thigh's and the shin's are
        /// NOT THE SAME - the thigh turned as expected while the shin went
        /// somewhere else entirely. Aiming never asks for that: the bone's
        /// "down" axis in the bind pose is found, and turned to wherever it
        /// is wanted.
        /// </summary>
        private static void Aim(Transform bone, Quaternion rest, Vector3 worldDir)
        {
            if (bone == null || worldDir.sqrMagnitude < 0.0001f) return;

            // In the bind pose the bone pointed down; this is what that
            // direction is in the bone's OWN space.
            Vector3 local = Quaternion.Inverse(rest) * Vector3.down;
            Vector3 now = bone.rotation * local;
            bone.rotation = Quaternion.FromToRotation(now, worldDir) * bone.rotation;
        }

        /// <summary>For the measuring tool: was a knee bone found?</summary>
        public int KneeCount { get { return Knees == null ? 0 : Knees.Length; } }

        /// <summary>
        /// How long is left of the transition. Once it drops below zero the
        /// Animator IS SWITCHED OFF.
        ///
        /// A seated guest played the same clip for ever, and a full hall had
        /// 67 Animators for its 67 figures. The fixed cost per Animator (the
        /// state machine evaluation + job scheduling) is ~45 microseconds
        /// even on a small skeleton, that is ~3 ms a frame - and it buys
        /// NOTHING, because a seated guest does not move.
        ///
        /// When the transition is over the pose is already sitting where its
        /// last frame left it; switching the Animator off does not change
        /// the look. When the pose changes, Set() turns it back on.
        /// </summary>
        private float _settleLeft;

        private const float CrossFade = 0.18f;

        /// <summary>
        /// The walk clip's OWN speed (m/s) - that is, the ground speed at
        /// which the feet do not slide.
        ///
        /// The clip walks on the spot (no root motion), so this number is
        /// written down nowhere and it was MEASURED: the "WALK" line of
        /// Editor/PlacementAudit samples the clip at twenty-four points and
        /// finds the widest opening between the two feet (the stride); one
        /// cycle is two strides, so the natural speed = 2 x stride / clip
        /// length.
        ///
        /// Why it is needed: Anim.speed was not being set anywhere. At x4
        /// game speed the figure moves four times as fast but the legs
        /// played at the same tempo - the feet slid along the ground.
        ///
        /// Measured: a 0.67 s clip, a 0.426 m stride -> 2 x 0.426 / 0.67 =
        /// 1.28 m/s. Walker.Speed, though, is 1.15 - so there was an 11%
        /// mismatch even at x1, and 3.6 times that at x4.
        ///
        /// If this number changes, the audit's walk line says so.
        /// </summary>
        public const float WalkClipSpeed = 1.28f;

        /// <summary>
        /// Ties the clip's playback speed to the GROUND SPEED.
        ///
        /// For a walking figure: the legs turn according to the distance the
        /// body really covers. For a standing figure it is 1 - the speed of
        /// the sitting, chopping or washing clips has nothing to do with the
        /// ground speed.
        ///
        /// CAPPED: too high a multiplier turns the legs into a judder, and
        /// Walker teleports above x4.5 anyway.
        /// </summary>
        public void SetGroundSpeed(float metersPerSecond)
        {
            if (Anim == null || Anim.runtimeAnimatorController == null) return;

            float k = metersPerSecond > 0.01f
                ? metersPerSecond / WalkClipSpeed
                : 1f;
            if (k < 0.35f) k = 0.35f;
            if (k > 5f) k = 5f;
            if (!Mathf.Approximately(Anim.speed, k)) Anim.speed = k;
        }

        /// <summary>Puts the playback speed back to normal.</summary>
        public void ResetPlaybackSpeed()
        {
            if (Anim == null || Anim.runtimeAnimatorController == null) return;
            if (!Mathf.Approximately(Anim.speed, 1f)) Anim.speed = 1f;
        }

        public Pose Current { get { return _pose; } }

        /// <summary>
        /// The current clip's progress (the loop count included). So the
        /// tour can ask.
        ///
        /// Why it is needed: the "foot sliding" measure compared the clip's
        /// SPEED with the ground speed and could not see a FROZEN clip - the
        /// ratio comes out right even if the clip never advances at all.
        /// What the user saw ("they are not stepping, they are sliding") was
        /// exactly that, and the cause was that the clips had been imported
        /// without looping.
        ///
        /// Looking at whether this number IS ADVANCING is the only measure
        /// that says the animation is really playing.
        /// </summary>
        public float ClipProgress
        {
            get
            {
                if (Anim == null || !Anim.enabled
                    || Anim.runtimeAnimatorController == null) return -1f;
                return Anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
            }
        }

        public void Set(Pose p)
        {
            if (_started && p == _pose) return;
            _started = true;
            _pose = p;

            if (Anim == null || Anim.runtimeAnimatorController == null) return;

            // A transition time of 0.18 s: visible between sitting down and
            // standing up, but not long enough to feel like an unexpected
            // pause.
            if (!Anim.enabled) Anim.enabled = true;
            Anim.CrossFadeInFixedTime(StateNames[(int)p], CrossFade, 0);

            // The transition + one clip's grace: so that the first cycle of a
            // looping clip completes, otherwise it freezes in the middle of a
            // walk.
            _settleLeft = CrossFade + 0.9f;
        }

        /// <summary>
        /// Switches the Animator off when the transition is over.
        ///
        /// Update only does work WHILE the transition lasts; once it is off,
        /// all it does is compare a float, and that is immeasurably small
        /// next to the Animator's own cost.
        /// </summary>
        private void Update()
        {
            if (_settleLeft <= 0f) return;

            _settleLeft -= Time.deltaTime;
            if (_settleLeft > 0f) return;

            if (Anim != null && Anim.enabled) Anim.enabled = false;
        }

        /// <summary>
        /// KEEPS the Animator awake.
        ///
        /// Set() switches the Animator off 0.9 seconds after the transition,
        /// and that is right: a seated guest does not move, evaluating it is
        /// wasted. But it is wrong for a WALKING figure - a walk can last
        /// nine seconds and the figure would freeze in the middle of the
        /// path.
        ///
        /// Walker calls this every frame; when the walk ends it stops calling
        /// and the normal switch-off runs. So "stay awake" is not a flag but
        /// a HEARTBEAT - the kind that ends by itself if it is forgotten.
        /// </summary>
        public void HoldAwake()
        {
            if (Anim == null) return;
            if (!Anim.enabled) Anim.enabled = true;
            if (_settleLeft < 0.25f) _settleLeft = 0.25f;
        }

        /// <summary>
        /// A figure going back to the pool. At its next use Set() has to
        /// pose it again, otherwise it stays in the old pose.
        /// </summary>
        public void Release()
        {
            _started = false;
            _settleLeft = 0f;
        }

        /// <summary>
        /// Applies the pose in a single frame. For the editor preview: in
        /// editor mode the Animator does not run and every figure stayed in
        /// the bind pose - that is, with its arms out to the sides.
        /// </summary>
        public void Sample(Pose p, float time)
        {
            _pose = p;
            _started = true;
            int i = (int)p;
            if (Clips == null || i >= Clips.Length || Clips[i] == null) return;

            Clips[i].SampleAnimation(gameObject, time);
            BendKnees();
        }
    }
}
