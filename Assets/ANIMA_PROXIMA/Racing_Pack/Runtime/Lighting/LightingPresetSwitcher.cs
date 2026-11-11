using UnityEngine;
#if ENABLE_INPUT_SYSTEM && RACINGPACK_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AnimaProxima.RacingPack
{
    /// <summary>
    /// Keeps exactly one of several <see cref="LightingPreset"/> rigs active. Pick the preset from the inspector
    /// dropdown, or press 1, 2, 3… in Play mode. Activating a rig applies its RenderSettings (see LightingPreset).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("ANIMA PROXIMA/Racing Pack/Lighting Preset Switcher")]
    public class LightingPresetSwitcher : MonoBehaviour
    {
        [Tooltip("Lighting rigs in this scene, in key order (1, 2, 3…).")]
        [SerializeField] LightingPreset[] presets = new LightingPreset[0];

        [SerializeField] int activeIndex;

        [Tooltip("Switch presets with the number keys 1-9 in Play mode.")]
        [SerializeField] bool numberKeys = true;

        public LightingPreset[] Presets => presets;
        public int ActiveIndex => activeIndex;
        public LightingPreset Active => IsValid(activeIndex) ? presets[activeIndex] : null;

        bool IsValid(int index) => presets != null && index >= 0 && index < presets.Length && presets[index];

        void OnEnable() => Select(activeIndex);

        /// <summary>Activates the preset at <paramref name="index"/> and deactivates all others.</summary>
        public void Select(int index)
        {
            if (!IsValid(index)) return;
            activeIndex = index;

            // Deactivate first, so the newly activated preset is the last one to write RenderSettings.
            for (int i = 0; i < presets.Length; i++)
                if (i != index && presets[i] && presets[i].gameObject.activeSelf)
                    presets[i].gameObject.SetActive(false);

            var go = presets[index].gameObject;
            if (go.activeSelf) presets[index].Apply();
            else go.SetActive(true);
        }

        /// <summary>Activates the preset whose display name matches (case-insensitive). Returns false if none does.</summary>
        public bool Select(string displayName)
        {
            if (presets == null) return false;
            for (int i = 0; i < presets.Length; i++)
                if (presets[i] && string.Equals(presets[i].DisplayName, displayName, System.StringComparison.OrdinalIgnoreCase))
                {
                    Select(i);
                    return true;
                }
            return false;
        }

        void Update()
        {
            if (!numberKeys || !Application.isPlaying || presets == null) return;
            for (int i = 0; i < presets.Length && i < 9; i++)
                if (NumberKeyDown(i + 1))
                    Select(i);
        }

        static bool NumberKeyDown(int number)
        {
#if ENABLE_INPUT_SYSTEM && RACINGPACK_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            return keyboard[Key.Digit1 + (number - 1)].wasPressedThisFrame
                || keyboard[Key.Numpad1 + (number - 1)].wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha0 + number) || Input.GetKeyDown(KeyCode.Keypad0 + number);
#else
            return false;
#endif
        }
    }
}
