using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class GameUIButtonSound : MonoBehaviour
{
    public GameSound sound = GameSound.Click;
    private Button button;
    private void Awake() { button=GetComponent<Button>();button.onClick.AddListener(Play); }
    private void Play() { if(button.interactable)GameAudioService.Play(sound); }
    private void OnDestroy() { if(button!=null)button.onClick.RemoveListener(Play); }
}
