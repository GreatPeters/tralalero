var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CosmeticPresentationBuilder")).First(t=>t!=null);
type.GetMethod("BuildShops").Invoke(null,null);
type.GetMethod("CaptureFitting").Invoke(null,null);
return "Shop and fitting captures complete";
