if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var root=map.Find("Props/Highway_RestStop");
if(root==null||root.Find("Rear garden")!=null)throw new System.InvalidOperationException("Expected first rest-stop draft");
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"tmp/backups/skins-progression-2026-09-12/reststop-first-draft.unity",true);
foreach(var child in root.Cast<UnityEngine.Transform>().ToArray()){
 var point=child.position;
 if(child.name=="reststop_hall")point.z=173;
 else if(child.name=="reststop_restroom")point.z=174;
 else if(child.name=="reststop_picnic_shelter"||child.name.StartsWith("Rest-stop table_")||child.name.StartsWith("Rest-stop bench_"))point.z+=25;
 else if(child.name=="reststop_vending")point.z=177.6f;
 else if(child.name=="reststop_fuel_canopy")point.z=191;
 else if(child.name=="reststop_kiosk"){point=new UnityEngine.Vector3(53,5.075f,202);child.rotation=UnityEngine.Quaternion.Euler(0,-90,0);}
 else if(child.name=="Rest-stop light")point.z=174;
 child.position=point;
}
var forecourt=root.Find("Main forecourt");forecourt.position=new UnityEngine.Vector3(-20,5.035f,176.5f);forecourt.localScale=new UnityEngine.Vector3(160,.07f,5);
var concrete=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/ShooterSurvival/Models/Highway/Route/Concrete.mat");var earth=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/ShooterSurvival/Models/Highway/Route/Ground.mat");
void Surface(string name,UnityEngine.Vector3 position,UnityEngine.Vector3 size,UnityEngine.Material material){var obj=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(root,false);obj.transform.position=position;obj.transform.localScale=size;obj.GetComponent<UnityEngine.Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(obj.GetComponent<UnityEngine.Collider>());}
Surface("Rear garden",new UnityEngine.Vector3(-20,5.015f,146),new UnityEngine.Vector3(174,.03f,32),earth);
Surface("Garden path",new UnityEngine.Vector3(-20,5.035f,162),new UnityEngine.Vector3(174,.05f,2.5f),concrete);
Surface("Roadside cafe terrace",new UnityEngine.Vector3(53,5.035f,202),new UnityEngine.Vector3(9,.07f,9),concrete);
void Furniture(string key,string name,UnityEngine.Vector3 point,float width,float yaw){var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ithappy/Megacity/Prefabs/Props/"+key+".prefab");var obj=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,root);obj.name=name;var b=HighwayAssetImporter.BoundsOf(obj);obj.transform.localScale*=width/UnityEngine.Mathf.Max(b.size.x,b.size.z);obj.transform.rotation=UnityEngine.Quaternion.Euler(0,yaw,0);b=HighwayAssetImporter.BoundsOf(obj);obj.transform.position+=point-new UnityEngine.Vector3(b.center.x,b.min.y,b.center.z);foreach(var c in obj.GetComponentsInChildren<UnityEngine.Collider>(true))UnityEngine.Object.DestroyImmediate(c);}
for(int i=0;i<14;i++){
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ithappy/Megacity/Prefabs/Props/tree_012.prefab");var tree=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,root);tree.name="Rest-stop garden tree "+i;var b=HighwayAssetImporter.BoundsOf(tree);tree.transform.localScale*=(6.5f+i%3*.65f)/b.size.y;tree.transform.rotation=UnityEngine.Quaternion.Euler(0,i*47,0);b=HighwayAssetImporter.BoundsOf(tree);tree.transform.position+=new UnityEngine.Vector3(-99+i*12,5.03f,139+i%2*7)-new UnityEngine.Vector3(b.center.x,b.min.y,b.center.z);foreach(var c in tree.GetComponentsInChildren<UnityEngine.Collider>(true))UnityEngine.Object.DestroyImmediate(c);
}
Furniture("bench_001","Cafe bench",new UnityEngine.Vector3(50,5.075f,204.5f),2,90);Furniture("bench_001","Cafe bench",new UnityEngine.Vector3(50,5.075f,199.5f),2,90);
foreach(float x in new[]{-48f,4f,40f})Furniture("bench_001","Forecourt bench",new UnityEngine.Vector3(x,5.075f,175),2,0);
foreach(var component in root.GetComponentsInChildren<UnityEngine.Component>(true))if(component!=null&&UnityEditor.PrefabUtility.IsPartOfPrefabInstance(component))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component);
foreach(var t in root.GetComponentsInChildren<UnityEngine.Transform>(true))UnityEditor.GameObjectUtility.SetStaticEditorFlags(t.gameObject,UnityEditor.StaticEditorFlags.BatchingStatic|UnityEditor.StaticEditorFlags.OccludeeStatic);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return "Rest stop compacted; garden and roadside cafe added";
