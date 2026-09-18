var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("SharkSurfaceImporter")).First(t=>t!=null);type.GetMethod("RepairBodyNormals").Invoke(null,null);return "Normals updated";
