using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class InstallRestStopContactPairs
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(original.isDirty)throw new InvalidOperationException("Unsaved scene");string originalPath=original.path;
        const string path="Assets/ShooterSurvival/Scenes/Tools/RestStop.unity";
        const string backup="tmp/campaign-balance-2026-09-23/RestStop-before-contact-pairs.unity";
        if(!File.Exists(backup))File.Copy(path,backup);
        var scene=EditorSceneManager.OpenScene(path);
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var parent=map.Find("EncounterContacts");
        if(parent==null){parent=new GameObject("EncounterContacts").transform;parent.SetParent(map,false);}
        var actors=map.Find("Enemies").GetComponentsInChildren<EnemyScript_space>(true);int patrols=0,pairs=0;
        foreach(var actor in actors)
        {
            var events=actor.GetComponent<EnemyEventController>();
            if(events.EventMode!=EnemyEventMode.PatrolBetweenStartAndTarget||!events.PatrolAcrossRoad||!events.HasUsableTarget)continue;
            var center=(actor.transform.position+events.TargetPoint.position)*.5f;float distance=Vector3.Distance(actor.transform.position,events.TargetPoint.position);
            var forward=Vector3.ProjectOnPlane(actor.transform.forward,Vector3.up).normalized;
            actor.transform.position=center-forward*distance*.5f;events.TargetPoint.position=center+forward*distance*.5f;events.PatrolAcrossRoad=false;
            Record(actor.transform);Record(events.TargetPoint);Record(events);patrols++;
        }
        foreach(var left in actors.Where(e=>!e.name.EndsWith("_Right",StringComparison.Ordinal)))
        {
            var right=actors.FirstOrDefault(e=>e.name==left.name+"_Right");if(right==null)continue;
            Vector3 Center(EnemyScript_space actor)
            {var events=actor.GetComponent<EnemyEventController>();return events.EventMode==EnemyEventMode.PatrolBetweenStartAndTarget&&events.HasUsableTarget?(actor.transform.position+events.TargetPoint.position)*.5f:actor.transform.position;}
            var direction=Vector3.ProjectOnPlane(left.transform.forward,Vector3.up).normalized;
            var center=RestStopChapterBuilder.ProjectRoadCenter((Center(left)+Center(right))*.5f,direction,out var roadForward);
            foreach(var actor in new[]{left,right})
            {
                var delta=center+Vector3.Cross(Vector3.up,direction)*(actor==left?-1.1f:1.1f)-Center(actor);
                actor.transform.position+=delta;Record(actor.transform);
                var events=actor.GetComponent<EnemyEventController>();if(events.HasUsableTarget){events.TargetPoint.position+=delta;Record(events.TargetPoint);}
            }
            var group=parent.Find(left.name+"_ContactChoice");if(group==null){group=new GameObject(left.name+"_ContactChoice").transform;group.SetParent(parent,false);}
            var row=group.GetComponent<HighwayEncounterRow>();if(row==null)row=group.gameObject.AddComponent<HighwayEncounterRow>();row.left=left;row.right=right;
            var rowPosition=center;if(row.barriers!=null)rowPosition.y=group.position.y;
            group.SetPositionAndRotation(rowPosition,Quaternion.LookRotation(roadForward));row.routeDistance=0;
            foreach(var actor in new[]{left,right}){var member=actor.GetComponent<HighwayEncounterMember>();if(member==null)member=actor.gameObject.AddComponent<HighwayEncounterMember>();member.row=row;Record(member);}
            Record(row);pairs++;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        if(!string.IsNullOrEmpty(originalPath)&&originalPath!=path)EditorSceneManager.OpenScene(originalPath);
        return new{pairs,patrols,forcedLaneGeometryAdded=false};
    }
    static void Record(UnityEngine.Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
}
