#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using IndianOceanAssets.ShooterSurvival.Analytics;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameplayAnalyticsTests
{
    private const string ActiveRoundPlayerPrefsKey = "analytics_active_round_v1";

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(ActiveRoundPlayerPrefsKey);
        PlayerPrefs.Save();
        NoryangjinTurnSpot.ResetAllForNewRun();
        TimeManager.isGameRunning = false;
        TimeManager.timeFactor = 1f;
        CanvasScript.isGameOver = false;
    }

    [TearDown]
    public void TearDown()
    {
        GameplayAnalytics.DiscardLocalState();
        NoryangjinTurnSpot.ResetAllForNewRun();
        TimeManager.isGameRunning = false;
        TimeManager.timeFactor = 1f;
        CanvasScript.isGameOver = false;
        ClearUpgradePlayerPrefs();
    }

    [Test]
    public void AnalyticsEventData_CopiesParametersAndPreservesValueKinds()
    {
        var source = new[]
        {
            new AnalyticsParameterValue("text", "value"),
            new AnalyticsParameterValue("count", 12L),
            new AnalyticsParameterValue("ratio", 0.75d)
        };

        var eventData = new AnalyticsEventData("test_event", source);
        source[0] = new AnalyticsParameterValue("changed", "changed");

        Assert.That(eventData.Name, Is.EqualTo("test_event"));
        Assert.That(eventData.Parameters[0].Name, Is.EqualTo("text"));
        Assert.That(eventData.Parameters[0].Kind, Is.EqualTo(AnalyticsParameterKind.String));
        Assert.That(eventData.Parameters[0].StringValue, Is.EqualTo("value"));
        Assert.That(eventData.Parameters[1].Kind, Is.EqualTo(AnalyticsParameterKind.Long));
        Assert.That(eventData.Parameters[1].LongValue, Is.EqualTo(12L));
        Assert.That(eventData.Parameters[2].Kind, Is.EqualTo(AnalyticsParameterKind.Double));
        Assert.That(eventData.Parameters[2].DoubleValue, Is.EqualTo(0.75d));
    }

    [Test]
    public void StartRound_IgnoresSecondStart()
    {
        var sink = new RecordingAnalyticsSink();
        var tracker = new GameplayRunTracker(sink);

        Assert.That(
            tracker.StartRound("first", 1, 2, 10, "Map1", "campaign"),
            Is.True);
        Assert.That(
            tracker.StartRound("second", 8, 9, 20, "Map2", "challenge"),
            Is.False);

        GameplayRunSnapshot snapshot = tracker.CreateSnapshot();
        Assert.That(snapshot.runId, Is.EqualTo("first"));
        Assert.That(snapshot.startChapter, Is.EqualTo(1));
        Assert.That(snapshot.startStage, Is.EqualTo(2));
        Assert.That(snapshot.startMaxStage, Is.EqualTo(10));
        Assert.That(snapshot.sceneName, Is.EqualTo("Map1"));
        Assert.That(snapshot.gameMode, Is.EqualTo("campaign"));
        Assert.That(sink.Events, Has.Count.EqualTo(1));
        Assert.That(sink.Events[0].Name, Is.EqualTo(GameplayRunTracker.StartEventName));
        Assert.That(sink.Events[0].Parameters, Has.Count.EqualTo(7));
        Assert.That(StringParameter(sink.Events[0], "round_id"), Is.EqualTo("first"));
        Assert.That(
            LongParameter(sink.Events[0], "client_event_time_ms"),
            Is.GreaterThan(0));
    }

    [Test]
    public void Tick_OnlyCountsRunningUnscaledTime()
    {
        var tracker = new GameplayRunTracker(new RecordingAnalyticsSink());
        tracker.StartRound("run", 1, 1, 10, "Map1", "campaign");

        tracker.Tick(1.25d, true);
        tracker.Tick(8d, false);
        tracker.Tick(-3d, true);
        tracker.Tick(double.NaN, true);
        tracker.Tick(0.75d, true);

        Assert.That(tracker.ActivePlaySeconds, Is.EqualTo(2d).Within(0.0001d));
    }

    [Test]
    public void AddEarnedCoins_SumsOnlyNonnegativeRewards()
    {
        var tracker = new GameplayRunTracker(new RecordingAnalyticsSink());
        tracker.StartRound("run", 1, 1, 10, "Map1", "campaign");

        tracker.AddEarnedCoins(10);
        tracker.AddEarnedCoins(-50);
        tracker.AddEarnedCoins(0);
        tracker.AddEarnedCoins(7);

        Assert.That(tracker.EarnedCoins, Is.EqualTo(17));
    }

    [Test]
    public void Complete_EmitsFullRoundOnce()
    {
        var sink = new RecordingAnalyticsSink();
        var tracker = new GameplayRunTracker(sink);
        tracker.StartRound("run-42", 2, 3, 10, "Map2", "campaign");
        tracker.Tick(12.5d, true);
        tracker.AddEarnedCoins(125);

        bool first = tracker.Complete(
            "death",
            2,
            7,
            10,
            1.5d,
            2.5d,
            -3d,
            "attack=4",
            "multishot=2",
            "rifle=3");
        bool second = tracker.Complete(
            "victory",
            9,
            9,
            9,
            0d,
            0d,
            0d,
            string.Empty,
            string.Empty,
            string.Empty);

        Assert.That(first, Is.True);
        Assert.That(second, Is.False);
        Assert.That(sink.Events, Has.Count.EqualTo(2));

        AnalyticsEventData startEvent = sink.Events[0];
        Assert.That(startEvent.Name, Is.EqualTo(GameplayRunTracker.StartEventName));
        Assert.That(StringParameter(startEvent, "scene_name"), Is.EqualTo("Map2"));
        Assert.That(StringParameter(startEvent, "game_mode"), Is.EqualTo("campaign"));

        AnalyticsEventData eventData = sink.Events[1];
        Assert.That(eventData.Name, Is.EqualTo(GameplayRunTracker.EndEventName));
        Assert.That(eventData.Parameters, Has.Count.EqualTo(17));
        Assert.That(StringParameter(eventData, "round_id"), Is.EqualTo("run-42"));
        Assert.That(StringParameter(eventData, "outcome"), Is.EqualTo("death"));
        Assert.That(LongParameter(eventData, "chapter"), Is.EqualTo(2));
        Assert.That(LongParameter(eventData, "stage"), Is.EqualTo(7));
        Assert.That(LongParameter(eventData, "max_stage"), Is.EqualTo(10));
        Assert.That(DoubleParameter(eventData, "chapter_progress_pct"), Is.EqualTo(70d));
        Assert.That(LongParameter(eventData, "coins_earned"), Is.EqualTo(125));
        Assert.That(LongParameter(eventData, "play_time_ms"), Is.EqualTo(12500));
        Assert.That(DoubleParameter(eventData, "end_pos_x"), Is.EqualTo(1.5d));
        Assert.That(DoubleParameter(eventData, "end_pos_y"), Is.EqualTo(2.5d));
        Assert.That(DoubleParameter(eventData, "end_pos_z"), Is.EqualTo(-3d));
        Assert.That(
            StringParameter(eventData, "upgrade_levels"),
            Is.EqualTo("attack=4"));
        Assert.That(StringParameter(eventData, "upgrade_flat"), Is.EqualTo("multishot=2"));
        Assert.That(StringParameter(eventData, "upgrade_pct"), Is.EqualTo("rifle=3"));
        Assert.That(
            LongParameter(eventData, "client_event_time_ms"),
            Is.GreaterThanOrEqualTo(
                LongParameter(startEvent, "client_event_time_ms")));
    }

    [Test]
    public void Complete_UsesFiniteChapterProgressOverride()
    {
        var sink = new RecordingAnalyticsSink();
        var tracker = new GameplayRunTracker(sink);
        tracker.StartRound("route", 1, 1, 3, "Map", "forward_march");

        tracker.Complete(
            "death",
            1,
            2,
            3,
            0d,
            0d,
            0d,
            string.Empty,
            string.Empty,
            string.Empty,
            50d);

        Assert.That(
            DoubleParameter(sink.Events[1], "chapter_progress_pct"),
            Is.EqualTo(50d));
    }

    [Test]
    public void OccurrenceTimestamps_UseInjectedClockExactly()
    {
        long[] times = { 1234567890000L, 1234567894321L };
        int clockIndex = 0;
        var sink = new RecordingAnalyticsSink();
        var tracker = new GameplayRunTracker(
            sink,
            () => times[Math.Min(clockIndex++, times.Length - 1)]);

        tracker.StartRound("clocked", 1, 1, 10, "Map", "mode");
        GameplayRunSnapshot snapshot = tracker.CreateSnapshot();
        tracker.Complete(
            "win",
            1,
            2,
            10,
            0d,
            0d,
            0d,
            string.Empty,
            string.Empty,
            string.Empty);

        Assert.That(snapshot.startEventTimeMilliseconds, Is.EqualTo(times[0]));
        Assert.That(
            LongParameter(sink.Events[0], "client_event_time_ms"),
            Is.EqualTo(times[0]));
        Assert.That(
            LongParameter(sink.Events[1], "client_event_time_ms"),
            Is.EqualTo(times[1]));
    }

    [Test]
    public void Snapshot_RoundTripsAndCanCompleteAsAbandoned()
    {
        var original = new GameplayRunTracker(new RecordingAnalyticsSink());
        original.StartRound("resume-me", 3, 4, 12, "Map3", "campaign");
        original.Tick(9.25d, true);
        original.AddEarnedCoins(44);

        string json = JsonUtility.ToJson(original.CreateSnapshot());
        GameplayRunSnapshot stored = JsonUtility.FromJson<GameplayRunSnapshot>(json);

        var sink = new RecordingAnalyticsSink();
        var restored = new GameplayRunTracker(sink);
        Assert.That(restored.Restore(stored), Is.True);
        Assert.That(restored.ActivePlaySeconds, Is.EqualTo(9.25d));
        Assert.That(restored.EarnedCoins, Is.EqualTo(44));

        Assert.That(
            restored.Complete(
                "abandoned",
                3,
                4,
                12,
                0d,
                0d,
                0d,
                "attack=2",
                "none",
                "rifle=1"),
            Is.True);
        Assert.That(sink.Events, Has.Count.EqualTo(1));
        Assert.That(StringParameter(sink.Events[0], "outcome"), Is.EqualTo("abandoned"));
        Assert.That(StringParameter(sink.Events[0], "scene_name"), Is.EqualTo("Map3"));
        Assert.That(StringParameter(sink.Events[0], "game_mode"), Is.EqualTo("campaign"));
    }

    [Test]
    public void RecoverUnfinishedRun_EmitsOneAbandonedEndAndClearsCheckpoint()
    {
        var initialSink = new RecordingAnalyticsSink();
        GameplayAnalytics.Initialize(initialSink);
        GameplayAnalytics.BeginRun(null);
        GameplayAnalytics.Tick(4.25f, true);
        GameplayAnalytics.RecordCoinEarned(19);
        GameplayAnalytics.SaveCheckpoint();

        Assert.That(PlayerPrefs.HasKey(ActiveRoundPlayerPrefsKey), Is.True);
        Assert.That(initialSink.Events, Has.Count.EqualTo(1));

        var recoverySink = new RecordingAnalyticsSink();
        GameplayAnalytics.Initialize(recoverySink);
        GameplayAnalytics.RecoverUnfinishedRun();

        Assert.That(recoverySink.Events, Has.Count.EqualTo(1));
        Assert.That(
            StringParameter(recoverySink.Events[0], "outcome"),
            Is.EqualTo(GameplayAnalytics.OutcomeAbandoned));
        Assert.That(
            LongParameter(recoverySink.Events[0], "coins_earned"),
            Is.EqualTo(19));
        Assert.That(
            LongParameter(recoverySink.Events[0], "play_time_ms"),
            Is.EqualTo(4250));
        Assert.That(recoverySink.FlushCount, Is.EqualTo(1));
        Assert.That(PlayerPrefs.HasKey(ActiveRoundPlayerPrefsKey), Is.False);

        GameplayAnalytics.RecoverUnfinishedRun();
        Assert.That(recoverySink.Events, Has.Count.EqualTo(1));
    }

    [Test]
    public void RecoverUnfinishedRun_ClearsCorruptCheckpoint()
    {
        PlayerPrefs.SetString(ActiveRoundPlayerPrefsKey, "{not-json");
        PlayerPrefs.Save();
        GameplayAnalytics.Initialize(new RecordingAnalyticsSink());

        GameplayAnalytics.RecoverUnfinishedRun();

        Assert.That(PlayerPrefs.HasKey(ActiveRoundPlayerPrefsKey), Is.False);
        Assert.That(GameplayAnalytics.IsRunActive, Is.False);
    }

    [Test]
    public void StartEvent_WaitsForSinkAndLogsOnlyOnce()
    {
        var sink = new RecordingAnalyticsSink { IsReady = false };
        var tracker = new GameplayRunTracker(sink);

        tracker.StartRound("delayed", 1, 1, 10, "Map1", "campaign");
        Assert.That(sink.Events, Is.Empty);

        sink.IsReady = true;
        tracker.Tick(0.1d, false);
        tracker.Tick(0.1d, false);

        Assert.That(sink.Events, Has.Count.EqualTo(1));
        Assert.That(
            sink.Events[0].Name,
            Is.EqualTo(GameplayRunTracker.StartEventName));
    }

    [Test]
    public void RoundEvents_StayWithinFirebaseCustomEventLimits()
    {
        var sink = new RecordingAnalyticsSink();
        var tracker = new GameplayRunTracker(sink);
        tracker.StartRound(
            new string('r', 140),
            1,
            2,
            10,
            new string('s', 140),
            new string('m', 140));
        tracker.Complete(
            new string('o', 140),
            1,
            2,
            10,
            1d,
            2d,
            3d,
            new string('l', 100),
            new string('f', 100),
            new string('p', 100));

        Assert.That(sink.Events, Has.Count.EqualTo(2));
        foreach (AnalyticsEventData eventData in sink.Events)
        {
            Assert.That(eventData.Name.Length, Is.LessThanOrEqualTo(40));
            Assert.That(eventData.Parameters.Count, Is.LessThanOrEqualTo(25));

            foreach (AnalyticsParameterValue parameter in eventData.Parameters)
            {
                Assert.That(parameter.Name.Length, Is.LessThanOrEqualTo(40));
                if (parameter.Kind == AnalyticsParameterKind.String)
                {
                    Assert.That(
                        parameter.StringValue.Length,
                        Is.LessThanOrEqualTo(100));
                }
            }
        }
    }

    [Test]
    public void UpgradeSnapshot_LimitsAllStringValues()
    {
        var snapshot = new UpgradeAnalyticsSnapshot(
            new string('a', 101),
            new string('b', 140),
            null);

        Assert.That(snapshot.Levels, Has.Length.EqualTo(100));
        Assert.That(snapshot.FlatValues, Has.Length.EqualTo(100));
        Assert.That(snapshot.PercentValues, Is.Empty);
    }

    [Test]
    public void FirebaseSink_PersistsEventsBeforeFirebaseIsInitialized()
    {
        string persistenceKey =
            $"analytics_test_queue_{Guid.NewGuid():N}";

        try
        {
            var firstSink = new FirebaseAnalyticsSink(
                persistenceKey,
                true,
                _ => true);
            firstSink.LogEvent(
                new AnalyticsEventData(
                    "game_round_start",
                    new AnalyticsParameterValue("round_id", "first"),
                    new AnalyticsParameterValue("count", 12L),
                    new AnalyticsParameterValue("ratio", 0.75d),
                    new AnalyticsParameterValue(
                        "client_event_time_ms",
                        1234567890L)));
            firstSink.LogEvent(
                new AnalyticsEventData(
                    "game_round_end",
                    new AnalyticsParameterValue("round_id", "second")));

            Assert.That(firstSink.IsReady, Is.True);
            Assert.That(firstSink.PendingEventCount, Is.EqualTo(2));

            var drained = new List<AnalyticsEventData>();
            bool acceptSend = false;
            var reloadedSink = new FirebaseAnalyticsSink(
                persistenceKey,
                true,
                eventData =>
                {
                    if (!acceptSend)
                        return false;

                    drained.Add(eventData);
                    return true;
                });

            reloadedSink.MarkFirebaseReady();
            Assert.That(reloadedSink.PendingEventCount, Is.EqualTo(2));

            acceptSend = true;
            reloadedSink.Flush();

            Assert.That(reloadedSink.PendingEventCount, Is.Zero);
            Assert.That(drained, Has.Count.EqualTo(2));
            Assert.That(drained[0].Name, Is.EqualTo("game_round_start"));
            Assert.That(StringParameter(drained[0], "round_id"), Is.EqualTo("first"));
            Assert.That(LongParameter(drained[0], "count"), Is.EqualTo(12));
            Assert.That(DoubleParameter(drained[0], "ratio"), Is.EqualTo(0.75d));
            Assert.That(
                LongParameter(drained[0], "client_event_time_ms"),
                Is.EqualTo(1234567890L));
            Assert.That(drained[1].Name, Is.EqualTo("game_round_end"));
            Assert.That(StringParameter(drained[1], "round_id"), Is.EqualTo("second"));
            Assert.That(PlayerPrefs.HasKey(persistenceKey), Is.False);
        }
        finally
        {
            PlayerPrefs.DeleteKey(persistenceKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void FirebaseSink_OptOutClearsQueueAndRejectsNewEvents()
    {
        string persistenceKey =
            $"analytics_test_opt_out_{Guid.NewGuid():N}";

        try
        {
            var enabledSink = new FirebaseAnalyticsSink(
                persistenceKey,
                true,
                _ => true);
            enabledSink.LogEvent(
                new AnalyticsEventData(
                    "game_round_start",
                    new AnalyticsParameterValue("round_id", "queued")));
            Assert.That(enabledSink.PendingEventCount, Is.EqualTo(1));

            enabledSink.SetCollectionEnabled(false);
            enabledSink.LogEvent(
                new AnalyticsEventData(
                    "game_round_end",
                    new AnalyticsParameterValue("round_id", "discarded")));

            Assert.That(enabledSink.IsReady, Is.False);
            Assert.That(enabledSink.PendingEventCount, Is.Zero);
            Assert.That(PlayerPrefs.HasKey(persistenceKey), Is.False);

            var reloadedDisabledSink = new FirebaseAnalyticsSink(
                persistenceKey,
                false,
                _ => true);
            Assert.That(reloadedDisabledSink.PendingEventCount, Is.Zero);
        }
        finally
        {
            PlayerPrefs.DeleteKey(persistenceKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void FirebaseSink_EnableAfterNativeReadinessSendsImmediately()
    {
        string persistenceKey =
            $"analytics_test_late_opt_in_{Guid.NewGuid():N}";

        try
        {
            var sent = new List<AnalyticsEventData>();
            var sink = new FirebaseAnalyticsSink(
                persistenceKey,
                false,
                eventData =>
                {
                    sent.Add(eventData);
                    return true;
                });

            sink.MarkFirebaseReady();
            sink.SetCollectionEnabled(true);
            sink.LogEvent(
                new AnalyticsEventData(
                    "game_round_start",
                    new AnalyticsParameterValue("round_id", "late-opt-in")));

            Assert.That(sent, Has.Count.EqualTo(1));
            Assert.That(sink.PendingEventCount, Is.Zero);
        }
        finally
        {
            PlayerPrefs.DeleteKey(persistenceKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void FirebaseSink_EvictsOldestEventsAtQueueLimit()
    {
        string persistenceKey =
            $"analytics_test_eviction_{Guid.NewGuid():N}";

        try
        {
            var queuedSink = new FirebaseAnalyticsSink(
                persistenceKey,
                true,
                _ => true);
            for (int i = 0; i < 130; i++)
            {
                queuedSink.LogEvent(
                    new AnalyticsEventData(
                        "queued_event",
                        new AnalyticsParameterValue("index", (long)i)));
            }

            Assert.That(queuedSink.PendingEventCount, Is.EqualTo(128));

            var drained = new List<AnalyticsEventData>();
            var reloadedSink = new FirebaseAnalyticsSink(
                persistenceKey,
                true,
                eventData =>
                {
                    drained.Add(eventData);
                    return true;
                });
            reloadedSink.MarkFirebaseReady();

            Assert.That(drained, Has.Count.EqualTo(128));
            Assert.That(LongParameter(drained[0], "index"), Is.EqualTo(2));
            Assert.That(LongParameter(drained[127], "index"), Is.EqualTo(129));
        }
        finally
        {
            PlayerPrefs.DeleteKey(persistenceKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void FirebaseSink_DropsMalformedPersistedEvent()
    {
        string persistenceKey =
            $"analytics_test_malformed_{Guid.NewGuid():N}";

        try
        {
            PlayerPrefs.SetString(
                persistenceKey,
                "{\"events\":[{\"eventName\":\"bad\",\"parameters\":[" +
                "{\"parameterName\":\"value\",\"kind\":999}]}]}");
            PlayerPrefs.Save();

            var sink = new FirebaseAnalyticsSink(
                persistenceKey,
                true,
                _ => true);
            sink.MarkFirebaseReady();

            Assert.That(sink.PendingEventCount, Is.Zero);
            Assert.That(PlayerPrefs.HasKey(persistenceKey), Is.False);
        }
        finally
        {
            PlayerPrefs.DeleteKey(persistenceKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void Restore_RejectsInvalidSnapshots()
    {
        GameplayRunSnapshot[] invalidSnapshots =
        {
            null,
            new GameplayRunSnapshot
            {
                schemaVersion = 999,
                isStarted = true,
                runId = "x",
                startEventTimeMilliseconds = 1
            },
            new GameplayRunSnapshot
            {
                isStarted = true,
                runId = string.Empty,
                startEventTimeMilliseconds = 1
            },
            new GameplayRunSnapshot
            {
                isStarted = true,
                runId = "x",
                activePlaySeconds = -1d,
                startEventTimeMilliseconds = 1
            },
            new GameplayRunSnapshot
            {
                isStarted = true,
                runId = "x",
                activePlaySeconds = double.NaN,
                startEventTimeMilliseconds = 1
            },
            new GameplayRunSnapshot
            {
                isStarted = true,
                runId = "x",
                activePlaySeconds = double.PositiveInfinity,
                startEventTimeMilliseconds = 1
            },
            new GameplayRunSnapshot
            {
                isStarted = true,
                runId = "x",
                earnedCoins = -1,
                startEventTimeMilliseconds = 1
            },
            new GameplayRunSnapshot
            {
                isStarted = true,
                runId = "x",
                startEventTimeMilliseconds = 0
            }
        };

        foreach (GameplayRunSnapshot snapshot in invalidSnapshots)
        {
            var tracker = new GameplayRunTracker(new RecordingAnalyticsSink());
            Assert.That(tracker.Restore(snapshot), Is.False);
        }

        var alreadyStarted = new GameplayRunTracker(new RecordingAnalyticsSink());
        alreadyStarted.StartRound("started", 1, 1, 10, "Map", "mode");
        Assert.That(
            alreadyStarted.Restore(
                new GameplayRunSnapshot
                {
                    isStarted = true,
                    runId = "other",
                    startEventTimeMilliseconds = 1
                }),
            Is.False);
    }

    [Test]
    public void UpgradeSnapshot_CapturesStableCodesAndSanitizesInvalidNumbers()
    {
        PlayerPrefs.SetInt("upgrade_lv_1", 3);
        PlayerPrefs.SetInt(
            $"upgrade_stat_type_{UpgradeStatManager.UpgradeType.ATT}",
            (int)global::ValueType.Value);
        PlayerPrefs.SetFloat(
            $"upgrade_stat_{UpgradeStatManager.UpgradeType.ATT}",
            12.5f);
        PlayerPrefs.SetInt(
            $"upgrade_stat_type_{UpgradeStatManager.UpgradeType.ATT_SPEED}",
            (int)global::ValueType.Percent);
        PlayerPrefs.SetFloat(
            $"upgrade_stat_{UpgradeStatManager.UpgradeType.ATT_SPEED}",
            float.NaN);
        PlayerPrefs.Save();

        UpgradeAnalyticsSnapshot snapshot =
            UpgradeAnalyticsSnapshot.CaptureFromSavedValues();

        Assert.That(snapshot.Levels, Does.StartWith("att:3,hp:0,as:0"));
        Assert.That(snapshot.FlatValues, Does.StartWith("att:12.5,hp:0,as:0"));
        Assert.That(snapshot.PercentValues, Does.Contain("as:0"));
        Assert.That(snapshot.Levels, Does.EndWith("bb:0"));
    }

    [Test]
    public void NoryangjinSceneContext_HasNonzeroFallback()
    {
        Assert.That(
            GameplayAnalyticsSceneContext.TryGetDefaultsForSceneName(
                "Noryangjin_MapTool_Mode",
                out int chapter,
                out int stage,
                out int maxStage,
                out string gameMode),
            Is.True);
        Assert.That(chapter, Is.EqualTo(1));
        Assert.That(stage, Is.EqualTo(1));
        Assert.That(maxStage, Is.EqualTo(10));
        Assert.That(gameMode, Is.EqualTo("forward_march"));
    }

    [Test]
    public void NoryangjinMapScene_ResolvesLiveRouteProgress()
    {
        const string scenePath =
            "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity";
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;

        if (openedForTest)
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        try
        {
            Assert.That(
                NoryangjinTurnSpot.TryGetRouteProgress(
                    scene,
                    out int completedCheckpoints,
                    out int totalCheckpoints),
                Is.True);
            Assert.That(completedCheckpoints, Is.Zero);
            Assert.That(totalCheckpoints, Is.EqualTo(2));

            Assert.That(
                GameplayAnalyticsSceneContext.TryResolve(
                    scene,
                    out int chapter,
                    out int stage,
                    out int maxStage,
                    out string gameMode,
                    out double progress),
                Is.True);
            Assert.That(chapter, Is.EqualTo(1));
            Assert.That(stage, Is.EqualTo(1));
            Assert.That(maxStage, Is.EqualTo(3));
            Assert.That(gameMode, Is.EqualTo("forward_march"));
            Assert.That(progress, Is.EqualTo(0d));
        }
        finally
        {
            if (openedForTest && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void NoryangjinSceneContext_UsesConsumedTurnSpotsAsLiveProgress()
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Analytics Route");
            SceneManager.MoveGameObjectToScene(root, scene);
            GameplayAnalyticsSceneContext context =
                root.AddComponent<GameplayAnalyticsSceneContext>();
            context.Configure(2, 8, 10, "forward_march");

            var firstObject = new GameObject("First Checkpoint");
            firstObject.transform.SetParent(root.transform);
            NoryangjinTurnSpot first =
                firstObject.AddComponent<NoryangjinTurnSpot>();
            first.TurnDurationSeconds = 0f;

            var secondObject = new GameObject("Second Checkpoint");
            secondObject.transform.SetParent(root.transform);
            secondObject.AddComponent<NoryangjinTurnSpot>();

            Assert.That(
                GameplayAnalyticsSceneContext.TryResolve(
                    scene,
                    out int chapter,
                    out int stage,
                    out int maxStage,
                    out string gameMode,
                    out double progress),
                Is.True);
            Assert.That(chapter, Is.EqualTo(2));
            Assert.That(stage, Is.EqualTo(8));
            Assert.That(maxStage, Is.EqualTo(10));
            Assert.That(gameMode, Is.EqualTo("forward_march"));
            Assert.That(progress, Is.NaN);

            context.Configure(2, 8, 10, "forward_march", true);
            Assert.That(
                GameplayAnalyticsSceneContext.TryResolve(
                    scene,
                    out chapter,
                    out stage,
                    out maxStage,
                    out gameMode,
                    out progress),
                Is.True);
            Assert.That(chapter, Is.EqualTo(2));
            Assert.That(stage, Is.EqualTo(1));
            Assert.That(maxStage, Is.EqualTo(3));
            Assert.That(gameMode, Is.EqualTo("forward_march"));
            Assert.That(progress, Is.EqualTo(0d));

            var playerObject = new GameObject("Route Player");
            SceneManager.MoveGameObjectToScene(playerObject, scene);
            playerObject.tag = "Player";
            playerObject.AddComponent<Rigidbody>();
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            TimeManager.isGameRunning = true;

            MethodInfo tryActivate = typeof(NoryangjinTurnSpot).GetMethod(
                "TryActivate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tryActivate, Is.Not.Null);
            Assert.That(
                (bool)tryActivate.Invoke(first, new object[] { player }),
                Is.True);

            Assert.That(
                GameplayAnalyticsSceneContext.TryResolve(
                    scene,
                    out chapter,
                    out stage,
                    out maxStage,
                    out gameMode,
                    out progress),
                Is.True);
            Assert.That(stage, Is.EqualTo(2));
            Assert.That(maxStage, Is.EqualTo(3));
            Assert.That(progress, Is.EqualTo(50d));
        }
        finally
        {
            NoryangjinTurnSpot.ResetAllForNewRun();
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void CanvasFallbackStart_ResetsConsumedRouteProgress()
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        GameManager previousGameManager = GameManager.S;
        try
        {
            GameManager.S = null;
            var routeObject = new GameObject("Fallback Route Checkpoint");
            SceneManager.MoveGameObjectToScene(routeObject, scene);
            NoryangjinTurnSpot turnSpot =
                routeObject.AddComponent<NoryangjinTurnSpot>();
            turnSpot.TurnDurationSeconds = 0f;

            var playerObject = new GameObject("Fallback Route Player");
            SceneManager.MoveGameObjectToScene(playerObject, scene);
            playerObject.tag = "Player";
            playerObject.AddComponent<Rigidbody>();
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            TimeManager.isGameRunning = true;

            MethodInfo tryActivate = typeof(NoryangjinTurnSpot).GetMethod(
                "TryActivate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tryActivate, Is.Not.Null);
            Assert.That(
                (bool)tryActivate.Invoke(turnSpot, new object[] { player }),
                Is.True);
            Assert.That(routeObject.activeSelf, Is.False);

            var canvasObject = new GameObject("Fallback Canvas");
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            CanvasScript canvas = canvasObject.AddComponent<CanvasScript>();
            GameplayAnalytics.Initialize(new RecordingAnalyticsSink());

            canvas.PlayerPressedStartButton();

            Assert.That(routeObject.activeSelf, Is.True);
            Assert.That(
                NoryangjinTurnSpot.TryGetRouteProgress(
                    scene,
                    out int completed,
                    out int total),
                Is.True);
            Assert.That(completed, Is.Zero);
            Assert.That(total, Is.EqualTo(1));
        }
        finally
        {
            GameManager.S = previousGameManager;
            NoryangjinTurnSpot.ResetAllForNewRun();
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static string StringParameter(AnalyticsEventData eventData, string name)
    {
        AnalyticsParameterValue parameter = FindParameter(eventData, name);
        Assert.That(parameter.Kind, Is.EqualTo(AnalyticsParameterKind.String));
        return parameter.StringValue;
    }

    private static long LongParameter(AnalyticsEventData eventData, string name)
    {
        AnalyticsParameterValue parameter = FindParameter(eventData, name);
        Assert.That(parameter.Kind, Is.EqualTo(AnalyticsParameterKind.Long));
        return parameter.LongValue;
    }

    private static double DoubleParameter(AnalyticsEventData eventData, string name)
    {
        AnalyticsParameterValue parameter = FindParameter(eventData, name);
        Assert.That(parameter.Kind, Is.EqualTo(AnalyticsParameterKind.Double));
        return parameter.DoubleValue;
    }

    private static AnalyticsParameterValue FindParameter(
        AnalyticsEventData eventData,
        string name)
    {
        for (int i = 0; i < eventData.Parameters.Count; i++)
        {
            if (eventData.Parameters[i].Name == name)
                return eventData.Parameters[i];
        }

        Assert.Fail($"Missing analytics parameter '{name}'.");
        return null;
    }

    private static void ClearUpgradePlayerPrefs()
    {
        Array upgradeTypes = Enum.GetValues(typeof(UpgradeStatManager.UpgradeType));
        for (int i = 0; i < upgradeTypes.Length; i++)
        {
            var type = (UpgradeStatManager.UpgradeType)upgradeTypes.GetValue(i);
            PlayerPrefs.DeleteKey($"upgrade_lv_{i + 1}");
            PlayerPrefs.DeleteKey($"upgrade_stat_type_{type}");
            PlayerPrefs.DeleteKey($"upgrade_stat_{type}");
        }

        PlayerPrefs.Save();
    }

    private sealed class RecordingAnalyticsSink : IAnalyticsSink
    {
        public bool IsReady { get; set; } = true;
        public List<AnalyticsEventData> Events { get; } = new List<AnalyticsEventData>();
        public int FlushCount { get; private set; }

        public void LogEvent(AnalyticsEventData eventData)
        {
            Events.Add(eventData);
        }

        public void Flush()
        {
            FlushCount++;
        }
    }
}
#endif
