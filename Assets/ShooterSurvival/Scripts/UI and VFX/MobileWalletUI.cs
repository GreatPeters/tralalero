using TMPro;
using UnityEngine;

public sealed class MobileWalletUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coins;
    [SerializeField] private TMP_Text jewels;
    private MoneyScript wallet;
    public void Configure(TMP_Text coinText, TMP_Text jewelText) { coins = coinText; jewels = jewelText; Bind(); Refresh(); }
    private void OnEnable() { Bind(); Refresh(); }
    private void OnDisable() { if (wallet != null) wallet.onChanged -= Refresh; wallet = null; }
    private void LateUpdate() { if (wallet != MoneyScript.S) { Bind(); Refresh(); } }
    private void Bind()
    {
        if (wallet == MoneyScript.S) return;
        if (wallet != null) wallet.onChanged -= Refresh;
        wallet = MoneyScript.S;
        if (wallet != null) wallet.onChanged += Refresh;
    }
    private void Refresh()
    {
        if (coins != null) coins.text = (wallet != null ? wallet.Coin : 0).ToString("N0");
        if (jewels != null) jewels.text = (wallet != null ? wallet.Jewel : 0).ToString("N0");
    }
}
