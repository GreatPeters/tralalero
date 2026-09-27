using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// The rest-stop main building is walked through lengthwise. While the shark is inside (or at the
// entrance), its roof and overhead signs would cover the whole gameplay view, including the food-court
// holdout's raised camera, so they are switched off entirely and restored on exit. The generic camera
// occlusion only fades blocking scenery to 40%, which is not enough under a full roof.
public sealed class RestStopBuildingVisibility : MonoBehaviour
{
    public Renderer[] roof = System.Array.Empty<Renderer>();
    [Tooltip("World-space footprint (x/z) where the roof is hidden; y is ignored.")]
    public Bounds hideZone;
    private PlayerScript player;
    private bool[] original;
    private bool hidden;

    public static bool Inside(Bounds zone, Vector3 p) => p.x >= zone.min.x && p.x <= zone.max.x && p.z >= zone.min.z && p.z <= zone.max.z;

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerScript>();
        original = new bool[roof.Length];
        for (int i = 0; i < roof.Length; i++) if (roof[i] != null) original[i] = roof[i].forceRenderingOff;
    }

    private void LateUpdate()
    {
        if (player == null) return;
        bool inside = Inside(hideZone, player.transform.position);
        if (inside == hidden) return;
        hidden = inside;
        for (int i = 0; i < roof.Length; i++) if (roof[i] != null) roof[i].forceRenderingOff = inside || original[i];
    }

    private void OnDisable()
    {
        for (int i = 0; i < roof.Length; i++) if (roof[i] != null) roof[i].forceRenderingOff = original[i];
        hidden = false;
    }
}
