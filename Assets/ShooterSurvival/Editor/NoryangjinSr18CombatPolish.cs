#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NoryangjinSr18CombatPolish
{
    public const string RecordPath = "map-concepts/sr18-combat-polish-2026-09-09";
    public static string Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != NoryangjinSr18LatestEncounters.ScenePath || scene.isDirty)
            throw new InvalidOperationException("Clean SR18 Edit Mode scene required.");
        if (File.Exists(RecordPath + "/applied.json")) throw new InvalidOperationException("Already applied; preserve later edits.");
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        var roads=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
        var props=map.Find("Props");
        var pairs=map.GetComponentsInChildren<BonusWallChoicePair>(true);
        var ambushers=map.GetComponentsInChildren<EnemyEventController>(true).Where(e=>e.EventMode==EnemyEventMode.AmbushMoveThenShoot).ToArray();
        var buckets=props.Cast<Transform>().Where(t=>t.name.StartsWith("SR18_L_G") && t.GetComponent<ObstacleStats>()?.obstaclePattern==ObstaclePattern.Bucket).ToArray();
        if (pairs.Length!=25 || ambushers.Length!=5 || buckets.Length!=9) throw new InvalidOperationException("Unexpected scene baseline");
        float Floor(Vector3 p)
        {
            foreach(var offset in new[]{Vector3.zero,Vector3.left*.12f,Vector3.right*.12f,Vector3.forward*.12f,Vector3.back*.12f})
                foreach(var road in roads)
                    if(road.Raycast(new Ray(p+offset+Vector3.up*3f,Vector3.down),out var hit,6f) && hit.normal.y>.7f) return hit.point.y+.08f;
            throw new InvalidOperationException("No correct road deck: "+p);
        }
        foreach(var pair in pairs)
        {
            Vector3 center=(pair.Left.transform.position+pair.Right.transform.position)*.5f;
            Vector3 lateral=(pair.Right.transform.position-pair.Left.transform.position).normalized;
            foreach(float x in new[]{-2.95f,-1.9f,1.9f,2.95f}) Floor(center+lateral*x);
        }
        foreach(var enemy in ambushers)
        {
            Vector3 center=enemy.TargetPoint.position;
            Vector3 dir=Vector3.ProjectOnPlane(enemy.transform.forward,Vector3.up).normalized;
            Vector3 start=center+dir*8+Vector3.Cross(Vector3.up,dir)*1.6f;
            for(int i=0;i<=16;i++) Floor(Vector3.Lerp(start,center,i/16f));
            enemy.GetComponent<EnemyCornerClearance>().ValidateMovement(start,center);
        }
        foreach(var bucket in buckets)
            foreach(float z in new[]{-2.5f,0f,2.5f}) Floor(bucket.position+bucket.forward*z);
        Directory.CreateDirectory(RecordPath);
        string backup="tmp/backups/sr18-polish-2026-09-09/"+DateTime.Now.ToString("HHmmss")+"/before.unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        if(!EditorSceneManager.SaveScene(scene,backup,true)) throw new IOException("Backup failed");
        Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("SR18 readable combat and prop sizes");
        try
        {
            foreach(var pair in pairs)
            {
                Vector3 center=(pair.Left.transform.position+pair.Right.transform.position)*.5f;
                Vector3 lateral=(pair.Right.transform.position-pair.Left.transform.position).normalized;
                foreach(var altar in new[]{pair.Left,pair.Right})
                {
                    Undo.RecordObject(altar.transform,"Enlarge choice");
                    altar.transform.localScale=new Vector3(2.25f,2.9f,2.9f);
                    altar.transform.position=center+lateral*(altar==pair.Left?-1.9f:1.9f);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(altar.transform);
                }
            }
            foreach(var enemy in ambushers)
            {
                Undo.RecordObjects(new UnityEngine.Object[]{enemy,enemy.transform},"Walk-in ambush");
                enemy.HideWhileWaiting=false; enemy.AmbushEntrySide=1.6f;
                enemy.MoveAnimation=EnemyMoveAnimation.Walk; enemy.MoveSpeed=3f;
                Vector3 dir=Vector3.ProjectOnPlane(enemy.transform.forward,Vector3.up).normalized;
                Vector3 prior=enemy.transform.position;
                enemy.transform.position=enemy.TargetPoint.position+dir*8+Vector3.Cross(Vector3.up,dir)*1.6f;
                enemy.RefreshPlacementAfterAuthoringChange(prior,false);
                var spot=props.GetComponentsInChildren<EnemyEventActivationSpot>(true).Single(s=>s.Targets.Contains(enemy));
                Undo.RecordObject(spot.transform,"Earlier distant walk-in cue");
                Vector3 gate=enemy.GetComponent<EnemyCornerClearance>().ConstrainTrigger(enemy.TargetPoint.position-dir*44);
                gate.y=Floor(gate); spot.transform.position=gate;
                PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
                PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);
            }
            foreach(var source in buckets)
            {
                Vector3 center=source.position; Quaternion rotation=source.rotation; string id=source.name;
                var group=new GameObject(id); Undo.RegisterCreatedObjectUndo(group,"Bucket cluster");
                group.transform.SetParent(props,false); group.transform.SetPositionAndRotation(center,rotation);
                for(int i=0;i<3;i++)
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Obstacle_Real/Bucket.prefab");
                    GameObject part=i==0?source.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(prefab,group.transform);
                    if(i>0) Undo.RegisterCreatedObjectUndo(part,"Bucket cluster part");
                    else { Undo.RecordObject(part,"Rename bucket member"); Undo.SetTransformParent(part.transform,group.transform,"Group buckets"); }
                    part.name="Bucket_"+(i+1); part.SetActive(true);
                    part.transform.rotation=rotation; part.transform.localScale=Vector3.one*70;
                    Vector3 point=center+group.transform.forward*((i-1)*2.5f);
                    point.y=Floor(point); part.transform.position=point;
                    foreach(var c in part.GetComponentsInChildren<Component>(true))
                        if(c!=null) { EditorUtility.SetDirty(c); PrefabUtility.RecordPrefabInstancePropertyModifications(c); }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(part);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
            var record=new {backup,choices=25,altars=50,choiceScale=new[]{2.25f,2.9f,2.9f},choiceOffset=1.9f,ambushers=5,walkDistance=8,walkSpeed=3,activationLead=44,bucketStations=9,buckets=27,bucketScale=70,roads=map.Find("Roads").childCount,props=props.childCount};
            string json=Newtonsoft.Json.JsonConvert.SerializeObject(record,Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(RecordPath+"/applied.json",json);
            Undo.CollapseUndoOperations(undo);
            return json;
        }
        catch { Undo.RevertAllDownToGroup(undo); throw; }
    }
}
#endif
