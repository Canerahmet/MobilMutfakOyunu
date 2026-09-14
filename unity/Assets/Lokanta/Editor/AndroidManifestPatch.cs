using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Android bildirimini yapi sirasinda duzeltir.
    ///
    /// Neden elle bir AndroidManifest.xml yazmiyoruz: Unity'nin urettigi
    /// bildirim motorun surumune gore degisiyor ve elle yazilan bir kopya
    /// sessizce eskiyor. Uretilen dosyayi YAMALAMAK, yalnizca bizim
    /// kararimiz olan satirlari degistiriyor.
    ///
    /// Su an tek bir karar var: OTOMATIK YEDEKLEME KAPALI.
    ///
    /// Android'de android:allowBackup varsayilan olarak acik ve bu,
    /// Application.persistentDataPath altindaki kayit dosyalarinin
    /// kullanicinin Google Drive hesabina kopyalanmasi demek. Oyun
    /// tamamen cevrimdisi ve hicbir veri toplamiyor; yedekleme acik
    /// kalirsa "hicbir veri cihazdan cikmiyor" cumlesi teknik olarak
    /// yanlis olur ve Veri Guvenligi formuyla celisir.
    ///
    /// Bu bir KARAR, eksiklik degil: yedeklemeyi acmak da savunulabilir
    /// ("telefon degisince kayitlar gelir"), ama o zaman formda beyan
    /// edilmesi gerekir. Karar vermeden birakmak en kotusu.
    /// </summary>
    public sealed class AndroidManifestPatch : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 1; } }

        private const string Ns = "http://schemas.android.com/apk/res/android";

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // Gradle projesinde birden fazla bildirim var (launcher ve
            // unityLibrary). Uygulama etiketini tasiyan her birini
            // yamiyoruz; hangisinin birlestirmede kazandigi surume gore
            // degisiyor.
            int patched = 0;
            foreach (string file in Directory.GetFiles(
                         Directory.GetParent(path).FullName,
                         "AndroidManifest.xml", SearchOption.AllDirectories))
            {
                if (Patch(file)) patched++;
            }

            // GRADLE BETIGI de yamaniyor: yerel kutuphaneler
            // SIKISTIRILMIS kalsin.
            //
            // Varsayilan paketleme .so dosyalarini kuruluma ACARAK
            // kopyaliyor: 32 MB'lik indirme cihazda 110 MB'i asan bir
            // kuruluma donuyor, cunku 77 MB'lik yuk (yalnizca
            // libil2cpp.so 57 MB) hem paketin icinde hem /data altinda
            // duruyor. Dusuk depolamali cihazlarda kurulum basarisiz
            // oluyor.
            //
            // BuildPlayer bunu yillarca YORUMDA soz verdi ve kodda hic
            // yapmadi; Unity 6'da PlayerSettings.Android.
            // useLegacyPackaging da yok. Tek dogru yer burasi.
            int gradle = 0;
            foreach (string file in Directory.GetFiles(
                         Directory.GetParent(path).FullName,
                         "build.gradle", SearchOption.AllDirectories))
            {
                if (PatchGradle(file)) gradle++;
            }

            Debug.Log(patched + " Android bildirimi yamandi (yedekleme kapali), "
                      + gradle + " gradle betigi (sikistirilmis kutuphane).");
        }

        /// <summary>
        /// Uygulama modulunun build.gradle'ina paketleme ayarini ekler.
        ///
        /// Yalnizca UYGULAMA modulu (com.android.application eklentisi);
        /// kutuphane modullerinde bu ayarin karsiligi yok. Ayri bir
        /// android { } blogu olarak SONA ekleniyor - Gradle ayni
        /// betikteki bloklari birlestiriyor, yani mevcut yapiyi
        /// ayristirmaya gerek kalmiyor.
        /// </summary>
        private static bool PatchGradle(string file)
        {
            // YALNIZCA "launcher" MODULU.
            //
            // Ilk yazimda olcut "dosyada com.android.application geciyor
            // mu" idi ve KOK build.gradle'a dustu - orada o ad eklenti
            // siniflandirmasinda geciyor ama android { } blogu YOK:
            //
            //   Could not find method android() ... on root project
            //
            // Klasor adi hem daha kesin hem daha okunur.
            string dir = Path.GetFileName(Path.GetDirectoryName(file));
            if (dir != "launcher") return false;

            // DEGER DEGISTIRILIYOR, BLOK EKLENMIYOR.
            //
            // Unity zaten "useLegacyPackaging true" yaziyor. Ilk yazimda
            // dosyanin sonuna ikinci bir android { } blogu ekleniyordu
            // ve "zaten yamanmis" korumasi Unity'nin kendi satirini
            // gorup HER SEFERINDE atliyordu: yama sessizce hicbir sey
            // yapmadi ve APK'da kutuphaneler sikistirilmis kaldi.
            //
            // Eski paketleme .so dosyalarini kuruluma ACARAK kopyaliyor:
            // 30,8 MB'lik paket, cihazda 30,8 + 77 = ~108 MB tutuyor.
            // Yenisinde kutuphaneler pakette HAM duruyor ve yerinden
            // okunuyor: paket buyuyor ama kurulum ~77 MB'a iniyor ve
            // hicbir sey kopyalanmiyor. Google minSdk 23+ icin bunu
            // oneriyor; bizim taban 25.
            string text = File.ReadAllText(file);
            const string legacy = "useLegacyPackaging true";
            if (text.IndexOf(legacy, System.StringComparison.Ordinal) < 0) return false;

            File.WriteAllText(file, text.Replace(legacy, "useLegacyPackaging false"));
            return true;
        }

        private static bool Patch(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);

            // KURULUM YERI: DAHILI.
            //
            // Unity varsayilani preferExternal ve kayitlar
            // getExternalFilesDir altina dusuyor - yani oyuncunun altmis
            // gunluk kampanyasi cikarilabilir bir karta yazilabiliyor.
            // Yerel kutuphaneler ayrica sikistirilmamis ve yerinde
            // okunuyor (useLegacyPackaging false); bunlari harici
            // depolamadan okumak da yavas.
            //
            // Unity 6'da PlayerSettings.Android.forceInternalStorage
            // kalkti, karsiligi bu oznitelik.
            XmlNode manifest = doc.SelectSingleNode("/manifest");
            if (manifest != null && manifest.Attributes != null)
                Set(doc, manifest, "installLocation", "internalOnly");

            XmlNode app = doc.SelectSingleNode("/manifest/application");
            if (app == null || app.Attributes == null) return false;

            Set(doc, app, "allowBackup", "false");

            // NOT: extractNativeLibs BURAYA YAZILMIYOR.
            //
            // Denendi ve AGP yapiyi kirdi, ustelik dogru yeri soyleyerek:
            //
            //   android:extractNativeLibs is set to "false" in
            //   AndroidManifest.xml. Avoid setting it explicitly, and
            //   instead set android.packagingOptions.jniLibs.
            //   useLegacyPackaging to false in the build script.
            //
            // Yani ayar bildirimde degil GRADLE BETIGINDE. PatchGradle()
            // onu oraya yaziyor.
            Set(doc, app, "fullBackupContent", "false");
            Set(doc, app, "dataExtractionRules", null);   // varsa kaldir

            doc.Save(file);
            return true;
        }

        private static void Set(XmlDocument doc, XmlNode node, string name, string value)
        {
            XmlAttribute existing = node.Attributes[name, Ns];

            if (value == null)
            {
                if (existing != null) node.Attributes.Remove(existing);
                return;
            }

            if (existing == null)
            {
                existing = doc.CreateAttribute("android", name, Ns);
                node.Attributes.Append(existing);
            }
            existing.Value = value;
        }
    }
}
