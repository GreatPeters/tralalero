using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Invoke with official Pipeline run_script while in Play Mode. This is an
// Editor-only bounded presentation stress probe, not an Android frame-rate claim.
public static class MobileCombatPerformanceProbe
{
    public static object VerifyTeardown()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required.");
        const string path = "tmp/mobile-performance-2026-09-13/live-teardown.txt";
        if (File.Exists(path)) throw new InvalidOperationException("Preserve previous teardown evidence.");
        var enemy = Object.FindFirstObjectByType<EnemyScript_space>();
        var data = (EnemySO)new SerializedObject(enemy).FindProperty("enemyData").objectReferenceValue;
        var anchor = new GameObject("Temporary pool teardown anchor");
        EnemyHitEffectPool.Play(data.enemyHitVFX, anchor.transform);
        var pool = Object.FindFirstObjectByType<EnemyHitEffectPool>();
        Object.Destroy(pool.gameObject);
        int frame = Time.frameCount;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; return; }
            if (Time.frameCount < frame + 2) return;
            EditorApplication.update -= tick;
            int children = anchor.transform.childCount;
            bool removed = pool == null;
            EnemyHitEffectPool.Prewarm(data.enemyHitVFX);
            int fresh = Object.FindFirstObjectByType<EnemyHitEffectPool>().transform.childCount;
            File.WriteAllText(path, "oldPoolRemoved=" + removed + "; detachedChildren=" + children + "; freshIdleEffects=" + fresh);
            Object.Destroy(anchor);
        };
        EditorApplication.update += tick;
        return "Waiting for two actual frames.";
    }

    [Serializable]
    private sealed class Report
    {
        public string scene;
        public int frames, popupObjects, retainedHitObjects, visualEvents;
        public float medianFrameMs, p95FrameMs, health, travelled;
        public bool gameRunning;
    }

    public static object Begin(string label, int frameLimit = 600)
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            throw new InvalidOperationException("Unpaused Play Mode required.");
        if (frameLimit < 60 || frameLimit > 3600) throw new ArgumentOutOfRangeException(nameof(frameLimit));
        string folder = "tmp/mobile-performance-2026-09-13/" + label;
        if (Directory.Exists(folder)) throw new InvalidOperationException("Preserve prior evidence labels.");
        Directory.CreateDirectory(folder);
        var player = Object.FindFirstObjectByType<PlayerScript>();
        var canvas = Object.FindFirstObjectByType<CanvasScript>();
        var popups = Object.FindFirstObjectByType<DamagePopupPool>();
        var effects = Object.FindFirstObjectByType<EnemyHitEffectPool>();
        var retained = effects.GetComponentsInChildren<ParticleSystem>(true);
        var enemy = Object.FindFirstObjectByType<EnemyScript_space>();
        var data = (EnemySO)new SerializedObject(enemy).FindProperty("enemyData").objectReferenceValue;
        var start = player.transform.position;
        OpeningStoryUI.Instance?.Skip();
        canvas.PlayerPressedStartButton();
        if (!TimeManager.isGameRunning) throw new InvalidOperationException("Start was blocked.");
        int frames = 0, lastFrame = -1, events = 0;
        var times = new float[frameLimit];
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; return; }
            if (EditorApplication.isPaused || lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                times[frames++] = Time.unscaledDeltaTime * 1000f;
                if (frames % 6 == 0)
                    for (int i = 0; i < 8; i++)
                    {
                        DamagePopupFX.Show(player.transform.position + Vector3.up * 2f, 123 + i);
                        EnemyHitEffectPool.Play(data.enemyHitVFX, player.transform);
                        events++;
                    }
                if (frames < frameLimit) return;
                EditorApplication.update -= tick;
                Array.Sort(times);
                var report = new Report
                {
                    scene = player.gameObject.scene.name, frames = frames, visualEvents = events,
                    medianFrameMs = times[times.Length / 2],
                    p95FrameMs = times[(int)(times.Length * .95f)],
                    popupObjects = popups.transform.childCount,
                    retainedHitObjects = retained.Count(p => p != null),
                    health = player.currentHealth, travelled = Vector3.Distance(start, player.transform.position),
                    gameRunning = TimeManager.isGameRunning
                };
                File.WriteAllText(folder + "/report.json", JsonUtility.ToJson(report, true));
            }
            catch (Exception error)
            {
                EditorApplication.update -= tick;
                File.WriteAllText(folder + "/error.txt", error.ToString());
            }
        };
        EditorApplication.update += tick;
        return new { folder, frameLimit };
    }
}
