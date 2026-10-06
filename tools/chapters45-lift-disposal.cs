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

// Optional directed fixture: actual component-disable and actual additive scene
// unload during a ride. The player remains in ShoeTower for cleanup assertions.
// No scene is saved; root exits Play and restores prepared preferences afterward.
public static class Chapters45LiftDisposal
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo Board = typeof(Chapter45Director).GetMethod("BeginLift", Private);
    static readonly FieldInfo Blocks = typeof(PlayerScript).GetField("bucketShootBlockers", Private);
    static PlayerScript player;
    static Chapter45Director director;
    static ChapterProgression chapter;
    static CanvasScript canvas;
    static Rigidbody body;
    static AsyncOperation unloading;
    static Object disposedOwner;
    static bool active, beforeKinematic, beforeShooting;
    static int state, frame, failures;
    static double began, stateAt;
    static string folder, phase;
    static readonly List<object> checks = new();
    static readonly List<string> errors = new();
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    static void Write(string name, object value) => File.WriteAllText(Path.Combine(folder, name), Json(value));
    static double Age => EditorApplication.timeSinceStartup - stateAt;
    static void Property(object owner, string name, object value) => owner.GetType().GetProperty(name).GetSetMethod(true).Invoke(owner, new[] { value });
    static void Check(string name, bool pass, object evidence)
    { var row = new { name, pass, evidence }; checks.Add(row); if (!pass) failures++; File.AppendAllText(Path.Combine(folder, "checks.jsonl"), Json(row) + "\n"); }
    static void Require(string name, bool pass, object evidence)
    { Check(name, pass, evidence); if (!pass) throw new InvalidOperationException(name); }
    static void Status() => Write("status.json", new { active, phase, state, failures, errors = errors.Count, folder });
    static void Next(int next, string label) { state = next; phase = label; stateAt = EditorApplication.timeSinceStartup; Status(); }
    public static object Begin()
    {
        string prepared = SessionState.GetString("Chapter45.QA.folder", "");
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "ShoeTower" || TimeManager.isGameRunning || !File.Exists(Path.Combine(prepared, "before-prefs.tsv")))
            throw new InvalidOperationException("Fresh prepared ShoeTower Play lobby required; run separately from ordinary/lifecycle fixtures.");
        folder = Path.Combine(prepared, "lift-disposal"); Directory.CreateDirectory(folder);
        if (File.Exists(Path.Combine(folder, "conditions.json"))) throw new InvalidOperationException("Preserve existing evidence; prepare another run.");
        player = Object.FindFirstObjectByType<PlayerScript>(); director = Object.FindFirstObjectByType<Chapter45Director>(); chapter = Object.FindFirstObjectByType<ChapterProgression>(); canvas = Object.FindFirstObjectByType<CanvasScript>();
        if (player == null || director == null || chapter == null || canvas == null) throw new InvalidOperationException("Gameplay dependencies missing.");
        body = player.GetComponent<Rigidbody>(); if (body == null || director.transform.parent != null) throw new InvalidOperationException("Player body and root-level chapter world required.");
        checks.Clear(); errors.Clear(); state = failures = 0; frame = -1; unloading = null; disposedOwner = null;
        active = true; began = stateAt = EditorApplication.timeSinceStartup; phase = "real start";
        Write("conditions.json", new { mode = "directed disposal during lift", ordinaryGameplay = false,
            fixtureMutations = new[] { "Teleport to first authored lift and invoke real BeginLift", "Disable actual Chapter45Director during first ride",
                "BeginRun resets the fixture; start a second ride", "While paused, move actual chapter world root into a temporary runtime scene and unload that scene; retain the real player for cleanup assertions" },
            persistentWrites = false, savesScenes = false, restoration = "Root must exit Play and run Chapters45Playtest.Restore; runtime chapter scenery is deliberately unloaded.", watchdogSeconds = 45 });
        OpeningStoryUI.Instance?.Skip(); Time.timeScale = 1;
        Application.logMessageReceived += OnLog; EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Status(); return new { active, folder };
    }
    static void OnLog(string text, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) { errors.Add(text); File.AppendAllText(Path.Combine(folder, "errors.txt"), text + "\n" + stack + "\n"); } }
    static void BoardFixture()
    {
        canvas.PauseGame(); var lift = director.lifts[0];
        Property(director, "CurrentSegment", lift.afterSegment); Property(director, "Distance", director.route.SegmentEnd(lift.afterSegment));
        director.route.SampleSegment(lift.afterSegment, director.Distance, out var center, out var forward);
        player.ApplyContinuousRoutePose(center + Vector3.up * director.footOffset, forward, 0); Physics.SyncTransforms();
        beforeKinematic = body.isKinematic; beforeShooting = player.canShoot;
        canvas.ResumeGame(); Board.Invoke(director, new object[] { lift });
        Require("real transfer begins", director.IsTransferring && body.isKinematic && !player.canShoot, new { director.IsTransferring, body.isKinematic, player.canShoot });
    }
    static void Tick()
    {
        if (!active) return;
        if (!EditorApplication.isPlaying) { Finish("play-mode-ended"); return; }
        if (EditorApplication.isCompiling || EditorApplication.isPaused || frame == Time.frameCount) return;
        frame = Time.frameCount;
        try
        {
            if (EditorApplication.timeSinceStartup - began > 45) { Finish("watchdog"); return; }
            switch (state)
            {
                case 0:
                    if (Age < 1.5) return;
                    canvas.PlayerPressedStartButton(); if (!TimeManager.isGameRunning) return;
                    BoardFixture(); Next(1, "disable during actual ride"); return;
                case 1:
                    if (Age < .8) return;
                    Require("component disabled midride", director.IsTransferring, new { director.IsTransferring });
                    director.enabled = false;
                    var blockers = (HashSet<Object>)Blocks.GetValue(player);
                    Check("component disable releases transfer state", !director.IsTransferring && !director.Running && Chapter45Director.Active == null, new { director.IsTransferring, director.Running, activeCleared = Chapter45Director.Active == null });
                    Check("component disable restores player body and fire", body.isKinematic == beforeKinematic && player.canShoot == beforeShooting && !blockers.Contains(director),
                        new { beforeKinematic, afterKinematic = body.isKinematic, beforeShooting, afterShooting = player.canShoot, retainedBlockers = blockers.Count });
                    canvas.PauseGame(); Next(2, "restart isolated fixture"); return;
                case 2:
                    if (Age < .3) return;
                    director.enabled = true; chapter.BeginRun(); BoardFixture(); Next(3, "unload during actual ride"); return;
                case 3:
                    if (Age < .8) return;
                    Require("world unloaded midride", director.IsTransferring, new { director.IsTransferring });
                    canvas.PauseGame(); disposedOwner = director;
                    var temporary = SceneManager.CreateScene("Chapter45_LiftUnloadFixture_" + DateTime.UtcNow.Ticks);
                    SceneManager.MoveGameObjectToScene(director.gameObject, temporary);
                    unloading = SceneManager.UnloadSceneAsync(temporary); Require("native scene unload scheduled", unloading != null, new { temporary.name });
                    Next(4, "observe unload cleanup"); return;
                case 4:
                    if (unloading == null || !unloading.isDone) return;
                    var remainingBlocks = (HashSet<Object>)Blocks.GetValue(player);
                    Check("native unload destroys world and clears active director", director == null && Chapter45Director.Active == null && player != null, new { worldDestroyed = director == null, activeCleared = Chapter45Director.Active == null, retainedPlayer = player != null });
                    Check("native unload restores retained player body and fire", body.isKinematic == beforeKinematic && player.canShoot == beforeShooting && !remainingBlocks.Contains(disposedOwner),
                        new { beforeKinematic, afterKinematic = body.isKinematic, beforeShooting, afterShooting = player.canShoot, retainedBlockers = remainingBlocks.Count });
                    Next(5, "flush results"); return;
                case 5:
                    if (Age < .3) return;
                    Finish(failures == 0 && errors.Count == 0 ? "passed" : "failed"); return;
            }
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(folder, "harness-error.txt"), error.ToString()); Finish("harness-error"); }
    }
    static void Finish(string outcome)
    {
        active = false; EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (EditorApplication.isPlaying && canvas != null) canvas.PauseGame();
        Write("summary.json", new { outcome, failures, checks, errors,
            limitations = "Tests actual disable/unload cleanup with a deliberately retained player in another runtime scene. Does not test normal scene-transition visuals or ordinary gameplay. Exit Play Mode before preference restoration." });
        phase = outcome; Status();
    }
    public static object Poll() => new { active, phase, folder, failures, summary = folder != null && File.Exists(Path.Combine(folder, "summary.json")) ? File.ReadAllText(Path.Combine(folder, "summary.json")) : null };
}
