using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class CheckProductionBaseline
{
    public static object Main()
    {
        const string temporary="Assets/__ProductionBaseline20260925";
        if(Directory.Exists(temporary))throw new Exception("Temporary baseline folder already exists");
        Directory.CreateDirectory(temporary);
        var rows=new System.Collections.Generic.List<object>();
        try{foreach(var name in new[]{"RestStop","HighWay"})
        {
            var path=temporary+"/"+name+".unity";File.Copy("outputs/reststop-scene-integration-2026-09-25/backups/"+name+".before.unity",path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var baseline=Read(EditorSceneManager.OpenScene(path),name);
            var current=Read(EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity"),name);
            if(Json(baseline)!=Json(current))throw new Exception("Baseline differs: "+name);
            rows.Add(new{scene=name,unchanged=true,baseline,current});
        }}finally{EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/HighWay.unity");AssetDatabase.DeleteAsset(temporary);}
        File.WriteAllText("outputs/reststop-scene-integration-2026-09-25/preexisting-test-failures.json",Json(rows));return rows;
    }
    static object Read(Scene scene,string name)
    {
        var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas");var opening=canvas.GetComponentInChildren<OpeningStoryUI>(true);
        var chapter=canvas.GetComponent<ChapterProgression>();var title=canvas.transform.Find("UI/Main/Center/StartHint/Title").GetComponent<TMPro.TMP_Text>();
        var tutorial=canvas.GetComponent<CoastalTutorialUI>().panel.transform.Find("Panel").GetComponent<RectTransform>();
        var cards=canvas.transform.Find("UI/Upgrade2").GetComponentsInChildren<UpgradeUI>(true);
        return new{enabledWorkbookEnemies=EncounterPlacementTables.Rows.Count(r=>r.scene==name&&r.kind=="적 배치"&&r.enabled),nextText=opening.nextText!=null,captionText=opening.captionText!=null,pageProgress=opening.pageProgress!=null,
            clearRewardText=chapter.clearRewardText!=null,outlineWidth=title.fontSharedMaterial.GetFloat("_OutlineWidth"),tutorialHeight=tutorial.anchorMax.y-tutorial.anchorMin.y,
            upgradeIcons=cards.OrderBy(c=>c.UpgradeId).Select(c=>AssetDatabase.GetAssetPath(new SerializedObject(c).FindProperty("iconOverride").objectReferenceValue)).ToArray()};
    }
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
}
