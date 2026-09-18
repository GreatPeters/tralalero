var cat=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
var reference=UnityEngine.Object.Instantiate(cat.previewModel);var shoe=UnityEngine.Object.Instantiate(cat.Find("shoes_gold").accessory);var previewObject=new UnityEngine.GameObject("Invariant preview");var preview=previewObject.AddComponent<CosmeticPreview>();preview.catalog=cat;
try
{
 var r=reference.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single();var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>("Assets/ShooterSurvival/Resources/Cosmetics/SharkSkinAndShoes.asset");
 var matrices=r.bones.Select((b,i)=>r.transform.worldToLocalMatrix*b.localToWorldMatrix*source.bindposes[i]).ToArray();var raw=source.vertices;var weights=source.boneWeights;
 UnityEngine.Matrix4x4 M(UnityEngine.BoneWeight w){var m=new UnityEngine.Matrix4x4();for(int k=0;k<16;k++)m[k]=matrices[w.boneIndex0][k]*w.weight0+matrices[w.boneIndex1][k]*w.weight1+matrices[w.boneIndex2][k]*w.weight2+matrices[w.boneIndex3][k]*w.weight3;return m;}
 float W(UnityEngine.BoneWeight w,int i)=>(w.boneIndex0==i?w.weight0:0)+(w.boneIndex1==i?w.weight1:0)+(w.boneIndex2==i?w.weight2:0)+(w.boneIndex3==i?w.weight3:0);
 var verts=raw.Select((v,i)=>M(weights[i]).MultiplyPoint3x4(v)).ToArray();var ids=new[]{9,13,22,26};var candidates=source.GetTriangles(1).Distinct().Where(i=>W(weights[i],9)>.1f&&ids.All(j=>W(weights[i],j)<=W(weights[i],9))).ToArray();
 float length=candidates.Max(i=>verts[i].z)-candidates.Min(i=>verts[i].z),bottom=candidates.Min(i=>verts[i].y),top=candidates.Max(i=>verts[i].y);
 var collar=candidates.Where(i=>verts[i].y>UnityEngine.Mathf.Lerp(bottom,top,.85f)).Select(i=>verts[i]).ToArray();var origin=new UnityEngine.Vector3((candidates.Min(i=>verts[i].x)+candidates.Max(i=>verts[i].x))*.5f,bottom,collar.Average(v=>v.z));
 var mf=shoe.GetComponentsInChildren<UnityEngine.MeshFilter>(true).First();var expected=origin+mf.transform.TransformPoint(mf.sharedMesh.vertices[0])*length;var fitted=cat.Find("shoes_gold").fittedShoeMesh;var actual=M(fitted.boneWeights[0]).MultiplyPoint3x4(fitted.vertices[0]);
 preview.Show("skin_original","shoes_gold","hat_none");var live=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.SkinnedMeshRenderer>().Single(x=>x.name=="__CosmeticShoes"&&x.transform.root.name=="__CosmeticPreview");var liveM=live.transform.worldToLocalMatrix*live.bones[9].localToWorldMatrix*source.bindposes[9];
 return new{expected=expected.ToString("F8"),actual=actual.ToString("F8"),error=(expected-actual).magnitude,refMatrix=matrices[9].ToString("F7"),liveMatrix=liveM.ToString("F7"),length,footMinY=bottom,rawFirst=fitted.vertices[0].ToString("F8")};
}
finally{UnityEngine.Object.DestroyImmediate(reference);UnityEngine.Object.DestroyImmediate(shoe);preview.Dispose();UnityEngine.Object.DestroyImmediate(previewObject);}
