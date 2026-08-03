using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [AddComponentMenu("Shooter Survival/Enemy Movement Activation Trigger")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EnemyMovementActivationTrigger : MonoBehaviour
    {
        private static readonly HashSet<EnemyMovementActivationTrigger> ConsumedTriggers = new();

        [Tooltip("플레이어가 이 영역에 들어왔을 때 발동할 적들입니다.")]
        [HideInInspector]
        [SerializeField] private EnemyMovementController[] targets =
            Array.Empty<EnemyMovementController>();

        [Tooltip("한 번 발동한 뒤 다음 런이 시작될 때까지 트리거를 끕니다.")]
        [SerializeField] private bool oneShot = true;

        public EnemyMovementController[] Targets
        {
            get => targets;
            set => targets = value ?? Array.Empty<EnemyMovementController>();
        }

        public bool OneShot
        {
            get => oneShot;
            set => oneShot = value;
        }

        private void Reset()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1f, 0f);
            trigger.size = new Vector3(4f, 2f, 0.8f);
        }

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        private void OnValidate()
        {
            if (targets == null)
                targets = Array.Empty<EnemyMovementController>();

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
            // Only the player's root movement collider may activate a trigger.
            // Weapon and visual child colliders otherwise produce duplicate calls.
            return other != null ? other.GetComponent<PlayerScript>() : null;
        }

        public bool ActivateTargets()
        {
            bool accepted = false;
            foreach (EnemyMovementController target in
                     targets ?? Array.Empty<EnemyMovementController>())
            {
                if (target != null && target.ActivateFromTrigger())
                    accepted = true;
            }

            if (accepted && oneShot)
            {
                ConsumedTriggers.Add(this);
                BoxCollider trigger = GetComponent<BoxCollider>();
                if (trigger != null)
                    trigger.enabled = false;
            }

            return accepted;
        }

        public static void ResetAllForNewRun()
        {
            foreach (EnemyMovementActivationTrigger trigger in ConsumedTriggers)
            {
                if (trigger != null)
                    trigger.ResetForNewRun();
            }

            ConsumedTriggers.Clear();
        }

        private void ResetForNewRun()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger != null)
                trigger.enabled = true;
        }

        private void EnsureTriggerCollider()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger != null)
                trigger.isTrigger = true;
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
