if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode required");
const string folder = "outputs/chapters-polish-2026-09-12/balance";
var reportText = System.IO.File.ReadAllText(folder + "/verification.json");
string RecordedHash(string key) => System.Text.RegularExpressions.Regex.Match(reportText, "\"" + key + "\"\\s*:\\s*\"([a-f0-9]{64})\"").Groups[1].Value;
var before = System.IO.File.ReadAllBytes(GameDataWorkbook.GetEditorSourceAbsolutePath());
var candidate = System.IO.File.ReadAllBytes(folder + "/Data.xlsx");
string Hash(byte[] bytes) { using var sha = System.Security.Cryptography.SHA256.Create(); return System.BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
var knownHashes = System.Text.RegularExpressions.Regex.Match(reportText, "\"knownCandidateHashes\"\\s*:\\s*\\[([^\\]]*)\\]").Groups[1].Value;
bool knownSource = Hash(before) == RecordedHash("sourceSha256") || Hash(before) == RecordedHash("candidateSha256") || knownHashes.Contains("\"" + Hash(before) + "\"");
if (!knownSource || Hash(candidate) != RecordedHash("candidateSha256"))
    throw new System.InvalidOperationException("Workbook changed after review; inspect before installing");
GameDataWorkbookSchema.Validate(candidate);
string backup = "tmp/backups/chapters-polish-2026-09-13/Data-before-" + Hash(before).Substring(0,12) + ".xlsx";
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));
if (!System.IO.File.Exists(backup)) System.IO.File.WriteAllBytes(backup, before);
else if (Hash(System.IO.File.ReadAllBytes(backup)) != Hash(before)) throw new System.InvalidOperationException("Preserve prior balance backup");
System.IO.File.WriteAllBytes(GameDataWorkbook.GetEditorSourceAbsolutePath(), candidate);
UnityEditor.AssetDatabase.ImportAsset("Assets/ShooterSurvival/GameData/Editor/Data.xlsx", UnityEditor.ImportAssetOptions.ForceSynchronousImport);
GameDataWorkbookEditor.EnsureRuntimeArchiveCurrent(false);
GameDataWorkbookEditor.ValidateRuntimeArchiveOrThrow();
EncounterPlacementTables.Reload(); EnvironmentVariableTables.Reload(); UpgradeTables.Reload(); CosmeticTables.Reload();
return new { installed = true, hash = Hash(candidate), encounterRows = EncounterPlacementTables.Rows.Count, archiveVerified = true };
