using System;
using System.Linq;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // One live-pose owner and one release clock. The Animator is retained for
    // death, but cannot rewrite the holding/throwing rig underneath these poses.
    [DisallowMultipleComponent, DefaultExecutionOrder(200)]
    public sealed class FatManCratePose : MonoBehaviour
    {
        [SerializeField] private Animator model;
        [SerializeField] private Transform crate;
        [SerializeField] private Vector3 crateSize = new(.76f, .28f, .36f);
        [SerializeField] private Vector3 carryOffset = new(0f, .065f, .64f);
        [SerializeField, HideInInspector] private BonePose[] referencePose = Array.Empty<BonePose>();
        [SerializeField, HideInInspector] private Vector3 referenceModelPosition, referenceChest;
        private static readonly Vector3 LeftPalm = new(.030800f, .131140f, .022211f);
        private static readonly Vector3 RightPalm = new(-.022836f, .124187f, .046591f);
        private static readonly Quaternion LeftRotation = new(.331203f, .373332f, .723902f, .476336f);
        private static readonly Quaternion RightRotation = new(.292698f, -.349589f, -.696775f, .553733f);
        private const float FollowThroughSeconds = .14f;
        private const float ReturnSeconds = .4f;
        private Transform chest, spine;
        private Arm left, right;
        private Bounds meshBounds;
        private EnemyEventController owner;
        private EnemyScript_space combat;
        private EnemyGroundedPose grounding;
        private bool ready, windingUp, released;
        private float elapsed, duration, recovery;
        private Vector3 leftGripLocal, rightGripLocal;
        public Transform Crate => crate;
        public Animator Model => model;
        public bool HasReferencePose => referencePose != null && referencePose.Length > 0;
        public bool ControlsAlivePose => isActiveAndEnabled && HasReferencePose;
        public bool IsHolding => !released;
        public bool ReadyToRelease => windingUp && elapsed >= duration && StrokeProgress >= 1f;
        public float ThrowProgress => windingUp ? Mathf.Clamp01(elapsed / duration) : released ? 1f : 0f;
        public float StrokeProgress { get; private set; }
        public Vector3 LeftGrip => model != null ? model.transform.TransformPoint(leftGripLocal) : Vector3.zero;
        public Vector3 RightGrip => model != null ? model.transform.TransformPoint(rightGripLocal) : Vector3.zero;
        public Vector3 LeftPalmPosition => left.hand != null ? left.hand.TransformPoint(LeftPalm) : Vector3.zero;
        public Vector3 RightPalmPosition => right.hand != null ? right.hand.TransformPoint(RightPalm) : Vector3.zero;

        [Serializable] private struct BonePose
        {
            public Transform bone;
            public Vector3 position, scale;
            public Quaternion rotation;
        }
        private struct Arm { public Transform shoulder, upper, forearm, hand; }

        public void Configure(Animator animator, Transform prop)
        {
            model = animator; crate = prop; ready = false;
            if (!Initialize()) return;
            if (crate.parent != model.transform) crate.SetParent(model.transform, false);
            crate.localRotation = Quaternion.identity;
            crate.localScale = new Vector3(crateSize.x / meshBounds.size.x,
                crateSize.y / meshBounds.size.y, crateSize.z / meshBounds.size.z);
            if (!HasReferencePose) CaptureReferencePose();
            PlaceCrate(0f, 0f);
        }

        // Author this from a sampled, quiet idle clip via the Unity editor.
        public void CaptureReferencePose()
        {
            if (!Initialize()) return;
            referencePose = model.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("mixamorig:", StringComparison.Ordinal))
                .Select(t => new BonePose { bone = t, position = t.localPosition,
                    rotation = t.localRotation, scale = t.localScale }).ToArray();
            referenceModelPosition = model.transform.localPosition;
            referenceChest = model.transform.InverseTransformPoint(chest.position);
        }

        private void Awake() => Initialize();
        private void OnEnable()
        {
            if (!Application.isPlaying || !Initialize() || !HasReferencePose) return;
            model.enabled = false;
            ResetCarry();
            ApplyPose(0f);
        }
        private bool Initialize()
        {
            if (ready) return true;
            if (model == null || crate == null || !model.isHuman) return false;
            var filter = crate.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return false;
            meshBounds = filter.sharedMesh.bounds;
            chest = model.GetBoneTransform(HumanBodyBones.Chest);
            spine = model.GetBoneTransform(HumanBodyBones.Spine);
            left = Bones(false); right = Bones(true);
            owner = GetComponent<EnemyEventController>();
            combat = GetComponent<EnemyScript_space>();
            grounding = GetComponent<EnemyGroundedPose>();
            ready = chest != null && spine != null && left.shoulder != null && right.shoulder != null &&
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

        public void ResetCarry() { windingUp = released = false; elapsed = recovery = 0f; StrokeProgress = 0f; }
        public void BeginWindup(float releaseDelay)
        {
            elapsed = 0f; duration = Mathf.Max(.01f, releaseDelay); windingUp = true; released = false;
        }
        public void Release() { released = true; windingUp = false; recovery = 0f; }

        private void Update()
        {
            if (!TimeManager.isGameRunning || (owner != null && owner.RuntimeState == EnemyEventRuntimeState.Dead)) return;
            float delta = Time.deltaTime * Mathf.Max(0f, TimeManager.timeFactor);
            if (windingUp) elapsed = Mathf.Min(duration, elapsed + delta);
            if (released) recovery = Mathf.Min(FollowThroughSeconds + ReturnSeconds, recovery + delta);
        }
        private void LateUpdate()
        {
            if (!Initialize() || !HasReferencePose ||
                (owner != null && owner.RuntimeState == EnemyEventRuntimeState.Dead)) return;
            model.enabled = false;
            // Apply even while paused: grounding must not leave the model at an
            // unposed height. Only Update advances this motion clock.
            if (released) ApplyRecovery();
            else ApplyPose(ThrowProgress);

            // The final hand pose and crate origin exist before the sole launch.
            if (TimeManager.isGameRunning && ReadyToRelease && combat != null)
                combat.ReleaseCrateAtPose();
        }

        public void ApplyPose(float progress)
        {
            if (!Initialize() || !HasReferencePose) return;
            progress = Mathf.Clamp01(progress);
            float windup = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / .3f));
            float stroke = Mathf.Clamp01((progress - .3f) / .7f);
            ApplyMotion(stroke * stroke, windup, 0f);
        }

        private void ApplyRecovery()
        {
            if (recovery < FollowThroughSeconds)
            {
                float t = recovery / FollowThroughSeconds;
                ApplyMotion(1f + .12f * (1f - (1f - t) * (1f - t)), 1f, .05f * t);
            }
            else
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((recovery - FollowThroughSeconds) / ReturnSeconds));
                ApplyMotion(1.12f * (1f - t), 1f - t, .05f * (1f - t));
            }
        }
        private void RestoreReference()
        {
            model.transform.localPosition = referenceModelPosition;
            foreach (var pose in referencePose)
                if (pose.bone != null)
                { pose.bone.SetLocalPositionAndRotation(pose.position, pose.rotation); pose.bone.localScale = pose.scale; }
        }
        private void ApplyMotion(float stroke, float windup, float open)
        {
            RestoreReference();
            StrokeProgress = stroke;
            spine.rotation = Quaternion.AngleAxis(10f * stroke, model.transform.right) * spine.rotation;
            chest.rotation = Quaternion.AngleAxis(8f * stroke, model.transform.right) * chest.rotation;
            grounding?.Settle();
            Vector3 center = PlaceCrate(stroke, windup);
            leftGripLocal = Grip(center, false, open); rightGripLocal = Grip(center, true, open);
            FitArm(left, LeftGrip, LeftPalm, LeftRotation, false, stroke);
            FitArm(right, RightGrip, RightPalm, RightRotation, true, stroke);
        }
        private Vector3 PlaceCrate(float stroke, float windup)
        {
            Vector3 center = referenceChest + carryOffset;
            center.y += -.035f * windup + .05f * stroke;
            center.z += .16f * stroke;
            crate.SetLocalPositionAndRotation(center - Vector3.Scale(meshBounds.center, crate.localScale), Quaternion.identity);
            return center;
        }
        private Vector3 Grip(Vector3 center, bool rightSide, float open) => center +
            new Vector3((rightSide ? 1f : -1f) * (crateSize.x * .5f + .035f + open),
                crateSize.y * .30f, -crateSize.z * .5f + .04f);

        private void FitArm(Arm arm, Vector3 grip, Vector3 palm, Quaternion handRotation, bool isRight, float stroke)
        {
            Vector3 original = arm.upper.position - arm.shoulder.position;
            arm.shoulder.rotation = Quaternion.FromToRotation(original,
                original + model.transform.TransformVector(Vector3.forward * (.08f + .24f * stroke))) * arm.shoulder.rotation;
            Quaternion rotation = model.transform.rotation * handRotation;
            Vector3 wrist = grip - rotation * Vector3.Scale(palm, arm.hand.lossyScale);
            SolveArm(arm.upper, arm.forearm, arm.hand, wrist,
                model.transform.TransformDirection(new Vector3(isRight ? 1 : -1, -.6f, .1f)));
            arm.hand.rotation = rotation;
        }

        private void OnDisable()
        {
            if (!ready || model == null) return;
            if (HasReferencePose) RestoreReference();
            model.enabled = true;
        }

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
