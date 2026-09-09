using System.Linq;
using UnityEngine;

public sealed class PlayerCosmeticCustomizer : MonoBehaviour
{
    public Transform modelRoot;
    public CosmeticVisualCatalog visuals;
    private void OnEnable() { if (Application.isPlaying) CosmeticService.Changed += RefreshAppearance; }
    private void OnDisable() => CosmeticService.Changed -= RefreshAppearance;
    private void Start() => RefreshAppearance();
    public void RefreshAppearance()
    {
        if (!Application.isPlaying) return;
        var inventory = CosmeticService.Current;
        if (inventory == null || modelRoot == null || visuals == null) return;
        CosmeticAppearance.Apply(modelRoot, visuals, inventory.Equipped(CosmeticSlot.Skin).visualKey,
            inventory.Equipped(CosmeticSlot.Shoes).visualKey, inventory.Equipped(CosmeticSlot.Hat).visualKey);
    }
}

public static class CosmeticAppearance
{
    public static void Apply(Transform model, CosmeticVisualCatalog catalog, string skin, string shoes, string hat)
    {
        var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.sharedMesh != null && r.sharedMesh.vertexCount == catalog.splitSharkMesh.vertexCount);
        if (renderer == null) return;
        renderer.sharedMesh = catalog.splitSharkMesh;
        renderer.sharedMaterials = new[] { catalog.Find(skin).material, catalog.Find(shoes).material };
        // Destroy is deferred in Play: a second equip in this frame must also hide the latest mount.
        foreach (var old in model.GetComponentsInChildren<Transform>(true).Where(t => t.name == "__CosmeticHat"))
        {
            old.gameObject.SetActive(false);
            if (Application.isPlaying) Object.Destroy(old.gameObject); else Object.DestroyImmediate(old.gameObject);
        }
        var item = catalog.Find(hat);
        if (item?.accessory == null) return;
        var head = renderer.bones.FirstOrDefault(b => b.name == "head");
        if (head == null) return;
        var mount = new GameObject("__CosmeticHat"); mount.transform.SetParent(head, false);
        mount.transform.localPosition = catalog.hatLocalPosition;
        mount.transform.localRotation = catalog.hatLocalRotation;
        mount.transform.localScale = catalog.hatLocalScale;
        var accessory = Object.Instantiate(item.accessory, mount.transform, false);
        foreach (var t in accessory.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = model.gameObject.layer;
    }
}
