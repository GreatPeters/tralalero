using UnityEngine;

[CreateAssetMenu(menuName="Game/SpawnPositionInfo")]
public class SpawnPositionInfo : ScriptableObject
{
    public float slotStartZ = 20f;       // 첫 슬롯 시작 Z
    public float slotInterval = 25f;     // 슬롯 간격 (네가 정한 25)
    public float obstacleLength = 15f;   // 장애물 길이 (HOLE 기준)
    public float slotMargin = 5f;        // 앞뒤 여유 5

    // spawnZ 구하는 함수
    public float GetSpawnZ(int slotIndex)
    {
        // 슬롯 시작
        float baseZ = slotStartZ + slotIndex * slotInterval;

        // 장애물이 들어갈 위치 = baseZ + margin
        return baseZ + slotMargin;
    }
}
