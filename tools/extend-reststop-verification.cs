if(!UnityEditor.EditorApplication.isPlaying||!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("QA Play Mode required");
var type=typeof(Sr18ProgressionPlaytest);var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var config=type.GetField("config",flags).GetValue(null);
config.GetType().GetField("maxRuns").SetValue(config,40);
type.GetMethod("SaveConfig",flags).Invoke(null,null);
UnityEditor.SessionState.SetBool("SR18.Progression.Active",true);UnityEditor.EditorApplication.isPaused=false;
return Sr18ProgressionPlaytest.Status();
