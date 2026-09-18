using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // Each helper owns its route cursor; player turn triggers are never consumed by a helper.
    [DisallowMultipleComponent]
    public sealed class NoryangjinHelperRouteFollower : MonoBehaviour
    {
        private List<ChapterRouteTurn> turns = new();
        private int nextTurn;
        private Vector3 forward;
        private NoryangjinRoadHeightFollower ground;
        private PlayerScript player;
        private HighwayRoute highway;
        private float highwayDistance;
        public int CompletedTurns => nextTurn;

        public void Configure(PlayerScript owner)
        {
            player = owner;
            highway = FindFirstObjectByType<HighwayRoute>();
            if (highway != null) highwayDistance = highway.NearestDistance(transform.position);
            var remaining = new List<ChapterRouteTurn>();
            foreach (var root in owner.gameObject.scene.GetRootGameObjects())
                foreach (var spot in root.GetComponentsInChildren<NoryangjinTurnSpot>(true))
                    if (!spot.IsSlopeTransition && !spot.IsConsumedThisRun && spot.isActiveAndEnabled)
                        remaining.Add(new ChapterRouteTurn(spot.transform.position, spot.TargetWorldDirection, spot.name));
            ConfigureRoute(owner.transform.forward, remaining, owner.GetComponent<NoryangjinRoadHeightFollower>());
        }

        public void ConfigureRoute(Vector3 direction, IReadOnlyList<ChapterRouteTurn> checkpoints, NoryangjinRoadHeightFollower heightSupport)
        {
            forward = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            turns = ChapterEnemyProgression.OrderRemainingTurns(transform.position, forward, checkpoints);
            nextTurn = 0;
            ground = heightSupport;
        }

        public void Advance(float distance)
        {
            if (player != null && player.IsStationaryCombat) return;
            if (highway != null)
            {
                // Stay near the player until its fork decision is known.
                highwayDistance = Mathf.Min(highway.length, highwayDistance + distance, highway.Distance + 8);
                bool bypass = false;
                foreach (var fork in highway.forks)
                    if (fork.decided && fork.bypass && highwayDistance >= fork.start && highwayDistance <= fork.end) bypass = true;
                highway.Sample(highwayDistance, bypass, out var p, out var f);
                p += Vector3.up * .12f;
                if (ground != null && ground.TryProjectPosition(p, f, out var supported)) p = supported;
                transform.SetPositionAndRotation(p, Quaternion.LookRotation(f));
                return;
            }
            // Small steps keep floor sampling on the correct level of the two crossing bridges.
            while (distance > .0001f)
            {
                Vector3 destination = nextTurn < turns.Count ? turns[nextTurn].Position : transform.position + forward * distance;
                destination.y = transform.position.y;
                float remaining = Vector3.Distance(transform.position, destination);
                float step = Mathf.Min(distance, .25f, remaining);
                Vector3 proposed = Vector3.MoveTowards(transform.position, destination, step);
                if (ground != null)
                {
                    if (!ground.TryProjectPosition(proposed, forward, out var supported))
                    {
                        // Timber plank gaps are not the end of the road. Probe only a short
                        // distance along this segment, never sideways onto a shop or crossing.
                        bool gap = ground.TryProjectPosition(proposed + forward * .2f, forward, out supported) ||
                                   ground.TryProjectPosition(proposed + forward * .4f, forward, out supported);
                        if (!gap || Mathf.Abs(supported.y - proposed.y) > .3f) return;
                        supported.x = proposed.x; supported.z = proposed.z;
                    }
                    proposed = supported;
                }
                Vector3 travel = proposed - transform.position;
                transform.position = proposed;
                if (travel.sqrMagnitude > .000001f)
                    transform.rotation = Quaternion.LookRotation(travel.normalized, Vector3.up);
                distance -= step;
                if (remaining <= step + .001f && nextTurn < turns.Count)
                {
                    forward = Vector3.ProjectOnPlane(turns[nextTurn++].OutgoingDirection, Vector3.up).normalized;
                    transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }
                else if (step <= .0001f) return;
            }
        }
    }
}
