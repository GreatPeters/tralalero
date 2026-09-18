if(UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("Original project required");
var mismatches=new System.Collections.Generic.List<string>();
var lines=System.IO.File.ReadAllLines("tmp/backups/chapters-polish-2026-09-13/original-playerprefs.tsv");
foreach(var line in lines){
 var c=line.Split('\t');string key=c[0],kind=c[1];bool present=bool.Parse(c[2]);
 string current=kind=="string"?System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(UnityEngine.PlayerPrefs.GetString(key))):kind=="int"?UnityEngine.PlayerPrefs.GetInt(key).ToString(System.Globalization.CultureInfo.InvariantCulture):UnityEngine.PlayerPrefs.GetFloat(key).ToString("R",System.Globalization.CultureInfo.InvariantCulture);
 if(present!=UnityEngine.PlayerPrefs.HasKey(key)||current!=c[3])mismatches.Add(key);
}
var newRewardKeys=Enumerable.Range(1,5).Where(chapter=>UnityEngine.PlayerPrefs.HasKey("chapter_rewarded_"+chapter)).ToArray();
var report=new{keys=lines.Length,unchanged=mismatches.Count==0,mismatches,newRewardKeysAbsent=newRewardKeys.Length==0,coins=UnityEngine.PlayerPrefs.GetInt("coin"),jewels=UnityEngine.PlayerPrefs.GetInt("jewel"),scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name};
var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("map-concepts/chapters-polish-2026-09-12/original-prefs-verification.json",(string)serialize.Invoke(null,new object[]{report}));
return report;
