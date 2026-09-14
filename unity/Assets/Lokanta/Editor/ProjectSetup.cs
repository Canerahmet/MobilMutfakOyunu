using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Proje ayarlarini kod ile uygular. Elle tiklanmaz, tekrarlanabilir.
    ///
    /// Calistirma:
    ///   Unity.exe -batchmode -quit -projectPath unity
    ///             -executeMethod Lokanta.EditorTools.ProjectSetup.ApplyAll
    ///             -logFile -
    ///
    /// Kaynak: docs/19-teknik-kurulum.md B4 ve B5.
    /// Ilk surum SADECE Android (Mac yok), bkz. docs/22-cevaplar-ve-yon.md.
    /// </summary>
    public static class ProjectSetup
    {
        private const string UrpDir = "Assets/Settings";
        private const string UrpAssetPath = UrpDir + "/LokantaURP.asset";
        private const string UrpRendererPath = UrpDir + "/LokantaURP_Renderer.asset";

        private static readonly List<string> Log = new List<string>();
        private static readonly List<string> Problems = new List<string>();

        private static void Ok(string what, object value)
        {
            Log.Add("  " + what.PadRight(34) + " = " + value);
        }

        private static void Fail(string what, Exception e)
        {
            Problems.Add("  " + what + " -> " + e.GetType().Name + ": " + e.Message);
        }

        [MenuItem("Lokanta/Proje ayarlarini uygula")]
        public static void ApplyAll()
        {
            Log.Clear();
            Problems.Clear();
            Log.Add("=== Lokanta proje kurulumu ===");

            ApplyIdentity();
            ApplyRendering();
            ApplyAndroid();
            ApplyOrientation();
            ApplyQuality();
            EnsureUrp();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(string.Join("\n", Log));
            if (Problems.Count > 0)
            {
                Debug.LogError("SORUNLAR:\n" + string.Join("\n", Problems));
                Finish(1);
            }
            else
            {
                Debug.Log("=== Kurulum tamam, sorun yok ===");
                Finish(0);
            }
        }

        private static void Finish(int code)
        {
            // Toplu kipte cikis kodu anlamli olsun. Editorde acikken cikma.
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        // -------------------------------------------------------------------
        /// <summary>
        /// Kimlik alanlari. DEGERLER BuildPlayer'DAN GELIYOR.
        ///
        /// Once burasi kendi degerlerini yaziyordu: com.lokanta.game,
        /// companyName "Lokanta", LandscapeLeft, targetSdk Auto -
        /// BuildPlayer ise com.ahmetakar.lokanta, "Ahmet Akar",
        /// AutoRotation, targetSdk 36 yaziyor VE ucunun de neden oyle
        /// olmasi gerektigini kendi yorumlarinda uzun uzun anlatiyor.
        ///
        /// Ikisi ayni alanlari ters yonde yaziyordu ve hangisinin son
        /// kostugu gorunmuyordu. PAKET ADI ILK YUKLEMEDE SONSUZA KADAR
        /// KILITLENIYOR: com.lokanta.game ile bir kez yuklenirse donus
        /// yok.
        ///
        /// Ayni sinif hata (bir sayi iki yerde) bu projede URP
        /// ayarlarinda yasandi ve check_urp.py o yuzden yazildi.
        /// Cozumu ayni: tek kaynak.
        /// </summary>
        private static void ApplyIdentity()
        {
            try
            {
                PlayerSettings.companyName = BuildPlayer.Company;
                PlayerSettings.productName = BuildPlayer.Product;
                PlayerSettings.SetApplicationIdentifier(
                    NamedBuildTarget.Android, BuildPlayer.Package);
                PlayerSettings.bundleVersion = "0.1.0";
                PlayerSettings.Android.bundleVersionCode = 1;
                Ok("companyName", PlayerSettings.companyName);
                Ok("productName", PlayerSettings.productName);
                Ok("applicationIdentifier", BuildPlayer.Package);
            }
            catch (Exception e) { Fail("kimlik", e); }
        }

        // -------------------------------------------------------------------
        private static void ApplyRendering()
        {
            try
            {
                // docs/19 B4: yumusak isik ve golge icin gerekli
                PlayerSettings.colorSpace = ColorSpace.Linear;
                Ok("colorSpace", PlayerSettings.colorSpace);
            }
            catch (Exception e) { Fail("colorSpace", e); }

            try
            {
                // Vulkan oncelikli, OpenGL ES 3 yedek. Otomatik secim kapali.
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
                {
                    GraphicsDeviceType.Vulkan,
                    GraphicsDeviceType.OpenGLES3
                });
                Ok("Android grafik API", "Vulkan, OpenGLES3");
            }
            catch (Exception e) { Fail("grafik API", e); }
        }

        // -------------------------------------------------------------------
        private static void ApplyAndroid()
        {
            try
            {
                // docs/19 B5: en dusuk Android 10 = API 29
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
                // HEDEF SDK SABIT, "Auto" DEGIL.
                //
                // Auto, yapinin alindigi MAKINEDE kurulu en yuksek
                // platforma baglaniyor - yani ayni depo iki makinede iki
                // farkli hedefle derleniyor ve Play'in esigi kacirilirsa
                // bu ancak yukleme aninda ortaya cikiyor. BuildPlayer
                // 36 yaziyor ve gerekcesi orada; burasi onu bozmasin.
                PlayerSettings.Android.targetSdkVersion =
                    AndroidSdkVersions.AndroidApiLevel36;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetScriptingBackend(
                    NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetApiCompatibilityLevel(
                    NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
                PlayerSettings.Android.useAPKExpansionFiles = false;
                EditorUserBuildSettings.buildAppBundle = true;   // Play Store AAB ister
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

                Ok("minSdkVersion", PlayerSettings.Android.minSdkVersion);
                Ok("targetArchitectures", PlayerSettings.Android.targetArchitectures);
                Ok("scriptingBackend", "IL2CPP");
                Ok("apiCompatibility", "NET_Standard");
                Ok("dokuSikistirma", EditorUserBuildSettings.androidBuildSubtarget);
                Ok("buildAppBundle", EditorUserBuildSettings.buildAppBundle);
            }
            catch (Exception e) { Fail("android", e); }

            try
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildTargetGroup.Android, BuildTarget.Android);
                }
                Ok("activeBuildTarget", EditorUserBuildSettings.activeBuildTarget);
            }
            catch (Exception e) { Fail("build target degistirme", e); }
        }

        // -------------------------------------------------------------------
        private static void ApplyOrientation()
        {
            try
            {
                // docs/16 ve kullanici karari 9 Eylul 2026: sadece yatay.
                // AUTOROTATION, LandscapeLeft DEGIL.
                //
                // Sabit bir yon Unity'ye manifestte
                // screenOrientation="landscape" yazdiriyor ve yatay
                // bayraklari YOK SAYIYOR: telefon ters cevrilince oyun
                // bas asagi duruyor. BuildPlayer bunu zaten duzeltmisti;
                // burasi geri aliyordu.
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                PlayerSettings.allowedAutorotateToPortrait = false;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
                PlayerSettings.allowedAutorotateToPortrait = false;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
                PlayerSettings.useAnimatedAutorotation = true;
                PlayerSettings.accelerometerFrequency = 0;   // ivmeolcer kapali
                Ok("yon", "sadece yatay (sol ve sag)");
                Ok("accelerometer", "kapali");
            }
            catch (Exception e) { Fail("yon", e); }
        }

        // -------------------------------------------------------------------
        private static void ApplyQuality()
        {
            try
            {
                // Mobilde tek kalite seviyesi tutuyoruz; cihaz farkini
                // URP asseti uzerinden yonetecegiz, kalite matrisinden degil.
                QualitySettings.vSyncCount = 0;             // kare hizini kod ayarlar
                QualitySettings.antiAliasing = 0;           // URP MSAA'yi kendi yonetiyor
                QualitySettings.shadowCascades = 1;
                QualitySettings.softParticles = false;
                QualitySettings.realtimeReflectionProbes = false;
                QualitySettings.billboardsFaceCameraPosition = false;
                QualitySettings.skinWeights = SkinWeights.TwoBones;
                Ok("vSyncCount", QualitySettings.vSyncCount);
                Ok("skinWeights", QualitySettings.skinWeights);
            }
            catch (Exception e) { Fail("kalite", e); }
        }

        // -------------------------------------------------------------------
        private static void EnsureUrp()
        {
            try
            {
                if (!Directory.Exists(UrpDir))
                {
                    Directory.CreateDirectory(UrpDir);
                    AssetDatabase.Refresh();
                }

                UniversalRenderPipelineAsset urp =
                    AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);

                if (urp == null)
                {
                    UniversalRendererData rendererData =
                        AssetDatabase.LoadAssetAtPath<UniversalRendererData>(UrpRendererPath);

                    if (rendererData == null)
                    {
                        rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                        AssetDatabase.CreateAsset(rendererData, UrpRendererPath);
                    }

                    urp = UniversalRenderPipelineAsset.Create(rendererData);
                    AssetDatabase.CreateAsset(urp, UrpAssetPath);
                    AssetDatabase.SaveAssets();
                    Ok("URP asseti", "olusturuldu");
                }
                else
                {
                    Ok("URP asseti", "zaten var");
                }

                ConfigureUrp(urp);

                GraphicsSettings.defaultRenderPipeline = urp;
                QualitySettings.renderPipeline = urp;
                Ok("renderPipeline", urp.name);
            }
            catch (Exception e) { Fail("URP", e); }
        }

        /// <summary>
        /// URP 17'de bu alanlarin cogu salt okunur ozellik. Serilestirilmis
        /// alani dogrudan yaziyoruz. Alan adi surumle degisirse sessizce
        /// gecmek yerine rapor ediyoruz.
        /// </summary>
        private static void SetUrpField(SerializedObject so, string field,
                                        Action<SerializedProperty> set, string label)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Problems.Add("  URP alani bulunamadi: " + field
                             + " (URP surumu degismis olabilir)");
                return;
            }
            set(p);
            Ok(label, Describe(p));
        }

        private static string Describe(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Boolean: return p.boolValue.ToString();
                case SerializedPropertyType.Integer: return p.intValue.ToString();
                case SerializedPropertyType.Float: return p.floatValue.ToString("0.##");
                case SerializedPropertyType.Enum:
                    return (p.enumDisplayNames != null
                            && p.enumValueIndex >= 0
                            && p.enumValueIndex < p.enumDisplayNames.Length)
                        ? p.enumDisplayNames[p.enumValueIndex]
                        : "index " + p.enumValueIndex;
                default: return p.propertyType.ToString();
            }
        }

        private static void ConfigureUrp(UniversalRenderPipelineAsset urp)
        {
            // docs/19 B4 ve degerlendirmenin duzeltmesi.
            SerializedObject so = new SerializedObject(urp);

            // BU BLOK VARLIK SEBEBIYLE TEHLIKELI: LokantaURP.asset'teki
            // sayilarin IKINCI kopyasi. Performans turu .asset'i elle
            // duzeltmis, burayi unutmustu - yani ApplyAll'i kostaran biri
            // MSAA'yi 1'den 4'e, golge mesafesini 45'ten 25'e geri
            // cekiyordu ve bunu hicbir denetim yakalamiyordu. Degerler
            // simdi .asset ile AYNI; degistiren ikisini birden
            // degistirmeli.
            SetUrpField(so, "m_SupportsHDR", p => p.boolValue = false, "URP HDR");

            // MSAA 1. 4x, 0,8 render olcegiyle birlestiginde dusuk
            // seviye bir Adreno'da tek basina birkac ms yiyordu ve
            // low-poly'de duz renkli yuzeylerde kazanci gorunmuyor.
            SetUrpField(so, "m_MSAA", p => p.intValue = 1, "URP MSAA");
            SetUrpField(so, "m_RenderScale", p => p.floatValue = 0.8f, "URP render olcegi");
            SetUrpField(so, "m_RequireDepthTexture", p => p.boolValue = false, "URP derinlik dokusu");
            SetUrpField(so, "m_RequireOpaqueTexture", p => p.boolValue = false, "URP opak doku");
            SetUrpField(so, "m_MainLightShadowsSupported", p => p.boolValue = true, "URP ana isik golgesi");
            SetUrpField(so, "m_AdditionalLightShadowsSupported", p => p.boolValue = false, "URP ek isik golgesi");

            // LightRenderingMode { Disabled=0, PerVertex=1, PerPixel=2 }.
            // KAPALI: dolgu isigi ikinci bir piksel gecisi aciyordu ve
            // sahnede yalnizca bir yonlu isik var.
            SetUrpField(so, "m_AdditionalLightsRenderingMode", p => p.enumValueIndex = 0, "URP ek isik kipi");
            SetUrpField(so, "m_AdditionalLightsPerObjectLimit", p => p.intValue = 4, "URP ek isik siniri");

            // GOLGE MESAFESI KAMERADAN OLCULUYOR, SAHNEDEN DEGIL.
            //
            // Performans turu bunu 25'ten 14'e indirmisti, gerekce
            // "arsa 18 x 9,6 m, her sey golge haritasinin icinde" idi.
            // Gerekce YANLIS birimdeydi: URP bu mesafeyi kameranin
            // gorus derinligi boyunca olcuyor ve kamera 26-35 m uzakta
            // duruyor. Yani deger butun DUSEN GOLGELERI sessizce
            // kapatmisti - hicbir test bunu yakalamadi, ekran
            // goruntusunde gorulup duzeltildi.
            //
            // 45 = en kotu durumda (dar kare orani, kalin arayuz
            // cubuklari) sahnenin en uzak kosesi 39,4 m + pay.
            SetUrpField(so, "m_ShadowDistance", p => p.floatValue = 45f, "URP golge mesafesi");
            SetUrpField(so, "m_ShadowCascadeCount", p => p.intValue = 1, "URP golge kademesi");
            // 45 m tek kademede 512 harita = 18 cm/texel, golgeler
            // taniinmaz oluyor. 1024 = 9 cm idi ve YETMEDI (asagi bak).
            SetUrpField(so, "m_MainLightShadowmapResolution", p => p.intValue = 2048,
                        "URP golge haritasi");

            // GOLGE HARITASI 1024 -> 2048 VE BIAS VARSAYILANDAN DUSUK.
            //
            // Olculdu: figurun golgesi AYAGINDAN KOPUKTU. 45 m'de 1024
            // harita 8,8 cm/texel demek ve Unity'nin varsayilan normal
            // bias'i (1,0) ornegi normal boyunca BIR TEXEL kaydiriyor -
            // yani golge, 1,10 m'lik bir figurde govde boyunun %8'i
            // kadar uzaga dusuyor. Ekran goruntusunde figur havada
            // duruyor gibi gorunuyordu ("peter-panning").
            //
            // Iki kaldirac da texel boyuna bagli, o yuzden ikisi birden:
            // 2048 texeli 4,4 cm'ye indiriyor ve ayni bias yarisi kadar
            // kaydiriyor; bias'in kendisi de dusurulunce kayma 4,4 x 0,4
            // = 1,8 cm'ye iniyor - bir figurde gorunmez.
            //
            // 2048 BURADA UCUZ, cunku golge gecisinde neredeyse hicbir
            // sey yok: Modeler'in urettigi butun geometri (zemin,
            // duvar, mobilya) shadowCastingMode.Off ve golge dusuren
            // tek sey karakterler. Yani maliyet haritanin TEMIZLENMESI
            // ve 8 MB bellek; rasterlenen alan bir avuc kucuk figur.
            //
            // Bias'lar ONCE HIC AYARLANMIYORDU: SetUrpField listesinde
            // yoklardi, yani check_urp.py de onlari denetlemiyordu ve
            // Unity varsayilaninda kalmislardi. Artik denetleniyorlar.
            SetUrpField(so, "m_ShadowDepthBias", p => p.floatValue = 0.6f, "URP golge derinlik bias");
            SetUrpField(so, "m_ShadowNormalBias", p => p.floatValue = 0.4f, "URP golge normal bias");

            // GPU Resident Drawer: degerlendirme KAPALI dedi.
            // Forward+ istiyor, GLES'te calismiyor, 100 cizim cagrisinda faydasi yok.
            // docs/19 B4 tablosunda "Acik" yaziyordu; bu satir onu gecersiz kiliyor.
            SerializedProperty grd = so.FindProperty("m_GPUResidentDrawerMode");
            if (grd != null)
            {
                grd.enumValueIndex = 0;   // Disabled
                Ok("GPU Resident Drawer", "Disabled (degerlendirme duzeltmesi)");
            }
            else
            {
                Log.Add("  GPU Resident Drawer alani yok; bu URP surumunde zaten kapali");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);
        }
    }
}
