const string folder="map-concepts/skins-reststop-2026-09-12/hat-fitting-v3";System.IO.Directory.CreateDirectory(folder);
var catalog=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var go=new UnityEngine.GameObject("Hat fitting capture");var preview=go.AddComponent<CosmeticPreview>();preview.catalog=catalog;
try
{
 foreach(var key in new[]{"hat_cap","hat_bucket","hat_goggles","hat_diver","hat_pirate","hat_relic","hat_tophat"})
 {preview.Show("skin_original","shoes_mint",key);preview.ResetView();CosmeticPresentationBuilder.Save(preview.Texture,folder+"/"+key+".png");}
}
finally{preview.Dispose();UnityEngine.Object.DestroyImmediate(go);}
return folder;
