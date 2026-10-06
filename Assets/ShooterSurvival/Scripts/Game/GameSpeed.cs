using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Single owner of Time.timeScale (essential proposal 10, 2026-10-06).
// Effective scale = editor test base × player X1/X2 × temporary event slow-down (highway choice popup).
// Section speed-ups such as the highway open road (1.4) multiply forward speed, so X2 turns them into 2.8.
// Pausing uses TimeManager.timeFactor = 0, which freezes gameplay regardless of this scale.
// Timers that use scaled time (shutter countdown, cooldowns, chapter clocks) run faster at X2.
public static class GameSpeed
{
    public const float Normal = 1f, Fast = 2f;
    private static float user = Normal, eventScale = 1f, testBase = 1f, sectionScale = 1f;
    private static bool hooked, running;

    // Chapter section speed (highway open road 1.4). Like X1/X2 it only applies while a run is live.
    public static float SectionScale => sectionScale;
    public static void SetSectionScale(float value)
    {
        value = Mathf.Clamp(value, .1f, 4f);
        if (Mathf.Approximately(value, sectionScale)) return;
        sectionScale = value;
        Apply();
    }

    public static event Action Changed;
    public static float UserScale => user;
    public static bool IsFast => user > Normal;
    public static float EventScale => eventScale;
    // The player's choice is remembered, but only multiplies time while a run is live.
    public static float Effective => Compose(testBase, running ? user * sectionScale : Normal, eventScale);

    public static void SetRunning(bool live)
    {
        if (running == live) return;
        running = live;
        Apply();
    }

    public static float Compose(float testBaseScale, float userScale, float eventSlow)
        => Mathf.Max(0f, testBaseScale) * Mathf.Max(0f, userScale) * Mathf.Max(0f, eventSlow);

    public static void SetUserScale(float value)
    {
        float next = value > (Normal + Fast) * .5f ? Fast : Normal;
        if (Mathf.Approximately(next, user)) return;
        user = next;
        Apply();
        Changed?.Invoke();
    }
    public static void Toggle() => SetUserScale(IsFast ? Normal : Fast);

    public static void SetEventScale(float value) { eventScale = Mathf.Clamp(value, .01f, 1f); Apply(); }
    public static void ClearEventScale() { eventScale = 1f; Apply(); }

    // Editor map-tool test speed (1×/2×/3×). Never set by player builds.
    public static float TestBaseScale
    {
        get => testBase;
        set { testBase = Mathf.Max(.1f, value); Apply(); }
    }

    public static void Apply() => Time.timeScale = Effective;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        user = Normal; eventScale = 1f; testBase = 1f; sectionScale = 1f; running = false; Changed = null;
        SceneManager.sceneLoaded -= SceneLoaded;
        hooked = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        if (hooked) return;
        hooked = true;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    // A scene change ends any section slow-down; the player's X1/X2 choice is kept.
    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        eventScale = 1f; sectionScale = 1f;
        Apply();
    }
}
