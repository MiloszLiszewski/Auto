using UnityEngine;
#if ENABLE_INPUT_SYSTEM && RACINGPACK_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AnimaProxima.RacingPack
{
    /// <summary>
    /// Keeps exactly one <see cref="ShotCamera"/> active. Pick the shot from the inspector dropdown, or press
    /// Left/Right arrow in Play mode. Optionally switches the scene's lighting to the shot's preset.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("ANIMA PROXIMA/Racing Pack/Shot Camera Switcher")]
    public class ShotCameraSwitcher : MonoBehaviour
    {
        [Tooltip("Shot cameras in this scene, in browsing order.")]
        [SerializeField] ShotCamera[] shots = new ShotCamera[0];

        [SerializeField] int activeIndex;

        [Tooltip("Also switch the lighting to the shot's preset (by display name).")]
        [SerializeField] bool applyShotPreset = true;

        [Tooltip("Lighting switcher used when Apply Shot Preset is on.")]
        [SerializeField] LightingPresetSwitcher lighting;

        [Tooltip("Browse shots with the Left/Right arrow keys in Play mode.")]
        [SerializeField] bool arrowKeys = true;

        public ShotCamera[] Shots => shots;
        public int ActiveIndex => activeIndex;
        public ShotCamera Active => IsValid(activeIndex) ? shots[activeIndex] : null;
        public LightingPresetSwitcher Lighting => lighting;

        bool IsValid(int index) => shots != null && index >= 0 && index < shots.Length && shots[index];

        void OnEnable() => Select(activeIndex, false);

        /// <summary>Activates the shot at <paramref name="index"/>, deactivates the others, and optionally applies its preset.</summary>
        public void Select(int index) => Select(index, applyShotPreset);

        public void Select(int index, bool applyPreset)
        {
            if (!IsValid(index)) return;
            activeIndex = index;
            for (int i = 0; i < shots.Length; i++)
                if (shots[i] && shots[i].gameObject.activeSelf != (i == index))
                    shots[i].gameObject.SetActive(i == index);
            if (applyPreset && lighting && !string.IsNullOrEmpty(shots[index].Preset))
                lighting.Select(shots[index].Preset);
        }

        void Update()
        {
            if (!arrowKeys || !Application.isPlaying || shots == null || shots.Length == 0) return;
            int step = KeyDown(true) ? 1 : KeyDown(false) ? -1 : 0;
            if (step != 0) Select((activeIndex + step + shots.Length) % shots.Length);
        }

        static bool KeyDown(bool next)
        {
#if ENABLE_INPUT_SYSTEM && RACINGPACK_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && (next ? keyboard.rightArrowKey : keyboard.leftArrowKey).wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(next ? KeyCode.RightArrow : KeyCode.LeftArrow);
#else
            return false;
#endif
        }
    }
}
