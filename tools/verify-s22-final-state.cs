using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class S22FinalState
{
 public static object Main(){string root="outputs/s22-polish-2026-10-01/";var saved=File.ReadAllLines(root+"session-before.txt");bool match=SessionState.GetBool("NoryangjinMapTool.TestPower9999",false)==bool.Parse(saved[3])&&SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false)==bool.Parse(saved[4])&&Mathf.Approximately(SessionState.GetFloat("NoryangjinMapTool.TestTimeScale",1),float.Parse(saved[5],System.Globalization.CultureInfo.InvariantCulture))&&OpeningStoryUI.EditorAutoPlayEnabled==bool.Parse(saved[6])&&SessionState.GetInt("NoryangjinMapTool.TestStartStage",0)==int.Parse(saved[2])&&AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)==saved[1];
  const string scratch="Assets/tmp/image-previews/s22-polish-2026-10-01";if(AssetDatabase.IsValidFolder(scratch)&&Directory.GetFiles(scratch,"*",SearchOption.AllDirectories).Length==0)AssetDatabase.DeleteAsset(scratch);
  File.WriteAllText(root+"session-verified.txt",$"sessionMatchesSnapshot={match}; play={EditorApplication.isPlaying}; sceneDirty={UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty}; coin={PlayerPrefs.GetInt("coin")}; jewel={PlayerPrefs.GetInt("jewel")}");if(!match)throw new Exception("Session mismatch");return new{sessionMatchesSnapshot=match,play=EditorApplication.isPlaying,sceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty};}
}
