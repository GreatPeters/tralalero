using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [DefaultExecutionOrder(150)]
    public sealed class EnemyGunAim : MonoBehaviour
    {
        public Transform gun;
        public Transform muzzle;
        public Vector3 localBarrelAxis = Vector3.right;
        private PlayerScript player;
        private EnemyEventController owner;

        private void Awake() { owner = GetComponent<EnemyEventController>(); player = FindFirstObjectByType<PlayerScript>(); }
        private void LateUpdate()
        {
            AimAtPlayer();
        }
        public void AimAtPlayer()
        {
            if (gun == null || player == null || owner == null || owner.RuntimeState == EnemyEventRuntimeState.Dead) return;
            var collider = player.GetComponent<Collider>();
            Vector3 target = collider != null ? collider.bounds.center : player.transform.position + Vector3.up;
            for (int iteration = 0; iteration < 3; iteration++)
            {
                Vector3 direction = target - (muzzle != null ? muzzle.position : gun.position);
                if (direction.sqrMagnitude < .001f) return;
                gun.rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.FromToRotation(localBarrelAxis, Vector3.forward);
            }
        }
    }
}
