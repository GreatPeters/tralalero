using UnityEngine;

public sealed class HarborSwipeHint : MonoBehaviour
{
    public RectTransform finger;
    public float amplitude=64f;
    public float period=1.6f;
    public float tiltDegrees=12f;
    private Vector2 origin;
    private Quaternion rotation;
    private float started;
    private void OnEnable() { started=Time.unscaledTime;if(finger!=null){origin=finger.anchoredPosition;rotation=finger.localRotation;} }
    private void Update() { if(finger==null)return;float wave=Mathf.Sin((Time.unscaledTime-started)*Mathf.PI*2/Mathf.Max(.2f,period));finger.anchoredPosition=origin+new Vector2(wave*amplitude,0);finger.localRotation=rotation*Quaternion.Euler(0,0,-wave*tiltDegrees); }
    private void OnDisable() { if(finger!=null){finger.anchoredPosition=origin;finger.localRotation=rotation;} }
}
