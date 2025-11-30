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


        private void Awake()
        {
            canShoot = true;

            // Set player health to the max health at the start
            currentHealth = originalHealth;
            originalDamage = GetComponentInChildren<WeaponScript>().damage;
        }


        private void Start()
        {
            playerAnimator = GetComponentInChildren<Animator>();
            playerAnimator.SetBool("PlayerIsDead", false);
            weaponManager = GetComponent<WeaponManager>();          

            moveSensitivity = PlayerPrefs.GetFloat("moveSensitivity", 1f);  // Get move sensitivity from PlayerPrefs

            previousPosition = transform.position;
            playerMesh = transform.GetChild(0);

            extraHelpWeaponScript = new List<WeaponScript>();
            healthText.text = currentHealth.ToString("N0");
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
        }

        private void FixedUpdate()
        {
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
            if(currentHealth <= 0)
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
                //죽으면 애니 속도 0으로
                
            }

            healthBar.fillAmount = currentHealth / 100;
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

    }
}