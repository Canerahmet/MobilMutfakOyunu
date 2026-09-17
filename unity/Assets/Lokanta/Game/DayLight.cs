using Lokanta.Core.Sim;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// THE TIME OF DAY, ON SCREEN.
    ///
    /// The user's sentence: "inside the game you cannot tell morning,
    /// midday and evening apart". It was true - the phase of the day could
    /// only be read from a line of text in the interface, and the hall was
    /// equally bright at every hour.
    ///
    /// Four channels change at once, because a single channel (say, the
    /// light intensity alone) does not read as "evening has come" but as
    /// "someone turned the lamp down":
    ///
    ///   1. THE SUN'S ANGLE - low from the east in the morning, overhead
    ///      at midday, low from the west in the evening. The direction
    ///      and the length of the shadows are the day's strongest clock.
    ///   2. THE LIGHT'S COLOUR - cold in the morning, neutral at midday,
    ///      warm in the afternoon, purple-blue in the evening.
    ///   3. THE BACKGROUND - a flat colour instead of a sky (the docs/19
    ///      budget); pale grey-blue by day, nearly black at night.
    ///   4. THE LAMPS - the street lamps, the restaurant's own ceiling
    ///      lights and the warm fill inside come on in the evening.
    ///
    /// THE INSIDE LIGHTS COME ON BEFORE THE ONES OUTSIDE (RoomThreshold
    /// 0.62 - LampThreshold 0.76). With both on the same switch the hall
    /// stayed dark in the early evening; the user's complaint that "when
    /// evening comes the inside of the restaurant goes dark" described
    /// exactly that interval. A restaurant turns its own lights on before
    /// the street lamps anyway.
    ///
    /// THE VALUES ARE INTERPOLATED: there is no cut during the day,
    /// otherwise it jumps a frame saying "it is 2 o'clock". The service
    /// progress is turned into a 0-1 ratio and every channel is read from
    /// that ratio.
    /// </summary>
    public sealed class DayLight : MonoBehaviour
    {
        public Light Sun;
        public Light Fill;
        public Light Warm;      // the warm evening fill inside
        public Camera Cam;

        /// <summary>The glowing parts of the street lamps.</summary>
        public Renderer[] LampHeads;

        /// <summary>
        /// THE OUTSIDE OF THE CUISINE. The faint tint mixed into the
        /// background colour.
        ///
        /// The background stands in for the sky (below) and it was EXACTLY
        /// THE SAME in both cuisines - so the feeling of "I have walked into
        /// somewhere else" ended at the hall's four walls. The outside has to
        /// belong to the cuisine too: fast food colder and more urban, the
        /// Turkish restaurant warmer.
        ///
        /// The tint is mixed into the three DAYTIME stops only; the night is
        /// left alone. The night being almost black is what carries the
        /// contrast of the "a restaurant that is open" picture - lighting it
        /// up for the sake of a tint would spoil the whole night.
        ///
        /// The default white = no mixing, so if nobody sets it the behaviour
        /// is exactly what it was.
        /// </summary>
        public Color SkyTint = Color.white;

        /// <summary>The strength of the tint, 0-1. Chosen by measurement: 0.22.</summary>
        public float SkyTintStrength = 0.22f;

        /// <summary>The lamps' pools of light on the ground.</summary>
        public GameObject[] LampGlow;

        /// <summary>
        /// The ceiling lights INSIDE THE RESTAURANT.
        ///
        /// They are kept apart from the street lamps, because the two come on
        /// at different times: a restaurant turns its own lights on BEFORE
        /// the street lamps do (not when it has gone dark, but when it starts
        /// to). Had they been wired to the same switch the hall would stay
        /// dark in the early evening - which is exactly what the user
        /// reported.
        /// </summary>
        public GameObject[] RoomGlow;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _block;
        private bool _lampsOn;
        private bool _roomOn;
        private float _applied = -1f;

        /// <summary>The day's ratio, 0 morning - 1 night. So the tour can ask.</summary>
        public float DayProgress { get; private set; }

        /// <summary>Are the lamps lit? So the tour can ask.</summary>
        public bool LampsOn { get { return _lampsOn; } }

        /// <summary>The ratio at which the street lamps start to light.</summary>
        public const float LampThreshold = 0.76f;

        /// <summary>
        /// The ratio at which the restaurant's inside lights start to light.
        ///
        /// BEFORE the street lamps: after the middle of the day the light in
        /// the hall comes from its own lamps rather than from the sun. 0.62
        /// is about two thirds of service - late afternoon.
        /// </summary>
        public const float RoomThreshold = 0.62f;

        /// <summary>Are the inside lights on? So the tour can ask.</summary>
        public bool RoomLightsOn { get { return _roomOn; } }

        /// <summary>The number of inside ceiling lights bound. So the tour can ask.</summary>
        public int RoomLightCount { get { return RoomGlow == null ? 0 : RoomGlow.Length; } }

        // =====================================================================
        /// <summary>
        /// Turns the phase and the service progress into the day's ratio.
        ///
        /// The morning sits between 0.00 and 0.12 (a short morning before
        /// service opens), service covers 0.12-0.88, and the evening
        /// 0.88-1.00. That makes service itself the body of the day, and the
        /// player gets the feeling that "the day has begun" the moment they
        /// open service.
        /// </summary>
        public void Apply(DayPhase phase, float serviceProgress01)
        {
            BindLamps();

            float t;
            switch (phase)
            {
                case DayPhase.Morning: t = 0.10f; break;
                case DayPhase.Service:
                    t = 0.12f + Mathf.Clamp01(serviceProgress01) * 0.76f;
                    break;
                case DayPhase.Evening: t = 0.94f; break;
                default: t = 0.10f; break;
            }
            DayProgress = t;

            // The same frame is never written twice: light settings are not
            // cheap and the day's ratio changes by a thousandth per frame.
            if (Mathf.Abs(t - _applied) < 0.002f) return;
            _applied = t;

            if (Sun != null)
            {
                // THE PITCH STAYS IN A BAND: 44-66 degrees.
                //
                // What the user reported: "the shadows in the rooms slide into
                // other rooms". This was the second cause - the pitch was 26
                // degrees in the morning and 10 in the evening, and shadow
                // length is h/tan(angle): a 1.8 m fridge at 10 degrees in the
                // evening throws a 10 metre shadow, that is, a dark band
                // crossing three rooms at once.
                //
                // Inside the band the longest shadow is ~1.9 m; the narrowest
                // room is 3.2 m, so a shadow stays in its own room.
                //
                // THE TIME OF DAY IS NOT LOST: the direction (the azimuth) goes
                // on turning from 148 to 268 and that is what really tells the
                // time - the shadows stretch one way in the morning and the
                // other way in the evening. It is the DIRECTION that is read,
                // not the length.
                Sun.transform.rotation = Quaternion.Euler(
                    Curve(t, 44f, 66f, 52f, 46f),
                    Curve(t, 148f, 208f, 246f, 268f), 0f);

                // THE EVENING SHADOW FADES.
                //
                // What lights the hall at night is not the directional sun but
                // the inside lights (the pools + the warm fill). A full-strength
                // directional shadow looks wrong under that light: the light
                // comes from the ceiling but the shadow from the side.
                Sun.shadowStrength = Curve(t, 0.85f, 1.00f, 0.90f, 0.35f);
                Sun.color = Mix(t,
                    new Color(1.00f, 0.88f, 0.74f),
                    new Color(1.00f, 0.97f, 0.93f),
                    new Color(1.00f, 0.85f, 0.66f),
                    new Color(0.62f, 0.55f, 0.72f));
                Sun.intensity = Curve(t, 1.05f, 1.55f, 1.25f, 0.32f);
            }

            // THE FILL LIGHT = AN IMITATION OF BOUNCE.
            //
            // The user's question: "wouldn't light in reality bounce and
            // light the other parts too". Yes - and in this scene there is NO
            // bounce at all: there is no global illumination, and a light map
            // CANNOT BE BAKED either, because the whole restaurant is built at
            // runtime (RestaurantView generates the geometry from the table
            // count). The only thing that can stand in for bounce is a
            // shadowless fill from the opposite direction, plus ambient light.
            //
            // At night this fill used to be BLUE (0.42 / 0.46 / 0.70) at an
            // intensity of 0.22. So every surface the warm key did not reach
            // was filled with a COLD light - the exact opposite of what bounce
            // does inside a warm hall. Most of the "it is not bright enough
            // inside" complaint came from here: the half of each body that did
            // not face the key was both dark and the wrong colour.
            //
            // At night it is now warm and strong: real bounce is the WARM
            // light coming off the ceiling and the walls too.
            if (Fill != null)
            {
                Fill.color = Mix(t,
                    new Color(0.70f, 0.78f, 0.95f),
                    new Color(0.74f, 0.80f, 0.94f),
                    new Color(0.86f, 0.76f, 0.72f),
                    new Color(0.94f, 0.78f, 0.60f));
                Fill.intensity = Curve(t, 0.50f, 0.58f, 0.52f, 0.75f);
            }

            RenderSettings.ambientLight = Mix(t,
                new Color(0.30f, 0.32f, 0.39f),
                new Color(0.38f, 0.39f, 0.43f),
                new Color(0.35f, 0.31f, 0.31f),
                // The night ambient does NOT go fully dark: the player has to
                // be able to see which table is occupied. A dark atmosphere
                // must not come at the price of a hall that cannot be read.
                //
                // WARM and brighter (0.21/0.20/0.25 -> 0.32/0.28/0.26). In this
                // pipeline the ambient is the only counterpart of bounce: there
                // is no global illumination and, because the geometry is
                // generated at runtime, no light map can be baked. A cold
                // ambient is the wrong answer in a warmly lit hall.
                //
                // The ambient is GLOBAL: it hits the street as well. That is
                // acceptable and in fact right - in reality the pavement outside
                // a brightly lit restaurant is lit by the light spilling from
                // its window. What keeps the contrast is not the ambient but the
                // sky being nearly black at night.
                new Color(0.66f, 0.58f, 0.50f));

            // THE BACKGROUND STANDS IN FOR THE SKY.
            //
            // docs/19 does not carry a sky dome (the fill-rate budget); the
            // background is a flat colour. Once the street became visible that
            // flat colour had to be "the outside" - it was nearly black at
            // first and even the daytime read as night. Now it is a pale blue
            // in the morning, a light blue at midday, warm in the afternoon
            // and really dark in the evening.
            // THE GROUND MUST SIT BELOW THE SUBJECT, and by day it did not.
            //
            // Measured off the shipped store frames (median luminance):
            //
            //     evening     interior 51   background 34   subject 1.5x brighter
            //     midday      interior 62   background 132  GROUND 2.1x brighter
            //
            // That is a figure/ground inversion, and it is the whole reason
            // the night frame looks like a game and the day frames look like
            // a plan. In daylight the restaurant - the subject, the thing the
            // player is asked to watch - was the DARKEST region of the frame,
            // wrapped on three sides by a field twice its brightness. The eye
            // goes to the bright field and slides off the building.
            //
            // The midday stop was (0.38, 0.51, 0.65) - a LIGHT blue, brighter
            // than anything inside a room lit only by ambient. These stops are
            // pulled down and toward neutral so the background reads as air
            // BEHIND the building rather than as a lit surface in front of it.
            //
            // They are not made dark. Night is 0.04 and stays there; the point
            // is not to turn the day into evening but to stop the sky
            // outshining the hall. The remaining separation comes from hue -
            // the sky stays cool, the interior stays warm - which is what the
            // cuisine tint was always for and could never show while the
            // value was this high.
            if (Cam != null)
                Cam.backgroundColor = Mix(t,
                    Tint(new Color(0.17f, 0.21f, 0.27f)),
                    Tint(new Color(0.21f, 0.28f, 0.36f)),
                    Tint(new Color(0.26f, 0.19f, 0.17f)),
                    new Color(0.04f, 0.05f, 0.09f));   // night: NO tint

            // THE WARM FILL INSIDE: the hall's own light. The outside going
            // cold and dark while the inside stays warm is the whole meaning
            // of the "a restaurant that is open" picture.
            //
            // IT STARTS AT THE SAME MOMENT AS THE INSIDE LIGHTS AND IS
            // STRONGER THAN IT USED TO BE.
            //
            // The pools light only the FLOOR: the tables, the chairs and the
            // figures take nothing from them. This was the real reason behind
            // the user's "the inside of the restaurant is dark" complaint -
            // the floor was lit but everything standing on it stayed in the
            // dark. The direction is almost overhead (62 degrees) and there
            // is no shadow, so it reads like lighting coming from the ceiling.
            //
            // The directional light hits the outside as well (light layers are
            // off in URP, m_SupportsLightLayers: 0) but there are no bodies on
            // the street: only three slabs and three posts. The contrast is
            // already set up by the sky being nearly black at night.
            if (Warm != null)
            {
                float w = Mathf.InverseLerp(RoomThreshold - 0.06f, 1f, t);
                Warm.enabled = w > 0.01f;
                Warm.intensity = w * 3.20f;
            }

            // THE STREET GOES DARKER AT NIGHT.
            //
            // Raising the global and lowering the local: the ambient and the
            // warm fill light the pavement too (there is no local light), so
            // the outside is darkened through its own material. The
            // measurement demanded it - the hall's median brightness was
            // LOWER than the street's average.
            if (_view != null)
                _view.TintStreet(Mathf.Lerp(1f, 0.28f,
                    Mathf.InverseLerp(RoomThreshold - 0.06f, 0.95f, t)));

            bool shouldLight = t >= LampThreshold;
            if (shouldLight != _lampsOn) Lamps(shouldLight);

            bool roomShouldLight = t >= RoomThreshold;
            if (roomShouldLight != _roomOn) RoomLights(roomShouldLight);
        }

        // =====================================================================
        /// <summary>
        /// Binds the street lamps.
        ///
        /// When the restaurant grows the view is rebuilt and the lamps are
        /// created again; the old references then point at destroyed objects
        /// and nothing lit up in the evening - with no warning. It binds
        /// again when the build stamp changes.
        /// </summary>
        private void BindLamps()
        {
            if (_view == null) _view = FindFirstObjectByType<RestaurantView>();
            if (_view == null) return;
            if (_view.BuildStamp == _bound) return;

            _bound = _view.BuildStamp;
            LampHeads = _view.LampHeads;
            LampGlow = _view.LampGlow;
            RoomGlow = _view.RoomGlow;
            _applied = -1f;       // write the state again to the new lamps
            _lampsOn = !_lampsOn; // forced so that Lamps() gets called
            _roomOn = !_roomOn;   // and RoomLights() too
        }

        private RestaurantView _view;
        private int _bound = -1;

        /// <summary>The number of street lamps bound. So the tour can ask.</summary>
        public int LampCount { get { return LampHeads == null ? 0 : LampHeads.Length; } }

        private void Lamps(bool on)
        {
            _lampsOn = on;

            if (_block == null) _block = new MaterialPropertyBlock();

            // AN UNLIT LAMP IS SOMETHING TOO: WHITE GLASS.
            //
            // When it was off the head was DARK GREY (0.24) - so by day the
            // lamp's glass and its iron were the same colour and the lantern
            // read as "a post with a thicker end". A real lantern's glass is
            // white by day as well; it is in the reference image too.
            Color c = on ? new Color(1.00f, 0.86f, 0.52f)
                         : new Color(0.86f, 0.87f, 0.85f);

            if (LampHeads != null)
                for (int i = 0; i < LampHeads.Length; i++)
                {
                    if (LampHeads[i] == null) continue;
                    LampHeads[i].GetPropertyBlock(_block);
                    _block.SetColor(BaseColorId, c);
                    // 2.2 -> 3.4: the glass is no longer a small slab but a
                    // lantern body, and it has to read as the place the beam
                    // comes out of. The emission has to be above 1, otherwise it
                    // is not a glow but merely "a light colour".
                    _block.SetColor(EmissionId, on ? c * 2.6f : Color.black);
                    LampHeads[i].SetPropertyBlock(_block);
                }

            if (LampGlow != null)
                for (int i = 0; i < LampGlow.Length; i++)
                    if (LampGlow[i] != null) LampGlow[i].SetActive(on);
        }

        /// <summary>
        /// Turns the inside ceiling lights on and off.
        ///
        /// Lights with no body: only the pool on the floor. The fitting
        /// itself is not drawn - the camera is looking at a building with no
        /// ceiling, and a box hanging up there would do nothing but cover the
        /// place it lights.
        /// </summary>
        private void RoomLights(bool on)
        {
            _roomOn = on;
            if (RoomGlow == null) return;
            for (int i = 0; i < RoomGlow.Length; i++)
                if (RoomGlow[i] != null) RoomGlow[i].SetActive(on);
        }

        /// <summary>An interpolation over four stops. Morning, midday, afternoon, evening.</summary>
        private static float Curve(float t, float a, float b, float c, float d)
        {
            if (t < 0.40f) return Mathf.Lerp(a, b, t / 0.40f);
            if (t < 0.72f) return Mathf.Lerp(b, c, (t - 0.40f) / 0.32f);
            return Mathf.Lerp(c, d, Mathf.Clamp01((t - 0.72f) / 0.28f));
        }

        /// <summary>Shifts the daytime stop towards the cuisine's tint.</summary>
        private Color Tint(Color c)
        {
            return Color.Lerp(c, SkyTint, SkyTintStrength);
        }

        private static Color Mix(float t, Color a, Color b, Color c, Color d)
        {
            if (t < 0.40f) return Color.Lerp(a, b, t / 0.40f);
            if (t < 0.72f) return Color.Lerp(b, c, (t - 0.40f) / 0.32f);
            return Color.Lerp(c, d, Mathf.Clamp01((t - 0.72f) / 0.28f));
        }
    }
}
