if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
const string folder="tmp/backups/harbor-faithful-art-2026-09-17";
ChapterPlaytestPreferences.RestoreAt(folder+"/playerprefs.tsv");
foreach(string line in System.IO.File.ReadAllLines(folder+"/extra-prefs.tsv")){var p=line.Split('\t');if(bool.Parse(p[2]))PlayerPrefs.SetInt(p[0],int.Parse(p[3]));else PlayerPrefs.DeleteKey(p[0]);}
PlayerPrefs.Save();int records=0;
foreach(string file in new[]{"playerprefs.tsv","extra-prefs.tsv"})foreach(string line in System.IO.File.ReadAllLines(folder+"/"+file)){
 var p=line.Split('\t');records++;bool exists=bool.Parse(p[2]);if(PlayerPrefs.HasKey(p[0])!=exists)throw new Exception("Presence changed: "+p[0]);if(!exists)continue;
 bool matches=p[1]=="int"?PlayerPrefs.GetInt(p[0])==int.Parse(p[3]):p[1]=="float"?PlayerPrefs.GetFloat(p[0])==float.Parse(p[3],System.Globalization.CultureInfo.InvariantCulture):PlayerPrefs.GetString(p[0])==System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p[3]));if(!matches)throw new Exception("Value changed: "+p[0]);
}
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");EditorApplication.isPaused=false;
System.IO.File.WriteAllText("map-concepts/harbor-faithful-art-2026-09-17/restoration.txt",records+" preference records restored exactly from today's fresh snapshot. Coin="+PlayerPrefs.GetInt("coin")+". SR18 open in Edit Mode; Game View size unchanged.");
return new{records,coin=PlayerPrefs.GetInt("coin")};

