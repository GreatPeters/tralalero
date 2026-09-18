// Directed travel verification, separate from ordinary progression cohorts.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required");
if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Noryangjin_MapTool_Mode_SR18") throw new System.InvalidOperationException("Start in Noryangjin");
const string folder="tmp/qa-proof/chapter-travel-final";
if (System.IO.Directory.Exists(folder)) throw new System.InvalidOperationException("Preserve existing evidence");
System.IO.Directory.CreateDirectory(folder);
UnityEditor.EditorApplication.isPaused=false;
OpeningStoryUI.Instance?.Skip();
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var report=new System.Collections.Generic.Dictionary<string,object>();
int coins=MoneyScript.S.Coin,jewels=MoneyScript.S.Jewel,att=UnityEngine.PlayerPrefs.GetInt("upgrade_lv_1"),hp=UnityEngine.PlayerPrefs.GetInt("upgrade_lv_2");
string shoe=CosmeticService.Current.Equipped(CosmeticSlot.Shoes).id;
int phase=0;
double deadline=UnityEditor.EditorApplication.timeSinceStartup+90;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 if(!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=tick;return;}
 try{
  if(UnityEditor.EditorApplication.timeSinceStartup>deadline)throw new System.TimeoutException("Travel phase "+phase);
  string scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
  canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
  if(canvas==null)return;
  var chapter=canvas.GetComponent<ChapterProgression>();
  switch(phase){
   case 0: canvas.PlayerPressedStartButton();canvas.YouWin();phase++;break;
   case 1:
    if(!chapter.transitionUI.IsPresenting||chapter.transitionUI.player.time<.6)return;
    report["highwayMoviePlayed"]=chapter.transitionUI.player.frame>0;
    UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/highway-arrival.png");phase++;break;
   case 2:
    if(scene!="HighWay"||MoneyScript.S==null)return;
    report["automaticHighway"]=true;report["highwayLobbyNoRepeatedOpening"]=!OpeningStoryUI.IsBlockingGameplay;
    report["highwayWalletPreserved"]=MoneyScript.S.Coin==coins&&MoneyScript.S.Jewel==jewels;
    canvas.PlayerPressedStartButton();canvas.YouWin();phase++;break;
   case 3:
    if(!chapter.transitionUI.IsPresenting||chapter.transitionUI.player.time<.6)return;
    report["reststopMoviePlayed"]=chapter.transitionUI.player.frame>0;
    UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/reststop-arrival.png");
    chapter.transitionUI.Skip();phase++;break;
   case 4:
    if(scene!="RestStop"||MoneyScript.S==null)return;
    report["skipLoadsRestStop"]=true;report["reststopLobbyNoRepeatedOpening"]=!OpeningStoryUI.IsBlockingGameplay;
    report["walletPreserved"]=MoneyScript.S.Coin==coins&&MoneyScript.S.Jewel==jewels;
    report["upgradesPreserved"]=UnityEngine.PlayerPrefs.GetInt("upgrade_lv_1")==att&&UnityEngine.PlayerPrefs.GetInt("upgrade_lv_2")==hp;
    report["equipmentPreserved"]=CosmeticService.Current.Equipped(CosmeticSlot.Shoes).id==shoe;
    report["noLeakedMovieTargets"]=!UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.RenderTexture>().Any(t=>t.name=="Chapter transition");
    report["unlockedThirdChapter"]=UnityEngine.PlayerPrefs.GetInt("chapter_unlocked")==3;
    var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
    System.IO.File.WriteAllText(folder+"/result.json",(string)serialize.Invoke(null,new object[]{report}));
    UnityEditor.EditorApplication.update-=tick;break;
  }
 }catch(System.Exception e){System.IO.File.WriteAllText(folder+"/error.txt",e.ToString());UnityEditor.EditorApplication.update-=tick;}
};
UnityEditor.EditorApplication.update+=tick;
return new{folder,coins,jewels,att,hp};
