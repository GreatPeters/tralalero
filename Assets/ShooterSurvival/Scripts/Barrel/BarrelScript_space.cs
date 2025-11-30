using UnityEngine;
using TMPro;

namespace IndianOceanAssets.ShooterSurvival
{
    public class BarrelScript_space : MonoBehaviour
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

        public EffectOverlayScript effectOverlayVignette;

        /* Private references */
        private TextMeshProUGUI barrelHealthUI;                  // UI element to display barrel health
        private PlayerScript playerScript;
        private WeaponScript weaponScript;
        private SpriteRenderer spriteRenderer;
        private BulletPooler bulletPooler;                      // Reference to the bullet pooler for managing bullets
        private WeaponManager weaponManager;                    // Reference to the weapon manager for handling weapons
        private CameraShake cameraShake;                      // Reference to the camera shake script for visual effects
        private AudioSource barrelAudioSource;

        private float currentHealth;                            // Current health of the barrel
        private float barrelHealth;                              // Max health of the barrel based on type
        private ExplodeScript_space explodeScript;                    // Reference to the explode script for destruction
        public float deathRadius = 1.5f;                    // Explosion radius for the barrel when it dies
        public float deathDamage = 50f;                     // Explosion damage dealt to nearby entities

        public GameObject bulletHitFX;
        public AudioClip barrelHitSFX;


        private void Start()
        {
            barrelHealthUI = GetComponentInChildren<TextMeshProUGUI>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>();
            weaponManager = GameObject.FindGameObjectWithTag("Player").GetComponent<WeaponManager>();
            bulletPooler = FindFirstObjectByType<BulletPooler>();
            effectOverlayVignette = GameObject.FindGameObjectWithTag("VolumeTag").GetComponent<EffectOverlayScript>();
            explodeScript = GetComponentInParent<ExplodeScript_space>();
            barrelAudioSource = GetComponent<AudioSource>();
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
            barrelHealthUI.text = currentHealth.ToString();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("BulletTag"))
            {
                ReduceBarrelHealth();
            }
        }

        private void ReduceBarrelHealth()
        {
            currentHealth -= playerScript.currentDamage;

            Transform hitPos = transform.GetChild(transform.childCount - 1);

            barrelAudioSource.PlayOneShot(barrelHitSFX);
            GameObject hitfx = Instantiate(bulletHitFX, hitPos);
            Destroy(hitfx, hitfx.GetComponent<ParticleSystem>().main.duration);

            if (currentHealth <= 0)
            {
                weaponManager.ChangeWeapon((int)barrelType);

                currentHealth = 0;
                explodeScript.Explode();

                // effect nearby entities
                Collider[] colliders = Physics.OverlapSphere(transform.position, deathRadius);
                foreach (var obj in colliders)
                {
                    if (obj.CompareTag("EnemyTag"))
                    {
                        EnemyScript_space enemyScript = obj.GetComponent<EnemyScript_space>();
                        enemyScript.EnemyDeath();
                    }

                    if (obj.CompareTag("Player")) playerScript.currentHealth -= deathDamage;
                }
            }
        }
    }
}