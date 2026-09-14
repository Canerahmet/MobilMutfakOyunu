using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// content/ klasorunu Assets/Resources/content/ altina kopyalar.
    ///
    /// Neden kopya: icerik projenin kokunde uretiliyor (tools/balance,
    /// tools/content) ve orasi Unity'nin gormedigi bir yer. Unity'nin
    /// gordugu tek senkron kaynak Resources.
    ///
    /// Neden .json degil .txt DEGIL: Unity .json uzantisini zaten TextAsset
    /// olarak iceri aliyor, yani yeniden adlandirmaya gerek yok.
    ///
    /// KOPYA URETILEN BIR SEY. Assets/Resources/content altinda elle
    /// duzenleme yapilmamali; her calistirmada silinip yeniden yaziliyor.
    /// </summary>
    public static class SyncContent
    {
        private const string Target = "Assets/Resources/content";

        [MenuItem("Lokanta/Icerigi Resources'a kopyala")]
        public static void Run()
        {
            string root = Path.GetDirectoryName(Application.dataPath);      // unity/
            string repo = Path.GetDirectoryName(root);                      // proje koku
            string source = Path.Combine(repo, "content");

            if (!Directory.Exists(source))
            {
                Debug.LogError("Icerik klasoru yok: " + source);
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
            Debug.Log(string.Format("{0} icerik dosyasi kopyalandi -> {1}", files, Target));
        }

        /// <summary>
        /// Kopyanin GUNCEL oldugunu dogrular. Bayat bir kopya, oyunun
        /// dengesi degismis gibi gorunmesine yol acar ve sebebini bulmak
        /// saatler alir - bu projede tam olarak bu sinif hata iki kez oldu.
        /// </summary>
        [MenuItem("Lokanta/Icerik kopyasi guncel mi")]
        public static void Check()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string repo = Path.GetDirectoryName(root);
            string source = Path.Combine(repo, "content");
            string dest = Path.Combine(Application.dataPath, "Resources/content");

            if (!Directory.Exists(dest))
            {
                Debug.LogWarning("Resources kopyasi hic olusturulmamis.");
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

            if (missing == 0 && stale == 0) Debug.Log("Icerik kopyasi guncel.");
            else Debug.LogWarning(string.Format(
                "Icerik kopyasi BAYAT: {0} eksik, {1} farkli. Lokanta menusunden kopyala.",
                missing, stale));
        }
    }
}
