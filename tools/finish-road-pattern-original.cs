if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("Original Edit Mode required");
var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
const string backupFolder="map-concepts/road-patterns-2026-09-13/before/editor-snapshots";
System.IO.Directory.CreateDirectory(backupFolder);
if(current.isDirty)
{
    string backup="Assets/ShooterSurvival/Scenes/Tools/RoadPatterns_FinalBackup_20260913.unity";
    if(System.IO.File.Exists(backup))throw new System.InvalidOperationException("Preserve previous backup");
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(current,backup,true))throw new System.IO.IOException("Backup failed");
}
// All in-memory changes have been preserved as native Unity scene copies.
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(HighwaySceneBuilder.ScenePath);
var archived=new System.Collections.Generic.List<string>();
foreach(string file in new[]{"RoadPatterns_DirtyBackup_20260913.unity","RoadPatterns_FinalBackup_20260913.unity"})
{
    string source="Assets/ShooterSurvival/Scenes/Tools/"+file;
    if(!System.IO.File.Exists(source))continue;
    string target=backupFolder+"/"+file;
    if(System.IO.File.Exists(target))throw new System.InvalidOperationException("Archive destination exists");
    var bytes=System.IO.File.ReadAllBytes(source);System.IO.File.WriteAllBytes(target,bytes);
    if(!System.Linq.Enumerable.SequenceEqual(bytes,System.IO.File.ReadAllBytes(target)))throw new System.IO.IOException("Backup copy mismatch");
    if(System.IO.File.Exists(source+".meta"))System.IO.File.Copy(source+".meta",target+".meta");
    if(!UnityEditor.AssetDatabase.DeleteAsset(source))throw new System.IO.IOException("Could not remove archived backup from Assets");
    archived.Add(target);
}
// Existing native regression tests write the ATT level/cache. Restore their
// recorded pre-test state after all tests and scene-open callbacks have run.
var restored=ChapterPlaytestPreferences.RestoreAt("tmp/backups/road-patterns-2026-09-13/original-playerprefs.tsv");
return new{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,archived,restored};
