if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("QA Edit Mode required");
ChapterPlaytestPreferences.SnapshotAt("tmp/backups/chapters-polish-2026-09-13/before-frame-corrected-cohort.tsv");
for(int chapter=1;chapter<=5;chapter++)UnityEngine.PlayerPrefs.DeleteKey("chapter_rewarded_"+chapter);
UnityEngine.PlayerPrefs.SetInt("chapter_unlocked",1);UnityEngine.PlayerPrefs.Save();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
return Sr18ProgressionPlaytest.Begin("chapter-polish-final-nory-20260913",30,20260913,true,true,true,"tmp/backups/chapters-polish-2026-09-13/playerprefs.tsv",true);
