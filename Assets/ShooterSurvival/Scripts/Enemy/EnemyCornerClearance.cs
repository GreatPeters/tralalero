using System.IO;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [DisallowMultipleComponent]
    public sealed class EnemyCornerClearance : MonoBehaviour
    {
        [SerializeField] private Vector3 corner;
        [SerializeField] private Vector3 outgoingDirection = Vector3.forward;
        [SerializeField, Min(0)] private float minimumBodyDistance = 28;
        [SerializeField, Min(0)] private float minimumTriggerDistance = 20;
        public Vector3 Corner => corner;
        public Vector3 Direction => outgoingDirection;
        public float MinimumBodyDistance => minimumBodyDistance;
        public float MinimumTriggerDistance => minimumTriggerDistance;

        public void Configure(Vector3 routeCorner, Vector3 direction)
        {
            corner = routeCorner;
            outgoingDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        }
        public float DistanceAfterCorner(Vector3 point) => Vector3.Dot(point - corner, outgoingDirection);
        public void ValidateMovement(Vector3 start, Vector3 target)
        {
            if (Mathf.Min(DistanceAfterCorner(start), DistanceAfterCorner(target)) < minimumBodyDistance - .02f)
                throw new InvalidDataException(name + ": 이동 범위가 코너 뒤 대응 여유 28유닛을 침범합니다.");
        }
        public Vector3 ConstrainTrigger(Vector3 desired)
        {
            float deficit = minimumTriggerDistance - DistanceAfterCorner(desired);
            return deficit > 0 ? desired + outgoingDirection * deficit : desired;
        }
    }
}
