using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.ShooterSurvival
{
    // Applies workbook settings to existing authored objects; never replaces roads or models.
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class EncounterPlacementController : MonoBehaviour
    {
        private bool applied;
        private readonly HashSet<string> disabledEnemies = new(StringComparer.Ordinal);
        public int AppliedCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() => Ensure(SceneManager.GetActiveScene());
        private static void Loaded(Scene scene, LoadSceneMode mode) => Ensure(scene);
        private static void Ensure(Scene scene)
        {
            if (!scene.IsValid() || (!scene.name.StartsWith("Noryangjin_", StringComparison.Ordinal) && scene.name != "HighWay" && scene.name != "RestStop")) return;
            var map = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Noryangjin_MapTool");
            if (map != null && map.GetComponent<EncounterPlacementController>() == null) map.AddComponent<EncounterPlacementController>();
        }
        private void Start()
        {
            try { Apply(EncounterPlacementTables.Rows); }
            catch (Exception error) { Debug.LogError("[EncounterData] 원본 씬 배치를 유지합니다. " + error.Message, this); }
        }
        public void BeginNewRun() => BeginNewRun(EncounterPlacementTables.Rows);
        public void BeginNewRun(IReadOnlyList<EncounterPlacementRow> rows)
        {
            applied = false;
            Apply(rows);
        }
        public static bool IsExplicitlyDisabled(EnemyScript_space enemy)
        {
            var controller = enemy.GetComponentInParent<EncounterPlacementController>(true);
            return controller != null && controller.disabledEnemies.Contains(enemy.name);
        }
        public void Apply(IReadOnlyList<EncounterPlacementRow> allRows)
        {
            if (applied) return; // Settings are a run-start snapshot, never reset an active fight.
            EncounterPlacementTables.ValidateRows(allRows);
            var rows = allRows.Where(r => r.scene == gameObject.scene.name).ToArray();
            if (rows.Length == 0) { disabledEnemies.Clear(); combatRows.Clear(); AppliedCount = 0; applied = true; return; }
            var changes = new List<Action>();
            var map = transform;
            var roads = map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
            var curvedRoute = map.GetComponent<HighwayRoute>();
            float Floor(Vector3 p)
            {
                foreach (Vector3 offset in new[] { Vector3.zero, Vector3.right * .12f, Vector3.left * .12f, Vector3.forward * .12f, Vector3.back * .12f })
                    foreach (var road in roads)
                        if (road.Raycast(new Ray(p + offset + Vector3.up * 1.2f, Vector3.down), out var hit, 2.4f) && hit.normal.y > .7f)
                            return hit.point.y + .08f;
                throw new InvalidDataException("설정한 이동/발동 위치가 같은 높이의 길 밖입니다: " + p);
            }
            foreach (var row in rows)
            {
                Transform parent = map.Find(row.kind == "적 배치" ? "Enemies" : row.kind == "보너스 배치" ? "Bonuses" : "Props");
                var placement = parent?.Find(row.id);
                if (placement == null) throw new InvalidDataException("씬에서 배치ID를 찾을 수 없습니다: " + row.id);
                if (row.kind == "적 배치")
                {
                    var enemy = placement.GetComponent<EnemyEventController>();
                    var combat = placement.GetComponent<EnemyScript_space>();
                    if (enemy == null || combat == null) throw new InvalidDataException(row.id + ": 적 컴포넌트 없음");
                    if ((row.mode == EnemyEventMode.Shoot || row.mode == EnemyEventMode.AmbushMoveThenShoot) && !combat.HasConfiguredProjectile)
                        throw new InvalidDataException(row.id + ": 경비원/뚱보 등 투사체가 있는 모델만 사격할 수 있습니다.");
                    var matchingSpots = map.Find("Props").GetComponentsInChildren<EnemyEventActivationSpot>(true).Where(s => s.Targets.Contains(enemy)).ToArray();
                    if (matchingSpots.Length != 1 || matchingSpots[0].Targets.Length != 1)
                        throw new InvalidDataException(row.id + ": 일대일 발동 스팟 필요 (연결 스팟 " + matchingSpots.Length + "개, 대상 수 " + string.Join(",",matchingSpots.Select(s=>s.Targets.Length)) + ")");
                    var spot = matchingSpots[0];
                    var origin = placement.position;
                    Vector3 center = enemy.EventMode == EnemyEventMode.PatrolBetweenStartAndTarget && enemy.HasUsableTarget ?
                        (origin + enemy.TargetPoint.position) * .5f : enemy.EventMode == EnemyEventMode.AmbushMoveThenShoot && enemy.HasUsableTarget ? enemy.TargetPoint.position : origin;
                    Vector3 dir = Vector3.ProjectOnPlane(placement.forward, Vector3.up).normalized;
                    var start = center; var target = center;
                    if (row.mode == EnemyEventMode.PatrolBetweenStartAndTarget)
                    {
                        Vector3 axis = enemy.PatrolAcrossRoad ? Vector3.Cross(Vector3.up, dir) : dir;
                        start -= axis * row.moveDistance * .5f; target += axis * row.moveDistance * .5f;
                    }
                    else if (row.mode == EnemyEventMode.AmbushMoveThenShoot)
                        start += dir * row.moveDistance + Vector3.Cross(Vector3.up, dir) * enemy.AmbushEntrySide;
                    else if (row.mode == EnemyEventMode.MoveToTargetThenAttack) target -= dir * row.moveDistance;
                    Vector3 gate = center - dir * row.activationLead;
                    if (curvedRoute != null) gate = curvedRoute.ActivationPoint(center, row.activationLead);
                    gate.y = spot.transform.position.y; // A gate may be on a ramp even when its enemy is on the next flat.
                    var clearance = placement.GetComponent<EnemyCornerClearance>();
                    if (row.enabled && clearance != null)
                    {
                        clearance.ValidateMovement(start, target);
                        gate = clearance.ConstrainTrigger(gate);
                    }
                    if (row.enabled)
                    {
                        gate.y = Floor(gate);
                        if (EnemyEventController.RequiresTarget(row.mode))
                            for (int i = 0; i <= 8; i++)
                                if (Mathf.Abs(Floor(Vector3.Lerp(start, target, i / 8f)) - center.y) > .3f)
                                    throw new InvalidDataException(row.id + ": 이동 경로에 경사가 있습니다.");
                    }
                    changes.Add(() =>
                    {
                        placement.position = start;
                        enemy.RefreshPlacementAfterAuthoringChange(origin, false);
                        if (EnemyEventController.RequiresTarget(row.mode))
                        {
                            if (!enemy.HasUsableTarget)
                            {
                                var marker = new GameObject(row.id + "_WorkbookTarget");
                                marker.transform.SetParent(map, false); enemy.TargetPoint = marker.transform;
                            }
                            enemy.TargetPoint.position = target;
                        }
                        enemy.MoveSpeed = row.moveSpeed;
                        enemy.EventMode = row.mode;
                        combat.ConfigureTriggeredFire(row.throwDelay, row.throwSpeed);
                        combat.ConfigureRewards(row.dropBonusAltar,row.coinReward);
                        if (row.hasCombatStats) combat.ApplyStat(row.damage, row.health, row.tier);
                        spot.transform.position = gate;
                        placement.gameObject.SetActive(row.enabled); spot.gameObject.SetActive(row.enabled);
                    });
                }
                else if (row.kind == "보너스 배치")
                {
                    var altar = placement.GetComponent<AuthoredBonusWall>();
                    if (altar == null || altar.Wall == null) throw new InvalidDataException(row.id + ": 제단 컴포넌트 없음");
                    var pair = altar.ChoicePair;
                    if (pair != null)
                    {
                        if (!pair.IsConfigured || pair.Left != altar || pair.Right.Wall == null)
                            throw new InvalidDataException(row.id + ": 왼쪽 배치ID와 완전한 보너스 쌍이 필요합니다.");
                        changes.Add(() => pair.PrepareForRun(row.rarity, row.enabled));
                        continue;
                    }
                    changes.Add(() =>
                    {
                        altar.Configure(row.rarity);
                        placement.gameObject.SetActive(row.enabled);
                        if (row.enabled)
                        {
                            var wall = altar.Wall;
                            wall.ReactivateLifetimeObject();
                            wall.SetRandomStat();
                            wall.SetStats();
                            wall.SetWallSprite();
                        }
                    });
                }
                else
                {
                    var parts = placement.GetComponentsInChildren<ObstacleStats>(true).Where(s => s.transform == placement || s.gameObject.activeSelf).ToArray();
                    if (parts.Length == 0 || parts.Any(p => p.obstaclePattern != row.pattern) ||
                        (parts.Length != 1 && row.pattern != ObstaclePattern.Bucket && !(row.pattern==ObstaclePattern.HighwayToll&&parts.Length==3)))
                        throw new InvalidDataException(row.id + ": 기믹 모델/종류 불일치");
                    changes.Add(() =>
                    {
                        foreach (var obstacle in parts)
                        {
                            if (row.pattern == ObstaclePattern.Bucket) obstacle.bucketAttachSeconds = row.effectValue;
                            else obstacle.value = row.effectValue;
                            if(row.pattern==ObstaclePattern.Dolphin)obstacle.jumpHeight=row.effectValue;
                            if(row.pattern==ObstaclePattern.Oldman)
                                foreach(var paddle in obstacle.GetComponentsInChildren<SimpleProjectile>(true))paddle.damage=row.effectValue;
                            var highway = obstacle.GetComponent<HighwayHazard>();
                            if (highway != null && row.hasHighwaySettings)
                            {
                                highway.breakHealth = row.durability;
                                highway.warningSeconds = row.warningSeconds;
                                highway.cycleSeconds = row.operationSeconds;
                                highway.crossingDistance = row.crossingDistance;
                            }
                        }
                        placement.gameObject.SetActive(row.enabled);
                    });
                }
            }
            foreach (var change in changes) change();
            disabledEnemies.Clear();
            combatRows.Clear();
            foreach (var row in rows.Where(r => r.kind == "적 배치" && r.hasCombatStats)) combatRows.Add(row.id, row);
            foreach (var row in rows.Where(r => r.kind == "적 배치" && !r.enabled)) disabledEnemies.Add(row.id);
            AppliedCount = rows.Length; applied = true;
            Debug.Log($"[EncounterData] {gameObject.scene.name}: {AppliedCount}개 설정 적용", this);
        }

        private readonly Dictionary<string, EncounterPlacementRow> combatRows = new(StringComparer.Ordinal);
        public static bool HasPlacementStats(EnemyScript_space enemy) =>
            enemy != null && enemy.GetComponentInParent<EncounterPlacementController>(true) is { } owner && owner.combatRows.ContainsKey(enemy.name);
    }
}
