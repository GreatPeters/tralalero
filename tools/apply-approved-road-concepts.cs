using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEditor.Animations;
using Object=UnityEngine.Object;

public static class ApprovedRoadConcepts
{
    public const string Record="map-concepts/approved-road-concepts-2026-09-13";
    public const string Prefs=Record+"/before/playerprefs.tsv";
    public const string Preview="tmp/image-previews/approved-road-concepts-2026-09-13";
    public const string Art="Assets/ShooterSurvival/Models/Chapters/ApprovedRoadConcepts";
    public static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    public static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
    static void EditMode()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s=>s.isDirty))throw new InvalidOperationException("Saved Edit Mode required.");
    }
    public static string Prepare()
    {
        EditMode();if(Directory.Exists(Record+"/before"))throw new InvalidOperationException("Preserve this task's existing snapshot.");
        Directory.CreateDirectory(Record+"/before");Directory.CreateDirectory(Preview);
        var files=new List<string>{HighwaySceneBuilder.ScenePath,RestStopChapterBuilder.ScenePath,"Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity","Assets/ShooterSurvival/GameData/Editor/Data.xlsx","Assets/ShooterSurvival/Resources/GameData/Data.bytes","Assets/ShooterSurvival/Scripts/Game/RestStopHoldout.cs","Assets/ShooterSurvival/Scripts/Game/HighwayRoute.cs","Assets/ShooterSurvival/Editor/RoadChapterPatternBuilder.cs","Assets/Tests/Editor/RoadChapterPatternTests.cs"};
        foreach(string name in ChapterMascotImporter.Names)
        {
            files.Add(ChapterMascotImporter.PrefabPath(name));
            string folder="Assets/ShooterSurvival/Models/Chapters/Mascots/"+name;
            files.AddRange(Directory.GetFiles(folder,"*",SearchOption.AllDirectories));
        }
        var rows=new List<object>();foreach(string path in files.Distinct())
        {string target=Record+"/before/"+path;Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target);rows.Add(new{path,sha256=Hash(path)});}
        File.WriteAllText(Record+"/before/files.json",Json(rows));
        var prefs=ChapterPlaytestPreferences.SnapshotAt(Prefs);return Json(new{files=rows.Count,preferences=prefs,scene=SceneManager.GetActiveScene().path});
    }
    public static string Inspect()
    {
        var rows=new List<object>();
        foreach(string name in ChapterMascotImporter.Names)
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(ChapterMascotImporter.PrefabPath(name));var animator=p.GetComponentInChildren<Animator>(true);var head=animator.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Head");
            rows.Add(new{name,path=ChapterMascotImporter.PrefabPath(name),bounds=HighwayAssetImporter.BoundsOf(p).size.ToString(),animator=animator.name,headPosition=head?.position.ToString(),renderers=p.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>new{r.name,bones=r.bones.Length,mesh=r.sharedMesh.name}).ToArray()});
        }
        File.WriteAllText(Record+"/asset-inventory.json",Json(rows));return Json(rows);
    }

    static Transform Map()=>SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
    static Transform Group(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static Material Material(string name,Color color)
    {
        string path=Art+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.22f);AssetDatabase.CreateAsset(material,path);}GeneratedStylizedSurface.Apply(material);return material;
    }
    static Transform Box(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=size;
        Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;return g.transform;
    }
    static Transform Prop(string path,Transform parent,string name,Vector3 local,float yaw=0)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new FileNotFoundException(path);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);g.name=name;g.transform.localPosition=local;g.transform.localRotation=Quaternion.Euler(0,yaw,0);
        foreach(var collider in g.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);return g.transform;
    }
    static void Text(Transform parent,string value,Vector3 local,Vector2 size,float yaw=0)
    {
        var t=Group(parent,"Label "+value);t.localPosition=local;t.localRotation=Quaternion.Euler(0,yaw,0);
        var text=t.gameObject.AddComponent<TextMeshPro>();text.font=GameUIFont.Load();text.text=value;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;
        text.enableAutoSizing=true;text.fontSizeMin=1;text.fontSizeMax=16;text.rectTransform.sizeDelta=size;text.ForceMeshUpdate();
    }
    static void Save(Scene scene)
    {
        foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<Component>(true))if(c!=null&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");AssetDatabase.SaveAssets();
    }
    static void Stripe(Transform parent,HighwayRoute route,float start,float end,float lateral,Material material,string name,float width)
    {
        var line=Group(parent,name).gameObject.AddComponent<LineRenderer>();line.useWorldSpace=true;line.alignment=LineAlignment.TransformZ;line.transform.rotation=Quaternion.Euler(90,0,0);line.widthMultiplier=width;line.sharedMaterial=material;
        int count=Mathf.CeilToInt((end-start)/2)+1;line.positionCount=count;
        for(int i=0;i<count;i++){route.Sample(Mathf.Lerp(start,end,(float)i/(count-1)),false,out var p,out var f);line.SetPosition(i,p+Vector3.Cross(Vector3.up,f)*lateral+Vector3.up*.055f);}
    }
    static void OpposingDeck(Transform roads,HighwayRoute route,float start,float end,int index,Material asphalt)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();int steps=Mathf.CeilToInt((end-start)/2);
        for(int i=0;i<=steps;i++)
        {
            float d=Mathf.Lerp(start,end,(float)i/steps);route.Sample(d,false,out var p,out var f);var r=Vector3.Cross(Vector3.up,f);
            foreach(float lane in new[]{-21f,-7f}){vertices.Add(p+r*lane+Vector3.up*.002f);uv.Add(new Vector2((lane+21)/5,d/5));}
            if(i>0){int n=i*2;triangles.AddRange(new[]{n-2,n,n-1,n-1,n,n+1});}
        }
        var mesh=new Mesh{name="Opposing_"+index.ToString("D3")};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Art+"/"+mesh.name+".asset");
        var go=Group(roads,"Opposing_Road_"+index.ToString("D3")).gameObject;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=asphalt;go.AddComponent<MeshCollider>().sharedMesh=mesh;go.isStatic=true;
    }
    static void PushSceneryOutsideWestRoad(Transform item,HighwayRoute route)
    {
        var renderers=item.GetComponentsInChildren<Renderer>(false);if(renderers.Length==0)return;
        float d=route.NearestDistance(item.position);route.Sample(d,false,out var p,out var f);var r=Vector3.Cross(Vector3.up,f);
        float center=Vector3.Dot(item.position-p,r);if(center>=0)return;
        float maximum=float.NegativeInfinity;
        foreach(var renderer in renderers)
        {var b=renderer.bounds;for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)maximum=Mathf.Max(maximum,Vector3.Dot(b.center+Vector3.Scale(b.extents,new Vector3(x,0,z))-p,r));}
        float edge=-23f;
        if(route.forks.Any(fork=>d>=fork.start-30&&d<=fork.end+30)){route.Sample(d,true,out var branch,out _);edge=Mathf.Min(edge,Vector3.Dot(branch-p,r)-8.5f);}
        if(maximum>edge)item.position+=r*(edge-maximum);
    }
    public static string Highway()
    {
        EditMode();var scene=SceneManager.GetActiveScene();if(scene.name!="HighWay")throw new InvalidOperationException("HighWay required");
        var map=Map();if(map.Find("Approved_Highway_20260913")!=null)throw new InvalidOperationException("Already authored; refine explicitly");
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        var root=Group(map,"Approved_Highway_20260913");var roads=map.Find("Roads");var props=map.Find("Props");var route=map.GetComponent<HighwayRoute>();
        var white=Material("RoadWhite",new Color(.9f,.9f,.84f));var yellow=Material("CenterYellow",new Color(1,.70f,.055f));var steel=Material("GantrySteel",new Color(.36f,.43f,.46f));var green=Material("RoadSignGreen",new Color(.025f,.30f,.13f));
        var asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat");
        foreach(var line in roads.GetComponentsInChildren<LineRenderer>(true).Where(l=>l.name=="Lane"||l.name=="Edge").ToArray())Object.DestroyImmediate(line.gameObject);
        int count=0;for(float d=0;d<route.length;d+=40)OpposingDeck(roads,route,d,Mathf.Min(d+40,route.length),count++,asphalt);
        var paint=Group(roads,"Four_Lane_Markings");
        Stripe(paint,route,0,route.length,-7.17f,yellow,"Yellow_Center_Left",.14f);Stripe(paint,route,0,route.length,-6.83f,yellow,"Yellow_Center_Right",.14f);
        foreach(float edge in new[]{-20.7f,6.7f})Stripe(paint,route,0,route.length,edge,white,"Outer_White_Edge",.17f);
        for(float d=0;d<route.length-3;d+=9)foreach(float lane in new[]{-14f,0f})Stripe(paint,route,d,Mathf.Min(d+3.8f,route.length),lane,white,"Two_Lane_Divider",.17f);
        foreach(Transform item in props)
        {
            if(item.name=="Curved_Guardrail")foreach(Transform part in item)if(part.localPosition.x<0){var p=part.localPosition;p.x-=14;part.localPosition=p;}
            if(item.name.StartsWith("Highway_Tree_")||item.name.StartsWith("Highway_City_")||item.name.StartsWith("HighwayProp_")||item.name.StartsWith("Highway_Polish_Roadworks_"))PushSceneryOutsideWestRoad(item,route);
            if(item.name!="Highway_SignGantry")continue;
            foreach(Transform child in item.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            foreach(float x in new[]{-22.2f,8.2f})Box(item,"Gantry post",new Vector3(x,4,0),new Vector3(.3f,8,.3f),steel);
            Box(item,"Gantry beam",new Vector3(-7,7.8f,0),new Vector3(30.8f,.34f,.34f),steel);
            foreach(float x in new[]{-17.5f,-10.5f,-3.5f,3.5f}){Box(item,"Lane sign",new Vector3(x,6.5f,0),new Vector3(5,2,.18f),green);Text(item,x<-7?"↓":"↑",new Vector3(x,6.5f,-.11f),new Vector2(4,1.8f));}
        }
        var prior=map.Find("Reference_Scenery_20260913");
        if(prior!=null)
        {
            foreach(Transform section in prior)
            {
                if(section.name.StartsWith("Acoustic edge"))foreach(Transform part in section)if(part.localPosition.x<0){var p=part.localPosition;p.x-=14;part.localPosition=p;}
                if(section.name=="Roadside grove"||section.name=="City backdrop"||section.name=="City horizon")foreach(Transform part in section)PushSceneryOutsideWestRoad(part,route);
            }
        }
        var trafficRoot=Group(root,"Opposing_Visual_Traffic");var ambient=trafficRoot.gameObject.AddComponent<HighwayAmbientTraffic>();ambient.route=route;ambient.cars=new Transform[6];
        int[] cars={80,67,82,83,81,54};for(int i=0;i<cars.Length;i++)
        {var car=Prop(HighwayAssetImporter.Prefabs+"/HWY_"+cars[i].ToString("D3")+".prefab",trafficRoot,"Opposing car "+i,Vector3.zero);route.Sample(40+i*38,false,out var p,out var f);car.SetPositionAndRotation(p+Vector3.Cross(Vector3.up,f)*(i%2==0?-10.5f:-17.5f),Quaternion.LookRotation(-f));ambient.cars[i]=car;}
        var player=Object.FindFirstObjectByType<PlayerScript>();player.GetComponent<NoryangjinRoadHeightFollower>().Configure(roads,.12f);var occlusion=Camera.main.GetComponent<NoryangjinCameraOcclusion>();occlusion.Configure(player.transform,roads);
        occlusion.ConfigureAdditionalOccluders(props.Cast<Transform>().Where(t=>t.name.Contains("Gantry")||t.name=="Highway_Tunnel_Rib"||t.name=="Korean_Direction_Sign"||t.name=="Recovery_Stop").ToArray());
        Physics.SyncTransforms();Save(scene);
        var report=new{scene=scene.name,opposingDeckPieces=count,forwardLaneDivider=0,opposingLaneDivider=-14,centerLine=-7,roadEdges=new[]{-21,7},existingForwardRoutePreserved=true,ambientCars=ambient.cars.Length};File.WriteAllText(Record+"/highway-authored.json",Json(report));return Json(report);
    }
    static void SynchronizeHuman(Animator animator,string name)
    {
        string folder="Assets/ShooterSurvival/Models/Chapters/Mascots/"+name;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/"+name+".fbx");
        foreach(var t in animator.GetComponentsInChildren<Transform>(true))
        {
            if(t==animator.transform)continue;
            var original=source.transform.Find(AnimationUtility.CalculateTransformPath(t,animator.transform));
            if(original==null)continue; // Scene-owned throw sockets/effects retain their authored offsets.
            t.localPosition=original.localPosition;t.localRotation=original.localRotation;t.localScale=original.localScale;
        }
        foreach(var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var original=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==renderer.name);
            renderer.sharedMesh=original.sharedMesh;
            renderer.bones=original.bones.Select(b=>animator.transform.Find(AnimationUtility.CalculateTransformPath(b,source.transform))).ToArray();
            if(renderer.bones.Any(b=>b==null))throw new InvalidOperationException("Missing native bone: "+name);
            renderer.rootBone=animator.transform.Find(AnimationUtility.CalculateTransformPath(original.rootBone,source.transform));
            renderer.localBounds=original.localBounds;
        }
        animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(folder+"/Actions.controller");
        animator.applyRootMotion=false;
    }
    public static string ImportHumans()
    {
        EditMode();if(File.Exists(Record+"/humans-installed.json"))throw new InvalidOperationException("Humans already installed; do not reapply blindly");
        var rows=new List<object>();
        foreach(string name in ChapterMascotImporter.Names)
        {
            string source="outputs/approved-road-concepts-2026-09-13/humans/"+name;
            if(!File.Exists(source+"/fresh-fbx-verification.json")||!File.Exists(source+"/evidence-walk/front.png"))throw new InvalidOperationException("Unreviewed human: "+name);
            string folder="Assets/ShooterSurvival/Models/Chapters/Mascots/"+name,path=folder+"/"+name+".fbx";
            File.Copy(source+"/"+name+"-full-keys.fbx",path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
            importer.clipAnimations=Array.Empty<ModelImporterClipAnimation>();importer.SaveAndReimport();
            var settings=importer.defaultClipAnimations;
            foreach(var clip in settings){clip.name=clip.name.Split('|').Last();clip.loopTime=clip.name!="die"&&clip.name!="attack_once";clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;}
            importer.clipAnimations=settings;importer.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"/Actions.controller");
            foreach(var child in controller.layers[0].stateMachine.states)child.state.motion=clips.Single(c=>c.name==child.state.name);
            EditorUtility.SetDirty(controller);
            var prefab=PrefabUtility.LoadPrefabContents(ChapterMascotImporter.PrefabPath(name));
            try
            {
                var animator=prefab.GetComponentInChildren<Animator>(true);SynchronizeHuman(animator,name);
                float height=(name=="TireBruiser"?2.25f:2.1f)*Mathf.Abs(animator.transform.localScale.x);
                // The FBX visual root carries its importer scale in children; its prefab scale is role-only.
                var capsule=prefab.GetComponent<CapsuleCollider>();capsule.height=height;capsule.center=Vector3.up*height*.5f;
                PrefabUtility.SaveAsPrefabAsset(prefab,ChapterMascotImporter.PrefabPath(name));
                rows.Add(new{name,clips=clips.Select(c=>new{c.name,c.length}).ToArray(),bones=animator.GetComponentInChildren<SkinnedMeshRenderer>(true).bones.Length,colliderHeight=height});
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        AssetDatabase.SaveAssets();SynchronizeSceneHumans();File.WriteAllText(Record+"/humans-installed.json",Json(rows));return Json(rows);
    }
    public static string SynchronizeSceneHumans()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var scene=SceneManager.GetActiveScene();if(scene.name!="HighWay"&&scene.name!="RestStop")throw new InvalidOperationException("Road chapter required");int count=0;
        foreach(var animator in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Animator>(true)))
        {
            var renderer=animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r=>ChapterMascotImporter.Names.Any(n=>r.name==n+"_Body"));if(renderer==null)continue;
            string name=ChapterMascotImporter.Names.Single(n=>renderer.name==n+"_Body");SynchronizeHuman(animator,name);count++;
        }
        Save(scene);return Json(new{scene=scene.name,humanVisuals=count});
    }
    public static string ImportCounters()
    {
        EditMode();if(File.Exists(Record+"/counters-installed.json"))throw new InvalidOperationException("Counters already installed; refine explicitly");var rows=new List<object>();
        foreach(string key in new[]{"snack_counter","coffee_counter"})
        {
            string source=Directory.GetDirectories("outputs/approved-road-concepts-2026-09-13/counters").Single(p=>Path.GetFileName(p).Contains(key));
            if(!File.Exists(source+"/validation.json"))throw new InvalidOperationException("Unverified counter: "+key);
            string folder=Art+"/Counters/"+key;Directory.CreateDirectory(folder);
            File.Copy(source+"/model.fbx",folder+"/Model.fbx",true);
            foreach(string texture in new[]{"BaseColor","Normal","Metallic","Roughness"})File.Copy(source+"/textures/"+texture+".png",folder+"/"+texture+".png",true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var model=(ModelImporter)AssetImporter.GetAtPath(folder+"/Model.fbx");model.materialImportMode=ModelImporterMaterialImportMode.None;model.importAnimation=false;model.animationType=ModelImporterAnimationType.None;model.addCollider=false;model.SaveAndReimport();
            foreach(string texture in new[]{"BaseColor","Normal","Metallic","Roughness"})
            {var importer=(TextureImporter)AssetImporter.GetAtPath(folder+"/"+texture+".png");importer.textureType=texture=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=texture=="BaseColor";importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;importer.isReadable=texture=="Metallic"||texture=="Roughness";importer.SaveAndReimport();}
            var metal=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Metallic.png");var rough=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Roughness.png");var pixels=metal.GetPixels32();var roughness=rough.GetPixels32();
            if(pixels.Length!=roughness.Length)throw new InvalidOperationException("Counter mask sizes differ");for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(pixels[i].r,0,0,(byte)(255-roughness[i].r));
            var mask=new Texture2D(metal.width,metal.height,TextureFormat.RGBA32,false,true);mask.SetPixels32(pixels);mask.Apply();File.WriteAllBytes(folder+"/Mask.png",mask.EncodeToPNG());Object.DestroyImmediate(mask);AssetDatabase.ImportAsset(folder+"/Mask.png");
            var maskImporter=(TextureImporter)AssetImporter.GetAtPath(folder+"/Mask.png");maskImporter.sRGBTexture=false;maskImporter.maxTextureSize=2048;maskImporter.SaveAndReimport();
            string materialPath=folder+"/Surface.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){enableInstancing=true};AssetDatabase.CreateAsset(material,materialPath);}
            material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/BaseColor.png"));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Normal.png"));material.SetFloat("_BumpScale",.5f);material.EnableKeyword("_NORMALMAP");material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Mask.png"));material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",.55f);material.EnableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(material);
            GeneratedStylizedSurface.Apply(material);
            var container=new GameObject(key);
            try
            {
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Model.fbx"),container.transform);
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                var b=HighwayAssetImporter.BoundsOf(visual);visual.transform.localScale*=6.4f/b.size.x;b=HighwayAssetImporter.BoundsOf(visual);visual.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
                PrefabUtility.SaveAsPrefabAsset(container,folder+"/"+key+".prefab");rows.Add(new{key,size=HighwayAssetImporter.BoundsOf(visual).size.ToString(),source,prefab=folder+"/"+key+".prefab"});
            }
            finally{Object.DestroyImmediate(container);}
        }
        AssetDatabase.SaveAssets();File.WriteAllText(Record+"/counters-installed.json",Json(rows));return Json(rows);
    }
    static void HallTiles(Transform parent,Material a,Material b,Material dark,int rows=32)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new[]{new List<int>(),new List<int>(),new List<int>()};
        for(int z=0;z<rows;z++)for(int x=0;x<32;x++)
        {
            float left=-24+x*1.5f+.018f,near=-24+z*1.5f+.018f;int n=vertices.Count;
            vertices.AddRange(new[]{new Vector3(left,.04f,near),new Vector3(left+1.464f,.04f,near),new Vector3(left,.04f,near+1.464f),new Vector3(left+1.464f,.04f,near+1.464f)});
            uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one});int material=(x%8==3&&z%8==3)?2:(x+z)%2;indices[material].AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});
        }
        var mesh=new Mesh{name="OpenHallTiles_"+rows};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(indices[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Art+"/"+mesh.name+".asset");
        var tile=Group(parent,"Stone floor joints").gameObject;tile.AddComponent<MeshFilter>().sharedMesh=mesh;tile.AddComponent<MeshRenderer>().sharedMaterials=new[]{a,b,dark};tile.isStatic=true;
    }
    static void Store(Transform parent,string key,Vector3 position,float yaw,string sign)
    {
        string path=Art+"/Counters/"+key+"/"+key+".prefab";var store=Prop(path,parent,"Perimeter "+sign,position,yaw);
        // Counter visuals use +Z as their authored front; the sign is a separate native label.
        Text(store,sign,new Vector3(0,3.05f,1.05f),new Vector2(4.9f,.67f),180);
    }
    public static string RestStop()
    {
        EditMode();var scene=SceneManager.GetActiveScene();if(scene.name!="RestStop")throw new InvalidOperationException("RestStop required");var map=Map();
        if(map.Find("Approved_Open_Hall_20260913")!=null)throw new InvalidOperationException("Open hall already authored; refine explicitly");
        foreach(string key in new[]{"snack_counter","coffee_counter"})if(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Counters/"+key+"/"+key+".prefab")==null)throw new InvalidOperationException("Import counters first");
        var old=map.Find("FoodHall_Holdout");var holdout=old.GetComponent<RestStopHoldout>();var root=Group(map,"Approved_Open_Hall_20260913");root.position=old.position;
        var retained=new HashSet<Transform>(holdout.entrances){holdout.frontShutter,holdout.exitShutter,old.Find("Police_Pool"),old.Find("Police_Targets")};
        foreach(var child in old.Cast<Transform>().ToArray())if(!retained.Contains(child))Object.DestroyImmediate(child.gameObject);
        var previousFinish=map.Find("Reference_Scenery_20260913/Food hall finish");if(previousFinish!=null)previousFinish.gameObject.SetActive(false);
        var floor=map.Find("Roads/FoodHall_Floor");floor.localScale=new Vector3(48f/44,1,48f/72);
        var stone=Material("HallStoneA",new Color(.76f,.77f,.71f));var stoneB=Material("HallStoneB",new Color(.73f,.74f,.69f));var dark=Material("HallStoneInlay",new Color(.33f,.36f,.35f));
        foreach(var material in new[]{stone,stoneB}){material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/polyperfect/Poly Universal Pack/Textures/City/Concrete_A_City_Alb.png"));material.SetFloat("_Smoothness",.2f);EditorUtility.SetDirty(material);}
        floor.GetComponent<Renderer>().sharedMaterial=dark;HallTiles(root,stone,stoneB,dark);
        var cream=Material("HallCream",new Color(.78f,.77f,.68f));var timber=Material("HallTimber",new Color(.41f,.24f,.11f));var navy=Material("HallNavy",new Color(.028f,.12f,.21f));var white=Material("HallCeiling",new Color(.77f,.79f,.74f));
        var glass=Material("HallClerestory",new Color(.52f,.75f,.83f,.25f));glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.renderQueue=3000;GeneratedStylizedSurface.Apply(glass);
        var shell=Group(root,"Covered hall shell");
        foreach(int side in new[]{-1,1})
        {
            foreach(int end in new[]{-1,1})
            {
                Box(shell,"Perimeter lower wall",new Vector3(side*14.5f,2.75f,end*24),new Vector3(19,5.5f,.3f),cream);
                Box(shell,"Perimeter side wall",new Vector3(side*24,2.75f,end*14.5f),new Vector3(.3f,5.5f,19),cream);
                Box(shell,"Corner post",new Vector3(side*24,6,end*24),new Vector3(.65f,12,.65f),timber);
                Box(shell,"Door jamb",new Vector3(side*5,4,end*24),new Vector3(.3f,8,.5f),navy);
                Box(shell,"Side doorway jamb",new Vector3(side*24,4,end*5),new Vector3(.5f,8,.3f),navy);
            }
            Box(shell,"Long perimeter fascia",new Vector3(0,5.55f,side*24),new Vector3(48,.65f,.55f),timber);
            Box(shell,"Side perimeter fascia",new Vector3(side*24,5.55f,0),new Vector3(.55f,.65f,48),timber);
            Box(shell,"Upper rim",new Vector3(0,11.9f,side*24),new Vector3(48,.35f,.55f),white);
            Box(shell,"Upper side rim",new Vector3(side*24,11.9f,0),new Vector3(.55f,.35f,48),white);
            Box(shell,"Front back clerestory",new Vector3(0,8.8f,side*24),new Vector3(47.6f,5.6f,.08f),glass).GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Box(shell,"Side clerestory",new Vector3(side*24,8.8f,0),new Vector3(.08f,5.6f,47.6f),glass).GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            for(int x=-18;x<=18;x+=6){Box(shell,"Clerestory mullion",new Vector3(x,8.8f,side*24),new Vector3(.13f,5.8f,.17f),white);Box(shell,"Side mullion",new Vector3(side*24,8.8f,x),new Vector3(.17f,5.8f,.13f),white);}
            Box(shell,"Roof side",new Vector3(side*14.5f,12.15f,0),new Vector3(19,.28f,48),white);
            Box(shell,"Roof end",new Vector3(0,12.15f,side*19),new Vector3(10,.28f,10),white);
            Box(shell,"Skylight side frame",new Vector3(side*5,12.05f,0),new Vector3(.22f,.45f,28.4f),timber);
        }
        Box(shell,"Skylight glass",new Vector3(0,12.35f,0),new Vector3(10,.07f,28),glass).GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        for(int z=-12;z<=12;z+=4)Box(shell,"Skylight cross frame",new Vector3(0,12.15f,z),new Vector3(10,.25f,.15f),white);
        foreach(int side in new[]{-1,1})foreach(float z in new[]{-16f,-8,8,16})Store(root,z<0?"snack_counter":"coffee_counter",new Vector3(side*22,0,z),side<0?90:-90,z<0?"간식":"커피");
        foreach(int side in new[]{-1,1})foreach(float x in new[]{-17f,-8.5f,8.5f,17})Store(root,x<0?"snack_counter":"coffee_counter",new Vector3(x,0,side*22),side>0?180:0,x<0?"호두과자":"식당");
        var warm=Material("WarmFixture",new Color(.98f,.73f,.28f));warm.EnableKeyword("_EMISSION");warm.SetColor("_EmissionColor",new Color(1,.67f,.22f)*2);EditorUtility.SetDirty(warm);
        foreach(int x in new[]{-12,12})foreach(int z in new[]{-12,12})
        {
            var light=Group(root,"Hall ceiling light");light.localPosition=new Vector3(x,9.2f,z);var lamp=light.gameObject.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(1,.87f,.64f);lamp.intensity=6;lamp.range=27;lamp.shadows=LightShadows.None;
            Box(light,"Fixture body",Vector3.zero,new Vector3(2,.18f,.55f),navy);Box(light,"Warm lens",new Vector3(0,-.11f,0),new Vector3(1.8f,.04f,.42f),warm);Box(light,"Suspension",new Vector3(0,1.42f,0),new Vector3(.05f,2.8f,.05f),navy);
        }
        var doorPoints=new[]{new Vector3(-22,.12f,0),new Vector3(22,.12f,0),new Vector3(-2,.12f,-22),new Vector3(2,.12f,22)};
        for(int i=0;i<4;i++){holdout.entrances[i].localPosition=doorPoints[i];holdout.entrances[i].localRotation=Quaternion.LookRotation(-Vector3.ProjectOnPlane(doorPoints[i],Vector3.up));}
        holdout.frontShutter.localPosition=new Vector3(0,3.8f,-24);holdout.frontShutter.localScale=new Vector3(9.8f,7.6f,.18f);holdout.exitShutter.localPosition=new Vector3(0,3.8f,24);holdout.exitShutter.localScale=new Vector3(9.8f,7.6f,.18f);
        var entry=Group(root,"Food hall entrance facade");Box(entry,"Entrance sign backing",new Vector3(0,8.9f,-24.3f),new Vector3(18,1.8f,.35f),navy);Text(entry,"달빛 휴게소 · 식당",new Vector3(0,8.9f,-24.51f),new Vector2(16,1.5f));
        Text(root,"출입구",new Vector3(0,7.2f,23.7f),new Vector2(7,1.2f));
        foreach(Transform block in map.Find("Props"))
        {
            if(!block.name.StartsWith("RestStop_Block_"))continue;var b=HighwayAssetImporter.BoundsOf(block.gameObject);
            if(b.max.z<root.position.z-26||b.min.z>root.position.z+26)continue;
            if(block.position.x>root.position.x && b.min.x<root.position.x+27)block.position+=Vector3.right*(root.position.x+27-b.min.x);
            if(block.position.x<root.position.x && b.max.x>root.position.x-27)block.position+=Vector3.left*(b.max.x-(root.position.x-27));
        }
        var player=Object.FindFirstObjectByType<PlayerScript>();player.GetComponent<NoryangjinRoadHeightFollower>().Configure(map.Find("Roads"),.12f);Physics.SyncTransforms();Save(scene);
        var result=new{scene=scene.name,hallWidth=48,hallDepth=48,ceilingHeight=12.15f,perimeterCounters=16,centralClearHalfExtent=18,police=holdout.police.Length,doors=holdout.entrances.Select(t=>t.localPosition.ToString()).ToArray(),cameraUnchanged=true};File.WriteAllText(Record+"/reststop-authored.json",Json(result));return Json(result);
    }
    public static string RefineHallCameraClearance()
    {
        EditMode();var scene=SceneManager.GetActiveScene();if(scene.name!="RestStop")throw new InvalidOperationException("RestStop required");var map=Map();var root=map.Find("Approved_Open_Hall_20260913");var shell=root.Find("Covered hall shell");
        foreach(Transform t in shell)
        {
            var p=t.localPosition;var s=t.localScale;
            if(t.name=="Roof side"||t.name=="Roof end")p.y=16.5f;
            if(t.name=="Skylight side frame")p.y=16.4f;
            if(t.name=="Skylight cross frame")p.y=16.5f;
            if(t.name=="Skylight glass")p.y=16.7f;
            if(t.name=="Corner post"){p.y=8.25f;s.y=16.5f;}
            if(t.name=="Upper rim"||t.name=="Upper side rim")p.y=16.25f;
            if(t.name.Contains("clerestory")||t.name=="Clerestory mullion"||t.name=="Side mullion"){p.y=11.1f;s.y=10.2f;}
            t.localPosition=p;t.localScale=s;
        }
        foreach(Transform fixture in root)if(fixture.name=="Hall ceiling light")
        {var cable=fixture.Find("Suspension");cable.localPosition=new Vector3(0,3.62f,0);cable.localScale=new Vector3(.05f,7.24f,.05f);}
        var glass=AssetDatabase.LoadAssetAtPath<Material>(Art+"/HallClerestory.mat");glass.SetColor("_BaseColor",new Color(.6f,.8f,.86f,.10f));glass.SetFloat("_BlendModePreserveSpecular",0);glass.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);glass.DisableKeyword("_ALPHAPREMULTIPLY_ON");glass.SetOverrideTag("RenderType","Transparent");EditorUtility.SetDirty(glass);
        var occlusion=Camera.main.GetComponent<NoryangjinCameraOcclusion>();var serialized=new SerializedObject(occlusion);var property=serialized.FindProperty("additionalOccluderGroups");var groups=new List<Transform>();
        for(int i=0;i<property.arraySize;i++)if(property.GetArrayElementAtIndex(i).objectReferenceValue is Transform t)groups.Add(t);
        groups.Add(root.Find("Food hall entrance facade"));
        foreach(Transform t in shell)if(t.name=="Long perimeter fascia"||t.name=="Front back clerestory")groups.Add(t);
        occlusion.ConfigureAdditionalOccluders(groups.Distinct().ToArray());Save(scene);
        var player=Object.FindFirstObjectByType<PlayerScript>();return Json(new{roof=16.5f,cameraLocal=Camera.main.transform.localPosition.ToString(),inheritedScale=player.transform.lossyScale.ToString(),groups=groups.Count});
    }
    public static string RefineHallPerspective()
    {
        EditMode();var scene=SceneManager.GetActiveScene();if(scene.name!="RestStop")throw new InvalidOperationException("RestStop required");var map=Map();var root=map.Find("Approved_Open_Hall_20260913");var shell=root.Find("Covered hall shell");
        if(shell.Find("Far ceiling extension")!=null)throw new InvalidOperationException("Perspective revision already installed");
        foreach(Transform t in shell)
        {
            var p=t.localPosition;var size=t.localScale;
            if(p.z>23 && t.name!="Side clerestory" && t.name!="Side perimeter fascia" && t.name!="Upper side rim")p.z+=18;
            if(t.name=="Perimeter side wall"&&p.z>0){p.z=23.5f;size.z=37;}
            if(t.name=="Side perimeter fascia"||t.name=="Upper side rim"||t.name=="Side clerestory"){p.z=9;size.z+=18;}
            if(t.name=="Roof side"){p.z=9;size.z=66;}
            if(t.name=="Roof end"&&p.z>0)p.z=37;
            t.localPosition=p;t.localScale=size;
        }
        var ceiling=AssetDatabase.LoadAssetAtPath<Material>(Art+"/HallCeiling.mat");Box(shell,"Far ceiling extension",new Vector3(0,16.5f,23),new Vector3(10,.28f,18),ceiling);
        foreach(Transform t in root)
        {
            if(!t.name.StartsWith("Perimeter "))continue;var p=t.localPosition;
            if(p.z>20)p.z=40;else if(Mathf.Abs(p.x)>21&&p.z>0)p.z=p.z<12?18:32;
            t.localPosition=p;
            foreach(var text in t.GetComponentsInChildren<TMP_Text>(true)){var q=text.transform.localPosition;q.y=t.name.Contains("커피")||t.name.Contains("식당")?3.55f:3.35f;text.transform.localPosition=q;}
        }
        var oldTiles=root.Find("Stone floor joints");var materials=oldTiles.GetComponent<Renderer>().sharedMaterials;Object.DestroyImmediate(oldTiles.gameObject);HallTiles(root,materials[0],materials[1],materials[2],44);
        var floor=map.Find("Roads/FoodHall_Floor");floor.position=root.position+Vector3.forward*9;floor.localScale=new Vector3(48f/44,1,66f/72);
        var holdout=map.Find("FoodHall_Holdout").GetComponent<RestStopHoldout>();holdout.entrances[3].localPosition=new Vector3(2,.12f,40);holdout.entrances[3].localRotation=Quaternion.LookRotation(new Vector3(-2,0,-40));
        holdout.exitShutter.localPosition=new Vector3(0,1.85f,42);holdout.exitShutter.localScale=new Vector3(9.8f,3.7f,.18f);
        for(int i=0;i<4;i++){var lamp=holdout.entranceSignals[i].transform;lamp.localPosition=new Vector3(0,4.4f,0);lamp.localScale=new Vector3(.8f,.2f,.25f);}
        var exitLabel=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.text=="출입구");exitLabel.text="비상구";exitLabel.transform.localPosition=new Vector3(0,4.2f,41.72f);exitLabel.rectTransform.sizeDelta=new Vector2(5,.75f);
        Box(root,"Exit sign backing",new Vector3(0,4.2f,41.83f),new Vector3(5.3f,.9f,.14f),Material("ExitGreen",new Color(.025f,.27f,.12f)));
        foreach(Transform block in map.Find("Props"))
        {if(!block.name.StartsWith("RestStop_Block_"))continue;var b=HighwayAssetImporter.BoundsOf(block.gameObject);if(b.max.z<root.position.z-26||b.min.z>root.position.z+44)continue;if(block.position.x>root.position.x&&b.min.x<root.position.x+27)block.position+=Vector3.right*(root.position.x+27-b.min.x);}
        var occlusion=Camera.main.GetComponent<NoryangjinCameraOcclusion>();var data=new SerializedObject(occlusion);var field=data.FindProperty("additionalOccluderGroups");var groups=new List<Transform>();for(int i=0;i<field.arraySize;i++)if(field.GetArrayElementAtIndex(i).objectReferenceValue is Transform t)groups.Add(t);groups.Add(holdout.frontShutter);groups.Add(holdout.exitShutter);occlusion.ConfigureAdditionalOccluders(groups.Distinct().ToArray());
        Physics.SyncTransforms();Save(scene);var report=new{width=48,depth=66,front=-24,rear=42,roof=16.5f,cameraLocal=Camera.main.transform.localPosition.ToString()};File.WriteAllText(Record+"/reststop-perspective-refinement.json",Json(report));return Json(report);
    }
    public static string RefineHighwayPresentation()
    {
        EditMode();var scene=SceneManager.GetActiveScene();if(scene.name!="HighWay")throw new InvalidOperationException("HighWay required");var map=Map();var route=map.GetComponent<HighwayRoute>();
        foreach(string name in new[]{"RoadWhite","CenterYellow"})
        {var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+name+".mat");material.SetFloat("_Cull",0);material.SetColor("_BaseColor",name=="CenterYellow"?new Color(1,.72f,.035f):new Color(.89f,.90f,.86f));GeneratedStylizedSurface.Apply(material);}
        foreach(var line in map.Find("Roads/Four_Lane_Markings").GetComponentsInChildren<LineRenderer>())line.widthMultiplier=line.name.StartsWith("Yellow_")?.22f:.21f;
        var prior=map.Find("Reference_Scenery_20260913");
        float OuterLength(float distance,float lateral)
        {route.Sample(Mathf.Max(0,distance-6),false,out var a,out var af);route.Sample(Mathf.Min(route.length,distance+6),false,out var b,out var bf);return Vector3.Distance(a+Vector3.Cross(Vector3.up,af)*lateral,b+Vector3.Cross(Vector3.up,bf)*lateral)+.24f;}
        foreach(Transform section in prior)
        {
            if(!section.name.StartsWith("Acoustic edge"))continue;float d=route.NearestDistance(section.position);
            foreach(Transform part in section)
            {var scale=part.localScale;if(part.name=="HighwayProp_64")scale.x=OuterLength(d,part.localPosition.x)/8;else if(part.name=="Green acoustic band")scale.z=OuterLength(d,part.localPosition.x);part.localScale=scale;}
        }
        foreach(Transform item in map.Find("Props"))if(item.name=="Curved_Guardrail")
        {float d=route.NearestDistance(item.position);foreach(Transform rail in item)if(rail.name=="Rail"){var scale=rail.localScale;scale.z=OuterLength(d,rail.localPosition.x);rail.localScale=scale;}}
        int lit=0;
        foreach(var renderer in map.GetComponentsInChildren<MeshRenderer>(true))
        {
            if(renderer.GetComponent<TMP_Text>()!=null)continue;var materials=renderer.sharedMaterials;bool changed=false;
            for(int i=0;i<materials.Length;i++)
            {
                var source=materials[i];if(source==null||source.shader.name!="Universal Render Pipeline/Unlit"||!AssetDatabase.GetAssetPath(source).StartsWith("Assets/ithappy/"))continue;
                string path=Art+"/EnvironmentLit_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){enableInstancing=true};material.SetTexture("_BaseMap",source.GetTexture("_BaseMap"));material.SetColor("_BaseColor",new Color(.78f,.83f,.80f));material.SetFloat("_Smoothness",.14f);AssetDatabase.CreateAsset(material,path);}
                GeneratedStylizedSurface.Apply(material);materials[i]=material;changed=true;lit++;
            }
            if(changed)renderer.sharedMaterials=materials;
        }
        Save(scene);var report=new{paint="Stylized Surface without paint outlines",outerCurvePanelsFitted=true,litAssignments=lit};File.WriteAllText(Record+"/highway-presentation-refinement.json",Json(report));return Json(report);
    }
    public static string AddHighwayStartSupport()
    {
        EditMode();var scene=SceneManager.GetActiveScene();if(scene.name!="HighWay")throw new InvalidOperationException("HighWay required");var map=Map();var roads=map.Find("Roads");if(roads.Find("Entry_Approach")!=null)return "Entry approach already present";
        map.GetComponent<HighwayRoute>().Sample(0,false,out var p,out var f);var r=Vector3.Cross(Vector3.up,f);var mesh=new Mesh{name="HighwayEntryApproach"};
        mesh.vertices=new[]{p+r*-21-f*8,p+r*7-f*8,p+r*-21,p+r*7};mesh.uv=new[]{new Vector2(0,-1.6f),new Vector2(5.6f,-1.6f),Vector2.zero,new Vector2(5.6f,0)};mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Art+"/HighwayEntryApproach.asset");
        var approach=Group(roads,"Entry_Approach").gameObject;approach.AddComponent<MeshFilter>().sharedMesh=mesh;approach.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat");approach.AddComponent<MeshCollider>().sharedMesh=mesh;Physics.SyncTransforms();Save(scene);return "Eight-meter supported spawn approach saved";
    }
    public static string FinishRestStopPresentation()
    {
        EditMode();var scene=SceneManager.GetActiveScene();if(scene.name!="RestStop")throw new InvalidOperationException("RestStop required");var map=Map();var hall=map.Find("Approved_Open_Hall_20260913");if(hall.Find("Far food hall fascia")!=null)throw new InvalidOperationException("Final presentation already installed");
        foreach(Transform counter in hall)
            if(counter.name.StartsWith("Perimeter ")&&counter.localPosition.z>39&&Mathf.Abs(counter.localPosition.x)<10)
            {var p=counter.localPosition;p.x=Mathf.Sign(p.x)*7.8f;counter.localPosition=p;counter.localScale=new Vector3(.86f,1,1);}
        var fascia=Group(hall,"Far food hall fascia");Box(fascia,"Timber sign board",new Vector3(0,6.3f,41.7f),new Vector3(48,1.6f,.22f),Material("HallHeaderWood",new Color(.45f,.29f,.16f)));Text(fascia,"달빛 휴게소 · 푸드코트",new Vector3(0,6.3f,41.55f),new Vector2(24,1.25f));
        var holdout=map.Find("FoodHall_Holdout").GetComponent<RestStopHoldout>();var shutter=holdout.exitShutter;var slats=Group(shutter,"Roller shutter details");var scale=shutter.localScale;slats.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
        var metal=Material("ShutterRibs",new Color(.29f,.34f,.35f));for(float y=-1.65f;y<1.8f;y+=.23f)Box(slats,"Metal shutter rib",new Vector3(0,y,-.115f),new Vector3(9.55f,.025f,.045f),metal);
        foreach(int side in new[]{-1,1})Box(slats,"Shutter guide rail",new Vector3(side*4.72f,0,-.1f),new Vector3(.09f,3.55f,.08f),metal);
        int outdoor=0;var scenery=map.Find("Reference_Scenery_20260913");
        foreach(Transform court in scenery)
        {
            if(!court.name.StartsWith("Service courtyard"))continue;
            var kiosk=court.Find("reststop_kiosk");if(kiosk!=null)kiosk.gameObject.SetActive(false);
            Store(court,"snack_counter",new Vector3(9.4f,.13f,-11),-90,"간식");Store(court,"coffee_counter",new Vector3(10.1f,.13f,6),-90,"커피");
            foreach(Transform item in court)if(item.name=="Planted sidewalk"&&Mathf.Abs(item.localPosition.z-10)<.1f){var p=item.localPosition;p.z=11.2f;item.localPosition=p;}
            outdoor+=2;
        }
        var occlusion=Camera.main.GetComponent<NoryangjinCameraOcclusion>();var data=new SerializedObject(occlusion);var field=data.FindProperty("additionalOccluderGroups");var groups=new List<Transform>();for(int i=0;i<field.arraySize;i++)if(field.GetArrayElementAtIndex(i).objectReferenceValue is Transform t)groups.Add(t);groups.Add(fascia);occlusion.ConfigureAdditionalOccluders(groups.Distinct().ToArray());
        Save(scene);var report=new{outdoorCounters=outdoor,indoorCounters=16,shutterDetails=true,frontageFitsNormalCamera=true};File.WriteAllText(Record+"/reststop-final-presentation.json",Json(report));return Json(report);
    }
    public static string FinalBypassSceneryClearance()
    {
        EditMode();var scene=SceneManager.GetActiveScene();if(scene.name!="HighWay")throw new InvalidOperationException("HighWay required");var map=Map();var route=map.GetComponent<HighwayRoute>();var candidates=new List<Transform>();
        candidates.AddRange(map.Find("Props").Cast<Transform>().Where(t=>t.name.StartsWith("Highway_Tree_")||t.name.StartsWith("Highway_City_")));
        foreach(Transform group in map.Find("Reference_Scenery_20260913"))if(group.name=="Roadside grove"||group.name=="City backdrop"||group.name=="City horizon")candidates.AddRange(group.Cast<Transform>());
        int moved=0;
        foreach(var item in candidates)
        {
            if(item.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))continue;
            foreach(var fork in route.forks)
            {
                var b=HighwayAssetImporter.BoundsOf(item.gameObject);float best=float.PositiveInfinity,nearest=fork.start;bool intersects=false;
                for(float d=fork.start;d<=fork.end;d+=5)
                {route.Sample(d,true,out var p,out _);var q=b.ClosestPoint(p+Vector3.up);if(Vector3.Distance(q,p+Vector3.up)<8.5f)intersects=true;float distance=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(item.position.x,item.position.z));if(distance<best){best=distance;nearest=d;}}
                if(!intersects)continue;route.Sample(nearest,true,out var center,out var f);var right=Vector3.Cross(Vector3.up,f);float extent=Mathf.Abs(right.x)*b.extents.x+Mathf.Abs(right.z)*b.extents.z;float lane=Vector3.Dot(b.center-center,right);item.position+=right*(-9-extent-lane);moved++;
            }
        }
        Save(scene);File.WriteAllText(Record+"/bypass-scenery-clearance.json",Json(new{moved,colliderBearingObjectsMoved=0,gameplayGeometryChanged=false}));return Json(new{moved});
    }
    public static string GroundHighwayVerge()
    {
        EditMode();var scene=SceneManager.GetActiveScene();if(scene.name!="HighWay")throw new InvalidOperationException("HighWay required");var map=Map();var route=map.GetComponent<HighwayRoute>();var roads=map.Find("Roads");if(roads.Find("Approved_Shoulders")!=null)throw new InvalidOperationException("Grounding already applied");
        float ground=map.Find("Props/CityGround").GetComponent<Renderer>().bounds.max.y;var candidates=new List<Transform>();candidates.AddRange(map.Find("Props").Cast<Transform>().Where(t=>t.name.StartsWith("Highway_Tree_")||t.name.StartsWith("Highway_City_")));
        foreach(Transform group in map.Find("Reference_Scenery_20260913"))if(group.name=="Roadside grove"||group.name=="City backdrop"||group.name=="City horizon")candidates.AddRange(group.Cast<Transform>());
        int adjusted=0;foreach(var item in candidates){if(item.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))continue;var b=HighwayAssetImporter.BoundsOf(item.gameObject);float delta=ground-b.min.y;if(Mathf.Abs(delta)>.002f){item.position+=Vector3.up*delta;adjusted++;}}
        var shoulder=Group(roads,"Approved_Shoulders");var concrete=Material("ShoulderConcrete",new Color(.48f,.50f,.47f));concrete.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/polyperfect/Poly Universal Pack/Textures/City/Concrete_A_City_Alb.png"));EditorUtility.SetDirty(concrete);int index=0;
        for(float start=0;start<route.length;start+=40)foreach(int side in new[]{-1,1})
        {
            float end=Mathf.Min(start+40,route.length);int steps=Mathf.CeilToInt((end-start)/2);var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int i=0;i<=steps;i++){float d=Mathf.Lerp(start,end,(float)i/steps);route.Sample(d,false,out var p,out var f);var right=Vector3.Cross(Vector3.up,f);foreach(float lateral in side<0?new[]{-25f,-21f}:new[]{7f,11f}){vertices.Add(p+right*lateral-Vector3.up*.01f);uv.Add(new Vector2(lateral/4,d/4));}if(i>0){int n=i*2;triangles.AddRange(new[]{n-2,n,n-1,n-1,n,n+1});}}
            var mesh=new Mesh{name="Shoulder_"+index++};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Art+"/"+mesh.name+".asset");var g=Group(shoulder,mesh.name).gameObject;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=concrete;
        }
        Save(scene);var report=new{ground,adjusted,shoulderPieces=index,newColliders=0};File.WriteAllText(Record+"/highway-grounding.json",Json(report));return Json(report);
    }
}
