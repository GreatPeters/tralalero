var cat=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var go=UnityEngine.Object.Instantiate(cat.previewModel);UnityEngine.Mesh baked=new UnityEngine.Mesh();
try
{
 var r=go.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).First();r.BakeMesh(baked);
 var skin=r.transform.worldToLocalMatrix*r.bones[0].localToWorldMatrix*r.sharedMesh.bindposes[0];
 return new{rootScale=go.transform.localScale.ToString(),rendererScale=r.transform.localScale.ToString(),worldScale=r.transform.lossyScale.ToString(),rawBounds=r.sharedMesh.bounds.ToString(),bakedBounds=baked.bounds.ToString(),fittedBounds=cat.Find("shoes_gold").fittedShoeMesh.bounds.ToString(),skin=skin.ToString()};
}
finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(baked);}
