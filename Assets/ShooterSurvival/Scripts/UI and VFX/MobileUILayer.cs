using UnityEngine;

// Unity can clear overrideSorting when a nested canvas is authored while inactive.
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class MobileUILayer : MonoBehaviour
{
    [SerializeField] private int order = 100;
    public void Configure(int value) { order = value; Apply(); }
    private void OnEnable() => Apply();
    private void Apply()
    {
        var layer = GetComponent<Canvas>();
        layer.overrideSorting = true; layer.sortingOrder = order;
    }
}
