using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

// Outside-pier big waves (노량진 개편 C). Each wave is warned by a crest racing in from the sea and a
// red flood zone; touching the flooded part while it crashes is instant death. Three patterns:
//   Side   - one half of the pier floods (chained quickly for double/triple crashes).
//   Gap    - both sides flood and only a narrow green strip stays dry.
//   Hunter - the flood zone follows the shark's lane and locks shortly before it crashes.
[DefaultExecutionOrder(500)]
public sealed class NoryangjinWaveEvent : NoryangjinRevampEvent
{
    public enum Kind { Side, Gap, Hunter }

    [Serializable] public sealed class Wave
    {
        public Kind kind;
        public float along;
        [Tooltip("Side: flooded half. Hunter: the sea side the crest comes from.")] public int side = -1;
        [Tooltip("Gap: centre of the dry strip as a fraction of the half width (-1..1).")] public float gapCenter;
        public float length = 11;
        [Tooltip("0 uses the event default.")] public float warnSeconds, crashSeconds;
        [Tooltip("Holds ZoneA, ZoneB (unit boxes scaled at run time), Skull and SafeTag.")] public Transform warning;
        public Transform water;
        [NonSerialized] public float began = -1, center;
        [NonSerialized] public bool coinsDropped, crashed, finished;
        [NonSerialized] public Vector3 scale;
        [NonSerialized] public readonly List<GameObject> fx = new();
        [NonSerialized] public readonly List<Transform> crests = new();
        [NonSerialized] public readonly List<Vector2> crestPath = new();
    }

    public Wave[] waves = Array.Empty<Wave>();
    public float halfWidth = 3.4f;
    public float warnSeconds = 1.2f, crashSeconds = .9f;
    [Tooltip("Legacy trigger distance; the wave now starts from the measured forward speed.")] public float leadMetres = 30;
    public int coinsPerStack = 3;
    public float gapHalfWidth = .8f, hunterHalfWidth = 1.7f, hunterLockSeconds = .45f;
    [Header("Effects (SrRubfish water set)")]
    public GameObject crestPrefab;
    public GameObject foamPrefab, splashPrefab, puddlePrefab;
    public float crestScale = 3.2f, foamScale = 2.4f;
    public float shakeAmount = .35f;

    private float clock, lastAlong = float.NaN, speed = 4.8f, shakeLeft;
    private Camera shakeCamera;
    private MaterialPropertyBlock tint;

    public override void ResetForRun()
    {
        base.ResetForRun(); clock = 0; lastAlong = float.NaN; shakeLeft = 0;
        foreach (var w in waves)
        {
            w.began = -1; w.coinsDropped = w.crashed = w.finished = false;
            Clear(w);
            if (w.warning) w.warning.gameObject.SetActive(false);
            if (w.water) { if (w.scale == Vector3.zero) w.scale = w.water.localScale; w.water.localScale = w.scale; w.water.gameObject.SetActive(false); }
        }
    }
    protected override void OnTriggered() { clock = 0; }

    private float Warn(Wave w) => w.warnSeconds > 0 ? w.warnSeconds : warnSeconds;
    private float Crash(Wave w) => w.crashSeconds > 0 ? w.crashSeconds : crashSeconds;

    // Lateral intervals that flood for a wave whose (hunter) centre is `center`.
    public static int Flooded(Kind kind, int side, float gapCenter, float center, float halfWidth, float gapHalf, float hunterHalf, Vector2[] into)
    {
        float edge = halfWidth + 1.2f;
        switch (kind)
        {
            case Kind.Gap:
            {
                float g = Mathf.Clamp(gapCenter, -1, 1) * (halfWidth - gapHalf);
                into[0] = new Vector2(-edge, g - gapHalf); into[1] = new Vector2(g + gapHalf, edge); return 2;
            }
            case Kind.Hunter:
                into[0] = new Vector2(center - hunterHalf, center + hunterHalf); return 1;
            default:
                into[0] = side < 0 ? new Vector2(-edge, .35f) : new Vector2(-.35f, edge); return 1;
        }
    }
    public static bool IsFlooded(float lateral, Vector2[] zones, int count)
    {
        for (int i = 0; i < count; i++) if (lateral >= zones[i].x && lateral <= zones[i].y) return true;
        return false;
    }

    private readonly Vector2[] zones = new Vector2[2];

    public bool IsCrashing(Wave w) => w != null && w.began >= 0 && !w.finished && clock - w.began >= Warn(w) && clock - w.began < Warn(w) + Crash(w);
    public float SecondsToCrash(Wave w) => w == null || w.began < 0 || w.finished ? float.PositiveInfinity : w.began + Warn(w) - clock;

    // Where a shark should stand to survive this wave (tests and review bots); NaN when not active.
    public float SafeLateral(Wave w)
    {
        if (w == null || w.began < 0 || w.finished) return float.NaN;
        return w.kind switch
        {
            Kind.Gap => Mathf.Clamp(w.gapCenter, -1, 1) * (halfWidth - gapHalfWidth),
            Kind.Hunter => w.center + (w.center > 0 ? -1 : 1) * (hunterHalfWidth + .9f),
            _ => -w.side * halfWidth * .6f
        };
    }

    protected override void OnTick(float dt)
    {
        clock += dt;
        Vector3 p = Player.transform.position;
        float playerAlong = -Ahead(p);
        if (!float.IsNaN(lastAlong) && dt > 0) speed = Mathf.Lerp(speed, Mathf.Clamp((playerAlong - lastAlong) / dt, 1.5f, 14f), .1f);
        lastAlong = playerAlong;
        float lateral = Lateral(p);
        foreach (var w in waves)
        {
            if (w.finished) continue;
            float warn = Warn(w), crash = Crash(w);
            if (w.began < 0)
            {
                // Start so the crash lands when the shark reaches the middle of the wave.
                if (w.along - playerAlong > speed * warn + .5f) continue;
                if (w.along - playerAlong < -w.length) { w.finished = true; continue; }
                Begin(w);
            }
            float age = clock - w.began;
            if (w.kind == Kind.Hunter && age < warn - hunterLockSeconds)
                w.center = Mathf.MoveTowards(w.center, Mathf.Clamp(lateral, -halfWidth + .6f, halfWidth - .6f), 6f * dt);
            int count = Flooded(w.kind, w.side, w.gapCenter, w.center, halfWidth, gapHalfWidth, hunterHalfWidth, zones);
            UpdateWarning(w, age, warn, count);
            UpdateCrests(w, age, warn, crash);
            if (!w.crashed && age >= warn)
            {
                w.crashed = true;
                CrashEffects(w, count);
            }
            if (age >= warn + crash + 1.6f) { w.finished = true; Clear(w); if (w.warning) w.warning.gameObject.SetActive(false); continue; }
            bool crashing = age >= warn && age < warn + crash;
            if (!crashing || Player.currentHealth <= 0) continue;
            if (Mathf.Abs(playerAlong - w.along) > w.length * .5f) continue;
            if (IsFlooded(lateral, zones, count))
            {
                Director.Timeline.Add($"wave death ({w.kind}) at {Director.Elapsed:F1}");
                Player.DieFromHazard(false, PlayerDamageCause.Wave);
            }
        }
    }

    private void Begin(Wave w)
    {
        w.began = clock; w.crashed = false;
        w.center = Mathf.Clamp(Lateral(Player.transform.position), -halfWidth + .6f, halfWidth - .6f);
        if (w.warning) w.warning.gameObject.SetActive(true);
        Director.Hud?.Publish(w.kind switch
        {
            Kind.Gap => "양쪽에서 큰 파도! 초록 틈으로!",
            Kind.Hunter => "파도가 따라온다! 막판에 피해!",
            _ => w.side < 0 ? "왼쪽에서 큰 파도! 오른쪽으로!" : "오른쪽에서 큰 파도! 왼쪽으로!"
        }, 1.4f);
        if (!w.coinsDropped && w.kind != Kind.Hunter)
        {
            w.coinsDropped = true;
            float safe = w.kind == Kind.Gap ? Mathf.Clamp(w.gapCenter, -1, 1) * (halfWidth - gapHalfWidth) : -w.side * halfWidth * .55f;
            for (int i = -1; i <= 1; i++) CoinPickup.Spawn(At(w.along + i * w.length * .3f, safe, .3f), CoinDropUtility.ApplyCoinBonus(coinsPerStack));
        }
        // Crests race in from the sea on every flooded side.
        float warn = Warn(w), crash = Crash(w);
        int count = Flooded(w.kind, w.side, w.gapCenter, w.center, halfWidth, gapHalfWidth, hunterHalfWidth, zones);
        if (crestPrefab == null) return;
        for (int i = 0; i < count; i++)
        {
            int from = w.kind == Kind.Side ? w.side : w.kind == Kind.Hunter ? w.side : (i == 0 ? -1 : 1);
            float stop = from < 0 ? zones[i].y : zones[i].x;
            var crest = Spawn(crestPrefab, At(w.along, from * (halfWidth + 16), 0), crestScale, warn + crash + .4f, (warn + crash) * .9f);
            if (crest == null) continue;
            crest.transform.rotation = Quaternion.LookRotation(transform.right * -from, Vector3.up);
            w.crests.Add(crest.transform); w.crestPath.Add(new Vector2(from, stop));
            w.fx.Add(crest);
        }
    }

    private void UpdateCrests(Wave w, float age, float warn, float crash)
    {
        for (int i = 0; i < w.crests.Count; i++)
        {
            var crest = w.crests[i]; if (crest == null) continue;
            float from = w.crestPath[i].x, stop = w.crestPath[i].y;
            // Hunter crests follow the locked zone.
            if (w.kind == Kind.Hunter) stop = from < 0 ? w.center + hunterHalfWidth : w.center - hunterHalfWidth;
            float lateral;
            if (age < warn)
            {
                float u = Mathf.Clamp01(age / warn);
                lateral = Mathf.Lerp(from * (halfWidth + 16), from * (halfWidth + .8f), u * u);
            }
            else lateral = Mathf.Lerp(from * (halfWidth + .8f), stop, Mathf.SmoothStep(0, 1, (age - warn) / Mathf.Max(.05f, crash * .7f)));
            crest.position = At(w.along + w.length * .2f, lateral, -.2f);
        }
    }

    private void UpdateWarning(Wave w, float age, float warn, int count)
    {
        if (!w.warning) return;
        float pulse = .55f + .45f * Mathf.Sin(age * (age < warn ? 18f : 6f));
        for (int i = 0; i < 2; i++)
        {
            var zone = w.warning.Find(i == 0 ? "ZoneA" : "ZoneB");
            if (zone == null) continue;
            bool on = i < count;
            zone.gameObject.SetActive(on);
            if (!on) continue;
            float lo = Mathf.Max(zones[i].x, -halfWidth), hi = Mathf.Min(zones[i].y, halfWidth);
            zone.position = At(w.along, (lo + hi) * .5f, .06f);
            zone.rotation = transform.rotation;
            zone.localScale = new Vector3(Mathf.Max(.2f, hi - lo), 1, w.length);
            foreach (var r in zone.GetComponentsInChildren<Renderer>())
            {
                tint ??= new MaterialPropertyBlock();
                r.GetPropertyBlock(tint);
                var baseColor = r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.red;
                tint.SetColor("_BaseColor", new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (age < warn ? pulse : 1)));
                r.SetPropertyBlock(tint);
            }
        }
        // Labels sit at the far end of the zone so they never cover the shark while it dodges.
        var skull = w.warning.Find("Skull");
        if (skull != null && count > 0) skull.position = At(w.along + w.length * .5f, (Mathf.Max(zones[0].x, -halfWidth) + Mathf.Min(zones[0].y, halfWidth)) * .5f, 2.2f);
        var safe = w.warning.Find("SafeTag");
        if (safe != null)
        {
            float s = w.kind == Kind.Gap ? Mathf.Clamp(w.gapCenter, -1, 1) * (halfWidth - gapHalfWidth) : w.kind == Kind.Side ? -w.side * halfWidth * .6f : 99;
            safe.gameObject.SetActive(s < 50 && age < warn);
            if (s < 50) safe.position = At(w.along + w.length * .45f, s, 1.6f);
        }
    }

    private void CrashEffects(Wave w, int count)
    {
        shakeLeft = .45f;
        Director.Timeline.Add($"wave crash ({w.kind}) at {Director.Elapsed:F1}");
        for (int i = 0; i < count; i++)
        {
            float lo = Mathf.Max(zones[i].x, -halfWidth), hi = Mathf.Min(zones[i].y, halfWidth);
            float mid = (lo + hi) * .5f;
            // Burst in the front half of the zone: the crash reads clearly but never hides the shark.
            foreach (float k in new[] { .12f, .42f })
            {
                if (foamPrefab != null) w.fx.Add(Spawn(foamPrefab, At(w.along + k * w.length, mid, 0), foamScale, 1.3f, 0));
                if (splashPrefab != null) w.fx.Add(Spawn(splashPrefab, At(w.along + (k + .15f) * w.length, mid, .2f), foamScale * .9f, 1.4f, 0));
            }
            if (puddlePrefab != null) w.fx.Add(Spawn(puddlePrefab, At(w.along + w.length * .25f, mid, .05f), Mathf.Max(1.4f, hi - lo) * .8f, 2.2f, 0));
        }
    }

    private GameObject Spawn(GameObject prefab, Vector3 at, float scale, float life, float simulationSeconds)
    {
        if (prefab == null) return null;
        var go = Instantiate(prefab, at, transform.rotation);
        go.transform.localScale = Vector3.one * scale;
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main; main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            // Stretch the one-second authored crest over the whole warning + crash.
            if (simulationSeconds > 0) main.simulationSpeed = Mathf.Clamp(1.05f / simulationSeconds, .25f, 2f);
        }
        Destroy(go, life);
        return go;
    }

    private static void Clear(Wave w)
    {
        foreach (var go in w.fx) if (go != null) Destroy(go);
        w.fx.Clear(); w.crests.Clear(); w.crestPath.Clear();
    }

    private void LateUpdate()
    {
        if (shakeLeft <= 0) return;
        shakeLeft -= Time.deltaTime;
        if (shakeCamera == null) shakeCamera = Camera.main;
        if (shakeCamera == null) return;
        float k = Mathf.Clamp01(shakeLeft / .45f) * shakeAmount;
        shakeCamera.transform.position += new Vector3(Mathf.PerlinNoise(Time.time * 31, 0) - .5f, Mathf.PerlinNoise(0, Time.time * 29) - .5f, 0) * 2 * k;
    }
}
