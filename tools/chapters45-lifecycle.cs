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

// Directed native lifecycle fixtures, never ordinary-play or fun evidence.
// Root prepares ShoeTower with Chapters45Playtest.Prepare and restores that
// snapshot after leaving Play Mode. No assets, scenes or upgrades are saved here.
public static class Chapters45Lifecycle
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly FieldInfo ShotDeck = typeof(BulletScript).GetField("launchDeck", Private);
    static readonly FieldInfo ShootBlocks = typeof(PlayerScript).GetField("bucketShootBlockers", Private);
    static readonly MethodInfo BeginLift = typeof(Chapter45Director).GetMethod("BeginLift", Private);
    static PlayerScript player;
    static Chapter45Director director;
    static CanvasScript canvas;
    static ChapterProgression chapter;
    static ExtraHelpBuffScript[] helpers;
    static WeaponScript[] weapons;
    static BulletScript[] oldShots;
    static GameObject probe;
    static Chapter45Goal goal;
    static float[] helperHealth;
    static Vector3[] helperOffsets, pausedHelpers;
    static Vector3 pausedPosition;
    static float pausedClock, initialHealth;
    static int pausedShots, boardShots, state, frame, priorHandle, coinBefore, jewelBefore, coinAfter, jewelAfter;
    static bool active, baselineKinematic, baselineCanShoot;
    static double startedAt, stateAt;
    static string folder, phase;
    static readonly List<object> checks = new();
    static readonly List<string> errors = new();
    static readonly List<string> images = new();
    static int failures;

    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    static void Write(string file, object value) => File.WriteAllText(Path.Combine(folder, file), Json(value));
    static float[] V(Vector3 v) => new[] { v.x, v.y, v.z };
    static void Property(object owner, string name, object value) => owner.GetType().GetProperty(name).GetSetMethod(true).Invoke(owner, new[] { value });
    static double Age => EditorApplication.timeSinceStartup - stateAt;
    static int Shots => weapons == null ? 0 : weapons.Where(w => w != null).Sum(w => w.TotalProjectilesSpawned);

    public static object Begin()
    {
        string prepared = SessionState.GetString("Chapter45.QA.folder", "");
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "ShoeTower" || TimeManager.isGameRunning)
            throw new InvalidOperationException("Fresh prepared ShoeTower Play lobby required; do not interrupt an ordinary run.");
        if (!File.Exists(Path.Combine(prepared, "before-prefs.tsv"))) throw new InvalidOperationException("Prepare a preference snapshot with Chapters45Playtest.Prepare first.");
        folder = Path.Combine(prepared, "lifecycle"); Directory.CreateDirectory(folder);
        if (File.Exists(Path.Combine(folder, "conditions.json"))) throw new InvalidOperationException("Preserve prior lifecycle evidence; prepare a new snapshot.");
        checks.Clear(); errors.Clear(); images.Clear(); failures = 0; state = 0; frame = -1;
        Bind(); initialHealth = player.currentHealth;
        if (director.lifts.Length < 2) throw new InvalidOperationException("Both authored tower lifts are required.");
        Write("conditions.json", new { mode = "directed native lifecycle fixtures", ordinaryGameplay = false, funCertification = false,
            persistentStatOverrides = false, preferencesRestoredBy = "Chapters45Playtest.Restore after exiting Play Mode",
            fixtureMutations = new[] { "Teleport player and spawned helpers to authored lift boundaries",
                "Invoke actual private BeginLift to isolate transfer lifecycle from earlier encounters",
                "Inject currentHealth=0 during the otherwise invulnerable ride to test abort cleanup",
                "Seed required encounter/target completion before the isolated physical goal transaction probe" },
            actualApis = new[] { "Canvas.PlayerPressedStartButton", "Canvas.PauseGame/ResumeGame", "SpawnBonus for both helper types", "ChapterProgression.Replay", "native physics goal Enter/Stay" },
            watchdogSeconds = 90 });
        active = true; startedAt = stateAt = EditorApplication.timeSinceStartup; phase = "start";
        OpeningStoryUI.Instance?.Skip(); Time.timeScale = 1; Time.captureDeltaTime = 0;
        Application.logMessageReceived += OnLog; EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Status(); return new { active, folder, phase };
    }
    static void Bind()
    {
        player = Object.FindFirstObjectByType<PlayerScript>(); director = Object.FindFirstObjectByType<Chapter45Director>();
        canvas = Object.FindFirstObjectByType<CanvasScript>(); chapter = Object.FindFirstObjectByType<ChapterProgression>();
        var overlay = Object.FindFirstObjectByType<CombatHarness>();
        if (overlay != null) typeof(CombatHarness).GetField("overlayVisible", Private)?.SetValue(overlay, false);
        if (player == null || director == null || canvas == null || chapter == null) throw new InvalidOperationException("Chapter dependencies missing.");
    }
    static void Next(int next, string label) { state = next; phase = label; stateAt = EditorApplication.timeSinceStartup; Status(); }
    static void Check(string name, bool passed, object evidence)
    {
        checks.Add(new { name, passed, evidence }); if (!passed) failures++;
        File.AppendAllText(Path.Combine(folder, "checks.jsonl"), Json(new { name, passed, evidence }) + "\n");
    }
    static void Require(string name, bool passed, object evidence)
    { Check(name, passed, evidence); if (!passed) throw new InvalidOperationException("Fixture precondition failed: " + name); }
    static void Status() => Write("status.json", new { active, phase, state, failures, errors = errors.Count, seconds = EditorApplication.timeSinceStartup - startedAt, folder });
    static void Capture(string label)
    {
        string path = Path.Combine(folder, images.Count.ToString("D2") + "-" + label + ".png"); images.Add(path); ScreenCapture.CaptureScreenshot(path);
    }
    static void OnLog(string text, string stack, LogType kind)
    {
        if (kind != LogType.Error && kind != LogType.Exception && kind != LogType.Assert) return;
        errors.Add(kind + ": " + text); File.AppendAllText(Path.Combine(folder, "errors.txt"), text + "\n" + stack + "\n");
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
                    if (Age < 1.5) return;
                    canvas.PlayerPressedStartButton();
                    if (!TimeManager.isGameRunning) return;
                    Require("real start initializes chapter", director.Running && director.Distance < 3 && !director.GoalClaimed, new { director.Running, director.Distance });
                    var spawner = Object.FindFirstObjectByType<NoryangjinUpgradeExtraHelpSpawner>();
                    Require("actual helper spawner configured", spawner != null && spawner.IsConfigured, new { present = spawner != null });
                    helpers = new[] { spawner.SpawnBonus(HelpType.Tungtungtung, player), spawner.SpawnBonus(HelpType.Boombardino, player) };
                    Require("both real helpers owned by player", helpers.All(h => h != null && h.Owner == player), helpers.Select(h => h != null ? h.name : "null").ToArray());
                    Next(1, "allow real helper Start and shots"); return;
                case 1:
                    if (Age < 1) return;
                    weapons = player.GetComponentsInChildren<WeaponScript>(true).Concat(helpers.SelectMany(h => h.GetComponentsInChildren<WeaponScript>(true))).ToArray();
                    oldShots = Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled && (int)ShotDeck.GetValue(b) == 0).ToArray();
                    Require("real baseline projectiles exist", oldShots.Length > 0 && Shots > 0, new { count = oldShots.Length, shots = Shots });
                    canvas.PauseGame();
                    MoveFixtureToLift(director.lifts[0]);
                    helperHealth = helpers.Select(h => h.currentHealth).ToArray();
                    helperOffsets = helpers.Select(h => Quaternion.Inverse(player.transform.rotation) * (h.transform.position - player.transform.position)).ToArray();
                    baselineKinematic = player.GetComponent<Rigidbody>().isKinematic; baselineCanShoot = player.canShoot;
                    probe = new GameObject("Lifecycle deck probe"); probe.AddComponent<Chapter45Deck>().floor = 0; probe.transform.position = player.transform.position;
                    canvas.ResumeGame(); BeginLift.Invoke(director, new object[] { director.lifts[0] }); boardShots = Shots;
                    Check("boarding locks movement and fire", director.IsTransferring && !player.canShoot && player.GetComponent<Rigidbody>().isKinematic, new { director.IsTransferring, player.canShoot });
                    Check("boarding retires all captured shots", oldShots.All(b => b == null || !b.isActiveAndEnabled && !b.CanHitTarget(probe.transform)), new { captured = oldShots.Length });
                    Capture("lift-board-helpers"); Next(2, "ride then pause"); return;
                case 2:
                    if (Age < 1.5) return;
                    Require("lift still active before pause", director.IsTransferring, new { director.IsTransferring });
                    canvas.PauseGame(); pausedPosition = player.transform.position; pausedClock = director.Elapsed; pausedShots = Shots;
                    pausedHelpers = helpers.Select(h => h.transform.position).ToArray();
                    Next(3, "paused lift"); return;
                case 3:
                    if (Age < 1.1) return;
                    Check("real pause freezes lift and chapter clock", Vector3.Distance(player.transform.position, pausedPosition) < .001f && Mathf.Abs(director.Elapsed - pausedClock) < .001f,
                        new { before = V(pausedPosition), after = V(player.transform.position), elapsedBefore = pausedClock, elapsedAfter = director.Elapsed });
                    Check("pause freezes both helpers", helpers.Select((h, i) => Vector3.Distance(h.transform.position, pausedHelpers[i]) < .001f).All(x => x), helpers.Select(h => V(h.transform.position)).ToArray());
                    Check("pause and ride produce no player or helper shots", Shots == pausedShots && Shots == boardShots, new { boardShots, pausedShots, now = Shots });
                    Check("paused helper contact rejected", !Chapter45Director.CanHelperContact(helpers[0], probe.transform), new { paused = !TimeManager.isGameRunning });
                    Capture("paused-mid-lift"); canvas.ResumeGame(); Next(4, "resume and land"); return;
                case 4:
                    if (director.IsTransferring) return;
                    Require("real lift landed", director.LiftCount == 1 && director.CurrentFloor == 1, new { director.LiftCount, director.CurrentFloor });
                    Check("lift consumes exactly six running seconds", Mathf.Abs(director.LastLiftSeconds - 6) <= .025f, new { director.LastLiftSeconds });
                    Check("both helpers retain health", helpers.Select((h, i) => h != null && Mathf.Abs(h.currentHealth - helperHealth[i]) < .001f).All(x => x), helpers.Select(h => h != null ? h.currentHealth : -1).ToArray());
                    Check("both helpers retain landing relative pose", helpers.Select((h, i) => h != null && Vector3.Distance(Quaternion.Inverse(player.transform.rotation) * (h.transform.position - player.transform.position), helperOffsets[i]) < .5f).All(x => x),
                        helpers.Select((h, i) => new { h.helpType, expected = V(helperOffsets[i]), actual = V(Quaternion.Inverse(player.transform.rotation) * (h.transform.position - player.transform.position)) }).ToArray());
                    Check("landing restores body and shooting", player.GetComponent<Rigidbody>().isKinematic == baselineKinematic && player.canShoot == baselineCanShoot, new { baselineKinematic, kinematic = player.GetComponent<Rigidbody>().isKinematic, player.canShoot });
                    probe.transform.position = player.transform.position; probe.GetComponent<Chapter45Deck>().floor = 0;
                    Check("helper cannot contact old deck at matching height", !Chapter45Director.CanHelperContact(helpers[0], probe.transform), new { director.CurrentFloor, targetDeck = 0 });
                    probe.GetComponent<Chapter45Deck>().floor = 1;
                    Check("helper can contact current deck", Chapter45Director.CanHelperContact(helpers[0], probe.transform), new { director.CurrentFloor, targetDeck = 1 });
                    Capture("landed-helpers"); Next(5, "prepare death abort fixture"); return;
                case 5:
                    if (Age < .8) return;
                    var landedShots = Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled && (int)ShotDeck.GetValue(b) == 1).ToArray();
                    Check("landing resumes real current-deck projectiles", landedShots.Length > 0 && landedShots.All(b => b.CanHitTarget(probe.transform)), new { count = landedShots.Length, targetDeck = 1 });
                    probe.GetComponent<Chapter45Deck>().floor = 0;
                    Check("new shots reject old deck at matching height", landedShots.Length > 0 && landedShots.All(b => !b.CanHitTarget(probe.transform)), new { count = landedShots.Length, targetDeck = 0 });
                    canvas.PauseGame(); MoveFixtureToLift(director.lifts[1]);
                    baselineKinematic = player.GetComponent<Rigidbody>().isKinematic; baselineCanShoot = player.canShoot;
                    canvas.ResumeGame(); BeginLift.Invoke(director, new object[] { director.lifts[1] });
                    Next(6, "second lift fault injection"); return;
                case 6:
                    if (Age < .8) return;
                    Require("death injected during actual ride", director.IsTransferring, new { director.IsTransferring });
                    player.currentHealth = 0; player.UpdateHealth();
                    Next(7, "observe death cleanup"); return;
                case 7:
                    if (Age < .25) return;
                    var blockers = (HashSet<Object>)ShootBlocks.GetValue(player);
                    Check("death abort releases lift state and body", !director.IsTransferring && !director.Running && player.GetComponent<Rigidbody>().isKinematic == baselineKinematic,
                        new { director.IsTransferring, director.Running, baselineKinematic, kinematic = player.GetComponent<Rigidbody>().isKinematic });
                    Check("death abort removes only lift shoot blocker", !blockers.Contains(director) && player.canShoot == baselineCanShoot, new { player.canShoot, baselineCanShoot, retainedBlockers = blockers.Count });
                    Capture("death-abort"); priorHandle = SceneManager.GetActiveScene().handle; chapter.Replay(); Next(8, "actual replay after death"); return;
                case 8:
                    if (SceneManager.GetActiveScene().handle == priorHandle || Age < .5) return;
                    Bind(); OpeningStoryUI.Instance?.Skip();
                    Check("actual scene replay restores alive lobby", player.currentHealth > 0 && !TimeManager.isGameRunning && !CanvasScript.isGameOver && !director.IsTransferring,
                        new { player.currentHealth, TimeManager.isGameRunning, CanvasScript.isGameOver, director.IsTransferring });
                    canvas.PlayerPressedStartButton(); if (!TimeManager.isGameRunning) return;
                    Check("new BeginRun resets lift and choice state", director.LiftCount == 0 && director.choices.All(c => !c.Selected && !c.Rewarded) && director.goals.All(g => !g.Claimed),
                        new { director.LiftCount, choices = director.choices.Select(c => new { c.Selected, c.Rewarded }).ToArray() });
                    canvas.PauseGame();
                    // Isolated transaction fixture: prior combat is deliberately seeded complete.
                    Check("fresh run still requires authored combat", !director.RequiredEncountersComplete, new { director.RequiredEncountersComplete });
                    foreach (var encounter in director.encounters) Property(encounter, "Complete", true);
                    foreach (var target in director.targets)
                    {
                        Property(target, "Health", 0f); typeof(Chapter45Target).GetField("opening", Private).SetValue(target, 1f);
                        if (target.hitCollider != null) target.hitCollider.enabled = false;
                        if (target.panelCollider != null) target.panelCollider.enabled = false;
                    }
                    foreach (var choice in director.choices) choice.Commit(-1);
                    Require("seeded goal prerequisites are complete", director.RequiredEncountersComplete, new { director.RequiredEncountersComplete });
                    goal = director.goals.First(g => g.offering);
                    var pc = player.GetComponent<Collider>(); var gc = goal.GetComponent<Collider>();
                    Require("authored physical goal colliders present", pc != null && gc != null && gc.isTrigger, new { playerCollider = pc != null, goalCollider = gc != null });
                    // Keep logical progress and physical overlap on the same station;
                    // otherwise normal forward movement snaps an end-position fixture
                    // past an offering authored a few metres before the route end.
                    int goalSegment = -1;
                    float goalDistance = 0, closestSquared = float.PositiveInfinity, segmentStart = 0;
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
                    Require("goal projects onto final authored approach", goalSegment >= 0 && closestSquared < 25, new { goalSegment, goalDistance, closestSquared });
                    SetRouteFixture(goalSegment, goalDistance);
                    Physics.SyncTransforms(); player.transform.position += gc.bounds.center - pc.bounds.center;
                    player.GetComponent<Rigidbody>().position = player.transform.position; Physics.SyncTransforms();
                    coinBefore = MoneyScript.S.Coin; jewelBefore = MoneyScript.S.Jewel;
                    Require("goal fixture overlaps while paused", gc.bounds.Intersects(pc.bounds), new { player = V(pc.bounds.center), goal = V(gc.bounds.center), director.Distance, director.CurrentFloor, goal.floor });
                    Next(9, "native goal Enter and Stay while paused"); return;
                case 9:
                    if (Age < .6) return;
                    Check("paused physical goal contact grants nothing", !goal.Claimed && !director.GoalClaimed && !chapter.Completed && MoneyScript.S.Coin == coinBefore && MoneyScript.S.Jewel == jewelBefore,
                        new { goal.Claimed, director.GoalClaimed, chapter.Completed, coin = MoneyScript.S.Coin, jewel = MoneyScript.S.Jewel });
                    Capture("paused-offering-overlap"); canvas.ResumeGame(); Next(10, "resume existing goal overlap"); return;
                case 10:
                    if (!goal.Claimed && Age < 2) return;
                    Require("native OnTriggerStay collects on resume", goal.Claimed && director.GoalClaimed && chapter.Completed,
                        new { goal.Claimed, director.GoalClaimed, chapter.Completed, chapter.ClearJewels });
                    coinAfter = MoneyScript.S.Coin; jewelAfter = MoneyScript.S.Jewel;
                    Require("goal pays positive configured jewels exactly once", chapter.ClearJewels > 0 && jewelAfter - jewelBefore == chapter.ClearJewels, new { jewelBefore, jewelAfter, chapter.ClearJewels });
                    var collider = player.GetComponent<Collider>();
                    for (int i = 0; i < 5; i++)
                    {
                        typeof(Chapter45Goal).GetMethod("OnTriggerEnter", Private).Invoke(goal, new object[] { collider });
                        typeof(Chapter45Goal).GetMethod("OnTriggerStay", Private).Invoke(goal, new object[] { collider });
                        chapter.CompleteChapter();
                    }
                    Check("duplicate callbacks and completion do not double reward", MoneyScript.S.Coin == coinAfter && MoneyScript.S.Jewel == jewelAfter,
                        new { coinBefore, coinAfter, coinNow = MoneyScript.S.Coin, jewelAfter, jewelNow = MoneyScript.S.Jewel });
                    Capture("physical-offering-collected"); Next(11, "replay claimed goal"); return;
                case 11:
                    if (Age < .8) return;
                    priorHandle = SceneManager.GetActiveScene().handle; chapter.Replay(); Next(12, "verify claim reset after actual replay"); return;
                case 12:
                    if (SceneManager.GetActiveScene().handle == priorHandle || Age < .5) return;
                    Bind(); OpeningStoryUI.Instance?.Skip(); canvas.PlayerPressedStartButton(); if (!TimeManager.isGameRunning) return;
                    Check("actual replay clears claimed offering and choices", !chapter.Completed && !director.GoalClaimed && director.goals.All(g => !g.Claimed) && director.choices.All(c => !c.Selected && !c.Rewarded),
                        new { chapter.Completed, director.GoalClaimed, goals = director.goals.Select(g => g.Claimed).ToArray(), choices = director.choices.Select(c => c.Selection).ToArray() });
                    canvas.PauseGame(); Capture("replay-reset"); Next(13, "capture flush"); return;
                case 13:
                    if (Age < .5) return;
                    Finish(failures == 0 && errors.Count == 0 ? "passed" : "failed"); return;
            }
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(folder, "harness-error.txt"), error.ToString()); Finish("harness-error"); }
    }
    static void SetRouteFixture(int segment, float distance)
    {
        Property(director, "CurrentSegment", segment); Property(director, "Distance", distance);
        director.route.SampleSegment(segment, distance, out var center, out var forward);
        player.ApplyContinuousRoutePose(center + Vector3.up * director.footOffset, forward, 0); Physics.SyncTransforms();
    }
    static void MoveFixtureToLift(Chapter45Lift lift)
    {
        var offsets = helpers.Select(h => Quaternion.Inverse(player.transform.rotation) * (h.transform.position - player.transform.position)).ToArray();
        SetRouteFixture(lift.afterSegment, director.route.SegmentEnd(lift.afterSegment));
        for (int i = 0; i < helpers.Length; i++) helpers[i].transform.position = player.transform.position + player.transform.rotation * offsets[i];
        Physics.SyncTransforms();
    }
    static void Finish(string outcome)
    {
        active = false; EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (probe != null) Object.Destroy(probe);
        Write("summary.json", new { outcome, failures, checks, errors, seconds = EditorApplication.timeSinceStartup - startedAt, initialHealth,
            ordinaryPlayEvidence = false, fixtureMutations = true, screenshots = images.Where(File.Exists).ToArray(),
            limitations = new[] { "This is a directed lifecycle fixture, not an ordinary clear or fun evaluation.", "Lift boarding reachability and full combat remain covered by the separate ordinary runs.",
                "Deck assertions call the projectile/helper admission gates; they do not reproduce both physics callback orders.",
                "Death abort is exercised; disabling or unloading a scene during a ride is not exercised.",
                "Replay resets claims but this fixture does not recollect to verify replay reward persistence.",
                "Preferences still require root to exit Play Mode and run Chapters45Playtest.Restore." } });
        phase = outcome; Status();
    }
    public static object Poll() => new { active, phase, folder, failures, report = File.Exists(Path.Combine(folder ?? "", "summary.json")) ? File.ReadAllText(Path.Combine(folder, "summary.json")) : null };
}
