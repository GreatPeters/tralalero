using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace IndianOceanAssets.ShooterSurvival
{
    [MovedFrom(
        true,
        sourceNamespace: "IndianOceanAssets.ShooterSurvival",
        sourceAssembly: "Assembly-CSharp",
        sourceClassName: "EnemyMovementActivationTrigger")]
    [AddComponentMenu("Shooter Survival/Enemy Event Activation Spot")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EnemyEventActivationSpot : MonoBehaviour
    {
        private static readonly HashSet<EnemyEventActivationSpot> ConsumedSpots = new();

        public static readonly Vector3 DefaultColliderCenter =
            new(0f, 1f, 0f);
        public static readonly Vector3 DefaultColliderSize =
            new(4f, 2f, 0.8f);

        [Tooltip("플레이어가 이 영역에 들어오면 발동할 적입니다.")]
        [HideInInspector]
        [SerializeField] private EnemyEventController[] targets =
            Array.Empty<EnemyEventController>();

        public EnemyEventController[] Targets
        {
            get => targets;
            set => targets = value ?? Array.Empty<EnemyEventController>();
        }

        private void Reset()
        {
            ConfigureCollider(GetComponent<BoxCollider>(), applyDefaultShape: true);
        }

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        private void OnValidate()
        {
            if (targets == null)
                targets = Array.Empty<EnemyEventController>();

            EnsureTriggerCollider();
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerScript player = ResolveTriggerPlayer(other);
            if (player == null || !player.CompareTag("Player"))
                return;

            ActivateTargets();
        }

        public static PlayerScript ResolveTriggerPlayer(Collider other)
        {
            return other != null ? other.GetComponent<PlayerScript>() : null;
        }

        public bool ActivateTargets()
        {
            bool accepted = false;
            foreach (EnemyEventController target in
                     targets ?? Array.Empty<EnemyEventController>())
            {
                if (target != null && target.ActivateFromSpot())
                    accepted = true;
            }

            if (!accepted)
                return false;

            ConsumedSpots.Add(this);
            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger != null)
                trigger.enabled = false;
            return true;
        }

        public static void ResetAllForNewRun()
        {
            foreach (EnemyEventActivationSpot spot in ConsumedSpots)
            {
                if (spot != null)
                    spot.ResetForNewRun();
            }

            ConsumedSpots.Clear();
        }

        private void ResetForNewRun()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger != null)
                trigger.enabled = true;
        }

        private void EnsureTriggerCollider()
        {
            ConfigureCollider(GetComponent<BoxCollider>(), applyDefaultShape: false);
        }

        public static void ConfigureCollider(
            BoxCollider trigger,
            bool applyDefaultShape)
        {
            if (trigger == null)
                return;

            trigger.isTrigger = true;
            if (!applyDefaultShape)
                return;

            trigger.center = DefaultColliderCenter;
            trigger.size = DefaultColliderSize;
        }

        private void OnDrawGizmos()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger == null)
                return;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.2f, 0.05f, 0.8f);
            Gizmos.DrawWireCube(trigger.center, trigger.size);
            Gizmos.color = new Color(1f, 0.2f, 0.05f, 0.12f);
            Gizmos.DrawCube(trigger.center, trigger.size);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
