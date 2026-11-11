using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AnimaProxima.RacingPack.Editor
{
    /// <summary>Inspector for <see cref="LightingPresetSwitcher"/>: a dropdown that switches the active rig (with Undo).</summary>
    [CustomEditor(typeof(LightingPresetSwitcher))]
    public class LightingPresetSwitcherEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var switcher = (LightingPresetSwitcher)target;
            var presets = switcher.Presets ?? new LightingPreset[0];

            if (presets.Length > 0 && !serializedObject.isEditingMultipleObjects)
            {
                var names = presets.Select((p, i) => $"{i + 1}  {(p ? p.DisplayName : "(missing)")}").ToArray();
                EditorGUI.BeginChangeCheck();
                int index = EditorGUILayout.Popup("Active Preset", switcher.ActiveIndex, names);
                if (EditorGUI.EndChangeCheck()) Switch(switcher, index);
                EditorGUILayout.Space();
            }

            DrawPropertiesExcluding(serializedObject, "activeIndex");
            serializedObject.ApplyModifiedProperties();
        }

        static void Switch(LightingPresetSwitcher switcher, int index)
        {
            var targets = switcher.Presets.Where(p => p).Select(p => (Object)p.gameObject).Append(switcher).ToArray();
            Undo.RecordObjects(targets, "Switch Lighting Preset");
            switcher.Select(index);
            // RenderSettings are saved with the scene but changes to them aren't tracked by Undo.
            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(switcher.gameObject.scene);
        }
    }
}
