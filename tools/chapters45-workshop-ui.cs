using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IndianOceanAssets.ShooterSurvival;

// Directed native UI/transaction fixture, NOT ordinary gameplay or touch-input proof.
// Root: Prepare("Jamsil",0), enter fresh portrait Play lobby, Begin(), Poll().
// Then exit Play and Chapters45Playtest.Restore(); verify restored.json mismatches=0.
// Workspace tool only: no scene/asset writes and no automatic test/editor invocation.
public static class Chapters45WorkshopUI
{
    static HarborUpgradeTabs tabs;
    static ChapterUpgradeCardUI[] cards;
    static Button open, back;
    static ScrollRect scroll;
    static string folder, phase;
    static bool active;
    static int state, frame, failures, initialJewels, beforeCoins;
    static float beforeAttack, beforeHealth;
    static CombatHarness overlay;
    static System.Reflection.FieldInfo overlayVisible;
    static bool overlayWasVisible;
    static double began, stateAt;
    static readonly List<object> checks = new();
    static readonly List<string> errors = new(), images = new();
    static double Age => EditorApplication.timeSinceStartup - stateAt;
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    static void Write(string name, object value) => File.WriteAllText(Path.Combine(folder, name), Json(value));
    static T[] Find<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
    static void Require(string name, bool passed, object evidence)
    {
        var item = new { name, passed, evidence }; checks.Add(item);
        File.AppendAllText(Path.Combine(folder, "checks.jsonl"), Json(item) + "\n");
        if (!passed) { failures++; throw new InvalidOperationException(name); }
    }
    static bool Opens(Button button, GameObject target)
    {
        var calls = new SerializedObject(button).FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        for (int i = 0; calls != null && i < calls.arraySize; i++)
        {
            var call = calls.GetArrayElementAtIndex(i);
            if (call.FindPropertyRelative("m_Target").objectReferenceValue == target
                && call.FindPropertyRelative("m_MethodName").stringValue == "SetActive"
                && call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue) return true;
        }
        return false;
    }
    public static object Begin()
    {
        string prepared = SessionState.GetString("Chapter45.QA.folder", "");
        string snapshot = Path.Combine(prepared, "before-prefs.tsv");
        if (active || !EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Jamsil"
            || TimeManager.isGameRunning || CanvasScript.isGameOver || !File.Exists(snapshot)
            || File.Exists(Path.Combine(prepared, "restored.json")) || File.Exists(Path.Combine(prepared, "start.json")))
            throw new InvalidOperationException("Fresh prepared Jamsil Play lobby with unrestored complete snapshot required.");
        var keys = new HashSet<string>(File.ReadAllLines(snapshot).Select(line => line.Split('\t')[0]));
        var required = new List<string> { "coin", "jewel", "chapter_unlocked" };
        for (int i = 1; i <= 5; i++) { required.Add(ChapterUpgradeService.LevelKey(i)); required.Add(ChapterUpgradeService.OwnedKey(i)); }
        if (required.Any(k => !keys.Contains(k))) throw new InvalidOperationException("Snapshot lacks workshop/unlock/wallet keys.");
        if (MoneyScript.S == null || UpgradeStatManager.S == null) throw new InvalidOperationException("Native wallet/stat manager required.");
        tabs = Find<HarborUpgradeTabs>().Single();
        open = Find<Button>().Single(b => b.gameObject.activeInHierarchy && Opens(b, tabs.gameObject));
        back = tabs.transform.Find("Back").GetComponent<Button>();
        folder = Path.Combine(prepared, "workshop-native-ui");
        if (Directory.Exists(folder)) throw new InvalidOperationException("Preserve existing fixture evidence; prepare a fresh snapshot.");
        Directory.CreateDirectory(folder); checks.Clear(); errors.Clear(); images.Clear(); failures = 0; frame = -1; state = 0;
        began = stateAt = EditorApplication.timeSinceStartup;
        var director = Find<Chapter45Director>().Single();
        var chapter = Find<ChapterProgression>().Single();
        Require("Fresh lobby has not started combat", !director.Running && chapter.Elapsed < .01f && !chapter.Completed,
            new { director.Running, chapter.Elapsed, chapter.Completed });
        var opening = Find<OpeningStoryUI>().Single();
        var tutorial = Find<CoastalTutorialUI>().Single();
        var canvas = Find<CanvasScript>().Single();
        Require("New chapter lobby is unblocked without story skip", !opening.gameObject.activeInHierarchy && !OpeningStoryUI.IsBlockingGameplay
            && canvas.isActiveAndEnabled && !tutorial.enabled && tutorial.panel != null && !tutorial.panel.activeInHierarchy,
            new { openingActive = opening.gameObject.activeInHierarchy, blocking = OpeningStoryUI.IsBlockingGameplay, canvasActive = canvas.isActiveAndEnabled, tutorial.enabled });
        Require("Native viewport is portrait", Screen.width > 0 && Screen.height > Screen.width, new { Screen.width, Screen.height });
        initialJewels = MoneyScript.S.Jewel;
        Write("conditions.json", new { mode = "seeded directed native workshop UI fixture", ordinaryGameplay = false, touchGestureTest = false,
            snapshot, fixtureMutations = new[] { "chapter_unlocked=5", "chapter4/5 workshop level=0 and legacy owned=0", "wallet coins=100000; jewels unchanged" },
            actions = "Actual existing lobby/tab/back/buy UnityEvents, one buy invocation per new chapter; ScrollRect normalized-position control",
            appliedBonusProof = "UpgradeStatManager.ApplyToBase normalized by existing permanent upgrades, +0.05 per purchase; saved level verified before and after reopen",
            diskPersistenceLimit = "Production purchase calls PlayerPrefs.Save; this fixture verifies PlayerPrefs state and UI reopen in-session, not process restart",
            restore = "Root exits Play then Chapters45Playtest.Restore and checks restored.json", watchdogSeconds = 60 });
        PlayerPrefs.SetInt("chapter_unlocked", 5);
        foreach (int i in new[] { 4, 5 }) { PlayerPrefs.SetInt(ChapterUpgradeService.LevelKey(i), 0); PlayerPrefs.SetInt(ChapterUpgradeService.OwnedKey(i), 0); }
        PlayerPrefs.Save(); MoneyScript.S.Coin = 100000; ChapterUpgradeService.ApplyToStats(UpgradeStatManager.S);
        active = true; phase = "open actual workshop button";
        overlay = Find<CombatHarness>().FirstOrDefault();
        overlayVisible = typeof(CombatHarness).GetField("overlayVisible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (overlay != null && overlayVisible != null) { overlayWasVisible = (bool)overlayVisible.GetValue(overlay); overlayVisible.SetValue(overlay, false); }
        Application.logMessageReceived += OnLog; EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Click(open, "Lobby workshop open"); Status(); return new { active, folder, phase };
    }
    static void Click(Button button, string label)
    {
        Require(label + " is active and interactable", button != null && button.gameObject.activeInHierarchy && button.IsInteractable(), new { button = button != null ? button.name : "null" });
        button.onClick.Invoke();
    }
    static void Next(int next, string label) { state = next; phase = label; stateAt = EditorApplication.timeSinceStartup; Status(); }
    static void Roster(string label)
    {
        cards = tabs.chapters.GetComponentsInChildren<ChapterUpgradeCardUI>(true).OrderBy(c => c.chapter).ToArray();
        Require(label + " has exactly five independent cards", cards.Select(c => c.chapter).SequenceEqual(new[] { 1, 2, 3, 4, 5 })
            && cards.Select(c => c.buyButton).Distinct().Count() == 5 && cards.All(c => c.buyButton.transform.IsChildOf(c.transform)),
            new { chapters = cards.Select(c => c.chapter).ToArray(), cardIDs = cards.Select(c => c.GetInstanceID()).ToArray(), buyIDs = cards.Select(c => c.buyButton.GetInstanceID()).ToArray() });
        scroll = tabs.chapters.GetComponentInChildren<ScrollRect>(true);
        Require(label + " uses real clipped vertical viewport", scroll != null && scroll.isActiveAndEnabled && scroll.vertical
            && scroll.viewport != null && scroll.content != null && scroll.viewport.GetComponent<RectMask2D>() != null
            && cards.All(c => c.transform.IsChildOf(scroll.content)), new { scroll = scroll != null ? scroll.name : "null" });
    }
    static void ScrollTo(int chapter)
    {
        var card = cards.Single(c => c.chapter == chapter);
        Canvas.ForceUpdateCanvases(); scroll.content.GetComponent<EquipmentCardGrid>()?.RefreshLayout();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content); Canvas.ForceUpdateCanvases();
        var corners = new Vector3[4]; ((RectTransform)card.transform).GetWorldCorners(corners);
        float center = scroll.content.InverseTransformPoint((corners[0] + corners[2]) * .5f).y;
        float hidden = scroll.content.rect.height - scroll.viewport.rect.height;
        scroll.StopMovement(); scroll.verticalNormalizedPosition = hidden > 0 ? 1 - Mathf.Clamp01((scroll.content.rect.yMax - center - scroll.viewport.rect.height * .5f) / hidden) : 1;
    }
    static void Visible(int chapter)
    {
        var card = cards.Single(c => c.chapter == chapter);
        var corners = new Vector3[4]; ((RectTransform)card.transform).GetWorldCorners(corners);
        var local = corners.Select(p => scroll.viewport.InverseTransformPoint(p)).ToArray(); var rect = scroll.viewport.rect;
        bool contained = local.All(p => p.x >= rect.xMin - 2 && p.x <= rect.xMax + 2 && p.y >= rect.yMin - 2 && p.y <= rect.yMax + 2);
        Require("Chapter " + chapter + " entire card is inside portrait viewport", card.gameObject.activeInHierarchy && contained,
            new { min = new[] { local.Min(p => p.x), local.Min(p => p.y) }, max = new[] { local.Max(p => p.x), local.Max(p => p.y) }, viewport = new[] { rect.xMin, rect.yMin, rect.xMax, rect.yMax }, scroll.verticalNormalizedPosition });
        var buttonRect = (RectTransform)card.buyButton.transform; var uiCanvas = buttonRect.GetComponentInParent<Canvas>();
        var screen = RectTransformUtility.WorldToScreenPoint(uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera, buttonRect.TransformPoint(buttonRect.rect.center));
        var hits = new List<RaycastResult>(); var eventSystem = EventSystem.current;
        if (eventSystem != null) eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = screen }, hits);
        Require("Chapter " + chapter + " buy button is topmost raycast target", hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == card.buyButton,
            new { screen = new[] { screen.x, screen.y }, hits = hits.Take(5).Select(h => h.gameObject.name).ToArray() });
    }
    static float Multiplier(UpgradeStatManager.UpgradeType kind)
    {
        var stats = UpgradeStatManager.S;
        float unmultiplied = 100 * (1 + stats.GetPercentStat(kind) / 100) + stats.GetFlatStat(kind);
        if (Mathf.Abs(unmultiplied) < .001f) throw new InvalidOperationException("Cannot normalize zero stat baseline.");
        return stats.ApplyToBase(kind, 100) / unmultiplied;
    }
    static void Purchase(int chapter, int expectedCost)
    {
        var card = cards.Single(c => c.chapter == chapter);
        var row = ChapterUpgradeService.Catalog.entries.Single(r => r.chapter == chapter);
        Require("Chapter " + chapter + " offers rank 1 at correct cost and bonuses", ChapterUpgradeService.Level(chapter) == 0
            && row.CostAtLevel(0) == expectedCost && Mathf.Approximately(row.attackPercent, 5) && Mathf.Approximately(row.healthPercent, 5)
            && card.priceLabel != null && card.priceLabel.text == expectedCost.ToString("N0"), new { chapter, row.coinCost, row.attackPercent, row.healthPercent, priceText = card.priceLabel != null ? card.priceLabel.text : "null" });
        beforeCoins = MoneyScript.S.Coin; beforeAttack = Multiplier(UpgradeStatManager.UpgradeType.ATT); beforeHealth = Multiplier(UpgradeStatManager.UpgradeType.HP);
        Click(card.buyButton, "Chapter " + chapter + " actual buy");
        Require("Chapter " + chapter + " single callback performs one saved rank and one debit", MoneyScript.S.Coin == beforeCoins - expectedCost
            && PlayerPrefs.GetInt("coin", -1) == MoneyScript.S.Coin && ChapterUpgradeService.Level(chapter) == 1
            && PlayerPrefs.GetInt(ChapterUpgradeService.LevelKey(chapter), -1) == 1 && MoneyScript.S.Jewel == initialJewels,
            new { beforeCoins, afterCoins = MoneyScript.S.Coin, expectedCost, savedRank = PlayerPrefs.GetInt(ChapterUpgradeService.LevelKey(chapter), -1), jewels = MoneyScript.S.Jewel });
        Require("Chapter " + chapter + " purchase applies +5 percent attack and health", Mathf.Abs(Multiplier(UpgradeStatManager.UpgradeType.ATT) - beforeAttack - .05f) < .0001f
            && Mathf.Abs(Multiplier(UpgradeStatManager.UpgradeType.HP) - beforeHealth - .05f) < .0001f,
            new { beforeAttack, afterAttack = Multiplier(UpgradeStatManager.UpgradeType.ATT), beforeHealth, afterHealth = Multiplier(UpgradeStatManager.UpgradeType.HP) });
    }
    static void Refreshed(int chapter, int nextCost)
    {
        var card = cards.Single(c => c.chapter == chapter);
        Require("Chapter " + chapter + " UI refresh shows saved rank and next price", ChapterUpgradeService.Level(chapter) == 1
            && card.rankLabel != null && card.rankLabel.text.Contains("1 / 5") && card.priceLabel.text == nextCost.ToString("N0"),
            new { rankText = card.rankLabel != null ? card.rankLabel.text : "null", price = card.priceLabel != null ? card.priceLabel.text : "null", savedRank = PlayerPrefs.GetInt(ChapterUpgradeService.LevelKey(chapter), -1) });
    }
    static void Capture(string label)
    { string path = Path.Combine(folder, images.Count.ToString("D2") + "-" + label + ".png"); images.Add(path); ScreenCapture.CaptureScreenshot(path); }
    static void OnLog(string message, string stack, LogType kind)
    { if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) { errors.Add(kind + ": " + message); File.AppendAllText(Path.Combine(folder, "errors.txt"), message + "\n" + stack + "\n"); } }
    static void Status() => Write("status.json", new { active, phase, state, failures, errors = errors.Count, seconds = EditorApplication.timeSinceStartup - began, folder });
    public static object Poll() { if (folder != null) Status(); return new { active, phase, state, failures, errors = errors.Count, folder }; }
    static void Tick()
    {
        if (!active) return;
        try
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play mode ended before fixture completed.");
            if (EditorApplication.timeSinceStartup - began > 60) throw new TimeoutException("Workshop fixture exceeded 60 seconds.");
            if (EditorApplication.isPaused || EditorApplication.isCompiling || frame == Time.frameCount || Age < .7) return;
            frame = Time.frameCount;
            switch (state)
            {
                case 0:
                    Require("Actual lobby button opened workshop", tabs.gameObject.activeInHierarchy, new { tabs.gameObject.activeInHierarchy });
                    Click(tabs.chapterTab, "Chapter tab"); Next(1, "bind native cards"); break;
                case 1: Roster("Initial chapter tab"); ScrollTo(4); Next(2, "chapter4 purchase"); break;
                case 2: Visible(4); Purchase(4, 25000); Next(3, "chapter4 refreshed capture"); break;
                case 3: Refreshed(4, 50000); Capture("chapter4-rank1"); Next(12, "flush chapter4 capture"); break;
                case 12: ScrollTo(5); Next(4, "chapter5 purchase"); break;
                case 4: Visible(5); Purchase(5, 45000); Next(5, "chapter5 refreshed capture"); break;
                case 5: Refreshed(5, 90000); Capture("chapter5-rank1"); Next(6, "close real workshop"); break;
                case 6: Click(back, "Workshop Back"); Next(7, "reopen workshop"); break;
                case 7:
                    Require("Back closes workshop to living lobby", !tabs.gameObject.activeInHierarchy && !TimeManager.isGameRunning && !CanvasScript.isGameOver, new { workshopActive = tabs.gameObject.activeInHierarchy, TimeManager.isGameRunning });
                    Click(open, "Lobby workshop reopen"); Next(8, "reopen chapter tab"); break;
                case 8: Click(tabs.chapterTab, "Chapter tab after reopen"); Next(9, "verify persisted refresh and roster"); break;
                case 9:
                    Roster("Reopened chapter tab"); Refreshed(4, 50000); Refreshed(5, 90000);
                    Require("Only requested purchases changed wallet", MoneyScript.S.Coin == 30000 && MoneyScript.S.Jewel == initialJewels, new { MoneyScript.S.Coin, MoneyScript.S.Jewel });
                    ScrollTo(5); Next(10, "reopened portrait receipt"); break;
                case 10: Visible(5); Capture("reopened-chapter5-rank1"); Next(11, "flush capture"); break;
                case 11:
                    Require("All native portrait captures reached disk", images.All(p => File.Exists(p) && new FileInfo(p).Length > 1000), new { images = images.ToArray() });
                    Require("No native errors during transaction fixture", errors.Count == 0, new { errors = errors.ToArray() });
                    Click(back, "Final workshop Back"); Finish(null); break;
            }
        }
        catch (Exception e) { if (failures == 0) failures++; Finish(e.ToString()); }
    }
    static void Finish(string exception)
    {
        active = false; EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog; phase = exception == null ? "complete" : "failed";
        if (overlay != null && overlayVisible != null) overlayVisible.SetValue(overlay, overlayWasVisible);
        Write("summary.json", new { passed = failures == 0 && errors.Count == 0 && exception == null, checks = checks.Count, failures,
            errors = errors.ToArray(), exception, ordinaryGameplay = false, fixture = "seeded actual workshop UI callbacks and save transactions",
            images = images.ToArray(), seconds = EditorApplication.timeSinceStartup - began, restorePending = true,
            next = "Exit Play; Chapters45Playtest.Restore; confirm restored.json mismatches=0; independently review PNGs." }); Status();
    }
}
