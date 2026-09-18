using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;

public static class Sr18ProjectileLampProbe
{
    public static object Main(string label="projectile-lamp-final")
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new InvalidOperationException("Fresh Play lobby required");
        EditorApplication.isPaused=false;Time.timeScale=1;
        string folder="tmp/image-previews/sr18-presentation-progression-2026-09-10/"+label;
        if(Directory.Exists(folder))throw new InvalidOperationException("Use a new label");Directory.CreateDirectory(folder);
        OpeningStoryUI.Instance?.Skip();SessionState.SetBool("NoryangjinMapTool.TestPower9999",false);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false);SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",1);
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var pool=UnityEngine.Object.FindFirstObjectByType<BulletPooler>();
        var enemy=UnityEngine.Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None).Single(e=>e.name.StartsWith("SR18_L_E01_"));
        var health=typeof(EnemyScript_space).GetField("_health",BindingFlags.Instance|BindingFlags.NonPublic);
        float before=(float)health.GetValue(enemy);
        var weapon=p.GetComponentsInChildren<WeaponScript>(true).First(w=>w.gameObject.activeInHierarchy);
        var bullet=pool.Get(weapon.bulletKind,p.transform);bullet.transform.SetParent(null,true);bullet.transform.position=enemy.GetComponent<Collider>().bounds.center;
        bullet.GetComponentInChildren<BulletScript>().SetDirection(Vector3.zero,null,13f);Physics.SyncTransforms();
        double began=EditorApplication.timeSinceStartup;int phase=0;bool payloadPassed=false;float after=before;float hpBefore=0;float hpAfter=0;int impacts=0;double contactAt=0;
        ObstacleStats lamp=null;LampImpactFeedback feedback=null;EnemyScript_space behindEnemy=null;float behindBefore=0;
        var serializer=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null).GetMethod("SerializeObject",new[]{typeof(object)});
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            if(!EditorApplication.isPlaying||p==null){EditorApplication.update-=tick;return;}
            if(EditorApplication.isPaused)return;
            double elapsed=EditorApplication.timeSinceStartup-began;
            if(phase==0&&elapsed>.35)
            {
                after=(float)health.GetValue(enemy);payloadPassed=Mathf.Abs(before-after-13f)<.001f;
                if(bullet.activeSelf)pool.ReturnObjectToPool_Bullet(bullet);
                var map=UnityEngine.GameObject.Find("Noryangjin_MapTool").transform;
                lamp=map.Find("Props").Cast<Transform>().Single(t=>t.name.StartsWith("SR18_L_G02_")).GetComponentsInChildren<ObstacleStats>().Single(o=>o.obstaclePattern==ObstaclePattern.Light);
                var col=lamp.GetComponent<Collider>();feedback=lamp.GetComponent<LampImpactFeedback>();
                UnityEngine.Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
                behindEnemy=map.Find("Enemies").GetComponentsInChildren<EnemyScript_space>().Single(e=>e.name.StartsWith("SR18_L_E03_"));behindBefore=(float)health.GetValue(behindEnemy);
                Vector3 position=new Vector3(col.bounds.center.x-10,p.transform.position.y,col.bounds.center.z);
                Quaternion rotation=Quaternion.LookRotation(Vector3.right,Vector3.up);
                p.transform.SetPositionAndRotation(position,rotation);var rb=p.GetComponent<Rigidbody>();rb.position=position;rb.rotation=rotation;rb.linearVelocity=Vector3.zero;
                typeof(PlayerScript).GetMethod("RebaseRouteFrame",BindingFlags.Instance|BindingFlags.NonPublic,null,Type.EmptyTypes,null).Invoke(p,null);
                p.currentHealth=100;p.UpdateHealth();hpBefore=p.currentHealth;phase=1;began=EditorApplication.timeSinceStartup;
                ScreenCapture.CaptureScreenshot(folder+"/approach.png");
            }
            else if(phase==1)
            {
                if(feedback!=null&&feedback.ImpactCount>impacts){impacts=feedback.ImpactCount;ScreenCapture.CaptureScreenshot(folder+"/impact-"+impacts+".png");}
                if(p.currentHealth<hpBefore)
                {
                    hpAfter=p.currentHealth;p.enabled=false;p.canShoot=false;p.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;phase=2;contactAt=EditorApplication.timeSinceStartup;
                    ScreenCapture.CaptureScreenshot(folder+"/contact.png");
                }
                else if(elapsed>4){hpAfter=p.currentHealth;phase=2;contactAt=EditorApplication.timeSinceStartup;}
            }
            if(phase==2&&EditorApplication.timeSinceStartup-contactAt>.25)
            {
                float behindAfter=(float)health.GetValue(behindEnemy);
                var result=new{payload=new{before,after,requested=13,passed=payloadPassed},lamp=new{hpBefore,hpAfter,damage=hpBefore-hpAfter,impacts,tag=lamp.tag,active=lamp.enabled,behindBefore,behindAfter,passed=Mathf.Abs(hpBefore-hpAfter-50)<.001f&&impacts>0&&behindBefore==behindAfter},conditions="Focused collision probe: injected13-damage projectile, then repositioned on the existing road10m before G02; real forward movement/shooting/physics with100 HP. No normal-difficulty claim."};
                File.WriteAllText(folder+"/result.json",(string)serializer.Invoke(null,new object[]{result}));EditorApplication.update-=tick;EditorApplication.isPaused=true;
            }
        };
        EditorApplication.update+=tick;return new{folder,probe="launch damage and actual lamp approach"};
    }
}
