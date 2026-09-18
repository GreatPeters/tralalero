using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;

public static class Sr18ActorObservation
{
    public static object Main(string label="actor-observation")
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
        string folder="tmp/image-previews/sr18-presentation-progression-2026-09-10/"+label;
        Directory.CreateDirectory(folder);
        var counts=new Dictionary<string,int>();double next=0;
        if(!File.Exists(folder+"/poses.csv"))File.WriteAllText(folder+"/poses.csv","enemy,state,time,distance,model_facing,body_facing,mesh_min_y,root_y,carry_weight,left_foot_y,right_foot_y,visual_lift\n");
        EditorApplication.CallbackFunction observe=null;
        observe=()=>
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=observe;return;}
            if(EditorApplication.isPaused||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.14;
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();if(player==null||!TimeManager.isGameRunning)return;
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyEventController>(FindObjectsSortMode.None).Where(e=>e.name.Contains("FatMan")||e.name.Contains("Guard")))
            {
                if(enemy.RuntimeState==EnemyEventRuntimeState.Dead)continue;
                Vector3 delta=player.transform.position-enemy.transform.position;float distance=delta.magnitude;
                if(distance>36||Mathf.Abs(delta.y)>3)continue;
                var animator=enemy.GetComponentInChildren<Animator>();var mesh=enemy.GetComponentInChildren<SkinnedMeshRenderer>();
                Vector3 flat=Vector3.ProjectOnPlane(delta,Vector3.up).normalized;
                int layer=animator.GetLayerIndex(EnemyEventController.CarryLayerName);
                float body=animator.isHuman?Vector3.Dot(Vector3.ProjectOnPlane(animator.bodyRotation*Vector3.forward,Vector3.up).normalized,flat):1;
                var grounding=enemy.GetComponent<EnemyGroundedPose>();
                File.AppendAllText(folder+"/poses.csv",string.Join(",",enemy.name,enemy.RuntimeState,Time.time,distance,Vector3.Dot(animator.transform.forward,flat),body,mesh.bounds.min.y,enemy.transform.position.y,layer<0?0:animator.GetLayerWeight(layer),animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y,grounding==null?0:grounding.AppliedLift)+"\n");
                string key=enemy.name+"-"+enemy.RuntimeState;counts.TryGetValue(key,out int count);
                if(count<5&&enemy.RuntimeState!=EnemyEventRuntimeState.Waiting){ScreenCapture.CaptureScreenshot(folder+"/"+key+"-"+count+".png");counts[key]=count+1;}
            }
        };
        EditorApplication.update+=observe;return new{folder,readOnly=true};
    }
}
