if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required");OpeningStoryUI.Instance?.Skip();
var shop=UnityEngine.Object.FindFirstObjectByType<CosmeticShopUI>(UnityEngine.FindObjectsInactive.Include);shop.Open();shop.SelectSlot(CosmeticSlot.Shoes);shop.SelectItem("shoes_ruby");shop.Refresh();return shop.preview.Texture!=null;
