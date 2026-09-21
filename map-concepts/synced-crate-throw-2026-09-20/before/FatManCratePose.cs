using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // Own the crate independently of either hand. Run after route facing and
    // grounding, then solve both short arms against the same stable handles.
    [DisallowMultipleComponent, DefaultExecutionOrder(200)]
    public sealed class FatManCratePose : MonoBehaviour
    {
        [SerializeField] private Animator model;
        [SerializeField] private Transform crate;
        [SerializeField] private Vector3 crateSize = new(.76f, .28f, .36f);
        [SerializeField] private Vector3 carryOffset = new(0f, .065f, .64f);
        private static readonly Vector3 LeftPalm = new(.030800f, .131140f, .022211f);
        private static readonly Vector3 RightPalm = new(-.022836f, .124187f, .046591f);
        private static readonly Quaternion LeftRotation = new(.331203f, .373332f, .723902f, .476336f);
        private static readonly Quaternion RightRotation = new(.292698f, -.349589f, -.696775f, .553733f);
        private const float ReleaseRecovery = .2f;
        private Transform chest;
        private Arm left, right;
        private Bounds meshBounds;
        private EnemyEventController owner;
        private bool ready, windingUp, released;
        private bool firstEvaluation;
        private Renderer crateRenderer;
        private bool hiddenUntilPosed, originalRenderingOff;
        private float elapsed, duration, recovery;
        public Transform Crate => crate;
        public Animator Model => model;
        public bool IsHolding => !released;
        public Vector3 LeftGrip { get; private set; }
        public Vector3 RightGrip { get; private set; }
        public Vector3 LeftPalmPosition => left.hand != null ? left.hand.TransformPoint(LeftPalm) : Vector3.zero;
        public Vector3 RightPalmPosition => right.hand != null ? right.hand.TransformPoint(RightPalm) : Vector3.zero;

        private struct Arm
        {
            public Transform shoulder, upper, forearm, hand;
            public Quaternion shoulderLocal, upperLocal, forearmLocal, handLocal;
            public bool posed;
        }

        public void Configure(Animator animator, Transform prop)
        {
            model = animator; crate = prop; ready = false;
            if (!Initialize()) return;
            if (crate.parent != model.transform) crate.SetParent(model.transform, false);
            crate.localRotation = Quaternion.identity;
            crate.localScale = new Vector3(crateSize.x / meshBounds.size.x,
                crateSize.y / meshBounds.size.y, crateSize.z / meshBounds.size.z);
            PlaceCrate(0f);
        }

        private void Awake() => Initialize();
        private void OnEnable()
        {
            firstEvaluation = true;
            if (!Initialize()) return;
            // Objects enabled from a coroutine can miss this frame's LateUpdate.
            // Reveal only once both hands have been solved, never for a loose frame.
            originalRenderingOff = crateRenderer.forceRenderingOff;
            crateRenderer.forceRenderingOff = true; hiddenUntilPosed = true;
        }
        private bool Initialize()
        {
            if (ready) return true;
            if (model == null || crate == null || !model.isHuman) return false;
            var filter = crate.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return false;
            meshBounds = filter.sharedMesh.bounds;
            crateRenderer = crate.GetComponent<Renderer>();
            if (crateRenderer == null) return false;
            chest = model.GetBoneTransform(HumanBodyBones.Chest);
            left = Bones(false); right = Bones(true);
            owner = GetComponent<EnemyEventController>();
            ready = chest != null && left.shoulder != null && right.shoulder != null &&
                left.upper != null && right.upper != null && left.forearm != null &&
                right.forearm != null && left.hand != null && right.hand != null;
            return ready;
        }

        private Arm Bones(bool r) => new()
        {
            shoulder = model.GetBoneTransform(r ? HumanBodyBones.RightShoulder : HumanBodyBones.LeftShoulder),
            upper = model.GetBoneTransform(r ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm),
            forearm = model.GetBoneTransform(r ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm),
            hand = model.GetBoneTransform(r ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand)
        };

        public void ResetCarry() { windingUp = released = false; elapsed = recovery = 0f; }
        public void BeginWindup(float releaseDelay)
        {
            elapsed = 0f; duration = Mathf.Max(.01f, releaseDelay); windingUp = true; released = false;
        }
        public void PrepareRelease() { if (Initialize()) PlaceCrate(1f); }
        public void Release() { released = true; windingUp = false; recovery = ReleaseRecovery; }

        private void Update()
        {
            if (!TimeManager.isGameRunning) return;
            // Animator may cull an offscreen actor. Restore the authored locals
            // before its next evaluation so procedural reach never accumulates.
            RestoreArm(ref left); RestoreArm(ref right);
            float delta = Time.deltaTime * Mathf.Max(0f, TimeManager.timeFactor);
            if (windingUp) elapsed += delta;
            if (released) recovery = Mathf.Max(0f, recovery - delta);
        }
        private void LateUpdate()
        {
            if (!TimeManager.isGameRunning || !Initialize() ||
                (owner != null && owner.RuntimeState == EnemyEventRuntimeState.Dead)) return;
            bool settling = firstEvaluation;
            if (settling)
            {
                // A re-enabled Animator can finish rebinding after OnEnable.
                // Solve against its evaluated idle pose on this first visible frame.
                model.Update(0f);
                GetComponent<EnemyGroundedPose>()?.Settle();
                firstEvaluation = false;
            }
            if (released && recovery <= 0f) return;
            float progress = released ? 1f : windingUp ? Mathf.Clamp01(elapsed / duration) : 0f;
            float weight = released ? recovery / ReleaseRecovery : 1f;
            ApplyPose(progress, weight);
            // The first rendered frame of a rebind can still rewrite the rig.
            // Keep the prop hidden for that settling frame, then reveal it held.
            if (!settling) RestoreRendering();
        }

        // Also used by native authoring/probes after evaluating an animation pose.
        public void ApplyPose(float progress, float weight = 1f)
        {
            if (!Initialize()) return;
            progress = Mathf.Clamp01(progress); weight = Mathf.Clamp01(weight);
            Vector3 center = PlaceCrate(progress);
            LeftGrip = Grip(center, false); RightGrip = Grip(center, true);
            FitArm(ref left, LeftGrip, LeftPalm, LeftRotation, false, progress, weight);
            FitArm(ref right, RightGrip, RightPalm, RightRotation, true, progress, weight);
        }

        private Vector3 PlaceCrate(float progress)
        {
            Vector3 center = model.transform.InverseTransformPoint(chest.position) + carryOffset;
            // Wind up without retracting the rear wall into the belly.
            center.y -= .025f * Mathf.Sin(progress * Mathf.PI);
            center.z += .018f * progress * progress;
            crate.localRotation = Quaternion.identity;
            crate.localPosition = center - Vector3.Scale(meshBounds.center, crate.localScale);
            return center;
        }
        private Vector3 Grip(Vector3 center, bool r) => model.transform.TransformPoint(center +
            new Vector3((r ? 1f : -1f) * (crateSize.x * .5f + .035f), crateSize.y * .30f, -crateSize.z * .5f + .04f));

        private void FitArm(ref Arm arm, Vector3 grip, Vector3 palm, Quaternion handRotation,
            bool isRight, float progress, float weight)
        {
            arm.shoulderLocal = arm.shoulder.localRotation; arm.upperLocal = arm.upper.localRotation;
            arm.forearmLocal = arm.forearm.localRotation; arm.handLocal = arm.hand.localRotation; arm.posed = true;
            Quaternion shoulderBefore = arm.shoulder.rotation, upperBefore = arm.upper.rotation,
                forearmBefore = arm.forearm.rotation, handBefore = arm.hand.rotation;
            Vector3 original = arm.upper.position - arm.shoulder.position;
            float reach = Mathf.Lerp(.08f, .32f, progress * progress);
            arm.shoulder.rotation = Quaternion.FromToRotation(original,
                original + model.transform.TransformVector(Vector3.forward * reach)) * arm.shoulder.rotation;
            Quaternion rotation = model.transform.rotation * handRotation;
            Vector3 wrist = grip - rotation * Vector3.Scale(palm, arm.hand.lossyScale);
            SolveArm(arm.upper, arm.forearm, arm.hand, wrist,
                model.transform.TransformDirection(new Vector3(isRight ? 1 : -1, -.6f, .1f)));
            arm.hand.rotation = rotation;
            Quaternion shoulderAfter = arm.shoulder.rotation, upperAfter = arm.upper.rotation,
                forearmAfter = arm.forearm.rotation, handAfter = arm.hand.rotation;
            arm.shoulder.rotation = Quaternion.Slerp(shoulderBefore, shoulderAfter, weight);
            arm.upper.rotation = Quaternion.Slerp(upperBefore, upperAfter, weight);
            arm.forearm.rotation = Quaternion.Slerp(forearmBefore, forearmAfter, weight);
            arm.hand.rotation = Quaternion.Slerp(handBefore, handAfter, weight);
        }

        private static void RestoreArm(ref Arm arm)
        {
            if (!arm.posed || arm.hand == null) return;
            arm.shoulder.localRotation = arm.shoulderLocal; arm.upper.localRotation = arm.upperLocal;
            arm.forearm.localRotation = arm.forearmLocal; arm.hand.localRotation = arm.handLocal; arm.posed = false;
        }
        private void RestoreRendering()
        {
            if (!hiddenUntilPosed || crateRenderer == null) return;
            crateRenderer.forceRenderingOff = originalRenderingOff; hiddenUntilPosed = false;
        }
        private void OnDisable() { RestoreArm(ref left); RestoreArm(ref right); RestoreRendering(); }

        public static void SolveArm(Transform upper, Transform forearm, Transform hand, Vector3 target, Vector3 pole)
        {
            Vector3 origin = upper.position, delta = target - origin;
            float a = Vector3.Distance(origin, forearm.position), b = Vector3.Distance(forearm.position, hand.position);
            if (a < .0001f || b < .0001f || delta.sqrMagnitude < .000001f) return;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .0001f, a + b - .0001f);
            Vector3 forward = delta.normalized, bend = Vector3.ProjectOnPlane(pole, forward).normalized;
            if (bend.sqrMagnitude < .001f) bend = Vector3.Cross(forward, Vector3.up).normalized;
            float along = (a * a - b * b + distance * distance) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            Vector3 elbow = origin + forward * along + bend * height;
            upper.rotation = Quaternion.FromToRotation(forearm.position - origin, elbow - origin) * upper.rotation;
            forearm.rotation = Quaternion.FromToRotation(hand.position - forearm.position, target - forearm.position) * forearm.rotation;
        }
    }
}
