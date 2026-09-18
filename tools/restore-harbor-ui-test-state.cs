if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
var result=ChapterPlaytestPreferences.RestoreAt("tmp/backups/harbor-ui-live-2026-09-15/playerprefs-before.tsv");
var chapterLines=System.IO.File.ReadAllLines("tmp/backups/harbor-ui-live-2026-09-15/chapter-prefs-before.tsv");
foreach(var line in chapterLines)
{
    var parts=line.Split('\t');
    if(bool.Parse(parts[1]))PlayerPrefs.SetInt(parts[0],int.Parse(parts[2]));else PlayerPrefs.DeleteKey(parts[0]);
}
PlayerPrefs.Save();
var mismatches=new System.Collections.Generic.List<string>();
foreach(var line in System.IO.File.ReadAllLines("tmp/backups/harbor-ui-live-2026-09-15/playerprefs-before.tsv"))
{
    var parts=line.Split('\t');bool existed=bool.Parse(parts[2]);
    if(PlayerPrefs.HasKey(parts[0])!=existed){mismatches.Add(parts[0]+" presence");continue;}
    if(!existed)continue;
    if(parts[1]=="int"&&PlayerPrefs.GetInt(parts[0])!=int.Parse(parts[3],System.Globalization.CultureInfo.InvariantCulture))mismatches.Add(parts[0]);
    if(parts[1]=="float"&&PlayerPrefs.GetFloat(parts[0])!=float.Parse(parts[3],System.Globalization.CultureInfo.InvariantCulture))mismatches.Add(parts[0]);
    if(parts[1]=="string"&&PlayerPrefs.GetString(parts[0])!=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[3])))mismatches.Add(parts[0]);
}
foreach(var line in chapterLines)
{
    var parts=line.Split('\t');if(PlayerPrefs.HasKey(parts[0])!=bool.Parse(parts[1])||bool.Parse(parts[1])&&PlayerPrefs.GetInt(parts[0])!=int.Parse(parts[2]))mismatches.Add(parts[0]);
}
if(mismatches.Count>0)throw new Exception("Preference restoration mismatch: "+string.Join(",",mismatches));
System.IO.File.WriteAllText("map-concepts/harbor-ui-live-2026-09-15/preference-restoration.txt","Verified all 66 baseline entries and 3 chapter keys. Coin="+PlayerPrefs.GetInt("coin")+", Jewel="+PlayerPrefs.GetInt("jewel")+".");
return new{baseline=result,chapterKeys=chapterLines.Length,mismatches=mismatches.Count};
