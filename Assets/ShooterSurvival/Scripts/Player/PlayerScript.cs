using UnityEngine.UI;
using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
namespace IndianOceanAssets.ShooterSurvival
{
    public class PlayerScript : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] public int playerScore = 0;                // Current score of the player
        //[SerializeField] public float originalHealth;                // Player's current health
        [SerializeField] public float currentHealth;                // Player's current health
        [SerializeField] private GameObject currentWeapon;          // Reference to the current weapon the player is using
        [SerializeField] public float originalDamage;
        [SerializeField] public float currentDamage;                // Current damage dealt by the player
        [SerializeField] private float currentFireRate;             // Current fire rate of the weapon
        [SerializeField] public float moveSensitivity;              // Movement sensitivity for player movement
        [Tooltip("less value more move(standard is 50)")]
        [SerializeField] public float moveSensitivity_Devision;              // Mouse sensitivity for aiming

        [Header("Params")]
        [SerializeField]
        public float originalHealth = 100f;                           // Maximum player health
        [Tooltip("X movement Range (min to max)")]
        [SerializeField]
        private Vector2 xRange;                                     // Range of x-axis movement (min, max)
        [Tooltip("How smooth should the player move")]
        [UnityEngine.Range(1f, 50f)]
        [SerializeField]
        private float movementSmoothness;                           // Smoothness of player movement
        [Tooltip("Enemy detection range")]
        public float enemyDetectRadius;                             // Radius to detect enemy
        [Tooltip("Forward Movement speed")]
        [UnityEngine.Range(1f, 20f)]
        [SerializeField]
        private float fwdMoveSpeed;

        public float ForwardMoveSpeed => fwdMoveSpeed;

        [Header("Player Debugging Options")]
        public bool movement = true;
        public bool animationActive = true;
        public bool enemyDetection = true;

        [Header("Dependancies")]
        [SerializeField] private Image healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private GameObject sharksGO;

        // Local variables
        Animator playerAnimator;
        Vector3 startPos;                           // Start position of input (for mouse or touch)
        WeaponManager weaponManager;                // Reference to WeaponManager
        WeaponScript currentWeaponScript;           // Reference to the script of the current weapon
        [HideInInspector] public Vector3 previousPosition = Vector3.zero;
        [HideInInspector] public bool isMoving;
        [HideInInspector] public int dir;
        private bool isDead = false;
        Transform playerMesh;
        [HideInInspector] public Transform nearestEnemy;
        private bool winDancePlayed;

        public List<WeaponScript> extraHelpWeaponScript;
        public int extraHelpCount = 0;
        public float lastWallTouchTime;
        public bool canShoot;

        public Animator sharkAnim;
        public float originalMoveSpeed;
        private float gameplayElapsedSeconds;
        private int lastLoggedGameplaySecond;

        private float maxHealthWithUpgrades;
        private float healthRegenPerSecond;
        private CanvasScript canvasScript;
        private Canvas playerChildCanvas;
        private bool startGestureTriggered;
        private bool startGestureArmed;
        private const float StartDragThreshold = 8f;        
        private const string SkinBonusSourceKey = "player_skin_bonus";
        private const string PlayerDefaultHpVariableKey = "playerDefaultHp";
        private const string PlayerDefaultAttVariableKey = "playerDefaultAtt";
        private string appliedSkinItem;
        private bool subscribedToStats;
        private float lastReportedCurrentDamage = float.MinValue;
        private Rigidbody playerRigidbody;
        private bool isWorldYawTurnActive;
        private UnityEngine.Object activeWorldYawTurnSource;
        private Quaternion worldYawTurnStartRotation;
        private Quaternion worldYawTurnTargetRotation;
        private float worldYawTurnDuration;
        private float worldYawTurnElapsed;
        private Vector3 worldYawTurnLockedPosition;
        private RigidbodyConstraints constraintsBeforeWorldYawTurn;
        private bool worldYawTurnConstraintsCaptured;
        private Vector3 routeLaneOrigin;
        private Vector3 routeRight = Vector3.right;
        private bool routeFrameInitialized;

        public const float DefaultWorldYawTurnDuration = 0.5f;
        private const float ForwardMovementCompatibilityMultiplier = 2f;
        public bool IsWorldYawTurnActive => isWorldYawTurnActive;
        public float MaxHealth => maxHealthWithUpgrades > 0f ? maxHealthWithUpgrades : originalHealth;

        private void Awake()
        {
            playerRigidbody = GetComponent<Rigidbody>();
            canShoot = true;
            LoadDefaultStatsConfig();
            EnsurePlayerChildCanvasVisible();

            // Set player health to the max health at the start
            currentHealth = originalHealth;
            originalDamage = GetDefaultAttackValue();
            originalMoveSpeed = fwdMoveSpeed;
            RefreshUpgradeStats();
            RebaseRouteFrame();
        }

        private void OnEnable()
        {
            SubscribeToStatChanges();
        }

        private void OnDisable()
        {
            CancelWorldYawTurn();
            UnsubscribeFromStatChanges();
        }


        private void Start()
        {
            playerAnimator = FindPlayerAnimator();
            if (playerAnimator != null)
                playerAnimator.SetBool("PlayerIsDead", false);

            RefreshSharkAnimator();
            weaponManager = GetComponent<WeaponManager>();
            canvasScript = FindFirstObjectByType<CanvasScript>();

            moveSensitivity = PlayerPrefs.GetFloat("moveSensitivity", 1f);  // Get move sensitivity from PlayerPrefs

            previousPosition = transform.position;
            playerMesh = transform.GetChild(0);
            RebaseRouteFrame();
            EnsurePlayerChildCanvasVisible();

            extraHelpWeaponScript = new List<WeaponScript>();
            healthText.text = currentHealth.ToString("N0");
            RefreshUpgradeStats();
        }

        private void Update()
        {
            SubscribeToStatChanges();

            currentWeaponScript = GetComponentInChildren<WeaponScript>();

            // Update runtime variables
            currentHealth = UpdateHealth();
            currentWeapon = weaponManager.currentWeapon;

            if (currentWeaponScript != null)
            {
                currentDamage = currentWeaponScript.damage;
                currentFireRate = currentWeaponScript.fireRate;
            }
            else
            {
                currentDamage = 0f;
            }

            PushCurrentDamageToCanvasIfChanged();

            if (!string.Equals(appliedSkinItem, GetActiveSharkItemName(), StringComparison.OrdinalIgnoreCase))
                RefreshSharkAnimator();

            if (TimeManager.isGameRunning == true && winDancePlayed == false) RotateTowardEnemy();

            HandleAnimation();
            ApplyHealthRegen();
        }

        private Animator FindPlayerAnimator()
        {
            foreach (var animator in GetComponentsInChildren<Animator>(true))
            {
                if (sharksGO != null && animator.transform.IsChildOf(sharksGO.transform))
                    continue;

                return animator;
            }

            return null;
        }

        public void RefreshSharkAnimator()
        {
            sharkAnim = null;

            Transform visibleOriginal = transform.Find("Original");
            if (visibleOriginal != null && visibleOriginal.gameObject.activeInHierarchy)
                sharkAnim = visibleOriginal.GetComponentInChildren<Animator>(true);

            if (sharksGO == null)
                return;

            if (sharkAnim == null)
            {
                Transform sharksRoot = sharksGO.transform;
                for (int i = 0; i < sharksRoot.childCount; i++)
                {
                    Transform shark = sharksRoot.GetChild(i);
                    if (!shark.gameObject.activeSelf)
                        continue;

                    sharkAnim = shark.GetComponentInChildren<Animator>(true);
                    break;
                }
            }

            ApplySkinBonusFromActiveShark();
        }

        void OnStatsChanged()
        {
            RefreshUpgradeStats();

            foreach (var weapon in GetComponentsInChildren<WeaponScript>(true))
                weapon.ResetStatBonus();

            PushCurrentDamageToCanvasIfChanged(force: true);
        }

        void SubscribeToStatChanges()
        {
            if (subscribedToStats || UpgradeStatManager.S == null)
                return;

            UpgradeStatManager.S.StatsChanged += OnStatsChanged;
            subscribedToStats = true;
        }

        void UnsubscribeFromStatChanges()
        {
            if (!subscribedToStats || UpgradeStatManager.S == null)
                return;

            UpgradeStatManager.S.StatsChanged -= OnStatsChanged;
            subscribedToStats = false;
        }

        private void FixedUpdate()
        {
            if (!TimeManager.isGameRunning || TimeManager.timeFactor <= 0f)
            {
                TryStartGameFromHorizontalInput();
                return;
            }

            UpdateGameplayTimeDebug();

            bool isForwardMarchScene = TimeManager.Instance != null && TimeManager.Instance.isForwardMarchScene;
            if (CanvasScript.isGameOver || winDancePlayed) // Add winDancePlayed to stop movement
            {
                CancelWorldYawTurn();
                fwdMoveSpeed = 0;
                movement = false; // Disable horizontal movement
                if (playerAnimator != null)
                    playerAnimator.SetBool("WalkFwd", false); // Stop forward walk animation
                return; // Stop further fixed update logic for movement/input
            }

            if (isWorldYawTurnActive)
            {
                UpdateWorldYawTurn();
                enemyDetection = !isForwardMarchScene;
                if (playerAnimator != null)
                    playerAnimator.SetBool("WalkFwd", false);

                // Keep the drag anchor current while lateral motion is locked so
                // releasing a turn cannot apply the whole held gesture at once.
                PlayerInput();
                return;
            }

            if (isForwardMarchScene)
            {
                ApplyForwardMovement();
                if (isDead)
                    fwdMoveSpeed = 0f;
                enemyDetection = false;
                if (playerAnimator != null)
                    playerAnimator.SetBool("WalkFwd", true);
            }
            else
            {
                enemyDetection = true;
            }

            PlayerInput();
        }

        private void PlayerInput()
        {
            if (currentHealth <= 0)
            {
                return;
            }

            // Handles player touch input for Editor and Target platform

            if (Application.isMobilePlatform)
            {
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);                                            // Get the first touch input

                    if (touch.phase == TouchPhase.Began) startPos = touch.position;             // Set start position on touch begin
                    if (touch.phase == TouchPhase.Moved)
                    {
                        Vector2 delta = touch.position - new Vector2(startPos.x, startPos.y);
                        PlayerMove(delta.x);                                                    // Move player based on touch delta
                        startPos = touch.position;                                              // Update start position
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0)) startPos = Input.mousePosition;
                if (Input.GetMouseButton(0))
                {
                    Vector3 delta = Input.mousePosition - startPos;
                    PlayerMove(delta.x);
                    startPos = Input.mousePosition;
                }

                float keyboardInput = Input.GetAxisRaw("Horizontal");
                if (keyboardInput != 0)
                {
                    PlayerMove(keyboardInput * 15f);
                }
            }
        }

        private void PlayerMove(float deltaX)
        {
            if (!movement || isWorldYawTurnActive)
                return;

            EnsureRouteFrame();
            float sensitivityDivisor = Mathf.Abs(moveSensitivity_Devision) > 0.0001f
                ? moveSensitivity_Devision
                : 1f;
            float currentOffset = Vector3.Dot(transform.position - routeLaneOrigin, routeRight);
            float targetOffset = Mathf.Clamp(
                currentOffset + deltaX * moveSensitivity / sensitivityDivisor,
                xRange.x,
                xRange.y);
            Vector3 targetPosition = transform.position + routeRight * (targetOffset - currentOffset);
            Vector3 nextPosition = Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime * movementSmoothness * TimeManager.timeFactor);
            ApplyPlayerPosition(nextPosition);
        }

        private void ApplyForwardMovement()
        {
            Vector3 routeForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (routeForward.sqrMagnitude <= 0.0001f)
                return;

            // The original Forward scene advanced twice per FixedUpdate. Keep its
            // authored timing while consolidating that motion into one route-aware step.
            Vector3 nextPosition = transform.position +
                                   routeForward.normalized *
                                   (fwdMoveSpeed / 100f) *
                                   ForwardMovementCompatibilityMultiplier *
                                   TimeManager.timeFactor;
            ApplyPlayerPosition(nextPosition);
        }

        private void ApplyPlayerPosition(Vector3 position)
        {
            if (playerRigidbody != null)
                playerRigidbody.position = position;

            transform.position = position;
        }

        public bool RequestWorldYawTurn(
            float targetWorldYaw,
            float duration = DefaultWorldYawTurnDuration,
            UnityEngine.Object source = null)
        {
            Quaternion currentRotation =
                playerRigidbody != null ? playerRigidbody.rotation : transform.rotation;
            return RequestWorldRotation(
                currentRotation.eulerAngles.x,
                targetWorldYaw,
                duration,
                source);
        }

        public bool RequestWorldRotation(
            float targetWorldX,
            float targetWorldY,
            float duration = DefaultWorldYawTurnDuration,
            UnityEngine.Object source = null)
        {
            if (!isActiveAndEnabled ||
                isDead ||
                currentHealth <= 0f ||
                CanvasScript.isGameOver ||
                winDancePlayed ||
                !TimeManager.isGameRunning)
            {
                return false;
            }

            float normalizedTargetX = NormalizeWorldAngle(targetWorldX);
            float normalizedTargetY = NormalizeWorldAngle(targetWorldY);
            Quaternion currentRotation =
                playerRigidbody != null ? playerRigidbody.rotation : transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(
                normalizedTargetX,
                normalizedTargetY,
                currentRotation.eulerAngles.z);
            if (isWorldYawTurnActive &&
                activeWorldYawTurnSource == source &&
                Quaternion.Angle(
                    worldYawTurnTargetRotation,
                    targetRotation) <= 0.01f)
            {
                return false;
            }

            if (!isWorldYawTurnActive)
            {
                worldYawTurnLockedPosition = transform.position;
                CaptureWorldYawTurnConstraints();
            }

            worldYawTurnStartRotation = currentRotation;
            worldYawTurnTargetRotation = targetRotation;
            worldYawTurnDuration = Mathf.Max(0f, duration);
            worldYawTurnElapsed = 0f;
            activeWorldYawTurnSource = source;
            isWorldYawTurnActive = true;

            if (worldYawTurnDuration <= 0f)
                CompleteWorldYawTurn();

            return true;
        }

        public void CancelWorldYawTurn()
        {
            if (!isWorldYawTurnActive && !worldYawTurnConstraintsCaptured)
                return;

            isWorldYawTurnActive = false;
            activeWorldYawTurnSource = null;
            worldYawTurnElapsed = 0f;
            ApplyPlayerPosition(worldYawTurnLockedPosition);
            RestoreWorldYawTurnConstraints();
            RebaseRouteFrame();
        }

        public static float NormalizeWorldYaw(float yaw)
        {
            return NormalizeWorldAngle(yaw);
        }

        public static float NormalizeWorldAngle(float angle)
        {
            return Mathf.Repeat(angle, 360f);
        }

        public static Quaternion EvaluateWorldYawTurn(
            Quaternion start,
            Quaternion target,
            float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            float eased = t * t * (3f - 2f * t);
            return Quaternion.Slerp(start, target, eased);
        }

        private void UpdateWorldYawTurn()
        {
            if (!isWorldYawTurnActive)
                return;

            if (isDead || currentHealth <= 0f || CanvasScript.isGameOver || winDancePlayed)
            {
                CancelWorldYawTurn();
                return;
            }

            worldYawTurnElapsed += Time.fixedDeltaTime * Mathf.Max(0f, TimeManager.timeFactor);
            float normalizedTime = worldYawTurnDuration <= 0f
                ? 1f
                : worldYawTurnElapsed / worldYawTurnDuration;
            Quaternion nextRotation = EvaluateWorldYawTurn(
                worldYawTurnStartRotation,
                worldYawTurnTargetRotation,
                normalizedTime);
            ApplyWorldYawRotation(nextRotation);
            ApplyPlayerPosition(worldYawTurnLockedPosition);

            if (normalizedTime >= 1f)
                CompleteWorldYawTurn();
        }

        private void CompleteWorldYawTurn()
        {
            ApplyWorldYawRotation(worldYawTurnTargetRotation);
            ApplyPlayerPosition(worldYawTurnLockedPosition);
            isWorldYawTurnActive = false;
            activeWorldYawTurnSource = null;
            worldYawTurnElapsed = 0f;
            RestoreWorldYawTurnConstraints();
            RebaseRouteFrame();
        }

        private void ApplyWorldYawRotation(Quaternion rotation)
        {
            if (playerRigidbody != null)
            {
                playerRigidbody.MoveRotation(rotation);
                playerRigidbody.rotation = rotation;
                transform.rotation = rotation;
            }
            else
            {
                transform.rotation = rotation;
            }
        }

        private void CaptureWorldYawTurnConstraints()
        {
            playerRigidbody ??= GetComponent<Rigidbody>();
            if (playerRigidbody == null || worldYawTurnConstraintsCaptured)
                return;

            ApplyPlayerPosition(worldYawTurnLockedPosition);
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            constraintsBeforeWorldYawTurn = playerRigidbody.constraints;
            RigidbodyConstraints turnConstraints = constraintsBeforeWorldYawTurn;
            turnConstraints |= RigidbodyConstraints.FreezePositionX |
                               RigidbodyConstraints.FreezePositionZ;
            turnConstraints &= ~(RigidbodyConstraints.FreezeRotationX |
                                 RigidbodyConstraints.FreezeRotationY);
            playerRigidbody.constraints = turnConstraints;
            worldYawTurnConstraintsCaptured = true;
        }

        private void RestoreWorldYawTurnConstraints()
        {
            if (playerRigidbody == null || !worldYawTurnConstraintsCaptured)
                return;

            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.constraints = constraintsBeforeWorldYawTurn;
            worldYawTurnConstraintsCaptured = false;
        }

        private void EnsureRouteFrame()
        {
            if (!routeFrameInitialized)
                RebaseRouteFrame();
        }

        private void RebaseRouteFrame()
        {
            routeLaneOrigin = transform.position;
            Vector3 flattenedRight = Vector3.ProjectOnPlane(transform.right, Vector3.up);
            routeRight = flattenedRight.sqrMagnitude > 0.0001f
                ? flattenedRight.normalized
                : Vector3.right;
            routeFrameInitialized = true;
        }

        private void TryStartGameFromHorizontalInput()
        {
            if (startGestureTriggered || currentHealth <= 0 || CanvasScript.isGameOver)
                return;

            float horizontalDelta = 0f;

            if (Application.isMobilePlatform)
            {
                if (Input.touchCount <= 0)
                    return;

                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    startPos = touch.position;
                    startGestureArmed = canvasScript != null && canvasScript.IsPointerOverStartArea(touch.position);
                    return;
                }

                if (!startGestureArmed || touch.phase != TouchPhase.Moved)
                    return;

                horizontalDelta = touch.position.x - startPos.x;
                startPos = touch.position;
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    startPos = Input.mousePosition;
                    startGestureArmed = canvasScript != null && canvasScript.IsPointerOverStartArea(Input.mousePosition);
                    return;
                }

                if (!startGestureArmed || !Input.GetMouseButton(0))
                    return;

                horizontalDelta = Input.mousePosition.x - startPos.x;
                startPos = Input.mousePosition;
            }

            if (Mathf.Abs(horizontalDelta) < StartDragThreshold)
                return;

            startGestureTriggered = true;

            if (canvasScript == null)
                canvasScript = FindFirstObjectByType<CanvasScript>();

            if (canvasScript != null)
                canvasScript.PlayerPressedStartButton();
        }

        private void RotateTowardEnemy()
        {
            if (enemyDetection == false || TimeManager.Instance.isForwardMarchScene == true || CanvasScript.isGameOver == true || winDancePlayed == true) return;

            Collider[] colliders = Physics.OverlapSphere(transform.position, enemyDetectRadius);
            nearestEnemy = null;
            float minDistance = Mathf.Infinity;

            foreach (var obj in colliders)
            {
                if (obj.CompareTag("EnemyTag"))
                {
                    float dist = Vector3.Distance(transform.position, obj.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestEnemy = obj.transform;
                    }
                }
            }

            if (nearestEnemy != null)
            {
                Vector3 enemyDir = nearestEnemy.position - playerMesh.position;
                enemyDir.y = 0;

                if (enemyDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(enemyDir);
                    playerMesh.rotation = Quaternion.Slerp(playerMesh.rotation, targetRotation, 10f * Time.deltaTime * TimeManager.timeFactor);

                }
            }
            else
            {
                Quaternion targetRot = Quaternion.LookRotation(Vector3.forward);
                playerMesh.rotation = Quaternion.Slerp(playerMesh.rotation, targetRot, 10f * Time.deltaTime * TimeManager.timeFactor);

            }

        }

        private void HandleAnimation()
        {
            if (animationActive == false && TimeManager.isGameRunning == false)
            {
                if (winDancePlayed == true) return;
                playerAnimator.enabled = false;
                return;
            }

            playerAnimator.enabled = true;
            if (TimeManager.Instance != null && TimeManager.Instance.isForwardMarchScene == false && winDancePlayed == false)
                playerAnimator.SetBool("WalkFwd", false);

            EnsureRouteFrame();
            Vector3 positionDelta = transform.position - previousPosition;
            float lateralDelta = Vector3.Dot(positionDelta, routeRight);
            isMoving = Mathf.Abs(lateralDelta) > 0.01f;
            playerAnimator.SetBool("IsMoving", isMoving);

            if (isMoving == true && winDancePlayed == false)
            {
                dir = lateralDelta > 0f ? 1 : -1;

                playerAnimator.SetInteger("MoveDirection", dir);
            }

            previousPosition = transform.position;
        }


        public void PlayWinDance()
        {
            if (winDancePlayed == true) return;
            CancelWorldYawTurn();
            if (animationActive == true && playerAnimator != null)
            {
                playerAnimator.SetTrigger("WinDance");
                transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
                playerMesh.rotation = Quaternion.Euler(0, 180, 0);
            }

            winDancePlayed = true;
        }

        public float UpdateHealth()
        {
            //if (currentHealth > 100) currentHealth = 100;

            if (currentHealth <= 0 && isDead == false)
            {
                CancelWorldYawTurn();
                currentHealth = 0;
                isDead = true;
                RefreshSharkAnimator();
                if (sharkAnim != null)
                    sharkAnim.SetTrigger("Die");

                //playerAnimator.SetTrigger("PlayerIsDead");
                winDancePlayed = false;
                //二쎌쑝硫??좊땲 ?띾룄 0?쇰줈

            }

            float maxHealth = maxHealthWithUpgrades > 0f ? maxHealthWithUpgrades : originalHealth;
            if (healthBar) healthBar.fillAmount = currentHealth / maxHealth;
            if (healthText) healthText.text = currentHealth.ToString("N0");

            return currentHealth;
        }

        public void ApplyHarnessHealthDelta(float delta)
        {
            currentHealth = Mathf.Clamp(currentHealth + delta, 0f, MaxHealth);
            UpdateHealth();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("GameEndTriggerTag"))
            {
                CanvasScript canvasScript = FindAnyObjectByType<CanvasScript>();
                if (canvasScript != null && winDancePlayed == false) canvasScript.YouWin();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log(collision.gameObject.name + "!!");
        }

        public void ResetState()
        {
            Vector3 resetPosition = transform.position;
            CancelWorldYawTurn();
            ApplyPlayerPosition(resetPosition);
            isDead = false;
            winDancePlayed = false;  
            startGestureTriggered = false;
            startGestureArmed = false;
            gameplayElapsedSeconds = 0f;
            lastLoggedGameplaySecond = 0;

            // ?좊땲硫붿씠???꾩쟾 珥덇린??DeathAnim ?덉텧)
            if (playerAnimator)
            {
                RefreshSharkAnimator();
                if (sharkAnim != null)
                    sharkAnim.SetTrigger("Walk");
            }

            // 踰??ъ젒珥?荑?珥덇린??
            lastWallTouchTime = 0f;

            // ?대룞/?꾪닾 蹂듦뎄
            movement = true;
            canShoot = true;
            RebaseRouteFrame();
            EnsurePlayerChildCanvasVisible();

            // ?ㅼ떆 ?섍컻????
            foreach (var w in GetComponentsInChildren<WeaponScript>(true))
                w.ResetShooting();
        }

        private Canvas GetPlayerChildCanvas()
        {
            if (playerChildCanvas != null)
                return playerChildCanvas;

            foreach (var canvas in GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.transform != transform)
                {
                    playerChildCanvas = canvas;
                    break;
                }
            }

            return playerChildCanvas;
        }

        public void SetPlayerChildCanvasVisible(bool visible)
        {
            Canvas childCanvas = GetPlayerChildCanvas();
            if (childCanvas == null)
                return;

            if (childCanvas.gameObject.activeSelf != visible)
                childCanvas.gameObject.SetActive(visible);
        }

        public void EnsurePlayerChildCanvasVisible()
        {
            SetPlayerChildCanvasVisible(true);
        }

        public void ResetStatBonus()
        {
            // 泥대젰 ?먮났
            currentHealth = originalHealth;

            // UI 利됱떆 諛섏쁺
            if (healthBar) healthBar.fillAmount = 1f;
            if (healthText) healthText.text = currentHealth.ToString("N0");

            // Rare / ExtraHelp 愿??
            extraHelpCount = 0;
            if (extraHelpWeaponScript != null)
                extraHelpWeaponScript.Clear();

            // ?댁냽 ?먮났
            gameplayElapsedSeconds = 0f;
            lastLoggedGameplaySecond = 0;
            fwdMoveSpeed = originalMoveSpeed;
            RefreshUpgradeStats();
            PushCurrentDamageToCanvasIfChanged(force: true);
        }

        private void PushCurrentDamageToCanvasIfChanged(bool force = false)
        {
            if (canvasScript == null)
                canvasScript = FindFirstObjectByType<CanvasScript>();

            if (canvasScript == null)
                return;

            if (!force && Mathf.Approximately(lastReportedCurrentDamage, currentDamage))
                return;

            lastReportedCurrentDamage = currentDamage;
            canvasScript.UpdateAttackDebugText(currentDamage);
        }
        public void RefreshUpgradeStats()
        {
            if (UpgradeStatManager.S == null) return;

            maxHealthWithUpgrades = UpgradeStatManager.S.ApplyToBase(UpgradeStatManager.UpgradeType.HP, originalHealth);
            currentHealth = maxHealthWithUpgrades;

            if (healthBar) healthBar.fillAmount = currentHealth / maxHealthWithUpgrades;
            if (healthText) healthText.text = currentHealth.ToString("N0");

            healthRegenPerSecond = UpgradeStatManager.S.GetAppliedValue(UpgradeStatManager.UpgradeType.HP_REGEN, maxHealthWithUpgrades);

            BulletScript.ApplyProjectileSpeedUpgrade(
                UpgradeStatManager.S.GetFlatStat(UpgradeStatManager.UpgradeType.PROJECTILE_SPEED),
                UpgradeStatManager.S.GetPercentStat(UpgradeStatManager.UpgradeType.PROJECTILE_SPEED));
        }

        public void LogWeaponDamageDebug(string context = "GameStart")
        {
            var weapons = GetComponentsInChildren<WeaponScript>(true);
            foreach (var weapon in weapons)
                weapon.LogDamageBreakdown(context);
        }

        void ApplySkinBonusFromActiveShark()
        {
            if (UpgradeStatManager.S == null)
                return;

            string activeSkinItem = GetActiveSharkItemName();
            if (string.Equals(appliedSkinItem, activeSkinItem, StringComparison.OrdinalIgnoreCase))
                return;

            appliedSkinItem = activeSkinItem;
            UpgradeStatManager.S.ClearRuntimeModifier(SkinBonusSourceKey);

            if (!SkinTables.TryGetByItem(activeSkinItem, out var skin))
                return;

            if (!SkinBonusResolver.TryResolve(skin.bonusType, out var upgradeType))
                return;

            if (Mathf.Approximately(skin.bonusValue, 0f))
                return;

            UpgradeStatManager.S.SetRuntimeModifier(SkinBonusSourceKey, upgradeType, skin.bonusValue, skin.bonusValueType);
        }

        string GetActiveSharkItemName()
        {
            if (sharksGO == null)
                return string.Empty;

            Transform sharksRoot = sharksGO.transform;
            for (int i = 0; i < sharksRoot.childCount; i++)
            {
                Transform shark = sharksRoot.GetChild(i);
                if (shark.gameObject.activeSelf)
                    return shark.name;
            }

            return string.Empty;
        }

        private void ApplyHealthRegen()
        {
            if (!TimeManager.isGameRunning) return;
            if (healthRegenPerSecond <= 0f) return;

            float maxHealth = maxHealthWithUpgrades > 0f ? maxHealthWithUpgrades : originalHealth;
            if (currentHealth >= maxHealth) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegenPerSecond * Time.deltaTime);
        }

        private void LoadDefaultStatsConfig()
        {
            if (EnvironmentVariableTables.TryGetFloat(PlayerDefaultHpVariableKey, out var hpValue) && hpValue > 0f)
                originalHealth = hpValue;
        }

        private float GetDefaultAttackValue()
        {
            if (EnvironmentVariableTables.TryGetFloat(PlayerDefaultAttVariableKey, out var attackValue) && attackValue > 0f)
                return attackValue;

            var weapon = GetComponentInChildren<WeaponScript>();
            return weapon != null ? weapon.damage : originalDamage;
        }

        private void UpdateGameplayTimeDebug()
        {
            if (isDead || CanvasScript.isGameOver || winDancePlayed)
                return;

            gameplayElapsedSeconds += Time.fixedDeltaTime * Mathf.Max(0f, TimeManager.timeFactor);
            int currentSecond = Mathf.FloorToInt(gameplayElapsedSeconds);
            if (currentSecond <= 0 || currentSecond == lastLoggedGameplaySecond)
                return;

            lastLoggedGameplaySecond = currentSecond;
            Debug.Log($"[PlayerScript] Play time: {currentSecond}s");
        }




    }
}









