using IndianOceanAssets.ShooterSurvival;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyStartShade : MonoBehaviour
{
    public CanvasScript owner;
    public Image shade;
    private void LateUpdate()
    {
        if(owner==null||shade==null)return;
        bool visible=owner.startAreaImg!=null&&owner.startAreaImg.gameObject.activeInHierarchy&&
            !TimeManager.isGameRunning&&!CanvasScript.isGameOver&&!OpeningStoryUI.IsBlockingGameplay;
        if(shade.enabled!=visible)shade.enabled=visible;
    }
}
