if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
var window=UnityEngine.ScriptableObject.CreateInstance<NoryangjinMapToolWindow>();
try{
 var method=typeof(NoryangjinMapToolWindow).GetMethod("GetPaletteItems",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
 var counts=new System.Collections.Generic.List<int>();var pathsByConcept=new System.Collections.Generic.List<string[]>();
 for(int concept=0;concept<3;concept++){
  window.SetPaletteConcept(concept);var items=(System.Collections.IEnumerable)method.Invoke(window,null);
  var paths=items.Cast<object>().Select(item=>(string)item.GetType().GetProperty("PrefabPath").GetValue(item)).ToArray();counts.Add(paths.Length);pathsByConcept.Add(paths);
 }
 var rest=pathsByConcept[2];var models=rest.Where(p=>p!=null&&p.Contains("/RestStop/")).ToArray();
 if(models.Length!=8)throw new System.InvalidOperationException("Expected8 rest-stop assemblies, found"+models.Length);
 foreach(string furniture in new[]{"table_001.prefab","bench_001.prefab","tree_012.prefab"})if(!rest.Any(p=>p!=null&&p.EndsWith(furniture)))throw new System.InvalidOperationException("Missing furniture "+furniture);
 if(rest.Any(p=>p!=null&&p.EndsWith("HWY_051.prefab")))throw new System.InvalidOperationException("Unrelated construction prop in rest-stop palette");
 if(!pathsByConcept[1].Any(p=>p!=null&&p.EndsWith("HWY_051.prefab")))throw new System.InvalidOperationException("Highway palette lost construction prop");
 if(pathsByConcept[0].Any(p=>p!=null&&p.Contains("/Highway/RestStop/")))throw new System.InvalidOperationException("Noryangjin palette drifted");
 var serializer=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
 var json=(string)serializer.Invoke(null,new object[]{new{counts,models,furniture=3,threeConceptsSwitch=true}});
 System.IO.File.WriteAllText("map-concepts/skins-reststop-2026-09-12/palette-verified.json",json);return json;
}finally{UnityEngine.Object.DestroyImmediate(window);}
