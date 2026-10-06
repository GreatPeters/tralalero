using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Directed native art survey. Explicit route teleports and a held player pose;
// never ordinary gameplay, objective clearance, performance or fun evidence.
public static class Chapters45VisualSurvey
{
    static readonly float[] Stations = { 0, 180, 350, 590, 700, 850, 1150, 1400, 1510, 1580, 1640 };
    static readonly MethodInfo Visibility = typeof(Chapter45Director).GetMethod("RefreshVisibility", BindingFlags.Instance | BindingFlags.NonPublic);
    static PlayerScript player;
    static Chapter45Director director;
    static CanvasScript canvas;
    static Rigidbody body;
    static Camera camera;
    static GameObject holdOwner;
    static CombatHarness overlay;
    static FieldInfo overlayVisible;
    static bool overlayWasVisible;
    static bool active, captured, playerEnabled, bodyKinematic;
    static int stage, station, frame, movedFrame, quietFrames;
    static double began, stateAt;
    static Vector3 previousCameraPosition;
    static Quaternion previousCameraRotation;
    static float previousFov, initialHealth;
    static string folder, phase;
    static readonly List<string> images = new();
    static readonly List<string> errors = new();
    static readonly List<object> frames = new();
    static readonly List<object> phaseChecks = new();
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    static void Write(string name, object value) => File.WriteAllText(Path.Combine(folder, name), Json(value));
    static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
    static double Age => EditorApplication.timeSinceStartup - stateAt;
    static void Property(object owner, string name, object value) => owner.GetType().GetProperty(name).GetSetMethod(true).Invoke(owner, new[] { value });
    static void Status() => Write("status.json", new { active, phase, stage, station, totalStations = Stations.Length, phaseChecksCount = phaseChecks.Count, errors = errors.Count, folder });
    static void RecordPhase(string name)
    {
        var actors = director.encounters.SelectMany(e => e.actors).Where(e => e != null).ToArray();
        var row = new { phase = name, TimeManager.isGameRunning, director.Running, director.Distance, director.CurrentFloor,
            actorCount = actors.Length, activeActorCount = actors.Count(e => e.gameObject.activeInHierarchy),
            openingActive = Object.FindObjectsByType<OpeningStoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(o => o.gameObject.activeInHierarchy), player.currentHealth,
            render = new { UnityStats.batches, UnityStats.setPassCalls, UnityStats.triangles } };
        phaseChecks.Add(row); File.AppendAllText(Path.Combine(folder, "phase-checks.jsonl"), Json(row) + "\n");
    }
    static void RefuseActiveOpening()
    {
        if (Object.FindObjectsByType<OpeningStoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(o => o.gameObject.activeInHierarchy))
            throw new InvalidOperationException("Saved chapter opening root must be inactive; the visual survey does not skip or hide it.");
    }
    static void Next(int next, string label) { stage = next; phase = label; stateAt = EditorApplication.timeSinceStartup; Status(); }
    public static object Begin()
    {
        string prepared = SessionState.GetString("Chapter45.QA.folder", "");
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "ShoeTower" || TimeManager.isGameRunning || !File.Exists(Path.Combine(prepared, "before-prefs.tsv")))
            throw new InvalidOperationException("Fresh prepared ShoeTower Play lobby required. Do not run alongside ordinary gameplay or another fixture.");
        RefuseActiveOpening();
        folder = Path.Combine(prepared, "visual-survey"); Directory.CreateDirectory(folder);
        if (File.Exists(Path.Combine(folder, "conditions.json"))) throw new InvalidOperationException("Preserve prior survey evidence; prepare another run.");
        player = Object.FindFirstObjectByType<PlayerScript>(); director = Object.FindFirstObjectByType<Chapter45Director>(); canvas = Object.FindFirstObjectByType<CanvasScript>(); camera = Camera.main;
        if (player == null || director == null || canvas == null || camera == null) throw new InvalidOperationException("Native chapter/player/canvas/camera dependencies missing.");
        body = player.GetComponent<Rigidbody>(); if (body == null) throw new InvalidOperationException("Actual player body required.");
        active = true; captured = false; stage = station = quietFrames = 0; frame = -1; began = stateAt = EditorApplication.timeSinceStartup; phase = "actual start";
        images.Clear(); errors.Clear(); frames.Clear(); phaseChecks.Clear(); initialHealth = player.currentHealth;
        overlay = Object.FindFirstObjectByType<CombatHarness>(); overlayVisible = typeof(CombatHarness).GetField("overlayVisible", BindingFlags.Instance | BindingFlags.NonPublic);
        if (overlay != null && overlayVisible != null) { overlayWasVisible = (bool)overlayVisible.GetValue(overlay); overlayVisible.SetValue(overlay, false); }
        Write("conditions.json", new { scene = "ShoeTower", purpose = "native gameplay-camera art and readability review", ordinaryGameplay = false, funEvaluation = false,
            fixtureMutations = new[] { "Actual Canvas start, then PlayerScript disabled and its real Rigidbody held kinematic",
                "Distance and CurrentSegment set explicitly for each station; pose sampled through actual SampleOnCurrentDeck including branch offset/tangent",
                "Archive fork uses actual Commit(1) after its entry; the 700m preview remains uncommitted",
                "Old-deck actors retire through their actual Retire API, matching lift landing; RefreshVisibility invoked after each move",
                "Player firing blocked only while held for capture; targets, shields, hazards and their clocks otherwise keep native Update state" },
            healthWrites = false, targetStateOverrides = false, forcedWin = false, movieQualityEvaluation = false, openingSkipped = false, debugOverlayOnlySuppressed = overlay != null, stations = Stations,
            camera = "Actual MainCamera; wait at least 10 rendered frames and 3 quiet camera updates after every move. Fail rather than capture if it never settles.",
            preferenceRestoration = "Root exits Play Mode and calls Chapters45Playtest.Restore", watchdogSeconds = 120 });
        Time.timeScale = 1; Time.captureDeltaTime = 0;
        Application.logMessageReceived += OnLog; EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Status(); return new { active, folder, phase };
    }
    static void OnLog(string text, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) { errors.Add(text); File.AppendAllText(Path.Combine(folder, "errors.txt"), text + "\n" + stack + "\n"); } }
    static void Tick()
    {
        if (!active) return;
        if (!EditorApplication.isPlaying) { Finish("play-mode-ended"); return; }
        if (EditorApplication.isPaused || EditorApplication.isCompiling || frame == Time.frameCount) return;
        frame = Time.frameCount;
        try
        {
            if (EditorApplication.timeSinceStartup - began > 120) { Finish("watchdog"); return; }
            if (player.currentHealth <= 0 || CanvasScript.isGameOver || director.GoalClaimed) { Finish("native-run-stopped-during-survey"); return; }
            if (stage == 0)
            {
                if (Age < 1.2) return;
                RefuseActiveOpening(); RecordPhase("fresh lobby before start");
                string lobby = Path.Combine(folder, "lobby-before-start.png"); images.Add(lobby); ScreenCapture.CaptureScreenshot(lobby);
                Next(5, "flush lobby capture"); return;
            }
            if (stage == 5)
            {
                if (Age < .3) return;
                RefuseActiveOpening();
                canvas.PlayerPressedStartButton(); if (!TimeManager.isGameRunning) return;
                RecordPhase("actual start completed");
                playerEnabled = player.enabled; bodyKinematic = body.isKinematic; captured = true;
                holdOwner = new GameObject("Chapter45_VisualSurveyHold"); player.SetBucketShootBlock(holdOwner, true);
                player.enabled = false; body.isKinematic = true;
                Next(1, "place first station"); return;
            }
            if (stage == 1)
            {
                if (station >= Stations.Length) { Next(3, "flush final screenshot"); return; }
                float distance = Mathf.Clamp(Stations[station], 0, director.route.Length);
                foreach (var choice in director.choices)
                    if (choice.kind == Chapter45Choice.ChoiceKind.RouteFork && !choice.Selected && distance >= choice.distance)
                        choice.Commit(1);
                Property(director, "Distance", distance); Property(director, "CurrentSegment", director.route.SegmentAt(distance));
                director.SampleOnCurrentDeck(distance, out var center, out var forward);
                player.ApplyContinuousRoutePose(center + Vector3.up * director.footOffset, forward, 0); Physics.SyncTransforms();
                foreach (var encounter in director.encounters) if (encounter.floor != director.CurrentFloor) encounter.Retire();
                Visibility.Invoke(director, null);
                movedFrame = Time.frameCount; quietFrames = 0; previousCameraPosition = camera.transform.position; previousCameraRotation = camera.transform.rotation; previousFov = camera.fieldOfView;
                Next(2, "settle camera at " + distance.ToString("0") + "m"); return;
            }
            if (stage == 2)
            {
                bool quiet = Vector3.Distance(camera.transform.position, previousCameraPosition) < .015f && Quaternion.Angle(camera.transform.rotation, previousCameraRotation) < .015f && Mathf.Abs(camera.fieldOfView - previousFov) < .01f;
                quietFrames = quiet ? quietFrames + 1 : 0;
                previousCameraPosition = camera.transform.position; previousCameraRotation = camera.transform.rotation; previousFov = camera.fieldOfView;
                if (Time.frameCount - movedFrame < 10 || quietFrames < 3)
                { if (Age > 8) { Finish("camera-did-not-settle"); } return; }
                string path = Path.Combine(folder, station.ToString("D2") + "-" + Stations[station].ToString("0000") + "m-floor" + director.CurrentFloor + ".png");
                images.Add(path); ScreenCapture.CaptureScreenshot(path);
                var row = new { station, requestedDistance = Stations[station], director.Distance, director.CurrentSegment, director.CurrentFloor,
                    player = V(player.transform.position), playerForward = V(player.transform.forward), hp = player.currentHealth, attack = player.ResolvedAttackDamage,
                    cameraPosition = V(camera.transform.position), cameraEuler = V(camera.transform.eulerAngles), camera.fieldOfView,
                    framesAfterMove = Time.frameCount - movedFrame, quietFrames, width = Screen.width, height = Screen.height,
                    choices = director.choices.Select(c => new { c.name, c.Selected, c.Selection, c.Rewarded }).ToArray(),
                    targets = director.targets.Where(t => t.floor == director.CurrentFloor).Select(t => new { t.name, t.Health, t.Open, t.Phase, t.Shielded }).ToArray(),
                    enemies = director.encounters.Where(e => e.floor == director.CurrentFloor && e.Activated).SelectMany(e => e.actors).Where(e => e != null && e.gameObject.activeInHierarchy).Select(e => new { e.name, e.CurrentHealth, position = V(e.transform.position) }).ToArray(),
                    hazards = director.hazards.Where(h => h.floor == director.CurrentFloor).Select(h => new { h.name, h.Active, h.Triggered, h.Finished, h.Contacts }).ToArray(),
                    render = new { UnityStats.batches, UnityStats.setPassCalls, UnityStats.triangles }, nativeCapture = path };
                frames.Add(row); File.AppendAllText(Path.Combine(folder, "stations.jsonl"), Json(row) + "\n");
                RecordPhase("captured station " + Stations[station].ToString("0"));
                station++; Next(4, "allow screenshot frame to finish"); return;
            }
            if (stage == 4)
            {
                if (Age < .15) return;
                Next(1, "next station"); return;
            }
            if (stage == 3 && Age >= .5) Finish(errors.Count == 0 ? "captured" : "captured-with-errors");
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(folder, "harness-error.txt"), error.ToString()); Finish("harness-error"); }
    }
    static void Finish(string outcome)
    {
        active = false; EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (captured && player != null)
        {
            if (canvas != null && EditorApplication.isPlaying) canvas.PauseGame();
            player.SetBucketShootBlock(holdOwner, false); player.enabled = playerEnabled; body.isKinematic = bodyKinematic;
            if (holdOwner != null) Object.Destroy(holdOwner);
        }
        if (overlay != null && overlayVisible != null) overlayVisible.SetValue(overlay, overlayWasVisible);
        Write("summary.json", new { outcome, stationCount = frames.Count, requestedStations = Stations.Length, initialHealth, finalHealth = player != null ? player.currentHealth : -1,
            errors, phaseChecksCount = phaseChecks.Count, phaseChecks, screenshots = images.Where(File.Exists).ToArray(), frames,
            limitations = "Directed teleport/held-pose native art survey. Targets retain their native state; unopened gates and uncleared captain are intentional. No ordinary traversal, timing, physical reachability, device performance or fun claim. Root must exit Play and restore preferences." });
        phase = outcome; Status();
    }
    public static object Poll() => new { active, phase, folder, station, summary = folder != null && File.Exists(Path.Combine(folder, "summary.json")) ? File.ReadAllText(Path.Combine(folder, "summary.json")) : null };
}
