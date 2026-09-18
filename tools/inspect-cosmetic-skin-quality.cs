var catalog=UnityEngine.Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
return new{global=UnityEngine.QualitySettings.skinWeights.ToString(),source=string.Join(",",catalog.previewModel.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Select(r=>r.name+":"+r.quality)),mesh=catalog.Find("shoes_gold").fittedShoeMesh.vertexCount};
