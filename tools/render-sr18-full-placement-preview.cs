// Planning pictures only. Everything added lives in an isolated preview scene.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Planning capture requires Edit Mode; do not interrupt a running test.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;bool dirty=scene.isDirty;
string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())using(var file=System.IO.File.OpenRead(path))return System.BitConverter.ToString(sha.ComputeHash(file)).Replace("-","");}
string beforeHash=Hash(scene.path);
string output="tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/actual-map";string recordsFolder="map-concepts/sr18-full-enemy-bonus-flow-2026-09-06/actual-map";System.IO.Directory.CreateDirectory(output);System.IO.Directory.CreateDirectory(recordsFolder);
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var root=new GameObject("Encounter planning preview");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
var content=new GameObject("Annotations and examples");content.transform.SetParent(root.transform);
var mats=new System.Collections.Generic.List<Material>();
var placements=new System.Collections.Generic.List<object>();var texts=new System.Collections.Generic.List<(TextMesh Text,float Width)>();
var font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",64);
var defaults=UnityEditor.AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>("Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset");
var entries=(System.Collections.Generic.List<NoryangjinMapToolPalettePlacementEntry>)typeof(NoryangjinMapToolPaletteDefaults).GetField("entries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(defaults);
var obstacles=UnityEditor.AssetDatabase.LoadAssetAtPath<ObstaclePrefabs>("Assets/ShooterSurvival/Prefabs/Obstacle/ObstaclePrefabs.asset");
Material Mat(Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.SetColor("_BaseColor",color);mats.Add(m);return m;}
var red=Mat(new Color(1,.24f,.22f));var green=Mat(new Color(.2f,1,.58f));var orange=Mat(new Color(1,.69f,.14f));var purple=Mat(new Color(.85f,.43f,1));var white=Mat(Color.white);var zoneInk=Mat(new Color(.025f,.06f,.09f));
Bounds B(GameObject go){var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)&&r.GetComponent<TMPro.TMP_Text>()==null).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
void Visual(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;}
GameObject Add(string path,Vector3 point,float yaw,string role){
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new System.InvalidOperationException("Missing "+path);var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,content.transform);var e=entries.FirstOrDefault(x=>x.prefabPath==path);
 go.transform.localScale=Vector3.Scale(prefab.transform.localScale,e==null?Vector3.one:e.scale)*2.5f;go.transform.SetPositionAndRotation(point,Quaternion.Euler(0,yaw,0));
 foreach(var canvas in go.GetComponentsInChildren<Canvas>(true))canvas.gameObject.SetActive(false);
 foreach(var tmp in go.GetComponentsInChildren<TMPro.TMP_Text>(true))tmp.gameObject.SetActive(false);
 Visual(go);
 if(path.EndsWith("/Box_left.prefab")){var altar=go.GetComponent<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>()??go.AddComponent<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>();altar.Configure(role.Contains("unique")?IndianOceanAssets.ShooterSurvival.Rarity.Unique:role.Contains("elite")?IndianOceanAssets.ShooterSurvival.Rarity.Rare:IndianOceanAssets.ShooterSurvival.Rarity.Normal);altar.enabled=false;}
 var b=B(go);go.transform.position+=new Vector3(point.x-b.center.x,point.y-b.min.y,point.z-b.center.z);
 placements.Add(new{role,prefab=path,position=new[]{go.transform.position.x,go.transform.position.y,go.transform.position.z},scale=new[]{go.transform.localScale.x,go.transform.localScale.y,go.transform.localScale.z},bounds=B(go).size.ToString("F2")});return go;
}
var cameraGo=new GameObject("Camera");cameraGo.transform.SetParent(root.transform);var camera=cameraGo.AddComponent<Camera>();camera.scene=preview;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);camera.cullingMask=1<<31;camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.13f,.18f);
void Text(string words,Vector3 point,float size,float width,Color color,TextAnchor anchor=TextAnchor.MiddleCenter){font.RequestCharactersInTexture(words,64);for(int pass=0;pass<2;pass++){var go=new GameObject("Label "+words.Split('\n')[0]);go.transform.SetParent(content.transform);go.layer=31;go.transform.position=point+(pass==0?camera.transform.right*.15f-camera.transform.up*.15f+Vector3.down*.05f:Vector3.zero);go.transform.rotation=camera.transform.rotation;var tm=go.AddComponent<TextMesh>();tm.font=font;tm.fontSize=64;tm.characterSize=size;tm.text=words;tm.anchor=anchor;tm.alignment=anchor==TextAnchor.MiddleLeft?TextAlignment.Left:TextAlignment.Center;tm.color=pass==0?Color.black:color;go.GetComponent<MeshRenderer>().sharedMaterial=font.material;texts.Add((tm,width));}}
void Line(Vector3[] points,Material material,float width){var go=new GameObject("Planning line");go.transform.SetParent(content.transform);go.layer=31;var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.widthMultiplier=width;line.positionCount=points.Length;line.SetPositions(points);}
void Arrow(Vector3 a,Vector3 b,Material material,float width){var d=(b-a).normalized;var right=Vector3.Cross(Vector3.up,d);float length=Mathf.Min(3,Vector3.Distance(a,b)*.25f);Line(new[]{a,b,b-d*length+right*length*.6f,b,b-d*length-right*length*.6f},material,width);}
void Pin(Vector3 point,string label,Material material,float radius){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.transform.SetParent(content.transform);go.layer=31;go.transform.position=new Vector3(point.x,35,point.z);go.transform.localScale=new Vector3(radius*2,.1f,radius*2);go.GetComponent<Renderer>().sharedMaterial=material;Text(label,new Vector3(point.x,36,point.z),radius*.14f,radius*1.8f,Color.white);}
void Clear(){foreach(Transform child in content.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);texts.Clear();}
void Capture(string name,Vector3 point,float ortho,int width,int height){string path=output+"/"+name+".png";if(System.IO.File.Exists(path))throw new System.InvalidOperationException("Refusing overwrite "+path);var old=RenderTexture.active;var rt=new RenderTexture(width,height,24);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
 try{camera.transform.position=point-camera.transform.forward*1000;camera.orthographicSize=ortho;camera.aspect=(float)width/height;camera.targetTexture=rt;camera.Render();foreach(var item in texts){var bounds=item.Text.GetComponent<Renderer>().bounds;float actual=Mathf.Max(bounds.size.x,bounds.size.z);if(actual>item.Width)item.Text.transform.localScale*=item.Width/actual;}camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());}
 finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}}
try{
 foreach(string part in new[]{"Roads","Props","Water"}){var copy=UnityEngine.Object.Instantiate(map.Find(part).gameObject,root.transform);Visual(copy);foreach(var r in copy.GetComponentsInChildren<Renderer>(true)){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetFloat("_OutlineWidth",0);r.SetPropertyBlock(block);}}
 var sourceRoadColliders=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
 float Height(float x,float z){float best=-1;var ray=new Ray(new Vector3(x,60,z),Vector3.down);foreach(var c in sourceRoadColliders){RaycastHit hit;if(c.Raycast(ray,out hit,100))best=Mathf.Max(best,hit.point.y);}return best<0?.15f:best+.15f;}
 void Actor(string path,Vector3 center,Vector3 direction,int index,int count,string role){
   var side=Vector3.Cross(Vector3.up,direction);var point=center+side*((index%2==0?-1:1)*(count>1?1.7f:0))+direction*((index/2)*6-((count-1)/2)*3);
   point.y=Height(point.x,point.z);Add(path,point,Quaternion.LookRotation(-direction).eulerAngles.y,role);
 }
 var lightGo=new GameObject("Sun");lightGo.transform.SetParent(root.transform);lightGo.transform.rotation=Quaternion.Euler(50,-25,0);var sun=lightGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;sun.cullingMask=1<<31;
 camera.transform.rotation=Quaternion.Euler(90,0,0);
 const string bonusPath="Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab";

 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(8.9030f,0.0000f,-114.9370f),new Vector3(-1.0000f,0.0000f,0.0095f),0,1,"E01 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab",new Vector3(-10.6600f,0.0000f,-48.0690f),new Vector3(0.0021f,0.0000f,1.0000f),0,2,"E02 그물");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab",new Vector3(-10.6600f,0.0000f,-48.0690f),new Vector3(0.0021f,0.0000f,1.0000f),1,2,"E02 그물");
 Arrow(new Vector3(-10.7770f,25.0000f,-103.7685f),new Vector3(-10.7645f,25.0000f,-97.7685f),white,.8f);
 Arrow(new Vector3(-10.6105f,25.0000f,-24.1815f),new Vector3(-10.5980f,25.0000f,-18.1815f),white,.8f);
 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(29.8800f,0.0000f,-7.2000f),new Vector3(1.0000f,0.0000f,0.0000f),0,2,"E03 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(29.8800f,0.0000f,-7.2000f),new Vector3(1.0000f,0.0000f,0.0000f),1,2,"E03 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(149.2220f,0.0000f,-7.2000f),new Vector3(1.0000f,0.0000f,0.0000f),0,3,"E04 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(149.2220f,0.0000f,-7.2000f),new Vector3(1.0000f,0.0000f,0.0000f),1,3,"E04 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(149.2220f,0.0000f,-7.2000f),new Vector3(1.0000f,0.0000f,0.0000f),2,3,"E04 검");
 Actor(bonusPath,new Vector3(94.6080f,0.0000f,-7.2000f),new Vector3(1.0000f,0.0000f,0.0000f),0,2,"F01 normal");
 Actor(bonusPath,new Vector3(94.6080f,0.0000f,-7.2000f),new Vector3(1.0000f,0.0000f,0.0000f),1,2,"F01 normal");
 Arrow(new Vector3(162.4042f,25.0000f,-7.2000f),new Vector3(168.4042f,25.0000f,-7.2000f),white,.8f);
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(191.7000f,0.0000f,21.9600f),new Vector3(0.0000f,0.0000f,1.0000f),0,2,"E05 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(191.7000f,0.0000f,21.9600f),new Vector3(0.0000f,0.0000f,1.0000f),1,2,"E05 검");
 Arrow(new Vector3(191.7000f,25.0000f,-2.3025f),new Vector3(191.7000f,25.0000f,3.6975f),white,.8f);
 Arrow(new Vector3(191.7000f,25.0000f,42.6525f),new Vector3(191.7000f,25.0000f,48.6525f),white,.8f);
 Arrow(new Vector3(185.9542f,25.0000f,53.5500f),new Vector3(179.9542f,25.0000f,53.5500f),white,.8f);
 Arrow(new Vector3(159.7170f,25.0000f,53.5500f),new Vector3(153.7170f,25.0000f,53.5500f),white,.8f);
 Arrow(new Vector3(136.1707f,25.0000f,53.5500f),new Vector3(130.1707f,25.0000f,53.5500f),white,.8f);
 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(124.3860f,0.0000f,-34.7360f),new Vector3(-0.0004f,0.0000f,-1.0000f),0,3,"E06 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(124.3860f,0.0000f,-34.7360f),new Vector3(-0.0004f,0.0000f,-1.0000f),1,3,"E06 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(124.3860f,0.0000f,-34.7360f),new Vector3(-0.0004f,0.0000f,-1.0000f),2,3,"E06 검");
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(124.3460f,0.0000f,-127.0360f),new Vector3(-0.0004f,0.0000f,-1.0000f),0,3,"E07 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab",new Vector3(124.3460f,0.0000f,-127.0360f),new Vector3(-0.0004f,0.0000f,-1.0000f),1,3,"E07 그물");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab",new Vector3(124.3460f,0.0000f,-127.0360f),new Vector3(-0.0004f,0.0000f,-1.0000f),2,3,"E07 그물");
 Actor("Assets/JH/Model/Prefab/Enemy_FatMan.prefab",new Vector3(124.2850f,0.0000f,-267.4910f),new Vector3(-0.0004f,0.0000f,-1.0000f),0,3,"E08 뚱보");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(124.2850f,0.0000f,-267.4910f),new Vector3(-0.0004f,0.0000f,-1.0000f),1,3,"E08 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(124.2850f,0.0000f,-267.4910f),new Vector3(-0.0004f,0.0000f,-1.0000f),2,3,"E08 검");
 Actor(bonusPath,new Vector3(124.3160f,0.0000f,-195.2570f),new Vector3(-0.0004f,0.0000f,-1.0000f),0,2,"F02 normal");
 Actor(bonusPath,new Vector3(124.3160f,0.0000f,-195.2570f),new Vector3(-0.0004f,0.0000f,-1.0000f),1,2,"F02 elite");
 Arrow(new Vector3(124.4035f,25.0000f,4.3808f),new Vector3(124.4009f,25.0000f,-1.6192f),white,.8f);
 Arrow(new Vector3(124.3353f,25.0000f,-152.1266f),new Vector3(124.3326f,25.0000f,-158.1266f),white,.8f);
 Arrow(new Vector3(124.2740f,25.0000f,-292.5820f),new Vector3(124.2714f,25.0000f,-298.5820f),white,.8f);
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(86.0000f,0.0000f,-347.7510f),new Vector3(-1.0000f,0.0000f,0.0000f),0,3,"E09 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(86.0000f,0.0000f,-347.7510f),new Vector3(-1.0000f,0.0000f,0.0000f),1,3,"E09 검");
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(86.0000f,0.0000f,-347.7510f),new Vector3(-1.0000f,0.0000f,0.0000f),2,3,"E09 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(-23.0130f,0.0000f,-347.7510f),new Vector3(-1.0000f,0.0000f,0.0000f),0,3,"E10 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(-23.0130f,0.0000f,-347.7510f),new Vector3(-1.0000f,0.0000f,0.0000f),1,3,"E10 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(-23.0130f,0.0000f,-347.7510f),new Vector3(-1.0000f,0.0000f,0.0000f),2,3,"E10 경비");
 Actor(bonusPath,new Vector3(32.4500f,0.0000f,-347.7510f),new Vector3(-1.0000f,0.0000f,0.0000f),0,1,"F03 normal");
 Arrow(new Vector3(-39.1376f,25.0000f,-347.7511f),new Vector3(-45.1376f,25.0000f,-347.7511f),white,.8f);
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab",new Vector3(-67.0000f,0.0000f,-301.8510f),new Vector3(0.0000f,0.0000f,1.0000f),0,3,"E11 그물");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab",new Vector3(-67.0000f,0.0000f,-301.8510f),new Vector3(0.0000f,0.0000f,1.0000f),1,3,"E11 그물");
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(-67.0000f,0.0000f,-301.8510f),new Vector3(0.0000f,0.0000f,1.0000f),2,3,"E11 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_FatMan.prefab",new Vector3(-67.0000f,0.0000f,-234.3510f),new Vector3(0.0000f,0.0000f,1.0000f),0,4,"E12 뚱보");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(-67.0000f,0.0000f,-234.3510f),new Vector3(0.0000f,0.0000f,1.0000f),1,4,"E12 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(-67.0000f,0.0000f,-234.3510f),new Vector3(0.0000f,0.0000f,1.0000f),2,4,"E12 검");
 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(-67.0000f,0.0000f,-234.3510f),new Vector3(0.0000f,0.0000f,1.0000f),3,4,"E12 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(-67.0000f,0.0000f,-131.7510f),new Vector3(0.0000f,0.0000f,1.0000f),0,3,"E13 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(-67.0000f,0.0000f,-131.7510f),new Vector3(0.0000f,0.0000f,1.0000f),1,3,"E13 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(-67.0000f,0.0000f,-131.7510f),new Vector3(0.0000f,0.0000f,1.0000f),2,3,"E13 검");
 Actor(bonusPath,new Vector3(-67.0000f,0.0000f,-177.6510f),new Vector3(0.0000f,0.0000f,1.0000f),0,2,"F04 normal");
 Actor(bonusPath,new Vector3(-67.0000f,0.0000f,-177.6510f),new Vector3(0.0000f,0.0000f,1.0000f),1,2,"F04 elite");
 Arrow(new Vector3(-67.0001f,25.0000f,-210.3511f),new Vector3(-67.0001f,25.0000f,-204.3511f),white,.8f);
 Arrow(new Vector3(-67.0001f,25.0000f,-115.8511f),new Vector3(-67.0001f,25.0000f,-109.8511f),white,.8f);
 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(32.4500f,0.0000f,-77.7510f),new Vector3(1.0000f,0.0000f,0.0000f),0,2,"E14 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(32.4500f,0.0000f,-77.7510f),new Vector3(1.0000f,0.0000f,0.0000f),1,2,"E14 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(93.8750f,0.0000f,-77.7510f),new Vector3(1.0000f,0.0000f,0.0000f),0,2,"E15 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(93.8750f,0.0000f,-77.7510f),new Vector3(1.0000f,0.0000f,0.0000f),1,2,"E15 검");
 Actor(bonusPath,new Vector3(193.3250f,0.0000f,-77.7510f),new Vector3(1.0000f,0.0000f,0.0000f),0,1,"F05 elite");
 Arrow(new Vector3(-31.9751f,25.0000f,-77.7511f),new Vector3(-25.9751f,25.0000f,-77.7511f),white,.8f);
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(225.5000f,0.0000f,-50.7510f),new Vector3(0.0000f,0.0000f,1.0000f),0,3,"E16 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(225.5000f,0.0000f,-50.7510f),new Vector3(0.0000f,0.0000f,1.0000f),1,3,"E16 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(225.5000f,0.0000f,-50.7510f),new Vector3(0.0000f,0.0000f,1.0000f),2,3,"E16 검");
 Actor("Assets/JH/Model/Prefab/Enemy_FatMan.prefab",new Vector3(225.5000f,0.0000f,-12.9510f),new Vector3(0.0000f,0.0000f,1.0000f),0,3,"E17 뚱보");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab",new Vector3(225.5000f,0.0000f,-12.9510f),new Vector3(0.0000f,0.0000f,1.0000f),1,3,"E17 그물");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab",new Vector3(225.5000f,0.0000f,-12.9510f),new Vector3(0.0000f,0.0000f,1.0000f),2,3,"E17 그물");
 Actor(bonusPath,new Vector3(225.5000f,0.0000f,28.8990f),new Vector3(0.0000f,0.0000f,1.0000f),0,2,"F06 normal");
 Actor(bonusPath,new Vector3(225.5000f,0.0000f,28.8990f),new Vector3(0.0000f,0.0000f,1.0000f),1,2,"F06 elite");
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(276.1250f,0.0000f,57.2490f),new Vector3(1.0000f,0.0000f,0.0000f),0,4,"E18 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(276.1250f,0.0000f,57.2490f),new Vector3(1.0000f,0.0000f,0.0000f),1,4,"E18 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(276.1250f,0.0000f,57.2490f),new Vector3(1.0000f,0.0000f,0.0000f),2,4,"E18 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(276.1250f,0.0000f,57.2490f),new Vector3(1.0000f,0.0000f,0.0000f),3,4,"E18 검");
 Arrow(new Vector3(235.6624f,25.0000f,57.2489f),new Vector3(241.6624f,25.0000f,57.2489f),white,.8f);
 Arrow(new Vector3(310.5874f,25.0000f,57.2489f),new Vector3(316.5874f,25.0000f,57.2489f),white,.8f);
 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(326.7500f,0.0000f,83.5740f),new Vector3(0.0000f,0.0000f,1.0000f),0,3,"E19 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_OldMan.prefab",new Vector3(326.7500f,0.0000f,83.5740f),new Vector3(0.0000f,0.0000f,1.0000f),1,3,"E19 노인");
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(326.7500f,0.0000f,83.5740f),new Vector3(0.0000f,0.0000f,1.0000f),2,3,"E19 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_FatMan.prefab",new Vector3(326.7500f,0.0000f,171.3240f),new Vector3(0.0000f,0.0000f,1.0000f),0,3,"E20 뚱보");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(326.7500f,0.0000f,171.3240f),new Vector3(0.0000f,0.0000f,1.0000f),1,3,"E20 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(326.7500f,0.0000f,171.3240f),new Vector3(0.0000f,0.0000f,1.0000f),2,3,"E20 검");
 Actor(bonusPath,new Vector3(326.7500f,0.0000f,111.3610f),new Vector3(0.0000f,0.0000f,1.0000f),0,2,"F07 elite");
 Actor(bonusPath,new Vector3(326.7500f,0.0000f,111.3610f),new Vector3(0.0000f,0.0000f,1.0000f),1,2,"F07 elite");
 Arrow(new Vector3(326.7499f,25.0000f,130.2989f),new Vector3(326.7499f,25.0000f,136.2989f),white,.8f);
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(380.7500f,0.0000f,203.4990f),new Vector3(1.0000f,0.0000f,0.0000f),0,4,"E21 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(380.7500f,0.0000f,203.4990f),new Vector3(1.0000f,0.0000f,0.0000f),1,4,"E21 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(380.7500f,0.0000f,203.4990f),new Vector3(1.0000f,0.0000f,0.0000f),2,4,"E21 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab",new Vector3(380.7500f,0.0000f,203.4990f),new Vector3(1.0000f,0.0000f,0.0000f),3,4,"E21 그물");
 Arrow(new Vector3(338.3749f,25.0000f,203.4989f),new Vector3(344.3749f,25.0000f,203.4989f),white,.8f);
 Arrow(new Vector3(421.6249f,25.0000f,203.4989f),new Vector3(427.6249f,25.0000f,203.4989f),white,.8f);
 Actor(bonusPath,new Vector3(439.2500f,0.0000f,169.7490f),new Vector3(0.0000f,0.0000f,-1.0000f),0,1,"F08 elite");
 Arrow(new Vector3(439.2499f,25.0000f,197.7239f),new Vector3(439.2499f,25.0000f,191.7239f),white,.8f);
 Arrow(new Vector3(439.2499f,25.0000f,147.7739f),new Vector3(439.2499f,25.0000f,141.7739f),white,.8f);
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(402.9120f,0.0000f,135.9990f),new Vector3(-1.0000f,0.0000f,0.0000f),0,2,"E22 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(402.9120f,0.0000f,135.9990f),new Vector3(-1.0000f,0.0000f,0.0000f),1,2,"E22 검");
 Arrow(new Vector3(342.7999f,25.0000f,135.9989f),new Vector3(336.7999f,25.0000f,135.9989f),white,.8f);
 Arrow(new Vector3(275.8624f,25.0000f,135.9989f),new Vector3(269.8624f,25.0000f,135.9989f),white,.8f);
 Actor("Assets/JH/Model/Prefab/Enemy_FatMan.prefab",new Vector3(248.0000f,0.0000f,170.4240f),new Vector3(0.0000f,0.0000f,1.0000f),0,3,"E23 뚱보");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(248.0000f,0.0000f,170.4240f),new Vector3(0.0000f,0.0000f,1.0000f),1,3,"E23 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(248.0000f,0.0000f,170.4240f),new Vector3(0.0000f,0.0000f,1.0000f),2,3,"E23 검");
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(248.0000f,0.0000f,223.0740f),new Vector3(0.0000f,0.0000f,1.0000f),0,4,"E24 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_Guard.prefab",new Vector3(248.0000f,0.0000f,223.0740f),new Vector3(0.0000f,0.0000f,1.0000f),1,4,"E24 경비");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(248.0000f,0.0000f,223.0740f),new Vector3(0.0000f,0.0000f,1.0000f),2,4,"E24 검");
 Actor("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",new Vector3(248.0000f,0.0000f,223.0740f),new Vector3(0.0000f,0.0000f,1.0000f),3,4,"E24 검");
 Actor("Assets/JH/Model/Prefab/Enemy_Woman.prefab",new Vector3(248.0000f,0.0000f,277.7490f),new Vector3(0.0000f,0.0000f,1.0000f),0,1,"E25 여성 보스");
 Actor(bonusPath,new Vector3(248.0000f,0.0000f,322.2990f),new Vector3(0.0000f,0.0000f,1.0000f),0,1,"F09 unique");
 Arrow(new Vector3(247.9999f,25.0000f,238.2989f),new Vector3(247.9999f,25.0000f,244.2989f),white,.8f);

 Text("노량진  전체 전투 · 보너스 배치",new Vector3(181,42,381),2.0f,570,Color.white);
 Text("실제 길·상점 + 기존 적·BonusWall 프리팹",new Vector3(181,42,361),.9f,560,Color.white);
 Text("시작",new Vector3(35,42,-133),1.4f,56,Color.white);
 Line(new[]{new Vector3(27,30,-128),new Vector3(27,30,-115)},white,.65f);
 Text("보스",new Vector3(278,42,277),1.4f,60,Color.white);
 Text("출구",new Vector3(248,42,351),1.4f,62,Color.white);
 Text("기획용 미리보기 · 적/보너스는 식별용 2.5배 표시 · 원본 씬 미적용",new Vector3(181,42,-391),.84f,590,Color.white);
 Capture("01-full-stage-actual",new Vector3(182,0,-7),410,3000,3900);
 Capture("02-start-and-large-loop",new Vector3(80,0,-145),239,2800,3200);
 Capture("03-bridges-and-finish",new Vector3(251,0,132),234,2800,3200);
 if(placements.Count!=83)throw new System.InvalidOperationException("Expected 69 enemies + 14 fixed walls");
 if(scene.isDirty!=dirty||Hash(scene.path)!=beforeHash)throw new System.InvalidOperationException("Source scene changed during planning capture");
 var asm=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");var serialize=asm.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
 System.IO.File.WriteAllText(recordsFolder+"/preview-manifest.json",(string)serialize.Invoke(null,new object[]{new{status="Planning only; original route/props cloned; actors shown at 2.5x for visibility",sourceScene=scene.path,sourceHash=beforeHash,placements,enemies=69,fixedWalls=14,output}}));
 return new{output,enemyCount=69,fixedWalls=14,sceneUnchanged=true};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);foreach(var m in mats)UnityEngine.Object.DestroyImmediate(m);UnityEngine.Object.DestroyImmediate(font);}
