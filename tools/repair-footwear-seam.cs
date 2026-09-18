var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("SharkSurfaceImporter")).First(t=>t!=null);
return type.GetMethod("RepairFootwearSeam").Invoke(null,null);
