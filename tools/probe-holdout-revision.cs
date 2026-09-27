using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using IndianOceanAssets.ShooterSurvival;
public static class ProbeHoldoutRevision
{
    public static object Manual()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused) throw new Exception("Unpaused Play Mode required");
        var canvas = UnityEngine.Object.FindFirstObjectByType<CanvasScript>();
        OpeningStoryUI.Instance?.Skip(); canvas.PlayerPressedStartButton();
        var h = UnityEngine.Object.FindFirstObjectByType<RestStopHoldout>();
        var p = UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        p.ApplyContinuousRoutePose(h.center.position + Vector3.up * .145f, h.center.forward, 0);
        Time.timeScale = 1; TimeManager.timeFactor = 1;
        return new { health = p.currentHealth, input = "actual mouse/touch", controlledPosition = true };
    }
    public static object State()
    {
        var h = UnityEngine.Object.FindFirstObjectByType<RestStopHoldout>();
        var p = UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        return new { h.Active, h.AimYaw, h.Elapsed, h.Spawned, h.Killed, position = p.transform.position.ToString("F4"), locked = h.LockedPosition.ToString("F4"), p.currentHealth,
            roofHidden = h.overheadOccluders.Count(r => r.forceRenderingOff), Camera.main.fieldOfView };
    }
    public static object Screenshot(string name)
    {
        string path = "tmp/image-previews/reststop-holdout-2026-09-25/" + name + ".png";
        if (File.Exists(path)) throw new Exception("Preserve previous screenshot");
        ScreenCapture.CaptureScreenshot(path);
        return new { path, state = State() };
    }
    public static object Main(string label = "r1", float seconds = 63)
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || TimeManager.isGameRunning) throw new Exception("Fresh unpaused Play Mode lobby required");
        string folder = "tmp/image-previews/reststop-holdout-2026-09-25/" + label;
        if (Directory.Exists(folder)) throw new Exception("Preserve previous evidence");
        Directory.CreateDirectory(folder);
        var canvas = UnityEngine.Object.FindFirstObjectByType<CanvasScript>();
        canvas.StartCoroutine(Run(canvas, folder, seconds));
        return new { folder, controlledHealth = 100000, seconds };
    }
    static IEnumerator Run(CanvasScript canvas, string folder, float seconds)
    {
        var errors = new List<string>();
        Application.LogCallback log = (m, s, t) => { if (t == LogType.Error || t == LogType.Exception || t == LogType.Assert) errors.Add(m); };
        Application.logMessageReceived += log;
        OpeningStoryUI.Instance?.Skip(); canvas.PlayerPressedStartButton(); yield return null;
        var h = UnityEngine.Object.FindFirstObjectByType<RestStopHoldout>();
        var p = UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        p.ApplyContinuousRoutePose(h.center.position + Vector3.up * .145f, h.center.forward, 0);
        p.currentHealth = 100000; Time.timeScale = 1; TimeManager.timeFactor = 1;
        var initial = p.transform.position; float nextCapture = 1, began = Time.time, maxDrift = 0, maxRate = 0;
        int frame = 0, peakAlive = 0, visibleApproaches = 0; float previousYaw = h.AimYaw; bool hadActive = false;
        var samples = new List<object>();
        try
        {
            while (Time.time - began < seconds)
            {
                yield return null;
                if (h.Active)
                {
                    if (!hadActive) { previousYaw = h.AimYaw; hadActive = true; }
                    float delta = Time.deltaTime * TimeManager.timeFactor;
                    // A controlled alternating drag command, through the same rotation path as touch.
                    float input = h.Elapsed < 6 ? -1 : h.Elapsed < 12 ? 1 : 1;
                    h.RotateAim(input, delta);
                    if (delta > .00001f) maxRate = Mathf.Max(maxRate, Mathf.Abs(Mathf.DeltaAngle(previousYaw, h.AimYaw)) / delta);
                    previousYaw = h.AimYaw;
                    maxDrift = Mathf.Max(maxDrift, Vector3.Distance(p.transform.position, h.LockedPosition));
                    int living = h.police.Count(x => x.gameObject.activeInHierarchy && x.RuntimeState != EnemyEventRuntimeState.Dead);
                    peakAlive = Mathf.Max(peakAlive, living);
                    visibleApproaches = h.entrances.Count(x => { var v = Camera.main.WorldToViewportPoint(x.position + Vector3.up); return v.z > 0 && v.x > 0 && v.x < 1 && v.y > 0 && v.y < .8f; });
                }
                if (Time.time - began >= nextCapture)
                {
                    nextCapture += 1;
                    Capture(Camera.main, folder + "/frame-" + frame++.ToString("D3") + ".png");
                    samples.Add(new { seconds = Time.time - began, h.Active, h.Elapsed, h.AimYaw, h.Spawned, h.Killed,
                        living = h.police.Count(x => x.gameObject.activeInHierarchy && x.RuntimeState != EnemyEventRuntimeState.Dead), visibleApproaches, health = p.currentHealth });
                }
            }
            var ui = new List<object>();
            if (EventSystem.current != null) foreach (var point in new[] { new Vector2(.5f, .45f), new Vector2(.2f, .35f), new Vector2(.8f, .4f) })
            {
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point * new Vector2(Screen.width, Screen.height) }, hits);
                ui.Add(new { point = point.ToString(), hits = hits.Select(x => x.gameObject.name).ToArray() });
            }
            var result = new { controlled = true, healthOverride = 100000, seconds = Time.time - began,
                h.Completed, h.Active, h.Elapsed, h.Duration, h.Spawned, h.Killed, h.SpawnedByDoor, peakAlive, maxDrift, maxRate,
                unlocked = !p.IsStationaryCombat, finalHealth = p.currentHealth, shots = p.GetComponentsInChildren<WeaponScript>(true).Sum(x => x.TotalProjectilesSpawned), errors, ui, samples };
            File.WriteAllText(folder + "/result.json", Json(result));
        }
        finally { Application.logMessageReceived -= log; Time.timeScale = 1; EditorApplication.isPaused = true; }
    }
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    static void Capture(Camera c, string path)
    {
        var rt = new RenderTexture(720, 1560, 24); rt.Create(); var old = c.targetTexture; var active = RenderTexture.active;
        try { c.targetTexture = rt; c.Render(); RenderTexture.active = rt;
            var image = new Texture2D(720, 1560, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 720, 1560), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); UnityEngine.Object.Destroy(image); }
        finally { c.targetTexture = old; RenderTexture.active = active; rt.Release(); UnityEngine.Object.Destroy(rt); }
    }
}
