using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SharkHeadwearFitTests
{
    private static CosmeticVisualCatalog Catalog => AssetDatabase.LoadAssetAtPath<CosmeticVisualCatalog>("Assets/ShooterSurvival/Resources/Cosmetics/Catalog.asset");

    [Test] public void SavedSocketSeatsInsideTheForeheadInsteadOfAboveTheBrim()
    {
        var catalog=Catalog;var renderer=catalog.previewModel.GetComponentInChildren<SkinnedMeshRenderer>(true);
        int head=Array.FindIndex(renderer.bones,b=>b.name=="head");
        var point=catalog.splitSharkMesh.bindposes[head].inverse.MultiplyPoint3x4(catalog.hatLocalPosition);
        float surface=SharkHeadwearFitter.SurfaceHeight(catalog.splitSharkMesh,point.z);
        Assert.That(point.z,Is.EqualTo(SharkHeadwearFitter.ForeheadZ).Within(.0000001f));
        Assert.That(surface-point.y,Is.EqualTo(SharkHeadwearFitter.SeatingDepth).Within(.0000001f));
    }

    [Test] public void ChangingBodySkinAndFootwearKeepsOneHatOnTheSameHeadSocket()
    {
        var catalog=Catalog;var model=UnityEngine.Object.Instantiate(catalog.previewModel);
        try
        {
            foreach(var skin in catalog.entries.Where(e=>e.key.StartsWith("skin_")))
            foreach(var shoes in new[]{"shoes_original","shoes_mint"})
            {
                CosmeticAppearance.Apply(model.transform,catalog,skin.key,shoes,"hat_bucket");
                var hats=model.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="__CosmeticHat"&&t.gameObject.activeSelf).ToArray();
                Assert.That(hats.Length,Is.EqualTo(1),skin.key+" / "+shoes);
                Assert.That(hats[0].parent.name,Is.EqualTo("head"));
                Assert.That(Vector3.Distance(hats[0].localPosition,catalog.hatLocalPosition),Is.LessThan(.0000001f));
                Assert.That(Quaternion.Angle(hats[0].localRotation,catalog.hatLocalRotation),Is.LessThan(.01f));
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(model);}
    }

    [Test] public void PirateLiningClosesTheCrownWithoutAddingCollision()
    {
        var pirate=Catalog.Find("hat_pirate").accessory;
        Assert.That(pirate.transform.Find("Inner crown lining"),Is.Not.Null);
        Assert.That(pirate.GetComponentsInChildren<Collider>(true),Is.Empty);
    }
}
