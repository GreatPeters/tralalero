using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class MobileSafeArea : MonoBehaviour
{
    private Rect lastArea;
    private Vector2Int lastScreen;
    private RectTransform target;
    private void OnEnable() { target = (RectTransform)transform; lastScreen = Vector2Int.zero; Apply(); }
    private void Update() => Apply();
    private void Apply()
    {
        if (target == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect area = Screen.safeArea;
        var screen = new Vector2Int(Screen.width, Screen.height);
        if (area == lastArea && screen == lastScreen) return;
        lastArea = area; lastScreen = screen;
        target.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
        target.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
        target.offsetMin = target.offsetMax = Vector2.zero;
    }
}
