using System;using System.IO;using System.Linq;using System.Globalization;using UnityEngine;using UnityEditor;
public static class RestoreNativePrefsCycle06 {
 public sealed class Row {public string key,kind,value;public bool existed;}
 static Type J=>AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
 static string Json(object o)=>(string)J.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
 static Row[] Read(string p)=>(Row[])J.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{File.ReadAllText(p),typeof(Row[])});
 static Row Current(Row r)=>new Row{key=r.key,kind=r.kind,existed=PlayerPrefs.HasKey(r.key),value=r.kind=="int"?PlayerPrefs.GetInt(r.key).ToString(CultureInfo.InvariantCulture):r.kind=="float"?PlayerPrefs.GetFloat(r.key).ToString("R",CultureInfo.InvariantCulture):Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(r.key)))};
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new Exception("Clean Edit required");
  const string root="outputs/chapter45-detailed-design-2026-10-03/restart-cycle06/";string receipt=root+"native-playerprefs-restoration.json";if(File.Exists(receipt))throw new Exception("Native restoration already performed");
  var original=Read(root+"persistent-catalog-current.json");var observed=Read(root+"catalog-after-final-restoration.json");if(original.Length!=78||observed.Length!=78||observed.Any(r=>Json(r)!=Json(Current(r))))throw new Exception("Native preferences changed after read-only diagnosis");
  var changed=original.Where((r,i)=>Json(r)!=Json(observed[i])).ToArray();var allowed=new[]{"coin","jewel","chapter_rewarded_4","chapter_rewarded_5","chapter_unlocked"};if(changed.Any(r=>!allowed.Contains(r.key)))throw new Exception("Unexpected changed preference outside test rewards");
  foreach(var r in changed){if(!r.existed)PlayerPrefs.DeleteKey(r.key);else if(r.kind=="int")PlayerPrefs.SetInt(r.key,int.Parse(r.value,CultureInfo.InvariantCulture));else throw new Exception("Unexpected changed key type");}PlayerPrefs.Save();
  if(original.Any(r=>Json(r)!=Json(Current(r))))throw new Exception("Native restoration mismatch");var result=new{restored=true,changedKeys=changed.Select(r=>r.key).ToArray(),verifiedObservedEntries=78,mismatches=0,method="Actual native Unity PlayerPrefs API; shell HKCU view proved different",scope="Only five observed test-reward keys; every other observed key preserved"};File.WriteAllText(receipt,Json(result));return result;
 }
}
