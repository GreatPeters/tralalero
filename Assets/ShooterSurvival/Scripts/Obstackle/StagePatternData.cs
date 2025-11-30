using UnityEngine;



[CreateAssetMenu(fileName = "StagePatternData", menuName = "Game/Stage Pattern Data")]
public class StagePatternData : ScriptableObject
{
    public ChapterData[] chapters = new ChapterData[10];
}

[System.Serializable]
public class ChapterData
{
    public StageData[] stages = new StageData[10];
}

[System.Serializable]
public class StageData
{
    public ObstaclePattern[] steps = new ObstaclePattern[15];
}



