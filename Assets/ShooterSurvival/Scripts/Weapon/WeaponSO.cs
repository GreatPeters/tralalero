using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Weapon/Create New Weapon Type")]
    public class WeaponSO : ScriptableObject
    {
        [Header("Enemy Stats")]
        [Tooltip("Damage dealt by the weapon")]
        public float weaponDamage;                                  // Weapon damage

        [Tooltip("Fire rate of the weapon (rounds per second)")]
        public float weaponFireRate;                                // Weapon fire rate

        [Header("Dependencies")]
        [Tooltip("Sound effect played when the weapon is fired")]
        public AudioClip weaponSound;
    }
}