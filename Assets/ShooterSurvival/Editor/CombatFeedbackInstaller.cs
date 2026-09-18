#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CombatFeedbackInstaller
{
    private const string Folder = "Assets/ShooterSurvival/Prefabs/CombatFeedback";
    private const string Shared = "Assets/JH/Model/Animatior/ForwardEnemyShared/";
    private static readonly string[] Scenes = {
        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity",
        "Assets/ShooterSurvival/Scenes/Tools/HighWay.unity",
        "Assets/ShooterSurvival/Scenes/Tools/RestStop.unity"
    };

    private static void Backup(string path)
    {
        string target = "tmp/combat-feedback-2026-09-14/before/assets/" + path;
        if (File.Exists(target)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(path, target);
    }

    public static object Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        RepairMelee();
        var hit = BuildHitEffect();
        string original = SceneManager.GetActiveScene().path;
        var results = new List<string>();
        foreach (var path in Scenes)
        {
            Backup(path);
            var scene = EditorSceneManager.OpenScene(path);
            int poles = 0;
            foreach (var obstacle in Object.FindObjectsByType<ObstacleStats>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (obstacle.obstaclePattern != ObstaclePattern.Light && obstacle.obstaclePattern != ObstaclePattern.Hole) continue;
                if (obstacle.obstaclePattern == ObstaclePattern.Light) { obstacle.canBeShotDown = true; poles++; }
                var rb = obstacle.GetComponent<Rigidbody>();
                if (rb == null) rb = obstacle.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true; rb.useGravity = false;
                EditorUtility.SetDirty(obstacle);
            }
            foreach (var enemy in Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var data = (EnemySO)new SerializedObject(enemy).FindProperty("enemyData").objectReferenceValue;
                if (data == null || data.enemyHitVFX == hit) continue;
                Backup(AssetDatabase.GetAssetPath(data));
                data.enemyHitVFX = hit; EditorUtility.SetDirty(data); AssetDatabase.SaveAssetIfDirty(data);
            }
            var canvas = Object.FindFirstObjectByType<CanvasScript>();
            var hint = canvas.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "StartHint");
            if (hint != null)
            {
                hint.anchorMin = new Vector2(.08f, .32f); hint.anchorMax = new Vector2(.92f, .46f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(hint);
                var title = hint.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Title");
                if (title != null) { title.anchorMin = new Vector2(.04f,.66f); title.anchorMax = new Vector2(.96f,.96f); PrefabUtility.RecordPrefabInstancePropertyModifications(title); }
                var oldText = hint.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Hint");
                if (oldText != null) { oldText.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(oldText.gameObject); }
                var gesture = hint.GetComponentInChildren<SwipeStartHint>(true);
                if (gesture == null)
                {
                    gesture = new GameObject("Swipe finger and arrows", typeof(RectTransform)).AddComponent<SwipeStartHint>();
                    gesture.transform.SetParent(hint, false);
                }
                gesture.raycastTarget = false;
                if (gesture.GetComponent<CanvasRenderer>() == null) gesture.gameObject.AddComponent<CanvasRenderer>();
                gesture.color = Color.white;
                gesture.gameObject.layer = hint.gameObject.layer;
                gesture.rectTransform.anchorMin = gesture.rectTransform.anchorMax = new Vector2(.5f,.34f);
                gesture.rectTransform.anchoredPosition = Vector2.zero;
                gesture.rectTransform.sizeDelta = new Vector2(310, 90);
            }
            EditorSceneManager.SaveScene(scene);
            results.Add(scene.name + ": " + poles + " shootable lethal poles; swipe hint=" + (hint != null));
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene(original);
        return results;
    }

    public static void RepairMelee()
    {
        MobileKnifePose.Apply();
        var ready = AssetDatabase.LoadAssetAtPath<AnimationClip>(MobileKnifePose.ClipPath);
        foreach (string name in new[] { "Enemy_Woman", "Enemy_YllowMan_Sword" })
        {
            string path = Shared + "Overrides/" + name + ".overrideController";
            Backup(path);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            controller["ForwardEnemy_Idle"] = ready;
            string attackPath = Shared + name + "_ControlledSlash.anim";
            var attack = AssetDatabase.LoadAssetAtPath<AnimationClip>(attackPath);
            if (attack == null)
            {
                var source = ready;
                attack = Object.Instantiate(source); attack.name = name + "_ControlledSlash";
                foreach (var binding in AnimationUtility.GetCurveBindings(attack))
                {
                    // Build from the neutral humanoid pose, not the imported spinning slash.
                    if (binding.type == typeof(Animator))
                    {
                        var curve = AnimationUtility.GetEditorCurve(attack, binding);
                        AnimationUtility.SetEditorCurve(attack, binding, AnimationCurve.Constant(0, 1.6f, curve.Evaluate(.15f)));
                    }
                }
                void Muscle(int index, float rest, float windup, float strike, float follow)
                {
                    var curve = new AnimationCurve(new Keyframe(0,rest),new Keyframe(.45f,windup),new Keyframe(.68f,strike),new Keyframe(.92f,follow),new Keyframe(1.6f,rest));
                    for(int key=0;key<curve.length;key++) curve.SmoothTangents(key,.4f);
                    AnimationUtility.SetEditorCurve(attack,EditorCurveBinding.FloatCurve("",typeof(Animator),HumanTrait.MuscleName[index]),curve);
                }
                Muscle(48,-.4f,.3f,-.1f,-.4f); // Upper arm rises before cutting downward.
                Muscle(49,.2f,-.2f,.55f,.35f); // The blade crosses the front of the body.
                Muscle(50,-.15f,-.3f,.15f,.1f);
                Muscle(51,.15f,-.35f,.65f,.3f);
                Muscle(52,.5f,.35f,.55f,.5f);
                Muscle(53,.1f,.04f,-.08f,.04f);
                Muscle(2,0,.12f,-.15f,-.06f);
                Muscle(5,0,.12f,-.15f,-.06f);
                var settings = AnimationUtility.GetAnimationClipSettings(attack);
                settings.loopTime = true; settings.loopBlend = true;
                AnimationUtility.SetAnimationClipSettings(attack, settings);
                AssetDatabase.CreateAsset(attack, attackPath);
            }
            // Also repair an existing revision without replacing its stable GUID.
            foreach (var binding in AnimationUtility.GetCurveBindings(attack))
                if (binding.type == typeof(Animator) && (binding.propertyName.StartsWith("RootQ.") || binding.propertyName.StartsWith("MotionQ.")))
                {
                    var curve = AnimationUtility.GetEditorCurve(attack, binding);
                    var neutral = AnimationUtility.GetEditorCurve(ready, binding);
                    AnimationUtility.SetEditorCurve(attack, binding, AnimationCurve.Constant(0, attack.length, neutral != null ? neutral.Evaluate(0) : curve.Evaluate(0)));
                }
            attack.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(attack); AssetDatabase.SaveAssetIfDirty(attack);
            controller["ForwardEnemy_AttackOnce"] = attack;
            controller["ForwardEnemy_AttackLoop"] = attack;
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
        }
        // Carry only arms: a torso/head override fights both locomotion and gaze.
        string maskPath = Shared + "ForwardEnemy_Carry.mask";
        Backup(maskPath);
        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, false);
        EditorUtility.SetDirty(mask); AssetDatabase.SaveAssetIfDirty(mask);
    }

    private static GameObject BuildHitEffect()
    {
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        string path = Folder + "/ImpactSplash.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing;
        var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        material.name = "Impact droplets"; material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.renderQueue = 3000;
        AssetDatabase.CreateAsset(material, Folder + "/ImpactDroplets.mat");
        var root = new GameObject("Impact splash");
        try
        {
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = .55f; main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.18f,.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f,4f);
            main.startSize = new ParticleSystem.MinMaxCurve(.055f,.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1,.13f,.24f),new Color(1,.47f,.3f));
            main.gravityModifier = .7f; main.maxParticles = 16;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            var emission = particles.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0,12) });
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .1f;
            var color = particles.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white,0), new GradientColorKey(Color.white,1) },
                new[] { new GradientAlphaKey(1,0), new GradientAlphaKey(1,.45f), new GradientAlphaKey(0,1) });
            color.color = gradient;
            var size = particles.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.1f));
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            return PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
#endif
