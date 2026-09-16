using Lokanta.Content;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Reads the content out of Resources. docs/26: the platform is
    /// behind the port.
    ///
    /// Why Resources and not StreamingAssets: on Android StreamingAssets
    /// sits inside the APK and can only be read with UnityWebRequest,
    /// ASYNCHRONOUSLY. Loading content is a synchronous operation (the
    /// loader validates and throws); making it asynchronous would have
    /// infected the whole loading chain. Resources is synchronous on every
    /// platform.
    ///
    /// The price: the files are embedded in the build and have to be
    /// copied by hand in the Editor - "Copy content into Resources" from
    /// the Lokanta menu. That copy is a build step, not a place to edit.
    /// </summary>
    public sealed class ResourcesContentSource : IContentSource
    {
        /// <summary>The root folder under Resources.</summary>
        public const string Root = "content";

        private static string Key(string rel)
        {
            // Resources wants the path without its extension: "content/dishes/turk"
            if (rel.EndsWith(".json", System.StringComparison.Ordinal))
                rel = rel.Substring(0, rel.Length - 5);
            return Root + "/" + rel;
        }

        public bool Exists(string rel)
        {
            TextAsset a = Resources.Load<TextAsset>(Key(rel));
            if (a == null) return false;
            Resources.UnloadAsset(a);
            return true;
        }

        public string ReadText(string rel)
        {
            TextAsset a = Resources.Load<TextAsset>(Key(rel));
            if (a == null)
                throw new ContentException("not in Resources: " + Key(rel));
            string text = a.text;
            Resources.UnloadAsset(a);
            return text;
        }

        public string Describe(string rel) { return "Resources/" + Key(rel); }
    }
}
