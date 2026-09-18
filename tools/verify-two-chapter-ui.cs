if (!UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isPaused)
    throw new System.InvalidOperationException("Unpaused lobby Play Mode required.");
const string folder="tmp/image-previews/two-chapter-2026-09-11/ui-functional";
if(System.IO.Directory.Exists(folder))throw new System.InvalidOperationException("Preserve existing UI evidence.");
System.IO.Directory.CreateDirectory(folder);
var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
var report=new System.Collections.Generic.Dictionary<string,object>();
var sceneName=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var opening=OpeningStoryUI.Instance;
if(opening!=null)opening.Skip();
var shop=UnityEngine.Object.FindFirstObjectByType<CosmeticShopUI>(UnityEngine.FindObjectsInactive.Include);
var entry=canvas.transform.Find("UI/Main/Bottom/Skin_Button").GetComponent<UnityEngine.UI.Button>();
int originalCoin=MoneyScript.S.Coin,originalJewel=MoneyScript.S.Jewel;
var item=CosmeticTables.Rows.Single(r=>r.visualKey=="shoes_steel");
entry.onClick.Invoke();
canvas.PlayerPressedStartButton();
report["shopBlocksStart"]=!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning;
MapToolCurrencyCheats.Grant(100,100);
shop.SelectSlot(CosmeticSlot.Shoes);shop.SelectItem(item.id);
int phase=0;double ready=UnityEditor.EditorApplication.timeSinceStartup+.5,deadline=ready+40;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    if(!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=tick;return;}
    double now=UnityEditor.EditorApplication.timeSinceStartup;if(now<ready)return;
    try
    {
        if(now>deadline)throw new System.TimeoutException("UI verification phase "+phase);
        switch(phase)
        {
            case 0:
                report["grantCoin"]=MoneyScript.S.Coin-originalCoin;report["grantJewels"]=MoneyScript.S.Jewel-originalJewel;
                if(!shop.actionButton.interactable)throw new System.InvalidOperationException("Purchase button not interactive.");
                shop.actionButton.onClick.Invoke();phase++;ready=now+.5;break;
            case 1:
                report["purchased"]=CosmeticService.Current.Owns(item.id);report["coinUnspent"]=MoneyScript.S.Coin==originalCoin+100;
                report["jewelCost"]=originalJewel+100-MoneyScript.S.Jewel;
                UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/shop-purchased.png");
                shop.SelectItem(CosmeticTables.Rows.Single(r=>r.slot==CosmeticSlot.Shoes&&r.isDefault).id);phase++;ready=now+.3;break;
            case 2:
                shop.actionButton.onClick.Invoke();shop.SelectItem(item.id);phase++;ready=now+.3;break;
            case 3:
                shop.actionButton.onClick.Invoke();report["reequipNoCharge"]=MoneyScript.S.Jewel==originalJewel+100-item.price;
                shop.transform.Find("Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                report["lobbyRestored"]=canvas.transform.Find("UI/Main/Bottom").gameObject.activeInHierarchy && canvas.IsStartAreaActive();
                canvas.transform.Find("UI/Main/Bottom/Story_Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                opening=OpeningStoryUI.Instance;phase++;ready=now+.4;break;
            case 4:
                if(opening==null || !opening.IsMoviePlaying || opening.video.frame<5)return;
                report["moviePlaying"]=true;report["movieFrame"]=opening.video.frame;
                canvas.PlayerPressedStartButton();report["movieBlocksStart"]=!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning;
                UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/story-playing.png");opening.Next();phase++;ready=now+.6;break;
            case 5:
                if(opening.CurrentPage!=1 || opening.video.time<opening.video.length*.24)return;
                report["seekPage"]=opening.CurrentPage;report["seekTime"]=opening.video.time;
                UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/story-seek.png");
                for(int i=0;i<5;i++){opening.ReplayMovie();opening.Skip();opening.Open();}
                opening.Skip();report["skipReleasedTarget"]=opening.video.targetTexture==null&&opening.movieDisplay.texture==null&&!opening.video.isPlaying;
                phase++;ready=now+.5;break;
            case 6:
                System.IO.File.WriteAllText(folder+"/before-reload.json",(string)serialize.Invoke(null,new object[]{report}));
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);phase++;ready=now+1.5;break;
            case 7:
                if(MoneyScript.S==null)return;
                CosmeticService.ReloadCatalog();
                report["savedOwnership"]=CosmeticService.Current.Owns(item.id);
                report["savedEquipment"]=CosmeticService.Current.Equipped(CosmeticSlot.Shoes).id==item.id;
                report["savedWallet"]=MoneyScript.S.Coin==originalCoin+100&&MoneyScript.S.Jewel==originalJewel+100-item.price;
                report["expectedPrice"]=item.price;
                if(OpeningStoryUI.Instance!=null)OpeningStoryUI.Instance.Skip();
                System.IO.File.WriteAllText(folder+"/result.json",(string)serialize.Invoke(null,new object[]{report}));
                UnityEditor.EditorApplication.update-=tick;break;
        }
    }
    catch(System.Exception error)
    {
        UnityEditor.EditorApplication.update-=tick;
        System.IO.File.WriteAllText(folder+"/error.txt",error.ToString());
    }
};
UnityEditor.EditorApplication.update+=tick;
return new{folder,item.id,item.price,originalCoin,originalJewel};
