using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IndianOceanAssets.ShooterSurvival
{
    [DisallowMultipleComponent]
    public sealed class BonusChoiceAltarVfx : MonoBehaviour
    {
        private const float UniqueGlowScale = 1.28f;
        private const float UniqueEnergyScale = 1.22f;
        private const float UniqueGroundAuraScale = 1.16f;
        private const float UniqueIconAuraScale = 1.2f;
        private const float UniqueMotionSpeed = 1.25f;
        private const float UniqueMotionAmount = 1.45f;
        private const float UniqueParticleRate = 1.8f;
        private const float UniqueParticleSize = 1.3f;
        private const float UniqueParticleSpeed = 1.15f;
        private const float UniqueParticleCapacity = 1.75f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color UniqueWorldPurple =
            new(1.65f, 0.35f, 3f, 1f);
        private static readonly Color UniqueUiPurple =
            new(0.76f, 0.24f, 1f, 1f);

        [SerializeField] private Transform glowRoot;
        [SerializeField] private RectTransform iconRect;
        [SerializeField] private RectTransform[] iconAuraRects;
        [SerializeField] private float rotationSpeed = 28f;
        [SerializeField] private float pulseAmount = 0.08f;
        [SerializeField] private float pulseSpeed = 2.2f;
        [SerializeField] private float iconBobDistance = 0.07f;
        [SerializeField] private float iconBobSpeed = 2.6f;
        [SerializeField] private float iconSwayAngle = 2.5f;
        [SerializeField] private float phaseOffset;

        private Vector3 glowBaseScale;
        private Vector2 iconBasePosition;
        private Quaternion iconBaseRotation;
        private Vector2[] auraBasePositions;
        private Vector3[] auraBaseScales;
        private Quaternion[] auraBaseRotations;
        private Graphic[] auraGraphics;
        private Color[] auraBaseColors;
        private Transform energyRoot;
        private Transform groundAura;
        private ParticleSystem particles;
        private Vector3 energyBaseScale;
        private Vector3 groundAuraBaseScale;
        private ParticleSystem.MinMaxCurve particleBaseEmission;
        private ParticleSystem.MinMaxCurve particleBaseSpeed;
        private ParticleSystem.MinMaxCurve particleBaseSizeX;
        private ParticleSystem.MinMaxCurve particleBaseSizeY;
        private ParticleSystem.MinMaxCurve particleBaseSizeZ;
        private int particleBaseCapacity;
        private Renderer[] effectRenderers;
        private MaterialPropertyBlock[] effectBasePropertyBlocks;
        private float[] effectBaseAlphas;
        private Rarity rarity;
        private bool baselinesCached;

        public Rarity Rarity => rarity;

        public void Configure(
            Transform targetGlowRoot,
            RectTransform targetIconRect,
            params RectTransform[] targetIconAuraRects)
        {
            Configure(
                true,
                targetGlowRoot,
                targetIconRect,
                targetIconAuraRects);
        }

        public void Configure(
            bool aggressiveVfx,
            Transform targetGlowRoot,
            RectTransform targetIconRect,
            params RectTransform[] targetIconAuraRects)
        {
            rotationSpeed = aggressiveVfx ? 16f : -10f;
            pulseAmount = aggressiveVfx ? 0.025f : 0.035f;
            pulseSpeed = aggressiveVfx ? 4.8f : 3.1f;
            iconBobDistance = 0.03f;
            iconBobSpeed = aggressiveVfx ? 2.5f : 2f;
            iconSwayAngle = 1.1f;
            phaseOffset = aggressiveVfx ? 0f : 1.4f;
            glowRoot = targetGlowRoot;
            iconRect = targetIconRect;
            iconAuraRects = targetIconAuraRects;
            baselinesCached = false;
            CacheBaselines();
        }

        public void SetRarity(Rarity grade)
        {
            rarity = grade;
            if (Application.isPlaying)
                RefreshPresentation();
        }

        private void Awake()
        {
            CacheBaselines();
        }

        private void OnEnable()
        {
            CacheBaselines();
            RestoreBaselines();
            if (Application.isPlaying)
                ApplyRarityPresentation();
        }

        private void LateUpdate()
        {
            bool isUnique = rarity == Rarity.Unique;
            float motionSpeed = isUnique ? UniqueMotionSpeed : 1f;
            float motionAmount = isUnique ? UniqueMotionAmount : 1f;
            float pulse = Mathf.Sin(Time.time * pulseSpeed * motionSpeed + phaseOffset);
            float bob = Mathf.Sin(
                Time.time * iconBobSpeed * motionSpeed + phaseOffset * 0.73f) *
                iconBobDistance * motionAmount;
            float sway = Mathf.Sin(
                Time.time * iconBobSpeed * motionSpeed * 0.7f + phaseOffset * 0.41f) *
                iconSwayAngle * motionAmount;
            float glowScale = isUnique ? UniqueGlowScale : 1f;
            float auraScale = isUnique ? UniqueIconAuraScale : 1f;

            if (glowRoot != null)
            {
                glowRoot.Rotate(
                    0f,
                    rotationSpeed * motionSpeed * Time.deltaTime,
                    0f,
                    Space.Self);
                glowRoot.localScale = glowBaseScale * glowScale *
                    (1f + pulse * pulseAmount * motionAmount);
            }

            if (iconRect != null)
            {
                iconRect.anchoredPosition = iconBasePosition + Vector2.up * bob;
                iconRect.localRotation = iconBaseRotation * Quaternion.Euler(0f, 0f, sway);
            }

            if (iconAuraRects == null)
                return;

            for (int index = 0; index < iconAuraRects.Length; index++)
            {
                RectTransform aura = iconAuraRects[index];
                if (aura == null)
                    continue;

                aura.anchoredPosition = auraBasePositions[index] + Vector2.up * bob;
                aura.localRotation =
                    auraBaseRotations[index] * Quaternion.Euler(0f, 0f, sway * 0.7f);
                float auraPulse = 1f + pulse * pulseAmount * (index == 0 ? 0.85f : 1f);
                aura.localScale = auraBaseScales[index] * auraScale * auraPulse;
            }
        }

        private void CacheBaselines()
        {
            if (baselinesCached)
                return;

            if (glowRoot != null)
                glowBaseScale = glowRoot.localScale;

            Transform visualRoot = glowRoot != null ? glowRoot.parent : null;
            energyRoot = visualRoot != null
                ? visualRoot.Find("IconEnergyBillboard")
                : null;
            groundAura = visualRoot != null ? visualRoot.Find("GroundAura") : null;
            particles = visualRoot != null
                ? visualRoot.Find("ChoiceParticles")?.GetComponent<ParticleSystem>()
                : null;

            if (energyRoot != null)
                energyBaseScale = energyRoot.localScale;
            if (groundAura != null)
                groundAuraBaseScale = groundAura.localScale;

            if (particles != null)
            {
                ParticleSystem.MainModule main = particles.main;
                ParticleSystem.EmissionModule emission = particles.emission;
                particleBaseEmission = emission.rateOverTime;
                particleBaseSpeed = main.startSpeed;
                particleBaseSizeX = main.startSizeX;
                particleBaseSizeY = main.startSizeY;
                particleBaseSizeZ = main.startSizeZ;
                particleBaseCapacity = main.maxParticles;
            }

            CacheEffectColorBaselines(visualRoot);

            if (iconRect != null)
            {
                iconBasePosition = iconRect.anchoredPosition;
                iconBaseRotation = iconRect.localRotation;
            }

            int auraCount = iconAuraRects != null ? iconAuraRects.Length : 0;
            auraBasePositions = new Vector2[auraCount];
            auraBaseScales = new Vector3[auraCount];
            auraBaseRotations = new Quaternion[auraCount];
            auraGraphics = new Graphic[auraCount];
            auraBaseColors = new Color[auraCount];
            for (int index = 0; index < auraCount; index++)
            {
                RectTransform aura = iconAuraRects[index];
                if (aura == null)
                    continue;

                auraBasePositions[index] = aura.anchoredPosition;
                auraBaseScales[index] = aura.localScale;
                auraBaseRotations[index] = aura.localRotation;
                auraGraphics[index] = aura.GetComponent<Graphic>();
                if (auraGraphics[index] != null)
                    auraBaseColors[index] = auraGraphics[index].color;
            }

            baselinesCached = true;
        }

        private void RestoreBaselines()
        {
            if (glowRoot != null)
                glowRoot.localScale = glowBaseScale;

            if (energyRoot != null)
                energyRoot.localScale = energyBaseScale;
            if (groundAura != null)
                groundAura.localScale = groundAuraBaseScale;

            RestoreParticleBaselines();
            RestoreEffectColorBaselines();

            if (iconRect != null)
            {
                iconRect.anchoredPosition = iconBasePosition;
                iconRect.localRotation = iconBaseRotation;
            }

            if (iconAuraRects == null)
                return;

            for (int index = 0; index < iconAuraRects.Length; index++)
            {
                RectTransform aura = iconAuraRects[index];
                if (aura == null)
                    continue;

                aura.anchoredPosition = auraBasePositions[index];
                aura.localScale = auraBaseScales[index];
                aura.localRotation = auraBaseRotations[index];
                if (auraGraphics[index] != null)
                    auraGraphics[index].color = auraBaseColors[index];
            }
        }

        private void ApplyRarityPresentation()
        {
            if (rarity != Rarity.Unique)
                return;

            if (glowRoot != null)
                glowRoot.localScale = glowBaseScale * UniqueGlowScale;
            if (energyRoot != null)
                energyRoot.localScale = energyBaseScale * UniqueEnergyScale;
            if (groundAura != null)
                groundAura.localScale = groundAuraBaseScale * UniqueGroundAuraScale;

            if (iconAuraRects != null)
            {
                for (int index = 0; index < iconAuraRects.Length; index++)
                {
                    RectTransform aura = iconAuraRects[index];
                    if (aura != null)
                        aura.localScale = auraBaseScales[index] * UniqueIconAuraScale;
                }
            }

            ApplyUniqueEffectColors();

            if (particles == null)
                return;

            ParticleSystem.MainModule main = particles.main;
            main.startSpeed = ScaleCurve(particleBaseSpeed, UniqueParticleSpeed);
            main.startSizeX = ScaleCurve(particleBaseSizeX, UniqueParticleSize);
            main.startSizeY = ScaleCurve(particleBaseSizeY, UniqueParticleSize);
            main.startSizeZ = ScaleCurve(particleBaseSizeZ, UniqueParticleSize);
            main.maxParticles = Mathf.CeilToInt(
                particleBaseCapacity * UniqueParticleCapacity);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = ScaleCurve(
                particleBaseEmission,
                UniqueParticleRate);
        }

        private void RefreshPresentation()
        {
            CacheBaselines();
            RestoreBaselines();
            ApplyRarityPresentation();
        }

        private void RestoreParticleBaselines()
        {
            if (particles == null)
                return;

            ParticleSystem.MainModule main = particles.main;
            main.startSpeed = particleBaseSpeed;
            main.startSizeX = particleBaseSizeX;
            main.startSizeY = particleBaseSizeY;
            main.startSizeZ = particleBaseSizeZ;
            main.maxParticles = particleBaseCapacity;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = particleBaseEmission;
        }

        private void CacheEffectColorBaselines(Transform visualRoot)
        {
            var renderers = new List<Renderer>();
            AddRenderers(renderers, glowRoot);
            AddRenderers(renderers, energyRoot);
            AddRenderer(renderers, groundAura != null
                ? groundAura.GetComponent<Renderer>()
                : null);
            Transform frontSigil = visualRoot != null
                ? visualRoot.Find("FrontSigil")
                : null;
            AddRenderer(renderers, frontSigil != null
                ? frontSigil.GetComponent<Renderer>()
                : null);
            AddRenderer(renderers, particles != null
                ? particles.GetComponent<ParticleSystemRenderer>()
                : null);

            effectRenderers = renderers.ToArray();
            effectBasePropertyBlocks = new MaterialPropertyBlock[effectRenderers.Length];
            effectBaseAlphas = new float[effectRenderers.Length];
            for (int index = 0; index < effectRenderers.Length; index++)
            {
                Renderer effectRenderer = effectRenderers[index];
                var propertyBlock = new MaterialPropertyBlock();
                effectRenderer.GetPropertyBlock(propertyBlock);
                effectBasePropertyBlocks[index] = propertyBlock;

                Color propertyColor = propertyBlock.GetColor(BaseColorId);
                if (propertyColor == default)
                    propertyColor = propertyBlock.GetColor(ColorId);
                if (propertyColor != default)
                {
                    effectBaseAlphas[index] = propertyColor.a;
                    continue;
                }

                Material material = effectRenderer.sharedMaterial;
                if (material == null)
                {
                    effectBaseAlphas[index] = 1f;
                }
                else if (material.HasProperty(BaseColorId))
                {
                    effectBaseAlphas[index] = material.GetColor(BaseColorId).a;
                }
                else if (material.HasProperty(ColorId))
                {
                    effectBaseAlphas[index] = material.GetColor(ColorId).a;
                }
                else
                {
                    effectBaseAlphas[index] = 1f;
                }
            }
        }

        private void ApplyUniqueEffectColors()
        {
            if (effectRenderers != null)
            {
                var propertyBlock = new MaterialPropertyBlock();
                for (int index = 0; index < effectRenderers.Length; index++)
                {
                    Renderer effectRenderer = effectRenderers[index];
                    if (effectRenderer == null)
                        continue;

                    propertyBlock.Clear();
                    effectRenderer.GetPropertyBlock(propertyBlock);
                    Color purple = UniqueWorldPurple;
                    purple.a = effectBaseAlphas[index];
                    propertyBlock.SetColor(BaseColorId, purple);
                    propertyBlock.SetColor(ColorId, purple);
                    effectRenderer.SetPropertyBlock(propertyBlock);
                }
            }

            if (auraGraphics == null)
                return;

            for (int index = 0; index < auraGraphics.Length; index++)
            {
                Graphic auraGraphic = auraGraphics[index];
                if (auraGraphic == null)
                    continue;

                Color purple = UniqueUiPurple;
                purple.a = auraBaseColors[index].a;
                auraGraphic.color = purple;
            }
        }

        private void RestoreEffectColorBaselines()
        {
            if (effectRenderers == null)
                return;

            for (int index = 0; index < effectRenderers.Length; index++)
            {
                Renderer effectRenderer = effectRenderers[index];
                if (effectRenderer != null)
                {
                    MaterialPropertyBlock baseBlock = effectBasePropertyBlocks[index];
                    effectRenderer.SetPropertyBlock(
                        baseBlock != null && !baseBlock.isEmpty ? baseBlock : null);
                }
            }
        }

        private static void AddRenderers(List<Renderer> renderers, Transform root)
        {
            if (root == null)
                return;

            foreach (Renderer effectRenderer in root.GetComponentsInChildren<Renderer>(true))
                AddRenderer(renderers, effectRenderer);
        }

        private static void AddRenderer(List<Renderer> renderers, Renderer effectRenderer)
        {
            if (effectRenderer != null && !renderers.Contains(effectRenderer))
                renderers.Add(effectRenderer);
        }

        private static ParticleSystem.MinMaxCurve ScaleCurve(
            ParticleSystem.MinMaxCurve source,
            float multiplier)
        {
            return source.mode switch
            {
                ParticleSystemCurveMode.Constant =>
                    new ParticleSystem.MinMaxCurve(source.constant * multiplier),
                ParticleSystemCurveMode.TwoConstants =>
                    new ParticleSystem.MinMaxCurve(
                        source.constantMin * multiplier,
                        source.constantMax * multiplier),
                ParticleSystemCurveMode.Curve =>
                    new ParticleSystem.MinMaxCurve(
                        source.curveMultiplier * multiplier,
                        source.curve),
                ParticleSystemCurveMode.TwoCurves =>
                    new ParticleSystem.MinMaxCurve(
                        source.curveMultiplier * multiplier,
                        source.curveMin,
                        source.curveMax),
                _ => source
            };
        }
    }
}
