var types=System.AppDomain.CurrentDomain.GetAssemblies();
types.Select(a=>a.GetType("WearableAssetImporter")).First(t=>t!=null).GetMethod("BuildAvailable").Invoke(null,null);
types.Select(a=>a.GetType("EquipmentIconRenderer")).First(t=>t!=null).GetMethod("Build").Invoke(null,null);
types.Select(a=>a.GetType("CosmeticPresentationBuilder")).First(t=>t!=null).GetMethod("BuildShops").Invoke(null,null);
return "Wearables, item icons and shops updated";
