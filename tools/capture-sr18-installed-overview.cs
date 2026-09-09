// Render the actual authored scene, with no clone layout, extra objects, or scale overrides.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
bool dirty=scene.isDirty;
var view=UnityEditor.SceneView.lastActiveSceneView;
if(view!=null){view.orthographic=true;view.LookAtDirect(new Vector3(177,0,-8),Quaternion.Euler(90,0,0),390);view.Repaint();}
string path="tmp/image-previews/sr18-encounters-applied-2026-09-06/03-installed-map-overview.png";
if(System.IO.File.Exists(path))throw new System.InvalidOperationException("Refusing overwrite");
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var go=new GameObject("SR18 capture camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,preview);
var cam=go.AddComponent<Camera>();var rt=new RenderTexture(2400,2400,24);var tex=new Texture2D(2400,2400,TextureFormat.RGB24,false);var previous=RenderTexture.active;
try{
 cam.scene=scene;cam.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);cam.cullingMask=~(1<<5);cam.orthographic=true;cam.orthographicSize=390;cam.aspect=1;cam.nearClipPlane=.1f;cam.farClipPlane=1600;cam.transform.SetPositionAndRotation(new Vector3(177,700,-8),Quaternion.Euler(90,0,0));cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.2f,.43f,.56f);
 var extra=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();extra.renderShadows=false;extra.renderPostProcessing=false;
 cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,2400,2400),0,0);tex.Apply();System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());
 return new{path,actualScene=true,scaleOverrides=false,uiHiddenInCaptureOnly=true,initialDirty=dirty};
}finally{cam.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
