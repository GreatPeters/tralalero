#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
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
    internal const string AttackTemplatePath =
        SharedRoot + "/ForwardEnemy_Attack.anim";
    internal const string DieTemplatePath =
        SharedRoot + "/ForwardEnemy_Die.anim";
    internal const string OverridesRoot =
        SharedRoot + "/Overrides";

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
        .Distinct()
        .ToArray();

    [MenuItem(MenuPath, false, 2320)]
    public static void Configure()
    {
        EnsureHumanoidImports();
        EnsureFolder(SharedRoot);
        EnsureFolder(OverridesRoot);

        AnimationClip idleTemplate = GetOrCreateTemplateClip(IdleTemplatePath);
        AnimationClip attackTemplate = GetOrCreateTemplateClip(AttackTemplatePath);
        AnimationClip dieTemplate = GetOrCreateTemplateClip(DieTemplatePath);
        AnimatorController sharedController = ConfigureSharedController(
            idleTemplate,
            attackTemplate,
            dieTemplate);

        foreach (EnemyDefinition enemy in Enemies)
        {
            AnimationClip idleClip = LoadClip(enemy.IdleModelPath, enemy.IdleClipName);
            AnimationClip attackClip = LoadClip(enemy.AttackModelPath, enemy.AttackClipName);
            AnimationClip dieClip = LoadClip(enemy.DieModelPath, enemy.DieClipName);
            Avatar avatar = LoadValidHumanoidAvatar(enemy.AvatarModelPath);
            AnimatorOverrideController overrideController = ConfigureOverrideController(
                enemy,
                sharedController,
                idleTemplate,
                idleClip,
                attackTemplate,
                attackClip,
                dieTemplate,
                dieClip);

            ConfigurePrefab(enemy.PrefabPath, avatar, overrideController);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[Forward Enemy Animator] Configured one shared Idle/Attack/Die controller, " +
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
            if (!requiresReimport)
                continue;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
        }
    }

    private static AnimatorController ConfigureSharedController(
        AnimationClip idleTemplate,
        AnimationClip attackTemplate,
        AnimationClip dieTemplate)
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
                     attackTemplate,
                     dieTemplate))
        {
            return controller;
        }

        while (controller.layers.Length > 1)
            controller.RemoveLayer(controller.layers.Length - 1);

        while (controller.parameters.Length > 0)
            controller.RemoveParameter(controller.parameters.Length - 1);

        controller.AddParameter("act", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("die", AnimatorControllerParameterType.Trigger);

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

        AnimatorState idle = stateMachine.AddState("Idle", new Vector3(300f, 100f));
        AnimatorState attack = stateMachine.AddState("Attack", new Vector3(520f, 100f));
        AnimatorState die = stateMachine.AddState("Die", new Vector3(520f, -20f));
        idle.motion = idleTemplate;
        attack.motion = attackTemplate;
        die.motion = dieTemplate;
        stateMachine.defaultState = idle;

        AnimatorStateTransition attackTransition = idle.AddTransition(attack);
        attackTransition.hasExitTime = false;
        attackTransition.duration = 0.05f;
        attackTransition.AddCondition(AnimatorConditionMode.If, 0f, "act");

        AnimatorStateTransition idleTransition = attack.AddTransition(idle);
        idleTransition.hasExitTime = true;
        idleTransition.exitTime = 1f;
        idleTransition.duration = 0.05f;

        AnimatorStateTransition dieTransition =
            stateMachine.AddAnyStateTransition(die);
        dieTransition.hasExitTime = false;
        dieTransition.duration = 0.05f;
        dieTransition.canTransitionToSelf = false;
        dieTransition.AddCondition(AnimatorConditionMode.If, 0f, "die");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static bool SharedControllerMatchesContract(
        AnimatorController controller,
        AnimationClip idleTemplate,
        AnimationClip attackTemplate,
        AnimationClip dieTemplate)
    {
        if (controller.layers.Length != 1 ||
            controller.parameters.Length != 2 ||
            controller.parameters.Any(parameter =>
                parameter.type != AnimatorControllerParameterType.Trigger) ||
            !controller.parameters.Select(parameter => parameter.name)
                .OrderBy(name => name)
                .SequenceEqual(new[] { "act", "die" }))
        {
            return false;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        if (stateMachine.states.Length != 3)
            return false;

        Dictionary<string, AnimatorState> states = stateMachine.states
            .ToDictionary(childState => childState.state.name, childState => childState.state);
        if (!states.TryGetValue("Idle", out AnimatorState idle) ||
            !states.TryGetValue("Attack", out AnimatorState attack) ||
            !states.TryGetValue("Die", out AnimatorState die) ||
            idle.motion != idleTemplate ||
            attack.motion != attackTemplate ||
            die.motion != dieTemplate ||
            stateMachine.defaultState != idle ||
            idle.transitions.Length != 1 ||
            idle.transitions[0].destinationState != attack ||
            !HasOnlyCondition(idle.transitions[0], "act") ||
            attack.transitions.Length != 1 ||
            attack.transitions[0].destinationState != idle ||
            !attack.transitions[0].hasExitTime ||
            die.transitions.Length != 0 ||
            stateMachine.anyStateTransitions.Length != 1 ||
            stateMachine.anyStateTransitions[0].destinationState != die ||
            !HasOnlyCondition(stateMachine.anyStateTransitions[0], "die"))
        {
            return false;
        }

        return true;
    }

    private static bool HasOnlyCondition(
        AnimatorStateTransition transition,
        string parameterName)
    {
        return transition.conditions.Length == 1 &&
               string.Equals(
                   transition.conditions[0].parameter,
                   parameterName,
                   StringComparison.Ordinal);
    }

    private static AnimatorOverrideController ConfigureOverrideController(
        EnemyDefinition enemy,
        AnimatorController sharedController,
        AnimationClip idleTemplate,
        AnimationClip idleClip,
        AnimationClip attackTemplate,
        AnimationClip attackClip,
        AnimationClip dieTemplate,
        AnimationClip dieClip)
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

        var overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>(
                overrideController.overridesCount);
        overrideController.GetOverrides(overrides);

        bool overridesChanged = false;
        for (int index = 0; index < overrides.Count; index++)
        {
            AnimationClip original = overrides[index].Key;
            AnimationClip replacement;
            if (original == idleTemplate)
                replacement = idleClip;
            else if (original == attackTemplate)
                replacement = attackClip;
            else if (original == dieTemplate)
                replacement = dieClip;
            else
                throw new InvalidOperationException(
                    $"Unexpected shared animation slot '{original.name}'.");

            if (overrides[index].Value == replacement)
                continue;

            overrides[index] =
                new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
            overridesChanged = true;
        }

        if (overridesChanged)
            overrideController.ApplyOverrides(overrides);
        if (controllerChanged || overridesChanged)
            EditorUtility.SetDirty(overrideController);
        return overrideController;
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
