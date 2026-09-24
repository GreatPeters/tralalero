using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// A visible construction funnel and its two physical opponents form one choice.
public sealed class HighwayEncounterRow : MonoBehaviour
{
    public EnemyScript_space left, right;
    public float routeDistance, laneCenter, halfWidth = 1.85f;
    public Transform barriers;
    private EnemyScript_space contacted;
    public bool HasLivingOpponent => Alive(left) || Alive(right);
    static bool Alive(EnemyScript_space actor) => actor != null && actor.gameObject.activeInHierarchy && actor.CurrentHealth > 0;
    public void ResetForRun() => contacted = null;
    public bool AcceptPhysicalContact(EnemyScript_space actor)
    {
        if(actor != left && actor != right)return false;
        if(contacted != null)return contacted == actor;
        contacted = actor;
        return true;
    }
    public static float PassageHalfWidth(float ahead,float openWidth,float combatWidth)
    {
        float entry=Mathf.SmoothStep(0,1,Mathf.InverseLerp(22,12,ahead));
        float exit=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-6,-3,ahead));
        return Mathf.Lerp(openWidth,combatWidth,Mathf.Min(entry,exit));
    }
}
