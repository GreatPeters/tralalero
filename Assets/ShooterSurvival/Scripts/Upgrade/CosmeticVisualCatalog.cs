using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Shooter Survival/Cosmetic Visual Catalog")]
public sealed class CosmeticVisualCatalog : ScriptableObject
{
    [Serializable] public sealed class Entry
    {
        public string key;
        public Material material;
        public GameObject accessory;
        public Sprite icon;
        public Color swatch = Color.white;
        public string accessoryAnchor;
        public Vector3 accessoryOffset;
        public float accessoryWorldScale = 1f;
        public bool replacesBaseShoes;
        public Mesh fittedShoeMesh;
        public Mesh fittedBodyMesh;
        public Vector3 hatOffset;
        public Vector3 hatEuler;
        public float hatScale=1f;
    }
    [Serializable] public sealed class FootMount
    {
        public string bone;
        public Vector3 localPosition;
        public Quaternion localRotation = Quaternion.identity;
        public Vector3 localScale = Vector3.one;
    }
    public Mesh sourceSharkMesh;
    public Mesh splitSharkMesh;
    public Mesh bodyOnlyMesh;
    public FootMount[] footMounts;
    public GameObject previewModel;
    public float previewScale = 450;
    public Vector3 hatLocalPosition;
    public Quaternion hatLocalRotation = Quaternion.identity;
    public Vector3 hatLocalScale = Vector3.one;
    public Entry[] entries;
    public Entry Find(string key) => Array.Find(entries, e => e.key == key);
}
