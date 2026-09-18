using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // The original hit effect is cosmetic. A burst may recycle its oldest visual,
    // but never changes projectile delivery, damage or enemy lifetime.
    public sealed class EnemyHitEffectPool : MonoBehaviour
    {
        private const int CapacityPerPrefab = 32;
        private static EnemyHitEffectPool instance;
        private sealed class Effect
        {
            public GameObject root;
            public ParticleSystem particles;
            public float remaining;
            public bool active;
        }
        private sealed class Pool
        {
            public readonly Effect[] effects = new Effect[CapacityPerPrefab];
            public int next;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public float duration;
        }
        private readonly Dictionary<GameObject, Pool> pools = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        public static void Prewarm(GameObject prefab)
        {
            if (prefab == null) return;
            if (instance == null)
                instance = new GameObject("Enemy hit effect pool").AddComponent<EnemyHitEffectPool>();
            instance.GetPool(prefab);
        }

        public static void Play(GameObject prefab, Transform attachment)
        {
            if (prefab == null || attachment == null) return;
            Prewarm(prefab);
            Pool pool = instance.GetPool(prefab);
            Effect effect = pool.effects[pool.next];
            pool.next = (pool.next + 1) % pool.effects.Length;
            // A pooled enemy can be destroyed independently of the pool.
            if (effect.root == null) instance.CreateEffect(prefab, effect);
            effect.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var pose = effect.root.transform;
            pose.SetParent(attachment, false);
            pose.localPosition = pool.localPosition;
            pose.localRotation = pool.localRotation;
            pose.localScale = pool.localScale;
            effect.remaining = pool.duration;
            effect.active = true;
            effect.root.SetActive(true);
            effect.particles.Play(true);
        }

        private Pool GetPool(GameObject prefab)
        {
            if (pools.TryGetValue(prefab, out Pool pool)) return pool;
            pool = new Pool
            {
                localPosition = prefab.transform.localPosition,
                localRotation = prefab.transform.localRotation,
                localScale = prefab.transform.localScale,
                duration = prefab.GetComponent<ParticleSystem>().main.duration
            };
            for (int i = 0; i < pool.effects.Length; i++)
            {
                pool.effects[i] = new Effect();
                CreateEffect(prefab, pool.effects[i]);
            }
            pools.Add(prefab, pool);
            return pool;
        }

        private void CreateEffect(GameObject prefab, Effect effect)
        {
            effect.root = Instantiate(prefab, transform);
            effect.particles = effect.root.GetComponent<ParticleSystem>();
            var main = effect.particles.main;
            main.stopAction = ParticleSystemStopAction.None;
            effect.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.root.SetActive(false);
        }

        private void Update() => Tick(Time.deltaTime);

        private void Tick(float deltaTime)
        {
            foreach (Pool pool in pools.Values)
            foreach (Effect effect in pool.effects)
            {
                if (!effect.active) continue;
                effect.remaining -= deltaTime;
                if (effect.root == null) { effect.active = false; continue; }
                if (effect.remaining > 0f && effect.root.activeInHierarchy) continue;
                effect.active = false;
                effect.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.root.SetActive(false);
                effect.root.transform.SetParent(transform, false);
            }
        }

        private void OnDestroy()
        {
            // Active effects are temporarily parented to enemies, outside this root.
            foreach (Pool pool in pools.Values)
            foreach (Effect effect in pool.effects)
                if (effect.root != null && !effect.root.transform.IsChildOf(transform))
                {
                    if (Application.isPlaying) Destroy(effect.root);
                    else DestroyImmediate(effect.root);
                }
            if (instance == this) instance = null;
        }
    }
}
