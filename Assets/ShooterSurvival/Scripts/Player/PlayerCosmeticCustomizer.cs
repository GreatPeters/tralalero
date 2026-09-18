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
        if (model == null || catalog == null || catalog.splitSharkMesh == null) return;
        int sourceCount = catalog.sourceSharkMesh != null ? catalog.sourceSharkMesh.vertexCount : catalog.splitSharkMesh.vertexCount;
        var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.sharedMesh != null &&
            (r.sharedMesh == catalog.splitSharkMesh || r.sharedMesh == catalog.bodyOnlyMesh ||
             r.sharedMesh == catalog.sourceSharkMesh || r.sharedMesh.vertexCount == sourceCount ||
             catalog.entries.Any(e=>e.fittedBodyMesh!=null&&e.fittedBodyMesh==r.sharedMesh)));
        if (renderer == null) return;
        var skinItem = catalog.Find(skin); var shoeItem = catalog.Find(shoes);
        if (skinItem == null || shoeItem == null) return;
        bool replacement = shoeItem.replacesBaseShoes && shoeItem.fittedShoeMesh != null && catalog.bodyOnlyMesh != null &&
            catalog.footMounts != null && catalog.footMounts.Length > 0 &&
            catalog.footMounts.All(m=>renderer.bones.Any(b=>b!=null && b.name==m.bone));
        renderer.sharedMesh = replacement ? shoeItem.fittedBodyMesh!=null?shoeItem.fittedBodyMesh:catalog.bodyOnlyMesh : catalog.splitSharkMesh;
        renderer.quality=SkinQuality.Bone4;
        renderer.sharedMaterials = replacement ? new[] { skinItem.material } : new[] { skinItem.material, shoeItem.material };
        ApplyEquipmentAttachment(model, renderer, skinItem, "__CosmeticSkin");
        if (replacement) ApplyReplacementShoes(model, renderer, catalog, shoeItem);
        else ApplyEquipmentAttachment(model, renderer, shoeItem, "__CosmeticShoes");
        // Destroy is deferred in Play: a second equip in this frame must also hide the latest mount.
        foreach (var old in model.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name == "__CosmeticHat").ToArray())
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
        accessory.transform.localPosition=item.hatOffset;
        accessory.transform.localRotation=Quaternion.Euler(item.hatEuler);
        accessory.transform.localScale=Vector3.one*item.hatScale;
        foreach (var t in accessory.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = model.gameObject.layer;
    }

    private static void ApplyEquipmentAttachment(Transform model, SkinnedMeshRenderer renderer, CosmeticVisualCatalog.Entry item, string mountName)
    {
        foreach (var old in model.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name == mountName).ToArray())
        {
            old.gameObject.SetActive(false);
            if (Application.isPlaying) Object.Destroy(old.gameObject); else Object.DestroyImmediate(old.gameObject);
        }
        if (item?.accessory == null) return;
        var anchors = item.accessoryAnchor == "feet"
            ? renderer.bones.Where(b => b.name.EndsWith("leg2")).ToArray()
            : renderer.bones.Where(b => b.name == item.accessoryAnchor).ToArray();
        foreach (var bone in anchors)
        {
            var mount = new GameObject(mountName).transform; mount.SetParent(bone, false);
            mount.position = bone.position + model.rotation * item.accessoryOffset;
            mount.rotation = model.rotation;
            Vector3 s = bone.lossyScale;
            mount.localScale = new Vector3(item.accessoryWorldScale / Mathf.Abs(s.x), item.accessoryWorldScale / Mathf.Abs(s.y), item.accessoryWorldScale / Mathf.Abs(s.z));
            var accessory = Object.Instantiate(item.accessory, mount, false);
            foreach (var t in accessory.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = model.gameObject.layer;
        }
    }

    private static void ApplyReplacementShoes(Transform model, SkinnedMeshRenderer renderer, CosmeticVisualCatalog catalog, CosmeticVisualCatalog.Entry item)
    {
        foreach (var old in model.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name == "__CosmeticShoes").ToArray())
        {
            old.gameObject.SetActive(false);
            if (Application.isPlaying) Object.Destroy(old.gameObject); else Object.DestroyImmediate(old.gameObject);
        }
        var mount=new GameObject("__CosmeticShoes");mount.layer=model.gameObject.layer;
        mount.transform.SetParent(renderer.transform,false);
        var shoes=mount.AddComponent<SkinnedMeshRenderer>();shoes.sharedMesh=item.fittedShoeMesh;
        // Fitted vertices were inverse-skinned using all source influences.
        // Project defaults allow only two, which visibly twists the rear shoes.
        shoes.quality=SkinQuality.Bone4;
        shoes.bones=renderer.bones;shoes.rootBone=renderer.rootBone;shoes.sharedMaterial=item.material;
        shoes.updateWhenOffscreen=renderer.updateWhenOffscreen;shoes.localBounds=renderer.localBounds;
    }
}
