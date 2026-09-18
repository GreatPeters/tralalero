using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HarborCharacterPresentationRepair
{
    // Reviewed in the calm idle, walk and attack poses. The mesh's long axis is
    // local X; rotating at the hand preserves the existing grip/socket hierarchy.
    public static readonly Vector3 ShovelPosition=new Vector3(.258189f,.1860821f,-.1792898f);
    public static readonly Quaternion ShovelRotation=new Quaternion(.4524445f,.2758887f,.21994549f,.819025934f);
    public static object ApplyShovelGrip()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        if(setup.Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty))throw new InvalidOperationException("Save open scenes first");
        int count=0;
        void Apply(GameObject root) {
            foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.name=="Sifter_Shovel_Illustr_0815073251_texture")) {
                var t=mesh.transform;t.localPosition=ShovelPosition;t.localRotation=ShovelRotation;t.localScale=Vector3.one*63;
                EditorUtility.SetDirty(t);if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);count++;
            }
        }
        string path="Assets/JH/Model/Prefab/Enemy_OldMan.prefab";
        string backup="tmp/backups/feedback-2026-09-19/Enemy_OldMan.prefab";
        if(!File.Exists(backup))File.Copy(path,backup);
        var prefab=PrefabUtility.LoadPrefabContents(path);
        try{Apply(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        try {
            foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}) {
                var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
                foreach(var root in scene.GetRootGameObjects())Apply(root);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
        }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return new{shovels=count};
    }
}
