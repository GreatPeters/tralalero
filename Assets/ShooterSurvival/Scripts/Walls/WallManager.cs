using System.Collections;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;


public class WallManager : MonoBehaviour
{
    public static WallManager S;
    public WallScript[] walls;
    private Coroutine checkWallSameAbilityCoroutine;

    void Awake()
    {
        S = this; 
    }

    void Start()
    {
        ScheduleSameAbilityCheck();
    }

    public void InIt()
    {
        foreach(var w in walls)
        {
            bool wasActive = w.gameObject.activeInHierarchy;
            w.ReactivateLifetimeObject();
            if (!wasActive && w.gameObject.activeInHierarchy)
                continue;

            w.SetRandomStat();
            w.SetStats();
            w.SetWallSprite();
        }

        ScheduleSameAbilityCheck();
    }

    private void ScheduleSameAbilityCheck()
    {
        if (checkWallSameAbilityCoroutine != null)
            StopCoroutine(checkWallSameAbilityCoroutine);

        checkWallSameAbilityCoroutine = StartCoroutine(CheckWallSameAbility());
    }

    IEnumerator CheckWallSameAbility()
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i + 1 < walls.Length; i += 2)
        {
            var a = walls[i];
            var b = walls[i + 1];

            if (a.buffType == b.buffType)
                b.RerollTWallType(a.buffType);
        }

        checkWallSameAbilityCoroutine = null;
    }
}
