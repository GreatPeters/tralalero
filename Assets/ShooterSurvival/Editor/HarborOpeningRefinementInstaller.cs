using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class HarborOpeningRefinementInstaller
{
    private const string Folder="Assets/ShooterSurvival/Resources/HarborRefinement";
    public static object ApplyOpening()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var scene=SceneManager.GetActiveScene();if(scene.name!="Noryangjin_MapTool_Mode_SR18")throw new InvalidOperationException("SR18 required");
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        var roads=map.Find("Roads");var props=map.Find("Props");
        if(props.Find("HarborDeparture_20260916")!=null)throw new InvalidOperationException("Opening already installed; refine the existing assembly");
        var record=new GameObject("HarborDeparture_20260916").transform;record.SetParent(props,false);
        var player=Object.FindFirstObjectByType<PlayerScript>();var original=player.transform.position;
        var source=roads.Find("Road_LeftTurn_X+87_Z-526");if(source==null)throw new InvalidOperationException("Reviewed pier module missing");
        var straight=roads.Find("Road_Basic_X-62_Z-497");if(straight==null)throw new InvalidOperationException("Straight pier module missing");
        for(int i=1;i<=5;i++)
        {
            var piece=Object.Instantiate(straight.gameObject,roads);piece.name="HarborOpeningPier_"+i.ToString("00");piece.transform.rotation=source.rotation;piece.transform.position=source.position+Vector3.right*(11.25f*i);
        }
        var deck=Object.Instantiate(straight.gameObject,roads);deck.name="HarborWorkshopSideDeck";deck.transform.rotation=source.rotation;
        // The module origin is offset from its visible/collidable deck center.
        deck.transform.position=new Vector3(54.07f,source.position.y,-119.50f);
        var offset=Vector3.right*48;player.transform.position+=offset;
        var camera=Object.FindFirstObjectByType<StableGameplayCamera>();camera.transform.position+=offset;camera.Configure(player.transform);
        EditorUtility.SetDirty(camera);EditorUtility.SetDirty(player.transform);
        var signal=props.Cast<Transform>().Single(t=>t.name.Contains("Harbor_lane_signal_gantry"));
        signal.position=new Vector3(18,.02f,-115.11f);signal.rotation=Quaternion.AngleAxis(90,Vector3.up)*signal.rotation;signal.localScale*=1.3f;
        foreach(var collider in signal.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        var left=map.Find("Enemies/SR18_L_E01_T005_Enemy_OldMan").GetComponent<EnemyEventController>();var old=left.transform.position;
        left.transform.position+=Vector3.forward*4;left.RefreshPlacementAfterAuthoringChange(old,false);
        var target=new GameObject("HarborFirstOldManApproach").transform;target.SetParent(map,false);target.position=left.transform.position-left.transform.forward*2;
        left.TargetPoint=target;left.MoveSpeed=1.8f;left.EventMode=EnemyEventMode.MoveToTargetThenAttack;
        foreach(var spot in props.GetComponentsInChildren<EnemyEventActivationSpot>(true).Where(s=>s.Targets.Contains(left)))spot.transform.position=left.transform.position-left.transform.forward*12;
        var bucket=props.Find("SR18_L_G01_T013_Bucket");if(bucket!=null)bucket.gameObject.SetActive(false);
        var colliders=roads.GetComponentsInChildren<MeshCollider>(true);
        Physics.SyncTransforms();
        for(float x=31;x<=player.transform.position.x+3;x+=2)
            foreach(float z in new[]{-116.5f,-115.11f,-113.7f})
                if(!new[]{Vector3.zero,Vector3.right*.12f,Vector3.left*.12f,Vector3.forward*.12f,Vector3.back*.12f}.Any(offset2=>colliders.Any(c=>c.Raycast(new Ray(new Vector3(x,2,z)+offset2,Vector3.down),out var hit,4)&&hit.normal.y>.7f)))throw new InvalidOperationException("Gap in opening pier at "+x+","+z);
        foreach(var component in new Component[]{player.transform,camera,signal,left,left.transform}){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Opening save failed");
        File.WriteAllText("map-concepts/harbor-opening-refinement-2026-09-16/opening-installed.txt","Pier:5 extra modules, player "+original+" -> "+player.transform.position+", signal "+signal.position+", scale "+signal.localScale+"; floor samples passed.");
        return new{player=player.transform.position.ToString(),signal=signal.position.ToString(),addedPierLength=56.25f};
    }

    public static object ApplyCalmIdles()
    {
        Directory.CreateDirectory(Folder+"/Animations");AssetDatabase.Refresh();
        var setup=EditorSceneManager.GetSceneManagerSetup();var replaced=new HashSet<string>();
        try{
            foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}){
                var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
                foreach(var actor in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<EnemyEventController>(true))){
                    var animator=actor.GetComponentInChildren<Animator>(true);if(animator==null||!(animator.runtimeAnimatorController is AnimatorOverrideController original))continue;
                    if(original.name.EndsWith("_Calm"))continue;
                    var pairs=new List<KeyValuePair<AnimationClip,AnimationClip>>();original.GetOverrides(pairs);
                    var idle=pairs.FirstOrDefault(p=>p.Key.name=="ForwardEnemy_Idle");
                    if(idle.Key==null||idle.Value==null||!idle.Value.name.ToLowerInvariant().Contains("act"))continue;
                    string path=Folder+"/Animations/"+original.name+"_Calm.overrideController";
                    var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
                    if(controller==null){
                        controller=Object.Instantiate(original);controller.name=original.name+"_Calm";
                        var clip=MakeIdle(idle.Value,original.name);controller[idle.Key.name]=clip;AssetDatabase.CreateAsset(controller,path);
                    }
                    animator.runtimeAnimatorController=controller;EditorUtility.SetDirty(animator);PrefabUtility.RecordPrefabInstancePropertyModifications(animator);replaced.Add(original.name);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
        }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return replaced.ToArray();
    }
    private static AnimationClip MakeIdle(AnimationClip source,string name)
    {
        string path=Folder+"/Animations/"+name+"_CalmIdle.anim";
        var clip=new AnimationClip{name=name+"_CalmIdle",frameRate=30};
        foreach(var binding in AnimationUtility.GetCurveBindings(source)){
            var sourceCurve=AnimationUtility.GetEditorCurve(source,binding);float value=sourceCurve.Evaluate(source.length*.08f);
            if(binding.propertyName.EndsWith("Arm Down-Up"))value=-.72f;
            if(binding.propertyName.EndsWith("Arm Front-Back"))value=.03f;
            if(binding.propertyName.EndsWith("Forearm Stretch"))value=.12f;
            float motion=binding.propertyName=="Spine Front-Back"?.012f:binding.propertyName=="Head Turn Left-Right"?.035f:0;
            var curve=new AnimationCurve(new Keyframe(0,value),new Keyframe(.9f,value+motion),new Keyframe(1.8f,value),new Keyframe(2.7f,value-motion),new Keyframe(3.6f,value));
            AnimationUtility.SetEditorCurve(clip,binding,curve);
        }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.keepOriginalPositionY=true;settings.keepOriginalOrientation=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
        AssetDatabase.CreateAsset(clip,path);return clip;
    }
}
