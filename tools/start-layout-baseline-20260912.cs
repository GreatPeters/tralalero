if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Scene is dirty");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
if(EncounterPlacementTables.Rows.Count!=200)throw new System.InvalidOperationException("New workbook not applied");
return Sr18ProgressionPlaytest.Begin("skins-layout-baseline-20260912",3,20260912,true,true,false);
