using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        public Image statIconImage;

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
        private float displayBonusValue;
        private BonusValueType bonusValueType;
        private BonusRow selectedBonusRow;
        private BonusRow selectedDisplayRow;
        private bool hasSelectedBonusRow;
        private string bonusAlias;

        public float CurrentBonusValue => bonusValue;
        public float CurrentBonusDisplayValue => displayBonusValue;
        public string CurrentBonusAlias => bonusAlias;

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
            if (playerScript == null)
            {
                StartCoroutine(InitializeWhenPlayerAvailable());
                return;
            }

            InitWall();
        }

        private IEnumerator InitializeWhenPlayerAvailable()
        {
            while (playerScript == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerScript = player.GetComponent<PlayerScript>();

                if (playerScript == null)
                    yield return null;
            }

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
            AuthoredBonusWall authoredAltar = GetComponentInParent<AuthoredBonusWall>();
            if (authoredAltar != null)
            {
                RollAuthoredAltar(authoredAltar);
                return;
            }

            hasSelectedBonusRow = false;
            bonusAlias = null;
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
            displayBonusValue = 0f;
            bonusValueType = BonusValueType.Value;
            float playerOriginalDamage = playerScript != null
                ? playerScript.originalDamage
                : 0f;
            float playerOriginalHealth = playerScript != null
                ? playerScript.originalHealth
                : 0f;

            if (HasInvalidAuthoredRoll())
            {
                bonusValue = 0f;
            }
            else if (hasSelectedBonusRow)
            {
                float baseValue = selectedBonusRow.stat switch
                {
                    "att" => playerOriginalDamage,
                    "hp" => playerOriginalHealth,
                    _ => 0f
                };
                float random01 = Random.value;
                bonusValue = BonusAltarRules.ResolveValue(
                    selectedBonusRow,
                    random01,
                    baseValue);
                displayBonusValue = BonusAltarRules.ResolveDisplayValue(
                    selectedBonusRow,
                    random01);
                bonusValueType = selectedBonusRow.valueType;
            }
            else
            {
                bonusValue = buffType switch
                {
                    BuffType.att_normmal or BuffType.att_unique =>
                        RollBonusValue("att", playerOriginalDamage),
                    BuffType.attPer_normal or BuffType.attPer_unique =>
                        RollBonusValue("attPercent", 0f),
                    BuffType.missileAdd_unique =>
                        RollBonusValue("missileAdd", 0f),
                    BuffType.attackSpeed_normal or BuffType.attackSpeed_unique =>
                        RollBonusValue("attackSpeed", 0f),
                    BuffType.missileDistance_normal or BuffType.missileDistance_unique =>
                        RollBonusValue("missileDistance", 0f),
                    BuffType.hp_normal or BuffType.hp_unique =>
                        RollBonusValue("hp", playerOriginalHealth),
                    BuffType.hpPer_normal or BuffType.hpPer_unique =>
                        RollBonusValue("hpPercent", 0f),
                    BuffType.tungtung_rare =>
                        RollBonusValue("tungtungAdd", 0f),
                    BuffType.boombar_rare =>
                        RollBonusValue("boombarAdd", 0f),
                    _ => 0f
                };
            }
        }

        private float RollBonusValue(
            string stat,
            float baseValue)
        {
            if (!BonusTables.TryGet(rarity.ToString(), stat, out BonusRow row))
            {
                Debug.LogError($"[WallScript] Bonus range missing. rarity={rarity} stat={stat}");
                return 0f;
            }

            bonusValueType = row.valueType;
            float random01 = Random.value;
            displayBonusValue = BonusAltarRules.ResolveDisplayValue(row, random01);
            return BonusAltarRules.ResolveValue(row, random01, baseValue);
        }

        private IEnumerator MoveOutsideForwardMarch()
        {
            while (TimeManager.Instance == null)
                yield return null;

            if (TimeManager.Instance.isForwardMarchScene)
                yield break;

            var fixedUpdate = new WaitForFixedUpdate();
            Transform movementRoot = GetLifetimeObject().transform;
            while (true)
            {
                yield return fixedUpdate;
                movementRoot.Translate(
                    -Vector3.forward *
                    wallMoveSpeed *
                    Time.fixedDeltaTime *
                    TimeManager.timeFactor);
            }
        }

        private void RollAuthoredAltar(AuthoredBonusWall authoredAltar)
        {
            authoredAltar.BeginRoll();
            selectedDisplayRow = default;
            wallType = WallType.BuffWall;
            isRandom = true;
            rarity = authoredAltar.Rarity;

            var rows = BonusTables.GetAll(BonusAltarRules.DataRarityFor(rarity));
            var candidates = BonusAltarRules.BuildCandidates(
                rows,
                rarity,
                authoredAltar.CollectNearbyRolledStats());
            if (candidates.Count == 0)
            {
                hasSelectedBonusRow = false;
                bonusAlias = null;
                Debug.LogError(
                    $"[WallScript] No supported bonus rows for altar grade {BonusAltarRules.GradeLabel(rarity)}.");
                return;
            }

            selectedBonusRow = candidates[Random.Range(0, candidates.Count)];
            if (!BonusAltarRules.TryResolveBuffType(
                    rarity,
                    selectedBonusRow.stat,
                    out buffType))
            {
                hasSelectedBonusRow = false;
                return;
            }

            hasSelectedBonusRow = true;
            selectedDisplayRow = BonusTables.ResolveDisplayRow(selectedBonusRow);
            bonusAlias = BonusAltarRules.ResolveAlias(selectedBonusRow);
            authoredAltar.CommitRoll(selectedBonusRow.stat);
        }

        private void OnTriggerEnter(Collider other)
        {
            // wall moves out of camera range
            if (other.CompareTag("DestroyerTag"))
            {
                Destroy(GetLifetimeObject()); // Destroy the complete authored wall when its visual is a sibling.
            }

            // player enters the wall
            else if (other.CompareTag("Player"))
            {
                if (playerScript == null || HasInvalidAuthoredRoll())
                    return;

                if (playerScript.lastWallTouchTime == 0 || Time.time - playerScript.lastWallTouchTime > 1f)
                {
                    playerScript.lastWallTouchTime = Time.time;           // Update the last time the wall was touched
                    ApplyWallEffect();                                      // Apply the effect based on the wall's type
                    gameObject.GetComponent<Collider>().isTrigger = false;  // Disable trigger once applied

                    GetLifetimeObject().SetActive(false);
                }
            }
        }

        private GameObject GetLifetimeObject()
        {
            BonusWallLifetimeRoot lifetimeRoot =
                GetComponentInParent<BonusWallLifetimeRoot>(true);
            if (lifetimeRoot != null)
                return lifetimeRoot.gameObject;

            AuthoredBonusWall authoredBonus =
                GetComponentInParent<AuthoredBonusWall>(true);
            return authoredBonus != null ? authoredBonus.gameObject : gameObject;
        }

        public void ReactivateLifetimeObject()
        {
            Collider trigger = GetComponent<Collider>();
            if (trigger != null)
            {
                trigger.enabled = true;
                trigger.isTrigger = true;
            }

            GetLifetimeObject().SetActive(true);
        }

        public void SetWallSprite()
        {
            if (HasInvalidAuthoredRoll())
            {
                UpdateStatIcon();
                DisableInvalidAuthoredPresentation();
                return;
            }

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

            if (currSprite != null)
                currSprite.sprite = selectedSprite;
            UpdateStatIcon();
            UpdateStatUI(buffType, displayBonusValue);
        }

        private void DisableInvalidAuthoredPresentation()
        {
            TextMeshProUGUI statNameText = null;
            if (statNameLoc != null)
            {
                statNameLoc.enabled = false;
                statNameText = statNameLoc.GetComponent<TextMeshProUGUI>();
            }

            foreach (TextMeshProUGUI text in
                     GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text == null ||
                    (text != statValueTmp &&
                     text != statNameText &&
                     text.gameObject.name is not ("Choice_Title" or "Value_Text")))
                {
                    continue;
                }

                text.text = string.Empty;
                text.enabled = false;
            }

            Collider trigger = GetComponent<Collider>();
            if (trigger != null)
            {
                trigger.isTrigger = false;
                trigger.enabled = false;
            }
        }

        private void UpdateStatIcon()
        {
            if (statIconImage == null)
            {
                foreach (Image image in GetComponentsInChildren<Image>(true))
                {
                    if (image.gameObject.name != "Stat_Icon")
                        continue;

                    statIconImage = image;
                    break;
                }
            }

            if (statIconImage == null)
                return;

            if (wallType != WallType.BuffWall || HasInvalidAuthoredRoll())
            {
                statIconImage.enabled = false;
                return;
            }

            string resourceName = BonusAltarRules.ResolveIconResourceName(buffType);
            Sprite icon = string.IsNullOrEmpty(resourceName)
                ? null
                : Resources.Load<Sprite>("WallBonusIcons/" + resourceName);
            statIconImage.sprite = icon;
            statIconImage.enabled = icon != null;
        }

        private void ApplyWallEffect()
        {
            if (HasInvalidAuthoredRoll())
                return;

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
            if (GetComponentInParent<RuntimeBonusWall>(true) != null)
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

        private bool HasInvalidAuthoredRoll()
        {
            return !hasSelectedBonusRow &&
                   GetComponentInParent<AuthoredBonusWall>(true) != null;
        }

        private void UpdateStatUI(BuffType bt, float value)
        {
            // 占싱몌옙(占쏙옙占시띰옙占쏙옙占쏙옙)
            UpdateStatName(bt);

            string formattedValue = BonusAltarRules.FormatDisplayValue(
                value,
                bonusValueType);

            if (statValueTmp != null)
            {
                statValueTmp.enabled = true;
                statValueTmp.text = formattedValue;
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (text != null && text.gameObject.name == "Value_Text")
                {
                    text.enabled = true;
                    text.text = formattedValue;
                }
                else if (text != null &&
                         text.gameObject.name == "Choice_Title" &&
                         !string.IsNullOrEmpty(bonusAlias))
                {
                    text.enabled = true;
                    text.text = bonusAlias;
                }
            }
        }

        private void UpdateStatName(BuffType bt)
        {
            if (statNameLoc == null)
                return;

            if (hasSelectedBonusRow)
            {
                statNameLoc.enabled = false;
                TextMeshProUGUI statNameText =
                    statNameLoc.GetComponent<TextMeshProUGUI>();
                if (statNameText != null)
                {
                    statNameText.enabled = true;
                    statNameText.text = BonusAltarRules.ResolveDisplayName(
                        selectedDisplayRow);
                }

                return;
            }

            TextMeshProUGUI localizedText =
                statNameLoc.GetComponent<TextMeshProUGUI>();
            if (localizedText != null)
                localizedText.enabled = true;

            statNameLoc.enabled = true;
            statNameLoc.StringReference.SetReference(
                tableName,
                BonusAltarRules.ResolveLocalizationKey(bt));
            statNameLoc.RefreshString();
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
