if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("QA Edit Mode required");
ChapterPlaytestPreferences.SnapshotAt("tmp/backups/chapters-polish-2026-09-13/reststop-low-entry-diagnostic.tsv");
ChapterPlaytestPreferences.RestoreAt("tmp/backups/chapters-polish-2026-09-13/final-reststop-entry.tsv");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RestStopChapterBuilder.ScenePath);
return Sr18ProgressionPlaytest.Begin("chapter-polish-final-reststop-paced-20260913",3,20260913,true,false,true,"tmp/backups/chapters-polish-2026-09-13/playerprefs.tsv",true);
