using System.Collections;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;


public class WallManager : MonoBehaviour
{
    public static WallManager S;
    public WallScript[] walls;

    void Awake()
    {
        S = this; 
    }

    void Start()
    {
        StartCoroutine("CheckWallSameAbility");
    }

    public void InIt()
    {
        foreach(var w in walls)
        {
            w.gameObject.SetActive(true);
            w.SetRandomStat();
            w.SetStats();
            w.SetWallSprite();
        }

        StartCoroutine("CheckWallSameAbility");
    }


    IEnumerator CheckWallSameAbility()
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < walls.Length; i+=2)
        {
            var a = walls[i];
            var b = walls[i + 1];

            if (a.buffType == b.buffType)
                b.RerollTWallType(a.buffType);
        }
    }
}
