if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var canvas=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
string folder="map-concepts/shop-fidelity-2026-09-17/verification";System.IO.Directory.CreateDirectory(folder);string report=folder+"/interactions.txt";System.IO.File.WriteAllText(report,"");
void Check(bool ok,string label){System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+label+"\n");if(!ok)throw new Exception(label);}
System.Collections.IEnumerator Verify(){
 canvas.GetComponentInChildren<OpeningStoryUI>(true).Skip();IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=false;
 var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);shop.Open();shop.SelectSlot(CosmeticSlot.Skin);shop.SelectItem("skin_coral");yield return new WaitForSecondsRealtime(.3f);
 bool owned=CosmeticService.Current.Owns("skin_coral");int equippedBefore=MoneyScript.S.Jewel;
 if(!owned){shop.actionButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);Check(MoneyScript.S.Jewel==equippedBefore&&!CosmeticService.Current.Owns("skin_coral"),"Insufficient gems do not buy or debit");Check(shop.messagePanel.gameObject.activeSelf,"Insufficient gems explain required amount");shop.messagePanel.Close();}
 MoneyScript.S.Jewel=10000;yield return new WaitForSecondsRealtime(.2f);int gems=MoneyScript.S.Jewel;
 shop.actionButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
 Check(CosmeticService.Current.Equipped(CosmeticSlot.Skin).id=="skin_coral","Purchase equips actual selected skin");
 Check(gems-MoneyScript.S.Jewel==(owned?0:CosmeticService.Current.Find("skin_coral").price),"Exactly one table-price cosmetic debit");
 Check(shop.selectionName.text=="현재 착용 중"&&!shop.actionButton.interactable,"Equipped badge and disabled action refresh");
 var selected=shop.itemsRoot.Find("skin_coral");Check(selected.Find("EquippedCheck").gameObject.activeSelf&&selected.Find("Selected").gameObject.activeSelf,"Equipped check and selected border refresh independently");
 Check(selected.GetComponent<UnityEngine.UI.Image>().color==Color.white,"Runtime refresh preserves authored frame color");
 gems=MoneyScript.S.Jewel;shop.actionButton.onClick.Invoke();yield return null;Check(gems==MoneyScript.S.Jewel,"Equipped action never double-charges");
 shop.preview.Rotate(45);yield return new WaitForSecondsRealtime(.2f);shop.preview.ResetView();shop.Close();yield return null;shop.Open();yield return new WaitForSecondsRealtime(.3f);
 Check(shop.preview.Texture!=null&&shop.preview.Texture.IsCreated(),"Real model preview reopens after resource disposal");shop.Close();
 MoneyScript.S.Coin=100000;canvas.transform.Find("UI/Main/Bottom/Upgrade_Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);
 var upgrade=canvas.transform.Find("UI/Upgrade2");var regular=upgrade.GetComponentsInChildren<UpgradeUI>().Single(c=>c.UpgradeId==1);
 int level=PlayerPrefs.GetInt("upgrade_lv_1");UpgradeTables.TryGet(1,level+1,out var next);int coins=MoneyScript.S.Coin;
 regular.transform.Find("Down").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
 Check(PlayerPrefs.GetInt("upgrade_lv_1")==level+1&&coins-MoneyScript.S.Coin==next.price,"Regular row retains table price and advances one level");
 var tabs=upgrade.GetComponent<HarborUpgradeTabs>();tabs.ShowChapters();PlayerPrefs.SetInt("chapter_unlocked",1);
 var chapter=upgrade.GetComponentsInChildren<ChapterUpgradeCardUI>().Single(c=>c.chapter==1);
 PlayerPrefs.SetInt(ChapterUpgradeService.LevelKey(1),0);chapter.Refresh();int expected=ChapterUpgradeService.Catalog.entries.Single(c=>c.chapter==1).CostAtLevel(0);coins=MoneyScript.S.Coin;
 chapter.buyButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
 Check(ChapterUpgradeService.Level(1)==1&&coins-MoneyScript.S.Coin==expected,"Chapter buys one rank at catalog cost");
 Check(chapter.rankPips.Count(p=>p.sprite==chapter.filledPip)==1&&chapter.effects.text.Contains("+5%"),"One filled rank and next five-percent gain");
 var locked=upgrade.GetComponentsInChildren<ChapterUpgradeCardUI>().Single(c=>c.chapter==2);locked.Refresh();coins=MoneyScript.S.Coin;locked.Buy();
 Check(MoneyScript.S.Coin==coins&&!locked.buyButton.gameObject.activeSelf&&locked.lockedText.gameObject.activeSelf,"Locked chapter cannot spend and displays availability");
 Check(locked.rankPips.All(p=>p.gameObject.activeSelf&&p.sprite==locked.emptyPip),"Locked rank slots remain hollow and visible");
 PlayerPrefs.SetInt(ChapterUpgradeService.LevelKey(1),5);chapter.Refresh();coins=MoneyScript.S.Coin;chapter.Buy();
 Check(MoneyScript.S.Coin==coins&&chapter.priceLabel.text=="MAX"&&!chapter.buyButton.interactable&&chapter.rankPips.All(p=>p.sprite==chapter.filledPip),"Five-rank cap displays MAX and rejects a sixth purchase");
 upgrade.gameObject.SetActive(false);System.IO.File.AppendAllText(report,"COMPLETE\n");
}
canvas.StartCoroutine(Verify());return report;
