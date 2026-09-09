if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity")throw new System.InvalidOperationException("SR18 required");
string folder="tmp/backups/sr18-choice-layout-2026-09-08/tests-"+System.DateTime.Now.ToString("HHmmss");System.IO.Directory.CreateDirectory(folder);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,folder+"/before.unity",true);
string live=System.IO.File.ReadAllText(folder+"/before.unity").Replace("\r\n","\n"),disk=System.IO.File.ReadAllText(scene.path).Replace("\r\n","\n");
string Normalize(string text){
 text=System.Text.RegularExpressions.Regex.Replace(text,@"(?m)^  (m_AnchorMin|m_AnchorMax|m_AnchoredPosition|m_SizeDelta): .*\n","");
 return System.Text.RegularExpressions.Regex.Replace(text,@"(?m)^    - target: \{fileID: (3472694769962777710|7901544632725012564), guid: 7bc4493fbc8ad4348bab6c61730917b1, type: 3\}\n      propertyPath: m_LocalRotation\.[wxyz]\n      value: [^\n]+\n      objectReference: \{fileID: 0\}\n","");
}
if(Normalize(live)!=Normalize(disk))throw new System.InvalidOperationException("Unrelated pending changes; inspect before isolating");
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
return new{isolated=true,backup=folder,billboardPoseUpdatesPreserved=true};
