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

// Append-only asset revisions; changes only Jamsil's visual presentation.
// Existing gameplay, collider and camera records must compare exactly before save.
public static class ApplyChapter4ReferenceStreet
{
    const string ScenePath = "Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity";
    const string RootName = "Jamsil_ReferenceStreet_20261002";
    const string Out = "outputs/chapter4-reference-2026-10-02";
    static string art;
    static Transform root;
    static Transform distantStreet;
    static Chapter45Director director;
    static TMP_FontAsset font;
    static Material cream, stone, brick, teal, wood, glass, ink, metal, warm, leaf;
    static int meshCount, peopleCount, shopCount;
    static string retainedBefore, retainedAfter;

    public static object InspectReopenStability()
    {
        RequireIdle(); var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var s=EditorSceneManager.OpenScene(ScenePath);var a=ProtectedState(s).Split('\n');
            s=EditorSceneManager.OpenScene(ScenePath);var b=ProtectedState(s).Split('\n');
            var oldRows=a.Except(b).ToArray();var newRows=b.Except(a).ToArray();
            return new {beforeCount=a.Length,afterCount=b.Length,removedCount=oldRows.Length,addedCount=newRows.Length,
                removed=oldRows.Take(2).Select(r=>r.Substring(0,Math.Min(300,r.Length))).ToArray(),
                added=newRows.Take(2).Select(r=>r.Substring(0,Math.Min(300,r.Length))).ToArray()};
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }


    public static object Main()
    {
        RequireIdle();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        bool saved = false;
        try
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            director = Object.FindFirstObjectByType<Chapter45Director>();
            string before = ProtectedState(scene); retainedBefore=before;
            string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
            art = "Assets/ShooterSurvival/Models/Chapters/Chapters45/ReferenceStreet/" + stamp;
            Directory.CreateDirectory(art); Directory.CreateDirectory(Out); AssetDatabase.Refresh();
            var old = director.transform.Find("Scenery/" + RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            root = Group(director.transform.Find("Scenery"), RootName);
            distantStreet=Group(root,"Continuous pavement and distant frontage");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");
            meshCount = peopleCount = shopCount = 0;
            Materials();
            var existing = director.transform.Find("Scenery/Jamsil_CityPolish_V1");
            if (existing == null) throw new InvalidOperationException("Existing refined city is required.");
            // Replace only the old retail bays. Landmark, lake, plaza, furniture and
            // short highway remain owned by their existing presentation groups.
            foreach (Transform child in existing)
                if (child.name.StartsWith("BoulevardBlock_"))
                    foreach (var renderer in child.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;

            int block = 0;
            for (float d = 160; d <= 1450; d += 64) Street(block++, d);
            Merge(distantStreet,"ContinuousStreet");
            if (before != ProtectedState(scene)) throw new InvalidOperationException("Protected gameplay/camera/collider records changed.");
            if (root.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidOperationException("Scenery must not add physical obstacles.");
            foreach (var asset in AssetDatabase.FindAssets("", new[] { art }))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(asset)));
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
            saved = true;
            EditorSceneManager.OpenScene(ScenePath);
            director = Object.FindFirstObjectByType<Chapter45Director>();
            root = director.transform.Find("Scenery/" + RootName);
            var receipt = new { scene = ScenePath, art, blocks = block, shops = shopCount, pedestrians = peopleCount,
                generatedMeshes = meshCount, addedColliders = root.GetComponentsInChildren<Collider>(true).Length,
                renderers = root.GetComponentsInChildren<Renderer>(true).Length,
                protectedStateUnchanged = before == (retainedAfter=ProtectedState(SceneManager.GetActiveScene())),
                screenshotAndOrdinaryGameplayPending = true };
            if (!receipt.protectedStateUnchanged)
            {
                var a=before.Split('\n');var b=retainedAfter.Split('\n');
                File.WriteAllText(Out+"/preservation-difference-"+stamp+".txt","REMOVED\n"+string.Join("\n",a.Except(b))+"\nADDED\n"+string.Join("\n",b.Except(a)));
                throw new InvalidOperationException("Saved scene preservation failed; retained field diff.");
            }
            File.WriteAllText(Out + "/street-apply-" + stamp + ".json", JsonUtility.ToJson(new Receipt { scene=ScenePath, art=art, blocks=block, shops=shopCount, pedestrians=peopleCount, meshes=meshCount, protectedStateUnchanged=true }, true));
            return receipt;
        }
        finally
        {
            if (!saved && SceneManager.GetActiveScene().path == ScenePath)
                EditorSceneManager.OpenScene(ScenePath);
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
    [Serializable] sealed class Receipt { public string scene, art; public int blocks, shops, pedestrians, meshes; public bool protectedStateUnchanged; }
    public static void RequireIdle()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Idle editor required.");
        for (int i=0;i<SceneManager.sceneCount;i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene present.");
    }
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    static string ProtectedState(Scene scene)
    {
        var records = new List<string>();
        foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))
        {
            if (c == null || PathOf(c.transform).Contains(RootName)) continue;
            bool protect = c is Collider || c is Rigidbody || c is Camera || c is MonoBehaviour && !(c is TMP_Text);
            if (!protect) continue;
            // Serialized object references use stable GlobalObjectId, not session instance IDs.
            var s = new SerializedObject(c); var it = s.GetIterator();
            var fields = new List<string>();
            bool descend=true;
            while (it.Next(descend))
            {
                // Object references expose an internal session m_FileID child.
                // Compare the stable reference above, never descend into its storage.
                descend=it.propertyType==SerializedPropertyType.Generic;
                if (it.propertyType == SerializedPropertyType.ObjectReference)
                    fields.Add(it.propertyPath + "=" + (it.objectReferenceValue == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(it.objectReferenceValue).ToString()));
                else if (it.propertyType != SerializedPropertyType.Generic)
                    fields.Add(it.propertyPath + "=" + it.type + ":" + Scalar(it));
            }
            records.Add(PathOf(c.transform)+":"+c.GetType().FullName+":"+c.transform.localToWorldMatrix.ToString("R")+":"+string.Join("|",fields));
        }
        return string.Join("\n", records.OrderBy(r=>r,StringComparer.Ordinal));
    }
    static string Scalar(SerializedProperty p)
    {
        switch(p.propertyType)
        {
            case SerializedPropertyType.Integer: return p.longValue.ToString();
            case SerializedPropertyType.Boolean: return p.boolValue.ToString();
            case SerializedPropertyType.Float: return p.doubleValue.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            case SerializedPropertyType.String: return p.stringValue;
            case SerializedPropertyType.Enum: return p.intValue.ToString();
            case SerializedPropertyType.ArraySize: return p.intValue.ToString();
            case SerializedPropertyType.Color: return p.colorValue.ToString("R");
            case SerializedPropertyType.Vector2: return p.vector2Value.ToString("R");
            case SerializedPropertyType.Vector3: return p.vector3Value.ToString("R");
            case SerializedPropertyType.Vector4: return p.vector4Value.ToString("R");
            case SerializedPropertyType.Quaternion: return p.quaternionValue.ToString("R");
            default: return p.propertyType.ToString();
        }
    }
    static Material Mat(string name, Color color, float smooth=.2f, float metallic=0)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name=name, enableInstancing=true };
        m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);
        AssetDatabase.CreateAsset(m,art+"/"+name+".mat");return m;
    }
    static void Materials()
    {
        cream=Mat("Warm limestone",new Color(.86f,.81f,.69f));
        stone=Mat("Pavement",new Color(.55f,.55f,.49f));
        brick=Mat("Clay pavers",new Color(.49f,.25f,.22f));
        teal=Mat("Painted jade",new Color(.14f,.32f,.30f));
        wood=Mat("Oak and terracotta",new Color(.38f,.22f,.13f));
        glass=Mat("Blue reflected glass",new Color(.16f,.31f,.37f),.7f,.28f);
        ink=Mat("Charcoal metal",new Color(.075f,.10f,.12f),.35f,.35f);
        metal=Mat("Brass edge",new Color(.57f,.46f,.27f),.55f,.65f);
        warm=Mat("Warm shop light",new Color(.95f,.71f,.38f));
        warm.EnableKeyword("_EMISSION");warm.SetColor("_EmissionColor",new Color(.48f,.25f,.075f));
        leaf=Mat("Leaf green",new Color(.21f,.34f,.16f));
        // World-facing repeating tile grain. No screen effect or extra draw pass.
        var texture=new Texture2D(128,128,TextureFormat.RGB24,true){name="Paving fine grain",wrapMode=TextureWrapMode.Repeat};
        var random=new System.Random(4102);var pixels=new Color[128*128];
        for(int y=0;y<128;y++)for(int x=0;x<128;x++)
        {bool joint=y%32<2 || (x+(y/32%2)*32)%64<2;float v=joint?.69f:.93f+(float)random.NextDouble()*.07f;pixels[y*128+x]=new Color(v,v,v);}
        texture.SetPixels(pixels);texture.Apply(true);AssetDatabase.CreateAsset(texture,art+"/Paving grain.asset");
        brick.mainTexture=texture;stone.mainTexture=texture;
        brick.mainTextureScale=new Vector2(12,32);stone.mainTextureScale=new Vector2(8,16);
    }
    static Transform Group(Transform parent,string name){var g=new GameObject(name).transform;g.SetParent(parent,false);return g;}
    static Transform Station(string name,float station,float half=32)
    {
        var g=Group(root,name);director.route.Sample(station,out var p,out var f);g.SetPositionAndRotation(p,Quaternion.LookRotation(f));
        var visibility=g.gameObject.AddComponent<Chapter45SceneryGroup>();visibility.floor=0;visibility.startDistance=station-half;visibility.endDistance=station+half;return g;
    }
    static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;
        Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;return g.transform;
    }
    static void Beam(Transform p,string name,Vector3 a,Vector3 b,float width,Material m)
    {var g=Box(p,name,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),m);g.localRotation=Quaternion.LookRotation(b-a);}
    static void Label(Transform parent,string text,Vector3 p,Vector2 size,Quaternion rotation)
    {
        var g=Group(parent,"Sign "+text);g.localPosition=p;g.localRotation=rotation;
        var t=g.gameObject.AddComponent<TextMeshPro>();t.font=font;t.text=text;t.fontSize=8;t.enableAutoSizing=true;t.fontSizeMax=8;t.fontSizeMin=2;
        t.alignment=TextAlignmentOptions.Center;t.color=new Color(.98f,.91f,.76f);t.textWrappingMode=TextWrappingModes.NoWrap;t.rectTransform.sizeDelta=size;
        t.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    static void Street(int index,float d)
    {
        bool branch=d>500&&d<910;float curb=branch?25.8f:8.4f;float facade=branch?32f:14.2f;
        var g=Station("Retail street "+index,d);
        var ground=Group(distantStreet,"Ground "+index);ground.SetPositionAndRotation(g.position,g.rotation);
        foreach(int side in new[]{-1,1})
        {
            Box(g,"Pavement",new Vector3(side*(curb+3.1f),.06f,0),new Vector3(6.2f,.12f,64.3f),stone);
            Box(g,"Curb",new Vector3(side*curb,.14f,0),new Vector3(.25f,.28f,64.3f),cream);
            for(int j=0;j<8;j++)Box(g,"Stone joint",new Vector3(side*(curb+3),.125f,-28+j*8),new Vector3(6,.009f,.055f),cream);
            bool lakeside=side<0&&d>540&&d<1370;
            if(!lakeside)
            {
                for(int bay=0;bay<5;bay++) Shop(g,index*5+bay,side,facade,-25.6f+bay*12.8f);
                // Cheap recessed shells continue the street into the distance.
                // Close detailed bays completely cover their outer faces.
                Box(ground,"Distant retail mass",new Vector3(side*(facade+3.7f),3.6f,0),new Vector3(4.6f,7.2f,64.1f),index%2==0?cream:teal);
                Box(ground,"Distant window band",new Vector3(side*(facade+1.36f),2.4f,0),new Vector3(.08f,3.7f,63.8f),glass);
            }
            else
                for(int j=0;j<3;j++)Planter(g,new Vector3(side*(curb+4),.13f,-24+j*24),j);
            if(index%2==0)Planter(g,new Vector3(side*(curb+1.4f),.13f,28),index);
            for(int j=0;j<2;j++)
            {
                float z=-14+j*27;float x=side*(curb+2.4f);
                var board=Box(g,"A frame menu",new Vector3(x,1.1f,z),new Vector3(.9f,1.55f,.10f),ink);board.localRotation=Quaternion.Euler(8,side<0?25:-25,0);
                Box(g,"Menu cream rule",new Vector3(x,1.47f,z-.075f),new Vector3(.67f,.08f,.025f),cream);
                Box(g,"Menu line",new Vector3(x,1.15f,z-.09f),new Vector3(.57f,.04f,.025f),cream);
                Beam(g,"Menu back leg",new Vector3(x,.13f,z+.7f),new Vector3(x,1.85f,z),.07f,wood);
            }
        }
        if(!branch)
        {
            // Pavers cover only the former visual asphalt, above its unchanged collider.
            Box(ground,"Pedestrian clay surface",new Vector3(0,.065f,0),new Vector3(16.55f,.02f,64.3f),brick);
            for(int side=-1;side<=1;side+=2)Box(ground,"Drain channel",new Vector3(side*7.7f,.08f,0),new Vector3(.14f,.02f,64.3f),ink);
        }
        Merge(g,"Block"+index);
        if(index%2==0)
        {
            foreach(int side in new[]{-1,1}) Tree(g,new Vector3(side*(curb+3.5f),.13f,30));
        }
        // Existing physical choice lanes are wider here. Keep the crowd outside both.
        People(index,d,branch?27.1f:9.25f);
    }
    static void Shop(Transform parent,int index,int side,float facade,float z)
    {
        var g=Group(parent,"Shop bay "+index);g.localPosition=new Vector3(side*facade,0,z);g.localRotation=Quaternion.Euler(0,side*90,0);
        // Local -Z faces the street. Recesses have real depth behind the facade.
        float h=7.2f+(index%3)*1.1f;Material skin=index%3==0?wood:index%3==1?cream:teal;
        Box(g,"Building mass",new Vector3(0,h*.5f,3.2f),new Vector3(12.6f,h,5.6f),skin);
        Box(g,"Recess dark back",new Vector3(0,2.3f,.18f),new Vector3(11.8f,4.1f,.18f),ink);
        for(int b=0;b<3;b++)
        {
            float x=-4+b*4;
            Box(g,"Window reflection",new Vector3(x,2.55f,-.07f),new Vector3(3.6f,3.15f,.10f),glass);
            Box(g,"Display warm panel",new Vector3(x,1.95f,-.15f),new Vector3(3.05f,1.9f,.08f),warm);
            Box(g,"Display shelf",new Vector3(x,1.3f,-.35f),new Vector3(3.3f,.14f,.60f),wood);
            for(int k=0;k<4;k++)Box(g,"Merchandise",new Vector3(x-1.0f+k*.68f,1.63f,-.32f),new Vector3(.39f,.55f+(k%2)*.25f,.36f),k%2==0?cream:teal);
            Box(g,"Window transom",new Vector3(x,3.78f,-.22f),new Vector3(3.65f,.09f,.12f),metal);
            Box(g,"Window mullion",new Vector3(x-1.85f,2.55f,-.23f),new Vector3(.12f,3.6f,.16f),cream);
        }
        Box(g,"Entry dark glass",new Vector3(3.7f,2.0f,-.35f),new Vector3(1.5f,3.55f,.1f),glass);
        Box(g,"Door handle",new Vector3(3.18f,1.65f,-.45f),new Vector3(.065f,.66f,.075f),metal);
        Box(g,"Stone footing",new Vector3(0,.28f,-.25f),new Vector3(12.4f,.45f,.7f),cream);
        Box(g,"Shop sign fascia",new Vector3(0,4.65f,-.43f),new Vector3(12.6f,.85f,.48f),index%2==0?teal:ink);
        Box(g,"Canopy",new Vector3(0,4.16f,-.95f),new Vector3(12.7f,.15f,1.8f),index%2==0?cream:wood);
        Box(g,"Warm strip",new Vector3(0,4.04f,-1.45f),new Vector3(11.9f,.06f,.14f),warm);
        for(int w=0;w<4;w++)
        {
            float x=-4.65f+w*3.1f;
            Box(g,"Upper window",new Vector3(x,6.2f,-.10f),new Vector3(2.4f,1.65f,.14f),glass);
            Box(g,"Upper sill",new Vector3(x,5.33f,-.25f),new Vector3(2.65f,.13f,.4f),cream);
            Box(g,"Upper sash",new Vector3(x,6.2f,-.2f),new Vector3(.08f,1.65f,.1f),metal);
        }
        Box(g,"Roof trim",new Vector3(0,h+.13f,2.6f),new Vector3(12.8f,.26f,6.1f),cream);
        string[] names={"JAMSIL COFFEE","BOOKS & OBJECTS","BREAD ATELIER","SEOUL OPTIC","STUDIO 04","FLOWER MARKET","DAILY SUPPLY","LAKE RECORDS"};
        // A perpendicular hanging sign faces the forward camera as well as walkers.
        var sign=Group(g,"Projecting sign");sign.localPosition=new Vector3(-5.8f,3.6f,-1.35f);sign.localRotation=Quaternion.Euler(0,90,0);
        Box(sign,"Sign body",Vector3.zero,new Vector3(1.35f,1.7f,.14f),index%2==0?teal:wood);
        Label(sign,index%2==0?"COFFEE":"SHOP",new Vector3(0,0,-.08f),new Vector2(1.28f,1.6f),Quaternion.identity);
        Label(sign,index%2==0?"COFFEE":"SHOP",new Vector3(0,0,.08f),new Vector2(1.28f,1.6f),Quaternion.Euler(0,180,0));
        Label(g,names[index%names.Length],new Vector3(0,4.65f,-.68f),new Vector2(11.9f,.76f),Quaternion.identity);
        shopCount++;
    }
    static void Planter(Transform parent,Vector3 p,int seed)
    {
        Box(parent,"Planting stone",p+Vector3.up*.38f,new Vector3(1.8f,.76f,2.1f),cream);
        Box(parent,"Planter earth",p+Vector3.up*.78f,new Vector3(1.6f,.04f,1.9f),wood);
        // Faceted volume; small foliage is merged into the block material batch.
        for(int i=0;i<4;i++)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Shrub";go.transform.SetParent(parent,false);
            go.transform.localPosition=p+new Vector3((i%2-.5f)*.72f,1.15f,(i/2-.5f)*.8f);
            go.transform.localScale=new Vector3(1,.9f+(seed%3)*.15f,1.1f);Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=leaf;
        }
    }
    static void People(int index,float station,float edge)
    {
        var g=Station("Pedestrians "+index,station);var life=g.gameObject.AddComponent<Chapter4StreetLife>();life.director=director;life.station=station;
        string[] ids={"A01_dad","A02_mom","A05_student","A06_grandma"};var walkers=new List<Chapter4StreetLife.Walker>();
        for(int i=0;i<12;i++)
        {
            int side=i%2==0?-1:1;var holder=Group(g,"Pedestrian "+i);holder.localPosition=new Vector3(side*(edge+(i/2%2)*1.1f),.13f,-27+(i/2)*10.8f);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/"+ids[(index+i)%ids.Length]+".prefab");
            var person=Object.Instantiate(source,holder,false);person.name=ids[(index+i)%ids.Length];
            foreach(var c in person.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            foreach(var c in person.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(c);
            var animator=person.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullCompletely;
            var clip=animator.runtimeAnimatorController.animationClips.First(c=>c.name=="walk");clip.SampleAnimation(animator.gameObject,.3f);
            var rs=person.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
            person.transform.localScale*=2.35f/b.size.y;b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
            person.transform.position+=new Vector3(holder.position.x-b.center.x,holder.position.y-b.min.y,holder.position.z-b.center.z);
            foreach(var r in rs)r.shadowCastingMode=ShadowCastingMode.Off;
            walkers.Add(new Chapter4StreetLife.Walker{body=holder,animator=animator,origin=holder.localPosition,phase=index*.7f+i*1.4f,travel=3.5f});peopleCount++;
        }
        life.walkers=walkers.ToArray();
    }
    static void Tree(Transform parent,Vector3 p)
    {
        const string path="Assets/polyperfect/Poly Universal Pack/Prefabs/Nature/Trees City/Tree_Japanese_Pagoda_A.prefab";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(source==null)throw new FileNotFoundException(path);
        var holder=Group(parent,"Street tree");holder.localPosition=p;
        var tree=Object.Instantiate(source,holder,false);
        foreach(var collider in tree.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
        foreach(var script in tree.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
        var rs=tree.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new InvalidOperationException("Tree contains no renderers.");
        var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
        if(b.size.y<2 || Mathf.Max(b.size.x,b.size.z)>b.size.y*1.6f)throw new InvalidOperationException("Expected a whole upright tree, not a pot or ground cover.");
        tree.transform.localScale*=4.8f/b.size.y;b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
        tree.transform.position+=new Vector3(holder.position.x-b.center.x,holder.position.y-b.min.y,holder.position.z-b.center.z);
        foreach(var r in rs)r.shadowCastingMode=ShadowCastingMode.Off;
    }
    static void Merge(Transform parent,string key)
    {
        var filters=parent.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<TMP_Text>()==null).ToArray();
        var batches=new Dictionary<Material,List<CombineInstance>>();
        foreach(var f in filters)
        {
            var m=f.GetComponent<MeshRenderer>().sharedMaterial;if(!batches.TryGetValue(m,out var list))batches[m]=list=new List<CombineInstance>();
            list.Add(new CombineInstance{mesh=f.sharedMesh,transform=parent.worldToLocalMatrix*f.transform.localToWorldMatrix});
        }
        foreach(var batch in batches)
        {
            var mesh=new Mesh{name=key+" "+batch.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(batch.Value.ToArray());mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,art+"/"+(meshCount++).ToString("D4")+".asset");
            var g=Group(parent,mesh.name);g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=batch.Key;r.shadowCastingMode=ShadowCastingMode.Off;
        }
        foreach(var f in filters){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
    }
}
