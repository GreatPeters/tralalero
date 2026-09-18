if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Original Editor must be in Edit Mode");
if (UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/")) throw new System.InvalidOperationException("Original project required");
const string folder = "tmp/backups/chapters-polish-2026-09-13";
const string prefsPath = folder + "/original-playerprefs.tsv";
const string scenePath = folder + "/original-live-before-merge.unity";
if (System.IO.File.Exists(prefsPath) || System.IO.File.Exists(scenePath)) throw new System.InvalidOperationException("Preserve existing original-state backups");
System.IO.Directory.CreateDirectory(folder);
var lines = new System.Collections.Generic.List<string>();
void Add(string key,string kind)
{
    string value = kind=="string" ? System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(UnityEngine.PlayerPrefs.GetString(key))) : kind=="int" ? UnityEngine.PlayerPrefs.GetInt(key).ToString(System.Globalization.CultureInfo.InvariantCulture) : UnityEngine.PlayerPrefs.GetFloat(key).ToString("R",System.Globalization.CultureInfo.InvariantCulture);
    lines.Add(string.Join("\t",key,kind,UnityEngine.PlayerPrefs.HasKey(key),value));
}
Add("coin","int"); Add("jewel","int"); Add("chapter_unlocked","int"); Add("ads_last_reward_round","string");
for (int id=1;id<=9;id++) Add("upgrade_lv_"+id,"int");
foreach (string type in System.Enum.GetNames(typeof(UpgradeStatManager.UpgradeType))) { Add("upgrade_stat_"+type,"float"); Add("upgrade_stat_type_"+type,"int"); }
foreach (var row in CosmeticTables.Rows) Add(CosmeticService.OwnedKey(row.id),"int");
foreach (CosmeticSlot slot in System.Enum.GetValues(typeof(CosmeticSlot))) Add(CosmeticService.EquippedKey(slot),"string");
System.IO.File.WriteAllLines(prefsPath,lines);
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool wasDirty = scene.isDirty;
if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath,true)) throw new System.IO.IOException("Live scene backup failed");
if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new System.IO.IOException("Original scene save failed");
return new { keys=lines.Count,prefsPath,scenePath,wasDirty,coins=UnityEngine.PlayerPrefs.GetInt("coin"),jewels=UnityEngine.PlayerPrefs.GetInt("jewel") };
