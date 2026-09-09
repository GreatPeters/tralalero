// Run with Unity Pipeline eval_file. SR18 scene overrides only; no mesh/texture edits.
const string scenePath="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
const string sourcePath="Assets/ShooterSurvival/Materials/Env/pirate/Noryangjin_RoadBasic_SubtleOutline.mat";
const string targetPath="Assets/ShooterSurvival/Materials/Generated/SR18_RoadSlope.mat";
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Use Edit Mode.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
if(!scene.isLoaded||scene.isDirty)throw new System.InvalidOperationException("Open and save SR18 before applying.");
var roads=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform.Find("Roads");
var ramps=roads.Cast<Transform>().Where(t=>t.name.EndsWith("_Uphill")||t.name.EndsWith("_Downhill")).ToArray();
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
if(ramps.Length!=12||source==null)throw new System.InvalidOperationException("Unexpected source setup.");
var renderers=ramps.SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).ToArray();
if(renderers.Any(r=>r.sharedMaterials.Any(m=>m!=source)))throw new System.InvalidOperationException("Ramp materials changed; inspect before reapplying.");
if(UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(targetPath)!=null)throw new System.InvalidOperationException("Slope material already exists; do not overwrite.");
string backup="tmp/backups/sr18-before-ramp-shading-"+System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".unity.tmp";
System.IO.Directory.CreateDirectory("tmp/backups");System.IO.File.Copy(scenePath,backup,false);
var material=new Material(source);material.name="SR18 Road Slope";
material.SetFloat("_SelfShadingSize",.45f);material.SetFloat("_ShadowEdgeSize",.08f);
UnityEditor.AssetDatabase.CreateAsset(material,targetPath);
UnityEditor.Undo.IncrementCurrentGroup();int group=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Fix SR18 ramp cel shading");
try{
 foreach(var renderer in renderers){UnityEditor.Undo.RecordObject(renderer,"Slope-safe wood shading");renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>material).ToArray();UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
 if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.InvalidOperationException("Scene save failed.");
 UnityEditor.Undo.CollapseUndoOperations(group);UnityEditor.SceneView.RepaintAll();
 return new{ramps=ramps.Length,material=targetPath,backup,saved=!scene.isDirty,sourceThreshold=source.GetFloat("_SelfShadingSize"),slopeThreshold=material.GetFloat("_SelfShadingSize")};
}catch{UnityEditor.Undo.RevertAllDownToGroup(group);throw;}
