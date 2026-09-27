using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
public static class InspectHoldoutRevision
{
    public const string Folder = "outputs/reststop-holdout-2026-09-25";
    public static object Main()
    {
        Directory.CreateDirectory(Folder);
        var h = UnityEngine.Object.FindFirstObjectByType<RestStopHoldout>();
        var p = UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        var c = Camera.main;
        var report = new { playing = EditorApplication.isPlaying, paused = EditorApplication.isPaused,
            scene = SceneManager.GetActiveScene().path, h.Active, h.Elapsed, h.Duration, h.Spawned, h.Killed,
            center = h.center.position.ToString("F3"), player = p.transform.position.ToString("F3"),
            camera = c.transform.position.ToString("F3"), rotation = c.transform.eulerAngles.ToString("F3"), c.fieldOfView, c.aspect,
            cameraComponents = c.GetComponents<Component>().Select(x => x.GetType().Name).ToArray(),
            entrances = h.entrances.Select(x => new { position = x.position.ToString("F3"), distance = Vector3.Distance(x.position, h.center.position) }).ToArray(),
            police = h.police.Select(x => new { x.name, active = x.gameObject.activeSelf, state = x.RuntimeState.ToString(), x.MoveSpeed,
                position = x.transform.position.ToString("F3"), health = x.GetComponent<EnemyScript_space>().CurrentHealth }).ToArray(),
            settings = new[] { "reststopHoldoutSeconds", "reststopPoliceHealth", "reststopPoliceSpeed", "reststopPoliceInterval" }.Select(x => new { key = x, value = HighwayRoute.Setting(x, -1, -100, 10000) }).ToArray()
        };
        File.WriteAllText(Folder + "/before-state.json", (string)AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { report }));
        if (!EditorApplication.isPlaying && !File.Exists(Folder + "/preferences-before.tsv")) ChapterPlaytestPreferences.SnapshotAt(Folder + "/preferences-before.tsv");
        if (!File.Exists(Folder + "/RestStop-before.unity")) File.Copy(SceneManager.GetActiveScene().path, Folder + "/RestStop-before.unity");
        Capture(c, Folder + "/before.png");
        return report;
    }
    public static void Capture(Camera c, string path)
    {
        var target = new RenderTexture(720, 1560, 24); target.Create();
        var old = c.targetTexture; var active = RenderTexture.active; float aspect = c.aspect;
        try { c.targetTexture = target; c.aspect = 720f / 1560; c.Render(); RenderTexture.active = target;
            var image = new Texture2D(720, 1560, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 720, 1560), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image); }
        finally { c.targetTexture = old; c.aspect = aspect; RenderTexture.active = active; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
    }
}
