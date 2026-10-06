using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Owned helpers retain their health; only their route pose is managed here.
public sealed class Chapter45HelperFollower : MonoBehaviour
{
    private Chapter45Director director;
    private float distance, lane, heightOffset;
    private int floor = int.MinValue;
    public void Rebase(Chapter45Director owner)
    {
        director = owner; floor = owner.CurrentFloor;
        owner.SampleOnCurrentDeck(owner.Distance, out var center, out var forward);
        Vector3 offset = transform.position - center;
        distance = Mathf.Clamp(owner.Distance + Vector3.Dot(offset, forward), owner.route.SegmentStart(owner.CurrentSegment), owner.route.SegmentEnd(owner.CurrentSegment));
        lane = Mathf.Clamp(Vector3.Dot(offset, Vector3.Cross(Vector3.up, forward)), -3.5f, 3.5f);
        heightOffset = offset.y;
    }
    public void Advance(Chapter45Director owner, float step)
    {
        if (director != owner || floor != owner.CurrentFloor) Rebase(owner);
        if (owner.IsTransferring) return;
        distance = Chapter45Rules.HelperProgress(distance, step, owner.Distance, owner.route.SegmentEnd(owner.CurrentSegment));
        owner.SampleOnCurrentDeck(distance, out var center, out var forward);
        lane = Mathf.MoveTowards(lane, owner.Lane, step * .65f);
        transform.position = center + Vector3.Cross(Vector3.up, forward) * lane + Vector3.up * heightOffset;
        transform.rotation = Quaternion.LookRotation(forward);
    }
}
