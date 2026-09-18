using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CaptureMobilePresentation
{
    public static object Main(string folder)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required");
        Directory.CreateDirectory(folder);
        var root = SceneManager.GetActiveScene().GetRootGameObjects().Single(g => g.name == "Canvas");
        var canvas = root.GetComponent<CanvasScript>();
        var story = root.GetComponentInChildren<OpeningStoryUI>(true);
        var shop = root.GetComponentInChildren<CosmeticShopUI>(true);
        foreach (var harness in UnityEngine.Object.FindObjectsByType<CombatHarness>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            typeof(CombatHarness).GetField("overlayVisible", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(harness, false);
        var steps = new (string name, Action action)[] {
            ("story", () => story.Open()),
            ("lobby", () => story.Skip()),
            ("upgrades", () => root.transform.Find("UI/Main/Bottom/Upgrade_Button").GetComponent<Button>().onClick.Invoke()),
            ("lateral-upgrade", () => root.transform.Find("UI/Upgrade2/UpgradeViewport").GetComponent<ScrollRect>().verticalNormalizedPosition = 0),
            ("cosmetics", () => {root.transform.Find("UI/Upgrade2/MobileUpgradeHeader/Back").GetComponent<Button>().onClick.Invoke(); shop.Open();}),
            ("lobby-settings", () => {shop.Close(); root.transform.Find("UI/Setting").gameObject.SetActive(true);}),
            ("pause", () => {root.transform.Find("UI/Setting").gameObject.SetActive(false); canvas.PauseGame();}),
            ("settings", () => canvas.SettingsMenu()),
            ("defeat", () => {canvas.settingsMenuUI.SetActive(false); canvas.gameOverUI.SetActive(true);}),
            ("victory", () => {canvas.gameOverUI.SetActive(false); canvas.youWinUI.SetActive(true);})
        };
        int stage = 0; bool capture = false;
        double due = EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction tick = null;
        tick = () => {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; return; }
            if (EditorApplication.timeSinceStartup < due) return;
            try {
                if (!capture) { steps[stage].action(); capture = true; due = EditorApplication.timeSinceStartup + 1.8; }
                else {
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder, steps[stage].name + ".png"));
                    File.WriteAllText(Path.Combine(folder, "progress.txt"), steps[stage].name);
                    stage++; capture = false; due = EditorApplication.timeSinceStartup + .6;
                    if (stage == steps.Length) { EditorApplication.update -= tick; File.WriteAllText(Path.Combine(folder, "complete.txt"), DateTime.UtcNow.ToString("O")); }
                }
            } catch (Exception ex) { EditorApplication.update -= tick; File.WriteAllText(Path.Combine(folder, "error.txt"), ex.ToString()); }
        };
        EditorApplication.update += tick;
        return new { scheduled = steps.Length, folder };
    }
}
