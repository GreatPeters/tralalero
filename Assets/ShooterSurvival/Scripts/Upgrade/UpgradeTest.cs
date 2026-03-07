using UnityEngine;

public class UpgradeTest : MonoBehaviour
{
    void Start()
    {
        // 업그레이드
        var up = UpgradeTables.Get(1, 2);
        Debug.Log($"UPGRADE -> {up}");

        // 스킨
        var skin = SkinTables.Get(1);
        Debug.Log($"SKIN -> {skin}");

        // 없는 값 안전 확인
        if (!UpgradeTables.TryGet(999, 1, out var none))
            Debug.Log("Upgrade 999/1 없음 (OK)");

        if (!SkinTables.TryGet(999, out var noneSkin))
            Debug.Log("Skin 999 없음 (OK)");
    }
}
