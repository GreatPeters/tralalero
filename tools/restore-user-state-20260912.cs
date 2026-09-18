if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play before restoring user state");
var lines=System.IO.File.ReadAllLines("tmp/backups/skins-progression-2026-09-12/playerprefs.tsv");if(lines.Length!=57)throw new System.InvalidOperationException("Expected original57-key snapshot");
foreach(var line in lines)
{
 var fields=line.Split('\t');string key=fields[0],kind=fields[1];bool exists=bool.Parse(fields[2]);
 if(!exists)UnityEngine.PlayerPrefs.DeleteKey(key);
 else if(kind=="int")UnityEngine.PlayerPrefs.SetInt(key,int.Parse(fields[3],System.Globalization.CultureInfo.InvariantCulture));
 else if(kind=="float")UnityEngine.PlayerPrefs.SetFloat(key,float.Parse(fields[3],System.Globalization.CultureInfo.InvariantCulture));
 else UnityEngine.PlayerPrefs.SetString(key,System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(fields.Length>4?fields[4]:fields[3])));
}
UnityEngine.PlayerPrefs.Save();CosmeticService.ReloadCatalog();
UnityEditor.SessionState.SetBool("NoryangjinMapTool.TestPower9999",false);UnityEditor.SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false);UnityEditor.SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",1);UnityEngine.Time.timeScale=1;
int checkedKeys=0;foreach(var line in lines)
{
 var fields=line.Split('\t');string key=fields[0],kind=fields[1];bool exists=bool.Parse(fields[2]);if(UnityEngine.PlayerPrefs.HasKey(key)!=exists)throw new System.InvalidOperationException("Presence mismatch "+key);
 if(exists)
 {
  if(kind=="int"&&UnityEngine.PlayerPrefs.GetInt(key)!=int.Parse(fields[3],System.Globalization.CultureInfo.InvariantCulture))throw new System.InvalidOperationException(key);
  if(kind=="float"&&UnityEngine.PlayerPrefs.GetFloat(key)!=float.Parse(fields[3],System.Globalization.CultureInfo.InvariantCulture))throw new System.InvalidOperationException(key);
  if(kind=="string"&&UnityEngine.PlayerPrefs.GetString(key)!=System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(fields.Length>4?fields[4]:fields[3])))throw new System.InvalidOperationException(key);
 }
 checkedKeys++;
}
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Preserve scene changes before switching");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
string report="{\"restoredKeys\":"+checkedKeys+",\"coin\":"+UnityEngine.PlayerPrefs.GetInt("coin")+",\"jewel\":"+UnityEngine.PlayerPrefs.GetInt("jewel")+",\"testSpeed\":1,\"power9999\":false,\"fastLateral\":false}";
System.IO.File.WriteAllText("map-concepts/skins-reststop-2026-09-12/user-state-restored.json",report);return report;
