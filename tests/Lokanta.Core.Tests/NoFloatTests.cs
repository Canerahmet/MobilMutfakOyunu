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
    /// docs/23-core-contract.md 2.5 yansima testi.
    ///
    /// Neden: IL2CPP (Android), Mono (editor) ve RyuJIT (denge araci) kayan
    /// nokta islemlerini farkli sirayla yapabilir. Tamsayi toplama her yerde ayni.
    /// Cekirdekte tek bir float bile determinizmi bozar.
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
        /// Taranan tipler: yalnizca SIMULASYON.
        ///
        /// Ayni derlemede Lokanta.Game.RoomPlan da var - kat plani, metre
        /// cinsinden. Orada kayan nokta DOGRU: 18,0 x 9,6 m bir arsayi
        /// tamsayiyla yazmak, olcuyu santimetreye cevirip her yerde
        /// bolmek demekti ve hicbir sey kazandirmazdi.
        ///
        /// Determinizm kurali simulasyona ait: ayni komut dizisi her
        /// platformda ayni sonucu vermeli (docs/23 2.5). Kat plani sonuca
        /// girmiyor; ekranda nerede durdugunu soyluyor. Kural GENISLETILMEDI,
        /// KAPSAMI YAZILDI - bu ayrim yazilmadigi surece bir gun birisi
        /// simulasyona float sokmak icin bu testi gevsetir.
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
        public void Cekirdekte_hicbir_alan_kayan_noktali_degil()
        {
            List<string> bad = new List<string>();
            foreach (Type t in Scanned())
                foreach (FieldInfo f in t.GetFields(All))
                    if (IsForbidden(f.FieldType))
                        bad.Add($"{t.FullName}.{f.Name} : {f.FieldType.Name}");

            Assert.True(bad.Count == 0, "Kayan noktali alan(lar):\n" + string.Join("\n", bad));
        }

        [Fact]
        public void Cekirdekte_hicbir_ozellik_kayan_noktali_degil()
        {
            List<string> bad = new List<string>();
            foreach (Type t in Scanned())
                foreach (PropertyInfo p in t.GetProperties(All))
                    if (IsForbidden(p.PropertyType))
                        bad.Add($"{t.FullName}.{p.Name} : {p.PropertyType.Name}");

            Assert.True(bad.Count == 0, "Kayan noktali ozellik(ler):\n" + string.Join("\n", bad));
        }

        [Fact]
        public void Cekirdekte_hicbir_metot_kayan_nokta_alip_vermez()
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

            Assert.True(bad.Count == 0, "Kayan noktali imza(lar):\n" + string.Join("\n", bad));
        }

        /// <summary>
        /// KAYNAK METIN TARAMASI - yansimanin GOREMEDIGI yer.
        ///
        /// Ustteki uc test yalnizca ALAN, OZELLIK ve IMZA bakiyor.
        /// Metot GOVDESINDEKI bir yerel degisken hepsinden gecer:
        ///
        ///     private void X() { double k = a / (double)b; ... }
        ///
        /// Oysa determinizmi bozan tam olarak odur - platformdan
        /// platforma farkli yuvarlanan bir ara hesap, imzada hicbir iz
        /// birakmiyor. Koruma sanildigindan DARDI.
        ///
        /// Yorumlar ve metin sabitleri ayiklanıyor: "double" kelimesi
        /// bir aciklamada gecebilir ve gecmeli.
        /// </summary>
        [Fact]
        public void Cekirdek_kaynaginda_kayan_nokta_yok()
        {
            string kok = Path.Combine(Paths.Root, "unity", "Assets", "Lokanta", "Core");
            Assert.True(Directory.Exists(kok), "Cekirdek kaynak agaci bulunamadi: " + kok);

            // Kelime sinirinda: "Doubled" ya da "floating" yakalanmasin.
            Regex yasak = new Regex(@"\b(float|double|decimal)\b");
            List<string> bad = new List<string>();

            string[] dosyalar = Directory.GetFiles(kok, "*.cs", SearchOption.AllDirectories);

            // TARAMA GERCEKTEN OLDU MU.
            //
            // Yol yanlis olsa ya da agac tasinsa dongu hic donmez ve
            // test "hicbir ihlal yok" diye YESIL kalirdi - bu projenin
            // en sik yakaladigi hata sinifi. Sayi bugunku 14'un altinda
            // tutuldu ki dosya silinmesi testi kirmasin, ama sifir
            // dosya asla gecmesin.
            Assert.True(dosyalar.Length >= 8,
                $"cekirdekte yalnizca {dosyalar.Length} kaynak dosya tarandi - "
                + "yol yanlis olabilir: " + kok);

            foreach (string dosya in dosyalar)
            {
                string[] satirlar = File.ReadAllLines(dosya);
                bool blokYorum = false;
                for (int i = 0; i < satirlar.Length; i++)
                {
                    string satir = Temizle(satirlar[i], ref blokYorum);
                    if (satir.Length == 0) continue;
                    if (!yasak.IsMatch(satir)) continue;
                    bad.Add($"{Path.GetFileName(dosya)}:{i + 1}: {satirlar[i].Trim()}");
                }
            }

            Assert.True(bad.Count == 0,
                "Cekirdek kaynaginda kayan nokta:" + NL + string.Join(NL, bad));
        }

        /// <summary>Yorumlari ve metin sabitlerini satirdan atar.</summary>
        private static string Temizle(string satir, ref bool blokYorum)
        {
            StringBuilder sb = new StringBuilder();
            bool metin = false;
            for (int i = 0; i < satir.Length; i++)
            {
                if (blokYorum)
                {
                    if (i + 1 < satir.Length && satir[i] == '*' && satir[i + 1] == '/')
                    {
                        blokYorum = false;
                        i++;
                    }
                    continue;
                }
                if (metin)
                {
                    if (satir[i] == BackSlash) { i++; continue; }
                    if (satir[i] == '"') metin = false;
                    continue;
                }
                if (satir[i] == '"') { metin = true; continue; }
                if (i + 1 < satir.Length && satir[i] == '/' && satir[i + 1] == '/') break;
                if (i + 1 < satir.Length && satir[i] == '/' && satir[i + 1] == '*')
                {
                    blokYorum = true;
                    i++;
                    continue;
                }
                sb.Append(satir[i]);
            }
            return sb.ToString();
        }

        [Fact]
        public void Cekirdek_unity_veya_json_referansi_tasimaz()
        {
            string[] banned = { "UnityEngine", "Newtonsoft", "System.Text.Json" };
            List<string> bad = new List<string>();

            foreach (AssemblyName an in Core.GetReferencedAssemblies())
                foreach (string b in banned)
                    if (an.Name.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0)
                        bad.Add(an.Name);

            Assert.True(bad.Count == 0, "Yasak referans(lar): " + string.Join(", ", bad));
        }
    }
}
