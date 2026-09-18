if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
string folder="tmp/backups/harbor-opening-refinement-2026-09-16";
ChapterPlaytestPreferences.RestoreAt(folder+"/playerprefs.tsv");
foreach(string line in System.IO.File.ReadAllLines(folder+"/extra-prefs.tsv")){
 var p=line.Split('\t');if(!bool.Parse(p[2]))PlayerPrefs.DeleteKey(p[0]);else if(p[1]=="int")PlayerPrefs.SetInt(p[0],int.Parse(p[3]));else PlayerPrefs.SetFloat(p[0],float.Parse(p[3],System.Globalization.CultureInfo.InvariantCulture));
}
PlayerPrefs.Save();int checkedKeys=0;var mismatch=new System.Collections.Generic.List<string>();
foreach(string file in new[]{"playerprefs.tsv","extra-prefs.tsv"})foreach(string line in System.IO.File.ReadAllLines(folder+"/"+file)){
 var p=line.Split('\t');checkedKeys++;bool exists=bool.Parse(p[2]);if(PlayerPrefs.HasKey(p[0])!=exists){mismatch.Add(p[0]+" presence");continue;}if(!exists)continue;
 bool match=p[1]=="int"?PlayerPrefs.GetInt(p[0])==int.Parse(p[3]):p[1]=="float"?PlayerPrefs.GetFloat(p[0])==float.Parse(p[3],System.Globalization.CultureInfo.InvariantCulture):PlayerPrefs.GetString(p[0])==System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p[3]));if(!match)mismatch.Add(p[0]);
}
if(mismatch.Count>0)throw new Exception(string.Join(",",mismatch));
Application.SetStackTraceLogType(LogType.Exception,(StackTraceLogType)SessionState.GetInt("HarborRefinement.OriginalExceptionTrace",(int)StackTraceLogType.ScriptOnly));
EditorApplication.isPaused=false;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
System.IO.File.WriteAllText("map-concepts/harbor-opening-refinement-2026-09-16/preference-restoration.txt",checkedKeys+" tracked records verified; coin="+PlayerPrefs.GetInt("coin")+", jewel="+PlayerPrefs.GetInt("jewel")+". Restored only this task's fresh snapshot. SR18 open in Edit Mode. Existing Game View dimensions were not changed.");
return new{checkedKeys,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel"),mismatches=mismatch.Count};
