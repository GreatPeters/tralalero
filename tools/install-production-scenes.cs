using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class InstallProductionScenes
{
    const string AssetsRoot="Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/";
    const string Evidence="outputs/reststop-scene-integration-2026-09-25/";
    const string Marker="ProductionInstallation20260925";
    static readonly Dictionary<string,string> Props=new Dictionary<string,string>{
        {"HWY_049","R04"},{"HWY_057","R04"},{"HWY_052","Assemblies/Sign"},{"HWY_054","V04"},{"HWY_065","E05"},
        {"HWY_067","V01"},{"HWY_068","V06"},{"HWY_069","V05"},{"HWY_077","R07"},
        {"HWY_080","V06"},{"HWY_081","V02"},{"HWY_082","V03"},{"HWY_083","V05"},
        {"reststop_hall","Assemblies/Hall"},{"reststop_kiosk","Assemblies/FoodCounter"},
        {"reststop_restroom","Assemblies/Restroom"},{"reststop_picnic_shelter","Assemblies/Shelter"},
        {"reststop_fuel_canopy","Assemblies/Fuel"},{"reststop_ev_charger","R10"},{"reststop_vending","E04"},{"reststop_wayfinding","Assemblies/Sign"},
        {"snack_counter","Assemblies/FoodCounter"},{"coffee_counter","Assemblies/CoffeeCounter"},
        {"bench_001","F01"},{"table_001","F07"},{"tree_012","F04"},{"bush_003","F03"}
    };
    public static object Main(string sceneName)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        if(sceneName!="RestStop"&&sceneName!="HighWay")throw new ArgumentException(sceneName);
        var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+sceneName+".unity");
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        if(map.Find(Marker)!=null)throw new Exception("Already installed; use the validation/refinement script");
        string before=Contract(scene);File.WriteAllText(Evidence+sceneName+"-contract-before.json",before);
        var sources=map.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).Select(t=>new{t,path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)}).ToArray();
        int replaced=0,characters=0,halls=0;
        var rows=new List<object>();
        foreach(var source in sources)
        {
            if(source.t==null||source.t.GetComponentInParent<EnemyScript_space>()!=null||!source.path.EndsWith(".prefab"))continue;
            if(source.t.GetComponentInParent<Animator>()!=null)continue;
            var key=Path.GetFileNameWithoutExtension(source.path);
            if(!Props.TryGetValue(key,out var id))continue;
            if(sceneName=="HighWay"&&(key=="tree_012"||key=="bush_003"))continue;
            if(key=="reststop_hall"&&++halls%2==0)id="Assemblies/Store";
            var t=source.t;var oldBounds=BoundsIn(t,t,true);
            var visual=Replace(t,id);
            if(id=="R04")Fit(visual,oldBounds.size);
            if(id[0]=='V'&&id.Length==3)
            {
                // Moving hazards keep the original visible footprint and collider contract.
                if(t.GetComponentInParent<HighwayHazard>()!=null||t.parent.name=="Opposing_Visual_Traffic")Fit(visual,oldBounds.size);
            }
            rows.Add(new{path=PathOf(t),source=source.path,id,active=t.gameObject.activeInHierarchy});replaced++;
        }
        if(sceneName=="RestStop")
        {
            var holdout=map.GetComponentInChildren<RestStopHoldout>(true);
            foreach(var enemy in map.GetComponentsInChildren<EnemyScript_space>(true))
            {
                var source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemy.gameObject);var role=Path.GetFileNameWithoutExtension(source);
                string id=role=="ParkingMarshal"?"H01":role=="SnackChef"?"H02":role=="CoffeeVendor"?"H03":holdout!=null&&holdout.police.Any(p=>p!=null&&p.gameObject==enemy.gameObject)?"H08":null;
                if(id==null)continue;ReplaceActor(enemy.gameObject,id);characters++;
            }
            foreach(var ambient in map.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Ambient_")&&t.GetComponentInParent<EnemyScript_space>()==null).ToArray())
            {
                var role=ambient.name.Substring(8);var id=role=="ParkingMarshal"?"H01":role=="SnackChef"?"H02":role=="CoffeeVendor"?"H03":null;
                if(id==null||ambient.GetComponentsInChildren<Animator>(true).Length==0)continue;
                var parent=ambient.parent;var local=ambient.localPosition;var rotation=ambient.localRotation;bool active=ambient.gameObject.activeSelf;
                var fresh=Instance(id,parent);fresh.name=ambient.name;fresh.localPosition=local;fresh.localRotation=rotation;fresh.gameObject.SetActive(active);
                UnityEngine.Object.DestroyImmediate(ambient.gameObject);characters++;
            }
            SpaceParking(map);
        }
        var marker=new GameObject(Marker).transform;marker.SetParent(map,false);
        AddDetails(map,marker,sceneName);
        InstallTolls(map);
        Physics.SyncTransforms();
        string after=Contract(scene);File.WriteAllText(Evidence+sceneName+"-contract-after.json",after);
        if(before!=after)throw new Exception("Gameplay/collider contract changed; not saving. Inspect contract-before/after.json");
        var audit=Audit(scene);
        File.WriteAllText(Evidence+sceneName+"-installation.json",Json(new{scene=sceneName,replaced,characters,rows,audit}));
        foreach(var component in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
        AssetDatabase.SaveAssets();return new{scene=sceneName,replaced,characters,audit};
    }
    public static object Verify(string sceneName)
    {
        var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+sceneName+".unity");
        var expected=File.ReadAllText(Evidence+sceneName+"-contract-before.json");var actual=Contract(scene);
        if(expected!=actual){File.WriteAllText(Evidence+sceneName+"-contract-reopened.json",actual);throw new Exception("Saved contract differs");}
        var audit=Audit(scene);File.WriteAllText(Evidence+sceneName+"-reopened.json",Json(audit));return audit;
    }
    public static object Refine(string sceneName)
    {
        var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+sceneName+".unity");var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        var root=map.Find(Marker);if(root==null)throw new Exception("Installation required");
        if(root.Find("Detail_V05")==null){var truck=Instance("V05",root);truck.name="Detail_V05";truck.localPosition=new Vector3(25,.12f,-3);}
        var stop=root.Find("Detail_R09");stop.localPosition=new Vector3(25,.12f,2.5f);
        var police=root.Find("Detail_H08");var baton=root.Find("Detail_P02");
        if(baton!=null)
        {
            var animator=police.GetComponentInChildren<Animator>();var hand=police.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Hand.R");baton.SetParent(hand,true);baton.localPosition=Palm(animator,hand);baton.rotation=police.rotation;
            var scale=hand.lossyScale;baton.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));baton.position-=police.up*.2f;
        }
        InstallTolls(map);
        if(Contract(scene)!=File.ReadAllText(Evidence+sceneName+"-contract-before.json"))throw new Exception("Contract changed");
        foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))if(c!=null&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return Audit(scene);
    }
    public static object RefineLayout(string sceneName)
    {
        var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+sceneName+".unity");var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        var marker=map.Find(Marker);if(marker.Find("ServiceApron")!=null)throw new Exception("Layout already refined");
        if(sceneName=="HighWay")
        {
            var service=map.Find("Props/Highway_RestStop");
            var hall=service.Cast<Transform>().First(t=>t.name=="reststop_hall");
            marker.SetPositionAndRotation(hall.position+hall.rotation*new Vector3(-50,-.02f,12),hall.rotation);
            var cars=service.Cast<Transform>().Where(t=>t.name.StartsWith("Parked car")&&t.Find("ProductionVisual")!=null).ToArray();
            foreach(var row in cars.GroupBy(t=>Mathf.RoundToInt(Mathf.Repeat(t.eulerAngles.y+45,360)/90)))
            {
                var axis=hall.right;var sorted=row.OrderBy(t=>Vector3.Dot(t.position,axis)).ToArray();if(sorted.Length<2)continue;
                float min=Vector3.Dot(sorted[0].position,axis),max=Vector3.Dot(sorted.Last().position,axis);
                for(int i=0;i<sorted.Length;i++){float target=Mathf.Lerp(min,max,(float)i/(sorted.Length-1));var visual=sorted[i].Find("ProductionVisual");visual.position+=axis*(target-Vector3.Dot(sorted[i].position,axis));Record(visual);}
            }
        }
        var curve=marker.Find("Detail_R02");curve.localPosition=new Vector3(27,.02f,5.7f);
        var apron=GameObject.CreatePrimitive(PrimitiveType.Cube);apron.name="ServiceApron";apron.transform.SetParent(marker,false);apron.transform.localPosition=new Vector3(26,-.11f,-1);apron.transform.localScale=new Vector3(23,.16f,32);UnityEngine.Object.DestroyImmediate(apron.GetComponent<Collider>());
        apron.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat");
        var buildings=map.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ProductionVisual"&&t.gameObject.activeInHierarchy&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains("/Assemblies/")).ToArray();
        foreach(var building in buildings)
        {
            string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(building.gameObject);
            if(path.EndsWith("/Hall.prefab")||path.EndsWith("/Store.prefab")||path.EndsWith("/Restroom.prefab")){building.position+=Vector3.up*.065f;Record(building);}
        }
        int trees=0;
        var footprints=buildings.Where(t=>{string p=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);return p.EndsWith("/Hall.prefab")||p.EndsWith("/Store.prefab")||p.EndsWith("/Restroom.prefab");}).Select(t=>new{t,b=BoundsIn(t,t,false)}).ToArray();
        foreach(var tree in map.GetComponentsInChildren<Transform>(true).Where(t=>t.gameObject.activeInHierarchy&&PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains("/Props/tree_")).ToArray())
        {
            if(!footprints.Any(f=>{var p=f.t.InverseTransformPoint(tree.position);return p.x>f.b.min.x-1&&p.x<f.b.max.x+1&&p.z>f.b.min.z-1&&p.z<f.b.max.z+1;}))continue;
            foreach(var renderer in tree.GetComponentsInChildren<Renderer>(true)){renderer.enabled=false;Record(renderer);}trees++;
        }
        if(Contract(scene)!=File.ReadAllText(Evidence+sceneName+"-contract-before.json"))throw new Exception("Contract changed");
        foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))if(c!=null&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Evidence+sceneName+"-layout-refined.json",Json(new{scene=sceneName,treesHiddenInsideNewBuildings=trees,markerPosition=V(marker.position)}));return new{sceneName,trees};
    }
    public static object PositionHighwayService()
    {
        var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/HighWay.unity");var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var root=map.Find(Marker);
        root.position=new Vector3(-89,5.055f,177)-root.rotation*new Vector3(25,0,-3);
        var curve=root.Find("Detail_R02");curve.localPosition=new Vector3(27,.02f,-12);
        var apron=root.Find("ServiceApron");apron.localPosition=new Vector3(26,-.11f,-6);apron.localScale=new Vector3(13,.16f,27);
        var truck=root.Find("Detail_V05");var tb=BoundsIn(truck,map,false);int conflicts=0;
        foreach(var t in map.GetComponentsInChildren<Transform>(true).Where(t=>t.gameObject.activeInHierarchy&&PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains("/Buildings/")))
        {var b=BoundsIn(t,map,false);if(tb.Intersects(b))conflicts++;}
        if(conflicts>0)throw new Exception("Service truck intersects existing building");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{truckPosition=V(truck.position),buildingIntersections=conflicts};
    }
    static void InstallTolls(Transform map)
    {
        foreach(var gate in map.GetComponentsInChildren<HighwayHazard>(true).Where(h=>h.barrierArm!=null))
        {
            var arm=gate.barrierArm;if(arm.Find("ProductionArm")!=null)continue;
            foreach(var r in arm.GetComponentsInChildren<Renderer>(true)){r.enabled=false;Record(r);}
            var model=Instance("R08",arm);model.name="ProductionArm";Fit(model,new Vector3(3,.17f,.17f));model.localPosition=new Vector3(1.5f,-.085f,0);
        }
    }
    static Transform Replace(Transform root,string id)
    {
        if(root.Find("ProductionVisual")!=null)throw new Exception("Already replaced "+PathOf(root));
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))if(renderer.GetComponent<TMPro.TMP_Text>()==null){renderer.enabled=false;Record(renderer);}
        var result=Instance(id,root);result.name="ProductionVisual";
        var scale=root.lossyScale;result.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
        return result;
    }
    static Transform Instance(string id,Transform parent)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetsRoot+id+".prefab")??throw new Exception("Missing "+id);
        var t=((GameObject)PrefabUtility.InstantiatePrefab(prefab,parent)).transform;t.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);return t;
    }
    static void Fit(Transform t,Vector3 size)
    {
        var bounds=BoundsIn(t,t,false);t.localScale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
    }
    static void ReplaceActor(GameObject root,string id)
    {
        bool ranged=root.GetComponent<EnemyScript_space>().HasConfiguredProjectile;
        if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(root),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var animators=root.GetComponentsInChildren<Animator>(true);var carried=new List<Transform>();
        foreach(var animator in animators)
        {
            var oldBones=new HashSet<Transform>(animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(s=>s.bones));
            foreach(var hand in animator.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Hand.R"))
                foreach(var child in hand.Cast<Transform>().Where(t=>!oldBones.Contains(t)).ToArray()){child.SetParent(root.transform,true);carried.Add(child);}
        }
        var combat=root.GetComponent<EnemyScript_space>();var combatData=new SerializedObject(combat);
        var held=combatData.FindProperty("heldProjectile").objectReferenceValue as Transform;
        if(held!=null&&animators.Any(a=>held.IsChildOf(a.transform))){held.SetParent(root.transform,true);carried.Add(held);}
        foreach(var animator in animators){if(animator.gameObject==root)throw new Exception("Unexpected root animator");UnityEngine.Object.DestroyImmediate(animator.gameObject);}
        var body=Instance(id,root.transform);body.name="Body";body.localPosition=Vector3.down*.1f;
        var aNew=body.GetComponentInChildren<Animator>();aNew.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var handNew=body.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Hand.R");
        var palm=Palm(aNew,handNew);
        foreach(var carry in carried)
        {
            carry.SetParent(handNew,true);carry.localPosition=palm;carry.rotation=root.transform.rotation;
            var scale=handNew.lossyScale;carry.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
        }
        if(id=="H01")
        {
            foreach(var carry in carried)foreach(var r in carry.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            var tool=Instance("P03",handNew);tool.localPosition=palm;tool.rotation=root.transform.rotation;
            var scale=handNew.lossyScale;tool.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
            tool.position-=root.transform.up*.18f;
        }
        if(id=="H08")
        {
            var left=body.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Hand.L");var shield=Instance("P01",left);shield.localPosition=Palm(aNew,left);shield.rotation=root.transform.rotation;
            var scale=left.lossyScale;shield.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));shield.position-=root.transform.up*.7f;shield.position+=root.transform.forward*.15f;
        }
        var reaction=root.GetComponent<HighwayEnemyAnimation>();if(reaction!=null){reaction.animator=aNew;reaction.head=body.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Head");reaction.chest=body.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Chest");reaction.deathSeconds=Mathf.Max(reaction.deathSeconds,aNew.runtimeAnimatorController.animationClips.Single(c=>c.name=="die"||c.name.EndsWith("|die")).length);}
        if(root.GetComponentsInChildren<Animator>(true).Length!=1||combat.HasConfiguredProjectile!=ranged)throw new Exception("Actor contract "+root.name);
        if(ranged&&new SerializedObject(combat).FindProperty("throwPoint").objectReferenceValue==null)throw new Exception("Missing launch point "+root.name);
    }
    static Vector3 Palm(Animator animator,Transform hand)
    {
        var skin=animator.GetComponentInChildren<SkinnedMeshRenderer>(true);int index=Array.IndexOf(skin.bones,hand);var mesh=skin.sharedMesh;var verts=mesh.vertices;var weights=mesh.boneWeights;var p=Vector3.zero;int count=0;
        for(int i=0;i<verts.Length;i++){var w=weights[i];float weight=(w.boneIndex0==index?w.weight0:0)+(w.boneIndex1==index?w.weight1:0)+(w.boneIndex2==index?w.weight2:0)+(w.boneIndex3==index?w.weight3:0);if(weight>.6f){p+=mesh.bindposes[index].MultiplyPoint3x4(verts[i]);count++;}}
        if(count==0)throw new Exception("No hand weights");return p/count;
    }
    static void SpaceParking(Transform map)
    {
        foreach(var block in map.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("RestStop_Block_")))
        {
            var cars=block.Cast<Transform>().Where(t=>t.name.StartsWith("ParkProp_")&&t.Find("ProductionVisual")!=null).ToArray();
            foreach(var side in cars.GroupBy(t=>Mathf.Sign(t.localPosition.x)))foreach(var car in side)
            {
                var visual=car.Find("ProductionVisual");var delta=block.TransformVector(Vector3.forward*(car.localPosition.z*.24f));visual.position+=delta;
            }
        }
    }
    static void AddDetails(Transform map,Transform root,string scene)
    {
        Vector3 origin=scene=="RestStop"?new Vector3(0,0,-365):map.Find("Props/Highway_RestStop").position;
        root.position=origin;
        if(scene=="HighWay")root.rotation=map.Find("Props/Highway_RestStop").rotation;
        Action<string,float,float,float,float> add=(id,x,y,z,yaw)=>{var t=Instance(id,root);t.name="Detail_"+id;t.localPosition=new Vector3(x,y,z);t.localRotation=Quaternion.Euler(0,yaw,0);};
        // Service-side approach and fixtures stay outside the traversable center lane.
        add("R01",25,-.02f,-3,0);add("R02",34,-.02f,10,0);add("R03",20,.03f,-3,0);add("R09",25,.12f,2.5f,0);add("V05",25,.12f,-3,0);
        add("R11",19,0,-8,0);add("R07",19,0,-10,0);add("R08",22,1.15f,-10,0);add("R12",19,.02f,-4,0);
        add("R04",31,0,-14,0);add("R10",29,0,-9,180);add("E05",35,0,-10,0);
        add("H08",18,0,-13,220);add("P02",18.6f,1,-13,0);add("H01",20,0,12,190);
    }
    static string Contract(Scene scene)
    {
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var enemies=all.Select(t=>t.GetComponent<EnemyScript_space>()).Where(e=>e!=null).OrderBy(e=>PathOf(e.transform)).Select(e=>new{path=PathOf(e.transform),position=V(e.transform.position),scale=V(e.transform.lossyScale),ranged=e.HasConfiguredProjectile,fields=PrimitiveFields(e)}).ToArray();
        var colliders=all.SelectMany(t=>t.GetComponents<Collider>()).Where(c=>c.GetComponentInParent<Animator>()==null&&c.GetComponent<SimpleProjectile>()==null).OrderBy(c=>PathOf(c.transform)).Select(c=>new{path=PathOf(c.transform),type=c.GetType().Name,position=V(c.transform.position),rotation=V(c.transform.eulerAngles),scale=V(c.transform.lossyScale),fields=PrimitiveFields(c)}).ToArray();
        var cameras=all.Select(t=>t.GetComponent<Camera>()).Where(c=>c!=null).Select(c=>new{path=PathOf(c.transform),position=V(c.transform.position),rotation=V(c.transform.eulerAngles),c.fieldOfView}).ToArray();
        return Json(new{enemies,colliders,cameras,bonuses=all.Count(t=>t.GetComponent<BonusWallChoicePair>()!=null)});
    }
    static object PrimitiveFields(UnityEngine.Object component)
    {
        var fields=new SortedDictionary<string,string>();var so=new SerializedObject(component);var p=so.GetIterator();
        while(p.NextVisible(true))
        {
            string value=null;
            switch(p.propertyType){case SerializedPropertyType.Integer:value=p.longValue.ToString();break;case SerializedPropertyType.Float:value=p.doubleValue.ToString("R",System.Globalization.CultureInfo.InvariantCulture);break;case SerializedPropertyType.Boolean:value=p.boolValue.ToString();break;case SerializedPropertyType.String:value=p.stringValue;break;case SerializedPropertyType.Enum:value=p.intValue.ToString();break;case SerializedPropertyType.Vector3:value=p.vector3Value.ToString("R");break;case SerializedPropertyType.Vector2:value=p.vector2Value.ToString("R");break;}
            if(value!=null)fields[p.propertyPath]=value;
        }return fields;
    }
    static object Audit(Scene scene)
    {
        var roots=scene.GetRootGameObjects();var all=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        var enemies=roots.SelectMany(g=>g.GetComponentsInChildren<EnemyScript_space>(true)).ToArray();
        var bad=enemies.Where(e=>e.GetComponentsInChildren<Animator>(true).Length!=1||e.HasConfiguredProjectile&&new SerializedObject(e).FindProperty("throwPoint").objectReferenceValue==null).Select(e=>e.name).ToArray();
        var invalidMaterials=all.SelectMany(t=>t.GetComponents<Renderer>()).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Where(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name.Contains("InternalError"))).Select(r=>PathOf(r.transform)).ToArray();
        var coverage=all.Where(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).Select(t=>new{t,path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)}).Where(x=>x.path.StartsWith(AssetsRoot)&&!x.path.Contains("/Assemblies/")).GroupBy(x=>Path.GetFileNameWithoutExtension(x.path)).Select(g=>new{id=g.Key,total=g.Count(),active=g.Count(x=>x.t.gameObject.activeInHierarchy)}).OrderBy(x=>x.id).ToArray();
        if(missing>0||bad.Length>0||invalidMaterials.Length>0)throw new Exception("Audit failed: scripts="+missing+", actors="+string.Join(",",bad)+", materials="+invalidMaterials.Length);
        return new{missingScripts=missing,enemies=enemies.Length,ranged=enemies.Count(e=>e.HasConfiguredProjectile),singleAnimator=true,invalidMaterials,coverage};
    }
    static Bounds BoundsIn(Transform root,Transform space,bool includeDisabled)
    {
        var b=new Bounds();bool first=true;foreach(var r in root.GetComponentsInChildren<Renderer>(true).Where(r=>includeDisabled||r.enabled))
        {var v=r.bounds;foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1}){var p=space.InverseTransformPoint(v.center+Vector3.Scale(v.extents,new Vector3(x,y,z)));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;
    }
    static void Record(UnityEngine.Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    static float[] V(Vector3 v)=>new[]{(float)Math.Round(v.x,4),(float)Math.Round(v.y,4),(float)Math.Round(v.z,4)};
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
}
