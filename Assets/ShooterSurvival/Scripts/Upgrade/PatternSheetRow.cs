using System;

[Serializable]
public class PatternSheetRow
{
    public const int StepCount = 6;

    public int id;
    public int chapter;
    public int stage;
    public ObstaclePattern[] patterns = new ObstaclePattern[StepCount];
    public ObstacleDifficulty[] difficulties = new ObstacleDifficulty[StepCount];
    public string note;

    public override string ToString()
    {
        return $"id={id}, chapter={chapter}, stage={stage}";
    }
}
