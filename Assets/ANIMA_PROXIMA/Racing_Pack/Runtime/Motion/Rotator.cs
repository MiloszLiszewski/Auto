using UnityEngine;

namespace AnimaProxima.RacingPack
{
    /// <summary>Continuous rotation at a constant speed (fan blades, windmill rotor).</summary>
    [AddComponentMenu("ANIMA PROXIMA/Racing Pack/Rotator")]
    public class Rotator : KinematicMotion
    {
        [Tooltip("Rotation speed in degrees per second. Negative values spin the other way.")]
        [SerializeField] float degreesPerSecond = 90f;

        [Tooltip("Start at a random angle so identical obstacles placed side by side don't move in lockstep.")]
        [SerializeField] bool randomizePhase = true;

        [Tooltip("Start angle in degrees (used when Randomize Phase is off).")]
        [SerializeField] float startAngle;

        float angle;

        public float DegreesPerSecond
        {
            get => degreesPerSecond;
            set => degreesPerSecond = value;
        }

        protected override void Awake()
        {
            base.Awake();
            angle = randomizePhase ? Random.Range(0f, 360f) : startAngle;
        }

        protected override float EvaluateAngle(float deltaTime)
        {
            angle = Mathf.Repeat(angle + degreesPerSecond * deltaTime, 360f);
            return angle;
        }
    }
}
