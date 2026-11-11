using UnityEngine;

namespace AnimaProxima.RacingPack
{
    /// <summary>Sine-eased back-and-forth swing around the rest pose (hammers).</summary>
    [AddComponentMenu("ANIMA PROXIMA/Racing Pack/Pendulum")]
    public class Pendulum : KinematicMotion
    {
        [Tooltip("Maximum swing to each side, in degrees.")]
        [SerializeField, Range(0f, 180f)] float amplitudeDegrees = 40f;

        [Tooltip("Time in seconds for one full swing (there and back).")]
        [SerializeField, Min(0.05f)] float period = 2.4f;

        [Tooltip("Offset into the cycle, in degrees (0-360). Stagger this across a row of hammers.")]
        [SerializeField, Range(0f, 360f)] float phaseDegrees;

        float time;

        public float AmplitudeDegrees
        {
            get => amplitudeDegrees;
            set => amplitudeDegrees = Mathf.Clamp(value, 0f, 180f);
        }

        public float Period
        {
            get => period;
            set => period = Mathf.Max(0.05f, value);
        }

        public float PhaseDegrees
        {
            get => phaseDegrees;
            set => phaseDegrees = Mathf.Repeat(value, 360f);
        }

        protected override float EvaluateAngle(float deltaTime)
        {
            time += deltaTime;
            var cycle = time / period * 2f * Mathf.PI + phaseDegrees * Mathf.Deg2Rad;
            return amplitudeDegrees * Mathf.Sin(cycle);
        }
    }
}
