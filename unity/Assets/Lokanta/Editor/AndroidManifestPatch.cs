using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Fixes up the Android manifest during the build.
    ///
    /// Why we do not write an AndroidManifest.xml by hand: the manifest Unity
    /// generates changes with the engine version, and a hand-written copy goes
    /// stale silently. PATCHING the generated file only changes the lines that
    /// are our own decision.
    ///
    /// Right now there is one decision: AUTOMATIC BACKUP IS OFF.
    ///
    /// On Android android:allowBackup is on by default, and that means the
    /// save files under Application.persistentDataPath get copied to the
    /// user's Google Drive account. The game is entirely offline and collects
    /// no data; if backup were left on, the sentence "no data leaves the
    /// device" would be technically false and would contradict the Data Safety
    /// form.
    ///
    /// This is a DECISION, not an omission: turning backup on is defensible
    /// too ("your saves follow you to a new phone"), but then it has to be
    /// declared on the form. Leaving it undecided is the worst of the three.
    /// </summary>
    public sealed class AndroidManifestPatch : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 1; } }

        private const string Ns = "http://schemas.android.com/apk/res/android";

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // There is more than one manifest in the Gradle project (launcher
            // and unityLibrary). We patch every one of them that carries an
            // application tag; which one wins the merge changes with the
            // version.
            int patched = 0;
            foreach (string file in Directory.GetFiles(
                         Directory.GetParent(path).FullName,
                         "AndroidManifest.xml", SearchOption.AllDirectories))
            {
                if (Patch(file)) patched++;
            }

            // THE GRADLE SCRIPT is patched too, so that the native libraries
            // stay COMPRESSED.
            //
            // The default packaging copies the .so files out into the
            // installation: a 32 MB download turns into an install of more
            // than 110 MB on the device, because the 77 MB of payload
            // (libil2cpp.so alone is 57 MB) sits both inside the package and
            // under /data. On low-storage devices the install fails.
            //
            // BuildPlayer promised this IN A COMMENT for ages and never did it
            // in code; and in Unity 6 there is no
            // PlayerSettings.Android.useLegacyPackaging either. This is the
            // one right place.
            int gradle = 0;
            foreach (string file in Directory.GetFiles(
                         Directory.GetParent(path).FullName,
                         "build.gradle", SearchOption.AllDirectories))
            {
                if (PatchGradle(file)) gradle++;
            }

            Debug.Log(patched + " Android manifests patched (backup off), "
                      + gradle + " gradle scripts (compressed libraries).");
        }

        /// <summary>
        /// Adds the packaging setting to the application module's build.gradle.
        ///
        /// Only the APPLICATION module (the com.android.application plugin);
        /// the setting has no counterpart in library modules. It is appended AT
        /// THE END as a separate android { } block - Gradle merges blocks
        /// within the same script, so there is no need to parse the existing
        /// structure.
        /// </summary>
        private static bool PatchGradle(string file)
        {
            // THE "launcher" MODULE ONLY.
            //
            // In the first version the test was "does com.android.application
            // appear in the file", and it caught the ROOT build.gradle - there
            // that name appears in the plugin classification but there is NO
            // android { } block:
            //
            //   Could not find method android() ... on root project
            //
            // The folder name is both more precise and easier to read.
            string dir = Path.GetFileName(Path.GetDirectoryName(file));
            if (dir != "launcher") return false;

            // A VALUE IS CHANGED, NO BLOCK IS ADDED.
            //
            // Unity already writes "useLegacyPackaging true". In the first
            // version a second android { } block was appended to the end of
            // the file, and the "already patched" guard saw Unity's own line
            // and skipped EVERY TIME: the patch silently did nothing and the
            // libraries in the APK stayed compressed.
            //
            // The old packaging copies the .so files OUT into the install: a
            // 30.8 MB package takes 30.8 + 77 = ~108 MB on the device. With
            // the new one the libraries sit in the package UNCOMPRESSED and
            // are read in place: the package grows but the install drops to
            // ~77 MB and nothing is copied. Google recommends this for
            // minSdk 23+; our floor is 25.
            string text = File.ReadAllText(file);
            const string legacy = "useLegacyPackaging true";
            if (text.IndexOf(legacy, System.StringComparison.Ordinal) < 0) return false;

            File.WriteAllText(file, text.Replace(legacy, "useLegacyPackaging false"));
            return true;
        }

        private static bool Patch(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);

            // INSTALL LOCATION: INTERNAL.
            //
            // Unity's default is preferExternal and the saves land under
            // getExternalFilesDir - which means the player's sixty-day
            // campaign can be written to a removable card. The native
            // libraries are also uncompressed and read in place
            // (useLegacyPackaging false); reading those off external storage
            // is slow as well.
            //
            // In Unity 6 PlayerSettings.Android.forceInternalStorage is gone;
            // this attribute is its counterpart.
            XmlNode manifest = doc.SelectSingleNode("/manifest");
            if (manifest != null && manifest.Attributes != null)
                Set(doc, manifest, "installLocation", "internalOnly");

            XmlNode app = doc.SelectSingleNode("/manifest/application");
            if (app == null || app.Attributes == null) return false;

            Set(doc, app, "allowBackup", "false");

            // NOTE: extractNativeLibs IS NOT WRITTEN HERE.
            //
            // It was tried and AGP broke the build - while telling us the
            // right place:
            //
            //   android:extractNativeLibs is set to "false" in
            //   AndroidManifest.xml. Avoid setting it explicitly, and
            //   instead set android.packagingOptions.jniLibs.
            //   useLegacyPackaging to false in the build script.
            //
            // So the setting belongs not in the manifest but IN THE GRADLE
            // SCRIPT. PatchGradle() writes it there.
            Set(doc, app, "fullBackupContent", "false");
            Set(doc, app, "dataExtractionRules", null);   // remove it if present

            doc.Save(file);
            return true;
        }

        private static void Set(XmlDocument doc, XmlNode node, string name, string value)
        {
            XmlAttribute existing = node.Attributes[name, Ns];

            if (value == null)
            {
                if (existing != null) node.Attributes.Remove(existing);
                return;
            }

            if (existing == null)
            {
                existing = doc.CreateAttribute("android", name, Ns);
                node.Attributes.Append(existing);
            }
            existing.Value = value;
        }
    }
}
