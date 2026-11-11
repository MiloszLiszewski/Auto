using UnityEngine;

namespace AnimaProxima.RacingPack
{
    /// <summary>
    /// A framed showcase shot: a camera plus the lighting preset it is meant for. Used by
    /// <see cref="ShotCameraSwitcher"/> in the scene and by the dev capture tool. Draws its frustum in the Scene view.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ANIMA PROXIMA/Racing Pack/Shot Camera")]
    public class ShotCamera : MonoBehaviour
    {
        [Tooltip("Shot name, used for menus and capture file names.")]
        [SerializeField] string shotName = "Shot";

        [Tooltip("Display name of the LightingPreset this shot is composed for (e.g. \"Vice City Sunset\"). Empty = any.")]
        [SerializeField] string preset;

        [TextArea(2, 4)]
        [SerializeField] string notes;

        [SerializeField] Color gizmoColor = new Color(1f, 0.8f, 0.2f, 0.8f);

        Camera cachedCamera;

        public string ShotName => string.IsNullOrEmpty(shotName) ? name : shotName;
        public string Preset => preset;
        public string Notes => notes;
        public Camera Camera => cachedCamera ? cachedCamera : cachedCamera = GetComponent<Camera>();

        void OnDrawGizmos()
        {
            var cam = Camera;
            if (!cam) return;
            Gizmos.color = gizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            const float far = 40f;   // short frustum: shows framing without cluttering the scene
            if (cam.orthographic)
            {
                var h = cam.orthographicSize * 2f;
                Gizmos.DrawWireCube(new Vector3(0f, 0f, far * 0.5f), new Vector3(h * cam.aspect, h, far));
            }
            else
            {
                Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, far, cam.nearClipPlane, cam.aspect);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
