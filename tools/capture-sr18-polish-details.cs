if(!UnityEditor.EditorApplication.isPlaying)throw new InvalidOperationException("Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var player=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_Player").GetComponent<IndianOceanAssets.ShooterSurvival.PlayerScript>();
string folder="tmp/image-previews/sr18-combat-polish-2026-09-09/"+DateTime.Now.ToString("HHmmss"); System.IO.Directory.CreateDirectory(folder);
var enemy=map.Find("Enemies").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>().OrderBy(e=>e.name).Skip(1).First();
var bucket=map.Find("Props").Cast<Transform>().First(t=>t.name.StartsWith("SR18_L_G")&&t.name.EndsWith("Bucket"));
var savedPosition=player.transform.position;
var go=new GameObject("Polish proof camera"); var cam=go.AddComponent<Camera>();
var extra=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();extra.renderShadows=false;extra.renderPostProcessing=false;
var rt=new RenderTexture(1200,1200,24); var texture=new Texture2D(1200,1200,TextureFormat.RGB24,false); var previous=RenderTexture.active;
try {
 cam.cullingMask=~(1<<5);cam.targetTexture=rt;cam.fieldOfView=50;cam.aspect=1;cam.farClipPlane=100;
 foreach(var item in new[]{(Name:"enemy-facing",Root:enemy.transform),(Name:"bucket-cluster",Root:bucket)}) {
  Vector3 dir=Vector3.ProjectOnPlane(item.Root.forward,Vector3.up).normalized;
  Vector3 cameraPosition=item.Root.position-dir*7+Vector3.up*4;
  if(item.Name=="enemy-facing") {
   player.transform.position=item.Root.position-dir*7+item.Root.right*1.5f;
   typeof(IndianOceanAssets.ShooterSurvival.EnemyEventController).GetMethod("FacePlayerExactly",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(enemy,null);
   cameraPosition=player.transform.position+Vector3.up*3;
  }
  cam.transform.SetPositionAndRotation(cameraPosition,Quaternion.LookRotation(item.Root.position+Vector3.up-cameraPosition));
  cam.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1200,1200),0,0);texture.Apply();
  System.IO.File.WriteAllBytes(folder+"/"+item.Name+".png",texture.EncodeToPNG());
 }
 return folder;
} finally {
 player.transform.position=savedPosition;cam.targetTexture=null;RenderTexture.active=previous;
 UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(go);
}
