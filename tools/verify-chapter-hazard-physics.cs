// Run through unity eval_file in an unpaused SR18 Play Mode lobby.
// These are isolated physical contacts, not normal chapter-clear runs.
if(!UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isPaused)throw new System.InvalidOperationException("Unpaused Play Mode required.");
string folder=UnityEditor.SessionState.GetString("TwoChapter.HazardProbeFolder","tmp/image-previews/two-chapter-2026-09-11/hazard-physics");
if(System.IO.Directory.Exists(folder))throw new System.InvalidOperationException("Preserve existing evidence.");
System.IO.Directory.CreateDirectory(folder);
var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
var report=new System.Collections.Generic.Dictionary<string,object>();
var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var chapter=canvas.GetComponent<ChapterProgression>();
if(OpeningStoryUI.Instance!=null)OpeningStoryUI.Instance.Skip();
var all=UnityEngine.Object.FindObjectsByType<ObstacleStats>(UnityEngine.FindObjectsSortMode.None);
var oilSource=all.First(o=>o.obstaclePattern==ObstaclePattern.Oil);
var gullSource=all.First(o=>o.obstaclePattern==ObstaclePattern.Seagull);
var shipSource=all.First(o=>o.obstaclePattern==ObstaclePattern.Ship);
foreach(var e in UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.EnemyScript_space>(UnityEngine.FindObjectsSortMode.None))e.gameObject.SetActive(false);
foreach(var o in all)o.gameObject.SetActive(false);
canvas.PlayerPressedStartButton();
UnityEngine.Time.timeScale=1;IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=1;
player.enabled=false;player.GetComponent<UnityEngine.Rigidbody>().constraints=UnityEngine.RigidbodyConstraints.FreezeAll;
var weapons=player.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.WeaponScript>();foreach(var w in weapons)w.enabled=false;
var start=player.transform.position;var rotation=player.transform.rotation;
float divider=player.moveSensitivity_Devision,initialHealth=player.currentHealth;
UnityEngine.GameObject Clone(UnityEngine.GameObject source,UnityEngine.Vector3 position)
{
    var holder=new UnityEngine.GameObject("Inactive probe staging");holder.SetActive(false);
    var go=UnityEngine.Object.Instantiate(source,holder.transform);go.SetActive(false);go.transform.SetParent(null);
    UnityEngine.Object.Destroy(holder);go.name="PhysicsProbe_"+source.name;go.transform.SetPositionAndRotation(position,rotation);return go;
}
void MovePlayer(UnityEngine.Vector3 p){player.transform.position=p;player.GetComponent<UnityEngine.Rigidbody>().position=p;UnityEngine.Physics.SyncTransforms();}
UnityEngine.GameObject Hazard(string name,UnityEngine.Vector3 p)=>Clone(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ShooterSurvival/Prefabs/Highway/Gimmicks/"+name+".prefab"),p);
var current=Clone(oilSource.gameObject,start);current.SetActive(true);UnityEngine.Physics.SyncTransforms();
ObstacleStats gull=null;HighwayHazard highway=null;
float healthBefore=initialHealth;int spawnedBefore=0;bool shadowSeen=false,birdSeen=false,projectileSeen=false;
double ready=UnityEditor.EditorApplication.timeSinceStartup+.3,deadline=ready+45;int phase=0;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    if(!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=tick;return;}
    double now=UnityEditor.EditorApplication.timeSinceStartup;
    if(gull!=null){shadowSeen|=gull.shadowSprite!=null&&gull.shadowSprite.enabled;birdSeen|=gull.balloon!=null&&gull.balloon.gameObject.activeInHierarchy;}
    if(phase==5&&highway!=null&&highway.Broken&&!report.ContainsKey("roadblockShotsToBreak"))report["roadblockShotsToBreak"]=weapons.Sum(w=>w.TotalProjectilesSpawned)-spawnedBefore;
    if(phase==4&&!projectileSeen)
    {
        var projectile=UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.SimpleProjectile>(UnityEngine.FindObjectsSortMode.None).FirstOrDefault(p=>p.name=="CannonBall(Clone)");
        if(projectile!=null){projectileSeen=true;var rb=projectile.GetComponent<UnityEngine.Rigidbody>();report["shipVelocity"]=rb.linearVelocity.magnitude;report["shipColliderActive"]=projectile.GetComponent<UnityEngine.Collider>().enabled;}
    }
    if(now<ready)return;
    try
    {
        if(now>deadline)throw new System.TimeoutException("Hazard probe phase "+phase);
        switch(phase)
        {
            case 0:
                report["oilSteeringRatio"]=divider/player.moveSensitivity_Devision;report["oilCanShoot"]=player.canShoot;
                report["oilHeadingUnchanged"]=UnityEngine.Quaternion.Angle(rotation,player.transform.rotation)<.01f;
                UnityEngine.Object.Destroy(current);phase++;ready=now+2.3;break;
            case 1:
                report["oilRestored"]=UnityEngine.Mathf.Approximately(divider,player.moveSensitivity_Devision);
                current=Clone(gullSource.gameObject,start+player.transform.forward*2);current.SetActive(true);gull=current.GetComponent<ObstacleStats>();
                phase++;ready=now+.1;break;
            case 2:
                MovePlayer(gull.transform.position+UnityEngine.Vector3.up*.12f);healthBefore=player.currentHealth;phase++;ready=now+3.8;break;
            case 3:
                report["seagullDamage"]=healthBefore-player.currentHealth;report["seagullShadowSeen"]=shadowSeen;report["seagullBirdSeen"]=birdSeen;
                report["seagullDeparted"]=!gull.balloon.gameObject.activeSelf;report["seagullAnchorStable"]=UnityEngine.Quaternion.Angle(gull.transform.rotation,rotation)<.01f;
                UnityEngine.Object.Destroy(current);gull=null;MovePlayer(start);
                current=Clone(shipSource.gameObject,start+player.transform.right*20);var ship=current.GetComponent<ObstacleStats>();
                ship.firePos.position=player.GetComponent<UnityEngine.Collider>().bounds.center+player.transform.right*15;
                ship.aheadOffset=0;current.SetActive(true);healthBefore=player.currentHealth;phase++;ready=now+1.2;break;
            case 4:
                report["shipProjectileSeen"]=projectileSeen;report["shipDamage"]=healthBefore-player.currentHealth;
                UnityEngine.Object.Destroy(current);MovePlayer(start);
                current=Hazard("HighwayRoadblock",start+player.transform.forward*5);highway=current.GetComponent<HighwayHazard>();highway.breakHealth=85;
                current.SetActive(true);foreach(var w in weapons)w.enabled=true;spawnedBefore=weapons.Sum(w=>w.TotalProjectilesSpawned);phase++;ready=now+5;break;
            case 5:
                report["roadblockBrokenByWeapon"]=highway.Broken;report["roadblockShotsFired"]=weapons.Sum(w=>w.TotalProjectilesSpawned)-spawnedBefore;
                report["roadblockColliderOff"]=!current.GetComponent<UnityEngine.Collider>().enabled;report["roadblockVisualOff"]=!highway.visual.gameObject.activeSelf;
                foreach(var w in weapons)w.enabled=false;UnityEngine.Object.Destroy(current);
                current=Hazard("HighwayToll",start+player.transform.forward*3);highway=current.GetComponent<HighwayHazard>();
                highway.laneIndex=0;highway.cycleSeconds=6;current.GetComponent<ObstacleStats>().value=40;current.SetActive(true);chapter.BeginRun();phase++;ready=now+.2;break;
            case 6:
                report["tollOpen"]=highway.IsOpen;healthBefore=player.currentHealth;MovePlayer(current.transform.position+UnityEngine.Vector3.up*.12f);phase++;ready=now+.3;break;
            case 7:
                report["tollOpenDamage"]=healthBefore-player.currentHealth;MovePlayer(start);highway.laneIndex=1;phase++;ready=now+.2;break;
            case 8:
                report["tollClosed"]=!highway.IsOpen;healthBefore=player.currentHealth;MovePlayer(current.transform.position+UnityEngine.Vector3.up*.12f);phase++;ready=now+.3;break;
            case 9:
                report["tollClosedDamage"]=healthBefore-player.currentHealth;UnityEngine.Object.Destroy(current);MovePlayer(start);
                current=Hazard("HighwayTraffic",start+player.transform.forward*8);highway=current.GetComponent<HighwayHazard>();
                highway.warningSeconds=1.5f;highway.cycleSeconds=4;highway.crossingDistance=9;current.SetActive(true);phase++;ready=now+.3;break;
            case 10:
                report["trafficWarning"]=highway.warning.activeSelf;report["trafficStart"]=current.transform.position.ToString();healthBefore=player.currentHealth;phase++;ready=now+5.5;break;
            case 11:
                report["trafficTravel"]=UnityEngine.Vector3.Distance(start+player.transform.forward*8,current.transform.position);
                report["trafficWarningFinished"]=!highway.warning.activeSelf;report["trafficAvoidDamage"]=healthBefore-player.currentHealth;
                MovePlayer(current.transform.position+UnityEngine.Vector3.up*.12f);phase++;ready=now+.3;break;
            case 12:
                report["trafficContactDamage"]=healthBefore-player.currentHealth;
                System.IO.File.WriteAllText(folder+"/result.json",(string)serialize.Invoke(null,new object[]{report}));
                UnityEditor.EditorApplication.update-=tick;UnityEngine.Object.Destroy(current);IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=false;break;
        }
    }
    catch(System.Exception error)
    {
        System.IO.File.WriteAllText(folder+"/error.txt",error.ToString());
        System.IO.File.WriteAllText(folder+"/partial.json",(string)serialize.Invoke(null,new object[]{report}));UnityEditor.EditorApplication.update-=tick;
    }
};
UnityEditor.EditorApplication.update+=tick;return new{folder,initialHealth};
