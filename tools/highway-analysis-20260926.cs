using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Object = UnityEngine.Object;

// Observation-only cohort. Does not author or save any scene; ordinary movement and damage remain active.
public static class HighwayAnalysis20260926
{
    const string Root = "outputs/highway-analysis-2026-09-26";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo Move = typeof(PlayerScript).GetMethod("PlayerMove", Flags);
    static readonly FieldInfo Origin = typeof(PlayerScript).GetField("routeLaneOrigin", Flags), Right = typeof(PlayerScript).GetField("routeRight", Flags);
    static readonly string[] Plans = { "LL", "RR", "LR", "RL", "LL", "RR", "LR", "RL", "LL", "RR" };
    static PlayerScript player; static HighwayRoute route; static ChapterProgression chapter;
    static EnemyScript_space[] enemies; static BonusWallChoicePair[] bonuses; static Collider[] obstacles;
    static int attempt, frame = -1, moves; static bool active, started, ended;
    static float began, nextSample, nextShot, initialHp, initialAttack; static double readyAt, endedAt;
    static readonly List<object> damage = new(); static readonly List<object> events = new();
    static bool[] decided, rewarded; static string Folder => Root + "/runs/" + attempt.ToString("00");
    static string Json(object o) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { o });
    static float[] V(Vector3 p) => new[] { p.x, p.y, p.z };
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    static void Write(string path, object o) => File.WriteAllText(path, Json(o));

    public static object Inspect()
    {
        if (SceneManager.GetActiveScene().name != "HighWay") throw new InvalidOperationException("HighWay required");
        Directory.CreateDirectory(Root);
        var r = Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        var trees = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(x => x.enabled && (PathOf(x.transform).ToLowerInvariant().Contains("tree") || PathOf(x.transform).ToLowerInvariant().Contains("forest") || PathOf(x.transform).ToLowerInvariant().Contains("ridge"))).ToArray();
        var samples = new List<object>(); var overlaps = new List<object>();
        for (float d = 0; d <= r.length; d += 5)
        {
            r.Sample(d, false, out var main, out var forward); r.Sample(d, true, out var branch, out var bf);
            samples.Add(new { d, main = V(main), branch = V(branch) });
            if (!r.forks.Any(f => d >= f.start && d <= f.end)) continue;
            foreach (var tree in trees)
            {
                var b = tree.bounds;
                if (b.max.y < branch.y + .4f || b.min.y > branch.y + 12) continue;
                var q = new Vector3(branch.x, Mathf.Clamp(branch.y + 2, b.min.y, b.max.y), branch.z);
                if (b.SqrDistance(q) < 16) overlaps.Add(new { d, path = PathOf(tree.transform), center = V(b.center), size = V(b.size), distanceToBounds = Mathf.Sqrt(b.SqrDistance(q)) });
            }
        }
        var traffic = Object.FindFirstObjectByType<OncomingLaneTraffic>(FindObjectsInactive.Include);
        var report = new { scene = SceneManager.GetActiveScene().path, length = r.length, forks = r.forks.Select(f => new { f.start, f.end, f.offset, midpoint = (f.start + f.end) * .5f }), samples, recovery = HighwayRoute.Setting("highwayBypassHeal", .1f, 0, .3f), trees = trees.Select(t => new { path = PathOf(t.transform), center = V(t.bounds.center), size = V(t.bounds.size) }), branchTreeBounds = overlaps, traffic = traffic == null ? null : new { traffic.carSpeed, traffic.damageFraction, traffic.minInterval, traffic.maxInterval, traffic.lanes, traffic.laneDirections }, enemies = Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length };
        Write(Root + "/scene.json", report); return new { r.length, forks = r.forks.Length, trees = trees.Length, overlapSamples = overlaps.Count };
    }

    public static object Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Clean HighWay Edit Mode required");
        Directory.CreateDirectory(Root);
        var snap = ChapterPlaytestPreferences.SnapshotAt(Root + "/before-prefs.tsv");
        var keys = new[] { "TutorialDone" }.Concat(Enumerable.Range(1, 5).SelectMany(c => new[] { ChapterUpgradeService.LevelKey(c), ChapterUpgradeService.OwnedKey(c) }));
        File.WriteAllLines(Root + "/extra-prefs.tsv", keys.Select(k => k + "\t" + PlayerPrefs.HasKey(k) + "\t" + PlayerPrefs.GetInt(k)));
        Write(Root + "/conditions.json", new { timeScale = 3, currentSave = true, purchases = false, invulnerable = false, hazardsDisabled = false, teleports = false, plans = Plans, seedStart = 202609260, unity = Application.unityVersion, attackLevel = PlayerPrefs.GetInt("upgrade_lv_1"), healthLevel = PlayerPrefs.GetInt("upgrade_lv_2"), speedLevel = PlayerPrefs.GetInt("upgrade_lv_3") });
        SessionState.SetBool("NoryangjinMapTool.TestPower9999", false); SessionState.SetBool("NoryangjinMapTool.TestFastLateral", false);
        SessionState.SetFloat("NoryangjinMapTool.TestTimeScale", 3);
        return snap;
    }
    static void ResetPreferences()
    {
        foreach (var line in File.ReadAllLines(Root + "/before-prefs.tsv"))
        {
            var p = line.Split('\t');
            if (!bool.Parse(p[2])) PlayerPrefs.DeleteKey(p[0]);
            else if (p[1] == "int") PlayerPrefs.SetInt(p[0], int.Parse(p[3], CultureInfo.InvariantCulture));
            else if (p[1] == "float") PlayerPrefs.SetFloat(p[0], float.Parse(p[3], CultureInfo.InvariantCulture));
            else PlayerPrefs.SetString(p[0], System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p[3])));
        }
        foreach (var line in File.ReadAllLines(Root + "/extra-prefs.tsv")) { var p = line.Split('\t'); if (bool.Parse(p[1])) PlayerPrefs.SetInt(p[0], int.Parse(p[2])); else PlayerPrefs.DeleteKey(p[0]); }
        PlayerPrefs.Save();
    }
    public static object Restore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        active = false; EditorApplication.update -= Tick; ResetPreferences();
        return ChapterPlaytestPreferences.RestoreAt(Root + "/before-prefs.tsv");
    }
    public static object VerifyRestored()
    {
        var mismatches = new List<string>();
        foreach (var line in File.ReadAllLines(Root + "/before-prefs.tsv"))
        {
            var p = line.Split('\t');
            string value = p[1] == "int" ? PlayerPrefs.GetInt(p[0]).ToString(CultureInfo.InvariantCulture) : p[1] == "float" ? PlayerPrefs.GetFloat(p[0]).ToString("R", CultureInfo.InvariantCulture) : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(p[0])));
            if (PlayerPrefs.HasKey(p[0]) != bool.Parse(p[2]) || bool.Parse(p[2]) && value != p[3]) mismatches.Add(p[0]);
        }
        foreach (var line in File.ReadAllLines(Root + "/extra-prefs.tsv")) { var p = line.Split('\t'); if (PlayerPrefs.HasKey(p[0]) != bool.Parse(p[1]) || bool.Parse(p[1]) && PlayerPrefs.GetInt(p[0]).ToString() != p[2]) mismatches.Add(p[0]); }
        var result = new { mismatches, playing = EditorApplication.isPlaying, dirty = SceneManager.GetActiveScene().isDirty, scene = SceneManager.GetActiveScene().name, timeScale = Time.timeScale, restoredKeys = 77 };
        Write(Root + "/restored-state.json", result); return result;
    }
    public static object Begin()
    {
        if (!EditorApplication.isPlaying || active || Directory.Exists(Root + "/runs")) throw new InvalidOperationException("Fresh Play Mode and cohort required");
        attempt = 1; frame = -1; player = null; started = ended = false; active = true;
        EditorApplication.update += Tick; return Status();
    }
    public static object Status() => new { active, attempt, started, ended, distance = route != null ? route.Distance : 0, hp = player != null ? player.currentHealth : 0 };
    static void Tick()
    {
        if (!active || !EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isCompiling || frame == Time.frameCount) return;
        frame = Time.frameCount;
        try
        {
            if (ended)
            {
                if (EditorApplication.timeSinceStartup - endedAt < 3.5) return;
                if (attempt >= Plans.Length) { active = false; EditorApplication.update -= Tick; Time.timeScale = 1; EditorApplication.isPaused = true; Write(Root + "/complete.json", new { runs = attempt, done = true }); return; }
                player.DamageTaken -= OnDamage; ResetPreferences(); attempt++; player = null; started = ended = false;
                SceneManager.LoadScene("HighWay"); return;
            }
            if (player == null)
            {
                player = Object.FindFirstObjectByType<PlayerScript>(); route = Object.FindFirstObjectByType<HighwayRoute>(); chapter = Object.FindFirstObjectByType<ChapterProgression>();
                if (player == null || route == null) return; readyAt = EditorApplication.timeSinceStartup + 2; return;
            }
            if (!started)
            {
                if (EditorApplication.timeSinceStartup < readyAt) return;
                OpeningStoryUI.Instance?.Skip(); UnityEngine.Random.InitState(202609260 + attempt);
                Object.FindFirstObjectByType<EncounterPlacementController>()?.BeginNewRun();
                Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
                if (!TimeManager.isGameRunning) return;
                Directory.CreateDirectory(Folder); Time.timeScale = 3; began = Time.time; nextSample = 0; nextShot = 1; moves = 0; initialHp = player.currentHealth; initialAttack = player.currentDamage;
                decided = new bool[route.forks.Length]; rewarded = new bool[route.forks.Length]; damage.Clear(); events.Clear(); player.DamageTaken += OnDamage;
                enemies = Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include, FindObjectsSortMode.None); bonuses = Object.FindObjectsByType<BonusWallChoicePair>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                obstacles = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c => c.GetComponentInParent<HighwayHazard>() != null || c.GetComponentInParent<RoadPotholeHazard>() != null || c.GetComponentInParent<ObstacleStats>() != null).ToArray();
                File.WriteAllText(Folder + "/timeline.csv", "time,distance,hp,maxHp,lane,bypass,x,y,z\n"); started = true; return;
            }
            float elapsed = Time.time - began;
            if (elapsed >= nextSample)
            {
                nextSample = elapsed + .5f; route.Sample(route.Distance, route.OnBypass, out var c, out var f); var pos = player.transform.position;
                File.AppendAllText(Folder + "/timeline.csv", string.Join(",", elapsed, route.Distance, player.currentHealth, player.MaxHealth, Vector3.Dot(pos - c, Vector3.Cross(Vector3.up, f)), route.OnBypass, pos.x, pos.y, pos.z) + "\n");
                Write(Root + "/status.json", Status());
            }
            for (int i = 0; i < route.forks.Length; i++)
            {
                var f = route.forks[i];
                if (f.decided != decided[i]) { decided[i] = f.decided; events.Add(new { type = "fork", i, d = route.Distance, f.bypass, hp = player.currentHealth }); Shot("fork-" + (i + 1)); }
                if (f.rewarded != rewarded[i]) { rewarded[i] = f.rewarded; events.Add(new { type = "recovery", i, d = route.Distance, hp = player.currentHealth, maxHp = player.MaxHealth }); Shot("recovery-" + (i + 1)); }
            }
            if (elapsed >= nextShot) { nextShot = elapsed + (route.OnBypass ? 5 : 18); Shot("frame-" + elapsed.ToString("000")); }
            if (player.currentHealth <= 0 || !TimeManager.isGameRunning || elapsed >= 420)
            {
                ended = true; endedAt = EditorApplication.timeSinceStartup;
                Write(Folder + "/result.json", new { attempt, plan = Plans[attempt - 1], seed = 202609260 + attempt, elapsed, distance = route.Distance, routeLength = route.length, outcome = chapter != null && chapter.Completed ? "clear" : player.currentHealth <= 0 ? "death" : elapsed >= 420 ? "timeout" : "stopped", initialHp, initialAttack, finalHp = player.currentHealth, maxHp = player.MaxHealth, lastDamage = player.LastDamageCause.ToString(), kills = enemies.Count(e => e != null && e.IsDead), moves, forks = route.forks.Select(f => new { f.start, f.end, f.offset, f.decided, f.bypass, f.rewarded }), damage, events }); Shot("end"); return;
            }
            Steer();
        }
        catch (Exception e) { Directory.CreateDirectory(Root); File.WriteAllText(Root + "/error.txt", e.ToString()); active = false; EditorApplication.update -= Tick; EditorApplication.isPaused = true; }
    }
    static void OnDamage(float amount, PlayerDamageCause cause) => damage.Add(new { time = Time.time - began, d = route.Distance, amount, cause = cause.ToString(), remaining = player.currentHealth });
    static void Shot(string name) => ScreenCapture.CaptureScreenshot(Folder + "/" + name + ".png");
    static void Steer()
    {
        if (player.IsWorldYawTurnActive) return;
        var lateral = (Vector3)Right.GetValue(player); var origin = (Vector3)Origin.GetValue(player); var forward = player.transform.forward;
        float current = Vector3.Dot(player.transform.position - origin, lateral), desired = -1.7f;
        // Target nearby bonuses/enemies in their actual road frame, using normal movement.
        var enemy = enemies.Where(e => e != null && !e.IsDead && e.gameObject.activeInHierarchy).Where(e => { var v = e.transform.position - player.transform.position; float ahead = Vector3.Dot(v, forward); return ahead > 0 && ahead < 38 && Mathf.Abs(Vector3.Dot(v, lateral)) < 5 && Mathf.Abs(v.y) < 2; }).OrderBy(e => (e.transform.position - player.transform.position).sqrMagnitude).FirstOrDefault();
        if (enemy != null) { float d = route.NearestDistance(enemy.transform.position); route.Sample(d, route.OnBypass, out var c, out var f); desired = Vector3.Dot(enemy.transform.position - c, Vector3.Cross(Vector3.up, f)); }
        foreach (var pair in bonuses)
        {
            if (pair == null || pair.Selected != null || !pair.Left.gameObject.activeInHierarchy) continue;
            var mid = (pair.Left.transform.position + pair.Right.transform.position) * .5f; var delta = mid - player.transform.position; float ahead = Vector3.Dot(delta, forward);
            if (ahead < 0 || ahead > 22 || Mathf.Abs(Vector3.Dot(delta, lateral)) > 5) continue;
            var target = ((pair.Right.RolledStat ?? "").Contains("tung") || (pair.Right.RolledStat ?? "").Contains("boom")) ? pair.Right : pair.Left;
            desired = Vector3.Dot(target.transform.position - origin, lateral);
        }
        int selecting = Array.FindIndex(route.forks, f => !f.decided && route.Distance >= f.start - 55);
        if (selecting >= 0) desired = Plans[attempt - 1][Math.Min(selecting, 1)] == 'R' ? 1.8f : -1.8f;
        if (route.OnBypass) desired = .4f;
        var nearby = obstacles.Where(c => c != null && c.enabled && c.gameObject.activeInHierarchy).Concat(enemies.Where(e => e != null && !e.IsDead && e.gameObject.activeInHierarchy).Select(e => e.GetComponent<Collider>())).Where(c => c != null && c.enabled).Where(c => { var v = c.bounds.center - player.transform.position; float ahead = Vector3.Dot(v, forward); return ahead > -1 && ahead < (c.GetComponent<EnemyScript_space>() != null ? 10 : 18) && Mathf.Abs(Vector3.Dot(v, lateral)) < 7 && Mathf.Abs(v.y) < 3; }).ToArray();
        float Cost(float lane)
        {
            float cost = Mathf.Abs(lane - desired) * .6f + Mathf.Abs(lane - current) * .1f;
            if (selecting >= 0 && Mathf.Sign(lane) != Mathf.Sign(desired)) cost += 40;
            // Outer moving traffic lanes are avoided unless an actual nearby obstacle requires them.
            if (!route.OnBypass && Mathf.Abs(lane) > 2.1f) cost += 8;
            foreach (var c in nearby) { var b = c.bounds; float x = Vector3.Dot(b.center - origin, lateral), half = Mathf.Abs(lateral.x) * b.extents.x + Mathf.Abs(lateral.z) * b.extents.z; float overlap = half + .6f - Mathf.Abs(lane - x); if (overlap > 0) cost += 100 + overlap * 10; }
            return cost;
        }
        desired = Enumerable.Range(0, 33).Select(i => -4f + i * .25f).OrderBy(Cost).First();
        float error = desired - current;
        if (Mathf.Abs(error) > .04f) { Move.Invoke(player, new object[] { Mathf.Sign(error) * Mathf.Min(15, Mathf.Abs(error) * 12) }); moves++; }
    }
}
