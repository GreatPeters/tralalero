var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("RestStopAssetImporter")).First(t=>t!=null);return type.GetMethod("BuildAvailable").Invoke(null,null);
