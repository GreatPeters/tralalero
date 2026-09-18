if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("QA Edit Mode required");
UnityEditor.SessionState.SetBool("RoadPattern.QA",false);
return ChapterPlaytestPreferences.RestoreAt("tmp/road-pattern-playtest/cycle1-reststop/preferences.tsv");
