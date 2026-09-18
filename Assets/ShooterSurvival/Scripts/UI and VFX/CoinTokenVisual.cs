using UnityEngine;
using UnityEngine.Rendering;

// A camera-facing gold pickup matching the wallet art, using one sprite draw.
public sealed class CoinTokenVisual : MonoBehaviour
{
    private static Sprite sprite;
    private Camera view;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAssets() { sprite = null; }

    public static void Attach(Transform parent)
    {
        if (sprite == null) sprite = Resources.Load<Sprite>("VFX/GoldCoin");
        var root = new GameObject("Gold Coin", typeof(SpriteRenderer));
        root.transform.SetParent(parent, false);
        var renderer = root.GetComponent<SpriteRenderer>();renderer.sprite=sprite;
        if(sprite!=null)root.transform.localScale=Vector3.one*(1.35f/Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y));
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        root.AddComponent<CoinTokenVisual>();
    }

    private void LateUpdate()
    {
        if (view == null) view = Camera.main;
        if (view == null) return;
        transform.rotation = view.transform.rotation * Quaternion.Euler(0, Mathf.Sin(Time.time * 2.8f) * 10f, 0);
    }
}
