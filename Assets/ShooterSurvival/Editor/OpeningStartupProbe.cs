#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

// Opt-in Editor probe. SessionState survives Play domain reload; ordinary runs do nothing.
[InitializeOnLoad]
public static class OpeningStartupProbe
{
    private const string Key = "OpeningStartupProbe.";
    private const string Folder = "map-concepts/startup-performance-2026-09-12/";
    [Serializable] private class Sample { public string name; public double totalMs, maxMs; public int calls; }
    [Serializable] private class Report {
        public string label; public bool videoEnabled; public double enteredPlaySeconds, firstTickSeconds, movieReadySeconds;
        public int frames; public double medianFrameMs, p95FrameMs, maxFrameMs; public Sample[] samples;
    }
    private static readonly Dictionary<string, Sample> samples = new();
    private static readonly List<double> frameTimes = new();
    private static double entered = -1, first = -1, ready = -1;
    private static int lastFrame = -1;

    static OpeningStartupProbe() { EditorApplication.playModeStateChanged += StateChanged; EditorApplication.update += Tick; }
    public static object Begin(string label, bool videoEnabled)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Key + "active", false)) throw new InvalidOperationException("Probe requires idle Edit Mode");
        Directory.CreateDirectory(Folder);
        if (File.Exists(Folder + label + ".json")) throw new InvalidOperationException("Keep earlier measurement");
        var opening = UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);
        SessionState.SetBool(Key + "originalVideo", opening.autoPlayMovie);
        SessionState.SetBool(Key + "originalProfiler", Profiler.enabled);
        SessionState.SetBool(Key + "originalDriver", ProfilerDriver.enabled);
        SessionState.SetBool(Key + "originalProfileEditor", ProfilerDriver.profileEditor);
        SessionState.SetString(Key + "label", label);
        SessionState.SetBool(Key + "video", videoEnabled);
        SessionState.SetFloat(Key + "start", (float)EditorApplication.timeSinceStartup);
        SessionState.SetBool(Key + "active", true);
        opening.autoPlayMovie = videoEnabled;
        ProfilerDriver.profileEditor = false;
        ProfilerDriver.enabled = true;
        Profiler.enabled = true;
        EditorApplication.isPlaying = true;
        return "Startup measurement scheduled: " + label;
    }
    private static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key + "active", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) entered = Elapsed();
        if (state == PlayModeStateChange.EnteredEditMode) Restore();
    }
    private static double Elapsed() => EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + "start", 0);
    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "active", false) || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
        if (first < 0) first = Elapsed();
        var opening = UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);
        if (ready < 0 && opening != null && opening.IsMoviePlaying && opening.video.frame > 0) ready = Elapsed();
        int latest = ProfilerDriver.lastFrameIndex;
        int start = lastFrame < 0 ? Math.Max(ProfilerDriver.firstFrameIndex, latest - 40) : lastFrame + 1;
        for (int frame = start; frame <= latest; frame++)
        {
            using var data = ProfilerDriver.GetRawFrameDataView(frame, 0);
            if (!data.valid) continue;
            frameTimes.Add(data.frameTimeMs);
            for (int i = 0; i < data.sampleCount; i++)
            {
                double ms = data.GetSampleTimeMs(i);
                if (ms < .2) continue;
                string name = data.GetSampleName(i);
                if (!samples.TryGetValue(name, out var item)) samples[name] = item = new Sample { name = name };
                item.totalMs += ms; item.maxMs = Math.Max(item.maxMs, ms); item.calls++;
            }
        }
        lastFrame = latest;
        if (Elapsed() - first < 8) return;
        var ordered = frameTimes.OrderBy(v => v).ToArray();
        var report = new Report {
            label = SessionState.GetString(Key + "label", "unknown"), videoEnabled = SessionState.GetBool(Key + "video", true),
            enteredPlaySeconds = entered, firstTickSeconds = first, movieReadySeconds = ready,
            frames = ordered.Length, medianFrameMs = ordered.Length == 0 ? 0 : ordered[ordered.Length / 2],
            p95FrameMs = ordered.Length == 0 ? 0 : ordered[Math.Min(ordered.Length - 1, (int)(ordered.Length * .95))],
            maxFrameMs = ordered.Length == 0 ? 0 : ordered[ordered.Length - 1],
            samples = samples.Values.OrderByDescending(s => s.maxMs).Take(60).ToArray()
        };
        File.WriteAllText(Folder + report.label + ".json", JsonUtility.ToJson(report, true));
        EditorApplication.isPlaying = false;
    }
    public static object Restore()
    {
        var opening = UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);
        if (opening != null) opening.autoPlayMovie = SessionState.GetBool(Key + "originalVideo", true);
        Profiler.enabled = SessionState.GetBool(Key + "originalProfiler", false);
        ProfilerDriver.enabled = SessionState.GetBool(Key + "originalDriver", false);
        ProfilerDriver.profileEditor = SessionState.GetBool(Key + "originalProfileEditor", false);
        SessionState.SetBool(Key + "active", false);
        samples.Clear(); frameTimes.Clear(); entered = first = ready = -1; lastFrame = -1;
        return "Startup probe state restored";
    }
}
#endif
