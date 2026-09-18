var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("RestStopSceneBuilder")).First(t=>t!=null);
try{return type.GetMethod("Build").Invoke(null,null);}catch(System.Reflection.TargetInvocationException error){throw error.InnerException??error;}
