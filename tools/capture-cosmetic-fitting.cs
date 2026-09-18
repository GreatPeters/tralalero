var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CosmeticPresentationBuilder")).First(t=>t!=null);
return type.GetMethod("CaptureFitting").Invoke(null,null);
