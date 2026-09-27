using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyMapToolStartStage
{
    public static object Refresh()
    {
        string folder = "tmp/maptool-start-stage-2026-09-25";
        Directory.CreateDirectory(folder);
        var scene = SceneManager.GetActiveScene();
        if (!File.Exists(folder + "/authoring-before.unity"))
        {
            if (!EditorSceneManager.SaveScene(scene, folder + "/authoring-before.unity", true)) throw new Exception("Scene snapshot failed");
            File.WriteAllText(folder + "/baseline.json", Json(new { scene.path, scene.isDirty, startScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) }));
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        return new { scene = scene.path, scene.isDirty, compilationRequested = true };
    }
    public static object Assertions()
    {
        // Invoke only this fixture in the live editor. Avoid Test Runner's
        // SaveModifiedSceneTask touching the user's already-dirty authoring scene.
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("NoryangjinMapToolTestStartStageTests")).First(t => t != null);
        var cases = new (string method, object[] args)[] {
            ("SelectionChangesPlayStartWithoutOpeningOrDirtyingTheAuthoringScene", new object[] { 1, "Noryangjin_MapTool_Mode_SR18" }),
            ("SelectionChangesPlayStartWithoutOpeningOrDirtyingTheAuthoringScene", new object[] { 2, "HighWay" }),
            ("SelectionChangesPlayStartWithoutOpeningOrDirtyingTheAuthoringScene", new object[] { 3, "RestStop" }),
            ("SwitchingStagesThenDataRestoresAnExistingCustomStartScene", Array.Empty<object>()),
            ("DataRestoresTheOrdinaryOpenSceneDefault", Array.Empty<object>()),
            ("InvalidSelectionDoesNotChangeTheStartScene", new object[] { -1 }),
            ("InvalidSelectionDoesNotChangeTheStartScene", new object[] { 4 }),
            ("DataDoesNotOverwriteAnotherToolsSubsequentChoice", Array.Empty<object>()) };
        int passed = 0;
        foreach (var test in cases)
        {
            var instance = Activator.CreateInstance(type);
            try { type.GetMethod("SaveSession").Invoke(instance, null); type.GetMethod(test.method).Invoke(instance, test.args); passed++; }
            finally { type.GetMethod("RestoreSession").Invoke(instance, null); }
        }
        var scene = SceneManager.GetActiveScene();
        var result = new { passed, total = cases.Length, mode = "native assertions; no Test Runner scene switch", scene = scene.path, scene.isDirty, startScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) };
        File.WriteAllText("tmp/maptool-start-stage-2026-09-25/assertions.json", Json(result));
        return result;
    }
    public static object Show()
    {
        var type = typeof(NoryangjinMapToolWindow);
        var window = EditorWindow.GetWindow(type);
        var field = type.GetField("selectedTab", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(window, Enum.ToObject(field.FieldType, 1)); window.Show(); window.Repaint();
        return new { window = window.titleContent.text, tab = "convenience" };
    }
    public static object Select(int stage)
    {
        var type = typeof(NoryangjinMapToolWindow).Assembly.GetType("NoryangjinMapToolTestStartStage", true);
        type.GetMethod("SelectStage", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { stage });
        return new { stage, nextPlayScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene), authoringScene = SceneManager.GetActiveScene().path };
    }
    public static object OpeningAssertions()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MapToolOpeningVideoTests")).First(t => t != null);
        var instance = Activator.CreateInstance(type);
        var scene = SceneManager.GetActiveScene(); bool dirty = scene.isDirty;
        var method = type.GetMethod("OpeningToggleSkipsOnlyAutomaticPlayback");
        foreach (var values in new[] {
            new object[] { false, true, false, true }, new object[] { true, true, false, false },
            new object[] { false, false, false, false }, new object[] { false, true, true, false } }) method.Invoke(instance, values);
        type.GetMethod("OffDoesNotHideAnAuthoredNonAutomaticStory").Invoke(instance, null);
        if (SceneManager.GetActiveScene() != scene || scene.isDirty != dirty) throw new Exception("Authoring scene changed");
        var result = new { passed = 5, total = 5, mode = "native assertions; no Test Runner scene switch", scene = scene.path, scene.isDirty };
        Directory.CreateDirectory("tmp/maptool-start-stage-2026-09-25");
        File.WriteAllText("tmp/maptool-start-stage-2026-09-25/opening-assertions.json", Json(result));
        return result;
    }
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
}
