var r=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.SkinnedMeshRenderer>().Single(x=>x.name=="__CosmeticShoes"&&x.transform.root.name=="__CosmeticPreview");
var mesh=r.sharedMesh;var poses=mesh.bindposes;var verts=mesh.vertices;var weights=mesh.boneWeights;var a=new UnityEngine.Mesh();var b=new UnityEngine.Mesh();r.BakeMesh(a,false);r.BakeMesh(b,true);
try
{
 var v=verts[0];var w=weights[0];UnityEngine.Vector3 P(int i)=>r.bones[i].localToWorldMatrix.MultiplyPoint3x4(poses[i].MultiplyPoint3x4(v));
 var world=P(w.boneIndex0)*w.weight0+P(w.boneIndex1)*w.weight1+P(w.boneIndex2)*w.weight2+P(w.boneIndex3)*w.weight3;
 return new{weights=$"{w.boneIndex0}:{w.weight0} {w.boneIndex1}:{w.weight1} {w.boneIndex2}:{w.weight2} {w.boneIndex3}:{w.weight3}",raw=v.ToString("F7"),manualLocal=r.transform.InverseTransformPoint(world).ToString("F7"),bakedFalse=a.vertices[0].ToString("F7"),bakedTrue=b.vertices[0].ToString("F7"),scale=r.transform.lossyScale.ToString("F7"),root=r.rootBone.name};
}
finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
