if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="HighWay")throw new System.InvalidOperationException("HighWay required");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var roads=map.Find("Roads");var props=map.Find("Props");var rest=props.Find("Highway_RestStop");
float[] V(UnityEngine.Vector3 v)=>new[]{v.x,v.y,v.z};
object Pose(UnityEngine.Transform t)=>new{name=t.name,position=V(t.position),euler=V(t.eulerAngles),scale=V(t.lossyScale)};
var route=roads.Cast<UnityEngine.Transform>().Where(t=>!t.name.StartsWith("Highway_RestStop")).Select(Pose).ToArray();
var encounters=map.Find("Enemies").Cast<UnityEngine.Transform>().Concat(map.Find("Bonuses").Cast<UnityEngine.Transform>()).Concat(props.Cast<UnityEngine.Transform>().Where(t=>t.GetComponentsInChildren<ObstacleStats>(true).Length>0)).OrderBy(t=>t.name).Select(Pose).ToArray();
var serializer=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
string Json(object value)=>(string)serializer.Invoke(null,new[]{value});
var snapshot=Json(new{route,encounters});const string folder="map-concepts/skins-reststop-2026-09-12";
if(rest==null){System.IO.File.WriteAllText(folder+"/before-reststop-validation.json",snapshot);return new{baseline=true,roads=route.Length,encounters=encounters.Length};}
var before=System.IO.File.ReadAllText(folder+"/before-reststop-validation.json");if(before!=snapshot)throw new System.InvalidOperationException("Existing route/encounter transforms changed");
if(rest.GetComponentsInChildren<UnityEngine.Collider>(true).Length!=0)throw new System.InvalidOperationException("Decorative props acquired collision");
UnityEngine.Physics.SyncTransforms();var probes=new System.Collections.Generic.List<object>();
void Floor(float x,float z){var hits=UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(x,12,z),UnityEngine.Vector3.down,20,UnityEngine.Physics.DefaultRaycastLayers,UnityEngine.QueryTriggerInteraction.Ignore).Where(h=>h.collider.transform.IsChildOf(roads)).ToArray();if(!hits.Any(h=>UnityEngine.Mathf.Abs(h.point.y-5)<.035f))throw new System.InvalidOperationException("Disconnected floor at"+x+","+z);probes.Add(new{x,z});}
for(float x=-150;x<=190;x+=10)Floor(x,220);
foreach(float x in new[]{-90f,70f})for(float z=202;z<=220;z+=1)Floor(x,z);
foreach(float x in new[]{-100f,-80f,-40f,0f,40f,65f})foreach(float z in new[]{132f,155f,185f,210f})Floor(x,z);
var assemblies=rest.Cast<UnityEngine.Transform>().Where(t=>t.name.StartsWith("reststop_")).Select(t=>new{name=t.name,bottom=HighwayAssetImporter.BoundsOf(t.gameObject).min.y,position=V(t.position),euler=V(t.eulerAngles)}).ToArray();
if(assemblies.Any(a=>a.bottom<4.98f||a.bottom>5.12f))throw new System.InvalidOperationException("Rest-stop assembly floating or sinking");
var labels=rest.GetComponentsInChildren<TMPro.TextMeshPro>(true);if(labels.Any(t=>t.font==null||t.text.Contains("□")))throw new System.InvalidOperationException("Missing sign font");
var result=new{routeAndEncountersPreserved=true,groundProbes=probes.Count,decorativeColliders=0,assemblies,signs=labels.Select(t=>t.text).ToArray()};
System.IO.File.WriteAllText(folder+"/reststop-scene-verified.json",Json(result));return result;
