var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(ChapterMascotImporter.PrefabPath("ConeMechanic"));
var animator=prefab.GetComponentInChildren<UnityEngine.Animator>(true);
var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name.EndsWith("walk"));
return new{avatar=animator.avatar.name,rootBone=animator.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>().rootBone.name,
 transforms=animator.GetComponentsInChildren<UnityEngine.Transform>(true).Take(8).Select(t=>new{name=t.name,path=UnityEditor.AnimationUtility.CalculateTransformPath(t,animator.transform)}).ToArray(),
 curves=UnityEditor.AnimationUtility.GetCurveBindings(clip).Where(b=>b.propertyName.Contains("Position")||b.propertyName.Contains("RootT")||b.propertyName.Contains("MotionT")).Take(12).Select(b=>new{b.path,b.propertyName,keys=UnityEditor.AnimationUtility.GetEditorCurve(clip,b).keys.Take(4).Select(k=>new{k.time,k.value}).ToArray()}).ToArray()};
