using UnityEngine;
using UnityEngine.UI;

public sealed class CosmeticPreview : MonoBehaviour
{
    public CosmeticVisualCatalog catalog;
    public RawImage display;
    private GameObject stage;
    private Transform model;
    private Camera previewCamera;
    private RenderTexture texture;
    private Material groundMaterial;
    public RenderTexture Texture => texture;

    public void Show(string skin, string shoes, string hat)
    {
        if (catalog == null || catalog.previewModel == null) return;
        EnsureStage();
        CosmeticAppearance.Apply(model, catalog, skin, shoes, hat);
        previewCamera.Render();
        if (display != null) display.texture = texture;
    }
    private void EnsureStage()
    {
        if (stage != null) return;
        stage = new GameObject("__CosmeticPreview") { hideFlags = HideFlags.HideAndDontSave };
        stage.transform.position = new Vector3(10000, 10000, 10000);
        var instance = Instantiate(catalog.previewModel, stage.transform, false);
        model = instance.transform; model.localScale = Vector3.one * catalog.previewScale;
        model.localRotation = Quaternion.identity;
        foreach (var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
        foreach (var r in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.enabled = true; r.updateWhenOffscreen = true; }
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ground.transform.SetParent(stage.transform, false); ground.layer = 31;
        ground.transform.localScale = new Vector3(3.6f, .02f, 4.6f);
        groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMaterial.color = new Color(.13f, .17f, .20f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
        var cameraObject = new GameObject("Preview Camera"); cameraObject.transform.SetParent(stage.transform, false);
        cameraObject.transform.localPosition = new Vector3(-3.7f, 2.5f, 6.3f);
        cameraObject.transform.LookAt(stage.transform.position + new Vector3(0, .8f, .5f));
        previewCamera = cameraObject.AddComponent<Camera>(); previewCamera.enabled = false;
        previewCamera.cullingMask = 1 << 31; previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear; previewCamera.fieldOfView = 34;
        previewCamera.nearClipPlane = .1f; previewCamera.farClipPlane = 20;
        previewCamera.allowHDR = false;
        texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32) { name = "Cosmetic preview" };
        texture.Create(); previewCamera.targetTexture = texture;
        var lightObject = new GameObject("Preview Fill"); lightObject.transform.SetParent(stage.transform, false);
        lightObject.transform.localPosition = new Vector3(-2, 4, 3);
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Point; light.range = 12;
        light.intensity = 3; light.cullingMask = 1 << 31; light.shadows = LightShadows.None;
    }
    private void OnDisable() => Dispose();
    private void OnDestroy() => Dispose();
    public void Dispose()
    {
        if (display != null) display.texture = null;
        if (texture != null) texture.Release();
        Remove(stage); Remove(texture); Remove(groundMaterial);
        stage = null; texture = null; groundMaterial = null;
    }
    private static void Remove(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
    }
}
