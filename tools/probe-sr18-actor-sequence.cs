using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;

public static class Sr18ActorSequenceProbe
{
    public static object Main(string prefix="SR18_L_E08_",string label="fatman-final")
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new InvalidOperationException("Fresh Play lobby required");
        EditorApplication.isPaused=false;
        string folder="tmp/image-previews/sr18-presentation-progression-2026-09-10/"+label;
        if(Directory.Exists(folder))throw new InvalidOperationException("Use a new evidence label");Directory.CreateDirectory(folder);
        OpeningStoryUI.Instance?.Skip();
        SessionState.SetBool("NoryangjinMapTool.TestPower9999",true);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false);SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",1);Time.timeScale=1;
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        var enemy=UnityEngine.Object.FindObjectsByType<EnemyEventController>(FindObjectsSortMode.None).Single(e=>e.name.StartsWith(prefix));
        UnityEngine.Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
        player.canShoot=false;player.movement=false;
        foreach(var weapon in player.GetComponentsInChildren<WeaponScript>(true))weapon.enabled=false;
        Vector3 target=enemy.TargetPoint!=null?enemy.TargetPoint.position:enemy.transform.position;
        Vector3 playerPosition=target-enemy.transform.forward*12;playerPosition.y=target.y;
        player.transform.SetPositionAndRotation(playerPosition,enemy.transform.rotation);
        var rb=player.GetComponent<Rigidbody>();rb.position=playerPosition;rb.rotation=enemy.transform.rotation;rb.linearVelocity=Vector3.zero;
        player.enabled=false;rb.constraints=RigidbodyConstraints.FreezeAll;player.SetSharkLocomotion(false);
        var main=Camera.main;var cameraObject=new GameObject("Actor verification camera");var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(main);main.enabled=false;
        camera.tag="MainCamera";camera.fieldOfView=38;camera.transform.position=target-enemy.transform.forward*9+Vector3.up*6;camera.transform.LookAt(target+Vector3.up*1.2f);
        var animator=enemy.GetComponentInChildren<Animator>();var projectile=enemy.GetComponentInChildren<SimpleProjectile>(true);
        var lookTarget=player.sharkAnim.GetComponentsInChildren<Transform>(true).First(t=>t.name=="head");
        File.WriteAllText(folder+"/poses.csv","seconds,state,left_foot_y,right_foot_y,root_y,lift,carry,body_facing,projectile_active,in_flight,trail,projectile_min_y,muzzle_dot,head_facing\n");
        double ready=EditorApplication.timeSinceStartup+.5,next=ready;float began=-1;int frame=0;
        EditorApplication.CallbackFunction sample=null;
        sample=()=>
        {
            if(!EditorApplication.isPlaying||enemy==null){EditorApplication.update-=sample;return;}
            if(EditorApplication.isPaused||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.12;
            if(began<0){enemy.ActivateFromSpot();began=Time.time;}
            float seconds=Time.time-began;
            var body=projectile.GetComponent<Rigidbody>();var trail=projectile.GetComponentInChildren<TrailRenderer>(true);var collider=projectile.GetComponent<Collider>();
            var ground=enemy.GetComponent<EnemyGroundedPose>();int layer=animator.GetLayerIndex(EnemyEventController.CarryLayerName);
            Vector3 toPlayer=Vector3.ProjectOnPlane(player.transform.position-enemy.transform.position,Vector3.up).normalized;
            var aim=enemy.GetComponent<EnemyGunAim>();float aimDot=aim==null?1:Vector3.Dot(aim.gun.TransformDirection(aim.localBarrelAxis).normalized,(player.GetComponent<Collider>().bounds.center-aim.muzzle.position).normalized);
            var head=animator.GetBoneTransform(HumanBodyBones.Head);
            File.AppendAllText(folder+"/poses.csv",string.Join(",",seconds,enemy.RuntimeState,animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y,enemy.transform.position.y,ground==null?0:ground.AppliedLift,layer<0?0:animator.GetLayerWeight(layer),Vector3.Dot(Vector3.ProjectOnPlane(animator.bodyRotation*Vector3.forward,Vector3.up).normalized,toPlayer),projectile.gameObject.activeInHierarchy,body!=null&&!body.isKinematic,trail!=null&&trail.emitting,collider==null?0:collider.bounds.min.y,aimDot,Vector3.Dot(head.forward,(lookTarget.position-head.position).normalized))+"\n");
            ScreenCapture.CaptureScreenshot(folder+"/frame-"+(frame++).ToString("D3")+".png");
            if(seconds>=8){EditorApplication.update-=sample;EditorApplication.isPaused=true;File.WriteAllText(folder+"/conditions.txt","Focused pose probe: player positioned before the actor, movement and firing disabled, editor power9999 enabled. Not a normal traversal or balance run.");}
        };
        EditorApplication.update+=sample;
        return new{folder,actor=enemy.name,seconds=8,focusedPoseTest=true};
    }
}
