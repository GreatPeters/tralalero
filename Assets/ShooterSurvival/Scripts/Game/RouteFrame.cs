using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Authored straight-segment route for scenes without a HighwayRoute (RestStop). Distance 0 is the start.
public sealed class RoutePolyline : MonoBehaviour
{
    public Vector3[] points = System.Array.Empty<Vector3>();

    public float Length
    {
        get { float l = 0; for (int i = 0; i < points.Length - 1; i++) l += Vector3.Distance(points[i], points[i + 1]); return l; }
    }

    public void Sample(float distance, out Vector3 centre, out Vector3 forward)
    {
        centre = points.Length > 0 ? points[0] : Vector3.zero; forward = Vector3.forward;
        for (int i = 0; i < points.Length - 1; i++)
        {
            var delta = points[i + 1] - points[i]; float len = delta.magnitude;
            if (distance <= len || i == points.Length - 2) { forward = delta.normalized; centre = points[i] + forward * Mathf.Clamp(distance, 0, len); return; }
            distance -= len;
        }
    }

    // Nearest distance on a segment that runs along the given heading (parallel roads stay independent).
    public float NearestDistance(Vector3 position, Vector3 heading)
    {
        float best = float.MaxValue, result = 0, walked = 0;
        heading.y = 0;
        for (int i = 0; i < points.Length - 1; i++)
        {
            var delta = points[i + 1] - points[i]; float len = delta.magnitude; var dir = delta / Mathf.Max(.001f, len);
            if (heading.sqrMagnitude < .01f || Vector3.Dot(dir, heading.normalized) > .7f)
            {
                float t = Mathf.Clamp(Vector3.Dot(position - points[i], dir), 0, len);
                var p = points[i] + dir * t; float e = Vector3.ProjectOnPlane(position - p, Vector3.up).sqrMagnitude;
                if (e < best) { best = e; result = walked + t; }
            }
            walked += len;
        }
        return result;
    }
}

// One route query surface for rest-stop and highway gimmicks.
public static class RouteFrame
{
    private static HighwayRoute route;
    private static RoutePolyline polyline;
    private static int cachedFrame = -1;

    private static void Resolve()
    {
        if (cachedFrame == Time.frameCount && (route != null || polyline != null)) return;
        cachedFrame = Time.frameCount;
        if (route == null) route = Object.FindFirstObjectByType<HighwayRoute>();
        if (route == null && polyline == null) polyline = Object.FindFirstObjectByType<RoutePolyline>();
    }

    public static bool Available { get { Resolve(); return route != null || polyline != null; } }

    public static void Sample(float distance, out Vector3 centre, out Vector3 forward)
    {
        Resolve();
        if (route != null) { route.Sample(distance, false, out centre, out forward); return; }
        if (polyline != null) { polyline.Sample(distance, out centre, out forward); return; }
        centre = Vector3.zero; forward = Vector3.forward;
    }

    public static float PlayerDistance(PlayerScript player)
    {
        Resolve();
        if (route != null) return route.Distance;
        if (polyline != null && player != null) return polyline.NearestDistance(player.transform.position, player.transform.forward);
        return 0;
    }

    public static bool PlayerOnBypass { get { Resolve(); return route != null && route.OnBypass; } }

    public static float NearestDistance(Vector3 position)
    {
        Resolve();
        if (route != null) return route.NearestDistance(position);
        return polyline != null ? polyline.NearestDistance(position, Vector3.zero) : 0;
    }

    public static float Lane(Vector3 position, float distance)
    {
        Sample(distance, out var c, out var f);
        return Vector3.Dot(position - c, Vector3.Cross(Vector3.up, f));
    }
}
