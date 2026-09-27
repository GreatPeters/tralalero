using System.Linq;
using UnityEditor;
using UnityEngine;

// Renders Meshy character prefabs beside a Noryangjin enemy in a preview scene (no scene edits).
public static class PreviewMeshyCharacters
{
    public static object Main(string idList, string outPath)
    {
        var ids = idList.Split(',');
        var ps = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        try
        {
            var light = new GameObject("L").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f;
            light.color = new Color(1, .94f, .83f); light.transform.rotation = Quaternion.Euler(48, -32, 0);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject, ps);
            var entries = new System.Collections.Generic.List<(GameObject go, string clip)>();
            var nry = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_FatMan.prefab"), ps);
            entries.Add((nry, null));
            foreach (var id in ids)
                foreach (var clip in new[] { "idle", "walk", "attack_once", "die" })
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ImportPath(id)), ps);
                    entries.Add((go, clip));
                }
            float x = 0;
            foreach (var (go, clip) in entries)
            {
                go.transform.SetPositionAndRotation(new Vector3(x, 0, 0), Quaternion.Euler(0, 180, 0));
                x += 2.6f;
                if (clip == null) continue;
                var anim = go.GetComponentInChildren<Animator>();
                var c = anim.runtimeAnimatorController.animationClips.FirstOrDefault(a => a.name == clip);
                if (c != null) c.SampleAnimation(anim.gameObject, c.length * (clip == "die" ? .95f : .45f));
            }
            var cam = new GameObject("C").AddComponent<Camera>(); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cam.gameObject, ps);
            cam.scene = ps; cam.orthographic = true; cam.orthographicSize = 1.9f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.78f, .82f, .8f);
            float mid = (x - 2.6f) / 2;
            cam.transform.SetPositionAndRotation(new Vector3(mid, 3.2f, -12), Quaternion.Euler(12, 0, 0));
            int w = Mathf.RoundToInt(260 * entries.Count), h = 460;
            cam.aspect = (float)w / h; cam.orthographicSize = Mathf.Max(1.6f, x / cam.aspect / 2);
            var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());
            RenderTexture.active = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
            return "ok " + entries.Count;
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(ps); }
    }
    static string ImportPath(string id) => "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/" + id + ".prefab";
}
