using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;
using Object = UnityEngine.Object;

// User request 2026-10-02: the opening's fourth scene goes back to the original
// (Curse_Opening_Animated.mp4 + Story_04.png) instead of the S22 native market-arrival
// render. Chapter-entry movies (highway / rest stop) are left as they are.
public static class RestoreOriginalOpeningScene4
{
    const string Movie = "Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4";
    const string Page = "Assets/JH/UI/Opening/Animated/Story_04.png";
    static readonly string[] Scenes = { "Noryangjin_MapTool_Mode_SR18_Revamp", "HighWay", "RestStop" };

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        var movie = AssetDatabase.LoadAssetAtPath<VideoClip>(Movie); var page = AssetDatabase.LoadAssetAtPath<Texture2D>(Page);
        if (movie == null || page == null) throw new Exception("Original opening assets missing");
        var rows = new List<string>(); var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var name in Scenes)
            {
                var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
                foreach (var story in Object.FindObjectsByType<OpeningStoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    string before = AssetDatabase.GetAssetPath(story.movie) + " | " + (story.pages != null && story.pages.Length == 4 ? AssetDatabase.GetAssetPath(story.pages[3]) : "-");
                    story.movie = movie; if (story.pages != null && story.pages.Length == 4) story.pages[3] = page;
                    EditorUtility.SetDirty(story); rows.Add(name + ": " + before + " -> " + Movie + " | " + Page);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        Directory.CreateDirectory("outputs/tralalero-reference-2026-10-01");
        File.WriteAllLines("outputs/tralalero-reference-2026-10-01/opening-scene4-restore.txt", rows);
        return new { rows, movieFrames = movie.frameCount, movieSeconds = movie.length, page = page.width + "x" + page.height };
    }
}
