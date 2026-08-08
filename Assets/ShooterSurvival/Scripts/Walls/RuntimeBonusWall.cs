using UnityEngine;

public sealed class RuntimeBonusWall : MonoBehaviour
{
    [SerializeField] private bool removeWhenPreparingStage = true;

    public bool RemoveWhenPreparingStage => removeWhenPreparingStage;

    public void KeepAsMapAuthoredWall()
    {
        removeWhenPreparingStage = false;
    }
}
