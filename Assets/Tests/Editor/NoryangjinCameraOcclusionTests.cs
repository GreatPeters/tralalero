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
    [Test] public void GroundedCombinedGantry_HidesAsAnExplicitProp_WhileRoadStaysVisible()
    {
        var player=Make("Player");var roads=Make("Roads");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(floor);floor.transform.SetParent(roads.transform);floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(10,1,50);
        var gantry=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(gantry);gantry.transform.position=new Vector3(0,3,-5);gantry.transform.localScale=new Vector3(8,6,1);
        var camera=Make("Camera");camera.transform.position=new Vector3(0,10,-10);
        var occlusion=camera.AddComponent<NoryangjinCameraOcclusion>();occlusion.Configure(player.transform,roads.transform);occlusion.ConfigureAdditionalOccluders(new[]{gantry.transform});occlusion.RefreshVisibility();
        Assert.That(gantry.GetComponent<Renderer>().forceRenderingOff,Is.True);Assert.That(floor.GetComponent<Renderer>().forceRenderingOff,Is.False);
        player.transform.position=Vector3.forward*40;camera.transform.position=new Vector3(0,10,30);occlusion.RefreshVisibility();
        Assert.That(gantry.GetComponent<Renderer>().forceRenderingOff,Is.False);
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
    [Test] public void AdditionalScenery_HidesWithoutMovingIt_AndRestoresWhenUnbound()
    {
        var player=Make("Player");var roads=Make("Roads");var props=Make("Props");
        var sign=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(sign);sign.transform.SetParent(props.transform);sign.transform.position=new Vector3(0,6,-5);sign.transform.localScale=new Vector3(10,1,2);
        var cam=Make("Camera");cam.transform.position=new Vector3(0,10,-10);var component=cam.AddComponent<NoryangjinCameraOcclusion>();component.Configure(player.transform,roads.transform);
        var renderer=sign.GetComponent<Renderer>();var position=sign.transform.position;
        var lettering=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(lettering);lettering.transform.SetParent(sign.transform);lettering.transform.position=new Vector3(4,6,-5);lettering.transform.localScale=Vector3.one*.01f;
        component.ConfigureAdditionalOccluders(new[]{sign.transform,sign.transform});component.RefreshVisibility();
        Assert.That(component.HiddenCount,Is.EqualTo(2));Assert.That(renderer.forceRenderingOff,Is.True);Assert.That(lettering.GetComponent<Renderer>().forceRenderingOff,Is.True);Assert.That(sign.transform.position,Is.EqualTo(position));Assert.That(sign.GetComponent<Collider>().enabled,Is.True);
        component.ConfigureAdditionalOccluders(System.Array.Empty<Transform>());component.RefreshVisibility();Assert.That(renderer.forceRenderingOff,Is.False);Assert.That(lettering.GetComponent<Renderer>().forceRenderingOff,Is.False);Assert.That(component.HiddenCount,Is.Zero);
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
}
#endif
