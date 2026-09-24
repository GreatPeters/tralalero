using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // A projectile advances along its own road progress. The owner can keep
    // moving or changing lanes without dragging an already-fired shot around.
    public struct HighwayProjectilePath
    {
        private HighwayRoute route;
        private bool bypass;
        private float distance, lane, height, forwardOffset;
        private float forwardFraction, lateralFraction, verticalFraction;
        private Vector3 previousPosition, previousDirection;

        public bool IsActive => route != null && route.isActiveAndEnabled && route.centers.Length > 1;

        public HighwayProjectilePath(HighwayRoute road, float startDistance, bool onBypass, Vector3 position, Vector3 direction)
        {
            route = road; bypass = onBypass; distance = startDistance;
            road.Sample(startDistance, onBypass, out var center, out var forward);
            var right = Vector3.Cross(Vector3.up, forward);
            var offset = position - center;
            lane = Vector3.Dot(offset, right); height = offset.y;
            forwardOffset = Vector3.Dot(offset, forward);
            forwardFraction = Vector3.Dot(direction, forward);
            lateralFraction = Vector3.Dot(direction, right);
            verticalFraction = direction.y;
            previousPosition = position; previousDirection = direction;
        }

        public Vector3 Advance(float travel, out Quaternion rotation)
        {
            distance += travel * forwardFraction;
            lane += travel * lateralFraction;
            height += travel * verticalFraction;
            float sample = Mathf.Clamp(distance, 0, route.length);
            route.Sample(sample, bypass, out var center, out var forward);
            center += forward * (distance - sample);
            var position = center + Vector3.Cross(Vector3.up, forward) * lane +
                Vector3.up * height + forward * forwardOffset;
            var movement = position - previousPosition;
            rotation = movement.sqrMagnitude > .000001f && previousDirection.sqrMagnitude > .000001f
                ? Quaternion.FromToRotation(previousDirection, movement) : Quaternion.identity;
            if (movement.sqrMagnitude > .000001f) previousDirection = movement;
            previousPosition = position;
            return position;
        }
    }
}
