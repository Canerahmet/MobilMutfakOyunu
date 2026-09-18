using System.Collections.Generic;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// THE PEOPLE WHO PASS ALONG THE STREET.
    ///
    /// The user's sentence: "not all the characters passing along the
    /// street should go into the restaurant, some should carry on their
    /// way, or talk among themselves and then carry on".
    ///
    /// Why it matters: a street where everyone who passes comes IN is not
    /// a street, it is a queue. Someone passing by makes the restaurant
    /// ITSELF a choice - this place is inside a world, and not everybody
    /// comes here.
    ///
    /// IT NEVER TOUCHES THE SIMULATION. These are NOT guests: the core
    /// does not know about them, their number does not affect the economy,
    /// none of them sits down at a table. They are the view layer's
    /// decoration - and that is why their randomness has its own seed too,
    /// without touching the core's RNG.
    ///
    /// THEY DO NOT WALK THROUGH ONE ANOTHER. In the first version they
    /// did, and the user saw it. A two-layer answer:
    ///
    ///   1. LANES - if two figures walking in opposite directions are on
    ///      the same line, meeting is unavoidable. Two separate lanes
    ///      (Paths.PavementLane) remove that case STRUCTURALLY.
    ///   2. PUSHING - for those who catch each other up in the same lane
    ///      and for those standing still to chat, a small correction at
    ///      the end of every frame that separates the bodies
    ///      (LateUpdate). Not pathfinding: it only stops two bodies from
    ///      overlapping.
    ///
    /// The two have to be separate: with the pushing alone, a pair coming
    /// from opposite directions would brake against each other as they
    /// passed (the pavement jams); with the lanes alone, a faster figure
    /// going the same way would pass through a slower one.
    ///
    /// CHEAP: five figures at most; the pushing is O(n^2) but n = 5 + the
    /// few guests waiting outside.
    /// </summary>
    public sealed class StreetLife : MonoBehaviour
    {
        /// <summary>The most people on the street at one time.</summary>
        private const int MaxWalkers = 5;

        /// <summary>
        /// The highest time multiplier the street follows.
        ///
        /// When the player presses x16 the hall runs sixteen times faster but
        /// the STREET does not: the people passing by have no counterpart in
        /// the simulation, and "I fast-forwarded" must not mean "the pavement
        /// emptied".
        ///
        /// The real reason was a bug, and it was found by measurement: when
        /// the multiplier goes above TeleportAbove (4.5), Walker stops
        /// drawing the walk and teleports to the end of the path. On the
        /// street the end of the path is only TWO points (the exit of the one
        /// going right, the exit of the one going left), so at high speed
        /// five pedestrians piled up at those two points - three plus two,
        /// exactly four interpenetrating pairs. The tour's "the pedestrians
        /// do not walk through each other" check caught it on its first run.
        ///
        /// 4: just below TeleportAbove, so the teleport never comes into
        /// play at all.
        /// </summary>
        public const float MaxSpeed = 4f;

        /// <summary>The street's current time multiplier.</summary>
        private static float Clock
        {
            get { return Mathf.Min(MaxSpeed, Mathf.Max(1f, Walker.GameSpeed)); }
        }

        /// <summary>How long a chat lasts (s).</summary>
        private const float ChatMin = 2.2f;
        private const float ChatMax = 4.5f;

        /// <summary>Two people coming close enough to chat (m).</summary>
        private const float ChatRange = 1.25f;

        /// <summary>
        /// The CLOSEST distance of a chat (m).
        ///
        /// Without a lower limit, two figures standing nose to nose do not
        /// look as if they are talking but as if they have merged. It has to
        /// be LARGER than Personal: if it were smaller the pushing would try
        /// to drive the two talkers apart for the whole length of the chat.
        /// </summary>
        private const float ChatNear = 0.75f;

        /// <summary>
        /// The SMALLEST distance between the centres of two bodies (m).
        ///
        /// 0.68 WAS MEASURED: Editor/PlacementAudit prints the figure's
        /// horizontal profile against height, and apart from the arms the
        /// widest band is at HEAD height (y 0.61-0.72) - 0.67 m. In this
        /// pack's proportions the head is a third of the body, that is,
        /// wider than the shoulders (0.58); had the shoulder measurement
        /// been used, two heads would have overlapped by 7 cm.
        ///
        /// THE FIRST MEASUREMENT MEASURED THE WRONG THING, and it was only
        /// noticed when the number came out absurd: the bounding box gave
        /// 1.14 m - impossible as a footprint for a figure a metre tall. The
        /// box measures the ARM SPAN, and this pack's figures hold their
        /// arms away from the body. And the second measurement was wrong
        /// too: it looked at hip height (y 0.18-0.50) and found 1.08 m,
        /// because at these proportions the HANDS are at that height as
        /// well. The only way to find the right band was to print the whole
        /// profile.
        ///
        /// The lane spacing comes from the same number (Paths.LaneHalf * 2 =
        /// 0.60): two bodies coming from opposite directions pass without
        /// touching. The two share the same number but serve two DIFFERENT
        /// purposes - the lane prevents the meeting structurally, while this
        /// threshold separates those catching each other up in the same lane
        /// and those standing still.
        public const float Personal = 0.68f;

        /// <summary>
        /// The fastest correction speed of the pushing (m/s) - A CEILING,
        /// not a target.
        ///
        /// The correction itself is PROPORTIONAL: the further two bodies
        /// have merged the further they are separated, so a correction is
        /// near zero when they only graze past each other (no juddering) and
        /// opens up in a single frame if they really are on top of one
        /// another. This ceiling only limits an unexpected jump.
        ///
        /// It went from 1.6 to 4.0 and the reason was measured: the tour's
        /// second run came out with "1 pair at worst". Pushing at a fixed
        /// speed could not keep up with the pedestrian being pushed by a
        /// GUEST crossing the pavement - the guest is not pushed (its path
        /// is tied to the simulation), the pedestrian is pressed from both
        /// sides at once, and the end of the chain resolved a frame late.
        private const float PushSpeed = 4.0f;

        /// <summary>
        /// The MOST relaxation passes in one frame of the mutual
        /// separation.
        ///
        /// The loop exits early: if a pass has corrected no pair at all the
        /// state has converged. For five people and ten pairs the cost per
        /// pass is ten distance comparisons - keeping the ceiling high costs
        /// nothing, keeping it low costs an unresolved chain.
        private const int Passes = 6;

        /// <summary>
        /// The SMALLEST distance to a street lamp post (m).
        ///
        /// The post is thin (9 cm): half a body (0.34) + half the post
        /// (0.045) + a margin. It has to be SMALLER than Personal - the post
        /// is on the pavement's outer edge and the outer lane is 0.45 m away
        /// from it; were the threshold Personal, every pedestrian in the
        /// outer lane would be pushed inwards CONSTANTLY, that is, the lane
        /// would be useless.
        /// <summary>
        /// How far a pedestrian is pushed out of an obstacle's centre.
        ///
        /// 0.40 -> 0.48, AND IT IS A SUM, NOT A FEELING: the lamp post's
        /// widest band is its base plate at radius 0.185, and the figure's
        /// half-width at torso height is 0.29 (the PROFILE lines of
        /// Editor/PlacementAudit). 0.40 left them 8 cm short, which is why
        /// the tour reported "1 people in a lamp post" in some runs and not
        /// in others - the push was resolving the overlap only when the
        /// approach happened to be shallow.
        /// </summary>
        private const float PostClear = 0.48f;

        /// <summary>
        /// The smallest SIDEWAYS share of the pushing.
        ///
        /// Why: for someone catching another up from behind in the same
        /// lane, the line separating the two bodies is almost entirely along
        /// the walking axis. Pushing in that direction speeds the one in
        /// front up and slows the one behind down - the two never come
        /// alongside each other, that is, nobody can OVERTAKE. Tilting the
        /// push sideways lets the one behind slide across and pass; the lane
        /// limits then pull it back into place afterwards.
        private const float SideBias = 0.62f;

        private sealed class Pedestrian
        {
            public GameObject Body;
            public Walker Walk;
            public Figure Fig;
            public float ChatLeft;
            public int Dir;              // +1 to the right, -1 to the left
            public float NextChat;       // do not chat again before this time
            public Pedestrian Partner;   // the person it is chatting to
            public int Side;             // which way it moves when pushed (+1/-1)
        }

        private readonly List<Pedestrian> _people = new List<Pedestrian>();
        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly List<Vector3> _others = new List<Vector3>();
        private readonly List<Vector3> _posts = new List<Vector3>();
        private System.Random _rng;
        private RestaurantView _view;
        private int _postStamp = -1;

        /// <summary>How many people are on the street right now. So the tour can ask.</summary>
        public int Count { get { return _people.Count; } }

        /// <summary>The number of obstacles (posts) the pushing knows about.</summary>
        public int PostsKnown { get { return _posts.Count; } }

        /// <summary>How many people are chatting right now. So the tour can ask.</summary>
        public int Chatting
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _people.Count; i++)
                    if (_people[i].ChatLeft > 0f) n++;
                return n;
            }
        }

        /// <summary>
        /// How many PAIRS were merged into each other in the frame that was
        /// DRAWN.
        ///
        /// The MEASURABLE form of the "they walk through each other"
        /// complaint: if it is greater than zero, they did.
        ///
        /// IT IS NOT CALCULATED, IT IS STORED - and the reason was a wrong
        /// measurement. This property used to calculate when it was asked,
        /// and the tour went red at random with "1 pair at worst". The cause
        /// was not on the street but AT THE MOMENT OF MEASUREMENT: in Unity
        /// `yield return null` wakes a coroutine BETWEEN Update and
        /// LateUpdate, so the tour was reading the positions AFTER Walker
        /// had moved them but BEFORE the pushing had corrected them. The
        /// frame it measured was never drawn at all.
        ///
        /// The number is now written at the end of LateUpdate, once the
        /// correction is over: that is, the number of the frame the player
        /// sees.
        public int Overlaps { get { return _overlaps; } }

        private int _overlaps;
        private int _postOverlaps;

        /// <summary>Counts the merged bodies in the frame being drawn.</summary>
        private void Measure()
        {
            // A margin BELOW THE THRESHOLD: the pushing stops the figures
            // exactly at the threshold and floating-point noise would make
            // the check unstable.
            float threshold = Personal - 0.06f;
            int n = 0;
            for (int i = 0; i < _people.Count; i++)
            {
                if (_people[i].Body == null) continue;
                Vector3 a = _people[i].Body.transform.localPosition;
                for (int j = i + 1; j < _people.Count; j++)
                {
                    if (_people[j].Body == null) continue;
                    Vector3 d = a - _people[j].Body.transform.localPosition;
                    d.y = 0f;
                    if (d.sqrMagnitude < threshold * threshold) n++;
                }
            }
            _overlaps = n;

            // IF THERE IS NO POST IT IS NOT ZERO BUT ONE.
            //
            // It used to return "0" and the check stayed GREEN even when the
            // posts had never been loaded - so it was measuring not that the
            // pushing worked but that there was nothing to measure.
            if (_posts.Count == 0) { _postOverlaps = 1; return; }

            float postThreshold = PostClear - 0.06f;
            n = 0;
            for (int i = 0; i < _people.Count; i++)
            {
                if (_people[i].Body == null) continue;
                Vector3 a = _people[i].Body.transform.localPosition;
                for (int k = 0; k < _posts.Count; k++)
                {
                    Vector3 d = a - _posts[k];
                    d.y = 0f;
                    if (d.sqrMagnitude < postThreshold * postThreshold) { n++; break; }
                }
            }
            _postOverlaps = n;
        }

        /// <summary>
        /// How many pedestrians were inside a street lamp post in the frame
        /// that was DRAWN.
        ///
        /// Why it is measured separately: pedestrians avoiding EACH OTHER
        /// and not passing through a post are two different mechanisms (one
        /// mutual pushing, the other one-sided) and a single number cannot
        /// measure both.
        public int PostOverlaps { get { return _postOverlaps; } }

        // =====================================================================
        /// <summary>
        /// Fills the street. prefabs: the guest figures (the same pack).
        /// <param name="dress">
        /// Called on each new figure, right after it is created. The street
        /// wears the same crowd colormap as the hall: the pavement is two
        /// metres from the window and in frame the whole time, so a street
        /// full of the pack's own colours next to a recoloured hall would
        /// read as the recolouring being broken.
        /// </param>
        public void Build(Transform root, GameObject[] prefabs, int seed,
                          System.Action<GameObject> dress = null)
        {
            _rng = new System.Random(seed);

            Clear();
            if (prefabs == null || prefabs.Length == 0) return;

            for (int i = 0; i < MaxWalkers; i++)
            {
                GameObject go = Instantiate(prefabs[i % prefabs.Length], root);
                go.name = "Passerby";
                if (dress != null) dress(go);
                Pedestrian p = new Pedestrian
                {
                    Body = go,
                    Walk = go.GetComponent<Walker>() ?? go.AddComponent<Walker>(),
                    Fig = go.GetComponentInChildren<Figure>(true),
                    Dir = (i % 2 == 0) ? 1 : -1,
                    Side = (i % 2 == 0) ? 1 : -1,
                };
                // Walker's body has to be bound: GoTo/Update drives the figure's
                // pose through it. If it is not bound the walking pose is
                // silently disabled.
                p.Walk.Body = p.Fig;

                // THE SPEEDS DIFFER.
                //
                // When everyone walks at the same speed the pavement flows like a
                // band: nobody catches anybody up, nobody overtakes anybody. The
                // difference is what makes the street ALIVE rather than crowded.
                p.Walk.Speed = 0.92f + (float)_rng.NextDouble() * 0.42f;
                p.Walk.SpeedCap = MaxSpeed;

                // They are spread along the pavement at the start: if they all
                // came in from the same edge the street would look like a crowd
                // emptying out of a door.
                float x = Lerp(i / (float)Mathf.Max(1, MaxWalkers - 1));
                // Appear, not Warp: this is a figure being PUT on the
                // pavement as the street is built, not one being moved off a
                // spot it was standing on. The distance from the origin is up
                // to 18.8 m and the tour reported exactly that the first time
                // warps were measured.
                p.Walk.Appear(new Vector3(x, 0f, Paths.PavementLane(p.Dir)),
                            p.Dir > 0 ? 90f : -90f);
                _people.Add(p);
                Send(p);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _people.Count; i++)
                if (_people[i].Body != null)
                {
                    if (Application.isPlaying) Destroy(_people[i].Body);
                    else DestroyImmediate(_people[i].Body);
                }
            _people.Clear();
        }

        // =====================================================================
        private void Update()
        {
            if (_people.Count == 0) return;

            // WHEN THE GAME IS PAUSED THE STREET STOPS TOO.
            //
            // The Mathf.Max(0.25f, ...) floor below was swallowing the zero:
            // in a paused world the pedestrians played the walk animation
            // while marking time.
            if (Walker.GameSpeed <= 0.001f) return;

            // The chat counters run on the CAPPED clock too: with the walking
            // capped but the chat uncapped, two people at x16 would stop for
            // the blink of an eye and carry on.
            float dt = Time.deltaTime * Clock;

            for (int i = 0; i < _people.Count; i++)
            {
                Pedestrian p = _people[i];
                if (p.Body == null) continue;

                if (p.NextChat > 0f) p.NextChat -= dt;

                if (p.ChatLeft > 0f)
                {
                    p.ChatLeft -= dt;
                    // THE FACE IS TURNED AGAIN EVERY FRAME: the pushing separates
                    // the two talkers a little and the direction written once at
                    // the start of the chat drifted - two figures looking past
                    // each other rather than at each other do not read as
                    // "talking".
                    Face(p);
                    if (p.ChatLeft <= 0f) { p.Partner = null; Send(p); }
                    continue;
                }

                if (p.Walk.Moving)
                {
                    if (p.Fig != null) p.Fig.HoldAwake();
                    continue;
                }

                // It has reached the edge: carry on from the opposite edge. As
                // its direction changes so does its lane - because the turn
                // happens outside the plot, the change of lane is not visible
                // on screen.
                p.Dir = -p.Dir;
                Send(p);
            }

            Gossip();
        }

        // =====================================================================
        /// <summary>
        /// THE CORRECTION THAT SEPARATES THE BODIES.
        ///
        /// It has to run AFTER Walker: Walker writes the position directly
        /// every frame, so a correction made inside Update would be erased
        /// on the next frame. A position written in LateUpdate is the
        /// position that frame is drawn at.
        ///
        /// ONLY THE PEDESTRIANS ARE PUSHED, NOT THE GUESTS: a guest's path
        /// is tied to an event in the simulation (sitting down at a table,
        /// leaving) and pushing it could break the arrival measurement. And
        /// it is the right way round anyway - someone walking into a
        /// restaurant keeps their line, someone passing by steps aside.
        /// </summary>
        private void LateUpdate()
        {
            if (_people.Count == 0) return;
            if (Walker.GameSpeed <= 0.001f) return;

            // In a SPED-UP game the correction speeds up too: otherwise at x4
            // the figures merge faster than the pushing can keep up with. It
            // uses the SAME ceiling as the walking - if the pushing were
            // slower than the walking it could never separate anything.
            float dt = Time.deltaTime * Clock;
            float step = PushSpeed * dt;

            // The guests standing outside count too: a pedestrian walking
            // through a guest waiting in front of the door is exactly the
            // same bug.
            _others.Clear();
            if (_view == null) _view = GetComponent<RestaurantView>();
            if (_view != null) _view.OutsideFigures(_others);

            // The posts are read once AT BUILD TIME: they stand still, and
            // building a list of eight objects every frame is not free. They
            // are read again when the build stamp changes.
            if (_view != null && _postStamp != _view.BuildStamp)
            {
                _postStamp = _view.BuildStamp;
                _posts.Clear();
                _view.StreetObstacles(_posts);
            }

            // THE ORDER MATTERS, AND IT WAS LEARNED THROUGH A BUG.
            //
            // In the first version, for each person the mutual separation was
            // done first and the obstacle push second. So a person's last
            // operation was "move away from the guest/the post", and that push
            // could drive them back into the neighbour they had ALREADY been
            // separated from - and nobody looked again. The tour showed it
            // with an unstable red: "1 pair at worst" on one run in three.
            //
            // The right order: the one-sided constraints first (obstacles, the
            // lane), the mutual separation LAST. That way what has the last
            // word in the frame being drawn is the thing the check measures.

            // --- 1. fixed obstacles: the guests and the posts ----------
            for (int i = 0; i < _people.Count; i++)
            {
                Pedestrian a = _people[i];
                if (a.Body == null) continue;

                for (int k = 0; k < _others.Count; k++)
                    Push(a, _others[k], step, Personal);
                for (int k = 0; k < _posts.Count; k++)
                    Push(a, _posts[k], step, PostClear);
            }

            // --- 2. the lane band --------------------------------------
            for (int i = 0; i < _people.Count; i++)
            {
                Pedestrian a = _people[i];
                if (a.Body == null) continue;

                // IT STAYS INSIDE ITS LANE. The pushing could throw a figure
                // off the pavement: into the road or inside the building. The
                // lane band pulls it back - and only if it has SPILLED out of
                // the band, otherwise it would fight the pushing and judder.
                Vector3 spot = a.Body.transform.localPosition;
                float lane = Paths.PavementLane(a.Dir);
                if (Mathf.Abs(spot.z - lane) > Paths.LaneHalf + 0.30f)
                {
                    spot.z = Mathf.MoveTowards(spot.z, lane, step);
                    a.Body.transform.localPosition = spot;
                }
            }

            // --- 3. mutual separation, until it converges --------------
            //
            // The chain: the guest pushes A, A goes into B, B into C. A
            // single pass moves the chain along by one link. The loop exits
            // EARLY when a pass makes no correction at all, so on a quiet
            // pavement it costs one pass.
            for (int pass = 0; pass < Passes; pass++)
            {
                bool changed = false;
                for (int i = 0; i < _people.Count; i++)
                {
                    Pedestrian a = _people[i];
                    if (a.Body == null) continue;

                    for (int j = i + 1; j < _people.Count; j++)
                    {
                        Pedestrian b = _people[j];
                        if (b.Body == null) continue;
                        if (Nudge(a, b, step)) changed = true;
                    }
                }
                if (!changed) break;
            }

            // THE MEASUREMENT COMES LAST: this frame is about to be drawn
            // and the number is that frame's number.
            Measure();
        }

        /// <summary>
        /// Separates two pedestrians, both of them moving. The correction
        /// is half the MISSING DISTANCE, capped at e. True if it made a
        /// correction.
        /// </summary>
        private static bool Nudge(Pedestrian a, Pedestrian b, float e)
        {
            Vector3 pa = a.Body.transform.localPosition;
            Vector3 pb = b.Body.transform.localPosition;
            Vector3 d = pa - pb;
            d.y = 0f;
            float u = d.magnitude;
            if (u >= Personal) return false;

            float push = Mathf.Min(e, (Personal - u) * 0.5f);
            Vector3 direction = Direction(d, u, a.Side);
            a.Body.transform.localPosition = pa + direction * push;
            b.Body.transform.localPosition = pb - direction * push;
            return true;
        }

        /// <summary>
        /// Moves a pedestrian away from a fixed point. Because the other
        /// side does not move, the correction is the WHOLE of the missing
        /// distance.
        /// </summary>
        private static void Push(Pedestrian a, Vector3 other, float e, float clear)
        {
            Vector3 pa = a.Body.transform.localPosition;
            Vector3 d = pa - other;
            d.y = 0f;
            float u = d.magnitude;
            if (u >= clear) return;

            float push = Mathf.Min(e, clear - u);
            a.Body.transform.localPosition = pa + Direction(d, u, a.Side) * push;
        }

        /// <summary>
        /// The direction of the separation - at least SideBias of it
        /// SIDEWAYS.
        ///
        /// If u is very close to zero (exactly on top of each other) the
        /// direction is undefined; the figure is then moved to its own side.
        /// Its side is FIXED, so the same figure is always pulled the same
        /// way - an unstable choice of side would produce juddering.
        /// </summary>
        private static Vector3 Direction(Vector3 d, float u, int side)
        {
            if (u < 0.001f) return new Vector3(0f, 0f, side);

            Vector3 n = d / u;
            // The walking axis is x, the sideways axis z. The sideways
            // component is pulled up to the floor, keeping its sign.
            float z = n.z;
            if (Mathf.Abs(z) < SideBias) z = SideBias * (z < 0f ? -1f : 1f);

            Vector3 r = new Vector3(n.x, 0f, z);
            float m = r.magnitude;
            return m < 0.001f ? new Vector3(0f, 0f, side) : r / m;
        }

        // =====================================================================
        /// Makes a pair who have come alongside each other talk.
        ///
        /// The conditions: both have to be walking, close enough but NOT
        /// TOO close, and neither of them may have talked recently. Without
        /// the last condition the same two people get stuck on the pavement
        /// - it reads as "a blockage" rather than "a chat".
        /// </summary>
        private void Gossip()
        {
            for (int i = 0; i < _people.Count; i++)
            {
                Pedestrian a = _people[i];
                // THE BODY CHECK IS ESSENTIAL HERE TOO.
                //
                // The Update loop guarded itself but this one did not: going
                // back to the main menu makes the view destroy all its
                // children, five dead records are left in _people and
                // a.Body.transform threw an exception every frame. Producing a
                // stack trace is expensive on Android.
                if (a.Body == null || a.Walk == null) continue;
                if (a.ChatLeft > 0f || a.NextChat > 0f || !a.Walk.Moving) continue;

                for (int j = i + 1; j < _people.Count; j++)
                {
                    Pedestrian b = _people[j];
                    if (b.Body == null || b.Walk == null) continue;
                    if (b.ChatLeft > 0f || b.NextChat > 0f || !b.Walk.Moving) continue;

                    Vector3 d = a.Body.transform.localPosition
                                - b.Body.transform.localPosition;
                    d.y = 0f;
                    float u2 = d.sqrMagnitude;
                    if (u2 > ChatRange * ChatRange) continue;
                    if (u2 < ChatNear * ChatNear) continue;
                    if (_rng.NextDouble() > 0.35) continue;

                    float seconds = ChatMin + (float)_rng.NextDouble() * (ChatMax - ChatMin);
                    Chat(a, b, seconds);
                    Chat(b, a, seconds);
                    break;
                }
            }
        }

        private void Chat(Pedestrian who, Pedestrian other, float seconds)
        {
            if (who.Body == null || who.Walk == null) return;

            who.ChatLeft = seconds;
            who.NextChat = seconds + 8f;
            who.Partner = other;
            who.Walk.Stop();
            if (who.Fig != null) who.Fig.Set(Figure.Pose.Idle);
            Face(who);
        }

        /// Turns a talker towards the person opposite.
        ///
        /// Two figures looking the same way do not read as "talking" but as
        /// "queueing".
        private static void Face(Pedestrian who)
        {
            if (who.Body == null) return;
            if (who.Partner == null || who.Partner.Body == null) return;

            float yaw = Paths.FaceFrom(who.Body.transform.localPosition,
                                       who.Partner.Body.transform.localPosition);
            who.Body.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>Sends them along the pavement towards the opposite edge.</summary>
        private void Send(Pedestrian p)
        {
            if (p.Body == null || p.Walk == null) return;

            float target = p.Dir > 0 ? RoomPlan.PlotW + 1.1f : -1.1f;
            _path.Clear();
            // The target is in its OWN LANE: a figure left outside its lane
            // after a chat or a push comes back to the lane by itself as it
            // walks - with no jump.
            _path.Add(new Vector3(target, 0f, Paths.PavementLane(p.Dir)));
            p.Walk.GoTo(_path, float.NaN, null);
            if (p.Fig != null) p.Fig.Set(Figure.Pose.Walk);
        }

        private static float Lerp(float t)
        {
            return Mathf.Lerp(-0.8f, RoomPlan.PlotW + 0.8f, t);
        }
    }
}
