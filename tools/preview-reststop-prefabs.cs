if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
const string folder="tmp/image-previews/skins-reststop-2026-09-12/reststop-assets";System.IO.Directory.CreateDirectory(folder);var stage=new UnityEngine.GameObject("Rest-stop asset preview"){hideFlags=UnityEngine.HideFlags.HideAndDontSave};stage.transform.position=new UnityEngine.Vector3(15000,15000,15000);var target=new UnityEngine.RenderTexture(1024,768,24,UnityEngine.RenderTextureFormat.ARGB32);target.Create();
try
{
 var cameraObj=new UnityEngine.GameObject("Asset camera");cameraObj.transform.SetParent(stage.transform,false);var camera=cameraObj.AddComponent<UnityEngine.Camera>();camera.enabled=false;camera.orthographic=true;camera.aspect=4f/3f;camera.cullingMask=1<<31;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=UnityEngine.Color.clear;camera.nearClipPlane=.01f;camera.farClipPlane=200;camera.targetTexture=target;
 var lightObj=new UnityEngine.GameObject("Asset key light");lightObj.transform.SetParent(stage.transform,false);lightObj.transform.rotation=UnityEngine.Quaternion.Euler(45,-35,0);var light=lightObj.AddComponent<UnityEngine.Light>();light.type=UnityEngine.LightType.Directional;light.intensity=.8f;light.cullingMask=1<<31;light.shadows=UnityEngine.LightShadows.None;
 foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{RestStopAssetImporter.Prefabs}))
 {
  string path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);var model=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path),stage.transform,false);foreach(var t in model.GetComponentsInChildren<UnityEngine.Transform>(true))t.gameObject.layer=31;
  var bounds=HighwayAssetImporter.BoundsOf(model);float size=UnityEngine.Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);camera.orthographicSize=size*.53f;camera.transform.position=bounds.center+new UnityEngine.Vector3(-1.4f,1.05f,1.8f)*size;camera.transform.LookAt(bounds.center);camera.Render();CosmeticPresentationBuilder.Save(target,folder+"/"+System.IO.Path.GetFileNameWithoutExtension(path)+".png");UnityEngine.Object.DestroyImmediate(model);
 }
}
finally{UnityEngine.Object.DestroyImmediate(stage);target.Release();UnityEngine.Object.DestroyImmediate(target);}
return folder;
