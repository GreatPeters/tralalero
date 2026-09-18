using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class HighwayEnemyBuilder
{
    public static readonly string[] Names = { "ConeMechanic", "TrafficPatrol", "TollgateChief", "TireBruiser", "AsphaltWorker", "DeliveryRider" };
    public static readonly string[] Labels = { "라바콘 정비공", "교통 순찰대", "톨게이트 대장", "타이어 중장병", "아스팔트 작업자", "배달 라이더" };
    private static readonly string[] Bases = { "Enemy_YllowMan_Sword", "Enemy_Guard", "Enemy_FatMan", "Enemy_FatMan", "Enemy_YllowMan_Net", "Enemy_Guard" };
    private static readonly string[] Sources = {
        "outputs/highway-enemies-2026-09-10/01-mechanic_4a97f10f4c", "outputs/highway-enemies-2026-09-10/02-patrol_cd22a7a411",
        "outputs/highway-enemies-2026-09-10/03-chief_49f79d0151", "outputs/highway-bodies-2026-09-11/TireBruiser_e47ba89970",
        "outputs/highway-bodies-2026-09-11/AsphaltWorker_e1aaa28d56", "outputs/highway-bodies-2026-09-11/DeliveryRider_aae8ac6877" };
    private static readonly string[] States = { "idle", "walk", "run", "attack_loop", "attack_once", "die" };
    public const string Folder = "Assets/ShooterSurvival/Prefabs/Highway/Enemies";

    public static object Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        Directory.CreateDirectory(Folder);
        foreach (string name in Names)
        {
            string folder = "Assets/ShooterSurvival/Models/Highway/Rigged/" + name;
            Directory.CreateDirectory(folder);
            string target = folder + "/" + name + ".fbx";
            File.Copy("outputs/highway-rigged-2026-09-11/" + name + "/" + name + ".fbx", target, true);
            int index = Array.IndexOf(Names, name);
            foreach (string texture in new[] { "BaseColor", "Normal", "Roughness", "Metallic" })
                File.Copy(Sources[index] + "/textures/" + texture + ".png", folder + "/" + texture + ".png", true);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var reports = new System.Collections.Generic.List<object>();
        for (int i = 0; i < Names.Length; i++)
        {
            string name = Names[i], folder = "Assets/ShooterSurvival/Models/Highway/Rigged/" + name, fbx = folder + "/" + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true; importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; importer.SaveAndReimport();
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = !clip.name.EndsWith("die") && !clip.name.EndsWith("attack_once");
                clip.lockRootRotation = true; clip.lockRootHeightY = true; clip.lockRootPositionXZ = true;
            }
            importer.clipAnimations = clips; importer.SaveAndReimport();
            var normal = (TextureImporter)AssetImporter.GetAtPath(folder + "/Normal.png"); normal.textureType = TextureImporterType.NormalMap; normal.maxTextureSize = 1024; normal.SaveAndReimport();
            var albedo = (TextureImporter)AssetImporter.GetAtPath(folder + "/BaseColor.png"); albedo.maxTextureSize = 1024; albedo.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(folder + "/Surface.mat");
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, folder + "/Surface.mat"); }
            material.SetColor("_BaseColor", Color.white); material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/BaseColor.png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Normal.png")); material.EnableKeyword("_NORMALMAP"); material.SetFloat("_Smoothness", .3f); EditorUtility.SetDirty(material);
            GeneratedStylizedSurface.Apply(material);
            string controllerPath = folder + "/Actions.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var child in stateMachine.states) stateMachine.RemoveState(child.state);
            var animations = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
            foreach (string stateName in States)
            {
                var clip = animations.SingleOrDefault(c => c.name == stateName || c.name.EndsWith("|" + stateName));
                if (clip == null) throw new InvalidOperationException(name + " missing action: " + stateName);
                var state = stateMachine.AddState(stateName); state.motion = clip;
                if (stateName == "idle") stateMachine.defaultState = state;
                if (stateName == "run") state.speed = 1.4f;
            }
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/" + Bases[i] + ".prefab"));
            try
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                root.name = Labels[i]; root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); root.transform.localScale = Vector3.one;
                var combat = root.GetComponent<EnemyScript_space>(); var serialized = new SerializedObject(combat);
                var held = serialized.FindProperty("heldProjectile").objectReferenceValue as Transform;
                if (held != null) held.SetParent(root.transform, true);
                var oldAnimator = root.GetComponentInChildren<Animator>(true);
                if (oldAnimator == null || oldAnimator.gameObject == root) throw new InvalidOperationException("Unexpected original visual root.");
                UnityEngine.Object.DestroyImmediate(oldAnimator.gameObject);
                var oldAim = root.GetComponent<EnemyGunAim>(); if (oldAim != null) UnityEngine.Object.DestroyImmediate(oldAim);
                var oldGrounding = root.GetComponent<EnemyGroundedPose>(); if (oldGrounding != null) UnityEngine.Object.DestroyImmediate(oldGrounding);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx), root.transform);
                visual.name = "Body"; visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * (i == 2 ? 1.2f : i == 3 ? 1.1f : .95f);
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                {
                    var surface = material;
                    if (renderer.name.StartsWith("Carry_", StringComparison.Ordinal))
                    {
                        string kind = renderer.name.Split('.')[0];
                        string path = folder + "/" + kind + ".mat";
                        surface = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (surface == null) { surface = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(surface,path); }
                        surface.color = kind switch { "Carry_Rubber"=>new Color(.028f,.033f,.038f), "Carry_Tread"=>new Color(.045f,.051f,.055f), "Carry_Steel"=>new Color(.22f,.27f,.3f), "Carry_Orange"=>new Color(.83f,.25f,.04f), "Carry_Cardboard"=>new Color(.55f,.35f,.16f), _=>new Color(.85f,.64f,.26f) };
                        surface.SetFloat("_Smoothness",.22f);GeneratedStylizedSurface.Apply(surface);
                    }
                    renderer.sharedMaterials=Enumerable.Repeat(surface,renderer.sharedMaterials.Length).ToArray();
                }
                var animator = visual.GetComponent<Animator>(); if (animator == null) animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var capsule = root.GetComponent<CapsuleCollider>(); if (capsule == null) capsule = root.AddComponent<CapsuleCollider>();
                capsule.isTrigger = true; capsule.radius = i == 2 || i == 3 ? .65f : .48f; capsule.height = i == 2 ? 2.6f : 2.1f; capsule.center = Vector3.up * capsule.height * .5f;
                if (held != null)
                {
                    var hand = visual.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Hand.R");
                    var throwPoint = new GameObject("HighwayThrowPoint").transform; throwPoint.SetParent(hand, false);
                    throwPoint.localPosition = new Vector3(0, .08f, .04f);
                    held.SetParent(throwPoint, false); held.localPosition = Vector3.zero; held.localRotation = Quaternion.identity; held.localScale = Vector3.one * .28f;
                    serialized.Update(); serialized.FindProperty("heldProjectile").objectReferenceValue = held; serialized.FindProperty("throwPoint").objectReferenceValue = throwPoint;
                    serialized.FindProperty("hideHeldProjectile").boolValue = true;
                    serialized.FindProperty("throwReleaseDelay").floatValue = .55f; serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var events = root.GetComponent<EnemyEventController>(); events.HideWhileWaiting = false;
                events.EventMode = held == null ? EnemyEventMode.AttackLoop : EnemyEventMode.Shoot;
                var eventData = new SerializedObject(events); eventData.FindProperty("modelForwardYaw").floatValue = 0; eventData.ApplyModifiedPropertiesWithoutUndo();
                events.SnapToRouteDirection();
                var hit = root.transform.Find("Walker-HitPos"); if (hit != null) hit.localPosition = Vector3.up * 1.1f;
                string prefab = Folder + "/" + name + ".prefab"; PrefabUtility.SaveAsPrefabAsset(root, prefab);
                reports.Add(new { name, prefab, actions = animations.Select(c => c.name).ToArray(), bones = visual.GetComponentInChildren<SkinnedMeshRenderer>().bones.Length, projectile = held != null });
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("map-concepts/highway-enemies-2026-09-11/combat-prefabs.json", Newtonsoft.Json.JsonConvert.SerializeObject(reports, Newtonsoft.Json.Formatting.Indented));
        NoryangjinMapToolWindow.RefreshOpenWindowPaletteAssets();
        return new { enemies = reports.Count, actionsPerEnemy = 6 };
    }
}
