// Bounded Play Mode verification. Starts at each elevation span, then stops movement.
if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Enter Play Mode first.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.PlayerScript>(true)).Single();
var timer=IndianOceanAssets.ShooterSurvival.TimeManager.Instance;
if(timer==null||!timer.isForwardMarchScene)throw new System.InvalidOperationException("Forward movement is not configured.");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var slopes=map.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.NoryangjinTurnSpot>(true).Where(s=>s.IsSlopeTransition).OrderBy(s=>s.name).ToArray();
var body=player.GetComponent<Rigidbody>();var camera=Camera.main;
string folder="tmp/image-previews/combat-route-camera-2026-09-20/"+System.DateTime.UtcNow.ToString("HHmmss");System.IO.Directory.CreateDirectory(folder);
string reportPath="map-concepts/combat-route-fixes-2026-09-20/camera-playmode-probe.json";
float oldScale=Time.timeScale,oldFactor=IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor;float started=Time.realtimeSinceStartup;float last=-100;int phase=0;bool[] captured=new bool[4];
var samples=new System.Collections.Generic.List<object>();var summaries=new System.Collections.Generic.List<object>();var files=new System.Collections.Generic.List<string>();
float maxHeight=0,minPitch=0,maxPitch=0;Vector3 previous=Vector3.zero;float minMovingStep=float.PositiveInfinity;
void Position(int span){IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=false;IndianOceanAssets.ShooterSurvival.NoryangjinTurnSpot.ResetAllForNewRun();player.ResetState();var p=span==0?new Vector3(-59,.12f,-77.7511f):new Vector3(375,.12f,135.9989f);var q=Quaternion.Euler(0,span==0?90:270,0);body.position=p;body.rotation=q;body.linearVelocity=Vector3.zero;player.transform.SetPositionAndRotation(p,q);Physics.SyncTransforms();previous=p;maxHeight=0;minPitch=0;maxPitch=0;minMovingStep=float.PositiveInfinity;IndianOceanAssets.ShooterSurvival.CanvasScript.isGameOver=false;IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=true;}
void Capture(string name){string path=folder+"/"+name+".png";var go=new GameObject("Runtime proof camera");go.hideFlags=HideFlags.HideAndDontSave;var cam=go.AddComponent<Camera>();cam.CopyFrom(camera);cam.enabled=false;cam.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);cam.scene=scene;cam.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);var old=RenderTexture.active;var rt=new RenderTexture(720,1280,24);var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);try{cam.aspect=720f/1280;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());files.Add(path);}finally{cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);}}
UnityEditor.EditorApplication.CallbackFunction tick=null;
void Finish(string status,string error){UnityEditor.EditorApplication.update-=tick;IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=false;IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=oldFactor;Time.timeScale=oldScale;var asm=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");var serialize=asm.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});System.IO.File.WriteAllText(reportPath,(string)serialize.Invoke(null,new object[]{new{status,error,seconds=Time.realtimeSinceStartup-started,summaries,files,samples}}));}
tick=()=>{
 try{
  if(!UnityEditor.EditorApplication.isPlaying||player==null){Finish("Interrupted","Play Mode ended");return;}
  if(Time.realtimeSinceStartup-started>60){Finish("Failed","Timed out");return;}
  if(Time.time-last<.08f)return;last=Time.time;
  Vector3 p=player.transform.position;float pitch=Mathf.DeltaAngle(0,player.transform.eulerAngles.x);maxHeight=Mathf.Max(maxHeight,p.y);minPitch=Mathf.Min(minPitch,pitch);maxPitch=Mathf.Max(maxPitch,pitch);
  float step=new Vector2(p.x-previous.x,p.z-previous.z).magnitude;if(step>.001f)minMovingStep=Mathf.Min(minMovingStep,step);previous=p;
  samples.Add(new{phase,t=Time.time,p=new[]{p.x,p.y,p.z},pitch,cornerTurning=player.IsWorldYawTurnActive});
  if(p.y>5&&p.y<9&&pitch< -15&&!captured[phase*2]){captured[phase*2]=true;Capture("span-"+(phase+1)+"-up");}
  if(p.y>5&&p.y<9&&pitch>15&&!captured[phase*2+1]){captured[phase*2+1]=true;Capture("span-"+(phase+1)+"-down");}
  if((phase==0&&p.x>=180)||(phase==1&&p.x<=278)){
   int consumed=slopes.Skip(phase*4).Take(4).Count(s=>!s.gameObject.activeSelf);bool passed=maxHeight>11.9f&&maxHeight<12.5f&&p.y<.5f&&minPitch< -19&&maxPitch>19&&Mathf.Abs(pitch)<.1f&&consumed==4;
   summaries.Add(new{phase,passed,maxHeight,minPitch,maxPitch,endHeight=p.y,endPitch=pitch,consumed,minMovingStep});
   if(!passed){Finish("Failed","Elevation or pitch/trigger contract failed");return;}
   if(phase==0){phase=1;Position(phase);}else Finish("Passed",null);
  }
 }catch(System.Exception e){Finish("Failed",e.ToString());}
};
IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=1;Time.timeScale=4;Position(0);UnityEditor.EditorApplication.update+=tick;
return new{started=true,reportPath,folder};
