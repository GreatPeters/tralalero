using UnityEngine;

// Property blocks are not serialized. Store the choice and apply it to this instance on load.
public sealed class NoryangjinShopTint : MonoBehaviour
{
    public Color color = Color.white;
    private void Awake()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color); renderer.SetPropertyBlock(block);
        }
    }
}
