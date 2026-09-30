using NUnit.Framework;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public sealed class NoryangjinFeedbackV3Tests
{
    [Test] public void SmallCeilingTileCoveringOnlyTheSharkStillFades()
    {
        var player=new GameObject("Player");var roads=new GameObject("Roads");var cam=new GameObject("Camera");var tile=GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            cam.transform.position=new Vector3(0,12,-20);tile.transform.position=new Vector3(0,7.8f,-12.08f);tile.transform.localScale=new Vector3(3.3f,.075f,.6f);
            var oc=cam.AddComponent<NoryangjinCameraOcclusion>();oc.Configure(player.transform,roads.transform);oc.ConfigureFeedbackTransparency(new[]{tile.transform});oc.RefreshVisibility(1);
            Assert.That(oc.SceneryOpacity(tile.GetComponent<Renderer>()),Is.LessThan(.3f));Assert.That(tile.GetComponent<Collider>().enabled,Is.True);
        }
        finally{Object.DestroyImmediate(cam);Object.DestroyImmediate(player);Object.DestroyImmediate(roads);Object.DestroyImmediate(tile);}
    }
    [Test] public void FeedbackOcclusionIsTranslucentAndPreservesFloorAndCollision()
    {
        var p=new GameObject("Player");var roads=new GameObject("Roads");var cam=new GameObject("Camera");
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            wall.transform.position=new Vector3(0,5,-5);wall.transform.localScale=new Vector3(10,10,1);
            floor.transform.SetParent(roads.transform);floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(10,1,50);
            cam.transform.position=new Vector3(0,10,-10);var view=cam.AddComponent<NoryangjinCameraOcclusion>();view.Configure(p.transform,roads.transform);view.ConfigureFeedbackTransparency(new[]{wall.transform});view.RefreshVisibility(.5f);
            Assert.That(wall.GetComponent<Renderer>().forceRenderingOff,Is.False);
            Assert.That(view.SceneryOpacity(wall.GetComponent<Renderer>()),Is.EqualTo(.25f).Within(.001f));
            Assert.That(floor.GetComponent<Renderer>().forceRenderingOff,Is.False);Assert.That(wall.GetComponent<Collider>().enabled,Is.True);
            view.ConfigureFeedbackTransparency(null);Assert.That(view.FadedCount,Is.Zero);
        }
        finally{Object.DestroyImmediate(cam);Object.DestroyImmediate(p);Object.DestroyImmediate(roads);Object.DestroyImmediate(wall);}
    }
    [TestCase(0,0,true)] [TestCase(1,1,true)] [TestCase(3,0,false)] [TestCase(0,3,false)]
    public void WetFootprintMatchesItsActualSurface(float x,float z,bool expected)
        =>Assert.That(NoryangjinWetPatch.Contains(new Vector3(x,.1f,z),new Vector2(1.5f,1.4f)),Is.EqualTo(expected));
    [Test] public void PuddleRemainsAfterSprayAndDriesThenResets()
    {
        var go=new GameObject("Puddle");
        try
        {
            var p=go.AddComponent<NoryangjinWetPatch>();p.drySeconds=10;p.SetSpraying(true);p.AdvanceDrying(20);Assert.That(p.Wetness,Is.EqualTo(1));
            p.SetSpraying(false);p.AdvanceDrying(4);Assert.That(p.Wetness,Is.EqualTo(.6f).Within(.001f));p.AdvanceDrying(20);Assert.That(p.Wetness,Is.Zero);
            p.SetSpraying(true);p.ResetPatch();Assert.That(p.Wetness,Is.Zero);
        }
        finally{Object.DestroyImmediate(go);}
    }
    [Test] public void SlipRestoresFastTestSensitivity()
    {
        var go=new GameObject("Player");
        try
        {
            var p=go.AddComponent<PlayerScript>();p.moveSensitivity_Devision=31.25f;
            var slip=go.AddComponent<NoryangjinWetSteering>();slip.Apply(p,1.7f,.95f);
            Assert.That(slip.SidewaysSpeed,Is.GreaterThan(0));Assert.That(p.moveSensitivity_Devision,Is.GreaterThan(31.25f));slip.Clear();Assert.That(p.moveSensitivity_Devision,Is.EqualTo(31.25f));
        }
        finally{Object.DestroyImmediate(go);}
    }
    [Test] public void RollerSlatsRetractIntoHousingAndReappearOnClose()
    {
        var go=new GameObject("Roller");
        try
        {
            var r=go.AddComponent<NoryangjinRollerShutterVisual>();r.height=6;r.slats=new Transform[10];
            for(int i=0;i<10;i++){r.slats[i]=new GameObject("Slat").transform;r.slats[i].SetParent(go.transform);}
            r.bottomRail=new GameObject("Rail").transform;r.bottomRail.SetParent(go.transform);r.SetOpening(4);
            int active=0;foreach(var t in r.slats)if(t.gameObject.activeSelf){active++;Assert.That(t.localPosition.y,Is.InRange(4f,6f));}
            Assert.That(active,Is.LessThan(10));Assert.That(r.bottomRail.localPosition.y,Is.EqualTo(4.07f).Within(.001f));
            r.SetOpening(0);foreach(var t in r.slats)Assert.That(t.gameObject.activeSelf,Is.True);
        }
        finally{Object.DestroyImmediate(go);}
    }
    [Test] public void ContainerShadowScaleDoesNotShrinkAcrossRestarts()
    {
        var go=new GameObject("ContainerEvent");var shadow=new GameObject("Shadow");shadow.transform.SetParent(go.transform);
        try
        {
            var e=go.AddComponent<NoryangjinMarketIncident>();e.warning=shadow.transform;shadow.transform.localScale=new Vector3(3.3f,5.4f,1);e.ResetForRun();shadow.transform.localScale=Vector3.one*.1f;e.ResetForRun();Assert.That(shadow.transform.localScale,Is.EqualTo(new Vector3(3.3f,5.4f,1)));
        }
        finally{Object.DestroyImmediate(go);}
    }
}
