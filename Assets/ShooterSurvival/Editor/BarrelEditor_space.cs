using UnityEditor;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [CustomEditor(typeof(BarrelScript_space))]
    public class BarrelEditor_space : Editor
    {
        public override void OnInspectorGUI()
        {
            BarrelScript_space barrel = (BarrelScript_space)target;

            // Weapon Type Selection
            barrel.barrelType = (BarrelType)EditorGUILayout.EnumPopup("Barrel Type", barrel.barrelType);
            EditorGUILayout.Space();

            // Show relevant health and sprite fields based on selected weapon
            switch (barrel.barrelType)
            {
                case BarrelType.Pistol:
                    EditorGUILayout.LabelField("Barrel Properties", EditorStyles.boldLabel);
                    barrel.PistolBarrelHealth = EditorGUILayout.FloatField("Barrel Health", barrel.PistolBarrelHealth);
                    barrel.PistolBarrelSprite = (Sprite)EditorGUILayout.ObjectField("Pistol Sprite", barrel.PistolBarrelSprite, typeof(Sprite), false);
                    break;

                case BarrelType.Rifle:
                    EditorGUILayout.LabelField("Barrel Properties", EditorStyles.boldLabel);
                    barrel.RifleBarrelHealth = EditorGUILayout.FloatField("Barrel Health", barrel.RifleBarrelHealth);
                    barrel.RifleBarrelSprite = (Sprite)EditorGUILayout.ObjectField("Rifle Sprite", barrel.RifleBarrelSprite, typeof(Sprite), false);
                    break;

                case BarrelType.Shotgun:
                    EditorGUILayout.LabelField("Barrel Properties", EditorStyles.boldLabel);
                    barrel.ShotgunBarrelHealth = EditorGUILayout.FloatField("Barrel Health", barrel.ShotgunBarrelHealth);
                    barrel.ShotgunBarrelSprite = (Sprite)EditorGUILayout.ObjectField("Shotgun Sprite", barrel.ShotgunBarrelSprite, typeof(Sprite), false);
                    break;

                case BarrelType.Minigun:
                    EditorGUILayout.LabelField("Barrel Properties", EditorStyles.boldLabel);
                    barrel.MinigunBarrelHealth = EditorGUILayout.FloatField("Barrel Health", barrel.MinigunBarrelHealth);
                    barrel.MinigunBarrelSprite = (Sprite)EditorGUILayout.ObjectField("Minigun Sprite", barrel.MinigunBarrelSprite, typeof(Sprite), false);
                    break;
            }

            barrel.barrelDamage = EditorGUILayout.FloatField("Barrel Damage", barrel.barrelDamage);


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Death Settings", EditorStyles.boldLabel);
            barrel.deathRadius = EditorGUILayout.FloatField("Death Radius", barrel.deathRadius);
            barrel.deathDamage = EditorGUILayout.FloatField("Death Damage", barrel.deathDamage);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dependencies", EditorStyles.boldLabel);

            barrel.bulletHitFX = (GameObject)EditorGUILayout.ObjectField($"Bullet Hit VFX", barrel.bulletHitFX, typeof(GameObject), true);
            barrel.barrelHitSFX = (AudioClip)EditorGUILayout.ObjectField($"Barrel Hit SFX", barrel.barrelHitSFX, typeof(AudioClip), true);



            if (GUI.changed)
            {
                EditorUtility.SetDirty(barrel);
            }
        }
    }
}