using IndianOceanAssets.ShooterSurvival.Account;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HarborAccountPanel : MonoBehaviour
{
    public HarborSettingsPanel settings;
    public TMP_Text status;
    public Button loginButton, deleteButton, saveButton;
    public GameObject dialog;
    public TMP_Text dialogTitle, dialogBody;
    public Button firstButton, secondButton, cancelButton;
    private PlayAccountService service;
    private bool deletionPrompt;

    private void Awake()
    {
        service = PlayAccountService.EnsureInstance();
        status.richText = dialogBody.richText = false;
        EnsureSaveButton();
        saveButton.onClick.AddListener(SaveProgress);
        loginButton.onClick.AddListener(LoginOrSync);
        deleteButton.onClick.AddListener(ConfirmDeletion);
        firstButton.onClick.AddListener(First);
        secondButton.onClick.AddListener(Second);
        cancelButton.onClick.AddListener(Cancel);
        service.Changed += Refresh;
    }
    private void OnEnable() { if (Application.isPlaying) Refresh(); }
    private void OnDestroy() { if (service != null) service.Changed -= Refresh; }
    public void OpenConflict()
    {
        if (!PlayAccountService.IsInLobby) return;
        settings.Open(false);
        Refresh();
    }
    private static void Label(Button button, string value) => button.GetComponentInChildren<TMP_Text>(true).text = value;
    private void EnsureSaveButton()
    {
        if (saveButton == null)
        {
            saveButton = Instantiate(loginButton, loginButton.transform.parent);
            saveButton.name = "SaveUserProgress";
            saveButton.onClick = new Button.ButtonClickedEvent();
        }
        void Place(Button button, float left, float right)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(left, rect.anchorMin.y);
            rect.anchorMax = new Vector2(right, rect.anchorMax.y);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        Place(saveButton, .065f, .365f);
        Place(loginButton, .38f, .695f);
        Place(deleteButton, .710f, .935f);
        Label(saveButton, "지금 저장");
    }
    private void SaveProgress() { service.SaveNow(); Refresh(); }
    public void Refresh()
    {
        service ??= PlayAccountService.EnsureInstance();
        bool linked = service.Linked;
        string accountMessage = string.IsNullOrEmpty(service.DisplayName) || !linked ? service.Message : service.DisplayName + " / " + service.Message;
        status.text = service.DeviceSaveStatus + "\n" + accountMessage;
        Label(loginButton, service.DeletionPending ? "삭제 재시도" : linked ? "계정 저장" : "Google Play 로그인");
        saveButton.interactable = service.CanSaveDevice;
        loginButton.interactable = service.CanManage;
        deleteButton.interactable = service.CanManage && linked;
        if (service.State == PlayAccountState.Conflict)
        {
            deletionPrompt = false;
            dialog.SetActive(true);
            dialogTitle.text = "이어갈 진행 선택";
            dialogBody.text = service.Message + "\n\n" + "저장 1  " + service.FirstChoice?.Summary + "\n저장 2  " + service.SecondChoice?.Summary;
            Label(firstButton, service.FirstLabel);
            firstButton.GetComponent<Image>().color = Color.white;
            Label(secondButton, service.SecondLabel);
            cancelButton.gameObject.SetActive(false);
            firstButton.interactable = secondButton.interactable = PlayAccountService.IsInLobby;
        }
        else if (service.State == PlayAccountState.Deleting)
        {
            dialog.SetActive(true);
            dialogTitle.text = "게임 데이터 삭제 중";
            dialogBody.text = service.Message + "\n삭제를 확인할 때까지 잠시 기다려 주세요.";
            firstButton.interactable = secondButton.interactable = false;
            cancelButton.gameObject.SetActive(false);
        }
        else if (!deletionPrompt) dialog.SetActive(false);
    }
    private void LoginOrSync()
    {
        if (service.DeletionPending) ConfirmDeletion();
        else if (service.Linked) service.SyncNow();
        else service.Login();
    }
    private void ConfirmDeletion()
    {
        if (!service.CanManage || !service.Linked) return;
        deletionPrompt = true;
        dialog.SetActive(true);
        dialogTitle.text = "게임 계정 탈퇴";
        dialogBody.text = "이 게임의 클라우드 저장과 기기 진행을 삭제합니다.\n코인, 보석, 강화, 꾸미기, 챕터 진행은 복구할 수 없습니다.\n\nGoogle 계정 자체는 삭제되지 않습니다.\n다른 기기에서도 이 게임을 종료한 뒤 진행해 주세요.";
        Label(firstButton, "탈퇴 및 데이터 삭제");
        firstButton.GetComponent<Image>().color = new Color(1f, .82f, .78f);
        Label(secondButton, "취소");
        firstButton.interactable = secondButton.interactable = true;
        cancelButton.gameObject.SetActive(false);
    }
    private void First()
    {
        if (service.State == PlayAccountState.Conflict) service.ChooseProgress(true);
        else if (deletionPrompt) { deletionPrompt = false; service.DeleteAccount(); }
    }
    private void Second()
    {
        if (service.State == PlayAccountState.Conflict) service.ChooseProgress(false);
        else Cancel();
    }
    private void Cancel()
    {
        if (service.State is PlayAccountState.Conflict or PlayAccountState.Deleting) return;
        deletionPrompt = false;
        dialog.SetActive(false);
    }
}
