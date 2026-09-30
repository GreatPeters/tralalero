using UnityEngine;

// Keeps the SR18 encounters and fixed bonus pairs that the revamp set pieces replace hidden in the
// safe-copy scene. Placement data (Data.xlsx) re-enables scene objects on Play, so this runs after it.
[DefaultExecutionOrder(500)]
public sealed class NoryangjinRevampSuppressor : MonoBehaviour
{
    public GameObject[] targets = System.Array.Empty<GameObject>();
    private void LateUpdate()
    {
        foreach (var t in targets) if (t != null && t.activeSelf) t.SetActive(false);
    }
}
