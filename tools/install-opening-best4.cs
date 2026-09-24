using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

public static class InstallOpeningBest4
{
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        const string path = "Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4";
        const string folder = "map-concepts/opening-best4-v5-2026-09-23/";
        var guid = AssetDatabase.AssetPathToGUID(path);
        File.Copy(folder + "opening-best4-v5.mp4", path, true);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
        if (clip == null || clip.frameCount != 936 || clip.width != 720 || clip.height != 1280 || Math.Abs(clip.frameRate - 24) > .001)
            throw new InvalidOperationException("Unexpected movie import");
        if (guid != AssetDatabase.AssetPathToGUID(path)) throw new InvalidOperationException("Movie GUID changed");
        var scenes = new[] { "Noryangjin_MapTool_Mode_SR18", "HighWay", "RestStop" };
        foreach (var name in scenes)
        {
            string scenePath = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var story = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<OpeningStoryUI>(true)).Single();
                if (story.movie != clip || story.sceneIndicators.Length != 4) throw new InvalidOperationException("Opening binding mismatch: " + name);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        var result = new { clip.frameCount, clip.frameRate, clip.length, clip.width, clip.height, guid, scenes };
        var json = (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
            .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new object[] { result });
        File.WriteAllText(folder + "import.json", json);
        return result;
    }
}
