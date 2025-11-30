using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IndianOceanAssets.ShooterSurvival
{
    public class EffectOverlayScript : MonoBehaviour
    {
        [Header("Vignette Settings")]

        [Tooltip("Reference to the global post-processing volume in the scene")]
        private Volume globalVolume;

        private Vignette vignette;

        private Color defaultColor = Color.black;
        private float defaultIntensity = 0.4f;
        private float defaultSmoothness = 0.4f;

        private Color _buffColor = Color.green;      // green overlay for health boost
        private Color _nerfColor = Color.red;       // red overlay for health loss
        private float _intensity = 0.5f;
        private float _smoothness = 0.25f;

        [Tooltip("Duration the effect stays before it starts fading")]
        [SerializeField] private float effectOverlayDuration;

        [Tooltip("Duration of transition back to default settings")]
        [SerializeField] private float transitionDuration;

        private void Start()
        {
            globalVolume = GetComponent<Volume>();
            globalVolume.profile.TryGet(out vignette);
        }

        private IEnumerator ApplyVignetteEffect(Color effectColor)
        {
            // Applies a vignette effect with color and transitions it back to default
            if (vignette != null)
            {
                vignette.color.Override(effectColor);
                vignette.intensity.Override(_intensity);
                vignette.smoothness.Override(_smoothness);
            }

            yield return new WaitForSeconds(effectOverlayDuration);

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;

                // Gradually return vignette settings to default
                vignette.intensity.Override(Mathf.Lerp(_intensity, defaultIntensity, t));
                vignette.smoothness.Override(Mathf.Lerp(_smoothness, defaultSmoothness, t));
                vignette.color.Override(Color.Lerp(effectColor, defaultColor, t));

                yield return null;
            }
        }

        #region "Public Methods"

        public void BuffOverlay()
        {
            // Green Vignette effect
            StopAllCoroutines();
            StartCoroutine(ApplyVignetteEffect(_buffColor));
        }

        public void NerfOverlay()
        {
            // Red Vignetter effect
            StopAllCoroutines();
            StartCoroutine(ApplyVignetteEffect(_nerfColor));
        }

        #endregion
    }
}