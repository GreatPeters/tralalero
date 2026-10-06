using System.Linq;
using NUnit.Framework;
using UnityEngine;

// Guards the reference-fit player mesh (tools/build-tralalero-reference-player.py):
// upright caudal fin kept, lower lobe turned into the tail leg/shoe, front legs
// under the chest. Units are normalised so the source shark is 2.8 tall.
public sealed class TralaleroReferenceAnatomyTests
{
    static CosmeticVisualCatalog Catalog()=>Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
    static float Scale(CosmeticVisualCatalog c){var v=c.sourceSharkMesh.vertices;return 2.8f/(v.Max(p=>p.y)-v.Min(p=>p.y));}
    // Model frame used by the builder: x right, y toward the tail, z up.
    static Vector3 View(Vector3 p,float s)=>new Vector3(p.x*s,-p.z*s,p.y*s);

    [TestCase("default")][TestCase("body")]
    public void InstalledBodyKeepsTheUprightCaudalFin(string key)
    {
        var c=Catalog();float s=Scale(c);var mesh=key=="default"?c.splitSharkMesh:c.bodyOnlyMesh;
        var tail=mesh.vertices.Select(p=>View(p,s)).Where(p=>p.y>2.7f).ToArray();
        Assert.That(tail.Length,Is.GreaterThan(50),"upper lobe vertices behind the fork");
        Assert.That(tail.Max(p=>p.z),Is.GreaterThan(2.1f),"upper lobe rises above the back");
        Assert.That(tail.Max(p=>p.y),Is.GreaterThan(2.95f),"upper lobe keeps its length");
    }

    [Test]
    public void TailLegReplacesTheLowerLobeAndEndsAtTheTailShoe()
    {
        var c=Catalog();float s=Scale(c);var mesh=c.splitSharkMesh;var v=mesh.vertices;var w=mesh.boneWeights;
        int footIndex=c.sourceSharkMesh.bindposes.Length+3;
        Assert.That(mesh.bindposes.Length,Is.EqualTo(footIndex+1));
        var foot=Enumerable.Range(0,v.Length).Where(i=>w[i].boneIndex0==footIndex&&w[i].weight0>.9f).Select(i=>View(v[i],s)).ToArray();
        Assert.That(foot.Length,Is.GreaterThan(100),"third shoe and leg end follow TailFoot");
        Assert.That(foot.Min(p=>p.z),Is.LessThan(.2f),"tail shoe reaches the ground");
        Assert.That(foot.Average(p=>Mathf.Abs(p.x)),Is.LessThan(.25f),"tail shoe sits on the midline");
        // The source lower lobe (tail2/tail3) hung down to z~0.47; it is now the leg.
        var lobe=Enumerable.Range(0,v.Length).Where(i=>(w[i].boneIndex0==4||w[i].boneIndex0==5)&&w[i].weight0>.5f).Select(i=>View(v[i],s)).Where(p=>p.y>2f&&p.z<.75f).ToArray();
        Assert.That(lobe.Length,Is.EqualTo(0));
    }

    [Test]
    public void FrontShoesStandUnderTheChestOnEveryFittedStyle()
    {
        var c=Catalog();float s=Scale(c);
        foreach(var mesh in new[]{c.splitSharkMesh}.Concat(c.entries.Where(e=>e.fittedShoeMesh!=null).Select(e=>e.fittedShoeMesh)))
        {
            var v=mesh.vertices;var front=v.Select(p=>View(p,s)).Where(p=>p.y<-.7f&&p.z<.45f).ToArray();
            Assert.That(front.Length,Is.GreaterThan(100),mesh.name);
            Assert.That(front.Max(p=>Mathf.Abs(p.x)),Is.LessThan(1.25f),mesh.name+" front shoes splay out sideways");
        }
    }
}
