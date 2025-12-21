using UnityEngine;



[CreateAssetMenu(fileName = "StagePatternData", menuName = "Game/Stage Pattern Data")]
public class StagePatternData : ScriptableObject
{
    public ChapterData[] chapters = new ChapterData[10];
}


public enum ObstacleDifficulty { Easy = 1, Normal = 2, Hard = 3 }
[System.Serializable]
public struct StepData
{
    public ObstaclePattern pattern;
    public ObstacleDifficulty obstacleDifficulty;
}

[System.Serializable]
public class ChapterData
{
    public StageData[] stages = new StageData[10];
}

[System.Serializable]
public class StageData
{
    //public ObstaclePattern[] steps = new ObstaclePattern[6];
    public StepData[] steps = new StepData[6];
}



