using UnityEngine;

public class HandPingPongUI : MonoBehaviour
{
    [SerializeField] private float distance = 35f;
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rectTransform;
    private Vector2 startAnchoredPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            startAnchoredPosition = rectTransform.anchoredPosition;
    }

    void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
            rectTransform.anchoredPosition = startAnchoredPosition;
    }

    void Update()
    {
        if (rectTransform == null)
            return;

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float offsetX = Mathf.Sin(time * speed) * distance;
        rectTransform.anchoredPosition = startAnchoredPosition + new Vector2(offsetX, 0f);
    }
}
