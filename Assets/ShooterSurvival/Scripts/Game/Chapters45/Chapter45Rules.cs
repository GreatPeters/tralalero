using UnityEngine;

public static class Chapter45Rules
{
    public static float CaptainHealthAfterHit(float health, float maximum, int phase, float damage, out int nextPhase)
    {
        nextPhase = Mathf.Clamp(phase, 0, 2);
        if (health <= 0 || damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage)) return health;
        float next = Mathf.Max(0, health - damage);
        float threshold = maximum * (2 - nextPhase) / 3f;
        if (nextPhase < 2 && health > threshold && next <= threshold)
        { next = threshold; nextPhase++; }
        return Mathf.Min(health, next);
    }
    public static bool SameDeck(int launchDeck, float launchHeight, int targetDeck, float targetHeight)
        => launchDeck < 0 || (targetDeck >= 0 ? targetDeck == launchDeck : Mathf.Abs(targetHeight - launchHeight) <= 4f);
    public static Vector3 LiftPosition(Vector3 from, Vector3 to, float elapsed, float duration)
        => Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, duration <= 0 ? 1 : Mathf.Clamp01(elapsed / duration)));
    // Flat comb plates at both ends and a constant-speed sloped run between them.
    public static Vector3 EscalatorPosition(Vector3 from, Vector3 to, float progress)
    {
        float t = Mathf.Clamp01(progress);
        float horizontal = Vector3.ProjectOnPlane(to - from, Vector3.up).magnitude;
        float flatFraction = horizontal > .01f ? Mathf.Min(.2f, 2f / horizontal) : 0;
        var point = Vector3.Lerp(from, to, t);
        point.y = Mathf.Lerp(from.y, to.y, Mathf.InverseLerp(flatFraction, 1 - flatFraction, t));
        return point;
    }
    public static float HelperProgress(float distance, float step, float playerDistance, float deckEnd)
        => Mathf.Min(deckEnd, Mathf.Max(distance, Mathf.Min(distance + Mathf.Max(0, step), playerDistance + 12)));
}
