if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required");
OpeningStoryUI.Instance?.Skip();
var shop=UnityEngine.Object.FindFirstObjectByType<CosmeticShopUI>(UnityEngine.FindObjectsInactive.Include);
shop.Open();shop.SelectSlot(CosmeticSlot.Shoes);shop.SelectItem(CosmeticTables.Rows.Single(r=>r.visualKey=="shoes_mint").id);shop.Refresh();
return new{scene=shop.gameObject.scene.name,columns=shop.itemsRoot.GetComponent<UnityEngine.UI.GridLayoutGroup>().constraintCount};
