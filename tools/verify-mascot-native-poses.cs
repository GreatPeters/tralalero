if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
var names=new[]{"ConeMechanic","TrafficPatrol","TollgateChief","TireBruiser","AsphaltWorker","DeliveryRider","SnackChef","CoffeeVendor","ParkingMarshal"};
var reports=new System.Collections.Generic.List<object>();
foreach(var name in names){
 var root=UnityEditor.PrefabUtility.LoadPrefabContents(ChapterMascotImporter.PrefabPath(name));
 var mesh=new UnityEngine.Mesh();
 try{
  var animator=root.GetComponentInChildren<UnityEngine.Animator>(true);
  var transforms=animator.GetComponentsInChildren<UnityEngine.Transform>(true);
  var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
  void ResetPose(){for(int i=0;i<transforms.Length;i++){transforms[i].SetLocalPositionAndRotation(positions[i],rotations[i]);transforms[i].localScale=scales[i];}}
  var bodies=animator.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Where(r=>!r.name.EndsWith("_Equipment")).ToArray();
  var skins=bodies.Select(body=>new{body,vertices=body.sharedMesh.vertices,weights=body.sharedMesh.boneWeights,bindposes=body.sharedMesh.bindposes}).ToArray();
  var clips=animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
  var poses=new System.Collections.Generic.List<object>();
  foreach(var clip in clips){
   float minimum=float.PositiveInfinity,maximum=float.NegativeInfinity;
   foreach(float fraction in Enumerable.Range(0,61).Select(frame=>frame/60f)){
    ResetPose();
    clip.SampleAnimation(animator.gameObject,clip.length*fraction);
    float bottom=float.PositiveInfinity;
    foreach(var skin in skins){
     var vertices=skin.vertices;var weights=skin.weights;
     var matrices=skin.body.bones.Select((bone,i)=>bone.localToWorldMatrix*skin.bindposes[i]).ToArray();
     for(int i=0;i<vertices.Length;i++){
      var w=weights[i];var vertex=vertices[i];
      var world=matrices[w.boneIndex0].MultiplyPoint3x4(vertex)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(vertex)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(vertex)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(vertex)*w.weight3;
      bottom=UnityEngine.Mathf.Min(bottom,world.y);
     }
    }
    minimum=UnityEngine.Mathf.Min(minimum,bottom);maximum=UnityEngine.Mathf.Max(maximum,bottom);
   }
   poses.Add(new{clip=clip.name,seconds=clip.length,minY=minimum,maxY=maximum});
  }
  reports.Add(new{name,bones=bodies.SelectMany(r=>r.bones).Distinct().Count(),actions=clips.Length,ranged=root.GetComponent<IndianOceanAssets.ShooterSurvival.EnemyScript_space>().HasConfiguredProjectile,poses});
 }finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
const string file="map-concepts/chapters-polish-2026-09-12/review/native-poses-skin-matrices-60fps.json";
System.IO.File.WriteAllText(file,(string)serialize.Invoke(null,new object[]{reports}));
return new{file,characters=reports.Count};
