if(!UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isPaused)throw new System.InvalidOperationException("Unpaused Play Mode required");OpeningStoryUI.Instance?.Skip();
var shop=UnityEngine.Object.FindFirstObjectByType<CosmeticShopUI>(UnityEngine.FindObjectsInactive.Include);shop.Open();
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();canvas.PlayerPressedStartButton();
if(IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)throw new System.InvalidOperationException("Shop did not block start");
foreach(CosmeticSlot slot in System.Enum.GetValues(typeof(CosmeticSlot))){shop.SelectSlot(slot);shop.Refresh();if(shop.itemsRoot.GetComponentsInChildren<UnityEngine.UI.Button>().Length!=8)throw new System.InvalidOperationException("Expected8 cards per slot");}
shop.SelectSlot(CosmeticSlot.Shoes);shop.SelectItem("shoes_ruby");shop.Refresh();
System.IO.File.WriteAllText("map-concepts/skins-reststop-2026-09-12/nory-shop-verified.json","{\"startBlocked\":true,\"cardsPerSlot\":8,\"gridColumns\":2,\"selectionAcrossThreeSlots\":true}");return "Noryangjin shop actions passed";
