// Planning pictures only. Everything added lives in an isolated preview scene.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Planning capture requires Edit Mode; do not interrupt a running test.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;bool dirty=scene.isDirty;
string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())using(var file=System.IO.File.OpenRead(path))return System.BitConverter.ToString(sha.ComputeHash(file)).Replace("-","");}
string beforeHash=Hash(scene.path);
string output="tmp/image-previews/sr18-encounter-planning-2026-09-05";string recordsFolder="map-concepts/sr18-encounter-visuals-2026-09-05";System.IO.Directory.CreateDirectory(output);System.IO.Directory.CreateDirectory(recordsFolder);
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
 go.transform.localScale=Vector3.Scale(prefab.transform.localScale,e==null?Vector3.one:e.scale);go.transform.SetPositionAndRotation(point,Quaternion.Euler(0,yaw,0));
 foreach(var canvas in go.GetComponentsInChildren<Canvas>(true))canvas.gameObject.SetActive(false);
 foreach(var tmp in go.GetComponentsInChildren<TMPro.TMP_Text>(true))tmp.gameObject.SetActive(false);
 Visual(go);
 if(path.EndsWith("/Box_left.prefab")){var altar=go.GetComponent<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>()??go.AddComponent<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>();altar.Configure(role.Contains("elite")?IndianOceanAssets.ShooterSurvival.Rarity.Rare:IndianOceanAssets.ShooterSurvival.Rarity.Normal);altar.enabled=false;}
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
 foreach(string part in new[]{"Roads","Props","Water"}){var copy=UnityEngine.Object.Instantiate(map.Find(part).gameObject,root.transform);Visual(copy);}
 var lightGo=new GameObject("Sun");lightGo.transform.SetParent(root.transform);lightGo.transform.rotation=Quaternion.Euler(50,-25,0);var sun=lightGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;sun.cullingMask=1<<31;
 camera.transform.rotation=Quaternion.Euler(90,0,0);
 Text("SR18  전투 · 보상 배치 초안",new Vector3(135,42,383),2.8f,680,Color.white);
 Text("현재 길·상점 유지  /  기획용 표시이며 씬에는 미적용",new Vector3(135,42,360),1.1f,660,Color.white);
 var zones=new[]{("A  첫 상점가","단독 적 → 두 역할\n드롭으로 성장 학습",new Vector3(-10.8f,0,-55)),("B  큰 루프 전반","일반 vs 고등급\n욕심내면 적 발동",new Vector3(124.25f,0,-202)),("C  큰 루프 후반","기믹 → 전투 → 회수\n판단을 순서대로",new Vector3(-67,0,-225)),("D  첫 고가","경사에는 선택·기믹 X\n정상 평지는 단순 전투",new Vector3(50,12,-77.75f)),("E  중후반 상점가","이동형 적 조합\n양동이는 독립 구간",new Vector3(225.5f,0,8)),("F  마지막 돌파","배운 패턴 재조합\n마지막 보상 후보",new Vector3(248,0,275))};
 var zoneLabels=new[]{new Vector3(-37,0,-55),new Vector3(101,0,-202),new Vector3(-91,0,-225),new Vector3(50,0,-58),new Vector3(203,0,8),new Vector3(223,0,275)};
 for(int i=0;i<zones.Length;i++){float z=295-i*55;Text(zones[i].Item1,new Vector3(-211,42,z),1.65f,178,Color.white,TextAnchor.MiddleLeft);Text(zones[i].Item2,new Vector3(-211,42,z-20),1.15f,178,Color.white,TextAnchor.MiddleLeft);Line(new[]{zoneLabels[i]+Vector3.up*34,new Vector3(zones[i].Item3.x,34,zones[i].Item3.z)},white,.65f);Pin(zoneLabels[i],zones[i].Item1.Substring(0,1),zoneInk,7);}
 var enemyPoints=new[]{new Vector3(-10.8f,0,-88),new Vector3(30,0,-7.2f),new Vector3(191.7f,0,24),new Vector3(124.25f,0,-123),new Vector3(124.25f,0,-234),new Vector3(124.25f,0,-299),new Vector3(35,0,-347.75f),new Vector3(-67,0,-285),new Vector3(-67,0,-190),new Vector3(47,12,-77.75f),new Vector3(225.5f,0,-30),new Vector3(300,0,57.25f),new Vector3(326.75f,0,104),new Vector3(400,0,203.5f),new Vector3(409,0,136),new Vector3(248,0,205),new Vector3(248,0,294)};
 foreach(var p in enemyPoints)Pin(p,"적",red,3.6f);
 foreach(var p in new[]{new Vector3(145,0,-7.2f),new Vector3(124.25f,0,-202),new Vector3(95,0,-347.75f),new Vector3(-67,0,-143),new Vector3(198,0,-77.75f),new Vector3(267,0,57.25f),new Vector3(248,0,322)})Pin(p,"B",green,4.2f);
 foreach(var p in new[]{new Vector3(82,0,-7.2f),new Vector3(124.25f,0,-264),new Vector3(-67,0,-239),new Vector3(225.5f,0,14),new Vector3(439.25f,0,170),new Vector3(248,0,259)})Pin(p,"G",orange,3.8f);
 Text("적 = 전투 묶음   B = 고정 BonusWall   G = 기믹 묶음",new Vector3(135,42,-388),1.25f,685,Color.white);
 Text("적 처치 드롭은 별도 · 코너/경사 전후는 여유 확보 · 개수와 능력치는 미확정",new Vector3(135,42,-409),.9f,685,Color.white);
 Capture("01-overall-plan-v2",new Vector3(135,0,-10),430,2600,2800);

 Clear();camera.transform.rotation=Quaternion.Euler(90,180,0);
 Vector3 P(float side,float along,float y=30)=>new Vector3(124.25f-side,y,-210-along);
 const string bonusPath="Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab";
 Add(bonusPath,P(-1.75f,-8,.08f),0,"Choice example: normal");Add(bonusPath,P(1.75f,-8,.08f),0,"Choice example: elite");
 Add("Assets/JH/Model/Prefab/Enemy_Guard.prefab",P(1.6f,18,.08f),0,"Choice example: Guard shoot");Add("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",P(-1.6f,25,.08f),0,"Choice example: Sword move then attack");
 Text("B 확대  |  욕심내면 싸움이 시작되는 보너스",P(0,40),.39f,86,Color.white);
 Text("좌·우는 진행 방향 기준 / 실제 프리팹 배치 예시",P(0,36),.19f,82,Color.white);
 Arrow(P(-1.8f,-32,27),P(-1.8f,5,27),green,.16f);Arrow(P(1.8f,-32,27),P(1.8f,5,27),orange,.16f);
 Line(new[]{P(.3f,-16,28),P(3.15f,-16,28),P(3.15f,-14,28),P(.3f,-14,28),P(.3f,-16,28)},purple,.19f);
 Text("안전 쪽\n일반 BonusWall\n효과는 랜덤",P(-21,-8),.29f,25,Color.white);Line(new[]{P(-10,-8),P(-2,-8)},green,.14f);
 Text("욕심 쪽\n고등급 BonusWall\n효과는 랜덤",P(21,-8),.29f,25,Color.white);Line(new[]{P(10,-8),P(2,-8)},orange,.14f);
 Text("우측 접근 감지\n이쪽에 들어오면\n앞의 적 활성화",P(22,-23),.23f,27,Color.white);Line(new[]{P(11,-21),P(3,-15)},purple,.14f);
 Text("경비원\n발사 1회",P(20,21),.27f,24,Color.white);Line(new[]{P(11,21),P(2,18)},red,.14f);
 Text("검을 든 적\n지정 위치로 진입",P(-21,24),.27f,26,Color.white);Arrow(P(-1.6f,24,28),P(1.4f,8,28),red,.15f);
 Arrow(P(0,-36,28),P(0,-29,28),white,.25f);Text("플레이어 진행",P(-15,-33),.24f,28,Color.white);
 Text("새 갈래길 없이 선택 · 적/BonusWall 연속 접촉 간격은 적용 전 검증",P(0,-40),.17f,90,Color.white);
 Text("씬 미적용 / 두 보너스의 강제 1택 시스템을 추가한 그림은 아님",P(0,-44),.15f,90,Color.white);
 Capture("02-risk-reward-choice-v2",new Vector3(124.25f,0,-210),47,1900,1800);

 Clear();camera.transform.rotation=Quaternion.Euler(90,0,0);
 Vector3 Q(float side,float along,float y=30)=>new Vector3(-67+side,y,-224+along);
 var hole=obstacles.obstaclePrefabs.Single(o=>o.pattern==ObstaclePattern.Hole).prefab;
 Add(UnityEditor.AssetDatabase.GetAssetPath(hole),Q(1.8f,-21,.03f),0,"Sequential example: Hole");
 Add("Assets/JH/Model/Prefab/Enemy_Guard.prefab",Q(-1.5f,3,.08f),180,"Sequential example: Guard");Add("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab",Q(1.6f,9,.08f),180,"Sequential example: Sword");
 Add(bonusPath,Q(0,31,.08f),180,"Sequential example: reward after combat");
 Text("C 확대  |  기믹 → 전투 → 보상 회수",Q(0,47),.39f,92,Color.white);
 Text("한꺼번에 겹치지 않고, 앞의 판단이 끝난 다음 새 압박",Q(0,43),.2f,91,Color.white);
 Text("① 구멍을 먼저 읽기\n반대편 통로 확보",Q(22,-21),.27f,32,Color.white);Line(new[]{Q(10,-21),Q(2,-21)},orange,.14f);
 Arrow(Q(-1.9f,-34,27),Q(-1.9f,-12,27),green,.18f);
 Text("② 다음 구간에서 전투\n사격 + 이동형 적",Q(-23,6),.28f,34,Color.white);Line(new[]{Q(-10,6),Q(-2,3)},red,.15f);
 Text("③ 싸운 뒤 회수\n적 드롭과 고정 보너스\n사이에 여유 구간",Q(23,30),.25f,32,Color.white);Line(new[]{Q(11,30),Q(0,31)},green,.15f);
 Arrow(Q(0,16,27),Q(0,27,27),green,.18f);
 Text("기름·양동이를 구멍과 겹치지 않음 / 고가와 경사에는 이 패턴을 놓지 않음",Q(0,-43),.17f,96,Color.white);
 Text("기획용 배치 예시 · 실제 피해량/밀도/획득 간격은 플레이테스트로 조정",Q(0,-47),.16f,96,Color.white);
 Capture("03-gimmick-combat-reward-v2",new Vector3(-67,0,-224),51,1900,1850);
 if(scene.isDirty!=dirty||Hash(scene.path)!=beforeHash)throw new System.InvalidOperationException("Source scene changed during planning capture");
 var asm=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");var serialize=asm.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
 System.IO.File.WriteAllText(recordsFolder+"/preview-manifest.json",(string)serialize.Invoke(null,new object[]{new{status="Planning only, not applied",sourceScene=scene.path,sourceHash=beforeHash,placements,enemyEncounterMarkers=enemyPoints.Length,fixedBonusLocations=7,gimmickLocations=6,output}}));
 return new{output,placements,sceneUnchanged=true};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);foreach(var m in mats)UnityEngine.Object.DestroyImmediate(m);UnityEngine.Object.DestroyImmediate(font);}
