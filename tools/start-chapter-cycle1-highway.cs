if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode required");
if (UnityEngine.Application.companyName != "TralaleroQA") throw new System.InvalidOperationException("QA preferences required");
for (int id=1;id<=9;id++) UnityEngine.PlayerPrefs.SetInt("upgrade_lv_"+id,0);
UnityEngine.PlayerPrefs.SetInt("upgrade_lv_1",10); UnityEngine.PlayerPrefs.SetInt("upgrade_lv_2",12);
UnityEngine.PlayerPrefs.SetInt("coin",0);UnityEngine.PlayerPrefs.SetInt("jewel",0);UnityEngine.PlayerPrefs.Save();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(HighwaySceneBuilder.ScenePath);
return Sr18ProgressionPlaytest.Begin("chapter-polish-cycle1-highway-20260913",3,20260913,true,false,true,"tmp/backups/chapters-polish-2026-09-13/playerprefs.tsv");
