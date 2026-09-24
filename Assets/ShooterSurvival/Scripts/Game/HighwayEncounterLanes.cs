using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

[DisallowMultipleComponent]
public sealed class HighwayEncounterLanes : MonoBehaviour
{
    public HighwayEncounterRow[] rows = System.Array.Empty<HighwayEncounterRow>();
    private HighwayRoute route;
    private Transform player;
    void Awake() => route=GetComponent<HighwayRoute>();
    public void ResetForRun(){foreach(var row in rows)if(row!=null)row.ResetForRun();}
    public float Constrain(float lane,float distance,bool bypass)
    {
        if(route==null)route=GetComponent<HighwayRoute>();
        foreach(var row in rows)
        {
            if(row==null||!row.isActiveAndEnabled)continue;
            float ahead=row.routeDistance-distance;
            if(ahead>22||ahead< -6)continue;
            if(!AuthoredRowEnabled(row))continue;
            // A recovery branch is another road, not this encounter's passage.
            if(bypass&&route!=null&&(route.Point(distance,true)-route.Point(distance,false)).sqrMagnitude>9)continue;
            float width=HighwayEncounterRow.PassageHalfWidth(ahead,4.4f,row.halfWidth);
            lane=Mathf.Clamp(lane,row.laneCenter-width,row.laneCenter+width);
        }
        return lane;
    }
    public Vector3 ConstrainWorldPosition(Vector3 position,Vector3 heading)
    {
        if(!isActiveAndEnabled)return position;
        // RestStop uses authored straight segments and discrete turns. Match the
        // row's world frame so nearby parallel/opposite roads stay independent.
        foreach(var row in rows)
        {
            if(row==null||!row.isActiveAndEnabled||row.barriers==null)continue;
            var forward=row.transform.forward;var right=row.transform.right;
            if(Vector3.Dot(heading,forward)<.8f)continue;
            var offset=position-row.transform.position;
            float ahead=-Vector3.Dot(offset,forward),lane=Vector3.Dot(offset,right);
            if(ahead>22||ahead< -6||Mathf.Abs(lane-row.laneCenter)>8||Mathf.Abs(offset.y)>2)continue;
            if(!AuthoredRowEnabled(row))continue;
            float width=HighwayEncounterRow.PassageHalfWidth(ahead,4.4f,row.halfWidth);
            position+=right*(Mathf.Clamp(lane,row.laneCenter-width,row.laneCenter+width)-lane);
        }
        return position;
    }
    static bool AuthoredRowEnabled(HighwayEncounterRow row) =>
        row.left==null||row.right==null||!EncounterPlacementController.IsExplicitlyDisabled(row.left)||!EncounterPlacementController.IsExplicitlyDisabled(row.right);
    void LateUpdate()
    {
        if(route==null&&player==null)player=FindFirstObjectByType<PlayerScript>()?.transform;
        if(route==null&&player==null)return;
        foreach(var row in rows)if(row!=null&&row.barriers!=null)
        {
            bool visible=(route!=null?Mathf.Abs(row.routeDistance-route.Distance)<90:(row.transform.position-player.position).sqrMagnitude<90*90)&&AuthoredRowEnabled(row);
            if(row.barriers.gameObject.activeSelf!=visible)row.barriers.gameObject.SetActive(visible);
        }
    }
}
