using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class Sr18LateRampEncounterFix
{
    public static object Main()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="Noryangjin_MapTool_Mode_SR18"||scene.isDirty)throw new InvalidOperationException("Clean SR18 Edit Mode required");
        const string backup="tmp/backups/sr18-release-20260910/before-late-ramp-encounter.unity";
        if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        var enemy=map.Find("Enemies").GetComponentsInChildren<EnemyEventController>(true).Single(e=>e.name.StartsWith("SR18_L_E23_"));
        var spot=map.Find("Props").GetComponentsInChildren<EnemyEventActivationSpot>(true).Single(s=>s.Targets.Contains(enemy));
        var before=enemy.transform.position;var after=new Vector3(260,before.y,136);
        Undo.RecordObjects(new UnityEngine.Object[]{enemy,enemy.transform,spot.transform},"Leave firing room after final descent");
        enemy.transform.position=after;enemy.RefreshPlacementAfterAuthoringChange(before,false);
        spot.transform.position=new Vector3(278,before.y,136);
        foreach(var o in new UnityEngine.Object[]{enemy,enemy.transform,spot.transform}){EditorUtility.SetDirty(o);PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return new{before=before.ToString(),after=after.ToString(),flatApproach=281.75f-after.x};
    }
}
