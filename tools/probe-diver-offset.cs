var catalog=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var entry=catalog.Find("hat_diver");var field=entry.GetType().GetField("hatOffset");var old=(UnityEngine.Vector3)field.GetValue(entry);
var go=new UnityEngine.GameObject("Diver offset probe");var preview=go.AddComponent<CosmeticPreview>();preview.catalog=catalog;
try{field.SetValue(entry,new UnityEngine.Vector3(0,-1.05f,.15f));preview.Show("skin_original","shoes_mint","hat_diver");CosmeticPresentationBuilder.Save(preview.Texture,"map-concepts/skins-reststop-2026-09-12/diver-offset-probe.png");}
finally{field.SetValue(entry,old);preview.Dispose();UnityEngine.Object.DestroyImmediate(go);}
return "Diver trial saved";
