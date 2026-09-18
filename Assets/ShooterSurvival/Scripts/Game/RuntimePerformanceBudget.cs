using UnityEngine;

// Limits idle rendering and physics catch-up; it cannot repair an Editor modal stall.
public sealed class RuntimePerformanceBudget : MonoBehaviour
{
    private static RuntimePerformanceBudget instance;
    private int previousFrameRate;
    private float previousMaximumDelta;
    private bool focused = true, paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var root = new GameObject("Runtime performance budget");
        DontDestroyOnLoad(root);
        root.AddComponent<RuntimePerformanceBudget>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        previousFrameRate = Application.targetFrameRate;
        previousMaximumDelta = Time.maximumDeltaTime;
        focused = Application.isFocused;
        Time.maximumDeltaTime = Mathf.Max(Time.fixedDeltaTime, Mathf.Min(previousMaximumDelta, .1f));
        ApplyFrameRate();
    }

    private void OnApplicationFocus(bool value) { focused = value; ApplyFrameRate(); }
    private void OnApplicationPause(bool value) { paused = value; ApplyFrameRate(); }
    private void ApplyFrameRate()
    {
        if (instance != this) return;
        // Desktop VSync is preserved; Android/iOS use this target directly.
        Application.targetFrameRate = (focused || Application.isBatchMode) && !paused ? 60 : 15;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        if (Application.isEditor)
        {
            Application.targetFrameRate = previousFrameRate;
            Time.maximumDeltaTime = previousMaximumDelta;
        }
        instance = null;
    }
}
