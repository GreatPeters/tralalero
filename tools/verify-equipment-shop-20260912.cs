if(!UnityEditor.EditorApplication.isPlaying||IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)throw new System.InvalidOperationException("Lobby Play Mode required");
const string folder="tmp/image-previews/skins-reststop-2026-09-12/shop-verified";if(System.IO.Directory.Exists(folder))throw new System.InvalidOperationException("Preserve earlier UI proof");System.IO.Directory.CreateDirectory(folder);
OpeningStoryUI.Instance?.Skip();var shop=UnityEngine.Object.FindFirstObjectByType<CosmeticShopUI>(UnityEngine.FindObjectsInactive.Include);var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var item=CosmeticTables.Rows.First(r=>r.slot==CosmeticSlot.Shoes&&!r.isDefault&&!CosmeticService.Current.Owns(r.id));
var defaults=CosmeticTables.Rows.Single(r=>r.slot==CosmeticSlot.Shoes&&r.isDefault);int oldCoins=MoneyScript.S.Coin,oldGems=MoneyScript.S.Jewel;string scene=shop.gameObject.scene.name;
var report=new System.Collections.Generic.Dictionary<string,object>();int phase=0;double next=UnityEditor.EditorApplication.timeSinceStartup+.5,deadline=next+70;int[] cards=null;float scrollPosition=0;
shop.Open();shop.SelectSlot(CosmeticSlot.Shoes);shop.SelectItem(item.id);
UnityEditor.EditorApplication.CallbackFunction tick=null;
void Check(bool value,string message){if(!value)throw new System.InvalidOperationException(message);}
tick=()=>
{
 double now=UnityEditor.EditorApplication.timeSinceStartup;if(now<next)return;
 try
 {
  if(!UnityEditor.EditorApplication.isPlaying||now>deadline)throw new System.TimeoutException("Shop verification phase "+phase);
  switch(phase)
  {
   case 0:
    Check(shop.preview.Texture!=null&&shop.preview.Texture.IsCreated(),"First-open preview missing");canvas.PlayerPressedStartButton();Check(!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning,"Shop did not block start");
    cards=shop.itemsRoot.Cast<UnityEngine.Transform>().Where(t=>t.gameObject.activeSelf).Select(t=>t.gameObject.GetInstanceID()).ToArray();var scroll=shop.itemsRoot.GetComponentInParent<UnityEngine.UI.ScrollRect>();scroll.verticalNormalizedPosition=.35f;scroll.StopMovement();scrollPosition=scroll.verticalNormalizedPosition;
    shop.SelectItem(CosmeticTables.Rows.First(r=>r.slot==CosmeticSlot.Shoes&&r.id!=item.id).id);shop.Refresh();phase++;next=now+.3;break;
   case 1:
    Check(cards.SequenceEqual(shop.itemsRoot.Cast<UnityEngine.Transform>().Where(t=>t.gameObject.activeSelf).Select(t=>t.gameObject.GetInstanceID())),"Selection recreated cards");Check(UnityEngine.Mathf.Abs(shop.itemsRoot.GetComponentInParent<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition-scrollPosition)<.02f,"Selection moved scroll");
    MapToolCurrencyCheats.Grant(100,item.price+20);shop.SelectItem(item.id);shop.Refresh();Check(shop.actionButton.interactable,"Funded purchase disabled");shop.actionButton.onClick.Invoke();phase++;next=now+.5;break;
   case 2:
    Check(CosmeticService.Current.Owns(item.id)&&CosmeticService.Current.Equipped(CosmeticSlot.Shoes).id==item.id,"Purchase did not equip");Check(MoneyScript.S.Jewel==oldGems+20&&MoneyScript.S.Coin==oldCoins+100,"Currency debit wrong");Check(!shop.actionButton.interactable,"Equipped action enabled");
    shop.itemsRoot.GetComponentInParent<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition=1;UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/shoes-purchased.png");shop.SelectItem(defaults.id);shop.Refresh();shop.actionButton.onClick.Invoke();phase++;next=now+.4;break;
   case 3:
    shop.SelectItem(item.id);shop.Refresh();shop.actionButton.onClick.Invoke();Check(MoneyScript.S.Jewel==oldGems+20,"Re-equip charged again");shop.SelectSlot(CosmeticSlot.Skin);phase++;next=now+.4;break;
   case 4:
    shop.SelectItem(CosmeticTables.Rows.Single(r=>r.visualKey=="skin_armor").id);shop.Refresh();shop.preview.Rotate(30);UnityEngine.ScreenCapture.CaptureScreenshot(folder+"/skin-preview.png");Check(CosmeticService.Current.Equipped(CosmeticSlot.Shoes).id==item.id,"Slot preview changed equipment");
    for(int i=0;i<5;i++){shop.Close();shop.Open();}phase++;next=now+.5;break;
   case 5:
    Check(shop.preview.Texture!=null&&shop.preview.Texture.IsCreated(),"Reopened preview missing");report["cardCount"]=cards.Length;report["purchasePrice"]=item.price;report["startBlocked"]=true;report["selectionKeepsCardsAndScroll"]=true;report["singleDebitAndReequip"]=true;report["reopenPreview"]=true;shop.Close();Check(canvas.IsStartAreaActive(),"Lobby start area not restored");UnityEngine.SceneManagement.SceneManager.LoadScene(scene);phase++;next=now+2;break;
   case 6:
    if(MoneyScript.S==null)return;OpeningStoryUI.Instance?.Skip();CosmeticService.ReloadCatalog();Check(CosmeticService.Current.Owns(item.id)&&CosmeticService.Current.Equipped(CosmeticSlot.Shoes).id==item.id,"Ownership/equip not persisted");Check(MoneyScript.S.Jewel==oldGems+20&&MoneyScript.S.Coin==oldCoins+100,"Wallet not persisted");report["reloadPersistence"]=true;
    var serializer=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});System.IO.File.WriteAllText(folder+"/result.json",(string)serializer.Invoke(null,new object[]{report}));UnityEditor.EditorApplication.update-=tick;break;
  }
 }
 catch(System.Exception error){UnityEditor.EditorApplication.update-=tick;System.IO.File.WriteAllText(folder+"/error.txt",error.ToString());}
};
UnityEditor.EditorApplication.update+=tick;return new{folder,item=item.id,price=item.price};
