using System;
using UnityEditor;
using UnityEngine;

public static class OriginalUpgradeArtwork
{
    public const string DatabasePath="Assets/JH/SO/SpriteDatabaseSO.asset";
    static readonly string[] Names={"공격력","체력","공격속도","미사일속도","보스피해","코인업","체력회복","퉁퉁퉁사후르","붐바르딜로"};
    public static Sprite ForUpgrade(int id)
    {
        if(id>=1&&id<=Names.Length)return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/"+Names[id-1]+".png");
        // The later lateral-speed stat has no file in the original nine-icon set.
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/Image/Lobby/Upgrade/img_lobby_upgrade_speed.png");
    }
    public static SpriteDatabase RefreshDatabase()
    {
        var database=AssetDatabase.LoadAssetAtPath<SpriteDatabase>(DatabasePath);
        var data=new SerializedObject(database);var entries=data.FindProperty("entries");
        void Set(string key,Sprite sprite)
        {
            int index=-1;
            for(int i=0;i<entries.arraySize;i++)if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==key){index=i;break;}
            if(index<0){index=entries.arraySize;entries.InsertArrayElementAtIndex(index);}
            var entry=entries.GetArrayElementAtIndex(index);entry.FindPropertyRelative("key").stringValue=key;entry.FindPropertyRelative("sprite").objectReferenceValue=sprite;
        }
        string[] keys={"ATT","HP","ATT_SPEED","PROJECTILE_SPEED","BOSS_DAMAGE","COIN_BONUS","HP_REGEN","TUNGTUNGTUNG","BOOMBAR","MOVE_SPEED"};
        for(int i=0;i<keys.Length;i++)Set(keys[i],ForUpgrade(i+1));
        Set("WallBonus_Attack",ForUpgrade(1));Set("WallBonus_Health",ForUpgrade(2));Set("WallBonus_AttackSpeed",ForUpgrade(3));
        Set("WallBonus_MissileDuration",ForUpgrade(4));Set("WallBonus_MissileAdd",ForUpgrade(4));
        Set("WallBonus_Tungtung",ForUpgrade(8));Set("WallBonus_Boombar",ForUpgrade(9));
        data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(database);return database;
    }
}
