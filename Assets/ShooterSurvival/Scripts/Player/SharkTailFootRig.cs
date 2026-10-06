using System;
using UnityEngine;

// Reuse the existing rear footfall without retaining two visible rear legs.
// Translation is applied in the model frame, so foot rotation never sweeps the
// new tail shoe around the old hip. Original animation curves stay untouched.
[DefaultExecutionOrder(10000)]
public sealed class SharkTailFootRig : MonoBehaviour
{
    private static readonly int[] SourceIndices={6,7,8,9};
    private static readonly string[] Names={"TailRoot","TailBend","TailAnkle","TailFoot"};
    private SkinnedMeshRenderer body;
    private Transform[] originals,proxies;
    private Matrix4x4 rootBind;
    private Matrix4x4[] sourceBind;
    private Animator animator;
    private Vector3 offset;

    public static void Ensure(SkinnedMeshRenderer renderer,CosmeticVisualCatalog catalog)
    {
        if(!catalog.usesTailFoot)return;
        var rig=renderer.GetComponent<SharkTailFootRig>();
        if(rig==null)rig=renderer.gameObject.AddComponent<SharkTailFootRig>();
        rig.Configure(renderer,catalog);
    }
    private void Configure(SkinnedMeshRenderer renderer,CosmeticVisualCatalog catalog)
    {
        body=renderer;offset=catalog.tailFootOffset;
        int originalCount=catalog.sourceSharkMesh.bindposes.Length;
        if(originalCount<10||body.bones.Length<originalCount)throw new InvalidOperationException("Tail foot requires the original shark rig.");
        if(originals==null)
        {
            originals=new Transform[originalCount];Array.Copy(body.bones,originals,originalCount);sourceBind=catalog.sourceSharkMesh.bindposes;rootBind=sourceBind[0];animator=body.GetComponentInParent<Animator>();
            proxies=new Transform[SourceIndices.Length];
            for(int i=0;i<proxies.Length;i++){var bone=new GameObject(Names[i]);bone.transform.SetParent(body.transform,false);proxies[i]=bone.transform;}
        }
        var bones=new Transform[originals.Length+proxies.Length];Array.Copy(originals,bones,originals.Length);Array.Copy(proxies,0,bones,originals.Length,proxies.Length);body.bones=bones;
        Synchronize();
    }
    public void Synchronize()
    {
        if(body==null||originals==null)return;
        float bindScale=(body.transform.worldToLocalMatrix*originals[0].localToWorldMatrix*rootBind).lossyScale.x;
        Vector3 delta=body.transform.TransformVector(offset*bindScale),parentScale=body.transform.lossyScale;
        bool idle=animator!=null&&animator.runtimeAnimatorController!=null&&!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
        var neutralRoot=originals[0].localToWorldMatrix*rootBind;
        for(int i=0;i<proxies.Length;i++)
        {
            var source=originals[SourceIndices[i]];var proxy=proxies[i];
            var pose=neutralRoot*sourceBind[SourceIndices[i]].inverse;
            // The legacy idle freezes a lifted rear toe. Keep the tail planted in
            // Idle; walking/death still follow the real authored animation.
            proxy.SetPositionAndRotation((idle?(Vector3)pose.GetColumn(3):source.position)+delta,idle?pose.rotation:source.rotation);
            var size=idle?pose.lossyScale:source.lossyScale;
            proxy.localScale=new Vector3(size.x/parentScale.x,size.y/parentScale.y,size.z/parentScale.z);
        }
    }
    private void LateUpdate()=>Synchronize();
}
