if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
if(!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("Isolated QA required");
var exit=System.IO.File.ReadAllLines("tmp/image-previews/sr18-presentation-progression-2026-09-10/chapter-polish-cycle3-nory-20260913/runs.csv").Last().Split(',');
if(exit[2]!="clear"||exit[10]!="458"||exit[11]!="11"||exit[12]!="12"||!UpgradeTables.TryGet(3,6,out var speed)||speed.price!=145)throw new System.InvalidOperationException("Recorded carryover no longer matches");
ChapterPlaytestPreferences.SnapshotAt("tmp/backups/chapters-polish-2026-09-13/highway-greedy-diagnostic.tsv");
// Reconstruct the real cycle3 Noryangjin exit: CSV gives wallet/ATT/HP;
// the final145coin speed purchase is level6 in the verified workbook (first price45).
for(int id=1;id<=9;id++)UnityEngine.PlayerPrefs.SetInt("upgrade_lv_"+id,id==1?11:id==2?12:id==3?6:0);
foreach(string type in System.Enum.GetNames(typeof(UpgradeStatManager.UpgradeType))){UnityEngine.PlayerPrefs.DeleteKey("upgrade_stat_"+type);UnityEngine.PlayerPrefs.DeleteKey("upgrade_stat_type_"+type);}
UnityEngine.PlayerPrefs.SetInt("coin",458);UnityEngine.PlayerPrefs.SetInt("jewel",0);UnityEngine.PlayerPrefs.Save();
ChapterPlaytestPreferences.SnapshotAt("tmp/backups/chapters-polish-2026-09-13/cycle3-highway-entry-reconstructed.tsv");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/HighWay.unity");
return Sr18ProgressionPlaytest.Begin("chapter-polish-cycle3-highway-saving-20260913",26,20260913,true,false,true,"tmp/backups/chapters-polish-2026-09-13/playerprefs.tsv",true);
