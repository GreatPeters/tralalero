using System.Collections;
using TMPro;
using UnityEngine;

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

        public int healthBoostAmt = 25;             // Amount of health boost
        public float fireRateIncMultipier = 4;      // Multiplier for fire rate increase
        public GameObject extraHelp;                // Prefab for Extra Help buff
        //占쏙옙 占싣뤄옙占쏙옙 占신깍옙 占쏙옙占쏙옙
        private float bonusValue;
        private bool isPercent;

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
        private void OnEnable()
        {
            StartCoroutine(MoveOutsideForwardMarch());

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
            //?쇰떒 鍮꾪솢?깊솕
            currSprite = GetComponentInChildren<SpriteRenderer>();
            if (wallAudioSource == null) wallAudioSource = GetComponent<AudioSource>();
            if (weaponManager == null) weaponManager = playerScript.GetComponent<WeaponManager>();

            SetRandomStat();
            SetStats();
            SetWallSprite();
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

            bonusValue = buffType switch
            {
                BuffType.att_normmal or BuffType.att_unique =>
                    RollBonusValue("att", playerOriginalDamage, true),
                BuffType.attPer_normal or BuffType.attPer_unique =>
                    RollBonusValue("attPercent", 0f, false),
                BuffType.missileAdd_unique =>
                    RollBonusValue("missileAdd", 0f, true),
                BuffType.attackSpeed_normal or BuffType.attackSpeed_unique =>
                    RollBonusValue("attackSpeed", 0f, false),
                BuffType.missileDistance_normal or BuffType.missileDistance_unique =>
                    RollBonusValue("missileDistance", 0f, false),
                BuffType.hp_normal or BuffType.hp_unique =>
                    RollBonusValue("hp", playerOriginalHealth, true),
                BuffType.hpPer_normal or BuffType.hpPer_unique =>
                    RollBonusValue("hpPercent", 0f, false),
                BuffType.tungtung_rare =>
                    RollBonusValue("tungtungAdd", 0f, true),
                BuffType.boombar_rare =>
                    RollBonusValue("boombarAdd", 0f, true),
                _ => 0f
            };

            isPercent = buffType is
                BuffType.attPer_normal or
                BuffType.attPer_unique or
                BuffType.attackSpeed_normal or
                BuffType.attackSpeed_unique or
                BuffType.missileDistance_normal or
                BuffType.missileDistance_unique or
                BuffType.hpPer_normal or
                BuffType.hpPer_unique;
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

        private IEnumerator MoveOutsideForwardMarch()
        {
            while (TimeManager.Instance == null)
                yield return null;

            if (TimeManager.Instance.isForwardMarchScene)
                yield break;

            var fixedUpdate = new WaitForFixedUpdate();
            while (true)
            {
                yield return fixedUpdate;
                transform.Translate(
                    -Vector3.forward *
                    wallMoveSpeed *
                    Time.fixedDeltaTime *
                    TimeManager.timeFactor);
            }
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
            Sprite selectedSprite = wallType switch
            {
                WallType.BuffWall => buffType switch
                {
                    BuffType.HealthBoost => healthBoostSpr,
                    BuffType.FireRateIncrease => fireRateIncreaseSpr,
                    BuffType.ExtraHelp => extraHelpSpr,
                    _ => null
                },
                WallType.NerfWall => nerfType switch
                {
                    NerfType.HealthReduce => healthReduceSpr,
                    NerfType.FireRateReduce => fireRateReduceSpr,
                    _ => null
                },
                _ => null
            };

            currSprite.sprite = selectedSprite;
            SetBonusValueText(bonusValue, isPercent);
            UpdateStatUI(buffType, bonusValue, isPercent);
        }

        private void ApplyWallEffect()
        {
            switch (wallType)
            {
                case WallType.BuffWall:
                    if (buffType == BuffType.HealthBoost)
                    {
                        playerScript.currentHealth += healthBoostAmt;       // Increase player's health
                    }
                    else if (buffType == BuffType.FireRateIncrease)
                    {
                        WeaponScript weaponScript = GetCurrentWeaponScript();
                        if (weaponScript == null)
                            return;

                        weaponScript.fireRate *= fireRateIncMultipier;
                        ShowFireRateModifier(fireRateIncreaseSpr, fireRateIncMultipier);
                    }
                    else if (buffType == BuffType.ExtraHelp)
                    {
                        playerScript.extraHelpCount++;
                        SpawnExtraHelp(HelpType.Tungtungtung);
                    }
                    else if ((buffType == BuffType.att_normmal) || (buffType == BuffType.att_unique))
                    {
                        WeaponScript weaponScript = GetCurrentWeaponScript();
                        if (weaponScript == null)
                            return;
                        weaponScript.damage += bonusValue;
                    }
                    else if ((buffType == BuffType.attPer_normal) || (buffType == BuffType.attPer_unique))
                    {
                        WeaponScript weaponScript = GetCurrentWeaponScript();
                        if (weaponScript == null)
                            return;
                        weaponScript.damage *= 1f + bonusValue * 0.01f;
                    }
                    else if ((buffType == BuffType.attackSpeed_normal) || (buffType == BuffType.attackSpeed_unique))
                    {
                        WeaponScript weaponScript = GetCurrentWeaponScript();
                        if (weaponScript == null)
                            return;
                        weaponScript.fireRate += weaponScript.originalFireRate * bonusValue * 0.01f;
                    }
                    else if ((buffType == BuffType.missileDistance_normal) || (buffType == BuffType.missileDistance_unique))
                    {
                        BulletScript.AddMissileDurationPercent(bonusValue);
                    }
                    else if ((buffType == BuffType.hp_normal) || (buffType == BuffType.hp_unique))
                    {
                        playerScript.currentHealth += bonusValue;
                        playerScript.UpdateHealth();
                    }
                    else if ((buffType == BuffType.hpPer_normal) || buffType == BuffType.hpPer_unique)
                    {
                        playerScript.currentHealth *= 1f + bonusValue * 0.01f;
                        playerScript.UpdateHealth();
                    }

                    else if ((buffType == BuffType.missileAdd_unique))
                    {
                        WeaponScript weaponScript = GetCurrentWeaponScript();
                        if (weaponScript == null)
                            return;

                        weaponScript.bulletCount += (int)bonusValue;
                        playerScript.currentHealth = 1f;
                        playerScript.UpdateHealth();
                    }
                    else if ((buffType == BuffType.tungtung_rare))
                    {
                        playerScript.extraHelpCount++;
                        SpawnExtraHelp(HelpType.Tungtungtung);
                    }
                    else if ((buffType == BuffType.boombar_rare))
                    {
                        playerScript.extraHelpCount++;
                        SpawnExtraHelp(HelpType.Boombardino);
                    }
                    if (buffSFX != null)
                        AudioSource.PlayClipAtPoint(buffSFX, transform.position);

                    ShowBuffOverlay();

                    break;

                case WallType.NerfWall:
                    if (nerfType == NerfType.HealthReduce)
                    {
                        playerScript.currentHealth -= healthReduceAmt;          // Reduce player's health
                        ShowNerfOverlay();
                    }

                    else if (nerfType == NerfType.FireRateReduce)
                    {
                        WeaponScript weaponScript = GetCurrentWeaponScript();
                        if (weaponScript == null)
                            return;

                        weaponScript.fireRate *= fireRateDecMultipier;
                        ShowNerfOverlay();

                        ShowFireRateModifier(fireRateReduceSpr, fireRateDecMultipier);
                    }

                    if (wallAudioSource != null && nerfSFX != null)
                        wallAudioSource.PlayOneShot(nerfSFX);
                    break;
            }
        }

        private void ShowBuffOverlay()
        {
            EffectOverlayScript overlay = ResolveEffectOverlay();
            if (overlay != null)
                overlay.BuffOverlay();
        }

        private void ShowNerfOverlay()
        {
            EffectOverlayScript overlay = ResolveEffectOverlay();
            if (overlay != null)
                overlay.NerfOverlay();
        }

        private EffectOverlayScript ResolveEffectOverlay()
        {
            if (GetComponent<RuntimeBonusWall>() != null)
                return null;

            if (effectOverlayVignette != null)
                return effectOverlayVignette;

            GameObject volumeObject = GameObject.FindGameObjectWithTag("VolumeTag");
            if (volumeObject != null)
                effectOverlayVignette = volumeObject.GetComponent<EffectOverlayScript>();

            return effectOverlayVignette;
        }

        private WeaponScript GetCurrentWeaponScript()
        {
            if (weaponManager == null && playerScript != null)
                weaponManager = playerScript.GetComponent<WeaponManager>();

            return weaponManager != null && weaponManager.currentWeapon != null
                ? weaponManager.currentWeapon.GetComponentInChildren<WeaponScript>()
                : null;
        }

        private static void ShowFireRateModifier(Sprite sprite, float multiplier)
        {
            GameObject display = GameObject.FindGameObjectWithTag("FireRateDisplayTag");
            if (display == null)
                return;

            SpriteRenderer displaySprite = display.GetComponentInChildren<SpriteRenderer>();
            if (displaySprite != null)
                displaySprite.sprite = sprite;

            TextMeshProUGUI displayText = display.GetComponentInChildren<TextMeshProUGUI>();
            if (displayText != null)
                displayText.text = "x" + multiplier;

            Animator displayAnimator = display.GetComponent<Animator>();
            if (displayAnimator != null)
                displayAnimator.SetTrigger("FireTextPopIn");
        }

        private void SpawnExtraHelp(HelpType helpType)
        {
            if (extraHelp == null || playerScript == null) return;

            Vector3 spawnOffset = new Vector3(1.5f, 0, -0.75f);
            Vector3 spawnPosition = playerScript.transform.position + spawnOffset;
            GameObject prefab = helpType == HelpType.Tungtungtung
                ? GameManager.S.extraHelp_TungTungTung
                : GameManager.S.extraHelp_BoomBarDino;
            GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity);
            ExtraHelpBuffScript helper = instance.GetComponent<ExtraHelpBuffScript>();
            helper.spawnIndex = playerScript.extraHelpCount - 1;
            helper.helpType = helpType;
            playerScript.extraHelpWeaponScript.Add(instance.GetComponentInChildren<WeaponScript>());
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
