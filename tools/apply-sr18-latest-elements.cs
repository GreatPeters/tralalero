var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NoryangjinSr18LatestEncounters")).First(t=>t!=null);
type.GetMethod("Apply").Invoke(null,null);
return "Applied latest SR18 elements; see map-concepts/sr18-latest-elements-applied-2026-09-07/placement-report.json";
