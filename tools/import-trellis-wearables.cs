try
{
    var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("WearableAssetImporter")).First(t=>t!=null);
    var result=type.GetMethod("BuildAvailable").Invoke(null,null);
    return result;
}
catch(System.Exception error)
{
    System.IO.File.WriteAllText("map-concepts/skins-reststop-2026-09-12/wearable-import-error.txt",error.ToString());
    throw;
}
