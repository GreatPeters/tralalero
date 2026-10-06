using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

public static class PreviewAccountSave
{
    public static object Main(string output, int width, int height)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        var source = UnityEngine.Object.FindFirstObjectByType<HarborSettingsPanel>(FindObjectsInactive.Include);
        if (source == null) throw new Exception("Settings panel not found");
        var original = SceneManager.GetActiveScene();
        bool originalDirty = original.isDirty;
        var preview = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(width, height, 24);
        var previous = RenderTexture.active;
        object result;
        try
        {
            var cameraObject = new GameObject("Settings preview camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .07f, .12f);
            camera.orthographic = true; camera.orthographicSize = 5;
            camera.nearClipPlane = .1f; camera.farClipPlane = 10;
            camera.targetTexture = target;
            var canvasObject = new GameObject("Settings preview canvas", typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(canvasObject, preview);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            var clone = UnityEngine.Object.Instantiate(source, canvas.transform, false);
            clone.gameObject.SetActive(true);
            var rect = clone.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            clone.runActions.SetActive(false);
            var panel = clone.transform.Find("Panel");
            var fit = panel.GetComponent<AspectRatioFitter>(); if (fit != null) fit.aspectRatio = .72f;
            var account = clone.GetComponent<HarborAccountPanel>();
            typeof(HarborAccountPanel).GetMethod("EnsureSaveButton", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(account, null);
            account.dialog.SetActive(false);
            account.status.text = "기기 저장 완료 12:34\n로그인하면 다른 기기에서도 진행을 이어갈 수 있습니다.";
            account.loginButton.GetComponentInChildren<TMP_Text>().text = "Google Play 로그인";
            account.deleteButton.interactable = false;
            Canvas.ForceUpdateCanvases();
            foreach (var text in clone.GetComponentsInChildren<TMP_Text>(true)) text.ForceMeshUpdate(true);
            camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllBytes(output, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            result = new { output, width, height, buttons = new[] { account.saveButton.name, account.loginButton.name, account.deleteButton.name },
                overflow = account.status.isTextOverflowing,
                note = "Isolated native preview of the actual Settings clone and production button layout. Sample save status; no login, gameplay, preference write or real cloud save performed." };
        }
        finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); EditorSceneManager.ClosePreviewScene(preview); }
        if (original.isDirty != originalDirty) throw new Exception("Original scene dirty state changed");
        return result;
    }
}
