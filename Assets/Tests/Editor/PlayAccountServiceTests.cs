using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using IndianOceanAssets.ShooterSurvival.Account;
using NUnit.Framework;
using UnityEngine;

public sealed class PlayAccountServiceTests
{
    private sealed class Backend : IPlayProgressBackend
    {
        public string PlayerId => "__account_test_A__";
        public string DisplayName => "Test account";
        public bool IsAvailable => true;
        public bool IsAuthenticated => true;
        public event Action<GameProgressSnapshot, GameProgressSnapshot, Action<bool>> Conflict;
        public void TriggerConflict() => Conflict?.Invoke(Progress(1), Progress(2), _ => { });
        public GameProgressSnapshot Cloud;
        public string ReadError, DeleteError;
        public int Writes, Deletes;
        public bool HoldWrite;
        public Action<ProgressResult> PendingWrite;
        public void Authenticate(bool manual, Action<bool, string> done) => done(true, null);
        public void Read(Action<ProgressResult> done) => done(ReadError == null ? new ProgressResult { success = true, snapshot = Cloud } : ProgressResult.Failure(ReadError));
        public void Write(GameProgressSnapshot snapshot, string expected, Action<ProgressResult> done)
        {
            Writes++;
            if ((Cloud?.Fingerprint ?? "") != expected) { done(new ProgressResult { success = true, conflict = true, snapshot = Cloud }); return; }
            if (HoldWrite) { PendingWrite = done; return; }
            Cloud = snapshot;
            done(new ProgressResult { success = true, snapshot = snapshot });
        }
        public void Delete(Action<bool, string> done) { Deletes++; done(DeleteError == null, DeleteError); }
    }
    private GameObject go;
    private PlayAccountService service;
    private Backend backend;
    private bool lobby;
    private Dictionary<string, (string kind, bool exists, string value)> original;
    private const string Id = "__account_test_A__";
    private static GameProgressSnapshot Progress(int coin) => new GameProgressSnapshot {
        playerId = Id, entries = new[] { new ProgressEntry { key = "coin", kind = "int", value = coin.ToString(CultureInfo.InvariantCulture) } }
    };
    [SetUp] public void Setup()
    {
        var keys = GameProgressStore.Keys();
        foreach (var key in new[] { "play_account_owner", "play_account_base", "play_account_deletion_pending" }) keys[key] = "string";
        keys["play_account_auto_login"] = "int";
        original = new Dictionary<string, (string, bool, string)>();
        foreach (var pair in keys)
        {
            original[pair.Key] = (pair.Value, PlayerPrefs.HasKey(pair.Key), pair.Value == "int" ? PlayerPrefs.GetInt(pair.Key).ToString(CultureInfo.InvariantCulture)
                : pair.Value == "float" ? PlayerPrefs.GetFloat(pair.Key).ToString("R", CultureInfo.InvariantCulture) : PlayerPrefs.GetString(pair.Key));
            PlayerPrefs.DeleteKey(pair.Key);
        }
        lobby = true;
        backend = new Backend();
        go = new GameObject("Account test service");
        service = go.AddComponent<PlayAccountService>();
        service.Initialize(backend, _ => { }, () => lobby);
    }
    [TearDown] public void TearDown()
    {
        if (go != null) UnityEngine.Object.DestroyImmediate(go);
        foreach (var pair in original)
        {
            if (!pair.Value.exists) PlayerPrefs.DeleteKey(pair.Key);
            else if (pair.Value.kind == "int") PlayerPrefs.SetInt(pair.Key, int.Parse(pair.Value.value, CultureInfo.InvariantCulture));
            else if (pair.Value.kind == "float") PlayerPrefs.SetFloat(pair.Key, float.Parse(pair.Value.value, CultureInfo.InvariantCulture));
            else PlayerPrefs.SetString(pair.Key, pair.Value.value);
        }
        PlayerPrefs.Save();
        string path = Path.Combine(Application.persistentDataPath, "PlayAccount", GameProgressSnapshot.Hash(Id) + ".json");
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
    }
    [Test] public void Login_NewAccountKeepsGuestWallet() { PlayerPrefs.SetInt("coin", 123); service.Login(); Assert.That(service.Linked, Is.True); Assert.That(PlayerPrefs.GetInt("coin"), Is.EqualTo(123)); }
    [Test] public void SaveNow_GuestPreservesProgressWithoutClaimingCloudSave()
    {
        PlayerPrefs.SetInt("coin", 123);
        PlayerPrefs.SetFloat(ChapterRunProgress.BestKey(3), .75f);
        Assert.That(service.SaveNow(), Is.True);
        Assert.That(service.State, Is.EqualTo(PlayAccountState.SignedOut));
        Assert.That(service.DeviceSaveStatus, Does.StartWith("기기 저장 완료"));
        Assert.That(PlayerPrefs.GetInt("coin"), Is.EqualTo(123));
        Assert.That(GameProgressStore.Capture("").entries, Has.Some.Matches<ProgressEntry>(e => e.key == ChapterRunProgress.BestKey(3) && e.value == "0.75"));
        Assert.That(backend.Writes, Is.Zero);
    }
    [Test] public void SaveNow_LinkedLobbyUploadsLatestWallet()
    {
        service.Login(); PlayerPrefs.SetInt("coin", 321);
        Assert.That(service.SaveNow(), Is.True);
        Assert.That(backend.Cloud.Int("coin"), Is.EqualTo(321));
    }
    [Test] public void SaveNow_PausedRunFlushesLocallyWithoutCloudRestore()
    {
        service.Login(); lobby = false; PlayerPrefs.SetInt("coin", 123);
        Assert.That(service.SaveNow(), Is.True);
        Assert.That(backend.Writes, Is.Zero);
    }
    [Test] public void SaveNow_DeletionJournalPreventsSaveAndUpload()
    {
        service.Login(); PlayerPrefs.SetString("play_account_deletion_pending", Id);
        Assert.That(service.SaveNow(), Is.False);
        Assert.That(backend.Writes, Is.Zero);
    }
    [Test] public void LocalBackup_IsSerializedWithItsAccountIdentity()
    {
        PlayerPrefs.SetInt("coin", 123); service.Login();
        string path = Path.Combine(Application.persistentDataPath, "PlayAccount", GameProgressSnapshot.Hash(Id) + ".json");
        string data = File.ReadAllText(path);
        Assert.That(data, Does.Contain("\"snapshot\""));
        Assert.That(data, Does.Contain(Id));
        Assert.That(data.Length, Is.GreaterThan(20));
    }
    [Test] public void Login_ExistingCloudRestoresEmptyDevice() { backend.Cloud = Progress(400); service.Login(); Assert.That(PlayerPrefs.GetInt("coin"), Is.EqualTo(400)); Assert.That(service.State, Is.EqualTo(PlayAccountState.Ready)); }
    [Test] public void GuestAndCloudConflict_RequiresChoiceBeforeWriting()
    {
        PlayerPrefs.SetInt("coin", 50); backend.Cloud = Progress(400); service.Login();
        Assert.That(service.State, Is.EqualTo(PlayAccountState.Conflict)); Assert.That(backend.Writes, Is.Zero);
        service.ChooseProgress(false); Assert.That(PlayerPrefs.GetInt("coin"), Is.EqualTo(400));
    }
    [Test] public void LocalChoice_UsesWalletAsWholeSnapshotRatherThanAddingCoins()
    {
        PlayerPrefs.SetInt("coin", 50); backend.Cloud = Progress(400); service.Login(); service.ChooseProgress(true);
        Assert.That(backend.Cloud.Int("coin"), Is.EqualTo(50)); Assert.That(backend.Writes, Is.EqualTo(1));
    }
    [Test] public void CloudChangesWhileChoiceIsVisible_CannotOverwriteTheUnseenRevision()
    {
        PlayerPrefs.SetInt("coin", 50); backend.Cloud = Progress(400); service.Login();
        backend.Cloud = Progress(999); service.ChooseProgress(true);
        Assert.That(backend.Cloud.Int("coin"), Is.EqualTo(999));
        Assert.That(PlayerPrefs.GetInt("coin"), Is.EqualTo(50));
        Assert.That(service.State, Is.EqualTo(PlayAccountState.Error));
    }
    [Test] public void ReadFailure_DoesNotEnableUploadsOrChangeProgress()
    {
        PlayerPrefs.SetInt("coin", 77); backend.ReadError = "offline"; service.Login(); service.SyncNow();
        Assert.That(PlayerPrefs.GetInt("coin"), Is.EqualTo(77)); Assert.That(backend.Writes, Is.Zero);
    }
    [Test] public void LateCloudConflict_AfterReadFailureCannotReopenAnOldChoice()
    {
        backend.ReadError = "offline"; service.Login(); backend.TriggerConflict();
        Assert.That(service.State, Is.EqualTo(PlayAccountState.Error));
        Assert.That(service.FirstChoice, Is.Null);
    }
    [Test] public void DeleteFailure_PreservesWalletAndJournalsDeletionWithoutUploading()
    {
        PlayerPrefs.SetInt("coin", 77); service.Login(); backend.DeleteError = "offline"; service.DeleteAccount(); service.SyncNow();
        Assert.That(PlayerPrefs.GetInt("coin"), Is.EqualTo(77)); Assert.That(service.DeletionPending, Is.True); Assert.That(backend.Writes, Is.Zero);
    }
    [Test] public void ConfirmedDelete_ResetsGameProgressAndDisablesAutomaticRelinking()
    {
        PlayerPrefs.SetInt("coin", 77); service.Login(); service.DeleteAccount(); service.SyncNow();
        Assert.That(PlayerPrefs.HasKey("coin"), Is.False); Assert.That(service.State, Is.EqualTo(PlayAccountState.Deleted));
        Assert.That(PlayerPrefs.GetInt("play_account_auto_login", 1), Is.Zero); Assert.That(service.Linked, Is.False); Assert.That(backend.Writes, Is.Zero);
    }
    [Test] public void PausedRun_CannotLoginOrDelete()
    {
        PlayerPrefs.SetInt("coin", 77); service.Login(); lobby = false; service.DeleteAccount(); service.SyncNow();
        Assert.That(backend.Deletes, Is.Zero); Assert.That(backend.Writes, Is.Zero); Assert.That(PlayerPrefs.GetInt("coin"), Is.EqualTo(77));
    }
    [Test] public void LateWriteCallback_FromOldSessionCannotChangeNewSession()
    {
        service.Login(); PlayerPrefs.SetInt("coin", 10); backend.HoldWrite = true; service.SyncNow();
        Assert.That(service.State, Is.EqualTo(PlayAccountState.Saving));
        service.Initialize(new Backend(), _ => { }, () => lobby);
        backend.PendingWrite(new ProgressResult { success = true, snapshot = Progress(10) });
        Assert.That(service.State, Is.EqualTo(PlayAccountState.SignedOut)); Assert.That(PlayerPrefs.GetString("play_account_base"), Is.Empty);
    }
    [Test] public void DeletionJournalAfterRestart_BlocksRestoringOrReuploadingDeletedCloud()
    {
        PlayerPrefs.SetString("play_account_deletion_pending", Id); PlayerPrefs.SetString("play_account_owner", Id); PlayerPrefs.SetInt("coin", 77);
        service.Login(); service.SyncNow();
        Assert.That(service.State, Is.EqualTo(PlayAccountState.Error)); Assert.That(backend.Writes, Is.Zero); Assert.That(PlayerPrefs.GetInt("coin"), Is.EqualTo(77));
    }
}
