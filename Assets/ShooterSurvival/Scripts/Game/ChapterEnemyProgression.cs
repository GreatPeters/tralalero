using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.ShooterSurvival
{
    public readonly struct ChapterRouteTurn
    {
        public ChapterRouteTurn(
            Vector3 position,
            Vector3 outgoingDirection,
            string stableKey = null)
        {
            Position = position;
            OutgoingDirection = outgoingDirection;
            StableKey = stableKey ?? string.Empty;
        }

        public Vector3 Position { get; }
        public Vector3 OutgoingDirection { get; }
        public string StableKey { get; }
    }

    public static class ChapterEnemyProgression
    {
        private const float DirectionEpsilonSqr = 0.000001f;
        private const float MinimumForwardDistance = 0.01f;
        private const float MaximumTurnLateralDistance = 6f;

        private readonly struct RouteSegment
        {
            public RouteSegment(
                Vector3 start,
                Vector3 direction,
                Vector3 travelDirection,
                float length,
                float startDistance)
            {
                Start = start;
                Direction = direction;
                TravelDirection = travelDirection;
                Length = length;
                StartDistance = startDistance;
            }

            public Vector3 Start { get; }
            public Vector3 Direction { get; }
            public Vector3 TravelDirection { get; }
            public float Length { get; }
            public float StartDistance { get; }
        }

        public static float CalculateProgress(int enemyIndex, int enemyCount)
            => MonsterStatInterpolator.CalculateProgress(enemyIndex, enemyCount);

        public static List<EnemyScript_space> CollectEncounterEnemies(Scene scene)
        {
            var enemies = new List<EnemyScript_space>();
            if (!scene.IsValid() || !scene.isLoaded)
                return enemies;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (EnemyScript_space enemy in
                         root.GetComponentsInChildren<EnemyScript_space>(true))
                {
                    if (enemy != null && !IsPooledEnemy(enemy.gameObject))
                        enemies.Add(enemy);
                }
            }

            return enemies;
        }

        public static List<ChapterRouteTurn> CollectRouteTurns(Scene scene)
        {
            var routeTurns = new List<ChapterRouteTurn>();
            if (!scene.IsValid() || !scene.isLoaded)
                return routeTurns;

            foreach (NoryangjinTurnSpot turnSpot in
                     UnityEngine.Object.FindObjectsByType<NoryangjinTurnSpot>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (turnSpot == null || turnSpot.gameObject.scene != scene)
                    continue;

                HideFlags combinedHideFlags =
                    turnSpot.hideFlags | turnSpot.gameObject.hideFlags;
                if ((combinedHideFlags & HideFlags.DontSave) != 0)
                    continue;

                routeTurns.Add(new ChapterRouteTurn(
                    turnSpot.transform.position,
                    turnSpot.TargetWorldDirection,
                    turnSpot.gameObject.name));
            }

            return routeTurns;
        }

        public static int ApplyStats(
            IReadOnlyList<EnemyScript_space> enemies,
            IReadOnlyDictionary<EnemyTier, MonsterGrowthRow> chapterRows,
            Vector3 routeStart,
            Vector3 initialDirection,
            IReadOnlyList<ChapterRouteTurn> turns)
        {
            if (enemies == null || enemies.Count == 0)
                return 0;
            if (chapterRows == null)
                throw new ArgumentNullException(nameof(chapterRows));

            var orderedEnemies = new List<EnemyScript_space>(enemies.Count);
            var routeDistances = new Dictionary<EnemyScript_space, float>();
            foreach (EnemyScript_space enemy in enemies)
            {
                if (enemy == null)
                    continue;

                orderedEnemies.Add(enemy);
                routeDistances.Add(
                    enemy,
                    CalculateRouteDistance(
                        enemy.transform.position,
                        routeStart,
                        initialDirection,
                        turns));
            }

            orderedEnemies.Sort((left, right) => CompareEncounterEnemies(
                left,
                right,
                routeDistances));

            for (int index = 0; index < orderedEnemies.Count; index++)
            {
                EnemyScript_space enemy = orderedEnemies[index];
                EnemyTier fixedTier = ForwardEnemyTierResolver.ResolveOrFallback(
                    enemy.gameObject.name,
                    EnemyTier.Normal);
                if (!chapterRows.TryGetValue(fixedTier, out MonsterGrowthRow growth))
                {
                    throw new KeyNotFoundException(
                        $"Monster growth has no '{fixedTier}' row for this chapter.");
                }

                float progress = CalculateProgress(index, orderedEnemies.Count);
                MonsterStatInterpolator.Evaluate(
                    growth,
                    progress,
                    out float damage,
                    out float health);
                enemy.ApplyStat(
                    damage,
                    health,
                    fixedTier);
            }

            return orderedEnemies.Count;
        }

        public static bool IsPooledEnemy(GameObject enemyObject)
            => enemyObject != null &&
               enemyObject.GetComponentInParent<EnemyPooler>(includeInactive: true) != null;

        public static float CalculateRouteDistance(
            Vector3 position,
            Vector3 routeStart,
            Vector3 initialDirection,
            IReadOnlyList<ChapterRouteTurn> turns)
        {
            List<RouteSegment> segments = BuildRouteSegments(
                routeStart,
                initialDirection,
                turns,
                out Vector3 finalStart,
                out Vector3 finalDirection,
                out float finalStartDistance);

            Vector3 horizontalPosition = Horizontal(position);
            float bestSqrDistance = float.PositiveInfinity;
            float bestRouteDistance = 0f;

            foreach (RouteSegment segment in segments)
            {
                float along = Mathf.Clamp(
                    Vector3.Dot(horizontalPosition - segment.Start, segment.Direction),
                    0f,
                    segment.Length);
                Vector3 nearest = segment.Start + segment.Direction * along;
                ConsiderCandidate(
                    horizontalPosition,
                    nearest,
                    segment.StartDistance + along,
                    ref bestSqrDistance,
                    ref bestRouteDistance);
            }

            float finalAlong = Mathf.Max(
                0f,
                Vector3.Dot(horizontalPosition - finalStart, finalDirection));
            ConsiderCandidate(
                horizontalPosition,
                finalStart + finalDirection * finalAlong,
                finalStartDistance + finalAlong,
                ref bestSqrDistance,
                ref bestRouteDistance);

            return bestRouteDistance;
        }

        public static Vector3 CalculateRouteDirection(
            Vector3 position,
            Vector3 routeStart,
            Vector3 initialDirection,
            IReadOnlyList<ChapterRouteTurn> turns)
        {
            List<RouteSegment> segments = BuildRouteSegments(
                routeStart,
                initialDirection,
                turns,
                out Vector3 finalStart,
                out Vector3 finalDirection,
                out float finalStartDistance);

            Vector3 horizontalPosition = Horizontal(position);
            float bestSqrDistance = float.PositiveInfinity;
            float bestRouteDistance = 0f;
            Vector3 bestRouteDirection = finalDirection;

            foreach (RouteSegment segment in segments)
            {
                float along = Mathf.Clamp(
                    Vector3.Dot(horizontalPosition - segment.Start, segment.Direction),
                    0f,
                    segment.Length);
                Vector3 nearest = segment.Start + segment.Direction * along;
                ConsiderDirectionCandidate(
                    horizontalPosition,
                    nearest,
                    segment.StartDistance + along,
                    segment.TravelDirection,
                    ref bestSqrDistance,
                    ref bestRouteDistance,
                    ref bestRouteDirection);
            }

            float finalAlong = Mathf.Max(
                0f,
                Vector3.Dot(horizontalPosition - finalStart, finalDirection));
            ConsiderDirectionCandidate(
                horizontalPosition,
                finalStart + finalDirection * finalAlong,
                finalStartDistance + finalAlong,
                finalDirection,
                ref bestSqrDistance,
                ref bestRouteDistance,
                ref bestRouteDirection);

            return bestRouteDirection;
        }

        private static List<RouteSegment> BuildRouteSegments(
            Vector3 routeStart,
            Vector3 initialDirection,
            IReadOnlyList<ChapterRouteTurn> turns,
            out Vector3 finalStart,
            out Vector3 finalDirection,
            out float finalStartDistance)
        {
            var segments = new List<RouteSegment>();
            var remainingTurns = turns != null
                ? new List<ChapterRouteTurn>(turns)
                : new List<ChapterRouteTurn>();
            Vector3 currentStart = Horizontal(routeStart);
            Vector3 currentDirection = HorizontalDirection(initialDirection, Vector3.forward);
            float distanceFromStart = 0f;

            while (TryTakeNextTurn(
                       currentStart,
                       currentDirection,
                       remainingTurns,
                       out ChapterRouteTurn nextTurn))
            {
                Vector3 turnPosition = Horizontal(nextTurn.Position);
                Vector3 segmentDelta = turnPosition - currentStart;
                float segmentLength = segmentDelta.magnitude;
                if (segmentLength > MinimumForwardDistance)
                {
                    segments.Add(new RouteSegment(
                        currentStart,
                        segmentDelta / segmentLength,
                        currentDirection,
                        segmentLength,
                        distanceFromStart));
                    distanceFromStart += segmentLength;
                }

                currentStart = turnPosition;
                currentDirection = HorizontalDirection(
                    nextTurn.OutgoingDirection,
                    currentDirection);
            }

            finalStart = currentStart;
            finalDirection = currentDirection;
            finalStartDistance = distanceFromStart;
            return segments;
        }

        private static bool TryTakeNextTurn(
            Vector3 currentStart,
            Vector3 currentDirection,
            List<ChapterRouteTurn> remainingTurns,
            out ChapterRouteTurn nextTurn)
        {
            int bestIndex = -1;
            float bestForwardDistance = float.PositiveInfinity;
            float bestLateralDistance = float.PositiveInfinity;
            string bestStableKey = string.Empty;

            for (int i = 0; i < remainingTurns.Count; i++)
            {
                ChapterRouteTurn candidate = remainingTurns[i];
                Vector3 delta = Horizontal(candidate.Position) - currentStart;
                float forwardDistance = Vector3.Dot(delta, currentDirection);
                if (forwardDistance <= MinimumForwardDistance)
                    continue;

                Vector3 lateral = delta - currentDirection * forwardDistance;
                float lateralDistance = lateral.magnitude;
                if (lateralDistance > MaximumTurnLateralDistance)
                    continue;

                int forwardComparison = forwardDistance.CompareTo(bestForwardDistance);
                bool isBetter = forwardComparison < 0 ||
                                (forwardComparison == 0 &&
                                 lateralDistance < bestLateralDistance) ||
                                (forwardComparison == 0 &&
                                 Mathf.Approximately(lateralDistance, bestLateralDistance) &&
                                 string.CompareOrdinal(candidate.StableKey, bestStableKey) < 0);
                if (!isBetter)
                    continue;

                bestIndex = i;
                bestForwardDistance = forwardDistance;
                bestLateralDistance = lateralDistance;
                bestStableKey = candidate.StableKey;
            }

            if (bestIndex < 0)
            {
                nextTurn = default;
                return false;
            }

            nextTurn = remainingTurns[bestIndex];
            remainingTurns.RemoveAt(bestIndex);
            return true;
        }

        private static void ConsiderCandidate(
            Vector3 position,
            Vector3 nearest,
            float routeDistance,
            ref float bestSqrDistance,
            ref float bestRouteDistance)
        {
            float sqrDistance = (position - nearest).sqrMagnitude;
            if (sqrDistance < bestSqrDistance ||
                (Mathf.Approximately(sqrDistance, bestSqrDistance) &&
                 routeDistance < bestRouteDistance))
            {
                bestSqrDistance = sqrDistance;
                bestRouteDistance = routeDistance;
            }
        }

        private static void ConsiderDirectionCandidate(
            Vector3 position,
            Vector3 nearest,
            float routeDistance,
            Vector3 routeDirection,
            ref float bestSqrDistance,
            ref float bestRouteDistance,
            ref Vector3 bestRouteDirection)
        {
            float sqrDistance = (position - nearest).sqrMagnitude;
            if (sqrDistance < bestSqrDistance ||
                (Mathf.Approximately(sqrDistance, bestSqrDistance) &&
                 routeDistance < bestRouteDistance))
            {
                bestSqrDistance = sqrDistance;
                bestRouteDistance = routeDistance;
                bestRouteDirection = routeDirection;
            }
        }

        private static int CompareEncounterEnemies(
            EnemyScript_space left,
            EnemyScript_space right,
            IReadOnlyDictionary<EnemyScript_space, float> routeDistances)
        {
            int comparison = routeDistances[left].CompareTo(routeDistances[right]);
            if (comparison != 0)
                return comparison;

            comparison = left.transform.position.x.CompareTo(right.transform.position.x);
            if (comparison != 0)
                return comparison;

            comparison = left.transform.position.z.CompareTo(right.transform.position.z);
            if (comparison != 0)
                return comparison;

            return string.CompareOrdinal(left.gameObject.name, right.gameObject.name);
        }

        private static Vector3 Horizontal(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private static Vector3 HorizontalDirection(Vector3 direction, Vector3 fallback)
        {
            Vector3 horizontal = Horizontal(direction);
            if (horizontal.sqrMagnitude > DirectionEpsilonSqr)
                return horizontal.normalized;

            horizontal = Horizontal(fallback);
            return horizontal.sqrMagnitude > DirectionEpsilonSqr
                ? horizontal.normalized
                : Vector3.forward;
        }
    }
}
