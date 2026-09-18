using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class EquipmentCardGrid : MonoBehaviour
{
    public GridLayoutGroup grid;
    public ScrollRect scroll;
    public float cardHeight = 186f;
    private float width, height;
    private int count, lastColumns;
    private void LateUpdate() => RefreshLayout();
    public void RefreshLayout()
    {
        var rect = (RectTransform)transform;
        int active = 0;
        for (int i=0;i<transform.childCount;i++) if(transform.GetChild(i).gameObject.activeSelf) active++;
        if (grid == null) return;
        int columns = Mathf.Max(1, grid.constraintCount);
        int rows = Mathf.CeilToInt(active/(float)columns);
        float requiredHeight = rows*cardHeight+Mathf.Max(0,rows-1)*grid.spacing.y+grid.padding.vertical;
        if (Mathf.Abs(rect.rect.width-width)<.1f && Mathf.Abs(rect.rect.height-requiredHeight)<.1f && count==active && lastColumns==columns && Mathf.Approximately(height,cardHeight)) return;
        width=rect.rect.width;count=active;
        lastColumns=columns;height=cardHeight;
        grid.cellSize=new Vector2(Mathf.Max(1,(width-grid.padding.horizontal-grid.spacing.x*(columns-1))/columns),cardHeight);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,requiredHeight);
    }
}
