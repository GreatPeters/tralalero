using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CosmeticPreviewDrag : MonoBehaviour, IDragHandler
{
    public CosmeticPreview preview;
    public void OnDrag(PointerEventData eventData)
    {
        float width=((RectTransform)transform).rect.width*transform.lossyScale.x;
        if(width>0 && preview!=null)preview.Rotate(eventData.delta.x/width*180f);
    }
}
