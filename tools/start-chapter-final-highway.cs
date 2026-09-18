if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("QA Edit Mode required");
ChapterPlaytestPreferences.SnapshotAt("tmp/backups/chapters-polish-2026-09-13/final-highway-entry.tsv");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/HighWay.unity");
return Sr18ProgressionPlaytest.Begin("chapter-polish-final-highway-20260913",30,20260913,true,false,true,"tmp/backups/chapters-polish-2026-09-13/playerprefs.tsv",true);
