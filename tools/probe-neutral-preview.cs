var cat=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var go=new UnityEngine.GameObject("Neutral pose probe");var preview=go.AddComponent<CosmeticPreview>();preview.catalog=cat;
try
{
 preview.Show("skin_original","shoes_original","hat_none");
 var r=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.SkinnedMeshRenderer>().Single(x=>x.name=="char1"&&x.transform.root.name=="__CosmeticPreview");
 var model=r.transform.root.GetChild(0);float ratio=(r.transform.worldToLocalMatrix*r.bones[0].localToWorldMatrix*r.sharedMesh.bindposes[0]).lossyScale.x;model.localScale*=ratio;
 var matrices=r.sharedMesh.bindposes.Select(b=>r.transform.localToWorldMatrix*b.inverse).ToArray();
 for(int i=0;i<r.bones.Length;i++){var b=r.bones[i];var m=matrices[i];b.SetPositionAndRotation(m.GetColumn(3),m.rotation);var s=b.parent.lossyScale;var target=m.lossyScale;b.localScale=new UnityEngine.Vector3(target.x/s.x,target.y/s.y,target.z/s.z);}
 var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(CosmeticPreview).GetMethod("ClearPose",flags).Invoke(preview,null);typeof(CosmeticPreview).GetMethod("BakePreviewPose",flags).Invoke(preview,null);
 preview.Rotate(0);CosmeticPresentationBuilder.Save(preview.Texture,"map-concepts/skins-reststop-2026-09-12/neutral-preview.png");return ratio;
}
finally{preview.Dispose();UnityEngine.Object.DestroyImmediate(go);}
