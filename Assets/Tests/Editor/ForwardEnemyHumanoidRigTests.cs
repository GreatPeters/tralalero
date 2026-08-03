#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ForwardEnemyHumanoidRigTests
{
    private const string SharedControllerPath =
        "Assets/JH/Model/Animatior/ForwardEnemyShared/ForwardEnemyShared.controller";

    private static readonly string[] HumanoidModelPaths =
    {
        "Assets/JH/Model/Enemy/Fat_Throw/던짐_스킨/Builder_Bob_0811155314_texture@Goalie Throw 1.fbx",
        "Assets/JH/Model/Enemy/Fat_Throw/던짐_스킨/Builder_Bob_0811155314_texture@Goalie Throw.fbx",
        "Assets/JH/Model/Enemy/Fat_Throw/죽음_노스킨/Builder_Bob_0811155314_texture@Flying Back Death.fbx",
        "Assets/JH/Model/Enemy/Garden_Spear/Die_NoSkin.fbx",
        "Assets/JH/Model/Enemy/Garden_Spear/Shoot_Skin(Idle).fbx",
        "Assets/JH/Model/Enemy/Garden_Spear/Shoot_Skin.fbx",
        "Assets/JH/Model/Enemy/Oldman_Shovel/Action_Skin/Elderly_Cartoon_Chara_0815100721_texture@Sword And Shield Attack.fbx",
        "Assets/JH/Model/Enemy/Oldman_Shovel/Die_NoSkin/Elderly_Cartoon_Chara_0815100721_texture@Flying Back Death.fbx",
        "Assets/JH/Model/Enemy/Woman_Boss/죽기_노스킨/Baker_in_Apron_0815171726_texture@Sword And Shield Death.fbx",
        "Assets/JH/Model/Enemy/Woman_Boss/휘두르기_스킨/Baker_in_Apron_0815171726_texture@Stable Sword Outward Slash.fbx",
        "Assets/JH/Model/Enemy/YellowMan_Web/Die_NoSkin/Bearded_Builder_in_Ye_0811162114_texture@Flying Back Death.fbx",
        "Assets/JH/Model/Enemy/YellowMan_Web/Web_Skin/Bearded_Builder_in_Ye_0811162114_texture@Fishing Idle.fbx"
    };

    private static readonly object[] EnemyAnimatorAssets =
    {
        new object[]
        {
            "Assets/JH/Model/Prefab/Enemy_FatMan.prefab",
            "Assets/JH/Model/Animatior/ForwardEnemyShared/Overrides/Enemy_FatMan.overrideController"
        },
        new object[]
        {
            "Assets/JH/Model/Prefab/Enemy_Guard.prefab",
            "Assets/JH/Model/Animatior/ForwardEnemyShared/Overrides/Enemy_Guard.overrideController"
        },
        new object[]
        {
            "Assets/JH/Model/Prefab/Enemy_OldMan.prefab",
            "Assets/JH/Model/Animatior/ForwardEnemyShared/Overrides/Enemy_OldMan.overrideController"
        },
        new object[]
        {
            "Assets/JH/Model/Prefab/Enemy_Woman.prefab",
            "Assets/JH/Model/Animatior/ForwardEnemyShared/Overrides/Enemy_Woman.overrideController"
        },
        new object[]
        {
            "Assets/JH/Model/Prefab/Enemy_YllowMan.prefab",
            "Assets/JH/Model/Animatior/ForwardEnemyShared/Overrides/Enemy_YllowMan.overrideController"
        }
    };

    [TestCaseSource(nameof(HumanoidModelPaths))]
    public void ForwardEnemyModel_ImportsAsValidHumanoid(string modelPath)
    {
        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        Assert.That(importer, Is.Not.Null, $"Missing model importer at {modelPath}.");
        Assert.That(
            importer.animationType,
            Is.EqualTo(ModelImporterAnimationType.Human),
            $"{modelPath} is not imported as Humanoid.");

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<Avatar>()
            .FirstOrDefault();
        Assert.That(avatar, Is.Not.Null, $"Missing Avatar sub-asset at {modelPath}.");
        Assert.That(avatar.isHuman, Is.True, $"Avatar is not Humanoid at {modelPath}.");
        Assert.That(avatar.isValid, Is.True, $"Avatar is invalid at {modelPath}.");

        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__"))
            .ToArray();
        Assert.That(clips, Is.Not.Empty, $"No imported animation clips at {modelPath}.");
        foreach (AnimationClip clip in clips)
        {
            Assert.That(
                clip.isHumanMotion,
                Is.True,
                $"{modelPath} clip '{clip.name}' is not Humanoid motion.");
        }
    }

    [Test]
    public void SharedController_UsesIdleAttackDieContract()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(SharedControllerPath);
        Assert.That(controller, Is.Not.Null, "Missing shared enemy controller.");
        Assert.That(controller.layers, Has.Length.EqualTo(1));
        Assert.That(
            controller.parameters.Select(parameter => parameter.name),
            Is.EquivalentTo(new[] { "act", "die" }));
        Assert.That(
            controller.parameters.All(
                parameter => parameter.type == AnimatorControllerParameterType.Trigger),
            Is.True);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        Dictionary<string, AnimatorState> states = stateMachine.states
            .ToDictionary(childState => childState.state.name, childState => childState.state);
        Assert.That(
            states.Keys,
            Is.EquivalentTo(new[] { "Idle", "Attack", "Die" }));
        Assert.That(stateMachine.defaultState, Is.SameAs(states["Idle"]));
        Assert.That(states.Values.All(state => state.motion != null), Is.True);

        AnimatorStateTransition attackTransition = states["Idle"].transitions.Single();
        Assert.That(attackTransition.destinationState, Is.SameAs(states["Attack"]));
        Assert.That(
            attackTransition.conditions.Select(condition => condition.parameter),
            Is.EquivalentTo(new[] { "act" }));

        AnimatorStateTransition idleTransition = states["Attack"].transitions.Single();
        Assert.That(idleTransition.destinationState, Is.SameAs(states["Idle"]));
        Assert.That(idleTransition.hasExitTime, Is.True);

        AnimatorStateTransition dieTransition =
            stateMachine.anyStateTransitions.Single();
        Assert.That(dieTransition.destinationState, Is.SameAs(states["Die"]));
        Assert.That(
            dieTransition.conditions.Select(condition => condition.parameter),
            Is.EquivalentTo(new[] { "die" }));
        Assert.That(states["Die"].transitions, Is.Empty);
    }

    [TestCaseSource(nameof(EnemyAnimatorAssets))]
    public void ForwardEnemyPrefab_UsesHumanoidOverrideController(
        string prefabPath,
        string overrideControllerPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.That(prefab, Is.Not.Null, $"Missing enemy prefab at {prefabPath}.");

        Animator[] animators = prefab
            .GetComponentsInChildren<Animator>(true)
            .Where(animator => animator.runtimeAnimatorController != null)
            .ToArray();
        Assert.That(
            animators,
            Has.Length.EqualTo(1),
            $"{prefabPath} must have exactly one configured Animator.");

        Animator animator = animators[0];
        Assert.That(
            animator.avatar,
            Is.Not.Null,
            $"{prefabPath} Animator '{animator.name}' has no Avatar.");
        Assert.That(
            animator.avatar.isHuman,
            Is.True,
            $"{prefabPath} Animator '{animator.name}' does not use a Humanoid Avatar.");
        Assert.That(
            animator.avatar.isValid,
            Is.True,
            $"{prefabPath} Animator '{animator.name}' uses an invalid Avatar.");

        AnimatorOverrideController expectedOverride =
            AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(
                overrideControllerPath);
        Assert.That(expectedOverride, Is.Not.Null, $"Missing {overrideControllerPath}.");
        Assert.That(animator.runtimeAnimatorController, Is.SameAs(expectedOverride));

        AnimatorController sharedController =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(SharedControllerPath);
        Assert.That(
            expectedOverride.runtimeAnimatorController,
            Is.SameAs(sharedController));

        var overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>(
                expectedOverride.overridesCount);
        expectedOverride.GetOverrides(overrides);
        Assert.That(overrides, Has.Count.EqualTo(3));
        Assert.That(
            overrides.Select(pair => pair.Key.name),
            Is.EquivalentTo(new[]
            {
                "ForwardEnemy_Idle",
                "ForwardEnemy_Attack",
                "ForwardEnemy_Die"
            }));

        foreach (KeyValuePair<AnimationClip, AnimationClip> pair in overrides)
        {
            Assert.That(
                pair.Value,
                Is.Not.Null,
                $"{overrideControllerPath} does not override '{pair.Key.name}'.");
            Assert.That(
                pair.Value.isHumanMotion,
                Is.True,
                $"{overrideControllerPath} clip '{pair.Value.name}' is not Humanoid motion.");
        }
    }

    [TestCaseSource(nameof(EnemyAnimatorAssets))]
    public void ForwardEnemyPrefab_TransitionsThroughSharedStates(
        string prefabPath,
        string _)
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, previewScene);
            Animator animator = instance
                .GetComponentsInChildren<Animator>(true)
                .Single(candidate => candidate.runtimeAnimatorController != null);

            animator.Rebind();
            animator.Update(0f);
            AssertCurrentState(animator, "Idle", prefabPath);

            animator.SetTrigger("act");
            animator.Update(0f);
            animator.Update(0.1f);
            AssertCurrentState(animator, "Attack", prefabPath);

            animator.SetTrigger("die");
            animator.Update(0f);
            animator.Update(0.1f);
            AssertCurrentState(animator, "Die", prefabPath);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    private static void AssertCurrentState(
        Animator animator,
        string expectedState,
        string prefabPath)
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        Assert.That(
            state.shortNameHash,
            Is.EqualTo(Animator.StringToHash(expectedState)),
            $"{prefabPath} did not enter '{expectedState}'.");
    }
}
#endif
