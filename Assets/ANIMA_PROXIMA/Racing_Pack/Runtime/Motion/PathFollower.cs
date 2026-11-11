using UnityEngine;

namespace AnimaProxima.RacingPack
{
    /// <summary>
    /// Moves a kinematic Rigidbody along a <see cref="WaypointPath"/> at constant speed, facing the direction of
    /// travel and banking into turns. Used for showcase/trailer cars; it is not a driving controller.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ANIMA PROXIMA/Racing Pack/Path Follower")]
    public class PathFollower : MonoBehaviour
    {
        [SerializeField] WaypointPath path;

        [Tooltip("Speed in units per second.")]
        [SerializeField] float speed = 25f;

        [Tooltip("Distance along the path to start from. Use different values to spread several cars out.")]
        [SerializeField, Min(0f)] float startDistance;

        [Tooltip("Raise the object above the path (along the path's up vector).")]
        [SerializeField] float heightOffset;

        [Tooltip("How far ahead to look when aiming the object. Larger values give smoother steering.")]
        [SerializeField, Min(0.1f)] float lookAhead = 3f;

        [Tooltip("Maximum roll into turns, in degrees.")]
        [SerializeField, Range(0f, 45f)] float maxBankAngle = 8f;

        [Tooltip("Corrects for models whose front isn't +Z. Example: a car modelled facing +X needs (0, -90, 0).")]
        [SerializeField] Vector3 modelRotationOffset;

        Rigidbody body;
        float distance;
        float bank;

        public WaypointPath Path
        {
            get => path;
            set => path = value;
        }

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        public float Distance => distance;

        void Reset() => ConfigureBody(GetComponent<Rigidbody>());

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            ConfigureBody(body);
            distance = startDistance;
        }

        void Start()
        {
            // Snap onto the path immediately so there's no first-frame jump.
            if (path && path.Length > 0f) transform.SetPositionAndRotation(EvaluatePosition(distance), EvaluateRotation(distance, 0f));
        }

        void FixedUpdate()
        {
            if (!path || path.Length <= 0f) return;
            distance += speed * Time.fixedDeltaTime;
            if (!path.Closed && distance > path.Length) distance = path.Length;

            body.MovePosition(EvaluatePosition(distance));
            body.MoveRotation(EvaluateRotation(distance, Time.fixedDeltaTime));
        }

        Vector3 EvaluatePosition(float d) => path.GetPointAtDistance(d) + path.GetUpAtDistance(d) * heightOffset;

        Quaternion EvaluateRotation(float d, float deltaTime)
        {
            var position = path.GetPointAtDistance(d);
            var ahead = path.GetPointAtDistance(d + lookAhead * Mathf.Sign(speed == 0f ? 1f : speed));
            var forward = ahead - position;
            if (forward.sqrMagnitude < 1e-8f) forward = path.GetTangentAtDistance(d);
            var up = path.GetUpAtDistance(d);

            // Bank by how sharply the path turns over the look-ahead distance.
            var nextTangent = path.GetTangentAtDistance(d + lookAhead);
            var turn = Vector3.SignedAngle(path.GetTangentAtDistance(d), nextTangent, up);
            var targetBank = Mathf.Clamp(-turn * 0.5f, -maxBankAngle, maxBankAngle);
            bank = deltaTime > 0f ? Mathf.Lerp(bank, targetBank, 1f - Mathf.Exp(-4f * deltaTime)) : targetBank;

            return Quaternion.LookRotation(forward.normalized, up) * Quaternion.AngleAxis(bank, Vector3.forward) * Quaternion.Euler(modelRotationOffset);
        }

        static void ConfigureBody(Rigidbody rb)
        {
            if (!rb) return;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }
}
