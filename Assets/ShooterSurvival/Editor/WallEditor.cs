using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace IndianOceanAssets.ShooterSurvival
{
    [CustomEditor(typeof(WallScript))]
    public class WallEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            WallScript wall = (WallScript)target;

            wall.isRandom = EditorGUILayout.Toggle("Is Random Wall", wall.isRandom);

            if (wall.isRandom)
            {
                wall.rarity = (Rarity)EditorGUILayout.EnumPopup("Wall Rarity", wall.rarity);
            }

            wall.wallType = (WallType)EditorGUILayout.EnumPopup("Wall Type", wall.wallType);
            EditorGUILayout.Space();

            switch (wall.wallType)
            {
                case WallType.BuffWall:
                    EditorGUILayout.LabelField("Buff Wall Properties", EditorStyles.boldLabel);
                    wall.buffType = (BuffType)EditorGUILayout.EnumPopup("Buff Type", wall.buffType);

                    switch (wall.buffType)
                    {
                        case BuffType.HealthBoost:
                            wall.healthBoostAmt = EditorGUILayout.IntField("Health Boost Amount", wall.healthBoostAmt);
                            wall.healthBoostSpr = (Sprite)EditorGUILayout.ObjectField("Health Boost Sprite", wall.healthBoostSpr, typeof(Sprite), false);
                            break;
                        case BuffType.FireRateIncrease:
                            wall.fireRateIncMultipier = EditorGUILayout.FloatField("Fire Rate Inc Multiplier", wall.fireRateIncMultipier);
                            wall.fireRateIncreaseSpr = (Sprite)EditorGUILayout.ObjectField("Fire Rate Inc Sprite", wall.fireRateIncreaseSpr, typeof(Sprite), false);
                            break;
                        case BuffType.ExtraHelp:
                            wall.extraHelp = (GameObject)EditorGUILayout.ObjectField("Extra Help Object", wall.extraHelp, typeof(GameObject), false);
                            wall.extraHelpSpr = (Sprite)EditorGUILayout.ObjectField("Extra Help Sprite", wall.extraHelpSpr, typeof(Sprite), false);
                            break;
                    }
                    break;

                case WallType.NerfWall:
                    EditorGUILayout.LabelField("Nerf Wall Properties", EditorStyles.boldLabel);
                    wall.nerfType = (NerfType)EditorGUILayout.EnumPopup("Nerf Type", wall.nerfType);

                    switch (wall.nerfType)
                    {
                        case NerfType.HealthReduce:
                            wall.healthReduceAmt = EditorGUILayout.IntField("Health Reduce Amount", wall.healthReduceAmt);
                            wall.healthReduceSpr = (Sprite)EditorGUILayout.ObjectField("Health Reduce Sprite", wall.healthReduceSpr, typeof(Sprite), false);
                            break;
                        case NerfType.FireRateReduce:
                            wall.fireRateDecMultipier = EditorGUILayout.FloatField("Fire Rate Dec Multiplier", wall.fireRateDecMultipier);
                            wall.fireRateReduceSpr = (Sprite)EditorGUILayout.ObjectField("Fire Rate Dec Sprite", wall.fireRateReduceSpr, typeof(Sprite), false);
                            break;
                    }
                    break;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dependencies", EditorStyles.boldLabel);
            wall.buffSFX = (AudioClip)EditorGUILayout.ObjectField("Buff SFX", wall.buffSFX, typeof(AudioClip), true);
            wall.nerfSFX = (AudioClip)EditorGUILayout.ObjectField("Nerf SFX", wall.nerfSFX, typeof(AudioClip), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI (Localization)", EditorStyles.boldLabel);
            wall.statNameLoc = (LocalizeStringEvent)EditorGUILayout.ObjectField(
                "Stat Name (LocalizeStringEvent)", wall.statNameLoc, typeof(LocalizeStringEvent), true);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(wall);
            }
        }
    }
}
