using System;using System.IO;using System.Linq;using System.Globalization;using UnityEngine;using UnityEditor;
// One-time rollback of the failed pre-goal fixture's native reward transaction.
// Copies only changed observed keys back to the immediately prior passed boundary.
public static class RepairFixturePrefsCycle06 {
 public sealed class Row {public string key,kind,value;public bool existed;}
 public sealed class Snapshot {public Row[] persisted;}
 static Type J=>AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
 static string Json(object o)=>(string)J.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
 static Snapshot Read(string p)=>(Snapshot)J.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{File.ReadAllText(p),typeof(Snapshot)});
 static Row Current(Row r)=>new Row{key=r.key,kind=r.kind,existed=PlayerPrefs.HasKey(r.key),value=r.kind=="int"?PlayerPrefs.GetInt(r.key).ToString(CultureInfo.InvariantCulture):r.kind=="float"?PlayerPrefs.GetFloat(r.key).ToString("R",CultureInfo.InvariantCulture):Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(r.key)))};
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new Exception("Clean Edit required");
  const string root="outputs/chapter45-detailed-design-2026-10-03/restart-cycle06/";string receipt=root+"shoe-before/fixture-preference-rollback.json";if(File.Exists(receipt))throw new Exception("Rollback already performed");
  var before=Read(root+"cinema-complete/boundary.json").persisted;var failed=Read(root+"shoe-before/boundary.json").persisted;
  if(before.Length!=78||failed.Length!=78||failed.Where((r,i)=>Json(r)!=Json(Current(r))).Any())throw new Exception("Preferences changed since failed fixture; rollback withheld");
  var changed=before.Where((r,i)=>Json(r)!=Json(failed[i])).ToArray();var allowed=new[]{"jewel","chapter_rewarded_5","chapter_unlocked"};if(changed.Any(r=>!allowed.Contains(r.key)))throw new Exception("Unexpected preference changes: "+string.Join(",",changed.Select(r=>r.key)));
  foreach(var r in changed){if(!r.existed)PlayerPrefs.DeleteKey(r.key);else if(r.kind=="int")PlayerPrefs.SetInt(r.key,int.Parse(r.value,CultureInfo.InvariantCulture));else throw new Exception("Unexpected reward key type");}PlayerPrefs.Save();
  if(before.Any(r=>Json(r)!=Json(Current(r))))throw new Exception("Rollback mismatch");var result=new{restoredPriorPassedBoundary=true,changedKeys=changed.Select(r=>r.key).ToArray(),verifiedObservedEntries=78,mismatches=0,reason="Failed shoe-before fixture was incorrectly positioned inside the goal; preserve evidence and restore its native reward transaction before corrected first-clear test"};File.WriteAllText(receipt,Json(result));return result;
 }
}
