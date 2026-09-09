try { return AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NoryangjinSr18CombatPolish")).First(t=>t!=null).GetMethod("Apply").Invoke(null,null); }
catch(Exception error) { return error.InnerException?.ToString() ?? error.ToString(); }
