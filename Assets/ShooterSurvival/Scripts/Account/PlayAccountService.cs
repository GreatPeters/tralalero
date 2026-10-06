using System;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.ShooterSurvival.Account
{
    public enum PlayAccountState { SignedOut, Authenticating, Loading, Ready, Saving, Conflict, Deleting, Deleted, Error }

    public sealed class PlayAccountService : MonoBehaviour
    {
        private const string OwnerKey = "play_account_owner";
        private const string BaseKey = "play_account_base";
        private const string AutoLoginKey = "play_account_auto_login";
        private const string DeletionKey = "play_account_deletion_pending";
        private static readonly ConcurrentQueue<Action> MainThread = new ConcurrentQueue<Action>();
        public static PlayAccountService Instance { get; private set; }
        public event Action Changed;
        public PlayAccountState State { get; private set; } = PlayAccountState.SignedOut;
        public string Message { get; private set; } = "로그인하면 다른 기기에서도 진행을 이어갈 수 있습니다.";
        public string DeviceSaveStatus { get; private set; } = "기기에 자동 저장됩니다.";
        public bool CanSaveDevice => !IsBusy && State != PlayAccountState.Conflict && string.IsNullOrEmpty(PlayerPrefs.GetString(DeletionKey, ""));
        public string DisplayName => backend?.DisplayName ?? "";
        public string PlayerId => backend?.PlayerId ?? "";
        public GameProgressSnapshot FirstChoice { get; private set; }
        public GameProgressSnapshot SecondChoice { get; private set; }
        public string FirstLabel { get; private set; }
        public string SecondLabel { get; private set; }
        public bool IsBusy => State is PlayAccountState.Authenticating or PlayAccountState.Loading or PlayAccountState.Saving or PlayAccountState.Deleting;
        public bool BlocksGameplay => IsBusy || State == PlayAccountState.Conflict;
        public bool Linked => !string.IsNullOrEmpty(PlayerId) && PlayerPrefs.GetString(OwnerKey, "") == PlayerId && State != PlayAccountState.Deleted;
        public bool CanManage => inLobby() && !IsBusy && State != PlayAccountState.Conflict;
        public bool DeletionPending => !string.IsNullOrEmpty(PlayerId) && PlayerPrefs.GetString(DeletionKey, "") == PlayerId;
        public static bool IsInLobby
        {
            get
            {
                var canvas = FindFirstObjectByType<CanvasScript>();
                return canvas != null && canvas.startAreaImg != null && canvas.startAreaImg.gameObject.activeSelf && !TimeManager.isGameRunning && !CanvasScript.isGameOver;
            }
        }

        private IPlayProgressBackend backend;
        private Action<bool> resolveChoice;
        private float nextPoll, nextSync, readDeadline;
        private Action<GameProgressSnapshot> reloadProgress = ReloadProgressScene;
        private Func<bool> inLobby = () => IsInLobby;
        private int operation;
        private bool loaded;
        private string observedFingerprint;
        private string baseline => PlayerPrefs.GetString(BaseKey, "");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { Instance = null; while (MainThread.TryDequeue(out _)) { } }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot() => EnsureInstance();
        public static PlayAccountService EnsureInstance()
        {
            if (Instance == null) new GameObject("Google Play Account").AddComponent<PlayAccountService>();
            return Instance;
        }
        public static void Dispatch(Action callback) => MainThread.Enqueue(callback);
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            Initialize(new PlayGamesProgressBackend());
        }
        public void Initialize(IPlayProgressBackend progressBackend, Action<GameProgressSnapshot> restoreScene = null, Func<bool> lobby = null)
        {
            if (backend != null) backend.Conflict -= CloudConflict;
            backend = progressBackend ?? throw new ArgumentNullException(nameof(progressBackend));
            reloadProgress = restoreScene ?? ReloadProgressScene;
            inLobby = lobby ?? (() => IsInLobby);
            backend.Conflict += CloudConflict;
            loaded = false;
            operation++;
            SetState(PlayAccountState.SignedOut, "로그인하면 다른 기기에서도 진행을 이어갈 수 있습니다.");
        }
        private void Start()
        {
            if (backend.IsAvailable && PlayerPrefs.GetInt(AutoLoginKey, 1) == 1) Login(false);
        }
        private void OnDestroy()
        {
            operation++;
            if (backend != null) backend.Conflict -= CloudConflict;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            while (MainThread.TryDequeue(out var callback)) callback();
            if (State is PlayAccountState.Authenticating or PlayAccountState.Loading && Time.unscaledTime >= readDeadline)
            {
                operation++;
                Retry("연결이 지연되었습니다. 기기의 진행으로 플레이할 수 있습니다.");
            }
            if (!loaded || !Linked || Time.unscaledTime < nextPoll || IsBusy || State == PlayAccountState.Conflict) return;
            nextPoll = Time.unscaledTime + 5f;
            var local = GameProgressStore.Capture(PlayerId);
            if (local.Fingerprint != observedFingerprint)
            {
                observedFingerprint = local.Fingerprint;
                WriteLocal(local);
                nextSync = Time.unscaledTime + 10f;
            }
            if (local.Fingerprint != baseline && Time.unscaledTime >= nextSync) SyncNow();
        }
        private void OnApplicationPause(bool paused)
        {
            if (paused && loaded && Linked)
            {
                WriteLocal(GameProgressStore.Capture(PlayerId));
                if (!IsBusy && State != PlayAccountState.Conflict) SyncNow();
            }
        }
        private void OnApplicationFocus(bool focused)
        {
            if (focused && loaded && Linked && CanManage) SyncNow();
        }
        private void OnApplicationQuit()
        {
            if (loaded && Linked) WriteLocal(GameProgressStore.Capture(PlayerId));
        }
        private void SetState(PlayAccountState state, string message)
        {
            if (state is PlayAccountState.Authenticating or PlayAccountState.Loading) readDeadline = Time.unscaledTime + 20f;
            State = state; Message = message; Changed?.Invoke();
        }

        public void Login(bool manual = true)
        {
            if (IsBusy || State == PlayAccountState.Conflict || manual && !inLobby()) return;
            int token = ++operation;
            SetState(PlayAccountState.Authenticating, "Google Play에 로그인하는 중…");
            backend.Authenticate(manual, (success, error) => {
                if (token != operation) return;
                if (!success || string.IsNullOrEmpty(PlayerId)) { SetState(PlayAccountState.SignedOut, error ?? "로그인하지 못했습니다."); return; }
                PlayerPrefs.SetInt(AutoLoginKey, 1);
                if (DeletionPending) { loaded = false; SetState(PlayAccountState.Error, "계정 삭제가 완료되지 않았습니다. 삭제를 다시 시도해 주세요."); return; }
                LoadAccount(token);
            });
        }

        private void LoadAccount(int token)
        {
            SetState(PlayAccountState.Loading, "계정의 진행을 확인하는 중…");
            backend.Read(result => {
                if (token != operation) return;
                if (!result.success) { SetState(PlayAccountState.Error, result.error); return; }
                var owner = PlayerPrefs.GetString(OwnerKey, "");
                var local = GameProgressStore.Capture(owner);
                var remote = result.snapshot;
                if (!string.IsNullOrEmpty(owner) && owner != PlayerId)
                {
                    WriteLocal(local); // Keep the previous account isolated on disk.
                    var pending = ReadLocal(PlayerId);
                    if (pending != null && pending.snapshot.Fingerprint != (remote?.Fingerprint ?? "") && pending.snapshot.Fingerprint != pending.cloudFingerprint)
                    {
                        PresentChoice(pending.snapshot, remote ?? new GameProgressSnapshot { playerId = PlayerId }, "이 계정의 기기 진행 사용", "클라우드 진행 사용", keepPending => {
                            if (token != operation) return;
                            if (keepPending) { GameProgressStore.Apply(pending.snapshot, PlayerId); AttachLocal(pending.snapshot, remote?.Fingerprint ?? ""); Save(pending.snapshot); reloadProgress(pending.snapshot); }
                            else Attach(remote ?? new GameProgressSnapshot { playerId = PlayerId }, true);
                        });
                        return;
                    }
                    Attach(remote ?? new GameProgressSnapshot { playerId = PlayerId }, true);
                    return;
                }
                local.playerId = PlayerId;
                if (remote != null && remote.Fingerprint == local.Fingerprint) { Attach(remote, false); return; }
                if (remote != null && owner == PlayerId && remote.Fingerprint == baseline) { AttachLocal(local, remote.Fingerprint); return; }
                if (remote != null && (!local.HasProgress || owner == PlayerId && local.Fingerprint == baseline)) { Attach(remote, true); return; }
                if (remote == null && string.IsNullOrEmpty(owner)) { AttachLocal(local, ""); return; }
                PresentChoice(local, remote ?? new GameProgressSnapshot { playerId = PlayerId }, "기기 진행 사용", "클라우드 진행 사용", chooseLocal => {
                    if (token != operation) return;
                    if (chooseLocal) { AttachLocal(local, remote?.Fingerprint ?? ""); Save(local); }
                    else Attach(remote ?? new GameProgressSnapshot { playerId = PlayerId }, true);
                });
            });
        }

        private void AttachLocal(GameProgressSnapshot snapshot, string cloudFingerprint)
        {
            PlayerPrefs.SetString(OwnerKey, PlayerId);
            PlayerPrefs.SetString(BaseKey, cloudFingerprint);
            PlayerPrefs.Save();
            loaded = true;
            observedFingerprint = snapshot.Fingerprint;
            WriteLocal(snapshot);
            nextSync = Time.unscaledTime + 1f;
            SetState(PlayAccountState.Ready, "계정이 연결되었습니다.");
        }
        private void Attach(GameProgressSnapshot snapshot, bool restore)
        {
            if (restore && !inLobby()) { SetState(PlayAccountState.Error, "로비에서 계정을 다시 연결해 주세요."); return; }
            if (restore) GameProgressStore.Apply(snapshot, PlayerId);
            AttachLocal(snapshot, snapshot.Fingerprint);
            if (restore) reloadProgress(snapshot);
        }

        public void SyncNow()
        {
            if (!loaded || !Linked || DeletionPending || !inLobby() || IsBusy || State == PlayAccountState.Conflict) return;
            int token = ++operation;
            var submitted = GameProgressStore.Capture(PlayerId);
            WriteLocal(submitted);
            SetState(PlayAccountState.Loading, "계정 저장을 확인하는 중…");
            backend.Read(result => {
                if (token != operation) return;
                if (!result.success) { Retry(result.error); return; }
                var remote = result.snapshot;
                var local = GameProgressStore.Capture(PlayerId);
                if (remote?.Fingerprint == local.Fingerprint) { Saved(local); return; }
                if ((remote?.Fingerprint ?? "") == baseline) { Save(local); return; }
                // Loading another device's progress is deferred until a real lobby, not a pause.
                if (!inLobby()) { Retry("다른 기기의 저장이 있습니다. 로비에서 저장을 선택해 주세요."); return; }
                if (local.Fingerprint == baseline && remote != null) { Attach(remote, true); return; }
                PresentChoice(local, remote ?? new GameProgressSnapshot { playerId = PlayerId }, "기기 진행 사용", "클라우드 진행 사용", chooseLocal => {
                    if (token != operation) return;
                    if (chooseLocal) Save(GameProgressStore.Capture(PlayerId), remote?.Fingerprint ?? "");
                    else Attach(remote ?? new GameProgressSnapshot { playerId = PlayerId }, true);
                });
            });
        }
        public bool SaveNow()
        {
            if (!CanSaveDevice) return false;
            try
            {
                // Gameplay transactions already update the approved progression preferences.
                // Flush them even for a guest or a paused run; cloud restore stays lobby-only.
                PlayerPrefs.Save();
                DeviceSaveStatus = "기기 저장 완료 " + DateTime.Now.ToString("HH:mm");
            }
            catch (PlayerPrefsException)
            {
                DeviceSaveStatus = "기기에 저장하지 못했습니다. 저장 공간을 확인해 주세요.";
                Changed?.Invoke();
                return false;
            }
            Changed?.Invoke();
            if (loaded && Linked && inLobby()) SyncNow();
            return true;
        }
        private void Save(GameProgressSnapshot snapshot, string expectedFingerprint = null)
        {
            int token = ++operation;
            SetState(PlayAccountState.Saving, "진행을 저장하는 중…");
            backend.Write(snapshot, expectedFingerprint ?? baseline, result => {
                if (token != operation) return;
                if (result.conflict) { Retry("다른 기기의 저장이 갱신되었습니다. 다시 동기화해 주세요."); return; }
                if (!result.success) { Retry(result.error); return; }
                Saved(snapshot);
            });
        }
        private void Saved(GameProgressSnapshot snapshot)
        {
            PlayerPrefs.SetString(BaseKey, snapshot.Fingerprint);
            PlayerPrefs.Save();
            nextSync = Time.unscaledTime + 60f;
            SetState(PlayAccountState.Ready, "진행이 계정에 저장되었습니다.");
        }
        private void Retry(string error)
        {
            nextSync = Time.unscaledTime + 60f;
            SetState(PlayAccountState.Error, error ?? "동기화를 다시 시도해 주세요.");
        }
        private void CloudConflict(GameProgressSnapshot first, GameProgressSnapshot second, Action<bool> resolve)
        {
            if (State != PlayAccountState.Loading && State != PlayAccountState.Saving) return;
            var previous = State;
            int token = operation;
            PresentChoice(first, second, "저장 1 사용", "저장 2 사용", chooseFirst => {
                if (token != operation) return;
                SetState(previous, "선택한 저장을 불러오는 중…");
                resolve(chooseFirst);
            });
        }
        private void PresentChoice(GameProgressSnapshot first, GameProgressSnapshot second, string firstLabel, string secondLabel, Action<bool> selected)
        {
            FirstChoice = first; SecondChoice = second; FirstLabel = firstLabel; SecondLabel = secondLabel;
            resolveChoice = selected;
            SetState(PlayAccountState.Conflict, "저장이 서로 다릅니다. 이어갈 진행을 선택해 주세요. 선택하지 않은 진행은 덮어써집니다.");
            FindFirstObjectByType<HarborAccountPanel>(FindObjectsInactive.Include)?.OpenConflict();
        }
        public void ChooseProgress(bool first)
        {
            if (State != PlayAccountState.Conflict || resolveChoice == null || !inLobby()) return;
            var callback = resolveChoice;
            resolveChoice = null;
            FirstChoice = SecondChoice = null;
            callback(first);
        }

        public void DeleteAccount()
        {
            if (!CanManage || !backend.IsAuthenticated || !Linked || !loaded && !DeletionPending) return;
            int token = ++operation;
            string player = PlayerId;
            PlayerPrefs.SetString(DeletionKey, player);
            PlayerPrefs.Save();
            loaded = false; // Journal deletion before networking so crashes cannot upload old progress.
            SetState(PlayAccountState.Deleting, "클라우드의 게임 데이터를 삭제하는 중…");
            backend.Delete((success, error) => {
                if (token != operation) return;
                if (!success) { Retry(error); return; }
                // Remove the owned local backup before clearing the deletion journal.
                try
                {
                    var cache = LocalPath(player);
                    if (File.Exists(cache)) File.Delete(cache);
                    if (File.Exists(cache + ".tmp")) File.Delete(cache + ".tmp");
                }
                catch (IOException) { Retry("기기 백업 삭제를 완료하지 못했습니다. 계정 삭제를 다시 시도해 주세요."); return; }
                operation++;
                GameProgressStore.Apply(new GameProgressSnapshot { playerId = player }, player);
                PlayerPrefs.DeleteKey(OwnerKey);
                PlayerPrefs.DeleteKey(BaseKey);
                PlayerPrefs.DeleteKey(DeletionKey);
                PlayerPrefs.SetInt(AutoLoginKey, 0);
                PlayerPrefs.Save();
                SetState(PlayAccountState.Deleted, "게임 계정 데이터가 삭제되었습니다.");
                reloadProgress(new GameProgressSnapshot { playerId = player });
            });
        }

        private static string LocalPath(string player) => Path.Combine(Application.persistentDataPath, "PlayAccount", GameProgressSnapshot.Hash(player) + ".json");
        [Serializable] private sealed class LocalRecord { public GameProgressSnapshot snapshot; public string cloudFingerprint; }
        private static LocalRecord ReadLocal(string player)
        {
            try
            {
                string path = LocalPath(player);
                if (!File.Exists(path) || new FileInfo(path).Length > GameProgressSnapshot.MaxBytes) return null;
                var record = JsonUtility.FromJson<LocalRecord>(File.ReadAllText(path));
                return record != null && GameProgressSnapshot.Validate(record.snapshot, player, GameProgressStore.Keys(), out _) ? record : null;
            }
            catch (Exception) { return null; }
        }
        private static void WriteLocal(GameProgressSnapshot snapshot)
        {
            try
            {
                string path = LocalPath(snapshot.playerId);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(new LocalRecord { snapshot = snapshot, cloudFingerprint = PlayerPrefs.GetString(BaseKey, "") }));
                File.Copy(temp, path, true);
                File.Delete(temp);
            }
            catch (IOException) { Debug.LogWarning("[Account] Local backup could not be written; PlayerPrefs progress is retained."); }
        }
        private static void ReloadProgressScene(GameProgressSnapshot snapshot)
        {
            string[] scenes = { "Noryangjin_MapTool_Mode_SR18_Revamp", "HighWay", "RestStop", "Jamsil", "ShoeTower" };
            int chapter = Mathf.Clamp(snapshot.Int("chapter_last_played", 1), 1, snapshot.Int("chapter_unlocked", 1));
            string name = scenes[chapter - 1];
            LoadingOverlay.LoadScene(Application.CanStreamedLevelBeLoaded(name) ? name : SceneManager.GetActiveScene().name);
        }
    }
}
