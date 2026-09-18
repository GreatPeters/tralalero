if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("QA Edit Mode required");
ChapterPlaytestPreferences.SnapshotAt("tmp/backups/chapters-polish-2026-09-13/final-directed-tests.tsv");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
UnityEditor.SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",1);
UnityEditor.EditorApplication.isPaused=false;
UnityEditor.EditorApplication.isPlaying=true;
return "Directed UI/ad/reward verification prepared; restore final-directed-tests.tsv afterward";
