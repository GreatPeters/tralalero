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


    private bool _isLoading;
    public void LoadStageObstacles()
    {
        // ✅ 같은 프레임/연속 호출로 2번 로드되는 케이스 방지
        if (_isLoading) return;
        _isLoading = true;

        ClearObstacles();

        if (patternData == null)
        {
            Debug.LogError("[StageObstacleManager] patternData is null");
            _isLoading = false;
            return;
        }

        var steps = patternData.chapters[chapterIndex].stages[stageIndex].steps;

        for (int i = 0; i < steps.Length; i++)
        {
            ObstacleDifficulty obstacleDifficulty = steps[i].obstacleDifficulty;
            ObstaclePattern pattern = steps[i].pattern;

            if (pattern == ObstaclePattern.None) continue;

            Vector3 pos = GetSpawnPosition(i, pattern);

            GameObject obj = ObstaclePooler.Instance.Get(pattern, pos);
            if (obj == null) continue; // ✅ null 가드 (여기 없으면 아래에서 터짐)

            // ✅ 중요: ClearObstacles가 Return하려면 Identifier가 반드시 있어야 함
            var id = obj.GetComponent<ObstacleIdentifier>();
            if (id == null) id = obj.AddComponent<ObstacleIdentifier>();
            id.pattern = pattern;

            // (풀러가 위치 세팅을 안 할 수도 있어서 안전하게 한번 더)
            obj.transform.position = pos;

            activeObstacles.Add(obj);
            if (ObstacleParent != null)
                obj.transform.SetParent(ObstacleParent.transform);

            // ✅ 풀 재사용 안정화: 자식 전부 OFF 후 필요한 것만 ON
            int childCount = obj.transform.childCount;
            for (int j = 0; j < childCount; j++)
                obj.transform.GetChild(j).gameObject.SetActive(false);

            int max = Mathf.Min(childCount, (int)obstacleDifficulty);
            for (int j = 0; j < max; j++)
                obj.transform.GetChild(j).gameObject.SetActive(true);
        }

        _isLoading = false;
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
                x = 0f;
                y = 0.56f;
                z += Random.Range(5f, 14f);
                //z = 0f;
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

            case ObstaclePattern.Oldman:
                // = 4.67f;
                y = -2.0f;
                z += 10f;
                break;


        }

        return new Vector3(x, y, z);
    }

    private bool IsSeaObstacle(ObstaclePattern p)
    {
        return p == ObstaclePattern.Ship
            || p == ObstaclePattern.Oldman
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
