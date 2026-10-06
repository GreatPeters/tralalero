using System;using System.Linq;using UnityEngine;
public static class InspectCycle03Hud {
 public static object Main(){var h=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>().hud;object R(RectTransform r)=>new{r.name,rect=r.rect.ToString(),size=r.sizeDelta.ToString(),pivot=r.pivot.ToString(),anchorMin=r.anchorMin.ToString(),anchorMax=r.anchorMax.ToString(),position=r.anchoredPosition.ToString()};return new{panel=R(h.panel.GetComponent<RectTransform>()),description=R(h.description.rectTransform),title=R(h.title.rectTransform),h.description.fontSize,h.description.fontSizeMin,h.description.fontSizeMax,h.description.enableAutoSizing,h.description.overflowMode,readOnly=true};}
}
