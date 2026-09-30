using UnityEngine;

public sealed class NoryangjinNumberedHat : MonoBehaviour
{
    // Compatibility for archived scene snapshots only. No current installer creates this pickup.
    private void OnEnable() { if(Application.isPlaying)gameObject.SetActive(false); }
}
