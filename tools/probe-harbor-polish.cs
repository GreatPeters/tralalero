if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var c=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var folder="map-concepts/harbor-polish-2026-09-15";System.IO.Directory.CreateDirectory(folder);
var report=folder+"/baseline-runtime.txt";System.IO.File.WriteAllText(report,"");
void Log(string phase)
{
    var a=p.sharkAnim;var s=a.GetCurrentAnimatorStateInfo(0);
    System.IO.File.AppendAllText(report,$"{phase}: running={IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning}, shark={a.name}, enabled={a.enabled}, state={s.shortNameHash}, walk={s.IsName("Walk")}, idle={s.IsName("Idle")}, normalized={s.normalizedTime}, speed={a.speed}, legacyScore={c.scoreParent.activeInHierarchy}, childCanvas={p.GetComponentInChildren<Canvas>(true)?.gameObject.activeInHierarchy}\n");
}
System.Collections.IEnumerator Probe()
{
    c.GetComponentInChildren<OpeningStoryUI>(true).Skip();yield return new WaitForSecondsRealtime(.3f);Log("lobby");
    c.PlayerPressedStartButton();yield return new WaitForSecondsRealtime(.3f);Log("started");
    var camera=Camera.main;float low=float.MaxValue,high=float.MinValue,modelLow=float.MaxValue,modelHigh=float.MinValue;
    for(int i=0;i<90;i++)
    {float y=camera.transform.position.y-p.transform.position.y;low=Mathf.Min(low,y);high=Mathf.Max(high,y);float modelY=p.transform.Find("Original").localPosition.y;modelLow=Mathf.Min(modelLow,modelY);modelHigh=Mathf.Max(modelHigh,modelY);yield return null;}
    System.IO.File.AppendAllText(report,$"camera relativeY range={high-low}; model localY range={modelHigh-modelLow}; parent={camera.transform.parent.name}\n");
    c.PauseGame();yield return new WaitForSecondsRealtime(.2f);Log("paused");
    c.ResumeGame();yield return new WaitForSecondsRealtime(.3f);Log("resumed");
    var sources=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(a=>a.isPlaying);
    foreach(var a in sources)System.IO.File.AppendAllText(report,$"audio {a.name}: clip={a.clip?.name}, volume={a.volume}, loop={a.loop}\n");
    c.PauseGame();System.IO.File.AppendAllText(report,"COMPLETE\n");
}
c.StartCoroutine(Probe());return "scheduled";
