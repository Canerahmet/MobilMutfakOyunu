using System;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Sounds come FROM A FILE first, and from code only if there is none.
    ///
    /// If there is a clip under `Resources/audio/<name>` that one is
    /// played; if not, the synthesised tone of the same name steps in. So
    /// adding a sound file NEEDS NO CODE CHANGE - putting the file in the
    /// folder is enough.
    ///
    /// Why it works this way: for a while every sound was produced in code
    /// alone, and the reasoning was commercial ("every file downloaded is
    /// a licence decision"). The reasoning is half right: a licence is a
    /// real risk, but synthesis is a real cost too - simple waveforms
    /// sound cheap in a restaurant game, and sounds like a kitchen, a
    /// doorbell or a crowd are not convincing when synthesised.
    ///
    /// The two-layer answer solves both: the synthesis stays as the
    /// FALLBACK (so the game runs complete with no sound files at all and
    /// adds zero bytes to the APK), and if a file is put there, the file
    /// wins. The list of files to add and where they come from:
    /// Art/ATTRIBUTION.md.
    ///
    /// The synthesis is simple but deliberate: every sound is multiplied
    /// by an ENVELOPE (attack-release), because a tone without one sounds
    /// like a "beep". The clicks are short and soft; the kitchen sounds
    /// are noisy; the money sound has two tones.
    /// </summary>
    public static class Sfx
    {
        /// <summary>
        /// The sample rate. 22050, not 44100.
        ///
        /// The sounds are short, narrow-band things - a click, a confirm,
        /// money, a bell; at 22 kHz the difference cannot be heard, but the
        /// memory and the synthesis time are HALVED. At the previous value
        /// ten clips meant 181,000 samples, all of them produced at startup,
        /// through a delegate, most of them calling a few Mathf.Sin - 20-40
        /// ms in the first frame on a low-end phone.
        ///
        /// The clip data also drops from 724 KB to 362 KB, and the temporary
        /// float arrays do not fall into the large object heap.
        /// </summary>
        public const int SampleRate = 22050;

        private static AudioSource _source;
        private static AudioClip _click, _confirm, _cancel, _coin, _bell,
                                 _sizzle, _pour, _upset, _levelUp, _day,
                                 _alarm, _empty, _combo;
        private static float _volume = 0.7f;
        private static bool _built;

        public static float Volume
        {
            get { return _volume; }
            set { _volume = Mathf.Clamp01(value); }
        }

        /// <summary>
        /// Returns the file if there is one, otherwise the synthesised clip.
        ///
        /// Resources.Load also returns null quietly when the folder does not
        /// exist, so a project that has never had a sound file added does not
        /// throw.
        /// <summary>
        /// The file if there is one, otherwise it SYNTHESISES.
        ///
        /// The synthesis now runs only when it is needed: the file is looked
        /// for first and the tone is produced only if it is not found.
        /// Before, all ten clips were synthesised unconditionally and then
        /// THROWN AWAY if a file existed - so part of the work was wasted
        /// from the start.
        /// </summary>
        private static AudioClip Prefer(string file, System.Func<AudioClip> synth)
        {
            AudioClip c = Resources.Load<AudioClip>("audio/" + file);
            return c != null ? c : synth();
        }

        /// <summary>Called once in the scene.</summary>
        public static void Init(AudioSource source)
        {
            _source = source;
            if (_built) return;

            // FOR EVERY CLIP, THE FILE FIRST AND THE SYNTHESIS IF THERE IS NONE.
            //
            // The names are the same as the list in Art/ATTRIBUTION.md; that
            // table says which file lands where.
            _click = Prefer("tik", () => Tone("tik", 0.045f, (t, n) =>
                Env(t, n, 0.004f, 0.040f) * Sine(t, 880f) * 0.35f));

            _confirm = Prefer("onay", () => Tone("onay", 0.22f, (t, n) =>
                Env(t, n, 0.010f, 0.20f) *
                (Sine(t, 587f) * 0.5f + Sine(t, 880f) * 0.35f) * 0.4f));

            _cancel = Prefer("iptal", () => Tone("iptal", 0.18f, (t, n) =>
                Env(t, n, 0.006f, 0.17f) *
                (Sine(t, 392f) * 0.5f + Sine(t, 294f) * 0.35f) * 0.4f));

            // Money: two quick tones, the second one higher. The feel of a "till".
            _coin = Prefer("para", () => Tone("para", 0.26f, (t, n) =>
            {
                float a = Env(t, n, 0.004f, 0.09f) * Sine(t, 1047f);
                float b = t > 0.07f
                    ? Env(t - 0.07f, n, 0.004f, 0.15f) * Sine(t - 0.07f, 1568f)
                    : 0f;
                return (a * 0.45f + b * 0.5f) * 0.4f;
            }));

            // Bell: a door chime. Three harmonics, a long release.
            _bell = Prefer("kapi-zili", () => Tone("zil", 0.85f, (t, n) =>
                Env(t, n, 0.003f, 0.80f) *
                (Sine(t, 1319f) * 0.5f + Sine(t, 1976f) * 0.28f
                 + Sine(t, 2637f) * 0.14f) * 0.32f));

            // Sizzle: noise, softened for a low-pass feel.
            _sizzle = Prefer("cizirti", () => Tone("cizirti", 0.55f, (t, n) =>
                Env(t, n, 0.05f, 0.45f) * Noise(t) * 0.22f));

            // Pouring: noise + a falling tone.
            _pour = Prefer("dokme", () => Tone("dokme", 0.45f, (t, n) =>
                Env(t, n, 0.03f, 0.40f) *
                (Noise(t) * 0.35f + Sine(t, 320f - 120f * (t / n)) * 0.25f) * 0.3f));

            // An angry guest: two falling tones.
            _upset = Prefer("kizgin", () => Tone("kizgin", 0.35f, (t, n) =>
                Env(t, n, 0.006f, 0.32f) *
                Sine(t, 330f - 90f * (t / n)) * 0.42f));

            // Levelling up: a rising triad.
            _levelUp = Prefer("seviye", () => Tone("seviye", 0.5f, (t, n) =>
            {
                float f = t < 0.14f ? 523f : (t < 0.28f ? 659f : 784f);
                return Env(t, n, 0.006f, 0.45f) * Sine(t, f) * 0.4f;
            }));

            // PATIENCE RUNNING OUT. docs/17 marks this as one of the two
            // sounds that matter most, and it had no sound of its own - it
            // borrowed the angry-guest grumble, which is the sound of being
            // TOO LATE. A warning has to be distinguishable from a failure,
            // so this one RISES where `kizgin` falls.
            _alarm = Prefer("uyari", () => Tone("uyari", 0.30f, (t, n) =>
            {
                float f = t < 0.12f ? 740f : 988f;
                return Env(t, n, 0.004f, 0.26f) * Sine(t, f) * 0.34f;
            }));

            // ASKED FOR AND NOT THERE. docs/17's other bold row, and it had
            // no sound at all: a guest asking for something the kitchen
            // cannot make was exactly as quiet as one who never asked. Dull
            // and short - a shelf coming up empty, not an error.
            _empty = Prefer("bitti", () => Tone("bitti", 0.20f, (t, n) =>
                Env(t, n, 0.004f, 0.18f) *
                (Sine(t, 196f) * 0.45f + Noise(t) * 0.12f) * 0.34f));

            // A COMBO LANDED. The signature mechanic of fast food emitted
            // SimEventKind.ComboOrdered and NOTHING listened: no sound, no
            // notice, no badge. The share reaches the player in the evening
            // report and at year end, so the mechanic was not invisible - but
            // the MOMENT was, and the moment is what teaches the player that
            // the toggle they flicked is doing something.
            //
            // Two notes UP a fourth, short and bright. It has to sit apart
            // from `para` (the same guest pays a second later) and from
            // `seviye` (three notes, a much bigger event), so it is quieter
            // and shorter than either.
            _combo = Prefer("kombo", () => Tone("kombo", 0.16f, (t, n) =>
            {
                float f = t < 0.07f ? 587f : 784f;
                return Env(t, n, 0.004f, 0.14f) * Sine(t, f) * 0.26f;
            }));

            // The turn of the day: soft, low, two tones.
            _day = Prefer("gun-donumu", () => Tone("gun", 0.7f, (t, n) =>
                Env(t, n, 0.06f, 0.62f) *
                (Sine(t, 262f) * 0.4f + Sine(t, 349f) * 0.3f) * 0.35f));

            _built = true;
        }

        /// <summary>
        /// How many sounds come FROM A FILE. The tour reports this: saying
        /// "the sounds are fine" would otherwise have counted the ten
        /// synthesised tones as valid too.
        /// </summary>
        public static int FileBackedCount()
        {
            string[] names = { "tik", "onay", "iptal", "para", "kapi-zili",
                               "cizirti", "dokme", "kizgin", "seviye",
                               "gun-donumu" };
            int n = 0;
            for (int i = 0; i < names.Length; i++)
                if (Resources.Load<AudioClip>("audio/" + names[i]) != null) n++;
            return n;
        }

        // --- playing ----------------------------------------------------------
        private static void Play(AudioClip c, float scale = 1f)
        {
            if (_source == null || c == null) return;
            _source.PlayOneShot(c, _volume * scale);
        }

        public static void Click() { Play(_click); }
        public static void Confirm() { Play(_confirm); }
        public static void Cancel() { Play(_cancel); }
        public static void Coin() { Play(_coin); }
        public static void DoorBell() { Play(_bell, 0.6f); }
        public static void Sizzle() { Play(_sizzle, 0.5f); }
        public static void Pour() { Play(_pour, 0.6f); }
        public static void Upset() { Play(_upset, 0.8f); }
        public static void LevelUp() { Play(_levelUp); }
        public static void DayChange() { Play(_day); }
        public static void Alarm() { Play(_alarm, 0.7f); }
        public static void Empty() { Play(_empty, 0.7f); }
        public static void Combo() { Play(_combo, 0.8f); }

        // --- synthesis --------------------------------------------------------
        private static AudioClip Tone(string name, float seconds, Func<float, float, float> f)
        {
            int count = Mathf.CeilToInt(seconds * SampleRate);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(f(t, seconds), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sine(float t, float hz)
        {
            return Mathf.Sin(t * hz * 2f * Mathf.PI);
        }

        /// <summary>
        /// Deterministic noise. Random IS NOT USED: the same seed means the
        /// same wave, and there is no point in a sound changing from run to
        /// run. It also never touches the core's random streams.
        /// </summary>
        private static float Noise(float t)
        {
            float x = t * 12345.678f;
            return Mathf.Repeat(Mathf.Sin(x) * 43758.5453f, 2f) - 1f;
        }

        /// <summary>An attack-release envelope. Without one a tone sounds like a "beep".</summary>
        private static float Env(float t, float total, float attack, float release)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / attack;
            float rest = total - attack;
            if (rest <= 0f) return 0f;
            float k = (t - attack) / Mathf.Max(0.0001f, release);
            return Mathf.Exp(-3f * k);
        }
    }
}
