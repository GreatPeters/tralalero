#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class FatManTwoHandPoseTests
{
    [Test]
    public void ReleaseWaitsForTheCompletedForwardPose_NotOnlyElapsedTime()
    {
        var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_FatMan.prefab"));
        try
        {
            var pose=actor.GetComponent<FatManCratePose>();pose.BeginWindup(.62f);pose.ApplyPose(0);
            Assert.That(pose.ReadyToRelease,Is.False);
            typeof(FatManCratePose).GetField("elapsed",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(pose,.62f);
            Assert.That(pose.ReadyToRelease,Is.False,"A timer must not launch while the hands are still winding up");
            var before=pose.Crate.position;pose.ApplyPose(1);
            Assert.That(pose.ReadyToRelease,Is.True);
            Assert.That(Vector3.Dot(pose.Crate.position-before,pose.Model.transform.forward),Is.GreaterThan(.19f));
            pose.Release();Assert.That(pose.ReadyToRelease,Is.False,"Release must be consumed once");
        }
        finally{Object.DestroyImmediate(actor);}
    }

    [Test]
    public void HoldingPoseIgnoresImportedAnimationTwistAndKeepsItsSurfaceShader()
    {
        var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_FatMan.prefab"));
        try
        {
            var pose=actor.GetComponent<FatManCratePose>();Assert.That(pose.HasReferencePose,Is.True);
            var animator=pose.Model;var rotation=animator.transform.localRotation;
            var upper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var fore=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            pose.ApplyPose(.6f);var arm=upper.localRotation;var elbow=fore.position;var box=pose.Crate.position;
            var clip=animator.runtimeAnimatorController.animationClips.First(c=>c.name=="Fatman_ThrowShort");
            for(int i=0;i<20;i++)
            {
                clip.SampleAnimation(animator.gameObject,i*.05f);animator.transform.localRotation=rotation;pose.ApplyPose(.6f);
                Assert.That(Quaternion.Angle(upper.localRotation,arm),Is.LessThan(.01f));
                Assert.That(Vector3.Distance(fore.position,elbow),Is.LessThan(.0001f));
                Assert.That(Vector3.Distance(pose.Crate.position,box),Is.LessThan(.0001f));
            }
            var material=actor.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterial;
            Assert.That(material.shader.name,Is.EqualTo("FlatKit/Stylized Surface"));
            Assert.That(material.GetFloat("_OutlineEnabled"),Is.Zero);
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/JH/Model/Enemy/Fat_Throw/던짐_스킨/Material.001.mat");
            Assert.That(material.GetTexture("_BaseMap"),Is.EqualTo(source.GetTexture("_BaseMap")));
            Assert.That(material.GetColor("_BaseColor"),Is.EqualTo(source.GetColor("_BaseColor")));
        }
        finally{Object.DestroyImmediate(actor);}
    }

    [TestCase(0f)] [TestCase(90f)] [TestCase(180f)] [TestCase(270f)]
    public void EntireWindup_KeepsBothPalmsOnTheHandlesAndTorsoOutsideTheCrate(float yaw)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_FatMan.prefab");
        var actor=Object.Instantiate(prefab);var mesh=new Mesh();
        try
        {
            actor.transform.rotation=Quaternion.Euler(0,yaw,0);
            var pose=actor.GetComponent<FatManCratePose>();Assert.That(pose,Is.Not.Null);
            var animator=pose.Model;var modelRotation=animator.transform.localRotation;
            Assert.That(pose.Crate.parent,Is.EqualTo(animator.transform));
            var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var weights=skin.sharedMesh.boneWeights;
            bool Body(int index)
            {
                string name=skin.bones[index].name;
                return name.Contains("Spine")||name.Contains("Hips")||name.Contains("Neck")||name.Contains("Head");
            }
            var indices=Enumerable.Range(0,weights.Length).Where(i=>
            {
                var w=weights[i];return (Body(w.boneIndex0)?w.weight0:0)+(Body(w.boneIndex1)?w.weight1:0)+
                    (Body(w.boneIndex2)?w.weight2:0)+(Body(w.boneIndex3)?w.weight3:0)>.5f;
            }).ToArray();
            Assert.That(indices.Length,Is.GreaterThan(100));
            var bounds=pose.Crate.GetComponent<MeshFilter>().sharedMesh.bounds;
            foreach(var clip in animator.runtimeAnimatorController.animationClips.Where(c=>c.name=="Fatman_ThrowShort"||c.name=="Fatman Idle").Distinct())
            for(int frame=0;frame<=31;frame++)
            {
                float time=frame*.02f;clip.SampleAnimation(animator.gameObject,time);animator.transform.localRotation=modelRotation;
                pose.ApplyPose(clip.name=="Fatman Idle"?0f:time/.62f);
                Assert.That(Vector3.Distance(pose.LeftPalmPosition,pose.LeftGrip),Is.LessThan(.015f),"Left palm "+clip.name+" at "+time);
                Assert.That(Vector3.Distance(pose.RightPalmPosition,pose.RightGrip),Is.LessThan(.015f),"Right palm "+clip.name+" at "+time);
                // Unity 6 compensation is required for this renderer's imported scale of 100.
                skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
                int inside=indices.Count(i=>bounds.Contains(pose.Crate.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]))));
                Assert.That(inside,Is.Zero,"Torso/head vertices inside crate at "+time+"s, yaw "+yaw);
            }
        }
        finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(mesh);}
    }
}
#endif
