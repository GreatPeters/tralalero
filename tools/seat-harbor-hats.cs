if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
var catalog=AssetDatabase.LoadAssetAtPath<CosmeticVisualCatalog>("Assets/ShooterSurvival/Resources/Cosmetics/Catalog.asset");
var folder="C:/Users/ljh/tralalero Shooter/tmp/image-previews/harbor-polish-2026-09-15/hats-fitted";System.IO.Directory.CreateDirectory(folder);
var go=new GameObject("Hat surface fitting");var preview=go.AddComponent<CosmeticPreview>();preview.catalog=catalog;
var changes=new System.Collections.Generic.List<object>();
try
{
    foreach(var key in new[]{"hat_cap","hat_bucket","hat_goggles","hat_diver","hat_pirate","hat_relic","hat_tophat"})
    {
        preview.Show("skin_original","shoes_original",key);
        var model=(Transform)typeof(CosmeticPreview).GetField("model",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(preview);
        var mount=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="__CosmeticHat"&&t.gameObject.activeSelf);
        var renderers=mount.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        var pose=model.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="__PreviewPose"&&!f.transform.IsChildOf(mount));
        var collider=pose.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=pose.sharedMesh;Physics.SyncTransforms();
        var ray=new Ray(new Vector3(bounds.center.x,bounds.max.y+5,bounds.center.z),Vector3.down);
        if(!collider.Raycast(ray,out var hit,15))throw new Exception("Hat footprint missed scalp: "+key);
        float shift=hit.point.y+.015f-bounds.min.y;
        catalog.Find(key).hatOffset+=mount.InverseTransformVector(Vector3.up*shift);
        changes.Add(new{key,worldHeightShift=shift,localOffset=catalog.Find(key).hatOffset.ToString("F4")});
        UnityEngine.Object.DestroyImmediate(collider);
        preview.Show("skin_original","shoes_original","hat_none");preview.Show("skin_original","shoes_original",key);
        preview.ResetView();CosmeticPresentationBuilder.Save(preview.Texture,folder+"/"+key+".png");
    }
}
finally{preview.Dispose();UnityEngine.Object.DestroyImmediate(go);}
EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);return changes;
