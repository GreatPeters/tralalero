#if UNITY_EDITOR
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SeagullContactTests
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private readonly List<GameObject> objects=new();
    private GameObject Make(string name){var go=new GameObject(name);objects.Add(go);return go;}
    [TestCase(false)] [TestCase(true)]
    public void AuthoredBirdAndWarningLeaveRoomToDodge(bool placedInScene)
    {
        const string path="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
        var scene=SceneManager.GetSceneByPath(path);bool opened=placedInScene&&(!scene.IsValid()||!scene.isLoaded);
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var sources=placedInScene
                ? scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ObstacleStats>(true)).Where(s=>s.obstaclePattern==ObstaclePattern.Seagull).ToArray()
                : new[]{AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Obstacle_Real/Seagull.prefab").GetComponent<ObstacleStats>()};
            Assert.That(sources,Is.Not.Empty);
            foreach(var source in sources)
            {
                var clone=Object.Instantiate(source.gameObject);objects.Add(clone);clone.hideFlags=HideFlags.HideAndDontSave;
                clone.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);clone.transform.localScale=source.transform.lossyScale;
                var stats=clone.GetComponent<ObstacleStats>();var shadow=stats.shadowSprite;
                shadow.transform.localScale=Vector3.one*stats.shadowEndScale;
                float width=shadow.sprite.bounds.size.x*Mathf.Abs(shadow.transform.lossyScale.x);
                float depth=shadow.sprite.bounds.size.y*Mathf.Abs(shadow.transform.lossyScale.y);
                Assert.That(width,Is.EqualTo(3.744f).Within(.01f),source.name+" warning diameter must grow 20% from 3.12m.");
                Assert.That(depth,Is.EqualTo(3.744f).Within(.01f));
                Assert.That(stats.balloon.localScale.x,Is.EqualTo(4.095f).Within(.001f));
                Assert.That(stats.triggerRadius,Is.EqualTo(28f));
                Assert.That(stats.telegraphTime,Is.EqualTo(1.0f));
                Assert.That(stats.dropTime,Is.EqualTo(.4f));
                Assert.That(shadow.sharedMaterial.GetColor("_BaseColor").a*.376f,Is.InRange(.8f,.9f),"The warning core must be dark enough to read on wood.");
                stats.balloon.gameObject.SetActive(true);var animator=stats.balloon.GetComponent<Animator>();
                var skin=stats.balloon.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
                try
                {
                    foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                    for(int frame=0;frame<20;frame++)
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*frame/20f);skin.BakeMesh(mesh,true);
                        foreach(var vertex in mesh.vertices)
                        {
                            var offset=skin.transform.TransformPoint(vertex)-stats.balloon.position;offset.y=0;
                            float margin=clip.name.Contains("Sitting")?0f:.25f;
                            Assert.That(offset.magnitude,Is.LessThanOrEqualTo(width*.5f+margin),"The landed body must fit inside the warning; the larger flying wing tips have at most 0.25m overhang.");
                        }
                    }
                }
                finally{Object.DestroyImmediate(mesh);}
            }
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    [TearDown] public void Cleanup(){foreach(var go in objects)if(go!=null)Object.DestroyImmediate(go);objects.Clear();TimeManager.isGameRunning=false;}
    private PlayerScript Player(float maximum,float current)
    {
        var go=Make("Contact player");var player=go.AddComponent<PlayerScript>();go.AddComponent<CapsuleCollider>();
        typeof(PlayerScript).GetField("maxHealthWithUpgrades",Private).SetValue(player,maximum);player.currentHealth=current;
        return player;
    }
    private ObstacleStats Bird()
    {
        var root=Make("Bird owner");var stats=root.AddComponent<ObstacleStats>();stats.obstaclePattern=ObstaclePattern.Seagull;stats.value=20;
        var bird=Make("Visible bird");bird.transform.SetParent(root.transform);bird.AddComponent<BoxCollider>();stats.balloon=bird.transform;
        typeof(ObstacleStats).GetMethod("InitSeagull",Private).Invoke(stats,null);
        bird.SetActive(true);typeof(ObstacleStats).GetMethod("SetSeagullContact",Private).Invoke(stats,new object[]{true});
        TimeManager.isGameRunning=true;return stats;
    }
    [TestCase(100,100,80)] [TestCase(500,300,200)] [TestCase(500,50,0)]
    public void ActualBodyContact_RemovesTwentyPercentOfMaximumOnce(float maximum,float current,float expected)
    {
        var player=Player(maximum,current);var stats=Bird();var body=player.GetComponent<Collider>();
        Assert.That(stats.TrySeagullContact(body),Is.True);Assert.That(player.currentHealth,Is.EqualTo(expected));
        Assert.That(stats.TrySeagullContact(body),Is.False);Assert.That(player.currentHealth,Is.EqualTo(expected));
        Assert.That(stats.balloon.GetComponent<Collider>().enabled,Is.False);
    }
    [Test] public void WeaponOrVisualChildCollider_DoesNotCountAsPlayerBody()
    {
        var player=Player(100,100);var stats=Bird();var weapon=Make("Invisible weapon collider");weapon.transform.SetParent(player.transform);var collider=weapon.AddComponent<BoxCollider>();
        Assert.That(stats.TrySeagullContact(collider),Is.False);Assert.That(player.currentHealth,Is.EqualTo(100));
        Assert.That(stats.TrySeagullContact(player.GetComponent<Collider>()),Is.True);
    }
    [Test] public void HiddenTelegraphAndPausedGame_DoNotDamage()
    {
        var player=Player(100,100);var stats=Bird();stats.balloon.gameObject.SetActive(false);
        Assert.That(stats.TrySeagullContact(player.GetComponent<Collider>()),Is.False);
        stats.balloon.gameObject.SetActive(true);TimeManager.isGameRunning=false;
        Assert.That(stats.TrySeagullContact(player.GetComponent<Collider>()),Is.False);Assert.That(player.currentHealth,Is.EqualTo(100));
    }
    [Test] public void LandingKeepsContactEnabled_AndResetAllowsTheNextRun()
    {
        var player=Player(100,100);var stats=Bird();typeof(ObstacleStats).GetMethod("OnBalloonImpact",Private).Invoke(stats,null);
        Assert.That(stats.balloon.GetComponent<Collider>().enabled,Is.True);
        Assert.That(stats.TrySeagullContact(player.GetComponent<Collider>()),Is.True);
        typeof(ObstacleStats).GetMethod("InitSeagull",Private).Invoke(stats,null);stats.balloon.gameObject.SetActive(true);
        typeof(ObstacleStats).GetMethod("SetSeagullContact",Private).Invoke(stats,new object[]{true});
        Assert.That(stats.TrySeagullContact(player.GetComponent<Collider>()),Is.True);Assert.That(player.currentHealth,Is.EqualTo(60));
    }
    [Test] public void RetryRestoresAStandaloneHitSpin()
    {
        var player=Player(100,100);var model=Make("Spinning model");model.transform.SetParent(player.transform);
        var resting=Quaternion.Euler(0,15,0);model.transform.localRotation=resting;
        var spin=model.AddComponent<CosmeticHitSpin>();spin.Play(1.2f);model.transform.localRotation=Quaternion.Euler(0,95,0);
        player.ResetState();Assert.That(Quaternion.Angle(model.transform.localRotation,resting),Is.LessThan(.001f));
    }
    [TestCase(0f)] [TestCase(90f)]
    public void EarlyLandedBirdWaitsForRoadProgressBeforeDeparture(float heading)
    {
        var stats=Bird();stats.transform.rotation=Quaternion.Euler(0,heading,0);
        var player=Make("Approaching runner").transform;
        typeof(ObstacleStats).GetField("_player",Private).SetValue(stats,player);
        var impact=(Vector3)typeof(ObstacleStats).GetField("_impactPoint",Private).GetValue(stats);
        player.position=impact-stats.transform.forward*12+stats.transform.right*1.5f;
        bool Waiting()=>(bool)typeof(ObstacleStats).GetMethod("ShouldKeepSeagullLanded",Private).Invoke(stats,null);
        Assert.That(Waiting(),Is.True,"The bird must stay while the runner approaches, including a lateral dodge.");
        player.position=impact+stats.transform.forward*3+stats.transform.right*1.5f;
        Assert.That(Waiting(),Is.False,"Passing along the road releases the bird, regardless of lateral offset.");
        player.position=impact-stats.transform.forward*12;
        Assert.That(stats.TrySeagullContact(Player(100,100).GetComponent<Collider>()),Is.True);
        Assert.That(Waiting(),Is.False,"A consumed contact can release the bird without a second penalty.");
    }
    [Test] public void InitialStartCapturesPlacementAssignedAfterEnable()
    {
        var stats=Bird();stats.transform.position=new Vector3(5000,0,5000);
        typeof(ObstacleStats).GetMethod("Start",Private).Invoke(stats,null);
        Assert.That((Vector3)typeof(ObstacleStats).GetField("_impactPoint",Private).GetValue(stats),Is.EqualTo(stats.transform.position));
        Assert.That(stats.balloon.gameObject.activeSelf,Is.False);
        Assert.That(stats.balloon.GetComponent<Collider>().enabled,Is.False);
    }
    [TestCase(0f)] [TestCase(90f)]
    public void KnockbackCarriesTheImpactPoseIntoAnOutwardArcAndThreeTumbles(float heading)
    {
        var stats=Bird();var player=Player(100,100).transform;
        player.rotation=Quaternion.Euler(0,heading,0);stats.transform.rotation=player.rotation;
        stats.balloon.position=player.position+player.forward;
        var direction=(Vector3)typeof(ObstacleStats).GetMethod("SeagullKnockbackDirection",Private).Invoke(stats,new object[]{player});
        Assert.That(Vector3.Dot(direction,player.forward),Is.GreaterThan(.4f));
        Assert.That(Mathf.Abs(Vector3.Dot(direction,player.right)),Is.GreaterThan(.3f));
        var method=typeof(ObstacleStats).GetMethod("ApplySeagullKnockbackPose",Private);
        var start=stats.balloon.position;var rotation=stats.balloon.rotation;float degrees=0;
        var axis=(Vector3.Cross(Vector3.up,direction)+Vector3.up*.25f).normalized;
        for(int frame=0;frame<=60;frame++)
        {
            var previous=stats.balloon.rotation;
            method.Invoke(stats,new object[]{start,direction,rotation,frame/60f});
            var delta=stats.balloon.rotation*Quaternion.Inverse(previous);delta.ToAngleAxis(out float angle,out var turnAxis);
            if(angle>180)angle-=360;
            if(angle!=0)degrees+=angle*Mathf.Sign(Vector3.Dot(turnAxis,axis));
            if(frame==0)Assert.That(Vector3.Distance(stats.balloon.position,start),Is.LessThan(.001f));
            if(frame==30)Assert.That(stats.balloon.position.y-start.y,Is.GreaterThan(4f));
        }
        Assert.That(degrees,Is.EqualTo(1080f).Within(.1f));
        Assert.That(Vector3.Dot(stats.balloon.position-start,direction),Is.EqualTo(7.5f).Within(.001f));
    }
}
#endif
