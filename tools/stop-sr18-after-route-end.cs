if(!UnityEditor.EditorApplication.isPlaying)throw new Exception("Play required");
var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
string folder=UnityEditor.SessionState.GetString("SR18.LivePlaytest.20260909.folder","");
UnityEditor.EditorApplication.CallbackFunction stop=null;
stop=()=>{
 if(!UnityEditor.EditorApplication.isPlaying||p==null){UnityEditor.EditorApplication.update-=stop;return;}
 if(p.transform.position.z>354 && Mathf.Abs(p.transform.position.x-248)<5 && p.transform.forward.z>.9f){
  System.IO.File.WriteAllText(folder+"/past-route-end.json","{\"gameTime\":"+Time.time.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"gameStillRunningBeforeTestStop\":"+IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning.ToString().ToLowerInvariant()+",\"position\":\""+p.transform.position.ToString("F3")+"\"}");
  ScreenCapture.CaptureScreenshot(folder+"/past-route-end.png");
  IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=false;
  UnityEditor.EditorApplication.update-=stop;
 }
};
UnityEditor.EditorApplication.update+=stop;
return "Test will stop only after traveling 15+ units past the authored exit, preserving the pre-stop running state.";
