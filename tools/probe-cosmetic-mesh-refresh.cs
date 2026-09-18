var catalog=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var go=new UnityEngine.GameObject("Mesh refresh probe");var preview=go.AddComponent<CosmeticPreview>();preview.catalog=catalog;UnityEngine.Mesh copy=null;
try
{
 preview.Show("skin_original","shoes_gold","hat_none");var r=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.SkinnedMeshRenderer>().Single(x=>x.name=="__CosmeticShoes"&&x.transform.root.name=="__CosmeticPreview");
 var old=r.sharedMesh;copy=new UnityEngine.Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};copy.vertices=old.vertices;copy.normals=old.normals;copy.uv=old.uv;copy.triangles=old.triangles;copy.bindposes=old.bindposes;copy.boneWeights=old.boneWeights;copy.RecalculateTangents();copy.RecalculateBounds();r.sharedMesh=null;copy.UploadMeshData(false);r.sharedMesh=copy;preview.Rotate(0);CosmeticPresentationBuilder.Save(preview.Texture,"map-concepts/skins-reststop-2026-09-12/fitting-fresh-mesh.png");return copy.vertexCount;
}
finally{preview.Dispose();UnityEngine.Object.DestroyImmediate(go);if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);}
