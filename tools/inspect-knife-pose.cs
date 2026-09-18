using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class InspectKnifePose
{
    public static object Main(string folder, bool candidate = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        Directory.CreateDirectory(folder);
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab");
        var report = new System.Text.StringBuilder();
        foreach (string state in new[] { "idle", "walk", "attack_loop", "die" })
        {
            var preview = new PreviewRenderUtility(); Texture2D picture = null;
            AnimatorOverrideController candidateController = null; AnimationClip candidateClip = null;
            try
            {
                var root = Object.Instantiate(asset); preview.AddSingleGO(root); root.SetActive(true);
                root.transform.position = Vector3.zero; root.transform.rotation = Quaternion.identity;
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.gameObject.SetActive(false);
                var animator = root.GetComponentInChildren<Animator>(true); animator.enabled = true;
                if (candidate)
                {
                    var original = (AnimatorOverrideController)animator.runtimeAnimatorController;
                    candidateClip = Object.Instantiate(original["ForwardEnemy_Idle"]);
                    int[] indices = { 39, 40, 41, 42, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54 };
                    float[] values = { -.65f, .05f, 0, .7f, 0, 0, 0, 0, -.4f, .2f, -.15f, .15f, .5f, .1f, -.1f };
                    for (int i=0; i<indices.Length; i++)
                        AnimationUtility.SetEditorCurve(candidateClip, EditorCurveBinding.FloatCurve("", typeof(Animator), HumanTrait.MuscleName[indices[i]]), AnimationCurve.Constant(0, candidateClip.length, values[i]));
                    candidateController = Object.Instantiate(original); candidateController["ForwardEnemy_Idle"] = candidateClip;
                    animator.runtimeAnimatorController = candidateController;
                }
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.Rebind();
                animator.Play(state, 0, .3f); animator.SetLayerWeight(1, state == "walk" ? 1 : 0); animator.Update(0);
                using (var handler = new HumanPoseHandler(animator.avatar, animator.transform))
                {
                    var pose = new HumanPose(); handler.GetHumanPose(ref pose);
                    report.AppendLine("muscles " + string.Join(",", Enumerable.Range(39, 16).Select(i => HumanTrait.MuscleName[i] + "=" + pose.muscles[i])));
                }
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var weapon = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Chef_s_Steel_0811163218_texture");
                var bounds = new Bounds(root.transform.position + Vector3.up, Vector3.one * .01f);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy)) bounds.Encapsulate(renderer.bounds);
                report.AppendLine(state + " bounds=" + bounds + " hand=" + hand.position + " sword=" + weapon.position);
                foreach (var filter in weapon.GetComponentsInChildren<MeshFilter>(true))
                    report.AppendLine(" mesh " + AssetDatabase.GetAssetPath(filter.sharedMesh) + " bounds=" + filter.sharedMesh.bounds + " transform=" + filter.transform.localToWorldMatrix);
                preview.camera.cameraType = CameraType.Game; preview.camera.orthographic = true;
                preview.camera.orthographicSize = bounds.size.y * .62f;
                preview.camera.transform.position = bounds.center + new Vector3(1.5f, .6f, -6f);
                preview.camera.transform.LookAt(bounds.center);
                preview.camera.nearClipPlane = .1f; preview.camera.farClipPlane = 100;
                preview.camera.clearFlags = CameraClearFlags.SolidColor; preview.camera.backgroundColor = new Color(.79f, .87f, .86f);
                preview.lights[0].intensity = 1.2f; preview.lights[0].transform.rotation = Quaternion.Euler(40, 130, 0);
                preview.lights[1].intensity = .8f; preview.ambientColor = new Color(.6f, .6f, .6f);
                preview.BeginStaticPreview(new Rect(0, 0, 900, 1000)); preview.Render(true);
                picture = preview.EndStaticPreview(); File.WriteAllBytes(Path.Combine(folder, state + ".png"), picture.EncodeToPNG());
            }
            finally { if (picture != null) Object.DestroyImmediate(picture); preview.Cleanup(); if(candidateController!=null)Object.DestroyImmediate(candidateController); if(candidateClip!=null)Object.DestroyImmediate(candidateClip); }
        }
        File.WriteAllText(Path.Combine(folder, "metrics.txt"), report.ToString());
        return new { folder, poses = 4 };
    }
}
