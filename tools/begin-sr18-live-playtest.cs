if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required before snapshot");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="Noryangjin_MapTool_Mode_SR18"||scene.isDirty)throw new InvalidOperationException("Clean SR18 required");
const string key="SR18.LivePlaytest.20260909";
if(UnityEditor.SessionState.GetBool(key+".active",false))throw new InvalidOperationException("An unrestored test snapshot exists");
foreach(var name in new[]{"coin","jewel"}){
 UnityEditor.SessionState.SetBool(key+"."+name+".exists",PlayerPrefs.HasKey(name));
 UnityEditor.SessionState.SetInt(key+"."+name,PlayerPrefs.GetInt(name));
}
UnityEditor.SessionState.SetFloat(key+".speed",UnityEditor.SessionState.GetFloat("NoryangjinMapTool.TestTimeScale",1));
UnityEditor.SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",1);
UnityEditor.SessionState.SetBool(key+".active",true);
string folder="tmp/image-previews/sr18-live-playtest-"+DateTime.Now.ToString("yyyy-MM-dd")+"/"+DateTime.Now.ToString("HHmmss");System.IO.Directory.CreateDirectory(folder);
UnityEditor.SessionState.SetString(key+".folder",folder);
return new{folder,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel"),testTimeScale=1};
