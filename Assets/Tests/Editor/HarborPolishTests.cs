using NUnit.Framework;
using UnityEngine;

public sealed class HarborPolishTests
{
    [Test] public void EnemyRadius_GrowsOnceWithoutChangingHeight()
    {
        var go=new GameObject("Hit target");
        try
        {
            var collider=go.AddComponent<CapsuleCollider>();collider.isTrigger=true;collider.radius=.33f;collider.height=2;
            var tuning=go.AddComponent<EnemyHitboxSize>();tuning.Configure(collider);tuning.Configure(collider);
            Assert.That(collider.radius,Is.EqualTo(.43f).Within(.0001f));Assert.That(collider.height,Is.EqualTo(2));
        }
        finally{Object.DestroyImmediate(go);}
    }
    [Test] public void BoxTarget_GrowsHorizontallyWithoutGrowingVertically()
    {
        var go=new GameObject("Box target");
        try
        {
            var collider=go.AddComponent<BoxCollider>();collider.size=new Vector3(2,4,6);
            go.AddComponent<EnemyHitboxSize>().Configure(collider);
            Assert.That(collider.size.x,Is.EqualTo(2.6f).Within(.001f));Assert.That(collider.size.y,Is.EqualTo(4));Assert.That(collider.size.z,Is.EqualTo(7.8f).Within(.001f));
        }
        finally{Object.DestroyImmediate(go);}
    }
    [Test] public void SceneRadiusOverride_IsNotReplacedByThePrefabBaseline()
    {
        var go=new GameObject("Overridden target");
        try
        {
            var collider=go.AddComponent<CapsuleCollider>();collider.radius=.33f;
            var tuning=go.AddComponent<EnemyHitboxSize>();tuning.Configure(collider);
            collider.radius=.67f;tuning.Configure(collider);tuning.Configure(collider);
            Assert.That(collider.radius,Is.EqualTo(.87f).Within(.001f));
        }
        finally{Object.DestroyImmediate(go);}
    }
    [Test] public void CameraFollowsYawWithoutInheritingSlopePitchOrRoll()
    {
        var target=new GameObject("Target");var camera=new GameObject("Camera");
        try
        {
            camera.transform.SetPositionAndRotation(new Vector3(0,10,-12),Quaternion.Euler(35,0,0));
            var follow=camera.AddComponent<StableGameplayCamera>();follow.Configure(target.transform);
            target.transform.rotation=Quaternion.Euler(22,90,15);
            typeof(StableGameplayCamera).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(follow,null);
            Assert.That(Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(0,90,0)*Quaternion.Euler(35,0,0)),Is.LessThan(.01f));
            Assert.That(camera.transform.position.y,Is.EqualTo(10).Within(.001f));
            Assert.That(camera.transform.position.x,Is.EqualTo(-12).Within(.001f));
        }
        finally{Object.DestroyImmediate(camera);Object.DestroyImmediate(target);}
    }
}
