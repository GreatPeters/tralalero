if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
string folder="tmp/backups/coastal-enamel-ui-2026-09-16";
ChapterPlaytestPreferences.RestoreAt(folder+"/playerprefs.tsv");
foreach(string file in new[]{"chapter-prefs.tsv","additional-prefs.tsv"})foreach(string line in System.IO.File.ReadAllLines(folder+"/"+file)){
 var p=line.Split('\t');if(bool.Parse(p[1]))PlayerPrefs.SetInt(p[0],int.Parse(p[2]));else PlayerPrefs.DeleteKey(p[0]);
}
foreach(string line in System.IO.File.ReadAllLines(folder+"/settings-before-interaction.tsv")){
 var p=line.Split('\t');if(!bool.Parse(p[2]))PlayerPrefs.DeleteKey(p[0]);else if(p[1]=="int")PlayerPrefs.SetInt(p[0],int.Parse(p[3]));else PlayerPrefs.SetFloat(p[0],float.Parse(p[3],System.Globalization.CultureInfo.InvariantCulture));
}
PlayerPrefs.Save();var mismatches=new System.Collections.Generic.List<string>();int checkedKeys=0;
foreach(string file in new[]{"playerprefs.tsv","settings-before-interaction.tsv"})foreach(string line in System.IO.File.ReadAllLines(folder+"/"+file)){
 var p=line.Split('\t');checkedKeys++;bool exists=bool.Parse(p[2]);if(PlayerPrefs.HasKey(p[0])!=exists){mismatches.Add(p[0]+" presence");continue;}if(!exists)continue;
 bool match=p[1]=="int"?PlayerPrefs.GetInt(p[0])==int.Parse(p[3]):p[1]=="float"?PlayerPrefs.GetFloat(p[0])==float.Parse(p[3],System.Globalization.CultureInfo.InvariantCulture):PlayerPrefs.GetString(p[0])==System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p[3]));
 if(!match)mismatches.Add(p[0]);
}
foreach(string file in new[]{"chapter-prefs.tsv","additional-prefs.tsv"})foreach(string line in System.IO.File.ReadAllLines(folder+"/"+file)){
 var p=line.Split('\t');checkedKeys++;if(PlayerPrefs.HasKey(p[0])!=bool.Parse(p[1])||bool.Parse(p[1])&&PlayerPrefs.GetInt(p[0])!=int.Parse(p[2]))mismatches.Add(p[0]);
}
if(mismatches.Count>0)throw new Exception(string.Join(",",mismatches));
System.IO.File.WriteAllText("map-concepts/coastal-enamel-ui-2026-09-16/preference-restoration.txt",checkedKeys+" tracked keys verified. Coin="+PlayerPrefs.GetInt("coin")+", jewel="+PlayerPrefs.GetInt("jewel")+". Settings snapshot precedes interactive toggles.");
return new{checkedKeys,mismatches=mismatches.Count,coins=PlayerPrefs.GetInt("coin"),gems=PlayerPrefs.GetInt("jewel")};
