var catalog=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var entry=catalog.Find("hat_relic");var field=entry.GetType().GetField("hatOffset");var old=(UnityEngine.Vector3)field.GetValue(entry);var root=new UnityEngine.GameObject("Circlet fit probe");var preview=root.AddComponent<CosmeticPreview>();preview.catalog=catalog;
try{field.SetValue(entry,new UnityEngine.Vector3(0,-.05f,.05f));preview.Show("skin_original","shoes_mint","hat_relic");CosmeticPresentationBuilder.Save(preview.Texture,"map-concepts/skins-reststop-2026-09-12/circlet-fit-probe.png");}
finally{field.SetValue(entry,old);preview.Dispose();UnityEngine.Object.DestroyImmediate(root);}
return "Circlet trial saved";
