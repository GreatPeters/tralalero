var catalog=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
var go=new UnityEngine.GameObject("Fitting deformation probe");var preview=go.AddComponent<CosmeticPreview>();preview.catalog=catalog;UnityEngine.Mesh baked=null;
try
{
    preview.Show("skin_original","shoes_gold","hat_none");
    var renderer=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.SkinnedMeshRenderer>().Single(r=>r.name=="__CosmeticShoes" && r.transform.root.name=="__CosmeticPreview");
    baked=new UnityEngine.Mesh();renderer.BakeMesh(baked,true);
    renderer.gameObject.AddComponent<UnityEngine.MeshFilter>().sharedMesh=baked;renderer.gameObject.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial=renderer.sharedMaterial;renderer.enabled=false;
    preview.Rotate(0);CosmeticPresentationBuilder.Save(preview.Texture,"map-concepts/skins-reststop-2026-09-12/fitting-cpu-scale-true.png");
    return new{quality=renderer.quality.ToString(),bones=renderer.bones.Length,vertices=baked.vertexCount,bounds=baked.bounds.ToString()};
}
finally{preview.Dispose();UnityEngine.Object.DestroyImmediate(go);if(baked!=null)UnityEngine.Object.DestroyImmediate(baked);}
