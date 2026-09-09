using System;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [DisallowMultipleComponent]
    public sealed class BonusWallChoicePair : MonoBehaviour
    {
        [SerializeField] private AuthoredBonusWall left;
        [SerializeField] private AuthoredBonusWall right;
        public AuthoredBonusWall Left => left;
        public AuthoredBonusWall Right => right;
        public AuthoredBonusWall Selected { get; private set; }
        public bool IsConfigured => left != null && right != null && left != right &&
            left.gameObject.scene == gameObject.scene && right.gameObject.scene == gameObject.scene &&
            left.ChoicePair == this && right.ChoicePair == this;

        public void Configure(AuthoredBonusWall leftChoice, AuthoredBonusWall rightChoice)
        {
            if (leftChoice == null || rightChoice == null || leftChoice == rightChoice ||
                leftChoice.gameObject.scene != gameObject.scene || rightChoice.gameObject.scene != gameObject.scene)
                throw new ArgumentException("A choice pair needs two distinct altars.");
            left = leftChoice; right = rightChoice;
            left.ChoicePair = this; right.ChoicePair = this;
            Selected = null;
        }

        public void PrepareForRun(Rarity grade, bool enabled)
        {
            if (!IsConfigured) throw new InvalidOperationException("Incomplete bonus pair: " + name);
            Selected = null;
            left.Configure(grade); right.Configure(grade);
            left.BeginRoll(); right.BeginRoll();
            left.gameObject.SetActive(enabled); right.gameObject.SetActive(enabled);
            if (!enabled) return;
            Refresh(left); Refresh(right);
        }

        private static void Refresh(AuthoredBonusWall altar)
        {
            altar.Wall.ReactivateLifetimeObject();
            altar.Wall.SetRandomStat();
            altar.Wall.SetStats();
            altar.Wall.SetWallSprite();
        }

        public bool TryChoose(AuthoredBonusWall choice)
        {
            if (!IsConfigured || Selected != null || (choice != left && choice != right) ||
                !choice.gameObject.activeInHierarchy || (Application.isPlaying && !TimeManager.isGameRunning))
                return false;
            Selected = choice; // Claim before disabling the other side or applying any reward.
            var other = choice == left ? right : left;
            foreach (var collider in other.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            other.gameObject.SetActive(false);
            return true;
        }
    }
}
