#if UNITY_EDITOR
using System;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class Sr18PresentationAssets
{
    public const string StableFatThrowPath = "Assets/JH/Model/Animatior/ForwardEnemyShared/Fatman_ThrowShort.anim";

    public static AnimationClip GetStableFatThrow()
    {
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(StableFatThrowPath);
        if(existing!=null)return existing;
        const string sourcePath="Assets/JH/Model/Enemy/Fat_Throw/던짐_스킨/Builder_Bob_0811155314_texture@Goalie Throw.fbx";
        var source=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().Single(c=>c.name=="Fatman Act");
        var clip=UnityEngine.Object.Instantiate(source);clip.name="Fatman_ThrowShort";
        const float end=1.1f;
        foreach(var binding in AnimationUtility.GetCurveBindings(clip))
        {
            var original=AnimationUtility.GetEditorCurve(clip,binding);
            var keys=original.keys.Where(k=>k.time<end).ToList();
            keys.Add(new Keyframe(end,original.Evaluate(end)));
            AnimationUtility.SetEditorCurve(clip,binding,new AnimationCurve(keys.ToArray()));
        }
        foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            AnimationUtility.SetObjectReferenceCurve(clip,binding,AnimationUtility.GetObjectReferenceCurve(clip,binding).Where(k=>k.time<=end).ToArray());
        AnimationUtility.SetAnimationEvents(clip,AnimationUtility.GetAnimationEvents(clip).Where(e=>e.time<=end).ToArray());
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=end;settings.loopTime=false;settings.loopBlend=false;
        AnimationUtility.SetAnimationClipSettings(clip,settings);clip.EnsureQuaternionContinuity();
        AssetDatabase.CreateAsset(clip,StableFatThrowPath);return clip;
    }

    public static void RepairAnimationAssets()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CosmeticVisualCatalog>("Assets/ShooterSurvival/Resources/Cosmetics/Catalog.asset");
        var source = catalog.previewModel.GetComponentInChildren<SkinnedMeshRenderer>(true);
        CosmeticMeshPartition.Apply(source.sharedMesh, catalog.splitSharkMesh, source.bones);
        EditorUtility.SetDirty(catalog.splitSharkMesh);
        const string idlePath = "Assets/JH/Anim/Shark/Original_Idle.anim";
        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(idlePath);
        if (idle == null)
        {
            idle = new AnimationClip { name = "Original_Idle", frameRate = 30 };
            Transform root = catalog.previewModel.transform;
            foreach (Transform bone in source.bones)
            {
                string path = AnimationUtility.CalculateTransformPath(bone, root);
                Vector3 p = bone.localPosition; Quaternion q = bone.localRotation;
                string[] axes = { "x", "y", "z" };
                for (int i = 0; i < 3; i++) idle.SetCurve(path, typeof(Transform), "m_LocalPosition." + axes[i], AnimationCurve.Constant(0, 2.8f, p[i]));
                for (int i = 0; i < 4; i++) idle.SetCurve(path, typeof(Transform), "m_LocalRotation." + "xyzw"[i], AnimationCurve.Constant(0, 2.8f, q[i]));
                if (bone.name == "chest")
                    idle.SetCurve(path, typeof(Transform), "m_LocalScale.y", new AnimationCurve(new Keyframe(0, bone.localScale.y), new Keyframe(1.4f, bone.localScale.y * 1.012f), new Keyframe(2.8f, bone.localScale.y)));
            }
            var settings = AnimationUtility.GetAnimationClipSettings(idle); settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(idle, settings); AssetDatabase.CreateAsset(idle, idlePath);
        }
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/JH/Anim/Shark/Original.controller");
        var state = controller.layers[0].stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Idle")
            ?? controller.layers[0].stateMachine.AddState("Idle");
        state.motion = idle; state.writeDefaultValues = true;
        controller.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);

        const string sharedFolder = "Assets/JH/Model/Animatior/ForwardEnemyShared/";
        var enemies = AssetDatabase.LoadAssetAtPath<AnimatorController>(sharedFolder + "ForwardEnemyShared.controller");
        const string maskPath = sharedFolder + "ForwardEnemy_Carry.mask";
        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if (mask == null)
        {
            mask = new AvatarMask { name = "Upper body carry" };
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            foreach (var part in new[] { AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers })
                mask.SetHumanoidBodyPartActive(part, true);
            AssetDatabase.CreateAsset(mask, maskPath);
        }
        if (!enemies.layers.Any(l => l.name == EnemyEventController.CarryLayerName))
        {
            enemies.AddLayer(EnemyEventController.CarryLayerName);
            var layers = enemies.layers; var layer = layers[layers.Length - 1];
            layer.avatarMask = mask; layer.defaultWeight = 0f; layer.blendingMode = AnimatorLayerBlendingMode.Override;
            var carry = layer.stateMachine.AddState("Carry");
            carry.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(sharedFolder + "ForwardEnemy_Idle.anim");
            carry.writeDefaultValues = true; layer.stateMachine.defaultState = carry;
            enemies.layers = layers; EditorUtility.SetDirty(enemies);
        }
        var ikLayers = enemies.layers;
        for (int i = 0; i < ikLayers.Length; i++) ikLayers[i].iKPass = true;
        enemies.layers = ikLayers; EditorUtility.SetDirty(enemies);
        var fat=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(sharedFolder+"Overrides/Enemy_FatMan.overrideController");
        fat["ForwardEnemy_AttackOnce"]=GetStableFatThrow();EditorUtility.SetDirty(fat);
        AssetDatabase.SaveAssets();
    }
}
#endif
