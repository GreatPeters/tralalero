var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("DiverHelmetAperture")).First(t=>t!=null);type.GetMethod("ApplyToCatalog").Invoke(null,null);
var fitting=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("WearableAssetImporter")).First(t=>t!=null);return fitting.GetMethod("FitHeadwear").Invoke(null,null);
