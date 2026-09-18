var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ChapterEncounterRebalance")).First(t=>t!=null);
try{type.GetMethod("BuildOpenScene").Invoke(null,null);return "Applied "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;}
catch(System.Exception error){System.IO.File.WriteAllText("map-concepts/skins-reststop-2026-09-12/encounter-layout-error.txt",error.ToString());throw;}
