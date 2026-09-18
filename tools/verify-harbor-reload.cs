if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
var old=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
old.StopAllCoroutines();EditorApplication.isPaused=false;
int oldId=old.GetInstanceID();
System.Collections.IEnumerator Verify(IndianOceanAssets.ShooterSurvival.CanvasScript c)
{
    yield return new WaitForSecondsRealtime(.4f);
    if(c.GetInstanceID()==oldId)throw new Exception("Expected a fresh Canvas");
    if(IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)throw new Exception("Reload should enter the lobby");
    if(c.scoreParent.activeInHierarchy)throw new Exception("Legacy score visible after reload");
    c.PlayerPressedStartButton();IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;
    yield return new WaitForSecondsRealtime(.35f);
    var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
    if(!p.sharkAnim.GetCurrentAnimatorStateInfo(0).IsName("Walk")||p.currentHealth<=0)throw new Exception("Walk failed immediately after reload/start");
    System.IO.File.WriteAllText("map-concepts/harbor-polish-2026-09-15/reload-check.txt","PASS: actual Retry/LoadGame returns to the lobby\nPASS: start after reload plays Walk\nPASS: legacy score remains hidden\nMovement frozen after start to isolate reset from later hazards.\n");
}
UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode> handler=null;
handler=(scene,mode)=>
{
    UnityEngine.SceneManagement.SceneManager.sceneLoaded-=handler;
    var c=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
    c.StartCoroutine(Verify(c));
};
UnityEngine.SceneManagement.SceneManager.sceneLoaded+=handler;
old.LoadGame();return "reload check scheduled on the new scene's Canvas";
