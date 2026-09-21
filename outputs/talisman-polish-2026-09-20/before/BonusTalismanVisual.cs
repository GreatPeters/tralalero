using TMPro;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    /// <summary>Shared artwork only; never applies a bonus or owns a collider.</summary>
    public sealed class BonusTalismanVisual : MonoBehaviour
    {
        public Transform paper, leftFold, rightFold;
        public SpriteRenderer icon;
        public TMP_Text label;
        public GameObject labelRoot;
        public bool IsPickup { get; set; }
        private Vector3 restPosition;
        private Camera viewCamera;

        private void OnEnable() { restPosition = transform.localPosition; }
        public void RememberRestPose() { restPosition = transform.localPosition; }

        public void SetContent(Sprite sprite, string text, Color accent)
        {
            icon.sprite = sprite;
            icon.color = Color.white;
            if (sprite != null)
            {
                float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                icon.transform.localScale = Vector3.one * (.83f / Mathf.Max(.01f, size));
            }
            label.text = text;
            label.color = accent;
        }

        public void SetOpen(float amount)
        {
            leftFold.localRotation = Quaternion.Euler(0, Mathf.Lerp(-62, -12, amount), 0);
            rightFold.localRotation = Quaternion.Euler(0, Mathf.Lerp(62, 12, amount), 0);
        }

        private void LateUpdate()
        {
            if (IsPickup) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera != null) transform.rotation = viewCamera.transform.rotation;
            Vector3 scale = transform.lossyScale;
            transform.localScale = Vector3.Scale(transform.localScale, new Vector3(
                1 / Mathf.Max(.001f, Mathf.Abs(scale.x)), 1 / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1 / Mathf.Max(.001f, Mathf.Abs(scale.z))));
            Vector3 bob = Vector3.up * (Mathf.Sin(Time.time * 2.3f) * .05f);
            transform.localPosition = restPosition + (transform.parent != null ? transform.parent.InverseTransformVector(bob) : bob);
        }
    }
}
