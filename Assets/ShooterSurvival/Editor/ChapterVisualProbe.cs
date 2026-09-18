using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Camera render for a resident batch Editor, where no desktop backbuffer is visible.
public static class ChapterVisualProbe
{
    public static object Capture(string path, int width = 576, int height = 1024)
    {
        var camera = Camera.main;
        if (camera == null) throw new InvalidOperationException("Main camera missing.");
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(c => c.gameObject.scene == camera.gameObject.scene && c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            .Select(c => (canvas: c, mode: c.renderMode, camera: c.worldCamera, distance: c.planeDistance)).ToArray();
        var target = new RenderTexture(width, height, 24);
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active; float previousAspect = camera.aspect;
        try
        {
            camera.targetTexture = target; camera.aspect = width / (float)height;
            foreach (var entry in canvases)
            {
                entry.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                entry.canvas.worldCamera = camera; entry.canvas.planeDistance = camera.nearClipPlane + 1;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render(); camera.Render();
            RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, image.EncodeToPNG());
            return new { path, width, height, source = "camera with temporary UI projection", playing = EditorApplication.isPlaying };
        }
        finally
        {
            foreach (var entry in canvases)
            {
                entry.canvas.renderMode = entry.mode; entry.canvas.worldCamera = entry.camera; entry.canvas.planeDistance = entry.distance;
            }
            camera.targetTexture = previousTarget; camera.aspect = previousAspect; RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
