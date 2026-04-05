using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndianOceanAssets.ShooterSurvival
{
    public static class DamagePopupFX
    {
        private const float RiseDistance = 0.8f;
        private const float Duration = 0.75f;
        private const int PopupSortingOrder = 32767;
        private static readonly Vector3 CanvasScale = new Vector3(0.01f, 0.01f, 0.01f);
        private static readonly Color DamageColor = new Color(1f, 0.25f, 0.25f, 1f);

        public static void Show(Vector3 worldPosition, float amount)
        {
            GameObject popupObject = new GameObject("DamagePopup");
            popupObject.transform.position = worldPosition + new Vector3(Random.Range(-0.15f, 0.15f), 0.35f, 0f);
            popupObject.transform.localScale = CanvasScale;

            Camera cam = Camera.main;
            Canvas canvas = popupObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvas.overrideSorting = true;
            canvas.sortingOrder = PopupSortingOrder;

            popupObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            popupObject.AddComponent<GraphicRaycaster>();
            popupObject.transform.SetAsLastSibling();

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(240f, 120f);

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(popupObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = CreatePopupText(textObject.transform);
            text.text = Mathf.RoundToInt(amount).ToString();
            Color baseColor = text.color;

            if (cam != null)
                popupObject.transform.forward = cam.transform.forward;

            Sequence seq = DOTween.Sequence();
            seq.Join(popupObject.transform.DOMoveY(popupObject.transform.position.y + RiseDistance, Duration).SetEase(Ease.OutCubic));
            seq.Join(DOTween.To(
                () => 1f,
                alpha =>
                {
                    if (text == null)
                        return;

                    Color color = baseColor;
                    color.a = alpha;
                    text.color = color;
                },
                0f,
                Duration).SetEase(Ease.InQuad));
            seq.OnComplete(() => Object.Destroy(popupObject));
        }

        private static TextMeshProUGUI CreatePopupText(Transform parent)
        {
            CanvasScript canvasScript = Object.FindFirstObjectByType<CanvasScript>();
            if (canvasScript != null && canvasScript.DamagePopupPrefab != null)
            {
                TextMeshProUGUI instance = Object.Instantiate(canvasScript.DamagePopupPrefab, parent, false);
                RectTransform rect = instance.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return instance;
            }

            TextMeshProUGUI text = parent.gameObject.AddComponent<TextMeshProUGUI>();
            text.color = DamageColor;
            text.fontSize = 72f;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.8f);
            return text;
        }
    }
}
