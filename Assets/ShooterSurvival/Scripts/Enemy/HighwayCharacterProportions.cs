using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [DefaultExecutionOrder(150)]
    public sealed class HighwayCharacterProportions : MonoBehaviour
    {
        public Transform leftHand,rightHand,head;
        public float handScale=.82f,headScale=.94f;
        EnemyEventController owner;
        void Awake()=>owner=GetComponentInParent<EnemyEventController>();
        void LateUpdate()
        {
            if(owner!=null&&!owner.VisualIsRelevant)return;
            // Full-key imported clips restore unit bone scale every frame.
            if(leftHand!=null)leftHand.localScale=Vector3.one*handScale;
            if(rightHand!=null)rightHand.localScale=Vector3.one*handScale;
            if(head!=null)head.localScale=Vector3.one*headScale;
        }
    }
}
