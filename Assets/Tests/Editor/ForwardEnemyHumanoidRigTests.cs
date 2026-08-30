#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
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
    private const string ExternalLocomotionPath =
        "Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/UAL1_Standard.fbx";

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
        "Assets/JH/Model/Enemy/YellowMan_Web/Web_Skin/Bearded_Builder_in_Ye_0811162114_texture@Fishing Idle.fbx",
        ExternalLocomotionPath
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

    private static readonly object[] PreservedEnemyMotionAssets =
    {
        new object[]
        {
            "Assets/JH/Model/Prefab/Enemy_FatMan.prefab",
            "Assets/JH/Model/Enemy/Fat_Throw/던짐_스킨/Builder_Bob_0811155314_texture@Goalie Throw 1.fbx",
            "Fatman Idle",
            "Assets/JH/Model/Enemy/Fat_Throw/던짐_스킨/Builder_Bob_0811155314_texture@Goalie Throw.fbx",
            "Fatman Act",
            "Assets/JH/Model/Enemy/Fat_Throw/죽음_노스킨/Builder_Bob_0811155314_texture@Flying Back Death.fbx",
            "Fatman Die"
        },
        new object[]
        {
            "Assets/JH/Model/Prefab/Enemy_Guard.prefab",
            "Assets/JH/Model/Enemy/Garden_Spear/Shoot_Skin(Idle).fbx",
            "guard idle",
            "Assets/JH/Model/Enemy/Garden_Spear/Shoot_Skin.fbx",
            "guard act",
            "Assets/JH/Model/Enemy/Garden_Spear/Die_NoSkin.fbx",
            "guard die"
        },
        new object[]
        {
            "Assets/JH/Model/Prefab/Enemy_OldMan.prefab",
            "Assets/JH/Model/Enemy/Oldman_Shovel/Action_Skin/Elderly_Cartoon_Chara_0815100721_texture@Sword And Shield Attack.fbx",
            "old man act",
            "Assets/JH/Model/Enemy/Oldman_Shovel/Action_Skin/Elderly_Cartoon_Chara_0815100721_texture@Sword And Shield Attack.fbx",
            "old man act",
            "Assets/JH/Model/Enemy/Oldman_Shovel/Die_NoSkin/Elderly_Cartoon_Chara_0815100721_texture@Flying Back Death.fbx",
            "old man die"
        },
        new object[]
        {
            "Assets/JH/Model/Prefab/Enemy_Woman.prefab",
            "Assets/JH/Model/Enemy/Woman_Boss/휘두르기_스킨/Baker_in_Apron_0815171726_texture@Stable Sword Outward Slash.fbx",
            "Woman Act",
            "Assets/JH/Model/Enemy/Woman_Boss/휘두르기_스킨/Baker_in_Apron_0815171726_texture@Stable Sword Outward Slash.fbx",
            "Woman Act",
            "Assets/JH/Model/Enemy/Woman_Boss/죽기_노스킨/Baker_in_Apron_0815171726_texture@Sword And Shield Death.fbx",
            "Woman Die"
        },
        new object[]
        {
            "Assets/JH/Model/Prefab/Enemy_YllowMan.prefab",
            "Assets/JH/Model/Enemy/YellowMan_Web/Web_Skin/Bearded_Builder_in_Ye_0811162114_texture@Fishing Idle.fbx",
            "bearman act",
            "Assets/JH/Model/Enemy/YellowMan_Web/Web_Skin/Bearded_Builder_in_Ye_0811162114_texture@Fishing Idle.fbx",
            "bearman act",
            "Assets/JH/Model/Enemy/YellowMan_Web/Die_NoSkin/Bearded_Builder_in_Ye_0811162114_texture@Flying Back Death.fbx",
            "bearman die"
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
    public void ExternalLocomotionSource_KeepsLicenseAndProvenance()
    {
        TextAsset license = AssetDatabase.LoadAssetAtPath<TextAsset>(
            "Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/LICENSE.txt");
        Assert.That(
            license,
            Is.Not.Null);
        Assert.That(license.text, Does.Contain("CC0 1.0 Universal"));

        const string sourcePath =
            "Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/SOURCE.md";
        Assert.That(
            AssetDatabase.LoadAssetAtPath<DefaultAsset>(sourcePath),
            Is.Not.Null);
        string source = System.IO.File.ReadAllText(
            System.IO.Path.GetFullPath(sourcePath));
        Assert.That(
            source,
            Does.Contain("https://quaternius.itch.io/universal-animation-library"));
        Assert.That(
            source,
            Does.Contain(
                "CC73FC4E495B82958207316596317A3F40B9FA38065BDE1027937452DA537724"));
        Assert.That(
            source,
            Does.Contain(
                "21B32D912DA3CB93426D974FB945E86F5B2E86970ACD2CE89905E0FBF9F1DCC2"));
    }

    [Test]
    public void PreviousSharedLocomotionClip_RemainsAvailable()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/JH/Model/Animatior/ForwardEnemyShared/ForwardEnemy_Locomotion.anim");
        Assert.That(clip, Is.Not.Null);
        Assert.That(clip.isHumanMotion, Is.True);
        Assert.That(clip.isLooping, Is.True);
    }

    [Test]
    public void SharedController_UsesSixStateEnemyContract()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(SharedControllerPath);
        Assert.That(controller, Is.Not.Null, "Missing shared enemy controller.");
        Assert.That(controller.layers, Has.Length.EqualTo(1));
        Assert.That(controller.parameters, Is.Empty);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        Dictionary<string, AnimatorState> states = stateMachine.states
            .ToDictionary(childState => childState.state.name, childState => childState.state);
        Assert.That(
            states.Keys,
            Is.EquivalentTo(new[]
            {
                "idle",
                "attack_loop",
                "walk",
                "run",
                "die",
                "attack_once"
            }));
        Assert.That(stateMachine.defaultState, Is.SameAs(states["idle"]));
        Assert.That(states.Values.All(state => state.motion != null), Is.True);
        Assert.That(states["walk"].speed, Is.EqualTo(1f).Within(0.001f));

        AnimatorStateTransition idleTransition =
            states["attack_once"].transitions.Single();
        Assert.That(idleTransition.destinationState, Is.SameAs(states["idle"]));
        Assert.That(idleTransition.hasExitTime, Is.True);
        Assert.That(idleTransition.exitTime, Is.EqualTo(1f).Within(0.001f));
        Assert.That(stateMachine.anyStateTransitions, Is.Empty);
        AnimatorStateTransition attackLoopTransition =
            states["attack_loop"].transitions.Single();
        Assert.That(
            attackLoopTransition.destinationState,
            Is.SameAs(states["attack_loop"]));
        Assert.That(attackLoopTransition.hasExitTime, Is.True);
        Assert.That(
            states.Where(pair =>
                    pair.Key != "attack_once" &&
                    pair.Key != "attack_loop")
                .All(pair => pair.Value.transitions.Length == 0),
            Is.True);
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
        Assert.That(overrides, Has.Count.EqualTo(6));
        Assert.That(
            overrides.Select(pair => pair.Key.name),
            Is.EquivalentTo(new[]
            {
                "ForwardEnemy_Idle",
                "ForwardEnemy_AttackLoop",
                "ForwardEnemy_Walk",
                "ForwardEnemy_Run",
                "ForwardEnemy_Die",
                "ForwardEnemy_AttackOnce"
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

        foreach (string loopingSlot in new[]
                 {
                     "ForwardEnemy_Walk",
                     "ForwardEnemy_Run"
                 })
        {
            AnimationClip clip = overrides.Single(
                pair => pair.Key.name == loopingSlot).Value;
            Assert.That(clip.isLooping, Is.True, loopingSlot);
        }

        KeyValuePair<AnimationClip, AnimationClip> walkOverride =
            overrides.Single(pair => pair.Key.name == "ForwardEnemy_Walk");
        KeyValuePair<AnimationClip, AnimationClip> runOverride =
            overrides.Single(pair => pair.Key.name == "ForwardEnemy_Run");
        Assert.That(
            AssetDatabase.GetAssetPath(walkOverride.Value),
            Is.EqualTo(ExternalLocomotionPath));
        Assert.That(walkOverride.Value.name, Is.EqualTo("Armature|Walk_Loop"));
        Assert.That(
            AssetDatabase.GetAssetPath(runOverride.Value),
            Is.EqualTo(ExternalLocomotionPath));
        Assert.That(runOverride.Value.name, Is.EqualTo("Armature|Sprint_Loop"));

        foreach (KeyValuePair<AnimationClip, AnimationClip> pair in overrides)
        {
            if (pair.Key.name == "ForwardEnemy_Walk" ||
                pair.Key.name == "ForwardEnemy_Run")
            {
                continue;
            }

            Assert.That(
                AssetDatabase.GetAssetPath(pair.Value),
                Is.Not.EqualTo(ExternalLocomotionPath),
                $"Existing motion slot '{pair.Key.name}' was unexpectedly replaced.");
        }
    }

    [TestCaseSource(nameof(EnemyAnimatorAssets))]
    public void ForwardEnemyPrefab_AttackOnceEventReturnsToLoopingIdle(
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
            EnemyEventController eventController =
                instance.GetComponent<EnemyEventController>();
            animator.Rebind();
            animator.Update(0f);
            eventController.EventMode = EnemyEventMode.AttackOnce;

            Assert.That(eventController.ActivateFromSpot(), Is.True);
            animator.Update(0f);
            animator.Update(0.1f);
            AssertCurrentState(animator, "attack_once", prefabPath);

            animator.Update(10f);
            AssertCurrentState(animator, "idle", prefabPath);
            animator.Update(10f);
            AssertCurrentState(animator, "idle", prefabPath);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void NoMoveAnimation_KeepsIdleWhileMovementEventStarts()
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/JH/Model/Prefab/Enemy_Guard.prefab");
            var instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, previewScene);
            var target = new GameObject("No Animation Move Target");
            SceneManager.MoveGameObjectToScene(target, previewScene);
            target.transform.position = instance.transform.position + Vector3.forward * 5f;
            Animator animator = instance
                .GetComponentsInChildren<Animator>(true)
                .Single(candidate => candidate.runtimeAnimatorController != null);
            EnemyEventController eventController =
                instance.GetComponent<EnemyEventController>();
            eventController.EventMode = EnemyEventMode.MoveToTargetThenAttack;
            eventController.MoveAnimation = EnemyMoveAnimation.None;
            eventController.TargetPoint = target.transform;
            eventController.MoveSpeed = 2f;
            animator.Rebind();
            animator.Update(0f);

            Assert.That(eventController.ActivateFromSpot(), Is.True);
            animator.Update(0f);
            animator.Update(0.1f);

            AssertCurrentState(
                animator,
                ForwardEnemyAnimationContract.Idle,
                prefab.name);
            Assert.That(
                eventController.RuntimeState,
                Is.EqualTo(EnemyEventRuntimeState.MovingToTarget));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [TestCaseSource(nameof(EnemyAnimatorAssets))]
    public void ForwardEnemyPrefab_QuaterniusWalkChangesHumanoidPose(
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
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Play(ForwardEnemyAnimationContract.Walk, 0, 0f);
            animator.Update(0f);

            var authoredRotations = new Dictionary<HumanBodyBones, Quaternion>();
            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone)
                    continue;

                Transform boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform != null)
                    authoredRotations[bone] = boneTransform.localRotation;
            }

            animator.Update(0.4f);
            Assert.That(
                authoredRotations.Any(pair =>
                    Quaternion.Angle(
                        pair.Value,
                        animator.GetBoneTransform(pair.Key).localRotation) > 0.01f),
                Is.True,
                $"{prefabPath} did not retarget the Quaternius walk pose.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [TestCaseSource(nameof(PreservedEnemyMotionAssets))]
    public void ForwardEnemyPrefab_PreservesExistingNonLocomotionMotions(
        string prefabPath,
        string idlePath,
        string idleName,
        string attackPath,
        string attackName,
        string diePath,
        string dieName)
    {
        string assetName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
        string overridePath =
            $"Assets/JH/Model/Animatior/ForwardEnemyShared/Overrides/{assetName}.overrideController";
        AnimatorOverrideController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(
            controller.overridesCount);
        controller.GetOverrides(overrides);
        Dictionary<string, AnimationClip> bySlot = overrides.ToDictionary(
            pair => pair.Key.name,
            pair => pair.Value);

        AssertMotion(bySlot["ForwardEnemy_Idle"], idlePath, idleName);
        Assert.That(
            bySlot["ForwardEnemy_Idle"].isLooping,
            Is.True,
            $"{prefabPath} idle clip is not configured to loop.");
        AssertMotion(
            bySlot["ForwardEnemy_AttackLoop"],
            attackPath,
            attackName);
        AssertMotion(
            bySlot["ForwardEnemy_AttackOnce"],
            attackPath,
            attackName);
        AssertMotion(bySlot["ForwardEnemy_Die"], diePath, dieName);

        string yaml = System.IO.File.ReadAllText(
            System.IO.Path.GetFullPath(overridePath));
        Assert.That(
            yaml,
            Does.Not.Contain("996652653cc38714ba00f5dd5f79d47a"),
            $"{overridePath} still serializes the retired attack template slot.");
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
            EnemyEventController eventController =
                instance.GetComponent<EnemyEventController>();
            Assert.That(eventController, Is.Not.Null, prefabPath);

            animator.Rebind();
            animator.Update(0f);
            Quaternion authoredVisualRotation = animator.transform.rotation;
            eventController.SnapToRouteDirection();
            Assert.That(
                Quaternion.Angle(
                    animator.transform.rotation,
                    authoredVisualRotation),
                Is.LessThan(0.01f),
                $"{prefabPath} lost its authored visual rotation offset.");
            AssertCurrentState(animator, "idle", prefabPath);

            animator.Play("walk", 0, 0f);
            animator.Update(0f);
            AssertCurrentState(animator, "walk", prefabPath);

            animator.Play("run", 0, 0f);
            animator.Update(0f);
            AssertCurrentState(animator, "run", prefabPath);

            animator.Play("attack_loop", 0, 0f);
            animator.Update(0f);
            AssertCurrentState(animator, "attack_loop", prefabPath);

            animator.Play("attack_once", 0, 0f);
            animator.Update(0f);
            AssertCurrentState(animator, "attack_once", prefabPath);
            animator.Update(10f);
            AssertCurrentState(animator, "idle", prefabPath);

            animator.Play("die", 0, 0f);
            animator.Update(0f);
            AssertCurrentState(animator, "die", prefabPath);
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

    private static void AssertMotion(
        AnimationClip clip,
        string expectedPath,
        string expectedName)
    {
        Assert.That(clip, Is.Not.Null, expectedName);
        Assert.That(AssetDatabase.GetAssetPath(clip), Is.EqualTo(expectedPath));
        Assert.That(clip.name, Is.EqualTo(expectedName));
    }
}
#endif
