using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class SetEnemyRadius055
{
    static readonly string[] Chapters={"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"};
    static readonly List<string> changes=new();
    static string backup;
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        backup="tmp/enemy-radius-055-2026-09-21/"+DateTime.UtcNow.ToString("HHmmss");Directory.CreateDirectory(backup);
        changes.Clear();var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach(string chapter in Chapters)
        {
            string path="Assets/ShooterSurvival/Scenes/Tools/"+chapter+".unity";
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                CopyBefore(path);
                if(scene.isDirty)EditorSceneManager.SaveScene(scene,backup+"/"+chapter+"-unsaved.unity",true);
                int count=0;
                foreach(var root in scene.GetRootGameObjects())count+=Apply(root,path);
                if(count>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                changes.Add($"SCENE {chapter}: {count} changed");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/JH/Model/Prefab","Assets/ShooterSurvival/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!Capsules(asset).Any(c=>Mathf.Approximately(c.radius,.43f)))continue;
            CopyBefore(path);var root=PrefabUtility.LoadPrefabContents(path);
            try{if(Apply(root,path)>0)PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);
        File.WriteAllLines(backup+"/changes.txt",changes);
        return string.Join("\n",changes.Where(s=>s.StartsWith("SCENE")))+"\nChanged capsules: "+changes.Count(s=>s.StartsWith("CAPSULE"))+"\nEvidence: "+backup;
    }
    static IEnumerable<CapsuleCollider> Capsules(GameObject root)=>root.GetComponentsInChildren<CapsuleCollider>(true)
        .Where(c=>c.GetComponent<EnemyScript_space>()!=null||c.GetComponent<EnemyScript>()!=null);
    static int Apply(GameObject root,string asset)
    {
        int count=0;
        foreach(var capsule in Capsules(root))
        {
            if(!Mathf.Approximately(capsule.radius,.43f))continue;
            float height=capsule.height;Vector3 center=capsule.center;
            capsule.radius=.55f;Record(capsule);
            foreach(var tuning in capsule.GetComponents<EnemyHitboxSize>())
            {
                var data=new SerializedObject(tuning);
                if(data.FindProperty("body").objectReferenceValue!=capsule||!data.FindProperty("captured").boolValue)continue;
                // The existing initializer expands its stored baseline by 30%.
                // Store the matching baseline so Awake/rebuild cannot restore .43.
                data.FindProperty("originalRadius").floatValue=.55f/1.3f;
                data.ApplyModifiedPropertiesWithoutUndo();Record(tuning);
                typeof(EnemyHitboxSize).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tuning,null);
                tuning.Configure(capsule);tuning.Configure(capsule);
            }
            if(!Mathf.Approximately(capsule.radius,.55f)||capsule.height!=height||capsule.center!=center)throw new InvalidOperationException("Collider preservation failed: "+capsule.name);
            changes.Add("CAPSULE "+asset+" :: "+capsule.name+" 0.43 -> 0.55 (initializer/repeated Configure verified)");count++;
        }
        return count;
    }
    static void Record(UnityEngine.Object target){EditorUtility.SetDirty(target);if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
    static void CopyBefore(string path){string target=backup+"/"+path;Directory.CreateDirectory(Path.GetDirectoryName(target));if(!File.Exists(target))File.Copy(path,target);}
}
