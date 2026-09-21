using System;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// The authored samples use chapter progress metres. Branches share that clock.
[DisallowMultipleComponent]
public sealed class HighwayRoute : MonoBehaviour
{
    [Serializable]
    public sealed class Fork
    {
        public float start, end, offset = -30;
        [NonSerialized] public bool decided, bypass, rewarded;
    }
    public Vector3[] centers = Array.Empty<Vector3>();
    public float length = 2340;
    public Fork[] forks = Array.Empty<Fork>();
    public ChapterPatternHUD hud;
    public float Distance { get; private set; }
    public bool OnBypass => Array.Exists(forks, f => f.decided && f.bypass && Distance >= f.start && Distance <= f.end);
    private float recoveryFraction = .10f;

    public void BeginRun()
    {
        Distance = 0;
        recoveryFraction = Setting("highwayBypassHeal", .10f, 0, .3f);
        foreach (var fork in forks) fork.decided = fork.bypass = fork.rewarded = false;
        hud?.Clear(this);
    }

    public void Advance(PlayerScript target, float step)
    {
        if (centers.Length < 2 || step <= 0) return;
        Sample(Distance, OnBypass, out var oldCenter, out var oldForward);
        float lane = Vector3.Dot(target.transform.position - oldCenter, Vector3.Cross(Vector3.up, oldForward));
        float next = Mathf.Min(length, Distance + step);
        foreach (var fork in forks)
        {
            if (!fork.decided && next >= fork.start)
            { fork.decided = true; fork.bypass = lane < 0; }
            if (!fork.decided && Distance >= fork.start - 60)
                hud?.Show(this, "갈림길", "왼쪽 초록선  회복 우회로     오른쪽 분홍선  본선", 1);
            if (fork.decided && Distance >= fork.start && Distance < fork.end)
                hud?.Show(this, fork.bypass ? "회복 우회로" : "고속도로 본선", fork.bypass ? "초록 유도선을 따라 합류하세요" : "차량 경고를 보고 빈 차로로 이동하세요", 1);
            if (fork.bypass && !fork.rewarded && next >= (fork.start + fork.end) * .5f)
            {
                fork.rewarded = true;
                target.currentHealth = RecoveredHealth(target.currentHealth, target.MaxHealth, recoveryFraction);
                target.UpdateHealth();
            }
            if (Distance < fork.end && next >= fork.end) hud?.Clear(this);
        }
        Distance = next;
        Sample(Distance, OnBypass, out var center, out var forward);
        target.ApplyContinuousRoutePose(center + Vector3.up * .12f, forward, lane);
    }

    public void Sample(float distance, bool bypass, out Vector3 center, out Vector3 forward)
    {
        center = Point(distance, bypass);
        forward = Vector3.ProjectOnPlane(Point(Mathf.Min(length, distance + .5f), bypass) - Point(Mathf.Max(0, distance - .5f), bypass), Vector3.up).normalized;
        if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
    }
    public Vector3 Point(float distance, bool bypass = false)
    {
        var center = MainPoint(distance);
        if (!bypass) return center;
        foreach (var fork in forks)
        {
            if (distance < fork.start || distance > fork.end) continue;
            var forward = (MainPoint(Mathf.Min(length, distance + .5f)) - MainPoint(Mathf.Max(0, distance - .5f))).normalized;
            center += Vector3.Cross(Vector3.up, forward).normalized * BranchOffset(distance, fork.start, fork.end, fork.offset);
        }
        return center;
    }
    private Vector3 MainPoint(float distance)
    {
        if (centers.Length == 0) return transform.position;
        float index = Mathf.Clamp01(distance / Mathf.Max(1, length)) * (centers.Length - 1);
        int first = Mathf.Min(Mathf.FloorToInt(index), centers.Length - 1);
        float t = index - first;
        var p1 = centers[first]; var p2 = centers[Mathf.Min(first + 1, centers.Length - 1)];
        var p0 = first > 0 ? centers[first - 1] : 2 * p1 - p2;
        var p3 = first + 2 < centers.Length ? centers[first + 2] : 2 * p2 - p1;
        return .5f * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t);
    }
    public static float BranchOffset(float d, float start, float end, float offset)
    {
        float t = Mathf.InverseLerp(start, end, d);
        float wave = Mathf.Sin(t * Mathf.PI);
        return offset * wave * wave;
    }
    public float NearestDistance(Vector3 position)
    {
        float best = float.PositiveInfinity, distance = 0;
        for (int i = 0; i < centers.Length - 1; i++)
        {
            var delta = centers[i + 1] - centers[i];
            float t = Mathf.Clamp01(Vector3.Dot(position - centers[i], delta) / Mathf.Max(.001f, delta.sqrMagnitude));
            float sqr = (position - Vector3.Lerp(centers[i], centers[i + 1], t)).sqrMagnitude;
            if (sqr < best) { best = sqr; distance = (i + t) * length / (centers.Length - 1); }
        }
        return distance;
    }
    public Vector3 ActivationPoint(Vector3 center, float lead)
    {
        float distance = NearestDistance(center);
        Sample(distance, false, out var point, out var forward);
        float lane = Vector3.Dot(center - point, Vector3.Cross(Vector3.up, forward));
        Sample(distance - lead, false, out point, out forward);
        return point + Vector3.Cross(Vector3.up, forward) * lane;
    }
    public static float Setting(string name, float fallback, float minimum, float maximum)
    {
        return EnvironmentVariableTables.TryGetFloat(name, out float value) && !float.IsNaN(value) && !float.IsInfinity(value)
            ? Mathf.Clamp(value, minimum, maximum) : fallback;
    }
    public static float RecoveredHealth(float current, float maximum, float fraction)
        => Mathf.Max(current, Mathf.Min(maximum, current + maximum * Mathf.Max(0, fraction)));
}
