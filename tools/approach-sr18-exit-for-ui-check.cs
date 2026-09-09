if(!UnityEditor.EditorApplication.isPlaying||!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)throw new Exception("Running Play required");
var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
p.transform.SetPositionAndRotation(new Vector3(247.99992f,.16f,329),Quaternion.identity);
p.GetType().GetMethod("RebaseRouteFrame",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(Vector3)},null).Invoke(p,new object[]{p.transform.position});
return new{focusedExitTest=true,position=p.transform.position.ToString(),note="Only the final UI/replay check uses a near-exit start; the full continuous traversal is recorded separately."};
