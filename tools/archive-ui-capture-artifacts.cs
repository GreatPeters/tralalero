if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
const string destination="tmp/image-previews/skins-reststop-2026-09-12/early-captures";System.IO.Directory.CreateDirectory(destination);
foreach(string name in new[]{"shop-after-render.png","shop-first.png"}){
 string source="Assets/tmp/image-previews/skins-reststop-2026-09-12/"+name,target=destination+"/"+name;
 if(!System.IO.File.Exists(source))continue;if(System.IO.File.Exists(target))throw new System.InvalidOperationException("Preserve earlier copy "+target);
 System.IO.File.Copy(source,target);if(!UnityEditor.AssetDatabase.DeleteAsset(source))throw new System.InvalidOperationException("Cannot remove generated capture asset "+source);
}
return destination;
