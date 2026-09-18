var shop=UnityEngine.Object.FindFirstObjectByType<CosmeticShopUI>(UnityEngine.FindObjectsInactive.Include);
shop.preview.Rotate(0);
return new{active=shop.gameObject.activeInHierarchy,component=shop.preview.enabled,texture=shop.preview.Texture==null?"null":shop.preview.Texture.name,display=shop.preview.display.texture==null?"null":shop.preview.display.texture.name,created=shop.preview.Texture!=null&&shop.preview.Texture.IsCreated()};
