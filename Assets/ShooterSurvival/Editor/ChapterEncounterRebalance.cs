using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit authoring pass. The workbook remains the runtime source for combat values.
public static class ChapterEncounterRebalance
{
    private const string Output="map-concepts/skins-reststop-2026-09-12";
    private static Transform map, enemies, props, bonuses, targets;
    private static MeshCollider[] roads;
    private static readonly List<object> copies=new(),gimmicks=new(),placements=new();
    public static object BuildOpenScene()
    {
        var scene=SceneManager.GetActiveScene();
        bool highway=scene.name=="HighWay";
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||(!highway&&scene.path!=NoryangjinMapToolWindow.Sr18MapToolScenePath))throw new InvalidOperationException("Clean chapter scene in Edit Mode required.");
        string report=Output+"/encounter-layout-"+scene.name+".json";
        if(File.Exists(report))throw new InvalidOperationException("Layout already authored; refine the recorded result.");
        map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        enemies=map.Find("Enemies");props=map.Find("Props");bonuses=map.Find("Bonuses");targets=map.Find(highway?"Highway_EnemyTargets":"SR18_EnemyTargets");roads=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
        copies.Clear();gimmicks.Clear();placements.Clear();Physics.SyncTransforms();
        string backup="tmp/backups/skins-progression-2026-09-12/before-encounters-"+scene.name+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new IOException("Scene backup failed.");
        try
        {
            if(highway)RebuildHighway();else PairNoryangjin();
            foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Component>(true))
                if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            Physics.SyncTransforms();
            int enemyCount=enemies.GetComponentsInChildren<EnemyEventController>(true).Length;
            int pairCount=bonuses.GetComponentsInChildren<BonusWallChoicePair>(true).Length;
            if(enemyCount!=50||pairCount!=25)throw new InvalidOperationException($"Unexpected composition {enemyCount}/{pairCount}");
            var result=new{scene=scene.name,backup,enemyCount,pairCount,gimmickStations=25,copies,gimmicks,placements};
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed.");
            File.WriteAllText(report,JsonConvert.SerializeObject(result,Formatting.Indented));return result;
        }
        catch{EditorSceneManager.OpenScene(scene.path);throw;}
    }
    private static void PairNoryangjin()
    {
        var originals=enemies.GetComponentsInChildren<EnemyEventController>(true).Where(e=>!e.name.EndsWith("_Right",StringComparison.Ordinal)).OrderBy(e=>e.name).ToArray();
        if(originals.Length!=25)throw new InvalidOperationException("Expected25 existing Noryangjin encounter leaders.");
        var settings=EncounterPlacementTables.Rows.Where(r=>r.scene==map.gameObject.scene.name&&r.kind=="적 배치").ToDictionary(r=>r.id);
        foreach(var source in originals)
        {
            var row=settings[source.name];var direction=Vector3.ProjectOnPlane(source.transform.forward,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,direction);
            Vector3 center=source.EventMode==EnemyEventMode.PatrolBetweenStartAndTarget?(source.transform.position+source.TargetPoint.position)*.5f:source.EventMode==EnemyEventMode.AmbushMoveThenShoot?source.TargetPoint.position:source.transform.position;
            var clearance=source.GetComponent<EnemyCornerClearance>();
            if(clearance!=null){var projected=clearance.Corner+direction*Vector3.Dot(center-clearance.Corner,direction);projected.y=center.y;center=projected;}
            var gate=props.GetComponentsInChildren<EnemyEventActivationSpot>(true).Single(s=>s.Targets.Length==1&&s.Targets[0]==source);
            var existing=enemies.Find(source.name+"_Right");var companion=existing!=null?existing.GetComponent<EnemyEventController>():Clone(source.gameObject,enemies,source.name+"_Right").GetComponent<EnemyEventController>();
            if(existing==null)
            {
                if(source.HasUsableTarget){var target=new GameObject(companion.name+"_Target").transform;target.SetParent(targets,false);companion.TargetPoint=target;}
                Clone(gate.gameObject,props,gate.name+"_Right").GetComponent<EnemyEventActivationSpot>().Targets=new[]{companion};
                copies.Add(new{id=companion.name,sourceId=source.name});
            }
            foreach(var actor in new[]{source,companion})
            {
                var lane=center+right*(actor==source?-1.1f:1.1f);var start=lane;var end=lane;
                if(row.mode==EnemyEventMode.PatrolBetweenStartAndTarget){var axis=actor.PatrolAcrossRoad?right:direction;start-=axis*row.moveDistance*.5f;end+=axis*row.moveDistance*.5f;}
                if(row.mode==EnemyEventMode.AmbushMoveThenShoot){actor.AmbushEntrySide=0;start+=direction*row.moveDistance;}
                MoveEnemy(actor,start);if(actor.HasUsableTarget){end.y=Floor(end);actor.TargetPoint.position=end;}
                placements.Add(new{id=actor.name,kind="enemy",move=row.moveDistance,lead=row.activationLead});
            }
        }
        ReplaceNoryGimmick(8,"Oldman");ReplaceNoryGimmick(17,"Dolphin");
        var ambient=props.Find("SR18_Polish_Ship_1");UnityEngine.Object.DestroyImmediate(ambient.GetComponent<ObstacleStats>());
        gimmicks.Add(new{removeId=ambient.name,reason="Boat remains as scenery; retain the second active coastal cannon."});
    }
    private static void ReplaceNoryGimmick(int index,string kind)
    {
        var old=props.Cast<Transform>().Single(t=>t.name.StartsWith($"SR18_L_G{index:D2}_",StringComparison.Ordinal));
        Vector3 center=old.position;Quaternion rotation=old.rotation;Vector3 direction=old.forward;string prior=old.name,id=prior.Replace("Light",kind);
        UnityEngine.Object.DestroyImmediate(old.gameObject);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Obstacle_Real/"+(kind=="Oldman"?"Boat":"Dolphins")+".prefab");
        var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,props);root.name=id;root.SetActive(true);
        root.transform.SetPositionAndRotation(new Vector3(center.x,0,center.z),rotation);
        var parts=root.GetComponentsInChildren<ObstacleStats>(true);var active=parts[0];foreach(var extra in parts.Skip(1))extra.gameObject.SetActive(false);
        if(kind=="Oldman")
        {
            var bounds=HighwayAssetImporter.BoundsOf(root);root.transform.localScale*=5.2f/Mathf.Max(bounds.size.x,bounds.size.z);
            root.transform.position=new Vector3(center.x,-.45f,center.z)+Vector3.Cross(Vector3.up,direction)*4.6f;active.value=12;
            foreach(var collider in root.GetComponentsInChildren<Collider>(true))if(collider.GetComponentInParent<SimpleProjectile>()==null)collider.enabled=false;
        }
        else
        {
            Vector3 point=new Vector3(center.x,-.8f,center.z)+Vector3.Cross(Vector3.up,direction)*14;
            var a=new GameObject("JumpStart").transform;a.SetParent(root.transform,false);a.position=point-direction*4;
            var b=new GameObject("JumpEnd").transform;b.SetParent(root.transform,false);b.position=point+direction*4;
            active.pointA=a;active.pointB=b;active.transform.position=a.position;active.jumpHeight=3;active.jumpTime=2;active.flipYawOnReverse=false;active.yawOffset=0;active.value=0;
        }
        gimmicks.Add(new{oldId=prior,id,pattern=kind,effect=active.value,ambient=kind=="Dolphin"});
    }
    private static void RebuildHighway()
    {
        var schedule=JObject.Parse(File.ReadAllText(Output+"/highway-encounter-schedule.json"))["events"];
        float At(int station,string kind)=>(float)schedule.Single(e=>(int)e["station"]==station&&(string)e["kind"]==kind)["distance"];
        var originals=enemies.GetComponentsInChildren<EnemyEventController>(true).OrderBy(e=>e.name).ToList();
        if(originals.Count!=23)throw new InvalidOperationException("Expected23 Highway leaders.");
        for(int i=0;i<2;i++)
        {
            var source=originals[i];var clone=Clone(source.gameObject,enemies,"HWY_AddedLeader_"+(i+1)).GetComponent<EnemyEventController>();
            if(source.HasUsableTarget){var marker=new GameObject(clone.name+"_Target").transform;marker.SetParent(targets,false);clone.TargetPoint=marker;}
            var gate=props.GetComponentsInChildren<EnemyEventActivationSpot>(true).Single(s=>s.Targets.Length==1&&s.Targets[0]==source);
            Clone(gate.gameObject,props,clone.name+"_Activation").GetComponent<EnemyEventActivationSpot>().Targets=new[]{clone};
            originals.Insert(originals.Count-1,clone);copies.Add(new{id=clone.name,sourceId=source.name});
        }
        for(int i=0;i<originals.Count;i++)
        {
            var leader=originals[i];float distance=At(i+1,"enemy");Vector3 center=HighwaySceneBuilder.Sample(distance,out var direction),right=Vector3.Cross(Vector3.up,direction);
            float sectionStart=SectionStart(distance);float lead=Mathf.Min(42,distance-sectionStart-20);
            var gate=props.GetComponentsInChildren<EnemyEventActivationSpot>(true).Single(s=>s.Targets.Length==1&&s.Targets[0]==leader);
            var companion=Clone(leader.gameObject,enemies,leader.name+"_Right").GetComponent<EnemyEventController>();
            if(leader.HasUsableTarget){var target=new GameObject(companion.name+"_Target").transform;target.SetParent(targets,false);companion.TargetPoint=target;}
            var rightGate=Clone(gate.gameObject,props,gate.name+"_Right").GetComponent<EnemyEventActivationSpot>();rightGate.Targets=new[]{companion};
            foreach(var actor in new[]{leader,companion})
            {
                Vector3 lane=center+right*(actor==leader?-2.5f:2.5f);float span=actor.EventMode==EnemyEventMode.PatrolBetweenStartAndTarget?2.8f:0;
                actor.transform.rotation=Quaternion.LookRotation(direction);MoveEnemy(actor,lane-right*span*.5f);
                if(actor.HasUsableTarget){var end=lane+right*span*.5f;end.y=Floor(end);actor.TargetPoint.position=end;}
                var clearance=actor.GetComponent<EnemyCornerClearance>()??actor.gameObject.AddComponent<EnemyCornerClearance>();clearance.Configure(HighwaySceneBuilder.Sample(sectionStart,out _),direction);
                var activation=actor==leader?gate:rightGate;var p=HighwaySceneBuilder.Sample(distance-lead,out _);p.y=Floor(p);activation.transform.SetPositionAndRotation(p,Quaternion.LookRotation(direction));
                placements.Add(new{id=actor.name,station=i+1,kind="enemy",distance,lead,move=span});
            }
            copies.Add(new{id=companion.name,sourceId=leader.name});
        }
        var pairs=bonuses.GetComponentsInChildren<BonusWallChoicePair>(true).OrderBy(p=>p.name).ToList();
        while(pairs.Count<25)
        {
            string id="HWY_B"+(pairs.Count+1).ToString("D2");var left=Clone(pairs[0].Left.gameObject,bonuses,id).GetComponent<AuthoredBonusWall>();var right=Clone(pairs[0].Right.gameObject,bonuses,id+"_Right").GetComponent<AuthoredBonusWall>();
            var pair=left.GetComponent<BonusWallChoicePair>()??left.gameObject.AddComponent<BonusWallChoicePair>();pair.Configure(left,right);pairs.Add(pair);
        }
        for(int i=0;i<pairs.Count;i++)
        {
            float distance=At(i+1,"bonus");var p=HighwaySceneBuilder.Sample(distance,out var direction);var pair=pairs[i];
            foreach(var altar in new[]{pair.Left,pair.Right}){var point=p+Vector3.Cross(Vector3.up,direction)*(altar==pair.Left?-3:3);point.y=Floor(point);altar.transform.SetPositionAndRotation(point,Quaternion.LookRotation(-direction));}
            placements.Add(new{id=pair.Left.name,station=i+1,kind="bonus",distance});
        }
        var hazards=props.Cast<Transform>().Where(t=>t.GetComponent<HighwayHazard>()!=null).OrderBy(t=>t.name).ToList();
        // Toll lanes share distance but differ laterally. Group by the existing recorded distance.
        var old=JObject.Parse(File.ReadAllText("map-concepts/highway-chapter-2026-09-11/placements.json"))["gimmicks"];
        var tollGroups=old.Where(r=>(string)r["pattern"]=="HighwayToll").GroupBy(r=>(float)r["distance"]).OrderBy(g=>g.Key).ToArray();
        var ordinary=hazards.Where(t=>t.GetComponent<ObstacleStats>().obstaclePattern!=ObstaclePattern.HighwayToll).ToList();
        for(int i=0;i<2;i++){var clone=Clone(ordinary[0].gameObject,props,"HWY_AddedRoadblock_"+(i+1));ordinary.Add(clone.transform);}
        int ordinaryIndex=0,tollIndex=0;
        for(int station=1;station<=25;station++)
        {
            float distance=At(station,"gimmick");var p=HighwaySceneBuilder.Sample(distance,out var direction);var right=Vector3.Cross(Vector3.up,direction);
            if(new[]{16,19,22}.Contains(station))
            {
                string id="HWY_TollStation_"+(++tollIndex);var group=new GameObject(id).transform;group.SetParent(props,false);group.SetPositionAndRotation(p,Quaternion.LookRotation(direction));
                foreach(var row in tollGroups[tollIndex-1]){var part=props.Find((string)row["id"]);part.SetParent(group,true);}
                gimmicks.Add(new{id,station,distance,pattern="HighwayToll",memberIds=tollGroups[tollIndex-1].Select(r=>(string)r["id"]).ToArray()});
            }
            else
            {
                var root=ordinary[ordinaryIndex++];var hazard=root.GetComponent<HighwayHazard>();float lane=hazard.GetComponent<ObstacleStats>().obstaclePattern==ObstaclePattern.HighwayTraffic?-4.5f:(station%2==0?-3.5f:3.5f);
                root.SetPositionAndRotation(p+right*lane,Quaternion.LookRotation(direction));
                gimmicks.Add(new{id=root.name,station,distance,pattern=root.GetComponent<ObstacleStats>().obstaclePattern.ToString()});
            }
        }
    }
    private static float SectionStart(float distance){float start=0;for(int i=0;i<HighwaySceneBuilder.Points.Length-1;i++){float length=Vector3.ProjectOnPlane(HighwaySceneBuilder.Points[i+1]-HighwaySceneBuilder.Points[i],Vector3.up).magnitude;if(distance<=start+length)return start;start+=length;}return start;}
    private static void MoveEnemy(EnemyEventController enemy,Vector3 destination)
    {
        var prior=enemy.transform.position;destination.y=Floor(destination);Floor(destination+enemy.transform.right*.6f);Floor(destination-enemy.transform.right*.6f);
        enemy.transform.position=destination;enemy.RefreshPlacementAfterAuthoringChange(prior,true);
    }
    private static float Floor(Vector3 point)
    {
        foreach(var offset in new[]{Vector3.zero,Vector3.right*.12f,Vector3.left*.12f,Vector3.forward*.12f,Vector3.back*.12f})
            foreach(var road in roads)if(road.Raycast(new Ray(point+offset+Vector3.up*2,Vector3.down),out var hit,4)&&hit.normal.y>.7f)return hit.point.y+.08f;
        throw new InvalidOperationException("No road support at "+point);
    }
    private static GameObject Clone(GameObject source,Transform parent,string name)
    {
        var clone=UnityEngine.Object.Instantiate(source,parent);clone.name=name;clone.SetActive(true);
        string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(source);
        if(!string.IsNullOrEmpty(path))PrefabUtility.ConvertToPrefabInstance(clone,AssetDatabase.LoadAssetAtPath<GameObject>(path),new ConvertToPrefabInstanceSettings{componentsNotMatchedBecomesOverride=true,gameObjectsNotMatchedBecomesOverride=true,recordPropertyOverridesOfMatches=true,changeRootNameToAssetName=false},InteractionMode.AutomatedAction);
        return clone;
    }
}
