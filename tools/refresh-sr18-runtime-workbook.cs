var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GameDataWorkbookEditor")).First(t=>t!=null);
var changed=type.GetMethod("EnsureRuntimeArchiveCurrent").Invoke(null,new object[]{true});
return new{archiveRefreshed=changed,rows=EncounterPlacementTables.Rows.Count};
