using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class ImportHighwayRebuild
{
    const string Root="Assets/ShooterSurvival/Models/Highway/Rebuilt20260923/";
    static string currentName;
    static AnimatorController controller;
    static Material bodyMaterial,steel,orange,cream,navy;
    static GameObject model;
    static float releaseDelay;
    public static string Main(string name,string revision="v1")
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        currentName=name;string source="outputs/highway-enemy-rebuild-2026-09-23/rigged/"+revision+"/"+name;
        if(!File.Exists(source+"/rig-report.json"))throw new Exception("Validated rig required");
        if(!File.Exists("tmp/image-previews/highway-enemy-rebuild-2026-09-23/"+name+"-fbx-"+revision+"/inspection.json"))throw new Exception("Fresh FBX motion check required");
        string production=Directory.GetDirectories("outputs/highway-enemy-rebuild-2026-09-23/production","*"+name+"_*").Single();
        string folder=Root+name;Directory.CreateDirectory(folder);File.Copy(source+"/"+name+".fbx",folder+"/"+name+".fbx",true);File.Copy(production+"/textures/BaseColor.png",folder+"/BaseColor.png",true);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string fbx=folder+"/"+name+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.SaveAndReimport();
        var clips=importer.defaultClipAnimations;foreach(var c in clips){c.loopTime=!(c.name.EndsWith("die")||c.name.EndsWith("attack_once")||c.name.EndsWith("hit"));c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;}importer.clipAnimations=clips;importer.SaveAndReimport();
        var tex=(TextureImporter)AssetImporter.GetAtPath(folder+"/BaseColor.png");tex.maxTextureSize=2048;tex.mipmapEnabled=true;tex.SaveAndReimport();
        bodyMaterial=Surface(folder+"/Body.mat",Color.white);bodyMaterial.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/BaseColor.png"));bodyMaterial.SetFloat("_TextureImpact",1);bodyMaterial.EnableKeyword("_TEXTUREBLENDINGMODE_MULTIPLY");
        steel=Surface(Root+"ToolSteel.mat",new Color(.56f,.66f,.73f));orange=Surface(Root+"ToolOrange.mat",new Color(.95f,.34f,.035f));cream=Surface(Root+"ToolCream.mat",new Color(.96f,.91f,.75f));navy=Surface(Root+"ToolNavy.mat",new Color(.055f,.085f,.14f));
        string controllerPath=folder+"/Actions.controller";controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var sm=controller.layers[0].stateMachine;foreach(var state in sm.states)sm.RemoveState(state.state);
        var animations=AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
        releaseDelay=animations.Single(c=>c.name=="attack_once"||c.name.EndsWith("|attack_once")).length*.46f;
        foreach(string action in new[]{"idle","walk","run","attack_loop","attack_once","hit","die"}){var clip=animations.Single(c=>c.name==action||c.name.EndsWith("|"+action));var state=sm.AddState(action);state.motion=clip;if(action=="idle")sm.defaultState=state;}
        model=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        string prefab="Assets/ShooterSurvival/Prefabs/Highway/Enemies/"+name+".prefab";Backup(prefab);
        var root=PrefabUtility.LoadPrefabContents(prefab);try{Install(root);PrefabUtility.SaveAsPrefabAsset(root,prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;const string scenePath="Assets/ShooterSurvival/Scenes/Tools/HighWay.unity";Backup(scenePath);var scene=EditorSceneManager.OpenScene(scenePath);
        int placed=0;foreach(var actor in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<EnemyScript_space>(true)))
        {
            string linked=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor.gameObject);
            if(linked!=prefab&&!actor.name.Contains(name))continue;Install(actor.gameObject);placed++;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(original);
        return $"{name}: saved source prefab and {placed} highway placements; seven clips, fitted body, hand tool and full death duration.";
    }
    static void Install(GameObject root)
    {
        var combat=root.GetComponent<EnemyScript_space>();var data=new SerializedObject(combat);var held=data.FindProperty("heldProjectile").objectReferenceValue as Transform;
        if(held!=null)held.SetParent(root.transform,true);
        foreach(var old in root.GetComponents<EnemyGunAim>())UnityEngine.Object.DestroyImmediate(old);
        foreach(var old in root.GetComponents<EnemyGroundedPose>())UnityEngine.Object.DestroyImmediate(old);
        var prior=root.GetComponentInChildren<Animator>(true);if(prior==null||prior.gameObject==root)throw new Exception("Unexpected old visual");UnityEngine.Object.DestroyImmediate(prior.gameObject);
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(model,root.transform);visual.name="Body";visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
        foreach(var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)){renderer.sharedMaterials=Enumerable.Repeat(bodyMaterial,renderer.sharedMaterials.Length).ToArray();renderer.forceMatrixRecalculationPerRender=true;Record(renderer);}
        var animator=visual.GetComponent<Animator>();if(animator==null)animator=visual.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;Record(animator);
        var skin=visual.GetComponentInChildren<SkinnedMeshRenderer>();var hand=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Hand.R");int index=Array.IndexOf(skin.bones,hand);
        var verts=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;Vector3 center=Vector3.zero;int count=0;
        for(int i=0;i<verts.Length;i++){var w=weights[i];float weight=w.boneIndex0==index?w.weight0:w.boneIndex1==index?w.weight1:w.boneIndex2==index?w.weight2:w.boneIndex3==index?w.weight3:0;if(weight>.8f){center+=skin.sharedMesh.bindposes[index].MultiplyPoint3x4(verts[i]);count++;}}
        if(count==0)throw new Exception("No hand-bound vertices");center/=count;
        var grip=new GameObject("PalmGrip").transform;grip.SetParent(hand,false);grip.localPosition=center;grip.rotation=visual.transform.rotation;
        var scale=hand.lossyScale;grip.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
        if(held!=null)
        {
            if(currentName!="TrafficPatrol")
            {
                UnityEngine.Object.DestroyImmediate(held.gameObject);
                held=BuildThrownProp(grip,currentName);
            }
            held.SetParent(grip,false);held.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);held.localScale=Vector3.one*.45f;
            if(currentName!="TrafficPatrol")held.localScale=Vector3.one;
            data.Update();data.FindProperty("heldProjectile").objectReferenceValue=held;data.FindProperty("throwPoint").objectReferenceValue=grip;data.FindProperty("hideHeldProjectile").boolValue=currentName=="TrafficPatrol";data.FindProperty("throwReleaseDelay").floatValue=releaseDelay;data.ApplyModifiedPropertiesWithoutUndo();
        }
        if(currentName=="ConeMechanic")
        {
            Primitive(PrimitiveType.Cylinder,grip,"WrenchHandle",new Vector3(0,.14f,0),new Vector3(.065f,.26f,.065f),steel);
            Primitive(PrimitiveType.Cube,grip,"WrenchJawL",new Vector3(-.07f,.43f,0),new Vector3(.08f,.2f,.075f),steel);
            Primitive(PrimitiveType.Cube,grip,"WrenchJawR",new Vector3(.07f,.43f,0),new Vector3(.08f,.2f,.075f),steel);
            Primitive(PrimitiveType.Cube,grip,"WrenchBridge",new Vector3(0,.34f,0),new Vector3(.19f,.075f,.075f),steel);
            Primitive(PrimitiveType.Cylinder,grip,"OrangeGrip",new Vector3(0,-.015f,0),new Vector3(.085f,.10f,.085f),orange);
        }
        else if(currentName=="AsphaltWorker")
        {
            Primitive(PrimitiveType.Cylinder,grip,"ShovelShaft",new Vector3(0,-.24f,0),new Vector3(.065f,.42f,.065f),cream);
            Primitive(PrimitiveType.Cube,grip,"ShovelBlade",new Vector3(0,-.73f,.02f),new Vector3(.38f,.34f,.08f),steel);
            Primitive(PrimitiveType.Cube,grip,"ShovelGrip",new Vector3(0,.19f,0),new Vector3(.24f,.08f,.09f),navy);
        }
        else if(currentName=="TrafficPatrol")
        {
            var gun=new GameObject("PatrolLauncher").transform;gun.SetParent(grip,false);
            Primitive(PrimitiveType.Cube,gun,"Grip",Vector3.zero,new Vector3(.10f,.20f,.12f),navy);
            Primitive(PrimitiveType.Cube,gun,"Receiver",new Vector3(0,.13f,.13f),new Vector3(.15f,.17f,.35f),cream);
            Primitive(PrimitiveType.Cube,gun,"Barrel",new Vector3(0,.13f,.36f),new Vector3(.105f,.105f,.20f),navy);
            var muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(gun,false);muzzle.localPosition=new Vector3(0,.13f,.47f);
            var aim=root.AddComponent<EnemyGunAim>();aim.gun=gun;aim.muzzle=muzzle;aim.grip=grip;aim.localGripPoint=Vector3.zero;aim.localBarrelAxis=Vector3.forward;
            if(held!=null){data.Update();data.FindProperty("throwPoint").objectReferenceValue=muzzle;data.ApplyModifiedPropertiesWithoutUndo();}Record(aim);
        }
        var presentation=root.GetComponent<HighwayEnemyAnimation>();if(presentation==null)presentation=root.AddComponent<HighwayEnemyAnimation>();presentation.animator=animator;presentation.chest=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Chest");presentation.head=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Head");presentation.deathSeconds=1.05f;Record(presentation);
        var hit=root.transform.Find("Walker-HitPos");if(hit!=null){hit.localPosition=Vector3.up*1.45f;Record(hit);}Record(combat);Record(visual.transform);
    }
    static void Primitive(PrimitiveType type,Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=material;
    }
    static Transform BuildThrownProp(Transform grip,string name)
    {
        var root=new GameObject(name=="TireBruiser"?"ThrownTire":name=="TollgateChief"?"ThrownCone":"DeliveryParcel").transform;root.SetParent(grip,false);
        if(name=="TireBruiser")
        {
            string path=Root+"ThrownTire.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                var verts=new Vector3[32*12];var uv=new Vector2[verts.Length];var tris=new int[32*12*6];int k=0;
                for(int i=0;i<32;i++)for(int j=0;j<12;j++)
                {
                    float u=i*Mathf.PI*2/32,v=j*Mathf.PI*2/12,r=.39f+.12f*Mathf.Cos(v);int index=i*12+j;
                    verts[index]=new Vector3(.12f*Mathf.Sin(v),-.39f+r*Mathf.Cos(u),r*Mathf.Sin(u));uv[index]=new Vector2(i/32f,j/12f);
                    int a=index,b=((i+1)%32)*12+j,c=((i+1)%32)*12+(j+1)%12,d=i*12+(j+1)%12;tris[k++]=a;tris[k++]=b;tris[k++]=c;tris[k++]=a;tris[k++]=c;tris[k++]=d;
                }
                mesh=new Mesh(){name="ThrownTire",vertices=verts,uv=uv,triangles=tris};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=navy;
        }
        else if(name=="TollgateChief")
        {
            string path=Root+"ThrownCone.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                var verts=new Vector3[66];var triangles=new int[32*12];int k=0;
                for(int i=0;i<32;i++){float t=i*Mathf.PI*2/32;verts[i]=new Vector3(Mathf.Cos(t)*.25f,-.55f,Mathf.Sin(t)*.25f);verts[i+32]=new Vector3(Mathf.Cos(t)*.025f,.12f,Mathf.Sin(t)*.025f);}
                verts[64]=new Vector3(0,-.55f,0);verts[65]=new Vector3(0,.12f,0);
                for(int i=0;i<32;i++){int j=(i+1)%32;foreach(int index in new[]{i,i+32,j+32,i,j+32,j,64,i,j,65,j+32,i+32})triangles[k++]=index;}
                mesh=new Mesh(){name="ThrownCone",vertices=verts,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            var cone=new GameObject("Cone",typeof(MeshFilter),typeof(MeshRenderer));cone.transform.SetParent(root,false);cone.GetComponent<MeshFilter>().sharedMesh=mesh;cone.GetComponent<MeshRenderer>().sharedMaterial=orange;
            Primitive(PrimitiveType.Cube,root,"ConeBase",new Vector3(0,-.56f,0),new Vector3(.6f,.07f,.6f),navy);
            Primitive(PrimitiveType.Cylinder,root,"ReflectiveBand",new Vector3(0,-.19f,0),new Vector3(.27f,.035f,.27f),cream);
        }
        else
        {
            Primitive(PrimitiveType.Cube,root,"Parcel",new Vector3(0,-.08f,.18f),new Vector3(.54f,.48f,.5f),orange);
            Primitive(PrimitiveType.Cube,root,"PackingTape",new Vector3(0,-.08f,.435f),new Vector3(.09f,.49f,.01f),cream);
            Primitive(PrimitiveType.Cube,root,"TopTape",new Vector3(0,.165f,.18f),new Vector3(.09f,.01f,.5f),cream);
        }
        var collider=root.gameObject.AddComponent<SphereCollider>();collider.isTrigger=true;collider.radius=name=="TireBruiser"?.51f:.3f;collider.center=name=="TireBruiser"?Vector3.down*.39f:name=="TollgateChief"?Vector3.down*.22f:new Vector3(0,-.08f,.18f);
        var rigid=root.gameObject.AddComponent<Rigidbody>();rigid.isKinematic=true;rigid.useGravity=false;root.gameObject.AddComponent<SimpleProjectile>();return root;
    }
    static Material Surface(string path,Color color)
    {
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("FlatKit/Stylized Surface"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);mat.SetColor("_ColorDim",new Color(.62f,.68f,.77f));mat.SetFloat("_LightContribution",.12f);mat.SetFloat("_ShadowEdgeSize",.1f);mat.EnableKeyword("_CELPRIMARYMODE_SINGLE");mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
    }
    static void Record(UnityEngine.Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
    static void Backup(string path){string target="tmp/highway-enemy-rebuild-2026-09-23/before/"+path;Directory.CreateDirectory(Path.GetDirectoryName(target));if(!File.Exists(target))File.Copy(path,target);}
}
