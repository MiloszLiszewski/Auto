using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimaProxima.RacingPack.Editor
{
    /// <summary>
    /// Racing Pack Setup (Tools → ANIMA PROXIMA → Racing Pack Setup): matches the pack's materials, lighting rigs and
    /// cameras to the project's render pipeline by importing one of the packages in Racing_Pack/RenderPipelines. Each
    /// package overwrites the same assets (same GUIDs), so scenes and prefabs keep working.
    ///
    /// Opens by itself once per project after import, and again (once per session) when the installed variant doesn't
    /// match the active pipeline. Nothing in the project changes until a button is pressed. Uses no render-pipeline
    /// assemblies: pipelines and Post Processing are detected by type name.
    /// </summary>
    public class PipelineSetupWindow : EditorWindow
    {
        public enum Pipeline { BuiltIn, URP, HDRP }

        // Assets are found by GUID, so the pack still works after being moved.
        const string UrpPackage = "8ddde5da4996349dfb78d2add623da88";
        const string BuiltInPackage = "83b5b400245a54ee5ac2268d3b0c7efd";
        const string PostFXPackage = "12a0c9bd3efe246f39fd9bde90cf3276";
        const string HdrpPackage = "a1fa08c6d0adf469b8e77b8556faf86e";
        const string ReferenceMaterial = "a95b8230418944ac696862faf9698bd3";   // Palette.mat
        const string CameraPrefab = "f21768dadda9d4055bd076fab915e0e8";        // ShowcaseCameras.prefab
        const string UrpAsset = "c6c6c24a9e6f249c0825d5f246b55204";            // RacingPack_URP_Asset
        const string HdrpAsset = "7fe43b017f8c74777b425f19eeb37cdc";           // RacingPack_HDRP_Asset (HDRP package only)

        const string PostProcessLayerType = "UnityEngine.Rendering.PostProcessing.PostProcessLayer, Unity.Postprocessing.Runtime";
        const string SessionKey = "AnimaProxima.RacingPack.SetupChecked";
        static string ShownKey => "AnimaProxima.RacingPack.SetupShown." + Hash128.Compute(Application.dataPath);   // per project

        Vector2 scroll;
        List<string> stubs = new();

        [MenuItem("Tools/ANIMA PROXIMA/Racing Pack Setup")]
        public static void Open()
        {
            var window = GetWindow<PipelineSetupWindow>(true, "Racing Pack Setup");
            window.minSize = new Vector2(460, 420);
            window.Refresh();
        }

        [InitializeOnLoadMethod]
        static void AutoOpen()
        {
            if (Application.isBatchMode || SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.delayCall += () =>
            {
                SessionState.SetBool(SessionKey, true);
                bool firstTime = !EditorPrefs.GetBool(ShownKey, false);
                var installed = InstalledVariant();
                if (installed == null) return;   // pack not (fully) imported yet
                if (firstTime || !Matches(installed, ActivePipeline))
                {
                    EditorPrefs.SetBool(ShownKey, true);
                    Open();
                }
            };
        }

        // ================================================================ detection

        public static Pipeline ActivePipeline
        {
            get
            {
                var name = GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.GetType().Name : "";
                return name.Contains("Universal") ? Pipeline.URP : name.Contains("HDRenderPipeline") ? Pipeline.HDRP : Pipeline.BuiltIn;
            }
        }

        public static bool PostProcessingInstalled => Type.GetType(PostProcessLayerType) != null;

        /// <summary>"URP", "HDRP", "BuiltIn", "BuiltIn_PostFX", "Mismatch" (shaders of another pipeline) or null.</summary>
        public static string InstalledVariant()
        {
            var material = Load<Material>(ReferenceMaterial);
            if (!material) return null;
            switch (material.shader.name)
            {
                case "Universal Render Pipeline/Lit": return "URP";
                case "HDRP/Lit": return "HDRP";
                case "Standard":
                    var cameras = Load<GameObject>(CameraPrefab);
                    bool postFx = cameras && cameras.GetComponentsInChildren<Component>(true).Any(c => c && c.GetType().Name == "PostProcessLayer");
                    return postFx ? "BuiltIn_PostFX" : "BuiltIn";
                default: return "Mismatch";
            }
        }

        static bool Matches(string variant, Pipeline pipeline) => variant.StartsWith(pipeline.ToString());

        static T Load<T>(string guid) where T : UnityEngine.Object
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        static string PackRoot()
        {
            var package = AssetDatabase.GUIDToAssetPath(UrpPackage);   // <root>/RenderPipelines/URP.unitypackage
            return string.IsNullOrEmpty(package) ? null : System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(package)).Replace('\\', '/');
        }

        /// <summary>
        /// Pack assets whose main object can't load: assets of another pipeline that no package replaced, such as the
        /// recommended URP asset in a Built-in or HDRP project. Packages can add and overwrite files but not delete them.
        /// </summary>
        public static List<string> FindStubs()
        {
            var root = PackRoot();
            if (root == null) return new List<string>();
            return AssetDatabase.FindAssets("", new[] { root }).Select(AssetDatabase.GUIDToAssetPath).Distinct()
                .Where(p => p.EndsWith(".asset") && !AssetDatabase.LoadMainAssetAtPath(p))
                .OrderBy(p => p).ToList();
        }

        // ================================================================ actions

        public static void Import(Pipeline pipeline, bool postFx)
        {
            var guids = pipeline switch
            {
                Pipeline.URP => new[] { UrpPackage },
                Pipeline.HDRP => new[] { HdrpPackage },
                _ => postFx ? new[] { BuiltInPackage, PostFXPackage } : new[] { BuiltInPackage },
            };
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError($"Racing Pack: package {guid} is missing from RenderPipelines/. Re-import the pack.");
                    return;
                }
                AssetDatabase.ImportPackage(path, false);
            }
            Debug.Log($"Racing Pack: imported the {pipeline}{(postFx ? " + PostFX" : "")} version.");
        }

        public static void DeleteStubs(IEnumerable<string> paths)
        {
            var failed = new List<string>();
            AssetDatabase.DeleteAssets(paths.ToArray(), failed);
            foreach (var p in failed) Debug.LogWarning($"Racing Pack: could not delete {p}");
        }

        /// <summary>Assigns the pack's recommended pipeline asset to Graphics settings and the current quality level.</summary>
        public static void UseRecommendedPipelineAsset(Pipeline pipeline)
        {
            var asset = Load<RenderPipelineAsset>(pipeline == Pipeline.HDRP ? HdrpAsset : UrpAsset);
            if (!asset) return;
            GraphicsSettings.defaultRenderPipeline = asset;
            QualitySettings.renderPipeline = asset;
            Debug.Log($"Racing Pack: Graphics settings and quality level '{QualitySettings.names[QualitySettings.GetQualityLevel()]}' now use {asset.name}.");
        }

        /// <summary>Built-in: shadow settings the showcase is tuned for (same reach as the recommended URP asset).</summary>
        public static void UseRecommendedBuiltInShadows()
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowDistance = 700f;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowCascade4Split = new Vector3(0.05f, 0.15f, 0.4f);
            Debug.Log($"Racing Pack: quality level '{QualitySettings.names[QualitySettings.GetQualityLevel()]}' now uses 700 m shadows with 4 cascades.");
        }

        // ================================================================ GUI

        void OnFocus() => Refresh();

        void Refresh()
        {
            stubs = FindStubs();
            Repaint();
        }

        void OnGUI()
        {
            var pipeline = ActivePipeline;
            var installed = InstalledVariant() ?? "not found";
            bool ppv2 = PostProcessingInstalled;
            bool matches = Matches(installed, pipeline);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Low Poly Racing Track Starter Pack", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Materials, lighting rigs and cameras come in one version per render pipeline. Import the " +
                                    "version that matches this project; scenes and prefabs keep working.", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Active pipeline", Label(pipeline));
            EditorGUILayout.LabelField("Installed version", installed.Replace("_PostFX", " + PostFX"));
            if (pipeline == Pipeline.BuiltIn) EditorGUILayout.LabelField("Post Processing (PPv2)", ppv2 ? "installed" : "not installed");

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(matches ? "The installed version matches this project's pipeline."
                : $"The installed version doesn't match. Import the {Label(pipeline)} version.", matches ? MessageType.Info : MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("1. Import", EditorStyles.boldLabel);
            if (pipeline == Pipeline.BuiltIn)
            {
                if (GUILayout.Button("Import Built-in version")) Run(() => Import(pipeline, false));
                using (new EditorGUI.DisabledScope(!ppv2))
                    if (GUILayout.Button("Import Built-in version + PostFX")) Run(() => Import(pipeline, true));
                if (!ppv2)
                {
                    EditorGUILayout.HelpBox("For the post-processing look (ACES, color grading, bloom, vignette), install " +
                                            "Post Processing from the Package Manager first.", MessageType.None);
                    if (GUILayout.Button("Open Package Manager")) UnityEditor.PackageManager.UI.Window.Open("com.unity.postprocessing");
                }
            }
            else if (GUILayout.Button($"Import {Label(pipeline)} version")) Run(() => Import(pipeline, false));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("2. Clean up", EditorStyles.boldLabel);
            if (stubs.Count == 0) EditorGUILayout.LabelField("Nothing to clean up.", EditorStyles.miniLabel);
            else
            {
                EditorGUILayout.HelpBox("These pack assets belong to another render pipeline and can't load here:\n" +
                                        string.Join("\n", stubs.Select(s => "• " + System.IO.Path.GetFileName(s))), MessageType.Warning);
                if (GUILayout.Button($"Delete {stubs.Count} unusable asset(s)…") &&
                    EditorUtility.DisplayDialog("Racing Pack Setup", "Delete these assets?\n\n" + string.Join("\n", stubs) +
                                                "\n\nImporting the URP version brings them back.", "Delete", "Cancel"))
                    Run(() => DeleteStubs(stubs));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("3. Optional: recommended settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("The showcase scene is about 1 km across. These settings give it long, sharp shadows. " +
                                    "They change this project's settings, so they are never applied automatically.", MessageType.None);
            switch (pipeline)
            {
                case Pipeline.BuiltIn:
                    if (GUILayout.Button("Use recommended shadow settings for the current quality level")) Run(UseRecommendedBuiltInShadows);
                    break;
                default:
                    bool available = Load<RenderPipelineAsset>(pipeline == Pipeline.HDRP ? HdrpAsset : UrpAsset);
                    using (new EditorGUI.DisabledScope(!available))
                        if (GUILayout.Button($"Use the pack's {Label(pipeline)} pipeline asset")) Run(() => UseRecommendedPipelineAsset(pipeline));
                    if (!available) EditorGUILayout.LabelField("Import the matching version first.", EditorStyles.miniLabel);
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        void Run(Action action)
        {
            action();
            AssetDatabase.SaveAssets();
            Refresh();
        }

        static string Label(Pipeline p) => p switch { Pipeline.BuiltIn => "Built-in", _ => p.ToString() };
    }
}
