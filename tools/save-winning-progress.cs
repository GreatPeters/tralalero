var values=new System.Collections.Generic.Dictionary<string,string>();
for(int i=1;i<=9;i++)values["upgrade_lv_"+i]=UnityEngine.PlayerPrefs.GetInt("upgrade_lv_"+i).ToString(System.Globalization.CultureInfo.InvariantCulture);
foreach(string type in System.Enum.GetNames(typeof(UpgradeStatManager.UpgradeType)))
{
 values["upgrade_stat_"+type]=UnityEngine.PlayerPrefs.GetFloat("upgrade_stat_"+type).ToString("R",System.Globalization.CultureInfo.InvariantCulture);
 values["upgrade_stat_type_"+type]=UnityEngine.PlayerPrefs.GetInt("upgrade_stat_type_"+type).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
values["coin"]=UnityEngine.PlayerPrefs.GetInt("coin").ToString();values["jewel"]=UnityEngine.PlayerPrefs.GetInt("jewel").ToString();
var serializer=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("map-concepts/skins-reststop-2026-09-12/nory-winning-progress.json",(string)serializer.Invoke(null,new object[]{values}));return values.Count;
