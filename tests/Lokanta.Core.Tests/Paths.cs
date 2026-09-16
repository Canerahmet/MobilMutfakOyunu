using System;
using System.IO;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// Finds the repository root. The test assembly runs under bin/.
    /// .NET 10 produces the solution file as .slnx; both are accepted.
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
                "Repository root not found (" + string.Join(" or ", Markers)
                + "), starting from: " + AppContext.BaseDirectory);
        }
    }
}
