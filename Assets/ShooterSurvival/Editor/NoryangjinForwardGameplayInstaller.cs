#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using IndianOceanAssets.ShooterSurvival.Analytics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class NoryangjinForwardGameplayInstaller
{
    internal const string SourceScenePath = "Assets/ShooterSurvival/Demo Scenes/Forward March Mode.unity";
    internal const string TargetScenePath = NoryangjinMapToolWindow.MapToolScenePath;
    internal const string RuntimePlayerName = "Noryangjin_Player";
    internal const string OriginalProjectileMuzzleName = "ProjectileMuzzle";
    internal const float OriginalProjectileMuzzleForwardOffset = 0.35f;

    [MenuItem("Tools/맵 제작 도구/노량진 맵 제작/게임플레이/Forward 기능 연결", false, 2310)]
    public static void InstallIntoOpenNoryangjinScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Noryangjin Gameplay] 플레이 모드에서는 씬 구성을 변경할 수 없습니다.");
            return;
        }

        Scene targetScene = SceneManager.GetActiveScene();
        if (!targetScene.IsValid() ||
            !string.Equals(targetScene.path, TargetScenePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError($"[Noryangjin Gameplay] 먼저 대상 씬을 여세요: {TargetScenePath}");
            return;
        }

        PlayerScript existingPlayer = FindInScene<PlayerScript>(targetScene);
        GameObject original = FindOriginalVisual(targetScene, existingPlayer);
        if (original == null && existingPlayer == null)
        {
            Debug.LogError("[Noryangjin Gameplay] 보존할 Original 캐릭터를 찾지 못했습니다.");
            return;
        }

        Scene sourceScene = SceneManager.GetSceneByPath(SourceScenePath);
        bool openedSourceScene = !sourceScene.IsValid() || !sourceScene.isLoaded;
        EditorBuildSettingsScene[] buildSettingsBeforeInstall = EditorBuildSettings.scenes;
        bool buildSettingsUpdated = false;
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Install Forward Gameplay Into Noryangjin");

        try
        {
            if (openedSourceScene)
                sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

            ValidateSourceScene(sourceScene);

            Camera targetCamera = FindMainCamera(targetScene);
            if (targetCamera == null)
                throw new InvalidOperationException("Original 아래의 MainCamera를 찾지 못했습니다.");

            PlayerScript player = EnsurePlayer(sourceScene, targetScene, original);
            EnsureManagers(sourceScene, targetScene);
            EnsureAnalyticsSceneContext(targetScene);
            EnsureChapterEnemyStats(targetScene);
            CanvasScript canvas = EnsureCanvas(sourceScene, targetScene);
            EnsureEventSystem(sourceScene, targetScene);
            EnsureUpgradeServices(sourceScene, targetScene);

            RebindCanvasCamera(canvas, targetCamera);
            ValidateInstalledScene(targetScene, player, canvas);
            RecordBuildSettingsForUndo();
            EnsureTargetSceneInBuildSettings();
            buildSettingsUpdated = true;

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(targetScene);
            if (!EditorSceneManager.SaveScene(targetScene))
                throw new InvalidOperationException("노량진 씬 저장에 실패했습니다.");

            Selection.activeGameObject = player.gameObject;
            Debug.Log(
                "[Noryangjin Gameplay] Original 비주얼을 보존한 Forward 이동/공격 리그, 시작 UI, 상점 UI 연결을 완료했습니다.");
        }
        catch (Exception exception)
        {
            if (buildSettingsUpdated)
                EditorBuildSettings.scenes = buildSettingsBeforeInstall;

            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
        }
        finally
        {
            if (openedSourceScene && sourceScene.IsValid() && sourceScene.isLoaded)
                EditorSceneManager.CloseScene(sourceScene, true);
        }
    }

    private static void ValidateSourceScene(Scene sourceScene)
    {
        var missing = new List<string>();
        if (FindInScene<PlayerScript>(sourceScene) == null)
            missing.Add("Player_fwdMode");
        if (FindInScene<CanvasScript>(sourceScene) == null)
            missing.Add("Canvas");
        if (FindInScene<TimeManager>(sourceScene) == null)
            missing.Add("Managers/TimeManager");
        if (FindInScene<SettingsManager>(sourceScene) == null)
            missing.Add("Managers/SettingsManager");
        if (FindInScene<BulletPooler>(sourceScene) == null)
            missing.Add("Managers/BulletPooler");
        if (FindInScene<EventSystem>(sourceScene) == null)
            missing.Add("EventSystem");
        if (FindInScene<UpgradeStatManager>(sourceScene) == null)
            missing.Add("UpgradeStatManager");
        GameManager sourceGameManager = FindInScene<GameManager>(sourceScene);
        if (sourceGameManager == null)
        {
            missing.Add("GameManager");
        }
        else
        {
            if (sourceGameManager.extraHelp_TungTungTung == null)
                missing.Add("GameManager/extraHelp_TungTungTung");
            if (sourceGameManager.extraHelp_BoomBarDino == null)
                missing.Add("GameManager/extraHelp_BoomBarDino");
        }

        if (missing.Count > 0)
            throw new InvalidOperationException($"Forward 씬 필수 구성이 없습니다: {string.Join(", ", missing)}");
    }

    private static PlayerScript EnsurePlayer(
        Scene sourceScene,
        Scene targetScene,
        GameObject original)
    {
        PlayerScript player = FindInScene<PlayerScript>(targetScene);
        bool created = player == null;
        if (created)
        {
            PlayerScript sourcePlayer = FindInScene<PlayerScript>(sourceScene);
            GameObject clone = CloneIntoScene(
                sourcePlayer.transform.root.gameObject,
                targetScene,
                RuntimePlayerName);
            player = clone.GetComponent<PlayerScript>();
            if (player == null)
                throw new InvalidOperationException("복제한 플레이어에 PlayerScript가 없습니다.");
        }

        if (original == null)
            original = FindOriginalVisual(targetScene, player);

        if (original == null)
            throw new InvalidOperationException("Original 캐릭터를 찾지 못했습니다.");

        Transform playerTransform = player.transform;
        if (created)
        {
            Undo.RecordObject(playerTransform, "Align Noryangjin Player");
            playerTransform.position = original.transform.position;
            playerTransform.rotation = original.transform.rotation;
        }

        Undo.RecordObject(player.gameObject, "Configure Noryangjin Player");
        player.gameObject.name = RuntimePlayerName;
        player.gameObject.tag = "Player";

        if (!original.transform.IsChildOf(playerTransform))
            Undo.SetTransformParent(original.transform, playerTransform, "Attach Original Visual To Player");

        EnsureOriginalAnimator(player.gameObject, original);
        EnsureOriginalProjectileMuzzle(player.gameObject, original);
        RestoreOriginalVisualRenderers(original);
        HideForwardPlayerRenderers(player.gameObject, original.transform);
        EditorUtility.SetDirty(player.gameObject);
        EditorUtility.SetDirty(original);
        return player;
    }

    internal static Animator EnsureOriginalAnimator(
        GameObject playerRoot,
        GameObject originalVisual)
    {
        if (playerRoot == null)
            throw new ArgumentNullException(nameof(playerRoot));
        if (originalVisual == null)
            throw new ArgumentNullException(nameof(originalVisual));

        Transform sourceTransform = playerRoot.transform.Find("Sharks/Original");
        Animator sourceAnimator =
            sourceTransform != null ? sourceTransform.GetComponent<Animator>() : null;
        if (sourceAnimator == null)
        {
            throw new InvalidOperationException(
                "Forward player source Animator was not found at 'Sharks/Original'.");
        }

        if (sourceAnimator.runtimeAnimatorController == null)
        {
            throw new InvalidOperationException(
                "Forward player source Animator at 'Sharks/Original' has no controller.");
        }

        Animator originalAnimator = originalVisual.GetComponent<Animator>();
        if (originalAnimator == null)
            originalAnimator = Undo.AddComponent<Animator>(originalVisual);

        Undo.RecordObject(originalAnimator, "Configure Noryangjin Original Animator");
        originalAnimator.runtimeAnimatorController =
            sourceAnimator.runtimeAnimatorController;
        originalAnimator.avatar = sourceAnimator.avatar;
        originalAnimator.applyRootMotion = sourceAnimator.applyRootMotion;
        originalAnimator.updateMode = sourceAnimator.updateMode;
        originalAnimator.cullingMode = sourceAnimator.cullingMode;
        originalAnimator.enabled = true;
        EditorUtility.SetDirty(originalAnimator);
        return originalAnimator;
    }

    internal static Transform EnsureOriginalProjectileMuzzle(
        GameObject playerRoot,
        GameObject originalVisual)
    {
        if (playerRoot == null)
            throw new ArgumentNullException(nameof(playerRoot));
        if (originalVisual == null)
            throw new ArgumentNullException(nameof(originalVisual));

        Transform mouth = originalVisual
            .GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate =>
                string.Equals(candidate.name, "headend", StringComparison.Ordinal));
        if (mouth == null)
        {
            throw new InvalidOperationException(
                "The visible Original character has no 'headend' mouth bone.");
        }

        Transform muzzle = mouth.Find(OriginalProjectileMuzzleName);
        if (muzzle == null)
        {
            var muzzleObject = new GameObject(OriginalProjectileMuzzleName);
            Undo.RegisterCreatedObjectUndo(
                muzzleObject,
                "Create Noryangjin Original Projectile Muzzle");
            muzzle = muzzleObject.transform;
            Undo.SetTransformParent(
                muzzle,
                mouth,
                "Attach Noryangjin Original Projectile Muzzle");
        }

        Undo.RecordObject(muzzle, "Align Noryangjin Original Projectile Muzzle");
        Vector3 forward = playerRoot.transform.forward.normalized;
        muzzle.SetPositionAndRotation(
            mouth.position + forward * OriginalProjectileMuzzleForwardOffset,
            Quaternion.FromToRotation(Vector3.up, forward));
        EditorUtility.SetDirty(muzzle);
        return muzzle;
    }

    internal static void RestoreOriginalVisualRenderers(GameObject originalVisual)
    {
        foreach (Renderer renderer in originalVisual.GetComponentsInChildren<Renderer>(true))
        {
            Renderer prefabRenderer =
                PrefabUtility.GetCorrespondingObjectFromSource(renderer) as Renderer;
            bool shouldBeVisible = prefabRenderer == null || prefabRenderer.enabled;
            if (!shouldBeVisible || renderer.enabled)
                continue;

            Undo.RecordObject(renderer, "Restore Noryangjin Original Visual");
            renderer.enabled = true;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void HideForwardPlayerRenderers(GameObject playerRoot, Transform originalVisual)
    {
        foreach (Renderer renderer in playerRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (originalVisual != null && renderer.transform.IsChildOf(originalVisual))
                continue;

            if (!renderer.enabled)
                continue;

            Undo.RecordObject(renderer, "Hide Forward Player Visual");
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void EnsureManagers(Scene sourceScene, Scene targetScene)
    {
        TimeManager timeManager = FindInScene<TimeManager>(targetScene);
        SettingsManager settingsManager = FindInScene<SettingsManager>(targetScene);
        BulletPooler bulletPooler = FindInScene<BulletPooler>(targetScene);

        if (timeManager == null && settingsManager == null && bulletPooler == null)
        {
            TimeManager sourceTimeManager = FindInScene<TimeManager>(sourceScene);
            CloneIntoScene(sourceTimeManager.transform.root.gameObject, targetScene, "Managers");
            timeManager = FindInScene<TimeManager>(targetScene);
            settingsManager = FindInScene<SettingsManager>(targetScene);
            bulletPooler = FindInScene<BulletPooler>(targetScene);
        }

        if (timeManager == null || settingsManager == null || bulletPooler == null)
            throw new InvalidOperationException("대상 씬의 Managers 구성이 일부만 존재합니다. 중복 생성을 중단했습니다.");

        Undo.RecordObject(timeManager, "Configure Noryangjin Time Manager");
        timeManager.isForwardMarchScene = true;
        EditorUtility.SetDirty(timeManager);
    }

    private static void EnsureAnalyticsSceneContext(Scene targetScene)
    {
        GameplayAnalyticsSceneContext context =
            FindInScene<GameplayAnalyticsSceneContext>(targetScene);
        if (context == null)
        {
            TimeManager timeManager = FindInScene<TimeManager>(targetScene);
            context = Undo.AddComponent<GameplayAnalyticsSceneContext>(
                timeManager.gameObject);
        }

        Undo.RecordObject(context, "Configure Noryangjin Analytics Context");
        context.Configure(1, 1, 10, "forward_march", true);
        EditorUtility.SetDirty(context);
    }

    private static void EnsureChapterEnemyStats(Scene targetScene)
    {
        ChapterEnemyStatController controller =
            FindInScene<ChapterEnemyStatController>(targetScene);
        if (controller == null)
        {
            TimeManager timeManager = FindInScene<TimeManager>(targetScene);
            controller = Undo.AddComponent<ChapterEnemyStatController>(
                timeManager.gameObject);
        }

        GameplayAnalyticsSceneContext context =
            FindInScene<GameplayAnalyticsSceneContext>(targetScene);
        Undo.RecordObject(controller, "Configure Noryangjin Chapter Enemy Stats");
        controller.Configure(context != null ? context.Chapter : 1);
        EditorUtility.SetDirty(controller);
    }

    private static CanvasScript EnsureCanvas(Scene sourceScene, Scene targetScene)
    {
        CanvasScript canvas = FindInScene<CanvasScript>(targetScene);
        if (canvas != null)
            return canvas;

        CanvasScript sourceCanvas = FindInScene<CanvasScript>(sourceScene);
        GameObject clone = CloneIntoScene(
            sourceCanvas.transform.root.gameObject,
            targetScene,
            "Canvas");
        canvas = clone.GetComponent<CanvasScript>();
        if (canvas == null)
            throw new InvalidOperationException("복제한 Forward Canvas에 CanvasScript가 없습니다.");

        return canvas;
    }

    private static void EnsureEventSystem(Scene sourceScene, Scene targetScene)
    {
        if (FindInScene<EventSystem>(targetScene) != null)
            return;

        EventSystem sourceEventSystem = FindInScene<EventSystem>(sourceScene);
        CloneIntoScene(sourceEventSystem.gameObject, targetScene, "EventSystem");
    }

    private static void EnsureUpgradeServices(Scene sourceScene, Scene targetScene)
    {
        UpgradeStatManager upgradeManager = FindInScene<UpgradeStatManager>(targetScene);
        if (upgradeManager == null)
        {
            UpgradeStatManager sourceUpgradeManager = FindInScene<UpgradeStatManager>(sourceScene);
            GameObject clone = CloneIntoScene(
                sourceUpgradeManager.transform.root.gameObject,
                targetScene,
                "UpgradeServices");

            foreach (MonoBehaviour behaviour in clone.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null && behaviour is not UpgradeStatManager)
                    Undo.DestroyObjectImmediate(behaviour);
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(clone);
            upgradeManager = clone.GetComponentInChildren<UpgradeStatManager>(true);
        }

        NoryangjinUpgradeExtraHelpSpawner spawner =
            FindInScene<NoryangjinUpgradeExtraHelpSpawner>(targetScene);
        if (spawner == null)
        {
            spawner = Undo.AddComponent<NoryangjinUpgradeExtraHelpSpawner>(
                upgradeManager.gameObject);
        }

        GameManager sourceGameManager = FindInScene<GameManager>(sourceScene);
        Undo.RecordObject(spawner, "Configure Noryangjin Upgrade Extra Helps");
        spawner.Configure(
            sourceGameManager.extraHelp_TungTungTung,
            sourceGameManager.extraHelp_BoomBarDino);
        EditorUtility.SetDirty(spawner);
    }

    private static void RebindCanvasCamera(CanvasScript canvasScript, Camera targetCamera)
    {
        Canvas canvas = canvasScript != null ? canvasScript.GetComponent<Canvas>() : null;
        if (canvas == null)
            throw new InvalidOperationException("Forward Canvas의 Canvas 컴포넌트를 찾지 못했습니다.");

        Undo.RecordObject(canvas, "Bind Noryangjin UI Camera");
        canvas.worldCamera = targetCamera;
        EditorUtility.SetDirty(canvas);
    }

    private static void ValidateInstalledScene(
        Scene targetScene,
        PlayerScript player,
        CanvasScript canvas)
    {
        var missing = new List<string>();
        if (player == null)
            missing.Add("PlayerScript");
        if (player != null && player.GetComponent<WeaponManager>() == null)
            missing.Add("WeaponManager");
        if (player != null && player.transform.Find("Original") == null)
            missing.Add("Original visual");
        if (FindInScene<TimeManager>(targetScene) == null)
            missing.Add("TimeManager");
        if (FindInScene<SettingsManager>(targetScene) == null)
            missing.Add("SettingsManager");
        if (FindInScene<BulletPooler>(targetScene) == null)
            missing.Add("BulletPooler");
        if (canvas == null)
            missing.Add("CanvasScript");
        if (FindInScene<EventSystem>(targetScene) == null)
            missing.Add("EventSystem");
        if (FindInScene<UpgradeStatManager>(targetScene) == null)
            missing.Add("UpgradeStatManager");
        NoryangjinUpgradeExtraHelpSpawner spawner =
            FindInScene<NoryangjinUpgradeExtraHelpSpawner>(targetScene);
        if (spawner == null || !spawner.IsConfigured)
            missing.Add("NoryangjinUpgradeExtraHelpSpawner");
        if (FindInScene<MoneyScript>(targetScene) == null)
            missing.Add("MoneyScript");
        if (FindInScene<GameplayAnalyticsSceneContext>(targetScene) == null)
            missing.Add("GameplayAnalyticsSceneContext");
        if (FindInScene<ChapterEnemyStatController>(targetScene) == null)
            missing.Add("ChapterEnemyStatController");

        if (missing.Count > 0)
            throw new InvalidOperationException($"설치 검증 실패: {string.Join(", ", missing)}");
    }

    private static GameObject CloneIntoScene(
        GameObject source,
        Scene targetScene,
        string targetName)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        GameObject clone = UnityEngine.Object.Instantiate(source);
        clone.name = targetName;
        if (clone.scene != targetScene)
            SceneManager.MoveGameObjectToScene(clone, targetScene);

        Undo.RegisterCreatedObjectUndo(clone, $"Create {targetName}");
        return clone;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    internal static GameObject FindOriginalVisual(Scene scene, PlayerScript player)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        if (player != null)
        {
            foreach (Transform child in player.transform)
            {
                if (string.Equals(child.name, "Original", StringComparison.Ordinal))
                    return child.gameObject;
            }
        }

        GameObject bestCandidate = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (!string.Equals(candidate.name, "Original", StringComparison.Ordinal))
                    continue;

                if (player != null && candidate.IsChildOf(player.transform))
                    continue;

                bestCandidate ??= candidate.gameObject;
                if (candidate.GetComponentInChildren<Camera>(true) != null)
                    return candidate.gameObject;
            }
        }

        return bestCandidate;
    }

    private static Camera FindMainCamera(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        Camera fallback = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            {
                fallback ??= camera;
                if (camera.CompareTag("MainCamera"))
                    return camera;
            }
        }

        return fallback;
    }

    private static void EnsureTargetSceneInBuildSettings()
    {
        EditorBuildSettings.scenes = BuildTargetSceneList(EditorBuildSettings.scenes);
    }

    internal static EditorBuildSettingsScene[] BuildTargetSceneList(
        IEnumerable<EditorBuildSettingsScene> currentScenes)
    {
        List<EditorBuildSettingsScene> scenes =
            (currentScenes ?? Enumerable.Empty<EditorBuildSettingsScene>()).ToList();
        scenes.RemoveAll(scene =>
            string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scene.path, TargetScenePath, StringComparison.OrdinalIgnoreCase));

        // Noryangjin is the project's enabled default scene. Keep Forward available
        // after it as the source and fallback gameplay scene.
        scenes.Insert(0, new EditorBuildSettingsScene(TargetScenePath, true));
        scenes.Insert(1, new EditorBuildSettingsScene(SourceScenePath, true));

        return scenes.ToArray();
    }

    private static void RecordBuildSettingsForUndo()
    {
        EditorBuildSettings buildSettings =
            Resources.FindObjectsOfTypeAll<EditorBuildSettings>().FirstOrDefault();
        if (buildSettings == null)
        {
            throw new InvalidOperationException(
                "EditorBuildSettings object could not be found for Undo registration.");
        }

        Undo.RegisterCompleteObjectUndo(
            buildSettings,
            "Configure Noryangjin Build Scenes");
    }
}
#endif
