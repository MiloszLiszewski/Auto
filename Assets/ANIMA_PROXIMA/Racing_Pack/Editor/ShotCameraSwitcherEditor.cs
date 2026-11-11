using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnimaProxima.RacingPack.Editor
{
    /// <summary>Inspector for <see cref="ShotCameraSwitcher"/>: a dropdown that switches the active shot (with Undo).</summary>
    [CustomEditor(typeof(ShotCameraSwitcher))]
    public class ShotCameraSwitcherEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var switcher = (ShotCameraSwitcher)target;
            var shots = switcher.Shots ?? new ShotCamera[0];

            if (shots.Length > 0 && !serializedObject.isEditingMultipleObjects)
            {
                var names = shots.Select((s, i) => s ? $"{i + 1:00}  {s.ShotName}  ({(string.IsNullOrEmpty(s.Preset) ? "any" : s.Preset)})" : "(missing)").ToArray();
                EditorGUI.BeginChangeCheck();
                int index = EditorGUILayout.Popup("Active Shot", switcher.ActiveIndex, names);
                if (EditorGUI.EndChangeCheck()) Switch(switcher, index);

                var active = switcher.Active;
                if (active && !string.IsNullOrEmpty(active.Notes)) EditorGUILayout.HelpBox(active.Notes, MessageType.None);
                if (active && GUILayout.Button("Align Scene View to Shot") && SceneView.lastActiveSceneView)
                    SceneView.lastActiveSceneView.AlignViewToObject(active.transform);
                EditorGUILayout.Space();
            }

            DrawPropertiesExcluding(serializedObject, "activeIndex");
            serializedObject.ApplyModifiedProperties();
        }

        static void Switch(ShotCameraSwitcher switcher, int index)
        {
            var targets = switcher.Shots.Where(s => s).Select(s => (Object)s.gameObject).Append(switcher).ToList();
            if (switcher.Lighting)
                targets.AddRange(switcher.Lighting.Presets.Where(p => p).Select(p => (Object)p.gameObject).Append(switcher.Lighting));
            Undo.RecordObjects(targets.ToArray(), "Switch Shot");
            switcher.Select(index);
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(switcher.gameObject.scene);
        }
    }
}
