using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndianOceanAssets.ShooterSurvival
{
    public class CoinFlyFX : MonoBehaviour
    {
        public static CoinFlyFX S;

        [Header("Refs")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform coinTarget;
        [SerializeField] private RectTransform coinPrefab;
        [SerializeField] private TextMeshProUGUI popupPrefab;

        [Header("Tuning")]
        [SerializeField] private int maxFlyCount = 6;
        [SerializeField] private float duration = 0.6f;
        [SerializeField] private float stagger = 0.05f;
        [SerializeField] private Vector2 spawnJitter = new Vector2(24f, 24f);
        [SerializeField] private float targetJitter = 6f;
        [SerializeField] private float scalePunch = 0.2f;
        [SerializeField] private float popupRise = 0.1f;
        [SerializeField] private float popupDuration = 0.6f;
        [SerializeField] private float popupScale = 1.15f;

        private RectTransform _canvasRect;
        private Camera _uiCam;

        private void Awake()
        {
            if (S != null && S != this)
            {
                Destroy(gameObject);
                return;
            }
            S = this;

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            if (canvas != null)
            {
                _canvasRect = canvas.GetComponent<RectTransform>();
                _uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            }
        }

        private void Start()
        {
            if (coinTarget == null && MoneyScript.S != null)
                coinTarget = MoneyScript.S.CoinTarget;
        }

        public void PlayFromWorld(Vector3 worldPos, int amount)
        {
            if (canvas == null || coinPrefab == null || coinTarget == null) return;

            Vector3 screen = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPos) : worldPos;
            PlayFromScreen(screen, amount);
        }

        public void PlayFromScreen(Vector2 screenPos, int amount)
        {
            if (canvas == null || coinPrefab == null || coinTarget == null || _canvasRect == null) return;

            int count = Mathf.Clamp(Mathf.Max(1, amount / 10), 1, maxFlyCount);
            bool popupTriggered = false;
            for (int i = 0; i < count; i++)
            {
                Action onArrive = null;
                if (!popupTriggered)
                {
                    popupTriggered = true;
                    int popupAmount = amount;
                    onArrive = () => ShowPopup(popupAmount);
                }
                SpawnOne(screenPos, i * stagger, onArrive);
            }
        }

        private void SpawnOne(Vector2 screenPos, float delay, Action onArrive)
        {
            RectTransform rt = Instantiate(coinPrefab, _canvasRect);

            Vector2 startScreen = screenPos + new Vector2(
                UnityEngine.Random.Range(-spawnJitter.x, spawnJitter.x),
                UnityEngine.Random.Range(-spawnJitter.y, spawnJitter.y));

            Vector3 startWorld;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRect, startScreen, _uiCam, out startWorld);
            rt.position = startWorld;
            rt.localScale = Vector3.one;

            Vector3 targetWorld = coinTarget.position + new Vector3(
                UnityEngine.Random.Range(-targetJitter, targetJitter),
                UnityEngine.Random.Range(-targetJitter, targetJitter),
                0f);

            CanvasGroup cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            Sequence seq = DOTween.Sequence();
            seq.SetDelay(delay);
            seq.Join(rt.DOMove(targetWorld, duration).SetEase(Ease.OutCubic));
            seq.Join(rt.DOScale(1f + scalePunch, duration * 0.5f).SetEase(Ease.OutBack));
            seq.Join(cg.DOFade(0f, duration).SetEase(Ease.InQuad));
            seq.OnComplete(() =>
            {
                onArrive?.Invoke();
                Destroy(rt.gameObject);
            });
        }

        private void ShowPopup(int amount)
        {
            if (popupPrefab == null || coinTarget == null || _canvasRect == null) return;

            TextMeshProUGUI popup = Instantiate(popupPrefab, _canvasRect);
            popup.text = $"+{amount}";
            popup.rectTransform.position = coinTarget.position;
            popup.rectTransform.localScale = Vector3.one;

            CanvasGroup cg = popup.GetComponent<CanvasGroup>();
            if (cg == null) cg = popup.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            Sequence seq = DOTween.Sequence();
            seq.Join(popup.rectTransform.DOMoveY(popup.rectTransform.position.y + popupRise, popupDuration)
                .SetEase(Ease.OutCubic));
            seq.Join(popup.rectTransform.DOScale(popupScale, popupDuration * 0.4f).SetEase(Ease.OutBack));
            seq.Join(cg.DOFade(0f, popupDuration).SetEase(Ease.InQuad));
            seq.OnComplete(() => Destroy(popup.gameObject));
        }
    }
}
