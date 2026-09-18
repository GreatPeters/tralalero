var paths = new[] {
    "Assets/ShooterSurvival/Models/Chapters/Props/049/Model.fbx",
    "Assets/ShooterSurvival/Prefabs/Highway/Enemies/ConeMechanic.prefab",
    "Assets/ShooterSurvival/Prefabs/RestStop/Enemies/CoffeeVendor.prefab"
};
var result = new System.Collections.Generic.List<object>();
foreach (var path in paths)
{
    var importer = UnityEditor.AssetImporter.GetAtPath(path);
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    var root = UnityEngine.Object.Instantiate(asset);
    try
    {
        var bounds = HighwayAssetImporter.BoundsOf(root);
        result.Add(new { path, bounds = bounds.ToString(), rootScale = root.transform.localScale.ToString(),
            settings = new[]{"globalScale","fileScale","useFileScale","bakeAxisConversion"}.ToDictionary(k=>k,k=>importer.GetType().GetProperty(k)?.GetValue(importer)),
            transforms = root.GetComponentsInChildren<UnityEngine.Transform>().Take(8).Select(t=>new {name=t.name,position=t.position.ToString(),scale=t.lossyScale.ToString()}).ToArray() });
    }
    finally { UnityEngine.Object.DestroyImmediate(root); }
}
return result;
