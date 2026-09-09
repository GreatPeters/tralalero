var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NoryangjinSr18ChoiceLayout")).First(t=>t!=null);
return type.GetMethod("Apply").Invoke(null,null);
