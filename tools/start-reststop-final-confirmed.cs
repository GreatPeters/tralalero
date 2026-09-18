if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("QA Edit Mode required");
ChapterPlaytestPreferences.SnapshotAt("tmp/backups/chapters-polish-2026-09-13/reststop-32-run-clear.tsv");
ChapterPlaytestPreferences.RestoreAt("tmp/backups/chapters-polish-2026-09-13/final-reststop-entry.tsv");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RestStopChapterBuilder.ScenePath);
return Sr18ProgressionPlaytest.Begin("chapter-polish-final-reststop-confirmed-20260913",35,20260913,true,false,true,"tmp/backups/chapters-polish-2026-09-13/playerprefs.tsv",true);
