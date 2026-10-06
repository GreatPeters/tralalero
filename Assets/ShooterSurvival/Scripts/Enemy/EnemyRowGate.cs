using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // Essential proposal 2 (2026-10-06): the shark cannot slip between (or around) enemies standing in a row.
    // Rows are physical, not authored: activation spots hold one enemy each, so two or more close-combat
    // enemies within MaxRowDepth of each other along the shark's heading and inside the road (MaxLateral)
    // form a row. If the shark crosses that line while no member of the row has been defeated, it collides
    // with the member nearest its lane through the normal contact HP exchange. Defeating at least one member
    // opens the row; the rule never asks for the whole row. Ranged/ambush shooters are not members, and a
    // lone enemy can still be dodged. One scene-level instance is created by CanvasScript.
    public sealed class EnemyRowGate : MonoBehaviour
    {
        public const float MaxLateral = 5.5f, MaxLeadDistance = 8f, MaxRowDepth = 3.5f, ScanRadius = 16f;
        private readonly List<EnemyEventController> controllers = new();
        private readonly Dictionary<EnemyEventController, float> previousAlong = new();
        private readonly HashSet<EnemyEventController> resolved = new();
        private PlayerScript player;
        private float nextRefresh;

        public static int ForcedContacts { get; private set; }

        public static bool IsCloseCombat(EnemyEventMode mode)
            => mode is EnemyEventMode.AttackLoop or EnemyEventMode.AttackOnce
                or EnemyEventMode.MoveToTargetThenAttack or EnemyEventMode.PatrolBetweenStartAndTarget;

        // The enemy was just ahead of the shark and is now level with or behind it.
        public static bool Crossed(float previousAlong, float along)
            => !float.IsNaN(previousAlong) && previousAlong > 0f && previousAlong <= MaxLeadDistance && along <= 0f;

        public static bool IsRow(int members, float alongMin, float alongMax)
            => members >= 2 && alongMax - alongMin <= MaxRowDepth;

        public static int NearestLane(IReadOnlyList<float> laterals)
        {
            int best = -1;
            for (int i = 0; i < laterals.Count; i++)
                if (best < 0 || Mathf.Abs(laterals[i]) < Mathf.Abs(laterals[best])) best = i;
            return best;
        }

        public static EnemyRowGate Ensure(Component owner)
        {
            if (owner == null || !Application.isPlaying) return null;
            var gate = owner.GetComponent<EnemyRowGate>();
            return gate != null ? gate : owner.gameObject.AddComponent<EnemyRowGate>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ForcedContacts = 0;

        // New run: every row may block again.
        public void ResetGate() { previousAlong.Clear(); resolved.Clear(); }

        private static bool Eligible(EnemyEventController c, out EnemyScript_space combat)
        {
            combat = null;
            if (c == null || !c.isActiveAndEnabled || !IsCloseCombat(c.EventMode)) return false;
            combat = c.GetComponent<EnemyScript_space>();
            return combat != null;
        }

        private static bool Defeated(EnemyEventController c, EnemyScript_space combat)
            => combat.IsDead || c.RuntimeState == EnemyEventRuntimeState.Dead;

        private readonly List<float> laterals = new();
        private readonly List<EnemyScript_space> row = new();
        private readonly List<EnemyEventController> rowControllers = new();

        private void Update()
        {
            if (!TimeManager.isGameRunning || TimeManager.timeFactor <= 0f) return;
            if (player == null) player = FindFirstObjectByType<PlayerScript>();
            if (player == null || player.currentHealth <= 0f) return;
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 1f;
                controllers.Clear();
                foreach (var c in FindObjectsByType<EnemyEventController>(FindObjectsSortMode.None))
                    if (c.gameObject.scene == gameObject.scene && Eligible(c, out _)) controllers.Add(c);
            }
            var origin = player.transform.position;
            var forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .5f) return;
            var right = Vector3.Cross(Vector3.up, forward);

            foreach (var c in controllers)
            {
                if (!Eligible(c, out var combat) || resolved.Contains(c) || Defeated(c, combat)) { previousAlong.Remove(c); continue; }
                var delta = c.transform.position - origin;
                if (delta.sqrMagnitude > ScanRadius * ScanRadius) { previousAlong.Remove(c); continue; }
                float along = Vector3.Dot(delta, forward), lateral = Vector3.Dot(delta, right);
                float before = previousAlong.TryGetValue(c, out var p) ? p : float.NaN;
                previousAlong[c] = along;
                if (Mathf.Abs(lateral) > MaxLateral || !Crossed(before, along)) continue;
                ResolveCrossing(c, along, origin, forward, right);
            }
        }

        private void ResolveCrossing(EnemyEventController crossed, float crossedAlong, Vector3 origin, Vector3 forward, Vector3 right)
        {
            row.Clear(); rowControllers.Clear(); laterals.Clear();
            bool opened = false;
            float alongMin = float.MaxValue, alongMax = float.MinValue;
            foreach (var c in controllers)
            {
                if (!Eligible(c, out var combat)) continue;
                var delta = c.transform.position - origin;
                float along = Vector3.Dot(delta, forward), lateral = Vector3.Dot(delta, right);
                if (Mathf.Abs(lateral) > MaxLateral || Mathf.Abs(along - crossedAlong) > MaxRowDepth) continue;
                if (Defeated(c, combat)) { opened = true; continue; } // someone in this row is already down
                row.Add(combat); rowControllers.Add(c); laterals.Add(lateral);
                alongMin = Mathf.Min(alongMin, along); alongMax = Mathf.Max(alongMax, along);
            }
            foreach (var c in rowControllers) resolved.Add(c);
            if (opened || !IsRow(row.Count, alongMin, alongMax)) return;
            if (row[NearestLane(laterals)].ForceRowContact(player)) ForcedContacts++;
        }
    }
}
