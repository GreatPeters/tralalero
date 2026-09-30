using UnityEditor;
using UnityEngine;

// Renders Meshy object prefabs from the +Z side (their intended front) in a preview scene.
public static class PreviewMeshyObjects
{
    public static object Noryangjin0928() => Main("N03_forklift,N04_livefish_truck,N05_harbor_crane,N06_red_lighthouse,N07_market_cat,N08_frozen_tuna", "tmp/image-previews/noryangjin-revamp-fix-2026-09-28/meshy-native-six.png");
    public static object Main(string idList, string outPath)
    {
        var ids = idList.Split(',');
        var ps = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        int w = 360, h = 300;
        var sheet = new Texture2D(w * ids.Length, h, TextureFormat.RGB24, false);
        try
        {
            var light = new GameObject("L").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f; light.transform.rotation = Quaternion.Euler(45, 150, 0);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject, ps);
            for (int i = 0; i < ids.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/" + ids[i] + ".prefab"), ps);
                var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var cam = new GameObject("C").AddComponent<Camera>(); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cam.gameObject, ps);
                cam.scene = ps; cam.fieldOfView = 30; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.85f, .88f, .86f);
                var dir = Quaternion.Euler(20, 215, 0) * Vector3.forward; // camera sits at +Z/+X looking back toward -Z
                cam.transform.position = b.center - dir * b.extents.magnitude * 3.6f; cam.transform.LookAt(b.center);
                var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.aspect = (float)w / h; cam.Render();
                RenderTexture.active = rt; sheet.ReadPixels(new Rect(0, 0, w, h), i * w, 0);
                RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(go);
            }
            sheet.Apply(); System.IO.File.WriteAllBytes(outPath, sheet.EncodeToPNG());
            return "ok";
        }
        finally { Object.DestroyImmediate(sheet); UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(ps); }
    }
}
