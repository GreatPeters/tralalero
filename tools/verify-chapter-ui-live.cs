if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required");
const string folder="tmp/qa-proof/chapter-ui-final-v3";
if(System.IO.Directory.Exists(folder))throw new System.InvalidOperationException("Preserve evidence");
System.IO.Directory.CreateDirectory(folder);
UnityEditor.EditorApplication.isPaused=false;
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var opening=canvas.GetComponentInChildren<OpeningStoryUI>(true);
var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);
opening.Open();
int phase=0;double next=UnityEditor.EditorApplication.timeSinceStartup+.5,deadline=next+60;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 if(!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=tick;return;}
 double now=UnityEditor.EditorApplication.timeSinceStartup;if(now<next)return;
 try{
  if(now>deadline)throw new System.TimeoutException("UI phase "+phase);
  switch(phase){
   case 0:
    if(!opening.video.isPrepared||opening.video.time<.5)return;
    opening.video.Pause();UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/opening.png");phase++;next=now+.3;break;
   case 1: opening.Skip();shop.Open();shop.SelectSlot(CosmeticSlot.Shoes);phase++;next=now+1;break;
   case 2:
    UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/shoes.png");
    var scrollbar=shop.GetComponentInChildren<UnityEngine.UI.Scrollbar>(true);
    if(scrollbar==null||!scrollbar.gameObject.activeInHierarchy||scrollbar.size>=1)throw new System.InvalidOperationException("Catalog scrollbar is not visible for the longer list");
    phase++;next=now+.3;break;
   case 3: shop.SelectSlot(CosmeticSlot.Skin);phase++;next=now+1;break;
   case 4: UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/skins.png");phase++;next=now+.3;break;
   case 5:
    shop.Close();
    canvas.PlayerPressedStartButton();
    var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
    player.ApplyHarnessHealthDelta(-player.MaxHealth*2);
    phase++;next=now+1;break;
   case 6:
    if(!canvas.gameOverUI.activeInHierarchy)return;
    canvas.gameOverUI.GetComponent<DefeatPresentation>().SetResult(75,90,System.Guid.NewGuid().ToString("N"),70);
    phase++;next=now+.5;break;
   case 7:
    UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/defeat.png");
    var labels=canvas.GetComponentsInChildren<TMPro.TMP_Text>(true);
    var data=labels.Where(t=>t.gameObject.activeInHierarchy).Select(t=>{t.ForceMeshUpdate();return new {name=t.name,text=t.text,font=t.font.name,overflow=t.isTextOverflowing};}).ToArray();
    var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
    System.IO.File.WriteAllText(folder+"/labels.json",(string)serialize.Invoke(null,new object[]{data}));
    System.IO.File.WriteAllText(folder+"/complete.txt","Opening, two catalog grids and defeat captured in native portrait Play Mode.");
    UnityEditor.EditorApplication.update-=tick;break;
  }
 }catch(System.Exception e){System.IO.File.WriteAllText(folder+"/error.txt",e.ToString());UnityEditor.EditorApplication.update-=tick;}
};
UnityEditor.EditorApplication.update+=tick;return folder;
