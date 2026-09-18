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
            int tungCount = ResolvePermanentCount(UpgradeStatManager.S.GetStat(UpgradeStatManager.UpgradeType.TUNGTUNGTUNG));
            int boomCount = ResolvePermanentCount(UpgradeStatManager.S.GetStat(UpgradeStatManager.UpgradeType.BOOMBAR));

            for (int i = 0; i < tungCount; i++)
                SpawnExtraHelp(tungTungTungPrefab, HelpType.Tungtungtung, player);

            for (int i = 0; i < boomCount; i++)
                SpawnExtraHelp(boomBarDinoPrefab, HelpType.Boombardino, player);
        }

        public ExtraHelpBuffScript SpawnBonus(HelpType type, PlayerScript player)
            => SpawnExtraHelp(type == HelpType.Tungtungtung ? tungTungTungPrefab : boomBarDinoPrefab, type, player);

        // Upgrade amounts describe helper health/attack, not the number of helpers.
        public static int ResolvePermanentCount(float value) => value > 0f && !float.IsInfinity(value) ? 1 : 0;

        public static ExtraHelpBuffScript SpawnExtraHelp(
            GameObject prefab,
            HelpType helpType,
            PlayerScript player)
        {
            if (prefab == null || player == null || prefab.GetComponent<ExtraHelpBuffScript>() == null)
                return null;

            Vector3 spawnOffset = helpType == HelpType.Tungtungtung ? Vector3.zero :
                -player.transform.forward * 1.5f + Vector3.up * 2f;
            GameObject helper = Instantiate(
                prefab,
                player.transform.position + spawnOffset,
                player.transform.rotation);
            if (helpType == HelpType.Boombardino)
            {
                Transform model = helper.transform.Find("bomb_0910060745_texture");
                if (model != null) model.localScale *= 1.7f;
            }
            ExtraHelpBuffScript extraHelp = helper.GetComponent<ExtraHelpBuffScript>();
            if (extraHelp != null)
            {
                player.extraHelpCount++;
                extraHelp.spawnIndex = player.extraHelpCount - 1;
                extraHelp.helpType = helpType;
                extraHelp.ConfigureOwner(player);
            }

            WeaponScript weapon = helper.GetComponentInChildren<WeaponScript>();
            if (weapon != null && player.extraHelpWeaponScript != null)
                player.extraHelpWeaponScript.Add(weapon);
            return extraHelp;
        }
    }
}
