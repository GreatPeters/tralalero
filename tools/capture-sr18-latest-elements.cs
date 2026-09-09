if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity")throw new System.InvalidOperationException("SR18 required");
string folder="tmp/image-previews/sr18-latest-elements-2026-09-07/"+System.DateTime.Now.ToString("HHmmss");System.IO.Directory.CreateDirectory(folder);
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var go=new GameObject("SR18 actual capture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,preview);
var cam=go.AddComponent<Camera>();var extra=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();extra.renderShadows=false;extra.renderPostProcessing=false;
var rt=new RenderTexture(1800,1800,24);var tex=new Texture2D(1800,1800,TextureFormat.RGB24,false);var previous=RenderTexture.active;
try{
 cam.scene=scene;cam.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);cam.cullingMask=~(1<<5);
 cam.orthographic=true;cam.aspect=1;cam.nearClipPlane=.1f;cam.farClipPlane=1600;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.2f,.43f,.56f);cam.targetTexture=rt;
 var shots=new[]{(Name:"01-full-map",Center:new Vector3(177,0,-8),Size:390f),(Name:"02-market-encounters",Center:new Vector3(107,0,-7),Size:75f),(Name:"03-bridge-bonuses",Center:new Vector3(60,0,-77.7511f),Size:100f)};
 foreach(var shot in shots){cam.orthographicSize=shot.Size;cam.transform.SetPositionAndRotation(shot.Center+Vector3.up*700,Quaternion.Euler(90,0,0));cam.Render();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1800,1800),0,0);tex.Apply();System.IO.File.WriteAllBytes(folder+"/"+shot.Name+".png",tex.EncodeToPNG());}
 return folder;
}finally{cam.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
