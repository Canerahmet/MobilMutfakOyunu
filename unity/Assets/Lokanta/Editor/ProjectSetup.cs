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
    /// Applies the project settings in code. Nothing is clicked by hand, and
    /// it is repeatable.
    ///
    /// To run it:
    ///   Unity.exe -batchmode -quit -projectPath unity
    ///             -executeMethod Lokanta.EditorTools.ProjectSetup.ApplyAll
    ///             -logFile -
    ///
    /// The source: docs/19-technical-setup.md B4 and B5.
    /// The first release is Android ONLY (there is no Mac), see
    /// docs/22-answers-and-direction.md.
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

        [MenuItem("Lokanta/Apply the project settings")]
        public static void ApplyAll()
        {
            Log.Clear();
            Problems.Clear();
            Log.Add("=== Lokanta project setup ===");

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
                Debug.LogError("PROBLEMS:\n" + string.Join("\n", Problems));
                Finish(1);
            }
            else
            {
                Debug.Log("=== Setup done, no problems ===");
                Finish(0);
            }
        }

        private static void Finish(int code)
        {
            // In batch mode the exit code should mean something. Do not exit
            // while the editor is open.
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        // -------------------------------------------------------------------
        /// <summary>
        /// The identity fields. THE VALUES COME FROM BuildPlayer.
        ///
        /// This used to write its own values: com.lokanta.game, companyName
        /// "Lokanta", LandscapeLeft, targetSdk Auto - while BuildPlayer writes
        /// com.ahmetakar.lokanta, "Ahmet Akar", AutoRotation, targetSdk 36 AND
        /// explains at length in its own comments why each of the three has to
        /// be that way.
        ///
        /// The two were writing the same fields in opposite directions and
        /// there was no way to see which had run last. THE PACKAGE NAME IS
        /// LOCKED FOREVER ON THE FIRST UPLOAD: upload once as com.lokanta.game
        /// and there is no way back.
        ///
        /// The same class of bug (one number in two places) happened on this
        /// project with the URP settings, and check_urp.py was written because
        /// of it. The cure is the same: one source.
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
            catch (Exception e) { Fail("identity", e); }
        }

        // -------------------------------------------------------------------
        private static void ApplyRendering()
        {
            try
            {
                // docs/19 B4: needed for soft light and shadow
                PlayerSettings.colorSpace = ColorSpace.Linear;
                Ok("colorSpace", PlayerSettings.colorSpace);
            }
            catch (Exception e) { Fail("colorSpace", e); }

            try
            {
                // Vulkan first, OpenGL ES 3 as the fallback. Automatic
                // selection is off.
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
                {
                    GraphicsDeviceType.Vulkan,
                    GraphicsDeviceType.OpenGLES3
                });
                Ok("Android graphics API", "Vulkan, OpenGLES3");
            }
            catch (Exception e) { Fail("graphics API", e); }
        }

        // -------------------------------------------------------------------
        private static void ApplyAndroid()
        {
            try
            {
                // docs/19 B5: the lowest is Android 10 = API 29
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
                // THE TARGET SDK IS FIXED, NOT "Auto".
                //
                // Auto ties it to the highest platform installed ON THE MACHINE
                // the build is taken on - that is, the same repository compiles
                // against two different targets on two machines, and if Play's
                // threshold is missed it only comes out at upload time.
                // BuildPlayer writes 36 and the justification is there; this
                // place must not undo it.
                PlayerSettings.Android.targetSdkVersion =
                    AndroidSdkVersions.AndroidApiLevel36;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetScriptingBackend(
                    NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetApiCompatibilityLevel(
                    NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
                PlayerSettings.Android.useAPKExpansionFiles = false;
                EditorUserBuildSettings.buildAppBundle = true;   // the Play Store wants an AAB
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

                Ok("minSdkVersion", PlayerSettings.Android.minSdkVersion);
                Ok("targetArchitectures", PlayerSettings.Android.targetArchitectures);
                Ok("scriptingBackend", "IL2CPP");
                Ok("apiCompatibility", "NET_Standard");
                Ok("textureCompression", EditorUserBuildSettings.androidBuildSubtarget);
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
            catch (Exception e) { Fail("switching the build target", e); }
        }

        // -------------------------------------------------------------------
        private static void ApplyOrientation()
        {
            try
            {
                // docs/16 and the user's decision of 9 September 2026:
                // landscape only.
                // AUTOROTATION, NOT LandscapeLeft.
                //
                // A fixed orientation makes Unity write
                // screenOrientation="landscape" into the manifest and IGNORE
                // the landscape flags: turn the phone the other way round and
                // the game is upside down. BuildPlayer had already fixed this;
                // this place was undoing it.
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
                PlayerSettings.accelerometerFrequency = 0;   // accelerometer off
                Ok("orientation", "landscape only (left and right)");
                Ok("accelerometer", "off");
            }
            catch (Exception e) { Fail("orientation", e); }
        }

        // -------------------------------------------------------------------
        private static void ApplyQuality()
        {
            try
            {
                // On mobile we keep a single quality level; the difference
                // between devices will be handled through the URP asset, not
                // through the quality matrix.
                QualitySettings.vSyncCount = 0;             // the code sets the frame rate
                QualitySettings.antiAliasing = 0;           // URP manages MSAA itself
                QualitySettings.shadowCascades = 1;
                QualitySettings.softParticles = false;
                QualitySettings.realtimeReflectionProbes = false;
                QualitySettings.billboardsFaceCameraPosition = false;
                QualitySettings.skinWeights = SkinWeights.TwoBones;
                Ok("vSyncCount", QualitySettings.vSyncCount);
                Ok("skinWeights", QualitySettings.skinWeights);
            }
            catch (Exception e) { Fail("quality", e); }
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
                    Ok("URP asset", "created");
                }
                else
                {
                    Ok("URP asset", "already there");
                }

                ConfigureUrp(urp);

                GraphicsSettings.defaultRenderPipeline = urp;
                QualitySettings.renderPipeline = urp;
                Ok("renderPipeline", urp.name);
            }
            catch (Exception e) { Fail("URP", e); }
        }

        /// <summary>
        /// In URP 17 most of these are read-only properties. We write the
        /// serialised field directly. If a field name changes with the version
        /// we report it rather than pass over it silently.
        /// </summary>
        private static void SetUrpField(SerializedObject so, string field,
                                        Action<SerializedProperty> set, string label)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Problems.Add("  URP field not found: " + field
                             + " (the URP version may have changed)");
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
            // docs/19 B4 and the correction from the review.
            SerializedObject so = new SerializedObject(urp);

            // THIS BLOCK IS DANGEROUS BY ITS VERY NATURE: it is the SECOND
            // copy of the numbers in LokantaURP.asset. The performance round
            // fixed the .asset by hand and forgot this place - so anyone who
            // ran ApplyAll pulled MSAA back from 1 to 4 and the shadow distance
            // from 45 back to 25, and no check caught it. The values are now
            // THE SAME as the .asset; whoever changes one has to change both.
            SetUrpField(so, "m_SupportsHDR", p => p.boolValue = false, "URP HDR");

            // MSAA 1. Combined with a 0.8 render scale, 4x was eating a few ms
            // on its own on a low-end Adreno, and on flat low-poly surfaces the
            // gain is not visible.
            SetUrpField(so, "m_MSAA", p => p.intValue = 1, "URP MSAA");
            SetUrpField(so, "m_RenderScale", p => p.floatValue = 0.8f, "URP render scale");
            SetUrpField(so, "m_RequireDepthTexture", p => p.boolValue = false, "URP depth texture");
            SetUrpField(so, "m_RequireOpaqueTexture", p => p.boolValue = false, "URP opaque texture");
            SetUrpField(so, "m_MainLightShadowsSupported", p => p.boolValue = true, "URP main light shadow");
            SetUrpField(so, "m_AdditionalLightShadowsSupported", p => p.boolValue = false, "URP additional light shadow");

            // LightRenderingMode { Disabled=0, PerVertex=1, PerPixel=2 }.
            // OFF: the fill light was opening a second per-pixel pass and there
            // is only one directional light in the scene.
            SetUrpField(so, "m_AdditionalLightsRenderingMode", p => p.enumValueIndex = 0, "URP additional light mode");
            SetUrpField(so, "m_AdditionalLightsPerObjectLimit", p => p.intValue = 4, "URP additional light limit");

            // THE SHADOW DISTANCE IS MEASURED FROM THE CAMERA, NOT FROM THE
            // SCENE.
            //
            // The performance round had cut this from 25 to 14, on the grounds
            // that "the plot is 18 x 9.6 m, everything is inside the shadow
            // map". The justification was IN THE WRONG UNIT: URP measures this
            // distance along the camera's view depth, and the camera stands
            // 26-35 m away. So the value had silently turned off every CAST
            // SHADOW - no test caught it, it was seen in a screenshot and
            // fixed.
            //
            // 45 = in the worst case (narrow aspect ratio, thick interface
            // bars) the furthest corner of the scene is 39.4 m, plus a margin.
            SetUrpField(so, "m_ShadowDistance", p => p.floatValue = 45f, "URP shadow distance");
            SetUrpField(so, "m_ShadowCascadeCount", p => p.intValue = 1, "URP shadow cascades");
            // At 45 m on a single cascade a 512 map = 18 cm/texel and the
            // shadows become unrecognisable. 1024 = 9 cm and that WAS NOT
            // ENOUGH (see below).
            SetUrpField(so, "m_MainLightShadowmapResolution", p => p.intValue = 2048,
                        "URP shadow map");

            // THE SHADOW MAP GOES 1024 -> 2048 AND THE BIASES BELOW THE
            // DEFAULT.
            //
            // Measured: the figure's shadow was DETACHED FROM ITS FEET. At
            // 45 m a 1024 map means 8.8 cm/texel, and Unity's default normal
            // bias (1.0) shifts the sample ONE TEXEL along the normal - so on a
            // 1.10 m figure the shadow lands 8% of the body height away. In the
            // screenshot the figure looked as if it were floating
            // ("peter-panning").
            //
            // Both levers depend on the texel size, so both at once: 2048 takes
            // the texel down to 4.4 cm and the same bias shifts by half as much;
            // and once the bias itself is lowered too the shift comes down to
            // 4.4 x 0.4 = 1.8 cm - invisible on a figure.
            //
            // 2048 IS CHEAP HERE, because there is almost nothing in the shadow
            // pass: all the geometry the Modeler generates (floor, wall,
            // furniture) is shadowCastingMode.Off and the only things casting a
            // shadow are the characters. So the cost is CLEARING the map and
            // 8 MB of memory; the area rasterised is a handful of small
            // figures.
            //
            // The biases WERE NEVER SET BEFORE: they were not in the SetUrpField
            // list, which means check_urp.py was not checking them either and
            // they had been left at Unity's defaults. They are checked now.
            SetUrpField(so, "m_ShadowDepthBias", p => p.floatValue = 0.6f, "URP shadow depth bias");
            SetUrpField(so, "m_ShadowNormalBias", p => p.floatValue = 0.4f, "URP shadow normal bias");

            // GPU Resident Drawer: the review said OFF.
            // It wants Forward+, it does not work on GLES, and at 100 draw
            // calls it buys nothing. The docs/19 B4 table said "On"; this line
            // overrides it.
            SerializedProperty grd = so.FindProperty("m_GPUResidentDrawerMode");
            if (grd != null)
            {
                grd.enumValueIndex = 0;   // Disabled
                Ok("GPU Resident Drawer", "Disabled (the review's correction)");
            }
            else
            {
                Log.Add("  no GPU Resident Drawer field; it is already off in this URP version");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);
        }
    }
}
