if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="Noryangjin_MapTool_Mode_SR18"||scene.isDirty)throw new Exception("Clean SR18 required");
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var title=canvas.youWinUI.transform.Find("Text (TMP)").GetComponent<TMPro.TMP_Text>();
var next=canvas.youWinUI.transform.Find("Next Level");var text=next.GetComponentInChildren<TMPro.TMP_Text>(true);
if(title.text!="GAME OVER"||canvas.youWinUI==canvas.gameOverUI)throw new Exception("Unexpected win panel baseline");
string backup="tmp/backups/sr18-runtime-fixes-2026-09-10/win-ui-"+DateTime.Now.ToString("HHmmss")+".unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,backup,true);
UnityEditor.Undo.RecordObject(title,"Correct clear title");title.text="STAGE CLEAR";
UnityEditor.Undo.RecordObject(next.gameObject,"Enable replay action");next.gameObject.SetActive(true);
UnityEditor.Undo.RecordObject(text,"Name current-stage replay");text.text="PLAY AGAIN";
foreach(var obj in new UnityEngine.Object[]{title,next.gameObject,text}){UnityEditor.EditorUtility.SetDirty(obj);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new{saved=true,title=title.text,replay=text.text,backup};
