if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
const string folder="tmp/image-previews/skins-reststop-2026-09-12/reststop-scene";System.IO.Directory.CreateDirectory(folder);
var cameraObj=new UnityEngine.GameObject("Rest-stop review camera"){hideFlags=UnityEngine.HideFlags.HideAndDontSave};var camera=cameraObj.AddComponent<UnityEngine.Camera>();camera.CopyFrom(UnityEngine.Camera.main);camera.enabled=false;camera.cullingMask&=~(1<<31);camera.nearClipPlane=.1f;camera.farClipPlane=700;camera.aspect=16f/9f;
var target=new UnityEngine.RenderTexture(1600,900,24,UnityEngine.RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;bool fog=UnityEngine.RenderSettings.fog;
try{
 UnityEngine.RenderSettings.fog=false;camera.orthographic=true;camera.orthographicSize=73;camera.transform.position=new UnityEngine.Vector3(-60,108,300);camera.transform.LookAt(new UnityEngine.Vector3(-20,5,167));camera.Render();CosmeticPresentationBuilder.Save(target,folder+"/overview.png");
 camera.orthographic=false;camera.fieldOfView=57;camera.transform.position=new UnityEngine.Vector3(-39,14,211);camera.transform.LookAt(new UnityEngine.Vector3(-20,7,173));camera.Render();CosmeticPresentationBuilder.Save(target,folder+"/forecourt.png");
 camera.transform.position=new UnityEngine.Vector3(-108,12,219);camera.transform.LookAt(new UnityEngine.Vector3(-75,7,193));camera.Render();CosmeticPresentationBuilder.Save(target,folder+"/entrance.png");
}finally{UnityEngine.RenderSettings.fog=fog;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraObj);target.Release();UnityEngine.Object.DestroyImmediate(target);}
return folder;
