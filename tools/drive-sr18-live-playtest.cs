if(!UnityEditor.EditorApplication.isPlaying)throw new InvalidOperationException("Play required");
var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
if(IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)throw new Exception("Start screen required");
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var move=p.GetType().GetMethod("PlayerMove",flags);var origin=p.GetType().GetField("routeLaneOrigin",flags);var right=p.GetType().GetField("routeRight",flags);
UnityEditor.SessionState.SetFloat("SR18.LivePlaytest.Lane",0);
UnityEditor.EditorApplication.CallbackFunction steer=null;
steer=()=>{
 if(!UnityEditor.EditorApplication.isPlaying||p==null){UnityEditor.EditorApplication.update-=steer;return;}
 if(!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning||p.currentHealth<=0)return;
 float lane=Vector3.Dot(p.transform.position-(Vector3)origin.GetValue(p),(Vector3)right.GetValue(p));
 float error=UnityEditor.SessionState.GetFloat("SR18.LivePlaytest.Lane",0)-lane;
 if(Mathf.Abs(error)>.04f)move.Invoke(p,new object[]{Mathf.Sign(error)*Mathf.Min(15,Mathf.Abs(error)*12)});
};
UnityEditor.EditorApplication.update+=steer;
UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>().PlayerPressedStartButton();
return new{started=true,health=p.currentHealth,attack=p.currentDamage,speed=p.ForwardMoveSpeed,controls="real PlayerMove only; no transform teleport or stat override"};
