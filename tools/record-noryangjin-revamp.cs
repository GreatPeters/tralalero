using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;

// Play-mode recording of one ordinary run through the Noryangjin revamp copy scene.
// Real input path (PlayerMove), real collisions and damage; the only helper is steering:
// dodge the wave's danger half and live hose jets, otherwise hold the centre, pick the left reward.
//   editor_play, then: unity command run_script --file tools/record-noryangjin-revamp.cs --entry RecordNoryangjinRevamp.Main --args '["label",2]'
public static class RecordNoryangjinRevamp
{
    public static object Main(string label, float timeScale) => MainAt(label, timeScale, "");

    // Mechanic checks only: start the run at a later stretch instead of the chapter start.
    public static object MainAt(string label, float timeScale, string spot)
    {
        var mover = UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        (Vector3 at, float yaw)? start = spot switch
        {
            "hall" => (new Vector3(124.3f, 0, 75), 180f), "pier" => (new Vector3(118, 0, -348.1f), 270f), "pier-stay" => (new Vector3(118, 0, -348.1f), 270f),
            "auction" => (new Vector3(334, 0, 203.5f), 90f), "boss" => (new Vector3(248, 0, 205), 0f), "gate" => (new Vector3(225.5f, 0, -40), 0f),
            _ => null
        };
        if (start.HasValue)
        {
            var body = mover.GetComponent<Rigidbody>();
            mover.transform.SetPositionAndRotation(start.Value.at + Vector3.up * .1f, Quaternion.Euler(0, start.Value.yaw, 0));
            if (body != null) { body.position = mover.transform.position; body.rotation = mover.transform.rotation; }
            mover.GetType().GetMethod("RebaseRouteFrame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null)?.Invoke(mover, null);
            var cam = GameObject.Find("MapTool_Camera");
            if (cam != null) { var off = new Vector3(.12f, 12.55f, -19.27f); cam.transform.position = mover.transform.position + mover.transform.rotation * off; cam.transform.rotation = mover.transform.rotation * Quaternion.Euler(30, 0, 0); }
        }
        noSteer = spot.EndsWith("-stay");
        return Run(label, timeScale);
    }

    static bool noSteer;
    static object Run(string label, float timeScale)
    {
        if (!EditorApplication.isPlaying || TimeManager.isGameRunning) throw new InvalidOperationException("Play start screen required");
        var p = UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        var director = UnityEngine.Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        if (director == null) throw new InvalidOperationException("Revamp director missing (wrong scene?)");
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var move = p.GetType().GetMethod("PlayerMove", flags);
        var origin = p.GetType().GetField("routeLaneOrigin", flags);
        var right = p.GetType().GetField("routeRight", flags);
        var waves = UnityEngine.Object.FindObjectsByType<NoryangjinWaveEvent>(FindObjectsSortMode.None);
        var hoses = UnityEngine.Object.FindObjectsByType<NoryangjinHoseEvent>(FindObjectsSortMode.None);
        var hazards = GameObject.Find("Noryangjin_MapTool/Props").transform.Cast<Transform>().Where(t => t.name.StartsWith("SR18_L_G")).ToArray();
        var bonuses = UnityEngine.Object.FindObjectsByType<BonusWallChoicePair>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        string folder = "tmp/image-previews/noryangjin-revamp-2026-09-28/" + label;
        Directory.CreateDirectory(folder);
        foreach (var f in Directory.GetFiles(folder)) File.Delete(f);
        double nextCapture = 0; int shots = 0, seen = 0; float started = Time.realtimeSinceStartup;
        EditorApplication.CallbackFunction steer = null;
        steer = () =>
        {
            if (!EditorApplication.isPlaying || p == null) { EditorApplication.update -= steer; return; }
            bool over = p.currentHealth <= 0 || CanvasScript.isGameOver || (UnityEngine.Object.FindFirstObjectByType<ChapterProgression>()?.Completed ?? false);
            if (director.Timeline.Count > seen)
            {
                for (; seen < director.Timeline.Count; seen++)
                    ScreenCapture.CaptureScreenshot($"{folder}/event-{shots++:D3}-{Safe(director.Timeline[seen])}.png");
            }
            if (over || Time.realtimeSinceStartup - started > 600)
            {
                EditorApplication.update -= steer; Time.timeScale = 1;
                ScreenCapture.CaptureScreenshot($"{folder}/end-{shots++:D3}.png");
                File.WriteAllText(folder + "/timeline.txt", string.Join("\n", director.Timeline));
                File.WriteAllText(folder + "/complete.txt",
                    $"health={p.currentHealth:F0}/{p.MaxHealth:F0} cause={p.LastDamageCause} elapsed={director.Elapsed:F1} completed={UnityEngine.Object.FindFirstObjectByType<ChapterProgression>()?.Completed}");
                return;
            }
            if (!TimeManager.isGameRunning) return;
            if (EditorApplication.timeSinceStartup > nextCapture) { nextCapture = EditorApplication.timeSinceStartup + 2.5; ScreenCapture.CaptureScreenshot($"{folder}/frame-{shots++:D3}.png"); }
            if (p.IsWorldYawTurnActive || noSteer) return;
            Vector3 lat = (Vector3)right.GetValue(p);
            float desired = 0, nearest = 26;
            Vector3 fwd = Vector3.ProjectOnPlane(p.transform.forward, Vector3.up).normalized;
            foreach (var pair in bonuses)
            {
                if (pair == null || pair.Selected != null || !pair.Left.gameObject.activeInHierarchy) continue;
                Vector3 delta = (pair.Left.transform.position + pair.Right.transform.position) * .5f - p.transform.position;
                float ahead = Vector3.Dot(delta, fwd);
                if (ahead < -1 || ahead > nearest || Mathf.Abs(delta.y) > 2 || Mathf.Abs(Vector3.Dot(delta, lat)) > 5) continue;
                nearest = ahead;
                float leftLane = Vector3.Dot(pair.Left.transform.position - (Vector3)origin.GetValue(p), lat);
                desired = Mathf.Clamp(leftLane, -1.9f, 1.9f);
            }
            foreach (var hz in hazards)
            {
                if (hz == null || !hz.gameObject.activeInHierarchy) continue;
                Vector3 d = hz.position - p.transform.position; float ah = Vector3.Dot(d, fwd);
                if (ah < -3 || ah > 14 || Mathf.Abs(d.y) > 2 || Mathf.Abs(Vector3.Dot(d, lat)) > 5) continue;
                float off = Vector3.Dot(hz.position - (Vector3)origin.GetValue(p), lat); desired = off >= 0 ? -1.9f : 1.9f;
            }
            foreach (var w in waves)
                foreach (var wave in w.waves)
                    if (wave.warning != null && wave.warning.gameObject.activeInHierarchy) desired = -wave.side * 1.8f;
            foreach (var h in hoses)
            {
                if (!h.Triggered) continue;
                float along = -h.Ahead(p.transform.position);
                foreach (var jet in h.jets)
                    if (jet.along - along > -1 && jet.along - along < 8 && jet.visual != null && jet.visual.gameObject.activeSelf) desired = -jet.side * 1.9f;
            }
            float lane = Vector3.Dot(p.transform.position - (Vector3)origin.GetValue(p), lat);
            float error = desired - lane;
            if (Mathf.Abs(error) > .05f) move.Invoke(p, new object[] { Mathf.Sign(error) * Mathf.Min(15, Mathf.Abs(error) * 12) });
        };
        EditorApplication.update += steer;
        UnityEngine.Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
        Time.timeScale = timeScale;
        return new { started = true, folder, health = p.currentHealth, attack = p.ResolvedAttackDamage };
    }

    static string Safe(string s) => new string(s.Select(c => char.IsLetterOrDigit(c) ? c : '_').Take(48).ToArray());
}
