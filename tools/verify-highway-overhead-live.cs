if(!UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isPaused)throw new System.InvalidOperationException("Unpaused Play Mode required");
OpeningStoryUI.Instance?.Skip();var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();canvas.PlayerPressedStartButton();player.enabled=false;
player.transform.SetPositionAndRotation(new UnityEngine.Vector3(45.26f,5.12f,218.30f),UnityEngine.Quaternion.Euler(0,90,0));var body=player.GetComponent<UnityEngine.Rigidbody>();if(body!=null)body.isKinematic=true;
var component=UnityEngine.Camera.main.GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinCameraOcclusion>();component.enabled=false;
const string folder="tmp/image-previews/skins-reststop-2026-09-12/overhead-visibility-v2";System.IO.Directory.CreateDirectory(folder);string before=folder+"/before.png",after=folder+"/after.png";if(System.IO.File.Exists(before)||System.IO.File.Exists(after))throw new System.InvalidOperationException("Preserve earlier visibility proof");
var original=player.transform.position;int phase=0;double next=UnityEditor.EditorApplication.timeSinceStartup+.4;UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 if(UnityEditor.EditorApplication.timeSinceStartup<next)return;
 try{
  if(phase==0){UnityEngine.ScreenCapture.CaptureScreenshot(before);phase++;return;}
  if(phase==1){if(!System.IO.File.Exists(before))return;component.enabled=true;component.RefreshVisibility();next=UnityEditor.EditorApplication.timeSinceStartup+.3;phase++;return;}
  if(phase==2){if(component.HiddenCount<=0)throw new System.InvalidOperationException("Overhead sign still visible");UnityEngine.ScreenCapture.CaptureScreenshot(after);phase++;return;}
  if(!System.IO.File.Exists(after))return;
  if(UnityEngine.Vector3.Distance(original,player.transform.position)>.001f)throw new System.InvalidOperationException("Stationary camera probe moved");
  System.IO.File.WriteAllText(folder+"/result.json","{\"hiddenRenderers\":"+component.HiddenCount+",\"playerPosePreserved\":true,\"playerMovementDisabled\":true,\"timeScale\":"+UnityEngine.Time.timeScale.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");UnityEditor.EditorApplication.update-=tick;
 }catch(System.Exception error){UnityEditor.EditorApplication.update-=tick;System.IO.File.WriteAllText(folder+"/error.txt",error.ToString());}
};UnityEditor.EditorApplication.update+=tick;return folder;
