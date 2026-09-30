using System.Collections.Generic;
using System.IO;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Edit-mode previews of the revamp set pieces from the gameplay camera offset.
//   unity command run_script --file tools/capture-noryangjin-revamp.cs --entry CaptureNoryangjinRevamp.Main
public static class CaptureNoryangjinRevamp
{
    const string Target = "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18_Revamp.unity";
    public static object Main()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != Target) scene = EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
        var player = Object.FindFirstObjectByType<PlayerScript>().transform;
        var cam = GameObject.Find("MapTool_Camera").GetComponent<Camera>();
        Vector3 localOffset = Quaternion.Inverse(player.rotation) * (cam.transform.position - player.position);
        Quaternion localRot = Quaternion.Inverse(player.rotation) * cam.transform.rotation;
        var spots = new List<(string name, Vector3 at, Vector3 forward)>
        {
            ("A_barricade", new Vector3(147, 0, -7.25f), Vector3.right),
            ("E0_hall_entrance", new Vector3(124.3f, 0, 62), Vector3.back),
            ("E1_rush", new Vector3(124.3f, 0, 10), Vector3.back),
            ("H_hose", new Vector3(124.3f, 0, -95), Vector3.back),
            ("F_turret", new Vector3(124.3f, 0, -175), Vector3.back),
            ("G_walls", new Vector3(124.3f, 0, -245), Vector3.back),
            ("G_shutter", new Vector3(124.3f, 0, -318), Vector3.back),
            ("C_wave", new Vector3(112, 0, -348.1f), Vector3.left),
            ("Gate_1", new Vector3(225.5f, 0, 20), Vector3.forward),
            ("D_auction", new Vector3(376, 0, 203.5f), Vector3.right),
            ("I_boss", new Vector3(248, 0, 262), Vector3.forward),
        };
        string dir = "tmp/image-previews/noryangjin-revamp-2026-09-28";
        Directory.CreateDirectory(dir);
        var rt = new RenderTexture(540, 1170, 24);
        var prev = cam.targetTexture;
        var pos = cam.transform.position; var rot = cam.transform.rotation;
        var shots = new List<string>();
        foreach (var s in spots)
        {
            var frame = Quaternion.LookRotation(s.forward);
            cam.transform.SetPositionAndRotation(s.at + frame * localOffset, frame * localRot);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            string file = $"{dir}/edit-{s.name}.png";
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            shots.Add(file);
        }
        cam.targetTexture = prev; RenderTexture.active = null;
        cam.transform.SetPositionAndRotation(pos, rot);
        Object.DestroyImmediate(rt);
        return new { offset = localOffset, shots };
    }
}
