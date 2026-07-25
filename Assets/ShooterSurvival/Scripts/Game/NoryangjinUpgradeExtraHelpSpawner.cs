using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [DisallowMultipleComponent]
    public sealed class NoryangjinUpgradeExtraHelpSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject tungTungTungPrefab;
        [SerializeField] private GameObject boomBarDinoPrefab;

        private bool spawnedForCurrentRun;

        public bool IsConfigured =>
            tungTungTungPrefab != null &&
            boomBarDinoPrefab != null;

        public void Configure(GameObject tungTungTung, GameObject boomBarDino)
        {
            tungTungTungPrefab = tungTungTung;
            boomBarDinoPrefab = boomBarDino;
        }

        public void ApplyUpgradeExtraHelps(PlayerScript player)
        {
            if (spawnedForCurrentRun || player == null || UpgradeStatManager.S == null)
                return;

            spawnedForCurrentRun = true;
            int tungCount = Mathf.Max(
                0,
                Mathf.RoundToInt(
                    UpgradeStatManager.S.GetStat(
                        UpgradeStatManager.UpgradeType.TUNGTUNGTUNG)));
            int boomCount = Mathf.Max(
                0,
                Mathf.RoundToInt(
                    UpgradeStatManager.S.GetStat(
                        UpgradeStatManager.UpgradeType.BOOMBAR)));

            for (int i = 0; i < tungCount; i++)
                SpawnExtraHelp(tungTungTungPrefab, HelpType.Tungtungtung, player);

            for (int i = 0; i < boomCount; i++)
                SpawnExtraHelp(boomBarDinoPrefab, HelpType.Boombardino, player);
        }

        private static void SpawnExtraHelp(
            GameObject prefab,
            HelpType helpType,
            PlayerScript player)
        {
            if (prefab == null)
                return;

            Vector3 spawnOffset =
                player.transform.right * 1.5f -
                player.transform.forward * 0.75f;
            GameObject helper = Instantiate(
                prefab,
                player.transform.position + spawnOffset,
                player.transform.rotation);
            ExtraHelpBuffScript extraHelp = helper.GetComponent<ExtraHelpBuffScript>();
            if (extraHelp != null)
            {
                player.extraHelpCount++;
                extraHelp.spawnIndex = player.extraHelpCount - 1;
                extraHelp.helpType = helpType;
            }

            WeaponScript weapon = helper.GetComponentInChildren<WeaponScript>();
            if (weapon != null && player.extraHelpWeaponScript != null)
                player.extraHelpWeaponScript.Add(weapon);
        }
    }
}
