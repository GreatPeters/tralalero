#if UNITY_EDITOR
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class NoryangjinCameraOcclusionTests
{
    private readonly List<GameObject> objects = new();
    private GameObject Make(string name) { var go=new GameObject(name);objects.Add(go);return go; }
    [TearDown] public void Cleanup() { foreach(var go in objects)if(go!=null)Object.DestroyImmediate(go);objects.Clear(); }
    [Test] public void SevereGantryFadesToFortyPercent_WhileRoadAndCollisionStayVisible()
    {
        var player=Make("Player");var roads=Make("Roads");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(floor);floor.transform.SetParent(roads.transform);floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(10,1,50);
        var gantry=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(gantry);gantry.transform.position=new Vector3(0,5,-5);gantry.transform.localScale=new Vector3(8,10,1);
        var camera=Make("Camera");camera.transform.position=new Vector3(0,10,-10);
        var occlusion=camera.AddComponent<NoryangjinCameraOcclusion>();occlusion.Configure(player.transform,roads.transform);occlusion.ConfigureAdditionalOccluders(new[]{gantry.transform});occlusion.RefreshVisibility();
        Assert.That(gantry.GetComponent<Renderer>().forceRenderingOff,Is.False);Assert.That(floor.GetComponent<Renderer>().forceRenderingOff,Is.False);
        Assert.That(occlusion.SceneryOpacity(gantry.GetComponent<Renderer>()),Is.EqualTo(.4f).Within(.001f));Assert.That(gantry.GetComponent<Collider>().enabled,Is.True);
        player.transform.position=Vector3.forward*40;camera.transform.position=new Vector3(0,10,30);occlusion.RefreshVisibility();
        Assert.That(gantry.GetComponent<Renderer>().forceRenderingOff,Is.False);
        Assert.That(occlusion.FadedCount,Is.Zero);
    }
    [Test] public void OverheadOccluder_HidesThenRestoresWithoutRemovingFloorOrColliders()
    {
        var player=Make("Player");var roads=Make("Roads");
        var bridge=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(bridge);bridge.transform.SetParent(roads.transform);bridge.transform.position=new Vector3(0,6,-5);bridge.transform.localScale=new Vector3(10,1,2);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(floor);floor.transform.SetParent(roads.transform);floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(10,1,50);
        var cam=Make("Camera");cam.transform.position=new Vector3(0,10,-10);
        var component=cam.AddComponent<NoryangjinCameraOcclusion>();component.Configure(player.transform,roads.transform);component.RefreshVisibility();
        Assert.That(bridge.GetComponent<Renderer>().forceRenderingOff,Is.True);
        Assert.That(floor.GetComponent<Renderer>().forceRenderingOff,Is.False);
        Assert.That(bridge.GetComponent<Collider>().enabled,Is.True);
        player.transform.position=Vector3.forward*40;cam.transform.position=new Vector3(0,10,30);component.RefreshVisibility();
        Assert.That(component.HiddenCount,Is.Zero);Assert.That(bridge.GetComponent<Renderer>().forceRenderingOff,Is.False);
    }
    [Test] public void Disable_RestoresOriginalRendererState()
    {
        var p=Make("Player");var root=Make("Roads");var b=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(b);b.transform.SetParent(root.transform);b.transform.position=new Vector3(0,6,-5);b.transform.localScale=new Vector3(10,1,2);
        var cam=Make("Camera");cam.transform.position=new Vector3(0,10,-10);var component=cam.AddComponent<NoryangjinCameraOcclusion>();component.Configure(p.transform,root.transform);
        // Non-ExecuteAlways behaviours do not receive these callbacks in Edit Mode.
        var disable=typeof(NoryangjinCameraOcclusion).GetMethod("OnDisable",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        component.RefreshVisibility();disable.Invoke(component,null);Assert.That(b.GetComponent<Renderer>().forceRenderingOff,Is.False);
        b.GetComponent<Renderer>().forceRenderingOff=true;component.RefreshVisibility();disable.Invoke(component,null);Assert.That(b.GetComponent<Renderer>().forceRenderingOff,Is.True);
    }
    [Test] public void AdditionalSceneryFadesWithoutMovingIt_AndRestoresWhenUnbound()
    {
        var player=Make("Player");var roads=Make("Roads");var props=Make("Props");
        var sign=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(sign);sign.transform.SetParent(props.transform);sign.transform.position=new Vector3(0,5,-5);sign.transform.localScale=new Vector3(10,8,2);
        var cam=Make("Camera");cam.transform.position=new Vector3(0,10,-10);var component=cam.AddComponent<NoryangjinCameraOcclusion>();component.Configure(player.transform,roads.transform);
        var renderer=sign.GetComponent<Renderer>();var position=sign.transform.position;
        var lettering=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(lettering);lettering.transform.SetParent(sign.transform);lettering.transform.position=new Vector3(4,6,-5);lettering.transform.localScale=Vector3.one*.01f;
        component.ConfigureAdditionalOccluders(new[]{sign.transform,sign.transform});component.RefreshVisibility();
        Assert.That(component.FadedCount,Is.EqualTo(2));Assert.That(renderer.forceRenderingOff,Is.False);Assert.That(lettering.GetComponent<Renderer>().forceRenderingOff,Is.False);Assert.That(sign.transform.position,Is.EqualTo(position));Assert.That(sign.GetComponent<Collider>().enabled,Is.True);
        component.ConfigureAdditionalOccluders(System.Array.Empty<Transform>());component.RefreshVisibility();Assert.That(renderer.forceRenderingOff,Is.False);Assert.That(lettering.GetComponent<Renderer>().forceRenderingOff,Is.False);Assert.That(component.HiddenCount,Is.Zero);
        Assert.That(component.FadedCount,Is.Zero);
    }

    [Test] public void PartialBodyOcclusionAndSideIntrusionRemainOpaque()
    {
        var cam=new Vector3(0,10,-10);var torso=new Vector3(0,1.4f,0);
        Assert.That(NoryangjinCameraOcclusion.SeverelyBlocks(new Bounds(new Vector3(0,6,-5),new Vector3(10,1,1)),cam,torso,Vector3.forward,Vector3.right),Is.False,"A beam across the shark only must not erase scenic depth.");
        Assert.That(NoryangjinCameraOcclusion.SeverelyBlocks(new Bounds(new Vector3(4,5,-5),new Vector3(1,10,1)),cam,torso,Vector3.forward,Vector3.right),Is.False);
    }

    [Test] public void ConnectedUphillTiles_RemainVisibleAboveTheCurrentFloor()
    {
        var player=Make("Climbing player");player.transform.position=Vector3.up*.12f;
        var roads=Make("Ramp tiles");var renderers=new List<Renderer>();
        for(int i=0;i<24;i++)
        {
            var tile=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(tile);tile.transform.SetParent(roads.transform);
            tile.transform.position=new Vector3(0,i*.25f-.25f,i*.5f);tile.transform.localScale=new Vector3(8,.5f,.55f);
            Object.DestroyImmediate(tile.GetComponent<BoxCollider>());
            tile.AddComponent<MeshCollider>().sharedMesh=tile.GetComponent<MeshFilter>().sharedMesh;
            renderers.Add(tile.GetComponent<Renderer>());
        }
        player.AddComponent<NoryangjinRoadHeightFollower>().Configure(roads.transform,.12f);
        var camera=Make("Ramp camera");camera.transform.position=new Vector3(0,12,-19);
        var occlusion=camera.AddComponent<NoryangjinCameraOcclusion>();occlusion.Configure(player.transform,roads.transform);
        Physics.SyncTransforms();occlusion.RefreshVisibility();
        foreach(var renderer in renderers)Assert.That(renderer.forceRenderingOff,Is.False,"Connected uphill road must not expose the lower deck");
    }
    [Test] public void CombinedTallRoadUsesColliderAndPreservesOrdinaryFloor()
    {
        var p=Make("Player");var roads=Make("Roads");
        var combined=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(combined);combined.transform.SetParent(roads.transform);
        combined.transform.position=new Vector3(0,4,-5);combined.transform.localScale=new Vector3(10,12,1);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(floor);floor.transform.SetParent(roads.transform);floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(10,1,50);
        var camera=Make("Camera");camera.transform.position=new Vector3(0,10,-10);
        var view=camera.AddComponent<NoryangjinCameraOcclusion>();view.Configure(p.transform,roads.transform);view.ConfigureClearViewOccluders(null,true);
        Physics.SyncTransforms();view.RefreshVisibility();
        Assert.That(combined.GetComponent<Renderer>().forceRenderingOff,Is.True);
        Assert.That(combined.GetComponent<Collider>().enabled,Is.True);Assert.That(floor.GetComponent<Renderer>().forceRenderingOff,Is.False);
        view.ConfigureClearViewOccluders(null,false);Assert.That(combined.GetComponent<Renderer>().forceRenderingOff,Is.False);
    }
    [Test] public void ExplicitClearViewShopHidesAllChildrenAndRestoresWhenUnbound()
    {
        var p=Make("Player");var roads=Make("Roads");var shop=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(shop);shop.transform.position=new Vector3(0,5,-5);shop.transform.localScale=new Vector3(10,10,1);
        var camera=Make("Camera");camera.transform.position=new Vector3(0,10,-10);
        var view=camera.AddComponent<NoryangjinCameraOcclusion>();view.Configure(p.transform,roads.transform);view.ConfigureClearViewOccluders(new[]{shop.transform},true);view.RefreshVisibility();
        Assert.That(shop.GetComponent<Renderer>().forceRenderingOff,Is.True);Assert.That(shop.GetComponent<Collider>().enabled,Is.True);
        view.ConfigureClearViewOccluders(null,false);Assert.That(shop.GetComponent<Renderer>().forceRenderingOff,Is.False);
    }
    [Test] public void DistantGroupsSkipDetailedWork_AndMovingGroupEntersView()
    {
        var p=Make("Player");var roads=Make("Roads");var groups=new List<Transform>();
        for(int i=0;i<80;i++){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(g);g.transform.position=new Vector3(200+i*10,5,-5);g.transform.localScale=new Vector3(10,10,1);groups.Add(g.transform);}
        var camera=Make("Camera");camera.transform.position=new Vector3(0,10,-10);
        var view=camera.AddComponent<NoryangjinCameraOcclusion>();view.Configure(p.transform,roads.transform);view.ConfigureFeedbackTransparency(groups.ToArray());view.RefreshVisibility();
        Assert.That(view.CandidateGroupCount,Is.EqualTo(80));Assert.That(view.VisitedGroupCount,Is.Zero);
        groups[0].position=new Vector3(0,5,-5);view.RefreshVisibility();
        Assert.That(view.VisitedGroupCount,Is.EqualTo(1));Assert.That(view.FadedCount,Is.EqualTo(1));
        groups[0].position=new Vector3(300,5,-5);view.RefreshVisibility();
        Assert.That(view.FadedCount,Is.Zero,"A distant previously faded group must restore.");
        view.RefreshVisibility();Assert.That(view.VisitedGroupCount,Is.Zero);
    }
    [Test] public void DecorativeFloorStaysOpaqueWhileWalkingOnItsSeparateCollider()
    {
        var roads=Make("Roads");var player=Make("Player");player.transform.position=new Vector3(0,8.12f,0);
        var support=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(support);support.transform.SetParent(roads.transform);support.transform.position=new Vector3(0,7.75f,0);support.transform.localScale=new Vector3(10,.5f,60);
        Object.DestroyImmediate(support.GetComponent<BoxCollider>());support.AddComponent<MeshCollider>().sharedMesh=support.GetComponent<MeshFilter>().sharedMesh;
        var decoration=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(decoration);decoration.transform.position=new Vector3(0,8.02f,0);decoration.transform.localScale=new Vector3(10,.03f,60);Object.DestroyImmediate(decoration.GetComponent<Collider>());
        player.AddComponent<NoryangjinRoadHeightFollower>().Configure(roads.transform,.12f);
        var camera=Make("Camera");camera.transform.position=new Vector3(0,4,-10);var view=camera.AddComponent<NoryangjinCameraOcclusion>();view.Configure(player.transform,roads.transform);view.ConfigureFeedbackTransparency(new[]{decoration.transform});
        Physics.SyncTransforms();view.RefreshVisibility();Assert.That(view.SceneryOpacity(decoration.GetComponent<Renderer>()),Is.LessThan(1));
        view.ConfigureWalkingFloor(support.transform,new[]{decoration.GetComponent<Renderer>()});view.RefreshVisibility();
        Assert.That(view.SceneryOpacity(decoration.GetComponent<Renderer>()),Is.EqualTo(1));Assert.That(support.GetComponent<Collider>().enabled,Is.True);
        player.transform.position=new Vector3(0,.12f,0);camera.transform.position=new Vector3(0,12,-10);view.RefreshVisibility();Assert.That(view.SceneryOpacity(decoration.GetComponent<Renderer>()),Is.LessThan(1),"The same floor can still fade while passing underneath it.");
    }
    [Test] public void CurtainStripsAreConsideredTogetherForClearView()
    {
        var p=Make("Player");var roads=Make("Roads");var curtain=Make("Curtain");var strips=new List<Renderer>();
        for(int i=0;i<16;i++)
        {
            var strip=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(strip);strip.transform.SetParent(curtain.transform);strip.transform.position=new Vector3((i-7.5f)*.65f,5,-5);strip.transform.localScale=new Vector3(.6f,10,.08f);strips.Add(strip.GetComponent<Renderer>());
        }
        var camera=Make("Camera");camera.transform.position=new Vector3(0,10,-10);
        var view=camera.AddComponent<NoryangjinCameraOcclusion>();view.Configure(p.transform,roads.transform);view.ConfigureClearViewOccluders(new[]{curtain.transform},true);view.RefreshVisibility();
        foreach(var strip in strips)Assert.That(strip.forceRenderingOff,Is.True);
        view.ConfigureClearViewOccluders(null,false);foreach(var strip in strips)Assert.That(strip.forceRenderingOff,Is.False);
    }
}
#endif
