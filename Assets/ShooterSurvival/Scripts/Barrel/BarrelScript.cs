using TMPro;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public enum BarrelType { Pistol, Rifle, Shotgun, Minigun }

    public class BarrelScript : MonoBehaviour
    {
        [Header("Weapon Type")]
        public BarrelType barrelType;                     // Specifies the type of the barrel (Pistol, Rifle, Shotgun, Minigun)

        [Header("Weapon-Specific Health")]
        public float PistolBarrelHealth = 50f;
        public float RifleBarrelHealth = 80f;
        public float ShotgunBarrelHealth = 100f;
        public float MinigunBarrelHealth = 150f;

        [Header("Weapon Sprites")]
        public Sprite PistolBarrelSprite;
        public Sprite RifleBarrelSprite;
        public Sprite ShotgunBarrelSprite;
        public Sprite MinigunBarrelSprite;

        [Header("General Params")]
        public float barrelDamage;                           // Damage dealt by the barrel

        public EffectOverlayScript effectOverlayVignette;   // Effect overlay for health reduction

        /* Private references */
        private TextMeshProUGUI barrelHealthUI;                  // UI element to display barrel health
        private PlayerScript playerScript;
        private WeaponScript weaponScript;
        private SpriteRenderer spriteRenderer;
        private BulletPooler bulletPooler;                      // Reference to the bullet pooler for managing bullets
        private EnemyPooler enemyPooler;                        // Reference to the enemy pooler for managing enemies
        private FX_pooler fX_Pooler;
        private WeaponManager weaponManager;                    // Reference to the weapon manager for handling weapons
        private CameraShake cameraShake;                      // Reference to the camera shake script for visual effects

        private float currentHealth;                            // Current health of the barrel
        private float barrelHealth;                              // Max health of the barrel based on type
        private ExplodeScript explodeScript;                    // Reference to the explode script for destruction
        public float deathRadius = 1.5f;                    // Explosion radius for the barrel when it dies
        public float deathDamage = 50f;                     // Explosion damage dealt to nearby entities

        public AudioClip barrelHitSFX;
        private AudioSource barrelAudioSource;

        private void Start()
        {
            barrelHealthUI = GetComponentInChildren<TextMeshProUGUI>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>();
            weaponManager = GameObject.FindGameObjectWithTag("Player").GetComponent<WeaponManager>();
            bulletPooler = FindFirstObjectByType<BulletPooler>();
            enemyPooler = FindFirstObjectByType<EnemyPooler>().GetComponent<EnemyPooler>();
            effectOverlayVignette = GameObject.FindGameObjectWithTag("VolumeTag").GetComponent<EffectOverlayScript>();
            explodeScript = GetComponentInParent<ExplodeScript>();
            barrelAudioSource = GetComponent<AudioSource>();
            fX_Pooler = FindFirstObjectByType<FX_pooler>().GetComponent<FX_pooler>();
            cameraShake = FindFirstObjectByType<CameraShake>().GetComponent<CameraShake>();

            // Assign health and sprite based on the selected barrel type
            switch (barrelType)
            {
                case BarrelType.Pistol:
                    barrelHealth = PistolBarrelHealth;
                    spriteRenderer.sprite = PistolBarrelSprite;
                    break;
                case BarrelType.Rifle:
                    barrelHealth = RifleBarrelHealth;
                    spriteRenderer.sprite = RifleBarrelSprite;
                    break;
                case BarrelType.Shotgun:
                    barrelHealth = ShotgunBarrelHealth;
                    spriteRenderer.sprite = ShotgunBarrelSprite;
                    break;
                case BarrelType.Minigun:
                    barrelHealth = MinigunBarrelHealth;
                    spriteRenderer.sprite = MinigunBarrelSprite;
                    break;
            }

            currentHealth = barrelHealth;  // Set the initial current health to max health
        }

        private void Update()
        {
            // Update HP UI for barrel
            barrelHealthUI.text = currentHealth.ToString();
        }

        void OnTriggerEnter(Collider other)
        {

            // barrel gets hit by bullet
            if (other.CompareTag("BulletTag"))
            {
                ReduceBarrelHealth();

                // Spawn VFX at the hit position
                fX_Pooler.GetObjectFromPool_FX("Barrel_HitEffect", transform);

                // Trigger camera shake effect for explosion
                cameraShake.Shake(0.2f, 0.1f, 2f, 1f);
            }

            // barrel touches the player
            if (other.CompareTag("Player"))
            {
                playerScript.currentHealth -= barrelDamage;
                effectOverlayVignette.NerfOverlay();
            }

            // barrel touches Extra Help entity
            if (other.CompareTag("ExtraHelpTag"))
            {
                ExtraHelpBuffScript extraHelpBuffScript = other.GetComponent<ExtraHelpBuffScript>();
                extraHelpBuffScript.currentHealth -= barrelDamage;
            }

            // barrel goes out of camera
            if (other.CompareTag("DestroyerTag"))
            {
                Destroy(gameObject);
            }
        }

        private void ReduceBarrelHealth()
        {
            if (weaponManager == null) print("weaponManager not found!");

            weaponScript = GameObject.FindGameObjectWithTag("WeaponTag").GetComponent<WeaponScript>();
            currentHealth -= weaponScript.damage;                                   // Reduce barrel health by weapon damage

            barrelAudioSource.clip = barrelHitSFX;
            barrelAudioSource.Play();

            // Scale the flame particles based on the health percentage
            fX_Pooler.GetObjectFromPool_FX("Barrel_FlameParticles", transform);

            // Check if barrel health reaches 0 and trigger explosion
            if (currentHealth <= 0)
            {
                weaponManager.ChangeWeapon((int)barrelType);     // Change player weapon when barrel dies

                currentHealth = 0;
                explodeScript.Explode();                        // explode


                var explosionPos = transform.position;

                // Effect nearby entities
                Collider[] colliders = Physics.OverlapSphere(explosionPos, deathRadius);
                foreach (var obj in colliders)
                {

                    // enemy is near barrel
                    if (obj.CompareTag("EnemyTag"))
                    {
                        EnemyScript enemyScript = obj.GetComponent<EnemyScript>();
                        enemyPooler.ReturnObjectToPool_Enemy(enemyScript.enemyType, obj.gameObject);
                        playerScript.playerScore += enemyScript._score;                     // Add score to player for killing enemy
                    }

                    // player is enar barrel
                    if (obj.CompareTag("Player")) playerScript.currentHealth -= deathDamage;    // Damage player if within explosion range
                }
            }
        }
    }
}