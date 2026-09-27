using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Supplemental visual route surveys, explicitly excluded from the ten ordinary combat attempts.
public static class HighwayCoverage20260926
{
    const string Root = "outputs/highway-analysis-2026-09-26/coverage";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static PlayerScript player; static HighwayRoute route; static int run, frame = -1; static bool started, ended;
    static float began, nextShot, nextSample; static double ready, finish;
    static Renderer[] scenery; static readonly List<object> occlusions = new();
    static string Folder => Root + (run == 0 ? "/left" : "/right");
    static string Json(object o) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { o });
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    public static object Begin()
    {
        if (!EditorApplication.isPlaying || !File.Exists("outputs/highway-analysis-2026-09-26/complete.json") || Directory.Exists(Root)) throw new InvalidOperationException("Finish combat cohort first; preserve old evidence");
        Directory.CreateDirectory(Root); run = 0; player = null; started = ended = false;
        EditorApplication.isPaused = false; SceneManager.LoadScene("HighWay"); EditorApplication.update += Tick;
        return new { mode = "visual coverage only", collisionDisabled = true, hpPinned = 100000, runs = 2 };
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || frame == Time.frameCount) return;
        frame = Time.frameCount;
        try
        {
            if (ended)
            {
                if (EditorApplication.timeSinceStartup < finish) return;
                if (run == 1) { EditorApplication.update -= Tick; Time.timeScale = 1; EditorApplication.isPaused = true; File.WriteAllText(Root + "/complete.json", Json(new { done = true, runs = 2 })); return; }
                run++; player = null; started = ended = false; SceneManager.LoadScene("HighWay"); return;
            }
            if (player == null) { player = Object.FindFirstObjectByType<PlayerScript>(); route = Object.FindFirstObjectByType<HighwayRoute>(); ready = EditorApplication.timeSinceStartup + 2; return; }
            if (!started)
            {
                if (EditorApplication.timeSinceStartup < ready) return;
                OpeningStoryUI.Instance?.Skip(); Object.FindFirstObjectByType<CoastalTutorialUI>()?.Close();
                Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton(); if (!TimeManager.isGameRunning) return;
                foreach (var c in player.GetComponentsInChildren<Collider>()) c.enabled = false;
                Directory.CreateDirectory(Folder); player.currentHealth = 100000; Time.timeScale = 3; began = Time.time; nextShot = nextSample = 0; occlusions.Clear();
                scenery = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r is MeshRenderer && !PathOf(r.transform).Contains("Enemies") && !PathOf(r.transform).Contains("Player") && r.bounds.max.y > .8f).ToArray();
                File.WriteAllText(Folder + "/timeline.csv", "time,distance,bypass,hp\n"); started = true; return;
            }
            float elapsed = Time.time - began; player.currentHealth = Mathf.Max(player.currentHealth, 100000);
            if (elapsed >= nextSample) { nextSample = elapsed + 1; File.AppendAllText(Folder + "/timeline.csv", string.Join(",", elapsed, route.Distance, route.OnBypass, player.currentHealth) + "\n"); File.WriteAllText(Root + "/status.json", Json(new { run, elapsed, distance = route.Distance, route.OnBypass })); }
            if (elapsed >= nextShot)
            {
                nextShot = elapsed + (route.forks.Any(f => route.Distance >= f.start - 50 && route.Distance <= f.end + 20) ? 3 : 12);
                string shot = "d-" + route.Distance.ToString("0000"); ScreenCapture.CaptureScreenshot(Folder + "/" + shot + ".png");
                var cam = Camera.main;
                if (cam != null)
                {
                    var hits = new List<object>();
                    foreach (var uv in new[] { new Vector2(.5f, .5f), new Vector2(.5f, .7f), new Vector2(.3f, .6f), new Vector2(.7f, .6f) })
                    {
                        var ray = cam.ViewportPointToRay(uv);
                        var candidates = scenery.Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy).Select(r => { bool hit = r.bounds.IntersectRay(ray, out float dist); return new { r, hit, dist }; }).Where(x => x.hit && x.dist > 0 && x.dist < 70).OrderBy(x => x.dist).Take(4).Select(x => new { path = PathOf(x.r.transform), distance = x.dist, size = new[] { x.r.bounds.size.x, x.r.bounds.size.y, x.r.bounds.size.z } }).ToArray();
                        hits.Add(new { x = uv.x, y = uv.y, candidates });
                    }
                    occlusions.Add(new { d = route.Distance, shot, bypass = route.OnBypass, hits });
                    File.WriteAllText(Folder + "/occlusions.json", Json(occlusions));
                }
            }
            if (!TimeManager.isGameRunning || elapsed > 420)
            {
                File.WriteAllText(Folder + "/result.json", Json(new { mode = "visual coverage; player colliders disabled and health pinned", elapsed, distance = route.Distance, route.length, completed = Object.FindFirstObjectByType<ChapterProgression>()?.Completed, forks = route.forks.Select(f => new { f.start, f.end, f.decided, f.bypass, f.rewarded }) })); ended = true; finish = EditorApplication.timeSinceStartup + 3.5; return;
            }
            var right = (Vector3)typeof(PlayerScript).GetField("routeRight", Flags).GetValue(player); var origin = (Vector3)typeof(PlayerScript).GetField("routeLaneOrigin", Flags).GetValue(player);
            float lane = Vector3.Dot(player.transform.position - origin, right), desired = run == 0 ? -1.8f : 1.8f;
            typeof(PlayerScript).GetMethod("PlayerMove", Flags).Invoke(player, new object[] { Mathf.Clamp((desired - lane) * 12, -15, 15) });
        }
        catch (Exception e) { File.WriteAllText(Root + "/error.txt", e.ToString()); EditorApplication.update -= Tick; EditorApplication.isPaused = true; }
    }
}
