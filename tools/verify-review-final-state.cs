using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class VerifyReviewFinalState
{
    public static string Main()
    {
        const string folder="tmp/review-fixes-20-runs-2026-09-22";
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        ChapterPlaytestPreferences.RestoreAt(folder+"/before-prefs.tsv");
        foreach(var line in File.ReadAllLines(folder+"/extra-prefs.tsv")){var p=line.Split('\t');if(bool.Parse(p[1]))PlayerPrefs.SetInt(p[0],int.Parse(p[2]));else PlayerPrefs.DeleteKey(p[0]);}PlayerPrefs.Save();
        int keys=0;
        foreach(var line in File.ReadAllLines(folder+"/before-prefs.tsv"))
        {
            var p=line.Split('\t');bool exists=bool.Parse(p[2]);if(PlayerPrefs.HasKey(p[0])!=exists)throw new Exception("Key presence changed: "+p[0]);
            if(exists)
            {
                string value=p[1]=="int"?PlayerPrefs.GetInt(p[0]).ToString(CultureInfo.InvariantCulture):p[1]=="float"?PlayerPrefs.GetFloat(p[0]).ToString("R",CultureInfo.InvariantCulture):Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(p[0])));
                if(value!=p[3])throw new Exception("Key value changed: "+p[0]);
            }
            keys++;
        }
        if(PlayerPrefs.HasKey("TutorialDone"))throw new Exception("Tutorial absence not restored");
        var report=new List<string>{$"Verified preferences={keys}, TutorialDone absent, coin={PlayerPrefs.GetInt("coin")}, jewel={PlayerPrefs.GetInt("jewel")}, speed={SessionState.GetFloat("NoryangjinMapTool.TestTimeScale",1)}"};
        var status=GameDataWorkbookEditor.GetRuntimeArchiveStatus(out string detail);if(status!=GameDataRuntimeArchiveStatus.Current)throw new Exception(detail);report.Add("Runtime archive: "+status);
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        try
        {
            foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
                var enemies=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<EnemyScript_space>(true)).ToArray();
                var camera=Camera.main;var occlusion=camera.GetComponent<NoryangjinCameraOcclusion>();var groups=new SerializedObject(occlusion).FindProperty("additionalOccluderGroups");
                for(int i=0;i<groups.arraySize;i++)if(groups.GetArrayElementAtIndex(i).objectReferenceValue==null)throw new Exception(name+" null occluder");
                var defeat=UnityEngine.Object.FindFirstObjectByType<DefeatPresentation>(FindObjectsInactive.Include);
                if(defeat.causeText==null||defeat.adviceText==null||defeat.buildText==null)throw new Exception(name+" missing defeat binding");
                report.Add(name+": enemies="+enemies.Length+", radii="+string.Join(",",enemies.Select(e=>e.GetComponent<CapsuleCollider>()).Where(c=>c!=null).GroupBy(c=>c.radius).Select(g=>$"{g.Key}:{g.Count()}"))+", occluderGroups="+groups.arraySize+", defeat bindings valid");
            }
        }
        finally{EditorSceneManager.OpenScene(original);}
        report.Add("Editor stopped; SR18 reopened clean; no scene save from playtests.");
        File.WriteAllLines(folder+"/final-state.txt",report);return string.Join("\n",report);
    }
}
