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
        var prior=root.GetComponentsInChildren<Animator>(true);if(prior.Length==0||prior.Any(a=>a.gameObject==root))throw new Exception("Unexpected old visual");foreach(var old in prior)if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(model,root.transform);visual.name="Body";visual.transform.localPosition=Vector3.down*.1f;visual.transform.localRotation=Quaternion.identity;
        foreach(var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)){renderer.sharedMaterials=Enumerable.Repeat(bodyMaterial,renderer.sharedMaterials.Length).ToArray();renderer.forceMatrixRecalculationPerRender=true;Record(renderer);}
        var animator=visual.GetComponent<Animator>();if(animator==null)animator=visual.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;Record(animator);
        var skin=visual.GetComponentInChildren<SkinnedMeshRenderer>();var hand=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Hand.R");int index=Array.IndexOf(skin.bones,hand);
        var verts=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;Vector3 center=Vector3.zero;int count=0;
        for(int i=0;i<verts.Length;i++){var w=weights[i];float weight=w.boneIndex0==index?w.weight0:w.boneIndex1==index?w.weight1:w.boneIndex2==index?w.weight2:w.boneIndex3==index?w.weight3:0;if(weight>.8f){center+=skin.sharedMesh.bindposes[index].MultiplyPoint3x4(verts[i]);count++;}}
        if(count==0)throw new Exception("No hand-bound vertices");center/=count;
        var grip=new GameObject("PalmGrip").transform;grip.SetParent(hand,false);grip.localPosition=center;grip.rotation=visual.transform.rotation;
        var scale=hand.lossyScale;grip.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
        bool ranged=currentName=="TrafficPatrol"||currentName=="TireBruiser"||currentName=="DeliveryRider"||currentName=="TollgateChief";
        if(ranged)
        {
            if(currentName=="TrafficPatrol"&&held==null)
            {
                var donor=AssetDatabase.LoadAssetAtPath<GameObject>(HighwayEnemyBuilder.Folder+"/TrafficPatrol.prefab");var donorData=new SerializedObject(donor.GetComponent<EnemyScript_space>());
                var template=donorData.FindProperty("heldProjectile").objectReferenceValue as Transform;if(template==null)throw new Exception("Missing authoritative patrol bullet");
                held=UnityEngine.Object.Instantiate(template.gameObject).transform;held.name=template.name;
            }
            if(currentName!="TrafficPatrol")
            {
                if(held!=null)UnityEngine.Object.DestroyImmediate(held.gameObject);
                held=BuildThrownProp(grip,currentName);
            }
            held.SetParent(grip,false);held.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);held.localScale=Vector3.one*.45f;
            if(currentName!="TrafficPatrol")held.localScale=Vector3.one;
            foreach(var renderer in held.GetComponentsInChildren<Renderer>(true))renderer.enabled=currentName!="TrafficPatrol"&&!(renderer is TrailRenderer);
            data.Update();data.FindProperty("heldProjectile").objectReferenceValue=held;data.FindProperty("throwPoint").objectReferenceValue=grip;data.FindProperty("hideHeldProjectile").boolValue=currentName=="TrafficPatrol";data.FindProperty("throwReleaseDelay").floatValue=releaseDelay;data.ApplyModifiedPropertiesWithoutUndo();
        }
        if(currentName=="ConeMechanic")
        {
            CatalogTool(grip,"Tools/Wrench",.67f,Quaternion.Euler(0,90,0),.34f);
        }
        else if(currentName=="AsphaltWorker")
        {
            CatalogTool(grip,"Farm/Tools Farm/Shovel_Farm_C",1.08f,Quaternion.identity,.78f);
        }
        else if(currentName=="TrafficPatrol")
        {
            var gun=CatalogTool(grip,"Guns/Glock_18",.46f,Quaternion.identity,.3f,true);gun.name="PatrolSidearm";
            var bounds=GeometryBounds(gun);var muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(gun,false);muzzle.localPosition=new Vector3(bounds.center.x,bounds.max.y-bounds.size.y*.2f,bounds.max.z+.004f);
            var aim=root.AddComponent<EnemyGunAim>();aim.gun=gun;aim.muzzle=muzzle;aim.grip=grip;aim.localGripPoint=gun.InverseTransformPoint(grip.position);aim.localBarrelAxis=Vector3.forward;
            if(held!=null){data.Update();data.FindProperty("throwPoint").objectReferenceValue=muzzle;data.ApplyModifiedPropertiesWithoutUndo();}Record(aim);
        }
        var presentation=root.GetComponent<HighwayEnemyAnimation>();if(presentation==null)presentation=root.AddComponent<HighwayEnemyAnimation>();presentation.animator=animator;presentation.chest=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Chest");presentation.head=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Head");presentation.deathSeconds=1.05f;Record(presentation);
        var hit=root.transform.Find("Walker-HitPos");if(hit!=null){hit.localPosition=Vector3.up*1.45f;Record(hit);}Record(combat);Record(visual.transform);
        if(root.GetComponentsInChildren<Animator>(true).Length!=1)throw new Exception(root.name+": duplicate visual rigs");
        if(ranged&&(!combat.HasConfiguredProjectile||new SerializedObject(combat).FindProperty("throwPoint").objectReferenceValue==null))throw new Exception(root.name+": missing projectile binding");
    }
    static void Primitive(PrimitiveType type,Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=material;
    }
    static Transform BuildThrownProp(Transform grip,string name)
    {
        var root=new GameObject(name=="TireBruiser"?"ThrownTire":name=="TollgateChief"?"ThrownCone":"DeliveryParcel").transform;root.SetParent(grip,false);
        if(name=="TireBruiser")CatalogTool(root,"Guns/Shooting Range/Tire",1.0f,Quaternion.Euler(0,0,90),.93f);
        else if(name=="TollgateChief")CatalogTool(root,"City/Props City/Street Props/Cone_City",.72f,Quaternion.identity,.90f);
        else CatalogTool(root,"Fantasy/Docks Fantasy/Package_Fantasy",.55f,Quaternion.identity,.5f,false,true);
        var bounds=GeometryBounds(root);var collider=root.gameObject.AddComponent<SphereCollider>();collider.isTrigger=true;collider.radius=Mathf.Max(bounds.extents.x,bounds.extents.y,bounds.extents.z);collider.center=bounds.center;
        var rigid=root.gameObject.AddComponent<Rigidbody>();rigid.isKinematic=true;rigid.useGravity=false;root.gameObject.AddComponent<SimpleProjectile>();return root;
    }
    static Transform CatalogTool(Transform parent,string path,float extent,Quaternion rotation,float gripFraction,bool pistol=false,bool parcel=false)
    {
        var holder=new GameObject(Path.GetFileName(path)).transform;holder.SetParent(parent,false);holder.localRotation=rotation;
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/polyperfect/Poly Universal Pack/Prefabs/"+path+".prefab");if(model==null)throw new Exception("Missing authored tool "+path);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,holder);instance.transform.localPosition=Vector3.zero;
        foreach(var collider in instance.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
        foreach(var rigid in instance.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(rigid);
        foreach(var script in instance.GetComponentsInChildren<MonoBehaviour>(true))if(script!=null)UnityEngine.Object.DestroyImmediate(script);
        foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=renderer.sharedMaterials.Select(original=>
        {
            string asset=Root+"Tool_"+original.name+".mat";var color=original.HasProperty("_BaseColor")?original.GetColor("_BaseColor"):original.HasProperty("_Color")?original.GetColor("_Color"):Color.white;
            var m=Surface(asset,color);var tex=original.HasProperty("_BaseMap")?original.GetTexture("_BaseMap"):original.HasProperty("_MainTex")?original.GetTexture("_MainTex"):null;m.SetTexture("_BaseMap",tex);m.SetFloat("_TextureImpact",1);m.EnableKeyword("_TEXTUREBLENDINGMODE_MULTIPLY");return m;
        }).ToArray();
        var b=GeometryBounds(holder);holder.localScale=Vector3.one*(extent/Mathf.Max(b.size.x,b.size.y,b.size.z));
        var local=GeometryBounds(holder);Vector3 point=pistol?new Vector3(local.center.x,Mathf.Lerp(local.min.y,local.max.y,gripFraction),Mathf.Lerp(local.min.z,local.max.z,.25f)):
            parcel?new Vector3(local.center.x,local.center.y,local.min.z+.025f):new Vector3(local.center.x,Mathf.Lerp(local.min.y,local.max.y,gripFraction),local.center.z);
        // Grip fractions are evaluated after rotation, in the palm's axes.
        if(!pistol&&!parcel)
        {
            var rotated=GeometryBoundsIn(holder,parent);var target=new Vector3(rotated.center.x,Mathf.Lerp(rotated.min.y,rotated.max.y,gripFraction),rotated.center.z);holder.localPosition-=target;
        }
        else holder.localPosition-=holder.localRotation*Vector3.Scale(point,holder.localScale);
        return holder;
    }
    static Bounds GeometryBounds(Transform root)=>GeometryBoundsIn(root,root);
    static Bounds GeometryBoundsIn(Transform subtree,Transform space)
    {
        bool set=false;var bounds=new Bounds();foreach(var filter in subtree.GetComponentsInChildren<MeshFilter>(true))foreach(var p in filter.sharedMesh.vertices)
        {var point=space.InverseTransformPoint(filter.transform.TransformPoint(p));if(!set){bounds=new Bounds(point,Vector3.zero);set=true;}else bounds.Encapsulate(point);}if(!set)throw new Exception("No tool geometry");return bounds;
    }
    static Material Surface(string path,Color color)
    {
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("FlatKit/Stylized Surface"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);mat.SetColor("_ColorDim",new Color(.62f,.68f,.77f));mat.SetFloat("_LightContribution",.12f);mat.SetFloat("_ShadowEdgeSize",.1f);mat.EnableKeyword("_CELPRIMARYMODE_SINGLE");mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
    }
    static void Record(UnityEngine.Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
    static void Backup(string path){string target="tmp/highway-enemy-rebuild-2026-09-23/before/"+path;Directory.CreateDirectory(Path.GetDirectoryName(target));if(!File.Exists(target))File.Copy(path,target);}
}
