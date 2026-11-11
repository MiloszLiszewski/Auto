using UnityEngine;

namespace AnimaProxima.RacingPack
{
    /// <summary>
    /// Base class for animated obstacle parts that must physically push other bodies (cars, cones).
    /// Drives a kinematic <see cref="Rigidbody"/> with <see cref="Rigidbody.MoveRotation"/> in FixedUpdate,
    /// so contacts are resolved by the physics engine instead of the part tunnelling through colliders.
    /// The rotation is applied on top of the rest pose captured in Awake, relative to the parent,
    /// so the obstacle can be placed, rotated and moved freely in a scene.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public abstract class KinematicMotion : MonoBehaviour
    {
        [Tooltip("Rotation axis in this object's local space (rest pose).")]
        [SerializeField] protected Vector3 axis = Vector3.right;

        Rigidbody body;
        Quaternion restLocalRotation;

        public Vector3 Axis
        {
            get => axis;
            set => axis = value;
        }

        /// <summary>Angle in degrees around <see cref="Axis"/>, measured from the rest pose.</summary>
        protected abstract float EvaluateAngle(float deltaTime);

        protected virtual void Reset() => ConfigureBody(GetComponent<Rigidbody>());

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody>();
            ConfigureBody(body);
            restLocalRotation = transform.localRotation;
        }

        void FixedUpdate()
        {
            var localAxis = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector3.right;
            var local = restLocalRotation * Quaternion.AngleAxis(EvaluateAngle(Time.fixedDeltaTime), localAxis);
            var parent = transform.parent;
            body.MoveRotation(parent ? parent.rotation * local : local);
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
