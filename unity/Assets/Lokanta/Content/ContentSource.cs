using System;
using System.IO;

namespace Lokanta.Content
{
    /// <summary>
    /// WHERE we read content text from. The docs/26 architecture rule
    /// "the platform lives behind a port".
    ///
    /// The reason is concrete: on the desktop and in the tests, content is
    /// a folder on disk; on Android it is inside the APK and there is no
    /// such thing as a file path. The loader must not know the difference -
    /// it has to run the SAME validations on both sides, otherwise we only
    /// find out the game is broken once it is opened on a phone.
    ///
    /// Paths ALWAYS use forward slashes and are relative to the content root:
    ///     "economy.json", "dishes/turk.json", "archetypes/shared.json"
    /// </summary>
    public interface IContentSource
    {
        bool Exists(string relativePath);
        string ReadText(string relativePath);
        /// <summary>The source name to show in error messages.</summary>
        string Describe(string relativePath);
    }

    /// <summary>A folder on disk. Tests, the balance tool and the editor.</summary>
    public sealed class DirectoryContentSource : IContentSource
    {
        private readonly string _root;

        public DirectoryContentSource(string root)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentNullException(nameof(root));
            _root = root;
        }

        private string Full(string rel)
        {
            return Path.Combine(_root, rel.Replace('/', Path.DirectorySeparatorChar));
        }

        public bool Exists(string rel) { return File.Exists(Full(rel)); }
        public string ReadText(string rel) { return File.ReadAllText(Full(rel)); }
        public string Describe(string rel) { return Full(rel); }
    }
}
