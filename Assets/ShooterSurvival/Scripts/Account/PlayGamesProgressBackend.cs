using System;
using System.Collections.Generic;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival.Account
{
    public sealed class ProgressResult
    {
        public bool success;
        public string error;
        public GameProgressSnapshot snapshot;
        public bool conflict;
        public static ProgressResult Failure(string error) => new ProgressResult { error = error };
    }

    public interface IPlayProgressBackend
    {
        string PlayerId { get; }
        string DisplayName { get; }
        bool IsAvailable { get; }
        bool IsAuthenticated { get; }
        event Action<GameProgressSnapshot, GameProgressSnapshot, Action<bool>> Conflict;
        void Authenticate(bool manual, Action<bool, string> completed);
        void Read(Action<ProgressResult> completed);
        void Write(GameProgressSnapshot snapshot, string expectedFingerprint, Action<ProgressResult> completed);
        void Delete(Action<bool, string> completed);
    }

    public sealed class PlayGamesProgressBackend : IPlayProgressBackend
    {
        public const string Slot = "tralalero_progress_v1";
        private int requestVersion;
        public event Action<GameProgressSnapshot, GameProgressSnapshot, Action<bool>> Conflict;
        public string PlayerId => IsAuthenticated ? PlayGamesPlatform.Instance.GetUserId() : "";
        public string DisplayName => IsAuthenticated ? PlayGamesPlatform.Instance.GetUserDisplayName() : "";
        public bool IsAuthenticated => IsAvailable && PlayGamesPlatform.Instance.IsAuthenticated();
        public bool IsAvailable
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                var config = PlayGamesSettings.LoadInstance();
                return config != null && !string.IsNullOrEmpty(config.AppId);
#else
                return false;
#endif
            }
        }

        public void Authenticate(bool manual, Action<bool, string> completed)
        {
            requestVersion++;
            if (!IsAvailable) { completed(false, Application.isEditor ? "Google Play 로그인은 Android에서 사용할 수 있습니다." : "Google Play 계정 연결을 준비 중입니다."); return; }
            PlayGamesPlatform.Activate();
            void Done(SignInStatus status) => completed(status == SignInStatus.Success, status == SignInStatus.Success ? null : "로그인하지 못했습니다. 다시 시도해 주세요.");
            if (manual) PlayGamesPlatform.Instance.ManuallyAuthenticate(Done);
            else PlayGamesPlatform.Instance.Authenticate(Done);
        }

        private void Open(Action<ProgressResult, ISavedGameMetadata> completed)
        {
            int version = ++requestVersion;
            if (!IsAuthenticated) { completed(ProgressResult.Failure("Google Play 로그인이 필요합니다."), null); return; }
            var client = PlayGamesPlatform.Instance.SavedGame;
            if (client == null) { completed(ProgressResult.Failure("클라우드 저장을 사용할 수 없습니다."), null); return; }
            client.OpenWithManualConflictResolution(Slot, DataSource.ReadNetworkOnly, true,
                (resolver, first, firstBytes, second, secondBytes) => {
                    if (version != requestVersion) return;
                    if (!Decode(firstBytes, out var a, out var error) || !Decode(secondBytes, out var b, out error))
                    {
                        completed(ProgressResult.Failure(error), null);
                        return; // Preserve both cloud versions rather than selecting corrupt data.
                    }
                    if (a == null) { resolver.ChooseMetadata(second); return; }
                    if (b == null || a.Fingerprint == b.Fingerprint) { resolver.ChooseMetadata(first); return; }
                    if (Conflict == null) { completed(ProgressResult.Failure("저장 충돌을 해결해야 합니다."), null); return; }
                    Conflict(a, b, chooseFirst => resolver.ChooseMetadata(chooseFirst ? first : second));
                },
                (status, metadata) => {
                    if (version != requestVersion) return;
                    if (status != SavedGameRequestStatus.Success) { completed(ProgressResult.Failure("저장을 불러오지 못했습니다. 기기의 진행은 유지됩니다."), null); return; }
                    client.ReadBinaryData(metadata, (readStatus, bytes) => {
                        if (version != requestVersion) return;
                        if (readStatus != SavedGameRequestStatus.Success) { completed(ProgressResult.Failure("저장 데이터를 읽지 못했습니다."), null); return; }
                        if (!Decode(bytes, out var snapshot, out var error)) { completed(ProgressResult.Failure(error), null); return; }
                        completed(new ProgressResult { success = true, snapshot = snapshot }, metadata);
                    });
                });
        }

        private bool Decode(byte[] bytes, out GameProgressSnapshot snapshot, out string error)
        {
            snapshot = null; error = null;
            return bytes == null || bytes.Length == 0 || GameProgressSnapshot.TryDecode(bytes, PlayerId, GameProgressStore.Keys(), out snapshot, out error);
        }

        public void Read(Action<ProgressResult> completed) => Open((result, _) => completed(result));

        public void Write(GameProgressSnapshot snapshot, string expectedFingerprint, Action<ProgressResult> completed)
        {
            if (!GameProgressSnapshot.Validate(snapshot, PlayerId, GameProgressStore.Keys(), out var error)) { completed(ProgressResult.Failure(error)); return; }
            if (snapshot.Encode().Length > GameProgressSnapshot.MaxBytes) { completed(ProgressResult.Failure("저장 데이터가 허용 크기를 초과했습니다.")); return; }
            Open((result, metadata) => {
                if (!result.success) { completed(result); return; }
                if (snapshot.playerId != PlayerId) { completed(ProgressResult.Failure("계정이 바뀌었습니다. 다시 로그인해 주세요.")); return; }
                if ((result.snapshot?.Fingerprint ?? "") != (expectedFingerprint ?? ""))
                {
                    result.conflict = true;
                    completed(result);
                    return;
                }
                var update = new SavedGameMetadataUpdate.Builder().WithUpdatedDescription("Tralalero Shooter · " + snapshot.Summary).Build();
                PlayGamesPlatform.Instance.SavedGame.CommitUpdate(metadata, update, snapshot.Encode(), (status, _) =>
                    completed(status == SavedGameRequestStatus.Success ? new ProgressResult { success = true, snapshot = snapshot }
                        : ProgressResult.Failure("클라우드 저장을 완료하지 못했습니다. 기기의 진행은 유지됩니다.")));
            });
        }

        public void Delete(Action<bool, string> completed)
        {
            requestVersion++;
            if (!IsAuthenticated) { completed(false, "Google Play 로그인이 필요합니다."); return; }
            if (Application.internetReachability == NetworkReachability.NotReachable) { completed(false, "인터넷 연결 후 계정 탈퇴를 다시 시도해 주세요."); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
            // The official Unity SDK's Delete has no completion callback. Use the same
            // native SnapshotsClient Task API so failure cannot erase the local account.
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var games = new AndroidJavaClass("com.google.android.gms.games.PlayGames"))
                {
                    var client = games.CallStatic<AndroidJavaObject>("getSnapshotsClient", activity);
                    DeleteFrom(client, completed);
                }
            }
            catch (Exception) { completed(false, "클라우드 데이터 삭제를 시작하지 못했습니다."); }
#else
            completed(false, "계정 탈퇴는 로그인한 Android 기기에서 사용할 수 있습니다.");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void LoadSlots(AndroidJavaObject client, Action<List<AndroidJavaObject>> loaded, Action failed)
        {
            using (var task = client.Call<AndroidJavaObject>("load", true))
            {
                task.Call<AndroidJavaObject>("addOnSuccessListener", new SuccessObject(value => {
                    if (value.Call<bool>("isStale")) { failed(); return; }
                    var slots = new List<AndroidJavaObject>();
                    using (var buffer = value.Call<AndroidJavaObject>("get"))
                    {
                        int count = buffer.Call<int>("getCount");
                        for (int i = 0; i < count; i++)
                        {
                            var metadata = buffer.Call<AndroidJavaObject>("get", i);
                            if (metadata.Call<string>("getUniqueName") == Slot) slots.Add(metadata);
                            else metadata.Dispose();
                        }
                        buffer.Call("release");
                    }
                    loaded(slots);
                }, failed)).Dispose();
                task.Call<AndroidJavaObject>("addOnFailureListener", new Failure(failed)).Dispose();
            }
        }

        private static void DeleteFrom(AndroidJavaObject client, Action<bool, string> completed)
        {
            void Fail() { client.Dispose(); PlayAccountService.Dispatch(() => completed(false, "클라우드 삭제를 확인하지 못했습니다. 데이터는 기기에 유지됩니다.")); }
            LoadSlots(client, slots => {
                void Verify() => LoadSlots(client, remaining => {
                    bool removed = remaining.Count == 0;
                    foreach (var metadata in remaining) metadata.Dispose();
                    client.Dispose();
                    PlayAccountService.Dispatch(() => completed(removed, removed ? null : "클라우드 삭제를 확인하지 못했습니다."));
                }, Fail);
                void Next(int i)
                {
                    if (i == slots.Count) { Verify(); return; }
                    using (var task = client.Call<AndroidJavaObject>("delete", slots[i]))
                    {
                        slots[i].Dispose();
                        task.Call<AndroidJavaObject>("addOnSuccessListener", new SuccessString(() => Next(i + 1), Fail)).Dispose();
                        task.Call<AndroidJavaObject>("addOnFailureListener", new Failure(Fail)).Dispose();
                    }
                }
                Next(0);
            }, Fail);
        }

        [UnityEngine.Scripting.Preserve]
        private sealed class SuccessObject : AndroidJavaProxy
        {
            private readonly Action<AndroidJavaObject> done;
            private readonly Action failed;
            public SuccessObject(Action<AndroidJavaObject> done, Action failed) : base("com.google.android.gms.tasks.OnSuccessListener") { this.done = done; this.failed = failed; }
            [UnityEngine.Scripting.Preserve]
            public void onSuccess(AndroidJavaObject result) { try { using (result) done(result); } catch (Exception) { failed(); } }
        }
        [UnityEngine.Scripting.Preserve]
        private sealed class SuccessString : AndroidJavaProxy
        {
            private readonly Action done, failed;
            public SuccessString(Action done, Action failed) : base("com.google.android.gms.tasks.OnSuccessListener") { this.done = done; this.failed = failed; }
            [UnityEngine.Scripting.Preserve]
            public void onSuccess(string result) { try { done(); } catch (Exception) { failed(); } }
        }
        [UnityEngine.Scripting.Preserve]
        private sealed class Failure : AndroidJavaProxy
        {
            private readonly Action done;
            public Failure(Action done) : base("com.google.android.gms.tasks.OnFailureListener") { this.done = done; }
            [UnityEngine.Scripting.Preserve]
            public void onFailure(AndroidJavaObject error) { using (error) done(); }
        }
#endif
    }
}
