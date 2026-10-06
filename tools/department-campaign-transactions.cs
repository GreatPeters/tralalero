using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Actual ordinary-stat gameplay. Reflection invokes the same PlayerMove method
// as touch/keyboard; it never writes position, health, damage or enemy state.
public static class Chapters45Playtest
{
    const string Session = "Chapter45.QA.";
    const string Selector = "NoryangjinMapTool.TestStartStage";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo Move = typeof(PlayerScript).GetMethod("PlayerMove", Private);
    static readonly FieldInfo Origin = typeof(PlayerScript).GetField("routeLaneOrigin", Private);
    static readonly FieldInfo Right = typeof(PlayerScript).GetField("routeRight", Private);
    static readonly FieldInfo Range = typeof(PlayerScript).GetField("xRange", Private);
    static readonly FieldInfo ShotDirection = typeof(BulletScript).GetField("direction", Private);
    static readonly FieldInfo ShotDeck = typeof(BulletScript).GetField("launchDeck", Private);
    static readonly FieldInfo ShotHeight = typeof(BulletScript).GetField("launchFloorHeight", Private);
    static readonly FieldInfo ShotRetired = typeof(BulletScript).GetField("chapterRetired", Private);
    static readonly FieldInfo RideElapsed = typeof(Chapter45Director).GetField("rideElapsed", Private);
    static readonly FieldInfo RideDuration = typeof(Chapter45Director).GetField("rideDuration", Private);
    static WeaponScript[] playerWeapons;
    static PlayerScript player;
    static Chapter45Director director;
    static ChapterProgression chapter;
    static CanvasScript canvas;
    static string folder, scene;
    static int routeChoice, moves, shots, frame, timelineCursor, initialCoins, initialJewels;
    static float nextSample, nextShot, initialHealth, initialAttack, lastHealth, lastAttack, began;
    static double readyAt, wallStarted, endedAt;
    static bool active, started, finished;
    static string outcome;
    static readonly List<object> damage = new();
    static readonly List<string> images = new();
    static readonly List<string> errors = new();
    static readonly List<string> animationWarnings = new();
    static readonly Dictionary<int, float> openingLanes = new();
    static readonly Dictionary<int, EnemyScript_space> enemyCommitments = new();
    static readonly Dictionary<int, float> avoidanceLanes = new();
    static string inputPolicy;
    static float desiredLane;
    static readonly HashSet<int> liftMidCaptured = new();
    static readonly HashSet<int> liftTurnCaptured = new();
    static string hudObserved, hudRecorded;
    static float hudChangedAt;
    static int hudChanges, hudCaptures;

    [Serializable] sealed class PrefRow { public string key, kind, value; public bool existed; }
    [Serializable] sealed class SnapshotReference
    {
        public int version = 1, keyCount;
        public string snapshotPath, sha256, editorStateSessionKey;
    }
    [Serializable] sealed class EditorState
    {
        public string preparedFolder, startScene;
        public bool tutorialExists;
        public int tutorial;
        public float timeScale, captureDeltaTime;
        public PrefRow[] session;
    }
    [Serializable] sealed class HudState
    {
        public bool present, panelVisible, titleVisible, detailVisible;
        public string title, detail;
    }
    const string ReferenceFile = "snapshot-reference.json";

    static string Hash(byte[] bytes)
    { using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    static Dictionary<string, string> PreferenceCatalog()
    {
        // Mirrors the current ChapterPlaytestPreferences.SnapshotAt catalog,
        // without calling its disk-writing snapshot API.
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        void Add(string key, string kind) { if (!keys.TryAdd(key, kind)) throw new InvalidOperationException("Duplicate current preference key: " + key); }
        Add("coin", "int"); Add("jewel", "int"); Add("chapter_unlocked", "int"); Add("ads_last_reward_round", "string");
        for (int chapter = 1; chapter <= 5; chapter++)
        {
            Add("chapter_rewarded_" + chapter, "int"); Add(ChapterUpgradeService.LevelKey(chapter), "int"); Add(ChapterUpgradeService.OwnedKey(chapter), "int");
        }
        for (int i = 1; i <= Enum.GetValues(typeof(UpgradeStatManager.UpgradeType)).Length; i++) Add("upgrade_lv_" + i, "int");
        foreach (string type in Enum.GetNames(typeof(UpgradeStatManager.UpgradeType))) { Add("upgrade_stat_" + type, "float"); Add("upgrade_stat_type_" + type, "int"); }
        foreach (var row in CosmeticTables.Rows) Add(CosmeticService.OwnedKey(row.id), "int");
        foreach (CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot))) Add(CosmeticService.EquippedKey(slot), "string");
        return keys;
    }
    static string CurrentValue(string key, string kind) => kind == "int" ? PlayerPrefs.GetInt(key).ToString(CultureInfo.InvariantCulture)
        : kind == "float" ? PlayerPrefs.GetFloat(key).ToString("R", CultureInfo.InvariantCulture)
        : Convert.ToBase64String(Encoding.UTF8.GetBytes(PlayerPrefs.GetString(key)));
    static PrefRow[] ReadSnapshot(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException("Existing snapshot file is missing.");
        var rows = new List<PrefRow>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(path))
        {
            var p = line.Split('\t');
            if (p.Length != 4 || string.IsNullOrEmpty(p[0]) || !seen.Add(p[0]) || !bool.TryParse(p[2], out bool existed))
                throw new InvalidOperationException("Malformed or duplicate snapshot row.");
            if (p[1] == "int") int.Parse(p[3], CultureInfo.InvariantCulture);
            else if (p[1] == "float") float.Parse(p[3], CultureInfo.InvariantCulture);
            else if (p[1] == "string") new UTF8Encoding(false, true).GetString(Convert.FromBase64String(p[3]));
            else throw new InvalidOperationException("Unsupported snapshot value kind: " + p[1]);
            rows.Add(new PrefRow { key = p[0], kind = p[1], existed = existed, value = p[3] });
        }
        var catalog = PreferenceCatalog();
        var unexpected = rows.Where(r => !catalog.TryGetValue(r.key, out string kind) || kind != r.kind).Select(r => r.key).ToArray();
        var missing = catalog.Keys.Where(k => !seen.Contains(k)).ToArray();
        if (unexpected.Length != 0 || missing.Length != 0)
            throw new InvalidOperationException("Snapshot catalog mismatch. Missing: " + string.Join(",", missing) + "; unexpected/type mismatch: " + string.Join(",", unexpected));
        return rows.ToArray();
    }
    static string[] PreferenceMismatches(IEnumerable<PrefRow> rows) => rows
        .Where(r => PlayerPrefs.HasKey(r.key) != r.existed || CurrentValue(r.key, r.kind) != r.value).Select(r => r.key).ToArray();
    public static object InspectSnapshotEligibility(string existingSnapshot)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stable Edit Mode required for eligibility inspection.");
        string path = string.IsNullOrEmpty(existingSnapshot) ? "" : Path.GetFullPath(existingSnapshot);
        try
        {
            var rows = ReadSnapshot(path); var mismatches = PreferenceMismatches(rows);
            return new { eligible = mismatches.Length == 0, snapshotPath = path, keyCount = rows.Length,
                sha256 = Hash(File.ReadAllBytes(path)), catalogExact = true, mismatchedKeys = mismatches, mutations = false };
        }
        catch (Exception e) { return new { eligible = false, snapshotPath = path, catalogExact = false, reason = e.Message, mutations = false }; }
    }
    static bool HasPreparation(string prepared) => !string.IsNullOrEmpty(prepared)
        && (File.Exists(Path.Combine(prepared, "before-prefs.tsv")) || File.Exists(Path.Combine(prepared, ReferenceFile)));
    static SnapshotReference ReadReference(string prepared)
    {
        var reference = Decode<SnapshotReference>(File.ReadAllText(Path.Combine(prepared, ReferenceFile)));
        if (reference == null || reference.version != 1 || string.IsNullOrEmpty(reference.snapshotPath)
            || !Path.IsPathRooted(reference.snapshotPath) || string.IsNullOrEmpty(reference.editorStateSessionKey))
            throw new InvalidOperationException("Invalid snapshot reference artifact.");
        if (!File.Exists(reference.snapshotPath) || Hash(File.ReadAllBytes(reference.snapshotPath)) != reference.sha256)
            throw new InvalidOperationException("Referenced snapshot changed or disappeared; restoration is blocked before writes.");
        var rows = ReadSnapshot(reference.snapshotPath);
        if (rows.Length != reference.keyCount) throw new InvalidOperationException("Referenced snapshot catalog count changed.");
        return reference;
    }
    public static string PreparedSnapshotPath(string preparedFolder)
    {
        if (string.IsNullOrEmpty(preparedFolder)) throw new InvalidOperationException("No prepared folder.");
        string reference = Path.Combine(preparedFolder, ReferenceFile);
        if (File.Exists(reference)) return ReadReference(preparedFolder).snapshotPath;
        string legacy = Path.Combine(preparedFolder, "before-prefs.tsv"); ReadSnapshot(legacy); return legacy;
    }
    public static string ValidatePrepared(string preparedFolder)
    {
        string path = PreparedSnapshotPath(preparedFolder);
        if (RestorationSucceeded(preparedFolder)) throw new InvalidOperationException("Preparation has already been restored.");
        if (File.Exists(Path.Combine(preparedFolder, ReferenceFile))) ReadEditorState(ReadReference(preparedFolder), preparedFolder);
        else if (!File.Exists(Path.Combine(preparedFolder, "editor-before.tsv"))) throw new InvalidOperationException("Legacy editor state is missing.");
        return path;
    }
    static PrefRow SessionValue(string key, string kind)
    {
        bool exists; string value;
        if (kind == "bool")
        { bool a = SessionState.GetBool(key, false), b = SessionState.GetBool(key, true); exists = a == b; value = a.ToString(); }
        else if (kind == "int")
        { int a = SessionState.GetInt(key, int.MinValue), b = SessionState.GetInt(key, int.MaxValue); exists = a == b; value = a.ToString(CultureInfo.InvariantCulture); }
        else if (kind == "float")
        { float a = SessionState.GetFloat(key, float.MinValue), b = SessionState.GetFloat(key, float.MaxValue); exists = a.Equals(b); value = a.ToString("R", CultureInfo.InvariantCulture); }
        else
        { string a = SessionState.GetString(key, "missing:a"), b = SessionState.GetString(key, "missing:b"); exists = a == b; value = a; }
        return new PrefRow { key = key, kind = kind, existed = exists, value = value };
    }
    static EditorState CaptureEditorState(string prepared)
    {
        return new EditorState { preparedFolder = prepared, startScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            tutorialExists = PlayerPrefs.HasKey("TutorialDone"), tutorial = PlayerPrefs.GetInt("TutorialDone", 0),
            timeScale = Time.timeScale, captureDeltaTime = Time.captureDeltaTime,
            session = new[] { SessionValue(Selector, "int"), SessionValue(Selector + ".Active", "bool"),
                SessionValue(Selector + ".DefaultPath", "string"), SessionValue(Selector + ".AppliedPath", "string"),
                SessionValue("NoryangjinMapTool.OpeningVideoEnabled", "bool"), SessionValue("NoryangjinMapTool.TestPower9999", "bool"),
                SessionValue("NoryangjinMapTool.TestFastLateral", "bool"), SessionValue("NoryangjinMapTool.TestTimeScale", "float"),
                SessionValue("SR18.Progression.Active", "bool") } };
    }
    static EditorState ReadEditorState(SnapshotReference reference, string prepared)
    {
        string json = SessionState.GetString(reference.editorStateSessionKey, "");
        if (string.IsNullOrEmpty(json)) throw new InvalidOperationException("Original editor state is missing from SessionState; no guessed restoration or fresh backup will be made.");
        var state = Decode<EditorState>(json);
        ValidateEditorState(state, prepared);
        return state;
    }
    static void ValidateEditorState(EditorState state, string prepared)
    {
        if (state == null || state.preparedFolder != prepared || state.session == null || state.session.Length != 9)
            throw new InvalidOperationException("Original editor SessionState is incomplete or belongs to another run.");
        var expected = CaptureEditorState(prepared).session.ToDictionary(r => r.key, r => r.kind);
        if (state.session.Select(r => r.key).Distinct().Count() != expected.Count
            || state.session.Any(r => !expected.TryGetValue(r.key, out string kind) || r.kind != kind))
            throw new InvalidOperationException("Original editor state key catalog changed.");
        foreach (var row in state.session)
        {
            if (row.kind == "bool") bool.Parse(row.value);
            else if (row.kind == "int") int.Parse(row.value, CultureInfo.InvariantCulture);
            else if (row.kind == "float") float.Parse(row.value, CultureInfo.InvariantCulture);
        }
        if (!string.IsNullOrEmpty(state.startScene) && AssetDatabase.LoadAssetAtPath<SceneAsset>(state.startScene) == null)
            throw new InvalidOperationException("Original Play Mode start scene no longer exists.");
    }
    static string ValidateEditorRoundTrip(EditorState original, string prepared)
    {
        // Unity JsonUtility omitted the nested PrefRow[] in the first native
        // preparation. Dedicated Newtonsoft serialization must prove the full
        // original state survives before any QA state is changed.
        ValidateEditorState(original, prepared);
        string json = Json(original);
        var roundTrip = Decode<EditorState>(json);
        ValidateEditorState(roundTrip, prepared);
        bool typedEqual = original.preparedFolder == roundTrip.preparedFolder && original.startScene == roundTrip.startScene
            && original.tutorialExists == roundTrip.tutorialExists && original.tutorial == roundTrip.tutorial
            && original.timeScale == roundTrip.timeScale && original.captureDeltaTime == roundTrip.captureDeltaTime
            && original.session.Length == roundTrip.session.Length
            && original.session.Zip(roundTrip.session, (a, b) => a.key == b.key && a.kind == b.kind && a.existed == b.existed && a.value == b.value).All(equal => equal);
        if (!typedEqual) throw new InvalidOperationException("Decoded editor state differs from the captured typed fields.");
        var currentMismatches = EditorMismatches(roundTrip);
        if (currentMismatches.Length != 0) throw new InvalidOperationException("Decoded editor state differs from actual current state: " + string.Join(",", currentMismatches));
        if (Json(roundTrip) != json) throw new InvalidOperationException("Editor state serialization did not preserve every field/value/existence flag.");
        return json;
    }
    public static object InspectEditorStateRoundTrip()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stable Edit Mode required.");
        const string probe = "read-only-editor-round-trip";
        var original = CaptureEditorState(probe);
        string json = ValidateEditorRoundTrip(original, probe);
        var decoded = Decode<EditorState>(json);
        return new { valid = true, mutations = false, serializer = "Newtonsoft.Json", sessionCount = decoded.session.Length,
            keys = decoded.session.Select(r => new { r.key, r.kind, r.existed }).ToArray(),
            exactRoundTrip = Json(decoded) == json, actualStateMismatches = EditorMismatches(decoded), serializedCharacters = json.Length };
    }
    static void RestoreEditorState(EditorState state)
    {
        if (state.tutorialExists) PlayerPrefs.SetInt("TutorialDone", state.tutorial); else PlayerPrefs.DeleteKey("TutorialDone");
        foreach (var row in state.session)
        {
            if (row.kind == "bool") { if (row.existed) SessionState.SetBool(row.key, bool.Parse(row.value)); else SessionState.EraseBool(row.key); }
            else if (row.kind == "int") { if (row.existed) SessionState.SetInt(row.key, int.Parse(row.value, CultureInfo.InvariantCulture)); else SessionState.EraseInt(row.key); }
            else if (row.kind == "float") { if (row.existed) SessionState.SetFloat(row.key, float.Parse(row.value, CultureInfo.InvariantCulture)); else SessionState.EraseFloat(row.key); }
            else { if (row.existed) SessionState.SetString(row.key, row.value); else SessionState.EraseString(row.key); }
        }
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(state.startScene) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(state.startScene);
        Time.timeScale = state.timeScale; Time.captureDeltaTime = state.captureDeltaTime;
        PlayerPrefs.Save();
    }
    static string[] EditorMismatches(EditorState state)
    {
        var mismatches = new List<string>();
        foreach (var row in state.session)
        { var current = SessionValue(row.key, row.kind); if (current.existed != row.existed || row.existed && current.value != row.value) mismatches.Add(row.key); }
        if (PlayerPrefs.HasKey("TutorialDone") != state.tutorialExists || PlayerPrefs.GetInt("TutorialDone", 0) != state.tutorial) mismatches.Add("TutorialDone");
        if (AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) != state.startScene) mismatches.Add("playModeStartScene");
        if (Time.timeScale != state.timeScale) mismatches.Add("Time.timeScale");
        if (Time.captureDeltaTime != state.captureDeltaTime) mismatches.Add("Time.captureDeltaTime");
        return mismatches.ToArray();
    }

    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    static T Decode<T>(string json) => (T)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { json, typeof(T) });
    static void Write(string file, object value) => File.WriteAllText(Path.Combine(folder, file), Json(value));
    [Serializable] sealed class RestorationReceipt { public bool restored; public int mismatches; }
    static bool RestorationSucceeded(string previous)
    {
        string path = Path.Combine(previous, "restored.json");
        if (!File.Exists(path)) return false;
        try { var receipt = Decode<RestorationReceipt>(File.ReadAllText(path)); return receipt != null && receipt.restored && receipt.mismatches == 0; }
        catch { return false; }
    }
    static void SelectStage(int stage) => typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestStartStage")
        .GetMethod("SelectStage", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { stage });

    public static object Prepare(string scene, int routeChoice = 0, string existingSnapshot = null)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Clean Edit Mode required.");
        for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scene work before QA.");
        if (!Chapter45Director.IsChapterScene(scene) || routeChoice < 0 || routeChoice > 1) throw new ArgumentException("Use Jamsil/ShoeTower and routeChoice 0/1.");
        string previous = SessionState.GetString(Session + "folder", "");
        if (HasPreparation(previous) && !RestorationSucceeded(previous))
            throw new InvalidOperationException("Restore the previous prepared QA snapshot before creating another.");
        if (string.IsNullOrEmpty(existingSnapshot))
            throw new InvalidOperationException("No new backups are permitted. Supply an eligible existing before-prefs.tsv; inspect it first with InspectSnapshotEligibility.");
        string snapshot = Path.GetFullPath(existingSnapshot);
        if (!File.Exists(snapshot)) throw new InvalidOperationException("Existing preference snapshot is missing.");
        string snapshotHash = Hash(File.ReadAllBytes(snapshot));
        var rows = ReadSnapshot(snapshot); var mismatchKeys = PreferenceMismatches(rows);
        if (mismatchKeys.Length != 0) throw new InvalidOperationException("Current preferences differ from the existing snapshot: " + string.Join(",", mismatchKeys));
        if (Hash(File.ReadAllBytes(snapshot)) != snapshotHash) throw new InvalidOperationException("Snapshot changed during eligibility verification.");
        folder = Path.GetFullPath("outputs/chapters45-2026-10-02/play/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + scene + "-" + routeChoice);
        if (Directory.Exists(folder)) throw new InvalidOperationException("Preserve existing run evidence.");
        string stateKey = Session + "editor." + Hash(Encoding.UTF8.GetBytes(folder));
        var editorState = CaptureEditorState(folder);
        string editorJson = ValidateEditorRoundTrip(editorState, folder);
        var reference = new SnapshotReference { snapshotPath = snapshot, sha256 = snapshotHash, keyCount = rows.Length, editorStateSessionKey = stateKey };
        Directory.CreateDirectory(folder);
        SessionState.SetString(stateKey, editorJson);
        if (Json(ReadEditorState(reference, folder)) != editorJson)
            throw new InvalidOperationException("Retained editor SessionState failed exact read-back; no QA flags were changed.");
        // Only a reference artifact: no preference values, editor values or TSV copy.
        File.WriteAllText(Path.Combine(folder, ReferenceFile), Json(reference));
        SessionState.SetString(Session + "folder", folder); SessionState.SetString(Session + "scene", scene); SessionState.SetInt(Session + "choice", routeChoice);
        SessionState.SetBool("NoryangjinMapTool.TestPower9999", false);
        SessionState.SetBool("NoryangjinMapTool.TestFastLateral", false);
        SessionState.SetFloat("NoryangjinMapTool.TestTimeScale", 1);
        // Tutorial dismissal affects onboarding only and is included in restoration.
        PlayerPrefs.SetInt("TutorialDone", 1); PlayerPrefs.Save();
        SelectStage(scene == "Jamsil" ? 4 : 5);
        Write("conditions.json", new { scene, routeChoice, speed = 1, stats = "current user persistent upgrades", statOverrides = false,
            snapshotMode = "verified existing reference", snapshot, snapshotHash, snapshotKeyCount = rows.Length, newBackups = false,
            teleport = false, forcedWin = false, enemiesDisabledByHarness = false, collisionBypass = false, transitionsSuppressed = false,
            input = "FixedUpdate invocation of PlayerMove, the touch/keyboard input path", tutorialDismissed = true, watchdogSeconds = 600 });
        return new { prepared = true, scene, routeChoice, folder, snapshot, newBackups = false };
    }

    public static object Begin()
    {
        folder = SessionState.GetString(Session + "folder", ""); scene = SessionState.GetString(Session + "scene", ""); routeChoice = SessionState.GetInt(Session + "choice", 0);
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != scene || !HasPreparation(folder)) throw new InvalidOperationException("Prepared scene in fresh Play Mode required.");
        PreparedSnapshotPath(folder);
        if (File.Exists(Path.Combine(folder, ReferenceFile))) ReadEditorState(ReadReference(folder), folder);
        if (RestorationSucceeded(folder)) throw new InvalidOperationException("This prepared run has already been restored; prepare a fresh evidence folder.");
        if (File.Exists(Path.Combine(folder, "start.json"))) throw new InvalidOperationException("Preserve prior run evidence; prepare a new run.");
        player = Object.FindFirstObjectByType<PlayerScript>(); director = Object.FindFirstObjectByType<Chapter45Director>();
        chapter = Object.FindFirstObjectByType<ChapterProgression>(); canvas = Object.FindFirstObjectByType<CanvasScript>();
        var overlay = Object.FindFirstObjectByType<CombatHarness>();
        if (overlay != null) typeof(CombatHarness).GetField("overlayVisible", Private)?.SetValue(overlay, false);
        if (player == null || director == null || chapter == null || canvas == null || TimeManager.isGameRunning) throw new InvalidOperationException("Fresh chapter lobby and all runtime components required.");
        playerWeapons = player.GetComponentsInChildren<WeaponScript>(true);
        active = true; started = finished = false; outcome = "running"; frame = -1; moves = shots = timelineCursor = 0;
        damage.Clear(); images.Clear(); errors.Clear(); animationWarnings.Clear(); openingLanes.Clear(); enemyCommitments.Clear(); avoidanceLanes.Clear();
        liftMidCaptured.Clear(); liftTurnCaptured.Clear(); hudObserved = hudRecorded = null; hudChangedAt = 0; hudChanges = hudCaptures = 0;
        inputPolicy = "starting"; desiredLane = 0;
        nextSample = nextShot = 0; readyAt = EditorApplication.timeSinceStartup + 1.5; wallStarted = EditorApplication.timeSinceStartup;
        Time.timeScale = 1; Time.captureDeltaTime = 0;
        if (Object.FindObjectsByType<OpeningStoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(o => o.gameObject.activeInHierarchy)) throw new InvalidOperationException("Saved chapter opening root must be inactive; QA does not skip it.");
        File.WriteAllText(Path.Combine(folder, "timeline.jsonl"), ""); File.WriteAllText(Path.Combine(folder, "director-timeline.txt"), "");
        Application.logMessageReceived += OnLog; EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Write("status.json", new { prepared = true, active, started, finished, scene, routeChoice, folder });
        return new { active, scene, routeChoice, folder };
    }
    static void OnLog(string message, string stack, LogType kind)
    {
        if (kind == LogType.Warning && (message.IndexOf("Animator", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("animation", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            animationWarnings.Add(message);
            File.AppendAllText(Path.Combine(folder, "animation-warnings.txt"), message + "\n" + stack + "\n");
        }
        if (kind != LogType.Error && kind != LogType.Exception && kind != LogType.Assert) return;
        errors.Add(kind + ": " + message);
        File.AppendAllText(Path.Combine(folder, "errors.txt"), kind + ": " + message + "\n" + stack + "\n");
    }
    static void Tick()
    {
        if (!active) return;
        if (!EditorApplication.isPlaying) { Detach(); return; }
        if (EditorApplication.isCompiling || EditorApplication.isPaused || frame == Time.frameCount) return;
        frame = Time.frameCount;
        try
        {
            if (finished)
            {
                ObserveHud();
                if (EditorApplication.timeSinceStartup - endedAt < 3) return;
                Capture("result-or-transition");
                Write("capture-status.json", new { requested = images.ToArray(), written = images.Where(File.Exists).ToArray(), width = Screen.width, height = Screen.height,
                    hudChanges, hudCaptures, hudCaptureLimit = 100, liftMidCaptured = liftMidCaptured.OrderBy(i => i).ToArray() });
                Detach(); return;
            }
            if (EditorApplication.timeSinceStartup - wallStarted >= 600) { Finish("wall-clock-limit"); return; }
            if (player == null || director == null) { Finish("unexpected-scene-unload"); return; }
            if (!started)
            {
                if (EditorApplication.timeSinceStartup < readyAt) return;
                canvas.PlayerPressedStartButton();
                if (!TimeManager.isGameRunning) { readyAt = EditorApplication.timeSinceStartup + 1; return; }
                if (Mathf.Abs(Time.timeScale - 1) > .001f || Mathf.Abs(TimeManager.timeFactor - 1) > .001f) throw new InvalidOperationException("Ordinary 1x run failed: unexpected time override.");
                started = true; began = Time.time; initialHealth = lastHealth = player.currentHealth; initialAttack = lastAttack = player.ResolvedAttackDamage;
                initialCoins = MoneyScript.S != null ? MoneyScript.S.Coin : 0; initialJewels = MoneyScript.S != null ? MoneyScript.S.Jewel : 0;
                Write("start.json", new { scene, routeChoice, health = initialHealth, maxHealth = player.MaxHealth, attack = initialAttack, speed = player.ForwardMoveSpeed,
                    fireRate = player.DefaultFireRate, missileSpeed = BulletScript.BaseMissileSpeed, missileSeconds = BulletScript.CurrentMissileDuration,
                    coin = initialCoins, jewel = initialJewels, statOverrides = false, timeScale = Time.timeScale, timeFactor = TimeManager.timeFactor,
                    levels = Enumerable.Range(1, Enum.GetValues(typeof(UpgradeStatManager.UpgradeType)).Length).Select(i => new { id = i, level = PlayerPrefs.GetInt("upgrade_lv_" + i) }).ToArray() });
                Capture("start"); canvas.StartCoroutine(Drive());
            }
            float elapsed = Time.time - began;
            ObserveHud();
            while (timelineCursor < director.Timeline.Count)
            {
                string entry = director.Timeline[timelineCursor++]; File.AppendAllText(Path.Combine(folder, "director-timeline.txt"), entry + "\n");
                if (entry.Contains("lift board") || entry.Contains("lift land") || entry.Contains("choice committed") || entry.Contains("choice reward") || entry.Contains("captain phase") || entry.Contains("target destroyed") || entry.Contains("physically")) Capture("event-" + timelineCursor);
            }
            if (player.currentHealth < lastHealth - .01f)
            {
                damage.Add(new { t = elapsed, distance = director.Distance, floor = director.CurrentFloor, before = lastHealth, after = player.currentHealth, cause = player.LastDamageCause.ToString() });
                Capture("damage-" + damage.Count);
            }
            lastHealth = player.currentHealth; lastAttack = player.ResolvedAttackDamage;
            for (int index = 0; index < director.choices.Length; index++)
            {
                var choice = director.choices[index];
                if (!choice.Selected && director.Distance >= choice.distance - 26 && director.Distance < choice.distance - 20 && !images.Any(x => x.Contains("choice-preview-" + index))) Capture("choice-preview-" + index);
            }
            if (elapsed >= nextShot) { nextShot = elapsed + 35; Capture("route"); }
            if (director.IsTransferring && !liftMidCaptured.Contains(director.LiftCount))
            {
                float rideElapsed = (float)RideElapsed.GetValue(director), rideDuration = (float)RideDuration.GetValue(director);
                if (rideElapsed >= 3 && rideElapsed < rideDuration)
                {
                    liftMidCaptured.Add(director.LiftCount); Capture("lift-mid-" + director.LiftCount);
                    File.AppendAllText(Path.Combine(folder, "lift-captures.jsonl"), Json(new { liftIndex = director.LiftCount,
                        directorElapsed = director.Elapsed, rideElapsed, rideDuration, requestedRideElapsed = 3,
                        delayPastRequested = rideElapsed - 3, boardElapsed = director.Elapsed - rideElapsed }) + "\n");
                }
            }
            if (director.IsTransferring && !liftTurnCaptured.Contains(director.LiftCount))
            {
                float rideElapsed = (float)RideElapsed.GetValue(director), rideDuration = (float)RideDuration.GetValue(director);
                if (rideElapsed >= 5.1f && rideElapsed < rideDuration)
                {
                    liftTurnCaptured.Add(director.LiftCount); Capture("lift-turn-" + director.LiftCount);
                    File.AppendAllText(Path.Combine(folder, "lift-turn-captures.jsonl"), Json(new { liftIndex = director.LiftCount,
                        directorElapsed = director.Elapsed, rideElapsed, rideDuration, requestedRideElapsed = 5.1f,
                        playerForward = V(player.transform.forward), position = V(player.transform.position) }) + "\n");
                }
            }
            if (elapsed >= nextSample)
            {
                nextSample = elapsed + .5f;
                var state = new { scene, routeChoice, active, started, finished, outcome, seconds = elapsed, distance = director.Distance, length = director.route.Length,
                    floor = director.CurrentFloor, position = new[] { player.transform.position.x, player.transform.position.y, player.transform.position.z }, lane = director.Lane,
                    hp = player.currentHealth, attack = player.ResolvedAttackDamage, running = TimeManager.isGameRunning, lift = director.IsTransferring, lifts = director.LiftCount,
                    hud = ReadHud(),
                    choice = director.choices.Select(c => new { c.name, c.Selected, c.Selection, c.Rewarded }).ToArray(),
                    targets = director.targets.Where(t => t.floor == director.CurrentFloor && t.Eligible).Select(t => new { t.name, t.Health, t.Open, t.Phase, t.Shielded }).ToArray(),
                    activeEnemies = director.encounters.SelectMany(e => e.actors).Count(e => e != null && e.gameObject.activeInHierarchy && !e.IsDead),
                    playerForward = V(player.transform.forward),
                    weapons = playerWeapons.Where(w => w != null && w.gameObject.activeInHierarchy).Select(w => new { w.name, w.TotalProjectilesSpawned,
                        spawn = V(w.LastProjectileSpawnPosition), mouth = V(w.LastVisibleMouthPositionAtSpawn), w.fireRate, w.damage, w.bulletCount }).ToArray(),
                    nearbyActors = director.encounters.SelectMany(e => e.actors).Where(e => e != null && e.gameObject.activeInHierarchy && Vector3.Distance(e.transform.position, player.transform.position) < 55).Select(ActorSample).ToArray(),
                    projectiles = Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled).Take(30).Select(b => new { b.name,
                        position = V(b.transform.position), rootPosition = V(b.transform.root.position), direction = V((Vector3)ShotDirection.GetValue(b)),
                        deck = (int)ShotDeck.GetValue(b), launchHeight = (float)ShotHeight.GetValue(b), retired = (bool)ShotRetired.GetValue(b), b.LaunchDamage, b.HasDamagePayload }).ToArray(),
                    moves, inputPolicy, desiredLane, errors = errors.Count, render = new { UnityStats.batches, UnityStats.setPassCalls, UnityStats.triangles } };
                File.AppendAllText(Path.Combine(folder, "timeline.jsonl"), Json(state) + "\n"); Write("status.json", state);
            }
            if (player.currentHealth <= 0) { Finish("death"); return; }
            if (chapter.Completed || director.GoalClaimed) { Finish("clear"); return; }
            if (CanvasScript.isGameOver) { Finish("game-over-without-clear"); return; }
            if (elapsed >= 600) Finish("game-time-limit");
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(folder, "harness-error.txt"), error.ToString()); Finish("harness-error"); }
    }
    static IEnumerator Drive()
    {
        while (active && !finished && player != null && director != null)
        {
            yield return new WaitForFixedUpdate();
            if (!TimeManager.isGameRunning || director.IsTransferring || player.currentHealth <= 0 || player.IsWorldYawTurnActive || player.IsStationaryCombat) continue;
            var origin = (Vector3)Origin.GetValue(player); var right = (Vector3)Right.GetValue(player); var range = (Vector2)Range.GetValue(player);
            float desired = 0, nearest = 35;
            inputPolicy = "clear lane";
            var forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            foreach (var encounter in director.encounters)
            {
                if (!encounter.Activated || encounter.Complete || encounter.floor != director.CurrentFloor || !encounter.Eligible) continue;
                // A folded floor can bring passed actors geometrically ahead on
                // a neighboring corridor. They are not the current shot lane.
                if (!encounter.requiredToProceed && director.Distance > encounter.stopDistance + 8) continue;
                int key = encounter.GetInstanceID();
                if (!enemyCommitments.TryGetValue(key, out var target) || target == null || target.IsDead)
                {
                    target = encounter.actors.Where(e => e != null && e.gameObject.activeInHierarchy && !e.IsDead)
                        .OrderBy(e => e.CurrentHealth).ThenBy(e => Mathf.Abs(Vector3.Dot(e.transform.position - player.transform.position, right))).FirstOrDefault();
                    enemyCommitments[key] = target;
                }
                if (target == null) continue;
                float actorLane = Vector3.Dot(target.transform.position - origin, right);
                if (actorLane < range.x - 2 || actorLane > range.y + 2) continue;
                float ahead = Vector3.Dot(target.transform.position - player.transform.position, forward);
                if (ahead < 0 || ahead > nearest) continue;
                nearest = ahead;
                // A fresh second guard in a row may be too close to defeat before
                // contact. Ordinary play can use the clear lane instead of ramming.
                // Required fights have an authored forward stop and retain aim.
                if (!encounter.requiredToProceed && !avoidanceLanes.ContainsKey(key) && ahead < 20)
                {
                    float contactSeconds = Mathf.Max(0, ahead - 2) / Mathf.Max(.1f, player.ForwardMoveSpeed);
                    float flightSeconds = Mathf.Max(0, ahead - 2.3f) / Mathf.Max(1, BulletScript.BaseMissileSpeed);
                    // An established firing lane already has shots in flight.
                    // A newly selected lane must first wait for projectile travel.
                    float targetLane = Vector3.Dot(target.transform.position - origin, right);
                    if (Mathf.Abs(targetLane - director.Lane) < .85f) flightSeconds = 0;
                    int possibleShots = Mathf.FloorToInt(Mathf.Max(0, contactSeconds - flightSeconds - .2f) * player.DefaultFireRate);
                    int neededShots = Mathf.CeilToInt(target.CurrentHealth / Mathf.Max(.1f, player.ResolvedAttackDamage));
                    if (neededShots > possibleShots)
                    {
                        float clearLane = FindClearLane(encounter, origin, right, range);
                        avoidanceLanes[key] = clearLane;
                        File.AppendAllText(Path.Combine(folder, "input-policy.jsonl"), Json(new { seconds = Time.time - began,
                            distance = director.Distance, encounter = encounter.name, target = target.name, action = "dodge insufficient firing window",
                            ahead, target.CurrentHealth, neededShots, possibleShots, clearLane }) + "\n");
                    }
                }
                if (avoidanceLanes.TryGetValue(key, out float avoidLane)) { desired = avoidLane; inputPolicy = "dodge " + encounter.name; }
                else { desired = Vector3.Dot(target.transform.position - origin, right); inputPolicy = "aim " + encounter.name; }
            }
            foreach (var group in director.targets.Where(t => t.floor == director.CurrentFloor && t.Eligible).GroupBy(t => Mathf.RoundToInt(t.distance * 10)))
            {
                var first = group.First(); float ahead = first.distance - director.Distance;
                if (ahead < -.5f || ahead > 32 || (ahead > 8 && ahead > nearest)) continue;
                if (!openingLanes.TryGetValue(group.Key, out float lane))
                {
                    lane = group.OrderBy(t => t.Health).ThenBy(t => Mathf.Abs(t.CurrentLane - director.Lane)).First().CurrentLane;
                    openingLanes[group.Key] = lane;
                }
                var captain = group.FirstOrDefault(t => t.captain && t.Alive);
                desired = captain != null ? captain.CurrentLane : lane; nearest = ahead; inputPolicy = "aim target " + first.name;
            }
            // Physical commitments take precedence in the marked approach window.
            foreach (var choice in director.choices)
                if (!choice.Selected && director.Distance >= choice.distance - 26 && director.Distance <= choice.distance)
                    desired = choice.kind == Chapter45Choice.ChoiceKind.ShieldOrAttack ? (routeChoice == 0 ? -3 : 3) : (routeChoice == 0 ? -3 : 3);
            foreach (var hazard in director.hazards)
                if (hazard.floor == director.CurrentFloor && hazard.Triggered && !hazard.Finished && Mathf.Abs(hazard.distance - director.Distance) < 35)
                    desired = hazard.safeLane;
            if (director.Distance >= director.route.Length - 25)
            {
                var goal = director.goals.FirstOrDefault(g => g.floor == director.CurrentFloor && !g.Claimed);
                if (goal != null) desired = Vector3.Dot(goal.transform.position - origin, right);
            }
            desired = Mathf.Clamp(desired, range.x, range.y);
            desiredLane = desired;
            float error = desired - Vector3.Dot(player.transform.position - origin, right);
            if (Mathf.Abs(error) > .06f) { Move.Invoke(player, new object[] { Mathf.Clamp(error * 12, -15, 15) }); moves++; }
        }
    }
    static float FindClearLane(Chapter45Encounter encounter, Vector3 origin, Vector3 right, Vector2 range)
    {
        float low = Chapter45Director.ConstrainLane(player, range.x + .6f);
        float high = Chapter45Director.ConstrainLane(player, range.y - .6f);
        var occupied = encounter.actors.Where(e => e != null && e.gameObject.activeInHierarchy && !e.IsDead)
            .Select(e => Vector3.Dot(e.transform.position - origin, right)).OrderBy(x => x).ToArray();
        var candidates = new List<float> { Mathf.Clamp(0, low, high), low, high };
        for (int i = 1; i < occupied.Length; i++) candidates.Add(Mathf.Clamp((occupied[i - 1] + occupied[i]) * .5f, low, high));
        float best = candidates[0], score = float.NegativeInfinity;
        foreach (float lane in candidates)
        {
            float clearance = occupied.Length == 0 ? 100 : occupied.Min(x => Mathf.Abs(x - lane));
            // Prefer generous clearance; equal gaps favor less steering.
            float candidateScore = clearance - Mathf.Abs(lane - director.Lane) * .025f;
            if (candidateScore > score) { best = lane; score = candidateScore; }
        }
        return best;
    }
    static HudState ReadHud()
    {
        var hud = director != null ? director.hud : null;
        bool TextVisible(TMPro.TMP_Text text) => text != null && text.isActiveAndEnabled
            && text.gameObject.activeInHierarchy && text.color.a > .01f && text.canvasRenderer.GetAlpha() > .01f && !text.canvasRenderer.cull;
        return new HudState { present = hud != null,
            panelVisible = hud != null && hud.panel != null && hud.panel.activeInHierarchy,
            title = hud != null && hud.title != null ? hud.title.text : null,
            detail = hud != null && hud.description != null ? hud.description.text : null,
            titleVisible = hud != null && TextVisible(hud.title), detailVisible = hud != null && TextVisible(hud.description) };
    }
    static void ObserveHud()
    {
        var hud = ReadHud(); string signature = Json(hud);
        if (signature != hudObserved) { hudObserved = signature; hudChangedAt = Time.unscaledTime; return; }
        if (signature == hudRecorded || Time.unscaledTime - hudChangedAt < .1f) return;
        hudRecorded = signature; hudChanges++;
        // One screenshot per stable state change; no .5s sampling duplicates.
        // Cap screenshots if a product bug causes repeated flicker, retaining all
        // stable changes in the timeline and explicitly reporting suppression.
        bool capture = hudCaptures < 100;
        File.AppendAllText(Path.Combine(folder, "hud-timeline.jsonl"), Json(new { change = hudChanges,
            seconds = started ? Time.time - began : 0, directorElapsed = director != null ? director.Elapsed : -1,
            distance = director != null ? director.Distance : -1, hud, captureRequested = capture,
            observation = "Native active/enabled/alpha/culling flags; screenshot is separate visual evidence." }) + "\n");
        if (capture) { hudCaptures++; Capture("hud-change-" + hudChanges); }
    }
    static void Capture(string label)
    {
        string path = Path.Combine(folder, (shots++).ToString("D3") + "-" + (started ? (Time.time - began).ToString("000.0", CultureInfo.InvariantCulture) : "lobby") + "-" + label + ".png");
        images.Add(path); ScreenCapture.CaptureScreenshot(path);
    }
    static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
    static object ActorSample(EnemyScript_space actor)
    {
        var collider = actor.GetComponent<Collider>();
        var renderers = actor.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(actor.transform.position, Vector3.zero);
        if (renderers.Length > 0) { bounds = renderers[0].bounds; for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds); }
        var delta = actor.transform.position - player.transform.position;
        return new { actor.name, actor.CurrentHealth, actor.AttackDamage, actor.IsDead, position = V(actor.transform.position), forward = V(actor.transform.forward),
            ahead = Vector3.Dot(delta, player.transform.forward), lane = Vector3.Dot(actor.transform.position - (Vector3)Origin.GetValue(player), (Vector3)Right.GetValue(player)),
            colliderEnabled = collider != null && collider.enabled, colliderMin = collider != null ? V(collider.bounds.min) : null, colliderMax = collider != null ? V(collider.bounds.max) : null,
            modelMin = V(bounds.min), modelMax = V(bounds.max) };
    }
    static void Finish(string reason)
    {
        if (finished) return;
        finished = true; outcome = reason; endedAt = EditorApplication.timeSinceStartup;
        Capture("end-" + reason);
        Write("summary.json", new { scene, routeChoice, outcome, finished, seconds = started ? Time.time - began : 0, wallSeconds = EditorApplication.timeSinceStartup - wallStarted,
            initialHealth, initialAttack, hp = player != null ? player.currentHealth : -1, attack = player != null ? player.ResolvedAttackDamage : -1,
            initialCoins, initialJewels, coin = MoneyScript.S != null ? MoneyScript.S.Coin : -1, jewel = MoneyScript.S != null ? MoneyScript.S.Jewel : -1,
            completed = chapter != null && chapter.Completed, claimed = director != null && director.GoalClaimed, distance = director != null ? director.Distance : -1,
            lifts = director != null ? director.LiftCount : -1, lastLiftSeconds = director != null ? director.LastLiftSeconds : -1,
            choices = director != null ? director.choices.Select(c => new { c.name, c.Selected, c.Selection, c.Rewarded, c.ResolvedCoins }).ToArray() : null,
            targets = director != null ? director.targets.Select(t => new { t.name, t.Health, t.Open, t.Phase }).ToArray() : null,
            encounters = director != null ? director.encounters.Select(e => new { e.name, e.Activated, e.Complete, e.Eligible }).ToArray() : null,
            damage, moves, errors, animationWarnings, nativeImages = images.ToArray(), statOverrides = false, teleports = false, forcedWins = false,
            hudChanges, hudCaptures, hudCaptureLimit = 100, liftMidCaptured = liftMidCaptured.OrderBy(i => i).ToArray(),
            limits = "Automated input policy; this is not a human fun certification or device performance measurement." });
        Write("status.json", new { finished, outcome, scene, routeChoice, folder });
    }
    static void Detach() { active = false; EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog; }
    public static object Poll()
    {
        string root = SessionState.GetString(Session + "folder", "");
        string path = Path.Combine(root, "status.json");
        return new { folder = root, status = File.Exists(path) ? File.ReadAllText(path) : "not begun", summaryExists = File.Exists(Path.Combine(root, "summary.json")) };
    }
    public static object Restore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before restoring user preferences.");
        folder = SessionState.GetString(Session + "folder", "");
        if (!HasPreparation(folder)) throw new InvalidOperationException("No prepared snapshot or reference.");
        string snapshot = PreparedSnapshotPath(folder); var rows = ReadSnapshot(snapshot);
        bool referenced = File.Exists(Path.Combine(folder, ReferenceFile));
        EditorState state;
        if (referenced) state = ReadEditorState(ReadReference(folder), folder);
        else
        {
            var legacy = File.ReadAllLines(Path.Combine(folder, "editor-before.tsv")).Select(x => x.Split(new[] { '\t' }, 2)).ToDictionary(x => x[0], x => x[1]);
            PrefRow Known(string key, string kind, string value) => new PrefRow { key = key, kind = kind, value = value, existed = true };
            string savedKey = "ChapterTest." + snapshot;
            var power = SessionValue(savedKey + ".OriginalPower", "bool");
            var lateral = SessionValue(savedKey + ".OriginalLateral", "bool");
            var speed = SessionValue(savedKey + ".OriginalSpeed", "float");
            if (!power.existed || !lateral.existed || !speed.existed)
                throw new InvalidOperationException("Legacy test-flag SessionState is unavailable; cannot safely guess original editor flags.");
            state = new EditorState { preparedFolder = folder, startScene = legacy["startScene"],
                tutorialExists = bool.Parse(legacy["tutorialExists"]), tutorial = int.Parse(legacy["tutorial"], CultureInfo.InvariantCulture),
                timeScale = 1, captureDeltaTime = 0,
                session = new[] { Known(Selector, "int", legacy["stage"]), Known(Selector + ".Active", "bool", legacy["selectorActive"]),
                    Known(Selector + ".DefaultPath", "string", legacy["selectorDefault"]), Known(Selector + ".AppliedPath", "string", legacy["selectorApplied"]),
                    Known("NoryangjinMapTool.OpeningVideoEnabled", "bool", legacy["opening"]),
                    Known("NoryangjinMapTool.TestPower9999", "bool", power.value), Known("NoryangjinMapTool.TestFastLateral", "bool", lateral.value),
                    Known("NoryangjinMapTool.TestTimeScale", "float", speed.value), Known("SR18.Progression.Active", "bool", "False") } };
            foreach (var row in state.session)
            {
                if (row.kind == "bool") bool.Parse(row.value);
                else if (row.kind == "int") int.Parse(row.value, CultureInfo.InvariantCulture);
                else if (row.kind == "float") float.Parse(row.value, CultureInfo.InvariantCulture);
            }
            if (!string.IsNullOrEmpty(state.startScene) && AssetDatabase.LoadAssetAtPath<SceneAsset>(state.startScene) == null)
                throw new InvalidOperationException("Original legacy Play Mode start scene is missing.");
        }
        // All snapshot/reference/catalog/editor validation completed before the
        // first preference write. Do not call RestoreAt on a referenced old run:
        // its old SessionState flags do not describe this new preparation.
        foreach (var row in rows)
        {
            if (!row.existed) PlayerPrefs.DeleteKey(row.key);
            else if (row.kind == "int") PlayerPrefs.SetInt(row.key, int.Parse(row.value, CultureInfo.InvariantCulture));
            else if (row.kind == "float") PlayerPrefs.SetFloat(row.key, float.Parse(row.value, CultureInfo.InvariantCulture));
            else PlayerPrefs.SetString(row.key, Encoding.UTF8.GetString(Convert.FromBase64String(row.value)));
        }
        PlayerPrefs.Save(); CosmeticService.ReloadCatalog(); RestoreEditorState(state); Detach();
        string[] preferenceMismatches = PreferenceMismatches(rows), editorMismatches = EditorMismatches(state);
        int mismatches = preferenceMismatches.Length + editorMismatches.Length;
        Write("restored.json", new { restored = mismatches == 0, mismatches, preferenceMismatches, editorMismatches,
            snapshot, snapshotMode = referenced ? "existing reference" : "legacy local snapshot", newBackups = false,
            sceneSelector = SessionState.GetInt(Selector, 0), startScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene), utc = DateTime.UtcNow.ToString("O") });
        return new { restored = mismatches == 0, mismatches, preferenceMismatches, editorMismatches, folder };
    }
}


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
        string snapshot = Chapters45Playtest.ValidatePrepared(prepared);
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