if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
const string folder="tmp/backups/shop-fidelity-2026-09-17/";
var result=ChapterPlaytestPreferences.RestoreAt(folder+"playerprefs.tsv");
foreach(string file in new[]{"extra-prefs.tsv","rank-prefs.tsv"})foreach(string line in System.IO.File.ReadAllLines(folder+file)){
 var p=line.Split('\t');if(!bool.Parse(p[2]))PlayerPrefs.DeleteKey(p[0]);else PlayerPrefs.SetInt(p[0],int.Parse(p[3]));
}
PlayerPrefs.Save();
var checks=new System.Collections.Generic.List<string>();
foreach(string file in new[]{"playerprefs.tsv","extra-prefs.tsv","rank-prefs.tsv"})foreach(string line in System.IO.File.ReadAllLines(folder+file)){
 var p=line.Split('\t');bool exists=bool.Parse(p[2]);if(PlayerPrefs.HasKey(p[0])!=exists)throw new Exception("Existence mismatch "+p[0]);
 if(!exists)continue;
 string value=p[1]=="int"?PlayerPrefs.GetInt(p[0]).ToString(System.Globalization.CultureInfo.InvariantCulture):p[1]=="float"?PlayerPrefs.GetFloat(p[0]).ToString("R",System.Globalization.CultureInfo.InvariantCulture):Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(p[0])));
 if(value!=p[3])throw new Exception("Value mismatch "+p[0]);checks.Add("PASS "+p[0]);
}
System.IO.File.WriteAllLines("map-concepts/shop-fidelity-2026-09-17/verification/restored-prefs.txt",checks);return result;
