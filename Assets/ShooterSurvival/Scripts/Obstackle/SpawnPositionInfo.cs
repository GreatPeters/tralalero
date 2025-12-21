using UnityEngine;

[CreateAssetMenu(menuName="Game/SpawnPositionInfo")]
public class SpawnPositionInfo : ScriptableObject
{
    //public float slotStartZ;       // 첫 슬롯 시작 Z
    public float slotInterval;     // 슬롯 간격 (네가 정한 25)
    //public float obstacleLength;   // 장애물 길이 (HOLE 기준)
    //public float slotMargin;        // 앞뒤 여유 5

    // spawnZ 구하는 함수
    public float GetSpawnZ(int slotIndex)
    {
        // 슬롯 시작
        //float baseZ = slotStartZ + slotIndex * slotInterval * 2;

        // 장애물이 들어갈 위치 = baseZ + margin
        //return baseZ + slotMargin;

        return ((slotIndex + 1) * slotInterval * 2);
    }
}
