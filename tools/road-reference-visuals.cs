using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using TMPro;
using Object = UnityEngine.Object;

// Execute named entry points through official Unity Pipeline run_script.
// Outside Assets so inspection/authoring does not cause a domain reload.
public static class RoadReferenceVisuals
{
    public const string Record = "map-concepts/road-reference-visuals-2026-09-13";
    public const string Previews = "tmp/image-previews/road-reference-visuals-2026-09-13";
    public const string Art = "Assets/ShooterSurvival/Models/Chapters/ReferenceScenery";
    public const string GroupName = "Reference_Scenery_20260913";
    public const string Prefs = Record + "/before/playerprefs.tsv";
    static readonly Vector3[] RestPoints = { new(0,0,-420),new(0,0,0),new(240,0,0),new(240,0,440),new(-100,0,440),new(-100,0,780),new(220,0,780) };

    public static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    static void SavedEditMode()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s=>s.isDirty))
            throw new InvalidOperationException("Saved Edit Mode required; preserve unsaved changes first.");
    }
    public static string Prepare()
    {
        SavedEditMode();
        if(Directory.Exists(Record+"/before")) throw new InvalidOperationException("Do not overwrite this run's backup.");
        Directory.CreateDirectory(Record+"/before"); Directory.CreateDirectory(Previews);
        var files=new[]{HighwaySceneBuilder.ScenePath,RestStopChapterBuilder.ScenePath,"Assets/ShooterSurvival/GameData/Editor/Data.xlsx","Assets/ShooterSurvival/Resources/GameData/Data.bytes","Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity"};
        var rows=new List<object>();
        foreach(var path in files) { File.Copy(path,Record+"/before/"+Path.GetFileName(path)); rows.Add(new {path,sha256=Hash(path)}); }
        var prefs=ChapterPlaytestPreferences.SnapshotAt(Prefs);
        File.WriteAllText(Record+"/before/files.json",Json(rows));
        return Json(prefs);
    }
    public static string Hash(string path) { using var sha=SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-",""); }
    static Transform Map() => SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
    public static void Sample(float distance, out Vector3 p, out Vector3 f, bool bypass=false)
    {
        var route=Map().GetComponent<HighwayRoute>();
        if(route!=null) { route.Sample(distance,bypass,out p,out f); return; }
        for(int i=0;i<RestPoints.Length-1;i++) { var delta=RestPoints[i+1]-RestPoints[i]; float length=delta.magnitude; if(distance<=length || i==RestPoints.Length-2) { f=delta.normalized; p=RestPoints[i]+f*Mathf.Clamp(distance,0,length); return; } distance-=length; }
        throw new InvalidOperationException("No route");
    }
    public static string Capture(string label,float distance)
    {
        var source=Camera.main;
        Sample(distance,out var p,out var f);
        var go=new GameObject("Reference review camera") {hideFlags=HideFlags.HideAndDontSave};
        var camera=go.AddComponent<Camera>(); camera.CopyFrom(source); camera.enabled=false;
        camera.transform.SetPositionAndRotation(p+Quaternion.LookRotation(f)*source.transform.localPosition,Quaternion.LookRotation(f)*source.transform.localRotation);
        camera.aspect=9f/16; camera.cullingMask &= ~(1<<31);
        var target=new RenderTexture(720,1280,24,RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture=target;
        string path=Previews+"/"+SceneManager.GetActiveScene().name+"-"+label+"-"+distance.ToString("0000")+".png";
        if(File.Exists(path)) throw new InvalidOperationException("Preserve capture: "+path);
        var active=RenderTexture.active;
        try { camera.Render(); CosmeticPresentationBuilder.Save(target,path); }
        finally { RenderTexture.active=active; camera.targetTexture=null; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(go); }
        return path;
    }
    public static string Inspect()
    {
        var map=Map();
        var data=new { scene=SceneManager.GetActiveScene().name, roots=map.Cast<Transform>().Select(t=>new{t.name,t.childCount}).ToArray(), blocks=map.Find("Props").Cast<Transform>().Where(t=>t.name.StartsWith("RestStop_Block")).Take(3).Select(t=>new{t.name,position=t.position.ToString(),children=t.Cast<Transform>().Select(c=>new{c.name,position=c.localPosition.ToString(),size=HighwayAssetImporter.BoundsOf(c.gameObject).size.ToString()}).ToArray()}).ToArray(), hall=map.Find("FoodHall_Holdout")?.localScale.ToString() };
        return Json(data);
    }

    static string GameplayState()
    {
        var map=Map(); var roots=map.Cast<Transform>().Where(t=>t.name=="Roads" || t.name=="Enemies" || t.name=="Bonuses" || t.name.EndsWith("EnemyTargets") || t.name.EndsWith("StageExit") || t.name=="Oncoming_Traffic").ToArray();
        var transforms=roots.SelectMany(t=>t.GetComponentsInChildren<Transform>(true)).Concat(map.Find("Props").Cast<Transform>().Where(t=>t.name.StartsWith("HWY_") || t.name.StartsWith("RST_")).SelectMany(t=>t.GetComponentsInChildren<Transform>(true)));
        var rows=transforms.Select(t=>GlobalObjectId.GetGlobalObjectIdSlow(t)+"|"+t.gameObject.activeSelf+"|"+t.localPosition.ToString("R")+"|"+t.localRotation.ToString("R")+"|"+t.localScale.ToString("R")).ToList();
        foreach(var c in map.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c is HighwayRoute || c is HighwayOncomingTraffic || c is RestStopHoldout || c is EnemyEventController || c is EncounterPlacementController || c is HighwayHazard))
            rows.Add(GlobalObjectId.GetGlobalObjectIdSlow(c)+"|"+EditorJsonUtility.ToJson(c));
        var player=Object.FindFirstObjectByType<PlayerScript>(); rows.Add(EditorJsonUtility.ToJson(player)); rows.Add(EditorJsonUtility.ToJson(Camera.main.transform));
        return string.Join("\n",rows.OrderBy(r=>r,StringComparer.Ordinal));
    }
    static Material green,white,cream,paver,red,blue;
    static Material Mat(string name,Color color,float smooth=.22f)
    {
        string path=Art+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true}; m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",smooth); AssetDatabase.CreateAsset(m,path); }
        GeneratedStylizedSurface.Apply(m);return m;
    }
    static void Palette()
    {
        Directory.CreateDirectory(Art); AssetDatabase.Refresh();
        green=Mat("ServiceGreen",new Color(.035f,.34f,.14f)); white=Mat("Porcelain",new Color(.89f,.91f,.86f));
        cream=Mat("WarmStone",new Color(.66f,.65f,.56f)); paver=Mat("Sidewalk",new Color(.46f,.49f,.47f));
        red=Mat("VendingRed",new Color(.72f,.055f,.025f)); blue=Mat("SignBlue",new Color(.025f,.16f,.30f));
    }
    static Transform Group(Transform parent,string name,Vector3 local=default)
    { var t=new GameObject(name).transform; t.SetParent(parent,false); t.localPosition=local; return t; }
    static Transform Cube(Transform parent,string name,Vector3 pos,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=size;
        Object.DestroyImmediate(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial=material; return g.transform;
    }
    static Transform Instance(string path,Transform parent,string name,Vector3 local,float yaw=0,float height=0)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(prefab==null) throw new FileNotFoundException(path);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent); g.name=name;
        g.transform.localPosition=Vector3.zero; g.transform.localRotation=Quaternion.identity;
        if(height>0)g.transform.localScale*=height/HighwayAssetImporter.BoundsOf(g).size.y;
        // Keep the importer-created visual child axes and offset intact.
        g.transform.localRotation=Quaternion.Euler(0,yaw,0); g.transform.localPosition=local;
        foreach(var c in g.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        return g.transform;
    }
    static Transform Prop(int id,Transform parent,Vector3 local,float yaw=0) => Instance(HighwayAssetImporter.Prefabs+"/HWY_"+id.ToString("D3")+".prefab",parent,"HighwayProp_"+id,local,yaw);
    static Transform Rest(string key,Transform parent,Vector3 local,float yaw=0) => Instance(RestStopAssetImporter.Prefabs+"/"+key+".prefab",parent,key,local,yaw);
    static Transform Furniture(string key,Transform parent,Vector3 local,float yaw,float height) => Instance("Assets/ithappy/Megacity/Prefabs/Props/"+key+".prefab",parent,key,local,yaw,height);
    static Transform At(Transform parent,string name,float d)
    { Sample(d,out var p,out var f); var t=Group(parent,name); t.SetPositionAndRotation(p,Quaternion.LookRotation(f)); return t; }
    static void Label(Transform parent,string text,Vector3 pos,float width,float height,float yaw=0)
    {
        var t=Group(parent,"Service label "+text,pos); t.localRotation=Quaternion.Euler(0,yaw,0);
        var label=t.gameObject.AddComponent<TextMeshPro>(); label.font=GameUIFont.Load(); label.text=text; label.color=Color.white; label.alignment=TextAlignmentOptions.Center;
        label.enableAutoSizing=true; label.fontSizeMin=3; label.fontSizeMax=15; label.rectTransform.sizeDelta=new Vector2(width,height);
    }
    static void Daylight()
    {
        string path=Art+"/BlueDaySky.mat"; var sky=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(sky==null) { sky=new Material(Shader.Find("Skybox/Cubemap")); AssetDatabase.CreateAsset(sky,path); }
        sky.shader=Shader.Find("Skybox/Cubemap");
        sky.SetTexture("_Tex",AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/ithappy/Megacity/Skyboxes/Skybox_Textures/Skybox_5.png")); sky.SetFloat("_Exposure",1.05f); sky.SetColor("_Tint",new Color(.42f,.49f,.59f)); EditorUtility.SetDirty(sky);
        RenderSettings.skybox=sky; RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogColor=new Color(.57f,.74f,.83f); RenderSettings.fogStartDistance=180; RenderSettings.fogEndDistance=430;
        RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.64f,.74f,.85f); RenderSettings.ambientEquatorColor=new Color(.49f,.56f,.58f); RenderSettings.ambientGroundColor=new Color(.29f,.32f,.30f);
        var sun=SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="MapTool_DirectionalLight").GetComponent<Light>();
        sun.color=new Color(1,.96f,.87f); sun.intensity=1.55f; sun.transform.rotation=Quaternion.Euler(48,-32,0); sun.shadows=LightShadows.Soft;
    }
    static void Planter(Transform parent,Vector3 pos,float size=1.7f)
    {
        var box=Group(parent,"Planted sidewalk",pos);
        Cube(box,"Stone planter",new Vector3(0,.35f,0),new Vector3(size,.7f,size),cream);
        var shrub=Furniture("bush_001",box,new Vector3(0,.60f,0),0,size*.48f);
        shrub.localScale=new Vector3(shrub.localScale.x*.65f,shrub.localScale.y,shrub.localScale.z*1.5f);
    }
    static void Save(string before,Transform added)
    {
        if(GameplayState()!=before) throw new InvalidOperationException("Protected gameplay state changed; do not save.");
        if(added.GetComponentsInChildren<Collider>(true).Length!=0) throw new InvalidOperationException("Decorative collider detected");
        foreach(var c in Map().GetComponentsInChildren<Component>(true)) if(c!=null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        var scene=SceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed"); AssetDatabase.SaveAssets();
        File.WriteAllText(Record+"/"+scene.name+"-authoring.json",Json(new {scene=scene.name,gameplayUnchanged=true,newRenderers=added.GetComponentsInChildren<Renderer>(true).Length,newColliders=0,sceneHash=Hash(scene.path)}));
    }
    public static string ApplyHighway()
    {
        SavedEditMode(); if(SceneManager.GetActiveScene().name!="HighWay")throw new InvalidOperationException("HighWay required");
        var map=Map(); if(map.Find(GroupName)!=null)throw new InvalidOperationException("Already applied; refine explicitly");
        string before=GameplayState(); File.WriteAllText(Record+"/before/HighWay-gameplay.txt",before);
        Palette(); var added=Group(map,GroupName); var route=map.GetComponent<HighwayRoute>(); var props=map.Find("Props");
        // Retain previous pieces for recovery, hiding only acoustic wall scenery.
        foreach(Transform t in props) if(t.name.StartsWith("Highway_Polish_NoiseWall_"))t.gameObject.SetActive(false);
        int serial=0;
        for(float d=8;d<route.length-12;d+=12)
        {
            bool fork=route.forks.Any(f=>d>f.start-24 && d<f.end+24);
            var section=At(added,"Acoustic edge "+(++serial),d);
            foreach(int side in new[]{-1,1})
            {
                if(side<0 && fork)continue;
                var wall=Prop(64,section,new Vector3(side*9.5f,0,0),90);
                wall.localScale=new Vector3(1.51f,.55f,.62f);
                Cube(section,"Green acoustic band",new Vector3(side*9.05f,1.10f,0),new Vector3(.07f,.42f,12.1f),green);
            }
        }
        for(float d=15;d<route.length-20;d+=24)
        {
            var section=At(added,"Roadside grove",d); bool fork=route.forks.Any(f=>d>f.start-45&&d<f.end+45);
            foreach(int side in new[]{-1,1})
            {
                float x=side*(side<0&&fork?65:23+(serial%3)*4);
                Furniture(serial%2==0?"tree_016":"tree_017",section,new Vector3(x,-6.5f,0),serial*37%360,17+serial%4);
                if(!fork)Furniture("bush_002",section,new Vector3(side*15,-.8f,8),serial*19%360,3.8f);
            }
            serial++;
        }
        var buildings=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ithappy/Megacity/Prefabs/Buildings/Skyscrapers"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.Contains("eiffel")).OrderBy(p=>p).ToArray();
        for(float d=40;d<route.length-40;d+=95)
        {
            var section=At(added,"City backdrop",d);
            foreach(int side in new[]{-1,1}) Instance(buildings[(serial++*7)%buildings.Length],section,"Distant city",new Vector3(side*100,-6.5f,22),side*90,58+(serial%4)*10);
        }
        foreach(float d in new[]{30f,530,1210,2000}) { var section=At(added,"Speed camera cluster",d); Prop(53,section,new Vector3(8,0,0),180); Prop(56,section,new Vector3(-8.2f,0,6),90); }
        Daylight(); Save(before,added); return "HighWay scenery saved; protected gameplay unchanged.";
    }

    public static string ApplyRestStop()
    {
        SavedEditMode(); if(SceneManager.GetActiveScene().name!="RestStop")throw new InvalidOperationException("RestStop required");
        var map=Map(); if(map.Find(GroupName)!=null)throw new InvalidOperationException("Already applied; refine explicitly");
        string before=GameplayState(); File.WriteAllText(Record+"/before/RestStop-gameplay.txt",before);
        Palette(); var added=Group(map,GroupName); var props=map.Find("Props");
        // Recompose only decorative opening blocks. Their original hierarchy is retained.
        foreach(Transform block in props)
            if(block.name.StartsWith("RestStop_Block_") && block.position.z<0 && Mathf.Abs(block.position.x)<1)
                foreach(Transform item in block) if(item.name!="Tree" && !item.name.StartsWith("Ambient_") && item.name!="ParkProp_65") item.gameObject.SetActive(false);
        foreach(float d in new[]{32f,112,192,272,352}) ServiceCourt(added,d);
        // Consistent sidewalk furnishing throughout the six existing zones, clear of corners and holdout.
        float cursor=0;
        for(int i=0;i<RestPoints.Length-1;i++)
        {
            float length=(RestPoints[i+1]-RestPoints[i]).magnitude;
            for(float local=20;local<length-25;local+=16)
            {
                float d=cursor+local; if(d>825 && d<980)continue;
                var section=At(added,"Planted verge",d);
                foreach(int side in new[]{-1,1})
                {
                    Cube(section,"Raised pavement",new Vector3(side*9.9f,.04f,0),new Vector3(5.5f,.16f,16),paver);
                    Cube(section,"Curb",new Vector3(side*7.25f,.15f,0),new Vector3(.25f,.32f,16),white);
                    if((int)local%32==20) { Planter(section,new Vector3(side*8.3f,.14f,0)); Furniture("tree_016",section,new Vector3(side*24,0,6),i*30,9); }
                }
            }
            cursor+=length;
        }
        // Existing shop street stays in place, with added service colour and readable fascia.
        foreach(var building in props.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="reststop_hall" && t.gameObject.activeInHierarchy))
            Cube(building,"Reference green fascia",new Vector3(0,3.25f,4.82f),new Vector3(14,.65f,.14f),green);
        RefineHall(added,map.Find("FoodHall_Holdout"));
        Daylight(); Save(before,added); return "RestStop scenery saved; protected gameplay unchanged.";
    }
    static void ServiceCourt(Transform parent,float distance)
    {
        var court=At(parent,"Service courtyard "+distance,distance);
        var fuel=Rest("reststop_fuel_canopy",court,new Vector3(-17,.13f,5),90);
        Cube(fuel,"Green canopy fascia",new Vector3(0,4.83f,3.5f),new Vector3(12.2f,.64f,.16f),green);
        Cube(fuel,"Green canopy fascia",new Vector3(0,4.83f,-3.5f),new Vector3(12.2f,.64f,.16f),green);
        Label(fuel,"주유  FUEL",new Vector3(0,4.85f,3.61f),8,.55f,180);
        var hall=Rest("reststop_hall",court,new Vector3(17.5f,.13f,8),-90);
        Cube(hall,"Green store fascia",new Vector3(0,3.25f,4.83f),new Vector3(14,.68f,.16f),green);
        Label(hall,"휴게소  MART",new Vector3(0,3.27f,4.94f),10,.64f,180);
        var kiosk=Rest("reststop_kiosk",court,new Vector3(11.1f,.13f,-11),-90);
        Cube(kiosk,"Snack fascia",new Vector3(0,2.27f,1.6f),new Vector3(3.5f,.38f,.12f),green);
        var vending=Rest("reststop_vending",court,new Vector3(9.5f,.13f,0),-90);
        Cube(vending,"Red vending side",new Vector3(.68f,1.04f,0),new Vector3(.08f,2.05f,1),red);
        Rest("reststop_ev_charger",court,new Vector3(8.5f,.13f,-20),-90);
        var umbrella=Furniture("umbrella_001",court,new Vector3(10.2f,.13f,19),0,3.1f);
        Furniture("table_001",court,new Vector3(10.2f,.13f,19),0,.85f);
        Furniture("bench_001",court,new Vector3(8.8f,.13f,19),90,.65f);
        Furniture("trash_003",court,new Vector3(-8.3f,.13f,-18),0,1.2f);
        var bus=Prop(80,court,new Vector3(-16,.13f,25)); bus.localScale*=.78f;
        Prop(82,court,new Vector3(-12,.13f,-15),90);
        Prop(67,court,new Vector3(15,.13f,-25),-90);
        foreach(int side in new[]{-1,1})foreach(float z in new[]{-27f,-7,13,31})
        {
            var bollard=Prop(76,court,new Vector3(side*7.65f,.14f,z)); bollard.localScale*=.55f;
        }
        Planter(court,new Vector3(8.4f,.14f,10)); Planter(court,new Vector3(-8.4f,.14f,15));
        var sign=Group(court,"Service directory",new Vector3(-8.5f,0,-6));
        Cube(sign,"Directory pole",new Vector3(0,2.4f,0),new Vector3(.15f,4.8f,.15f),paver);
        Cube(sign,"Directory plate",new Vector3(0,4.5f,0),new Vector3(3.2f,1.7f,.15f),blue);
        Label(sign,"P  주유\n식당 · 편의점",new Vector3(0,4.5f,-.09f),3,1.5f);
    }
    static void RefineHall(Transform added,Transform hall)
    {
        var decor=Group(added,"Food hall finish"); decor.SetPositionAndRotation(hall.position,hall.rotation);
        foreach(Transform item in hall)
        {
            var renderer=item.GetComponent<Renderer>(); if(renderer==null || item.GetComponent<TMP_Text>()!=null)continue;
            if(item.name=="Floor tile joint")renderer.sharedMaterial=((int)Mathf.Round(item.localPosition.x/2.88f)+(int)Mathf.Round(item.localPosition.z/2.88f))%2==0?white:cream;
            if(item.name=="Side wall" || item.name=="Front back wall")renderer.sharedMaterial=white;
            if(item.name=="Side skirting" || item.name=="Entrance fascia" || item.name=="Back fascia")renderer.sharedMaterial=green;
        }
        foreach(int side in new[]{-1,1})foreach(float z in new[]{-17f,17})
        {
            Planter(decor,new Vector3(side*11.4f,.03f,z),1.2f);
            Cube(decor,"Food menu board",new Vector3(side*12.9f,2.4f,z*.61f),new Vector3(.12f,1.6f,3.1f),blue);
            Label(decor,z<0?"COFFEE\n커피 · 음료":"KITCHEN\n식사 · 간식",new Vector3(side*12.78f,2.4f,z*.61f),3,1.4f,side<0?-90:90);
        }
        foreach(int side in new[]{-1,1})foreach(float z in new[]{-12f,12})
            Cube(decor,"Green wall cornice",new Vector3(side*13,4.25f,z),new Vector3(.2f,.25f,15.5f),green);
    }

    public static string RefinePassTwo()
    {
        SavedEditMode(); Palette(); var map=Map(); var added=map.Find(GroupName); if(added==null)throw new InvalidOperationException("Apply first");
        string before=GameplayState(); Daylight();
        // New vegetation uses lit copies of the existing texture atlas, keeping source materials intact.
        foreach(var renderer in added.GetComponentsInChildren<Renderer>(true))
        {
            var mats=renderer.sharedMaterials; bool changed=false;
            for(int i=0;i<mats.Length;i++)
            {
                var source=mats[i]; if(source==null || source.shader.name!="Universal Render Pipeline/Unlit")continue;
                string path=Art+"/Lit_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".mat";
                var lit=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(lit==null) { lit=new Material(Shader.Find("Universal Render Pipeline/Lit")); lit.SetTexture("_BaseMap",source.GetTexture("_BaseMap")); lit.SetColor("_BaseColor",new Color(.72f,.79f,.65f)); lit.SetFloat("_Smoothness",.15f); lit.enableInstancing=true; AssetDatabase.CreateAsset(lit,path); }
                GeneratedStylizedSurface.Apply(lit);mats[i]=lit; changed=true;
            }
            if(changed)renderer.sharedMaterials=mats;
        }
        foreach(var grove in added.Cast<Transform>().Where(t=>t.name=="Roadside grove"))
            foreach(Transform tree in grove) if(tree.name.StartsWith("tree_") && tree.localScale.y>0)
            { float h=HighwayAssetImporter.BoundsOf(tree.gameObject).size.y; tree.localScale*=12.5f/h; var p=tree.localPosition; p.x=Mathf.Sign(p.x)*Mathf.Max(30,Mathf.Abs(p.x)); tree.localPosition=p; }
        foreach(var t in added.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="bush_003").ToArray())
        { t.gameObject.SetActive(false); var shrub=Furniture("bush_001",t.parent,new Vector3(0,.60f,0),0,.8f); shrub.localScale=new Vector3(shrub.localScale.x*.65f,shrub.localScale.y,shrub.localScale.z*1.5f); }
        foreach(var label in added.GetComponentsInChildren<TMP_Text>(true))
        { if(label.text.StartsWith("P  주유"))label.text="P  주유\n식당 →"; label.fontSizeMin=1; label.ForceMeshUpdate(); }
        Save(before,added); return "Second visual pass saved";
    }

    public static string AddBackdrop()
    {
        SavedEditMode(); var map=Map(); var added=map.Find(GroupName); if(added.Find("City horizon")!=null)throw new InvalidOperationException("Backdrop already exists");
        string before=GameplayState(); var skyline=Group(added,"City horizon");
        var buildings=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ithappy/Megacity/Prefabs/Buildings/Skyscrapers","Assets/ithappy/Megacity/Prefabs/Buildings/Business center"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.Contains("eiffel")&&!p.Contains("casino")).OrderBy(p=>p).ToArray();
        var route=map.GetComponent<HighwayRoute>(); float length=route==null?2100:route.length; int serial=0;
        var roadPoints=new List<Vector3>(); for(float d=0;d<length;d+=5) { Sample(d,out var p,out _); roadPoints.Add(p); if(route!=null) { Sample(d,out p,out _,true); roadPoints.Add(p); } }
        for(float d=50;d<length-50;d+=80)
        {
            Sample(d,out var p,out var f); var r=Vector3.Cross(Vector3.up,f);
            foreach(int side in new[]{-1,1})
            {
                var position=p+r*(side*76)+f*35;
                if(roadPoints.Min(v=>Vector2.Distance(new Vector2(v.x,v.z),new Vector2(position.x,position.z)))<48)continue;
                var item=Instance(buildings[(serial++*11)%buildings.Length],skyline,"Background building",Vector3.zero,serial*90,38+serial%4*9);
                item.position=position; var b=HighwayAssetImporter.BoundsOf(item.gameObject); item.position-=new Vector3(0,b.min.y-(route==null?0:-6.5f),0);
            }
        }
        foreach(var label in added.GetComponentsInChildren<TMP_Text>(true))
        { if(label.text.StartsWith("P  주유")) { label.text="주유\n식당 →"; label.fontSizeMin=.7f; label.fontSizeMax=7; label.ForceMeshUpdate(); } }
        Save(before,added); return "Background buildings "+serial;
    }

    public static string SetupLive()
    {
        SavedEditMode(); if(!File.Exists(Prefs)||File.ReadAllLines(Prefs).Length!=63)throw new InvalidOperationException("Fresh verified snapshot required");
        for(int i=1;i<=9;i++)PlayerPrefs.SetInt("upgrade_lv_"+i,i==1?37:i==2?46:i==3?30:0);
        foreach(CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))PlayerPrefs.SetString(CosmeticService.EquippedKey(slot),CosmeticTables.Rows.Single(r=>r.slot==slot&&r.isDefault).id);
        PlayerPrefs.Save(); SessionState.SetBool("NoryangjinMapTool.TestPower9999",false); SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false); SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",1);
        return "Directed visual test: ATT37/HP46/speed30; no ongoing health correction";
    }
    public static string RefineServiceSurfaces()
    {
        SavedEditMode(); if(SceneManager.GetActiveScene().name!="RestStop")throw new InvalidOperationException("RestStop required");
        string before=GameplayState(); Palette(); var map=Map(); var added=map.Find(GroupName);
        paver.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/polyperfect/Poly Universal Pack/Textures/City/Sidewalk_A_City_Alb.png")); paver.SetTextureScale("_BaseMap",new Vector2(4,12)); paver.SetColor("_BaseColor",new Color(.78f,.79f,.75f)); EditorUtility.SetDirty(paver);
        var apron=Mat("FuelApron",new Color(.30f,.32f,.32f));
        foreach(var t in map.Find("Props").Cast<Transform>().Where(t=>t.name=="RestStop_Deck_0"))t.GetComponentInChildren<MeshRenderer>().sharedMaterial=apron;
        var asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat");
        foreach(Transform t in map.Find("Roads")) if(t.name.StartsWith("RST_R")) { var renderer=t.GetComponent<MeshRenderer>(); if(renderer!=null) { var materials=renderer.sharedMaterials; materials[0]=asphalt; renderer.sharedMaterials=materials; } }
        foreach(var fuel in added.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="reststop_fuel_canopy"))
        {
            foreach(Transform trim in fuel) if(trim.name=="Green canopy fascia") { trim.localPosition=new Vector3(0,5.06f,Mathf.Sign(trim.localPosition.z)*4.08f); trim.localScale=new Vector3(12.9f,.60f,.16f); }
            foreach(var label in fuel.GetComponentsInChildren<TMP_Text>()) { label.transform.localPosition=new Vector3(0,5.07f,4.18f); label.rectTransform.sizeDelta=new Vector2(8,.55f); }
        }
        foreach(var hall in added.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="reststop_hall"))
        {
            if(hall.Find("Street-facing green fascia")==null) { Cube(hall,"Street-facing green fascia",new Vector3(0,3.55f,-4.84f),new Vector3(14,.58f,.16f),green); Label(hall,"MART  편의점",new Vector3(0,3.55f,-4.94f),9,.55f); }
        }
        foreach(var machine in added.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="reststop_vending"))
            foreach(var renderer in machine.GetComponentsInChildren<Renderer>())
            {
                if(renderer.name=="Red vending side")continue;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    string path=Art+"/VendingPaint.mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(material==null) { material=new Material(materials[i]); material.SetColor("_BaseColor",new Color(1,.27f,.20f)); AssetDatabase.CreateAsset(material,path); }
                    materials[i]=material;
                }
                renderer.sharedMaterials=materials;
            }
        Save(before,added); return "Tiled sidewalks, asphalt drive and visible green fascia saved";
    }
    public static string FinishHallFloor()
    {
        SavedEditMode(); if(SceneManager.GetActiveScene().name!="RestStop")throw new InvalidOperationException("RestStop required");
        string before=GameplayState(); Palette(); var map=Map(); var added=map.Find(GroupName);
        var tile=Mat("FoodHallStone",new Color(.58f,.59f,.54f));
        tile.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/polyperfect/Poly Universal Pack/Textures/City/Concrete_A_City_Alb.png")); tile.SetFloat("_Smoothness",.24f); EditorUtility.SetDirty(tile);
        foreach(Transform t in map.Find("FoodHall_Holdout"))if(t.name=="Floor tile joint")t.GetComponent<Renderer>().sharedMaterial=tile;
        Save(before,added); return "Quiet stone floor with existing tile joints saved";
    }
    public static string VerifySaved()
    {
        SavedEditMode(); var map=Map(); var scene=SceneManager.GetActiveScene(); var added=map.Find(GroupName);
        var before=File.ReadAllText(Record+"/before/"+scene.name+"-gameplay.txt"); var current=GameplayState();
        // A saved prefab's file IDs remain stable; this is a second comparison after reload.
        File.WriteAllText(Record+"/"+scene.name+"-gameplay-current.txt",current);
        bool same=before==current;
        var report=new {scene=scene.name,gameplayUnchanged=same,newRenderers=added.GetComponentsInChildren<Renderer>().Length,newColliders=added.GetComponentsInChildren<Collider>(true).Length,enemyRoots=map.Find("Enemies").childCount,bonusRoots=map.Find("Bonuses").childCount,missingScripts=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))),skyTexture=RenderSettings.skybox.GetTexture("_Tex")?.GetType().Name,sceneHash=Hash(scene.path)};
        File.WriteAllText(Record+"/"+scene.name+"-verification.json",Json(report)); if(!same)throw new InvalidOperationException("Compare persisted gameplay snapshot"); return Json(report);
    }
    public static string RestorePreferences()
    {
        SavedEditMode(); ChapterPlaytestPreferences.RestoreAt(Prefs);
        var mismatch=new List<string>();
        foreach(var line in File.ReadAllLines(Prefs))
        {
            var p=line.Split('\t'); bool exists=bool.Parse(p[2]); if(PlayerPrefs.HasKey(p[0])!=exists) {mismatch.Add(p[0]);continue;} if(!exists)continue;
            bool same=p[1]=="int"?PlayerPrefs.GetInt(p[0])==int.Parse(p[3],System.Globalization.CultureInfo.InvariantCulture):p[1]=="float"?PlayerPrefs.GetFloat(p[0])==float.Parse(p[3],System.Globalization.CultureInfo.InvariantCulture):PlayerPrefs.GetString(p[0])==System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p[3])); if(!same)mismatch.Add(p[0]);
        }
        var untouched=new[]{"Assets/ShooterSurvival/GameData/Editor/Data.xlsx","Assets/ShooterSurvival/Resources/GameData/Data.bytes","Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity"}.Select(path=>new{path,unchanged=Hash(path)==Hash(Record+"/before/"+Path.GetFileName(path))}).ToArray();
        var report=new{snapshot=Prefs,keys=File.ReadAllLines(Prefs).Length,mismatches=mismatch,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel"),untouched}; File.WriteAllText(Record+"/user-state-restored.json",Json(report));
        if(mismatch.Count>0||untouched.Any(x=>!x.unchanged))throw new InvalidOperationException("Preservation check failed"); return Json(report);
    }
    public static string BeginLive(string label,float startDistance,float seconds,float lane)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");
        string folder=Record+"/live/"+label; if(Directory.Exists(folder))throw new InvalidOperationException("Preserve earlier evidence"); Directory.CreateDirectory(folder);
        var player=Object.FindFirstObjectByType<PlayerScript>(); var canvas=Object.FindFirstObjectByType<CanvasScript>(); var holdout=Object.FindFirstObjectByType<RestStopHoldout>(); var route=Object.FindFirstObjectByType<HighwayRoute>();
        OpeningStoryUI.Instance?.Skip(); UnityEngine.Random.InitState(20260913); canvas.PlayerPressedStartButton();
        if(!TimeManager.isGameRunning)throw new InvalidOperationException("Start did not take effect");
        if(startDistance>0) { if(route!=null)throw new InvalidOperationException("Highway begins at authored start"); Sample(startDistance,out var p,out var f); player.ApplyContinuousRoutePose(p+Vector3.up*.12f,f,0); }
        Time.timeScale=1; TimeManager.timeFactor=1;
        var move=typeof(PlayerScript).GetMethod("PlayerMove",BindingFlags.Instance|BindingFlags.NonPublic); var origin=typeof(PlayerScript).GetField("routeLaneOrigin",BindingFlags.Instance|BindingFlags.NonPublic); var right=typeof(PlayerScript).GetField("routeRight",BindingFlags.Instance|BindingFlags.NonPublic);
        float began=Time.time,nextShot=1,nextSample=0,maxDrift=0; int frame=-1,moves=0; bool done=false; double finishAt=0;
        var originalCamera=Camera.main.transform.localPosition; var originalRotation=Camera.main.transform.localRotation;
        File.WriteAllText(folder+"/timeline.csv","time,health,distance,holdout,spawned,x,y,z\n");
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            try
            {
                if(!EditorApplication.isPlaying) { EditorApplication.update-=tick; return; }
                if(done) { if(EditorApplication.timeSinceStartup>finishAt) { EditorApplication.update-=tick; EditorApplication.isPaused=true; } return; }
                if(EditorApplication.isPaused||frame==Time.frameCount)return; frame=Time.frameCount;
                float elapsed=Time.time-began;
                if(holdout!=null&&holdout.Active)maxDrift=Mathf.Max(maxDrift,Vector3.Distance(player.transform.position,holdout.LockedPosition));
                if(elapsed>=nextShot) { nextShot+=8; ScreenCapture.CaptureScreenshot(folder+"/frame-"+elapsed.ToString("000")+".png"); }
                if(elapsed>=nextSample) { nextSample+=.5f; File.AppendAllText(folder+"/timeline.csv",string.Join(",",elapsed,player.currentHealth,route?.Distance??-1,holdout?.Active??false,holdout?.Spawned??0,player.transform.position.x,player.transform.position.y,player.transform.position.z)+"\n"); }
                bool completed=elapsed>seconds || player.currentHealth<=0 || !TimeManager.isGameRunning || (holdout!=null && holdout.Completed && Vector3.Dot(player.transform.position-holdout.center.position,holdout.center.forward)>35);
                if(completed)
                {
                    var traffic=Object.FindFirstObjectByType<HighwayOncomingTraffic>();
                    var result=new {label,elapsed,health=player.currentHealth,startDistance,distance=route?.Distance??-1,endurance=false,attackLevel=37,healthLevel=46,forks=route?.forks.Select(f=>new{f.decided,f.bypass,f.rewarded}).ToArray(),trafficLaunched=traffic?.Launched??0,holdoutCompleted=holdout?.Completed??false,holdoutSeconds=holdout?.Elapsed??0,police=holdout?.Spawned??0,doors=holdout?.SpawnedByDoor,spawnError=holdout?.MaximumSpawnPositionError??0,maxDrift,cameraPositionError=holdout!=null&&holdout.Completed?Vector3.Distance(originalCamera,Camera.main.transform.localPosition):-1,cameraRotationError=holdout!=null&&holdout.Completed?Quaternion.Angle(originalRotation,Camera.main.transform.localRotation):-1,movementLocked=player.IsStationaryCombat,moves};
                    File.WriteAllText(folder+"/result.json",Json(result)); ScreenCapture.CaptureScreenshot(folder+"/end.png"); done=true; finishAt=EditorApplication.timeSinceStartup+.7; return;
                }
                var r=(Vector3)right.GetValue(player); var o=(Vector3)origin.GetValue(player); float current=Vector3.Dot(player.transform.position-o,r);
                move.Invoke(player,new object[]{Mathf.Clamp((lane-current)*125,-90,90)}); moves++;
            }
            catch(Exception e) { File.WriteAllText(folder+"/error.txt",e.ToString()); EditorApplication.update-=tick; EditorApplication.isPaused=true; }
        };
        EditorApplication.update+=tick; return folder;
    }
}
