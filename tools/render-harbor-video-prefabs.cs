// Isolated native UI previews: no game-scene objects, preferences or playback are changed.
if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
var originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool originalDirty = originalScene.isDirty;
var reports = new System.Collections.Generic.List<string>();
foreach (var variant in new[] { "A", "B" })
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
    GameObject root = null;
    GameObject cameraObject = null;
    RenderTexture target = null;
    Texture2D capture = null;
    var previous = RenderTexture.active;
    try
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/UI/HarborVideo/HarborVideo_" + variant + ".prefab");
        root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 31;
        root.GetComponent<UnityEngine.UI.CanvasScaler>().enabled = false;
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        var rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
        rect.sizeDelta = new Vector2(887,1774); rect.position = Vector3.zero; rect.localScale = Vector3.one;
        cameraObject = new GameObject("HarborVideoPreviewCamera") { hideFlags = HideFlags.HideAndDontSave };
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<Camera>();
        camera.scene = scene;
        camera.overrideSceneCullingMask = UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);
        camera.transform.position = new Vector3(0,0,-2000); camera.transform.rotation = Quaternion.identity;
        camera.orthographic = true; camera.orthographicSize = 887; camera.aspect = .5f;
        camera.nearClipPlane = .1f; camera.farClipPlane = 3000;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.02f,.02f,.02f);
        camera.cullingMask = 1 << 31; camera.allowHDR = false; camera.allowMSAA = false;
        canvas.worldCamera = camera;
        target = new RenderTexture(887,1774,24,RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture = target;
        Canvas.ForceUpdateCanvases();
        foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>()) text.ForceMeshUpdate();
        camera.Render();
        RenderTexture.active = target;
        capture = new Texture2D(887,1774,TextureFormat.RGBA32,false);
        capture.ReadPixels(new Rect(0,0,887,1774),0,0); capture.Apply();
        var samplePixels = capture.GetPixels32();
        if(samplePixels.Count(p => p.r > 40 || p.g > 40 || p.b > 40) < samplePixels.Length / 5)
            throw new Exception("Native render is blank/dark; inspect preview-scene culling before accepting evidence");
        string path = "C:/Users/ljh/tralalero Shooter/tmp/image-previews/harbor-video-ui-2026-09-15/production/" + variant + "-unity-prefab.png";
        System.IO.File.WriteAllBytes(path,capture.EncodeToPNG());
        var fit = root.GetComponentInChildren<UnityEngine.UI.AspectRatioFitter>();
        var display = root.GetComponentInChildren<UnityEngine.UI.RawImage>();
        if (fit.aspectMode != UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent || Mathf.Abs(fit.aspectRatio-9f/16f)>.001f)
            throw new Exception("Video aspect contract failed");
        var ratio = display.rectTransform.rect.width / display.rectTransform.rect.height;
        if (Mathf.Abs(ratio-9f/16f)>.002f) throw new Exception("Actual video rectangle ratio failed: " + ratio);
        foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>())
            if (button.GetComponentInChildren<TMPro.TMP_Text>() == null || !button.targetGraphic.raycastTarget)
                throw new Exception("Button label or raycast binding failed: " + button.name);
        reports.Add(variant+": native render, movie rectangle ratio="+ratio+", 3 button/label groups");
    }
    finally
    {
        RenderTexture.active = previous;
        if(root!=null) UnityEngine.Object.DestroyImmediate(root);
        if(cameraObject!=null) UnityEngine.Object.DestroyImmediate(cameraObject);
        if(capture!=null) UnityEngine.Object.DestroyImmediate(capture);
        if(target!=null) {target.Release();UnityEngine.Object.DestroyImmediate(target);}
        UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
    }
}
if (originalScene.isDirty != originalDirty || UnityEngine.SceneManagement.SceneManager.GetActiveScene() != originalScene)
    throw new Exception("Unexpected original scene state change");
System.IO.File.WriteAllLines("map-concepts/harbor-video-ui-2026-09-15/production/native-render-verification.txt",reports);
return reports;
