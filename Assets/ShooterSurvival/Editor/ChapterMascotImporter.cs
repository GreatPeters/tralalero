using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Replaces presentation inside stable combat prefabs; scene placement remains workbook-owned.
public static class ChapterMascotImporter
{
    public const string RestStopEnemies = "Assets/ShooterSurvival/Prefabs/RestStop/Enemies";
    private const string Models = "Assets/ShooterSurvival/Models/Chapters/Mascots";
    private const string Output = "outputs/chapters-polish-2026-09-12/rigged/v2";
    private const string Record = "map-concepts/chapters-polish-2026-09-12";
    public static readonly string[] Names = HighwayEnemyBuilder.Names.Concat(new[] { "SnackChef", "CoffeeVendor", "ParkingMarshal" }).ToArray();
    private static readonly string[] Actions = { "idle", "walk", "run", "attack_loop", "attack_once", "die" };
    private static readonly string[] RestBases = { "Enemy_YllowMan_Sword", "Enemy_FatMan", "Enemy_YllowMan_Sword" };

    public static string PrefabPath(string name) =>
        (Array.IndexOf(Names, name) < 6 ? HighwayEnemyBuilder.Folder : RestStopEnemies) + "/" + name + ".prefab";

    public static object ImportReviewed()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        foreach (string name in Names)
        {
            string source = Output + "/" + name;
            if (!File.Exists(source + "/fresh-inspection.json") || !File.Exists(source + "/fresh-fbx-fullkeys-pose-report.json"))
                throw new InvalidOperationException("Fresh import evidence required: " + name);
            string folder = Models + "/" + name;
            Directory.CreateDirectory(folder);
            File.Copy(source + "/" + name + "-full-keys.fbx", folder + "/" + name + ".fbx", true);
            var rig = JObject.Parse(File.ReadAllText(source + "/rig-report.json"));
            string textures = Path.Combine(Path.GetDirectoryName((string)rig["source"]), "textures");
            File.Copy(Path.Combine(textures, "BaseColor.png"), folder + "/BaseColor.png", true);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Directory.CreateDirectory(RestStopEnemies);
        var results = new List<object>();
        for (int index = 0; index < Names.Length; index++)
        {
            string name = Names[index], folder = Models + "/" + name, fbx = folder + "/" + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.SaveAndReimport();
            var clipSettings = importer.defaultClipAnimations;
            foreach (var clip in clipSettings)
            {
                clip.loopTime = !clip.name.EndsWith("die", StringComparison.Ordinal) && !clip.name.EndsWith("attack_once", StringComparison.Ordinal);
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
            }
            importer.clipAnimations = clipSettings;
            importer.SaveAndReimport();
            var texture = (TextureImporter)AssetImporter.GetAtPath(folder + "/BaseColor.png");
            texture.maxTextureSize = 1024;
            texture.mipmapEnabled = true;
            texture.SaveAndReimport();
            var materialSlots = (JObject)JObject.Parse(File.ReadAllText(Output + "/" + name + "/fresh-pose-report.json"))["materials"];
            var surfaces = new Dictionary<string, Material[]>();
            foreach (var mesh in materialSlots.Properties())
            {
                surfaces[mesh.Name] = mesh.Value.Select(slot =>
                {
                    string materialName = (string)slot["name"];
                    string path = folder + "/" + materialName + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        AssetDatabase.CreateAsset(material, path);
                    }
                    bool textured = (bool)slot["textured"];
                    var color = slot["color"].Values<float>().ToArray();
                    material.SetColor("_BaseColor", textured ? Color.white : new Color(color[0], color[1], color[2], color[3]));
                    material.SetTexture("_BaseMap", textured ? AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/BaseColor.png") : null);
                    material.SetFloat("_Smoothness", .12f);
                    material.SetFloat("_Metallic", 0);
                    material.DisableKeyword("_NORMALMAP");
                    GeneratedStylizedSurface.Apply(material);
                    return material;
                }).ToArray();
            }
            var controller = BuildController(folder, fbx);
            string destination = PrefabPath(name);
            string sourcePrefab = File.Exists(destination) ? destination : "Assets/JH/Model/Prefab/" + RestBases[index - 6] + ".prefab";
            string backup = "tmp/backups/chapters-polish-2026-09-12/" + name + ".prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (File.Exists(destination) && !File.Exists(backup)) File.Copy(destination, backup);
            var root = PrefabUtility.LoadPrefabContents(sourcePrefab);
            try
            {
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                root.transform.localScale = Vector3.one;
                var combat = root.GetComponent<EnemyScript_space>();
                var combatData = new SerializedObject(combat);
                var held = combatData.FindProperty("heldProjectile").objectReferenceValue as Transform;
                if (held != null) held.SetParent(root.transform, true);
                var previous = root.GetComponentInChildren<Animator>(true);
                if (previous == null || previous.gameObject == root) throw new InvalidOperationException("Unexpected visual root: " + name);
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
                foreach (var old in root.GetComponents<EnemyGunAim>()) UnityEngine.Object.DestroyImmediate(old);
                foreach (var old in root.GetComponents<EnemyGroundedPose>()) UnityEngine.Object.DestroyImmediate(old);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx), root.transform);
                visual.name = "Body";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale *= name == "TollgateChief" ? 1.1f : 1f;
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    if (!surfaces.TryGetValue(renderer.name, out var slots) || slots.Length != renderer.sharedMaterials.Length)
                        throw new InvalidOperationException("Unmapped material slots: " + name + "/" + renderer.name);
                    renderer.sharedMaterials = slots;
                }
                var animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                if (held != null)
                {
                    var hand = visual.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Hand.R");
                    var point = new GameObject("MascotThrowPoint").transform;
                    point.SetParent(hand, false);
                    point.position = hand.position + root.transform.up * .06f + root.transform.forward * .04f;
                    var handScale = hand.lossyScale;
                    point.localScale = new Vector3(1f / Mathf.Abs(handScale.x), 1f / Mathf.Abs(handScale.y), 1f / Mathf.Abs(handScale.z));
                    held.SetParent(point, false);
                    held.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    held.localScale = Vector3.one * .28f;
                    combatData.Update();
                    combatData.FindProperty("heldProjectile").objectReferenceValue = held;
                    combatData.FindProperty("throwPoint").objectReferenceValue = point;
                    combatData.FindProperty("hideHeldProjectile").boolValue = true;
                    combatData.FindProperty("throwReleaseDelay").floatValue = .55f;
                    combatData.ApplyModifiedPropertiesWithoutUndo();
                    if (name == "CoffeeVendor") StyleCoffeeProjectile(held, surfaces[name + "_Equipment"]);
                }
                var capsule = root.GetComponent<CapsuleCollider>() ?? root.AddComponent<CapsuleCollider>();
                capsule.isTrigger = true;
                capsule.height = name == "TireBruiser" || name == "TollgateChief" ? 1.8f : 1.65f;
                capsule.radius = name == "TireBruiser" ? .58f : .44f;
                capsule.center = Vector3.up * capsule.height * .5f;
                var hit = root.transform.Find("Walker-HitPos");
                if (hit != null) hit.localPosition = Vector3.up * .85f;
                var events = root.GetComponent<EnemyEventController>();
                var eventData = new SerializedObject(events);
                eventData.FindProperty("modelForwardYaw").floatValue = 0;
                eventData.ApplyModifiedPropertiesWithoutUndo();
                if (index >= 6)
                {
                    root.name = name switch { "SnackChef" => "휴게소 요리사", "CoffeeVendor" => "커피 직원", _ => "주차 안내원" };
                    events.HideWhileWaiting = false;
                    events.EventMode = held != null ? EnemyEventMode.Shoot : EnemyEventMode.AttackLoop;
                }
                PrefabUtility.SaveAsPrefabAsset(root, destination);
                results.Add(new { name, prefab = destination, ranged = held != null, bones = 18, actions = Actions.Length });
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        NoryangjinMapToolWindow.RefreshOpenWindowPaletteAssets();
        File.WriteAllText(Record + "/mascot-import.json", JsonConvert.SerializeObject(results, Formatting.Indented));
        return results;
    }

    private static AnimatorController BuildController(string folder, string fbx)
    {
        string path = folder + "/Actions.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        foreach (var prior in machine.states) machine.RemoveState(prior.state);
        var clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview", StringComparison.Ordinal)).ToArray();
        foreach (string name in Actions)
        {
            var clip = clips.Single(c => c.name == name || c.name.EndsWith("|" + name, StringComparison.Ordinal));
            var state = machine.AddState(name);
            state.motion = clip;
            if (name == "idle") machine.defaultState = state;
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void StyleCoffeeProjectile(Transform held, Material[] materials)
    {
        foreach (Transform child in held.Cast<Transform>().Where(t => t.name == "CoffeeCup" || t.name == "CoffeeLid").ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        // Keep the existing projectile's scripts, trigger, rigidbody and damage lifecycle.
        foreach (var renderer in held.GetComponentsInChildren<Renderer>(true)) UnityEngine.Object.DestroyImmediate(renderer);
        foreach (var filter in held.GetComponentsInChildren<MeshFilter>(true)) UnityEngine.Object.DestroyImmediate(filter);
        var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cup.name = "CoffeeCup";
        cup.transform.SetParent(held, false);
        cup.transform.localScale = new Vector3(.7f, .5f, .7f);
        UnityEngine.Object.DestroyImmediate(cup.GetComponent<Collider>());
        cup.GetComponent<Renderer>().sharedMaterial = materials[0];
        var lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lid.name = "CoffeeLid";
        lid.transform.SetParent(held, false);
        lid.transform.localPosition = Vector3.up * .51f;
        lid.transform.localScale = new Vector3(.8f, .06f, .8f);
        UnityEngine.Object.DestroyImmediate(lid.GetComponent<Collider>());
        lid.GetComponent<Renderer>().sharedMaterial = materials[1];
    }
}
