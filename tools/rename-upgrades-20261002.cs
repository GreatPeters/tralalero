using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static class RenameUpgrades20261002
{
    const string Folder="outputs/upgrade-names-2026-10-02";
    static readonly string[] Names={"공격력","체력","공격 속도","미사일 지속","보스 피해","코인 획득","체력 회복","동료 체력","동료 공격력","좌우 속도"};
    static readonly string[] Scenes={"Noryangjin_MapTool_Mode_SR18_Revamp","HighWay","RestStop"};
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});

    public static object Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve unsaved scenes");
        Directory.CreateDirectory(Folder+"/before");var setup=EditorSceneManager.GetSceneManagerSetup();var rows=new List<object>();
        try
        {
            foreach(string name in Scenes)
            {
                string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";
                string backup=Folder+"/before/"+name+".unity";if(!File.Exists(backup))File.Copy(path,backup);
                var scene=EditorSceneManager.OpenScene(path);int changed=0;
                foreach(var card in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UpgradeUI>(true)))
                {
                    int id=card.UpgradeId;if(id<1||id>Names.Length)throw new Exception("Unexpected upgrade id "+id);
                    var data=new SerializedObject(card);var label=data.FindProperty("displayNameOverride");
                    string before=label.stringValue;label.stringValue=Names[id-1];data.ApplyModifiedPropertiesWithoutUndo();
                    var text=data.FindProperty("nameText").objectReferenceValue as TMP_Text;
                    if(text!=null){text.text=Names[id-1];EditorUtility.SetDirty(text);}
                    EditorUtility.SetDirty(card);if(before!=Names[id-1])changed++;
                    rows.Add(new{scene=name,id,before,after=Names[id-1]});
                }
                if(changed>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
            }
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        File.WriteAllText(Folder+"/applied.json",Json(rows));return new{cards=rows.Count,names=Names};
    }
    public static object Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        var setup=EditorSceneManager.GetSceneManagerSetup();int checkedCards=0;
        try{foreach(string name in Scenes){var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");foreach(var card in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UpgradeUI>(true))){var data=new SerializedObject(card);string actual=data.FindProperty("displayNameOverride").stringValue;if(actual!=Names[card.UpgradeId-1])throw new Exception(name+" label mismatch");checkedCards++;}}}
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        File.WriteAllText(Folder+"/verified.txt",$"{checkedCards} saved cards verified across {Scenes.Length} scenes; names only.");return new{checkedCards,scenes=Scenes.Length};
    }
    public static object RestoreUnshippedLegacyScene()
    {
        const string name="Noryangjin_MapTool_Mode_SR18.unity";
        string path="Assets/ShooterSurvival/Scenes/Tools/"+name;
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        if(File.Exists(Folder+"/legacy-restored.txt"))return "Already restored";
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path==path)throw new Exception("Close legacy scene first");
        File.Copy(path,Folder+"/legacy-auto-reserialized.unity",false);
        File.Copy(Folder+"/before/"+name,path,true);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        File.WriteAllText(Folder+"/legacy-restored.txt","Unshipped legacy scene restored byte-for-byte. Opening/saving it introduced unrelated serialization changes; production scope is three scenes.");
        return "Unshipped original scene restored from this task's fresh backup";
    }
}
