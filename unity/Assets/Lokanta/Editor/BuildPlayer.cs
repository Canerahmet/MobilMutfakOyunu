using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Oynanabilir yapi alir.
    ///
    /// Iki hedef var ve ikisi de gerekli:
    ///
    ///   Windows - DOGRULAMA icin. Cihaz yok, editorde play mode toplu
    ///   kipte takiliyor; oyunu gercekten calistirmanin tek yolu bir
    ///   masaustu yapisi. Autopilot ile birlikte arayuzu, sesi, kaydi ve
    ///   simulasyonu tek kosuda dogruluyor.
    ///
    ///   Android - GERCEK hedef. Ayarlari burada yaziyor ki bir dahaki
    ///   sefere "paket adi neydi" diye aranmasin.
    ///
    ///   .\tools\unity\run.ps1 -Method Lokanta.EditorTools.BuildPlayer.Windows
    ///   .\tools\unity\run.ps1 -Method Lokanta.EditorTools.BuildPlayer.Android
    /// </summary>
    public static class BuildPlayer
    {
        // =====================================================================
        // Yapi PARAMETRELERI: komut satiri ve ortam degiskenleri.
        //
        // Surum kodu ve imzalama, betige gomulemeyecek iki seydir:
        // birincisi her yayinda artmali, ikincisi depoya girmemeli.
        // =====================================================================

        /// <summary>Ortam degiskeni; yoksa bos dize.</summary>
        private static string Env(string name)
        {
            string v = System.Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(v) ? string.Empty : v.Trim();
        }

        /// <summary>Komut satiri bayraginin degeri; yoksa varsayilan.</summary>
        private static string Arg(string flag, string fallback)
        {
            string[] a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (a[i] == flag) return a[i + 1];
            return fallback;
        }

        private static int ArgInt(string flag, int fallback)
        {
            int v;
            return int.TryParse(Arg(flag, string.Empty), out v) ? v : fallback;
        }

        private const string Scene = "Assets/Lokanta/Oyun.unity";
        /// <summary>
        /// Paket adi. ILK YUKLEMEDE SONSUZA KADAR KILITLENIYOR.
        ///
        /// Tek kaynak: ProjectSetup da buradan okuyor. Ikisi ayri
        /// yazdiginda hangisinin son kostugu gorunmuyordu ve yanlis bir
        /// paket adiyla tek bir yukleme geri alinamaz.
        /// </summary>
        public const string Package = "com.ahmetakar.lokanta";

        /// <summary>Sirket adi. Tek kaynak; ProjectSetup buradan okuyor.</summary>
        public const string Company = "Ahmet Akar";

        /// <summary>Urun adi. Tek kaynak; ProjectSetup buradan okuyor.</summary>
        public const string Product = "Lokanta";

        // =====================================================================
        [MenuItem("Lokanta/Yapi - Windows")]
        public static void Windows() { WindowsBuild("windows", il2cpp: false); }

        /// <summary>
        /// BUDAMA SINAVI. Windows, ama Android'in derleyicisi ve
        /// budayicisiyla: IL2CPP + ManagedStrippingLevel.High.
        ///
        /// Neden var: link.xml'in kendi yorumu "bu dosyanin korudugu
        /// hata, projedeki HICBIR testin ulasamadigi tek yapilandirmada
        /// yasiyor" diyordu - yani koruma akil yurutmeyle yazilmis,
        /// hic KOSULMAMISTI. APK'yi kosturacak cihaz yokken de budayici
        /// burada kosuyor: ayni link.xml, ayni Lokanta.Content ve
        /// Newtonsoft derlemeleri, ayni yansimayla icerik yukleme.
        ///
        /// Koruma yanlis olsaydi belirti cokme degil SESSIZ VARSAYILAN
        /// olurdu; oyun acilis dogrulamasinda hata ekranina duser ve tur
        /// ilk kontrolde kalir. Yani turun 129 kontrolu bu yapilandirmayi
        /// da olcebiliyor.
        ///
        /// Android'in birebir ayni olmadigi yer: motor modullerinin
        /// budanmasi platforma gore degisiyor. Ayni olan ve onemli olan
        /// yer: kendi derlemelerimizin YONETILEN budamasi.
        ///
        ///   .\tools\unity\tur.ps1 -Yapi windows-il2cpp
        /// </summary>
        [MenuItem("Lokanta/Yapi - Windows (IL2CPP + budama)")]
        public static void WindowsIl2cpp() { WindowsBuild("windows-il2cpp", il2cpp: true); }

        private static void WindowsBuild(string leaf, bool il2cpp)
        {
            Common();

            // AYAR GERI ALINIYOR. IL2CPP yapisi ~15 dakika, Mono ~1;
            // ayar projede kalirsa her tur kosusu on bes kat yavaslar ve
            // bunu kimse fark etmeden aylarca odeyebilir.
            ScriptingImplementation oncekiArka =
                PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            ManagedStrippingLevel oncekiBudama =
                PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Standalone);

            if (il2cpp)
            {
                PlayerSettings.SetScriptingBackend(
                    NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetManagedStrippingLevel(
                    NamedBuildTarget.Standalone, ManagedStrippingLevel.High);
            }

            try { WindowsCore(leaf); }
            finally
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, oncekiArka);
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, oncekiBudama);
            }
        }

        private static void WindowsCore(string leaf)
        {
            string dir = Out(leaf);
            BuildPlayerOptions o = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Path.Combine(dir, "Lokanta.exe"),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            // Pencereli ve sabit: tam ekran bir yapi, goruntu alirken
            // ekran cozunurlugune baglanir ve her makinede baska bir
            // kareyi cizer.
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;

            Report(BuildPipeline.BuildPlayer(o), leaf);
        }

        // =====================================================================
        /// <summary>Cihaza atilacak APK. Test icin.</summary>
        [MenuItem("Lokanta/Yapi - Android APK")]
        public static void Android() { AndroidBuild(bundle: false); }

        /// <summary>
        /// Magazaya yuklenecek AAB. Play Store 2021'den beri yalnizca
        /// bunu kabul ediyor.
        /// </summary>
        [MenuItem("Lokanta/Yapi - Android AAB")]
        public static void AndroidBundle() { AndroidBuild(bundle: true); }

        private static void AndroidBuild(bool bundle)
        {
            Common();

            // ACIKCA yaziliyor. Once yazilmiyordu ve Unity projede duran
            // ayari kullandi: dosyanin adi Lokanta.apk'ydi ama ICI bir
            // uygulama paketiydi (base/, BUNDLE-METADATA/). Cihaza
            // atilamayan, adi yanlis bir dosya.
            EditorUserBuildSettings.buildAppBundle = bundle;

            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, Package);

            // Dikey DEGIL yatay: kat plani 18,0 x 9,6 m, yani genis bir
            // dikdortgen. Dikey bir telefonda restoranin yarisi kare disi
            // kalirdi (docs/31 cerceveleme olcumu).
            // AUTOROTATION, LandscapeLeft DEGIL.
            //
            // Sabit bir yon Unity'ye manifestte
            // android:screenOrientation="landscape" yazdiriyor ve
            // allowedAutorotateToLandscapeRight bayragini YOK SAYIYOR -
            // Unity o bayraklara yalnizca varsayilan AutoRotation iken
            // bakiyor. Sonuc: telefonu ters cevirince oyun bas asagi
            // duruyor ve duzeltmenin bir yolu yok.
            //
            // AutoRotation + yalnizca yatay bayraklar, manifeste
            // sensorLandscape yaziyor: iki yatay yon serbest, dikey
            // kapali.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // ARKA PLANDA CALISMA KAPALI.
            //
            // Windows yolu (goruntu alma ve otomatik tur icin) bunu true
            // yapiyor ve deger PROJE AYARINA yaziliyor, yani Android
            // yapisi onu devraliyordu. Muzik OnAudioFilterRead ile
            // surekli uretiliyor: telefon calarken oyun calmaya ve 30
            // fps cizmeye devam ediyordu.
            PlayerSettings.runInBackground = false;

            // Kayitlar DAHILI depolamada.
            //
            // Unity 6'da PlayerSettings.Android.forceInternalStorage
            // KALKTI; karsiligi manifestteki installLocation ve onu
            // AndroidManifestPatch yaziyor. Varsayilan preferExternal,
            // kaydin cikarilabilir bir karta dusmesine izin veriyor.
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // API 29 (Android 10): BELGELERIN YAZDIGI TABAN.
            //
            // Once 25 (Android 7.1) idi ve docs/19'un desteklenen cihaz
            // tablosu "Android 10, 3 GB RAM, Vulkan veya OpenGL ES 3.1"
            // diyor. Yani yapi, oyunu kosturamayacak cihazlara kurulmaya
            // izin veriyordu: 2017 model, 1 GB, GLES 3.0 bir telefon.
            // O telefonun birakacagi yorum listeyi bozar ve geri
            // alinamaz.
            //
            // Bu yeni bir karar degil, YAZILI karari koda gecirmek -
            // zaten bu satirin kendi eski gerekcesi de "ayarin yazdigi
            // sey ile yapinin urettigi sey ayni olmali" idi.
            //
            // Yan fayda: uyarlanabilir simge API 26'dan itibaren
            // yeterli, yani eski ve yuvarlak simge yuvalari da
            // gereksizlesiyor (Unity ikisini de kullanimdan kaldirdigini
            // soyleyip uyari basiyordu).
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            // Hedef SDK ACIKCA yaziliyor, "kurulu olan neyse o" DEGIL.
            //
            // AndroidApiLevelAuto, yapinin hedefini makinede kurulu SDK'ya
            // baglıyor: baska bir makinede baska bir sayi cikiyor ve
            // magazanin esigi degistiginde bunu kimse fark etmiyor.
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;

            // IL2CPP + ARM64: Play Store 64 bit sart kosuyor.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // SURUM KOMUT SATIRINDAN.
            //
            // Ikisi de sabitti: betik her kostugunda versionCode 1
            // uretiyordu ve Play ikinci yuklemeyi "Version code 1 has
            // already been used" diye reddeder. Bir kez kullanilan sayi
            // bir daha kullanilamiyor, yani bu sabit yayin sonrasi
            // duzeltilemez bir hata sinifi.
            //
            //   -lokanta-surum-kodu 3  -lokanta-surum 0.3.0
            PlayerSettings.Android.bundleVersionCode = ArgInt("-lokanta-surum-kodu", 1);
            PlayerSettings.bundleVersion = Arg("-lokanta-surum", "0.1.0");

            // Yerel kutuphaneler SIKISTIRILMIS kalsin.
            //
            // Varsayilan "eski paketleme" onlari kuruluma acarak
            // kopyaliyor: 32 MB'lik indirme cihazda 110 MB'i asan bir
            // kuruluma donuyor cunku 77 MB'lik .so yuku (yalnizca
            // libil2cpp.so 57 MB) hem paketin icinde hem /data altinda
            // duruyor. Dusuk depolamali cihazlarda kurulum basarisiz
            // oluyor.
            //
            // BU YORUM UZUN SURE BIR SOZ VERIYOR VE KOD ONU TUTMUYORDU:
            // asagidaki iki satir sembol ve kare hizi ayari yapiyor,
            // paketleme ayarini HIC yazmiyordu. Uretilmis paketin
            // bildirimi cozuldugunde extractNativeLibs="true" cikti,
            // yani anlatilan sorun aynen duruyordu.
            //
            // Unity 6'da PlayerSettings.Android.useLegacyPackaging YOK
            // (derleyici reddediyor). Ayar artik Gradle tarafinda ve
            // AndroidManifestPatch onu hem bildirime hem build.gradle'a
            // yaziyor - orasi yamanin ZATEN calistigi yer.
            // YEREL SEMBOLLER URETILIYOR.
            //
            // IL2CPP yapisinda cokmeler libil2cpp.so icinde olur; sembol
            // olmadan Play Console'daki yiginlar ciplak adres gosterir.
            // Android vitals cokme orani bir MAGAZA GORUNURLUK esigi -
            // esigi asan uygulama one cikarmadan duser, ve sembolsuz
            // gelen raporla hata duzeltilemez. Baska kanal da yok
            // (cokme raporu API'si kapali).
            //
            // Sembol dosyasi PAKETE GIRMIYOR, ayrica yukleniyor - yani
            // boyut gerekcesi yok.
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
            PlayerSettings.Android.optimizedFramePacing = true;

            // Yonetilen kod AYIKLANIYOR: kullanilmayan tur ve metotlar
            // IL2CPP ciktisindan cikiyor.
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Android, ManagedStrippingLevel.High);
            PlayerSettings.Android.forceSDCardPermission = false;

            // IMZALAMA ORTAM DEGISKENINDEN.
            //
            // Once kosulsuz false'ti, yani her yapi HATA AYIKLAMA
            // anahtariyla imzalaniyordu (sertifika: CN=Android Debug).
            // Play boyle bir paketi yukleme aninda reddediyor.
            //
            // Parola koda ya da depoya GIRMIYOR: dort degisken de
            // ortamdan okunuyor. Hicbiri yoksa eski davranis suruyor -
            // yerel denemeler icin imzasiz yapi almak hala mumkun.
            //
            //   LOKANTA_KEYSTORE       anahtar deposu dosyasi
            //   LOKANTA_KEYSTORE_PASS  deposunun parolasi
            //   LOKANTA_KEYALIAS       anahtar takma adi
            //   LOKANTA_KEYALIAS_PASS  takma adin parolasi
            //
            // Anahtar dosyasi KAYBEDILIRSE uygulama bir daha
            // guncellenemez; iki ayri yerde yedeklenmeli.
            string store = Env("LOKANTA_KEYSTORE");
            string storePass = Env("LOKANTA_KEYSTORE_PASS");
            string alias = Env("LOKANTA_KEYALIAS");
            string aliasPass = Env("LOKANTA_KEYALIAS_PASS");

            bool signed = store.Length > 0 && storePass.Length > 0
                          && alias.Length > 0 && aliasPass.Length > 0;
            PlayerSettings.Android.useCustomKeystore = signed;
            if (signed)
            {
                PlayerSettings.Android.keystoreName = store;
                PlayerSettings.Android.keystorePass = storePass;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = aliasPass;
                Debug.Log("  imza  : yukleme anahtari (" + alias + ")");
            }
            else
            {
                Debug.LogWarning("  imza  : HATA AYIKLAMA anahtari - "
                                 + "Play bu paketi REDDEDER. "
                                 + "LOKANTA_KEYSTORE ve digerlerini ayarla.");

                // UYGULAMA PAKETI IMZASIZ URETILMEZ.
                //
                // Uyarip devam etmek, hata ayiklama anahtariyla imzali
                // bir .aab uretiyordu; Play Console onu YUKLEME ANINDA
                // reddediyor ve bu ancak magazaya cikarken fark
                // ediliyor. Dahasi: ilk yuklenen anahtar KALICIDIR.
                //
                // APK'da uyari yeterli (cihaza atip denemek icin
                // hata ayiklama imzasi yeterli); AAB yayin icindir.
                if (bundle)
                    throw new BuildFailedException(
                        "Yayin paketi (AAB) imzasiz uretilemez. "
                        + "LOKANTA_KEYSTORE, LOKANTA_KEYSTORE_PASS, "
                        + "LOKANTA_KEYALIAS, LOKANTA_KEYALIAS_PASS ayarla. "
                        + "Anahtar dosyasi kaybedilirse uygulama bir daha "
                        + "guncellenemez - iki ayri yerde yedekle.");
            }

            string dir = Out("android");
            BuildPlayerOptions o = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Path.Combine(
                    dir, bundle ? "Lokanta.aab" : "Lokanta.apk"),
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            Report(BuildPipeline.BuildPlayer(o), bundle ? "Android AAB" : "Android APK");
            DropBurstDebug(dir);
        }

        // =====================================================================
        /// <summary>Iki hedefte de ayni olan ayarlar.</summary>
        private static void Common()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;

            // ICERIK SENKRONU YAPININ PARCASI.
            //
            // Oyun icerigi content/ altinda uretiliyor ve Unity'nin
            // gormesi icin Assets/Resources/content/ altina kopyalaniyor.
            // Kopyalama ELLE bir menu ogesiydi; bayat bir kopyayla
            // alinan yapi, TEST EDILENDEN FARKLI denge sayilariyla
            // magazaya gider ve hicbir test kirilmaz. SyncContent'in
            // kendi yorumu "bu projede tam bu sinif hata iki kez oldu"
            // diyor - ucuncusu yapinin kendisinde engelleniyor.
            SyncContent.Run();

            // ACILIS EKRANI KAPALI.
            //
            // Logo yok (m_SplashScreenLogos bos) ama ekran acikti: oyuncu
            // acilista ~2 saniye BOMBOS koyu bir ekran goruyordu. Unity
            // 6'da kapatmak her lisans katmaninda serbest.
            PlayerSettings.SplashScreen.show = false;

            // Renk uzayi DOGRUSAL. Gamma'da dusuk poligonlu duz renkler
            // yikanmis gorunuyor ve golgeler sertlesiyor; URP zaten
            // dogrusali varsayiyor.
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // vSync BURADA AYARLANMIYOR, CALISMA ZAMANINDA ayarlaniyor.
            //
            // Burada "= 1" yaziyordu ve ProjectSetup.cs'in "kare hizini
            // kod ayarlar" karariyla (vSyncCount = 0) dogrudan
            // celisiyordu. Ustelik ikisi de yanlis yere yaziyordu:
            // ProjectSetup editorde AKTIF olan kalite seviyesine (Ultra)
            // yaziyor, Android ise seviye 2'yi (Medium) kullaniyor.
            //
            // Sonucu somut: vSyncCount != 0 iken Android
            // Application.targetFrameRate'i YOK SAYIYOR. 90 ya da 120 Hz
            // ucuz bir telefonda oyun 120 fps hedefliyordu - hedef 30
            // fps tabaniyken. Statik bir sahneye bakan yonetim oyununda
            // bu, iki kat CPU/GPU ve iki kat isi demek.
            //
            // Dogru yer GameApp.Awake: cihazda, calisirken, ve hangi
            // kalite seviyesinin aktif oldugundan bagimsiz.

            if (!File.Exists(Scene))
                Debug.LogWarning("Sahne yok: " + Scene
                                 + "  (Lokanta > Oyun sahnesini kur)");
        }

        private static string Out(string leaf)
        {
            string root = Path.GetDirectoryName(
                Path.GetDirectoryName(Application.dataPath));
            string dir = Path.Combine(root, "build", leaf);
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Burst'un biraktigi hata ayiklama klasoru. Adinda "DoNotShip"
        /// yaziyor; silinmezse yanlislikla dagitima girebiliyor.
        /// </summary>
        private static void DropBurstDebug(string dir)
        {
            foreach (string d in Directory.GetDirectories(dir))
                if (d.EndsWith("_BurstDebugInformation_DoNotShip"))
                {
                    Directory.Delete(d, true);
                    Debug.Log("  Burst hata ayiklama klasoru silindi.");
                }
        }

        private static void Report(BuildReport r, string what)
        {
            BuildSummary s = r.summary;
            if (s.result != BuildResult.Succeeded)
            {
                Debug.LogError(string.Format(
                    "SORUNLAR: {0} yapisi basarisiz - {1} hata, {2} uyari",
                    what, s.totalErrors, s.totalWarnings));
                return;
            }

            // Boyut DOSYADAN okunuyor, rapordan degil.
            //
            // BuildSummary.totalSize, Android'de sikistirilmamis toplami
            // veriyor: 35 MB'lik bir paket icin 828 MB yazdi ve bir an
            // icin oyunun magazaya sigmadigi sanildi.
            float mb = File.Exists(s.outputPath)
                ? new FileInfo(s.outputPath).Length / 1024f / 1024f
                : s.totalSize / 1024f / 1024f;

            Debug.Log(string.Format(
                "=== Lokanta yapi ===\n  hedef  : {0}\n  yol    : {1}\n"
                + "  boyut  : {2:0.0} MB\n  sure   : {3:0} sn\n  uyari  : {4}\n"
                + "=== yapi tamam ===",
                what, s.outputPath, mb, s.totalTime.TotalSeconds, s.totalWarnings));
        }
    }
}
