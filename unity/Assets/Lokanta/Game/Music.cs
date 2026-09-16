using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// THE MUSIC IS GENERATED IN CODE TOO. There is not a single sound
    /// file.
    ///
    /// The approach is not a short loop but CONTINUOUS SYNTHESIS.
    /// OnAudioFilterRead is called on every audio frame and we produce the
    /// notes right there. That has two benefits: zero bytes in the APK,
    /// and the music can follow the state of the game - the tempo rises as
    /// service gets busier.
    ///
    /// The musical choice was kept independent of the cuisine's identity.
    /// Putting "Turkish music" in a Turkish restaurant would be a cheap
    /// shortcut; the identity lives IN THE HALL (docs/10). What is here is
    /// a calm, repetitive background that does not come forward - a
    /// management game is played for hours, and a melody that stands out
    /// becomes the enemy in the third hour.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class Music : MonoBehaviour
    {
        public float Volume = 0.22f;

        /// <summary>0 calm, 1 busy. Set from how full service is.</summary>
        public float Intensity = 0f;

        private double _phase;
        private double _sampleRate = 48000.0;
        private int _step;
        private double _stepTime;
        private float _current;

        // C minor pentatonic: plain, a scale that "goes" nowhere.
        // A scale that never resolves is the right one for music that is
        // meant to stay in the background.
        private static readonly float[] Scale =
        {
            130.81f, 155.56f, 174.61f, 196.00f, 233.08f,      // C3 Eb3 F3 G3 Bb3
            261.63f, 311.13f, 349.23f, 392.00f, 466.16f,      // C4 ...
        };

        // The step sequence was written by hand: random notes SOUND random
        // over a long listen. Something repetitive becomes ordinary sooner
        // but bothers you less.
        private static readonly int[] Steps =
        { 0, 3, 2, 5, 3, 7, 5, 3, 2, 0, 3, 5, 7, 5, 3, 2 };

        private void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            AudioSource src = GetComponent<AudioSource>();
            src.playOnAwake = true;
            src.loop = true;
            src.spatialBlend = 0f;
            // An empty clip: the source has to be PLAYING for
            // OnAudioFilterRead to be called.
            src.clip = AudioClip.Create("silent", 1024, 1, (int)_sampleRate, false);
            src.Play();
        }

        /// <summary>
        /// ON QUIT THE SOUND GOES SILENT FIRST.
        ///
        /// OnAudioFilterRead is called FROM THE AUDIO THREAD, that is,
        /// independently of the main thread. If this callback is still inside
        /// while the AudioSource and its clip are being destroyed as the
        /// application quits, you get an access violation (0xC0000005) - the
        /// Windows build did exactly this on quit and nobody saw it, because
        /// NOTHING WAS LOOKING at the tour's exit code (see
        /// tools/unity/tour.ps1).
        ///
        /// It matters for the player too: a game that crashes on the way out
        /// shows Windows' "the program has stopped working" window.
        ///
        /// The flag is `volatile`: two separate threads read and write it.
        /// </summary>
        private volatile bool _silenced;

        private void OnApplicationQuit()
        {
            // THREE STEPS, BECAUSE TWO WERE NOT ENOUGH.
            //
            // Just the flag + Stop was tried and the access violation on the way
            // out carried on. The component ITSELF is switched off (Unity then
            // never calls the callback at all) and the listener is paused: so
            // that no window is left in which the audio thread can overlap with
            // the shutdown cleanup.
            _silenced = true;
            AudioSource src = GetComponent<AudioSource>();
            if (src != null) src.Stop();
            enabled = false;
            AudioListener.pause = true;
        }

        private void OnDisable() { _silenced = true; }

        // So that the music comes back if the component is enabled again:
        // OnDisable raises the flag, and if we never lowered it the
        // component would stay silent.
        private void OnEnable() { _silenced = false; }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_silenced)
            {
                for (int i = 0; i < data.Length; i++) data[i] = 0f;
                return;
            }

            // The note length shortens with intensity: calm 0.62 s, busy 0.40.
            double noteSeconds = Mathf.Lerp(0.62f, 0.40f, Mathf.Clamp01(Intensity));
            double inc = 1.0 / _sampleRate;

            for (int i = 0; i < data.Length; i += channels)
            {
                _stepTime += inc;
                if (_stepTime >= noteSeconds)
                {
                    _stepTime -= noteSeconds;
                    _step = (_step + 1) % Steps.Length;
                    _phase = 0.0;
                }

                float hz = Scale[Steps[_step] % Scale.Length];
                _phase += hz * inc;

                float t = (float)(_stepTime / noteSeconds);
                // A soft envelope: every note comes in and fades out quietly.
                float env = Mathf.Sin(t * Mathf.PI);
                env *= env;

                // A triangle wave sounds a little more "wooden" than a sine and is
                // not as harsh as a square wave.
                float phase = (float)(_phase - System.Math.Floor(_phase));
                float tri = 4f * Mathf.Abs(phase - 0.5f) - 1f;

                // An octave down, very quiet: the floor.
                float bass = Mathf.Sin((float)(_phase * 0.5f) * 2f * Mathf.PI) * 0.30f;

                _current = Mathf.Lerp(_current, (tri * 0.55f + bass) * env, 0.25f);
                float v = _current * Volume;

                for (int c = 0; c < channels; c++) data[i + c] = v;
            }
        }
    }
}
