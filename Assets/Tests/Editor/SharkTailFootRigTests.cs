using NUnit.Framework;
using UnityEngine;

public sealed class SharkTailFootRigTests
{
    [Test] public void TailFootOffsetDoesNotRotateAroundTheOldHipAndRepeatedEquipKeepsFourProxies()
    {
        var root=new GameObject("Tail rig fixture");var catalog=ScriptableObject.CreateInstance<CosmeticVisualCatalog>();var mesh=new Mesh();
        try
        {
            root.transform.localScale=Vector3.one*2;var skin=root.AddComponent<SkinnedMeshRenderer>();var bones=new Transform[27];var bind=new Matrix4x4[27];
            for(int i=0;i<bones.Length;i++){bones[i]=new GameObject("Bone"+i).transform;bones[i].SetParent(root.transform,false);bones[i].localPosition=new Vector3(i*.02f,0,0);bind[i]=bones[i].worldToLocalMatrix*root.transform.localToWorldMatrix;}
            mesh.bindposes=bind;skin.sharedMesh=mesh;skin.bones=bones;catalog.sourceSharkMesh=mesh;catalog.usesTailFoot=true;catalog.tailFootOffset=new Vector3(.5f,0,-1.5f);
            SharkTailFootRig.Ensure(skin,catalog);Assert.That(skin.bones.Length,Is.EqualTo(31));var rig=root.GetComponent<SharkTailFootRig>();
            bones[9].localRotation=Quaternion.Euler(45,60,10);bones[9].position+=Vector3.up*.3f;rig.Synchronize();
            Assert.That(Vector3.Distance(skin.bones[30].position-bones[9].position,root.transform.TransformVector(catalog.tailFootOffset)),Is.LessThan(.0001f));
            var originalPoint=new Vector3(.1f,.2f,.3f);var translatedPoint=originalPoint+catalog.tailFootOffset;
            var movedBind=bind[9]*Matrix4x4.Translate(-catalog.tailFootOffset);
            var oldWorld=bones[9].localToWorldMatrix.MultiplyPoint3x4(bind[9].MultiplyPoint3x4(originalPoint));
            var newWorld=skin.bones[30].localToWorldMatrix.MultiplyPoint3x4(movedBind.MultiplyPoint3x4(translatedPoint));
            Assert.That(Vector3.Distance(newWorld-oldWorld,root.transform.TransformVector(catalog.tailFootOffset)),Is.LessThan(.0001f));
            SharkTailFootRig.Ensure(skin,catalog);Assert.That(root.transform.childCount,Is.EqualTo(31));
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(catalog);Object.DestroyImmediate(mesh);}
    }
}
