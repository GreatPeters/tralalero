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
}
#endif
