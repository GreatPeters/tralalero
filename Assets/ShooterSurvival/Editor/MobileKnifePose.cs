using System;
using UnityEditor;
using UnityEngine;

public static class MobileKnifePose
{
    public const string ClipPath = "Assets/JH/Model/Animatior/ForwardEnemyShared/Knife_RelaxedCarry.anim";
    private const string ControllerPath = "Assets/JH/Model/Animatior/ForwardEnemyShared/Overrides/Enemy_YllowMan_Sword.overrideController";
    private const string SourcePath = "Assets/JH/Model/Enemy/YellowMan_Web/Web_Skin/Bearded_Builder_in_Ye_0811162114_texture@Fishing Idle.fbx";

    public static object Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ControllerPath);
        MobileFontMigration.Backup(ControllerPath);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            var source = Array.Find(AssetDatabase.LoadAllAssetsAtPath(SourcePath), a => a is AnimationClip && a.name == "bearman act") as AnimationClip;
            if (source == null) throw new InvalidOperationException("Original fishing idle is missing");
            clip = UnityEngine.Object.Instantiate(source); clip.name = "Knife_RelaxedCarry";
            // The original fishing pose bends both wrists sharply and raises the empty hand.
            // Keep its hips, feet, timing and breathing; author only the arm muscle channels.
            int[] indices = { 39, 40, 41, 42, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54 };
            float[] values = { -.65f, .05f, 0, .7f, 0, 0, 0, 0, -.4f, .2f, -.15f, .15f, .5f, .1f, -.1f };
            for (int i = 0; i < indices.Length; i++)
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), HumanTrait.MuscleName[indices[i]]), AnimationCurve.Constant(0, source.length, values[i]));
            AssetDatabase.CreateAsset(clip, ClipPath);
        }
        // Idle is also the shared CarryPose layer's slot; only this enemy's override changes.
        controller["ForwardEnemy_Idle"] = clip; EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
        return new { clip = ClipPath, duration = clip.length, controller = ControllerPath };
    }
}
