if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode required");
const string folder = "outputs/road-patterns-2026-09-13/balance";
var reportText = System.IO.File.ReadAllText(folder + "/verification.json");
string RecordedHash(string key) => System.Text.RegularExpressions.Regex.Match(reportText, "\"" + key + "\"\\s*:\\s*\"([a-f0-9]{64})\"").Groups[1].Value;
string Hash(byte[] bytes) { using var sha = System.Security.Cryptography.SHA256.Create(); return System.BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
var before = System.IO.File.ReadAllBytes(GameDataWorkbook.GetEditorSourceAbsolutePath());
var candidate = System.IO.File.ReadAllBytes(folder + "/Data.xlsx");
if (Hash(before) != RecordedHash("installFromHash") || Hash(candidate) != RecordedHash("candidateSha256")) throw new System.InvalidOperationException("Concurrent workbook change");
GameDataWorkbookSchema.Validate(candidate);
string archivePath = "Assets/ShooterSurvival/Resources/GameData/Data.bytes";
var archiveBefore = System.IO.File.ReadAllBytes(archivePath);
string backup = "tmp/backups/road-patterns-2026-09-13/Data-" + Hash(before) + ".xlsx";
if (!System.IO.File.Exists(backup)) System.IO.File.WriteAllBytes(backup, before);
try
{
    System.IO.File.WriteAllBytes(GameDataWorkbook.GetEditorSourceAbsolutePath(), candidate);
    UnityEditor.AssetDatabase.ImportAsset("Assets/ShooterSurvival/GameData/Editor/Data.xlsx", UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    GameDataWorkbookEditor.EnsureRuntimeArchiveCurrent(false);
    GameDataWorkbookEditor.ValidateRuntimeArchiveOrThrow();
    EncounterPlacementTables.Reload(); EnvironmentVariableTables.Reload();
    if (!EnvironmentVariableTables.TryGetFloat("reststopHoldoutSeconds", out float seconds) || seconds != 30) throw new System.InvalidOperationException("New balance controls unavailable");
}
catch
{
    System.IO.File.WriteAllBytes(GameDataWorkbook.GetEditorSourceAbsolutePath(), before);
    System.IO.File.WriteAllBytes(archivePath, archiveBefore);
    throw;
}
return new { installed = true, hash = Hash(candidate), archiveVerified = true, rows = EncounterPlacementTables.Rows.Count };
