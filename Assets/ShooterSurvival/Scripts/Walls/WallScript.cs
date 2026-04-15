using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

//占쌩곤옙
using UnityEngine.Localization.Components; // LocalizeStringEvent


namespace IndianOceanAssets.ShooterSurvival
{
    public enum WallType { BuffWall, NerfWall }
    public enum BuffType
    {
        HealthBoost, FireRateIncrease, ExtraHelp, att_normmal, attPer_normal, attackSpeed_normal, missileDistance_normal, hp_normal, hpPer_normal
    , tungtung_rare, boombar_rare, att_unique, attPer_unique, missileAdd_unique, attackSpeed_unique, missileDistance_unique, hp_unique, hpPer_unique
    }
    public enum NerfType { HealthReduce, FireRateReduce }

    public enum Rarity { Normal, Rare, Unique }

    public class WallScript : MonoBehaviour
    {
        //占쌩곤옙
        public LocalizeStringEvent statNameLoc; // 占싱몌옙 표占시울옙
        public TextMeshProUGUI statValueTmp; // 占쏙옙 표占시울옙
        public string tableName = "AllTexts";   // 占십곤옙 占쏙옙占쏙옙 占쏙옙占싱븝옙 占싱몌옙

        [Header("Type Params")]
        public WallType wallType;               // Type of wall (Buff or Nerf)
        public BuffType buffType;               // Type of buff for BuffWall
        public NerfType nerfType;               // Type of nerf for NerfWall                
        public bool isRandom;
        public Rarity rarity;

        [Header("Buff Wall Properties")]
        public Sprite healthBoostSpr;
        public Sprite fireRateIncreaseSpr;
        public Sprite extraHelpSpr;
        //占쏙옙 占싣뤄옙占쏙옙 占신깍옙 占싱뱄옙占쏙옙
        public Sprite attSpr;
        public Sprite attPercentSpr;
        public Sprite missileAddSpr;
        public Sprite attackSpeedSpr;
        public Sprite missileDistanceSpr;
        public Sprite hpSpr;
        public Sprite hpPercentSpr;
        public Sprite tungtungRareSpr;
        public Sprite boombarRareSpr;
        public Sprite attUniqueSpr;
        public Sprite attPerUniqueSpr;
        public Sprite missileAddUniqueSpr;
        public Sprite attackSpeedUniqueSpr;
        public Sprite distanceUniqueSpr;
        public Sprite hpUniqueSpr;
        public Sprite hpPerUniqueSpr;

        public int healthBoostAmt = 25;             // Amount of health boost
        public float fireRateIncMultipier = 4;      // Multiplier for fire rate increase
        public GameObject extraHelp;                // Prefab for Extra Help buff
        //占쏙옙 占싣뤄옙占쏙옙 占신깍옙 占쏙옙占쏙옙
        public float att;
        public float attPercent;
        public float missileAdd;
        public float attackSpeed;
        public float missileDistance;
        public float hp;
        public float hpPercent;
        public float tungtungAdd;
        public float boombarAdd;

        [Header("Nerf Wall Properties")]
        public Sprite healthReduceSpr;
        public Sprite fireRateReduceSpr;

        public int healthReduceAmt;                     // Amount of health reduction
        public float fireRateDecMultipier = 0.25f;      // Multiplier for fire rate reduction

        [Header("Dependencies")]
        public AudioClip buffSFX;
        public AudioClip nerfSFX;

        private EffectOverlayScript effectOverlayVignette;
        private AudioSource wallAudioSource;
        private float wallMoveSpeed = 1;                        // Speed at which the wall moves
        private SpriteRenderer currSprite;
        private PlayerScript playerScript;
        private WeaponManager weaponManager;
        private bool _initialized;



        // private void Start()
        // {
        //     transform.name += "_Z_" + transform.position.z.ToString();

        //     currSprite = GetComponentInChildren<SpriteRenderer>();
        //     playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>();
        //     effectOverlayVignette = GameObject.FindGameObjectWithTag("VolumeTag").GetComponent<EffectOverlayScript>();
        //     if (effectOverlayVignette == null) { print("not found"); }
        //     ;
        //     wallAudioSource = GetComponent<AudioSource>();

        //     SetRandomStat();
        //     SetStats();

        //     SetWallSprite();
        // }

        private void OnEnable()
        {
            _initialized = false;

            // Player ?뺣낫
            if (playerScript == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerScript = p.GetComponent<PlayerScript>();
            }

            // ?꾩쭅 Player ?놁쑝硫??湲?
            if (playerScript == null) return;

            InitWall();
        }

        private void InitWall()
        {
            transform.name += "_Z_" + transform.position.z.ToString();

            //?쇰떒 鍮꾪솢?깊솕
            currSprite = GetComponentInChildren<SpriteRenderer>();
            if (wallAudioSource == null) wallAudioSource = GetComponent<AudioSource>();

            if (effectOverlayVignette == null)
            {
                var v = GameObject.FindGameObjectWithTag("VolumeTag");
                if (v) effectOverlayVignette = v.GetComponent<EffectOverlayScript>();
            }

            SetRandomStat();
            SetStats();
            SetWallSprite();

            _initialized = true;
        }

        public void SetRandomStat()
        {
            if (isRandom == true)
            {
                if (rarity == Rarity.Normal)
                {
                    int rand = Random.Range(0, 6);
                    if (rand == 0) buffType = BuffType.att_normmal;
                    else if (rand == 1) buffType = BuffType.attPer_normal;
                    else if (rand == 2) buffType = BuffType.attackSpeed_normal;
                    else if (rand == 3) buffType = BuffType.missileDistance_normal;
                    else if (rand == 4) buffType = BuffType.hp_normal;
                    else if (rand == 5) buffType = BuffType.hpPer_normal;
                    wallType = WallType.BuffWall;
                }
                else if (rarity == Rarity.Rare)
                {
                    int rand = Random.Range(0, 2);
                    if (rand == 0) buffType = BuffType.tungtung_rare;
                    else if (rand == 1) buffType = BuffType.boombar_rare;
                    wallType = WallType.BuffWall;
                }
                else if (rarity == Rarity.Unique)
                {
                    int rand = Random.Range(0, 7);
                    if (rand == 0) buffType = BuffType.att_unique;
                    else if (rand == 1) buffType = BuffType.attPer_unique;
                    else if (rand == 2) buffType = BuffType.missileAdd_unique;
                    else if (rand == 3) buffType = BuffType.attackSpeed_unique;
                    else if (rand == 4) buffType = BuffType.missileDistance_unique;
                    else if (rand == 5) buffType = BuffType.hp_unique;
                    else if (rand == 6) buffType = BuffType.hpPer_unique;
                    wallType = WallType.BuffWall;
                }
            }
        }

        public void SetStats()
        {
            float playerOriginalDamage = playerScript.originalDamage;
            float playerOriginalHealth = playerScript.originalHealth;

            if (rarity == Rarity.Normal)
            {
                att = RollBonusValue("att", playerOriginalDamage, true);
                attPercent = RollBonusValue("attPercent", 0f, false);
                attackSpeed = RollBonusValue("attackSpeed", 0f, false);
                missileDistance = RollBonusValue("missileDistance", 0f, false);
                hp = RollBonusValue("hp", playerOriginalHealth, true);
                hpPercent = RollBonusValue("hpPercent", 0f, false);
            }
            else if (rarity == Rarity.Rare)
            {
                tungtungAdd = RollBonusValue("tungtungAdd", 0f, true);
                boombarAdd = RollBonusValue("boombarAdd", 0f, true);
            }
            else if (rarity == Rarity.Unique)
            {
                missileAdd = RollBonusValue("missileAdd", 0f, true);
                att = RollBonusValue("att", playerOriginalDamage, true);
                attPercent = RollBonusValue("attPercent", 0f, false);
                attackSpeed = RollBonusValue("attackSpeed", 0f, false);
                missileDistance = RollBonusValue("missileDistance", 0f, false);
                hp = RollBonusValue("hp", playerOriginalHealth, true);
                hpPercent = RollBonusValue("hpPercent", 0f, false);
            }
        }

        private float RollBonusValue(
            string stat,
            float baseValue,
            bool round)
        {
            if (!TryGetBonusRange(stat, out var min, out var max, out var valueType))
            {
                Debug.LogError($"[WallScript] Bonus range missing. rarity={rarity} stat={stat}");
                return 0f;
            }

            float v = Random.Range(min, max);

            if (valueType == BonusValueType.Ratio)
            {
                v = baseValue * v;
            }

            if (round) v = Mathf.Round(v);

            return v;
        }

        private bool TryGetBonusRange(string stat, out float min, out float max, out BonusValueType valueType)
        {
            min = 0f;
            max = 0f;
            valueType = BonusValueType.Value;

            if (!BonusTables.TryGet(rarity.ToString(), stat, out var row))
                return false;

            min = row.min;
            max = row.max;
            valueType = row.valueType;
            return true;
        }

        private void FixedUpdate()
        {
            // Wall movement
            if (!TimeManager.Instance.isForwardMarchScene) transform.Translate(-Vector3.forward * wallMoveSpeed * Time.deltaTime * TimeManager.timeFactor);
        }

        private void OnTriggerEnter(Collider other)
        {
            // wall moves out of camera range
            if (other.CompareTag("DestroyerTag"))
            {
                Destroy(gameObject); // Destroy the wall object
            }

            // player enters the wall
            else if (other.CompareTag("Player"))
            {
                if (playerScript.lastWallTouchTime == 0 || Time.time - playerScript.lastWallTouchTime > 1f)
                {
                    playerScript.lastWallTouchTime = Time.time;           // Update the last time the wall was touched
                    ApplyWallEffect();                                      // Apply the effect based on the wall's type
                    gameObject.GetComponent<Collider>().isTrigger = false;  // Disable trigger once applied

                    gameObject.SetActive(false);
                }
            }
        }

        public void SetWallSprite()
        {
            // Set the correct sprite based on the wall's type
            Sprite selectedSprite = null;

            float bonusValue = 0;
            bool isPercent = false;

            switch (wallType)
            {
                case WallType.BuffWall:
                    selectedSprite = buffType switch
                    {
                        BuffType.HealthBoost => healthBoostSpr,                 // Set sprite for health boost
                        BuffType.FireRateIncrease => fireRateIncreaseSpr,       // Set sprite for fire rate increase
                        BuffType.ExtraHelp => extraHelpSpr,                     // Set sprite for extra help                       
                        _ => null
                    };
                    break;
                case WallType.NerfWall:
                    selectedSprite = nerfType switch
                    {
                        NerfType.HealthReduce => healthReduceSpr,               // Set sprite for health reduction
                        NerfType.FireRateReduce => fireRateReduceSpr,           // Set sprite for fire rate reduction
                        _ => null
                    };
                    break;
            }

            if (wallType == WallType.BuffWall)
            {
                if (buffType == BuffType.att_normmal || buffType == BuffType.att_unique)
                {
                    bonusValue = att;
                    isPercent = false;
                }
                else if (buffType == BuffType.attPer_normal || buffType == BuffType.attPer_unique)
                {
                    bonusValue = attPercent;
                    isPercent = true;
                }
                else if (buffType == BuffType.missileAdd_unique)
                {
                    bonusValue = missileAdd;
                    isPercent = false;
                }
                else if (buffType == BuffType.attackSpeed_normal || buffType == BuffType.attackSpeed_unique)
                {
                    bonusValue = attackSpeed;
                    isPercent = true;
                }
                else if (buffType == BuffType.missileDistance_normal || buffType == BuffType.missileDistance_unique)
                {
                    bonusValue = missileDistance;
                    isPercent = true;
                }
                else if (buffType == BuffType.hp_normal || buffType == BuffType.hp_unique)
                {
                    bonusValue = hp;
                    isPercent = false;
                }
                else if (buffType == BuffType.hpPer_normal || buffType == BuffType.hpPer_unique)
                {
                    bonusValue = hpPercent;
                    isPercent = true;
                }
                else if (buffType == BuffType.tungtung_rare)
                {
                    bonusValue = tungtungAdd;
                    isPercent = false;
                }
                else if (buffType == BuffType.boombar_rare)
                {
                    bonusValue = boombarAdd;
                    isPercent = false;
                }
            }

            currSprite.sprite = selectedSprite;                                 // Apply the selected sprite
            SetBonusValueText(bonusValue, isPercent);
            UpdateStatUI(buffType, bonusValue, isPercent); // 占쏙옙 占쏙옙占시띰옙占쏙옙占쏙옙占?占싱몌옙 + 占쏙옙 占쏙옙占쏙옙

        }

        private void ApplyWallEffect()
        {
            // Apply effects based on wall type and specific buff/nerf.

            GameObject fireRateDisplay = GameObject.FindGameObjectWithTag("FireRateDisplayTag");
            Animator fireRateDisplayAnimator = fireRateDisplay.GetComponent<Animator>();
            SpriteRenderer fireRateDisplaySpr = fireRateDisplay.GetComponentInChildren<SpriteRenderer>();
            TextMeshProUGUI fireRateDisplayText = fireRateDisplay.GetComponentInChildren<TextMeshProUGUI>();

            Debug.Log(buffType);

            switch (wallType)
            {
                case WallType.BuffWall:
                    if (buffType == BuffType.HealthBoost)
                    {
                        playerScript.currentHealth += healthBoostAmt;       // Increase player's health
                        //effectOverlayVignette.BuffOverlay();
                    }
                    else if (buffType == BuffType.FireRateIncrease)
                    {
                        weaponManager = GameObject.FindGameObjectWithTag("Player").GetComponent<WeaponManager>();
                        weaponManager.currentWeapon.GetComponentInChildren<WeaponScript>().fireRate *= fireRateIncMultipier; // Increase fire rate
                        //effectOverlayVignette.BuffOverlay();

                        fireRateDisplaySpr.sprite = fireRateIncreaseSpr;            // Update fire rate display sprite
                        fireRateDisplayText.text = "x" + fireRateIncMultipier;      // Update fire rate display text
                        fireRateDisplayAnimator.SetTrigger("FireTextPopIn");
                    }
                    else if (buffType == BuffType.ExtraHelp)
                    {
                        playerScript.extraHelpCount++;
                        SpawnTungTung(HelpType.Tungtungtung);
                        //effectOverlayVignette.BuffOverlay();
                    }
                    else if ((buffType == BuffType.att_normmal) || (buffType == BuffType.att_unique))
                    {
                        weaponManager = GameObject.FindGameObjectWithTag("Player").GetComponent<WeaponManager>();
                        var weaponScript = weaponManager.currentWeapon.GetComponentInChildren<WeaponScript>();
                        weaponScript.damage += att; // WallScript占쏙옙 att 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쌥력울옙 占쏙옙占쏙옙

                        //effectOverlayVignette.BuffOverlay();
                        //wallAudioSource.PlayOneShot(buffSFX);
                    }
                    else if ((buffType == BuffType.attPer_normal) || (buffType == BuffType.attPer_unique))
                    {
                        weaponManager = GameObject.FindGameObjectWithTag("Player").GetComponent<WeaponManager>();
                        var weaponScript = weaponManager.currentWeapon.GetComponentInChildren<WeaponScript>();
                        weaponScript.damage *= (1 + attPercent * 0.01f); // WallScript占쏙옙 att 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쌥력울옙 占쏙옙占쏙옙

                        //effectOverlayVignette.BuffOverlay();
                        //wallAudioSource.PlayOneShot(buffSFX);
                    }
                    else if ((buffType == BuffType.attackSpeed_normal) || (buffType == BuffType.attackSpeed_unique))
                    {
                        weaponManager = GameObject.FindGameObjectWithTag("Player").GetComponent<WeaponManager>();
                        var weaponScript = weaponManager.currentWeapon.GetComponentInChildren<WeaponScript>();
                        weaponScript.fireRate += weaponScript.originalFireRate * attackSpeed * 0.01f;

                        //.BuffOverlay();
                        //wallAudioSource.PlayOneShot(buffSFX);
                    }
                    else if ((buffType == BuffType.missileDistance_normal) || (buffType == BuffType.missileDistance_unique))
                    {
                        BulletScript.bulletRange += BulletScript.originalBulletRange * missileDistance * 0.01f; // WallScript占쏙옙 missileDistance 占쏙옙占쏙옙 占싼억옙 占쏙옙타占쏙옙占?占쏙옙占쏙옙
                        Debug.Log(BulletScript.bulletRange);

                        //effectOverlayVignette.BuffOverlay();
                        //wallAudioSource.PlayOneShot(buffSFX);
                    }
                    else if ((buffType == BuffType.hp_normal) || (buffType == BuffType.hp_unique))
                    {
                        playerScript.currentHealth += hp;       // Increase player's health
                        playerScript.UpdateHealth();
                        //effectOverlayVignette.BuffOverlay();
                    }
                    else if ((buffType == BuffType.hpPer_normal) || buffType == BuffType.hpPer_unique)
                    {
                        playerScript.currentHealth *= (1 + hpPercent * 0.01f); // WallScript占쏙옙 att 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쌥력울옙 占쏙옙占쏙옙
                        playerScript.UpdateHealth();
                        //effectOverlayVignette.BuffOverlay();
                    }

                    else if ((buffType == BuffType.missileAdd_unique))
                    {
                        weaponManager = GameObject.FindGameObjectWithTag("Player").GetComponent<WeaponManager>();
                        var weaponScript = weaponManager.currentWeapon.GetComponentInChildren<WeaponScript>();

                        // 占쏙옙占쏙옙: bulletPositions 占썼열占쏙옙 占쏙옙占?占쏙옙占쏙옙占쏙옙 占시몌옙占쏙옙 占쏙옙占쏙옙 占쌩곤옙 占십울옙
                        weaponScript.bulletCount += (int)missileAdd;
                        playerScript.currentHealth = 1f;
                        playerScript.UpdateHealth();
                        Debug.Log(weaponScript.bulletCount);

                        //effectOverlayVignette.BuffOverlay();
                        //wallAudioSource.PlayOneShot(buffSFX);
                    }
                    else if ((buffType == BuffType.tungtung_rare))
                    {
                        //占쏙옙占쏙옙占쏙옙 占쌩곤옙
                        //effectOverlayVignette.BuffOverlay();
                        //wallAudioSource.PlayOneShot(buffSFX);

                        playerScript.extraHelpCount++;
                        SpawnTungTung(HelpType.Tungtungtung);
                    }
                    else if ((buffType == BuffType.boombar_rare))
                    {
                        //占쌌바몌옙 占쌩곤옙
                        //effectOverlayVignette.BuffOverlay();
                        //wallAudioSource.PlayOneShot(buffSFX);

                        playerScript.extraHelpCount++;
                        SpawnBoomBarDino(HelpType.Boombardino);
                    }
                    //wallAudioSource.PlayOneShot(buffSFX);
                    AudioSource.PlayClipAtPoint(buffSFX, transform.position);

                    effectOverlayVignette.BuffOverlay();

                    break;

                case WallType.NerfWall:
                    if (nerfType == NerfType.HealthReduce)
                    {
                        playerScript.currentHealth -= healthReduceAmt;          // Reduce player's health
                        effectOverlayVignette.NerfOverlay();
                    }

                    else if (nerfType == NerfType.FireRateReduce)
                    {
                        weaponManager = GameObject.FindGameObjectWithTag("Player").GetComponent<WeaponManager>();
                        weaponManager.currentWeapon.GetComponentInChildren<WeaponScript>().fireRate *= fireRateDecMultipier; // Reduce fire rate
                        effectOverlayVignette.NerfOverlay();

                        fireRateDisplaySpr.sprite = fireRateReduceSpr;              // Update fire rate display sprite
                        fireRateDisplayText.text = "x" + fireRateDecMultipier;      // Update fire rate display text
                        fireRateDisplayAnimator.SetTrigger("FireTextPopIn");
                    }

                    wallAudioSource.PlayOneShot(nerfSFX);                           // Play the nerf sound effect
                    break;
            }
        }

        private void SpawnTungTung(HelpType helptype = HelpType.Tungtungtung)
        {
            // Spawn Extra Help Buff

            if (extraHelp == null || playerScript == null) return;

            // Define spawn position relative to player
            Vector3 spawnOffset = new Vector3(1.5f, 0, -0.75f);
            Vector3 spawnPosition = playerScript.transform.position + spawnOffset;

            GameObject GO = Instantiate(GameManager.S.extraHelp_TungTungTung, spawnPosition, Quaternion.identity);
            GO.GetComponent<ExtraHelpBuffScript>().spawnIndex = playerScript.extraHelpCount - 1; // Set spawn index for identification
            GO.GetComponent<ExtraHelpBuffScript>().helpType = helptype;
            playerScript.extraHelpWeaponScript.Add(GO.GetComponentInChildren<WeaponScript>());
        }

        private void SpawnBoomBarDino(HelpType helptype = HelpType.Boombardino)
        {
            // Spawn Extra Help Buff

            if (extraHelp == null || playerScript == null) return;

            // Define spawn position relative to player
            Vector3 spawnOffset = new Vector3(1.5f, 0, -0.75f);
            Vector3 spawnPosition = playerScript.transform.position + spawnOffset;

            GameObject GO = Instantiate(GameManager.S.extraHelp_BoomBarDino, spawnPosition, Quaternion.identity);
            GO.GetComponent<ExtraHelpBuffScript>().spawnIndex = playerScript.extraHelpCount - 1; // Set spawn index for identification
            GO.GetComponent<ExtraHelpBuffScript>().helpType = helptype;
            playerScript.extraHelpWeaponScript.Add(GO.GetComponentInChildren<WeaponScript>());
        }

        private void SetBonusValueText(float volume, bool percent = false)
        {
            // 占쏙옙 占쏙옙크占쏙옙트占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙트(wall_att_normal 占쏙옙占쏙옙) 占쏙옙占쏙옙占쏙옙占쏙옙 TMP 占쌔쏙옙트 찾占쏙옙
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 2)
            {
                texts[1].text = "+" + volume.ToString(); // 占쏙옙 占쏙옙째 占쌔쏙옙트占쏙옙 占쏙옙占쏙옙 占쏙옙 占쏙옙占쏙옙
                if (percent == true)
                {
                    texts[1].text += "%"; // 占쌜쇽옙트 표占쏙옙 占쌩곤옙
                }
            }
            else
            {
                Debug.LogWarning("TMP 占쌔쏙옙트占쏙옙 2占쏙옙 占싱삼옙 占쏙옙占쏙옙!");
            }
        }

        private string KeyFor(BuffType bt)
        {
            switch (bt)
            {
                case BuffType.att_normmal:
                case BuffType.att_unique: return "att";

                case BuffType.attPer_normal:
                case BuffType.attPer_unique: return "attPercent";

                case BuffType.attackSpeed_normal:
                case BuffType.attackSpeed_unique: return "missileSpeed";   // 占쏙옙占싱븝옙占쏙옙 占쏙옙 占싱몌옙占쏙옙占쏙옙 占쏙옙占쏙옙

                case BuffType.missileDistance_normal:
                case BuffType.missileDistance_unique: return "missileDistance";

                case BuffType.hp_normal:
                case BuffType.hp_unique: return "hp";

                case BuffType.hpPer_normal:
                case BuffType.hpPer_unique: return "hpPercent";

                case BuffType.missileAdd_unique: return "missileAdd";
                case BuffType.tungtung_rare: return "tungtungAdd";
                case BuffType.boombar_rare: return "boombarAdd";
                default: return "att";
            }
        }

        private void UpdateStatUI(BuffType bt, float value, bool isPercent)
        {
            // 占싱몌옙(占쏙옙占시띰옙占쏙옙占쏙옙)
            statNameLoc.StringReference.SetReference(tableName, KeyFor(bt));
            statNameLoc.RefreshString();

            string formattedValue = isPercent
                ? $"+{Mathf.RoundToInt(value)}%"
                : $"+{Mathf.RoundToInt(value)}";

            if (statValueTmp != null)
            {
                statValueTmp.text = formattedValue;
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (text != null && text.gameObject.name == "Value_Text")
                {
                    text.text = formattedValue;
                }
            }
        }

        public void RerollTWallType(BuffType exceptBuffType, int iterationCount = 30)
        {
            if (!isRandom) return;

            SetRandomStat();

            int iteration = 0;
            while (buffType == exceptBuffType && iteration < iterationCount)
            {
                SetRandomStat();
                iteration++;
            }

            SetStats();
            SetWallSprite();
        }

    }


}
