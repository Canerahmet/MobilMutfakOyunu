using Lokanta.Content;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Icerigi Resources'tan okur. docs/26: platform portun arkasinda.
    ///
    /// Neden Resources ve neden StreamingAssets degil: Android'de
    /// StreamingAssets APK'nin icinde ve yalnizca UnityWebRequest ile,
    /// ASENKRON okunabiliyor. Icerik yukleme senkron bir islem (yukleyici
    /// dogrulama yapiyor ve hata firlatiyor); onu asenkron yapmak butun
    /// yukleme zincirini bulastirirdi. Resources her platformda senkron.
    ///
    /// Bedeli: dosyalar derlemeye gomuluyor ve Editor'de elle
    /// kopyalanmasi gerekiyor - Lokanta menusunden "Icerigi Resources'a
    /// kopyala". O kopyalama bir uretim adimi, elle duzenleme yeri degil.
    /// </summary>
    public sealed class ResourcesContentSource : IContentSource
    {
        /// <summary>Resources altindaki kok klasor.</summary>
        public const string Root = "content";

        private static string Key(string rel)
        {
            // Resources yol uzantisiz istiyor: "content/dishes/turk"
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
                throw new ContentException("Resources'ta yok: " + Key(rel));
            string text = a.text;
            Resources.UnloadAsset(a);
            return text;
        }

        public string Describe(string rel) { return "Resources/" + Key(rel); }
    }
}
