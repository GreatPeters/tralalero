using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Claude Code review bot for the Noryangjin revamp safe copy. Starts a real run from the Play start
// screen, picks the requested fork, steers with the game's own PlayerMove (no teleport, no HP pin),
// dodges waves via NoryangjinWaveEvent.SafeLateral and captures frames. Mechanic coverage, not balance.
//   unity command --project-path . run_script --file tools/claude-noryangjin-run.cs --entry ClaudeNoryangjinRun.Outside
public static class ClaudeNoryangjinRun
{
    const string Preview = "tmp/image-previews/noryangjin-claude-fix-2026-09-28";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    public static object Inside() => Begin("inside", 0, 2f, 3f);
    public static object Outside() => Begin("outside", 1, 2f, 3f);
    public static object OutsideSlow() => Begin("outside-slow", 1, 1f, .5f);
    public static object InsideSlow() => Begin("inside-slow", 0, 1f, .5f);
    public static object InsideReal() => Begin("inside-1x", 0, 1f, 3f);
    public static object OutsideReal() => Begin("outside-1x", 1, 1f, 3f);

    public static object Begin(string label, int route, float timeScale, float shotSeconds)
    {
        if (!EditorApplication.isPlaying || TimeManager.isGameRunning) throw new Exception("Play start screen required");
        var player = Object.FindFirstObjectByType<PlayerScript>();
        var director = Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        var progression = Object.FindFirstObjectByType<ChapterProgression>();
        var move = player.GetType().GetMethod("PlayerMove", Flags);
        var laneOrigin = player.GetType().GetField("routeLaneOrigin", Flags);
        var routeRight = player.GetType().GetField("routeRight", Flags);
        var waves = director.GetComponentsInChildren<NoryangjinWaveEvent>();
        var hoses = director.GetComponentsInChildren<NoryangjinHoseEvent>();
        var patches = director.GetComponentsInChildren<NoryangjinWetPatch>();
        var incidents = director.GetComponentsInChildren<NoryangjinMarketIncident>();
        var hazards = GameObject.Find("Noryangjin_MapTool/Props").transform.Cast<Transform>().Where(t => t.name.StartsWith("SR18_L_G")).ToArray();
        string folder = Preview + "/run-" + label + "-" + DateTime.Now.ToString("HHmmss"); Directory.CreateDirectory(folder);
        File.WriteAllText(folder + "/samples.csv", "elapsed,hp,maxhp,x,y,z,branchD,cameraY\n");
        int frame = -1, shots = 0, seen = 0; float nextShot = 0, nextSample = 0, nextTargetScan = 0; bool done = false; double stopAt = 0;
        var targets = Array.Empty<EnemyScript_space>();
        var cam = Camera.main;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            try
            {
                if (!EditorApplication.isPlaying || player == null) { EditorApplication.update -= tick; return; }
                if (done) { if (EditorApplication.timeSinceStartup > stopAt) { EditorApplication.update -= tick; EditorApplication.isPaused = true; Time.timeScale = 1; } return; }
                if (frame == Time.frameCount || EditorApplication.isPaused) return; frame = Time.frameCount;
                if (director.Branch.choiceUI.Pending && director.Branch.choiceUI.Remaining < 4) director.Branch.choiceUI.Select(route);
                var at = player.transform.position;
                if (director.Elapsed >= nextSample)
                {
                    nextSample += .25f;
                    File.AppendAllText(folder + "/samples.csv", $"{director.Elapsed:F2},{player.currentHealth:F0},{player.MaxHealth:F0},{at.x:F2},{at.y:F2},{at.z:F2},{director.Branch.Distance:F1},{(cam != null ? cam.transform.position.y : 0):F2}\n");
                }
                if (director.Elapsed >= nextShot || director.Timeline.Count > seen)
                {
                    nextShot = director.Elapsed + shotSeconds; seen = director.Timeline.Count;
                    ScreenCapture.CaptureScreenshot($"{folder}/f{shots++:D3}-{director.Elapsed:000.0}.png");
                }
                if (player.currentHealth <= 0 || progression.Completed || director.Elapsed > 420)
                {
                    done = true; stopAt = EditorApplication.timeSinceStartup + 1.5;
                    File.WriteAllText(folder + "/timeline.txt", string.Join("\n", director.Timeline));
                    File.WriteAllText(folder + "/result.txt", $"route={route} elapsed={director.Elapsed:F1} hp={player.currentHealth:F0}/{player.MaxHealth:F0} completed={progression.Completed} cause={player.LastDamageCause} branch={director.Branch.Selected}/{director.Branch.Outside}/{director.Branch.Complete}");
                    return;
                }
                if (!TimeManager.isGameRunning || player.IsWorldYawTurnActive) return;
                var origin = (Vector3)laneOrigin.GetValue(player); var right = (Vector3)routeRight.GetValue(player);
                float current = Vector3.Dot(at - origin, right), desired = 0; bool urgent = false;
                // Obey the wave that is crashing now; otherwise prepare for the one that crashes soonest.
                float soonest = float.PositiveInfinity;
                foreach (var wave in waves)
                    foreach (var band in wave.waves)
                    {
                        float safe = wave.SafeLateral(band);
                        if (float.IsNaN(safe) || Mathf.Abs(wave.Lateral(at)) > 5) continue;
                        if (Mathf.Abs(-wave.Ahead(at) - band.along) > band.length) continue;
                        float key = wave.IsCrashing(band) ? -1 : wave.SecondsToCrash(band);
                        if (key < 0 && !wave.IsCrashing(band)) continue; // already crashed
                        if (key >= soonest) continue;
                        soonest = key;
                        // SafeLateral is in the wave's frame; convert to the player's lane frame.
                        desired = current + (safe - wave.Lateral(at)); urgent = true;
                    }
                if (!urgent)
                    foreach (var hose in hoses)
                        if (hose.Triggered && Mathf.Abs(hose.Lateral(at)) < 5)
                            foreach (var jet in hose.jets)
                            {
                                float gap = jet.along + hose.Ahead(at);
                                if (gap > -1 && gap < 9 && jet.visual != null && jet.visual.gameObject.activeSelf) desired = current + (-jet.side * 1.6f - hose.Lateral(at));
                            }
                if (!urgent)
                    foreach (var patch in patches)
                    {
                        if (patch.Wetness < .1f) continue;
                        var local = patch.transform.InverseTransformPoint(at + player.transform.forward * 2.5f);
                        if (Mathf.Abs(local.z) < patch.halfSize.y + 1 && Mathf.Abs(local.x) < patch.halfSize.x + .5f)
                            desired = current + (local.x >= 0 ? 1 : -1) * (patch.halfSize.x + .6f - Mathf.Abs(local.x));
                    }
                if (!urgent)
                {
                    // Line up with the nearest living enemy ahead (auto-fire shoots straight along the lane).
                    if (director.Elapsed >= nextTargetScan) { targets = Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None); nextTargetScan = director.Elapsed + .2f; }
                    float best = 30; var flatAim = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
                    foreach (var enemy in targets)
                    {
                        if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;
                        var delta = enemy.transform.position - at; float ahead = Vector3.Dot(delta, flatAim), lane = Vector3.Dot(enemy.transform.position - origin, right);
                        if (ahead < 1 || ahead > 26 || Mathf.Abs(lane) > 2.2f || Mathf.Abs(delta.y) > 4) continue;
                        float priority = ahead + Mathf.Abs(lane - current) * 2;
                        if (priority < best) { best = priority; desired = Mathf.Clamp(lane, -1.8f, 1.8f); }
                    }
                }
                if (!urgent)
                {
                    // Existing SR18 holes, lamps and buckets: step to the far side of the nearest one ahead.
                    float nearestHazard = 16; var flat = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
                    foreach (var hazard in hazards)
                    {
                        if (hazard == null || !hazard.gameObject.activeInHierarchy) continue;
                        var colliders = hazard.GetComponentsInChildren<Collider>().Where(c => c.enabled && c.gameObject.activeInHierarchy && c.bounds.min.y < at.y + 3 && c.bounds.max.y > at.y).ToArray();
                        var point = hazard.position;
                        if (colliders.Length > 0) { var b = colliders[0].bounds; foreach (var c in colliders) b.Encapsulate(c.bounds); point = b.center; }
                        var delta = point - at; float ahead = Vector3.Dot(delta, flat);
                        if (ahead < -2 || ahead > 15 || Mathf.Abs(delta.y) > 2 || Mathf.Abs(Vector3.Dot(delta, right)) > 5 || ahead >= nearestHazard) continue;
                        nearestHazard = ahead; desired = Vector3.Dot(point - origin, right) >= 0 ? -1.8f : 1.8f;
                    }
                }
                foreach (var incident in incidents)
                    if (incident.Triggered && incident.Ahead(at) > -8 && incident.Ahead(at) < 24 && Mathf.Abs(incident.Lateral(at)) < 5)
                        desired = incident.lane < 0 ? 1.6f : -1.6f;
                desired = Mathf.Clamp(desired, -1.9f, 1.9f);
                float error = desired - current;
                if (Mathf.Abs(error) > .05f) move.Invoke(player, new object[] { Mathf.Sign(error) * Mathf.Min(15, Mathf.Abs(error) * 12) });
            }
            catch (Exception error)
            {
                EditorApplication.update -= tick;
                File.WriteAllText(folder + "/harness-error.txt", error.ToString());
                Time.timeScale = 1;
            }
        };
        EditorApplication.update += tick;
        foreach (var harness in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None)) harness.enabled = false;
        Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();
        Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton(); Time.timeScale = timeScale;
        File.WriteAllText("tmp/claude-probe/latest-run.txt", folder);
        return new { folder, route, health = player.currentHealth, attack = player.ResolvedAttackDamage };
    }
}
