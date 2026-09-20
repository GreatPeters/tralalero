using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NoryangjinRefinement
{
    private const string Prefix="SR18_Polish_";
    public static object Apply()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=NoryangjinMapToolWindow.Sr18MapToolScenePath)throw new InvalidOperationException("SR18 Edit Mode required.");
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var props=map.Find("Props");
        if(props.Cast<Transform>().Any(t=>t.name.StartsWith(Prefix)))throw new InvalidOperationException("Refinement already applied; preserve the authored result.");
        var roads=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
        var replacements=new List<object>();
        foreach(var spec in new[]{(number:10,kind:ObstaclePattern.Oil),(number:16,kind:ObstaclePattern.Seagull),(number:19,kind:ObstaclePattern.Oil)})
        {
            var old=props.Cast<Transform>().Single(t=>t.name.StartsWith($"SR18_L_G{spec.number:D2}_"));
            string previous=old.name,id=previous.Replace("Bucket",spec.kind.ToString());Vector3 position=old.position;Quaternion rotation=old.rotation;
            float ground=Ground(roads,position);UnityEngine.Object.DestroyImmediate(old.gameObject);
            GameObject root;
            if(spec.kind==ObstaclePattern.Oil)
            {
                root=new GameObject(id);root.transform.SetParent(props,false);
                var visual=Instantiate("Assets/ShooterSurvival/Prefabs/Obstacle_Real/Oil.prefab",root.transform,"OilVisual");Fit(visual,3.2f);
                foreach(var stat in visual.GetComponentsInChildren<ObstacleStats>(true))UnityEngine.Object.DestroyImmediate(stat);
                foreach(var collider in visual.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                root.transform.SetPositionAndRotation(new Vector3(position.x,ground+.025f,position.z),rotation);
                var stats=root.AddComponent<ObstacleStats>();stats.obstaclePattern=ObstaclePattern.Oil;stats.value=2;
                var box=root.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(2.1f,.35f,3.2f);box.center=Vector3.up*.15f;root.tag="Obstacle";
            }
            else
            {
                root=Instantiate("Assets/ShooterSurvival/Prefabs/Obstacle_Real/Seagull.prefab",props,id);root.transform.SetPositionAndRotation(new Vector3(position.x,ground,position.z),rotation);
                var stats=root.GetComponent<ObstacleStats>();stats.value=20;
                root.GetComponent<Collider>().enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(stats);
            }
            replacements.Add(new{previous,id,pattern=spec.kind.ToString(),effect=spec.kind==ObstaclePattern.Oil?2:20,center=new[]{root.transform.position.x,root.transform.position.y,root.transform.position.z}});
        }
        var additions=new List<object>();
        for(int i=0;i<2;i++)
        {
            var station=props.Cast<Transform>().Single(t=>t.name.StartsWith(i==0?"SR18_L_G08_":"SR18_L_G21_"));
            string id=Prefix+"Ship_"+(i+1);var ship=Instantiate("Assets/ShooterSurvival/Prefabs/Obstacle_Real/Ship.prefab",props,id);Fit(ship,7.5f);
            var position=station.position+station.right*(i==0?-14:14);position.y=-.7f;ship.transform.SetPositionAndRotation(position,Quaternion.LookRotation(Vector3.ProjectOnPlane(station.position-position,Vector3.up)));
            var stats=ship.GetComponent<ObstacleStats>();stats.value=10;stats.fireDistance=30;stats.aheadOffset=8;
            foreach(var collider in ship.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(stats);additions.Add(new{id,pattern="Ship",effect=10});
        }
        var shops=props.Cast<Transform>().Where(t=>t.name.StartsWith("SR18_Route_Shop_")).OrderBy(t=>t.name).ToArray();
        string[] keys={"001_STAGE01_NRY_PROPS_001_","002_STAGE01_NRY_PROPS_002_","003_STAGE01_NRY_PROPS_003_","020_STAGE01_NRY_DCR_001_","022_STAGE01_NRY_DCR_003_","032_STAGE01_NRY_PROPS_021_","033_STAGE01_NRY_PROPS_022_","034_STAGE01_NRY_DCR_023_"};
        string[] smallPaths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>keys.Any(key=>p.Contains("/"+key))).ToArray();
        int decorated=0;
        for(int i=0;i<shops.Length;i+=4)
        {
            var shop=shops[i];var bounds=HighwayAssetImporter.BoundsOf(shop.gameObject);
            float extent=Mathf.Abs(shop.forward.x)*bounds.extents.x+Mathf.Abs(shop.forward.z)*bounds.extents.z;
            for(int part=0;part<2;part++)
            {
                Vector3 point=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)+shop.forward*(extent*.65f)+shop.right*(part==0?-1.15f:1.05f);
                if(OnRoad(roads,point,.85f))continue;
                var item=Instantiate(smallPaths[(i+part)%smallPaths.Length],props,Prefix+"MarketDetail_"+(++decorated).ToString("D3"));Fit(item,part==0?1.35f:1.0f);
                item.transform.position=point;item.transform.rotation=shop.rotation*Quaternion.Euler(0,part==0?-7:11,0);
                foreach(var collider in item.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            }
        }
        string boatPath=AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("018_STAGE01_NRY_BG_002_Harbor_fishing_boat t:Prefab").First());
        var birdPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Obstacle_Real/Seagull.prefab").GetComponent<ObstacleStats>().balloon.gameObject;
        foreach(int index in new[]{12,76,164,244})
        {
            var shop=shops[Mathf.Min(index,shops.Length-1)];var boat=Instantiate(boatPath,props,Prefix+"MooredBoat_"+index);Fit(boat,8);
            var p=shop.position-shop.forward*12;p.y=-1.0f;boat.transform.SetPositionAndRotation(p,shop.rotation*Quaternion.Euler(0,12,0));
            foreach(var collider in boat.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            var bob=boat.AddComponent<AmbientHarborMotion>();bob.phase=index;bob.amplitude=.07f;
            if(index==12 || index==164)for(int n=0;n<2;n++)
            {
                var bird=UnityEngine.Object.Instantiate(birdPrefab,props);bird.name=Prefix+"Gull_"+index+"_"+n;bird.SetActive(true);
                foreach(var collider in bird.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                foreach(var body in bird.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
                bird.transform.position=p+new Vector3(n*2,7+n,0);bird.transform.localScale=Vector3.one*.7f;
                var motion=bird.AddComponent<AmbientHarborMotion>();motion.bird=true;motion.phase=index+n;motion.amplitude=.2f;
            }
        }
        foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Component>(true))
            if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("map-concepts/noryangjin-refinement-2026-09-11");
        File.WriteAllText("map-concepts/noryangjin-refinement-2026-09-11/changes.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{replacements,additions,marketDetails=decorated,mooredBoats=4,gulls=4},Newtonsoft.Json.Formatting.Indented));
        return new{replacements=3,ships=2,marketDetails=decorated,mooredBoats=4,gulls=4};
    }
    public static void RefreshRecordedCenters()
    {
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=NoryangjinMapToolWindow.Sr18MapToolScenePath)throw new InvalidOperationException("SR18 required.");
        const string path="map-concepts/noryangjin-refinement-2026-09-11/changes.json";
        var record=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));var props=GameObject.Find("Noryangjin_MapTool").transform.Find("Props");
        foreach(var row in record["replacements"]){var p=props.Find((string)row["id"]).position;row["center"]=new Newtonsoft.Json.Linq.JArray(p.x,p.y,p.z);}
        File.WriteAllText(path,record.ToString(Newtonsoft.Json.Formatting.Indented));
    }
    private static float Ground(MeshCollider[] roads,Vector3 p)
    {foreach(var road in roads)if(road.Raycast(new Ray(p+Vector3.up*3,Vector3.down),out var hit,6))return hit.point.y;throw new InvalidOperationException("No road at "+p);}
    private static bool OnRoad(MeshCollider[] roads,Vector3 p,float margin)
    {foreach(var offset in new[]{Vector3.zero,new Vector3(margin,0,margin),new Vector3(-margin,0,margin),new Vector3(margin,0,-margin),new Vector3(-margin,0,-margin)})foreach(var road in roads)if(road.Raycast(new Ray(p+offset+Vector3.up*1.5f,Vector3.down),out _,3))return true;return false;}
    private static GameObject Instantiate(string path,Transform parent,string name)
    {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException(path);var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);g.name=name;g.transform.localPosition=Vector3.zero;g.SetActive(true);return g;}
    private static void Fit(GameObject root,float size)
    {var b=HighwayAssetImporter.BoundsOf(root);root.transform.localScale*=size/Mathf.Max(b.size.x,b.size.y,b.size.z);}
}
