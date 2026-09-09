// One-shot authored-scene installation. Keeps existing road/scenery instances untouched.
// The data calls below are generated from the SHA-256-pinned full-stage proposal.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
const string scenePath="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
const string proposal="map-concepts/sr18-full-enemy-bonus-flow-2026-09-06/full-flow-final.json";
const string records="map-concepts/sr18-encounters-applied-2026-09-06";
string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=System.IO.File.OpenRead(path))return System.BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
if(Hash(proposal)!="2B595E936A51954B1F7750EB6075F3C2F9E7544DD0DB7181DA124F4A781E1EC5")throw new System.InvalidOperationException("Proposal changed; regenerate placement calls before applying");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
if(!scene.IsValid()||!scene.isLoaded||UnityEngine.SceneManagement.SceneManager.GetActiveScene()!=scene)throw new System.InvalidOperationException("Open and select the exact SR18 scene first");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var enemies=map.Find("Enemies");var bonuses=map.Find("Bonuses");var roads=map.Find("Roads");var props=map.Find("Props");
if(roads.childCount!=230||props.childCount!=666||enemies.childCount!=0||bonuses.childCount!=0||map.Find("SR18_EnemyTargets")!=null)throw new System.InvalidOperationException("Source layout differs or encounters already exist; refusing replacement");
var defaults=UnityEditor.AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>("Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset");
var entries=(System.Collections.Generic.List<NoryangjinMapToolPalettePlacementEntry>)typeof(NoryangjinMapToolPaletteDefaults).GetField("entries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(defaults);
var protectedRoots=roads.Cast<Transform>().Concat(props.Cast<Transform>()).Concat(map.Find("Water").Cast<Transform>()).ToArray();
var protectedComponents=protectedRoots.SelectMany(t=>t.GetComponentsInChildren<Component>(true)).Where(c=>c!=null).Distinct().ToDictionary(c=>c,c=>UnityEditor.EditorJsonUtility.ToJson(c));
var protectedObjects=protectedRoots.SelectMany(t=>t.GetComponentsInChildren<Transform>(true)).ToDictionary(t=>t.gameObject,t=>UnityEditor.EditorJsonUtility.ToJson(t.gameObject));
// Rough flat planks have slanted individual triangles; classify ramps by their authored root.
var sourceRoadColliders=roads.Cast<Transform>().Where(t=>!t.name.EndsWith("_Uphill")&&!t.name.EndsWith("_Downhill")).SelectMany(t=>t.GetComponentsInChildren<MeshCollider>(true)).ToArray();
string sourceHash=Hash(scenePath);bool initialDirty=scene.isDirty;
string backupFolder="tmp/backups/sr18-encounters-2026-09-06/"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
System.IO.Directory.CreateDirectory(backupFolder);System.IO.Directory.CreateDirectory(records);
string backup=backupFolder+"/before-encounters.unity";
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,backup,true))throw new System.InvalidOperationException("Backup failed");
if(System.IO.File.Exists(records+"/placement-report.json"))throw new System.InvalidOperationException("An installation report already exists; inspect it rather than overwrite");
var created=new System.Collections.Generic.List<GameObject>();var actorRows=new System.Collections.Generic.List<object>();var spotRows=new System.Collections.Generic.List<object>();
var actors=new System.Collections.Generic.List<IndianOceanAssets.ShooterSurvival.EnemyEventController>();
var walls=new System.Collections.Generic.List<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>();
var spots=new System.Collections.Generic.List<IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot>();
var surfacePlacements=new System.Collections.Generic.List<(Transform Root,float ExpectedHeight)>();
var flatTargets=new System.Collections.Generic.List<Transform>();
UnityEditor.Undo.IncrementCurrentGroup();int undoGroup=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Apply SR18 enemy and BonusWall layout");
Vector3 Axis(Vector3 a,Vector3 b){Vector3 d=b-a;return Mathf.Abs(d.x)>Mathf.Abs(d.z)?new Vector3(Mathf.Sign(d.x),0,0):new Vector3(0,0,Mathf.Sign(d.z));}
float Surface(Vector3 p,float expected){
 float best=float.NegativeInfinity;
 foreach(var offset in new[]{Vector3.zero,Vector3.forward*.12f,Vector3.back*.12f,Vector3.right*.12f,Vector3.left*.12f}){
  var ray=new Ray(new Vector3(p.x+offset.x,expected+4,p.z+offset.z),Vector3.down);
  foreach(var c in sourceRoadColliders){RaycastHit hit;if(c.Raycast(ray,out hit,8)&&Mathf.Abs(hit.point.y-expected)<1.1f&&hit.normal.y>.7f)best=Mathf.Max(best,hit.point.y);}
  if(!float.IsNegativeInfinity(best))return best;
 }
 throw new System.InvalidOperationException("No matching flat road beneath "+p+" at expected level "+expected);
}
GameObject NewRoot(string name,Transform parent){var go=new GameObject(name);UnityEditor.Undo.RegisterCreatedObjectUndo(go,"Create "+name);go.transform.SetParent(parent,false);created.Add(go);return go;}
GameObject Place(string path,string name,Transform parent,Vector3 p,float yaw,float level){
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new System.InvalidOperationException("Missing prefab "+path);
 var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,parent);UnityEditor.Undo.RegisterCreatedObjectUndo(go,"Place "+name);created.Add(go);go.name=name;
 var entry=entries.Single(x=>x.prefabPath==path);go.transform.localScale=Vector3.Scale(prefab.transform.localScale,entry.scale);
 p.y=Surface(p,level)+entry.heightOffset+.08f;go.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
 UnityEditor.GameObjectUtility.SetStaticEditorFlags(go,0);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
 surfacePlacements.Add((go.transform,level));return go;
}
Transform targetsRoot=null;
void Spot(string id,Vector3 p,Vector3 dir,float level,IndianOceanAssets.ShooterSurvival.EnemyEventController[] links){
 var go=Place("Assets/ShooterSurvival/Prefabs/Gameplay/Noryangjin_EnemyMovementTrigger.prefab","SR18_"+id+"_Activation",props,p,Quaternion.LookRotation(dir).eulerAngles.y,level);
 // Keep the normal palette transform; express an 8.8 x 2 x 1.2 world-space gate.
 var box=go.GetComponent<BoxCollider>();box.center=new Vector3(0,1,0);box.size=new Vector3(4,2,.8f);box.isTrigger=true;
 var spot=go.GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot>();spot.Targets=links;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(spot);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(box);
 spots.Add(spot);spotRows.Add(new{id,name=go.name,position=V(go.transform.position),targets=links.Select(e=>e.name).ToArray()});
}
void Group(string id,Vector3 start,Vector3 end,Vector3 center,string[] kinds,bool stagger,float level){
 Vector3 dir=Axis(start,end),right=Vector3.Cross(Vector3.up,dir);float yaw=Quaternion.LookRotation(dir).eulerAngles.y;
 int rowSize=stagger&&kinds.Length==2?1:2;int rowCount=(kinds.Length+rowSize-1)/rowSize;
 var links=new System.Collections.Generic.List<IndianOceanAssets.ShooterSurvival.EnemyEventController>();
 for(int i=0;i<kinds.Length;i++){
  int row=i/rowSize;int countInRow=Mathf.Min(rowSize,kinds.Length-row*rowSize);float along=(row-(rowCount-1)*.5f)*(stagger?9:5);
  float side=countInRow==1?0:(i%rowSize==0?-1.45f:1.45f);Vector3 pos=center+dir*along+right*side;
  string kind=kinds[i],path="Assets/JH/Model/Prefab/"+kind+".prefab";
  var go=Place(path,"SR18_"+id+"_"+(i+1).ToString("00")+"_"+kind,enemies,pos,yaw,level);
  var controller=go.GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventController>();if(controller==null)throw new System.InvalidOperationException("Missing event controller on "+go.name);
  bool moving=(kind=="Enemy_YllowMan_Sword"&&id!="E15")||(kind=="Enemy_FatMan"&&id=="E20");
  var serialized=new UnityEditor.SerializedObject(controller);
  int mode=moving?2:(kind=="Enemy_Guard"||kind=="Enemy_FatMan")?4:id=="E01"?5:0;
  serialized.FindProperty("eventMode").intValue=mode;
  if(moving){var target=NewRoot(go.name+"_Target",targetsRoot).transform;Vector3 point=pos-dir*4;point.y=Surface(point,level)+.08f;target.position=point;flatTargets.Add(target);serialized.FindProperty("targetPoint").objectReferenceValue=target;serialized.FindProperty("moveSpeed").floatValue=kind=="Enemy_YllowMan_Sword"?4:2;serialized.FindProperty("moveAnimation").intValue=kind=="Enemy_YllowMan_Sword"?1:0;}
  serialized.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(controller);actors.Add(controller);links.Add(controller);
  actorRows.Add(new{id,kind,name=go.name,position=V(go.transform.position),scale=V(go.transform.localScale),yaw,mode,target=controller.TargetPoint==null?null:V(controller.TargetPoint.position)});
 }
 float centerDistance=Vector3.Dot(center-start,dir);
 if(stagger){for(int row=0;row<rowCount;row++){float rowOffset=(row-(rowCount-1)*.5f)*9;float distance=Mathf.Max(7,centerDistance+rowOffset-18);Spot(id+"_"+(row+1),start+dir*distance,dir,level,links.Skip(row*rowSize).Take(rowSize).ToArray());}}
 else Spot(id,start+dir*Mathf.Max(7,centerDistance-18),dir,level,links.ToArray());
}
void Bonus(string id,Vector3 start,Vector3 end,Vector3 center,string[] grades){
 Vector3 dir=Axis(start,end),right=Vector3.Cross(Vector3.up,dir);float yaw=Quaternion.LookRotation(dir).eulerAngles.y+180;
 for(int i=0;i<grades.Length;i++){
  Vector3 point=center+right*(grades.Length==1?0:(i==0?-2.2f:2.2f));
  var go=Place("Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab","SR18_"+id+"_"+(i+1)+"_"+grades[i],bonuses,point,yaw,0);
  var altar=go.GetComponent<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>()??UnityEditor.Undo.AddComponent<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>(go);
  altar.Configure(grades[i]=="U"?IndianOceanAssets.ShooterSurvival.Rarity.Unique:grades[i]=="E"?IndianOceanAssets.ShooterSurvival.Rarity.Rare:IndianOceanAssets.ShooterSurvival.Rarity.Normal);
  foreach(var wall in go.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.WallScript>(true)){var marker=wall.GetComponent<RuntimeBonusWall>()??UnityEditor.Undo.AddComponent<RuntimeBonusWall>(wall.gameObject);marker.KeepAsMapAuthoredWall();UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(wall);}
  UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(altar);walls.Add(altar);actorRows.Add(new{id,kind="BonusWall",grade=grades[i],name=go.name,position=V(go.transform.position),scale=V(go.transform.localScale),yaw});
 }
}
bool saved=false;
try{
 targetsRoot=NewRoot("SR18_EnemyTargets",map).transform;
 Group("E01",new Vector3(27.0900f,0.0000f,-115.1100f),new Vector3(-10.8000f,0.0000f,-114.7500f),new Vector3(8.9030f,0.0000f,-114.9370f),new[]{"Enemy_OldMan"},false,0f);
 Group("E02",new Vector3(-10.8000f,0.0000f,-114.7500f),new Vector3(-10.5750f,0.0000f,-7.2000f),new Vector3(-10.6600f,0.0000f,-48.0690f),new[]{"Enemy_YllowMan_Net","Enemy_YllowMan_Net"},true,0f);
 Group("E03",new Vector3(-10.5750f,0.0000f,-7.2000f),new Vector3(191.7000f,0.0000f,-7.2000f),new Vector3(29.8800f,0.0000f,-7.2000f),new[]{"Enemy_OldMan","Enemy_OldMan"},false,0f);
 Group("E04",new Vector3(-10.5750f,0.0000f,-7.2000f),new Vector3(191.7000f,0.0000f,-7.2000f),new Vector3(149.2220f,0.0000f,-7.2000f),new[]{"Enemy_Guard","Enemy_YllowMan_Sword","Enemy_YllowMan_Sword"},false,0f);
 Bonus("F01",new Vector3(-10.5750f,0.0000f,-7.2000f),new Vector3(191.7000f,0.0000f,-7.2000f),new Vector3(94.6080f,0.0000f,-7.2000f),new[]{"N","N"});
 Group("E05",new Vector3(191.7000f,0.0000f,-7.2000f),new Vector3(191.7000f,0.0000f,53.5500f),new Vector3(191.7000f,0.0000f,21.9600f),new[]{"Enemy_YllowMan_Sword","Enemy_YllowMan_Sword"},false,0f);
 Group("E06",new Vector3(124.4250f,0.0000f,53.5500f),new Vector3(124.2499f,0.0000f,-347.7511f),new Vector3(124.3860f,0.0000f,-34.7360f),new[]{"Enemy_OldMan","Enemy_OldMan","Enemy_YllowMan_Sword"},false,0f);
 Group("E07",new Vector3(124.4250f,0.0000f,53.5500f),new Vector3(124.2499f,0.0000f,-347.7511f),new Vector3(124.3460f,0.0000f,-127.0360f),new[]{"Enemy_Guard","Enemy_YllowMan_Net","Enemy_YllowMan_Net"},false,0f);
 Group("E08",new Vector3(124.4250f,0.0000f,53.5500f),new Vector3(124.2499f,0.0000f,-347.7511f),new Vector3(124.2850f,0.0000f,-267.4910f),new[]{"Enemy_FatMan","Enemy_YllowMan_Sword","Enemy_YllowMan_Sword"},false,0f);
 Bonus("F02",new Vector3(124.4250f,0.0000f,53.5500f),new Vector3(124.2499f,0.0000f,-347.7511f),new Vector3(124.3160f,0.0000f,-195.2570f),new[]{"N","E"});
 Group("E09",new Vector3(124.2499f,0.0000f,-347.7511f),new Vector3(-67.0001f,0.0000f,-347.7511f),new Vector3(86.0000f,0.0000f,-347.7510f),new[]{"Enemy_YllowMan_Sword","Enemy_YllowMan_Sword","Enemy_Guard"},false,0f);
 Group("E10",new Vector3(124.2499f,0.0000f,-347.7511f),new Vector3(-67.0001f,0.0000f,-347.7511f),new Vector3(-23.0130f,0.0000f,-347.7510f),new[]{"Enemy_OldMan","Enemy_OldMan","Enemy_Guard"},true,0f);
 Bonus("F03",new Vector3(124.2499f,0.0000f,-347.7511f),new Vector3(-67.0001f,0.0000f,-347.7511f),new Vector3(32.4500f,0.0000f,-347.7510f),new[]{"N"});
 Group("E11",new Vector3(-67.0001f,0.0000f,-347.7511f),new Vector3(-67.0001f,0.0000f,-77.7511f),new Vector3(-67.0000f,0.0000f,-301.8510f),new[]{"Enemy_YllowMan_Net","Enemy_YllowMan_Net","Enemy_Guard"},false,0f);
 Group("E12",new Vector3(-67.0001f,0.0000f,-347.7511f),new Vector3(-67.0001f,0.0000f,-77.7511f),new Vector3(-67.0000f,0.0000f,-234.3510f),new[]{"Enemy_FatMan","Enemy_YllowMan_Sword","Enemy_YllowMan_Sword","Enemy_OldMan"},true,0f);
 Group("E13",new Vector3(-67.0001f,0.0000f,-347.7511f),new Vector3(-67.0001f,0.0000f,-77.7511f),new Vector3(-67.0000f,0.0000f,-131.7510f),new[]{"Enemy_Guard","Enemy_YllowMan_Sword","Enemy_YllowMan_Sword"},false,0f);
 Bonus("F04",new Vector3(-67.0001f,0.0000f,-347.7511f),new Vector3(-67.0001f,0.0000f,-77.7511f),new Vector3(-67.0000f,0.0000f,-177.6510f),new[]{"N","E"});
 Group("E14",new Vector3(-67.0001f,0.0000f,-77.7511f),new Vector3(225.4999f,0.0000f,-77.7511f),new Vector3(32.4500f,0.0000f,-77.7510f),new[]{"Enemy_OldMan","Enemy_OldMan"},false,12f);
 Group("E15",new Vector3(-67.0001f,0.0000f,-77.7511f),new Vector3(225.4999f,0.0000f,-77.7511f),new Vector3(93.8750f,0.0000f,-77.7510f),new[]{"Enemy_YllowMan_Sword","Enemy_YllowMan_Sword"},false,12f);
 Bonus("F05",new Vector3(-67.0001f,0.0000f,-77.7511f),new Vector3(225.4999f,0.0000f,-77.7511f),new Vector3(193.3250f,0.0000f,-77.7510f),new[]{"E"});
 Group("E16",new Vector3(225.4999f,0.0000f,-77.7511f),new Vector3(225.4999f,0.0000f,57.2489f),new Vector3(225.5000f,0.0000f,-50.7510f),new[]{"Enemy_Guard","Enemy_YllowMan_Sword","Enemy_YllowMan_Sword"},false,0f);
 Group("E17",new Vector3(225.4999f,0.0000f,-77.7511f),new Vector3(225.4999f,0.0000f,57.2489f),new Vector3(225.5000f,0.0000f,-12.9510f),new[]{"Enemy_FatMan","Enemy_YllowMan_Net","Enemy_YllowMan_Net"},false,0f);
 Bonus("F06",new Vector3(225.4999f,0.0000f,-77.7511f),new Vector3(225.4999f,0.0000f,57.2489f),new Vector3(225.5000f,0.0000f,28.8990f),new[]{"N","E"});
 Group("E18",new Vector3(225.4999f,0.0000f,57.2489f),new Vector3(326.7499f,0.0000f,57.2489f),new Vector3(276.1250f,0.0000f,57.2490f),new[]{"Enemy_Guard","Enemy_Guard","Enemy_YllowMan_Sword","Enemy_YllowMan_Sword"},true,0f);
 Group("E19",new Vector3(326.7499f,0.0000f,57.2489f),new Vector3(326.7499f,0.0000f,203.4989f),new Vector3(326.7500f,0.0000f,83.5740f),new[]{"Enemy_OldMan","Enemy_OldMan","Enemy_Guard"},false,0f);
 Group("E20",new Vector3(326.7499f,0.0000f,57.2489f),new Vector3(326.7499f,0.0000f,203.4989f),new Vector3(326.7500f,0.0000f,171.3240f),new[]{"Enemy_FatMan","Enemy_YllowMan_Sword","Enemy_YllowMan_Sword"},false,0f);
 Bonus("F07",new Vector3(326.7499f,0.0000f,57.2489f),new Vector3(326.7499f,0.0000f,203.4989f),new Vector3(326.7500f,0.0000f,111.3610f),new[]{"E","E"});
 Group("E21",new Vector3(326.7499f,0.0000f,203.4989f),new Vector3(439.2499f,0.0000f,203.4989f),new Vector3(380.7500f,0.0000f,203.4990f),new[]{"Enemy_Guard","Enemy_YllowMan_Sword","Enemy_YllowMan_Sword","Enemy_YllowMan_Net"},true,0f);
 Bonus("F08",new Vector3(439.2499f,0.0000f,203.4989f),new Vector3(439.2499f,0.0000f,135.9989f),new Vector3(439.2500f,0.0000f,169.7490f),new[]{"E"});
 Group("E22",new Vector3(439.2499f,0.0000f,135.9989f),new Vector3(247.9999f,0.0000f,135.9989f),new Vector3(402.9120f,0.0000f,135.9990f),new[]{"Enemy_Guard","Enemy_YllowMan_Sword"},false,0f);
 Group("E23",new Vector3(247.9999f,0.0000f,135.9989f),new Vector3(247.9999f,0.0000f,338.4989f),new Vector3(248.0000f,0.0000f,170.4240f),new[]{"Enemy_FatMan","Enemy_YllowMan_Sword","Enemy_YllowMan_Sword"},false,0f);
 Group("E24",new Vector3(247.9999f,0.0000f,135.9989f),new Vector3(247.9999f,0.0000f,338.4989f),new Vector3(248.0000f,0.0000f,223.0740f),new[]{"Enemy_Guard","Enemy_Guard","Enemy_YllowMan_Sword","Enemy_YllowMan_Sword"},true,0f);
 Group("E25",new Vector3(247.9999f,0.0000f,135.9989f),new Vector3(247.9999f,0.0000f,338.4989f),new Vector3(248.0000f,0.0000f,277.7490f),new[]{"Enemy_Woman"},false,0f);
 Bonus("F09",new Vector3(247.9999f,0.0000f,135.9989f),new Vector3(247.9999f,0.0000f,338.4989f),new Vector3(248.0000f,0.0000f,322.2990f),new[]{"U"});
 if(actors.Count!=69||walls.Count!=14||spots.Count!=31||flatTargets.Count!=27)throw new System.InvalidOperationException("Unexpected placement totals");
 foreach(var pair in protectedComponents)if(pair.Key==null||UnityEditor.EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new System.InvalidOperationException("Existing map component changed");
 foreach(var pair in protectedObjects)if(pair.Key==null||UnityEditor.EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new System.InvalidOperationException("Existing map object changed: "+(pair.Key==null?"destroyed":pair.Key.name)+" BEFORE "+pair.Value+" AFTER "+(pair.Key==null?"null":UnityEditor.EditorJsonUtility.ToJson(pair.Key)));
 foreach(var item in surfacePlacements)if(Mathf.Abs(item.Root.position.y-Surface(item.Root.position,item.ExpectedHeight)-.08f)>.01f)throw new System.InvalidOperationException("Incorrect authored height "+item.Root.name);
 if(actors.Any(e=>e.TargetPoint!=null&&e.TargetPoint.IsChildOf(e.transform)))throw new System.InvalidOperationException("Movement target is parented under enemy");
 if(spots.SelectMany(s=>s.Targets).Distinct().Count()!=69||spots.Sum(s=>s.Targets.Length)!=69)throw new System.InvalidOperationException("Every enemy must have exactly one activation link");
 Physics.SyncTransforms();
 var newEnemyColliders=actors.Select(a=>a.GetComponent<CapsuleCollider>()).ToArray();
 for(int i=0;i<newEnemyColliders.Length;i++)for(int j=i+1;j<newEnemyColliders.Length;j++)if(newEnemyColliders[i].bounds.Intersects(newEnemyColliders[j].bounds))throw new System.InvalidOperationException("Overlapping enemy bodies");
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
 if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.InvalidOperationException("Could not save SR18");
 saved=true;UnityEditor.Undo.CollapseUndoOperations(undoGroup);
 var asm=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");var serialize=asm.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
 var report=new{scene=scenePath,backup,initialDirty,sourceHash,afterHash=Hash(scenePath),proposal,enemyCount=actors.Count,bonusCount=walls.Count,activationSpotCount=spots.Count,movementTargets=flatTargets.Count,originalRoadCount=230,originalPropsPreserved=666,placements=actorRows,activationSpots=spotRows};
 System.IO.File.WriteAllText(records+"/placement-report.json",(string)serialize.Invoke(null,new object[]{report}));
 return new{applied=true,backup,enemies=actors.Count,bonuses=walls.Count,spots=spots.Count,movementTargets=flatTargets.Count,roadsPreserved=230,originalPropsPreserved=666};
}catch{if(!saved)UnityEditor.Undo.RevertAllDownToGroup(undoGroup);throw;}
