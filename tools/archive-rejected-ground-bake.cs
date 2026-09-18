const string record="map-concepts/chapters-polish-2026-09-12/review/rejected-native-ground-bake";
System.IO.Directory.CreateDirectory(record);
var paths=new[]{"Assets/ShooterSurvival/Models/Chapters/Mascots/ConeMechanic/Grounded_attack_loop.anim","Assets/ShooterSurvival/Models/Chapters/Mascots/ConeMechanic/Grounded_attack_once.anim","Assets/ShooterSurvival/Editor/ChapterMascotGrounding.cs"};
foreach(string path in paths){
 if(!System.IO.File.Exists(path))throw new System.InvalidOperationException("Expected task-created candidate missing: "+path);
 System.IO.File.Copy(path,record+"/"+System.IO.Path.GetFileName(path)+".rejected",false);
 if(!UnityEditor.AssetDatabase.DeleteAsset(path))throw new System.IO.IOException("Could not remove rejected candidate "+path);
}
System.IO.File.WriteAllText(record+"/README.md","The native BakeMesh inspection multiplied FBX scale twice. Independent boneWorld × bindpose × vertex skinning measured all nine rigs within 3mm of the floor. No controller was switched to these candidates. Preserve the source clips; rejected baker/candidates are retained only as diagnostic evidence.");
return new{archived=paths.Length,controllersChanged=false};
