using System;

public static class SkinBonusResolver
{
    public static bool TryResolve(string bonusType, out UpgradeStatManager.UpgradeType upgradeType)
    {
        switch ((bonusType ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "ATT":
                upgradeType = UpgradeStatManager.UpgradeType.ATT;
                return true;
            case "HP":
                upgradeType = UpgradeStatManager.UpgradeType.HP;
                return true;
            case "ATT_SPEED":
                upgradeType = UpgradeStatManager.UpgradeType.ATT_SPEED;
                return true;
            case "PROJECTILE_SPEED":
                upgradeType = UpgradeStatManager.UpgradeType.PROJECTILE_SPEED;
                return true;
            case "BOSS_DAMAGE":
                upgradeType = UpgradeStatManager.UpgradeType.BOSS_DAMAGE;
                return true;
            case "COIN_BONUS":
                upgradeType = UpgradeStatManager.UpgradeType.COIN_BONUS;
                return true;
            case "HP_REGEN":
                upgradeType = UpgradeStatManager.UpgradeType.HP_REGEN;
                return true;
            case "TUNGTUNGTUNG":
                upgradeType = UpgradeStatManager.UpgradeType.TUNGTUNGTUNG;
                return true;
            case "BOOMBAR":
                upgradeType = UpgradeStatManager.UpgradeType.BOOMBAR;
                return true;
            default:
                upgradeType = default;
                return false;
        }
    }
}
