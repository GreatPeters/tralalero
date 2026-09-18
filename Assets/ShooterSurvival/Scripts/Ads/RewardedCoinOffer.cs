using System;

namespace IndianOceanAssets.ShooterSurvival.Ads
{
    // The UI may close before the SDK reward callback arrives. Keep that attempt
    // valid until a different round/attempt replaces it, and consume it only once.
    public sealed class RewardedCoinOffer
    {
        private int token;
        private bool attempted;
        public string RoundId { get; private set; }
        public int Coins { get; private set; }
        public bool Claimed { get; private set; }
        public bool Showing { get; private set; }
        public bool Eligible => !string.IsNullOrEmpty(RoundId) && Coins > 0 && !Claimed;

        public void Reset(string roundId, int earnedCoins, int minimum, int maximum)
        {
            if (Showing) throw new InvalidOperationException("Cannot replace an active ad offer");
            bool alreadyClaimed = Claimed && string.Equals(RoundId, roundId, StringComparison.Ordinal);
            token++; attempted = false; Claimed = alreadyClaimed;
            RoundId = roundId;
            int lower = Math.Max(0, minimum), upper = Math.Max(lower, maximum);
            Coins = Math.Min(upper, Math.Max(lower, earnedCoins));
        }
        public bool TryBegin(out int attempt)
        {
            attempt = 0;
            if (!Eligible || Showing) return false;
            token++; attempted = true; Showing = true; attempt = token;
            return true;
        }
        public bool TryClaim(int attempt, out int coins)
        {
            coins = 0;
            if (!Eligible || !attempted || attempt != token) return false;
            Claimed = true; coins = Coins; return true;
        }
        public void Finish(int attempt) { if (attempt == token) Showing = false; }
        public void Invalidate() { token++; attempted = false; Showing = false; RoundId = null; }
    }
}
