using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class HighwayUniquePickup : MonoBehaviour
{
    public HighwayChapter2Controller owner;
    public int choice;
    private BoxCollider trigger;
    private void Awake(){trigger=GetComponent<BoxCollider>();trigger.isTrigger=true;}
    public bool Touches(PlayerScript player)
    {
        if(trigger==null)trigger=GetComponent<BoxCollider>();
        var body=player!=null?player.GetComponent<Collider>():null;
        return gameObject.activeInHierarchy&&trigger.enabled&&body!=null&&trigger.bounds.Intersects(body.bounds)
            &&Physics.ComputePenetration(trigger,trigger.transform.position,trigger.transform.rotation,body,body.transform.position,body.transform.rotation,out _,out _);
    }
    private void OnTriggerEnter(Collider other)=>Collect(other);
    private void OnTriggerStay(Collider other)=>Collect(other);
    private void Collect(Collider other)
    {
        var player=other.GetComponent<PlayerScript>();
        if(player!=null&&owner!=null)owner.TryCollectExit(choice,player);
    }
}
