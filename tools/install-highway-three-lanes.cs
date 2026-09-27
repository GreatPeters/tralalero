using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

public static class InstallHighwayThreeLanes
{
    const string Record="outputs/highway-three-lane-rebuild-2026-09-26";
    const string AssetRoot="Assets/ShooterSurvival/Models/Chapters/HighwayThreeLane20260926";
    const string RootName="ThreeLaneHighway_20260926";
    static readonly float[] Lanes={-4.2f,-.6f,3f};
    static string Json(object o)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
    static string P(Transform t)=>t.parent==null?t.name:P(t.parent)+"/"+t.name;
    static float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
    static HighwayRoute Route()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().name!="HighWay")throw new Exception("HighWay Edit Mode required");
        return Object.FindFirstObjectByType<HighwayRoute>();
    }
    public static object Inspect()
    {
        var r=Route();Directory.CreateDirectory(Record);
        var map=r.transform;var roads=map.Find("Roads");
        float Lane(Vector3 position){float d=r.NearestDistance(position);r.Sample(d,false,out var c,out var f);return Vector3.Dot(position-c,Vector3.Cross(Vector3.up,f));}
        var o=new{r.length,centers=r.centers.Length,
            forks=r.forks.Select(f=>new{f.start,f.end,f.offset}).ToArray(),
            rows=map.GetComponentsInChildren<HighwayEncounterRow>(true).Select(x=>new{path=P(x.transform),x.routeDistance,x.laneCenter,x.halfWidth,active=x.gameObject.activeInHierarchy,left=x.left==null?null:new{name=x.left.name,lane=Lane(x.left.transform.position)},right=x.right==null?null:new{name=x.right.name,lane=Lane(x.right.transform.position)}}).ToArray(),
            markings=roads.GetComponentsInChildren<LineRenderer>(true).GroupBy(x=>x.transform.parent.name+"/"+x.name).Select(g=>new{name=g.Key,count=g.Count()}).ToArray(),
            roadGroups=roads.Cast<Transform>().Select(t=>new{t.name,active=t.gameObject.activeSelf,children=t.childCount}).ToArray(),
            traffic=map.GetComponentsInChildren<OncomingLaneTraffic>(true).Select(x=>new{path=P(x.transform),x.lanes,x.laneDirections,ranges=x.activeRanges.Select(a=>new[]{a.x,a.y}).ToArray(),templates=x.carTemplates.Select(t=>t==null?null:t.name).ToArray(),x.minInterval,x.maxInterval}).ToArray(),
            logs=map.GetComponentsInChildren<LogTruckSpill>(true).Select(x=>new{path=P(x.transform),x.triggerDistance,x.truckLane,x.logLanes}).ToArray(),
            holes=map.GetComponentsInChildren<RoadPotholeHazard>(true).Select(x=>new{path=P(x.transform),distance=r.NearestDistance(x.transform.position),lane=Lane(x.transform.position)}).ToArray(),
            tolls=map.GetComponentsInChildren<HighwayHazard>(true).Where(x=>x.GetComponent<ObstacleStats>()?.obstaclePattern==ObstaclePattern.HighwayToll).Select(x=>new{path=P(x.transform),distance=r.NearestDistance(x.transform.position),lane=Lane(x.transform.position)}).ToArray(),
            prefabCounts=new{enemies=map.GetComponentsInChildren<EnemyScript_space>(true).Length,bonuses=map.GetComponentsInChildren<BonusWallChoicePair>(true).Length},
            materials=map.GetComponentsInChildren<Renderer>(true).SelectMany(x=>x.sharedMaterials).Where(m=>m!=null&&m.name.ToLower().Contains("concrete")).Select(m=>AssetDatabase.GetAssetPath(m)).Distinct().ToArray()};
        File.WriteAllText(Record+"/inspection.json",Json(o));return o;
    }

    static void RecordObject(Object o)
    {EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
    static void Disable(GameObject go,List<string> changes)
    {if(!go.activeSelf)return;Undo.RecordObject(go,"Three-lane Highway");changes.Add("disabled "+P(go.transform));go.SetActive(false);RecordObject(go);}
    static Transform Group(Transform parent,string name)
    {var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Three-lane Highway");go.transform.SetParent(parent,false);return go.transform;}
    static void Sample(HighwayRoute r,float d,bool branch,out Vector3 p,out Vector3 f)
    {r.Sample(Mathf.Clamp(d,0,r.length),branch,out p,out f);if(d<0)p+=f*d;else if(d>r.length)p+=f*(d-r.length);}
    static Mesh SaveMesh(string name,List<Vector3> vertices,List<Vector2> uv,List<int> triangles)
    {
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=AssetRoot+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);AssetDatabase.SaveAssetIfDirty(saved);}
        return saved;
    }
    static void MeshObject(Transform parent,string name,Mesh mesh,Material material,bool collider=false)
    {
        var t=parent.Find(name);if(t==null)t=Group(parent,name);t.gameObject.isStatic=true;
        var filter=t.GetComponent<MeshFilter>();if(filter==null)filter=t.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
        var renderer=t.GetComponent<MeshRenderer>();if(renderer==null)renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        if(!collider){renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}
        if(collider){var contact=t.GetComponent<MeshCollider>();if(contact==null)contact=t.gameObject.AddComponent<MeshCollider>();contact.sharedMesh=mesh;}
    }
    static void Quad(HighwayRoute r,float start,float end,float left,float right,float y,bool branch,List<Vector3> v,List<Vector2> uv,List<int> tri)
    {
        Sample(r,start,branch,out var a,out var f);Sample(r,end,branch,out var b,out var ff);var ar=Vector3.Cross(Vector3.up,f);var br=Vector3.Cross(Vector3.up,ff);int n=v.Count;
        v.Add(a+ar*left+Vector3.up*y);v.Add(a+ar*right+Vector3.up*y);v.Add(b+br*left+Vector3.up*y);v.Add(b+br*right+Vector3.up*y);
        uv.Add(new Vector2(left/5,start/5));uv.Add(new Vector2(right/5,start/5));uv.Add(new Vector2(left/5,end/5));uv.Add(new Vector2(right/5,end/5));tri.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});
    }
    static void Paint(Transform parent,HighwayRoute r,string name,float from,float to,Material white,bool branch=false,bool background=false)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();float[] edges=background?new[]{-18.84f}:branch?new[]{-5.4f,5.4f}:new[]{4.8f};float[] dividers=background?new[]{-11.64f,-15.24f}:branch?new[]{-1.8f,1.8f}:new[]{-2.4f,1.2f};
        foreach(float lane in edges)for(float d=from;d<to;d+=2)Quad(r,d,Mathf.Min(to,d+2),lane-.08f,lane+.08f,branch ? .053f : .045f,branch,v,uv,tri);
        foreach(float lane in dividers)for(float d=Mathf.Floor(from/20)*20;d<to;d+=20)
            for(float dd=Mathf.Max(from,d);dd<Mathf.Min(to,d+8);dd+=2)Quad(r,dd,Mathf.Min(to,Mathf.Min(d+8,dd+2)),lane-.075f,lane+.075f,branch ? .053f : .045f,branch,v,uv,tri);
        MeshObject(parent,name,SaveMesh(name,v,uv,tri),white);
    }
    static void BackgroundRoad(Transform parent,HighwayRoute r,string name,float from,float to,Material asphalt)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();for(float d=from;d<to;d+=2)Quad(r,d,Mathf.Min(to,d+2),-19.4f,-7.44f,.005f,false,v,uv,tri);
        MeshObject(parent,name,SaveMesh(name,v,uv,tri),asphalt);
    }
    static void Median(Transform parent,HighwayRoute r,string name,float from,float to,Material concrete)
    {
        var cross=new[]{new Vector2(-.42f,.025f),new Vector2(-.18f,.55f),new Vector2(-.18f,1f),new Vector2(.18f,1f),new Vector2(.42f,.025f)};
        var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();int steps=Mathf.CeilToInt((to-from)/2);
        for(int i=0;i<=steps;i++){float d=Mathf.Lerp(from,to,i/(float)steps);Sample(r,d,false,out var p,out var f);var right=Vector3.Cross(Vector3.up,f);foreach(var c in cross){v.Add(p+right*(-7.02f+c.x)+Vector3.up*c.y);uv.Add(new Vector2(d/4,c.y));}
            if(i>0)for(int j=0;j<cross.Length-1;j++){int a=(i-1)*cross.Length+j,b=i*cross.Length+j;tri.AddRange(new[]{a,b,a+1,a+1,b,b+1});}}
        MeshObject(parent,name,SaveMesh(name,v,uv,tri),concrete,true);
    }
    static void Place(HighwayRoute r,Transform t,float distance,float lane)
    {
        float oldD=r.NearestDistance(t.position);r.Sample(oldD,false,out var oldP,out var oldF);r.Sample(distance,false,out var p,out var f);float y=t.position.y-oldP.y;
        Undo.RecordObject(t,"Three-lane placement");t.SetPositionAndRotation(p+Vector3.Cross(Vector3.up,f)*lane+Vector3.up*y,Quaternion.LookRotation(f)*Quaternion.Inverse(Quaternion.LookRotation(oldF))*t.rotation);RecordObject(t);
    }
    static IEnumerable<Vector2> Cut(Vector2 range,float from,float to)
    {if(to<=range.x||from>=range.y){yield return range;yield break;}if(from>range.x)yield return new Vector2(range.x,from);if(to<range.y)yield return new Vector2(to,range.y);}
    static GameObject BusTemplate(OncomingLaneTraffic traffic)
    {
        var holder=Group(traffic.transform,"Template_V05_tour_bus");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/V05_tour_bus.prefab");
        var child=(GameObject)PrefabUtility.InstantiatePrefab(source,holder);child.transform.localPosition=Vector3.zero;child.transform.localRotation=Quaternion.identity;
        var rs=child.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var x in rs.Skip(1))b.Encapsulate(x.bounds);child.transform.localScale*=2.9f/b.size.x;
        b=rs[0].bounds;foreach(var x in rs.Skip(1))b.Encapsulate(x.bounds);child.transform.position+=holder.position-new Vector3(b.center.x,b.min.y,b.center.z);
        foreach(var c in child.GetComponentsInChildren<Collider>(true)){c.enabled=false;RecordObject(c);}foreach(var t in child.GetComponentsInChildren<Transform>(true)){t.gameObject.isStatic=false;RecordObject(t.gameObject);}
        RecordObject(child.transform);holder.gameObject.SetActive(false);return holder.gameObject;
    }
    static void ClearBypassScenery(HighwayRoute r,List<string> changes)
    {
        var candidates=new HashSet<Transform>();var map=r.transform;
        foreach(var renderer in map.GetComponentsInChildren<MeshRenderer>())
        {
            string path=P(renderer.transform);if(!path.Contains("Mountain_Ridge")&&!path.Contains("Roadside grove")&&!path.Contains("Highway_Tree"))continue;
            var bounds=renderer.bounds;bool blocked=false;
            foreach(var fork in r.forks)
            {for(float d=fork.start;d<=fork.end;d+=5){r.Sample(d,true,out var p,out var f);var q=new Vector3(p.x,Mathf.Clamp(p.y+2,bounds.min.y,bounds.max.y),p.z);if(bounds.max.y>p.y+.5f&&bounds.SqrDistance(q)<64){blocked=true;break;}}if(blocked)break;}
            if(!blocked)continue;
            var unit=renderer.transform;while(unit.parent!=null&&unit.name!="Mountain_Ridge"&&!unit.name.StartsWith("tree_")&&!unit.name.StartsWith("Highway_Tree")&&unit.parent!=map)unit=unit.parent;
            if(unit!=map)candidates.Add(unit);
        }
        foreach(var unit in candidates)Disable(unit.gameObject,changes);
    }
    public static object Apply()
    {
        var r=Route();var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Save unrelated authoring before install");
        if(r.transform.Find(RootName)!=null)throw new Exception("Already installed; use Verify");
        if(!File.Exists(Record+"/before/HighWay.unity"))throw new Exception("Working-copy backup required");
        var map=r.transform;var roads=map.Find("Roads");int enemies=map.GetComponentsInChildren<EnemyScript_space>(true).Length,bonuses=map.GetComponentsInChildren<BonusWallChoicePair>(true).Length;
        var asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat");var white=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Chapters/ApprovedRoadConcepts/RoadWhite.mat");var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Concrete.mat");
        if(asphalt==null||white==null||concrete==null)throw new Exception("Native road materials missing");
        Directory.CreateDirectory(AssetRoot);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install three-lane Highway");
        var changes=new List<string>();
        try
        {
            var root=Group(map,RootName);var main=Group(root,"MainWhiteMarkings");var side=Group(root,"EmptyAdjacentRoad");var median=Group(root,"ConcreteMedian");
            var oldMarks=roads.Find("Four_Lane_Markings");if(oldMarks!=null)Disable(oldMarks.gameObject,changes);
            foreach(var t in roads.Cast<Transform>().Where(t=>t.name.StartsWith("Opposing_Road_")).ToArray())Disable(t.gameObject,changes);
            foreach(var t in map.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Ext_Centre_Line").ToArray())Disable(t.gameObject,changes);
            int index=0;for(float d=-30;d<r.length+30;d+=40)
            {float end=Mathf.Min(r.length+30,d+40);Paint(main,r,"MainPaint_"+index,d,end,white);BackgroundRoad(side,r,"Adjacent_"+index,d,end,asphalt);Paint(side,r,"AdjacentPaint_"+index,d,end,white,false,true);Median(median,r,"Median_"+index,d,end,concrete);index++;}
            var branchMarks=Group(root,"RecoveryRoadMarkings");int bi=0;
            foreach(var fork in r.forks)
            {
                // Leave the overlap at the split/merge visually clean; the coloured guide identifies the choice.
                float a=fork.start,b=fork.end;while(a<b&&Mathf.Abs(HighwayRoute.BranchOffset(a,fork.start,fork.end,fork.offset))<14)a+=2;while(b>a&&Mathf.Abs(HighwayRoute.BranchOffset(b,fork.start,fork.end,fork.offset))<14)b-=2;
                foreach(var old in roads.Cast<Transform>().Where(t=>t.name.StartsWith("Bypass_")).SelectMany(t=>t.GetComponentsInChildren<LineRenderer>(true))){Undo.RecordObject(old,"Replace bypass marking");old.enabled=false;RecordObject(old);}
                if(b>a)Paint(branchMarks,r,"BypassPaint_"+bi,a,b,white,true);bi++;
            }
            foreach(var ambient in map.GetComponentsInChildren<AmbientTrafficPath>(true))Disable(ambient.gameObject,changes);
            foreach(var ambient in map.GetComponentsInChildren<AmbientTrafficLane>(true))Disable(ambient.gameObject,changes);
            foreach(var old in map.GetComponentsInChildren<HighwayOncomingTraffic>(true))Disable(old.gameObject,changes);
            foreach(var chase in map.GetComponentsInChildren<RestStopRouteGimmick>(true))
            {
                if(chase.kind==RestStopRouteGimmick.Kind.ChaseCar)Disable(chase.gameObject,changes);
                else{Undo.RecordObject(chase,"Three-lane rolling pattern");chase.lanes=new[]{-4.4f,0,4.4f};RecordObject(chase);}
            }
            var traffic=map.GetComponentInChildren<OncomingLaneTraffic>(true);Undo.RecordObject(traffic,"Three-lane incoming traffic");
            traffic.lanes=new[]{-4.4f,0,4.4f};traffic.laneDirections=new[]{-1f,-1f,-1f};traffic.preserveEscapeLane=true;traffic.protectEncounterPassages=true;traffic.escapeWindowSeconds=1.5f;
            traffic.carTemplates=traffic.carTemplates.Concat(new[]{BusTemplate(traffic)}).ToArray();
            var ranges=new List<Vector2>{new Vector2(85,r.length-25)};
            foreach(var toll in map.GetComponentsInChildren<HighwayHazard>(true).Where(t=>t.GetComponent<ObstacleStats>()?.obstaclePattern==ObstaclePattern.HighwayToll))
            {float d=r.NearestDistance(toll.transform.position);r.Sample(d,false,out var p,out var f);float lane=Vector3.Dot(toll.transform.position-p,Vector3.Cross(Vector3.up,f));Place(r,toll.transform,d,Mathf.Abs(lane)<1?0:Mathf.Sign(lane)*4.4f);ranges=ranges.SelectMany(x=>Cut(x,d-55,d+30)).ToList();}
            foreach(var hole in map.GetComponentsInChildren<RoadPotholeHazard>(true))
            {
                float d=r.NearestDistance(hole.transform.position);r.Sample(d,false,out var p,out var f);float lane=Vector3.Dot(hole.transform.position-p,Vector3.Cross(Vector3.up,f));float target=lane<0?-4.4f:4.4f;Place(r,hole.transform,d,target);
                foreach(Transform child in hole.transform)if(child.name.Contains("Sign"))Place(r,child,Mathf.Max(0,d-32),Mathf.Sign(target)*8.4f);
                ranges=ranges.SelectMany(x=>Cut(x,d-50,d+30)).ToList();changes.Add("pothole "+d.ToString("F0")+" lane="+target);
            }
            foreach(var fork in r.forks){ranges=ranges.SelectMany(x=>Cut(x,fork.start-35,fork.start+20)).ToList();ranges=ranges.SelectMany(x=>Cut(x,fork.end-15,fork.end+25)).ToList();}
            traffic.activeRanges=ranges.Where(x=>x.y-x.x>35).OrderBy(x=>x.x).ToArray();RecordObject(traffic);
            foreach(var spill in map.GetComponentsInChildren<LogTruckSpill>(true))
            {Undo.RecordObject(spill,"Head-on spill truck");spill.oncoming=true;spill.truckLane=0;spill.logLanes=new[]{.6f,4.4f,.3f,4.1f};spill.warnSeconds=2.5f;RecordObject(spill);}
            ClearBypassScenery(r,changes);
            SpaceRoadblocks(r);
            if(enemies!=map.GetComponentsInChildren<EnemyScript_space>(true).Length||bonuses!=map.GetComponentsInChildren<BonusWallChoicePair>(true).Length)throw new Exception("Combat/reward roots changed");
            Undo.FlushUndoRecordObjects();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("HighWay save failed");
            File.WriteAllText(Record+"/installed.json",Json(new{enemies,bonuses,segments=index,changes,trafficLanes=traffic.lanes,directions=traffic.laneDirections,ranges=traffic.activeRanges.Select(x=>new[]{x.x,x.y}).ToArray()}));
            Undo.CollapseUndoOperations(undo);return MatchCurrentNativeReference();
        }
        catch{Undo.RevertAllDownToGroup(undo);throw;}
    }
    public static object Verify()
    {
        var r=Route();var map=r.transform;var root=map.Find(RootName)??throw new Exception("Installation missing");var traffic=map.GetComponentInChildren<OncomingLaneTraffic>(true);
        if(!traffic.lanes.SequenceEqual(Lanes)||traffic.laneDirections.Length!=3||traffic.laneDirections.Any(d=>d>=0)||!traffic.preserveEscapeLane||!traffic.protectEncounterPassages)throw new Exception("Incoming lane configuration mismatch");
        if(map.GetComponentsInChildren<AmbientTrafficPath>().Any(a=>a.isActiveAndEnabled)||map.GetComponentsInChildren<AmbientTrafficLane>().Any(a=>a.isActiveAndEnabled))throw new Exception("Background traffic remains");
        if(map.GetComponentsInChildren<HighwayOncomingTraffic>().Any(a=>a.isActiveAndEnabled))throw new Exception("Legacy traffic remains");
        if(map.GetComponentsInChildren<RestStopRouteGimmick>().Any(g=>g.kind==RestStopRouteGimmick.Kind.ChaseCar&&g.isActiveAndEnabled))throw new Exception("Rear chase remains");
        if(map.GetComponentsInChildren<LogTruckSpill>().Any(s=>!s.oncoming||s.truckLane!=Lanes[1]))throw new Exception("Spill truck configuration mismatch");
        if(root.GetComponentsInChildren<MeshFilter>().Length!=362||root.GetComponentsInChildren<MeshFilter>().Any(m=>m.sharedMesh==null))throw new Exception("Native road mesh references incomplete");
        if(map.GetComponentsInChildren<EnemyScript_space>(true).Length!=72||map.GetComponentsInChildren<BonusWallChoicePair>(true).Length!=29)throw new Exception("Combat/reward contract changed");
        var result=new{scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,r.length,lanes=traffic.lanes,directions=traffic.laneDirections,templates=traffic.carTemplates.Length,meshes=root.GetComponentsInChildren<MeshFilter>().Length,medianColliders=root.Find("ConcreteMedian").GetComponentsInChildren<Collider>().Length,enemies=map.GetComponentsInChildren<EnemyScript_space>(true).Length,bonuses=map.GetComponentsInChildren<BonusWallChoicePair>(true).Length,logTrucks=map.GetComponentsInChildren<LogTruckSpill>().Length};
        File.WriteAllText(Record+"/verified.json",Json(result));return result;
    }
    public static object ReloadAndVerify()
    {
        Route();if(SceneManager.GetActiveScene().isDirty)throw new Exception("Save before reload");
        EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/HighWay.unity");return Verify();
    }
    public static object AlignSpillTrucks()
    {
        var r=Route();var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Clean scene required");
        foreach(var spill in r.GetComponentsInChildren<LogTruckSpill>(true))
        {Undo.RecordObject(spill,"Centre-lane oncoming spill truck");spill.truckLane=Lanes[1];RecordObject(spill);}
        Undo.FlushUndoRecordObjects();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");return Verify();
    }
    static object SpaceRoadblocks(HighwayRoute r)
    {
        var blocks=r.GetComponentsInChildren<HighwayHazard>(true).Where(h=>!h.name.Contains("Template")&&h.GetComponent<ObstacleStats>()?.obstaclePattern==ObstaclePattern.HighwayRoadblock).OrderBy(h=>r.NearestDistance(h.transform.position)).ToArray();
        var spills=r.GetComponentsInChildren<LogTruckSpill>(true);
        var holes=r.GetComponentsInChildren<RoadPotholeHazard>(true).Select(h=>r.NearestDistance(h.transform.position)).ToArray();
        var tolls=r.GetComponentsInChildren<HighwayHazard>(true).Where(h=>h.GetComponent<ObstacleStats>()?.obstaclePattern==ObstaclePattern.HighwayToll).Select(h=>r.NearestDistance(h.transform.position)).ToArray();
        var rows=r.GetComponentsInChildren<HighwayEncounterRow>(true).Select(h=>h.routeDistance>1?h.routeDistance:r.NearestDistance(h.transform.position)).ToArray();
        var chosen=new List<float>();var moves=new List<object>();
        foreach(var block in blocks)
        {
            float original=r.NearestDistance(block.transform.position);r.Sample(original,false,out var p,out var f);float lane=Vector3.Dot(block.transform.position-p,Vector3.Cross(Vector3.up,f))<0?Lanes[0]:Lanes[2];
            bool Safe(float d)=>!spills.Any(s=>d>=s.triggerDistance-20&&d<=s.triggerDistance+110)
                &&!tolls.Any(t=>d>=t-40&&d<=t+35)&&!holes.Any(h=>Mathf.Abs(d-h)<30)
                &&!r.forks.Any(b=>Mathf.Abs(d-b.start)<30||(d>=b.end-20&&d<=b.end+30))
                &&!rows.Any(row=>Mathf.Abs(d-row)<20)&&!chosen.Any(other=>Mathf.Abs(d-other)<30);
            var available=Enumerable.Range(11,544).Select(i=>i*5f).Where(Safe).OrderBy(d=>Mathf.Abs(d-original)).ToArray();
            if(available.Length==0)throw new Exception("No safe roadblock slot for "+block.name);
            float target=available[0];Place(r,block.transform,target,lane);chosen.Add(target);moves.Add(new{block.name,from=original,to=target,lane});
        }
        var result=new{count=blocks.Length,moves};File.WriteAllText(Record+"/roadblock-spacing.json",Json(result));return result;
    }
    public static object RefineRoadblockSpacing()
    {
        var r=Route();var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Clean scene required");
        var result=SpaceRoadblocks(r);Undo.FlushUndoRecordObjects();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");return result;
    }
    // Apply once after the initial installation; refreshes the same mesh assets in place.
    public static object MatchCurrentNativeReference()
    {
        var r=Route();var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Clean scene required");
        var root=r.transform.Find(RootName);var main=root.Find("MainWhiteMarkings");var side=root.Find("EmptyAdjacentRoad");var median=root.Find("ConcreteMedian");
        var yellowRoot=root.Find("MedianYellowLines");if(yellowRoot==null)yellowRoot=Group(root,"MedianYellowLines");
        var white=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Chapters/ApprovedRoadConcepts/RoadWhite.mat");
        var yellow=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Chapters/ApprovedRoadConcepts/CenterYellow.mat");
        var asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat");
        var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Concrete.mat");
        int index=0;for(float d=-30;d<r.length+30;d+=40)
        {
            float end=Mathf.Min(r.length+30,d+40);Paint(main,r,"MainPaint_"+index,d,end,white);BackgroundRoad(side,r,"Adjacent_"+index,d,end,asphalt);Paint(side,r,"AdjacentPaint_"+index,d,end,white,false,true);Median(median,r,"Median_"+index,d,end,concrete);
            var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();foreach(float lane in new[]{-6f,-8.04f})for(float s=d;s<end;s+=2)Quad(r,s,Mathf.Min(end,s+2),lane-.075f,lane+.075f,.049f,false,v,uv,tri);
            MeshObject(yellowRoot,"Yellow_"+index,SaveMesh("Yellow_"+index,v,uv,tri),yellow);index++;
        }
        var traffic=r.GetComponentInChildren<OncomingLaneTraffic>(true);Undo.RecordObject(traffic,"Native-reference lanes");traffic.lanes=(float[])Lanes.Clone();RecordObject(traffic);
        foreach(var hazard in r.GetComponentsInChildren<HighwayHazard>(true))
        {
            if(hazard.GetComponent<ObstacleStats>()?.obstaclePattern==ObstaclePattern.HighwayToll)Place(r,hazard.transform,r.NearestDistance(hazard.transform.position),Lanes[Mathf.Clamp(hazard.laneIndex,0,2)]);
            if(hazard.name=="ConeLayer_RoadblockTemplate")Place(r,hazard.transform,119,3.5f);
        }
        foreach(var hole in r.GetComponentsInChildren<RoadPotholeHazard>(true))
        {float d=r.NearestDistance(hole.transform.position);r.Sample(d,false,out var p,out var f);Place(r,hole.transform,d,Vector3.Dot(hole.transform.position-p,Vector3.Cross(Vector3.up,f))<0?Lanes[0]:Lanes[2]);}
        foreach(var spill in r.GetComponentsInChildren<LogTruckSpill>(true))
        {Undo.RecordObject(spill,"Native-reference log lanes");spill.truckLane=Lanes[1];spill.logLanes=new[]{-.6f,3f,-.3f,2.7f};RecordObject(spill);}
        foreach(var gimmick in r.GetComponentsInChildren<RestStopRouteGimmick>(true))
        {Undo.RecordObject(gimmick,"Native-reference rolling lanes");gimmick.lanes=(float[])Lanes.Clone();RecordObject(gimmick);}
        SpaceRoadblocks(r);Undo.FlushUndoRecordObjects();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");return Verify();
    }
}
