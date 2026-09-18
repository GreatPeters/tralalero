// Directed UI/transition verification; this does not count as an ordinary clear.
if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required.");
const string folder="tmp/image-previews/two-chapter-2026-09-11/chapter-transition";
if(System.IO.Directory.Exists(folder))throw new System.InvalidOperationException("Preserve existing evidence.");
System.IO.Directory.CreateDirectory(folder);
var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
var report=new System.Collections.Generic.Dictionary<string,object>();
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
if(OpeningStoryUI.Instance!=null)OpeningStoryUI.Instance.Skip();
int coin=MoneyScript.S.Coin,jewel=MoneyScript.S.Jewel;
int attackLevel=UnityEngine.PlayerPrefs.GetInt("upgrade_lv_1");
string shoes=CosmeticService.Current.Equipped(CosmeticSlot.Shoes).id;
var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);shop.Open();shop.SelectSlot(CosmeticSlot.Shoes);shop.SelectItem(shoes);
int phase=0;double ready=UnityEditor.EditorApplication.timeSinceStartup+.7,deadline=ready+45;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    if(!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=tick;return;}
    double now=UnityEditor.EditorApplication.timeSinceStartup;if(now<ready)return;
    try
    {
        if(now>deadline)throw new System.TimeoutException("Transition phase "+phase);
        switch(phase)
        {
            case 0:
                UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/shop-equipped-final.png");phase++;ready=now+.3;break;
            case 1:
                shop.Close();canvas.PlayerPressedStartButton();canvas.YouWin();phase++;ready=now+2;break;
            case 2:
                var next=canvas.youWinUI.transform.Find("NextChapter").GetComponent<UnityEngine.UI.Button>();
                report["noryClearOffersHighway"]=next.gameObject.activeInHierarchy&&next.interactable;
                report["noryCompleted"]=canvas.GetComponent<ChapterProgression>().Completed;
                UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/nory-clear-next.png");phase++;ready=now+.3;break;
            case 3:
                canvas.youWinUI.transform.Find("NextChapter").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();phase++;ready=now+1.5;break;
            case 4:
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="HighWay"||MoneyScript.S==null)return;
                canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
                report["loadedHighway"]=true;report["walletPreserved"]=MoneyScript.S.Coin==coin&&MoneyScript.S.Jewel==jewel;
                report["equipmentPreserved"]=CosmeticService.Current.Equipped(CosmeticSlot.Shoes).id==shoes;
                report["upgradePreserved"]=UnityEngine.PlayerPrefs.GetInt("upgrade_lv_1")==attackLevel;
                if(OpeningStoryUI.Instance!=null)OpeningStoryUI.Instance.Skip();
                canvas.PlayerPressedStartButton();phase++;ready=now+2;break;
            case 5:
                UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/highway-play-final.png");phase++;ready=now+.3;break;
            case 6:
                canvas.YouWin();phase++;ready=now+2;break;
            case 7:
                report["noInventedThirdChapter"]=string.IsNullOrEmpty(canvas.GetComponent<ChapterProgression>().nextScene)&&UnityEngine.PlayerPrefs.GetInt("chapter_unlocked")==2;
                var replay=canvas.youWinUI.transform.Find("ReplayChapter").GetComponent<UnityEngine.UI.Button>();
                report["replayReachable"]=replay.gameObject.activeInHierarchy&&replay.interactable;
                replay.onClick.Invoke();phase++;ready=now+1.5;break;
            case 8:
                canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();if(canvas==null)return;
                report["reloadedHighwayLobby"]=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="HighWay"&&!canvas.GetComponent<ChapterProgression>().Completed&&!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning;
                if(OpeningStoryUI.Instance!=null)OpeningStoryUI.Instance.Skip();
                System.IO.File.WriteAllText(folder+"/result.json",(string)serialize.Invoke(null,new object[]{report}));UnityEditor.EditorApplication.update-=tick;break;
        }
    }
    catch(System.Exception error){System.IO.File.WriteAllText(folder+"/error.txt",error.ToString());UnityEditor.EditorApplication.update-=tick;}
};
UnityEditor.EditorApplication.update+=tick;return new{folder,coin,jewel,shoes};
