using UnityEngine;

// Old road modules combine planks and tall rail posts. Keep their colliders and restore their
// visuals outside the bypass; hiding props alone cannot clear the crossing view.
public sealed class NoryangjinCrossingVisibility : MonoBehaviour
{
    public Renderer[] targets = System.Array.Empty<Renderer>();
    private bool[] original;
    private NoryangjinMarketBranch branch;
    private void Awake()
    {
        branch = GetComponentInChildren<NoryangjinMarketBranch>();
        original = new bool[targets.Length];
        for (int i=0;i<targets.Length;i++) if(targets[i]!=null) original[i]=targets[i].forceRenderingOff;
    }
    private void LateUpdate()
    {
        bool hide = branch != null && branch.Driving && branch.Outside;
        for (int i=0;i<targets.Length;i++) if(targets[i]!=null) targets[i].forceRenderingOff=original[i]||hide;
    }
    private void OnDisable()
    {
        if(original==null)return;
        for(int i=0;i<targets.Length;i++)if(targets[i]!=null)targets[i].forceRenderingOff=original[i];
    }
}
