using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Directed native campaign/transaction fixture. Never ordinary combat evidence.
// Root: Chapters45Playtest.Prepare("Jamsil", 0), enter fresh Play lobby, Begin().
// Poll() until complete, exit Play, then Chapters45Playtest.Restore().
// No scene/asset writes. Preference mutations require the existing full snapshot.
public static class Chapters45CampaignTransactions
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static PlayerScript player;
    static CanvasScript canvas;
    static ChapterProgression chapter, departing;
    static Chapter45Director director;
    static Chapter45Goal goal;
    static int state, frame, failures, priorHandle, initialCoins, initialJewels;
    static int transitionCoins, transitionJewels, goalCoins, goalJewels, firstTowerCoins, firstTowerJewels;
    static int expectedGoalReward, firstTowerReward, replayTowerReward;
    static bool active, transitionHasMovie, skipInvoked;
    static string folder, phase, destination;
    static double startedAt, stateAt;
    static Vector3 offeringRestScale;
    static readonly List<object> checks = new();
    static readonly List<string> errors = new();
    static readonly List<string> images = new();
    static double Age => EditorApplication.timeSinceStartup - stateAt;
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    static void Write(string file, object value) => File.WriteAllText(Path.Combine(folder, file), Json(value));
    static float[] V(Vector3 p) => new[] { p.x, p.y, p.z };
    static void Property(object owner, string name, object value) => owner.GetType().GetProperty(name).GetSetMethod(true).Invoke(owner, new[] { value });

    public static object Begin()
    {
        string prepared = SessionState.GetString("Chapter45.QA.folder", "");
        if (active || !EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Jamsil" || TimeManager.isGameRunning || CanvasScript.isGameOver)
            throw new InvalidOperationException("Fresh prepared Jamsil Play lobby required; never interrupt an ordinary run.");
        string snapshot = Path.Combine(prepared, "before-prefs.tsv");
        if (!File.Exists(snapshot) || File.Exists(Path.Combine(prepared, "restored.json")))
            throw new InvalidOperationException("Prepare a new unrestored Chapters45Playtest snapshot first.");
        var snapshotKeys = new HashSet<string>(File.ReadAllLines(snapshot).Select(line => line.Split('\t')[0]));
        var required = new List<string> { "coin", "jewel", "chapter_unlocked" };
        for (int i = 1; i <= 5; i++)
        {
            required.Add("chapter_rewarded_" + i);
            required.Add(ChapterUpgradeService.LevelKey(i));
            required.Add(ChapterUpgradeService.OwnedKey(i));
        }
        if (required.Any(key => !snapshotKeys.Contains(key))) throw new InvalidOperationException("Snapshot must include wallet and all five chapter reward/workshop keys.");
        if (SessionState.GetBool("NoryangjinMapTool.TestPower9999", false) || SessionState.GetFloat("NoryangjinMapTool.TestTimeScale", 1) != 1)
            throw new InvalidOperationException("Prepare without combat stat/time overrides.");
        folder = Path.Combine(prepared, "campaign-transactions");
        if (Directory.Exists(folder)) throw new InvalidOperationException("Preserve previous fixture evidence; prepare a new snapshot.");
        Directory.CreateDirectory(folder);
        checks.Clear(); errors.Clear(); images.Clear(); failures = 0; state = 0; frame = -1;
        Bind("Jamsil", true);
        if (director.Running || chapter.Completed || chapter.Elapsed > .01f || File.Exists(Path.Combine(prepared, "start.json")))
            throw new InvalidOperationException("The prepared lobby has already run gameplay; use a fresh snapshot instead of a paused or completed run.");
        if (MoneyScript.S == null || MoneyScript.S.Jewel > int.MaxValue - 500) throw new InvalidOperationException("Valid wallet with reward headroom required.");
        initialCoins = MoneyScript.S.Coin; initialJewels = MoneyScript.S.Jewel;
        firstTowerReward = replayTowerReward = 0;
        Write("conditions.json", new
        {
            mode = "directed native campaign and reward fixture", ordinaryGameplay = false, funCertification = false,
            snapshot, snapshotIncludesAllFiveChapters = true,
            preferencesRestoredBy = "Root exits Play Mode then invokes Chapters45Playtest.Restore and verifies restored.json",
            fixtureMutations = new[] { "Bootstrap RestStop using SceneManager.LoadSceneAsync from the prepared Jamsil lobby",
                "Dismiss the existing RestStop-only opening with its actual Skip callback if active; never dismiss new-chapter opening UI",
                "Set chapter_unlocked=3 and chapter_rewarded_3/4/5=0 under the full preference snapshot",
                "Direct CompleteChapter and TryAdvanceAutomatically for chapter3/4; these are not physical clears",
                "Seed Tower encounter/target completion and left choices before projecting the player into the authored goal twice" },
            actualApis = new[] { "Canvas.PlayerPressedStartButton", "ChapterProgression.CompleteChapter/TryAdvanceAutomatically",
                "ChapterTransitionUI.skipButton.onClick only when an authored movie actually presents", "Canvas.PauseGame/ResumeGame",
                "Native physics goal OnTriggerEnter/OnTriggerStay", "ChapterProgression.Replay", "actual wallet and PlayerPrefs writes" },
            initialCoins, initialJewels, watchdogSeconds = 90
        });
        for (int i = 3; i <= 5; i++) PlayerPrefs.SetInt("chapter_rewarded_" + i, 0);
        PlayerPrefs.SetInt("chapter_unlocked", 3); PlayerPrefs.Save();
        Time.timeScale = 1; Time.captureDeltaTime = 0;
        active = true; startedAt = stateAt = EditorApplication.timeSinceStartup; phase = "bootstrap RestStop";
        Application.logMessageReceived += OnLog; EditorApplication.update -= Tick; EditorApplication.update += Tick;
        SceneManager.LoadSceneAsync("RestStop", LoadSceneMode.Single);
        Status(); return new { active, folder, phase };
    }

    static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).Single();
    static void Bind(string expected, bool needsDirector)
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != expected) throw new InvalidOperationException("Expected " + expected + ", found " + scene.name);
        player = Find<PlayerScript>(scene); canvas = Find<CanvasScript>(scene); chapter = Find<ChapterProgression>(scene);
        director = needsDirector ? Find<Chapter45Director>(scene) : null;
        if (needsDirector)
        {
            var opening = Find<OpeningStoryUI>(scene);
            var tutorial = Find<CoastalTutorialUI>(scene);
            Require(expected + " inherited story is inactive without Skip", !opening.gameObject.activeSelf && !opening.gameObject.activeInHierarchy
                && opening.enabled && opening.autoPlayMovie && !OpeningStoryUI.IsBlockingGameplay,
                new { openingActive = opening.gameObject.activeSelf, opening.enabled, opening.autoPlayMovie, blocking = OpeningStoryUI.IsBlockingGameplay });
            Require(expected + " shared Canvas remains active and tutorial panel stays inactive", canvas.isActiveAndEnabled && !tutorial.enabled
                && tutorial.panel != null && !tutorial.panel.activeSelf && !tutorial.panel.activeInHierarchy,
                new { canvasActive = canvas.isActiveAndEnabled, tutorialEnabled = tutorial.enabled,
                    tutorialPanelPresent = tutorial.panel != null, tutorialPanelActive = tutorial.panel != null && tutorial.panel.activeSelf });
        }
        else if (expected == "RestStop")
        {
            // The original scene intentionally still has an active autoplay
            // opening. Bootstrap may be its first presentation this session.
            // Scope the normal dismissal to this scene; never hide new-scene UI.
            var opening = Find<OpeningStoryUI>(scene);
            if (opening.gameObject.activeInHierarchy)
            {
                opening.Skip();
                Require("RestStop bootstrap opening dismissed by its actual Skip callback", !opening.gameObject.activeInHierarchy,
                    new { scene = scene.name, opening.enabled, opening.autoPlayMovie, newChapterOpeningDismissed = false });
            }
        }
    }
    static void Next(int next, string label) { state = next; phase = label; stateAt = EditorApplication.timeSinceStartup; Status(); }
    static void Check(string name, bool passed, object evidence)
    {
        var result = new { name, passed, evidence }; checks.Add(result); if (!passed) failures++;
        File.AppendAllText(Path.Combine(folder, "checks.jsonl"), Json(result) + "\n");
    }
    static void Require(string name, bool passed, object evidence)
    { Check(name, passed, evidence); if (!passed) throw new InvalidOperationException("Fixture assertion failed: " + name); }
    static void Status() => Write("status.json", new { active, phase, state, failures, errors = errors.Count, seconds = EditorApplication.timeSinceStartup - startedAt, folder });
    static void Capture(string label)
    {
        string path = Path.Combine(folder, images.Count.ToString("D2") + "-" + label + ".png");
        images.Add(path); ScreenCapture.CaptureScreenshot(path);
    }
    static void OnLog(string message, string stack, LogType kind)
    {
        if (kind != LogType.Error && kind != LogType.Exception && kind != LogType.Assert) return;
        errors.Add(kind + ": " + message); File.AppendAllText(Path.Combine(folder, "errors.txt"), message + "\n" + stack + "\n");
    }
    static int ConfiguredReward(string scene, bool first)
    {
        string key = (first ? "firstClearJewels_" : "replayClearJewels_") + ChapterSceneKey.Resolve(scene);
        if (!EnvironmentVariableTables.TryGetFloat(key, out var value) || float.IsNaN(value) || float.IsInfinity(value))
            throw new InvalidOperationException("Missing reward setting " + key);
        return Mathf.FloorToInt(Mathf.Clamp(value, 0, 100));
    }
    static bool StartCurrent(string expected)
    {
        if (Age < .7) return false;
        // Do not dismiss inherited UI: a frozen story/matte must fail visibly.
        canvas.PlayerPressedStartButton();
        if (!TimeManager.isGameRunning)
        {
            if (Age > 10) throw new InvalidOperationException(expected + " real start callback remained blocked.");
            return false;
        }
        Require(expected + " real Start begins a living run", player.currentHealth > 0 && !CanvasScript.isGameOver && !chapter.Completed
            && (director == null || director.Running && !director.GoalClaimed && director.Distance < 5),
            new { player.currentHealth, TimeManager.isGameRunning, chapter.Completed, running = director != null && director.Running, distance = director != null ? director.Distance : -1 });
        return true;
    }
    static void BeginTransition(string next)
    {
        string source = SceneManager.GetActiveScene().name;
        Require(source + " authored destination", chapter.nextScene == next, new { chapter.nextScene, expected = next });
        int before = MoneyScript.S.Jewel, configured = ConfiguredReward(source, true);
        chapter.CompleteChapter();
        Require(source + " real first-clear transaction", chapter.Completed && configured > 0 && chapter.ClearJewels == configured
            && MoneyScript.S.Jewel - before == configured && PlayerPrefs.GetInt("chapter_rewarded_" + chapter.chapter) == 1,
            new { before, after = MoneyScript.S.Jewel, configured, chapter.ClearJewels, chapter.Completed });
        int paid = MoneyScript.S.Jewel;
        chapter.CompleteChapter();
        Require(source + " completion is idempotent", MoneyScript.S.Jewel == paid, new { paid, now = MoneyScript.S.Jewel });
        Require(source + " unlocks next chapter", PlayerPrefs.GetInt("chapter_unlocked") == chapter.chapter + 1, new { unlocked = PlayerPrefs.GetInt("chapter_unlocked") });
        destination = next; departing = chapter; priorHandle = SceneManager.GetActiveScene().handle;
        transitionHasMovie = chapter.nextChapterMovie != null; skipInvoked = false;
        transitionCoins = MoneyScript.S.Coin; transitionJewels = MoneyScript.S.Jewel;
        Require(source + " real transition coroutine starts", chapter.TryAdvanceAutomatically() && chapter.IsAdvancing,
            new { destination, hasMovie = transitionHasMovie, chapter.IsAdvancing });
    }
    static bool TransitionArrived()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != destination || scene.handle == priorHandle)
        {
            if (departing != null && departing.transitionUI != null && departing.transitionUI.IsPresenting && !skipInvoked && Age > 1)
            {
                var button = departing.transitionUI.skipButton;
                Require("authored transition skip button available", button != null && button.interactable, new { destination });
                button.onClick.Invoke(); skipInvoked = true;
                Check("actual movie skip callback invoked", true, new { destination, transitionHasMovie });
            }
            if (Age > 20) throw new InvalidOperationException("Timed out waiting for real transition to " + destination);
            return false;
        }
        if (Age < .7) return false;
        Bind(destination, true);
        Require(destination + " loaded as fresh playable lobby", !TimeManager.isGameRunning && !CanvasScript.isGameOver && !chapter.Completed && player.currentHealth > 0,
            new { scene = scene.name, scene.handle, priorHandle, player.currentHealth, TimeManager.isGameRunning, CanvasScript.isGameOver });
        Require(destination + " wallet survived scene transition", MoneyScript.S.Coin == transitionCoins && MoneyScript.S.Jewel == transitionJewels
            && PlayerPrefs.GetInt("jewel") == transitionJewels, new { transitionCoins, transitionJewels, coin = MoneyScript.S.Coin, jewel = MoneyScript.S.Jewel });
        Check(destination + " transition presentation receipt", !transitionHasMovie || skipInvoked,
            new { transitionHasMovie, skipInvoked, note = transitionHasMovie ? "Real skip button callback observed" : "Authored movie is null; loading path exercised, video skip not exercised" });
        Capture(destination + "-arrived-lobby"); return true;
    }

    static void Tick()
    {
        if (!active) return;
        if (!EditorApplication.isPlaying) { Finish("play-mode-ended"); return; }
        if (EditorApplication.isPaused || EditorApplication.isCompiling || frame == Time.frameCount) return;
        frame = Time.frameCount;
        try
        {
            if (EditorApplication.timeSinceStartup - startedAt > 90) { Finish("watchdog"); return; }
            switch (state)
            {
                case 0:
                    if (SceneManager.GetActiveScene().name != "RestStop" || Age < .8) return;
                    Bind("RestStop", false);
                    if (!StartCurrent("RestStop")) return;
                    BeginTransition("Jamsil"); Next(1, "actual RestStop to Jamsil transition"); return;
                case 1:
                    if (!TransitionArrived()) return;
                    Next(2, "start Jamsil through actual callback"); return;
                case 2:
                    if (!StartCurrent("Jamsil")) return;
                    BeginTransition("ShoeTower"); Next(3, "actual Jamsil to ShoeTower transition"); return;
                case 3:
                    if (!TransitionArrived()) return;
                    Next(4, "start Tower first collection fixture"); return;
                case 4:
                    if (!StartCurrent("ShoeTower")) return;
                    Require("first Tower transaction begins unclaimed", PlayerPrefs.GetInt("chapter_rewarded_5") == 0 && director.goals.All(g => !g.Claimed),
                        new { rewarded = PlayerPrefs.GetInt("chapter_rewarded_5") });
                    expectedGoalReward = ConfiguredReward("ShoeTower", true);
                    SeedPhysicalGoal(); offeringRestScale = goal.collectionVisual.localScale;
                    Next(5, "paused first physical offering overlap"); return;
                case 5:
                    if (Age < .6) return;
                    AssertPausedUnpaid(); canvas.ResumeGame(); Next(6, "native first offering collection"); return;
                case 6:
                    if (!goal.Claimed && Age < 3) return;
                    AssertCollected(false);
                    firstTowerReward = chapter.ClearJewels; firstTowerCoins = MoneyScript.S.Coin; firstTowerJewels = MoneyScript.S.Jewel;
                    Capture("first-physical-offering-collected"); Next(7, "first win presentation before actual Replay"); return;
                case 7:
                    if (Age < 2.5) return;
                    Require("Tower remains final chapter after physical clear", SceneManager.GetActiveScene().name == "ShoeTower" && string.IsNullOrEmpty(chapter.nextScene)
                        && !chapter.IsAdvancing && CanvasScript.isGameOver, new { chapter.nextScene, chapter.IsAdvancing, CanvasScript.isGameOver });
                    priorHandle = SceneManager.GetActiveScene().handle;
                    chapter.Replay(); Next(8, "actual Tower Replay scene reload"); return;
                case 8:
                    if (SceneManager.GetActiveScene().handle == priorHandle || Age < .8) return;
                    Bind("ShoeTower", true);
                    Require("Replay reloads a fresh alive Tower lobby", !TimeManager.isGameRunning && !CanvasScript.isGameOver && player.currentHealth > 0 && !chapter.Completed,
                        new { priorHandle, handle = SceneManager.GetActiveScene().handle, player.currentHealth, chapter.Completed });
                    Require("first collection persists across Replay", PlayerPrefs.GetInt("chapter_rewarded_5") == 1 && MoneyScript.S.Coin == firstTowerCoins
                        && MoneyScript.S.Jewel == firstTowerJewels && PlayerPrefs.GetInt("jewel") == firstTowerJewels,
                        new { rewarded = PlayerPrefs.GetInt("chapter_rewarded_5"), firstTowerCoins, firstTowerJewels, coin = MoneyScript.S.Coin, jewel = MoneyScript.S.Jewel });
                    Next(9, "start real replay before second fixture"); return;
                case 9:
                    if (!StartCurrent("ShoeTower replay")) return;
                    Require("Replay resets runtime goal choices and lifts", !director.GoalClaimed && director.goals.All(g => !g.Claimed)
                        && director.choices.All(c => !c.Selected && !c.Rewarded) && director.LiftCount == 0,
                        new { director.GoalClaimed, director.LiftCount, choices = director.choices.Select(c => c.Selection).ToArray() });
                    var replayVisual = director.goals.Single(g => g.offering).collectionVisual;
                    Require("Replay restores offering visibility and scale", replayVisual != null && replayVisual.gameObject.activeSelf
                        && Vector3.Distance(replayVisual.localScale, offeringRestScale) < .001f,
                        new { expected = V(offeringRestScale), actual = replayVisual != null ? V(replayVisual.localScale) : null });
                    expectedGoalReward = ConfiguredReward("ShoeTower", false);
                    Require("configured Tower replay reward is five jewels", expectedGoalReward == 5, new { expectedGoalReward });
                    SeedPhysicalGoal(); Next(10, "paused second physical offering overlap"); return;
                case 10:
                    if (Age < .6) return;
                    AssertPausedUnpaid(); canvas.ResumeGame(); Next(11, "native second offering collection"); return;
                case 11:
                    if (!goal.Claimed && Age < 3) return;
                    AssertCollected(true); replayTowerReward = chapter.ClearJewels;
                    Require("second physical collection pays exactly five persisted jewels", replayTowerReward == 5 && MoneyScript.S.Jewel - firstTowerJewels == 5
                        && PlayerPrefs.GetInt("jewel") == firstTowerJewels + 5,
                        new { firstTowerJewels, jewelNow = MoneyScript.S.Jewel, persisted = PlayerPrefs.GetInt("jewel"), replayTowerReward });
                    Capture("replay-physical-offering-plus-five"); Next(12, "final transaction and capture flush"); return;
                case 12:
                    if (Age < 2.5) return;
                    Finish(failures == 0 && errors.Count == 0 ? "passed" : "failed"); return;
            }
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(folder, "harness-error.txt"), error.ToString()); Finish("harness-error"); }
    }

    static void SeedPhysicalGoal()
    {
        canvas.PauseGame();
        Require("fresh Tower run requires real combat before seeding", !director.RequiredEncountersComplete, new { director.RequiredEncountersComplete });
        foreach (var encounter in director.encounters) Property(encounter, "Complete", true);
        foreach (var target in director.targets)
        {
            Property(target, "Health", 0f); typeof(Chapter45Target).GetField("opening", Private).SetValue(target, 1f);
            if (target.hitCollider != null) target.hitCollider.enabled = false;
            if (target.panelCollider != null) target.panelCollider.enabled = false;
        }
        foreach (var choice in director.choices) choice.Commit(-1);
        Require("seeded authored requirements complete", director.RequiredEncountersComplete, new { director.RequiredEncountersComplete });
        goal = director.goals.Single(g => g.offering);
        var pc = player.GetComponent<Collider>(); var gc = goal.GetComponent<Collider>();
        Require("physical offering has player and trigger colliders", pc != null && gc != null && gc.isTrigger && goal.collectionVisual != null,
            new { playerCollider = pc != null, goalCollider = gc != null });
        int goalSegment = -1; float goalDistance = 0, closestSquared = float.PositiveInfinity, segmentStart = 0;
        for (int i = 0; i < director.route.segments.Length; i++)
        {
            var segment = director.route.segments[i];
            if (segment.floor == goal.floor && segmentStart + segment.Length >= director.route.Length - 40)
            {
                Vector3 span = segment.end - segment.start;
                float fraction = Mathf.Clamp01(Vector3.Dot(gc.bounds.center - segment.start, span) / Mathf.Max(.0001f, span.sqrMagnitude));
                float squared = Vector3.ProjectOnPlane(gc.bounds.center - Vector3.Lerp(segment.start, segment.end, fraction), Vector3.up).sqrMagnitude;
                float progress = segmentStart + segment.Length * fraction;
                if (squared < closestSquared - .0001f || Mathf.Abs(squared - closestSquared) <= .0001f && progress > goalDistance)
                { closestSquared = squared; goalDistance = progress; goalSegment = i; }
            }
            segmentStart += segment.Length;
        }
        Require("physical goal projects onto final authored approach", goalSegment >= 0 && closestSquared < 25, new { goalSegment, goalDistance, closestSquared });
        Property(director, "CurrentSegment", goalSegment); Property(director, "Distance", goalDistance);
        director.route.SampleSegment(goalSegment, goalDistance, out var center, out var forward);
        player.ApplyContinuousRoutePose(center + Vector3.up * director.footOffset, forward, 0);
        Physics.SyncTransforms(); player.transform.position += gc.bounds.center - pc.bounds.center;
        player.GetComponent<Rigidbody>().position = player.transform.position; Physics.SyncTransforms();
        goalCoins = MoneyScript.S.Coin; goalJewels = MoneyScript.S.Jewel;
        Require("goal fixture physically overlaps while paused", gc.bounds.Intersects(pc.bounds) && director.CurrentFloor == goal.floor && !TimeManager.isGameRunning,
            new { player = V(pc.bounds.center), goal = V(gc.bounds.center), director.Distance, director.CurrentFloor, goal.floor });
    }
    static void AssertPausedUnpaid() => Require("paused native goal overlap grants nothing", !goal.Claimed && !director.GoalClaimed && !chapter.Completed
        && MoneyScript.S.Coin == goalCoins && MoneyScript.S.Jewel == goalJewels,
        new { goal.Claimed, director.GoalClaimed, chapter.Completed, goalJewels, jewelNow = MoneyScript.S.Jewel });
    static void AssertCollected(bool replay)
    {
        Require((replay ? "Replay" : "First") + " native goal callback completes chapter", goal.Claimed && director.GoalClaimed && chapter.Completed && CanvasScript.isGameOver,
            new { goal.Claimed, director.GoalClaimed, chapter.Completed, CanvasScript.isGameOver });
        Require("physical collection grants configured jewels once", expectedGoalReward > 0 && chapter.ClearJewels == expectedGoalReward
            && MoneyScript.S.Jewel - goalJewels == expectedGoalReward && PlayerPrefs.GetInt("chapter_rewarded_5") == 1,
            new { goalJewels, after = MoneyScript.S.Jewel, expectedGoalReward, chapter.ClearJewels, replay });
        int coinPaid = MoneyScript.S.Coin, jewelPaid = MoneyScript.S.Jewel;
        for (int i = 0; i < 5; i++) chapter.CompleteChapter();
        Require("repeat completion cannot duplicate the collection reward", MoneyScript.S.Coin == coinPaid && MoneyScript.S.Jewel == jewelPaid,
            new { coinPaid, jewelPaid, coinNow = MoneyScript.S.Coin, jewelNow = MoneyScript.S.Jewel, replay });
        Require("final chapter unlock stays five", PlayerPrefs.GetInt("chapter_unlocked") == 5, new { unlocked = PlayerPrefs.GetInt("chapter_unlocked") });
    }
    static void Finish(string outcome)
    {
        active = false; EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        Write("summary.json", new { outcome, failures, checks, errors, seconds = EditorApplication.timeSinceStartup - startedAt,
            ordinaryPlayEvidence = false, forcedFixture = true, firstTowerReward, replayTowerReward, initialCoins, initialJewels,
            finalCoins = MoneyScript.S != null ? MoneyScript.S.Coin : -1, finalJewels = MoneyScript.S != null ? MoneyScript.S.Jewel : -1,
            screenshots = images.Where(File.Exists).ToArray(), preferencesRestoreStillRequired = true,
            limitations = new[] { "Chapter3/4 completion was directly invoked; reachability and combat are not established by this fixture.",
                "Tower combat/choice prerequisites and goal position were seeded; physical callbacks and reward transactions are real.",
                "Movie skipping is only covered if an authored movie actually presents; null movies exercise the loading path only.",
                "This receipt does not replace ordinary-stat gameplay, visual review, fun evaluation or mobile frame measurements.",
                "Exit Play Mode and run Chapters45Playtest.Restore; verify restored.json has zero mismatches." } });
        phase = outcome; Status();
    }
    public static object Poll() => new { active, phase, folder, failures,
        report = !string.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, "summary.json")) ? File.ReadAllText(Path.Combine(folder, "summary.json")) : null };
}
