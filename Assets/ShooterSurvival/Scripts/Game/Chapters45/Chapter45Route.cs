using System;
using UnityEngine;

// Progress metres are independent of height. A segment is a flat combat deck or
// a quiet authored connection; vertical discontinuities require an explicit lift.
[DisallowMultipleComponent]
public sealed class Chapter45Route : MonoBehaviour
{
    [Serializable]
    public sealed class Segment
    {
        public Vector3 start, end;
        public float length;
        public int floor;
        public string label;
        public float Length => Mathf.Max(.01f, length > 0 ? length : Vector3.Distance(start, end));
    }

    public Segment[] segments = Array.Empty<Segment>();
    [Min(0), Tooltip("Short tangent blend at connected same-floor corners; zero preserves authored sharp sampling.")]
    public float cornerBlendDistance;

    public float Length { get { float total = 0; foreach (var segment in segments) total += segment.Length; return total; } }
    public float SegmentStart(int index) { float result = 0; for (int i = 0; i < Mathf.Min(index, segments.Length); i++) result += segments[i].Length; return result; }
    public float SegmentEnd(int index) => SegmentStart(index) + segments[index].Length;
    public int SegmentAt(float distance)
    {
        float end = 0;
        for (int i = 0; i < segments.Length; i++) { end += segments[i].Length; if (distance < end - .001f || i == segments.Length - 1) return i; }
        return -1;
    }
    public void Sample(float distance, out Vector3 center, out Vector3 forward)
        => SampleSegment(SegmentAt(distance), distance, out center, out forward);
    public void SampleSegment(int index, float distance, out Vector3 center, out Vector3 forward)
    {
        if (index < 0 || index >= segments.Length) { center = transform.position; forward = transform.forward; return; }
        if (cornerBlendDistance > 0 &&
            (TryCorner(index - 1, distance, out center, out forward) || TryCorner(index, distance, out center, out forward))) return;
        var segment = segments[index];
        center = Vector3.Lerp(segment.start, segment.end, Mathf.Clamp01((distance - SegmentStart(index)) / segment.Length));
        forward = Vector3.ProjectOnPlane(segment.end - segment.start, Vector3.up).normalized;
        if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
    }
    private bool TryCorner(int incoming, float distance, out Vector3 center, out Vector3 forward)
    {
        center = Vector3.zero; forward = Vector3.forward;
        if (incoming < 0 || incoming + 1 >= segments.Length) return false;
        var a = segments[incoming]; var b = segments[incoming + 1];
        if (a.floor != b.floor || (a.end - b.start).sqrMagnitude > .0001f) return false;
        Vector3 before = (a.end - a.start).normalized, after = (b.end - b.start).normalized;
        float dot = Vector3.Dot(before, after);
        if (dot > .999f || dot < -.9f) return false;
        float width = Mathf.Min(cornerBlendDistance, Mathf.Min(a.Length, b.Length) * .35f);
        float boundary = SegmentEnd(incoming), local = distance - boundary;
        if (width <= .001f || Mathf.Abs(local) > width) return false;
        float t = Mathf.Clamp01((local + width) / (2 * width));
        Vector3 entry = a.end - before * width, exit = b.start + after * width;
        center = Vector3.Lerp(Vector3.Lerp(entry, a.end, t), Vector3.Lerp(a.end, exit, t), t);
        forward = Vector3.Lerp(a.end - entry, exit - a.end, t).normalized;
        return true;
    }
    public static float BranchOffset(float distance, float start, float end, float offset, float transition)
        => HighwayRoute.SeparatedBranchOffset(distance, start, end, offset, Mathf.Max(.1f, transition));
}
