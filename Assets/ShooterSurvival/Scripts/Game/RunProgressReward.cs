using System;

public static class RunProgressReward
{
    public static int Calculate(float seconds, float intervalSeconds, int coinsPerCheckpoint, int maximumCheckpoints)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0 ||
            float.IsNaN(intervalSeconds) || float.IsInfinity(intervalSeconds) || intervalSeconds <= 0 ||
            coinsPerCheckpoint <= 0 || maximumCheckpoints <= 0) return 0;
        int checkpoints = (int)Math.Min(maximumCheckpoints, Math.Floor((double)seconds / intervalSeconds));
        return (int)Math.Min(int.MaxValue, (long)checkpoints * coinsPerCheckpoint);
    }
}
