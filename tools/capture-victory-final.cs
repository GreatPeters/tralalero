if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required");
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="RestStop")throw new System.InvalidOperationException("RestStop required; earlier chapters travel automatically");
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
OpeningStoryUI.Instance?.Skip();canvas.PlayerPressedStartButton();canvas.YouWin();
double end=UnityEditor.EditorApplication.timeSinceStartup+3;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{if(!UnityEditor.EditorApplication.isPlaying||canvas==null){UnityEditor.EditorApplication.update-=tick;return;}if(UnityEditor.EditorApplication.timeSinceStartup<end)return;
 if(!canvas.youWinUI.activeInHierarchy){if(UnityEditor.EditorApplication.timeSinceStartup>end+10){UnityEditor.EditorApplication.update-=tick;System.IO.File.WriteAllText("tmp/qa-proof/victory-capture-error.txt","Victory not visible");}return;}
 UnityEngine.ScreenCapture.CaptureScreenshot("tmp/qa-proof/victory-workshop-final.png");
 UnityEditor.EditorApplication.update-=tick;};
UnityEditor.EditorApplication.update+=tick;return "Holding final victory for capture";
