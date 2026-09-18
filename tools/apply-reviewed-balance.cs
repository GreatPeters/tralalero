if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
var candidate=System.IO.File.ReadAllBytes("outputs/progression-balance-2026-09-12/Data.xlsx");GameDataWorkbookSchema.Validate(candidate);
System.IO.File.WriteAllBytes(GameDataWorkbook.GetEditorSourceAbsolutePath(),candidate);
UnityEditor.AssetDatabase.ImportAsset("Assets/ShooterSurvival/GameData/Editor/Data.xlsx",UnityEditor.ImportAssetOptions.ForceUpdate);
EncounterPlacementTables.Reload();EnvironmentVariableTables.Reload();UpgradeTables.Reload();CosmeticTables.Reload();
return new{rows=EncounterPlacementTables.Rows.Count,cosmetics=CosmeticTables.Rows.Count};
