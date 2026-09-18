var catalog=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var material=catalog.Find("skin_original").material;float previous=material.GetFloat("_BumpScale");
try
{
 foreach(float strength in new[]{0f,.2f})
 {
  material.SetFloat("_BumpScale",strength);var root=new UnityEngine.GameObject("Skin shading probe");var preview=root.AddComponent<CosmeticPreview>();preview.catalog=catalog;
  try{preview.Show("skin_original","shoes_original","hat_none");CosmeticPresentationBuilder.Save(preview.Texture,"map-concepts/skins-reststop-2026-09-12/normal-strength-"+strength.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png");}
  finally{preview.Dispose();UnityEngine.Object.DestroyImmediate(root);}
 }
}
finally{material.SetFloat("_BumpScale",previous);}
return "Shading probes saved; material restored";
