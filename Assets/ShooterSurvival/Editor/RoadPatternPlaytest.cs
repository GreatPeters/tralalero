using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class RoadPatternPlaytest
{
    private const string Key = "RoadPattern.QA";
    [Serializable] private sealed class Config { public string folder, scene; public bool endurance; public float lane, limit; public int frame, attackLevel, healthLevel; }
    private static Config config;
    private static PlayerScript player;
    private static HighwayRoute route;
    private static RestStopHoldout holdout;
    private static CanvasScript canvas;
    private static bool started, pausedOnce, finished;
    private static float began, nextSample, nextShot, maximumDrift, pauseElapsed;
    private static double readyAt, pauseEnd, finishAt;
    private static int movementCalls, firstFrame, shots;
    private static readonly MethodInfo Move = typeof(PlayerScript).GetMethod("PlayerMove", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo Origin = typeof(PlayerScript).GetField("routeLaneOrigin", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo Right = typeof(PlayerScript).GetField("routeRight", BindingFlags.Instance | BindingFlags.NonPublic);
    static RoadPatternPlaytest() { EditorApplication.update -= Tick; EditorApplication.update += Tick; }

    public static object Begin(string label, string scene, float lane = -3.2f, float limit = 320, bool endurance = false, int attackLevel = 27, int healthLevel = 32)
    {
        if (!Application.dataPath.Replace('\\','/').Contains("/tmp/q/") || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Isolated QA Edit Mode required");
        config = new Config { folder = "tmp/road-pattern-playtest/" + label, scene = scene, lane = lane, limit = limit, endurance = endurance, frame = -1, attackLevel = attackLevel, healthLevel = healthLevel };
        player = null; route = null; holdout = null; canvas = null;
        started = pausedOnce = finished = false; movementCalls = shots = 0; maximumDrift = 0; pauseEnd = 0; nextSample = 0;
        if (Directory.Exists(config.folder)) throw new InvalidOperationException("Preserve the previous evidence label");
        Directory.CreateDirectory(config.folder);
        ChapterPlaytestPreferences.SnapshotAt(config.folder + "/preferences.tsv");
        for (int i = 1; i <= 9; i++) PlayerPrefs.SetInt("upgrade_lv_" + i, i == 1 ? attackLevel : i == 2 ? healthLevel : i == 3 ? 30 : 0);
        foreach (CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot))) PlayerPrefs.SetString(CosmeticService.EquippedKey(slot), CosmeticTables.Rows.Single(r => r.slot == slot && r.isDefault).id);
        PlayerPrefs.Save();
        SessionState.SetBool("NoryangjinMapTool.TestPower9999", false); SessionState.SetBool("NoryangjinMapTool.TestFastLateral", false); SessionState.SetFloat("NoryangjinMapTool.TestTimeScale", 1);
        EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + scene + ".unity");
        SessionState.SetString(Key + ".Config", JsonUtility.ToJson(config)); SessionState.SetBool(Key, true);
        EditorApplication.isPaused = false; EditorApplication.isPlaying = true;
        return new { config.folder, scene, endurance, attackLevel, healthLevel, sectionSetup = scene == "RestStop" };
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
        config ??= JsonUtility.FromJson<Config>(SessionState.GetString(Key + ".Config", "{}"));
        if (config.frame == Time.frameCount) return; config.frame = Time.frameCount;
        try { Step(); }
        catch (Exception error) { File.WriteAllText(config.folder + "/error.txt", error.ToString()); Stop(); }
    }
    private static void Step()
    {
        if (finished) { if (EditorApplication.timeSinceStartup > finishAt) Stop(); return; }
        if (player == null)
        {
            player = Object.FindFirstObjectByType<PlayerScript>(); canvas = Object.FindFirstObjectByType<CanvasScript>();
            route = Object.FindFirstObjectByType<HighwayRoute>(); holdout = Object.FindFirstObjectByType<RestStopHoldout>();
            readyAt = EditorApplication.timeSinceStartup + 2; return;
        }
        if (!started)
        {
            if (EditorApplication.timeSinceStartup < readyAt || canvas == null) return;
            OpeningStoryUI.Instance?.Skip(); UnityEngine.Random.InitState(20260913);
            canvas.PlayerPressedStartButton(); if (!TimeManager.isGameRunning) return;
            if (holdout != null) player.ApplyContinuousRoutePose(holdout.center.position - holdout.center.forward * 62 + Vector3.up * .12f, holdout.center.forward, 0);
            if (config.endurance) player.currentHealth = 100000;
            Time.timeScale = 2; began = Time.time; firstFrame = Time.frameCount; nextShot = 1; started = true;
            File.WriteAllText(config.folder + "/timeline.csv", "time,route,health,x,y,z,holdout,holdout_time,police,shots,drift\n");
            return;
        }
        float elapsed = Time.time - began;
        // This mode proves route reachability only. A one-time boost can be
        // replaced by a normal health wall and is not a durable endurance setup.
        if (config.endurance && player.currentHealth > 0) player.currentHealth = Mathf.Max(100000, player.currentHealth);
        if (pauseEnd > 0)
        {
            if (EditorApplication.timeSinceStartup < pauseEnd)
            {
                if (Mathf.Abs(holdout.Elapsed - pauseElapsed) > .001f) throw new InvalidOperationException("Holdout clock advanced while paused");
                return;
            }
            pauseEnd = 0; TimeManager.timeFactor = 1;
        }
        if (holdout != null && holdout.Active)
        {
            maximumDrift = Mathf.Max(maximumDrift, Vector3.Distance(player.transform.position, holdout.LockedPosition));
            if (!pausedOnce && holdout.Elapsed > 11)
            { pausedOnce = true; pauseElapsed = holdout.Elapsed; TimeManager.timeFactor = 0; pauseEnd = EditorApplication.timeSinceStartup + 2; }
        }
        if (elapsed >= nextSample)
        {
            nextSample = elapsed + .25f;
            shots = player.GetComponentsInChildren<WeaponScript>(true).Sum(w => w.TotalProjectilesSpawned);
            File.AppendAllText(config.folder + "/timeline.csv", string.Join(",", elapsed, route?.Distance ?? -1, player.currentHealth, player.transform.position.x, player.transform.position.y, player.transform.position.z, holdout?.Active ?? false, holdout?.Elapsed ?? 0, holdout?.Spawned ?? 0, shots, maximumDrift) + "\n");
        }
        if (elapsed >= nextShot)
        { nextShot = elapsed + (holdout != null ? 4 : elapsed < 14 ? 2 : 8); ScreenCapture.CaptureScreenshot(config.folder + $"/frame-{elapsed:000}.png"); }
        bool done = !TimeManager.isGameRunning || player.currentHealth <= 0 || elapsed > config.limit || (holdout != null && holdout.Completed && Vector3.Dot(player.transform.position - holdout.center.position, holdout.center.forward) > 48);
        if (done)
        {
            var traffic = Object.FindFirstObjectByType<HighwayOncomingTraffic>();
            var report = new { scene = config.scene, elapsed, endurance = config.endurance, health = player.currentHealth, distance = route?.Distance ?? -1, branches = route?.forks.Select(f => new { f.decided, f.bypass, f.rewarded }).ToArray(), holdoutCompleted = holdout?.Completed ?? false, holdoutSeconds = holdout?.Elapsed ?? 0, spawned = holdout?.Spawned ?? 0, spawnedByDoor = holdout?.SpawnedByDoor, spawnPositionError = holdout?.MaximumSpawnPositionError ?? 0, killed = holdout?.Killed ?? 0, maximumDrift, pausedOnce, movementLocked = player.IsStationaryCombat, shots, movementCalls, frames = Time.frameCount - firstFrame, trafficLaunched = traffic?.Launched ?? 0, trafficContacts = traffic?.Contacts ?? 0 };
            File.WriteAllText(config.folder + "/result.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            ScreenCapture.CaptureScreenshot(config.folder + "/end.png"); finished = true; finishAt = EditorApplication.timeSinceStartup + .5; return;
        }
        if (player.IsStationaryCombat) { Move.Invoke(player, new object[] { 500f }); return; }
        var right = (Vector3)Right.GetValue(player); var origin = (Vector3)Origin.GetValue(player);
        float lane = Vector3.Dot(player.transform.position - origin, right);
        Move.Invoke(player, new object[] { Mathf.Clamp((config.lane - lane) * 125f, -90, 90) }); movementCalls++;
    }
    public static void Stop()
    { SessionState.SetBool(Key, false); TimeManager.timeFactor = 1; Time.timeScale = 1; EditorApplication.isPaused = true; }
}
