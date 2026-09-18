if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
const string path="Assets/ShooterSurvival/Resources/Cosmetics/Catalog.asset";
var backup=SessionState.GetString("Harbor.PolishBackup","");if(string.IsNullOrEmpty(backup))throw new Exception("Baseline missing");
if(!System.IO.File.Exists(backup+"/cosmetics-before.asset"))System.IO.File.Copy(path,backup+"/cosmetics-before.asset");
var catalog=AssetDatabase.LoadAssetAtPath<CosmeticVisualCatalog>(path);
void Fit(string key,float scale,Vector3 offset,Vector3 rotation)
{var item=catalog.Find(key);item.hatScale=scale;item.hatOffset=offset;item.hatEuler=rotation;}
Fit("hat_cap",.93f,new Vector3(0,.005f,.015f),Vector3.zero);
Fit("hat_bucket",.92f,new Vector3(0,.035f,0),Vector3.zero);
Fit("hat_tophat",1f,new Vector3(0,.035f,0),new Vector3(0,0,-4));
Fit("hat_goggles",.94f,new Vector3(0,.15f,-.12f),new Vector3(-12,0,0));
Fit("hat_diver",.82f,new Vector3(0,-.22f,-.07f),new Vector3(-8,0,0));
Fit("hat_pirate",1.02f,new Vector3(0,.16f,.02f),new Vector3(0,0,-5));
Fit("hat_relic",1.02f,new Vector3(0,.07f,.08f),new Vector3(0,0,3));
var folder="C:/Users/ljh/tralalero Shooter/tmp/image-previews/harbor-polish-2026-09-15/hats-after";System.IO.Directory.CreateDirectory(folder);
var go=new GameObject("Hat fit review");var preview=go.AddComponent<CosmeticPreview>();preview.catalog=catalog;
try
{
    foreach(var key in new[]{"hat_cap","hat_bucket","hat_goggles","hat_diver","hat_pirate","hat_relic","hat_tophat"})
    {preview.Show("skin_original","shoes_original",key);preview.ResetView();CosmeticPresentationBuilder.Save(preview.Texture,folder+"/"+key+".png");}
}
finally{preview.Dispose();UnityEngine.Object.DestroyImmediate(go);}
EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
return new{folder,adjusted=7};
