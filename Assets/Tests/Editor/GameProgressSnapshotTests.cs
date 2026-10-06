using System;
using System.Collections.Generic;
using System.Text;
using IndianOceanAssets.ShooterSurvival.Account;
using NUnit.Framework;

public sealed class GameProgressSnapshotTests
{
    private static readonly Dictionary<string, string> Keys = new Dictionary<string, string> { ["coin"] = "int", ["chapter_unlocked"] = "int", ["upgrade_stat_ATT"] = "float", ["cosmetic_equipped_Hat"] = "string" };
    private static GameProgressSnapshot Progress(params ProgressEntry[] entries) => new GameProgressSnapshot { playerId = "player-A", entries = entries };
    private static ProgressEntry Entry(string key, string kind, string value) => new ProgressEntry { key = key, kind = kind, value = value };

    [Test] public void RoundTrip_PreservesWalletAndCosmetics()
    {
        var before = Progress(Entry("coin", "int", "12345"), Entry("cosmetic_equipped_Hat", "string", "hat_cap"));
        Assert.That(GameProgressSnapshot.TryDecode(before.Encode(), "player-A", Keys, out var after, out _), Is.True);
        Assert.That(after.Int("coin"), Is.EqualTo(12345));
        Assert.That(after.Fingerprint, Is.EqualTo(before.Fingerprint));
    }
    [Test] public void OtherAccount_IsRejected() => Assert.That(GameProgressSnapshot.Validate(Progress(), "player-B", Keys, out _), Is.False);
    [TestCase("0", true)][TestCase("0.75", true)][TestCase("1", true)][TestCase("1.01", false)]
    public void ChapterBestProgress_UsesNormalizedBounds(string value, bool expected)
    {
        string key = ChapterRunProgress.BestKey(3);
        Assert.That(GameProgressSnapshot.Validate(Progress(Entry(key, "float", value)), "player-A", GameProgressStore.Keys(), out _), Is.EqualTo(expected));
    }
    [Test] public void DebugPreferences_AreRejected() => Assert.That(GameProgressSnapshot.Validate(Progress(Entry("TestPower9999", "int", "1")), "player-A", Keys, out _), Is.False);
    [Test] public void DuplicateKeys_AreRejected() => Assert.That(GameProgressSnapshot.Validate(Progress(Entry("coin", "int", "2"), Entry("coin", "int", "3")), "player-A", Keys, out _), Is.False);
    [TestCase("NaN")][TestCase("Infinity")][TestCase("-1")]
    public void InvalidNumbers_AreRejected(string value) => Assert.That(GameProgressSnapshot.Validate(Progress(Entry("upgrade_stat_ATT", "float", value)), "player-A", Keys, out _), Is.False);
    [TestCase("-1")][TestCase("2147483648")][TestCase("no")]
    public void InvalidWallet_IsRejected(string value) => Assert.That(GameProgressSnapshot.Validate(Progress(Entry("coin", "int", value)), "player-A", Keys, out _), Is.False);
    [TestCase("0")][TestCase("6")]
    public void UnauthoredChapters_AreRejected(string value) => Assert.That(GameProgressSnapshot.Validate(Progress(Entry("chapter_unlocked", "int", value)), "player-A", Keys, out _), Is.False);
    [Test] public void FutureSchema_IsRejected() { var data = Progress(); data.version = 2; Assert.That(GameProgressSnapshot.Validate(data, "player-A", Keys, out _), Is.False); }
    [Test] public void OversizedOrMalformedData_IsRejected()
    {
        Assert.That(GameProgressSnapshot.TryDecode(new byte[GameProgressSnapshot.MaxBytes + 1], "player-A", Keys, out _, out _), Is.False);
        Assert.That(GameProgressSnapshot.TryDecode(Encoding.UTF8.GetBytes("not json"), "player-A", Keys, out _, out _), Is.False);
    }
    [Test] public void Fingerprint_IgnoresTimestampAndEntryOrder()
    {
        var a = Progress(Entry("coin", "int", "7"), Entry("chapter_unlocked", "int", "2"));
        var b = Progress(Entry("chapter_unlocked", "int", "2"), Entry("coin", "int", "7"));
        b.updatedUtc = 10000;
        Assert.That(a.Fingerprint, Is.EqualTo(b.Fingerprint));
    }
}
