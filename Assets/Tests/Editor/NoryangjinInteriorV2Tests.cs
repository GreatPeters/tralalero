using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed class NoryangjinInteriorV2Tests
{
    [Test]
    public void CompactDamageReadoutPreservesHealthAndRestoresFullBarOnReset()
    {
        var go=new GameObject("BreakableFixture");
        try
        {
            var block=go.AddComponent<NoryangjinBreakable>();block.showHealth=false;block.hitsToBreak=2;
            var bar=new GameObject("Bar").transform;bar.SetParent(go.transform);bar.localScale=new Vector3(4,.2f,.1f);
            var text=new GameObject("Damage").AddComponent<TextMeshPro>();text.transform.SetParent(go.transform);
            block.healthBarFill=bar;block.attachedDamageLabel=text;block.Bind(null);block.ResetForRun();
            block.Damage(25);
            Assert.That(block.Health,Is.EqualTo(75));Assert.That(bar.localScale.x,Is.EqualTo(3));
            Assert.That(text.text,Is.EqualTo("25"));
            block.ResetForRun();
            Assert.That(block.Health,Is.EqualTo(100));Assert.That(bar.localScale.x,Is.EqualTo(4));
            Assert.That(text.gameObject.activeSelf,Is.False);
        }
        finally{Object.DestroyImmediate(go);}
    }
    [Test]
    public void InteriorHidesOldRoadVisualsAndNearDisplaysAndRestoresThemOnDisable()
    {
        var go=new GameObject("VisibilityFixture");var player=new GameObject("PlayerPosition");
        var road=GameObject.CreatePrimitive(PrimitiveType.Cube);
        bool fog=RenderSettings.fog;var color=RenderSettings.fogColor;
        try
        {
            var view=go.AddComponent<NoryangjinInteriorDetailVisibility>();
            var near=new GameObject("NearDisplay").transform;near.SetParent(go.transform);near.localPosition=new Vector3(0,6,20);
            var far=new GameObject("AheadDisplay").transform;far.SetParent(go.transform);far.localPosition=new Vector3(0,6,60);
            view.hangingDisplays=new[]{near,far};view.oldRoadVisuals=new[]{road.GetComponent<Renderer>()};
            Invoke(view,"Start");player.transform.position=new Vector3(0,0,30);
            typeof(NoryangjinInteriorDetailVisibility).GetField("player",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,player.transform);
            Invoke(view,"Update");
            Assert.That(near.gameObject.activeSelf,Is.False);Assert.That(far.gameObject.activeSelf,Is.True);
            Assert.That(road.GetComponent<Renderer>().enabled,Is.False);
            Assert.That(road.GetComponent<Collider>().enabled,Is.True,"Ground collision must remain available.");
            Invoke(view,"OnDisable");
            Assert.That(near.gameObject.activeSelf,Is.True);Assert.That(road.GetComponent<Renderer>().enabled,Is.True);
            Assert.That(RenderSettings.fog,Is.EqualTo(fog));Assert.That(RenderSettings.fogColor,Is.EqualTo(color));
        }
        finally{Object.DestroyImmediate(go);Object.DestroyImmediate(player);Object.DestroyImmediate(road);}
    }
    static void Invoke(object target,string method)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
}
