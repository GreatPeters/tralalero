using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// A logging truck drives ahead in the centre lane, flashes a warning, and its logs fall off and roll back
// across the centre and right lanes. The left edge stays clear: the shark hugs left to dodge. While the
// logs roll, no new oncoming cars spawn and cars already behind a log brake for it, so no car ever meets a log.
public sealed class LogTruckSpill : MonoBehaviour
{
    public float triggerDistance = 1000f;
    public GameObject truckTemplate, logTemplate;
    public float truckLane = .4f; // centre: oncoming cars use the ±4.2 side lanes
    [Tooltip("Approach head-on instead of driving away. Enabled by the Highway three-lane scene.")]
    public bool oncoming;
    public float[] logLanes = { .4f, 3.6f, -.2f, 3.2f };
    public float truckLead = 70f, warnSeconds = 1.4f, logSpeed = 13f, stagger = .45f, damageFraction = .32f, hitRadius = 1.7f;
    public Transform[] laneWarnings = System.Array.Empty<Transform>();

    private enum Phase { Idle, Approach, Warn, Spill, Done }
    private Phase phase;
    private float t0, truckD;
    private Transform truck;
    private PlayerScript player;
    private bool truckHit;
    private sealed class Log { public Transform t; public float d, lane, start; public bool hit; }
    private readonly List<Log> logs = new();

    public static float LogDistance(float spawnD, float elapsed, float start, float speed) => spawnD - Mathf.Max(0, elapsed - start) * speed;
    public static float AdvanceTruck(float distance, float deltaTime, float speed, bool headOn)
        => distance + (headOn ? -1f : 1f) * Mathf.Max(0, deltaTime) * Mathf.Max(0, speed);

    private void Start() => player = FindFirstObjectByType<PlayerScript>();

    private void Update()
    {
        if (player == null || !TimeManager.isGameRunning || !RouteFrame.Available) return;
        float dt = Time.deltaTime * Mathf.Max(0, TimeManager.timeFactor);
        if (dt <= 0) return;
        float pd = RouteFrame.PlayerDistance(player);
        if (phase != Phase.Idle && pd < triggerDistance - 120) ResetForRun();
        switch (phase)
        {
            case Phase.Idle:
                if (pd < triggerDistance - 8 || pd > triggerDistance + 40 || RouteFrame.PlayerOnBypass) return;
                truck = Instantiate(truckTemplate, transform).transform; truck.gameObject.SetActive(true);
                if (oncoming) { truck.name = "IncomingLogTruck"; OncomingLaneTraffic.Blockers.Add(truck); }
                truckHit = false;
                truckD = pd + truckLead; t0 = Time.time; phase = Phase.Approach;
                // Oncoming cars already on the road pass the shark before the logs arrive; none spawn meanwhile.
                OncomingLaneTraffic.Suppress(11f);
                break;
            case Phase.Approach:
                truckD = AdvanceTruck(truckD, dt, 3.5f, oncoming);
                if (!oncoming) OncomingLaneTraffic.ClearAhead(truckD, 1f);
                if (Time.time - t0 > 1.2f) { phase = Phase.Warn; t0 = Time.time; GameAudioService.Play(GameSound.Warning); OncomingLaneTraffic.Suppress(9f); }
                break;
            case Phase.Warn:
                truckD = AdvanceTruck(truckD, dt, 3.5f, oncoming);
                if (!oncoming) OncomingLaneTraffic.ClearAhead(truckD, 1f);
                truck.localPosition += Vector3.up * Mathf.Sin(Time.time * 40) * .02f;
                if (Time.time - t0 > warnSeconds) { phase = Phase.Spill; t0 = Time.time; SpawnLogs(); }
                break;
            case Phase.Spill:
                truckD = AdvanceTruck(truckD, dt, 9f, oncoming);
                if (logs.TrueForAll(l => l.d < pd - 10) || Time.time - t0 > 10) phase = Phase.Done;
                break;
            case Phase.Done:
                truckD = AdvanceTruck(truckD, dt, 16f, oncoming);
                if (oncoming ? truckD - pd < -25 : truckD - pd > 160) Cleanup();
                break;
        }
        if (truck != null)
        {
            RouteFrame.Sample(truckD, out var c, out var f);
            truck.SetPositionAndRotation(c + Vector3.Cross(Vector3.up, f) * truckLane, Quaternion.LookRotation(oncoming ? -f : f));
            if (oncoming && !truckHit && !RouteFrame.PlayerOnBypass && Mathf.Abs(truckD - pd) < 5f && Mathf.Abs(RouteFrame.Lane(player.transform.position, pd) - truckLane) < 1.9f)
            { truckHit = true; player.ApplyDamage(player.MaxHealth * .22f, PlayerDamageCause.Traffic); }
        }
        bool warn = phase == Phase.Warn || phase == Phase.Spill;
        for (int i = 0; i < laneWarnings.Length; i++)
        {
            if (laneWarnings[i] == null) continue;
            laneWarnings[i].gameObject.SetActive(warn && Mathf.Repeat(Time.time * 4, 1) > .3f);
            if (!warn) continue;
            float lane = oncoming ? (i == 0 || logLanes.Length < 2 ? truckLane : logLanes[1]) : i == 0 ? .4f : 3.6f; float mid = pd + 18;
            RouteFrame.Sample(mid, out var c, out var f);
            laneWarnings[i].SetPositionAndRotation(c + Vector3.Cross(Vector3.up, f) * lane + Vector3.up * .06f, Quaternion.LookRotation(f));
            laneWarnings[i].localScale = new Vector3(oncoming ? 3.6f : 3f, .02f, 34f);
        }
        float elapsed = Time.time - t0;
        foreach (var log in logs)
        {
            if (log.t == null) continue;
            log.d = phase == Phase.Spill || phase == Phase.Done ? log.d - (elapsed >= log.start ? logSpeed * dt : 0) : log.d;
            log.t.gameObject.SetActive(elapsed >= log.start || phase == Phase.Done);
            RouteFrame.Sample(log.d, out var c, out var f);
            var right = Vector3.Cross(Vector3.up, f);
            log.t.position = c + right * log.lane + Vector3.up * .6f;
            log.t.rotation = Quaternion.LookRotation(right) * Quaternion.Euler(0, 0, 0);
            log.t.GetChild(0).Rotate(Vector3.forward, -logSpeed * 60f * dt, Space.Self);
            if (!log.hit && Mathf.Abs(log.d - pd) < 1.8f && Mathf.Abs(RouteFrame.Lane(player.transform.position, pd) - log.lane) < hitRadius)
            {
                log.hit = true; player.ApplyDamage(player.MaxHealth * damageFraction, PlayerDamageCause.Other);
                GameAudioService.PlayAt(GameSound.Explosion, log.t.position);
            }
        }
    }

    private void SpawnLogs()
    {
        for (int i = 0; i < logLanes.Length; i++)
        {
            var holder = new GameObject("RollingLog").transform; holder.SetParent(transform, false);
            var visual = Instantiate(logTemplate, holder); visual.SetActive(true);
            visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = logTemplate.transform.localRotation;
            logs.Add(new Log { t = holder, d = truckD + (oncoming ? 6 : -3), lane = logLanes[i], start = i * stagger });
            OncomingLaneTraffic.Blockers.Add(holder);
        }
    }

    private void Cleanup()
    {
        foreach (var l in logs) if (l.t != null) { OncomingLaneTraffic.Blockers.Remove(l.t); Destroy(l.t.gameObject); }
        logs.Clear();
        if (truck != null) { OncomingLaneTraffic.Blockers.Remove(truck); Destroy(truck.gameObject); }
        truck = null;
        foreach (var w in laneWarnings) if (w != null) w.gameObject.SetActive(false);
    }

    public void ResetForRun() { Cleanup(); phase = Phase.Idle; }
    private void OnDisable() => ResetForRun();
}
