using UnityEngine;

// Presentation visibility. Supporting colliders and gameplay objects stay live.
[DisallowMultipleComponent]
public sealed class Chapter45SceneryGroup : MonoBehaviour
{
    public float startDistance, endDistance;
    public int floor = -1;
    public bool alwaysVisible;
    public bool hideDuringTransfer;
    // Floor slabs stay below the departing cabin; hide only at its arrival deck.
    public bool hideWhileArriving;
    // Optional presentation-only exclusion for an enclosed room in the tower.
    public int hiddenOnFloor = -1;
    private Renderer[] renderers;
    private bool[] original;
    private bool hidden;
    private Light[] lights;
    private bool[] lightEnabled;
    public void SetVisible(bool visible)
    {
        if (renderers == null)
        {
            renderers = GetComponentsInChildren<Renderer>(true); original = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) original[i] = renderers[i].forceRenderingOff;
            lights = GetComponentsInChildren<Light>(true);
            lightEnabled = new bool[lights.Length];
            for (int i = 0; i < lights.Length; i++) lightEnabled[i] = lights[i].enabled;
        }
        if (hidden == !visible) return;
        hidden = !visible;
        for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].forceRenderingOff = hidden || original[i];
        for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].enabled = !hidden && lightEnabled[i];
    }
    public void Restore() { SetVisible(true); }
    private void OnDisable() => Restore();
}
