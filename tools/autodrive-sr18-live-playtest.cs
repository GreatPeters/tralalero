if(!UnityEditor.EditorApplication.isPlaying || IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)throw new InvalidOperationException("Play start screen required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var move=p.GetType().GetMethod("PlayerMove",flags);var origin=p.GetType().GetField("routeLaneOrigin",flags);var right=p.GetType().GetField("routeRight",flags);
var bonus=map.Find("Bonuses").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.BonusWallChoicePair>(true);
var hazards=map.Find("Props").Cast<Transform>().Where(t=>t.name.StartsWith("SR18_L_G")).ToArray();
double nextCapture=0; int shots=0;
string folder=UnityEditor.SessionState.GetString("SR18.LivePlaytest.20260909.folder","")+"/guided-"+DateTime.Now.ToString("HHmmss"); System.IO.Directory.CreateDirectory(folder);
UnityEditor.SessionState.SetFloat("SR18.LivePlaytest.Lane",float.NaN); // Invalidate any obsolete manual target.
UnityEditor.EditorApplication.CallbackFunction steer=null;
steer=()=>{
 if(!UnityEditor.EditorApplication.isPlaying||p==null){UnityEditor.EditorApplication.update-=steer;return;}
 if(!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning||p.currentHealth<=0)return;
 if(UnityEditor.EditorApplication.timeSinceStartup>nextCapture){nextCapture=UnityEditor.EditorApplication.timeSinceStartup+5;ScreenCapture.CaptureScreenshot(folder+"/frame-"+(shots++).ToString("D3")+".png");}
 if(p.IsWorldYawTurnActive)return;
 Vector3 forward=Vector3.ProjectOnPlane(p.transform.forward,Vector3.up).normalized;
 Vector3 lateral=(Vector3)right.GetValue(p);
 float desired=0,nearest=26;
 foreach(var pair in bonus){
  if(pair==null||pair.Selected!=null||!pair.Left.gameObject.activeInHierarchy)continue;
  Vector3 center=(pair.Left.transform.position+pair.Right.transform.position)*.5f;Vector3 delta=center-p.transform.position;
  float ahead=Vector3.Dot(delta,forward);
  if(ahead< -1||ahead>nearest||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,lateral))>5||Vector3.Dot(Vector3.ProjectOnPlane(pair.Left.transform.forward,Vector3.up).normalized,forward)>-.9f)continue;
  nearest=ahead;
  bool rightChoice=(pair.Right.RolledStat??"").Contains("tung")||(pair.Right.RolledStat??"").Contains("boom");
  desired=rightChoice?2:-2;
 }
 foreach(var hazard in hazards){
  if(hazard==null||!hazard.gameObject.activeInHierarchy)continue;
  Vector3 delta=hazard.position-p.transform.position;float ahead=Vector3.Dot(delta,forward);
  if(ahead< -5||ahead>nearest||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,lateral))>5||Vector3.Dot(hazard.forward,forward)<.9f)continue;
  nearest=ahead;float offset=Vector3.Dot(hazard.position-(Vector3)origin.GetValue(p),lateral);desired=offset>=0?-2:2;
 }
 float lane=Vector3.Dot(p.transform.position-(Vector3)origin.GetValue(p),lateral);float error=desired-lane;
 if(Mathf.Abs(error)>.04f)move.Invoke(p,new object[]{Mathf.Sign(error)*Mathf.Min(15,Mathf.Abs(error)*12)});
};
UnityEditor.EditorApplication.update+=steer;
UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>().PlayerPressedStartButton();
return new{started=true,folder,health=p.currentHealth,attack=p.currentDamage,statOverrides=false,collidersDisabled=false};
