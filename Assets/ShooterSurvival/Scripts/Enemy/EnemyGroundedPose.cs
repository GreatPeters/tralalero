using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // Keep imported humanoid animation offsets in the visual hierarchy. Gameplay
    // position, contact collider and route checkpoints stay on the authored root.
    [DefaultExecutionOrder(100)]
    public sealed class EnemyGroundedPose : MonoBehaviour
    {
        public Animator model;
        public float soleHeight = .12f;
        private Vector3 authoredPosition;
        private Transform leftFoot, rightFoot, leftToes, rightToes;
        private EnemyEventController owner;
        public float AppliedLift { get; private set; }
        private void Awake()
        {
            model ??= GetComponentInChildren<Animator>();
            owner = GetComponent<EnemyEventController>();
            if (model == null || !model.isHuman || model.transform == transform) return;
            authoredPosition = model.transform.localPosition;
            leftFoot=model.GetBoneTransform(HumanBodyBones.LeftFoot);rightFoot=model.GetBoneTransform(HumanBodyBones.RightFoot);
            leftToes=model.GetBoneTransform(HumanBodyBones.LeftToes);rightToes=model.GetBoneTransform(HumanBodyBones.RightToes);
        }
        private void Update()
        {
            if (leftFoot == null || rightFoot == null || (owner != null && (!owner.VisualIsRelevant || owner.RuntimeState == EnemyEventRuntimeState.Dead))) return;
            model.transform.localPosition = authoredPosition;
        }
        private void LateUpdate() { if(owner == null || owner.VisualIsRelevant) Settle(); }
        public void Settle()
        {
            if (leftFoot == null || rightFoot == null || (owner != null && owner.RuntimeState == EnemyEventRuntimeState.Dead)) return;
            float footY=Mathf.Min(leftFoot.position.y,rightFoot.position.y);
            if(leftToes!=null)footY=Mathf.Min(footY,leftToes.position.y);
            if(rightToes!=null)footY=Mathf.Min(footY,rightToes.position.y);
            float lift=Mathf.Max(0f,transform.position.y+soleHeight-footY);
            model.transform.position+=Vector3.up*lift;
            AppliedLift=Vector3.Dot(model.transform.position-transform.TransformPoint(authoredPosition),Vector3.up);
        }
    }
}
