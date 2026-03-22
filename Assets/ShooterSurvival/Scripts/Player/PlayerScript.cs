using UnityEngine.UI;
using UnityEngine;
using System;
using NUnit.Framework;
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

        [Header("Player Debugging Options")]
        public bool movement = true;
        public bool animationActive = true;
        public bool enemyDetection = true;

        [Header("Dependancies")]
        [SerializeField] private Image healthBar;
        [SerializeField] private TextMeshProUGUI healthText;

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

        private float maxHealthWithUpgrades;
        private float healthRegenPerSecond;
        private CanvasScript canvasScript;
        private bool startGestureTriggered;
        private bool startGestureArmed;
        private const float StartDragThreshold = 8f;

        private void Awake()
        {
            canShoot = true;

            // Set player health to the max health at the start
            currentHealth = originalHealth;
            originalDamage = GetComponentInChildren<WeaponScript>().damage;
            originalMoveSpeed = fwdMoveSpeed;
            RefreshUpgradeStats();
        }


        private void Start()
        {
            playerAnimator = GetComponentInChildren<Animator>();
            playerAnimator.SetBool("PlayerIsDead", false);
            weaponManager = GetComponent<WeaponManager>();
            canvasScript = FindFirstObjectByType<CanvasScript>();

            moveSensitivity = PlayerPrefs.GetFloat("moveSensitivity", 1f);  // Get move sensitivity from PlayerPrefs

            previousPosition = transform.position;
            playerMesh = transform.GetChild(0);

            extraHelpWeaponScript = new List<WeaponScript>();
            healthText.text = currentHealth.ToString("N0");
            RefreshUpgradeStats();
        }

        private void Update()
        {
            currentWeaponScript = GetComponentInChildren<WeaponScript>();

            // Update runtime variables
            currentHealth = UpdateHealth();
            currentWeapon = weaponManager.currentWeapon;

            if (currentWeaponScript != null)
            {
                currentDamage = currentWeaponScript.damage;
                currentFireRate = currentWeaponScript.fireRate;
            }

            if (TimeManager.isGameRunning == true && winDancePlayed == false) RotateTowardEnemy();

            HandleAnimation();
            ApplyHealthRegen();
        }

        private void FixedUpdate()
        {
            if (!TimeManager.isGameRunning || TimeManager.timeFactor <= 0f)
            {
                TryStartGameFromHorizontalInput();
                return;
            }

            if (TimeManager.Instance.isForwardMarchScene == true)
            {
                transform.position += Vector3.forward * fwdMoveSpeed / 100f * TimeManager.timeFactor;
                if (isDead == true) fwdMoveSpeed = 0;
                enemyDetection = false;
                playerAnimator.SetBool("WalkFwd", true);
            }
            else enemyDetection = true;

            if (CanvasScript.isGameOver || winDancePlayed) // Add winDancePlayed to stop movement
            {
                fwdMoveSpeed = 0;
                movement = false; // Disable horizontal movement
                playerAnimator.SetBool("WalkFwd", false); // Stop forward walk animation
                return; // Stop further fixed update logic for movement/input
            }
            else if (TimeManager.Instance.isForwardMarchScene == true)
            {
                transform.position += Vector3.forward * fwdMoveSpeed / 100f * TimeManager.timeFactor;
                if (isDead == true) fwdMoveSpeed = 0;
                enemyDetection = false;
                playerAnimator.SetBool("WalkFwd", true);
            }
            else enemyDetection = true;

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
            if (movement == false) return;
            float newX = Mathf.Clamp(transform.position.x + deltaX * moveSensitivity / moveSensitivity_Devision, xRange.x, xRange.y);    // Clamp player postion
            transform.position = Vector3.Lerp(transform.position, new Vector3(newX, transform.position.y, transform.position.z), Time.deltaTime * movementSmoothness * TimeManager.timeFactor);
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
            if (TimeManager.Instance.isForwardMarchScene == false && winDancePlayed == false) playerAnimator.SetBool("WalkFwd", false);

            isMoving = Mathf.Abs(transform.position.x - previousPosition.x) > 0.01f;
            playerAnimator.SetBool("IsMoving", isMoving);

            if (isMoving == true && winDancePlayed == false)
            {
                if (transform.position.x > previousPosition.x) dir = 1;
                else if (transform.position.x < previousPosition.x) dir = -1;

                playerAnimator.SetInteger("MoveDirection", dir);
            }

            previousPosition = transform.position;
        }


        public void PlayWinDance()
        {
            if (winDancePlayed == true) return;
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
                currentHealth = 0;
                isDead = true;
                sharkAnim.SetTrigger("Die");

                //playerAnimator.SetTrigger("PlayerIsDead");
                winDancePlayed = false;
                //二쎌쑝硫??좊땲 ?띾룄 0?쇰줈

            }

            float maxHealth = maxHealthWithUpgrades > 0f ? maxHealthWithUpgrades : originalHealth;
            if (healthBar) healthBar.fillAmount = currentHealth / maxHealth;
            healthText.text = currentHealth.ToString("N0");

            return currentHealth;
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
            isDead = false;
            winDancePlayed = false;  
            startGestureTriggered = false;
            startGestureArmed = false;

            // ?좊땲硫붿씠???꾩쟾 珥덇린??DeathAnim ?덉텧)
            if (playerAnimator)
            {
                sharkAnim.SetTrigger("Walk");
            }

            // 踰??ъ젒珥?荑?珥덇린??
            lastWallTouchTime = 0f;

            // ?대룞/?꾪닾 蹂듦뎄
            movement = true;
            canShoot = true;

            // ?ㅼ떆 ?섍컻????
            foreach (var w in GetComponentsInChildren<WeaponScript>(true))
                w.ResetShooting();
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
            fwdMoveSpeed = originalMoveSpeed;
            RefreshUpgradeStats();
        }
        public void RefreshUpgradeStats()
        {
            if (UpgradeStatManager.S == null) return;

            maxHealthWithUpgrades = UpgradeStatManager.S.ApplyToBase(UpgradeStatManager.UpgradeType.HP, originalHealth);
            currentHealth = maxHealthWithUpgrades;

            if (healthBar) healthBar.fillAmount = currentHealth / maxHealthWithUpgrades;
            if (healthText) healthText.text = currentHealth.ToString("N0");

            float regen = UpgradeStatManager.S.GetStat(UpgradeStatManager.UpgradeType.HP_REGEN);
            healthRegenPerSecond = UpgradeStatManager.S.GetValueType(UpgradeStatManager.UpgradeType.HP_REGEN) == ValueType.Percent
                ? maxHealthWithUpgrades * (regen / 100f)
                : regen;

            float projectileSpeedValue = UpgradeStatManager.S.GetStat(UpgradeStatManager.UpgradeType.PROJECTILE_SPEED);
            BulletScript.ApplyProjectileSpeedUpgrade(projectileSpeedValue,
                UpgradeStatManager.S.GetValueType(UpgradeStatManager.UpgradeType.PROJECTILE_SPEED));
        }

        private void ApplyHealthRegen()
        {
            if (!TimeManager.isGameRunning) return;
            if (healthRegenPerSecond <= 0f) return;

            float maxHealth = maxHealthWithUpgrades > 0f ? maxHealthWithUpgrades : originalHealth;
            if (currentHealth >= maxHealth) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegenPerSecond * Time.deltaTime);
        }




    }
}









