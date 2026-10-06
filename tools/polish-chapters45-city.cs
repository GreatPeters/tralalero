using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Object = UnityEngine.Object;

// Jamsil presentation only. Run in idle Edit Mode after the crown orientation repair.
// Preserves route, enemies, hazards, choices, goals and all existing colliders.
public static class PolishChapters45City
{
    const string ScenePath = "Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity";
    const string Art = "Assets/ShooterSurvival/Models/Chapters/Chapters45/CityPolishV1";
    const string Record = "outputs/chapters45-2026-10-02";
    const string OwnedRoot = "Jamsil_CityPolish_V1";
    const string Props = "Assets/ShooterSurvival/Models/RestStopProduction20260925/";
    static Transform root;
    static Chapter45Director director;
    static Chapter45Route route;
    static TMP_FontAsset font;
    static Material ivory, limestone, granite, asphalt, glass, blue, dark, bronze, teal, terracotta, leaf, water;
    static int meshIndex, modelCount, clusters, hiddenLegacy;
    static readonly List<string> notes = new();
    static readonly string[] ShopNames = { "SEOUL COFFEE", "JAMSIL BOOKS", "URBAN SPORT", "SEOUL BAKERY", "LAKE GALLERY", "JAMSIL DESIGN" };

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Idle Edit Mode required.");
        for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve dirty scenes first.");
        var setup = EditorSceneManager.GetSceneManagerSetup(); Scene scene = default; bool saved = false;
        try
        {
            scene = EditorSceneManager.OpenScene(ScenePath);
            director = Object.FindObjectsByType<Chapter45Director>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(d => d.gameObject.scene == scene && d.chapter == 4);
            route = director.route;
            var scenery = director.transform.Find("Scenery");
            if (scenery == null || route == null || route.segments.Length != 5) throw new InvalidOperationException("Expected authored Jamsil route and Scenery root.");
            var sourceCrown = scenery.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "GeneratedCrownAnchor" && !IsUnder(t, OwnedRoot));
            if (sourceCrown == null || sourceCrown.GetComponent<LODGroup>() == null) throw new InvalidOperationException("Installed, orientation-correct crown anchor is required.");
            var crownLOD0 = sourceCrown.Find("Crown_LOD0");
            if (crownLOD0 == null) throw new InvalidOperationException("Source crown LOD0 missing.");
            var crownBounds = BoundsOf(crownLOD0.gameObject);
            float crownLength = Mathf.Max(crownBounds.size.x, crownBounds.size.z);
            if (crownBounds.size.y > crownLength * .68f || crownBounds.size.y < crownLength * .24f) throw new InvalidOperationException("Repair source crown orientation before city polish. Expected sneaker height/length ~0.42.");
            string gameplay = GameplayFingerprint(), routeBefore = JsonUtility.ToJson(route), colliderBefore = ColliderFingerprint();
            Directory.CreateDirectory(Art); Directory.CreateDirectory(Record); AssetDatabase.Refresh();
            string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
            string backup = Record + "/Jamsil-before-city-polish-" + stamp + ".unity";
            File.Copy(ScenePath, backup, false);
            var previous = scenery.Find(OwnedRoot); if (previous != null) Object.DestroyImmediate(previous.gameObject);
            root = Group(scenery, OwnedRoot); meshIndex = modelCount = clusters = hiddenLegacy = 0; notes.Clear();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");
            if (font == null) throw new FileNotFoundException("GmarketHarbor SDF font missing.");
            Materials();
            // Renderer-only suppression retains prior authored colliders and transforms.
            // The new crown copies the corrected nested transforms before old renderers are hidden.
            Landmark(sourceCrown);
            foreach (var r in scenery.GetComponentsInChildren<Renderer>(true)) if (!r.transform.IsChildOf(root)) { r.enabled = false; hiddenLegacy++; }
            GroundAndApproach();
            Skyline();
            int block = 0;
            for (float distance = 142; distance < 1490; distance += 82) StreetBlock(block++, distance);
            Lakeside();
            Plaza();
            foreach (float d in new[] { 370f, 510f, 970f, 1460f }) Intersection(d);
            CameraAndDaylight(scene);
            if (gameplay != GameplayFingerprint() || routeBefore != JsonUtility.ToJson(route) || colliderBefore != ColliderFingerprint()) throw new InvalidOperationException("City presentation tool changed gameplay/collider state.");
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Jamsil scene save failed.");
            saved = true;
            var receipt = new Receipt { scene = ScenePath, backup = backup, clusters = clusters, importedPropInstances = modelCount, generatedMeshes = meshIndex, legacyRenderersHidden = hiddenLegacy,
                renderers = root.GetComponentsInChildren<Renderer>(true).Length,
                trianglesAllAuthored = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Sum(f => Enumerable.Range(0, f.sharedMesh.subMeshCount).Sum(s => (int)f.sharedMesh.GetIndexCount(s) / 3)),
                routeGameplayAndCollidersUnchanged = true, cameraPitch = 15, cameraFov = 62, shaftHeight = 390, crownLength = 105, notes = notes.ToArray() };
            File.WriteAllText(Record + "/city-polish-" + stamp + ".json", JsonUtility.ToJson(receipt, true));
            return receipt;
        }
        finally
        {
            if (!saved && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
    [Serializable] sealed class Receipt { public string scene, backup; public int clusters, importedPropInstances, generatedMeshes, legacyRenderersHidden, renderers, trianglesAllAuthored; public bool routeGameplayAndCollidersUnchanged; public float cameraPitch, cameraFov, shaftHeight, crownLength; public string[] notes; }
    static bool IsUnder(Transform t, string name) { for (; t != null; t = t.parent) if (t.name == name) return true; return false; }
    static string GameplayFingerprint()
    {
        var types = new HashSet<Type> { typeof(Chapter45Encounter), typeof(Chapter45Target), typeof(Chapter45Hazard), typeof(Chapter45Choice), typeof(Chapter45Goal), typeof(Chapter45Lift) };
        return string.Join("\n", director.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null && types.Contains(c.GetType())).OrderBy(c => c.GetInstanceID()).Select(c => JsonUtility.ToJson(c)));
    }
    static string ColliderFingerprint() => string.Join("\n", director.GetComponentsInChildren<Collider>(true).Where(c => !IsUnder(c.transform, OwnedRoot)).OrderBy(c => c.GetInstanceID()).Select(c => c.GetInstanceID() + ":" + EditorJsonUtility.ToJson(c) + ":" + c.transform.localToWorldMatrix.ToString()));
    static Transform Group(Transform parent, string name) { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
    static Transform Station(string name, float distance, float visibilityAhead = 150)
    {
        var g = Group(root, name); route.Sample(distance, out var center, out var forward); g.SetPositionAndRotation(center, Quaternion.LookRotation(forward));
        var v = g.gameObject.AddComponent<Chapter45SceneryGroup>(); v.startDistance = distance - visibilityAhead; v.endDistance = distance + 45; v.floor = 0; clusters++; return g;
    }
    static Material Mat(string name, Color color, float smooth = .2f, float metallic = 0)
    {
        string path = Art + "/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metallic); m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
    }
    static void Materials()
    {
        ivory = Mat("Ivory", new Color(.89f,.89f,.83f)); limestone = Mat("Limestone",new Color(.65f,.66f,.61f)); granite = Mat("Paving",new Color(.40f,.45f,.43f));
        asphalt = Mat("Asphalt",new Color(.105f,.145f,.17f)); glass=Mat("Glazing",new Color(.13f,.29f,.37f),.48f,.18f); blue=Mat("TowerGlass",new Color(.25f,.49f,.65f),.52f,.1f);
        dark=Mat("Graphite",new Color(.09f,.135f,.15f)); bronze=Mat("ChampagneMetal",new Color(.56f,.46f,.30f),.32f,.45f); teal=Mat("JamsilTeal",new Color(.07f,.35f,.32f));
        terracotta=Mat("WarmCeramic",new Color(.52f,.27f,.18f)); leaf=Mat("Planting",new Color(.19f,.36f,.22f)); water=Mat("LakeWater",new Color(.20f,.48f,.56f),.56f);
    }
    static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;
        Object.DestroyImmediate(g.GetComponent<Collider>());var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;return g;
    }
    static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,Material m)
    {var g=Box(parent,name,(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),m);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static TMP_Text Text(Transform parent,string name,string text,Vector3 p,Vector2 size,float fontSize,Color color,Quaternion? rotation=null)
    {
        var g=new GameObject(name,typeof(TextMeshPro));g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localRotation=rotation??Quaternion.identity;
        var t=g.GetComponent<TextMeshPro>();t.font=font;t.text=text;t.fontSize=fontSize;t.enableAutoSizing=true;t.fontSizeMax=fontSize;t.fontSizeMin=fontSize*.55f;t.alignment=TextAlignmentOptions.Center;t.color=color;t.enableWordWrapping=false;t.rectTransform.sizeDelta=size;
        var r=t.GetComponent<MeshRenderer>();r.shadowCastingMode=ShadowCastingMode.Off;return t;
    }
    static Bounds BoundsOf(GameObject go)
    {var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer).ToArray();if(rs.Length==0)throw new InvalidOperationException("No model renderers: "+go.name);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static GameObject Prop(Transform parent,string id,Vector3 localPosition,float targetHeight,float yaw)
    {
        // Direct validated FBX avoids stale prefab nested references. Preserve its authored
        // root rotation and scale: these imports are not guaranteed to be identity/Y-up.
        string path=Props+id+"/"+id+".fbx";var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new FileNotFoundException(path);
        var holder=Group(parent,id+"_ReviewedProp");holder.localPosition=localPosition;holder.localRotation=Quaternion.Euler(0,yaw,0);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.transform.SetParent(holder,false);
        var sourceMaterial=AssetDatabase.LoadAssetAtPath<Material>(Props+id+"/Surface_0.mat");
        var renderers=go.GetComponentsInChildren<MeshRenderer>(true);if(renderers.Length==0||!go.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh!=null&&f.sharedMesh.vertexCount>0))throw new InvalidOperationException(id+" FBX has no usable mesh.");
        foreach(var c in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        foreach(var r in renderers){if(sourceMaterial!=null)r.sharedMaterials=r.sharedMaterials.Select(_=>sourceMaterial).ToArray();r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;}
        var b=BoundsOf(go);if(b.size.y<.0001f)throw new InvalidOperationException(id+" has degenerate height.");go.transform.localScale*=targetHeight/b.size.y;b=BoundsOf(go);
        Vector3 anchor=holder.position;go.transform.position+=new Vector3(anchor.x-b.center.x,anchor.y-b.min.y,anchor.z-b.center.z);
        modelCount++;return holder.gameObject;
    }
    static void MeshObject(Transform parent,string name,Mesh mesh,Material mat)
    {
        string path=Art+"/"+(meshIndex++).ToString("D4")+"_"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;
    }
    static void Merge(Transform parent,string key)
    {
        // Only generated primitive/source meshes that are readable and fully materialed.
        // Imported props, LODs, and TMP remain independent shared meshes.
        var fs=parent.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&f.sharedMesh.isReadable&&f.GetComponent<MeshRenderer>()!=null&&f.GetComponentInParent<LODGroup>()==null&&f.GetComponent<TMP_Text>()==null&&f.sharedMesh.subMeshCount==f.GetComponent<MeshRenderer>().sharedMaterials.Length&&!IsImportedProp(f.transform)).ToArray();
        var batches=new Dictionary<Material,List<CombineInstance>>();
        foreach(var f in fs){var ms=f.GetComponent<MeshRenderer>().sharedMaterials;for(int s=0;s<ms.Length;s++){if(ms[s]==null)throw new InvalidOperationException("Unassigned city material.");if(!batches.TryGetValue(ms[s],out var list))batches.Add(ms[s],list=new List<CombineInstance>());list.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=s,transform=parent.worldToLocalMatrix*f.transform.localToWorldMatrix});}}
        int n=0;foreach(var batch in batches){var m=new Mesh{name=key+"_"+n,indexFormat=IndexFormat.UInt32};m.CombineMeshes(batch.Value.ToArray(),true,true);m.RecalculateBounds();MeshObject(parent,key+"_Batch"+n++,m,batch.Key);}
        foreach(var f in fs){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
    }
    static bool IsImportedProp(Transform t){for(;t!=null;t=t.parent)if(t.name.EndsWith("_ReviewedProp",StringComparison.Ordinal))return true;return false;}
    static void GroundAndApproach()
    {
        var g=Group(root,"GroundAndShortHighwayApproach");
        Box(g,"GroundContinuity",new Vector3(0,-.45f,930),new Vector3(1300,.4f,2440),granite);
        Box(g,"RoadBehindPlayer",new Vector3(0,-.022f,-49),new Vector3(18,.04f,102),asphalt);
        for(int side=-1;side<=1;side+=2)
        {
            Box(g,"ExitShoulder",new Vector3(side*10.2f,.015f,10),new Vector3(2.4f,.08f,180),limestone);
            Box(g,"GuardRail",new Vector3(side*11.1f,1.03f,6),new Vector3(.18f,.28f,178),ivory);
            for(int i=0;i<16;i++)Box(g,"GuardRailPost",new Vector3(side*11.1f,.56f,-75+i*11),new Vector3(.16f,1.12f,.24f),dark);
            Box(g,"GantryPost",new Vector3(side*10.7f,4.0f,57),new Vector3(.4f,8,.5f),dark);
        }
        Box(g,"GantryBeam",new Vector3(0,8.1f,57),new Vector3(22,.42f,.5f),dark);
        Box(g,"JamsilDirectionPanel",new Vector3(0,7.35f,56.7f),new Vector3(10,2.05f,.25f),teal);
        Text(g,"JamsilExitName","잠실  JAMSIL   ↑",new Vector3(0,7.4f,56.53f),new Vector2(9.5f,1.8f),27,Color.white);
        for(float z=-80;z<96;z+=15)foreach(int side in new[]{-1,1})Box(g,"ApproachLaneDash",new Vector3(side*3,.026f,z),new Vector3(.12f,.025f,6),ivory);
        Merge(g,"Approach");
    }
    static void Skyline()
    {
        var g=Group(root,"SetbackSkyline");
        for(int i=0;i<20;i++)
        {
            float d=125+i*77;route.Sample(d,out var c,out var f);var right=Vector3.Cross(Vector3.up,f);
            foreach(int side in new[]{-1,1})
            {
                if(side<0&&d>535&&d<1410)continue;
                float setback=49+(i%4)*10, h=18+(i*7%5)*6;var b=Group(g,"SetbackOffice_"+i+"_"+side);b.SetPositionAndRotation(c+right*(side*setback),Quaternion.LookRotation(f));
                float width=15+(i%3)*4, depth=25+(i%4)*3;
                Box(b,"GroundedOfficeBody",new Vector3(0,h*.5f,0),new Vector3(width,h,depth),i%3==0?limestone:i%3==1?glass:ivory);
                Box(b,"OfficePodium",new Vector3(0,2.7f,0),new Vector3(width+3,5.4f,depth+2),limestone);
                // Vertical glazed recesses break up office silhouettes; slim horizontal
                // slabs describe floors without making large floating striped boxes.
                for(int k=-1;k<=1;k++)Box(b,"OfficeVerticalGlazing",new Vector3(k*width*.28f,h*.53f,-depth*.5f-.03f),new Vector3(width*.19f,h*.78f,.10f),glass);
                for(float y=9;y<h-1;y+=7)Box(b,"OfficeFloorReveal",new Vector3(0,y,-depth*.5f-.08f),new Vector3(width,.12f,.16f),ivory);
                Box(b,"RoofSetback",new Vector3(width*.14f,h+1.5f,depth*.08f),new Vector3(width*.58f,3,depth*.54f),dark);
                Box(b,"RoofParapet",new Vector3(0,h+.25f,0),new Vector3(width+.4f,.5f,depth+.4f),ivory);
            }
        }
        Merge(g,"Skyline");
    }
    static void StreetBlock(int index,float distance)
    {
        bool fork=distance>510&&distance<865;
        var g=Station("BoulevardBlock_"+index.ToString("D2"),distance,165);float frontage=fork?34:19+(index%3)*1.5f;
        foreach(int side in new[]{-1,1})
        {
            bool lakeside=side<0&&distance>520&&distance<1400;
            float curb=fork?25:9.3f;
            Box(g,"ContinuousPavement",new Vector3(side*(curb+4),.045f,0),new Vector3(8,.13f,83),limestone);
            Box(g,"GraniteCurb",new Vector3(side*curb,.11f,0),new Vector3(.3f,.23f,83),ivory);
            for(int p=0;p<5;p++)Box(g,"PavingJoint",new Vector3(side*(curb+4),.118f,-34+p*17),new Vector3(7.6f,.013f,.07f),granite);
            if(!lakeside) Podium(g,index,side,frontage);
            else
            {
                Box(g,"PromenadePlantingBed",new Vector3(-frontage+1,.32f,0),new Vector3(3,.64f,44),limestone);
                Box(g,"DenseLowPlanting",new Vector3(-frontage+1,.72f,0),new Vector3(2.65f,.42f,43),leaf);
                for(int k=0;k<8;k++)Box(g,"PromenadeRailing",new Vector3(-frontage-1,1.0f,-35+k*10),new Vector3(.14f,1.8f,.14f),bronze);
                Box(g,"ContinuousHandrail",new Vector3(-frontage-1,1.87f,0),new Vector3(.15f,.15f,81),bronze);
            }
        }
        // Road marks stay inside the existing road; neither route nor road collider changes.
        for(int j=0;j<5;j++)foreach(int side in new[]{-1,1})Box(g,"LaneDash",new Vector3(side*3,.027f,-32+j*16),new Vector3(.12f,.025f,6),ivory);
        Merge(g,"Street_"+index);
        var props=Station("StreetFurniture_"+index.ToString("D2"),distance,15);
        foreach(int side in new[]{-1,1})Prop(props,"E05",new Vector3(side*(fork?27:12),.112f,-19),5.4f,side<0?90:-90);
        if(index%3==0)Prop(props,"F01",new Vector3(-(fork?29:14.3f),.112f,12),.95f,90);
        if(index%3==1)Prop(props,"F02",new Vector3(fork?29:14.3f,.112f,13),.72f,0);
        if(index%4==2)Prop(props,"F10",new Vector3(fork?31:16,.112f,11),1.3f,0);
    }
    static void Podium(Transform parent,int index,int side,float frontage)
    {
        float length=index%3==0?34:29, depth=index%2==0?12:14, height=6.2f+(index%3)*1.35f;
        var g=Group(parent,"ArticulatedPodium_"+side);g.localPosition=new Vector3(side*(frontage+depth*.5f),0,index%2==0?2:-3);
        Material skin=(index+side+6)%3==0?terracotta:(index+side+6)%3==1?ivory:limestone;
        Box(g,"GroundedFacade",new Vector3(0,height*.5f,0),new Vector3(depth,height,length),skin);
        Box(g,"StonePlinth",new Vector3(0,.25f,0),new Vector3(depth+.3f,.5f,length+.3f),granite);
        float face=-side*(depth*.5f+.04f);
        for(int bay=0;bay<4;bay++)
        {
            float z=-length*.38f+bay*length*.255f;
            Box(g,"RecessedShopGlass",new Vector3(face,2.6f,z),new Vector3(.14f,3.9f,length*.20f),glass);
            Box(g,"LimestonePilaster",new Vector3(face-side*.13f,2.5f,z-length*.113f),new Vector3(.28f,4.3f,.28f),ivory);
            Box(g,"GlazingDoorMullion",new Vector3(face-side*.13f,2.5f,z),new Vector3(.16f,3.9f,.10f),bronze);
            Box(g,"DoorHandle",new Vector3(face-side*.23f,2.0f,z+.13f),new Vector3(.10f,.5f,.05f),bronze);
        }
        Box(g,"ContinuousCanopy",new Vector3(face-side*.65f,4.9f,0),new Vector3(1.5f,.23f,length+.7f),dark);
        Box(g,"CanopyFascia",new Vector3(face-side*1.34f,4.84f,0),new Vector3(.12f,.48f,length+.7f),index%2==0?teal:terracotta);
        Box(g,"GroundFloorCornice",new Vector3(face-side*.05f,5.35f,0),new Vector3(.42f,.36f,length+.6f),ivory);
        if(height>7)
        {
            for(int k=0;k<4;k++)Box(g,"UpperRetailWindow",new Vector3(face,6.6f,-length*.34f+k*length*.23f),new Vector3(.13f,1.6f,length*.16f),glass);
        }
        Box(g,"RoofParapet",new Vector3(0,height+.18f,0),new Vector3(depth+.48f,.36f,length+.48f),ivory);
        Box(g,"RoofServiceEnclosure",new Vector3(side*depth*.2f,height+.7f,length*.17f),new Vector3(depth*.33f,1.4f,6),dark);
        // A front-facing corner shop sign is legible in the forward gameplay camera.
        Box(g,"CornerSignBacking",new Vector3(0,4.25f,-length*.5f-.16f),new Vector3(depth-.6f,1.15f,.25f),index%2==0?teal:dark);
        Text(g,"CornerShopName",ShopNames[(index+(side>0?2:0))%ShopNames.Length],new Vector3(0,4.25f,-length*.5f-.31f),new Vector2(depth-1,1),21,Color.white);
        // Vertical short signage on the street-facing frontage reinforces Korean retail scale.
        var faceRotation=Quaternion.Euler(0,side*90,0);
        Text(g,"StreetShopName",index%2==0?"잠실  ·  JAMSIL":"서울  ·  SEOUL",new Vector3(face-side*.17f,5.6f,0),new Vector2(length*.65f,.7f),16,Color.white,faceRotation);
        Box(g,"CornerEntryRecess",new Vector3(side*depth*.16f,2.1f,-length*.5f-.03f),new Vector3(depth*.42f,3.5f,.12f),glass);
        for(int k=0;k<3;k++)Box(g,"EntryFrame",new Vector3(side*depth*.16f+(-1+k)*depth*.21f,2.1f,-length*.5f-.14f),new Vector3(.15f,3.7f,.18f),bronze);
    }
    static void Lakeside()
    {
        var g=Group(root,"SeokchonLakeDaylight");
        Box(g,"ContinuousLake",new Vector3(-174,-.21f,980),new Vector3(298,.12f,910),water);
        Box(g,"StoneLakeEdge",new Vector3(-24,-.12f,980),new Vector3(2,.38f,910),limestone);
        for(int i=0;i<15;i++)
        {
            float z=540+i*62;Box(g,"FarBankPlanting",new Vector3(-318,.9f,z),new Vector3(12,2,42),leaf);
            // Low opposite-bank pavilions keep the open lake distinct from the shops.
            if(i%4==0){Box(g,"LakePavilion",new Vector3(-299,3.6f,z),new Vector3(13,7.2f,24),ivory);Box(g,"LakePavilionGlass",new Vector3(-292.4f,3.6f,z),new Vector3(.15f,5.5f,19),glass);}
        }
        Merge(g,"Lake");
    }
    static void Intersection(float distance)
    {
        var g=Station("CrossStreet_"+distance,distance,70);
        Box(g,"CrossStreetSurface",new Vector3(0,-.018f,0),new Vector3(110,.035f,18),asphalt);
        for(int i=-7;i<=7;i++)Box(g,"ZebraStripe",new Vector3(i*1.12f,.036f,-7),new Vector3(.62f,.02f,4.6f),ivory);
        foreach(int side in new[]{-1,1})
        {
            Box(g,"SignalPost",new Vector3(side*10.4f,3.1f,-10.5f),new Vector3(.18f,6.2f,.18f),dark);
            Box(g,"TrafficLightHousing",new Vector3(side*10.4f,5.6f,-10.5f),new Vector3(.48f,1.45f,.34f),dark);
            Box(g,"SignalAmber",new Vector3(side*10.4f,5.6f,-10.69f),new Vector3(.23f,.23f,.025f),bronze);
            Box(g,"SignalBase",new Vector3(side*10.4f,.3f,-10.5f),new Vector3(.4f,.6f,.4f),granite);
        }
        Merge(g,"Cross_"+distance);
    }
    static void Plaza()
    {
        var g=Station("TowerArrivalPlaza",1665,220);
        Box(g,"PlazaPaving",new Vector3(0,-.035f,0),new Vector3(118,.06f,395),limestone);
        for(int i=-7;i<=7;i++)Box(g,"PavingLongJoint",new Vector3(i*7.5f,.002f,0),new Vector3(.055f,.012f,390),granite);
        for(int i=-7;i<=7;i++)Box(g,"PavingCrossJoint",new Vector3(0,.002f,i*25),new Vector3(116,.012f,.055f),granite);
        foreach(int side in new[]{-1,1})
        {
            Box(g,"ArrivalPlanterBase",new Vector3(side*24,.45f,28),new Vector3(3.5f,.9f,75),ivory);
            Box(g,"ArrivalLowPlanting",new Vector3(side*24,1.0f,28),new Vector3(3.1f,.45f,74),leaf);
            Box(g,"EntryBollard",new Vector3(side*9,.65f,152),new Vector3(.35f,1.3f,.35f),bronze);
        }
        Merge(g,"ArrivalPlaza");
        var furniture=Station("ArrivalReviewedFurniture",1740,95);
        foreach(int side in new[]{-1,1}){Prop(furniture,"F01",new Vector3(side*19,.009f,-13),1.0f,side>0?-90:90);Prop(furniture,"E05",new Vector3(side*17,.009f,21),6.2f,side>0?-90:90);}
    }
    static void Landmark(Transform sourceCrown)
    {
        var g=Group(root,"JamsilCanonicalTower");g.localPosition=new Vector3(0,0,1880);
        var visibility=g.gameObject.AddComponent<Chapter45SceneryGroup>();visibility.alwaysVisible=true;visibility.floor=0;
        const int sides=16,rings=14;const float height=390;var vertices=new List<Vector3>();var indices=new List<int>();
        for(int ring=0;ring<rings;ring++)
        {
            float t=ring/(float)(rings-1), y=12+t*(height-12),r=Mathf.Lerp(35,8,Mathf.Pow(t,.83f));
            for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));}
        }
        for(int i=0;i<rings-1;i++)for(int j=0;j<sides;j++){int a=i*sides+j,b=i*sides+(j+1)%sides,c=a+sides,d=b+sides;indices.AddRange(new[]{a,c,b,b,c,d});}
        var shaft=new Mesh{name="JamsilSlenderTaperedShaft"};shaft.SetVertices(vertices);shaft.SetTriangles(indices,0);shaft.RecalculateNormals();shaft.RecalculateBounds();MeshObject(g,"TaperedGlassShaft",shaft,blue);
        for(int j=0;j<8;j++)
        {
            float a=j*Mathf.PI/4;
            for(int ring=0;ring<rings-1;ring++){float ta=ring/(float)(rings-1),tb=(ring+1)/(float)(rings-1),ra=Mathf.Lerp(35,8,Mathf.Pow(ta,.83f))+.18f,rb=Mathf.Lerp(35,8,Mathf.Pow(tb,.83f))+.18f;Beam(g,"ContinuousTowerFin",new Vector3(Mathf.Cos(a)*ra,ta*height,Mathf.Sin(a)*ra),new Vector3(Mathf.Cos(a)*rb,tb*height,Mathf.Sin(a)*rb),.6f,ivory);}
        }
        // Floor bands are narrow ring ribbons, not giant disks that can intersect the shaft.
        for(float y=12;y<390;y+=12)
        {
            float r=Mathf.Lerp(35,8,Mathf.Pow(y/height,.83f))+.12f;
            for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;Beam(g,"TowerFloorRibbon",new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r),new Vector3(Mathf.Cos(b)*r,y,Mathf.Sin(b)*r),.16f,glass);}
        }
        foreach(int side in new[]{-1,1})foreach(int depth in new[]{-1,1})
        {Beam(g,"BranchingCrownSupport",new Vector3(side*5,374,depth*4),new Vector3(side*35,407,depth*9),1.8f,ivory);Beam(g,"SupportStem",new Vector3(side*5,350,depth*4),new Vector3(side*5,374,depth*4),1.4f,ivory);}
        Box(g,"CrownBearingPlatform",new Vector3(0,407,0),new Vector3(75,1.7f,25),ivory);
        // Preserve every authored FBX/LOD child transform, including root X rotation.
        var crown=Object.Instantiate(sourceCrown.gameObject,g,false);crown.name="GeneratedCrownAnchor";crown.transform.localPosition=Vector3.zero;
        foreach(var r in crown.GetComponentsInChildren<Renderer>(true)){r.enabled=true;r.forceRenderingOff=false;r.shadowCastingMode=ShadowCastingMode.Off;}
        var lod0=crown.transform.Find("Crown_LOD0");var b0=BoundsOf(lod0.gameObject);crown.transform.localScale*=105/Mathf.Max(b0.size.x,b0.size.z);b0=BoundsOf(lod0.gameObject);
        Vector3 destination=g.TransformPoint(new Vector3(0,408,0));crown.transform.position+=new Vector3(destination.x-b0.center.x,destination.y-b0.min.y,destination.z-b0.center.z);
        crown.GetComponent<LODGroup>().RecalculateBounds();
        foreach(int side in new[]{-1,1})
        {
            Box(g,"GroundedEntryPodiumWing",new Vector3(side*23,6,-29),new Vector3(17,12,21),ivory);
            Box(g,"EntranceGlazingWing",new Vector3(side*10.2f,5.2f,-39.65f),new Vector3(9,9.8f,.25f),glass);
            foreach(float x in new[]{5.7f,10.2f,14.7f})Box(g,"EntryMullion",new Vector3(side*x,5.2f,-39.85f),new Vector3(.22f,10,.24f),bronze);
        }
        // An eleven-metre open entrance stays visually passable through the physical
        // goal at1847m; glazing and the podium do not masquerade as a solid wall.
        Box(g,"PodiumRoofBehindThreshold",new Vector3(0,11.7f,-22),new Vector3(69,.6f,20),ivory);
        Box(g,"EntryCanopy",new Vector3(0,9.9f,-43.0f),new Vector3(42,.65f,8),dark);
        Text(g,"TowerIdentity","SHOE TOWER  /  슈 타워",new Vector3(0,12.4f,-39.8f),new Vector2(43,2.3f),42,Color.white);
        Merge(g,"TowerLandmark");
        notes.Add("Landmark clone preserves corrected nested Crown_LOD transforms. 390m tapered shaft, 105m crown; total top approximately452m. Scene-native low-angle and framing review required.");
    }
    static void CameraAndDaylight(Scene scene)
    {
        var player=Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.PlayerScript>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(p=>p.gameObject.scene==scene);
        var camera=Camera.main;if(camera==null||camera.gameObject.scene!=scene)throw new InvalidOperationException("Jamsil gameplay camera missing.");
        var yaw=Quaternion.Euler(0,player.transform.eulerAngles.y,0);camera.transform.SetPositionAndRotation(player.transform.position+yaw*new Vector3(0,13,-23),yaw*Quaternion.Euler(15,0,0));camera.fieldOfView=62;camera.farClipPlane=2600;
        var follow=camera.GetComponent<StableGameplayCamera>();if(follow==null)throw new InvalidOperationException("StableGameplayCamera required.");follow.Configure(player.transform);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.68f,.81f,.88f);RenderSettings.fogStartDistance=650;RenderSettings.fogEndDistance=2900;RenderSettings.ambientLight=new Color(.61f,.67f,.72f);
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(l=>l.gameObject.scene==scene&&l.type==LightType.Directional)){light.transform.rotation=Quaternion.Euler(43,-32,0);light.color=new Color(1,.95f,.86f);light.intensity=1.05f;}
        notes.Add("Portrait camera pitch15/FOV62 keeps hero lower in frame and admits the distant crown. Assess HUD overlap and mid-route top-of-tower crop in native captures.");
        notes.Add("All art is grouped by shared material; nearby facade/furniture groups use existing route visibility. Imported props use reviewed FBX source geometry and materials, authored root orientation preserved.");
    }
}
