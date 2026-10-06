using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Essential proposal 13: in-chapter progress from each chapter's real structure, not elapsed time.
//   Ch1 Noryangjin : passed route turn checkpoints (NoryangjinTurnSpot) of all checkpoints + finish
//   Ch2 Highway    : HighwayRoute.Distance from the run's start to the finish trigger on the route
//   Ch3 RestStop   : RoutePolyline distance from the run's start to the finish trigger (the polyline starts
//                    375 m behind the shark and runs past the finish, so neither 0 nor Length is the goal;
//                    the holdout sits mid-route and simply holds progress while the shark is stopped)
//   Ch4/5          : Chapter45Director.Distance / Chapter45Route.Length (escalator jumps never go back)
// Progress never moves backwards within a run and only reaches 1 when the chapter is completed.
public sealed class ChapterRunProgress
{
    public const float BeforeFinishCap = .99f;
    public const string BestKeyPrefix = "chapter_best_progress_";
    private float shown, start = float.NaN;

    public float Value => shown;

    public void Reset() { shown = 0f; start = float.NaN; }

    public struct Reading
    {
        public bool isFraction;   // value already 0..1 (checkpoint count)
        public float value, goal; // otherwise a route distance and the finish distance
    }

    public float Update(Reading reading, bool completed)
    {
        float fraction = reading.value;
        if (!reading.isFraction)
        {
            if (float.IsNaN(start)) start = reading.value; // the run begins wherever the shark stands
            fraction = Span(start, reading.goal, reading.value);
        }
        shown = Advance(shown, fraction, completed);
        return shown;
    }

    public float Complete() => shown = 1f;

    public static float Span(float start, float goal, float distance)
        => goal - start <= .01f ? 0f : Mathf.Clamp01((distance - start) / (goal - start));

    public static float Advance(float previous, float measured, bool completed)
    {
        if (completed) return 1f;
        if (float.IsNaN(measured) || float.IsInfinity(measured)) measured = 0f;
        return Mathf.Max(Mathf.Clamp01(previous), Mathf.Clamp(measured, 0f, BeforeFinishCap));
    }

    public static float Checkpoints(int completed, int total)
        => total <= 0 ? 0f : Mathf.Clamp01(completed / (float)(total + 1)); // +1: last leg to the finish

    private static float FinishDistance(UnityEngine.SceneManagement.Scene scene, System.Func<Vector3, float> project, float fallback)
    {
        float best = float.NaN;
        foreach (var end in GameObject.FindGameObjectsWithTag("GameEndTriggerTag"))
        {
            if (end.scene != scene) continue;
            float d = project(end.transform.position);
            if (float.IsNaN(best) || d > best) best = d;
        }
        return float.IsNaN(best) ? fallback : best;
    }

    // Reads the current scene. Returns false when the scene has no recognised progress source.
    public static bool TryMeasure(ChapterProgression progression, PlayerScript player, out Reading reading)
    {
        reading = default;
        if (progression == null) return false;
        var scene = progression.gameObject.scene;
        var chapter45 = Object.FindFirstObjectByType<Chapter45Director>();
        if (chapter45 != null && chapter45.route != null && chapter45.gameObject.scene == scene)
        {
            reading = new Reading { value = chapter45.Distance, goal = chapter45.route.Length };
            return true;
        }
        var highway = Object.FindFirstObjectByType<HighwayRoute>();
        if (highway != null && highway.gameObject.scene == scene)
        {
            reading = new Reading { value = highway.Distance, goal = FinishDistance(scene, highway.NearestDistance, highway.length) };
            return true;
        }
        var polyline = Object.FindFirstObjectByType<RoutePolyline>();
        if (polyline != null && polyline.gameObject.scene == scene && player != null)
        {
            reading = new Reading
            {
                value = polyline.NearestDistance(player.transform.position, player.transform.forward),
                goal = FinishDistance(scene, p => polyline.NearestDistance(p, Vector3.zero), polyline.Length),
            };
            return true;
        }
        if (NoryangjinTurnSpot.TryGetRouteProgress(scene, out int done, out int total))
        {
            reading = new Reading { isFraction = true, value = Checkpoints(done, total) };
            return true;
        }
        return false;
    }
    public static string BestKey(int chapter) => BestKeyPrefix + Mathf.Clamp(chapter, 1, 5);

    public static float LoadBest(int chapter) => Mathf.Clamp01(PlayerPrefs.GetFloat(BestKey(chapter), 0f));

    public static bool SaveBestIfHigher(int chapter, float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        if (fraction <= LoadBest(chapter) + .0001f) return false;
        PlayerPrefs.SetFloat(BestKey(chapter), fraction);
        PlayerPrefs.Save();
        return true;
    }

    // "Best record" = furthest point reached: the highest chapter with any progress and its share.
    public static string BestRecordText()
    {
        int unlocked = Mathf.Clamp(PlayerPrefs.GetInt("chapter_unlocked", 1), 1, 5);
        for (int chapter = 5; chapter >= 1; chapter--)
        {
            float best = LoadBest(chapter);
            // No middle dot: the settings font has no glyph for it (renders as a box).
            if (best > 0f) return $"최고 기록  챕터 {chapter} / {Mathf.FloorToInt(best * 100f)}%";
        }
        return unlocked > 1 ? $"최고 기록  챕터 {unlocked - 1} 완주" : "최고 기록  아직 없음";
    }
}