using UnityEngine;

[CreateAssetMenu(fileName = "ObstaclePrefabs", menuName = "Game/Obstacle Prefabs")]
public class ObstaclePrefabs : ScriptableObject
{
    public ObstacleTypePrefab[] obstaclePrefabs;
}

[System.Serializable]
public class ObstacleTypePrefab
{
    public ObstaclePattern pattern;
    public GameObject prefab;
    public int poolSize = 10;
}
