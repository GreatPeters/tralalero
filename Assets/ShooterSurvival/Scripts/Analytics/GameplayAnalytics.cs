using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.ShooterSurvival.Analytics
{
    public static class GameplayAnalytics
    {
        public const string OutcomeDeath = "death";
        public const string OutcomeWin = "win";
        public const string OutcomeAbandoned = "abandoned";

        private const string ActiveRoundPlayerPrefsKey =
            "analytics_active_round_v1";

        private static IAnalyticsSink sink;
        private static GameplayRunTracker tracker;
        private static PlayerScript activePlayer;

        public static bool IsRunActive =>
            tracker != null &&
            tracker.IsStarted &&
            !tracker.IsCompleted;

        public static void Initialize(IAnalyticsSink analyticsSink)
        {
            sink = analyticsSink ??
                   throw new ArgumentNullException(nameof(analyticsSink));
            tracker = null;
            activePlayer = null;
        }

        public static void BeginRun(PlayerScript player)
        {
            EnsureInitialized();
            if (sink == null || !sink.IsReady || IsRunActive)
                return;

            RunContext context = CaptureContext();
            tracker = new GameplayRunTracker(sink);
            activePlayer = player;
            tracker.StartRound(
                Guid.NewGuid().ToString("N"),
                context.chapter,
                context.stage,
                context.maxStage,
                context.sceneName,
                context.gameMode);
            SaveCheckpoint();
        }

        public static void RecordCoinEarned(int amount)
        {
            if (amount > 0)
                tracker?.AddEarnedCoins(amount);
        }

        public static void Tick(float unscaledDeltaSeconds, bool isGameRunning)
        {
            tracker?.Tick(unscaledDeltaSeconds, isGameRunning);
        }

        public static void EndRun(string outcome, PlayerScript player)
        {
            if (!IsRunActive)
                return;

            RunContext context = CaptureContext();
            Vector3 endPosition =
                player != null
                    ? player.transform.position
                    : GetActivePlayerPosition();
            UpgradeAnalyticsSnapshot upgrades =
                UpgradeAnalyticsSnapshot.Capture();

            bool completed = tracker.Complete(
                string.IsNullOrWhiteSpace(outcome) ? "unknown" : outcome,
                context.chapter,
                context.stage,
                context.maxStage,
                endPosition.x,
                endPosition.y,
                endPosition.z,
                upgrades.Levels,
                upgrades.FlatValues,
                upgrades.PercentValues,
                context.chapterProgressPercent);

            if (!completed)
            {
                SaveCheckpoint();
                return;
            }

            ClearCheckpoint();
            sink.Flush();
            activePlayer = null;
        }

        public static void SaveCheckpoint()
        {
            if (!IsRunActive)
                return;

            RunContext context = CaptureContext();
            Vector3 position = GetActivePlayerPosition();
            UpgradeAnalyticsSnapshot upgrades =
                UpgradeAnalyticsSnapshot.Capture();
            var checkpoint = new GameplayAnalyticsCheckpoint
            {
                run = tracker.CreateSnapshot(),
                chapter = context.chapter,
                stage = context.stage,
                maxStage = context.maxStage,
                hasChapterProgressPercent =
                    !double.IsNaN(context.chapterProgressPercent) &&
                    !double.IsInfinity(context.chapterProgressPercent),
                chapterProgressPercent =
                    double.IsNaN(context.chapterProgressPercent) ||
                    double.IsInfinity(context.chapterProgressPercent)
                        ? 0d
                        : context.chapterProgressPercent,
                endX = position.x,
                endY = position.y,
                endZ = position.z,
                upgradeLevels = upgrades.Levels,
                upgradeFlat = upgrades.FlatValues,
                upgradePercent = upgrades.PercentValues
            };

            PlayerPrefs.SetString(
                ActiveRoundPlayerPrefsKey,
                JsonUtility.ToJson(checkpoint));
            PlayerPrefs.Save();
        }

        public static void RecoverUnfinishedRun()
        {
            if (!PlayerPrefs.HasKey(ActiveRoundPlayerPrefsKey))
                return;

            if (sink == null || !sink.IsReady)
            {
                DiscardLocalState();
                return;
            }

            try
            {
                GameplayAnalyticsCheckpoint checkpoint =
                    JsonUtility.FromJson<GameplayAnalyticsCheckpoint>(
                        PlayerPrefs.GetString(ActiveRoundPlayerPrefsKey));
                if (checkpoint?.run == null)
                {
                    ClearCheckpoint();
                    return;
                }

                tracker = new GameplayRunTracker(sink);
                if (!tracker.Restore(checkpoint.run) || tracker.IsCompleted)
                {
                    tracker = null;
                    ClearCheckpoint();
                    return;
                }

                bool completed = tracker.Complete(
                    OutcomeAbandoned,
                    checkpoint.chapter,
                    checkpoint.stage,
                    checkpoint.maxStage,
                    checkpoint.endX,
                    checkpoint.endY,
                    checkpoint.endZ,
                    checkpoint.upgradeLevels ?? string.Empty,
                    checkpoint.upgradeFlat ?? string.Empty,
                    checkpoint.upgradePercent ?? string.Empty,
                    checkpoint.hasChapterProgressPercent
                        ? checkpoint.chapterProgressPercent
                        : double.NaN);

                if (completed)
                {
                    ClearCheckpoint();
                    sink.Flush();
                    tracker = null;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[Analytics] Invalid active round checkpoint was cleared: " +
                    exception.Message);
                tracker = null;
                ClearCheckpoint();
            }
        }

        public static void Flush()
        {
            sink?.Flush();
        }

        public static void DiscardLocalState()
        {
            tracker = null;
            activePlayer = null;
            ClearCheckpoint();
        }

        private static void EnsureInitialized()
        {
            if (sink == null)
                FirebaseAnalyticsRuntime.EnsureInstance();
        }

        private static RunContext CaptureContext()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameManager gameManager = GameManager.S;
            int chapter;
            int stage;
            int maxStage;
            string configuredGameMode;
            double chapterProgressPercent = double.NaN;

            if (gameManager != null)
            {
                chapter = gameManager.currentChapter;
                stage = gameManager.currentStage;
                maxStage = gameManager.maxStage;
                configuredGameMode = string.Empty;
            }
            else if (!GameplayAnalyticsSceneContext.TryResolve(
                         activeScene,
                         out chapter,
                         out stage,
                         out maxStage,
                         out configuredGameMode,
                         out chapterProgressPercent))
            {
                chapter = 0;
                stage = 0;
                maxStage = 0;
                configuredGameMode = string.Empty;
            }

            string gameMode = configuredGameMode;
            if (TimeManager.Instance != null)
            {
                gameMode = TimeManager.Instance.isForwardMarchScene
                    ? "forward_march"
                    : "base_defend";
            }
            else if (string.IsNullOrWhiteSpace(gameMode))
            {
                gameMode = "unknown";
            }

            return new RunContext(
                chapter,
                stage,
                maxStage,
                activeScene.name,
                gameMode,
                chapterProgressPercent);
        }

        private static Vector3 GetActivePlayerPosition()
        {
            if (activePlayer == null)
                activePlayer = UnityEngine.Object.FindFirstObjectByType<PlayerScript>();

            return activePlayer != null
                ? activePlayer.transform.position
                : Vector3.zero;
        }

        private static void ClearCheckpoint()
        {
            PlayerPrefs.DeleteKey(ActiveRoundPlayerPrefsKey);
            PlayerPrefs.Save();
        }

        private readonly struct RunContext
        {
            public RunContext(
                int chapter,
                int stage,
                int maxStage,
                string sceneName,
                string gameMode,
                double chapterProgressPercent)
            {
                this.chapter = chapter;
                this.stage = stage;
                this.maxStage = maxStage;
                this.sceneName = sceneName ?? string.Empty;
                this.gameMode = gameMode ?? string.Empty;
                this.chapterProgressPercent = chapterProgressPercent;
            }

            public readonly int chapter;
            public readonly int stage;
            public readonly int maxStage;
            public readonly string sceneName;
            public readonly string gameMode;
            public readonly double chapterProgressPercent;
        }

        [Serializable]
        private sealed class GameplayAnalyticsCheckpoint
        {
            public GameplayRunSnapshot run;
            public int chapter;
            public int stage;
            public int maxStage;
            public bool hasChapterProgressPercent;
            public double chapterProgressPercent;
            public double endX;
            public double endY;
            public double endZ;
            public string upgradeLevels = string.Empty;
            public string upgradeFlat = string.Empty;
            public string upgradePercent = string.Empty;
        }
    }
}
