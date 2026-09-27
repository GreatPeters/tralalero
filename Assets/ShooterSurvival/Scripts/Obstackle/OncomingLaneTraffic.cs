using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Side-lane traffic replacing the old orange funnel rails. Each lane has a direction: -1 lanes come
// head-on (at an encounter row a car is timed to pass beside the enemies as the shark does, a moving
// wall); +1 lanes carry cars the shark's way that spawn behind it and overtake, queueing behind the
// shark while it sits in their lane. Cars (now and then a tow truck) never overlap: each keeps a gap behind the car ahead
// in its lane and brakes for deer or rolling logs in its path. A falling-log event suppresses spawning.
// Contact costs a share of max health once per car. The side lanes must stay free of static props
// (tools/install-traffic-clearance.cs enforces that for authored scenes).
public sealed class OncomingLaneTraffic : MonoBehaviour
{
    public GameObject[] carTemplates = System.Array.Empty<GameObject>();
    public GameObject towTruckTemplate;
    [Range(0, 1)] public float towTruckChance = .12f;
    public Vector2[] activeRanges = { new(40, 2300) };
    public float[] lanes = { -4.2f, 4.2f };
    [Tooltip("Per lane: +1 drives the shark's way (overtakes from behind), -1 comes head-on.")]
    public float[] laneDirections = { -1f, 1f };
    public float behindSpawn = 30f;
    public float carSpeed = 17f, spawnAhead = 95f, minInterval = 3.4f, maxInterval = 5.8f, damageFraction = .22f, hitRadius = 1.9f;
    public float rowLead = 60f;
    public float followGap = 2.5f;
    [Tooltip("Reserve at least one lane between head-on arrival groups. Enabled for the three-lane Highway.")]
    public bool preserveEscapeLane;
    public float escapeWindowSeconds = 1.5f;
    [Tooltip("Keep moving cars out of the forced combat passage when they meet the player.")]
    public bool protectEncounterPassages;

    private sealed class Car { public Transform t; public float d, lane, speed, half, dir; public bool hit; }
    private readonly List<Car> cars = new();
    private PlayerScript player;
    private float nextSpawn, lastPlayerD, playerSpeed = 7.8f;
    private HighwayEncounterRow[] rows = System.Array.Empty<HighwayEncounterRow>();
    private HighwayHazard[] roadblocks = System.Array.Empty<HighwayHazard>();
    private float[] rowDistance = System.Array.Empty<float>();
    private readonly HashSet<int> rowsServed = new();
    private static float suppressedUntil = -1;
    private readonly List<(Vector3 position, float lateral, float length)> blockers = new();

    // Moving things cars must not drive through (deer, logs). Registered by their owners while active.
    public static readonly HashSet<Transform> Blockers = new();

    private static float clearUntilD = float.MinValue, clearUntilTime = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        suppressedUntil = clearUntilTime = -1;
        clearUntilD = float.MinValue;
        Blockers.Clear();
    }

    // Called by falling-log events: no new cars while the logs roll.
    public static void Suppress(float seconds) => suppressedUntil = Mathf.Max(suppressedUntil, Time.time + seconds);
    // Same-direction cars between the shark and this route distance hurry past it (the log truck) before the
    // spill, and cars still behind the shark hold back until the logs are gone.
    public static void ClearAhead(float untilDistance, float seconds) { clearUntilD = untilDistance; clearUntilTime = Time.time + seconds; }
    private static bool Clearing => Time.time < clearUntilTime;
    public static bool Suppressed => Time.time < suppressedUntil;

    public static bool InRanges(Vector2[] ranges, float d)
    {
        foreach (var r in ranges) if (d >= r.x && d <= r.y) return true;
        return false;
    }

    // Distance ahead of the row at which a car must spawn to pass the row exactly when the shark does.
    public static float SyncedSpawnDistance(float rowD, float playerD, float playerSpeed, float carSpeed)
    {
        float t = Mathf.Max(0, rowD - playerD) / Mathf.Max(.1f, playerSpeed);
        return rowD + carSpeed * t;
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerScript>();
        rows = FindObjectsByType<HighwayEncounterRow>(FindObjectsSortMode.None);
        roadblocks = FindObjectsByType<HighwayHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        rowDistance = new float[rows.Length];
        for (int i = 0; i < rows.Length; i++)
            rowDistance[i] = rows[i].routeDistance > 1 ? rows[i].routeDistance : ProjectDistance(rows[i].transform.position, rows[i].transform.forward);
        ResetForRun();
    }

    private static float ProjectDistance(Vector3 p, Vector3 heading)
    {
        var poly = FindFirstObjectByType<RoutePolyline>();
        if (poly != null) return poly.NearestDistance(p, heading);
        var route = FindFirstObjectByType<HighwayRoute>();
        return route != null ? route.NearestDistance(p) : 0;
    }

    private void Update()
    {
        if (player == null || !RouteFrame.Available) return;
        if (!TimeManager.isGameRunning) return;
        float dt = Time.deltaTime * Mathf.Max(0, TimeManager.timeFactor);
        if (dt <= 0) return;
        float pd = RouteFrame.PlayerDistance(player);
        if (pd < lastPlayerD - 30) ResetForRun();
        if (dt > 0) playerSpeed = Mathf.Lerp(playerSpeed, Mathf.Clamp((pd - lastPlayerD) / dt, 0, 20), .05f);
        lastPlayerD = pd;
        bool quiet = Suppressed || RouteFrame.PlayerOnBypass || player.IsStationaryCombat;

        if (!quiet)
        {
            // Encounter rows: one car per side, timed to pass beside the enemy pair with the shark.
            for (int i = 0; i < rows.Length; i++)
            {
                float ahead = rowDistance[i] - pd;
                if (rowsServed.Contains(i) || ahead > rowLead || ahead < rowLead - 8 || !rows[i].HasLivingOpponent) continue;
                rowsServed.Add(i);
                float spawn = SyncedSpawnDistance(rowDistance[i], pd, playerSpeed, carSpeed);
                for (int l = 0; l < lanes.Length; l++) if (Direction(l) < 0 && InRanges(activeRanges, rowDistance[i])) Spawn(spawn, l);
            }
            if (Time.time >= nextSpawn)
            {
                nextSpawn = Time.time + Random.Range(minInterval, maxInterval);
                int l = Random.Range(0, lanes.Length);
                float spawnD = Direction(l) < 0 ? pd + spawnAhead : pd - behindSpawn;
                if (InRanges(activeRanges, pd) && InRanges(activeRanges, spawnD)) Spawn(spawnD, l);
            }
        }

        CollectBlockers();
        // Front-most car of each lane (in its travel direction) first, so followers see the updated leader.
        cars.Sort((a, b) => (b.d * b.dir).CompareTo(a.d * a.dir));
        float playerLane = RouteFrame.Lane(player.transform.position, pd);
        for (int i = 0; i < cars.Count; i++)
        {
            var c = cars[i];
            float speed = c.speed;
            if (c.dir > 0 && Clearing && c.d > pd - 6 && c.d < clearUntilD + 15) speed *= 2.4f;
            float next = c.d + c.dir * speed * dt;
            if (c.dir > 0 && (Suppressed || Clearing) && c.d <= pd - 6) next = Mathf.Min(next, c.d);
            var leader = Leader(i);
            if (leader != null) next = c.dir < 0 ? Mathf.Max(next, leader.d + leader.half + c.half + followGap) : Mathf.Min(next, leader.d - leader.half - c.half - followGap);
            // Same-direction cars never rear-end the shark: they wait out of shot behind it while it holds their lane.
            if (c.dir > 0 && c.d < pd && Mathf.Abs(playerLane - c.lane) < hitRadius) next = Mathf.Min(next, Mathf.Max(c.d, pd - c.half - 11f));
            RouteFrame.Sample(next, out var centre, out var forward);
            var right = Vector3.Cross(Vector3.up, forward);
            if (!Blocked(centre + right * c.lane, forward * c.dir, c.half)) c.d = c.dir < 0 ? Mathf.Min(c.d, next) : Mathf.Max(c.d, next);
            RouteFrame.Sample(c.d, out centre, out forward);
            right = Vector3.Cross(Vector3.up, forward);
            c.t.SetPositionAndRotation(centre + right * c.lane, Quaternion.LookRotation(forward * c.dir, Vector3.up));
            float gap = c.d - pd;
            if (!c.hit && Mathf.Abs(gap) < (c.dir < 0 ? 2.8f : c.half))
            {
                if (Mathf.Abs(playerLane - c.lane) < hitRadius)
                {
                    c.hit = true;
                    player.ApplyDamage(player.MaxHealth * damageFraction, PlayerDamageCause.Traffic);
                    GameAudioService.PlayAt(GameSound.Explosion, c.t.position);
                }
            }
        }
        for (int i = cars.Count - 1; i >= 0; i--)
            if ((cars[i].dir < 0 ? cars[i].d - pd < -18 : cars[i].d - pd > spawnAhead + 20) || !InRanges(activeRanges, cars[i].d)) Recycle(i);
    }

    private float Direction(int lane) => laneDirections != null && lane < laneDirections.Length && laneDirections[lane] > 0 ? 1f : -1f;

    private Car Leader(int index)
    {
        for (int j = index - 1; j >= 0; j--) if (Mathf.Abs(cars[j].lane - cars[index].lane) < 1f) return cars[j];
        return null;
    }

    public static bool GapFree(float d, float half, float lane, IReadOnlyList<(float d, float lane, float half)> others, float gap)
    {
        foreach (var o in others) if (Mathf.Abs(o.lane - lane) < 1f && Mathf.Abs(o.d - d) < o.half + half + gap) return false;
        return true;
    }

    public static float MeetingDistance(float playerD, float carD, float playerSpeed, float carSpeed)
    {
        float speed = Mathf.Max(0, playerSpeed);
        return playerD + Mathf.Max(0, carD - playerD) * speed / Mathf.Max(.01f, speed + Mathf.Max(0, carSpeed));
    }

    public static bool KeepsEscapeLane(float lane, float arrival, IReadOnlyList<Vector2> reservations, int laneCount, float window)
    {
        var occupied = new HashSet<float> { lane };
        foreach (var other in reservations)
            if (Mathf.Abs(other.y - arrival) <= Mathf.Max(0, window)) occupied.Add(other.x);
        return occupied.Count < laneCount;
    }

    public static bool CrossesCombatPassage(float meetingD, float lane, float rowD, float rowLane, float rowHalfWidth, float radius)
        => meetingD >= rowD - 24 && meetingD <= rowD + 9 && Mathf.Abs(lane - rowLane) < rowHalfWidth + radius;

    public static bool SpawnCrossesRoadblock(float playerD, float spawnD, float lane, float obstacleD, float obstacleLane, float obstacleHalfWidth, float radius)
        => obstacleD >= playerD - 30 && obstacleD <= spawnD + 10 && Mathf.Abs(lane - obstacleLane) <= obstacleHalfWidth + radius;

    private bool RoadblockInPath(float spawnD, float lane)
    {
        // Workbook run initialization can reactivate authored hazards. Inspect their live state,
        // not just their saved activeSelf flag, before sending a car through that lane.
        foreach (var hazard in roadblocks)
        {
            if (hazard == null || !hazard.isActiveAndEnabled || hazard.Broken) continue;
            var stats = hazard.GetComponent<ObstacleStats>();
            if (stats == null || stats.obstaclePattern != ObstaclePattern.HighwayRoadblock) continue;
            var collider = hazard.GetComponent<Collider>();
            if (collider == null || !collider.enabled) continue;
            var bounds = collider.bounds;
            float d = RouteFrame.NearestDistance(bounds.center);
            RouteFrame.Sample(d, out var centre, out var forward);
            var right = Vector3.Cross(Vector3.up, forward);
            float obstacleLane = Vector3.Dot(bounds.center - centre, right);
            float halfWidth = Mathf.Abs(right.x) * bounds.extents.x + Mathf.Abs(right.z) * bounds.extents.z;
            if (SpawnCrossesRoadblock(lastPlayerD, spawnD, lane, d, obstacleLane, halfWidth, hitRadius)) return true;
        }
        return false;
    }

    private void CollectBlockers()
    {
        blockers.Clear();
        Blockers.RemoveWhere(b => b == null);
        foreach (var b in Blockers)
            if (b.gameObject.activeInHierarchy)
            {
                // The oncoming spill truck is a long vehicle, not a one-metre rolling log.
                bool truck = b.name == "IncomingLogTruck";
                blockers.Add((b.position, truck ? 2.2f : b.name.Contains("Log") ? 3f : 2.2f, truck ? 7f : b.name.Contains("Log") ? 1f : 1.2f));
            }
    }

    // A car stops rather than drive through a deer or log in front of it.
    private bool Blocked(Vector3 position, Vector3 heading, float half)
    {
        foreach (var b in blockers)
        {
            var v = b.position - position; v.y = 0;
            float along = Vector3.Dot(v, heading);
            if (along > -half && along < half + b.length + 1.5f && Mathf.Abs(Vector3.Dot(v, Vector3.Cross(Vector3.up, heading))) < b.lateral) return true;
        }
        return false;
    }

    private void Spawn(float d, int laneIndex)
    {
        float lane = lanes[laneIndex], dir = Direction(laneIndex);
        if (carTemplates.Length == 0) return;
        var template = towTruckTemplate != null && Random.value < towTruckChance ? towTruckTemplate : carTemplates[Random.Range(0, carTemplates.Length)];
        bool tow = template == towTruckTemplate;
        bool reserveArrival = dir < 0 && (protectEncounterPassages || preserveEscapeLane);
        float speed = carSpeed * (tow ? 1.25f : reserveArrival ? Random.Range(.9f, 1.1f) : 1f);
        if (dir < 0 && protectEncounterPassages)
        {
            if (RoadblockInPath(d, lane)) return;
            float meeting = MeetingDistance(lastPlayerD, d, playerSpeed, speed);
            for (int i = 0; i < rows.Length; i++)
                if (rows[i] != null && rows[i].isActiveAndEnabled && CrossesCombatPassage(meeting, lane, rowDistance[i], rows[i].laneCenter, rows[i].halfWidth, hitRadius)) return;
        }
        if (dir < 0 && preserveEscapeLane)
        {
            float arrival = Mathf.Max(0, d - lastPlayerD) / Mathf.Max(.01f, speed + playerSpeed);
            var reservations = new List<Vector2>();
            foreach (var c in cars)
                if (c.dir < 0 && c.d >= lastPlayerD - c.half)
                    reservations.Add(new Vector2(c.lane, Mathf.Max(0, c.d - lastPlayerD) / Mathf.Max(.01f, c.speed + playerSpeed)));
            if (!KeepsEscapeLane(lane, arrival, reservations, lanes.Length, escapeWindowSeconds)) return;
        }
        var occupied = new List<(float d, float lane, float half)>(cars.Count);
        foreach (var c in cars) occupied.Add((c.d, c.lane, c.half));
        if (preserveEscapeLane && !halfLengths.ContainsKey(template)) halfLengths[template] = TemplateHalfLength(template);
        if (!GapFree(d, halfLengths.TryGetValue(template, out var known) ? known : 3f, lane, occupied, followGap + 4f)) return;
        var t = Instantiate(template, transform); t.SetActive(true);
        float half = HalfLength(template, t);
        if (tow) GameAudioService.Play(GameSound.Warning);
        if (!reserveArrival) speed = carSpeed * (tow ? 1.25f : Random.Range(.9f, 1.1f));
        cars.Add(new Car { t = t.transform, d = d, lane = lane, half = half, dir = dir, speed = speed });
    }

    // Inactive templates have empty Renderer.bounds. Measure their source geometry before the
    // first bus/truck rental too, otherwise the old 3m fallback can admit an overlapping spawn.
    public static float TemplateHalfLength(GameObject template)
    {
        float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
        foreach (var filter in template.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            var bounds = filter.sharedMesh.bounds;
            var matrix = template.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                float z = matrix.MultiplyPoint3x4(corner).z * Mathf.Abs(template.transform.lossyScale.z);
                minimum = Mathf.Min(minimum, z); maximum = Mathf.Max(maximum, z);
            }
        }
        return float.IsInfinity(minimum) ? 3f : Mathf.Max(.5f, (maximum - minimum) * .5f);
    }

    private readonly Dictionary<GameObject, float> halfLengths = new();
    // Measured once per template from a live instance (inactive templates report empty renderer bounds).
    private float HalfLength(GameObject template, GameObject instance)
    {
        if (halfLengths.TryGetValue(template, out var h)) return h;
        h = 3f; instance.transform.rotation = Quaternion.identity;
        var rs = instance.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); h = b.extents.z; }
        return halfLengths[template] = h;
    }

    private void Recycle(int i)
    {
        if (cars[i].t != null) Destroy(cars[i].t.gameObject);
        cars.RemoveAt(i);
    }

    public void ResetForRun()
    {
        for (int i = cars.Count - 1; i >= 0; i--) Recycle(i);
        rowsServed.Clear(); blockers.Clear();
        nextSpawn = Time.time + 2f;
        lastPlayerD = 0; playerSpeed = 7.8f;
        suppressedUntil = clearUntilTime = -1;
        clearUntilD = float.MinValue;
    }
}
