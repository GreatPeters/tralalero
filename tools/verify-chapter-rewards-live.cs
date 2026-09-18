// Directed verification; the cleared rounds below do not count as normal playtests.
if(!UnityEditor.EditorApplication.isPlaying||!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("QA Play Mode required");
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Noryangjin_MapTool_Mode_SR18")throw new System.InvalidOperationException("Start in Noryangjin");
const string folder="tmp/qa-proof/chapter-rewards-final-v2";
if(System.IO.Directory.Exists(folder))throw new System.InvalidOperationException("Preserve evidence");
System.IO.Directory.CreateDirectory(folder);UnityEditor.EditorApplication.isPaused=false;
OpeningStoryUI.Instance?.Skip();
for(int chapter=1;chapter<=3;chapter++)UnityEngine.PlayerPrefs.DeleteKey("chapter_rewarded_"+chapter);
int coins=MoneyScript.S.Coin,jewels=MoneyScript.S.Jewel,att=UnityEngine.PlayerPrefs.GetInt("upgrade_lv_1"),hp=UnityEngine.PlayerPrefs.GetInt("upgrade_lv_2");
string shoes=CosmeticService.Current.Equipped(CosmeticSlot.Shoes).id;
var report=new System.Collections.Generic.Dictionary<string,object>();
int phase=0;double next=0,deadline=UnityEditor.EditorApplication.timeSinceStartup+90;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 if(!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=tick;return;}
 double now=UnityEditor.EditorApplication.timeSinceStartup;if(now<next)return;
 try{
  if(now>deadline)throw new System.TimeoutException("Reward/travel phase "+phase);
  var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();if(canvas==null||MoneyScript.S==null)return;
  var chapter=canvas.GetComponent<ChapterProgression>();string scene=canvas.gameObject.scene.name;
  switch(phase){
   case 0:
    canvas.PlayerPressedStartButton();canvas.YouWin();chapter.CompleteChapter();
    report["noryFirst20Once"]=MoneyScript.S.Jewel==jewels+20&&chapter.ClearJewels==20;phase++;break;
   case 1:
    if(!chapter.transitionUI.IsPresenting||chapter.transitionUI.player.time<.6)return;
    report["highwayMovieAndRewardVisible"]=chapter.transitionUI.player.frame>0&&chapter.transitionUI.status.text.Contains("20");
    UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/highway-reward.png");phase++;break;
   case 2:
    if(scene!="HighWay")return;
    report["naturalTransition"]=!OpeningStoryUI.IsBlockingGameplay&&MoneyScript.S.Jewel==jewels+20;
    canvas.PlayerPressedStartButton();canvas.YouWin();chapter.CompleteChapter();
    report["highwayFirst25Once"]=MoneyScript.S.Jewel==jewels+45&&chapter.ClearJewels==25;phase++;break;
   case 3:
    if(!chapter.transitionUI.IsPresenting||chapter.transitionUI.player.time<.6)return;
    UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/reststop-reward.png");chapter.transitionUI.Skip();phase++;break;
   case 4:
    if(scene!="RestStop")return;
    report["skipTransition"]=!OpeningStoryUI.IsBlockingGameplay&&MoneyScript.S.Jewel==jewels+45;
    canvas.PlayerPressedStartButton();canvas.YouWin();chapter.CompleteChapter();
    report["restFirst30Once"]=MoneyScript.S.Jewel==jewels+75&&chapter.ClearJewels==30;phase++;next=now+2.5;break;
   case 5:
    report["victoryRewardVisible"]=canvas.youWinUI.activeInHierarchy&&chapter.clearRewardText.text.Contains("30");
    UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/reststop-victory.png");phase=50;next=now+.4;break;
   case 50:
    chapter.Replay();phase=6;next=now+1;break;
   case 6:
    if(scene!="RestStop"||chapter.Completed)return;
    canvas.PlayerPressedStartButton();canvas.YouWin();chapter.CompleteChapter();
    report["replay5Once"]=MoneyScript.S.Jewel==jewels+80&&chapter.ClearJewels==5;phase++;next=now+2.5;break;
   case 7:
    report["coinsPreserved"]=MoneyScript.S.Coin==coins;
    report["upgradesPreserved"]=UnityEngine.PlayerPrefs.GetInt("upgrade_lv_1")==att&&UnityEngine.PlayerPrefs.GetInt("upgrade_lv_2")==hp;
    report["equipmentPreserved"]=CosmeticService.Current.Equipped(CosmeticSlot.Shoes).id==shoes;
    report["noMovieTargets"]=!UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.RenderTexture>().Any(t=>t.name=="Chapter transition");
    report["jewelsBefore"]=jewels;report["jewelsAfter"]=MoneyScript.S.Jewel;
    var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
    System.IO.File.WriteAllText(folder+"/result.json",(string)serialize.Invoke(null,new object[]{report}));
    UnityEditor.EditorApplication.update-=tick;break;
  }
 }catch(System.Exception e){System.IO.File.WriteAllText(folder+"/error.txt",e.ToString());UnityEditor.EditorApplication.update-=tick;}
};
UnityEditor.EditorApplication.update+=tick;return new{folder,coins,jewels,att,hp};
