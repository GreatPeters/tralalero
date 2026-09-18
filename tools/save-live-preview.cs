var shop=UnityEngine.Object.FindFirstObjectByType<CosmeticShopUI>(UnityEngine.FindObjectsInactive.Include);
CosmeticPresentationBuilder.Save(shop.preview.Texture,"map-concepts/skins-reststop-2026-09-12/live-preview.png");
return shop.preview.Texture.antiAliasing;
