using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HighwayEncounterMember : MonoBehaviour
{
    public HighwayEncounterRow row;
    public bool AcceptPhysicalContact(EnemyScript_space actor) => row==null || row.AcceptPhysicalContact(actor);
}
