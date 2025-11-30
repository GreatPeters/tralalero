using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class WeaponManager : MonoBehaviour
    {
        [HideInInspector] public GameObject currentWeapon;          // The currently active weapon
        [SerializeField] private GameObject weaponHolder;           // The container holding all weapon objects

        /*
            Pistol - 0
            Rifle - 1
            Shotgun - 2
            Minigun - 3
        */

        private void Start()
        {
            HideAllWeapons();               // Hide all weapons at the start
            ChangeWeapon(0);                // Set the first weapon (Pistol)
        }

        // Changes the current weapon based on the provided index.
        public void ChangeWeapon(int currentWeaponIndex)
        {
            HideAllWeapons();                                   // Hide all weapons first

            if (currentWeaponIndex < weaponHolder.transform.childCount)
            {
                currentWeapon = weaponHolder.transform.GetChild(currentWeaponIndex).gameObject;
                currentWeapon.SetActive(true);
            }
        }

        // Hides all weapons
        private void HideAllWeapons()
        {
            for (int i = 0; i < weaponHolder.transform.childCount; i++)
            {
                weaponHolder.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}