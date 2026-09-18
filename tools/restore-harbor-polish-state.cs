if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode before restoring preferences");
var backup=SessionState.GetString("Harbor.PolishBackup","");if(string.IsNullOrEmpty(backup))throw new Exception("Baseline path missing");
var result=ChapterPlaytestPreferences.RestoreAt(backup+"/playerprefs.tsv");
foreach(var line in System.IO.File.ReadAllLines(backup+"/extra-prefs.tsv"))
{
    var parts=line.Split('\t');if(!bool.Parse(parts[2]))PlayerPrefs.DeleteKey(parts[0]);
    else if(parts[1]=="int")PlayerPrefs.SetInt(parts[0],int.Parse(parts[3]));
    else PlayerPrefs.SetFloat(parts[0],float.Parse(parts[3],System.Globalization.CultureInfo.InvariantCulture));
}
PlayerPrefs.Save();
var mismatches=new System.Collections.Generic.List<string>();int entries=0;
foreach(var file in new[]{"playerprefs.tsv","extra-prefs.tsv"})foreach(var line in System.IO.File.ReadAllLines(backup+"/"+file))
{
    var parts=line.Split('\t');bool exists=bool.Parse(parts[2]);entries++;
    if(PlayerPrefs.HasKey(parts[0])!=exists){mismatches.Add(parts[0]+" presence");continue;}
    if(!exists)continue;
    bool same=parts[1]=="int"?PlayerPrefs.GetInt(parts[0])==int.Parse(parts[3]):parts[1]=="float"?PlayerPrefs.GetFloat(parts[0])==float.Parse(parts[3],System.Globalization.CultureInfo.InvariantCulture):PlayerPrefs.GetString(parts[0])==System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[3]));
    if(!same)mismatches.Add(parts[0]+" value");
}
if(mismatches.Count>0)throw new Exception(string.Join(", ",mismatches));
IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=false;IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;IndianOceanAssets.ShooterSurvival.CanvasScript.isGameOver=false;
EditorApplication.isPaused=false;SessionState.EraseString("Harbor.PolishCapturePhase");SessionState.EraseInt("Harbor.ReloadOldID");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
System.IO.File.WriteAllText("map-concepts/harbor-polish-2026-09-15/restoration.txt",$"Restored {entries} entries; mismatches 0\nCoin {PlayerPrefs.GetInt("coin")}; jewel {PlayerPrefs.GetInt("jewel")}\nScene {scene.path}; dirty {scene.isDirty}; Play Mode false\n");
return new{entries,mismatches=mismatches.Count,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel"),scene=scene.path,scene.isDirty};
