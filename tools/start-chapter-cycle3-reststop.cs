if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode required");
if(!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("Isolated QA required");
ChapterPlaytestPreferences.SnapshotAt("tmp/backups/chapters-polish-2026-09-13/cycle3-reststop-entry.tsv");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RestStopChapterBuilder.ScenePath);
return Sr18ProgressionPlaytest.Begin("chapter-polish-cycle3-reststop-20260913",26,20260913,true,false,true,"tmp/backups/chapters-polish-2026-09-13/playerprefs.tsv",true);
