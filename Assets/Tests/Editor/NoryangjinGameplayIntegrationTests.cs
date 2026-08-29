using System.Collections;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class NoryangjinGameplayIntegrationTests
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        if (Application.isPlaying)
            yield return new ExitPlayMode();

        TimeManager.isGameRunning = false;
        TimeManager.timeFactor = 1f;
        CanvasScript.isGameOver = false;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        TimeManager.isGameRunning = false;
        TimeManager.timeFactor = 1f;
        CanvasScript.isGameOver = false;

        if (Application.isPlaying)
            yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator InstalledScene_StartsMovesAttacksTurnsAndResumes()
    {
        yield return new EnterPlayMode();

        SceneManager.LoadScene("Noryangjin_MapTool_Mode");
        yield return null;
        yield return null;

        PlayerScript player = Object.FindFirstObjectByType<PlayerScript>();
        CanvasScript canvas = Object.FindFirstObjectByType<CanvasScript>();
        TimeManager timeManager = Object.FindFirstObjectByType<TimeManager>();
        BulletPooler bulletPooler = Object.FindFirstObjectByType<BulletPooler>();
        UpgradeStatManager upgradeManager = Object.FindFirstObjectByType<UpgradeStatManager>();
        NoryangjinUpgradeExtraHelpSpawner extraHelpSpawner =
            Object.FindFirstObjectByType<NoryangjinUpgradeExtraHelpSpawner>();
        ChapterEnemyStatController chapterEnemyStats =
            Object.FindFirstObjectByType<ChapterEnemyStatController>();
        WeaponScript weapon = player == null
            ? null
            : player.GetComponentsInChildren<WeaponScript>(true)
                .FirstOrDefault(candidate => candidate.isActiveAndEnabled);

        Assert.That(player, Is.Not.Null);
        Assert.That(canvas, Is.Not.Null);
        Assert.That(timeManager, Is.Not.Null);
        Assert.That(timeManager.isForwardMarchScene, Is.True);
        Assert.That(bulletPooler, Is.Not.Null);
        Assert.That(upgradeManager, Is.Not.Null);
        Assert.That(extraHelpSpawner, Is.Not.Null);
        Assert.That(extraHelpSpawner.IsConfigured, Is.True);
        Assert.That(chapterEnemyStats, Is.Not.Null);
        Assert.That(chapterEnemyStats.Chapter, Is.EqualTo(1));
        float authoredForwardSpeed = player.ForwardMoveSpeed;
        Assert.That(player.UseExcelCharacterDefaults, Is.True);
        Assert.That(
            EnvironmentVariableTables.TryGetFloat3("playerSpeed", out var playerSpeed),
            Is.True);
        Assert.That(
            authoredForwardSpeed,
            Is.EqualTo(playerSpeed.value1).Within(0.0001f),
            "The installed scene must resolve forward speed from playerSpeed value1.");
        Transform originalVisual = player.transform.Find("Original");
        Assert.That(originalVisual, Is.Not.Null);
        Animator originalAnimator = originalVisual.GetComponent<Animator>();
        Assert.That(
            originalAnimator,
            Is.Not.Null,
            "The visible Original character must own the animator that drives its walk cycle.");
        Assert.That(originalAnimator.enabled, Is.True);
        Assert.That(originalAnimator.runtimeAnimatorController, Is.Not.Null);
        Assert.That(originalAnimator.runtimeAnimatorController.name, Is.EqualTo("Original"));
        SkinnedMeshRenderer visibleSkin =
            originalVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .FirstOrDefault(renderer =>
                    renderer.enabled &&
                    renderer.bones.Any(bone => bone != null && bone.name == "backleg"));
        Assert.That(
            visibleSkin,
            Is.Not.Null,
            "The visible Original character must have a rendered skin bound to its backleg bone.");
        Transform walkBone =
            visibleSkin.bones.FirstOrDefault(bone => bone != null && bone.name == "backleg");
        Assert.That(
            walkBone,
            Is.Not.Null,
            "The visible Original character must expose its animated backleg bone.");
        Assert.That(walkBone.IsChildOf(originalVisual), Is.True);
        Assert.That(
            visibleSkin.bones.Contains(walkBone),
            Is.True,
            "The sampled walk bone must belong to the visible skinned character.");
        FieldInfo playerAnimatorField = typeof(PlayerScript).GetField(
            "playerAnimator",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(playerAnimatorField, Is.Not.Null);
        Animator playerAnimator = (Animator)playerAnimatorField.GetValue(player);
        Assert.That(playerAnimator, Is.Not.Null);
        Assert.That(playerAnimator, Is.Not.SameAs(originalAnimator));
        Assert.That(
            playerAnimator.parameters.Select(parameter => parameter.name),
            Is.SupersetOf(new[] { "WalkFwd", "IsMoving", "MoveDirection" }),
            "PlayerScript must drive the locomotion Animator, not the visible Original controller.");
        Assert.That(
            originalVisual.GetComponentsInChildren<Renderer>(true)
                .Any(renderer => renderer.enabled),
            Is.True,
            "기존 Original 캐릭터 렌더러가 표시 상태여야 합니다.");
        Assert.That(
            player.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => !renderer.transform.IsChildOf(originalVisual))
                .All(renderer => !renderer.enabled),
            Is.True,
            "복제된 Forward 캐릭터 모델은 Original과 겹쳐 보이면 안 됩니다.");
        Assert.That(canvas.IsStartAreaActive(), Is.True);
        Assert.That(
            canvas.GetComponentsInChildren<Transform>(true)
                .Any(candidate => candidate.name == "Upgrade2"),
            Is.True,
            "Forward 상점/업그레이드 UI가 노량진 Canvas에 있어야 합니다.");
        Assert.That(weapon, Is.Not.Null);
        Transform mouthBone =
            visibleSkin.bones.FirstOrDefault(bone => bone != null && bone.name == "headend");
        Assert.That(
            mouthBone,
            Is.Not.Null,
            "The visible Original character must expose the animated head-end mouth bone.");
        Transform projectileMuzzle = mouthBone.Find(
            NoryangjinForwardGameplayInstaller.OriginalProjectileMuzzleName);
        Assert.That(
            projectileMuzzle,
            Is.Not.Null,
            "The saved Noryangjin scene must keep a dedicated projectile muzzle on the visible mouth.");
        FieldInfo bulletPositionsField = typeof(WeaponScript).GetField(
            "bulletPositions",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo resolveProjectileSpawnPosition = typeof(WeaponScript).GetMethod(
            "ResolveProjectileSpawnPosition",
            BindingFlags.Instance | BindingFlags.NonPublic);
        PropertyInfo lastProjectileSpawnPosition = typeof(WeaponScript).GetProperty(
            "LastProjectileSpawnPosition",
            BindingFlags.Instance | BindingFlags.Public);
        PropertyInfo lastVisibleMouthPositionAtSpawn = typeof(WeaponScript).GetProperty(
            "LastVisibleMouthPositionAtSpawn",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(bulletPositionsField, Is.Not.Null);
        Assert.That(
            resolveProjectileSpawnPosition,
            Is.Not.Null,
            "Player projectiles must resolve their spawn point from the visible mouth.");
        Assert.That(lastProjectileSpawnPosition, Is.Not.Null);
        Assert.That(lastVisibleMouthPositionAtSpawn, Is.Not.Null);
        Transform authoredMuzzle =
            ((Transform[])bulletPositionsField.GetValue(weapon)).First();
        Vector3 resolvedMouthSpawn = (Vector3)resolveProjectileSpawnPosition.Invoke(
            weapon,
            new object[] { authoredMuzzle });
        Assert.That(
            Vector3.Distance(resolvedMouthSpawn, projectileMuzzle.position),
            Is.LessThan(0.0001f));
        Vector3 mouthOffset = resolvedMouthSpawn - mouthBone.position;
        Assert.That(Vector3.Angle(mouthOffset, player.transform.forward), Is.LessThan(1f));
        Assert.That(
            Vector3.Dot(mouthOffset, player.transform.forward),
            Is.InRange(0.3f, 0.4f));
        Assert.That(
            Vector3.Distance(resolvedMouthSpawn, authoredMuzzle.position),
            Is.GreaterThan(0.5f),
            "The hidden Forward-model muzzle must not remain the player projectile origin.");

        Vector3 idlePosition = player.transform.position;
        yield return new WaitForFixedUpdate();
        Assert.That(
            Vector3.Distance(player.transform.position, idlePosition),
            Is.LessThan(0.001f),
            "시작 입력 전에는 캐릭터가 전진하면 안 됩니다.");

        upgradeManager.SetRuntimeModifier(
            "noryangjin_test_tung_value",
            UpgradeStatManager.UpgradeType.TUNGTUNGTUNG,
            1f,
            ValueType.Value);
        upgradeManager.SetRuntimeModifier(
            "noryangjin_test_tung_percent",
            UpgradeStatManager.UpgradeType.TUNGTUNGTUNG,
            1f,
            ValueType.Percent);
        upgradeManager.SetRuntimeModifier(
            "noryangjin_test_boom_value",
            UpgradeStatManager.UpgradeType.BOOMBAR,
            1f,
            ValueType.Value);
        upgradeManager.SetRuntimeModifier(
            "noryangjin_test_boom_percent",
            UpgradeStatManager.UpgradeType.BOOMBAR,
            1f,
            ValueType.Percent);
        Assert.That(
            upgradeManager.GetStat(UpgradeStatManager.UpgradeType.TUNGTUNGTUNG),
            Is.GreaterThanOrEqualTo(1f));
        Assert.That(
            upgradeManager.GetStat(UpgradeStatManager.UpgradeType.BOOMBAR),
            Is.GreaterThanOrEqualTo(1f));

        canvas.PlayerPressedStartButton();
        AnimatorStateInfo initialOriginalAnimation =
            originalAnimator.GetCurrentAnimatorStateInfo(0);
        Vector3 startPosition = player.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;

        ExtraHelpBuffScript[] helpers =
            Object.FindObjectsByType<ExtraHelpBuffScript>(FindObjectsSortMode.None);
        Assert.That(TimeManager.isGameRunning, Is.True);
        Assert.That(TimeManager.timeFactor, Is.GreaterThan(0f));
        Assert.That(canvas.IsStartAreaActive(), Is.False);
        Assert.That(
            Vector3.Distance(player.transform.position, startPosition),
            Is.GreaterThan(0.01f),
            "시작 후에는 Forward 이동이 적용되어야 합니다.");
        Assert.That(weapon.IsInvoking("ShootBullet"), Is.True);
        int projectileWaitFramesRemaining = 150;
        while (weapon.TotalProjectilesSpawned == 0 &&
               projectileWaitFramesRemaining-- > 0)
        {
            yield return new WaitForFixedUpdate();
        }

        Assert.That(
            weapon.TotalProjectilesSpawned,
            Is.GreaterThan(0),
            "Starting the stage must rent and initialize a player projectile.");
        Vector3 actualSpawnPosition =
            (Vector3)lastProjectileSpawnPosition.GetValue(weapon);
        Vector3 mouthPositionAtSpawn =
            (Vector3)lastVisibleMouthPositionAtSpawn.GetValue(weapon);
        Assert.That(
            Vector3.Distance(actualSpawnPosition, mouthPositionAtSpawn),
            Is.InRange(0.3f, 0.4f),
            "The latest player projectile must originate at the moving visible mouth.");
        FieldInfo projectileDirectionField = typeof(BulletScript).GetField(
            "direction",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(projectileDirectionField, Is.Not.Null);
        BulletScript[] activeProjectiles =
            Object.FindObjectsByType<BulletScript>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(projectile => projectile.gameObject.activeInHierarchy)
                .ToArray();
        Assert.That(activeProjectiles, Is.Not.Empty);
        foreach (BulletScript projectile in activeProjectiles)
        {
            Vector3 travelDirection =
                (Vector3)projectileDirectionField.GetValue(projectile);
            Assert.That(
                Vector3.Angle(projectile.transform.root.forward, travelDirection),
                Is.LessThan(0.1f),
                $"Projectile {projectile.transform.root.name} must point along its travel direction.");
        }
        AnimatorStateInfo movingOriginalAnimation =
            originalAnimator.GetCurrentAnimatorStateInfo(0);
        Assert.That(
            movingOriginalAnimation.shortNameHash,
            Is.EqualTo(Animator.StringToHash("Walk")),
            "The visible Original character must be in its Walk state while the player advances.");
        Assert.That(
            movingOriginalAnimation.normalizedTime,
            Is.GreaterThan(initialOriginalAnimation.normalizedTime),
            "The visible Original walk animation must advance while the player moves.");
        Quaternion initialWalkBoneRotation = walkBone.localRotation;
        float initialWalkPhase = movingOriginalAnimation.normalizedTime;
        AnimatorStateInfo separatedWalkAnimation = movingOriginalAnimation;
        int walkPoseFramesRemaining = 40;
        while (separatedWalkAnimation.normalizedTime - initialWalkPhase < 0.1f &&
               walkPoseFramesRemaining-- > 0)
        {
            yield return new WaitForFixedUpdate();
            separatedWalkAnimation = originalAnimator.GetCurrentAnimatorStateInfo(0);
        }

        Assert.That(
            separatedWalkAnimation.shortNameHash,
            Is.EqualTo(Animator.StringToHash("Walk")),
            "The visible Original character must remain in Walk while sampling its pose.");
        Assert.That(
            separatedWalkAnimation.normalizedTime - initialWalkPhase,
            Is.GreaterThanOrEqualTo(0.1f),
            "Walk samples must be separated by enough animation time to compare their poses.");
        Assert.That(
            Quaternion.Angle(initialWalkBoneRotation, walkBone.localRotation),
            Is.GreaterThan(0.1f),
            "The visible Original walk clip must change the rendered leg-bone pose.");
        Assert.That(
            helpers.Count(helper => helper.helpType == HelpType.Tungtungtung),
            Is.GreaterThanOrEqualTo(1));
        Assert.That(
            helpers.Count(helper => helper.helpType == HelpType.Boombardino),
            Is.GreaterThanOrEqualTo(1));
        Assert.That(player.extraHelpCount, Is.EqualTo(helpers.Length));
        Assert.That(
            player.extraHelpWeaponScript.Count,
            Is.EqualTo(
                helpers.Count(
                    helper => helper.GetComponentInChildren<WeaponScript>() != null)));
        Assert.That(
            player.ForwardMoveSpeed,
            Is.EqualTo(authoredForwardSpeed).Within(0.0001f),
            "Forward speed must remain at the resolved playerSpeed value1 during gameplay.");

        float targetYaw = PlayerScript.NormalizeWorldYaw(
            player.transform.eulerAngles.y + 90f);
        MethodInfo shootBullet = typeof(WeaponScript).GetMethod(
            "ShootBullet",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(shootBullet, Is.Not.Null);
        int[] activeProjectileRootsBeforeShot =
            Object.FindObjectsByType<BulletScript>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(projectile => projectile.gameObject.activeInHierarchy)
                .Select(projectile => projectile.transform.root.GetInstanceID())
                .Distinct()
                .ToArray();
        shootBullet.Invoke(weapon, null);
        BulletScript turningProjectile =
            Object.FindObjectsByType<BulletScript>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .FirstOrDefault(
                    projectile =>
                        projectile.gameObject.activeInHierarchy &&
                        !activeProjectileRootsBeforeShot.Contains(
                            projectile.transform.root.GetInstanceID()));
        Assert.That(
            turningProjectile,
            Is.Not.Null,
            "The integration turn needs a newly rented player missile to follow.");
        Vector3 projectileDirectionBeforeTurn =
            (Vector3)projectileDirectionField.GetValue(turningProjectile);
        Quaternion playerRotationBeforeTurn = player.transform.rotation;
        Vector3 turnStartPosition = player.transform.position;
        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        Assert.That(
            player.RequestWorldYawTurn(targetYaw, 0.5f, player),
            Is.True);
        Assert.That(player.IsWorldYawTurnActive, Is.True);
        Assert.That(
            Vector3.Distance(player.transform.position, turnStartPosition),
            Is.LessThan(0.001f));

        yield return new WaitForFixedUpdate();
        Quaternion firstTurnDelta =
            player.transform.rotation * Quaternion.Inverse(playerRotationBeforeTurn);
        Vector3 expectedProjectileDirection =
            firstTurnDelta * projectileDirectionBeforeTurn;
        Vector3 actualProjectileDirection =
            (Vector3)projectileDirectionField.GetValue(turningProjectile);
        Assert.That(
            Vector3.Angle(actualProjectileDirection, expectedProjectileDirection),
            Is.LessThan(0.1f),
            "A rented player missile must curve by the same incremental turn as its owner.");
        Assert.That(
            Vector3.Angle(
                turningProjectile.transform.root.forward,
                actualProjectileDirection),
            Is.LessThan(0.1f));
        Assert.That(player.IsWorldYawTurnActive, Is.True);
        Assert.That(
            playerRigidbody.constraints.HasFlag(RigidbodyConstraints.FreezePositionX),
            Is.True);
        Assert.That(
            playerRigidbody.constraints.HasFlag(RigidbodyConstraints.FreezePositionZ),
            Is.True);
        Assert.That(
            Vector3.Distance(player.transform.position, turnStartPosition),
            Is.LessThan(0.005f),
            "회전 중에는 전진 이동이 잠겨야 합니다.");

        int remainingFrames = 80;
        while (player.IsWorldYawTurnActive && remainingFrames-- > 0)
            yield return new WaitForFixedUpdate();

        Assert.That(player.IsWorldYawTurnActive, Is.False);
        Assert.That(
            Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y, targetYaw)),
            Is.LessThan(0.5f));

        Vector3 resumePosition = player.transform.position;
        Vector3 routeForward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(
            Vector3.Dot(player.transform.position - resumePosition, routeForward),
            Is.GreaterThan(0.01f),
            "회전이 끝나면 새 로컬 forward 방향으로 전진을 재개해야 합니다.");

        yield return new ExitPlayMode();
    }
}
