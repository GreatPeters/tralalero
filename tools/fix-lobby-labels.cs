using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Presentation-only repair for the two new chapter scenes. Run in idle Edit Mode.
// Keeps the inherited object/binding intact; disables only its obsolete TMP label.
public static class FixChapters45LobbyLabels
{
    const string SceneRoot = "Assets/ShooterSurvival/Scenes/Tools/";
    const string Output = "outputs/chapters45-2026-10-02";
    const string TitlePath = "UI/Main/Center/ChapterTitle";
    const string InheritedText = "\uB178\uB7C9\uC9C4 \uC218\uC0B0\uC2DC\uC7A5";

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Idle Edit Mode required.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Preserve dirty scenes before repairing lobby labels.");

        var setup = EditorSceneManager.GetSceneManagerSetup();
        var report = new List<string>();
        Scene owned = default;
        try
        {
            // Preflight BOTH scenes before writing either one.
            foreach (string name in new[] { "Jamsil", "ShoeTower" })
            {
                owned = EditorSceneManager.OpenScene(SceneRoot + name + ".unity", OpenSceneMode.Single);
                FindExactLabel(owned, name, out _);
            }
            string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
            foreach (string name in new[] { "Jamsil", "ShoeTower" })
            {
                string path = SceneRoot + name + ".unity";
                owned = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var label = FindExactLabel(owned, name, out var canvas);
                string before = UIState(canvas, label);
                bool changed = label.enabled || label.raycastTarget;
                if (changed)
                {
                    Directory.CreateDirectory(Output);
                    string backup = Output + "/" + name + "-before-lobby-label-fix-" + stamp + ".unity";
                    File.Copy(path, backup, false);
                    label.enabled = false;
                    label.raycastTarget = false;
                    EditorUtility.SetDirty(label);
                    if (UIState(canvas, label) != before)
                        throw new InvalidOperationException(name + ": unrelated UI changed; refusing to save.");
                    if (label.text != InheritedText || label.enabled || label.raycastTarget)
                        throw new InvalidOperationException(name + ": duplicate label suppression failed.");
                    EditorSceneManager.MarkSceneDirty(owned);
                    if (!EditorSceneManager.SaveScene(owned))
                        throw new IOException("Could not save " + path);
                    report.Add(name + ": disabled only " + TitlePath + "/Name TMP; backup " + backup);
                }
                else report.Add(name + ": exact inherited TMP already disabled; no save needed.");
            }
            return string.Join("\n", report);
        }
        finally
        {
            // Discard only an unsaved scene opened by this tool if a guard failed.
            if (owned.IsValid() && owned.isLoaded && owned.isDirty)
                EditorSceneManager.CloseScene(owned, true);
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    static TextMeshProUGUI FindExactLabel(Scene scene, string name, out CanvasScript canvas)
    {
        canvas = Object.FindObjectsByType<CanvasScript>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(c => c.gameObject.scene == scene);
        var title = canvas.transform.Find(TitlePath);
        if (title == null) throw new InvalidOperationException(name + ": chapter title root missing.");
        var current = title.GetComponent<TextMeshProUGUI>();
        string expected = name == "Jamsil"
            ? "04  \uC7A0\uC2E4\n\uD558\uB298\uC758 \uC2E0\uBC1C\uC744 \uD5A5\uD574"
            : "05  \uC288 \uD0C0\uC6CC\n\uAC00\uC7A5 \uB192\uC740 \uACF3\uC73C\uB85C";
        if (current == null || current.text != expected || !current.enabled)
            throw new InvalidOperationException(name + ": expected new chapter title is missing or changed.");
        var inherited = title.Find("Name");
        var label = inherited == null ? null : inherited.GetComponent<TextMeshProUGUI>();
        if (label == null || label.text != InheritedText)
            throw new InvalidOperationException(name + ": exact inherited market label not found; refusing a broad match.");
        return label;
    }

    static string UIState(CanvasScript canvas, TextMeshProUGUI excluded)
    {
        // All remaining canvas components, object names, active states and hierarchy
        // must survive unchanged, including the current title, hint and button bindings.
        return string.Join("\n", canvas.GetComponentsInChildren<Component>(true)
            .Where(c => c != null && c != excluded)
            .OrderBy(c => c.GetInstanceID())
            .Select(c => c.GetInstanceID() + "|" + c.gameObject.name + "|" + c.gameObject.activeSelf
                + "|" + EditorJsonUtility.ToJson(c)));
    }
}
