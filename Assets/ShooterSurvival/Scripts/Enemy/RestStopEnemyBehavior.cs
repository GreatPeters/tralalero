using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // Rest-stop enemy roles layered on top of EnemyEventController + EnemyScript_space.
    // The base controller still owns activation, movement, attacks and death; this component only
    // adds each role's distinguishing rule so the same encounter authoring keeps working.
    [RequireComponent(typeof(EnemyEventController))]
    public sealed class RestStopEnemyBehavior : MonoBehaviour
    {
        public enum Role
        {
            [InspectorName("경광봉 투척(착지 예고)")] Lobber = 0,
            [InspectorName("라바콘 설치")] ConeLayer = 1,
            [InspectorName("군집 돌격")] Swarm = 2,
            [InspectorName("깃발 대장")] SwarmLeader = 3,
            [InspectorName("예고 후 돌진")] Charger = 4,
            [InspectorName("정면 방패")] Shield = 5,
        }

        public Role role;
        [Tooltip("라바콘 설치: 비활성 상태로 둔 바리케이드 기믹 원본. 도착 시 좌우에 복제합니다.")]
        public GameObject coneTemplate;
        public float coneSpacing = 2.2f;
        [Tooltip("돌진: 멈춰서 발을 구르는 예고 시간과 돌진 속도 배율")]
        public float chargeTelegraphSeconds = .8f, chargeSpeedMultiplier = 2.6f;
        [Tooltip("대장: 살아 있는 동안 주변 군집 속도 배율, 적용 반경")]
        public float leaderSpeedMultiplier = 1.3f, leaderRadius = 14f;
        [Tooltip("방패: 정면 판정 각도(도)와 정면 피해 배율")]
        public float shieldHalfAngle = 60f, shieldDamageMultiplier = .3f;
        public Color markerColor = new(1f, .25f, .12f, .85f);

        private EnemyEventController controller;
        private EnemyScript_space combat;
        private Animator animator;
        private EnemyEventRuntimeState lastState;
        private float baseSpeed, telegraphUntil = -1, markerUntil = -1;
        private bool conesPlaced, panicked;
        private readonly List<GameObject> cones = new();
        private LineRenderer marker;
        private int lastAttackCycle = -1;
        private static readonly List<RestStopEnemyBehavior> Swarms = new();
        private static readonly int AttackOnce = Animator.StringToHash(ForwardEnemyAnimationContract.AttackOnce);

        public static float ShieldMultiplier(Vector3 facing, Vector3 toSource, float halfAngle, float multiplier)
        {
            facing.y = 0; toSource.y = 0;
            if (facing.sqrMagnitude < 1e-6f || toSource.sqrMagnitude < 1e-6f) return 1f;
            return Vector3.Angle(facing, toSource) <= halfAngle ? multiplier : 1f;
        }

        public static float ChargeSpeed(float baseSpeed, float elapsedSinceStart, float telegraph, float multiplier)
            => elapsedSinceStart < telegraph ? 0f : baseSpeed * multiplier;

        private void Awake()
        {
            controller = GetComponent<EnemyEventController>();
            combat = GetComponent<EnemyScript_space>();
            animator = GetComponentInChildren<Animator>(true);
            baseSpeed = controller.MoveSpeed;
        }

        private void OnEnable() { if (role == Role.Swarm) Swarms.Add(this); ResetRole(); }
        private void OnDisable() { Swarms.Remove(this); ClearCones(); if (marker != null) marker.enabled = false; }

        public void ResetRole()
        {
            if (controller == null) return;
            controller.MoveSpeed = baseSpeed;
            telegraphUntil = markerUntil = -1; conesPlaced = panicked = false; lastAttackCycle = -1;
            lastState = controller.RuntimeState;
            ClearCones();
        }

        public float ModifyIncomingDamage(float damage, Vector3 source)
        {
            if (role != Role.Shield) return damage;
            var facing = animator != null ? animator.transform.forward : transform.forward;
            return damage * ShieldMultiplier(facing, source - transform.position, shieldHalfAngle, shieldDamageMultiplier);
        }

        private void Update()
        {
            if (!TimeManager.isGameRunning || controller == null) return;
            var state = controller.RuntimeState;
            bool started = lastState == EnemyEventRuntimeState.Waiting && state != EnemyEventRuntimeState.Waiting;
            if (lastState == EnemyEventRuntimeState.Dead && state == EnemyEventRuntimeState.Waiting) ResetRole();
            switch (role)
            {
                case Role.Charger:
                    if (started && state == EnemyEventRuntimeState.MovingToTarget)
                    {
                        telegraphUntil = Time.time + chargeTelegraphSeconds;
                        GameAudioService.PlayAt(GameSound.Warning, transform.position);
                    }
                    if (telegraphUntil > 0)
                        controller.MoveSpeed = Time.time < telegraphUntil ? 0f : baseSpeed * chargeSpeedMultiplier;
                    break;
                case Role.ConeLayer:
                    // Place cones when the worker reaches the lane (after walking) or starts working in place.
                    if (!conesPlaced && state == EnemyEventRuntimeState.Attacking &&
                        (lastState == EnemyEventRuntimeState.MovingToTarget || lastState == EnemyEventRuntimeState.Waiting))
                        PlaceCones();
                    break;
                case Role.SwarmLeader:
                    bool alive = combat == null || !combat.IsDead;
                    foreach (var member in Swarms)
                    {
                        if (member == null || (member.transform.position - transform.position).sqrMagnitude > leaderRadius * leaderRadius) continue;
                        if (alive) member.controller.MoveSpeed = member.baseSpeed * leaderSpeedMultiplier;
                        else if (!panicked) member.Panic();
                    }
                    if (!alive) panicked = true;
                    break;
                case Role.Lobber:
                    UpdateLobMarker();
                    break;
            }
            lastState = state;
        }

        // Leader down: nearby swarm members stop, flail, and fall with their normal coin rewards.
        public void Panic()
        {
            if (panicked || combat == null || combat.IsDead) return;
            panicked = true;
            if (animator != null && animator.HasState(0, Animator.StringToHash("hit"))) animator.Play("hit", 0, 0);
            controller.MoveSpeed = 0;
            Invoke(nameof(FallFromPanic), .6f);
        }
        private void FallFromPanic() { if (combat != null && !combat.IsDead) combat.EnemyDeath(); }

        private void PlaceCones()
        {
            conesPlaced = true;
            if (coneTemplate == null) return;
            var right = Vector3.Cross(Vector3.up, (FindPlayerPosition() - transform.position).normalized);
            if (right.sqrMagnitude < .01f) right = transform.right;
            foreach (int side in new[] { -1, 1 })
            {
                var cone = Instantiate(coneTemplate, transform.position + right.normalized * coneSpacing * side, coneTemplate.transform.rotation, transform.parent);
                // Strip authored roadblock rules (instant death) from the copy; dropped cones only cost health.
                foreach (var hazard in cone.GetComponentsInChildren<HighwayHazard>(true)) DestroyImmediate(hazard);
                foreach (var stats in cone.GetComponentsInChildren<ObstacleStats>(true)) DestroyImmediate(stats);
                foreach (var c in cone.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
                var trigger = cone.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(2.2f, 2f, 1.2f); trigger.center = Vector3.up;
                cone.AddComponent<RestStopConeContact>();
                cone.SetActive(true);
                cones.Add(cone);
            }
            GameAudioService.PlayAt(GameSound.Warning, transform.position);
        }
        private void ClearCones() { foreach (var c in cones) if (c != null) Destroy(c); cones.Clear(); }

        private void UpdateLobMarker()
        {
            if (animator == null) return;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == AttackOnce)
            {
                int cycle = Mathf.FloorToInt(info.normalizedTime);
                if (cycle != lastAttackCycle) { lastAttackCycle = cycle; ShowMarker(FindPlayerPosition()); }
            }
            if (marker != null)
            {
                marker.enabled = Time.time < markerUntil;
                if (marker.enabled)
                {
                    float pulse = 1f + .15f * Mathf.Sin(Time.time * 18f);
                    marker.transform.localScale = Vector3.one * pulse;
                }
            }
        }

        private void ShowMarker(Vector3 at)
        {
            if (marker == null)
            {
                var go = new GameObject("LobLandingMarker");
                marker = go.AddComponent<LineRenderer>();
                marker.useWorldSpace = false; marker.loop = true; marker.positionCount = 32;
                marker.widthMultiplier = .18f; marker.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                marker.material.color = markerColor; marker.startColor = marker.endColor = markerColor;
                for (int i = 0; i < 32; i++)
                {
                    float a = i / 32f * Mathf.PI * 2;
                    marker.SetPosition(i, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.4f);
                }
            }
            marker.transform.position = at + Vector3.up * .08f;
            markerUntil = Time.time + 1.1f;
        }

        private Vector3 FindPlayerPosition()
        {
            var player = FindFirstObjectByType<PlayerScript>();
            return player != null ? player.transform.position : transform.position + transform.forward * 6f;
        }
    }
}
