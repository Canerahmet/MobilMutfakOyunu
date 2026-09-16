using System;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lokanta.Core;
using Xunit;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// The docs/23-core-contract.md 2.5 reflection test.
    ///
    /// Why: IL2CPP (Android), Mono (the editor) and RyuJIT (the balance harness)
    /// may perform floating point operations in a different order. Integer
    /// addition is the same everywhere. A single float in the core breaks
    /// determinism.
    /// </summary>
    public class NoFloatTests
    {
        private static readonly HashSet<Type> Forbidden = new HashSet<Type>
        {
            typeof(float), typeof(double), typeof(decimal),
            typeof(float?), typeof(double?), typeof(decimal?)
        };

        private static Assembly Core => typeof(Fx).Assembly;

        /// <summary>
        /// The types scanned: the SIMULATION only.
        ///
        /// The same assembly also holds Lokanta.Game.RoomPlan - the floor plan,
        /// in metres. Floating point is RIGHT there: writing an 18.0 x 9.6 m plot
        /// as integers would mean converting the measurement to centimetres and
        /// dividing everywhere, and would have gained nothing.
        ///
        /// The determinism rule belongs to the simulation: the same sequence of
        /// commands must give the same result on every platform (docs/23 2.5).
        /// The floor plan does not enter the result; it says where things stand
        /// on screen. The rule was NOT WIDENED, its SCOPE WAS WRITTEN DOWN -
        /// unless that distinction is written down, one day somebody will loosen
        /// this test in order to slip a float into the simulation.
        /// </summary>
        private static IEnumerable<Type> Scanned()
        {
            foreach (Type t in Core.GetTypes())
            {
                string ns = t.Namespace ?? "";
                if (ns != "Lokanta.Core" && !ns.StartsWith("Lokanta.Core.", StringComparison.Ordinal))
                    continue;
                yield return t;
            }
        }

        private static bool IsForbidden(Type t)
        {
            if (t == null) return false;
            if (t.IsByRef || t.IsPointer || t.IsArray)
                return IsForbidden(t.GetElementType());
            if (t.IsGenericType)
                return t.GetGenericArguments().Any(IsForbidden);
            return Forbidden.Contains(t);
        }

        private const char BackSlash = '\\';
        private static readonly string NL = Environment.NewLine;

        private const BindingFlags All =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        [Fact]
        public void No_field_in_the_core_is_floating_point()
        {
            List<string> bad = new List<string>();
            foreach (Type t in Scanned())
                foreach (FieldInfo f in t.GetFields(All))
                    if (IsForbidden(f.FieldType))
                        bad.Add($"{t.FullName}.{f.Name} : {f.FieldType.Name}");

            Assert.True(bad.Count == 0, "Floating point field(s):\n" + string.Join("\n", bad));
        }

        [Fact]
        public void No_property_in_the_core_is_floating_point()
        {
            List<string> bad = new List<string>();
            foreach (Type t in Scanned())
                foreach (PropertyInfo p in t.GetProperties(All))
                    if (IsForbidden(p.PropertyType))
                        bad.Add($"{t.FullName}.{p.Name} : {p.PropertyType.Name}");

            Assert.True(bad.Count == 0, "Floating point propert(ies):\n" + string.Join("\n", bad));
        }

        [Fact]
        public void No_method_in_the_core_takes_or_returns_floating_point()
        {
            List<string> bad = new List<string>();
            foreach (Type t in Scanned())
            {
                foreach (MethodInfo m in t.GetMethods(All))
                {
                    if (IsForbidden(m.ReturnType))
                        bad.Add($"{t.FullName}.{m.Name} -> {m.ReturnType.Name}");
                    foreach (ParameterInfo p in m.GetParameters())
                        if (IsForbidden(p.ParameterType))
                            bad.Add($"{t.FullName}.{m.Name}({p.Name} : {p.ParameterType.Name})");
                }
                foreach (ConstructorInfo c in t.GetConstructors(All))
                    foreach (ParameterInfo p in c.GetParameters())
                        if (IsForbidden(p.ParameterType))
                            bad.Add($"{t.FullName}..ctor({p.Name} : {p.ParameterType.Name})");
            }

            Assert.True(bad.Count == 0, "Floating point signature(s):\n" + string.Join("\n", bad));
        }

        /// <summary>
        /// THE SOURCE TEXT SCAN - the place reflection CANNOT SEE.
        ///
        /// The three tests above look only at FIELDS, PROPERTIES and SIGNATURES.
        /// A local variable in a method BODY passes all of them:
        ///
        ///     private void X() { double k = a / (double)b; ... }
        ///
        /// And yet that is exactly what breaks determinism - an intermediate
        /// calculation that rounds differently from platform to platform leaves
        /// no trace at all in a signature. The guard was NARROWER than it looked.
        ///
        /// Comments and string literals are stripped out: the word "double" can
        /// appear in an explanation, and it should be allowed to.
        /// </summary>
        [Fact]
        public void No_floating_point_in_the_core_source()
        {
            string root = Path.Combine(Paths.Root, "unity", "Assets", "Lokanta", "Core");
            Assert.True(Directory.Exists(root), "Core source tree not found: " + root);

            // On a word boundary: do not catch "Doubled" or "floating".
            Regex banned = new Regex(@"\b(float|double|decimal)\b");
            List<string> bad = new List<string>();

            string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);

            // DID THE SCAN ACTUALLY HAPPEN.
            //
            // If the path were wrong, or if the tree moved, the loop would never
            // turn and the test would stay GREEN saying "no violations" - the
            // class of bug this project catches most often. The number is kept
            // below today's 14 so that deleting a file does not break the test,
            // but zero files must never pass.
            Assert.True(files.Length >= 8,
                $"only {files.Length} source files were scanned in the core - "
                + "the path may be wrong: " + root);

            foreach (string file in files)
            {
                string[] lines = File.ReadAllLines(file);
                bool inBlockComment = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = Strip(lines[i], ref inBlockComment);
                    if (line.Length == 0) continue;
                    if (!banned.IsMatch(line)) continue;
                    bad.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
                }
            }

            Assert.True(bad.Count == 0,
                "Floating point in the core source:" + NL + string.Join(NL, bad));
        }

        /// <summary>Throws the comments and string literals out of a line.</summary>
        private static string Strip(string line, ref bool inBlockComment)
        {
            StringBuilder sb = new StringBuilder();
            bool inString = false;
            for (int i = 0; i < line.Length; i++)
            {
                if (inBlockComment)
                {
                    if (i + 1 < line.Length && line[i] == '*' && line[i + 1] == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }
                    continue;
                }
                if (inString)
                {
                    if (line[i] == BackSlash) { i++; continue; }
                    if (line[i] == '"') inString = false;
                    continue;
                }
                if (line[i] == '"') { inString = true; continue; }
                if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/') break;
                if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '*')
                {
                    inBlockComment = true;
                    i++;
                    continue;
                }
                sb.Append(line[i]);
            }
            return sb.ToString();
        }

        [Fact]
        public void The_core_carries_no_unity_or_json_reference()
        {
            string[] banned = { "UnityEngine", "Newtonsoft", "System.Text.Json" };
            List<string> bad = new List<string>();

            foreach (AssemblyName an in Core.GetReferencedAssemblies())
                foreach (string b in banned)
                    if (an.Name.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0)
                        bad.Add(an.Name);

            Assert.True(bad.Count == 0, "Banned reference(s): " + string.Join(", ", bad));
        }
    }
}
