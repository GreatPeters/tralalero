using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Stationary visual fixture for the Noryangjin revamp safe copy (Claude Code review tool).
// Reads tmp/claude-probe/shots.txt: one shot per line "name x y z headingDeg [outside|inside] [hose]".
// Enter Play first (map tool stage 1), then:
//   unity command --project-path . run_script --file tools/claude-noryangjin-shots.cs --entry ClaudeNoryangjinShots.Capture
// Firing, enemies and events are frozen; this is a framing check, not gameplay evidence.
public static class ClaudeNoryangjinShots
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    public static object Capture()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play required");
        EditorApplication.isPaused = false;
        var all = File.ReadAllLines("tmp/claude-probe/shots.txt");
        bool keepEnemies = all.Any(l => l.Trim() == "#keep-enemies"); // e.g. to frame the final boss
        var lines = all.Where(l => l.Trim().Length > 0 && !l.StartsWith("#")).ToArray();
        string folder = "tmp/image-previews/noryangjin-claude-fix-2026-09-28/shots-" + DateTime.Now.ToString("HHmmss");
        Directory.CreateDirectory(folder);
        var p = Object.FindFirstObjectByType<PlayerScript>();
        var d = Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        var owner = p.GetType().GetField("stationaryCombatOwner", Hidden);
        var point = p.GetType().GetField("stationaryCombatPosition", Hidden);
        var hold = new GameObject("ClaudeCaptureAnchor");
        int step = -1; double due = EditorApplication.timeSinceStartup + 1; bool started = false, waiting = false, requested = false; string file = "";
        var records = new List<string>();
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            try
            {
                if (!EditorApplication.isPlaying || p == null) { EditorApplication.update -= tick; return; }
                if (EditorApplication.timeSinceStartup < due) return;
                if (!started)
                {
                    Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();
                    if (!TimeManager.isGameRunning) Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
                    if (!TimeManager.isGameRunning) { due = EditorApplication.timeSinceStartup + .5; return; }
                    started = true; Time.timeScale = 1;
                    foreach (var w in Object.FindObjectsByType<WeaponScript>(FindObjectsSortMode.None)) { w.CancelInvoke(); w.StopAllCoroutines(); w.enabled = false; }
                    foreach (var b in Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None)) b.gameObject.SetActive(false);
                    foreach (var h in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None)) h.enabled = false;
                    var tutorial = Object.FindFirstObjectByType<CoastalTutorialUI>(); if (tutorial != null) { tutorial.Close(); tutorial.enabled = false; }
                    if (!keepEnemies) foreach (var e in Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None)) e.gameObject.SetActive(false);
                    foreach (var e in d.GetComponentsInChildren<NoryangjinRevampEvent>()) e.enabled = false;
                    due = EditorApplication.timeSinceStartup + .3; return;
                }
                if (waiting && !requested) { ScreenCapture.CaptureScreenshot(file); requested = true; return; }
                if (requested)
                {
                    if (!File.Exists(file)) return;
                    var cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
                    records.Add($"{Path.GetFileName(file)} player={p.transform.position} camera={cam.transform.position}");
                    requested = waiting = false;
                    if (step == lines.Length - 1)
                    {
                        File.WriteAllLines(folder + "/shots.txt", records);
                        File.WriteAllText("tmp/claude-probe/latest-shots.txt", folder);
                        EditorApplication.update -= tick; EditorApplication.isPaused = true; return;
                    }
                }
                step++;
                var f = lines[step].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var branch = d.Branch;
                Vector3 pos, heading; string route = null;
                if (f[1] == "d")
                {
                    // "name d <branchDistance> inside|outside": a pose on the market branch.
                    route = f[3];
                    branch.Sample(float.Parse(f[2]), route == "outside", out pos, out heading);
                    pos += Vector3.up * .12f;
                }
                else
                {
                    pos = new Vector3(float.Parse(f[1]), float.Parse(f[2]), float.Parse(f[3]));
                    heading = Quaternion.Euler(0, float.Parse(f[4]), 0) * Vector3.forward;
                    if (f.Length > 5 && (f[5] == "inside" || f[5] == "outside")) route = f[5];
                }
                if (branch != null && route != null)
                {
                    typeof(NoryangjinMarketBranch).GetProperty("Selected").SetValue(branch, true);
                    typeof(NoryangjinMarketBranch).GetProperty("Outside").SetValue(branch, route == "outside");
                }
                if (f.Contains("hose"))
                    foreach (var j in d.GetComponentInChildren<NoryangjinHoseEvent>().jets) { j.visual.gameObject.SetActive(true); j.wetPatch.SetSpraying(true); }
                p.ApplyContinuousRoutePose(pos, heading, 0); owner.SetValue(p, hold); point.SetValue(p, p.transform.position);
                file = folder + "/" + f[0] + ".png"; waiting = true; due = EditorApplication.timeSinceStartup + 1.6;
            }
            catch (Exception error) { EditorApplication.update -= tick; File.WriteAllText(folder + "/error.txt", error.ToString()); EditorApplication.isPaused = true; }
        };
        EditorApplication.update += tick;
        return new { folder, shots = lines.Length };
    }
}
