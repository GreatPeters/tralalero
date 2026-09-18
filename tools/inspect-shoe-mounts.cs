var cat=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
var model=UnityEngine.Object.Instantiate(cat.previewModel);
try
{
    var renderer=model.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).First(r=>r.sharedMesh.vertexCount==cat.sourceSharkMesh.vertexCount);
    var result=renderer.bones.Select((b,i)=>new{b,i}).Where(x=>x.b.name.EndsWith("leg2")).Select(x=>
    {
        var bind=cat.sourceSharkMesh.bindposes[x.i];var local=UnityEngine.Matrix4x4.TRS(UnityEngine.Vector3.zero,bind.rotation,bind.lossyScale);
        var actual=renderer.transform.worldToLocalMatrix*x.b.localToWorldMatrix*bind;
        return new{bone=x.b.name,det=bind.determinant,scale=bind.lossyScale,rotation=bind.rotation, actualUp=actual.MultiplyVector(UnityEngine.Vector3.up),actualForward=actual.MultiplyVector(UnityEngine.Vector3.forward),
          xError=(bind.MultiplyVector(UnityEngine.Vector3.right)-local.MultiplyVector(UnityEngine.Vector3.right)).magnitude,
          yError=(bind.MultiplyVector(UnityEngine.Vector3.up)-local.MultiplyVector(UnityEngine.Vector3.up)).magnitude,
          zError=(bind.MultiplyVector(UnityEngine.Vector3.forward)-local.MultiplyVector(UnityEngine.Vector3.forward)).magnitude};
    }).ToArray();
    return string.Join("\n",result.Select(r=>$"{r.bone} det={r.det} up={r.actualUp} forward={r.actualForward} error={r.xError},{r.yError},{r.zError}"))+"\n"+string.Join(",",renderer.bones.Select((b,i)=>i+":"+b.name));
}
finally{UnityEngine.Object.DestroyImmediate(model);}
