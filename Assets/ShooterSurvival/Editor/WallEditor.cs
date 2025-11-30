using UnityEditor;
using UnityEngine;
using TMPro;
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

            if (wall.isRandom == true)
            {
                wall.rarity = (Rarity)EditorGUILayout.EnumPopup("Wall Rarity", wall.rarity);
            }

            // Wall Type Selection
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
                        case BuffType.att_normmal:
                            wall.att = EditorGUILayout.FloatField("att value", wall.att);
                            wall.attSpr = (Sprite)EditorGUILayout.ObjectField("att Sprite", wall.attSpr, typeof(Sprite), false);
                            break;
                        case BuffType.attPer_normal:
                            wall.attPercent = EditorGUILayout.FloatField("att per value", wall.attPercent);
                            wall.attPercentSpr = (Sprite)EditorGUILayout.ObjectField("att per Sprite", wall.attPercentSpr, typeof(Sprite), false);
                            break;
                        case BuffType.attackSpeed_normal:
                            wall.attackSpeed = EditorGUILayout.FloatField("attack speed", wall.attackSpeed);
                            wall.attackSpeedSpr = (Sprite)EditorGUILayout.ObjectField("attack speed Sprite", wall.attackSpeedSpr, typeof(Sprite), false);
                            break;
                        case BuffType.missileDistance_normal:
                            wall.missileDistance = EditorGUILayout.FloatField("missile distance", wall.missileDistance);
                            wall.missileDistanceSpr = (Sprite)EditorGUILayout.ObjectField("missile distance Sprite", wall.missileDistanceSpr, typeof(Sprite), false);
                            break;
                        case BuffType.hp_normal:
                            wall.hp = EditorGUILayout.FloatField("hp value", wall.hp);
                            wall.hpSpr = (Sprite)EditorGUILayout.ObjectField("hp Sprite", wall.hpSpr, typeof(Sprite), false);
                            break;
                        case BuffType.hpPer_normal:
                            wall.hpPercent = EditorGUILayout.FloatField("hp per value", wall.hpPercent);
                            wall.hpPercentSpr = (Sprite)EditorGUILayout.ObjectField("hp per Sprite", wall.hpPercentSpr, typeof(Sprite), false);
                            break;
                        case BuffType.tungtung_rare:
                            wall.tungtungAdd = EditorGUILayout.FloatField("Tungtung Add", wall.tungtungAdd);
                            wall.tungtungRareSpr = (Sprite)EditorGUILayout.ObjectField("Tungtung Rare Sprite", wall.tungtungRareSpr, typeof(Sprite), false);
                            break;
                        case BuffType.boombar_rare:
                            wall.boombarAdd = EditorGUILayout.FloatField("Boombar Add", wall.boombarAdd);
                            wall.boombarRareSpr = (Sprite)EditorGUILayout.ObjectField("Boombar Rare Sprite", wall.boombarRareSpr, typeof(Sprite), false);
                            break;
                        case BuffType.att_unique:
                            wall.att = EditorGUILayout.FloatField("Unique att value", wall.att);
                            wall.attUniqueSpr = (Sprite)EditorGUILayout.ObjectField("Unique att Sprite", wall.attUniqueSpr, typeof(Sprite), false);
                            break;
                        case BuffType.attPer_unique:
                            wall.attPercent = EditorGUILayout.FloatField("Unique att per value", wall.attPercent);
                            wall.attPercentSpr = (Sprite)EditorGUILayout.ObjectField("Unique att per Sprite", wall.attPerUniqueSpr, typeof(Sprite), false);
                            break;
                        case BuffType.missileAdd_unique:
                            wall.missileAdd = EditorGUILayout.FloatField("Unique missile add", wall.missileAdd);
                            wall.missileAddSpr = (Sprite)EditorGUILayout.ObjectField("Unique missile add Sprite", wall.missileAddUniqueSpr, typeof(Sprite), false);
                            break;
                        case BuffType.attackSpeed_unique:
                            wall.attackSpeed = EditorGUILayout.FloatField("Unique missile speed", wall.attackSpeed);
                            wall.attackSpeedSpr = (Sprite)EditorGUILayout.ObjectField("Unique missile speed Sprite", wall.attackSpeedUniqueSpr, typeof(Sprite), false);
                            break;
                        case BuffType.missileDistance_unique:
                            wall.missileDistance = EditorGUILayout.FloatField("Unique missile distance", wall.missileDistance);
                            wall.missileDistanceSpr = (Sprite)EditorGUILayout.ObjectField("Unique missile distance Sprite", wall.distanceUniqueSpr, typeof(Sprite), false);
                            break;
                        case BuffType.hp_unique:
                            wall.hp = EditorGUILayout.FloatField("Unique hp value", wall.hp);
                            wall.hpSpr = (Sprite)EditorGUILayout.ObjectField("Unique hp Sprite", wall.hpUniqueSpr, typeof(Sprite), false);
                            break;
                        case BuffType.hpPer_unique:
                            wall.hpPercent = EditorGUILayout.FloatField("Unique hp per value", wall.hpPercent);
                            wall.hpPercentSpr = (Sprite)EditorGUILayout.ObjectField("Unique hp per Sprite", wall.hpPerUniqueSpr, typeof(Sprite), false);
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
            wall.buffSFX = (AudioClip)EditorGUILayout.ObjectField($"Buff SFX", wall.buffSFX, typeof(AudioClip), true);
            wall.nerfSFX = (AudioClip)EditorGUILayout.ObjectField($"Nerf SFX", wall.nerfSFX, typeof(AudioClip), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI (Localization)", EditorStyles.boldLabel);
            wall.statNameLoc = (LocalizeStringEvent)EditorGUILayout.ObjectField(
                "Stat Name (LocalizeStringEvent)", wall.statNameLoc, typeof(LocalizeStringEvent), true);

            //wall.statValueTmp = (TextMeshProUGUI)EditorGUILayout.ObjectField(
              //  "Stat Value (TMP)", wall.statValueTmp, typeof(TextMeshProUGUI), true);


            if (GUI.changed)
            {
                EditorUtility.SetDirty(wall);
            }
        }
    }
}