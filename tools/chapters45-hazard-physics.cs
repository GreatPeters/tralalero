using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Directed contact fixture. Uses actual authored hazard sweeps, player collider,
// Rigidbody simulation and OnTriggerEnter damage. Never invokes Hazard.Hit.
// Prepare/restore preferences with Chapters45Playtest around this separate run.
public static class Chapters45HazardPhysics
{
    static PlayerScript player;
    static Chapter45Director director;
    static CanvasScript canvas;
    static Rigidbody body;
    static Collider playerCollider;
    static Chapter45Encounter[] encounters;
    static Chapter45Target[] targets;
    static Chapter45Choice[] choices;
    static Chapter45Hazard[] hazards;
    static Chapter45Hazard hazard;
    static Collider[] hitboxes;
    static readonly List<(Chapter45Hazard hazard, bool safe)> cases = new();
    static readonly List<object> checks = new();
    static readonly List<string> errors = new();
    static readonly List<string> images = new();
    static bool active, captured, playerEnabled, wasKinematic, originalManual, safeCase, physicallyOverlapped, caseCaptured;
    static int state, frame, caseIndex, failures, damageEvents;
    static float damageSum, startHealth, expectedDamage, nextSample;
    static double began, stateAt;
    static string folder, scene, phase;

    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    static void Write(string name, object value) => File.WriteAllText(Path.Combine(folder, name), Json(value));
    static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
    static double Age => EditorApplication.timeSinceStartup - stateAt;
    static void Property(object owner, string name, object value) => owner.GetType().GetProperty(name).GetSetMethod(true).Invoke(owner, new[] { value });
    static void Check(string name, bool pass, object evidence)
    {
        var row = new { name, pass, evidence }; checks.Add(row); if (!pass) failures++;
        File.AppendAllText(Path.Combine(folder, "checks.jsonl"), Json(row) + "\n");
    }
    static void Require(string name, bool pass, object evidence)
    { Check(name, pass, evidence); if (!pass) throw new InvalidOperationException(name); }
    static void Next(int next, string name) { state = next; phase = name; stateAt = EditorApplication.timeSinceStartup; Status(); }
    static void Status() => Write("status.json", new { active, scene, phase, state, caseIndex, totalCases = cases.Count, failures, errors = errors.Count, folder });
    static void Capture(string label)
    {
        string path = Path.Combine(folder, images.Count.ToString("D2") + "-" + label + ".png"); images.Add(path); ScreenCapture.CaptureScreenshot(path);
    }
    public static object Begin()
    {
        scene = SceneManager.GetActiveScene().name;
        string prepared = SessionState.GetString("Chapter45.QA.folder", "");
        if (!EditorApplication.isPlaying || !Chapter45Director.IsChapterScene(scene) || TimeManager.isGameRunning || !File.Exists(Path.Combine(prepared, "before-prefs.tsv")))
            throw new InvalidOperationException("Fresh prepared Jamsil or ShoeTower Play lobby required. Preserve and finish ordinary runs first.");
        folder = Path.Combine(prepared, "hazard-physics"); Directory.CreateDirectory(folder);
        if (File.Exists(Path.Combine(folder, "conditions.json"))) throw new InvalidOperationException("Preserve existing evidence; prepare a new run.");
        player = Object.FindFirstObjectByType<PlayerScript>(); director = Object.FindFirstObjectByType<Chapter45Director>(); canvas = Object.FindFirstObjectByType<CanvasScript>();
        if (player == null || director == null || canvas == null) throw new InvalidOperationException("Chapter dependencies missing.");
        body = player.GetComponent<Rigidbody>(); playerCollider = player.GetComponent<Collider>();
        if (body == null || playerCollider == null || !playerCollider.enabled) throw new InvalidOperationException("Actual player Rigidbody and enabled collider required.");
        checks.Clear(); errors.Clear(); images.Clear(); cases.Clear(); state = caseIndex = failures = 0; frame = -1; captured = false;
        active = true; began = stateAt = EditorApplication.timeSinceStartup; phase = "real chapter start";
        Write("conditions.json", new { scene, mode = "directed native physics contact fixtures", ordinaryGameplay = false, funCertification = false,
            callsHazardHit = false, manualPhysicsCallbacks = false, persistentStatOverrides = false,
            fixtureMutations = new[] { "Teleport to authored hazard stations and hold the player still with its real Rigidbody kinematic and controller disabled",
                "Isolate one authored hazard from encounter, target and choice ticking; retain its original colliders, sweep, timing and damage",
                "Trigger each sweep through Chapter45Hazard.Trigger and allow normal Update/physics callbacks" },
            cases = "Jamsil: first taxi unsafe and safe. ShoeTower: first security sweep plus both captain pressure sweeps, each unsafe and safe.",
            restoration = "Runtime controller/body/arrays restored and paused when finished; root exits Play Mode and runs Chapters45Playtest.Restore." });
        OpeningStoryUI.Instance?.Skip(); Time.timeScale = 1; Time.captureDeltaTime = 0;
        Application.logMessageReceived += OnLog; EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Status(); return new { active, scene, folder };
    }
    static void OnLog(string text, string stack, LogType kind)
    { if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) { errors.Add(text); File.AppendAllText(Path.Combine(folder, "errors.txt"), text + "\n" + stack + "\n"); } }
    static void OnDamage(float amount, PlayerDamageCause cause)
    {
        damageEvents++; damageSum += amount;
        File.AppendAllText(Path.Combine(folder, "damage.jsonl"), Json(new { caseIndex, hazard = hazard != null ? hazard.name : "none", safeCase, amount, cause = cause.ToString(), hp = player.currentHealth }) + "\n");
    }
    static void Tick()
    {
        if (!active) return;
        if (!EditorApplication.isPlaying) { Finish("play-mode-ended"); return; }
        if (EditorApplication.isPaused || EditorApplication.isCompiling || frame == Time.frameCount) return;
        frame = Time.frameCount;
        try
        {
            if (EditorApplication.timeSinceStartup - began > 120) { Finish("watchdog"); return; }
            if (state == 0)
            {
                if (Age < 1.5) return;
                canvas.PlayerPressedStartButton(); if (!TimeManager.isGameRunning) return;
                encounters = director.encounters; targets = director.targets; choices = director.choices; hazards = director.hazards;
                playerEnabled = player.enabled; wasKinematic = body.isKinematic; captured = true;
                player.enabled = false; body.isKinematic = true; player.SetBucketShootBlock(director, true);
                director.encounters = Array.Empty<Chapter45Encounter>(); director.targets = Array.Empty<Chapter45Target>(); director.choices = Array.Empty<Chapter45Choice>();
                foreach (var h in hazards) h.Cancel();
                var selected = new List<Chapter45Hazard> { hazards.First(h => !h.manualOnly && h.choiceIndex < 0) };
                if (scene == "ShoeTower") selected.AddRange(hazards.Where(h => h.manualOnly && h.name.StartsWith("CaptainPressure_")).OrderBy(h => h.name));
                Require("authored hazard families found", scene != "ShoeTower" || selected.Count == 3, selected.Select(h => new { h.name, h.floor, h.safeLane }).ToArray());
                foreach (var h in selected) { cases.Add((h, false)); cases.Add((h, true)); }
                player.DamageTaken += OnDamage; Next(1, "set up first contact"); return;
            }
            if (state == 1)
            {
                if (caseIndex >= cases.Count) { Next(3, "flush final capture"); return; }
                hazard = cases[caseIndex].hazard; safeCase = cases[caseIndex].safe; originalManual = hazard.manualOnly;
                canvas.PauseGame(); hazard.ResetForRun(director); hazard.manualOnly = true; director.hazards = new[] { hazard };
                Property(director, "CurrentSegment", director.route.SegmentAt(hazard.distance)); Property(director, "Distance", hazard.distance);
                Require("hazard fixture uses authored deck", director.CurrentFloor == hazard.floor, new { hazard.name, hazard.floor, director.CurrentFloor, hazard.distance });
                director.route.Sample(hazard.distance, out var routeCenter, out var forward);
                Vector3 rest = hazard.body.position;
                Vector3 position = safeCase ? rest + hazard.transform.right * hazard.safeLane : rest + hazard.transform.TransformDirection(hazard.sweepTo);
                position.y = routeCenter.y + director.footOffset;
                player.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward)); body.position = position; body.rotation = player.transform.rotation;
                Physics.SyncTransforms();
                hitboxes = hazard.body.GetComponentsInChildren<Collider>(true);
                Require("actual authored trigger hitboxes retained", hitboxes.Length > 0 && hitboxes.All(c => c.isTrigger), new { hazard.name, count = hitboxes.Length, playerLayer = player.gameObject.layer, hazardLayer = hazard.body.gameObject.layer });
                damageEvents = 0; damageSum = 0; physicallyOverlapped = caseCaptured = false; startHealth = player.currentHealth; nextSample = 0;
                expectedDamage = player.MaxHealth * Chapter45Director.Setting("c45_hazardDamageFraction", hazard.damageFraction, .01f, .2f);
                Require("fixture has no protective shield", !director.ShieldReady && startHealth > expectedDamage, new { director.ShieldReady, startHealth, expectedDamage });
                canvas.ResumeGame(); hazard.Trigger();
                Next(2, hazard.name + (safeCase ? " safe lane" : " unsafe lane")); return;
            }
            if (state == 2)
            {
                if (hazard.Active)
                {
                    foreach (var hitbox in hitboxes)
                        if (hitbox.enabled && hitbox.gameObject.activeInHierarchy && Physics.ComputePenetration(playerCollider, playerCollider.transform.position, playerCollider.transform.rotation,
                            hitbox, hitbox.transform.position, hitbox.transform.rotation, out _, out _)) physicallyOverlapped = true;
                    if (!caseCaptured && (hazard.Contacts > 0 || safeCase && Age > Chapter45Director.Setting("c45_warningSeconds", hazard.warningSeconds, 1.2f, 4) + .55f))
                    { caseCaptured = true; Capture(caseIndex.ToString("D2") + "-" + hazard.name + (safeCase ? "-safe" : "-unsafe")); }
                }
                if (Age >= nextSample)
                {
                    nextSample = (float)Age + .1f;
                    File.AppendAllText(Path.Combine(folder, "physics.jsonl"), Json(new { caseIndex, hazard.name, safeCase, seconds = Age, hazard.Active, hazard.Finished, hazard.Contacts,
                        player = V(player.transform.position), playerMin = V(playerCollider.bounds.min), playerMax = V(playerCollider.bounds.max),
                        body = V(hazard.body.position), hitboxes = hitboxes.Select(c => new { c.enabled, active = c.gameObject.activeInHierarchy, min = V(c.bounds.min), max = V(c.bounds.max) }).ToArray(),
                        physicallyOverlapped, hp = player.currentHealth, damageEvents, damageSum }) + "\n");
                }
                if (!hazard.Finished) { if (Age > 10) throw new InvalidOperationException("Authored hazard failed to finish."); return; }
                if (safeCase)
                    Check(hazard.name + " advertised safe lane remains physically clear", hazard.Contacts == 0 && damageEvents == 0 && !physicallyOverlapped,
                        new { hazard.safeLane, hazard.Contacts, damageEvents, damageSum, physicallyOverlapped, before = startHealth, after = player.currentHealth });
                else
                    Check(hazard.name + " unsafe sweep causes exactly one native contact", hazard.Contacts == 1 && damageEvents == 1 && Mathf.Abs(damageSum - expectedDamage) < .01f,
                        new { hazard.Contacts, damageEvents, expectedDamage, damageSum, physicallyOverlapped, before = startHealth, after = player.currentHealth });
                hazard.manualOnly = originalManual; hazard = null; caseIndex++; Next(1, "next contact fixture"); return;
            }
            if (state == 3 && Age >= .5) Finish(failures == 0 && errors.Count == 0 ? "passed" : "failed");
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(folder, "harness-error.txt"), error.ToString()); Finish("harness-error"); }
    }
    static void Finish(string outcome)
    {
        active = false; EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (player != null) player.DamageTaken -= OnDamage;
        if (captured && director != null && player != null)
        {
            if (hazard != null) { hazard.Cancel(); hazard.manualOnly = originalManual; }
            canvas.PauseGame(); director.encounters = encounters; director.targets = targets; director.choices = choices; director.hazards = hazards;
            player.SetBucketShootBlock(director, false); player.enabled = playerEnabled; body.isKinematic = wasKinematic;
        }
        Write("summary.json", new { outcome, scene, failures, checks, errors, nativeImages = images.Where(File.Exists).ToArray(),
            ordinaryGameplay = false, physicalCallbacks = "Actual Chapter45HazardHitbox.OnTriggerEnter; Hazard.Hit was never invoked by the fixture.",
            limitations = "Directed held-position contact checks; these do not establish natural approach timing, player reaction difficulty, fun, or device performance. Root must restore preferences after leaving Play Mode." });
        phase = outcome; Status();
    }
    public static object Poll() => new { active, phase, folder, failures, summary = folder != null && File.Exists(Path.Combine(folder, "summary.json")) ? File.ReadAllText(Path.Combine(folder, "summary.json")) : null };
}
