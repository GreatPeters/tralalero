using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

// A real Play-mode camera renders evaluated skinning, then reads the target at
// end-of-frame. Immediate EditMode SampleAnimation previews are not used.
public static class S22MotionReview
{
 const string Folder="tmp/image-previews/s22-polish-2026-10-01/motion";
 public static object Main(){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");Directory.CreateDirectory(Folder);Object.FindFirstObjectByType<CanvasScript>().StartCoroutine(Run());return Folder;}
 static IEnumerator Run(){var records=new List<string>();
  foreach(string key in new[]{"N13_merchant_male","N14_merchant_female","N20_ajumma_boss"}){
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/"+key+".prefab");var actor=Object.Instantiate(prefab);actor.transform.position=new Vector3(10000,0,10000);foreach(var t in actor.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;var animator=actor.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;foreach(var r in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))r.updateWhenOffscreen=true;
   var clips=animator.runtimeAnimatorController.animationClips.Distinct().Where(c=>c.name=="bid"||c.name=="call"||c.name=="run"||c.name=="attack_once").ToArray();animator.runtimeAnimatorController=null;
   var cg=new GameObject("Motion review camera");var cam=cg.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.19f,.24f);cam.cullingMask=1<<31;cam.fieldOfView=32;cam.aspect=1;var rt=new RenderTexture(640,640,24);rt.Create();cam.targetTexture=rt;var tex=new Texture2D(640,640,TextureFormat.RGB24,false);
   yield return null;
   var renderers=actor.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);cam.transform.position=bounds.center+new Vector3(.25f,.1f,1).normalized*bounds.size.y*2.3f;cam.transform.LookAt(bounds.center);
   foreach(var clip in clips){var graph=PlayableGraph.Create("S22 motion review");var playable=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"Body",animator);output.SetSourcePlayable(playable);playable.SetSpeed(0);graph.Play();
    for(int i=0;i<8;i++){double time=clip.length*i/8d;playable.SetTime(time);graph.Evaluate(0);yield return null;yield return new WaitForEndOfFrame();var old=RenderTexture.active;RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();RenderTexture.active=old;File.WriteAllBytes(Folder+"/"+key+"-"+clip.name+"-"+i+".png",tex.EncodeToPNG());records.Add(key+" "+clip.name+" "+time.ToString("F3"));}
    graph.Destroy();
   }
   cam.targetTexture=null;rt.Release();Object.Destroy(cg);Object.Destroy(actor);Object.Destroy(rt);Object.Destroy(tex);yield return null;
  }
  File.WriteAllLines(Folder+"/complete.txt",records);
 }
 public static object Shoes(){if(!EditorApplication.isPlaying)throw new Exception("Play required");Object.FindFirstObjectByType<CanvasScript>().StartCoroutine(ShoeRoutine());return "Capturing all actual fitted shoes";}
 static IEnumerator ShoeRoutine(){var root=GameObject.Find("Noryangjin_Player/Original").transform;var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");foreach(var key in new[]{"shoes_original","shoes_ruby","shoes_mint","shoes_gold","shoes_steel","shoes_spring","shoes_relic","shoes_salvage"}){CosmeticAppearance.Apply(root,catalog,"skin_original",key,"hat_none");yield return null;yield return null;yield return new WaitForEndOfFrame();var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.GetComponent<SharkTailFootRig>()!=null);var camGo=new GameObject("Shoe fit review camera");var cam=camGo.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.fieldOfView=32;cam.transform.position=skin.bounds.center+root.rotation*new Vector3(.55f,.15f,1).normalized*6.5f;cam.transform.LookAt(skin.bounds.center);var rt=new RenderTexture(800,800,24);cam.targetTexture=rt;cam.aspect=1;yield return new WaitForEndOfFrame();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(800,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,800),0,0);image.Apply();RenderTexture.active=old;File.WriteAllBytes("tmp/image-previews/s22-polish-2026-10-01/"+key+"-fit.png",image.EncodeToPNG());cam.targetTexture=null;rt.Release();Object.Destroy(camGo);Object.Destroy(rt);Object.Destroy(image);}
  Object.FindFirstObjectByType<PlayerCosmeticCustomizer>().RefreshAppearance();File.WriteAllText("outputs/s22-polish-2026-10-01/shoe-capture-complete.txt","Eight shoe styles captured without purchases or saved equipment changes.");}
}
