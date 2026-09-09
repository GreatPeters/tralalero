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
    }
    public Mesh splitSharkMesh;
    public GameObject previewModel;
    public float previewScale = 450;
    public Vector3 hatLocalPosition;
    public Quaternion hatLocalRotation = Quaternion.identity;
    public Vector3 hatLocalScale = Vector3.one;
    public Entry[] entries;
    public Entry Find(string key) => Array.Find(entries, e => e.key == key);
}
