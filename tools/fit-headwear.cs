var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("WearableAssetImporter")).First(t=>t!=null);return type.GetMethod("FitHeadwear").Invoke(null,null);
