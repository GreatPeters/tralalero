using UnityEngine;

/// <summary>Follow route yaw and height without inheriting the shark's slope pitch or animation.</summary>
[DisallowMultipleComponent]
public sealed class StableGameplayCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 yawRelativeOffset;
    public Quaternion yawRelativeRotation = Quaternion.identity;
    private float height, heightVelocity;
    private Vector3 lastTarget;

    public void Configure(Transform player)
    {
        target = player;
        var yaw = Quaternion.Euler(0, player.eulerAngles.y, 0);
        yawRelativeOffset = Quaternion.Inverse(yaw) * (transform.position - player.position);
        yawRelativeRotation = Quaternion.Inverse(yaw) * transform.rotation;
        height = transform.position.y;
        heightVelocity = 0;
        lastTarget = player.position;
    }
    private void OnEnable()
    {
        height = transform.position.y;
        if (target != null) lastTarget = target.position;
        heightVelocity = 0;
    }
    private void LateUpdate()
    {
        if (target == null) return;
        var yaw = Quaternion.Euler(0, target.eulerAngles.y, 0);
        var position = target.position + yaw * yawRelativeOffset;
        if ((target.position - lastTarget).sqrMagnitude > 225f) { height = position.y; heightVelocity = 0; }
        else height = Mathf.SmoothDamp(height, position.y, ref heightVelocity, .14f, Mathf.Infinity, Time.deltaTime);
        lastTarget = target.position;
        position.y = height;
        transform.SetPositionAndRotation(position, yaw * yawRelativeRotation);
    }
}
