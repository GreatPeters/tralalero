using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [DefaultExecutionOrder(180)]
    public sealed class WomanKnifeGrip : MonoBehaviour
    {
        public Transform knife;
        public Transform[] fingers;
        public Quaternion[] fingerRotations;
        public Vector3[] fingerPositions;
        public Vector3 gripInHand=new(-.027f,.127f,.055f);
        public float fingerCurlDegrees=20;
        public Vector3 handlePoint=new(-.0065f,-.0015f,0);
        EnemyEventController owner;
        void Awake()=>owner=GetComponent<EnemyEventController>();
        void LateUpdate()
        {
            if(owner!=null&&(!owner.VisualIsRelevant||owner.RuntimeState==EnemyEventRuntimeState.Dead))return;
            ApplyGrip();
        }
        public void ApplyGrip()
        {
            if(fingers!=null&&fingerRotations!=null&&fingerPositions!=null)
                for(int i=0;i<fingers.Length&&i<fingerRotations.Length&&i<fingerPositions.Length;i++)if(fingers[i]!=null)
                {
                    fingers[i].localRotation=fingerRotations[i];fingers[i].localPosition=fingerPositions[i];
                    if(fingers[i].name.Contains("RightHandIndex")&&!fingers[i].name.EndsWith("4"))fingers[i].localRotation*=Quaternion.Euler(fingerCurlDegrees,0,0);
                }
            if(knife==null)return;
            knife.localRotation=Quaternion.Euler(0,180,0);
            knife.localPosition=gripInHand-knife.localRotation*Vector3.Scale(handlePoint,knife.localScale);
        }
    }
}
