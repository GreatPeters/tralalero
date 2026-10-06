using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Chapter45Goal : MonoBehaviour
{
    public int floor;
    public bool offering;
    public Transform collectionVisual;
    public bool Claimed { get; private set; }
    private Chapter45Director director;
    private Vector3 restScale;
    public void ResetForRun(Chapter45Director owner)
    {
        StopAllCoroutines();
        director = owner; Claimed = false;
        if (collectionVisual != null)
        {
            if (restScale == Vector3.zero) restScale = collectionVisual.localScale;
            collectionVisual.localScale = restScale; collectionVisual.gameObject.SetActive(true);
        }
        foreach (var collider in GetComponents<Collider>()) collider.enabled = true;
    }
    private void OnTriggerEnter(Collider other) => TryClaim(other);
    private void OnTriggerStay(Collider other) => TryClaim(other);
    private void TryClaim(Collider other)
    {
        var player = other.GetComponentInParent<PlayerScript>();
        if (Claimed || !TimeManager.isGameRunning || TimeManager.timeFactor <= 0 || director == null || !director.Running || director.IsTransferring || director.CurrentFloor != floor || player != director.Player || !director.RequiredEncountersComplete) return;
        Claimed = true;
        if (collectionVisual != null) StartCoroutine(CollectVisual());
        director.ClaimGoal(this);
    }
    private System.Collections.IEnumerator CollectVisual()
    {
        for (float t = 0; t < .55f; t += Time.unscaledDeltaTime)
        {
            if (collectionVisual == null) yield break;
            collectionVisual.localScale = restScale * (1 + Mathf.Sin(t / .55f * Mathf.PI) * .25f) * (1 - t / .55f);
            yield return null;
        }
        if (collectionVisual != null) collectionVisual.gameObject.SetActive(false);
    }
}
