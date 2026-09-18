if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
string folder=System.IO.Path.GetFullPath("tmp/image-previews/feedback-2026-09-19/accepted");System.IO.Directory.CreateDirectory(folder);
string report=folder+"/checks.txt";
if(System.IO.File.Exists(report))throw new InvalidOperationException("Preserve previous verification");
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var settings=canvas.settingsMenuUI.GetComponent<HarborSettingsPanel>();
var camera=Camera.main;var follow=camera.GetComponent<StableGameplayCamera>();float fov=camera.fieldOfView;
var map=GameObject.Find("Noryangjin_MapTool").transform;
var speed=typeof(IndianOceanAssets.ShooterSurvival.PlayerScript).GetField("currentForwardMoveSpeed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
void Check(bool pass,string message){System.IO.File.AppendAllText(report,(pass?"PASS ":"FAIL ")+message+"\n");if(!pass)throw new Exception(message);}
System.Collections.IEnumerator Shot(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return null;}
System.Collections.IEnumerator Run(){
 canvas.GetComponentInChildren<OpeningStoryUI>(true).Skip();settings.Open(false);yield return new WaitForSecondsRealtime(.3f);
 var state=IndianOceanAssets.ShooterSurvival.SettingsManager.Instance;bool sound=state.soundEnabled,vibration=state.vibrationEnabled;float volume=state.soundVolume,sensitivity=state.moveSensitivity;
 try {
  settings.soundButton.onClick.Invoke();Check(state.soundEnabled!=sound,"Sound toggle changes real state");settings.soundButton.onClick.Invoke();
  settings.vibrationButton.onClick.Invoke();Check(state.vibrationEnabled!=vibration,"Vibration toggle changes real state");settings.vibrationButton.onClick.Invoke();
  settings.volume.value=.63f;Check(Mathf.Abs(state.soundVolume-.63f)<.001f,"Volume slider binding");settings.volume.value=volume;
  settings.sensitivity.value=1.4f;Check(Mathf.Abs(player.moveSensitivity-1.4f)<.001f,"Sensitivity slider binding");settings.sensitivity.value=sensitivity;
 } finally {state.soundEnabled=sound;state.vibrationEnabled=vibration;state.soundVolume=volume;state.moveSensitivity=sensitivity;state.ApplyAudioSettings();state.SaveSettings();settings.Refresh();}
 yield return Shot("01-settings");settings.Close();
 canvas.PlayerPressedStartButton();canvas.GetComponent<CoastalTutorialUI>().Close();speed.SetValue(player,0f);player.canShoot=false;
 canvas.PauseGame();yield return new WaitForSecondsRealtime(.2f);yield return Shot("02-pause");settings.Close();
 Check(IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning,"Pause close resumes run");
 foreach(var enemy in map.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>(true))enemy.enabled=false;
 var altar=map.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>(true).Single(a=>a.name=="SR18_L_B01_T009");
 player.ApplyContinuousRoutePose(new Vector3(-10.7f,.12f,altar.transform.position.z-11),Vector3.forward,0);Physics.SyncTransforms();yield return new WaitForSecondsRealtime(1);
 Check(altar.GetComponent<IndianOceanAssets.ShooterSurvival.BonusRewardCue>().nearbyGlow.activeSelf,"Gold bonus sparkle visible on approach");yield return Shot("05-bonus-approach");
 var box=altar.Wall.GetComponent<Collider>();var center=box.bounds.center;float hp=player.currentHealth,attack=player.ResolvedAttackDamage;string bonus=altar.Wall.CurrentBonusAlias+" "+altar.Wall.CurrentBonusDisplayValue;
 player.ApplyContinuousRoutePose(new Vector3(center.x,.12f,center.z-3),Vector3.forward,0);Physics.SyncTransforms();speed.SetValue(player,3f);float until=Time.realtimeSinceStartup+5;
 while(altar.ChoicePair.Selected==null&&Time.realtimeSinceStartup<until)yield return null;
 speed.SetValue(player,0f);Check(altar.ChoicePair.Selected==altar&&!altar.gameObject.activeSelf,"Real collider contact claims and removes selected bonus");
 Check(!altar.ChoicePair.Right.gameObject.activeSelf,"Other bonus removed after choice");
 var cue=altar.GetComponent<IndianOceanAssets.ShooterSurvival.BonusRewardCue>();cue.PlayPickup();cue.PlayPickup();
 int Bursts()=>UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Select(p=>p.transform.root).Distinct().Count(t=>t.name.StartsWith("BonusPickupBurst"));
 Check(Bursts()==1,"Repeated claim cannot spawn duplicate pickup bursts");yield return new WaitForSecondsRealtime(.12f);yield return Shot("06-bonus-collected");
 System.IO.File.AppendAllText(report,"REWARD "+bonus+" health "+hp+" -> "+player.currentHealth+" attack "+attack+" -> "+player.ResolvedAttackDamage+"\n");
 yield return new WaitForSecondsRealtime(2.6f);Check(Bursts()==0,"One-shot pickup effect cleans itself up");
 IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;System.IO.File.AppendAllText(report,"COMPLETE\n");
}
canvas.StartCoroutine(Run());return folder;



