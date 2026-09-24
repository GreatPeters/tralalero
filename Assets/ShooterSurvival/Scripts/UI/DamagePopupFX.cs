using TMPro;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public static class DamagePopupFX
    {
        private static DamagePopupPool pool;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => pool = null;

        public static void Prewarm(TextMeshProUGUI template)
        {
            if (pool != null) return;
            pool = new GameObject("Damage popup pool").AddComponent<DamagePopupPool>();
            pool.Initialize(template);
        }

        public static void Show(Vector3 worldPosition, float amount)
        {
            EnsurePool();
            pool.Show(worldPosition, Mathf.RoundToInt(amount), false);
        }

        public static void ShowCoin(Vector3 worldPosition, int amount)
        {
            EnsurePool();
            pool.Show(worldPosition, amount, true);
        }

        public static void ShowPlayerDamage(Vector3 worldPosition, float amount)
        {
            EnsurePool();
            pool.ShowPlayerDamage(worldPosition, amount);
        }

        private static void EnsurePool()
        {
            if (pool != null) return;
            var canvas = Object.FindFirstObjectByType<CanvasScript>();
            Prewarm(canvas != null ? canvas.DamagePopupPrefab : null);
        }
    }
}
