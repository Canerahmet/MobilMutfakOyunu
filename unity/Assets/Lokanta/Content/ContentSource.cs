using System;
using System.IO;

namespace Lokanta.Content
{
    /// <summary>
    /// Icerik metnini NEREDEN okudugumuz. docs/26 mimarisinin "platform
    /// portun arkasinda" kurali.
    ///
    /// Sebebi somut: masaustunde ve testlerde icerik diskte bir klasor;
    /// Android'de APK'nin icinde ve dosya yolu diye bir sey yok. Yukleyici
    /// bunu bilmemeli - iki tarafta da AYNI dogrulamalari calistirmali,
    /// yoksa oyun ancak telefonda acilinca bozuldugunu ogreniriz.
    ///
    /// Yollar HER ZAMAN egik cizgiyle ve icerik kokune gore:
    ///     "economy.json", "dishes/turk.json", "archetypes/shared.json"
    /// </summary>
    public interface IContentSource
    {
        bool Exists(string relativePath);
        string ReadText(string relativePath);
        /// <summary>Hata mesajlarinda gorunecek kaynak adi.</summary>
        string Describe(string relativePath);
    }

    /// <summary>Diskteki bir klasor. Testler, denge araci ve editor.</summary>
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
