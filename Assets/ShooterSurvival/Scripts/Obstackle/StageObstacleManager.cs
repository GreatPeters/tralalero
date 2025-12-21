using System.Collections.Generic;
using UnityEngine;

public class StageObstacleManager : MonoBehaviour
{
    [SerializeField] private StagePatternData patternData;
    [SerializeField] private int chapterIndex;
    [SerializeField] private int stageIndex;

    [SerializeField] private SpawnPositionInfo spawnPositionInfo; // ⬅ 새로 추가
    [SerializeField] private float groundY = 0f;
    [SerializeField] private float seaY = -2f;
    [SerializeField] private GameObject ObstacleParent;

    private readonly List<GameObject> activeObstacles = new();

    public void LoadStageObstacles()
    {
        ClearObstacles();

        // 이 스테이지의 15칸 패턴
        var steps = patternData.chapters[chapterIndex].stages[stageIndex].steps;

        for (int i = 0; i < steps.Length; i++)
        {
            ObstacleDifficulty obstacleDifficulty = steps[i].obstacleDifficulty;
            ObstaclePattern pattern = steps[i].pattern;
            Debug.Log(GetSpawnPosition(i, pattern));

            if (pattern == ObstaclePattern.None) continue;

            Vector3 pos = GetSpawnPosition(i, pattern);

            GameObject obj = ObstaclePooler.Instance.Get(pattern, pos);
            if (obj != null)
            {
                activeObstacles.Add(obj);
                obj.transform.SetParent(ObstacleParent.transform);
            }

            //난이도에 따라 오브젝트 활성화 비활성화 결정
            //for()
            //{
                //일단 다 활성화  
            //}

            int max = Mathf.Min(obj.transform.childCount, (int)obstacleDifficulty);
            for (int j = 0; j < max; j++)
            {
                obj.transform.GetChild(j).gameObject.SetActive(true);
            }
        }

        Debug.Log("된거야?");
    }

    private Vector3 GetSpawnPosition(int slotIndex, ObstaclePattern pattern)
    {
        float z = spawnPositionInfo.GetSpawnZ(slotIndex);   // 20 + 25*index + 5
        float y = IsSeaObstacle(pattern) ? seaY : groundY;
        float x = 0;

        switch (pattern)
        {
            case ObstaclePattern.Hole:
                x = (Random.Range(0f, 1f) <= 0.5f) ? -1f : 1f;
                y = 0.08f;
                z += Random.Range(5f, 10f);
                break;

            case ObstaclePattern.Oil:
                //x = (Random.Range(0f, 1f) <= 0.5f) ? -1f : 1f;
                y = 0.08f;
                z += Random.Range(0f, 7f);
                break;

            case ObstaclePattern.Seagull:
                //x = Random.Range(-0.9f, 0.9f);
                y = 8.78f;
                z += Random.Range(0f, 7f);
                break;

            case ObstaclePattern.Bucket:
                x = (Random.Range(0f, 1f));
                y = 1.16f;
                z += Random.Range(5f, 14f);
                break;

            case ObstaclePattern.Light:
                x = 0;
                y = 0;
                break;

            case ObstaclePattern.Dolphin:
                x = 0;
                y = 0;
                z -= 15f;
                break;

            case ObstaclePattern.Ship:
                x = 5.29f;
                y = 2.19f;
                z += Random.Range(0f, 8f);
                break;

            case ObstaclePattern.Oldman_Stab:
                // = 4.67f;
                y = -2.5f;
                z += 10f;
                break;


        }

        return new Vector3(x, y, z);
    }

    private bool IsSeaObstacle(ObstaclePattern p)
    {
        return p == ObstaclePattern.Ship
            || p == ObstaclePattern.Oldman_Stab
            || p == ObstaclePattern.Dolphin;
    }

    public void ClearObstacles()
    {
        foreach (var obj in activeObstacles)
        {
            var id = obj.GetComponent<ObstacleIdentifier>();
            if (id != null)
                ObstaclePooler.Instance.Return(id.pattern, obj);
        }
        activeObstacles.Clear();
    }

    public void SetStage(int chapter, int stage)
    {
        chapterIndex = chapter;
        stageIndex = stage;
    }
}
