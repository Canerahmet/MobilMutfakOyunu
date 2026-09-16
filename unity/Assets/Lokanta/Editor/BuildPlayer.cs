using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Takes a playable build.
    ///
    /// There are two targets and both are needed:
    ///
    ///   Windows - for VERIFICATION. There is no device, and play mode in the
    ///   editor hangs in batch mode; the only way to actually run the game is
    ///   a desktop build. Together with the Autopilot it verifies the
    ///   interface, the sound, the save and the simulation in a single run.
    ///
    ///   Android - the REAL target. Its settings are written down here so that
    ///   next time nobody has to go hunting for "what was the package name".
    ///
    ///   .\tools\unity\run.ps1 -Method Lokanta.EditorTools.BuildPlayer.Windows
    ///   .\tools\unity\run.ps1 -Method Lokanta.EditorTools.BuildPlayer.Android
    /// </summary>
    public static class BuildPlayer
    {
        // =====================================================================
        // BUILD PARAMETERS: the command line and environment variables.
        //
        // The version code and the signing are the two things that cannot be
        // baked into the script: the first has to go up with every release,
        // the second must never enter the repository.
        // =====================================================================

        /// <summary>An environment variable; an empty string if it is absent.</summary>
        private static string Env(string name)
        {
            string v = System.Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(v) ? string.Empty : v.Trim();
        }

        /// <summary>The value of a command line flag; the fallback if it is absent.</summary>
        private static string Arg(string flag, string fallback)
        {
            string[] a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (a[i] == flag) return a[i + 1];
            return fallback;
        }

        private static int ArgInt(string flag, int fallback)
        {
            int v;
            return int.TryParse(Arg(flag, string.Empty), out v) ? v : fallback;
        }

        private const string Scene = "Assets/Lokanta/Game.unity";
        /// <summary>
        /// The package name. IT IS LOCKED FOREVER ON THE FIRST UPLOAD.
        ///
        /// One source: ProjectSetup reads it from here too. When the two wrote
        /// it separately there was no way to see which had run last, and a
        /// single upload under the wrong package name cannot be undone.
        /// </summary>
        public const string Package = "com.ahmetakar.lokanta";

        /// <summary>The company name. One source; ProjectSetup reads it from here.</summary>
        public const string Company = "Ahmet Akar";

        /// <summary>The product name. One source; ProjectSetup reads it from here.</summary>
        public const string Product = "Lokanta";

        // =====================================================================
        [MenuItem("Lokanta/Build - Windows")]
        public static void Windows() { WindowsBuild("windows", il2cpp: false); }

        /// <summary>
        /// THE STRIPPING EXAM. Windows, but with Android's compiler and
        /// stripper: IL2CPP + ManagedStrippingLevel.High.
        ///
        /// Why it exists: link.xml's own comment said "the bug this file
        /// guards against lives in the one configuration NO test in the
        /// project can reach" - that is, the protection was written by
        /// reasoning and had NEVER BEEN RUN. Even with no device to run an APK
        /// on, the stripper runs here: the same link.xml, the same
        /// Lokanta.Content and Newtonsoft assemblies, the same content loading
        /// through reflection.
        ///
        /// If the protection were wrong the symptom would not be a crash but a
        /// SILENT DEFAULT; the game falls to the error screen in its start-up
        /// validation and the tour stops at the first check. So the tour's 129
        /// checks can measure this configuration as well.
        ///
        /// Where Android is not exactly the same: how the engine modules are
        /// stripped varies by platform. What is the same, and what matters, is
        /// the MANAGED stripping of our own assemblies.
        ///
        ///   .\tools\unity\tour.ps1 -Build windows-il2cpp
        /// </summary>
        [MenuItem("Lokanta/Build - Windows (IL2CPP + stripping)")]
        public static void WindowsIl2cpp() { WindowsBuild("windows-il2cpp", il2cpp: true); }

        private static void WindowsBuild(string leaf, bool il2cpp)
        {
            Common();

            // THE SETTING IS PUT BACK. An IL2CPP build takes ~15 minutes, Mono
            // ~1; if the setting stays in the project every tour run gets
            // fifteen times slower, and that can be paid for months without
            // anyone noticing.
            ScriptingImplementation previousBackend =
                PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            ManagedStrippingLevel previousStripping =
                PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Standalone);

            if (il2cpp)
            {
                PlayerSettings.SetScriptingBackend(
                    NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetManagedStrippingLevel(
                    NamedBuildTarget.Standalone, ManagedStrippingLevel.High);
            }

            try { WindowsCore(leaf); }
            finally
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, previousBackend);
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, previousStripping);
            }
        }

        private static void WindowsCore(string leaf)
        {
            string dir = Out(leaf);
            BuildPlayerOptions o = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Path.Combine(dir, "Lokanta.exe"),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            // Windowed and fixed: a full-screen build ties the screenshot to
            // the display's resolution and draws a different frame on every
            // machine.
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;

            Report(BuildPipeline.BuildPlayer(o), leaf);
        }

        // =====================================================================
        /// <summary>The APK to push to a device. For testing.</summary>
        [MenuItem("Lokanta/Build - Android APK")]
        public static void Android() { AndroidBuild(bundle: false); }

        /// <summary>
        /// The AAB to upload to the store. Since 2021 the Play Store accepts
        /// nothing else.
        /// </summary>
        [MenuItem("Lokanta/Build - Android AAB")]
        public static void AndroidBundle() { AndroidBuild(bundle: true); }

        private static void AndroidBuild(bool bundle)
        {
            Common();

            // Written EXPLICITLY. It was not written before and Unity used the
            // setting left in the project: the file was called Lokanta.apk but
            // INSIDE it was an app bundle (base/, BUNDLE-METADATA/). A file
            // with the wrong name that cannot be pushed to a device.
            EditorUserBuildSettings.buildAppBundle = bundle;

            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, Package);

            // Landscape, NOT portrait: the floor plan is 18.0 x 9.6 m, that is,
            // a wide rectangle. On a portrait phone half the restaurant would
            // fall outside the frame (the docs/31 framing measurement).
            // AUTOROTATION, NOT LandscapeLeft.
            //
            // A fixed orientation makes Unity write
            // android:screenOrientation="landscape" into the manifest and
            // IGNORE the allowedAutorotateToLandscapeRight flag - Unity only
            // looks at those flags when the default AutoRotation is set. The
            // result: turn the phone the other way round and the game is
            // upside down, with no way to fix it.
            //
            // AutoRotation plus landscape-only flags writes sensorLandscape
            // into the manifest: both landscape orientations free, portrait
            // off.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // RUN IN BACKGROUND IS OFF.
            //
            // The Windows route (for screenshots and the automatic tour) sets
            // this to true, and the value is written into THE PROJECT SETTING,
            // so the Android build was inheriting it. The music is generated
            // continuously through OnAudioFilterRead: while the phone was
            // ringing the game carried on playing and drawing at 30 fps.
            PlayerSettings.runInBackground = false;

            // The saves live on INTERNAL storage.
            //
            // In Unity 6 PlayerSettings.Android.forceInternalStorage IS GONE;
            // its counterpart is installLocation in the manifest, and
            // AndroidManifestPatch writes it. The default, preferExternal,
            // lets the save land on a removable card.
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // API 29 (Android 10): THE FLOOR THE DOCUMENTS WRITE DOWN.
            //
            // It used to be 25 (Android 7.1) while the supported device table
            // in docs/19 says "Android 10, 3 GB RAM, Vulkan or OpenGL ES 3.1".
            // That is, the build was allowing installation on devices that
            // cannot run the game: a 2017 phone, 1 GB, GLES 3.0. The review
            // such a phone leaves behind spoils the listing and cannot be
            // undone.
            //
            // This is not a new decision, it is putting the WRITTEN decision
            // into code - the old justification for this very line was already
            // "what the setting says and what the build produces must agree".
            //
            // A side benefit: the adaptive icon is enough from API 26 onwards,
            // so the legacy and round icon slots become unnecessary too (Unity
            // was printing a warning saying it had deprecated both).
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            // The target SDK is written EXPLICITLY, NOT "whatever is
            // installed".
            //
            // AndroidApiLevelAuto ties the build's target to the SDK installed
            // on the machine: another machine produces another number, and when
            // the store's threshold moves nobody notices.
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;

            // IL2CPP + ARM64: the Play Store requires 64 bit.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // THE VERSION COMES FROM THE COMMAND LINE.
            //
            // Both were constants: every run of the script produced
            // versionCode 1, and Play refuses the second upload with "Version
            // code 1 has already been used". A number once used can never be
            // used again, so this constant is a class of bug that cannot be
            // fixed after release.
            //
            // The flag names stay as they are: docs/21-business-and-release.md
            // documents them by these spellings, and a flag renamed on one side
            // only is a release script that quietly builds version 1 again.
            //
            //   -lokanta-version-code 3  -lokanta-version 0.3.0
            PlayerSettings.Android.bundleVersionCode = ArgInt("-lokanta-version-code", 1);
            PlayerSettings.bundleVersion = Arg("-lokanta-version", "0.1.0");

            // The native libraries should stay COMPRESSED.
            //
            // The default "legacy packaging" copies them out into the install:
            // a 32 MB download turns into an install of more than 110 MB on the
            // device, because the 77 MB of .so payload (libil2cpp.so alone is
            // 57 MB) sits both inside the package and under /data. On
            // low-storage devices the install fails.
            //
            // THIS COMMENT MADE A PROMISE FOR A LONG TIME AND THE CODE DID NOT
            // KEEP IT: the two lines below set the symbols and the frame
            // pacing, and NEVER wrote the packaging setting at all. When the
            // manifest of a generated package was unpacked it read
            // extractNativeLibs="true" - that is, the problem described here
            // was still exactly there.
            //
            // In Unity 6 PlayerSettings.Android.useLegacyPackaging DOES NOT
            // EXIST (the compiler refuses it). The setting now lives on the
            // Gradle side and AndroidManifestPatch writes it into both the
            // manifest and build.gradle - which is where the patch was ALREADY
            // running.
            // NATIVE SYMBOLS ARE GENERATED.
            //
            // In an IL2CPP build the crashes happen inside libil2cpp.so;
            // without symbols the stacks in the Play Console show bare
            // addresses. The Android vitals crash rate is a STORE VISIBILITY
            // threshold - an app over the threshold drops out of promotion, and
            // a report that arrives without symbols cannot be used to fix the
            // bug. There is no other channel either (the crash report API is
            // closed).
            //
            // The symbol file DOES NOT GO INTO THE PACKAGE, it is uploaded
            // separately - so there is no size argument against it.
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
            PlayerSettings.Android.optimizedFramePacing = true;

            // The managed code IS STRIPPED: unused types and methods drop out
            // of the IL2CPP output.
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Android, ManagedStrippingLevel.High);
            PlayerSettings.Android.forceSDCardPermission = false;

            // THE SIGNING COMES FROM ENVIRONMENT VARIABLES.
            //
            // It used to be unconditionally false, that is, every build was
            // signed with the DEBUG key (certificate: CN=Android Debug). Play
            // refuses such a package at upload time.
            //
            // No password GOES into the code or the repository: all four
            // variables are read from the environment. If none of them is set
            // the old behaviour continues - taking an unsigned build for local
            // experiments is still possible.
            //
            //   LOKANTA_KEYSTORE       the keystore file
            //   LOKANTA_KEYSTORE_PASS  the keystore's password
            //   LOKANTA_KEYALIAS       the key alias
            //   LOKANTA_KEYALIAS_PASS  the alias's password
            //
            // IF the key file IS LOST the app can never be updated again; it
            // has to be backed up in two separate places.
            string store = Env("LOKANTA_KEYSTORE");
            string storePass = Env("LOKANTA_KEYSTORE_PASS");
            string alias = Env("LOKANTA_KEYALIAS");
            string aliasPass = Env("LOKANTA_KEYALIAS_PASS");

            bool signed = store.Length > 0 && storePass.Length > 0
                          && alias.Length > 0 && aliasPass.Length > 0;
            PlayerSettings.Android.useCustomKeystore = signed;
            if (signed)
            {
                PlayerSettings.Android.keystoreName = store;
                PlayerSettings.Android.keystorePass = storePass;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = aliasPass;
                Debug.Log("  signing: upload key (" + alias + ")");
            }
            else
            {
                Debug.LogWarning("  signing: the DEBUG key - "
                                 + "Play will REFUSE this package. "
                                 + "Set LOKANTA_KEYSTORE and the rest.");

                // AN APP BUNDLE IS NEVER PRODUCED UNSIGNED.
                //
                // Warning and carrying on produced an .aab signed with the
                // debug key; the Play Console refuses it AT UPLOAD TIME and
                // that is only noticed on the way to the store. What is more:
                // the first key uploaded is PERMANENT.
                //
                // For an APK a warning is enough (a debug signature is enough
                // to push it to a device and try it); the AAB is for release.
                if (bundle)
                    throw new BuildFailedException(
                        "A release bundle (AAB) cannot be produced unsigned. "
                        + "Set LOKANTA_KEYSTORE, LOKANTA_KEYSTORE_PASS, "
                        + "LOKANTA_KEYALIAS and LOKANTA_KEYALIAS_PASS. "
                        + "If the key file is lost the app can never be "
                        + "updated again - back it up in two separate places.");
            }

            string dir = Out("android");
            BuildPlayerOptions o = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Path.Combine(
                    dir, bundle ? "Lokanta.aab" : "Lokanta.apk"),
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            Report(BuildPipeline.BuildPlayer(o), bundle ? "Android AAB" : "Android APK");
            DropBurstDebug(dir);
        }

        // =====================================================================
        /// <summary>The settings that are the same on both targets.</summary>
        private static void Common()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;

            // THE CONTENT SYNC IS PART OF THE BUILD.
            //
            // The game's content is generated under content/ and copied under
            // Assets/Resources/content/ so that Unity can see it. The copying
            // used to be a menu item done BY HAND; a build taken with a stale
            // copy goes to the store with DIFFERENT balance numbers FROM THE
            // ONES TESTED and not a single test breaks. SyncContent's own
            // comment says "exactly this class of bug has happened twice on
            // this project" - the third time is stopped by the build itself.
            SyncContent.Run();

            // THE ADVANCED TEXT GENERATOR IS PART OF THE BUILD.
            //
            // Arabic letter joining depends on this setting and the setting is
            // a checkbox. A box ticked by hand is unticked on a machine that
            // has just cloned the repository; Arabic breaks and not a single
            // test fails - the lesson of the content sync, in the same words.
            AdvancedText.Set(true);

            // THE SPLASH SCREEN IS OFF.
            //
            // There is no logo (m_SplashScreenLogos is empty) but the screen
            // was on: at start-up the player was looking at ~2 seconds of a
            // COMPLETELY EMPTY dark screen. In Unity 6 turning it off is free
            // on every licence tier.
            PlayerSettings.SplashScreen.show = false;

            // The colour space is LINEAR. In gamma, low-poly flat colours look
            // washed out and the shadows harden; URP assumes linear anyway.
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // vSync IS NOT SET HERE, it is set AT RUN TIME.
            //
            // It used to read "= 1" here and that contradicted ProjectSetup.cs's
            // decision that "the code sets the frame rate" (vSyncCount = 0)
            // head on. What is more, both were writing to the wrong place:
            // ProjectSetup writes to the quality level that is ACTIVE in the
            // editor (Ultra), while Android uses level 2 (Medium).
            //
            // The consequence is concrete: while vSyncCount != 0, Android
            // IGNORES Application.targetFrameRate. On a cheap 90 or 120 Hz
            // phone the game was aiming at 120 fps - when the target is a floor
            // of 30 fps. In a management game looking at a static scene that
            // means twice the CPU/GPU and twice the heat.
            //
            // The right place is GameApp.Awake: on the device, while running,
            // and independent of which quality level is active.

            if (!File.Exists(Scene))
                Debug.LogWarning("No scene: " + Scene
                                 + "  (Lokanta > Build the game scene)");
        }

        private static string Out(string leaf)
        {
            string root = Path.GetDirectoryName(
                Path.GetDirectoryName(Application.dataPath));
            string dir = Path.Combine(root, "build", leaf);
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// The debug folder Burst leaves behind. Its name says "DoNotShip"; if
        /// it is not deleted it can end up shipped by accident.
        /// </summary>
        private static void DropBurstDebug(string dir)
        {
            foreach (string d in Directory.GetDirectories(dir))
                if (d.EndsWith("_BurstDebugInformation_DoNotShip"))
                {
                    Directory.Delete(d, true);
                    Debug.Log("  the Burst debug folder was deleted.");
                }
        }

        private static void Report(BuildReport r, string what)
        {
            BuildSummary s = r.summary;
            if (s.result != BuildResult.Succeeded)
            {
                Debug.LogError(string.Format(
                    "PROBLEMS: the {0} build failed - {1} errors, {2} warnings",
                    what, s.totalErrors, s.totalWarnings));
                return;
            }

            // The size is read FROM THE FILE, not from the report.
            //
            // On Android BuildSummary.totalSize gives the uncompressed total:
            // for a 35 MB package it printed 828 MB, and for a moment it looked
            // as though the game would not fit in the store.
            float mb = File.Exists(s.outputPath)
                ? new FileInfo(s.outputPath).Length / 1024f / 1024f
                : s.totalSize / 1024f / 1024f;

            Debug.Log(string.Format(
                "=== Lokanta build ===\n  target : {0}\n  path   : {1}\n"
                + "  size   : {2:0.0} MB\n  time   : {3:0} s\n  warnings: {4}\n"
                + "=== build done ===",
                what, s.outputPath, mb, s.totalTime.TotalSeconds, s.totalWarnings));
        }
    }
}
