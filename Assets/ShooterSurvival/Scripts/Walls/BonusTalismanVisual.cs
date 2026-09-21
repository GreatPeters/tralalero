using TMPro;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    /// <summary>Shared artwork only; never applies a bonus or owns a collider.</summary>
    public sealed class BonusTalismanVisual : MonoBehaviour
    {
        [System.Serializable]
        public struct Emblem
        {
            public string spriteName;
            public Mesh mesh;
            public Material[] materials;
        }
        public Emblem[] emblems;
        public SpriteDatabase enhancementIcons;
        public MeshFilter emblemMesh;
        public MeshRenderer emblemRenderer;
        public SpriteRenderer readyHalo;
        public SpriteRenderer[] idleGlints;
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
            if(sprite!=null&&enhancementIcons!=null&&enhancementIcons.TryGetSprite(sprite.name,out var original))sprite=original;
            icon.sprite = sprite;
            icon.color = Color.white;
            bool sculpted = false;
            if (emblemMesh != null && sprite != null && emblems != null)
                foreach (var emblem in emblems)
                    if (emblem.spriteName == sprite.name && emblem.mesh != null)
                    {
                        emblemMesh.sharedMesh = emblem.mesh;
                        emblemRenderer.sharedMaterials = emblem.materials;
                        emblemMesh.transform.localScale = Vector3.one * (1.08f / Mathf.Max(emblem.mesh.bounds.size.x, emblem.mesh.bounds.size.y));
                        sculpted = true;
                        break;
                    }
            if (emblemRenderer != null) emblemRenderer.enabled = sculpted;
            icon.enabled = !sculpted;
            if (sprite != null)
            {
                float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                icon.transform.localScale = sculpted ? Vector3.one : Vector3.one * (1.02f / Mathf.Max(.01f, size));
            }
            int split=text.LastIndexOf(' ');
            label.text=split>0?text.Substring(0,split)+" <color=#"+ColorUtility.ToHtmlStringRGB(accent)+">"+text.Substring(split+1)+"</color>":text;
            label.color = Color.white;
            var badge=labelRoot.transform.Find("Badge");
            var captionMaterial=Resources.Load<Material>("BonusTalisman/Polished/CaptionNavy");
            if(badge!=null&&captionMaterial!=null)badge.GetComponent<Renderer>().sharedMaterial=captionMaterial;
        }

        public void SetOpen(float amount)
        {
            leftFold.localRotation = Quaternion.Euler(0, Mathf.Lerp(58, -8, amount), 0);
            rightFold.localRotation = Quaternion.Euler(0, Mathf.Lerp(-58, 8, amount), 0);
        }

        private void LateUpdate()
        {
            if (IsPickup) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera != null) transform.rotation = viewCamera.transform.rotation * Quaternion.Euler(0,-7,0);
            if(readyHalo!=null)readyHalo.color=new Color(1,.79f,.38f,.30f+.05f*Mathf.Sin(Time.time*2.8f));
            if(idleGlints!=null)
                for(int i=0;i<idleGlints.Length;i++)
                {
                    bool near=viewCamera!=null&&(viewCamera.transform.position-transform.position).sqrMagnitude<900;
                    idleGlints[i].enabled=near;
                    float pulse=.5f+.5f*Mathf.Sin(Time.time*3.2f+i*1.7f);
                    idleGlints[i].color=new Color(1,.84f,.45f,.25f+pulse*.65f);
                    idleGlints[i].transform.localScale=Vector3.one*(.07f+pulse*.065f);
                }
            Vector3 scale = transform.lossyScale;
            transform.localScale = Vector3.Scale(transform.localScale, new Vector3(
                1 / Mathf.Max(.001f, Mathf.Abs(scale.x)), 1 / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1 / Mathf.Max(.001f, Mathf.Abs(scale.z))));
            Vector3 bob = Vector3.up * (Mathf.Sin(Time.time * 2.3f) * .05f);
            transform.localPosition = restPosition + (transform.parent != null ? transform.parent.InverseTransformVector(bob) : bob);
        }
    }
}
