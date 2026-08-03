using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.ShooterSurvival.Analytics
{
    public sealed class GameplayAnalyticsSceneContext : MonoBehaviour
    {
        private const string NoryangjinScenePrefix = "Noryangjin_";

        [SerializeField, Min(1)] private int chapter = 1;
        [SerializeField, Min(1)] private int stage = 1;
        [SerializeField, Min(1)] private int maxStage = 10;
        [SerializeField] private string gameMode = "forward_march";
        [SerializeField] private bool useTurnSpotsForProgress;

        public int Chapter => chapter;
        public int Stage => stage;
        public int MaxStage => maxStage;
        public string GameMode => gameMode;
        public bool UseTurnSpotsForProgress => useTurnSpotsForProgress;

        public void Configure(
            int newChapter,
            int newStage,
            int newMaxStage,
            string newGameMode = "forward_march",
            bool newUseTurnSpotsForProgress = false)
        {
            chapter = Mathf.Max(1, newChapter);
            stage = Mathf.Max(1, newStage);
            maxStage = Mathf.Max(stage, newMaxStage);
            gameMode = string.IsNullOrWhiteSpace(newGameMode)
                ? "unknown"
                : newGameMode;
            useTurnSpotsForProgress = newUseTurnSpotsForProgress;
        }

        public static bool TryResolve(
            Scene scene,
            out int resolvedChapter,
            out int resolvedStage,
            out int resolvedMaxStage,
            out string resolvedGameMode)
        {
            return TryResolve(
                scene,
                out resolvedChapter,
                out resolvedStage,
                out resolvedMaxStage,
                out resolvedGameMode,
                out _);
        }

        public static bool TryResolve(
            Scene scene,
            out int resolvedChapter,
            out int resolvedStage,
            out int resolvedMaxStage,
            out string resolvedGameMode,
            out double resolvedChapterProgressPercent)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    GameplayAnalyticsSceneContext context =
                        root.GetComponentInChildren<GameplayAnalyticsSceneContext>(true);
                    if (context == null)
                        continue;

                    resolvedChapter = context.chapter;
                    resolvedStage = context.stage;
                    resolvedMaxStage = context.maxStage;
                    resolvedGameMode = context.gameMode ?? string.Empty;
                    resolvedChapterProgressPercent = double.NaN;
                    if (context.useTurnSpotsForProgress)
                    {
                        ApplyNoryangjinRouteProgress(
                            scene,
                            ref resolvedStage,
                            ref resolvedMaxStage,
                            ref resolvedChapterProgressPercent);
                    }
                    return true;
                }
            }

            bool hasDefaults = TryGetDefaultsForSceneName(
                scene.name,
                out resolvedChapter,
                out resolvedStage,
                out resolvedMaxStage,
                out resolvedGameMode);
            resolvedChapterProgressPercent = double.NaN;
            if (hasDefaults)
            {
                ApplyNoryangjinRouteProgress(
                    scene,
                    ref resolvedStage,
                    ref resolvedMaxStage,
                    ref resolvedChapterProgressPercent);
            }

            return hasDefaults;
        }

        public static bool TryGetDefaultsForSceneName(
            string sceneName,
            out int resolvedChapter,
            out int resolvedStage,
            out int resolvedMaxStage,
            out string resolvedGameMode)
        {
            if (!string.IsNullOrEmpty(sceneName) &&
                sceneName.StartsWith(
                    NoryangjinScenePrefix,
                    StringComparison.Ordinal))
            {
                resolvedChapter = 1;
                resolvedStage = 1;
                resolvedMaxStage = 10;
                resolvedGameMode = "forward_march";
                return true;
            }

            resolvedChapter = 0;
            resolvedStage = 0;
            resolvedMaxStage = 0;
            resolvedGameMode = string.Empty;
            return false;
        }

        private static void ApplyNoryangjinRouteProgress(
            Scene scene,
            ref int resolvedStage,
            ref int resolvedMaxStage,
            ref double resolvedChapterProgressPercent)
        {
            if (!NoryangjinTurnSpot.TryGetRouteProgress(
                    scene,
                    out int completedCheckpointCount,
                    out int totalCheckpointCount))
            {
                return;
            }

            resolvedStage = completedCheckpointCount + 1;
            resolvedMaxStage = totalCheckpointCount + 1;
            resolvedChapterProgressPercent =
                (double)completedCheckpointCount /
                totalCheckpointCount *
                100d;
        }
    }
}
