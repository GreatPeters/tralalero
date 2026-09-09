using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public sealed class ResponsiveCardGrid : MonoBehaviour
{
    public int rows = 3;
    public float cardAspect = 300f / 390f;
    private Vector2 lastSize;
    private void OnEnable() => lastSize = Vector2.zero;
    private void LateUpdate()
    {
        var rect = (RectTransform)transform;
        if (rect.rect.size == lastSize) return;
        lastSize = rect.rect.size;
        var grid = GetComponent<GridLayoutGroup>();
        int columns = Mathf.Max(1, grid.constraintCount);
        float width = (rect.rect.width - grid.padding.horizontal - grid.spacing.x * (columns - 1)) / columns;
        float height = (rect.rect.height - grid.padding.vertical - grid.spacing.y * (rows - 1)) / Mathf.Max(1, rows);
        height = Mathf.Min(height, width / cardAspect);
        grid.cellSize = new Vector2(Mathf.Max(1, height * cardAspect), Mathf.Max(1, height));
    }
}
