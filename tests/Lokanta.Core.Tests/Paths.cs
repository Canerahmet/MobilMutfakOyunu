using System;
using System.IO;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// Depo kokunu bulur. Test derlemesi bin/ altinda calisiyor.
    /// .NET 10 cozum dosyasini .slnx olarak uretiyor; ikisi de kabul edilir.
    /// </summary>
    public static class Paths
    {
        private static readonly Lazy<string> _root = new Lazy<string>(FindRoot);

        private static readonly string[] Markers = { "Lokanta.sln", "Lokanta.slnx" };

        public static string Root => _root.Value;
        public static string Content => Path.Combine(Root, "content");
        public static string Golden => Path.Combine(Root, "tests", "golden");

        private static string FindRoot()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                foreach (string marker in Markers)
                    if (File.Exists(Path.Combine(d.FullName, marker)))
                        return d.FullName;
                d = d.Parent;
            }
            throw new InvalidOperationException(
                "Depo koku bulunamadi (" + string.Join(" veya ", Markers)
                + "), baslangic: " + AppContext.BaseDirectory);
        }
    }
}
