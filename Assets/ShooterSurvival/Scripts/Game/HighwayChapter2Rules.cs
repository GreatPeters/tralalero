using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum HighwayVehicleKind { Sedan, Taxi, OneTon, Box, Tanker, Bus, Police, Tow, Accident }
public enum HighwayVehicleRoute { Common, Jam, Open }
public enum HighwayUniqueBonus { ChainMissile, GoldenShield, Shatter, CoinMagnet }
public enum HighwayPrimaryHazard { None, Accident, Swarm, Log, Deer, Hole, Work }

// Required workbook values: no hidden runtime balance fallback for chapter2.
public static class HighwayChapter2Data
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.Ordinal);
    public static float Value(string key)
    {
        if (!Names.TryGetValue(key, out string name)) Names.Add(key, name = "hwy2_" + key);
        if (!EnvironmentVariableTables.TryGetFloat(name, out float value) || float.IsNaN(value) || float.IsInfinity(value))
            throw new InvalidDataException("Data.xlsx 환경 변수 missing/invalid: " + name);
        return value;
    }
    public static int Count(string key) => Mathf.RoundToInt(Value(key));
    public static float Lane(float index) => (index - 1) * Value("laneWidth");
    public static float HealthMultiplier(HighwayVehicleKind kind) => Value("hp" + kind);
}

public static class HighwayChapter2Rules
{
    public static bool IsRushSegment(bool running,int roadChoice,float distance,float start,float end)
        =>running&&roadChoice==1&&distance>=start&&distance<end;
    public static bool ShowForkPaint(bool branch, float lateralOnMain, float branchOffset, float roadEdge, bool outerEdge)
    {
        if (branch) return lateralOnMain > roadEdge + .25f;
        return !outerEdge || branchOffset <= .05f || branchOffset >= roadEdge * 2 + .5f;
    }

    public static float ContactDamage(float remainingHealth, float maximumHealth, bool fastRoad, float fastFraction)
        => Mathf.Max(0, fastRoad ? maximumHealth * fastFraction : remainingHealth);

    public static float FollowingDistance(float desiredDistance, float currentDistance, float leaderDistance,
        float ownHalfLength, float leaderHalfLength, float gap)
        => Mathf.Min(currentDistance, Mathf.Max(desiredDistance, leaderDistance + ownHalfLength + leaderHalfLength + gap));

    public static bool Overlaps(float distanceA, float laneA, float halfLengthA, float halfWidthA,
        float distanceB, float laneB, float halfLengthB, float halfWidthB, float padding, float sidePadding = 0)
        => Mathf.Abs(distanceA - distanceB) < halfLengthA + halfLengthB + padding
        && Mathf.Abs(laneA - laneB) < halfWidthA + halfWidthB + sidePadding;

    public static bool LeavesPassage(float candidateLane, float candidateHalfWidth,
        IReadOnlyList<Vector2> occupied, float laneWidth, float playerRadius)
    {
        for (int i = -1; i <= 1; i++)
        {
            float lane = i * laneWidth;
            bool blocked = Mathf.Abs(lane - candidateLane) < candidateHalfWidth + playerRadius;
            foreach (var other in occupied) blocked |= Mathf.Abs(lane - other.x) < other.y + playerRadius;
            if (!blocked) return true;
        }
        return false;
    }

    public static int BonusOutcome(float roll, float large, float normal, float coins, float miss)
    {
        float total = large + normal + coins + miss;
        if (total <= 0) throw new ArgumentOutOfRangeException(nameof(large));
        float value = Mathf.Clamp01(roll) * total;
        if (value < large) return 0;
        if (value < large + normal) return 1;
        return value < large + normal + coins ? 2 : 3;
    }

    public static bool ConflictsWithWindow(float station, float from, float to, float separation)
        => station >= from - separation && station <= to + separation;
}
