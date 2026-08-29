using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [DisallowMultipleComponent]
    public sealed class AuthoredBonusWall : MonoBehaviour
    {
        [SerializeField] private Rarity rarity = Rarity.Normal;
        [SerializeField, Min(0.1f)] private float nearbyDistance =
            BonusAltarRules.DefaultNearbyDistance;

        private string rolledStat;

        public Rarity Rarity => rarity;
        public float NearbyDistance => nearbyDistance;
        public string RolledStat => rolledStat;
        public WallScript Wall => GetComponentInChildren<WallScript>(true);

        public void Configure(Rarity grade)
        {
            rarity = grade;
            SyncWallAuthoringState();
        }

        public HashSet<string> CollectNearbyRolledStats()
        {
            var usedStats = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (AuthoredBonusWall altar in FindObjectsByType<AuthoredBonusWall>(
                         FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                if (altar == this ||
                    altar.gameObject.scene != gameObject.scene ||
                    string.IsNullOrEmpty(altar.rolledStat))
                {
                    continue;
                }

                float threshold = Mathf.Max(nearbyDistance, altar.nearbyDistance);
                if (BonusAltarRules.AreNearby(
                        transform.position,
                        altar.transform.position,
                        threshold))
                {
                    usedStats.Add(altar.rolledStat);
                }
            }

            return usedStats;
        }

        public void BeginRoll()
        {
            rolledStat = null;
        }

        public void CommitRoll(string stat)
        {
            rolledStat = stat;
        }

        private void OnEnable()
        {
            SyncWallAuthoringState();
        }

        private void OnValidate()
        {
            nearbyDistance = Mathf.Max(0.1f, nearbyDistance);
            SyncWallAuthoringState();
        }

        private void SyncWallAuthoringState()
        {
            WallScript wall = Wall;
            if (wall != null)
            {
                wall.wallType = WallType.BuffWall;
                wall.isRandom = true;
                wall.rarity = rarity;
            }

            GetComponent<BonusChoiceAltarVfx>()?.SetRarity(rarity);
        }
    }
}
