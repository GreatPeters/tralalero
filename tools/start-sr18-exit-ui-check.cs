if(!UnityEditor.EditorApplication.isPlaying)throw new Exception("Play required");
if(!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>().PlayerPressedStartButton();
var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
p.transform.SetPositionAndRotation(new Vector3(247.99992f,.16f,329),Quaternion.identity);
p.GetType().GetMethod("RebaseRouteFrame",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(Vector3)},null).Invoke(p,new object[]{p.transform.position});
return new{focusedExitTest=true,position=p.transform.position.ToString(),hp=p.currentHealth};
