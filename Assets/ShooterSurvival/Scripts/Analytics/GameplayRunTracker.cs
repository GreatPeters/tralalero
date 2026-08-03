using System;

namespace IndianOceanAssets.ShooterSurvival.Analytics
{
    [Serializable]
    public sealed class GameplayRunSnapshot
    {
        public int schemaVersion = GameplayRunTracker.SnapshotSchemaVersion;
        public string runId = string.Empty;
        public bool isStarted;
        public bool isCompleted;
        public double activePlaySeconds;
        public long earnedCoins;
        public int startChapter;
        public int startStage;
        public int startMaxStage;
        public string sceneName = string.Empty;
        public string gameMode = string.Empty;
        public bool startEventLogged;
        public long startEventTimeMilliseconds;
    }

    public sealed class GameplayRunTracker
    {
        public const int SnapshotSchemaVersion = 2;
        public const string StartEventName = "game_round_start";
        public const string EndEventName = "game_round_end";
        private const int MaxStringParameterLength = 100;

        private readonly IAnalyticsSink sink;
        private readonly Func<long> utcNowMilliseconds;

        private string runId = string.Empty;
        private bool isStarted;
        private bool isCompleted;
        private double activePlaySeconds;
        private long earnedCoins;
        private int startChapter;
        private int startStage;
        private int startMaxStage;
        private string sceneName = string.Empty;
        private string gameMode = string.Empty;
        private bool startEventLogged;
        private long startEventTimeMilliseconds;

        public GameplayRunTracker(IAnalyticsSink sink) :
            this(
                sink,
                () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        {
        }

        public GameplayRunTracker(
            IAnalyticsSink sink,
            Func<long> clock)
        {
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
            utcNowMilliseconds =
                clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public bool IsStarted => isStarted;
        public bool IsCompleted => isCompleted;
        public double ActivePlaySeconds => activePlaySeconds;
        public long EarnedCoins => earnedCoins;

        public bool StartRound(
            string newRunId,
            int chapter,
            int stage,
            int maxStage,
            string newSceneName,
            string newGameMode)
        {
            if (isStarted || string.IsNullOrWhiteSpace(newRunId))
                return false;

            runId = LimitString(newRunId);
            startChapter = Math.Max(0, chapter);
            startStage = Math.Max(0, stage);
            startMaxStage = Math.Max(0, maxStage);
            sceneName = LimitString(newSceneName);
            gameMode = LimitString(newGameMode);
            startEventTimeMilliseconds = Math.Max(0L, utcNowMilliseconds());
            isStarted = true;
            TryLogStartEvent();
            return true;
        }

        public void Tick(double unscaledDeltaSeconds, bool isGameRunning)
        {
            if (!isStarted || isCompleted)
                return;

            TryLogStartEvent();

            if (!isGameRunning)
                return;

            if (unscaledDeltaSeconds <= 0d ||
                double.IsNaN(unscaledDeltaSeconds) ||
                double.IsInfinity(unscaledDeltaSeconds))
            {
                return;
            }

            activePlaySeconds += unscaledDeltaSeconds;
        }

        public void AddEarnedCoins(long amount)
        {
            if (!isStarted || isCompleted || amount <= 0)
                return;

            earnedCoins =
                amount > long.MaxValue - earnedCoins
                    ? long.MaxValue
                    : earnedCoins + amount;
        }

        public GameplayRunSnapshot CreateSnapshot()
        {
            return new GameplayRunSnapshot
            {
                schemaVersion = SnapshotSchemaVersion,
                runId = runId,
                isStarted = isStarted,
                isCompleted = isCompleted,
                activePlaySeconds = activePlaySeconds,
                earnedCoins = earnedCoins,
                startChapter = startChapter,
                startStage = startStage,
                startMaxStage = startMaxStage,
                sceneName = sceneName,
                gameMode = gameMode,
                startEventLogged = startEventLogged,
                startEventTimeMilliseconds = startEventTimeMilliseconds
            };
        }

        public bool Restore(GameplayRunSnapshot snapshot)
        {
            if (isStarted ||
                snapshot == null ||
                snapshot.schemaVersion != SnapshotSchemaVersion ||
                !snapshot.isStarted ||
                string.IsNullOrWhiteSpace(snapshot.runId) ||
                snapshot.activePlaySeconds < 0d ||
                double.IsNaN(snapshot.activePlaySeconds) ||
                double.IsInfinity(snapshot.activePlaySeconds) ||
                snapshot.earnedCoins < 0 ||
                snapshot.startEventTimeMilliseconds <= 0)
            {
                return false;
            }

            runId = snapshot.runId;
            isStarted = true;
            isCompleted = snapshot.isCompleted;
            activePlaySeconds = snapshot.activePlaySeconds;
            earnedCoins = snapshot.earnedCoins;
            startChapter = snapshot.startChapter;
            startStage = snapshot.startStage;
            startMaxStage = snapshot.startMaxStage;
            sceneName = snapshot.sceneName ?? string.Empty;
            gameMode = snapshot.gameMode ?? string.Empty;
            startEventLogged = snapshot.startEventLogged;
            startEventTimeMilliseconds = snapshot.startEventTimeMilliseconds;
            return true;
        }

        public bool Complete(
            string outcome,
            int chapter,
            int stage,
            int maxStage,
            double endX,
            double endY,
            double endZ,
            string permanentUpgradeSummary,
            string roundUpgradeSummary,
            string weaponUpgradeSummary,
            double chapterProgressPercentOverride = double.NaN)
        {
            if (!isStarted || isCompleted || !sink.IsReady)
                return false;

            TryLogStartEvent();

            double chapterProgressPercent =
                !double.IsNaN(chapterProgressPercentOverride) &&
                !double.IsInfinity(chapterProgressPercentOverride)
                    ? Math.Max(
                        0d,
                        Math.Min(100d, chapterProgressPercentOverride))
                    : maxStage > 0
                    ? Math.Max(0d, Math.Min(100d, (double)stage / maxStage * 100d))
                    : 0d;

            var eventData = new AnalyticsEventData(
                EndEventName,
                new AnalyticsParameterValue("round_id", runId),
                new AnalyticsParameterValue("scene_name", sceneName),
                new AnalyticsParameterValue("game_mode", gameMode),
                new AnalyticsParameterValue("outcome", LimitString(outcome)),
                new AnalyticsParameterValue("chapter", (long)Math.Max(0, chapter)),
                new AnalyticsParameterValue("stage", (long)Math.Max(0, stage)),
                new AnalyticsParameterValue("max_stage", (long)Math.Max(0, maxStage)),
                new AnalyticsParameterValue(
                    "chapter_progress_pct",
                    chapterProgressPercent),
                new AnalyticsParameterValue("coins_earned", earnedCoins),
                new AnalyticsParameterValue("play_time_ms", ToMilliseconds(activePlaySeconds)),
                new AnalyticsParameterValue("end_pos_x", FiniteOrZero(endX)),
                new AnalyticsParameterValue("end_pos_y", FiniteOrZero(endY)),
                new AnalyticsParameterValue("end_pos_z", FiniteOrZero(endZ)),
                new AnalyticsParameterValue(
                    "upgrade_levels",
                    LimitString(permanentUpgradeSummary)),
                new AnalyticsParameterValue(
                    "upgrade_flat",
                    LimitString(roundUpgradeSummary)),
                new AnalyticsParameterValue(
                    "upgrade_pct",
                    LimitString(weaponUpgradeSummary)),
                new AnalyticsParameterValue(
                    "client_event_time_ms",
                    Math.Max(0L, utcNowMilliseconds())));

            sink.LogEvent(eventData);
            isCompleted = true;
            return true;
        }

        private void TryLogStartEvent()
        {
            if (startEventLogged || !sink.IsReady)
                return;

            sink.LogEvent(
                new AnalyticsEventData(
                    StartEventName,
                    new AnalyticsParameterValue("round_id", runId),
                    new AnalyticsParameterValue("scene_name", sceneName),
                    new AnalyticsParameterValue("game_mode", gameMode),
                    new AnalyticsParameterValue("chapter", (long)startChapter),
                    new AnalyticsParameterValue("stage", (long)startStage),
                    new AnalyticsParameterValue("max_stage", (long)startMaxStage),
                    new AnalyticsParameterValue(
                        "client_event_time_ms",
                        startEventTimeMilliseconds)));
            startEventLogged = true;
        }

        private static long ToMilliseconds(double seconds)
        {
            double milliseconds = seconds * 1000d;
            if (milliseconds >= long.MaxValue)
                return long.MaxValue;

            return (long)Math.Round(milliseconds, MidpointRounding.AwayFromZero);
        }

        private static double FiniteOrZero(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value)
                ? 0d
                : value;
        }

        private static string LimitString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= MaxStringParameterLength
                ? value
                : value.Substring(0, MaxStringParameterLength);
        }
    }
}
