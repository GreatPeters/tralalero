using UnityEngine;

/// <summary>Metal lamps acknowledge a projectile without removing their contact hazard.</summary>
public sealed class LampImpactFeedback : MonoBehaviour
{
    private Renderer target;
    private MaterialPropertyBlock original;
    private MaterialPropertyBlock flash;
    private float remaining;
    public int ImpactCount { get; private set; }
    private int lastFrame = -1;

    public void Pulse()
    {
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        if (target == null)
        {
            target = GetComponent<Renderer>();
            if (target == null) target = GetComponentInChildren<Renderer>();
            if (target == null) return;
            original = new MaterialPropertyBlock(); target.GetPropertyBlock(original);
            flash = new MaterialPropertyBlock(); target.GetPropertyBlock(flash);
            flash.SetColor("_BaseColor", new Color(1f, .66f, .18f));
            flash.SetColor("_EmissionColor", new Color(2f, .8f, .12f));
        }
        ImpactCount++;
        remaining = .14f;
        target.SetPropertyBlock(flash);
    }
    private void Update()
    {
        if (remaining <= 0f) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f && target != null) target.SetPropertyBlock(original);
    }
    private void OnDisable()
    {
        if (target != null) target.SetPropertyBlock(original);
        remaining = 0f;
    }
}
