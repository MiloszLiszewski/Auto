using UnityEngine;
using UnityEngine.Rendering;

namespace AnimaProxima.RacingPack
{
    /// <summary>
    /// Root component of a <c>LightingRig_*</c> prefab (sun + post volume).
    /// When the rig is enabled it applies its scene-wide settings in <see cref="RenderSettings"/>: sun, skybox,
    /// gradient ambient, fog and a sky reflection cubemap. Everything is realtime (no baking), so switching presets at
    /// runtime is instant and independent of the project's quality settings. Works in Edit mode too, so dropping a rig
    /// into a scene (or switching rigs with <see cref="LightingPresetSwitcher"/>) changes the look immediately.
    /// Keep only one rig active per scene; the last one enabled wins.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("ANIMA PROXIMA/Racing Pack/Lighting Preset")]
    public class LightingPreset : MonoBehaviour
    {
        [Tooltip("Name shown by the Lighting Preset Switcher.")]
        [SerializeField] string displayName = "Preset";

        [Tooltip("Directional light used as the sun (also drives the sun disc of procedural skyboxes).")]
        [SerializeField] Light sun;

        [Header("Sky")]
        [Tooltip("Set RenderSettings.skybox. Off for pipelines that draw the sky from a volume (HDRP).")]
        [SerializeField] bool applySkybox = true;
        [SerializeField] Material skybox;

        [Header("Ambient (gradient)")]
        [SerializeField] bool applyAmbient = true;
        [ColorUsage(false, true)] [SerializeField] Color ambientSky = new Color(0.5f, 0.66f, 0.77f);
        [ColorUsage(false, true)] [SerializeField] Color ambientEquator = new Color(0.75f, 0.57f, 0.55f);
        [ColorUsage(false, true)] [SerializeField] Color ambientGround = new Color(0.23f, 0.19f, 0.21f);

        [Header("Fog")]
        [Tooltip("Set RenderSettings fog. Off for pipelines that fog through a volume (HDRP).")]
        [SerializeField] bool applyFog = true;
        [SerializeField] bool fog = true;
        [SerializeField] FogMode fogMode = FogMode.Linear;
        [SerializeField] Color fogColor = new Color(0.95f, 0.64f, 0.51f);
        [Tooltip("Linear fog start distance.")]
        [SerializeField, Min(0f)] float fogStart = 350f;
        [Tooltip("Linear fog end distance.")]
        [SerializeField, Min(0f)] float fogEnd = 2600f;
        [Tooltip("Density for Exponential / Exponential Squared fog.")]
        [SerializeField, Min(0f)] float fogDensity = 0.001f;

        [Header("Reflections")]
        [Tooltip("Use Reflection as the scene's default (environment) reflection. Off for pipelines that reflect the " +
                 "sky from a volume (HDRP).")]
        [SerializeField] bool applyReflection = true;
        [Tooltip("Cubemap of this preset's sky, so water and glossy surfaces reflect it.")]
        [SerializeField] Cubemap reflection;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Light Sun => sun;

        void OnEnable()
        {
#if UNITY_EDITOR
            // Prefab Mode and prefab-editing previews must not overwrite the open scene's RenderSettings.
            if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene)) return;
#endif
            Apply();
        }

        /// <summary>Writes this preset into the active scene's RenderSettings and refreshes environment lighting.</summary>
        public void Apply()
        {
            if (sun) RenderSettings.sun = sun;

            if (applySkybox) RenderSettings.skybox = skybox;

            if (applyAmbient)
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = ambientSky;
                RenderSettings.ambientEquatorColor = ambientEquator;
                RenderSettings.ambientGroundColor = ambientGround;
            }

            if (applyFog)
            {
                RenderSettings.fog = fog;
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogStartDistance = fogStart;
                RenderSettings.fogEndDistance = fogEnd;
                RenderSettings.fogDensity = fogDensity;
            }

            if (applyReflection && reflection)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = reflection;
            }

            DynamicGI.UpdateEnvironment();
        }
    }
}
