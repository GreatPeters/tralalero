const string folder="map-concepts/skins-reststop-2026-09-12/fitting-v10";
System.IO.Directory.CreateDirectory(folder);
var catalog=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
var go=new UnityEngine.GameObject("Fitting capture");var preview=go.AddComponent<CosmeticPreview>();preview.catalog=catalog;
try
{
    foreach(var key in new[]{"shoes_gold","shoes_mint","shoes_relic","shoes_ruby","shoes_salvage","shoes_spring"})
    {
        preview.Show("skin_original",key,"hat_none");preview.ResetView();CosmeticPresentationBuilder.Save(preview.Texture,folder+"/"+key+"-front.png");
        preview.Rotate(125);CosmeticPresentationBuilder.Save(preview.Texture,folder+"/"+key+"-rear.png");
    }
}
finally{preview.Dispose();UnityEngine.Object.DestroyImmediate(go);}
return folder;
