using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One-shot authoring command; refine the saved scene after the first successful build.
public static class RestStopSceneBuilder
{
    private const string Record="map-concepts/skins-reststop-2026-09-12/reststop-scene.json";
    private static Transform root;
    private static Material asphalt,paint,concrete,earth;
    private static TMP_FontAsset font;
    private static readonly List<object> placements=new();
    public static object Build()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=HighwaySceneBuilder.ScenePath||scene.isDirty)throw new InvalidOperationException("Clean HighWay in Edit Mode required.");
        string[] keys={"reststop_hall","reststop_fuel_canopy","reststop_ev_charger","reststop_restroom","reststop_kiosk","reststop_picnic_shelter","reststop_vending","reststop_wayfinding"};
        foreach(string key in keys)if(AssetDatabase.LoadAssetAtPath<GameObject>(RestStopAssetImporter.Prefabs+"/"+key+".prefab")==null)throw new InvalidOperationException("Rest-stop asset not ready: "+key);
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var props=map.Find("Props");
        if(props.Find("Highway_RestStop")!=null)throw new InvalidOperationException("Rest stop already exists; refine recorded objects.");
        EditorSceneManager.SaveScene(scene,"tmp/backups/skins-progression-2026-09-12/before-reststop.unity",true);
        asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat");paint=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/RoadPaint.mat");concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Concrete.mat");earth=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Ground.mat");font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/JH/Font/쩡야공유/GmarketSansTTFBold SDF2.asset");
        root=Group(props,"Highway_RestStop");placements.Clear();
        var relocated=new List<object>();int treeIndex=0,cityIndex=0;
        foreach(var item in props.Cast<Transform>().Where(t=>t!=root).ToArray())
        {
            if(item.position.x< -115||item.position.x>85||item.position.z<125||item.position.z>213)continue;
            Vector3 before=item.position;
            if(item.name.StartsWith("Highway_City_",StringComparison.Ordinal))item.position=new Vector3(item.position.x,-6.5f,90-(cityIndex++%3)*18);
            else if(item.name.StartsWith("Highway_Tree_",StringComparison.Ordinal))item.position=new Vector3(treeIndex%2==0?-112:77,5,139+(treeIndex++%6)*11);
            else continue;
            relocated.Add(new{name=item.name,before=V(before),after=V(item.position)});
        }
        Terrain(map.Find("Roads"));
        Cube("Main forecourt",new Vector3(-8,5.035f,155),new Vector3(132,.07f,17),concrete);
        foreach(float x in new[]{-110f,70f})Cube("Kerb",new Vector3(x,5.1f,164),new Vector3(.3f,.2f,68),concrete);
        Cube("Rear kerb",new Vector3(-20,5.1f,129),new Vector3(180,.2f,.3f),concrete);
        var hall=Prop("reststop_hall",new Vector3(-31,5.075f,138),0);Sign(hall,"바다쉼 휴게소",.74f,7,12);
        var dining=Prop("reststop_hall",new Vector3(-13,5.075f,138),0);Sign(dining,"바다쉼 식당",.74f,7,12);
        var kiosk=Prop("reststop_kiosk",new Vector3(26,5.075f,145),0);Sign(kiosk,"바다 간식",.79f,4,5);
        var restroom=Prop("reststop_restroom",new Vector3(52,5.075f,141),0);Sign(restroom,"화장실",.75f,4,6);
        foreach(float x in new[]{-73f,-62f})
        {
            Prop("reststop_picnic_shelter",new Vector3(x,5.075f,151),0);
            Furniture("table_001",new Vector3(x,5.075f,151),2,0);
            Furniture("bench_001",new Vector3(x,5.075f,152),2,180);
            Furniture("bench_001",new Vector3(x,5.075f,150),2,0);
        }
        Prop("reststop_vending",new Vector3(-45,5.075f,155),0);
        var fuel=Prop("reststop_fuel_canopy",new Vector3(-85,5.02f,183),0);Sign(fuel,"주유소",.88f,7,10);
        for(int i=0;i<4;i++)Prop("reststop_ev_charger",new Vector3(14.4f+i*4.2f,5.02f,178.8f),0);
        var directory=Prop("reststop_wayfinding",new Vector3(-98,5.02f,206),0);Sign(directory,"바다쉼",.77f,2.7f,2);Sign(directory,"충전 · 식사 · 쉼터",.44f,1.2f,2.3f);foreach(var label in directory.GetComponentsInChildren<TextMeshPro>())label.color=new Color(.035f,.12f,.14f);directory.transform.rotation=Quaternion.Euler(0,-45,0);
        int bay=0,cars=0;
        for(int row=0;row<2;row++)for(int i=0;i<12;i++)
        {
            float x=-15+i*4.2f,z=row==0?183:197;
            for(int side=-1;side<=1;side+=2)Cube("Parking line "+(++bay),new Vector3(x+side*1.7f,5.025f,z),new Vector3(.11f,.016f,6.3f),paint);
            Cube("Parking end",new Vector3(x,5.025f,z+(row==0?-3.2f:3.2f)),new Vector3(3.4f,.016f,.11f),paint);
            if((i+row*3)%3==1)continue;
            var car=Instance(HighwayAssetImporter.Prefabs+"/HWY_"+new[]{67,81,82}[(i+row)%3].ToString("D3")+".prefab","Parked car "+(++cars));car.transform.SetPositionAndRotation(new Vector3(x,5.035f,z),Quaternion.Euler(0,row==0?180:0,0));
        }
        foreach(float x in new[]{-100f,-55f,0f,55f}){var light=Instance(HighwayAssetImporter.Prefabs+"/HWY_065.prefab","Rest-stop light");light.transform.position=new Vector3(x,5,166);}
        foreach(var point in new[]{new Vector3(-107,5,134),new Vector3(-108,5,166),new Vector3(71,5,133),new Vector3(73,5,162)})
        {
            var tree=Instance("Assets/ithappy/Megacity/Prefabs/Props/tree_012.prefab","Rest-stop tree");var b=HighwayAssetImporter.BoundsOf(tree);tree.transform.localScale*=5.5f/b.size.y;b=HighwayAssetImporter.BoundsOf(tree);tree.transform.position+=point-new Vector3(b.center.x,b.min.y,b.center.z);
        }
        var openedRails=new List<string>();
        foreach(var road in map.Find("Roads").Cast<Transform>().ToArray())foreach(var item in road.Cast<Transform>().ToArray())
        {
            if((item.name!="Guardrail"&&item.name!="Post")||item.position.z>=220||item.position.z<211)continue;
            if(Mathf.Abs(item.position.x+90)>10.1f&&Mathf.Abs(item.position.x-70)>10.1f)continue;
            item.gameObject.SetActive(false);openedRails.Add(road.name+"/"+item.name);
        }
        foreach(var collider in root.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
        foreach(var obj in root.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(obj.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
        foreach(var component in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        var result=new{scene=scene.name,placements,parkingSpaces=24,parkedCars=cars,chargers=4,openedRails,relocated,routeUnchanged=true};File.WriteAllText(Record,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    private static void Terrain(Transform roads)
    {
        const string folder="Assets/ShooterSurvival/Models/Highway/RestStop/Environment";Directory.CreateDirectory(folder);
        var center=new Vector3(-20,0,171);var mesh=new Mesh{name="Rest-stop embankment"};
        mesh.vertices=new[]{new Vector3(-90,4.95f,-42),new Vector3(90,4.95f,-42),new Vector3(-90,4.95f,42),new Vector3(90,4.95f,42),new Vector3(-104,-6.5f,-55),new Vector3(104,-6.5f,-55),new Vector3(-104,-6.5f,55),new Vector3(104,-6.5f,55)};
        mesh.triangles=new[]{0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"/Embankment.asset");
        var bank=Group(root,"Landscaped embankment");bank.position=center;bank.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;bank.gameObject.AddComponent<MeshRenderer>().sharedMaterial=earth;
        var deck=new Mesh{name="Rest-stop parking deck"};deck.vertices=new[]{new Vector3(-90,0,-42),new Vector3(90,0,-42),new Vector3(-90,0,42),new Vector3(90,0,42)};deck.uv=new[]{new Vector2(0,0),new Vector2(36,0),new Vector2(0,17),new Vector2(36,17)};deck.triangles=new[]{0,2,1,1,2,3};deck.RecalculateNormals();deck.RecalculateBounds();AssetDatabase.CreateAsset(deck,folder+"/Deck.asset");
        var surface=Group(roads,"Highway_RestStop_Deck");surface.position=center+Vector3.up*5;surface.gameObject.AddComponent<MeshFilter>().sharedMesh=deck;surface.gameObject.AddComponent<MeshRenderer>().sharedMaterial=asphalt;surface.gameObject.AddComponent<MeshCollider>().sharedMesh=deck;
        foreach(float x in new[]{-90f,70f})
        {
            Cube("Access apron",new Vector3(x,4.975f,207),new Vector3(20,.05f,12),asphalt);
            var access=Group(roads,"Highway_RestStop_Access");access.position=new Vector3(x,5,207);access.localScale=new Vector3(20f/180f,1,12f/84f);access.gameObject.AddComponent<MeshCollider>().sharedMesh=deck;
        }
    }
    private static GameObject Prop(string key,Vector3 point,float yaw)
    {
        var obj=Instance(RestStopAssetImporter.Prefabs+"/"+key+".prefab",key);obj.transform.SetPositionAndRotation(point,Quaternion.Euler(0,yaw,0));placements.Add(new{key,position=V(point),yaw});return obj;
    }
    private static GameObject Instance(string path,string name){var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root);obj.name=name;obj.SetActive(true);return obj;}
    private static void Furniture(string key,Vector3 point,float width,float yaw)
    {
        var obj=Instance("Assets/ithappy/Megacity/Prefabs/Props/"+key+".prefab","Rest-stop "+key);var bounds=HighwayAssetImporter.BoundsOf(obj);
        obj.transform.localScale*=width/Mathf.Max(bounds.size.x,bounds.size.z);obj.transform.rotation=Quaternion.Euler(0,yaw,0);bounds=HighwayAssetImporter.BoundsOf(obj);
        obj.transform.position+=point-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
    }
    private static Transform Group(Transform parent,string name){var obj=new GameObject(name);obj.transform.SetParent(parent,false);return obj.transform;}
    private static void Cube(string name,Vector3 point,Vector3 size,Material material){var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(root,false);obj.transform.position=point;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());}
    private static void Sign(GameObject building,string text,float heightFraction,float fontSize,float width)
    {
        var renderers=building.GetComponentsInChildren<Renderer>(true).Where(r=>r.GetComponent<TextMeshPro>()==null).ToArray();var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
        var obj=new GameObject("Sign "+text);obj.transform.SetParent(building.transform,false);obj.transform.position=new Vector3(bounds.center.x,Mathf.Lerp(bounds.min.y,bounds.max.y,heightFraction),bounds.max.z+.035f);obj.transform.rotation=Quaternion.Euler(0,180,0);
        var label=obj.AddComponent<TextMeshPro>();label.font=font;label.text=text;label.fontSize=fontSize;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.96f,.91f,.76f);label.rectTransform.sizeDelta=new Vector2(width,2);label.enableAutoSizing=true;label.fontSizeMin=fontSize*.65f;label.fontSizeMax=fontSize;
    }
    private static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
}
