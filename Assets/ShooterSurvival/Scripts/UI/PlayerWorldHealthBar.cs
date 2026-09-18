using IndianOceanAssets.ShooterSurvival;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A screen-space health bar that follows the real player without moving the camera.</summary>
public sealed class PlayerWorldHealthBar : MonoBehaviour
{
    public PlayerScript player;
    public Camera gameplayCamera;
    public Image fill;
    public CanvasGroup visibility;
    public Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    public Vector2 screenOffset = new Vector2(0f, 44f);

    private RectTransform rect;
    private Canvas canvas;

    private void Awake()
    {
        rect = (RectTransform)transform;
        var parentCanvas = GetComponentInParent<Canvas>();
        canvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
    }

    private void LateUpdate()
    {
        if (player == null || gameplayCamera == null || fill == null || canvas == null)
        {
            if (visibility != null) visibility.alpha = 0f;
            return;
        }

        Vector3 screen = gameplayCamera.WorldToScreenPoint(player.transform.position + worldOffset);
        bool visible = screen.z > 0f && gameplayCamera.pixelRect.Contains(screen);
        if (visibility != null) visibility.alpha = visible ? 1f : 0f;
        if (!visible) return;

        var parent = (RectTransform)rect.parent;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out Vector2 local))
            rect.anchoredPosition = local + screenOffset;

        float maximum = player.MaxHealth;
        fill.fillAmount = maximum > 0f ? Mathf.Clamp01(player.currentHealth / maximum) : 0f;
    }
}
