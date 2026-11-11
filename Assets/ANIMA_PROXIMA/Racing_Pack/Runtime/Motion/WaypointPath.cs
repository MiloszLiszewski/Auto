using System.Collections.Generic;
using UnityEngine;

namespace AnimaProxima.RacingPack
{
    /// <summary>
    /// Smooth path through this object's child transforms (in hierarchy order), using centripetal
    /// Catmull-Rom interpolation. Sampled by distance through an arc-length table, so followers move
    /// at constant speed. Each waypoint's up vector is interpolated too, which lets a path run through loops.
    /// </summary>
    [AddComponentMenu("ANIMA PROXIMA/Racing Pack/Waypoint Path")]
    public class WaypointPath : MonoBehaviour
    {
        [Tooltip("Connect the last waypoint back to the first.")]
        [SerializeField] bool closed = true;

        [Tooltip("Samples per segment for the arc-length table. Higher = more accurate constant speed.")]
        [SerializeField, Range(4, 64)] int samplesPerSegment = 24;

        [SerializeField] Color gizmoColor = new Color(1f, 0.35f, 0.75f);

        readonly List<Transform> points = new List<Transform>();
        readonly List<float> sampleDistances = new List<float>();
        readonly List<float> sampleParams = new List<float>();

        public bool Closed => closed;
        public float Length { get; private set; }
        public int WaypointCount => points.Count;

        void Awake() => Rebuild();

        void OnValidate() => Rebuild();

        /// <summary>Re-reads child waypoints and rebuilds the arc-length table. Call after moving waypoints at runtime.</summary>
        public void Rebuild()
        {
            points.Clear();
            foreach (Transform child in transform) points.Add(child);

            sampleDistances.Clear();
            sampleParams.Clear();
            Length = 0f;
            var segments = SegmentCount;
            if (segments < 1) return;

            var previous = EvaluateParam(0f);
            sampleDistances.Add(0f);
            sampleParams.Add(0f);
            var steps = segments * samplesPerSegment;
            for (int i = 1; i <= steps; i++)
            {
                var t = (float)i / samplesPerSegment;
                var p = EvaluateParam(t);
                Length += Vector3.Distance(previous, p);
                sampleDistances.Add(Length);
                sampleParams.Add(t);
                previous = p;
            }
        }

        public Vector3 GetPointAtDistance(float distance) => EvaluateParam(DistanceToParam(distance));

        public Vector3 GetTangentAtDistance(float distance)
        {
            var t = DistanceToParam(distance);
            var tangent = EvaluateParam(t + 0.01f) - EvaluateParam(t - 0.01f);
            return tangent.sqrMagnitude > 1e-8f ? tangent.normalized : transform.forward;
        }

        /// <summary>Interpolated up vector of the waypoints around this distance.</summary>
        public Vector3 GetUpAtDistance(float distance)
        {
            if (points.Count == 0) return Vector3.up;
            var t = DistanceToParam(distance);
            var segment = Mathf.Min(Mathf.FloorToInt(t), SegmentCount - 1);
            var local = t - segment;
            var a = Point(segment).up;
            var b = Point(segment + 1).up;
            return Vector3.Slerp(a, b, Mathf.SmoothStep(0f, 1f, local)).normalized;
        }

        int SegmentCount => points.Count < 2 ? 0 : closed ? points.Count : points.Count - 1;

        Transform Point(int index)
        {
            var count = points.Count;
            if (closed) return points[((index % count) + count) % count];
            return points[Mathf.Clamp(index, 0, count - 1)];
        }

        float DistanceToParam(float distance)
        {
            if (sampleDistances.Count < 2 || Length <= 0f) return 0f;
            distance = closed ? Mathf.Repeat(distance, Length) : Mathf.Clamp(distance, 0f, Length);

            // Binary search the arc-length table.
            int lo = 0, hi = sampleDistances.Count - 1;
            while (hi - lo > 1)
            {
                var mid = (lo + hi) / 2;
                if (sampleDistances[mid] < distance) lo = mid; else hi = mid;
            }
            var span = sampleDistances[hi] - sampleDistances[lo];
            var f = span > 1e-6f ? (distance - sampleDistances[lo]) / span : 0f;
            return Mathf.Lerp(sampleParams[lo], sampleParams[hi], f);
        }

        Vector3 EvaluateParam(float t)
        {
            var segments = SegmentCount;
            if (segments < 1) return points.Count == 1 ? points[0].position : transform.position;
            t = closed ? Mathf.Repeat(t, segments) : Mathf.Clamp(t, 0f, segments);
            var segment = Mathf.Min(Mathf.FloorToInt(t), segments - 1);
            var u = t - segment;

            var p0 = Point(segment - 1).position;
            var p1 = Point(segment).position;
            var p2 = Point(segment + 1).position;
            var p3 = Point(segment + 2).position;
            if (!closed)
            {
                // Mirror virtual end points so open paths start and end on the first/last waypoint.
                if (segment == 0) p0 = p1 + (p1 - p2);
                if (segment == segments - 1) p3 = p2 + (p2 - p1);
            }
            return CentripetalCatmullRom(p0, p1, p2, p3, u);
        }

        static Vector3 CentripetalCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u)
        {
            const float alpha = 0.5f;
            float t0 = 0f;
            float t1 = t0 + Mathf.Max(Mathf.Pow(Vector3.Distance(p0, p1), alpha), 1e-4f);
            float t2 = t1 + Mathf.Max(Mathf.Pow(Vector3.Distance(p1, p2), alpha), 1e-4f);
            float t3 = t2 + Mathf.Max(Mathf.Pow(Vector3.Distance(p2, p3), alpha), 1e-4f);
            float t = Mathf.Lerp(t1, t2, u);

            var a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
            var a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
            var a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;
            var b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2;
            var b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3;
            return (t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2;
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying && points.Count != transform.childCount) Rebuild();
            if (SegmentCount < 1) return;

            Gizmos.color = gizmoColor;
            var steps = SegmentCount * 16;
            var previous = EvaluateParam(0f);
            for (int i = 1; i <= steps; i++)
            {
                var p = EvaluateParam((float)i / 16);
                Gizmos.DrawLine(previous, p);
                previous = p;
            }
            foreach (var point in points)
            {
                Gizmos.DrawSphere(point.position, 0.6f);
                Gizmos.DrawLine(point.position, point.position + point.up * 2f);
            }
        }
    }
}
