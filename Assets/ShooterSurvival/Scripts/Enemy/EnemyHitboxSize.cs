using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHitboxSize : MonoBehaviour
{
    [SerializeField] private Collider body;
    [SerializeField] private bool captured;
    [SerializeField] private float originalRadius;
    [SerializeField] private Vector3 originalSize;
    public void Configure(Collider target)
    {
        body=target;
        // A scene can override a tuned prefab's radius. Treat that authored value
        // as its own baseline instead of replacing it with the prefab baseline.
        if(captured)
        {
            float expected=Mathf.Round(originalRadius*1.3f*100f)/100f;
            if(body is CapsuleCollider cap&&!Mathf.Approximately(cap.radius,expected))captured=false;
            else if(body is SphereCollider sph&&!Mathf.Approximately(sph.radius,expected))captured=false;
            else if(body is BoxCollider box&&box.size!=new Vector3(originalSize.x*1.3f,originalSize.y,originalSize.z*1.3f))captured=false;
        }
        if(!captured)
        {
            if(body is CapsuleCollider capsule)originalRadius=capsule.radius;
            else if(body is SphereCollider sphere)originalRadius=sphere.radius;
            else if(body is BoxCollider box)originalSize=box.size;
            captured=true;
        }
        Apply();
    }
    private void Awake() => Apply();
    private void Apply()
    {
        if(!captured||body==null)return;
        float radius=Mathf.Round(originalRadius*1.3f*100f)/100f;
        if(body is CapsuleCollider capsule)capsule.radius=radius;
        else if(body is SphereCollider sphere)sphere.radius=radius;
        else if(body is BoxCollider box)box.size=new Vector3(originalSize.x*1.3f,originalSize.y,originalSize.z*1.3f);
    }
}
