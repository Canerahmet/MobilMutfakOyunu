using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Copies the content/ folder under Assets/Resources/content/.
    ///
    /// Why a copy: content is generated at the root of the project
    /// (tools/balance, tools/content) and that is somewhere Unity cannot see.
    /// The only synchronised source Unity does see is Resources.
    ///
    /// Why NOT renamed from .json to .txt: Unity already imports the .json
    /// extension as a TextAsset, so there is no need to rename anything.
    ///
    /// THE COPY IS A GENERATED THING. Nothing under Assets/Resources/content
    /// should be edited by hand; it is deleted and rewritten on every run.
    /// </summary>
    public static class SyncContent
    {
        private const string Target = "Assets/Resources/content";

        [MenuItem("Lokanta/Copy content into Resources")]
        public static void Run()
        {
            string root = Path.GetDirectoryName(Application.dataPath);      // unity/
            string repo = Path.GetDirectoryName(root);                      // the project root
            string source = Path.Combine(repo, "content");

            if (!Directory.Exists(source))
            {
                Debug.LogError("No content folder: " + source);
                return;
            }

            string dest = Path.Combine(Application.dataPath, "Resources/content");
            if (Directory.Exists(dest)) Directory.Delete(dest, true);
            Directory.CreateDirectory(dest);

            int files = 0;
            foreach (string path in Directory.GetFiles(source, "*.json", SearchOption.AllDirectories))
            {
                string rel = path.Substring(source.Length + 1);
                string to = Path.Combine(dest, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(to));
                File.Copy(path, to, true);
                files++;
            }

            AssetDatabase.Refresh();
            Debug.Log(string.Format("{0} content files copied -> {1}", files, Target));
        }

        /// <summary>
        /// Checks that the copy is UP TO DATE. A stale copy makes it look as
        /// though the game's balance has changed, and finding the reason takes
        /// hours - exactly this class of bug has happened twice on this project.
        /// </summary>
        [MenuItem("Lokanta/Is the content copy up to date")]
        public static void Check()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string repo = Path.GetDirectoryName(root);
            string source = Path.Combine(repo, "content");
            string dest = Path.Combine(Application.dataPath, "Resources/content");

            if (!Directory.Exists(dest))
            {
                Debug.LogWarning("The Resources copy has never been created.");
                return;
            }

            int stale = 0, missing = 0;
            foreach (string path in Directory.GetFiles(source, "*.json", SearchOption.AllDirectories))
            {
                string rel = path.Substring(source.Length + 1);
                string to = Path.Combine(dest, rel);
                if (!File.Exists(to)) { missing++; continue; }
                if (File.ReadAllText(to) != File.ReadAllText(path)) stale++;
            }

            if (missing == 0 && stale == 0) Debug.Log("The content copy is up to date.");
            else Debug.LogWarning(string.Format(
                "The content copy is STALE: {0} missing, {1} different. Copy it from the Lokanta menu.",
                missing, stale));
        }
    }
}
