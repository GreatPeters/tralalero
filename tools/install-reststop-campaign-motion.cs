using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class InstallRestStopCampaignMotion
{
    const string AssetsRoot="Assets/ShooterSurvival/Models/Chapters/CampaignMotion20260923/";
    static AnimatorController controller;
    public static string Main(string role)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        if(!new[]{"CoffeeVendor","SnackChef","ParkingMarshal"}.Contains(role))throw new ArgumentException(role);
        var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(current.isDirty)throw new InvalidOperationException("Unsaved scene");string original=current.path;
        string output="outputs/campaign-balance-2026-09-23/reststop-motion-v2/"+role;
        string model="Assets/ShooterSurvival/Models/Chapters/Mascots/"+role+"/"+role+".fbx";
        string prefab="Assets/ShooterSurvival/Prefabs/RestStop/Enemies/"+role+".prefab";
        if(!File.Exists(output+"/motion-report.json"))throw new InvalidOperationException("Validated motion output required");
        Backup(model);Backup(prefab);Directory.CreateDirectory(AssetsRoot);File.Copy(output+"/"+role+".fbx",model,true);
        AssetDatabase.ImportAsset(model,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(model);importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
        var clips=importer.defaultClipAnimations;
        foreach(var clip in clips){clip.loopTime=!(clip.name.EndsWith("attack_once")||clip.name.EndsWith("hit")||clip.name.EndsWith("die"));clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;clip.lockRootRotation=true;}
        importer.clipAnimations=clips;importer.SaveAndReimport();
        var animations=AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
        string path=AssetsRoot+role+".controller";controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm=controller.layers[0].stateMachine;foreach(var s in sm.states)sm.RemoveState(s.state);
        foreach(string action in new[]{"idle","walk","run","attack_loop","attack_once","hit","die"})
        {var state=sm.AddState(action);state.motion=animations.Single(c=>c.name==action||c.name.EndsWith("|"+action));if(action=="idle")sm.defaultState=state;}
        var source=PrefabUtility.LoadPrefabContents(prefab);
        try{Install(source,role);PrefabUtility.SaveAsPrefabAsset(source,prefab);}finally{PrefabUtility.UnloadPrefabContents(source);}
        const string scenePath="Assets/ShooterSurvival/Scenes/Tools/RestStop.unity";Backup(scenePath);var scene=EditorSceneManager.OpenScene(scenePath);int count=0;
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(enemy.gameObject.scene==scene&&enemy.name.Contains(role)){Install(enemy.gameObject,role);count++;}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(original)&&original!=scenePath)EditorSceneManager.OpenScene(original);
        return role+": source + "+count+" placements, forward attack, visible tool, recoil and complete death";
    }
    static void Install(GameObject root,string role)
    {
        var animator=root.GetComponentInChildren<Animator>(true);if(animator==null)throw new Exception("Missing rig");
        var body=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==role+"_Body");
        // Renderer bounds include animation padding. Keep the three roles at the
        // same apparent scale as the neighboring reconstructed highway actors.
        animator.transform.localScale*=3.05f/Mathf.Max(.01f,body.bounds.size.y);
        var pos=animator.transform.localPosition;pos.y=-.1f/Mathf.Max(.01f,root.transform.lossyScale.y);animator.transform.localPosition=pos;
        animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        foreach(var r in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {r.forceMatrixRecalculationPerRender=true;if(r.name.EndsWith("_Equipment")){r.enabled=false;r.gameObject.SetActive(false);}Record(r);}
        var capsule=root.GetComponent<CapsuleCollider>();if(capsule!=null){capsule.height=3f/Mathf.Abs(root.transform.lossyScale.y);capsule.center=new Vector3(capsule.center.x,capsule.height*.5f,capsule.center.z);Record(capsule);}
        var hand=animator.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Hand.R");var grip=hand.Find("CampaignPalm");
        if(grip==null)
        {
            int index=Array.IndexOf(body.bones,hand);var vertices=body.sharedMesh.vertices;var weights=body.sharedMesh.boneWeights;Vector3 center=Vector3.zero;int n=0;
            for(int i=0;i<vertices.Length;i++){var w=weights[i];float strength=w.boneIndex0==index?w.weight0:w.boneIndex1==index?w.weight1:w.boneIndex2==index?w.weight2:w.boneIndex3==index?w.weight3:0;if(strength>.8f){center+=body.sharedMesh.bindposes[index].MultiplyPoint3x4(vertices[i]);n++;}}
            if(n==0)throw new Exception("No hand-bound vertices");
            grip=new GameObject("CampaignPalm").transform;grip.SetParent(hand,false);grip.localPosition=center/n;grip.rotation=animator.transform.rotation;
            var scale=hand.lossyScale;grip.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
            if(role=="CoffeeVendor")
            {
                var held=new GameObject("ThrownCoffeeCup").transform;held.SetParent(grip,false);
                Tool(held,"Movie Set/Cup_A_Movie",.44f,.5f,Quaternion.identity);
                var bounds=BoundsIn(held,held);var col=held.gameObject.AddComponent<SphereCollider>();col.isTrigger=true;col.radius=.23f;col.center=bounds.center;
                var rb=held.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;held.gameObject.AddComponent<SimpleProjectile>();
            }
            else if(role=="SnackChef")Tool(grip,"4July/Spatula_4july",.85f,.22f,Quaternion.Euler(35,0,0));
            else Tool(grip,"City/Signs City/Sign_Stop_B_City",1.05f,.25f,Quaternion.identity);
        }
        var combat=root.GetComponent<EnemyScript_space>();var data=new SerializedObject(combat);
        if(role=="CoffeeVendor")
        {
            var held=grip.Find("ThrownCoffeeCup");if(held==null)throw new Exception("Missing coffee projectile");
            var model=held.GetChild(0);var bounds=BoundsIn(model,held);
            model.localPosition+=new Vector3(-bounds.center.x,.12f-bounds.center.y,.17f-bounds.center.z);
            held.GetComponent<SphereCollider>().center=BoundsIn(held,held).center;
            var old=data.FindProperty("heldProjectile").objectReferenceValue as Transform;
            if(old!=null&&old!=held){old.gameObject.SetActive(false);Record(old.gameObject);}
            foreach(var aim in root.GetComponents<EnemyGunAim>())UnityEngine.Object.DestroyImmediate(aim);
            data.FindProperty("heldProjectile").objectReferenceValue=held;data.FindProperty("throwPoint").objectReferenceValue=held;data.FindProperty("hideHeldProjectile").boolValue=false;data.FindProperty("throwReleaseDelay").floatValue=.6f;
        }
        else if(role=="SnackChef")
        {
            var tool=grip.Find("Spatula_4july");tool.localRotation=Quaternion.Euler(35,0,0);tool.localPosition=Vector3.zero;
            var bounds=BoundsIn(tool,grip);tool.localPosition=-new Vector3(bounds.center.x,Mathf.Lerp(bounds.min.y,bounds.max.y,.22f),bounds.center.z)+Vector3.forward*.12f;
        }
        var animatorProperty=data.FindProperty("enemyAnimator");if(animatorProperty!=null)animatorProperty.objectReferenceValue=animator;data.ApplyModifiedPropertiesWithoutUndo();
        var reaction=root.GetComponent<HighwayEnemyAnimation>();if(reaction==null)reaction=root.AddComponent<HighwayEnemyAnimation>();
        reaction.animator=animator;reaction.chest=animator.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Chest");reaction.head=animator.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Head");reaction.deathSeconds=1.1f;
        reaction.releaseNormalizedTime=role=="CoffeeVendor"?.5f:.46f;
        Record(reaction);Record(animator);Record(animator.transform);Record(combat);
        if(root.GetComponentsInChildren<Animator>(true).Length!=1)throw new Exception("Duplicate rig");
    }
    static Transform Tool(Transform parent,string path,float extent,float gripFraction,Quaternion rotation)
    {
        var holder=new GameObject(Path.GetFileName(path)).transform;holder.SetParent(parent,false);holder.localRotation=rotation;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/polyperfect/Poly Universal Pack/Prefabs/"+path+".prefab");if(source==null)throw new Exception(path);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(source,holder);model.transform.localPosition=Vector3.zero;
        foreach(var c in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var b in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(b);
        foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))if(script!=null)UnityEngine.Object.DestroyImmediate(script);
        foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(Surface).ToArray();
        var bounds=BoundsIn(holder,holder);holder.localScale=Vector3.one*(extent/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z));
        var fitted=BoundsIn(holder,parent);holder.localPosition-=new Vector3(fitted.center.x,Mathf.Lerp(fitted.min.y,fitted.max.y,gripFraction),fitted.center.z);
        if(path.Contains("Cup_"))holder.localPosition+=Vector3.forward*.17f;
        return holder;
    }
    static Bounds BoundsIn(Transform root,Transform space)
    {
        bool first=true;var bounds=new Bounds();foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true))foreach(var v in mesh.sharedMesh.vertices)
        {var p=space.InverseTransformPoint(mesh.transform.TransformPoint(v));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}if(first)throw new Exception("Missing tool geometry");return bounds;
    }
    static Material Surface(Material source)
    {
        string path=AssetsRoot+"Tool_"+source.name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("FlatKit/Stylized Surface"));AssetDatabase.CreateAsset(material,path);}
        var color=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.color:Color.white;
        var texture=source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.HasProperty("_MainTex")?source.mainTexture:null;
        material.SetColor("_BaseColor",color);material.SetColor("_ColorDim",new Color(.65f,.7f,.78f));material.SetTexture("_BaseMap",texture);material.SetFloat("_TextureImpact",1);material.SetFloat("_LightContribution",.12f);material.EnableKeyword("_CELPRIMARYMODE_SINGLE");material.EnableKeyword("_TEXTUREBLENDINGMODE_MULTIPLY");material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
    }
    static void Record(UnityEngine.Object obj){EditorUtility.SetDirty(obj);if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
    static void Backup(string path){string target="tmp/campaign-balance-2026-09-23/before-reststop-motion/"+path;Directory.CreateDirectory(Path.GetDirectoryName(target));if(!File.Exists(target))File.Copy(path,target);}
}
