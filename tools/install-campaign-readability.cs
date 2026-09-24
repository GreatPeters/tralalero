using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static class InstallCampaignReadability
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(original.isDirty)throw new InvalidOperationException("Unsaved scene");
        string originalPath=original.path;
        const string scenePath="Assets/ShooterSurvival/Scenes/Tools/HighWay.unity";
        const string backup="tmp/campaign-balance-2026-09-23/HighWay-before-readability.unity";
        if(!File.Exists(backup))File.Copy(scenePath,backup);
        var scene=EditorSceneManager.OpenScene(scenePath);
        var traffic=UnityEngine.Object.FindFirstObjectByType<HighwayOncomingTraffic>();
        traffic.beats[0].distance=84;EditorUtility.SetDirty(traffic);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        const string path="Assets/ShooterSurvival/Resources/CombatFeedback/ContactWarning.prefab";
        var contents=PrefabUtility.LoadPrefabContents(path);
        try{contents.GetComponent<TMP_Text>().fontSize=3.4f;PrefabUtility.SaveAsPrefabAsset(contents,path);}
        finally{PrefabUtility.UnloadPrefabContents(contents);}
        if(!string.IsNullOrEmpty(originalPath)&&originalPath!=scenePath)EditorSceneManager.OpenScene(originalPath);
        return new{trafficStart=84,contactWarningFont=3.4};
    }
}
