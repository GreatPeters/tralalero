var root=UnityEditor.PrefabUtility.LoadPrefabContents(ChapterMascotImporter.PrefabPath("ConeMechanic"));
var mesh=new UnityEngine.Mesh();
try{
 var animator=root.GetComponentInChildren<UnityEngine.Animator>();var bone=animator.transform.Find("Rig/Root");
 var body=animator.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().Single(r=>!r.name.EndsWith("_Equipment"));
 var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name.EndsWith("die"));
 float Bottom(){body.BakeMesh(mesh,false);return mesh.vertices.Min(v=>body.transform.TransformPoint(v).y);}
 var rows=new System.Collections.Generic.List<object>();
 foreach(float time in new[]{0f,.25f,.5f,.75f,1f}){
  clip.SampleAnimation(animator.gameObject,time);float before=Bottom();var position=bone.localPosition;var delta=bone.parent.InverseTransformVector(UnityEngine.Vector3.up*-before);bone.localPosition+=delta;
  rows.Add(new{time,before,after=Bottom(),position=position.ToString("F8"),delta=delta.ToString("F8"),parentScale=bone.parent.lossyScale.ToString(),parentRotation=bone.parent.rotation.ToString("F4")});
 }
 return new{length=clip.length,bodyScale=body.transform.lossyScale.ToString(),meshSize=mesh.bounds.size.ToString(),renderSize=body.bounds.size.ToString(),rows};
}finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
