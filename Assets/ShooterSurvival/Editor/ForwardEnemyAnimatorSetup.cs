#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ForwardEnemyAnimatorSetup
{
    public const string MenuPath =
        "Tools/Shooter Survival/Forward Enemy/Build Shared Animator Setup";

    internal const string SharedRoot =
        "Assets/JH/Model/Animatior/ForwardEnemyShared";
    internal const string SharedControllerPath =
        SharedRoot + "/ForwardEnemyShared.controller";
    internal const string IdleTemplatePath =
        SharedRoot + "/ForwardEnemy_Idle.anim";
    internal const string AttackLoopTemplatePath =
        SharedRoot + "/ForwardEnemy_AttackLoop.anim";
    internal const string WalkTemplatePath =
        SharedRoot + "/ForwardEnemy_Walk.anim";
    internal const string RunTemplatePath =
        SharedRoot + "/ForwardEnemy_Run.anim";
    internal const string DieTemplatePath =
        SharedRoot + "/ForwardEnemy_Die.anim";
    internal const string AttackOnceTemplatePath =
        SharedRoot + "/ForwardEnemy_AttackOnce.anim";
    internal const string OverridesRoot =
        SharedRoot + "/Overrides";
    internal const string CommonLocomotionClipPath =
        SharedRoot + "/ForwardEnemy_Locomotion.anim";
    internal const string ExternalLocomotionModelPath =
        "Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/UAL1_Standard.fbx";
    internal const string ExternalWalkClipName = "Armature|Walk_Loop";
    internal const string ExternalRunClipName = "Armature|Sprint_Loop";

    private static readonly EnemyDefinition[] Enemies =
    {
        new EnemyDefinition(
            "Enemy_FatMan",
            "Assets/JH/Model/Prefab/Enemy_FatMan.prefab",
            "Assets/JH/Model/Enemy/Fat_Throw/던짐_스킨/Builder_Bob_0811155314_texture@Goalie Throw.fbx",
            "Assets/JH/Model/Enemy/Fat_Throw/던짐_스킨/Builder_Bob_0811155314_texture@Goalie Throw 1.fbx",
            "Fatman Idle",
            "Assets/JH/Model/Enemy/Fat_Throw/던짐_스킨/Builder_Bob_0811155314_texture@Goalie Throw.fbx",
            "Fatman Act",
            "Assets/JH/Model/Enemy/Fat_Throw/죽음_노스킨/Builder_Bob_0811155314_texture@Flying Back Death.fbx",
            "Fatman Die"),
        new EnemyDefinition(
            "Enemy_Guard",
            "Assets/JH/Model/Prefab/Enemy_Guard.prefab",
            "Assets/JH/Model/Enemy/Garden_Spear/Shoot_Skin.fbx",
            "Assets/JH/Model/Enemy/Garden_Spear/Shoot_Skin(Idle).fbx",
            "guard idle",
            "Assets/JH/Model/Enemy/Garden_Spear/Shoot_Skin.fbx",
            "guard act",
            "Assets/JH/Model/Enemy/Garden_Spear/Die_NoSkin.fbx",
            "guard die"),
        new EnemyDefinition(
            "Enemy_OldMan",
            "Assets/JH/Model/Prefab/Enemy_OldMan.prefab",
            "Assets/JH/Model/Enemy/Oldman_Shovel/Action_Skin/Elderly_Cartoon_Chara_0815100721_texture@Sword And Shield Attack.fbx",
            "Assets/JH/Model/Enemy/Oldman_Shovel/Action_Skin/Elderly_Cartoon_Chara_0815100721_texture@Sword And Shield Attack.fbx",
            "old man act",
            "Assets/JH/Model/Enemy/Oldman_Shovel/Action_Skin/Elderly_Cartoon_Chara_0815100721_texture@Sword And Shield Attack.fbx",
            "old man act",
            "Assets/JH/Model/Enemy/Oldman_Shovel/Die_NoSkin/Elderly_Cartoon_Chara_0815100721_texture@Flying Back Death.fbx",
            "old man die"),
        new EnemyDefinition(
            "Enemy_Woman",
            "Assets/JH/Model/Prefab/Enemy_Woman.prefab",
            "Assets/JH/Model/Enemy/Woman_Boss/휘두르기_스킨/Baker_in_Apron_0815171726_texture@Stable Sword Outward Slash.fbx",
            "Assets/JH/Model/Enemy/Woman_Boss/휘두르기_스킨/Baker_in_Apron_0815171726_texture@Stable Sword Outward Slash.fbx",
            "Woman Act",
            "Assets/JH/Model/Enemy/Woman_Boss/휘두르기_스킨/Baker_in_Apron_0815171726_texture@Stable Sword Outward Slash.fbx",
            "Woman Act",
            "Assets/JH/Model/Enemy/Woman_Boss/죽기_노스킨/Baker_in_Apron_0815171726_texture@Sword And Shield Death.fbx",
            "Woman Die"),
        new EnemyDefinition(
            "Enemy_YllowMan",
            "Assets/JH/Model/Prefab/Enemy_YllowMan.prefab",
            "Assets/JH/Model/Enemy/YellowMan_Web/Web_Skin/Bearded_Builder_in_Ye_0811162114_texture@Fishing Idle.fbx",
            "Assets/JH/Model/Enemy/YellowMan_Web/Web_Skin/Bearded_Builder_in_Ye_0811162114_texture@Fishing Idle.fbx",
            "bearman act",
            "Assets/JH/Model/Enemy/YellowMan_Web/Web_Skin/Bearded_Builder_in_Ye_0811162114_texture@Fishing Idle.fbx",
            "bearman act",
            "Assets/JH/Model/Enemy/YellowMan_Web/Die_NoSkin/Bearded_Builder_in_Ye_0811162114_texture@Flying Back Death.fbx",
            "bearman die")
    };

    private static readonly string[] HumanoidModelPaths = Enemies
        .SelectMany(enemy => new[]
        {
            enemy.AvatarModelPath,
            enemy.IdleModelPath,
            enemy.AttackModelPath,
            enemy.DieModelPath
        })
        .Append(ExternalLocomotionModelPath)
        .Distinct()
        .ToArray();

    [MenuItem(MenuPath, false, 2320)]
    public static void Configure()
    {
        EnsureHumanoidImports();
        EnsureFolder(SharedRoot);
        EnsureFolder(OverridesRoot);

        AnimationClip idleTemplate = GetOrCreateTemplateClip(IdleTemplatePath);
        AnimationClip attackLoopTemplate =
            GetOrCreateTemplateClip(AttackLoopTemplatePath);
        AnimationClip walkTemplate = GetOrCreateTemplateClip(WalkTemplatePath);
        AnimationClip runTemplate = GetOrCreateTemplateClip(RunTemplatePath);
        AnimationClip dieTemplate = GetOrCreateTemplateClip(DieTemplatePath);
        AnimationClip attackOnceTemplate =
            GetOrCreateTemplateClip(AttackOnceTemplatePath);
        ValidatePreservedLocomotionClip();
        AnimationClip walkClip = LoadClip(
            ExternalLocomotionModelPath,
            ExternalWalkClipName);
        AnimationClip runClip = LoadClip(
            ExternalLocomotionModelPath,
            ExternalRunClipName);
        AnimatorController sharedController = ConfigureSharedController(
            idleTemplate,
            attackLoopTemplate,
            walkTemplate,
            runTemplate,
            dieTemplate,
            attackOnceTemplate);

        foreach (EnemyDefinition enemy in Enemies)
        {
            AnimationClip idleClip = LoadClip(enemy.IdleModelPath, enemy.IdleClipName);
            AnimationClip attackClip = LoadClip(enemy.AttackModelPath, enemy.AttackClipName);
            AnimationClip dieClip = LoadClip(enemy.DieModelPath, enemy.DieClipName);
            Avatar avatar = LoadValidHumanoidAvatar(enemy.AvatarModelPath);
            var animationOverrides =
                new Dictionary<AnimationClip, AnimationClip>
                {
                    [idleTemplate] = idleClip,
                    [attackLoopTemplate] = attackClip,
                    [walkTemplate] = walkClip,
                    [runTemplate] = runClip,
                    [dieTemplate] = dieClip,
                    [attackOnceTemplate] = attackClip
                };
            AnimatorOverrideController overrideController = ConfigureOverrideController(
                enemy,
                sharedController,
                animationOverrides);

            ConfigurePrefab(enemy.PrefabPath, avatar, overrideController);
        }

        AssetDatabase.SaveAssets();
        Debug.Log(
            "[Forward Enemy Animator] Configured shared idle/attack_loop/walk/run/" +
            "die/attack_once states with Quaternius walk/run locomotion, " +
            "five Humanoid avatars, and five character override controllers.");
    }

    private static void EnsureHumanoidImports()
    {
        foreach (string modelPath in HumanoidModelPaths)
        {
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing ModelImporter: {modelPath}");

            bool requiresReimport =
                importer.animationType != ModelImporterAnimationType.Human ||
                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;
            ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations;
            if (clipAnimations.Length == 0)
                clipAnimations = importer.defaultClipAnimations;

            bool clipSettingsChanged = false;
            HashSet<string> loopingIdleClipNames = Enemies
                .Where(enemy => string.Equals(
                    enemy.IdleModelPath,
                    modelPath,
                    StringComparison.Ordinal))
                .Select(enemy => enemy.IdleClipName)
                .ToHashSet(StringComparer.Ordinal);
            foreach (ModelImporterClipAnimation clip in clipAnimations)
            {
                if (!loopingIdleClipNames.Contains(clip.name) || clip.loopTime)
                    continue;

                clip.loopTime = true;
                clipSettingsChanged = true;
            }

            if (string.Equals(
                    modelPath,
                    ExternalLocomotionModelPath,
                    StringComparison.Ordinal))
            {
                ModelImporterClipAnimation[] selectedLocomotionClips =
                    clipAnimations
                        .Where(clip =>
                            string.Equals(
                                clip.name,
                                ExternalWalkClipName,
                                StringComparison.Ordinal) ||
                            string.Equals(
                                clip.name,
                                ExternalRunClipName,
                                StringComparison.Ordinal))
                        .ToArray();
                if (selectedLocomotionClips.Length != 2)
                {
                    throw new InvalidOperationException(
                        "Quaternius source must contain the selected walk and run clips.");
                }

                bool selectionChanged =
                    clipAnimations.Length != selectedLocomotionClips.Length ||
                    !clipAnimations.Select(clip => clip.name).SequenceEqual(
                        selectedLocomotionClips.Select(clip => clip.name));
                clipAnimations = selectedLocomotionClips;
                clipSettingsChanged |= selectionChanged;

                foreach (ModelImporterClipAnimation clip in clipAnimations)
                {
                    if (clip.loopTime)
                        continue;

                    clip.loopTime = true;
                    clipSettingsChanged = true;
                }
            }

            requiresReimport |= clipSettingsChanged;
            if (!requiresReimport)
                continue;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            if (clipSettingsChanged)
                importer.clipAnimations = clipAnimations;
            importer.SaveAndReimport();
        }
    }

    private static AnimatorController ConfigureSharedController(
        AnimationClip idleTemplate,
        AnimationClip attackLoopTemplate,
        AnimationClip walkTemplate,
        AnimationClip runTemplate,
        AnimationClip dieTemplate,
        AnimationClip attackOnceTemplate)
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(SharedControllerPath);
        if (controller == null)
        {
            controller =
                AnimatorController.CreateAnimatorControllerAtPath(SharedControllerPath);
        }
        else if (SharedControllerMatchesContract(
                     controller,
                     idleTemplate,
                     attackLoopTemplate,
                     walkTemplate,
                     runTemplate,
                     dieTemplate,
                     attackOnceTemplate))
        {
            return controller;
        }

        while (controller.layers.Length > 1)
            controller.RemoveLayer(controller.layers.Length - 1);

        while (controller.parameters.Length > 0)
            controller.RemoveParameter(controller.parameters.Length - 1);

        AnimatorControllerLayer layer = controller.layers[0];
        layer.name = "Base Layer";
        layer.defaultWeight = 1f;
        controller.layers = new[] { layer };

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
            stateMachine.RemoveAnyStateTransition(transition);
        foreach (AnimatorTransition transition in stateMachine.entryTransitions)
            stateMachine.RemoveEntryTransition(transition);
        foreach (ChildAnimatorState childState in stateMachine.states)
            stateMachine.RemoveState(childState.state);
        foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
            stateMachine.RemoveStateMachine(childStateMachine.stateMachine);

        AnimatorState idle = stateMachine.AddState(
            ForwardEnemyAnimationContract.Idle,
            new Vector3(280f, 60f));
        AnimatorState attackLoop =
            stateMachine.AddState(
                ForwardEnemyAnimationContract.AttackLoop,
                new Vector3(500f, 0f));
        AnimatorState walk = stateMachine.AddState(
            ForwardEnemyAnimationContract.Walk,
            new Vector3(500f, 80f));
        AnimatorState run = stateMachine.AddState(
            ForwardEnemyAnimationContract.Run,
            new Vector3(500f, 160f));
        AnimatorState die = stateMachine.AddState(
            ForwardEnemyAnimationContract.Die,
            new Vector3(720f, 0f));
        AnimatorState attackOnce =
            stateMachine.AddState(
                ForwardEnemyAnimationContract.AttackOnce,
                new Vector3(720f, 100f));
        idle.motion = idleTemplate;
        attackLoop.motion = attackLoopTemplate;
        walk.motion = walkTemplate;
        walk.speed = 1f;
        run.motion = runTemplate;
        die.motion = dieTemplate;
        attackOnce.motion = attackOnceTemplate;
        stateMachine.defaultState = idle;

        AnimatorStateTransition attackLoopTransition =
            attackLoop.AddTransition(attackLoop);
        attackLoopTransition.hasExitTime = true;
        attackLoopTransition.exitTime = 1f;
        attackLoopTransition.duration = 0f;

        AnimatorStateTransition idleTransition = attackOnce.AddTransition(idle);
        idleTransition.hasExitTime = true;
        idleTransition.exitTime = 1f;
        idleTransition.duration = 0.05f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static bool SharedControllerMatchesContract(
        AnimatorController controller,
        AnimationClip idleTemplate,
        AnimationClip attackLoopTemplate,
        AnimationClip walkTemplate,
        AnimationClip runTemplate,
        AnimationClip dieTemplate,
        AnimationClip attackOnceTemplate)
    {
        if (controller.layers.Length != 1 ||
            controller.parameters.Length != 0)
        {
            return false;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        if (stateMachine.states.Length != 6 ||
            stateMachine.anyStateTransitions.Length != 0 ||
            stateMachine.entryTransitions.Length != 0)
        {
            return false;
        }

        Dictionary<string, AnimatorState> states = stateMachine.states
            .ToDictionary(childState => childState.state.name, childState => childState.state);
        if (!states.TryGetValue(
                ForwardEnemyAnimationContract.Idle,
                out AnimatorState idle) ||
            !states.TryGetValue(
                ForwardEnemyAnimationContract.AttackLoop,
                out AnimatorState attackLoop) ||
            !states.TryGetValue(
                ForwardEnemyAnimationContract.Walk,
                out AnimatorState walk) ||
            !states.TryGetValue(
                ForwardEnemyAnimationContract.Run,
                out AnimatorState run) ||
            !states.TryGetValue(
                ForwardEnemyAnimationContract.Die,
                out AnimatorState die) ||
            !states.TryGetValue(
                ForwardEnemyAnimationContract.AttackOnce,
                out AnimatorState attackOnce) ||
            idle.motion != idleTemplate ||
            attackLoop.motion != attackLoopTemplate ||
            walk.motion != walkTemplate ||
            !Mathf.Approximately(walk.speed, 1f) ||
            run.motion != runTemplate ||
            die.motion != dieTemplate ||
            attackOnce.motion != attackOnceTemplate ||
            stateMachine.defaultState != idle ||
            idle.transitions.Length != 0 ||
            attackLoop.transitions.Length != 1 ||
            attackLoop.transitions[0].destinationState != attackLoop ||
            !attackLoop.transitions[0].hasExitTime ||
            !Mathf.Approximately(attackLoop.transitions[0].exitTime, 1f) ||
            walk.transitions.Length != 0 ||
            run.transitions.Length != 0 ||
            die.transitions.Length != 0 ||
            attackOnce.transitions.Length != 1 ||
            attackOnce.transitions[0].destinationState != idle ||
            !attackOnce.transitions[0].hasExitTime ||
            !Mathf.Approximately(attackOnce.transitions[0].exitTime, 1f))
        {
            return false;
        }

        return true;
    }

    private static AnimatorOverrideController ConfigureOverrideController(
        EnemyDefinition enemy,
        AnimatorController sharedController,
        IReadOnlyDictionary<AnimationClip, AnimationClip> animationOverrides)
    {
        string overridePath =
            $"{OverridesRoot}/{enemy.AssetName}.overrideController";
        AnimatorOverrideController overrideController =
            AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);

        bool controllerChanged = false;
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(sharedController);
            AssetDatabase.CreateAsset(overrideController, overridePath);
            controllerChanged = true;
        }
        else if (overrideController.runtimeAnimatorController != sharedController)
        {
            overrideController.runtimeAnimatorController = sharedController;
            controllerChanged = true;
        }

        bool staleEntriesRemoved =
            RemoveMissingOverrideEntries(overrideController);

        var overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>(
                overrideController.overridesCount);
        overrideController.GetOverrides(overrides);

        bool overridesChanged = false;
        for (int index = 0; index < overrides.Count; index++)
        {
            AnimationClip original = overrides[index].Key;
            if (!animationOverrides.TryGetValue(
                    original,
                    out AnimationClip replacement))
            {
                throw new InvalidOperationException(
                    $"Unexpected shared animation slot '{original.name}'.");
            }

            if (overrides[index].Value == replacement)
                continue;

            overrides[index] =
                new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
            overridesChanged = true;
        }

        if (overridesChanged)
            overrideController.ApplyOverrides(overrides);
        if (controllerChanged || overridesChanged || staleEntriesRemoved)
            EditorUtility.SetDirty(overrideController);
        return overrideController;
    }

    private static bool RemoveMissingOverrideEntries(
        AnimatorOverrideController overrideController)
    {
        var serializedController = new SerializedObject(overrideController);
        SerializedProperty clips = serializedController.FindProperty("m_Clips");
        if (clips == null || !clips.isArray)
            return false;

        bool removed = false;
        for (int index = clips.arraySize - 1; index >= 0; index--)
        {
            SerializedProperty originalClip = clips
                .GetArrayElementAtIndex(index)
                .FindPropertyRelative("m_OriginalClip");
            if (originalClip != null &&
                originalClip.objectReferenceValue != null)
            {
                continue;
            }

            clips.DeleteArrayElementAtIndex(index);
            removed = true;
        }

        if (removed)
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        return removed;
    }

    private static void ConfigurePrefab(
        string prefabPath,
        Avatar avatar,
        AnimatorOverrideController overrideController)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            GameObject[] nestedPrefabRoots = prefabRoot
                .GetComponentsInChildren<Transform>(true)
                .Select(transform => transform.gameObject)
                .Where(PrefabUtility.IsAnyPrefabInstanceRoot)
                .ToArray();
            var originalModificationKeys =
                nestedPrefabRoots.ToDictionary(
                    nestedRoot => nestedRoot,
                    nestedRoot => new HashSet<string>(
                        (PrefabUtility.GetPropertyModifications(nestedRoot) ??
                         Array.Empty<PropertyModification>())
                        .Select(GetModificationKey)));

            Animator[] configuredAnimators = prefabRoot
                .GetComponentsInChildren<Animator>(true)
                .Where(animator =>
                    animator.avatar != null ||
                    animator.runtimeAnimatorController != null)
                .ToArray();
            if (configuredAnimators.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{prefabPath} must contain exactly one configured Animator, " +
                    $"but found {configuredAnimators.Length}.");
            }

            Animator animator = configuredAnimators[0];
            bool requiresSave =
                animator.avatar != avatar ||
                animator.runtimeAnimatorController != overrideController ||
                animator.applyRootMotion ||
                !animator.enabled;
            if (!requiresSave)
                return;

            animator.avatar = avatar;
            animator.runtimeAnimatorController = overrideController;
            animator.applyRootMotion = false;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            if (PrefabUtility.IsPartOfPrefabInstance(animator))
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            foreach (GameObject nestedRoot in nestedPrefabRoots)
            {
                PropertyModification[] modifications =
                    PrefabUtility.GetPropertyModifications(nestedRoot) ??
                    Array.Empty<PropertyModification>();
                PropertyModification[] filteredModifications = modifications
                    .Where(modification =>
                        originalModificationKeys[nestedRoot].Contains(
                            GetModificationKey(modification)) ||
                        !IsTransientHumanoidPoseOverride(modification))
                    .ToArray();
                PrefabUtility.SetPropertyModifications(
                    nestedRoot,
                    filteredModifications);
            }

            if (PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath) == null)
                throw new InvalidOperationException($"Failed to save prefab: {prefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static string GetModificationKey(PropertyModification modification)
    {
        int targetInstanceId =
            modification.target != null ? modification.target.GetInstanceID() : 0;
        return $"{targetInstanceId}:{modification.propertyPath}";
    }

    private static bool IsTransientHumanoidPoseOverride(
        PropertyModification modification)
    {
        if (!(modification.target is Transform))
            return false;

        return modification.propertyPath.StartsWith(
                   "m_LocalRotation.",
                   StringComparison.Ordinal) ||
               modification.propertyPath.StartsWith(
                   "m_LocalEulerAnglesHint.",
                   StringComparison.Ordinal);
    }

    private static AnimationClip GetOrCreateTemplateClip(string assetPath)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (clip != null)
            return clip;

        clip = new AnimationClip
        {
            name = System.IO.Path.GetFileNameWithoutExtension(assetPath),
            frameRate = 30f
        };
        AssetDatabase.CreateAsset(clip, assetPath);
        return clip;
    }

    private static AnimationClip LoadClip(string modelPath, string clipName)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate =>
                !candidate.name.StartsWith("__preview__", StringComparison.Ordinal) &&
                string.Equals(candidate.name, clipName, StringComparison.Ordinal));
        if (clip == null)
            throw new InvalidOperationException(
                $"Missing animation clip '{clipName}' at {modelPath}.");

        if (!clip.isHumanMotion)
            throw new InvalidOperationException(
                $"Animation clip '{clipName}' is not Humanoid motion: {modelPath}");

        return clip;
    }

    private static void ValidatePreservedLocomotionClip()
    {
        AnimationClip clip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(
                CommonLocomotionClipPath);
        if (clip == null || !clip.isHumanMotion || !clip.isLooping)
        {
            throw new InvalidOperationException(
                "Missing looping Humanoid ForwardEnemy_Locomotion.anim asset.");
        }
    }

    private static Avatar LoadValidHumanoidAvatar(string modelPath)
    {
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<Avatar>()
            .FirstOrDefault(candidate => candidate.isHuman && candidate.isValid);
        if (avatar == null)
            throw new InvalidOperationException(
                $"Missing valid Humanoid Avatar at {modelPath}.");

        return avatar;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string parent = System.IO.Path.GetDirectoryName(assetPath)
            ?.Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(assetPath);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            throw new InvalidOperationException($"Invalid asset folder: {assetPath}");

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private sealed class EnemyDefinition
    {
        public EnemyDefinition(
            string assetName,
            string prefabPath,
            string avatarModelPath,
            string idleModelPath,
            string idleClipName,
            string attackModelPath,
            string attackClipName,
            string dieModelPath,
            string dieClipName)
        {
            AssetName = assetName;
            PrefabPath = prefabPath;
            AvatarModelPath = avatarModelPath;
            IdleModelPath = idleModelPath;
            IdleClipName = idleClipName;
            AttackModelPath = attackModelPath;
            AttackClipName = attackClipName;
            DieModelPath = dieModelPath;
            DieClipName = dieClipName;
        }

        public string AssetName { get; }
        public string PrefabPath { get; }
        public string AvatarModelPath { get; }
        public string IdleModelPath { get; }
        public string IdleClipName { get; }
        public string AttackModelPath { get; }
        public string AttackClipName { get; }
        public string DieModelPath { get; }
        public string DieClipName { get; }
    }
}
#endif
