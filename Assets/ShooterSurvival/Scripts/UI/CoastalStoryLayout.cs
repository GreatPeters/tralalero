using UnityEngine;
using UnityEngine.UI;

/// <summary>Keep authored chrome corners and controls aligned as phone height changes.</summary>
[ExecuteAlways]
public sealed class CoastalStoryLayout : MonoBehaviour
{
    public Image chrome;
    public RectTransform counter,skip,movieSlot,movieDisplay,caption,title,shade,previous,next;
    public RectTransform[] markers;
    private Vector2 lastSize;
    private void OnEnable(){lastSize=Vector2.zero;}
    private void LateUpdate(){var size=((RectTransform)transform).rect.size;if(size!=lastSize)Refresh();FitMovie();}
    private void FitMovie()
    {
        if(movieDisplay==null||movieSlot==null||movieSlot.rect.height<=0)return;
        var fit=movieDisplay.GetComponent<AspectRatioFitter>();if(fit==null||fit.aspectRatio<=0)return;
        float window=movieSlot.rect.width/movieSlot.rect.height;
        float retained=Mathf.Min(window/fit.aspectRatio,fit.aspectRatio/window);
        // Fill the approved tall-phone frame with at most 15% edge crop.
        // Shorter displays retain the complete image instead of losing the subject.
        var mode=retained>=.85f?AspectRatioFitter.AspectMode.EnvelopeParent:AspectRatioFitter.AspectMode.FitInParent;
        if(fit.aspectMode!=mode)fit.aspectMode=mode;
    }
    public void Refresh()
    {
        var rect=(RectTransform)transform;lastSize=rect.rect.size;if(chrome==null||lastSize.x<=0)return;
        float scale=lastSize.x/853f;chrome.pixelsPerUnitMultiplier=1f/scale;
        Top(counter,.052f,.531f,79,75,scale);Top(skip,.663f,.951f,79,75,scale);
        if(movieSlot!=null){movieSlot.anchorMin=Vector2.zero;movieSlot.anchorMax=Vector2.one;movieSlot.offsetMin=new Vector2(24*scale,367*scale);movieSlot.offsetMax=new Vector2(-24*scale,-189*scale);}
        Bottom(title,.19f,.81f,507,57,scale);Bottom(caption,.085f,.915f,397,109,scale);Bottom(shade,.025f,.975f,364,310,scale);
        if(markers!=null)for(int i=0;i<markers.Length;i++){Bottom(markers[i],.017f+i*.244f,.253f+i*.244f,229,103,scale);if(markers[i].GetComponent<Image>() is Image image)image.pixelsPerUnitMultiplier=1f/scale;}
        Bottom(previous,.013f,.474f,52,151,scale);Bottom(next,.483f,.985f,52,151,scale);
        if(previous!=null)previous.GetComponent<Image>().pixelsPerUnitMultiplier=1f/scale;if(next!=null)next.GetComponent<Image>().pixelsPerUnitMultiplier=1f/scale;
    }
    private static void Top(RectTransform target,float left,float right,float top,float height,float scale)
    {
        if(target==null)return;target.anchorMin=new Vector2(left,1);target.anchorMax=new Vector2(right,1);target.offsetMin=new Vector2(0,-(top+height)*scale);target.offsetMax=new Vector2(0,-top*scale);
    }
    private static void Bottom(RectTransform target,float left,float right,float bottom,float height,float scale)
    {
        if(target==null)return;target.anchorMin=new Vector2(left,0);target.anchorMax=new Vector2(right,0);target.offsetMin=new Vector2(0,bottom*scale);target.offsetMax=new Vector2(0,(bottom+height)*scale);
    }
}
