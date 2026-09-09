if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before restoring test-only rewards");
const string key="SR18.LivePlaytest.20260909";
if(!UnityEditor.SessionState.GetBool(key+".active",false))throw new InvalidOperationException("No test snapshot");
foreach(var name in new[]{"coin","jewel"}) {
 if(UnityEditor.SessionState.GetBool(key+"."+name+".exists",false))PlayerPrefs.SetInt(name,UnityEditor.SessionState.GetInt(key+"."+name,0));
 else PlayerPrefs.DeleteKey(name);
}
PlayerPrefs.Save();
UnityEditor.SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",UnityEditor.SessionState.GetFloat(key+".speed",1));
UnityEditor.SessionState.SetBool(key+".active",false);
return new{restored=true,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel"),playing=UnityEditor.EditorApplication.isPlaying,sceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty};
