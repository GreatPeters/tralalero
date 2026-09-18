if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("QA Edit Mode required");
ChapterPlaytestPreferences.SnapshotAt("tmp/backups/chapters-polish-2026-09-13/cycle3-highway-saving-cleared.tsv");
ChapterPlaytestPreferences.RestoreAt("tmp/backups/chapters-polish-2026-09-13/cycle3-highway-entry-reconstructed.tsv");
// The new reward-only revision credits the already measured Noryangjin first clear.
UnityEngine.PlayerPrefs.SetInt("jewel",20);UnityEngine.PlayerPrefs.SetInt("chapter_rewarded_1",1);
for(int chapter=2;chapter<=5;chapter++)UnityEngine.PlayerPrefs.DeleteKey("chapter_rewarded_"+chapter);
UnityEngine.PlayerPrefs.Save();
ChapterPlaytestPreferences.SnapshotAt("tmp/backups/chapters-polish-2026-09-13/cycle4-highway-entry.tsv");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/HighWay.unity");
return Sr18ProgressionPlaytest.Begin("chapter-polish-cycle4-highway-20260913",26,20260913,true,false,true,"tmp/backups/chapters-polish-2026-09-13/playerprefs.tsv",true);
