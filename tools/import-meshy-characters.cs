using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

// Imports Meshy rigged characters and their per-action FBX files as one prefab with the
// shared enemy animation contract (idle/walk/run/attack_once/attack_loop/die/hit).
// Materials copy the Noryangjin enemy outline material so every Meshy character keeps the same
// FlatKit outline treatment. Run: unity command run_script --file tools/import-meshy-characters.cs
//   --entry ImportMeshyCharacters.Main --args '["outputs/meshy-reststop-2026-09-25","C01_patrol_police"]'
public static class ImportMeshyCharacters
{
    public const string ModelRoot = "Assets/ShooterSurvival/Models/MeshyRestStop20260925";
    public const string PrefabRoot = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925";
    const string OutlineReference = "Assets/JH/Model/Enemy/Garden_Spear/Material.001.mat";
    // Bind (A-pose) height. With Meshy's per-clip scale keys removed, actions keep this size, matching
    // Noryangjin enemies (2.25-2.5) and staying well below the 3.4-unit shark.
    public const float CharacterHeight = 2.3f;
    static readonly Dictionary<string, float> HeightOverride = new() { { "A03_kid", 1.5f }, { "A06_grandma", 2.1f }, { "C05_truck_driver", 2.45f } };

    static readonly Dictionary<string, string> FileToState = new()
    {
        { "anim_idle", "idle" }, { "anim_walking", "walk" }, { "anim_running", "run" },
        { "anim_attack_once", "attack_once" }, { "anim_hit", "hit" }, { "anim_die", "die" },
        { "anim_scared", "scared" }, { "anim_wave", "signal" }, { "anim_cheer", "greet" },
    };
    static readonly HashSet<string> Looping = new() { "idle", "walk", "run", "attack_loop", "scared", "signal", "greet" };

    public static object Main(string runDir, string idList)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        var ids = idList.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
        Directory.CreateDirectory(ModelRoot); Directory.CreateDirectory(PrefabRoot);
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var id in ids)
            {
                string src = Path.Combine(runDir, "models", id), dst = ModelRoot + "/" + id;
                Directory.CreateDirectory(dst);
                File.Copy(Path.Combine(src, "rigged.fbx"), dst + "/" + id + ".fbx", true);
                foreach (var file in Directory.GetFiles(src, "anim_*.fbx"))
                {
                    var key = Path.GetFileNameWithoutExtension(file);
                    if (FileToState.TryGetValue(key, out var state)) File.Copy(file, dst + "/" + id + "@" + state + ".fbx", true);
                }
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var reports = new List<object>();
        foreach (var id in ids) reports.Add(ImportOne(id));
        AssetDatabase.SaveAssets();
        return reports;
    }

    static object ImportOne(string id)
    {
        string folder = ModelRoot + "/" + id, body = folder + "/" + id + ".fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(body);
        Configure(importer, null);
        importer.SaveAndReimport();
        // Meshy embeds the base color texture; extract once so the material can reference it.
        if (!Directory.GetFiles(folder, "*.png").Any() && !Directory.GetFiles(folder, "*.jpg").Any())
        {
            importer.ExtractTextures(folder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        var avatar = AssetDatabase.LoadAllAssetsAtPath(body).OfType<Avatar>().First();
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var file in Directory.GetFiles(folder, id + "@*.fbx"))
        {
            string path = file.Replace('\\', '/'), state = Path.GetFileNameWithoutExtension(path).Split('@')[1];
            var ai = (ModelImporter)AssetImporter.GetAtPath(path);
            Configure(ai, avatar);
            var take = ai.defaultClipAnimations.First();
            var list = new List<ModelImporterClipAnimation> { Clip(take, state) };
            if (state == "attack_once") list.Add(Clip(take, "attack_loop"));
            ai.clipAnimations = list.ToArray();
            ai.SaveAndReimport();
            // Meshy bakes per-file bone scale keys (idle is ~17% taller than walk). Copy each clip into a
            // standalone .anim without scale curves so every action keeps the bind-pose body size.
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")))
            {
                string animPath = folder + "/" + id + "_" + clip.name + ".anim";
                var copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
                if (copy == null) { copy = new AnimationClip(); AssetDatabase.CreateAsset(copy, animPath); }
                EditorUtility.CopySerialized(clip, copy);
                copy.name = clip.name;
                foreach (var binding in AnimationUtility.GetCurveBindings(copy))
                    if (binding.propertyName.StartsWith("m_LocalScale")) AnimationUtility.SetEditorCurve(copy, binding, null);
                EditorUtility.SetDirty(copy);
                clips[clip.name] = copy;
            }
        }
        // Ambient civilians never die, so only locomotion and idle are mandatory.
        foreach (var required in new[] { "idle", "walk", "run" })
            if (!clips.ContainsKey(required)) throw new Exception(id + ": missing clip " + required);
        var texture = Directory.GetFiles(folder).Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg"))
            .Select(f => f.Replace('\\', '/')).OrderByDescending(f => new FileInfo(f).Length).First();
        var ti = (TextureImporter)AssetImporter.GetAtPath(texture);
        ti.maxTextureSize = 1024; ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.sRGBTexture = true; ti.SaveAndReimport();
        var material = OutlineMaterial(folder + "/" + id + "_Outline.mat", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
        var controller = Controller(folder + "/" + id + ".controller", clips);

        var root = new GameObject("MRS_" + id);
        try
        {
            var fit = new GameObject("Fit").transform; fit.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(body), fit);
            model.name = "Model";
            int tris = 0;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
                r.shadowCastingMode = ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer skin) { skin.updateWhenOffscreen = false; tris += skin.sharedMesh.triangles.Length / 3; }
            }
            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.avatar = avatar; animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var b = Bounds(model.transform, root.transform);
            float height = HeightOverride.TryGetValue(id, out var h) ? h : CharacterHeight;
            fit.localScale = Vector3.one * (height / b.size.y);
            b = Bounds(model.transform, root.transform);
            fit.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/" + id + ".prefab");
            return new { id, tris, clips = clips.Keys.OrderBy(k => k).ToArray(), height = HeightOverride.TryGetValue(id, out var hh) ? hh : CharacterHeight, texture = Path.GetFileName(texture) };
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void Configure(ModelImporter importer, Avatar source)
    {
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = source == null ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.CopyFromOther;
        if (source != null) importer.sourceAvatar = source;
        importer.importAnimation = source != null;
        importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
        importer.materialImportMode = source == null ? ModelImporterMaterialImportMode.ImportStandard : ModelImporterMaterialImportMode.None;
        importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
        importer.isReadable = source == null;
        importer.meshCompression = ModelImporterMeshCompression.Off;
    }

    static ModelImporterClipAnimation Clip(ModelImporterClipAnimation take, string name)
    {
        return new ModelImporterClipAnimation
        {
            name = name, takeName = take.takeName, firstFrame = take.firstFrame, lastFrame = take.lastFrame,
            loopTime = Looping.Contains(name), lockRootRotation = true, lockRootHeightY = true, lockRootPositionXZ = true,
            keepOriginalOrientation = true, keepOriginalPositionY = true, keepOriginalPositionXZ = true,
        };
    }

    public static Material OutlineMaterial(string path, Texture2D baseMap)
    {
        var reference = AssetDatabase.LoadAssetAtPath<Material>(OutlineReference);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(reference); AssetDatabase.CreateAsset(mat, path); }
        else mat.CopyPropertiesFromMaterial(reference);
        mat.shader = reference.shader; mat.shaderKeywords = reference.shaderKeywords;
        mat.SetTexture("_BaseMap", baseMap);
        mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_ColorDim")) mat.SetColor("_ColorDim", new Color(.86f, .86f, .88f, 1));
        mat.enableInstancing = true; EditorUtility.SetDirty(mat);
        return mat;
    }

    static AnimatorController Controller(string path, Dictionary<string, AnimationClip> clips)
    {
        var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = c.layers[0].stateMachine;
        foreach (var s in sm.states) sm.RemoveState(s.state);
        foreach (var pair in clips.OrderBy(p => p.Key))
        {
            var state = sm.AddState(pair.Key); state.motion = pair.Value;
            if (pair.Key == "idle") sm.defaultState = state;
        }
        EditorUtility.SetDirty(c);
        return c;
    }

    static Bounds Bounds(Transform model, Transform space)
    {
        bool first = true; var b = new Bounds();
        foreach (var s in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var baked = new Mesh(); s.BakeMesh(baked, false);
            foreach (var v in baked.vertices)
            {
                var p = space.InverseTransformPoint(s.transform.TransformPoint(v));
                if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
            }
            UnityEngine.Object.DestroyImmediate(baked);
        }
        foreach (var m in model.GetComponentsInChildren<MeshFilter>(true))
            foreach (var v in m.sharedMesh.vertices)
            {
                var p = space.InverseTransformPoint(m.transform.TransformPoint(v));
                if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
            }
        if (first) throw new Exception("Empty geometry");
        return b;
    }
}
