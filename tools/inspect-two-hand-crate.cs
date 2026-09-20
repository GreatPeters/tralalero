using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;
public static class InspectTwoHandCrate
{
 public static string Main()
 {
  var source=UnityEngine.Object.FindObjectsByType<EnemyEventController>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(e=>e.name.Contains("FatMan"));
  var go=UnityEngine.Object.Instantiate(source.gameObject);go.hideFlags=HideFlags.HideAndDontSave;go.transform.SetPositionAndRotation(new Vector3(10000,0,10000),Quaternion.identity);
  var a=go.GetComponentInChildren<Animator>();a.transform.localRotation=Quaternion.identity;
  a.runtimeAnimatorController.animationClips.First(c=>c.name=="Fatman_ThrowShort").SampleAnimation(a.gameObject,0);
  var bones=new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.RightMiddleProximal}.Select(b=>{var t=a.GetBoneTransform(b);return b+"="+(t!=null?a.transform.InverseTransformPoint(t.position).ToString("F4")+" rot="+(Quaternion.Inverse(a.transform.rotation)*t.rotation).eulerAngles.ToString("F3"):"null");});
  var result=string.Join("\n",bones)+"\nanimscale="+a.transform.lossyScale;
  foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
  {
   var mesh=new Mesh();skin.BakeMesh(mesh,true);var points=mesh.vertices.Select(v=>a.transform.InverseTransformPoint(skin.transform.TransformPoint(v))).ToArray();
   result+=$"\nSKIN {skin.name}: active={skin.gameObject.activeInHierarchy} meshbounds={mesh.bounds} vertices={points.Length}, yrange={points.Min(p=>p.y):F4}..{points.Max(p=>p.y):F4}, x={points.Min(p=>p.x):F4}..{points.Max(p=>p.x):F4}, z={points.Min(p=>p.z):F4}..{points.Max(p=>p.z):F4}";
   foreach(var span in new[]{new[]{.5f,.8f},new[]{.8f,1.1f},new[]{1.1f,1.3f},new[]{1.3f,1.7f}})
   {var filtered=points.Where(p=>Mathf.Abs(p.x)<.35f&&p.y>span[0]&&p.y<span[1]).ToArray();if(filtered.Length>0)result+=$"\n{skin.name} torso y={span[0]}..{span[1]} zmax={filtered.Max(p=>p.z):F4}";}
   UnityEngine.Object.DestroyImmediate(mesh);
  }
  UnityEngine.Object.DestroyImmediate(go);return result;
 }
}
